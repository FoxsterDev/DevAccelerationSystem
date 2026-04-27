using System;
using System.Collections.Generic;
using System.Threading;
using StabilityHub;
using StabilityHub.Monitoring;
using TheBestLogger.Examples.LogTargets;
using UnityEngine;

namespace TheBestLogger.Examples
{
    public enum GameLoggerExampleBootstrapMode
    {
        ResourcesDev = 0,
        ScriptedProductionPreset = 1,
        ScriptedQaPreset = 2
    }

    public static class GameLoggerExampleBootstrap
    {
        public const string DefaultDebugId = "sample-device";

        public static void Initialize(GameLoggerExampleBootstrapMode bootstrapMode,
                                      bool useRuntimeConsoleTarget,
                                      bool retrievePreviousSessionIssuesOnStart,
                                      bool useOpenSearchMockTarget = false)
        {
            var logTargets = CreateLogTargets(useRuntimeConsoleTarget, useOpenSearchMockTarget);
            var cancelToken = CancellationToken.None;
#if UNITY_2022_3_OR_NEWER
            cancelToken = Application.exitCancellationToken;
#endif

            switch (bootstrapMode)
            {
                case GameLoggerExampleBootstrapMode.ResourcesDev:
                    InitializeResources(logTargets, cancelToken);
                    StabilityHubService.Initialize(GameLogger.Stability);
                    break;

                case GameLoggerExampleBootstrapMode.ScriptedProductionPreset:
                    InitializeScripted(logTargets,
                                       cancelToken,
                                       LogManagerConfigurationPreset.Production,
                                       MonitoringConfigurationPreset.Production);
                    break;

                case GameLoggerExampleBootstrapMode.ScriptedQaPreset:
                    InitializeScripted(logTargets,
                                       cancelToken,
                                       LogManagerConfigurationPreset.Qa,
                                       MonitoringConfigurationPreset.Qa);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(bootstrapMode), bootstrapMode, null);
            }

            if (retrievePreviousSessionIssuesOnStart)
            {
                StabilityHubService.RetrieveAndLogPreviousSessionIssues();
            }
        }

        private static List<LogTarget> CreateLogTargets(bool useRuntimeConsoleTarget, bool useOpenSearchMockTarget)
        {
            var logTargets = new List<LogTarget>
            {
#if UNITY_EDITOR
                new UnityEditorConsoleLogTarget(),
#endif
#if UNITY_ANDROID
                new AndroidSystemLogTarget(Application.identifier),
#elif UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
                new AppleSystemLogTarget(Application.identifier, "Unity"),
#endif
            };

            if (useRuntimeConsoleTarget)
            {
                logTargets.Add(new IMGUIRuntimeLogTarget());
            }

            if (useOpenSearchMockTarget)
            {
                logTargets.Add(new MockOpenSearchLogTarget());
            }

            return logTargets;
        }

        private static void InitializeResources(List<LogTarget> logTargets, CancellationToken cancelToken)
        {
            var configuration = Resources.Load<LogManagerConfiguration>("GameLogger/Dev/" + nameof(LogManagerConfiguration));
            if (configuration == null)
            {
                throw new InvalidOperationException("GameLogger example resources configuration was not found.");
            }

            var runtimeConfiguration = ScriptableObject.Instantiate(configuration);
            AppendOptionalLogTargetConfigs(runtimeConfiguration, logTargets);
            LogManager.Initialize(logTargets.AsReadOnly(), runtimeConfiguration, cancelToken, DefaultDebugId);
        }

        private static void InitializeScripted(List<LogTarget> logTargets,
                                               CancellationToken cancelToken,
                                               LogManagerConfigurationPreset loggerPreset,
                                               MonitoringConfigurationPreset monitoringPreset)
        {
            var configuration = LogManagerConfigurationPresets.Create(
                loggerPreset,
                CreatePlatformLogTargetConfiguration(),
#if UNITY_EDITOR
                CreateUnityEditorConsoleLogTargetConfiguration(),
#endif
                CreateImguiRuntimeLogTargetConfiguration(logTargets),
                CreateOpenSearchMockLogTargetConfiguration(logTargets));
            configuration.DefaultUnityLogsCategoryName = "Sample";

            LogManager.Initialize(logTargets.AsReadOnly(), configuration, cancelToken, DefaultDebugId);

            var monitoringConfiguration = MonitoringConfigurationPresets.Create(monitoringPreset);
            StabilityHubService.Initialize(GameLogger.Stability, monitoringConfiguration);
        }

