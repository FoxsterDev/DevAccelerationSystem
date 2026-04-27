using System;

namespace TheBestLogger
{
    [System.Serializable]
    public class DebugModeConfiguration
    {
        public bool Enabled;
        public LogLevel MinLogLevel = LogLevel.Warning;
        public string[] IDs;
        public LogTargetCategory[] OverrideCategories;

        public void ApplyRuntimeDefaults()
        {
            IDs ??= Array.Empty<string>();
            OverrideCategories ??= Array.Empty<LogTargetCategory>();
        }
    }
}
