using UnityEngine;

namespace TheBestLogger
{
    [CreateAssetMenu(fileName = nameof(LogManagerConfiguration),
        menuName = "ScriptableObjects/Logger/LogManagerConfiguration", order = 1)]
    internal sealed class LogManagerConfiguration : ScriptableObject
    {
        [Tooltip("Set it to Debug.unityLogger.filterLogType")]
        public LogType DebugUnityLoggerFilterLogType = LogType.Log;

        public LogTargetConfigurationSO[] LogTargetConfigs = new LogTargetConfigurationSO[0];

        public string DefaultUnityLogsCategoryName = "Uncategorized";
        [Tooltip("This is period of creating new string formatting timestamp to reduce allocation")]
        public uint MinTimestampPeriodMs = 60;

        [Tooltip("This is used for periodical runupdates for decorations")]
        public uint MinUpdatesPeriodMs = 1000;

        [Tooltip("This is used in NOT Unity editor runtime")]
        public UnityLogTypeStackTraceConfiguration ApplicationLogTypesStackTrace = new UnityLogTypeStackTraceConfiguration();
    }
}