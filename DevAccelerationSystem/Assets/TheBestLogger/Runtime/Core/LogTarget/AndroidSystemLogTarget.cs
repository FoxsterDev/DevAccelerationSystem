using System;
using System.Collections.Generic;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    internal interface IAndroidSystemLogBridge
    {
        void Initialize(string globalTag);
        void Log(AndroidSystemLogMethod method, string message);
    }

    internal sealed class AndroidSystemNativeLogBridge : IAndroidSystemLogBridge
    {
#if UNITY_ANDROID
        private static IntPtr AndroidLogClass;
        private static IntPtr LogVMethodID;
        private static IntPtr LogDMethodID;
        private static IntPtr LogIMethodID;
        private static IntPtr LogWMethodID;
        private static IntPtr LogEMethodID;
        private static IntPtr GlobalTagJString;
#endif

        public void Initialize(string globalTag)
        {
#if UNITY_ANDROID
            // Find the android.util.Log class. Note the use of "/" instead of ".".
            var logClass = AndroidJNI.FindClass("android/util/Log");
            if (logClass == IntPtr.Zero)
            {
                Diagnostics.Write("NativeAndroidLogger: Could not find android.util.Log class.");
                return;
            }

            AndroidLogClass = AndroidJNI.NewGlobalRef(logClass);
            AndroidJNI.DeleteLocalRef(logClass);

            const string logMethodSignature = "(Ljava/lang/String;Ljava/lang/String;)I";
            LogVMethodID = AndroidJNI.GetStaticMethodID(AndroidLogClass, "v", logMethodSignature);
            LogDMethodID = AndroidJNI.GetStaticMethodID(AndroidLogClass, "d", logMethodSignature);
            LogIMethodID = AndroidJNI.GetStaticMethodID(AndroidLogClass, "i", logMethodSignature);
            LogWMethodID = AndroidJNI.GetStaticMethodID(AndroidLogClass, "w", logMethodSignature);
            LogEMethodID = AndroidJNI.GetStaticMethodID(AndroidLogClass, "e", logMethodSignature);

            var localTagString = AndroidJNI.NewStringUTF(globalTag ?? "Unity");
            GlobalTagJString = AndroidJNI.NewGlobalRef(localTagString);
            AndroidJNI.DeleteLocalRef(localTagString);
#endif
        }

        public void Log(AndroidSystemLogMethod method, string message)
        {
#if UNITY_ANDROID
            if (AndroidLogClass == IntPtr.Zero)
            {
                Diagnostics.Write("NativeAndroidLogger: AndroidLogClass == IntPtr.Zero.");
                return;
            }

            var jniArgs = new jvalue[2];
            var messageJavaString = IntPtr.Zero;

            try
            {
                messageJavaString = AndroidJNI.NewStringUTF(message);
                jniArgs[0].l = GlobalTagJString;
                jniArgs[1].l = messageJavaString;

                var methodId = method switch
                {
                    AndroidSystemLogMethod.Debug => LogDMethodID,
                    AndroidSystemLogMethod.Info => LogIMethodID,
                    AndroidSystemLogMethod.Warning => LogWMethodID,
                    AndroidSystemLogMethod.Error => LogEMethodID,
                    _ => IntPtr.Zero
                };

                if (methodId != IntPtr.Zero)
                {
                    AndroidJNI.CallStaticIntMethod(AndroidLogClass, methodId, jniArgs);
                }
            }
            finally
            {
                if (messageJavaString != IntPtr.Zero)
                {
                    AndroidJNI.DeleteLocalRef(messageJavaString);
                }
            }
#endif
        }
    }

    internal enum AndroidSystemLogMethod
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class AndroidSystemLogTarget : LogTarget
    {
        private static readonly IAndroidSystemLogBridge DefaultBridge = new AndroidSystemNativeLogBridge();
        internal static IAndroidSystemLogBridge Bridge = DefaultBridge;

        [Preserve]
        public AndroidSystemLogTarget(string globalTag)
        {
            Bridge?.Initialize(globalTag);
        }

        public override string LogTargetConfigurationName => nameof(AndroidSystemLogTargetConfiguration);

        /// <summary>
        /// Writes a log message to the native Android Logcat console using pre-cached JNI method IDs.
        /// This method will be ignored in the Unity Editor and on non-Android platforms.
        /// </summary>
        /// <param name="level">The severity level of the log (e.g., Debug, Error).</param>
        /// <param name="category">The log category (tag) to be used in Logcat.</param>
        /// <param name="message">The main log message.</param>
        /// <param name="logAttributes">Optional structured data. (Currently not serialized in this implementation).</param>
        /// <param name="exception">An optional exception to include in the log.</param>
        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes = null,
                                 Exception exception = null)
        {
            message = BuildMessagePayload(category, message, logAttributes, exception);
            Bridge?.Log(MapLogLevel(level), message);
        }

        public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            if (logBatch == null)
            {
                return;
            }

            foreach (var entry in logBatch)
            {
                Log(entry.Level, entry.Category, entry.Message, entry.Attributes, entry.Exception);
            }
        }

        internal static AndroidSystemLogMethod MapLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => AndroidSystemLogMethod.Debug,
                LogLevel.Info => AndroidSystemLogMethod.Info,
                LogLevel.Warning => AndroidSystemLogMethod.Warning,
                LogLevel.Error => AndroidSystemLogMethod.Error,
                LogLevel.Exception => AndroidSystemLogMethod.Error,
                _ => AndroidSystemLogMethod.Debug
            };
        }

        internal static string BuildMessagePayload(string category,
                                                   string message,
                                                   LogAttributes logAttributes,
                                                   Exception exception)
        {
            category ??= string.Empty;
            message = message == null
                          ? string.Empty
                          : StringOperations.Concat("[", category, "] ", message, logAttributes?.ToFlatString() ?? string.Empty);

            if (exception != null)
            {
                message = $"{message}\n--- Exception ---\n{exception}";
            }

            return message;
        }

        internal static void ResetTestHooks()
        {
            Bridge = DefaultBridge;
        }
    }
}
