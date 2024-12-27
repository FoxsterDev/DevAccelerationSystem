#if !UNITY_EDITOR || LOGGER_PLATFORM_BUILD_SIMULATION
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
        /// <param name="debugId">Optional identifier for enabling debug mode. If empty, a device ID will be used.</param>
        public static void Initialize(IReadOnlyList<LogTarget> logTargets,
                                      string resourceSubFolderThatContainsConfigs,
                                      CancellationToken disposingToken, string debugId = "")
        {
            if (_isInitialized)
            {
                Diagnostics.Write("LogManager is already initialized! Wrong behaviour usage detected", LogLevel.Warning);
                return;
            }

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

                var configuration = Resources.Load<LogManagerConfiguration>(resourceSubFolderThatContainsConfigs + nameof(LogManagerConfiguration));

                _configuration = configuration;

                var stackTraceFormatter = new StackTraceFormatter(Application.dataPath, _configuration.StackTraceFormatterConfiguration);
                _utilitySupplier = new UtilitySupplier(_configuration.MinTimestampPeriodMs, stackTraceFormatter);

                _minUpdatesPeriodMs = _configuration.MinUpdatesPeriodMs;

                var dict = ConvertToDictionaryWithKeyNameAndValueConfigSpecificData(configuration.LogTargetConfigs);
                TryApplyConfigurations(dict, logTargets);

                var currentTimeUtc = _utilitySupplier.GetTimeStamp().currentTimeUtc;
                _decoratedLogTargets = TryDecorateLogTargets(logTargets, currentTimeUtc, _utilitySupplier);

                _targetUpdates = TrySubscribeForUpdates(_decoratedLogTargets);

                UnityEngine.Debug.unityLogger.filterLogType = configuration.DebugUnityLoggerFilterLogType;

#if LOGGER_NOT_UNITY_EDITOR
                SetApplicationLogTypesStackTrace(_configuration);
#endif
                _isInitialized = true;
                var logger = CreateLogger(configuration.DefaultUnityLogsCategoryName);

                var logSources = new List<ILogSource>(4) { };
  
                if (configuration.UnityDebugLogSourceEnabled)
                {
                    logSources.Add(new UnityDebugLogSource(logger as ILogConsumer));
                }

                if (configuration.UnityApplicationLogMessageReceivedThreadedSourceEnabled)
                {
                    logSources.Add(new UnityApplicationLogSourceThreaded(logger as ILogConsumer));
                }
                else if (configuration.UnityApplicationLogMessageReceivedSourceEnabled)
                {
                    logSources.Add(new UnityApplicationLogSource(logger as ILogConsumer));
                }

                if (configuration.UnobservedSystemTaskExceptionLogSourceEnabled)
                {
                    logSources.Add(new UnobservedTaskExceptionLogSource(logger as ILogConsumer));
                }

                if (configuration.UnobservedUniTaskExceptionLogSourceEnabled)
                {
                    logSources.Add(new UnobservedUniTaskExceptionLogSource(logger as ILogConsumer));
                }

                if (configuration.CurrentDomainUnhandledExceptionLogSourceEnabled)
                {
                    logSources.Add(new CurrentDomainUnhandledExceptionLogSource(logger as ILogConsumer));
                }

                if (configuration.SystemDiagnosticsDebugLogSourceEnabled)
                {
                    logSources.Add(new SystemDiagnosticsDebugLogSource(logger as ILogConsumer));
                }

                if (configuration.SystemDiagnosticsConsoleLogSourceEnabled)
                {
                    logSources.Add(new SystemDiagnosticsConsoleLogSource(logger as ILogConsumer));
                }
    
                _logSources = logSources.AsReadOnly();

                if (_targetUpdates.Count > 0)
                {
                    RunUpdates(_targetUpdates, _minUpdatesPeriodMs, currentTimeUtc, disposingToken).FireAndForget();
                }

                var id = !string.IsNullOrEmpty(debugId) ? debugId : SystemInfo.deviceUniqueIdentifier;
                SetDebugMode(id, true);

                Diagnostics.Write("LogManager has initialized!");
            }
            catch (Exception ex)
            {
                FallbackLogger.LogError($"During LogManager initialization happened exception {ex.Message}:\n{ex.StackTrace}");
                Dispose();
            }
        }

        /// <summary>
        /// Creates or retrieves a logger for a specific category.
        /// If the LogManager is not properly configured, returns the fallback logger.
        /// </summary>
        /// <param name="categoryName">Name of the category for the logger.</param>
        /// <returns>An ILogger instance for the specified category.</returns>
        public static ILogger CreateLogger(string categoryName)
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

            if (!_loggers.TryGetValue(categoryName, out var logger))
            {
                Diagnostics.Write(" will create a new logger for category: " + categoryName);

                logger = new CoreLogger(categoryName, _decoratedLogTargets, _utilitySupplier);
                _loggers.TryAdd(categoryName, logger);
            }
            else
            {
                Diagnostics.Write(" will get a cached logger for category: " + categoryName);
            }

            return logger;
        }

        /// <summary>
        /// Updates configurations for existing log targets based on the provided configurations dictionary.
        /// If LogManager is not properly configured or configurations are null/empty, logs a warning and exits.
        /// </summary>
        /// <param name="logTargetConfigurations">Dictionary of log target configurations with log target names as keys.</param>
        public static void UpdateLogTargetsConfigurations(Dictionary<string, LogTargetConfiguration> logTargetConfigurations)
        {
            Diagnostics.Write("begin");

            if (IsNotProperlyConfigured())
            {
                return;
            }

            if (logTargetConfigurations == null || logTargetConfigurations.Count < 1)
            {
                Diagnostics.Write("The logTargetConfigurations are empty or null", LogLevel.Warning);
                return;
            }

            TryApplyConfigurations(logTargetConfigurations, _decoratedLogTargets);

            Diagnostics.Write("end");
        }

        /// <summary>
        /// Retrieves the current configurations for each decorated log target.
        /// If LogManager is not initialized, returns an empty dictionary.
        /// </summary>
        /// <returns>Dictionary with current log target configurations, keyed by log target name.</returns>
        public static Dictionary<string, LogTargetConfiguration> GetCurrentLogTargetConfigurations()
        {
            Diagnostics.Write("begin");

            var logTargetConfigurations = new Dictionary<string, LogTargetConfiguration>(3);

            if (IsNotProperlyConfigured())
            {
                return logTargetConfigurations;
            }

            foreach (var logTarget in _decoratedLogTargets)
            {
                logTargetConfigurations[logTarget.LogTargetConfigurationName] = logTarget.Configuration;
            }

            Diagnostics.Write("end");

            return logTargetConfigurations;
        }

        /// <summary>
        /// Enables or disables debug mode for specified log targets based on the debugId and state provided.
        /// If LogManager is not properly configured, logs a warning and returns false.
        /// </summary>
        /// <param name="debugId">Unique identifier (e.g., UDID, playerId) specific to each log target configuration.</param>
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
