using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    /// <summary>
    /// Production-level logging, performance-sensitive applications. Integration with System: Unified Logging System, Console app, log command-line
    /// There is used os_log API that is part of Apple's unified logging system and is specifically designed for macOS, iOS, watchOS, and tvOS. View All Logs with Command Line: log show --predicate 'subsystem == "com.yourcompany.unitygame"' --info
    /// os_log_t customLog = os_log_create("com.yourcompany.unitygame", "Unity"); os_log(customLog, "Subsystem log: %{public}s", "Hello, subsystem!");
    /// </summary>
    public class AppleSystemLogTarget : LogTarget
    {
        [Preserve]
        public AppleSystemLogTarget(string subSystem, string mainCategory)
            : base()
        {
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            TheBestLogger_initAppleSystemLogger(subSystem, mainCategory);
#endif
        }

        public override string LogTargetConfigurationName => nameof(AppleSystemLogTargetConfiguration);

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null)
        {

#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            switch (level)
            {
                case LogLevel.Debug:
                {
                    TheBestLogger_AppleSystemLogDebug(category, message);
                    break;
                }
                case LogLevel.Info:
                {
                    TheBestLogger_AppleSystemLogInfo(category, message);
                    break;
                }
                case LogLevel.Warning:
                {
                    TheBestLogger_AppleSystemLogDefault(category, message);
                    break;
                }
                case LogLevel.Error:
                {
                    TheBestLogger_AppleSystemLogError(category, message);
                    break;
                }
                case LogLevel.Exception:
                {
                    if (exception != null)
                    {
                        TheBestLogger_AppleSystemLogError(category, exception.Message + "\n" + logAttributes.StackTrace ?? exception.StackTrace);
                        return;
                    }
                    TheBestLogger_AppleSystemLogError(category, message);
                    break;
                }
                default:
                {
                    TheBestLogger_AppleSystemLogDefault(category, message);
                    break;
                }
            }
#endif
        }

        public override void LogBatch(
            IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> logBatch)
        {
            foreach (var b in logBatch)
            {
                Log(b.level, b.category, b.message, b.logAttributes, b.exception);
            }
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
