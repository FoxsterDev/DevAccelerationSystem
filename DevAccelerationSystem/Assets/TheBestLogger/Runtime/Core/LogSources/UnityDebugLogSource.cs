using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TheBestLogger
{
    internal class UnityDebugLogSource : ILogSource, ILogHandler
    {
        private ILogConsumer _logConsumer;
        private ILogHandler _defaultUnityLogHandler;
        private readonly LogType _defaultUnityFilterLogType;

        public UnityDebugLogSource(ILogConsumer logConsumer)
        {
            _logConsumer = logConsumer;

            Diagnostics.Write(" created with logConsumer:" + logConsumer.GetType());

            var currentHandler = Debug.unityLogger.logHandler;
            if (!ReferenceEquals(currentHandler, this))
            {
                _defaultUnityLogHandler = currentHandler;
                _defaultUnityFilterLogType = Debug.unityLogger.filterLogType;
                Debug.unityLogger.logEnabled = true;
                Debug.unityLogger.logHandler = this;

                Diagnostics.Write($" Debug.unityLogger.logHandler: {_defaultUnityLogHandler.GetType()} was overriden by {GetType()}");
            }
        }

        public void Dispose()
        {
            Diagnostics.Write("is disposing");

            if (_defaultUnityLogHandler != null)
            {
                Diagnostics.Write(
                    "Debug.unityLogger.logHandler set to " + _defaultUnityLogHandler.GetType() + ", Debug.unityLogger.filterLogType set to "
                    + _defaultUnityFilterLogType);
                Debug.unityLogger.logHandler = _defaultUnityLogHandler;
                Debug.unityLogger.filterLogType = _defaultUnityFilterLogType;
                _defaultUnityLogHandler = null;
            }

            _logConsumer = null;

            Diagnostics.Write("disposed");
        }

        [HideInCallstack]
        void ILogHandler.LogFormat(LogType logType,
                                   UnityEngine.Object context,
                                   string message,
                                   params object[] args)
        {
            LogSourceSafety.TryLog(_logConsumer,
                                   logType.ConvertToTheBestLoggerLogLevel(),
                                   nameof(UnityDebugLogSource),
                                   message,
                                   context: context,
                                   args: args);
        }

        [HideInCallstack] //handled or unhandled exceptions
        void ILogHandler.LogException(Exception exception, UnityEngine.Object context)
        {
            LogSourceSafety.TryLog(_logConsumer,
                                   LogLevel.Exception,
                                   nameof(UnityDebugLogSource),
                                   string.Empty,
                                   exception,
                                   context: context);
        }
    }
}
