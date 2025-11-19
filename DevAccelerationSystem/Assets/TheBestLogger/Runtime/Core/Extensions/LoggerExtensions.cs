using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TheBestLogger
{
    public static class LoggerExtensions
    {
        [HideInCallstack]
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogTrace(this ILogger logger, string message, LogAttributes logAttributes = null)
        {
            logger.LogDebug(message, logAttributes);
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug<TArg1>(this ILogger logger, string message, LogAttributes logAttributes, in TArg1 arg1)
        {
            logger.LogFormat(LogLevel.Debug, message, logAttributes, arg1);
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug<TArg1, TArg2>(this ILogger logger, string message, LogAttributes logAttributes, in TArg1 arg1, in TArg2 arg2)
        {
            logger.LogFormat(LogLevel.Debug, message, logAttributes, arg1, arg2);
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug<TArg1, TArg2, TArg3>(this ILogger logger, string message, LogAttributes logAttributes, in TArg1 arg1, in TArg2 arg2, in TArg3 arg3)
        {
            logger.LogFormat(LogLevel.Debug, message, logAttributes, arg1, arg2, arg3);
        }
    }
}
