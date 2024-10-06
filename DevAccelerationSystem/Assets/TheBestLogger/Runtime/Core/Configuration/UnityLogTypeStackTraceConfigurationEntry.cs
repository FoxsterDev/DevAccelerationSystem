using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    public struct UnityLogTypeStackTraceConfigurationEntry
    {
        [HideInInspector]
        public LogType LogType;
        public StackTraceLogType StackTraceLevel;
    }
}
