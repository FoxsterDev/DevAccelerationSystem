using System;
using System.Diagnostics;
using UnityEngine;

namespace TheBestLogger
{
    internal class FallbackLogger : ILogger
    {
        public void Dispose()
        {
        }

        [HideInCallstack]
        void ILogger.LogException(Exception ex, LogAttributes logAttributes)
        {
            Diagnostics.Write(ex.Message + "\n" + ex.StackTrace, LogLevel.Exception);
            UnityEngine.Debug.LogException(ex);
        }

        [HideInCallstack]
        void ILogger.LogError(string message, LogAttributes logAttributes)
        {
            Diagnostics.Write(message, LogLevel.Error);
            UnityEngine.Debug.LogError("[FallbackLogger] " + message);
        }

        [HideInCallstack]
        void ILogger.LogWarning(string message, LogAttributes logAttributes)
        {
            Diagnostics.Write(message, LogLevel.Warning);
            UnityEngine.Debug.LogWarning("[FallbackLogger] " + message);
        }

        [HideInCallstack]
        void ILogger.LogInfo(string message, LogAttributes logAttributes)
        {
            Diagnostics.Write(message, LogLevel.Info);
            UnityEngine.Debug.Log("[FallbackLogger] " + message);
        }

        [HideInCallstack]
        void ILogger.LogDebug(string message, LogAttributes logAttributes)
        {
            UnityEngine.Debug.Log("[FallbackLogger] " + message);
        }

        [HideInCallstack]
        void ILogger.LogFormat(LogLevel logLevel,
                               string message,
                               LogAttributes logAttributes,
                               params object[] args)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                {
                    ((ILogger) this).LogDebug(message);
                    break;
                }
                case LogLevel.Info:
                {
                    ((ILogger) this).LogDebug(message);
                    break;
                }
                case LogLevel.Warning:
                {
                    ((ILogger) this).LogWarning(message);
                    break;
                }
                case LogLevel.Error:
                {
                    ((ILogger) this).LogError(message);
                    break;
                }
                case LogLevel.Exception:
                {
                    ((ILogger) this).LogError(message);
                    break;
                }
            }
        }

        [HideInCallstack]
        public void LogTrace(string message, LogAttributes logAttributes = null)
        {
            TraceIfDebug(message, logAttributes);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private void TraceIfDebug(string message, LogAttributes logAttributes = null)
        {
            ((ILogger)this).LogDebug(message, logAttributes);
        }
    }
}
