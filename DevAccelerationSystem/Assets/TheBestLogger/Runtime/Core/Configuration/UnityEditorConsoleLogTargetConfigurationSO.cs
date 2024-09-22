using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    [CreateAssetMenu(fileName = nameof(UnityEditorConsoleLogTargetConfiguration), menuName = "ScriptableObjects/Logger/Create UnityEditorConsoleLogTargetConfiguration", order = 1)]
    public sealed class UnityEditorConsoleLogTargetConfigurationSO : LogTargetConfigurationSO
    {
        public UnityEditorConsoleLogTargetConfiguration SpecificConfiguration;
        public override LogTargetConfiguration Configuration => SpecificConfiguration;
    }
}