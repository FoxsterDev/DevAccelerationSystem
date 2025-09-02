using System;

namespace TheBestLogger
{
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Send an exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logAttributes">{'user_id': '12345', 'request_id': 'abcd-1234'}</param>
        void LogException(Exception ex, LogAttributes logAttributes = null);

        void LogError(string message, LogAttributes logAttributes = null);
        void LogError(string message, Exception exception, LogAttributes logAttributes = null);

        void LogWarning(string message, LogAttributes logAttributes = null);
        void LogInfo(string message, LogAttributes logAttributes = null);
        void LogDebug(string message, LogAttributes logAttributes = null);

        [Obsolete("Use LogFormat<T1,T2,T3..> generics instead")]
        void LogFormat(LogLevel logLevel,
                       string message,
                       LogAttributes logAttributes = null,
                       params object[] args);

        void LogFormat<T1>(LogLevel level, string message, LogAttributes attrs, in T1 arg1);
        void LogFormat<T1, T2>(LogLevel level, string message, LogAttributes attrs, in T1 arg1, in T2 arg2);
        void LogFormat<T1, T2, T3>(LogLevel level, string message, LogAttributes attrs, in T1 arg1, in T2 arg2, in T3 arg3);

        void LogFormat<T1>(LogLevel level, string message,  in T1 arg1);
        void LogFormat<T1, T2>(LogLevel level, string message, in T1 arg1, in T2 arg2);
        void LogFormat<T1, T2, T3>(LogLevel level, string message,  in T1 arg1, in T2 arg2, in T3 arg3);
    }
}
