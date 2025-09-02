using System;
using Cysharp.Text;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger
{
    public class SubCategorizedLoggerDecorator : ILogger
    {
        private readonly string _subCategoryName;
        private readonly ILogger _logger;

        public SubCategorizedLoggerDecorator(string subCategoryName, ILogger logger)
        {
            _subCategoryName = subCategoryName;
            _logger = logger;
        }

        public void Dispose()
        {
            _logger.Dispose();
        }

        public void LogException(Exception ex, LogAttributes logAttributes = null)
        {
            _logger.LogException(ex, logAttributes);
        }

        public void LogError(string message, LogAttributes logAttributes = null)
        {
            _logger.LogError(ZString.Concat(_subCategoryName, " ", message), logAttributes);
        }

        public void LogError(string message,
                             Exception exception,
                             LogAttributes logAttributes = null)
        {

            var formatted = LogMessageFormatter.TryFormat(_subCategoryName, message, exception);
            _logger.LogError(formatted,  logAttributes);
        }

        public void LogWarning(string message, LogAttributes logAttributes = null)
        {
            _logger.LogWarning(ZString.Concat(_subCategoryName, " ", message), logAttributes);
        }

        [HideInCallstack]
        public void LogInfo(string message, LogAttributes logAttributes = null)
        {
            _logger.LogInfo(ZString.Concat(_subCategoryName, " ", message), logAttributes);
        }

        public void LogDebug(string message, LogAttributes logAttributes = null)
        {
            _logger.LogDebug(ZString.Concat(_subCategoryName, " ", message), logAttributes);
        }

        public void LogFormat(LogLevel logLevel,
                              string message,
                              LogAttributes logAttributes = null,
                              params object[] args)
        {
            _logger.LogFormat(logLevel,ZString.Concat(_subCategoryName, " ", message), logAttributes, args);
        }

        public void LogFormat<T1>(LogLevel level,
                                  string message,
                                  LogAttributes attrs,
                                  in T1 arg1)
        {
            _logger.LogFormat(level,ZString.Concat(_subCategoryName, " ", message), attrs, arg1);
        }

        public void LogFormat<T1, T2>(LogLevel level,
                                      string message,
                                      LogAttributes attrs,
                                      in T1 arg1,
                                      in T2 arg2)
        {
            _logger.LogFormat(level,ZString.Concat(_subCategoryName, " ", message), attrs, arg1, arg2);
        }

        public void LogFormat<T1, T2, T3>(LogLevel level,
                                          string message,
                                          LogAttributes attrs,
                                          in T1 arg1,
                                          in T2 arg2,
                                          in T3 arg3)
        {
            _logger.LogFormat(level,ZString.Concat(_subCategoryName, " ", message), attrs, arg1, arg2, arg3);
        }

        public void LogFormat<T1>(LogLevel level,
                                  string message,
                                  in T1 arg1)
        {
            throw new NotImplementedException();
        }

        public void LogFormat<T1, T2>(LogLevel level,
                                      string message,
                                      in T1 arg1,
                                      in T2 arg2)
        {
            throw new NotImplementedException();
        }

        public void LogFormat<T1, T2, T3>(LogLevel level,
                                          string message,
                                          in T1 arg1,
                                          in T2 arg2,
                                          in T3 arg3)
        {
            throw new NotImplementedException();
        }
    }
}
