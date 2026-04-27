using System;
using System.Collections.Generic;
using System.Linq;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.IO;
#endif
using UnityEngine;

namespace TheBestLogger
{
    internal static class LogTargetConfigurationCacheStore
    {
        private const int SchemaVersion = 1;
        private const string CacheRootFolderName = "TheBestLogger";
        private const string CacheFolderName = "ConfigCache";
        private const string CacheDocumentFileName = "log-target-config-cache.json";
        private const string CacheDocumentPlayerPrefsKey = "TheBestLogger.ConfigCache.Document";
        private const string TempFileExtension = ".tmp";

        [Serializable]
        private sealed class CacheDocument
        {
            public int Version = SchemaVersion;
            public CacheDocumentEntry[] Entries = Array.Empty<CacheDocumentEntry>();
        }

        [Serializable]
        private sealed class CacheDocumentEntry
        {
            public string TargetName;
            public string RawJsonPatch;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        internal static string CacheDirectoryPath =>
            Path.Combine(Application.persistentDataPath, CacheRootFolderName, CacheFolderName);

        internal static string CacheDocumentFilePath =>
            Path.Combine(CacheDirectoryPath, CacheDocumentFileName);
#else
        internal static string CacheDirectoryPath => string.Empty;
        internal static string CacheDocumentFilePath => CacheDocumentPlayerPrefsKey;
#endif

#if UNITY_EDITOR
        internal static bool? UsePlayerPrefsStorageOverride;
        private static bool UsePlayerPrefsStorage => UsePlayerPrefsStorageOverride ?? false;
#elif UNITY_WEBGL
        private static bool UsePlayerPrefsStorage => true;
#else
        private static bool UsePlayerPrefsStorage => false;
#endif

        internal static void ClearCache()
        {
            if (UsePlayerPrefsStorage)
            {
                PlayerPrefs.DeleteKey(CacheDocumentPlayerPrefsKey);
                PlayerPrefs.Save();
                return;
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            if (File.Exists(CacheDirectoryPath))
            {
                File.Delete(CacheDirectoryPath);
                return;
            }

            if (!Directory.Exists(CacheDirectoryPath))
            {
                return;
            }

            Directory.Delete(CacheDirectoryPath, true);
#endif
        }

        internal static void TryOverlayCachedConfigurations(Dictionary<string, LogTargetConfiguration> builtInConfigurations)
        {
            if (builtInConfigurations == null || builtInConfigurations.Count < 1)
            {
                return;
            }

            var cacheDocument = TryLoadCacheDocument();
            if (cacheDocument?.Entries == null || cacheDocument.Entries.Length < 1)
            {
                return;
            }

            foreach (var entry in cacheDocument.Entries)
            {
                if (string.IsNullOrEmpty(entry?.TargetName) || string.IsNullOrEmpty(entry.RawJsonPatch))
                {
                    continue;
                }

                TryOverlayPatch(entry, builtInConfigurations);
            }
        }

        internal static void SaveConfigurationPatches(Dictionary<string, string> rawJsonPatches)
        {
            if (rawJsonPatches == null || rawJsonPatches.Count < 1)
            {
                return;
            }

            try
            {
                var cacheDocument = CreateCacheDocument(rawJsonPatches);
                if (cacheDocument == null || cacheDocument.Entries.Length < 1)
                {
                    return;
                }

                var json = JsonUtility.ToJson(cacheDocument);
                if (UsePlayerPrefsStorage)
                {
                    PlayerPrefs.SetString(CacheDocumentPlayerPrefsKey, json);
                    PlayerPrefs.Save();
                    return;
                }

#if !UNITY_WEBGL || UNITY_EDITOR
                Directory.CreateDirectory(CacheDirectoryPath);
                WriteTextAtomically(CacheDocumentFilePath, json);
#endif
            }
            catch (Exception ex)
            {
                Diagnostics.Write($"Failed to save logger configuration cache: {ex.Message}", LogLevel.Warning);
            }
        }

        private static CacheDocument CreateCacheDocument(Dictionary<string, string> rawJsonPatches)
        {
            var entries = rawJsonPatches
                .Where(pair => !string.IsNullOrEmpty(pair.Key) && !string.IsNullOrEmpty(pair.Value))
                .Select(pair => new CacheDocumentEntry
                {
                    TargetName = pair.Key,
                    RawJsonPatch = pair.Value
                })
                .ToArray();

            if (entries.Length < 1)
            {
                return null;
            }

            return new CacheDocument
            {
                Version = SchemaVersion,
                Entries = entries
            };
        }

        private static CacheDocument TryLoadCacheDocument()
        {
            try
            {
                string json;
                if (UsePlayerPrefsStorage)
                {
                    if (!PlayerPrefs.HasKey(CacheDocumentPlayerPrefsKey))
                    {
                        return null;
                    }

                    json = PlayerPrefs.GetString(CacheDocumentPlayerPrefsKey, string.Empty);
                }
                else
                {
#if !UNITY_WEBGL || UNITY_EDITOR
                    if (!File.Exists(CacheDocumentFilePath))
                    {
                        return null;
                    }

                    json = File.ReadAllText(CacheDocumentFilePath);
#else
                    return null;
#endif
                }

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                var cacheDocument = JsonUtility.FromJson<CacheDocument>(json);
                if (cacheDocument == null || cacheDocument.Version != SchemaVersion)
                {
                    return null;
                }

                return cacheDocument;
            }
            catch (Exception ex)
            {
                Diagnostics.Write($"Failed to load logger configuration cache document: {ex.Message}", LogLevel.Warning);
                return null;
            }
        }

        private static void TryOverlayPatch(CacheDocumentEntry entry,
                                            Dictionary<string, LogTargetConfiguration> builtInConfigurations)
        {
            try
            {
                if (!builtInConfigurations.TryGetValue(entry.TargetName, out var builtInConfiguration) || builtInConfiguration == null)
                {
                    return;
                }

                JsonUtility.FromJsonOverwrite(entry.RawJsonPatch, builtInConfiguration);
                builtInConfiguration.ApplyRuntimeDefaults();
            }
            catch (Exception ex)
            {
                Diagnostics.Write($"Failed to load cached logger configuration patch for {entry.TargetName}: {ex.Message}", LogLevel.Warning);
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private static void WriteTextAtomically(string path, string content)
        {
            var tempPath = path + TempFileExtension;
            File.WriteAllText(tempPath, content);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
#endif
    }
}
