using System;
using UnityEngine;

namespace TheBestLogger
{
    internal class UnityApplicationLogSource : ILogSource
    {
        private  ILogConsumer _logConsumer;

        public UnityApplicationLogSource(ILogConsumer logConsumer)
        {
            _logConsumer = logConsumer;
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stacktrace"></param>
        /// <param name="logType"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void OnLogMessageReceived(string message, string stacktrace, LogType logType)
        {
            _logConsumer.LogFormat(logType.ConvertToTheBestLoggerLogLevel(), nameof(UnityApplicationLogSource), message,null, stacktrace);
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            _logConsumer = null;
        }
    }
}