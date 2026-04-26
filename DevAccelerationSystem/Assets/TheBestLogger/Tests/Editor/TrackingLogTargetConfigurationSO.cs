using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    internal sealed class TrackingLogTarget : LogTarget
    {
        public List<LogEntry> LoggedEntries { get; } = new();
        public int DisposeCallCount { get; private set; }

        public TrackingLogTarget()
        {
            ApplyConfiguration(new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Debug,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            });
        }

        public override string LogTargetConfigurationName => nameof(TrackingLogTargetConfiguration);

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null)
        {
            LoggedEntries.Add(new LogEntry(level, category, message, logAttributes, exception));
        }

        public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            LoggedEntries.AddRange(logBatch);
        }

        public override void Dispose()
        {
            DisposeCallCount++;
        }
    }

    [Serializable]
    public sealed class TrackingLogTargetConfiguration : LogTargetConfiguration
    {
    }

    public sealed class TrackingLogTargetConfigurationSO : LogTargetConfigurationSO
    {
        public TrackingLogTargetConfiguration SpecificConfiguration;
        public override LogTargetConfiguration Configuration => SpecificConfiguration;
    }
}
