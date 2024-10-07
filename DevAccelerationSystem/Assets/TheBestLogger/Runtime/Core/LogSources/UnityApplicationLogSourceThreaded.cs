using System;
using UnityEngine;

namespace TheBestLogger
{
    internal class UnityApplicationLogSourceThreaded : ILogSource
    {
        private  ILogConsumer _logConsumer;

        public UnityApplicationLogSourceThreaded(ILogConsumer logConsumer)
        {
            _logConsumer = logConsumer;
            Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
            Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stacktrace"></param>
        /// <param name="logType"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void OnLogMessageReceivedThreaded(string message, string stacktrace, LogType logType)
        {
            _logConsumer.LogFormat(logType.ConvertToTheBestLoggerLogLevel(), nameof(UnityDebugLogSource), message,null, stacktrace);
        }

        public void Dispose()
        { 
            Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
            _logConsumer = null;
        }
    }
}
