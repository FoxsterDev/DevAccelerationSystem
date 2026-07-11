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

    internal static class LogSourceSafety
    {
        internal static void TryLog(ILogConsumer logConsumer,
                                    LogLevel logLevel,
                                    string logSourceId,
                                    string message,
                                    Exception exception = null,
                                    string stackTrace = null,
                                    UnityEngine.Object context = null,
                                    object[] args = null)
        {
            if (logConsumer == null)
            {
                return;
            }

            try
            {
                logConsumer.LogFormat(logLevel, logSourceId, message, exception, stackTrace, context, args);
            }
            catch (Exception)
            {
            }
        }
    }
}
