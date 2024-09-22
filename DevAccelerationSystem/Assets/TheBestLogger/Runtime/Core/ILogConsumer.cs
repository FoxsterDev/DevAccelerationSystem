using System;

namespace TheBestLogger
{
    internal interface ILogConsumer
    {
        void LogFormat(LogLevel logLevel,
            string logSourceId,
            string message,
            Exception exception = null,
            string stackTrace = null,
            UnityEngine.Object context = null,
            params object[] args);
    }
}