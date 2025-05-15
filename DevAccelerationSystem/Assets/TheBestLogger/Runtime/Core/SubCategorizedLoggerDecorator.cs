using System;
using Cysharp.Text;
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

        public void LogTrace(string message, LogAttributes logAttributes = null)
        {
            _logger.LogTrace(ZString.Concat(_subCategoryName, " ", message), logAttributes);
        }
    }
}
