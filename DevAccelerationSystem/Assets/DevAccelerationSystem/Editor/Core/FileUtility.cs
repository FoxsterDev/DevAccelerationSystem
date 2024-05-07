using System;
using System.IO;
using UnityEngine;

namespace DevAccelerationSystem.Core
{
    public class FileUtility
    {
        /// <summary>
        /// Save as json to file path. Not support abstract or subset of Unity objects
        /// </summary>
        /// <param name="output">Object to save as json to file path</param>
        /// <param name="filePath">Relative of full path to the file</param>
        /// <param name="isRelativeFilePath">Indicate is the path relative to Unity project root path</param>
        /// <param name="ensureClearDirectory">When true will recreate a directory for saving the file</param>
        /// <typeparam name="T">Not support abstract or subset of Unity objects</typeparam>
        public static void SaveAsJson<T>(T output, string filePath, bool isRelativeFilePath = true, bool ensureClearDirectory = true)
        {
            try
            {
                var fullPath = ensureClearDirectory ? FileUtility.EnsureClearDirectory(filePath, isRelativeFilePath) : filePath;
                var json = JsonUtility.ToJson(output, true);
                System.IO.File.WriteAllText(fullPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Could not save file: "+filePath + " "+ex.Message);
            }
        }
        
        public static T LoadFromJson<T>(string filePath,  bool isRelativeFilePath = true)
        {
            try
            {
                var fullPath = isRelativeFilePath ? FileUtility.GetFullPath(filePath) : filePath;
                if (File.Exists(fullPath))
                {
                    var json = System.IO.File.ReadAllText(fullPath);
                    var output = JsonUtility.FromJson<T>(json);
                    return (T) output;
                }

            }
            catch (Exception ex)
            {
                Debug.LogError("Could not load file: "+filePath + " "+ex.Message);
            }
            
            return default(T);
        }
        
        public static string GetFullPath(string relativePath)
        {
            var compilationOutputFullPath = Application.dataPath.Replace("Assets",
                relativePath);
            return compilationOutputFullPath;
        }

        /// <summary>
        /// You can use it for preparing directory and file path for writing
        /// </summary>
        /// <param name="filePath">Path to the file. Can be relative or full</param>
        /// <param name="isRelativeFilePath">Relative is For instance Library/YourFolder/FileName</param>
        /// <returns>return full path to the file</returns>
        public static string EnsureClearDirectory(string filePath, bool isRelativeFilePath = true)
        {
            try
            {
                var compilationOutputFullPath = isRelativeFilePath ? GetFullPath(filePath) : filePath;

                var fileInfo = new FileInfo(compilationOutputFullPath).Directory;
                if (fileInfo != null)
                {
                    if (Directory.Exists(fileInfo.FullName))
                    {
                        Directory.Delete(fileInfo.FullName, true);
                    }

                    Directory.CreateDirectory(fileInfo.FullName);
                    return compilationOutputFullPath;
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }

            return string.Empty;
        }
    }
}