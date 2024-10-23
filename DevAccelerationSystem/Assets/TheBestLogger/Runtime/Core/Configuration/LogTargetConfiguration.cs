using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace TheBestLogger
{
    [System.Serializable]
    public abstract class LogTargetConfiguration
    {
        public bool Muted;
        public LogLevel MinLogLevel = LogLevel.Warning;
        public LogTargetCategory[] OverrideCategories = new LogTargetCategory[0];
        public LogTargetBatchLogsConfiguration BatchLogs;
        public DebugModeConfiguration DebugMode = new DebugModeConfiguration();

        /// <summary>
        /// For performance reasons it will accessed with the int of loglevel, [0] for Debug, [1] for Info, [2] for Warning, [3] for Error, [4] for Exception
        /// </summary>
        [Tooltip(" [0] for Debug, [1] for Info, [2] for Warning, [3] for Error, [4] for Exception")]
        public LogLevelStackTraceConfiguration[] StackTraces = new LogLevelStackTraceConfiguration[5]
        {
            new LogLevelStackTraceConfiguration{ Level = LogLevel.Debug, Enabled = false},
            new LogLevelStackTraceConfiguration{ Level = LogLevel.Info, Enabled = false},
            new LogLevelStackTraceConfiguration{ Level = LogLevel.Warning, Enabled = false},
            new LogLevelStackTraceConfiguration{ Level = LogLevel.Error, Enabled = true},
            new LogLevelStackTraceConfiguration{ Level = LogLevel.Exception, Enabled = true}
        };

        public bool IsThreadSafe;
        public LogTargetDispatchingLogsToMainThreadConfiguration DispatchingLogsToMainThread;

        public virtual void Merge(LogTargetConfiguration newConfig)
        {
            Diagnostics.Write(" begin for "+GetType().Name);

            if (newConfig == null) return;

            Muted = newConfig.Muted;
            MinLogLevel = newConfig.MinLogLevel;
            if (newConfig.OverrideCategories != null && newConfig.OverrideCategories.Length > 0)
                OverrideCategories = newConfig.OverrideCategories;
            BatchLogs = newConfig.BatchLogs;
            if (newConfig.DebugMode != null) DebugMode = newConfig.DebugMode;
            IsThreadSafe = newConfig.IsThreadSafe;
            const int countOfLogLevels = 5;
            if (newConfig.StackTraces != null && newConfig.StackTraces.Length == countOfLogLevels) StackTraces = newConfig.StackTraces;
            Diagnostics.Write(" end for "+GetType().Name);
        }
    }
}
