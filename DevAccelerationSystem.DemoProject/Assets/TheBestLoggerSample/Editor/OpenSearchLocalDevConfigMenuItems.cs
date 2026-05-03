using System.IO;
using TheBestLogger.Examples.LogTargets;
using UnityEditor;
using UnityEngine;

namespace TheBestLoggerSample
{
    public static class OpenSearchLocalDevConfigMenuItems
    {
        private const string LocalConfigAssetPath =
            "Assets/GitIgnored/TheBestLoggerSample/Resources/GameLogger/Dev/OpenSearchLogTargetConfiguration.Local.asset";

        private const string SampleConfigAssetPath =
            "Assets/TheBestLoggerSample/Resources/GameLogger/Dev/OpenSearchLogTargetConfiguration.asset";

        [MenuItem("Tools/TheBestLogger Sample/OpenSearch/Select Or Create Local Dev Config")]
        public static void SelectOrCreateLocalDevConfig()
        {
            var localConfig = AssetDatabase.LoadAssetAtPath<OpenSearchLogTargetConfigurationSO>(LocalConfigAssetPath);
            if (localConfig == null)
            {
                localConfig = CreateLocalDevConfigTemplate();
            }

            Selection.activeObject = localConfig;
            EditorGUIUtility.PingObject(localConfig);
        }

        private static OpenSearchLogTargetConfigurationSO CreateLocalDevConfigTemplate()
        {
            EnsureFolderChain(Path.GetDirectoryName(LocalConfigAssetPath)?.Replace('\\', '/'));

            var sampleConfig = AssetDatabase.LoadAssetAtPath<OpenSearchLogTargetConfigurationSO>(SampleConfigAssetPath);
            if (sampleConfig == null)
            {
                throw new FileNotFoundException($"Sample OpenSearch config was not found at {SampleConfigAssetPath}");
            }

            var localConfig = ScriptableObject.CreateInstance<OpenSearchLogTargetConfigurationSO>();
            localConfig.SpecificConfiguration = new OpenSearchLogTargetConfiguration();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(sampleConfig.SpecificConfiguration), localConfig.SpecificConfiguration);

            AssetDatabase.CreateAsset(localConfig, LocalConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created local OpenSearch config template at {LocalConfigAssetPath}");
            return localConfig;
        }

        private static void EnsureFolderChain(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var segments = folderPath.Split('/');
            var currentPath = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var nextPath = $"{currentPath}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
