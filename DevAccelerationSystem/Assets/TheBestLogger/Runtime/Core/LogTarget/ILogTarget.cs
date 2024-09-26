using System;
using System.Collections.Generic;

namespace TheBestLogger
{
    public interface ILogTarget : IDisposable
    {
        LogTargetConfiguration Configuration { get; }
        void Mute(bool mute);
        bool IsLogLevelAllowed(LogLevel logLevel, string category);
        void Log(LogLevel level, string category, string message,   LogAttributes logAttributes, Exception exception = null);

        void LogBatch(
            IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception
                exception)> logBatch);
        void ApplyConfiguration(LogTargetConfiguration configuration);

        void SetDebugMode(bool isDebugModeEnabled);
    }
}