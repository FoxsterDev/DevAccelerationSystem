using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.Core
{
    public static class ScriptableObjectExtension
    {
        public static void SaveChanges( this ScriptableObject so, bool forceUpdateAssetDatabase)
        {
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssetIfDirty(so);
            if (forceUpdateAssetDatabase)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }
        
        // Generic method to load all ScriptableObjects of a specific type
        public static List<T> LoadAllAssetsOfType<T>() where T : ScriptableObject
        {
            var assets = new List<T>(1);
        
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }
    }
}