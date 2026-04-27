using System;
using UnityEngine;

namespace TheBestLogger
{
    [Serializable]
    public sealed class LogTargetConfigurationCacheSettings
    {
        [Tooltip("Keeps the last remote target configuration patch and reapplies it on the next startup before fresh remote config arrives.")]
        public bool Enabled = true;
    }
}
