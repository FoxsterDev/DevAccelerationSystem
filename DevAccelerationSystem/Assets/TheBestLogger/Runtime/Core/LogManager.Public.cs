#if !UNITY_EDITOR || THEBESTLOGGER_PLATFORM_BUILD_SIMULATION
#define LOGGER_NOT_UNITY_EDITOR
#else
#define LOGGER_UNITY_EDITOR
#endif


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger
{
    public partial class LogManager
    {
        /// <summary>
        /// Initializes the LogManager with specified log targets, configuration path, and disposal token.
        /// Should be called from the Unity main thread.
        /// </summary>
        /// <param name="logTargets">List of log targets to output logs, requiring configurations in LogManagerConfiguration.</param>
        /// <param name="disposingToken">Token used to dispose the logger, e.g., when the application exits.</param>
        /// <param name="resourceSubFolderThatContainsConfigs">Path to the Unity Resources folder containing log configurations (e.g., "Logger/Dev/").</param>
        /// <param name="debugId">Optional explicit identifier for allowlist-based debug mode during initialization.</param>
        public static void Initialize(IReadOnlyList<LogTarget> logTargets,
                                      string resourceSubFolderThatContainsConfigs,
                                      CancellationToken disposingToken, string debugId = "")
        {
            var configuration = Resources.Load<LogManagerConfiguration>(
                resourceSubFolderThatContainsConfigs + nameof(LogManagerConfiguration));
            Initialize(logTargets, configuration, disposingToken, debugId);
        }

        /// <summary>
        /// Initializes the LogManager with explicit configuration without relying on Resources paths.
        /// Should be called from the Unity main thread.
        /// </summary>
        public static void Initialize(IReadOnlyList<LogTarget> logTargets,
                                      LogManagerConfiguration configuration,
                                      CancellationToken disposingToken, string debugId = "")
        {
            if (_isInitialized)
            {
                Diagnostics.Write("LogManager is already initialized! Wrong behaviour usage detected", LogLevel.Warning);
                return;
            }

            _wasDisposed = false;

            try
            {
                Diagnostics.Write("LogManager is initializing!");

                if (logTargets == null || logTargets.Count < 1)
                {
                    throw new ArgumentException(nameof(logTargets));
                }

                _disposingToken = disposingToken;
                _disposingToken.Register(Dispose);

                logTargets = PatchUnityEditorConsoleLogTarget(logTargets);

                _originalLogTargets = logTargets;
                _loggers = new ConcurrentDictionary<string, ILogger>(4, 25);

                if (configuration == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                _configuration = configuration;

                var stackTraceFormatter = new StackTraceFormatter(Application.dataPath, _configuration.StackTraceFormatterConfiguration);
                _utilitySupplier = new UtilitySupplier(_configuration.MinTimestampPeriodMs, stackTraceFormatter);

                _minUpdatesPeriodMs = _configuration.MinUpdatesPeriodMs;

                var dict = ConvertToDictionaryWithKeyNameAndValueConfigSpecificData(_configuration.LogTargetConfigs);
                TryOverlayCachedConfigurationsIfSupported(dict);
                TryApplyConfigurations(dict, logTargets, out _);

                var currentTimeUtc = _utilitySupplier.GetTimeStamp().currentTimeUtc;
                _decoratedLogTargets = TryDecorateLogTargets(logTargets, currentTimeUtc, _utilitySupplier);
                var sessionDebugRolloutSessionId = Interlocked.Increment(ref _nextSessionDebugRolloutSessionId);
                _sessionDebugRolloutSessionKey = $"debug-rollout:{sessionDebugRolloutSessionId}:{DateTime.UtcNow.Ticks}";

                _targetUpdates = TrySubscribeForUpdates(_decoratedLogTargets);
                InitializeSessionDebugModeStates(_decoratedLogTargets);

                UnityEngine.Debug.unityLogger.filterLogType = _configuration.DebugUnityLoggerFilterLogType;

#if LOGGER_NOT_UNITY_EDITOR
                SetApplicationLogTypesStackTrace(_configuration);
#endif
                _hasWarnedAboutMissingInitialization = false;
                _isInitialized = true;
                var logger = CreateLogger(_configuration.DefaultUnityLogsCategoryName);

                var logSources = new List<ILogSource>(4) { };
  
                if (_configuration.UnityDebugLogSourceEnabled)
                {
                    logSources.Add(new UnityDebugLogSource(logger as ILogConsumer));
                }

                if (_configuration.UnityApplicationLogMessageReceivedThreadedSourceEnabled)
                {
                    logSources.Add(new UnityApplicationLogSourceThreaded(logger as ILogConsumer));
                }
                else if (_configuration.UnityApplicationLogMessageReceivedSourceEnabled)
                {
                    logSources.Add(new UnityApplicationLogSource(logger as ILogConsumer));
                }

                if (_configuration.UnobservedSystemTaskExceptionLogSourceEnabled)
                {
                    logSources.Add(new UnobservedTaskExceptionLogSource(logger as ILogConsumer));
                }

                if (_configuration.UnobservedUniTaskExceptionLogSourceEnabled)
                {
                    logSources.Add(new UnobservedUniTaskExceptionLogSource(logger as ILogConsumer, _configuration.UniTaskConfiguration));
                }

                if (_configuration.CurrentDomainUnhandledExceptionLogSourceEnabled)
                {
                    logSources.Add(new CurrentDomainUnhandledExceptionLogSource(logger as ILogConsumer));
                }

                if (_configuration.SystemDiagnosticsDebugLogSourceEnabled)
                {
                    logSources.Add(new SystemDiagnosticsDebugLogSource(logger as ILogConsumer));
                }

                if (_configuration.SystemDiagnosticsConsoleLogSourceEnabled)
                {
                    logSources.Add(new SystemDiagnosticsConsoleLogSource(logger as ILogConsumer));
                }
    
                _logSources = logSources.AsReadOnly();

                if (_targetUpdates.Count > 0)
                {
                    RunUpdates(_targetUpdates, _minUpdatesPeriodMs, currentTimeUtc, disposingToken).FireAndForget();
                }

                _currentDebugId = null;
                _debugModeRequestedState = false;
                ReapplyCurrentDebugModeState();
                if (!string.IsNullOrEmpty(debugId))
                {
                    _currentDebugId = debugId;
                    _debugModeRequestedState = true;
                    SetDebugMode(debugId, true);
                }

                Diagnostics.Write("LogManager has initialized!");
            }
            catch (Exception ex)
            {
                FallbackLogger.LogError("During LogManager initialization happened exception.", ex);
                Dispose();
            }
        }

        /// <summary>
        /// Creates or retrieves a logger for a specific category.
        /// If the LogManager is not properly configured, returns the fallback logger.
        /// </summary>
        /// <param name="categoryName">Name of the category for the logger.</param>
        /// <param name="subCategoryName">Optionally to add some prefix to message</param>
        /// <returns>An ILogger instance for the specified category.</returns>
        public static ILogger CreateLogger(string categoryName, string subCategoryName = "")
        {
            Diagnostics.Write("begin for category: " + categoryName);

            if (IsNotProperlyConfigured())
            {
                return FallbackLogger;
            }

            if (string.IsNullOrEmpty(categoryName))
            {
                Diagnostics.Write("can not create a logger without categoryName, fallback logger will be returned", LogLevel.Error);
                return FallbackLogger;
            }

            var key = string.Concat(categoryName, subCategoryName);
            if (!_loggers.TryGetValue(key, out var logger))
            {
                Diagnostics.Write(" will create a new logger for category: " + categoryName);

                logger = new CoreLogger(categoryName, subCategoryName, _decoratedLogTargets, _utilitySupplier, _configuration.MessageMaxLength);
                _loggers.TryAdd(key, logger);
            }
            else
            {
                Diagnostics.Write(" will get a cached logger for category: " + categoryName);
            }

            return logger;
        }

        /// <summary>
        /// Applies a whole remote configuration document made of raw per-target JSON patches.
        /// This is the batch remote-config entrypoint and preserves partial-patch semantics for primitive fields.
        /// Returns false and an aggregated error message when validation or application fails.
        /// The document is applied atomically: if any patch is invalid, no patch from the batch is applied.
        /// </summary>
        /// <param name="rawJsonLogTargetConfigurations">Dictionary of raw json patches with log target configuration names as keys.</param>
        public static bool TryApplyRemoteConfigurationDocument(IReadOnlyDictionary<string, string> rawJsonLogTargetConfigurations,
                                                               out string error)
        {
            error = null;
            Diagnostics.Write("begin");

            if (IsNotProperlyConfigured())
            {
                error = "LogManager is not initialized or has no active log targets.";
                return false;
            }

            if (rawJsonLogTargetConfigurations == null || rawJsonLogTargetConfigurations.Count < 1)
            {
                error = "The remote configuration document is null or empty.";
                Diagnostics.Write(error, LogLevel.Warning);
                return false;
            }

            var rawJsonPatchDictionary = new Dictionary<string, string>(rawJsonLogTargetConfigurations.Count);
            foreach (var pair in rawJsonLogTargetConfigurations)
            {
                rawJsonPatchDictionary[pair.Key] = pair.Value;
            }

            if (!TryBuildEffectiveLogTargetConfigurationsForRawJsonUpdate(rawJsonPatchDictionary,
                                                                          out var effectiveLogTargetConfigurations,
                                                                          out var acceptedRawJsonLogTargetConfigurations,
                                                                          out error))
            {
                return false;
            }

            var applySucceeded = TryApplyConfigurations(effectiveLogTargetConfigurations, _decoratedLogTargets, out var applyError);
            ReapplyCurrentDebugModeState();
            if (applySucceeded)
            {
                _lastIncomingLogTargetConfigurationPatchesForCache = acceptedRawJsonLogTargetConfigurations;
                SaveEffectiveConfigurationsIfSupported();
            }

            error = applyError;

            Diagnostics.Write("end");
            return applySucceeded;
        }

        /// <summary>
        /// Applies a single raw JSON remote patch for one target configuration.
        /// Returns false and an error message when validation or application fails.
        /// </summary>
        public static bool TryApplyRemoteConfigurationPatch(string logTargetConfigurationName,
                                                            string rawJsonLogTargetConfiguration,
                                                            out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(logTargetConfigurationName))
            {
                error = "The logTargetConfigurationName is null or empty.";
                Diagnostics.Write(error, LogLevel.Warning);
                return false;
            }

            return TryApplyRemoteConfigurationDocument(new Dictionary<string, string>(1)
            {
                [logTargetConfigurationName] = rawJsonLogTargetConfiguration
            }, out error);
        }

        /// <summary>
        /// Enables or disables debug mode for specified log targets based on the debugId and state provided.
        /// If LogManager is not properly configured, logs a warning and returns false.
        /// </summary>
        /// <param name="debugId">Unique identifier (e.g., UDID, playerId) used by each log target configuration for explicit debug allowlists.</param>
        /// <param name="state">Debug mode state to set (true to enable, false to disable).</param>
        /// <returns>True if debug mode state changes, false otherwise.</returns>
        public static bool SetDebugMode(string debugId, bool state)
        {
            Diagnostics.Write("begin");

            if (IsNotProperlyConfigured())
            {
                Diagnostics.Write(nameof(IsNotProperlyConfigured));
                return false;
            }

            if (string.IsNullOrEmpty(debugId))
            {
                 FallbackLogger.LogWarning("Trying set debug mode with empty debugId");
                 return false;
            }

            _currentDebugId = debugId;
            _debugModeRequestedState = state;

            var debugModeStateChanged = false;
            foreach (var logTarget in _decoratedLogTargets)
            {
                debugModeStateChanged |= TryUpdateDebugModeStateForLogTarget(logTarget, debugId, state);
            }

            Diagnostics.Write("end");
            return debugModeStateChanged;
        }

        /// <summary>
        /// Adds a global tag to the tags registry, to be associated with all log outputs.
        /// If LogManager is not properly configured or tag is null/empty, logs a warning and returns false.
        /// </summary>
        /// <param name="tag">Tag to be added globally across all logs.</param>
        /// <returns>True if the tag is added successfully, false otherwise.</returns>
        public static bool AddGlobalTag(string tag)
        {
            Diagnostics.Write("begin");
            if (IsNotProperlyConfigured())
            {
                return false;
            }

            if (string.IsNullOrEmpty(tag))
            {
                FallbackLogger.LogWarning("Trying set empty or null global tag");
                return false;
            }

            var result = _utilitySupplier.TagsRegistry.AddTag(tag);
            Diagnostics.Write("end "+result);
            return result;
        }

        /// <summary>
        /// Removes a global tag from the tags registry.
        /// If LogManager is not properly configured or tag is null/empty, logs a warning and returns false.
        /// </summary>
        /// <param name="tag">Tag to be removed from the global registry.</param>
        /// <returns>True if the tag is removed successfully, false otherwise.</returns>
        public static bool RemoveGlobalTag(string tag)
        {
            Diagnostics.Write("begin");
            if (IsNotProperlyConfigured())
            {
                return false;
            }

            if (string.IsNullOrEmpty(tag))
            {
                FallbackLogger.LogWarning("Trying remove empty or null global tag");
                return false;
            }

            var result = _utilitySupplier.TagsRegistry.RemoveTag(tag);
            Diagnostics.Write("end "+result);
            return result;
        }
    }
}
