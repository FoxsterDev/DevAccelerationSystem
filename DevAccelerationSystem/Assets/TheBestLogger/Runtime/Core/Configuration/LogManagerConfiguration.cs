using UnityEngine;

namespace TheBestLogger
{
    [CreateAssetMenu(fileName = nameof(LogManagerConfiguration),
        menuName = "ScriptableObjects/Logger/LogManagerConfiguration", order = 1)]
    internal sealed class LogManagerConfiguration : ScriptableObject
    {
        [Tooltip("Set it to Debug.unityLogger.filterLogType")]
        public LogType DebugUnityLoggerFilterLogType = LogType.Log;
        public bool IsActiveUnityDebugLogSource = false;
        public bool IsActiveUnobservedTaskExceptionLogSource = false;
        public bool IsActiveUnityApplicationLogSource = false;
        public bool IsActiveSystemDiagnosticsDebugLogSource = false;
        public LogTargetConfigurationSO[] LogTargetConfigs = new LogTargetConfigurationSO[0];

        public string DefaultUnityLogsCategoryName = "Uncategorized";
        public uint MinTimestampPeriodMs = 60;

        [Tooltip("This is used for periodical runupdates for decorations")]
        public uint MinUpdatesPeriodMs = 1000;
    }
}