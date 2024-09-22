using System;

namespace TheBestLogger
{
    [System.Serializable]
    public class DebugModeConfiguration
    {
        public bool Enabled;
        public LogLevel MinLogLevel = LogLevel.Warning;
        public string[] IDs = Array.Empty<string>();
        public LogTargetCategory[] OverrideCategories = new LogTargetCategory[0];
    }
}