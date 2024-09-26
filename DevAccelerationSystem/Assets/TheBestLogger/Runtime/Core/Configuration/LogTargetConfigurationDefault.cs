using UnityEngine;

namespace TheBestLogger
{
    internal class LogTargetConfigurationDefault : LogTargetConfiguration
    {
        public LogTargetConfigurationDefault(string logTargetString)
        {
            IsThreadSafe = false;
            Muted = true;
            ShowTimestamp = false;
            MinLogLevel = LogLevel.Exception;
            Debug.LogWarning($"Default log target configuration was applied to {logTargetString}. The target was muted because {logTargetString}ConfigurationSO is not assigned as reference or should have name as {(logTargetString+"Configuration")}");
        }
    }
}