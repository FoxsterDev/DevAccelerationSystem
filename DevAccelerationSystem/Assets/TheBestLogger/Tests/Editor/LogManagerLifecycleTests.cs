using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using TheBestLogger.Examples;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogManagerLifecycleTests
    {
        private const string DefaultResourceSubFolderName = "LogManagerLifecycleTests";
        private const string DefaultResourceSubFolder = DefaultResourceSubFolderName + "/";

        private string _tempRootAssetPath;
        private TrackingLogTarget _trackingTarget;

        [SetUp]
        public void SetUp()
        {
            ResetLogManagerState();
            LogManager.ResetConfigCacheTestState();
            ClearAllConfigCacheBackends();

            _trackingTarget = new TrackingLogTarget();
            _tempRootAssetPath = $"Assets/TheBestLogger/Tests/Editor/Generated/{Guid.NewGuid():N}";
            CreateConfigurationAssets(_tempRootAssetPath,
                                      DefaultResourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration
                                          {
                                              Enabled = true,
                                              IDs = new[] { "debug-user" },
                                              MinLogLevel = LogLevel.Debug
                                          },
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      });
        }

        [TearDown]
        public void TearDown()
        {
            ResetLogManagerState();
            LogManager.ResetConfigCacheTestState();
            ClearAllConfigCacheBackends();

            if (!string.IsNullOrEmpty(_tempRootAssetPath))
            {
                FileUtil.DeleteFileOrDirectory(_tempRootAssetPath);
                FileUtil.DeleteFileOrDirectory(_tempRootAssetPath + ".meta");
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void CreateLogger_BeforeInitialize_ReturnsFallbackLogger()
        {
            var logger = LogManager.CreateLogger("Gameplay");

            Assert.That(logger, Is.TypeOf<FallbackLogger>());
        }

        [Test]
        public void CreateLogger_BeforeInitialize_LogsMissingInitializationWarningOnlyOnce()
        {
            var warnings = CaptureFallbackInitializationWarnings(() =>
            {
                LogManager.CreateLogger("Gameplay");
                LogManager.CreateLogger("Bootstrap");
                LogManager.CreateLogger("UI");
            });

            Assert.That(warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetCurrentLogTargetConfigurations_BeforeInitialize_ReturnsEmptyDictionary()
        {
            var configs = LogManager.GetCurrentLogTargetConfigurations();

            Assert.That(configs, Is.Empty);
        }

        [Test]
        public void UpdateLogTargetsConfigurations_BeforeInitialize_DoesNothing()
        {
            Assert.DoesNotThrow(() =>
            {
                LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
                {
                    [nameof(TrackingLogTargetConfiguration)] = new TrackingLogTargetConfiguration()
                });
            });

            Assert.That(LogManager.GetCurrentLogTargetConfigurations(), Is.Empty);
        }

        [Test]
        public void SetDebugMode_BeforeInitialize_ReturnsFalse()
        {
            var changed = LogManager.SetDebugMode("debug-user", true);

            Assert.That(changed, Is.False);
        }

        [Test]
        public void Dispose_BeforeInitialize_IsSafe()
        {
            Assert.DoesNotThrow(LogManager.Dispose);

            ResetWasDisposedFlagOnly();
            var logger = LogManager.CreateLogger("Gameplay");
            Assert.That(logger, Is.TypeOf<FallbackLogger>());
        }

        [Test]
        public void Initialize_WithValidConfiguration_CreatesCachedCoreLogger()
        {
            InitializeForTests("debug-user");

            var logger1 = LogManager.CreateLogger("Gameplay", "UI");
            var logger2 = LogManager.CreateLogger("Gameplay", "UI");

            Assert.That(logger1, Is.TypeOf<CoreLogger>());
            Assert.That(logger2, Is.SameAs(logger1));
        }

        [Test]
        public void Initialize_WithExplicitConfiguration_AppliesProvidedTargetConfiguration()
        {
            var trackingConfigSo = ScriptableObject.CreateInstance<TrackingLogTargetConfigurationSO>();
            trackingConfigSo.SpecificConfiguration = new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };

            var unityEditorConfigSo = ScriptableObject.CreateInstance<UnityEditorConsoleLogTargetConfigurationSO>();
            unityEditorConfigSo.SpecificConfiguration = CreateDefaultUnityEditorConsoleTargetConfiguration();

            var configuration = ScriptableObject.CreateInstance<LogManagerConfiguration>();
            configuration.DefaultUnityLogsCategoryName = "RuntimeDefaultCategory";
            configuration.MessageMaxLength = 512;
            configuration.MinTimestampPeriodMs = 0;
            configuration.MinUpdatesPeriodMs = 1000;
            configuration.StackTraceFormatterConfiguration = new StackTraceFormatterConfiguration();
            configuration.UniTaskConfiguration = new UniTaskConfiguration();
            configuration.SetLogTargetConfigs(trackingConfigSo, unityEditorConfigSo);

            LogManager.Initialize(new LogTarget[] { _trackingTarget }, configuration, CancellationToken.None, "debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(LogManager.GetCurrentLogTargetConfigurations(), Contains.Key(nameof(TrackingLogTargetConfiguration)));
            Assert.That(LogManager.GetCurrentLogTargetConfigurations(), Contains.Key(nameof(UnityEditorConsoleLogTargetConfiguration)));

            UnityEngine.Object.DestroyImmediate(trackingConfigSo);
            UnityEngine.Object.DestroyImmediate(unityEditorConfigSo);
            UnityEngine.Object.DestroyImmediate(configuration);
        }

        [Test]
        public void LogManagerConfigurationPresets_QaPreset_AllowsPointOverrideBeforeInitialize()
        {
            var trackingConfigSo = ScriptableObject.CreateInstance<TrackingLogTargetConfigurationSO>();
            trackingConfigSo.SpecificConfiguration = new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Debug,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };

            var configuration = LogManagerConfigurationPresets.CreateQa(trackingConfigSo);
            trackingConfigSo.SpecificConfiguration.MinLogLevel = LogLevel.Exception;

            LogManager.Initialize(new LogTarget[] { _trackingTarget }, configuration, CancellationToken.None, "debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Exception));
            Assert.That(configuration.LogTargetConfigs.Length, Is.EqualTo(1));

            UnityEngine.Object.DestroyImmediate(trackingConfigSo);
            UnityEngine.Object.DestroyImmediate(configuration);
        }

        [Test]
        public void PublicLogger_LogFormat_WithLogLevelAndMessage_UsesDefaultGameLoggerCategory()
        {
            InitializeForTests("debug-user");

            Logger.LogFormat(LogLevel.Warning, "hello");

            Assert.That(_trackingTarget.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(_trackingTarget.LoggedEntries[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(_trackingTarget.LoggedEntries[0].Category, Is.EqualTo("DefaultGameLogger"));
            Assert.That(_trackingTarget.LoggedEntries[0].Message, Is.EqualTo("hello"));
        }

        [Test]
        public void CreateLogger_WithEmptyCategoryAfterInitialize_ReturnsFallbackLogger()
        {
            InitializeForTests("debug-user");

            var logger = LogManager.CreateLogger(string.Empty);

            Assert.That(logger, Is.TypeOf<FallbackLogger>());
        }

        [Test]
        public void SetDebugMode_WithMatchingId_EnablesTrackingTarget()
        {
            InitializeForTests("non-matching-id");

            var changed = LogManager.SetDebugMode("debug-user", true);

            Assert.That(changed, Is.True);
            Assert.That(((ILogTarget) _trackingTarget).DebugModeEnabled, Is.True);
        }

        [Test]
        public void SetDebugMode_WithEmptyId_ReturnsFalse()
        {
            InitializeForTests("debug-user");

            var changed = LogManager.SetDebugMode(string.Empty, true);

            Assert.That(changed, Is.False);
        }

        [Test]
        public void GetCurrentLogTargetConfigurations_AfterInitialize_ContainsTrackingTargetConfiguration()
        {
            InitializeForTests("debug-user");

            var configs = LogManager.GetCurrentLogTargetConfigurations();

            Assert.That(configs.ContainsKey(nameof(TrackingLogTargetConfiguration)), Is.True);
            Assert.That(configs[nameof(TrackingLogTargetConfiguration)].MinLogLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void UpdateLogTargetsConfigurations_AppliesNewConfigurationToExistingTarget()
        {
            InitializeForTests("debug-user");

            var updatedConfig = new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                Muted = true,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };

            var currentConfigs = LogManager.GetCurrentLogTargetConfigurations();

            LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = updatedConfig,
                [nameof(UnityEditorConsoleLogTargetConfiguration)] = currentConfigs[nameof(UnityEditorConsoleLogTargetConfiguration)]
            });

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(_trackingTarget.Configuration.Muted, Is.True);
        }

        [Test]
        public void UpdateLogTargetsConfigurations_WithPartialTargetDictionary_PreservesOmittedTargetsAndPersistsThem()
        {
            var unityEditorConsoleTarget = new UnityEditorConsoleLogTarget();
            LogManager.Initialize(new LogTarget[] { _trackingTarget, unityEditorConsoleTarget },
                                  DefaultResourceSubFolder,
                                  CancellationToken.None,
                                  "debug-user");

            LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = new TrackingLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Error,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                }
            });

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(unityEditorConsoleTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Debug));

            LogManager.Dispose();
            ResetLogManagerState();

            _trackingTarget = new TrackingLogTarget();
            unityEditorConsoleTarget = new UnityEditorConsoleLogTarget();

            LogManager.Initialize(new LogTarget[] { _trackingTarget, unityEditorConsoleTarget },
                                  DefaultResourceSubFolder,
                                  CancellationToken.None,
                                  "debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(unityEditorConsoleTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Debug));
        }

        [Test]
        public void UpdateLogTargetsConfigurations_ReappliesDebugModeStateUsingCurrentDebugId()
        {
            InitializeForTests("other-user");

            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.False);

            var updatedConfig = new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Warning,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration
                {
                    Enabled = true,
                    IDs = new[] { "other-user" },
                    MinLogLevel = LogLevel.Debug
                },
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };

            var currentConfigs = LogManager.GetCurrentLogTargetConfigurations();

            LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = updatedConfig,
                [nameof(UnityEditorConsoleLogTargetConfiguration)] = currentConfigs[nameof(UnityEditorConsoleLogTargetConfiguration)]
            });

            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.True);
        }

        [Test]
        public void Initialize_WithNonMatchingDebugId_KeepsBaseMinLogLevelAndCategoryOverrides()
        {
            const string resourceSubFolderName = "LogManagerLifecycleTests_DebugInactive";

            CreateConfigurationAssets(_tempRootAssetPath,
                                      resourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          OverrideCategories = new[]
                                          {
                                              new LogTargetCategory
                                              {
                                                  Category = "Gameplay",
                                                  MinLevel = LogLevel.Warning
                                              }
                                          },
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration
                                          {
                                              Enabled = true,
                                              IDs = new[] { "debug-user" },
                                              MinLogLevel = LogLevel.Debug,
                                              OverrideCategories = new[]
                                              {
                                                  new LogTargetCategory
                                                  {
                                                      Category = "Gameplay",
                                                      MinLevel = LogLevel.Debug
                                                  }
                                              }
                                          },
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      });

            LogManager.Initialize(new LogTarget[] { _trackingTarget }, resourceSubFolderName + "/", CancellationToken.None, "other-user");

            Assert.That(((ILogTarget) _trackingTarget).DebugModeEnabled, Is.False);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.False);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Warning, "Gameplay"), Is.True);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Network"), Is.False);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Warning, "Network"), Is.True);
        }

        [Test]
        public void UpdateLogTargetsConfigurations_WhenDebugModeStopsMatchingCurrentId_RestoresBaseFiltering()
        {
            const string resourceSubFolderName = "LogManagerLifecycleTests_DebugDisabledByConfigUpdate";

            CreateConfigurationAssets(_tempRootAssetPath,
                                      resourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          OverrideCategories = new[]
                                          {
                                              new LogTargetCategory
                                              {
                                                  Category = "Gameplay",
                                                  MinLevel = LogLevel.Warning
                                              }
                                          },
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration
                                          {
                                              Enabled = true,
                                              IDs = new[] { "debug-user" },
                                              MinLogLevel = LogLevel.Debug,
                                              OverrideCategories = new[]
                                              {
                                                  new LogTargetCategory
                                                  {
                                                      Category = "Gameplay",
                                                      MinLevel = LogLevel.Debug
                                                  }
                                              }
                                          },
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      });

            LogManager.Initialize(new LogTarget[] { _trackingTarget }, resourceSubFolderName + "/", CancellationToken.None, "debug-user");

            Assert.That(((ILogTarget) _trackingTarget).DebugModeEnabled, Is.True);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.True);

            var currentConfigs = LogManager.GetCurrentLogTargetConfigurations();
            var updatedConfig = new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Warning,
                OverrideCategories = new[]
                {
                    new LogTargetCategory
                    {
                        Category = "Gameplay",
                        MinLevel = LogLevel.Warning
                    }
                },
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration
                {
                    Enabled = true,
                    IDs = new[] { "other-user" },
                    MinLogLevel = LogLevel.Debug,
                    OverrideCategories = new[]
                    {
                        new LogTargetCategory
                        {
                            Category = "Gameplay",
                            MinLevel = LogLevel.Debug
                        }
                    }
                },
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };

            LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = updatedConfig,
                [nameof(UnityEditorConsoleLogTargetConfiguration)] = currentConfigs[nameof(UnityEditorConsoleLogTargetConfiguration)]
            });

            Assert.That(((ILogTarget) _trackingTarget).DebugModeEnabled, Is.False);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.False);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Warning, "Gameplay"), Is.True);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Network"), Is.False);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Warning, "Network"), Is.True);
        }

        [Test]
        public void UpdateLogTargetConfiguration_WithRawJsonPatch_PreservesAbsentPrimitiveFieldsAndPersistsAcrossRestart()
        {
            InitializeForTests("debug-user");

            LogManager.UpdateLogTargetConfiguration(nameof(TrackingLogTargetConfiguration), "{\"Muted\":true}");

            Assert.That(_trackingTarget.Configuration.Muted, Is.True);
            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Warning));

            LogManager.Dispose();
            ResetLogManagerState();

            _trackingTarget = new TrackingLogTarget();
            InitializeForTests("debug-user");

            Assert.That(_trackingTarget.Configuration.Muted, Is.True);
            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void UpdateLogTargetConfiguration_WithRawJsonPatch_PreservesAbsentNestedBatchLogsFields()
        {
            const string resourceSubFolderName = "LogManagerLifecycleTests_RawJsonBatchLogs";

            CreateConfigurationAssets(_tempRootAssetPath,
                                      resourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration(),
                                          BatchLogs = new LogTargetBatchLogsConfiguration
                                          {
                                              Enabled = false,
                                              UpdatePeriodMs = 333,
                                              MaxCountLogs = 17
                                          },
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      });

            LogManager.Initialize(new LogTarget[] { _trackingTarget }, resourceSubFolderName + "/", CancellationToken.None, "debug-user");

            LogManager.UpdateLogTargetConfiguration(nameof(TrackingLogTargetConfiguration), "{\"BatchLogs\":{\"Enabled\":true}}");

            Assert.That(_trackingTarget.Configuration.BatchLogs.Enabled, Is.True);
            Assert.That(_trackingTarget.Configuration.BatchLogs.UpdatePeriodMs, Is.EqualTo(333));
            Assert.That(_trackingTarget.Configuration.BatchLogs.MaxCountLogs, Is.EqualTo(17));
        }

        [Test]
        public void Initialize_AfterPersistedRuntimeUpdate_BootstrapsFromCachedConfiguration()
        {
            InitializeForTests("debug-user");

            var currentConfigs = LogManager.GetCurrentLogTargetConfigurations();
            var updatedConfig = new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration
                {
                    Enabled = true,
                    IDs = new[] { "debug-user" },
                    MinLogLevel = LogLevel.Debug
                },
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };

            LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = updatedConfig,
                [nameof(UnityEditorConsoleLogTargetConfiguration)] = currentConfigs[nameof(UnityEditorConsoleLogTargetConfiguration)]
            });

            LogManager.Dispose();
            ResetLogManagerState();

            _trackingTarget = new TrackingLogTarget();
            InitializeForTests("debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void Initialize_AfterPersistedRuntimeUpdate_UsesCachedEmptyOverrideCategories()
        {
            const string resourceSubFolderName = "LogManagerLifecycleTests_EmptyOverrides";

            CreateConfigurationAssets(_tempRootAssetPath,
                                      resourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Error,
                                          OverrideCategories = new[]
                                          {
                                              new LogTargetCategory
                                              {
                                                  Category = "LoadingFunnel",
                                                  MinLevel = LogLevel.Warning
                                              }
                                          },
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration(),
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      });

            LogManager.Initialize(new LogTarget[] { _trackingTarget }, resourceSubFolderName + "/", CancellationToken.None, "debug-user");

            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Warning, "LoadingFunnel"), Is.True);

            var updatedConfig = new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                OverrideCategories = Array.Empty<LogTargetCategory>(),
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };

            var currentConfigs = LogManager.GetCurrentLogTargetConfigurations();

            LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = updatedConfig,
                [nameof(UnityEditorConsoleLogTargetConfiguration)] = currentConfigs[nameof(UnityEditorConsoleLogTargetConfiguration)]
            });

            LogManager.Dispose();
            ResetLogManagerState();

            _trackingTarget = new TrackingLogTarget();
            LogManager.Initialize(new LogTarget[] { _trackingTarget }, resourceSubFolderName + "/", CancellationToken.None, "debug-user");

            Assert.That(_trackingTarget.Configuration.OverrideCategories, Is.Empty);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Warning, "LoadingFunnel"), Is.False);
        }

        [Test]
        public void Initialize_WithCorruptCachedConfiguration_IgnoresOnlyCorruptTarget()
        {
            var resourceSubFolderName = CreateUniqueResourceSubFolderName("LogManagerLifecycleTests_CorruptCache");
            LogTargetConfigurationCacheStore.UsePlayerPrefsStorageOverride = true;
            var unityEditorConsoleTarget = new UnityEditorConsoleLogTarget();

            CreateConfigurationAssets(_tempRootAssetPath,
                                      resourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration(),
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      },
                                      unityEditorConsoleConfig: new UnityEditorConsoleLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Debug,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration(),
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      });

            LogManager.Initialize(new LogTarget[] { _trackingTarget, unityEditorConsoleTarget },
                                  resourceSubFolderName + "/",
                                  CancellationToken.None,
                                  "debug-user");

            LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = new TrackingLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Error,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                },
                [nameof(UnityEditorConsoleLogTargetConfiguration)] = new UnityEditorConsoleLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Warning,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                }
            });

            CorruptRawPatchInExistingCacheDocumentForTests(nameof(TrackingLogTargetConfiguration), "{ not-valid-json");

            LogManager.Dispose();
            ResetLogManagerState();

            _trackingTarget = new TrackingLogTarget();
            unityEditorConsoleTarget = new UnityEditorConsoleLogTarget();

            LogManager.Initialize(new LogTarget[] { _trackingTarget, unityEditorConsoleTarget },
                                  resourceSubFolderName + "/",
                                  CancellationToken.None,
                                  "debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(unityEditorConsoleTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void Initialize_WithCachedConfigurationDocument_BootstrapsSuccessfully()
        {
            var currentConfigs = new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = new TrackingLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Error,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                }
            };

            LogTargetConfigurationCacheStore.SaveConfigurationPatches(ToRawJsonPatches(currentConfigs));

            InitializeForTests("debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void Initialize_WhenConfigCacheDisabled_IgnoresPersistedCache()
        {
            const string resourceSubFolderName = "LogManagerLifecycleTests_ConfigCacheDisabled";
            var currentConfigs = new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = new TrackingLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Error,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                }
            };

            LogTargetConfigurationCacheStore.SaveConfigurationPatches(ToRawJsonPatches(currentConfigs));
            CreateConfigurationAssets(_tempRootAssetPath,
                                      resourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration
                                          {
                                              Enabled = true,
                                              IDs = new[] { "debug-user" },
                                              MinLogLevel = LogLevel.Debug
                                          },
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      },
                                      configuration => ConfigureConfigCache(configuration, enabled: false));

            LogManager.Initialize(new LogTarget[] { _trackingTarget }, resourceSubFolderName + "/", CancellationToken.None, "debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void UpdateLogTargetsConfigurations_WhenConfigCacheDisabled_DoesNotPersistRuntimeChanges()
        {
            const string resourceSubFolderName = "LogManagerLifecycleTests_ConfigCacheDisabled_NoPersist";
            CreateConfigurationAssets(_tempRootAssetPath,
                                      resourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration
                                          {
                                              Enabled = true,
                                              IDs = new[] { "debug-user" },
                                              MinLogLevel = LogLevel.Debug
                                          },
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      },
                                      configuration => ConfigureConfigCache(configuration, enabled: false));

            LogManager.Initialize(new LogTarget[] { _trackingTarget }, resourceSubFolderName + "/", CancellationToken.None, "debug-user");

            var currentConfigs = LogManager.GetCurrentLogTargetConfigurations();
            var updatedConfig = new TrackingLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration
                {
                    Enabled = true,
                    IDs = new[] { "debug-user" },
                    MinLogLevel = LogLevel.Debug
                },
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };

            LogManager.UpdateLogTargetsConfigurations(new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = updatedConfig,
                [nameof(UnityEditorConsoleLogTargetConfiguration)] = currentConfigs[nameof(UnityEditorConsoleLogTargetConfiguration)]
            });

            Assert.That(File.Exists(LogTargetConfigurationCacheStore.CacheDocumentFilePath), Is.False);

            LogManager.Dispose();
            ResetLogManagerState();

            _trackingTarget = new TrackingLogTarget();
            LogManager.Initialize(new LogTarget[] { _trackingTarget }, resourceSubFolderName + "/", CancellationToken.None, "debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void Initialize_WithCorruptCacheDocument_FallsBackToBuiltInConfiguration()
        {
            Directory.CreateDirectory(LogTargetConfigurationCacheStore.CacheDirectoryPath);
            File.WriteAllText(LogTargetConfigurationCacheStore.CacheDocumentFilePath, "{ not-valid-json");

            InitializeForTests("debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void Initialize_WithSchemaVersionMismatchCacheDocument_FallsBackToBuiltInConfiguration()
        {
            var currentConfigs = new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = new TrackingLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Error,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                }
            };

            LogTargetConfigurationCacheStore.SaveConfigurationPatches(ToRawJsonPatches(currentConfigs));

            var cacheDocumentJson = File.ReadAllText(LogTargetConfigurationCacheStore.CacheDocumentFilePath);
            cacheDocumentJson = cacheDocumentJson.Replace("\"Version\":1", "\"Version\":999");
            File.WriteAllText(LogTargetConfigurationCacheStore.CacheDocumentFilePath, cacheDocumentJson);

            InitializeForTests("debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void SaveConfigurationPatches_WhenCachePathIsAFile_DoesNotThrow()
        {
            var parentDirectory = Path.GetDirectoryName(LogTargetConfigurationCacheStore.CacheDirectoryPath);
            Directory.CreateDirectory(parentDirectory);
            File.WriteAllText(LogTargetConfigurationCacheStore.CacheDirectoryPath, "occupied");

            var currentConfigs = new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = new TrackingLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Error,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                }
            };

            Assert.DoesNotThrow(() => LogTargetConfigurationCacheStore.SaveConfigurationPatches(ToRawJsonPatches(currentConfigs)));
        }

        [Test]
        public void SaveConfigurationPatches_ReplacesPreviousDocumentContent()
        {
            Directory.CreateDirectory(LogTargetConfigurationCacheStore.CacheDirectoryPath);
            File.WriteAllText(LogTargetConfigurationCacheStore.CacheDocumentFilePath,
                              BuildCacheDocumentJson(("StaleConfiguration", "{\"MinLogLevel\":4}")));

            var currentConfigs = new Dictionary<string, LogTargetConfiguration>
            {
                [nameof(TrackingLogTargetConfiguration)] = new TrackingLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Error,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                }
            };

            LogTargetConfigurationCacheStore.SaveConfigurationPatches(ToRawJsonPatches(currentConfigs));

            var cacheDocumentJson = File.ReadAllText(LogTargetConfigurationCacheStore.CacheDocumentFilePath);
            StringAssert.DoesNotContain("StaleConfiguration", cacheDocumentJson);
            StringAssert.Contains(nameof(TrackingLogTargetConfiguration), cacheDocumentJson);
        }

        [Test]
        public void LogTargetConfigurationCacheStore_WithPlayerPrefsBackend_BootstrapsWithoutFileSystemWrites()
        {
            LogTargetConfigurationCacheStore.UsePlayerPrefsStorageOverride = true;

            LogTargetConfigurationCacheStore.SaveConfigurationPatches(new Dictionary<string, string>
            {
                [nameof(TrackingLogTargetConfiguration)] = "{\"MinLogLevel\":3}"
            });

            Assert.That(File.Exists(LogTargetConfigurationCacheStore.CacheDocumentFilePath), Is.False);

            InitializeForTests("debug-user");

            Assert.That(_trackingTarget.Configuration.MinLogLevel, Is.EqualTo(LogLevel.Error));
            LogTargetConfigurationCacheStore.ClearCache();
            Assert.That(File.Exists(LogTargetConfigurationCacheStore.CacheDocumentFilePath), Is.False);
        }

        [Test]
        public void EffectiveRemoteOverrideStartupCacheSettings_WhenBackingFieldIsNull_DoesNotMutateAssetField()
        {
            var configuration = ScriptableObject.CreateInstance<LogManagerConfiguration>();
            configuration.RemoteOverrideStartupCache = null;

            var settings = configuration.EffectiveRemoteOverrideStartupCacheSettings;

            Assert.That(settings, Is.Not.Null);
            Assert.That(configuration.RemoteOverrideStartupCache, Is.Null);
        }

        [Test]
        public void Dispose_AfterInitialize_ClearsStateAndFutureLoggerFallsBack()
        {
            InitializeForTests("debug-user");

            LogManager.Dispose();
            ResetWasDisposedFlagOnly();

            var logger = LogManager.CreateLogger("Gameplay");

            Assert.That(_trackingTarget.DisposeCallCount, Is.EqualTo(1));
            Assert.That(logger, Is.TypeOf<FallbackLogger>());
        }

        [Test]
        public void Dispose_AfterInitialize_Twice_IsSafeAndDisposesTrackingTargetOnlyOnce()
        {
            InitializeForTests("debug-user");

            Assert.DoesNotThrow(() =>
            {
                LogManager.Dispose();
                LogManager.Dispose();
            });

            Assert.That(_trackingTarget.DisposeCallCount, Is.EqualTo(1));
        }

        [Test]
        public void CreateLogger_AfterDispose_ReturnsFallbackLogger()
        {
            InitializeForTests("debug-user");
            LogManager.Dispose();
            ResetWasDisposedFlagOnly();

            var logger = LogManager.CreateLogger("Gameplay");

            Assert.That(logger, Is.TypeOf<FallbackLogger>());
        }

        [Test]
        public void CreateLogger_AfterDispose_LogsMissingInitializationWarningOnlyOnce()
        {
            InitializeForTests("debug-user");
            LogManager.Dispose();
            ResetWasDisposedFlagOnly();

            var warnings = CaptureFallbackInitializationWarnings(() =>
            {
                LogManager.CreateLogger("Gameplay");
                LogManager.CreateLogger("Bootstrap");
                LogManager.CreateLogger("UI");
            });

            Assert.That(warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void Initialize_CalledTwice_DoesNotReplaceExistingRuntime()
        {
            InitializeForTests("debug-user");
            var replacementTarget = new TrackingLogTarget();

            LogManager.Initialize(new LogTarget[] { replacementTarget },
                                  DefaultResourceSubFolder,
                                  CancellationToken.None,
                                  "debug-user");

            var logger = LogManager.CreateLogger("Gameplay");
            logger.LogWarning("hello");

            Assert.That(_trackingTarget.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(replacementTarget.LoggedEntries.Count, Is.EqualTo(0));
        }

        [Test]
        public void Initialize_WithMultipleTargets_ContainsTrackingAndUnityEditorConsoleConfigurations()
        {
            var unityEditorConsoleTarget = new UnityEditorConsoleLogTarget();

            LogManager.Initialize(new LogTarget[] { _trackingTarget, unityEditorConsoleTarget },
                                  DefaultResourceSubFolder,
                                  CancellationToken.None,
                                  "debug-user");

            var configs = LogManager.GetCurrentLogTargetConfigurations();

            Assert.That(configs.Count, Is.EqualTo(2));
            Assert.That(configs, Contains.Key(nameof(TrackingLogTargetConfiguration)));
            Assert.That(configs, Contains.Key(nameof(UnityEditorConsoleLogTargetConfiguration)));
        }

        [Test]
        public void SetDebugMode_WithMultipleTargets_UpdatesMatchingTargets()
        {
            const string resourceSubFolderName = "LogManagerLifecycleTests_MultiTargetDebug";
            var unityEditorConsoleTarget = new UnityEditorConsoleLogTarget();

            CreateConfigurationAssets(_tempRootAssetPath,
                                      resourceSubFolderName,
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration
                                          {
                                              Enabled = true,
                                              IDs = new[] { "debug-user" },
                                              MinLogLevel = LogLevel.Debug
                                          },
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      },
                                      unityEditorConsoleConfig: new UnityEditorConsoleLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Error,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration
                                          {
                                              Enabled = true,
                                              IDs = new[] { "debug-user" },
                                              MinLogLevel = LogLevel.Info
                                          },
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      });

            LogManager.Initialize(new LogTarget[] { _trackingTarget, unityEditorConsoleTarget },
                                  resourceSubFolderName + "/",
                                  CancellationToken.None,
                                  "other-user");

            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.False);
            Assert.That(unityEditorConsoleTarget.IsLogLevelAllowed(LogLevel.Info, "Gameplay"), Is.False);
            Assert.That(unityEditorConsoleTarget.IsLogLevelAllowed(LogLevel.Error, "Gameplay"), Is.True);

            var enabled = LogManager.SetDebugMode("debug-user", true);

            Assert.That(enabled, Is.True);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.True);
            Assert.That(unityEditorConsoleTarget.IsLogLevelAllowed(LogLevel.Info, "Gameplay"), Is.True);
            Assert.That(unityEditorConsoleTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.False);

            var disabled = LogManager.SetDebugMode("debug-user", false);

            Assert.That(disabled, Is.True);
            Assert.That(_trackingTarget.IsLogLevelAllowed(LogLevel.Debug, "Gameplay"), Is.False);
            Assert.That(unityEditorConsoleTarget.IsLogLevelAllowed(LogLevel.Info, "Gameplay"), Is.False);
            Assert.That(unityEditorConsoleTarget.IsLogLevelAllowed(LogLevel.Error, "Gameplay"), Is.True);
        }

        [Test]
        public void Initialize_WithNullTargets_LeavesManagerUninitialized()
        {
            LogAssert.Expect(LogType.Error,
                             new Regex(@"\[FallbackLogger\] During LogManager initialization happened exception logTargets:",
                                       RegexOptions.Singleline));
            LogManager.Initialize(null, DefaultResourceSubFolder, CancellationToken.None, "debug-user");

            var logger = LogManager.CreateLogger("Gameplay");

            Assert.That(logger, Is.TypeOf<FallbackLogger>());
            Assert.That(LogManager.GetCurrentLogTargetConfigurations(), Is.Empty);
        }

        [Test]
        public void Initialize_WithEmptyTargets_LeavesManagerUninitialized()
        {
            LogAssert.Expect(LogType.Error,
                             new Regex(@"\[FallbackLogger\] During LogManager initialization happened exception logTargets:",
                                       RegexOptions.Singleline));
            LogManager.Initialize(Array.Empty<LogTarget>(), DefaultResourceSubFolder, CancellationToken.None, "debug-user");

            var logger = LogManager.CreateLogger("Gameplay");

            Assert.That(logger, Is.TypeOf<FallbackLogger>());
            Assert.That(LogManager.GetCurrentLogTargetConfigurations(), Is.Empty);
        }

        [Test]
        public void Initialize_WithMissingConfigurationPath_LeavesManagerUninitialized()
        {
            var originalIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            string capturedError = null;

            void Capture(string condition, string _, LogType type)
            {
                if (type == LogType.Error && condition.Contains("[FallbackLogger] During LogManager initialization happened exception"))
                {
                    capturedError = condition;
                }
            }

            Application.logMessageReceived += Capture;
            try
            {
                LogManager.Initialize(new LogTarget[] { _trackingTarget }, "MissingLogManagerConfigPath/", CancellationToken.None, "debug-user");
            }
            finally
            {
                Application.logMessageReceived -= Capture;
                LogAssert.ignoreFailingMessages = originalIgnoreFailingMessages;
            }

            var logger = LogManager.CreateLogger("Gameplay");

            Assert.That(Regex.IsMatch(capturedError ?? string.Empty,
                                      @".*\[FallbackLogger\] During LogManager initialization happened exception .*configuration.*",
                                      RegexOptions.Singleline),
                        Is.True,
                        $"Expected fallback initialization error to mention configuration. Actual: {capturedError}");
            Assert.That(logger, Is.TypeOf<FallbackLogger>());
            Assert.That(LogManager.GetCurrentLogTargetConfigurations(), Is.Empty);
        }

        [Test]
        public void Initialize_WithAllLogSourcesDisabled_SubscribesNoSources()
        {
            InitializeForTests("debug-user");

            var logSources = GetStaticField<IReadOnlyList<ILogSource>>("_logSources");

            Assert.That(logSources, Is.Empty);
        }

        [Test]
        public void Initialize_WithUnityDebugLogSourceEnabled_SubscribesExpectedSource()
        {
            CreateConfigurationAssets(_tempRootAssetPath,
                                      "LogManagerLifecycleTests_UnityDebugSource",
                                      new TrackingLogTargetConfiguration
                                      {
                                          MinLogLevel = LogLevel.Warning,
                                          IsThreadSafe = true,
                                          DebugMode = new DebugModeConfiguration(),
                                          BatchLogs = new LogTargetBatchLogsConfiguration(),
                                          DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
                                      },
                                      configuration =>
                                      {
                                          SetEditorLogSourceFlag(configuration, "_unityDebugLogSourceForUnityEditor", true);
                                      });

            LogManager.Initialize(new LogTarget[] { _trackingTarget },
                                  "LogManagerLifecycleTests_UnityDebugSource/",
                                  CancellationToken.None,
                                  "debug-user");

            var logSources = GetStaticField<IReadOnlyList<ILogSource>>("_logSources");

            Assert.That(logSources.Count, Is.EqualTo(1));
            Assert.That(logSources[0], Is.TypeOf<UnityDebugLogSource>());
        }

        private void InitializeForTests(string debugId)
        {
            LogManager.Initialize(new LogTarget[] { _trackingTarget }, DefaultResourceSubFolder, CancellationToken.None, debugId);
        }

        private static void CreateConfigurationAssets(string tempRootAssetPath,
                                                      string resourceSubFolderName,
                                                      TrackingLogTargetConfiguration trackingConfig,
                                                      Action<LogManagerConfiguration> configureLogManager = null,
                                                      UnityEditorConsoleLogTargetConfiguration unityEditorConsoleConfig = null)
        {
            var absoluteResourcesPath = Path.Combine(Application.dataPath,
                                                     tempRootAssetPath.Replace("Assets/", string.Empty),
                                                     "Resources",
                                                     resourceSubFolderName);
            Directory.CreateDirectory(absoluteResourcesPath);
            AssetDatabase.Refresh();

            var trackingConfigSo = ScriptableObject.CreateInstance<TrackingLogTargetConfigurationSO>();
            trackingConfigSo.SpecificConfiguration = trackingConfig;
            AssetDatabase.CreateAsset(trackingConfigSo,
                                      $"{tempRootAssetPath}/TrackingLogTargetConfigurationSO_{resourceSubFolderName}.asset");

            var unityEditorConsoleConfigSo = ScriptableObject.CreateInstance<UnityEditorConsoleLogTargetConfigurationSO>();
            unityEditorConsoleConfigSo.SpecificConfiguration =
                unityEditorConsoleConfig ?? CreateDefaultUnityEditorConsoleTargetConfiguration();
            AssetDatabase.CreateAsset(unityEditorConsoleConfigSo,
                                      $"{tempRootAssetPath}/UnityEditorConsoleLogTargetConfigurationSO_{resourceSubFolderName}.asset");

            var logManagerConfiguration = ScriptableObject.CreateInstance<LogManagerConfiguration>();
            logManagerConfiguration.DefaultUnityLogsCategoryName = "DefaultCategory";
            logManagerConfiguration.MessageMaxLength = 512;
            logManagerConfiguration.MinTimestampPeriodMs = 0;
            logManagerConfiguration.MinUpdatesPeriodMs = 1000;
            logManagerConfiguration.StackTraceFormatterConfiguration = new StackTraceFormatterConfiguration();
            logManagerConfiguration.UniTaskConfiguration = new UniTaskConfiguration();
            logManagerConfiguration.LogTargetConfigs = new LogTargetConfigurationSO[] { trackingConfigSo, unityEditorConsoleConfigSo };

            SetEditorLogSourceFlag(logManagerConfiguration, "_unityDebugLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_unityApplicationLogMessageReceivedSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_unityApplicationLogMessageReceivedThreadedSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_unobservedSystemTaskExceptionLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_unobservedUniTaskExceptionLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_systemDiagnosticsDebugLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_systemDiagnosticsConsoleLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_currentDomainUnhandledExceptionLogSourceForUnityEditor", false);
            configureLogManager?.Invoke(logManagerConfiguration);

            AssetDatabase.CreateAsset(logManagerConfiguration,
                                      $"{tempRootAssetPath}/Resources/{resourceSubFolderName}/LogManagerConfiguration.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static UnityEditorConsoleLogTargetConfiguration CreateDefaultUnityEditorConsoleTargetConfiguration()
        {
            return new UnityEditorConsoleLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Debug,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };
        }

        private static void SetEditorLogSourceFlag(LogManagerConfiguration configuration, string fieldName, bool value)
        {
            var field = typeof(LogManagerConfiguration).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(configuration, value);
        }

        private static void ConfigureConfigCache(LogManagerConfiguration configuration,
                                                 bool enabled = true)
        {
            configuration.RemoteOverrideStartupCache = new LogTargetConfigurationCacheSettings
            {
                Enabled = enabled
            };
        }

        private static void ResetLogManagerState()
        {
            LogManager.ResetConfigCacheTestState();
            SetStaticField("_wasDisposed", false);
            SetStaticField("_isInitialized", false);
            SetStaticField("_hasWarnedAboutMissingInitialization", false);
            SetStaticField("_isRunningUpdates", false);
            SetStaticField("_configuration", null);
            SetStaticField("_lastIncomingLogTargetConfigurationPatchesForCache", null);
            SetStaticField("_utilitySupplier", null);
            SetStaticField("_loggers", null);
            SetStaticField("_logSources", Array.Empty<ILogSource>());
            SetStaticField("_decoratedLogTargets", Array.Empty<ILogTarget>());
            SetStaticField("_originalLogTargets", Array.Empty<LogTarget>());
            SetStaticField("_targetUpdates", new List<IScheduledUpdate>());
            SetStaticField("_minUpdatesPeriodMs", (uint) 0);
            SetStaticField("_timeStampPrevious", default(DateTime));
            SetStaticField("_timeStampPreviousString", null);
            SetStaticField("_currentDebugId", null);
            SetStaticField("_debugModeRequestedState", false);
        }

        private static Dictionary<string, string> ToRawJsonPatches(Dictionary<string, LogTargetConfiguration> configurations)
        {
            var rawJsonPatches = new Dictionary<string, string>(configurations.Count);
            foreach (var pair in configurations)
            {
                rawJsonPatches[pair.Key] = JsonUtility.ToJson(pair.Value);
            }

            return rawJsonPatches;
        }

        private static string CreateUniqueResourceSubFolderName(string baseName)
        {
            return $"{baseName}_{Guid.NewGuid():N}";
        }

        private static void ClearAllConfigCacheBackends()
        {
            LogTargetConfigurationCacheStore.UsePlayerPrefsStorageOverride = false;
            LogTargetConfigurationCacheStore.ClearCache();

            LogTargetConfigurationCacheStore.UsePlayerPrefsStorageOverride = true;
            LogTargetConfigurationCacheStore.ClearCache();

            LogTargetConfigurationCacheStore.UsePlayerPrefsStorageOverride = null;
        }

        private static void WriteRawCacheDocumentForTests(string cacheDocumentJson)
        {
            if (LogTargetConfigurationCacheStore.UsePlayerPrefsStorageOverride == true)
            {
                var playerPrefsKey = GetPrivateConstantString(typeof(LogTargetConfigurationCacheStore), "CacheDocumentPlayerPrefsKey");
                PlayerPrefs.SetString(playerPrefsKey, cacheDocumentJson);
                PlayerPrefs.Save();
                return;
            }

            Directory.CreateDirectory(LogTargetConfigurationCacheStore.CacheDirectoryPath);
            File.WriteAllText(LogTargetConfigurationCacheStore.CacheDocumentFilePath, cacheDocumentJson);
        }

        private static void CorruptRawPatchInExistingCacheDocumentForTests(string targetName, string corruptedRawJsonPatch)
        {
            string cacheDocumentJson;
            if (LogTargetConfigurationCacheStore.UsePlayerPrefsStorageOverride == true)
            {
                var playerPrefsKey = GetPrivateConstantString(typeof(LogTargetConfigurationCacheStore), "CacheDocumentPlayerPrefsKey");
                cacheDocumentJson = PlayerPrefs.GetString(playerPrefsKey, string.Empty);
            }
            else
            {
                cacheDocumentJson = File.ReadAllText(LogTargetConfigurationCacheStore.CacheDocumentFilePath);
            }

            var cacheDocument = JsonUtility.FromJson<TestCacheDocument>(cacheDocumentJson);
            Assert.That(cacheDocument, Is.Not.Null);
            Assert.That(cacheDocument.Entries, Is.Not.Null.And.Length.GreaterThan(0));

            var wasUpdated = false;
            foreach (var entry in cacheDocument.Entries)
            {
                if (!string.Equals(entry.TargetName, targetName, StringComparison.Ordinal))
                {
                    continue;
                }

                entry.RawJsonPatch = corruptedRawJsonPatch;
                wasUpdated = true;
                break;
            }

            Assert.That(wasUpdated, Is.True, $"Missing cache document entry for {targetName}");
            WriteRawCacheDocumentForTests(JsonUtility.ToJson(cacheDocument));
        }

        private static string GetPrivateConstantString(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            return field.GetRawConstantValue() as string;
        }

        private static string BuildCacheDocumentJson(params (string targetName, string rawJsonPatch)[] entries)
        {
            var serializedEntries = new List<string>(entries.Length);
            foreach (var entry in entries)
            {
                serializedEntries.Add(
                    $"{{\"TargetName\":\"{entry.targetName}\",\"RawJsonPatch\":{JsonUtility.ToJson(entry.rawJsonPatch)}}}");
            }

            return $"{{\"Version\":1,\"Entries\":[{string.Join(",", serializedEntries)}]}}";
        }

        [Serializable]
        private sealed class TestCacheDocument
        {
            public int Version;
            public TestCacheDocumentEntry[] Entries;
        }

        [Serializable]
        private sealed class TestCacheDocumentEntry
        {
            public string TargetName;
            public string RawJsonPatch;
        }

        private static void ResetWasDisposedFlagOnly()
        {
            SetStaticField("_wasDisposed", false);
        }

        private static List<string> CaptureFallbackInitializationWarnings(Action action)
        {
            var originalIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            var warnings = new List<string>();

            void Capture(string condition, string _, LogType type)
            {
                if (type == LogType.Warning && condition.Contains("[FallbackLogger] LogManager is not initialized!"))
                {
                    warnings.Add(condition);
                }
            }

            Application.logMessageReceived += Capture;
            try
            {
                action();
            }
            finally
            {
                Application.logMessageReceived -= Capture;
                LogAssert.ignoreFailingMessages = originalIgnoreFailingMessages;
            }

            return warnings;
        }

        private static void SetStaticField(string fieldName, object value)
        {
            var field = typeof(LogManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(null, value);
        }

        private static T GetStaticField<T>(string fieldName)
        {
            var field = typeof(LogManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            return (T) field.GetValue(null);
        }
    }
}
