using System;
using System.Diagnostics;
using Cysharp.Text;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using UnityEngine.Scripting;

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

        public void LogError(string message,
                             Exception exception,
                             LogAttributes logAttributes = null)
        {
            Diagnostics.Write(message, LogLevel.Error);
            UnityEngine.Debug.LogError("[FallbackLogger] " + message + ", " + exception?.Message + "\n" + exception?.StackTrace);
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
        public void LogFormat(LogLevel logLevel,
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

        public void LogFormat<T1>(LogLevel level,
                                  string message,
                                  LogAttributes attrs,
                                  in T1 arg1)
        {
            var formatted = LogMessageFormatter.TryFormat("[FallbackLogger]", message, null, arg1);
            LogFormat(level, formatted, attrs, null);
        }

        public void LogFormat<T1, T2>(LogLevel level,
                                      string message,
                                      LogAttributes attrs,
                                      in T1 arg1,
                                      in T2 arg2)
        {
            var formatted = LogMessageFormatter.TryFormat("[FallbackLogger]", message, null, arg1, arg2);
            LogFormat(level, formatted, attrs, null);
        }

        public void LogFormat<T1, T2, T3>(LogLevel level,
                                          string message,
                                          LogAttributes attrs,
                                          in T1 arg1,
                                          in T2 arg2,
                                          in T3 arg3)
        {
            var formatted = LogMessageFormatter.TryFormat("[FallbackLogger]", message, null, arg1, arg2, arg3);
            LogFormat(level, formatted, attrs, null);
        }

        public void LogFormat<T1>(LogLevel level,
                                  string message,
                                  in T1 arg1)
        {
            var formatted = LogMessageFormatter.TryFormat("[FallbackLogger]", message, null, arg1);
            LogFormat(level, formatted, null, null);
        }

        public void LogFormat<T1, T2>(LogLevel level,
                                      string message,
                                      in T1 arg1,
                                      in T2 arg2)
        {
            var formatted = LogMessageFormatter.TryFormat("[FallbackLogger]", message, null, arg1, arg2);
            LogFormat(level, formatted, null, null);
        }

        public void LogFormat<T1, T2, T3>(LogLevel level,
                                          string message,
                                          in T1 arg1,
                                          in T2 arg2,
                                          in T3 arg3)
        {
            var formatted = LogMessageFormatter.TryFormat("[FallbackLogger]", message, null, arg1, arg2, arg3);
            LogFormat(level, formatted, null, null);
        }
    }
}
