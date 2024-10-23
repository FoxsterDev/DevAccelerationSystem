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
        ///  Should be called from Unity main thread
        /// </summary>
        /// <param name="logTargets">List of targets to print logs. It has to be assigned log target configs so into logmanagerconfiguration</param>
        /// <param name="disposingToken">As example dispose logger when application exit</param>
        /// <param name="resourceSubFolderThatContainsConfigs">Inside Unity some Resources folder. Example "Logger/Dev/"</param>
        /// <param name="debugId">Is used to enabled debug mode</param>
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

                _debugId = !string.IsNullOrEmpty(debugId) ? debugId : SystemInfo.deviceUniqueIdentifier;
                logTargets = PatchUnityEditorConsoleLogTarget(logTargets);

                _originalLogTargets = logTargets;
                _loggers = new ConcurrentDictionary<string, ILogger>(4, 25);

                var configuration = Resources.Load<LogManagerConfiguration>(resourceSubFolderThatContainsConfigs + nameof(LogManagerConfiguration));

                _configuration = configuration;
                _utilitySupplier = new UtilitySupplier(_configuration.MinTimestampPeriodMs);
                _minUpdatesPeriodMs = _configuration.MinUpdatesPeriodMs;

                var dict = ConvertToDictionaryWithKeyNameAndValueConfigSpecificData(configuration.LogTargetConfigs);
                TryApplyConfigurations(dict, logTargets, _debugId);

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
                _logSources = logSources.AsReadOnly();

#if LOGGER_UNITY_EDITOR
                logSources.Add(new UnityDebugLogSource(logger as ILogConsumer));
                logSources.Add(new UnityApplicationLogSource(logger as ILogConsumer));
                logSources.Add(new UnityApplicationLogSourceThreaded(logger as ILogConsumer));
                logSources.Add(new CurrentDomainUnhandledExceptionLogSource(logger as ILogConsumer));
                logSources.Add(new UnobservedTaskExceptionLogSource(logger as ILogConsumer));
                logSources.Add(new SystemDiagnosticsDebugLogSource(logger as ILogConsumer));
#else
                logSources.Add(new UnityApplicationLogSourceThreaded(logger as ILogConsumer));
                logSources.Add(new UnobservedTaskExceptionLogSource(logger as ILogConsumer));
#endif

                if (_targetUpdates.Count > 0)
                {
                    RunUpdates(_targetUpdates, _minUpdatesPeriodMs, currentTimeUtc, disposingToken).FireAndForget();
                }

                Diagnostics.Write("LogManager has initialized!");
            }
            catch (Exception ex)
            {
                FallbackLogger.LogError($"During LogManager initialization happened exception {ex.Message}:\n{ex.StackTrace}");
                Dispose();
            }
        }

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

        public static void UpdateLogTargetsConfigurations(Dictionary<string, LogTargetConfiguration> logTargetConfigurations)
        {
            Diagnostics.Write("begin");

            if (IsNotProperlyConfigured())
            {
                return;
            }

            TryApplyConfigurations(logTargetConfigurations, _decoratedLogTargets, _debugId);

            Diagnostics.Write("end");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>In case if the logmanager is not initialized will return empty else fill the collection with the actual data from _decoratedLogTargets</returns>
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
        /// 
        /// </summary>
        /// <param name="debugId">UDID or playerId , any string that is configured in config per target</param>
        /// <param name="state"></param>
        public static void SetDebugMode(string debugId, bool state)
        {
            Diagnostics.Write("begin");

            if (IsNotProperlyConfigured())
            {
                return;
            }

            foreach (var logTarget in _decoratedLogTargets)
            {
                TrySetDebugMode(logTarget, debugId, state);
            }

            Diagnostics.Write("end");
        }

        public static bool AddGlobalTag(string tag)
        {
            Diagnostics.Write("begin");
            if (IsNotProperlyConfigured())
            {
                return false;
            }

            var result = _utilitySupplier.TagsRegistry.AddTag(tag);
            Diagnostics.Write("end "+result);
            return result;
        }
        
        public static bool RemoveGlobalTag(string tag)
        {
            Diagnostics.Write("begin");
            if (IsNotProperlyConfigured())
            {
                return false;
            }
            var result = _utilitySupplier.TagsRegistry.RemoveTag(tag);
            Diagnostics.Write("end "+result);
            return result;
        }
    }
}
