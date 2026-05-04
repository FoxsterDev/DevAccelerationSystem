using System;
using System.Collections.Generic;
using TheBestLogger.Examples.LogTargets;

namespace TheBestLogger.Examples
{
    /// <summary>
    /// Generic example that shows how an app-level remote-config provider can be normalized
    /// into the logger 3.0.0 contract: targetName -> rawJsonPatch.
    /// </summary>
    public static class GenericRemoteConfigAdapterExample
    {
        [Serializable]
        public sealed class RemotePatchMapping
        {
            public string RemoteConfigKey;
            public string TargetConfigurationName;
        }

        /// <summary>
        /// Minimal provider contract for examples.
        /// Your real app can adapt Firebase Remote Config, a custom backend, LaunchDarkly, etc.
        /// </summary>
        public interface IRemoteConfigStringProvider
        {
            bool TryGetString(string key, out string value);
        }

        /// <summary>
        /// Reads provider values and builds the normalized logger document:
        /// target configuration name -> raw JSON patch.
        /// Missing or empty provider values are skipped.
        /// </summary>
        public static Dictionary<string, string> BuildLoggerPatchDocument(IReadOnlyList<RemotePatchMapping> mappings,
                                                                          IRemoteConfigStringProvider provider,
                                                                          ILogger statusLogger = null)
        {
            var document = new Dictionary<string, string>(StringComparer.Ordinal);
            if (mappings == null || mappings.Count < 1 || provider == null)
            {
                return document;
            }

            for (var index = 0; index < mappings.Count; index++)
            {
                var mapping = mappings[index];
                if (string.IsNullOrEmpty(mapping?.RemoteConfigKey) || string.IsNullOrEmpty(mapping.TargetConfigurationName))
                {
                    continue;
                }

                if (!provider.TryGetString(mapping.RemoteConfigKey, out var rawJsonPatch) || string.IsNullOrEmpty(rawJsonPatch))
                {
                    continue;
                }

                document[mapping.TargetConfigurationName] = rawJsonPatch;
            }

            statusLogger?.LogInfo($"Prepared logger remote-config document with {document.Count} target patch(es).");
            return document;
        }

        /// <summary>
        /// End-to-end example:
        /// 1. read provider-specific keys
        /// 2. normalize them into targetName -> rawJsonPatch
        /// 3. pass the normalized batch to LogManager
        /// </summary>
        public static bool TryApplyMappedLoggerPatches(IReadOnlyList<RemotePatchMapping> mappings,
                                                       IRemoteConfigStringProvider provider,
                                                       ILogger statusLogger = null)
        {
            var document = BuildLoggerPatchDocument(mappings, provider, statusLogger);
            if (document.Count < 1)
            {
                statusLogger?.LogWarning("No logger remote-config patches were found in the provider.");
                return false;
            }

            var applied = LogManager.TryApplyRemoteConfigurationDocument(document, out var error);
            if (applied)
            {
                statusLogger?.LogInfo("Logger remote-config document applied.");
                return true;
            }

            statusLogger?.LogError($"Logger remote-config document was rejected. {error}");
            return false;
        }

        /// <summary>
        /// Example mapping set.
        /// Replace these remote keys with the real keys from your product's remote-config system.
        /// </summary>
        public static IReadOnlyList<RemotePatchMapping> CreateDefaultMappings()
        {
            return new[]
            {
                new RemotePatchMapping
                {
                    RemoteConfigKey = "logger_unity_console_patch",
                    TargetConfigurationName = nameof(UnityEditorConsoleLogTargetConfiguration)
                },
                new RemotePatchMapping
                {
                    RemoteConfigKey = "logger_opensearch_patch",
                    TargetConfigurationName = nameof(OpenSearchLogTargetConfiguration)
                }
            };
        }
    }
}
