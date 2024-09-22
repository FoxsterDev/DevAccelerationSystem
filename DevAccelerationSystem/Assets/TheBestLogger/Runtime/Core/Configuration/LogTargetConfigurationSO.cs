using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    public abstract class LogTargetConfigurationSO : ScriptableObject
    {
        public abstract LogTargetConfiguration Configuration { get; }
    }
}