        private static void AppendOptionalLogTargetConfigs(LogManagerConfiguration configuration, IReadOnlyList<LogTarget> logTargets)
        {
            var optionalConfigs = new List<LogTargetConfigurationSO>(2);
            var openSearchMockConfig = CreateOpenSearchMockLogTargetConfiguration(logTargets);
            if (openSearchMockConfig != null)
            {
                optionalConfigs.Add(openSearchMockConfig);
            }

            if (optionalConfigs.Count == 0)
            {
                return;
            }

            var existingConfigs = configuration.LogTargetConfigs ?? Array.Empty<LogTargetConfigurationSO>();
            var existingNames = new HashSet<string>();
            for (var index = 0; index < existingConfigs.Length; index++)
            {
                if (existingConfigs[index]?.Configuration != null)
                {
                    existingNames.Add(existingConfigs[index].Configuration.GetType().Name);
                }
            }

            var missingConfigs = new List<LogTargetConfigurationSO>(optionalConfigs.Count);
            for (var index = 0; index < optionalConfigs.Count; index++)
            {
                var optionalConfig = optionalConfigs[index];
                var configurationName = optionalConfig?.Configuration?.GetType().Name;
                if (!string.IsNullOrEmpty(configurationName) && !existingNames.Contains(configurationName))
                {
                    missingConfigs.Add(optionalConfig);
                }
            }

            if (missingConfigs.Count == 0)
            {
                return;
            }

            var combinedConfigs = new LogTargetConfigurationSO[existingConfigs.Length + missingConfigs.Count];
            Array.Copy(existingConfigs, combinedConfigs, existingConfigs.Length);
            for (var index = 0; index < missingConfigs.Count; index++)
            {
                combinedConfigs[existingConfigs.Length + index] = missingConfigs[index];
            }

            configuration.SetLogTargetConfigs(combinedConfigs);
        }

        private static LogTargetConfigurationSO CreatePlatformLogTargetConfiguration()
        {
#if UNITY_ANDROID
            var configurationSo = ScriptableObject.CreateInstance<AndroidSystemLogTargetConfigurationSO>();
            configurationSo.SpecificConfiguration = new AndroidSystemLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Warning,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };
            return configurationSo;
#elif UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
            var configurationSo = ScriptableObject.CreateInstance<AppleSystemLogTargetConfigurationSO>();
            configurationSo.SpecificConfiguration = new AppleSystemLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Warning,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };
            return configurationSo;
#else
            return null;
#endif
        }

        private static UnityEditorConsoleLogTargetConfigurationSO CreateUnityEditorConsoleLogTargetConfiguration()
        {
            var configurationSo = ScriptableObject.CreateInstance<UnityEditorConsoleLogTargetConfigurationSO>();
            configurationSo.SpecificConfiguration = new UnityEditorConsoleLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Debug,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };
            return configurationSo;
        }

        private static LogTargetConfigurationSO CreateImguiRuntimeLogTargetConfiguration(IReadOnlyList<LogTarget> logTargets)
        {
            foreach (var logTarget in logTargets)
            {
                if (logTarget is IMGUIRuntimeLogTarget)
                {
                    var configurationSo = ScriptableObject.CreateInstance<IMGUIRuntimeLogTargetConfigurationSO>();
                    configurationSo.SpecificConfiguration = new IMGUIRuntimeLogTargetConfiguration
                    {
                        MinLogLevel = LogLevel.Debug,
                        IsThreadSafe = false,
                        CountLogsToPick = 200,
                        MaxStringLengthForOneMessage = 500,
                        DebugMode = new DebugModeConfiguration(),
                        BatchLogs = new LogTargetBatchLogsConfiguration(),
                        DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                    };
                    return configurationSo;
                }
            }

            return null;
        }

        private static LogTargetConfigurationSO CreateOpenSearchMockLogTargetConfiguration(IReadOnlyList<LogTarget> logTargets)
        {
            foreach (var logTarget in logTargets)
            {
                if (logTarget is MockOpenSearchLogTarget)
                {
                    var configurationSo = ScriptableObject.CreateInstance<OpenSearchLogTargetConfigurationSO>();
                    configurationSo.SpecificConfiguration = new OpenSearchLogTargetConfiguration
                    {
                        MinLogLevel = LogLevel.Info,
                        IsThreadSafe = true,
                        OpenSearchHostUrl = "mock://sample-opensearch",
                        OpenSearchSingleLogMethod = "/logs",
                        IndexPrefix = "thebestlogger-sample-",
                        ApiKey = "sample-demo-key",
                        DebugMode = new DebugModeConfiguration(),
                        BatchLogs = new LogTargetBatchLogsConfiguration(),
                        DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                    };
                    return configurationSo;
                }
            }

            return null;
        }
    }
}
