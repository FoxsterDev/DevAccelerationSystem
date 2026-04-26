using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    internal interface IAppleSystemLogBridge
    {
        void Initialize(string subsystem, string category);
        void LogDefault(string category, string message);
        void LogInfo(string category, string message);
        void LogDebug(string category, string message);
        void LogError(string category, string message);
    }

    internal sealed class AppleSystemNativeLogBridge : IAppleSystemLogBridge
    {
        public void Initialize(string subsystem, string category)
        {
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            TheBestLogger_initAppleSystemLogger(subsystem, category);
#endif
        }

        public void LogDefault(string category, string message)
        {
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            TheBestLogger_AppleSystemLogDefault(category, message);
#endif
        }

        public void LogInfo(string category, string message)
        {
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            TheBestLogger_AppleSystemLogInfo(category, message);
#endif
        }

        public void LogDebug(string category, string message)
        {
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            TheBestLogger_AppleSystemLogDebug(category, message);
#endif
        }

        public void LogError(string category, string message)
        {
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            TheBestLogger_AppleSystemLogError(category, message);
#endif
        }
    }

    internal enum AppleSystemLogMethod
    {
        Default,
        Info,
        Debug,
        Error
    }

    /// <summary>
    /// Production-level logging, performance-sensitive applications. Integration with System: Unified Logging System, Console app, log command-line
    /// There is used os_log API that is part of Apple's unified logging system and is specifically designed for macOS, iOS, watchOS, and tvOS. View All Logs with Command Line: log show --predicate 'subsystem == "com.yourcompany.unitygame"' --info
    /// os_log_t customLog = os_log_create("com.yourcompany.unitygame", "Unity"); os_log(customLog, "Subsystem log: %{public}s", "Hello, subsystem!");
    /// </summary>
    public class AppleSystemLogTarget : LogTarget
    {
        private static readonly IAppleSystemLogBridge DefaultBridge = new AppleSystemNativeLogBridge();
        internal static IAppleSystemLogBridge Bridge = DefaultBridge;

        [Preserve]
        public AppleSystemLogTarget(string subSystem, string mainCategory)
            : base()
        {
            Bridge?.Initialize(subSystem, mainCategory);
        }

        public override string LogTargetConfigurationName => nameof(AppleSystemLogTargetConfiguration);

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null)
        {
            switch (MapLogLevel(level))
            {
                case AppleSystemLogMethod.Debug:
                {
                    Bridge?.LogDebug(category, message);
                    break;
                }
                case AppleSystemLogMethod.Info:
                {
                    Bridge?.LogInfo(category, message);
                    break;
                }
                case AppleSystemLogMethod.Default:
                {
                    Bridge?.LogDefault(category, message);
                    break;
                }
                case AppleSystemLogMethod.Error:
                {
                    if (exception != null)
                    {
                        Bridge?.LogError(category, BuildExceptionMessage(exception, logAttributes));
                        return;
                    }
                    Bridge?.LogError(category, message);
                    break;
                }
                default:
                {
                    Bridge?.LogDefault(category, message);
                    break;
                }
            }
        }

        public override void LogBatch(
            IReadOnlyList<LogEntry> logBatch)
        {
            if (logBatch == null)
            {
                return;
            }

            foreach (var b in logBatch)
            {
                Log(b.Level, b.Category, b.Message, b.Attributes, b.Exception);
            }
        }

        internal static AppleSystemLogMethod MapLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => AppleSystemLogMethod.Debug,
                LogLevel.Info => AppleSystemLogMethod.Info,
                LogLevel.Warning => AppleSystemLogMethod.Default,
                LogLevel.Error => AppleSystemLogMethod.Error,
                LogLevel.Exception => AppleSystemLogMethod.Error,
                _ => AppleSystemLogMethod.Default
            };
        }

        internal static string BuildExceptionMessage(Exception exception, LogAttributes logAttributes)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            var stackTrace = !string.IsNullOrEmpty(logAttributes?.StackTrace)
                                 ? logAttributes.StackTrace
                                 : exception.StackTrace;

            return string.IsNullOrEmpty(stackTrace)
                       ? exception.Message ?? string.Empty
                       : (exception.Message ?? string.Empty) + "\n" + stackTrace;
        }

        internal static void ResetTestHooks()
        {
            Bridge = DefaultBridge;
        }

#if (UNITY_IOS || UNITY_STANDALONE_OSX)  && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void TheBestLogger_initAppleSystemLogger(string subsystem, string category);

        [DllImport("__Internal")]
        private static extern void TheBestLogger_AppleSystemLogDefault(string category, string message);

        [DllImport("__Internal")]
        private static extern void TheBestLogger_AppleSystemLogInfo(string category, string message);

        [DllImport("__Internal")]
        private static extern void TheBestLogger_AppleSystemLogDebug(string category, string message);

        [DllImport("__Internal")]
        private static extern void TheBestLogger_AppleSystemLogError(string category, string message);

        [DllImport("__Internal")]
        private static extern void TheBestLogger_AppleSystemLogFault(string category, string message);

        [DllImport("__Internal")]
        private static extern void TheBestLogger_AppleSystemLogFormatted(string category,
                                                                         string format,
                                                                         string arg1);
#endif

    }
}
