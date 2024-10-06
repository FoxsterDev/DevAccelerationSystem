using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    public struct LogLevelStackTraceConfigurationEntry
    {
        [HideInInspector]
        public LogLevel Level;
        public bool Enabled;
    }
}
