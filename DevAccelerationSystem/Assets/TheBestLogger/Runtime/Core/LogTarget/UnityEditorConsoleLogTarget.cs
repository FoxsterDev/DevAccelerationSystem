using System;
using System.Collections.Generic;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    public class UnityEditorConsoleLogTarget : LogTarget
    {
        protected ILogHandler _defaultUnityLogHandler;

        [Preserve]
        public UnityEditorConsoleLogTarget()
        {
            _defaultUnityLogHandler = Debug.unityLogger.logHandler;
            Diagnostics.Write("cached default UnityLogHandler");
        }

        public override string LogTargetConfigurationName => nameof(UnityEditorConsoleLogTargetConfiguration);

        [HideInCallstack]
        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null
        )
        {
            if (level != LogLevel.Exception)
            {
                message = StringOperations.Concat("[", category, "] ", message, logAttributes.ToRegularString(true, false));
            }

            switch (level)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    _defaultUnityLogHandler.LogFormat(LogType.Log, logAttributes?.UnityContextObject, "{0}", message);
                    break;
                case LogLevel.Warning:
                    _defaultUnityLogHandler.LogFormat(LogType.Warning, logAttributes?.UnityContextObject, "{0}", message);
                    break;
                case LogLevel.Error:
                    _defaultUnityLogHandler.LogFormat(LogType.Error, logAttributes?.UnityContextObject, "{0}", message);
                    break;
                case LogLevel.Exception:
                    _defaultUnityLogHandler.LogException(exception, logAttributes?.UnityContextObject);
                    break;
            }
        }

        public override void LogBatch(
            IReadOnlyList<LogEntry> logBatch)
        {
            if (logBatch == null)
            {
                return;
            }

            if (logBatch.Count == 1)
            {
                Log(
                    logBatch[0].Level, logBatch[0].Category, logBatch[0].Message, logBatch[0].Attributes,
                    logBatch[0].Exception);
                return;
            }

            var str = "";
            using (var sb = StringOperations.CreateStringBuilder())
            {
                var number = 0;
                foreach (var log in logBatch)
                {
                    var message = StringOperations.Concat("batch[:", number, "] ", "[", log.Level, "] ", "[", log.Category, "] ", log.Message);
                    number++;
                    sb.AppendLine(message);
                }

                str = sb.ToString();
            }

            _defaultUnityLogHandler.LogFormat(LogType.Log, null, "{0}", str);
        }

        public override void Dispose()
        {
            base.Dispose();
            _defaultUnityLogHandler = null;
            Diagnostics.Write("has disposed and cleaned default UnityLogHandler");
        }
    }
}
