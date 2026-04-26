using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
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
            LogAssert.Expect(LogType.Error,
                             new Regex(@"\[FallbackLogger\] During LogManager initialization happened exception Object reference not set to an instance of an object:",
                                       RegexOptions.Singleline));
            LogManager.Initialize(new LogTarget[] { _trackingTarget }, "MissingLogManagerConfigPath/", CancellationToken.None, "debug-user");

            var logger = LogManager.CreateLogger("Gameplay");

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

        private static void ResetLogManagerState()
        {
            SetStaticField("_wasDisposed", false);
            SetStaticField("_isInitialized", false);
            SetStaticField("_isRunningUpdates", false);
            SetStaticField("_configuration", null);
            SetStaticField("_utilitySupplier", null);
            SetStaticField("_loggers", null);
            SetStaticField("_logSources", Array.Empty<ILogSource>());
            SetStaticField("_decoratedLogTargets", Array.Empty<ILogTarget>());
            SetStaticField("_originalLogTargets", Array.Empty<LogTarget>());
            SetStaticField("_targetUpdates", new List<IScheduledUpdate>());
            SetStaticField("_minUpdatesPeriodMs", (uint) 0);
            SetStaticField("_timeStampPrevious", default(DateTime));
            SetStaticField("_timeStampPreviousString", null);
        }

        private static void ResetWasDisposedFlagOnly()
        {
            SetStaticField("_wasDisposed", false);
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
