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

            if (Debug.unityLogger.logHandler.GetType() != typeof(UnityDebugLogSource))
            {
                _defaultUnityLogHandler = Debug.unityLogger.logHandler;
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
            _logConsumer.LogFormat(logType.ConvertToTheBestLoggerLogLevel(), nameof(UnityDebugLogSource), message, null, null, context, args);
        }

        [HideInCallstack] //handled or unhandled exceptions
        void ILogHandler.LogException(Exception exception, UnityEngine.Object context)
        {
            _logConsumer.LogFormat(LogLevel.Exception, nameof(UnityDebugLogSource), string.Empty, exception, null, context);
        }
    }
}
