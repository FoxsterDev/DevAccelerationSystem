using System;

namespace TheBestLogger
{
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logAttributes">{'user_id': '12345', 'request_id': 'abcd-1234'}</param>
        void LogException(Exception ex, LogAttributes logAttributes = null);
        void LogError(string message, LogAttributes logAttributes = null);
        void LogWarning(string message, LogAttributes logAttributes = null);
        void LogInfo(string message, LogAttributes logAttributes = null);
        void LogDebug(string message, LogAttributes logAttributes = null);
        void LogFormat(LogLevel logLevel, string message, LogAttributes logAttributes = null, params object[] args);
    }
}