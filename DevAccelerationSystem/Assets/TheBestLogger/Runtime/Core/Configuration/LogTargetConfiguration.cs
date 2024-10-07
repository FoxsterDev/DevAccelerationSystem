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
        public bool IsThreadSafe;
        public bool ShowTimestamp;
        public LogLevelStackTraceConfiguration LogLevelLevelStackTrace = new LogLevelStackTraceConfiguration();

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
            ShowTimestamp = newConfig.ShowTimestamp;

            Diagnostics.Write(" end for "+GetType().Name);
        }
    }
}
