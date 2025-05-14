using System;
using System.Collections.Generic;

namespace TheBestLogger
{
    public interface ILogTarget : IDisposable
    {
        LogTargetConfiguration Configuration { get; }
        string LogTargetConfigurationName { get; }
        void Mute(bool mute);
        bool IsLogLevelAllowed(LogLevel logLevel, string category);
        bool IsStackTraceEnabled(LogLevel logLevel, string category);
        void Log(LogLevel level, string category, string message,   LogAttributes logAttributes, Exception exception = null);

        void LogBatch(
            IReadOnlyList<LogEntry> logBatch);
        void ApplyConfiguration(LogTargetConfiguration configuration);

        bool DebugModeEnabled { get; internal set; }
    }
}