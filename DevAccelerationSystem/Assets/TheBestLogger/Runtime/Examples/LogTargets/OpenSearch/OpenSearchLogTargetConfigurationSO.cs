using UnityEngine;

namespace TheBestLogger.Examples.LogTargets
{
    [CreateAssetMenu(
        fileName = nameof(OpenSearchLogTargetConfiguration), menuName = "ScriptableObjects/Logger/Create OpenSearchLogTargetConfiguration", order = 1)]
    public sealed class OpenSearchLogTargetConfigurationSO : LogTargetConfigurationSO
    {
        public OpenSearchLogTargetConfiguration SpecificConfiguration;
        public override LogTargetConfiguration Configuration => SpecificConfiguration;
    }
}
