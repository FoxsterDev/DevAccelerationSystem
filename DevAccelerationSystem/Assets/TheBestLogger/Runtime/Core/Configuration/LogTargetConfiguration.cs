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
    }
}