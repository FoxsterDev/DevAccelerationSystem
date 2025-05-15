using System;
using System.Diagnostics;
using UnityEngine;

namespace TheBestLogger
{
    public class Logger
    {
        private static ILogger DefaultGameLogger => LogManager.CreateLogger(nameof(DefaultGameLogger));

        [HideInCallstack]
        public static void LogException(Exception ex, LogAttributes attributes)
        {
            DefaultGameLogger.LogException(ex, attributes);
        }

        [HideInCallstack]
        public static void LogError(string message, LogAttributes attributes)
        {
            DefaultGameLogger.LogError(message, attributes);
        }

        [HideInCallstack]
        public static void LogWarning(string message, LogAttributes attributes)
        {
            DefaultGameLogger.LogWarning(message, attributes);
        }

        [HideInCallstack]
        public static void LogInfo(string message, LogAttributes attributes)
        {
            DefaultGameLogger.LogInfo(message, attributes);
        }

        [HideInCallstack]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(string message, LogAttributes attributes)
        {
            DefaultGameLogger.LogDebug(message, attributes);
        }
    }
}
