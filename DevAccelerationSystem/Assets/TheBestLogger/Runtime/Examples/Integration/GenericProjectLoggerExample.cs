using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheBestLogger.Examples
{
    /// <summary>
    /// Generic integration example for projects that want one app-level logger facade on top of LogManager.
    /// This example follows the 3.0.0 package contract:
    /// - logger access goes through cached category loggers
    /// - debug mode activation is explicit and can reuse a persisted debug id
    /// - remote configuration uses raw JSON patch APIs only
    /// </summary>
    public static class GenericProjectLoggerExample
    {
        public const string DefaultDebugIdPlayerPrefsKey = "TheBestLogger.DebugId";

        private static readonly Dictionary<string, ILogger> Loggers = new(StringComparer.Ordinal);

        public static ILogger Technical => GetLogger(nameof(Technical));
        public static ILogger Gameplay => GetLogger(nameof(Gameplay));
        public static ILogger UI => GetLogger(nameof(UI));
        public static ILogger Analytics => GetLogger(nameof(Analytics));
        public static ILogger Network => GetLogger(nameof(Network));
        public static ILogger Loading => GetLogger(nameof(Loading));
        public static ILogger LiveOps => GetLogger(nameof(LiveOps));

        public static ILogger GetLogger(string categoryName, string subCategoryName = "")
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                return LogManager.CreateLogger(string.Empty);
            }

            var key = string.Concat(categoryName, "|", subCategoryName ?? string.Empty);
            if (Loggers.TryGetValue(key, out var logger))
            {
                return logger;
            }

            logger = LogManager.CreateLogger(categoryName, subCategoryName);
            Loggers[key] = logger;
            return logger;
        }

        public static string GetStoredDebugIdOrEmpty(string playerPrefsKey = DefaultDebugIdPlayerPrefsKey)
        {
            return PlayerPrefs.GetString(playerPrefsKey, string.Empty);
        }

        public static void StoreDebugId(string debugId, string playerPrefsKey = DefaultDebugIdPlayerPrefsKey)
        {
            PlayerPrefs.SetString(playerPrefsKey, debugId ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static bool TryEnableStoredDebugMode(ILogger statusLogger = null,
                                                    string playerPrefsKey = DefaultDebugIdPlayerPrefsKey)
        {
            return TryEnableDebugMode(GetStoredDebugIdOrEmpty(playerPrefsKey), statusLogger);
        }

        public static bool TryEnableDebugMode(string debugId, ILogger statusLogger = null)
        {
            if (string.IsNullOrEmpty(debugId))
            {
                statusLogger?.LogWarning("Debug mode was not enabled because debug id is empty.");
                return false;
            }

            var changed = LogManager.SetDebugMode(debugId, true);
            if (changed)
            {
                statusLogger?.LogInfo($"Debug mode enabled for id {debugId}.");
            }
            else
            {
                statusLogger?.LogWarning($"Debug mode was not enabled for id {debugId}.");
            }

            return changed;
        }

        public static bool TryApplyRemoteConfigurationPatch(string targetName,
                                                            string rawJsonPatch,
                                                            ILogger statusLogger = null)
        {
            var applied = LogManager.TryApplyRemoteConfigurationPatch(targetName, rawJsonPatch, out var error);
            ReportRemoteConfigurationResult(statusLogger, applied, error, targetName, isBatch: false);
            return applied;
        }

        public static bool TryApplyRemoteConfigurationDocument(IReadOnlyDictionary<string, string> rawJsonPatches,
                                                               ILogger statusLogger = null)
        {
            var applied = LogManager.TryApplyRemoteConfigurationDocument(rawJsonPatches, out var error);
            ReportRemoteConfigurationResult(statusLogger, applied, error, "batch", isBatch: true);
            return applied;
        }

        public static void Dispose()
        {
            Loggers.Clear();
        }

        private static void ReportRemoteConfigurationResult(ILogger statusLogger,
                                                            bool applied,
                                                            string error,
                                                            string targetName,
                                                            bool isBatch)
        {
            if (statusLogger == null)
            {
                return;
            }

            if (applied)
            {
                if (isBatch)
                {
                    statusLogger.LogInfo("Remote configuration document applied.");
                }
                else
                {
                    statusLogger.LogInfo($"Remote configuration patch applied for {targetName}.");
                }

                return;
            }

            if (isBatch)
            {
                statusLogger.LogError($"Remote configuration document was rejected. {error}");
            }
            else
            {
                statusLogger.LogError($"Remote configuration patch was rejected for {targetName}. {error}");
            }
        }
    }
}
