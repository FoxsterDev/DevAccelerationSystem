using UnityEngine;

namespace TheBestLogger.Examples.LogTargets
{
    [CreateAssetMenu(
        fileName = nameof(IMGUIRuntimeLogTargetConfiguration), menuName = "ScriptableObjects/Logger/Create IMGUIRuntimeLogTargetConfiguration", order = 1)]
    public sealed class IMGUIRuntimeLogTargetConfigurationSO : LogTargetConfigurationSO
    {
        public IMGUIRuntimeLogTargetConfiguration SpecificConfiguration;
        public override LogTargetConfiguration Configuration => SpecificConfiguration;
    }
}
