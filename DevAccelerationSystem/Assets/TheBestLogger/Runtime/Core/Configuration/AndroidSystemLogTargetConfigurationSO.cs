using System;
using UnityEngine;

namespace TheBestLogger
{
    [Serializable]
    [CreateAssetMenu(
        fileName = nameof(AndroidSystemLogTargetConfiguration), menuName = "ScriptableObjects/Logger/Create AndroidSystemLogTargetConfiguration", order = 1)]
    public sealed class AndroidSystemLogTargetConfigurationSO : LogTargetConfigurationSO
    {
        public AndroidSystemLogTargetConfiguration SpecificConfiguration;
        public override LogTargetConfiguration Configuration => SpecificConfiguration;
    }
}
