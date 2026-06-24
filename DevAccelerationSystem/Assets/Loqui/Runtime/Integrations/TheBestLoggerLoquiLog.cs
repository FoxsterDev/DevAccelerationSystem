using System;
using TheBestLoggerILogger = TheBestLogger.ILogger;

namespace Loqui.Integrations
{
    public sealed class TheBestLoggerLoquiLog : ILoquiLog
    {
        private readonly TheBestLoggerILogger _logger;

        public TheBestLoggerLoquiLog(TheBestLoggerILogger logger)
        {
            _logger = logger;
        }

        public void LogWarning(string message) => _logger?.LogWarning(message);
        public void LogError(string message) => _logger?.LogError(message);
        public void LogException(Exception exception) => _logger?.LogException(exception);
    }
}
