using System;
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
            Debug.LogException(ex);
        }

        [HideInCallstack]
        void ILogger.LogError(string message, LogAttributes logAttributes)
        {
            Diagnostics.Write(message, LogLevel.Error);
            Debug.LogError("[FallbackLogger] " + message);
        }

        [HideInCallstack]
        void ILogger.LogWarning(string message, LogAttributes logAttributes)
        {
            Diagnostics.Write(message, LogLevel.Warning);
            Debug.LogWarning("[FallbackLogger] " + message);
        }

        [HideInCallstack]
        void ILogger.LogInfo(string message, LogAttributes logAttributes)
        {
            Diagnostics.Write(message, LogLevel.Info);
            Debug.Log("[FallbackLogger] " + message);
        }

        [HideInCallstack]
        void ILogger.LogDebug(string message, LogAttributes logAttributes)
        {
            Debug.Log("[FallbackLogger] " + message);
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
    }
}
