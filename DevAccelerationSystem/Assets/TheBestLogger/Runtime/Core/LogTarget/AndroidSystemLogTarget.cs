using System;
using System.Collections.Generic;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    public class AndroidSystemLogTarget : LogTarget
    {
        private static IntPtr AndroidLogClass; // Pointer to the android.util.Log class
        private static IntPtr LogVMethodID; // Pointer to the 'v' (verbose) method
        private static IntPtr LogDMethodID; // Pointer to the 'd' (debug) method
        private static IntPtr LogIMethodID; // Pointer to the 'i' (info) method
        private static IntPtr LogWMethodID; // Pointer to the 'w' (warning) method
        private static IntPtr LogEMethodID; // Pointer to the 'e' (error) method
        private static IntPtr GlobalTagJString;

        [Preserve]
        public AndroidSystemLogTarget(string globalTag)
        {
#if UNITY_ANDROID
            // Find the android.util.Log class. Note the use of "/" instead of ".".
            var logClass = AndroidJNI.FindClass("android/util/Log");
            if (logClass == IntPtr.Zero)
            {
                Diagnostics.Write("NativeAndroidLogger: Could not find android.util.Log class.");
                return;
            }

            // We use NewGlobalRef to prevent the class reference from being garbage collected.
            AndroidLogClass = AndroidJNI.NewGlobalRef(logClass);
            // We can delete the local reference now that we have a global one.
            AndroidJNI.DeleteLocalRef(logClass);

            // The JNI signature for all these methods is "(Ljava/lang/String;Ljava/lang/String;)I", which means:
            // - Takes two String arguments (the tag and the message).
            // - Returns an int.
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
#if UNITY_ANDROID 
            if (AndroidLogClass == IntPtr.Zero)
            {
                Diagnostics.Write("NativeAndroidLogger: AndroidLogClass == IntPtr.Zero.");
                return;
            }

            if (category == null)
            {
                category = string.Empty;
            }

            if (message == null)
            {
                message = string.Empty;
            }
            else
            {
                message = StringOperations.Concat("[", category, "] ", message, logAttributes?.ToFlatString() ?? string.Empty);
            }

            if (exception != null)
            {
                message = $"{message}\n--- Exception ---\n{exception}";
            }

            var jniArgs = new jvalue[2];
            var messageJavaString = IntPtr.Zero;

            try
            {
                messageJavaString = AndroidJNI.NewStringUTF(message);
                jniArgs[0].l = GlobalTagJString;
                jniArgs[1].l = messageJavaString;

                switch (level)
                {
                    case LogLevel.Debug:
                        if (LogDMethodID != IntPtr.Zero)
                        {
                            AndroidJNI.CallStaticIntMethod(AndroidLogClass, LogDMethodID, jniArgs);
                        }

                        break;
                    case LogLevel.Info:
                        if (LogIMethodID != IntPtr.Zero)
                        {
                            AndroidJNI.CallStaticIntMethod(AndroidLogClass, LogIMethodID, jniArgs);
                        }

                        break;
                    case LogLevel.Warning:
                        if (LogWMethodID != IntPtr.Zero)
                        {
                            AndroidJNI.CallStaticIntMethod(AndroidLogClass, LogWMethodID, jniArgs);
                        }

                        break;
                    case LogLevel.Error:
                    case LogLevel.Exception:
                        if (LogEMethodID != IntPtr.Zero)
                        {
                            AndroidJNI.CallStaticIntMethod(AndroidLogClass, LogEMethodID, jniArgs);
                        }
                        break;
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

        public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            foreach (var entry in logBatch)
            {
                Log(entry.Level, entry.Category, entry.Message, entry.Attributes, entry.Exception);
            }
        }
    }
}
