#if !UNITY_EDITOR || THEBESTLOGGER_PLATFORM_BUILD_SIMULATION
#define LOGGER_NOT_UNITY_EDITOR
#else
#define LOGGER_UNITY_EDITOR
#endif

#if THEBESTLOGGER_ENABLE_PROFILER
using Unity.Profiling;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheBestLogger
{
    internal class CoreLogger : ILogger, ILogConsumer
    {
#if THEBESTLOGGER_ENABLE_PROFILER
        private static readonly ProfilerMarker _logTargetsUpdatesMarker = new(ProfilerCategory.Scripts, "TheBestLogger.LogTargetUpdates");
#endif
        private readonly string _categoryName;
        private readonly bool _hasSubCategory;
        private readonly uint _messageMaxLength;
        private readonly string _subCategoryName;
        private readonly UtilitySupplier _utilitySupplier;
        private IReadOnlyList<ILogTarget> _logTargets;

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
            SendToLogTargets(logLevel, message, logSourceId, exception, stackTrace, context, null, true, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(Exception ex, LogAttributes logAttributes = null)
        {
            if (!AnyTargetWillLog(LogLevel.Exception))
                return;

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, null, ex);
            SendToLogTargets(LogLevel.Exception, formatted, "direct", ex, null, null, logAttributes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, LogAttributes logAttributes = null)
        {
            if (!AnyTargetWillLog(LogLevel.Error))
                return;

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message,null);
            SendToLogTargets(LogLevel.Error, formatted, "direct", null, null, null, logAttributes);
        }

        public void LogError(string message,
                             Exception exception,
                             LogAttributes logAttributes = null)
        {
            if (!AnyTargetWillLog(LogLevel.Error))
                return;

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, exception);

            SendToLogTargets(LogLevel.Error,
                                          formatted,
                                          logSourceId: "direct",
                                          exception: exception,
                                          stackTrace: null,
                                          context: null,
                                          logAttributes: logAttributes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message, LogAttributes logAttributes = null)
        {
            if (!AnyTargetWillLog(LogLevel.Warning))
            {
                return;
            }

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, null);
            SendToLogTargets(LogLevel.Warning, formatted, "direct", null, null, null, logAttributes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message, LogAttributes logAttributes = null)
        {
            if (!AnyTargetWillLog(LogLevel.Info))
            {
                return;
            }

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, null);
            SendToLogTargets(LogLevel.Info, formatted, "direct", null, null, null, logAttributes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string message, LogAttributes logAttributes = null)
        {
            if (!AnyTargetWillLog(LogLevel.Debug))
                return;

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, null);
            SendToLogTargets(LogLevel.Debug, formatted, "direct", null, null, null, logAttributes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat(LogLevel logLevel,
                              string message,
                              LogAttributes logAttributes = null,
                              params object[] args)
        {
            if (!AnyTargetWillLog(logLevel))
                return;

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, null, args);
            SendToLogTargets(logLevel, formatted, "direct", null, null, null, logAttributes, true, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat<T1>(LogLevel level,
                                  string message,
                                  LogAttributes attrs,
                                  in T1 arg1)
        {
            if (!AnyTargetWillLog(level))
            {
                return;
            }

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, null, arg1) ?? string.Empty;
            SendToLogTargets(level, formatted, "direct", null, null, null, attrs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat<T1, T2>(LogLevel level,
                                      string message,
                                      LogAttributes attrs,
                                      in T1 arg1,
                                      in T2 arg2)
        {
            if (!AnyTargetWillLog(level))
            {
                return;
            }

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, null, arg1, arg2) ?? string.Empty;
            SendToLogTargets(level, formatted, "direct", null, null, null, attrs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat<T1, T2, T3>(LogLevel level,
                                          string message,
                                          LogAttributes attrs,
                                          in T1 arg1,
                                          in T2 arg2,
                                          in T3 arg3)
        {
            if (!AnyTargetWillLog(level))
            {
                return;
            }

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, null, arg1, arg2, arg3) ?? string.Empty;
            SendToLogTargets(level, formatted, "direct", null, null, null, attrs);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat<T1>(LogLevel level,
                                  string message,
                                  in T1 arg1)
        {
            LogFormat(level, message, null, arg1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat<T1, T2>(LogLevel level,
                                      string message,
                                      in T1 arg1,
                                      in T2 arg2)
        {
            LogFormat(level, message, null, arg1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat<T1, T2, T3>(LogLevel level,
                                          string message,
                                          in T1 arg1,
                                          in T2 arg2,
                                          in T3 arg3)
        {
            LogFormat(level, message, null, arg1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AnyTargetWillLog(LogLevel level)
        {
            if (_logTargets == null)
            {
                return false;
            }

            var isMainThread = _utilitySupplier.IsMainThread;
            for (int i = 0, n = _logTargets.Count; i < n; i++)
            {
                var logTarget = _logTargets[i];
                if (logTarget == null)
                {
                    continue;
                }

                if (!logTarget.Configuration.IsThreadSafe && !isMainThread &&
                    !logTarget.Configuration.DispatchingLogsToMainThread.Enabled)
                {
                    continue;
                }

                if (logTarget.IsLogLevelAllowed(level, _categoryName))
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            _logTargets = null;
        }

        [HideInCallstack]
        private void SendToLogTargets(LogLevel logLevel,
                                      string formattedMessage,
                                      string logSourceId,
                                      Exception exception = null,
                                      string stackTrace = null,
                                      Object context = null,
                                      LogAttributes logAttributes = null, bool argsConcat = false,
                                      params object[] args)
        {
            var logPrepared = false;

            if (_logTargets == null)
            {
                return;
            }
#if THEBESTLOGGER_ENABLE_PROFILER
            using (_logTargetsUpdatesMarker.Auto())
            {
#endif
            if (formattedMessage == null) formattedMessage = string.Empty;

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
                            "Message:{" + formattedMessage + "} was skipped because " + logTarget.Configuration.GetType() +
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

                    if (argsConcat && args != null && args.Length > 0)
                    {
                        formattedMessage = LogMessageFormatter.TryFormat(_subCategoryName, formattedMessage, exception, args) ?? string.Empty;
                    }

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
#if THEBESTLOGGER_ENABLE_PROFILER
            }
#endif
        }
    }
}
