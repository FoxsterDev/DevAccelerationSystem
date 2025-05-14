#if !UNITY_EDITOR || THEBESTLOGGER_PLATFORM_BUILD_SIMULATION
#define LOGGER_NOT_UNITY_EDITOR
#else
#define LOGGER_UNITY_EDITOR
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheBestLogger
{
    internal class CoreLogger : ILogger, ILogConsumer
    {
        private readonly string _categoryName;
        private readonly string _subCategoryName;
        private readonly uint _messageMaxLength;
        private IReadOnlyList<ILogTarget> _logTargets;
        private readonly UtilitySupplier _utilitySupplier;
        private readonly bool _hasSubCategory;

        public CoreLogger(string categoryName,
                          string subCategoryName,
                          IReadOnlyList<ILogTarget> logTargets,
                          UtilitySupplier utilitySupplier,
                          uint messageMaxLength)
        {
            _logTargets = logTargets;
            _categoryName = categoryName;
            _subCategoryName = subCategoryName;
            _hasSubCategory = !string.IsNullOrEmpty(_subCategoryName);
            _utilitySupplier = utilitySupplier;
            _messageMaxLength = messageMaxLength;
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
#if THEBESTLOGGER_DIAGNOSTICS_ENABLED
            var message2 = message;
            if (message2 == "{0}")
            {
                message2 = string.Format(message, args);
            }
            Diagnostics.Write($"[{logLevel}] {message2}{stackTrace}", LogLevel.Debug, exception, logSourceId);
#endif

#if LOGGER_UNITY_EDITOR
            //temporary for debug, avoid recursive callbacks
            if (logSourceId != nameof(UnityDebugLogSource) &&
                logSourceId != nameof(UnobservedTaskExceptionLogSource) &&
                logSourceId != nameof(SystemDiagnosticsConsoleLogSource) &&
                logSourceId != nameof(UnobservedUniTaskExceptionLogSource))
            {
                Diagnostics.Write($"the log was filtered out in editor mode because {logSourceId}");
                return;
            }
#endif
            SendToLogTargets(logLevel, message, logSourceId, exception, stackTrace, context, null, args);
        }

        [HideInCallstack]
        public void LogException(Exception ex, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Exception, null, "direct", ex, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogError(string message, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Error, message, "direct", null, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogWarning(string message, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Warning, message, "direct", null, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogInfo(string message, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Info, message, "direct", null, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogDebug(string message, LogAttributes logAttributes = null)
        {
            SendToLogTargets(LogLevel.Debug, message, "direct", null, null, null, logAttributes, null);
        }

        [HideInCallstack]
        public void LogFormat(LogLevel logLevel,
                              string message,
                              LogAttributes logAttributes = null,
                              params object[] args)
        {
            SendToLogTargets(logLevel, message, "direct", null, null, null, logAttributes, args);
        }
 
        [HideInCallstack]
        public void LogTrace(string message, LogAttributes logAttributes = null)
        {
            TraceIfDebug(message, logAttributes);
        }

        public void Dispose()
        {
            _logTargets = null;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private void TraceIfDebug(string message, LogAttributes logAttributes = null)
        {
            LogDebug(message, logAttributes);
        }

        [HideInCallstack]
        private void SendToLogTargets(LogLevel logLevel,
                                      string message,
                                      string logSourceId,
                                      Exception exception = null,
                                      string stackTrace = null,
                                      Object context = null,
                                      LogAttributes logAttributes = null,
                                      params object[] args)
        {
            string formattedMessage = null;
            var logPrepared = false;

            if (_logTargets == null)
            {
                return;
            }

            var isMainThread = _utilitySupplier.IsMainThread;
            var logTargetsCount = _logTargets.Count;
            for (var index = 0; index < logTargetsCount; index++)
            {
                var logTarget = _logTargets[index];
                if (logTarget == null)
                {
                    continue;
                }

                if (!logTarget.Configuration.IsThreadSafe && !isMainThread)
                {
                    if (!logTarget.Configuration.DispatchingLogsToMainThread.Enabled)
                    {
                        Diagnostics.Write(
                            "Message:{" + message + "} was skipped because " + logTarget.Configuration.GetType() +
                            " is not thread safe and called outside of unity main thread", LogLevel.Warning);
                        continue;
                    }
                }

                if (!logTarget.IsLogLevelAllowed(logLevel, _categoryName))
                {
                    continue;
                }

                if (!logPrepared)
                {
                    logPrepared = true;
                    formattedMessage = (_hasSubCategory ? LogMessageFormatter.TryFormat(_subCategoryName, message, exception, args) : LogMessageFormatter.TryFormat(message, exception, args))  ?? string.Empty;

                    if (formattedMessage.Length > _messageMaxLength)
                    {
                        formattedMessage = formattedMessage.Substring(0, (int) _messageMaxLength);
                        formattedMessage = StringOperations.Concat(formattedMessage, "\n--Truncated--");
                    }

                    logAttributes ??= new LogAttributes();
                    logAttributes.UnityContextObject = context;
                    var timeStamp = _utilitySupplier.GetTimeStamp();
                    logAttributes.TimeStampFormatted = timeStamp.Item2;
                    logAttributes.TimeUtc = timeStamp.Item1;
                    logAttributes.StackTrace = stackTrace;
                    logAttributes.Tags = _utilitySupplier.TagsRegistry.GetAllTags();

#if THEBESTLOGGER_DIAGNOSTICS_ENABLED
                    logAttributes.Add("LogSourceId", logSourceId);
                    logAttributes.Add("StackTraceSourceId", stackTrace != null ? "direct" : "recreated");
#endif
                }

                if (string.IsNullOrEmpty(logAttributes.StackTrace))
                {
                    if (logTarget.IsStackTraceEnabled(logLevel, _categoryName))
                    {
                        logAttributes.StackTrace = _utilitySupplier.StackTraceFormatter.Extract(exception);
                    }
                }

                logTarget.Log(logLevel, _categoryName, formattedMessage, logAttributes, exception);
            }
        }
    }
}
