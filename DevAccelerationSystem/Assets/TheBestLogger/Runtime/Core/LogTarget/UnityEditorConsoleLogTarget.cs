using System;
using System.Collections.Generic;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    public class UnityEditorConsoleLogTarget : LogTarget
    {
        protected ILogHandler _defaultUnityLogHandler;

        [Preserve]
        public UnityEditorConsoleLogTarget()
            : base()
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
            if (_showTimestamp)
            {
                message = ZString.Concat("[", logAttributes.TimeStampFormatted, "] ", "[", category, "] ", message);
            }
            else
            {
                message = ZString.Concat("[", category, "] ", message);
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
            IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> logBatch)
        {
            if (logBatch.Count == 1)
            {
                Log(
                    logBatch[0].level, logBatch[0].category, logBatch[0].message, logBatch[0].logAttributes,
                    logBatch[0].exception);
                return;
            }

            var str = "";
            using (var sb = ZString.CreateStringBuilder())
            {
                var number = 0;
                foreach (var log in logBatch)
                {
                    var message = ZString.Concat("batch[:", number, "] ", "[", log.level, "] ", "[", log.category, "] ", log.message);
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
