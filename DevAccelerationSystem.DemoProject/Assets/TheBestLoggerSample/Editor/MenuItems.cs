using System.Collections;
using System.Collections.Generic;
using TheBestLogger;
using UnityEditor;
using UnityEngine;

namespace TheBestLoggerSample
{
    public class TheBestLoggerSampleMenuItems 
    {
        [UnityEditor.MenuItem("Tools/Print Jsons for Dev Configs")]
        public static void PrintJsonsForDevForLogTargetConfigurations()
        {
            var list = LoadAllBaseScriptableObjects<LogTargetConfigurationSO>(
                "TheBestLoggerSample/Resources/GameLogger/Dev");
            foreach (var l in list)
            {
                Debug.Log(l.Configuration.GetType().Name);
                Debug.Log(JsonUtility.ToJson(l.Configuration));
            }
        }
      
        private static List<T> LoadAllBaseScriptableObjects<T>(string folderPath = "") where T : ScriptableObject
        {
            var searchPath = string.IsNullOrEmpty(folderPath) ? "Assets" : $"Assets/{folderPath}";
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchPath });

            var results = new List<T>();

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (asset != null)
                {
                    results.Add(asset);
                }
            }

            return results;
        }
    }
}
