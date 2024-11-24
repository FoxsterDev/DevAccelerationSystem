using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    [CreateAssetMenu(fileName = nameof(AppleSystemLogTargetConfiguration), menuName = "ScriptableObjects/Logger/Create AppleSystemLogTargetConfiguration", order = 1)]
    public sealed class AppleSystemLogTargetConfigurationSO : LogTargetConfigurationSO
    {
        public AppleSystemLogTargetConfiguration SpecificConfiguration;
        public override LogTargetConfiguration Configuration => SpecificConfiguration;
    }
}
