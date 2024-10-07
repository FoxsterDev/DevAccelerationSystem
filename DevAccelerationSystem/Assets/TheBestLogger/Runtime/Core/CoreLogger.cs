using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheBestLogger
{
    internal class CoreLogger : ILogger, ILogConsumer
    {
        private readonly string _categoryName;
        private IReadOnlyList<ILogTarget> _logTargets;
        private IUtilitySupplier _utilitySupplier;

        public CoreLogger(string categoryName,
                          IReadOnlyList<ILogTarget> logTargets,
                          IUtilitySupplier utilitySupplier)
        {
            _logTargets = logTargets;
            _categoryName = categoryName;
            _utilitySupplier = utilitySupplier;
        }

        [HideInCallstack]
        public void LogException(Exception ex, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Exception, null, ex, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogError(string message, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Error, message, null, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogWarning(string message, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Warning, message, null, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogInfo(string message, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Info, message, null, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogDebug(string message, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Debug, message, null, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogFormat(LogLevel logLevel,
                              string message,
                              LogAttributes logAttributes = null,
                              params object[] args)
        {
            SendToLogTargets(logLevel, message, null, null, null, logAttributes, args);
        }

        private string ExtractStackTrace(Exception exception)
        {
            var needFileInfo = false;

#if UNITY_EDITOR
            needFileInfo = true;
#else
            needFileInfo = Debug.isDebugBuild;
#endif
            
            string stackTrace;
            if (exception != null)
            {
                string exceptionMessage;
                string stackTrace1;

                StackTraceFormatter.ExtractStringFromExceptionInternal(exception, out exceptionMessage, out stackTrace1, 3, 5, needFileInfo);
                if (needFileInfo)
                {
                    stackTrace = exceptionMessage + "\n" + stackTrace1;
                }
                else
                {
                    stackTrace = stackTrace1;
                }
                //
            }
            else
            {
                stackTrace = StackTraceFormatter.ExtractFormattedStackTrace(4, needFileInfo);
            }

            return stackTrace;
        }

        [HideInCallstack]
        private void SendToLogTargets(LogLevel logLevel,
                                      string message,
                                      Exception exception = null,
                                      string stackTrace = null,
                                      UnityEngine.Object context = null,
                                      LogAttributes logAttributes = null,
                                      params object[] args)
        {
            string formattedMessage = null;
            var logPrepared = false;

            if (_logTargets == null) return;

            var isMainThread = _utilitySupplier.IsMainThread;
            for (var index = 0; index < _logTargets.Count; index++)
            {
                var logTarget = _logTargets[index];
                if (logTarget == null) continue;

                if (!logTarget.Configuration.IsThreadSafe && !isMainThread)
                {
                    Diagnostics.Write(
                        message + " was skipped because " + logTarget.Configuration.GetType() +
                        " is not thread safe and called outside of unity main thread", LogLevel.Warning);
                    continue;
                }

                if (!logTarget.IsLogLevelAllowed(logLevel, _categoryName))
                {
                    continue;
                }

                if (!logPrepared)
                {
                    logPrepared = true;
                    formattedMessage = LogMessageFormatter.TryFormat(message, exception, args) ?? string.Empty;

                    logAttributes ??= new LogAttributes();
                    logAttributes.UnityContextObject = context;
                    var timeStamp = _utilitySupplier.GetTimeStamp();
                    logAttributes.TimeStampFormatted = timeStamp.Item2;
                    logAttributes.TimeUtc = timeStamp.Item1;
                    logAttributes.StackTrace = stackTrace;
                }

                if (string.IsNullOrEmpty(logAttributes.StackTrace))
                {
                    if (logTarget.IsStackTraceEnabled(logLevel, _categoryName))
                    {
                        logAttributes.StackTrace = ExtractStackTrace(exception);
                    }
                }

                logTarget.Log(logLevel, _categoryName, formattedMessage, logAttributes, exception);
            }
        }

        /// <summary>
        /// For internal needs
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="logSourceId"></param>
        /// <param name="message">Simple message or message for string format if args are provided</param>
        /// <param name="exception"></param>
        /// <param name="stackTrace"></param>
        /// <param name="context"></param>
        /// <param name="args">If it is not null it will try to string format with a message, otherwise no </param>
        void ILogConsumer.LogFormat(LogLevel logLevel,
                                    string logSourceId,
                                    string message,
                                    Exception exception,
                                    string stackTrace,
                                    Object context,
                                    params object[] args)
        {
            SendToLogTargets(logLevel, message, exception, stackTrace, context, null, args);
        }

        public void Dispose()
        {
            _logTargets = null;
        }
    }
}
