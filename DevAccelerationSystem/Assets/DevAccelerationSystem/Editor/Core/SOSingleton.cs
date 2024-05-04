using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DevAccelerationSystem.Core
{
    public  class SOSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    CreateAndLoad();
                }

                return _instance;
            }
        }

        public static void ResetInstance()
        {
            _instance = null;
        }

        public static void SaveChangesInUnityEditor(bool assetDatabaseRefresh = true)
        {
            Save(_instance, true);
            //UnityEditor.EditorUtility.SetDirty(Instance);
            //AssetDatabase.SaveAssets();
            if (assetDatabaseRefresh)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        private static void CreateAndLoad()
        {
            var filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                var objects = InternalEditorUtility.LoadSerializedFileAndForget(filePath);
                if (objects != null && objects.Length > 0)
                {
                    _instance = objects[0] as T;
                }

                if (_instance != null)
                {
                    return;
                }
                
                var directoryName = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                
                _instance = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(_instance, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                Selection.activeObject = _instance;
            }
       }

        private static void Save(UnityEngine.Object obj, bool saveAsText)
        {
            if (obj == null)
            {
                Debug.LogError("Cannot save ScriptableSingleton: no instance!");
            }
            else
            {
                var filePath = GetFilePath();
                
               if (!string.IsNullOrEmpty(filePath))
                {
                    var directoryName = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    InternalEditorUtility.SaveToSerializedFileAndForget(new T[1]
                    {
                        (T)obj
                    }, filePath, saveAsText);
                }
                else
                {
                    Debug.LogWarning(string.Format(
                        "Saving has no effect. Your class '{0}' is missing the FilePathAttribute. Use this attribute to specify where to save your ScriptableSingleton.\nOnly call Save() and use this attribute if you want your state to survive between sessions of Unity.",
                        obj.GetType()));
                }
            }
        }

        private static string GetFilePath()
        {
            foreach (var customAttribute in typeof(T).GetCustomAttributes(true))
            {
                if (customAttribute is AssetPathAttribute)
                {
                    return (customAttribute as AssetPathAttribute).Filepath;
                }
            }

            return string.Empty;
        }
    }
}