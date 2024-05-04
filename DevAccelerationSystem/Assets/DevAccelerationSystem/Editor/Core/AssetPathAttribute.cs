using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DevAccelerationSystem.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AssetPathAttribute : Attribute
    {
        private string _filePath;
        private string _relativePath;
        private AssetPathAttribute.Location _location;
        private readonly Type _targetType;
        private readonly string _label;

        internal string Filepath
        {
            get
            {
                if (this._filePath == null && this._relativePath != null)
                {
                    this._filePath = CombineFilePath(this._relativePath, this._location);
                    this._relativePath = (string) null;
                }
                return this._filePath;
            }
        }

        public AssetPathAttribute(string relativePath, AssetPathAttribute.Location location, System.Type targetType, string label)
        {
            this._relativePath = !string.IsNullOrEmpty(relativePath) ? relativePath : throw new ArgumentException("Invalid relative path (it is empty)");
            this._location = location;
            _targetType = targetType;
            _label = label;
        }

        private  string CombineFilePath(string relativePath, AssetPathAttribute.Location location)
        {
            switch (location)
            {
                case AssetPathAttribute.Location.LocallyInParentFolderWithTheScriptType:
                {
                    var ar = AssetDatabase.FindAssets(_targetType.Name + " t:Script");
                    var path = AssetDatabase.GUIDToAssetPath(ar[0]);
                    path = path.Substring(0, path.Length - _targetType.Name.Length - 3);
                    path += relativePath;
                    return path;
                }
                default:
                    Debug.LogError((object) ("Unhandled enum: " + location.ToString()));
                    return relativePath;
            }
        }

        public enum Location
        {
            LocallyInParentFolderWithTheScriptType
        }
    }
}