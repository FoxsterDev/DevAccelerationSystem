#if !UNITY_EDITOR || THEBESTLOGGER_PLATFORM_BUILD_SIMULATION
#define LOGGER_NOT_UNITY_EDITOR
#else
#define LOGGER_UNITY_EDITOR
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger
{
    //references https://learn.microsoft.com/en-us/dotnet/core/extensions/logging
    //https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/beta/debugging-logging
    //https://opentelemetry.io/docs/what-is-opentelemetry/

    public partial class LogManager
    {
        private static ConcurrentDictionary<string, ILogger> _loggers;
        private static IReadOnlyList<ILogTarget> _decoratedLogTargets = Array.Empty<ILogTarget>();
        private static IReadOnlyList<LogTarget> _originalLogTargets = Array.Empty<LogTarget>();
        private static IReadOnlyList<ILogSource> _logSources = Array.Empty<ILogSource>();
        private static LogManagerConfiguration _configuration;
        private static UtilitySupplier _utilitySupplier;
        private static uint _minUpdatesPeriodMs;
        private static DateTime _timeStampPrevious;
        private static string _timeStampPreviousString;

        private static bool _isRunningUpdates = false;
        private static bool _wasDisposed = false;
        private static List<IScheduledUpdate> _targetUpdates;
        private static CancellationToken _disposingToken;
        private static bool _isInitialized = false;
        private static readonly ILogger FallbackLogger = new FallbackLogger();


        private static bool IsNotProperlyConfigured()
        {
            if (!_isInitialized)
            {
                FallbackLogger.LogWarning(
                    "LogManager is not initialized!");
                return true;
            }

            if (_decoratedLogTargets == null || _decoratedLogTargets.Count < 1)
            {
                Diagnostics.Write("The current LogTargets are empty or null", LogLevel.Warning);
                return true;
            }

            return false;
        }

#if LOGGER_UNITY_EDITOR
        /// <summary>
        /// to avoid changes on scriptable objects when modified configs at runtime
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T DeepCopyInUnityEditor<T>(T obj)
        {
            var json = JsonUtility.ToJson(obj);
            return (T) (object) JsonUtility.FromJson(json, obj.GetType());
        }
#endif

        /// <summary>
        /// Key of dict is logTargetConfigSo.Configuration.GetType().Name. Value is Speficic LogTargetConfiguration
        /// </summary>
        /// <param name="logTargetConfigurationsSo"></param>
        /// <returns></returns>
        private static Dictionary<string, LogTargetConfiguration> ConvertToDictionaryWithKeyNameAndValueConfigSpecificData(
            LogTargetConfigurationSO[] logTargetConfigurationsSo)
        {
            var logTargetConfigurationsData = new Dictionary<string, LogTargetConfiguration>();

            foreach (var logTargetConfigSo in logTargetConfigurationsSo)
            {
                if (logTargetConfigSo != null)
                {
                    var key = logTargetConfigSo.Configuration.GetType().Name;

#if LOGGER_NOT_UNITY_EDITOR
                    logTargetConfigurationsData[key] = logTargetConfigSo.Configuration;
#else
                    var config = logTargetConfigSo.Configuration;
                    var logTargetConfigurationNew = DeepCopyInUnityEditor(config);
                    logTargetConfigurationsData[key] = logTargetConfigurationNew;
#endif
                }
                else
                {
                    Diagnostics.Write("logTargetConfigSO is null", LogLevel.Warning);
                }
            }

            return logTargetConfigurationsData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logTarget"></param>
        /// <param name="debugId">Key to match with predefined </param>
        /// <param name="state"></param>
        /// <returns>True if debugMode was changed for some logtarget</returns>
        private static bool TryUpdateDebugModeStateForLogTarget(ILogTarget logTarget,
                                            string debugId,
                                            bool state)
        {
            Diagnostics.Write("begin");

            var debugModeState = false;
            var debugModeStateChanged = false;
            if (logTarget?.Configuration != null && logTarget.DebugModeEnabled != state)
            {
                var debugMode = logTarget.Configuration.DebugMode;

                if (debugMode.Enabled && debugMode.IDs != null && debugMode.IDs.Length > 0)
                {
                    foreach (var id in debugMode.IDs)
                    {
                        if (id == debugId)
                        {
                            Diagnostics.Write($"For LogTarget: {logTarget.GetType()} was enabled {state} debugMode");
                            debugModeState = state;
                            debugModeStateChanged = true;
                            break;
                        }
                    }
                }

                //does it make sense to plaer prefs safe to get logs from launch the game ?
                logTarget.DebugModeEnabled = debugModeState;
            }
            else
            {
                Diagnostics.Write(
                    $"logTarget or logTarget.configuration is null", LogLevel.Error);
            }

            Diagnostics.Write("end with debugModeEnabled:"+debugModeState);
            return debugModeStateChanged;
        }

        /// <summary>
        /// Manager will apply automaticaly configs from provided lists with logTarget.GetType().Name+Configuration == logTargetConfig.name
        /// </summary>
        /// <param name="logTargetConfigurations"></param>
        /// <param name="logTargets"></param>
        /// <param name="debugId"></param>
        private static void TryApplyConfigurations(Dictionary<string, LogTargetConfiguration> logTargetConfigurations,
                                                   IReadOnlyList<ILogTarget> logTargets)
        {
            Diagnostics.Write("begin");

            if (logTargetConfigurations == null || logTargetConfigurations.Count < 1)
            {
                Diagnostics.Write("logTargetConfigurations is null or empty", LogLevel.Warning);
                return;
            }

            if (logTargets == null || logTargets.Count < 1)
            {
                Diagnostics.Write("logTargets is null or empty", LogLevel.Warning);
                return;
            }

            foreach (var logTarget in logTargets)
            {
                if (logTarget == null)
                {
                    Diagnostics.Write("Log target is null", LogLevel.Error);
                    continue;
                }

                var logTargetConfigurationName = logTarget.LogTargetConfigurationName;
                Diagnostics.Write("logTarget: " + logTarget.GetType() + " with " + logTargetConfigurationName, LogLevel.Debug);

                logTargetConfigurations.TryGetValue(logTargetConfigurationName, out var logTargetConfiguration);
                if (logTargetConfiguration == null)
                {
                    Diagnostics.Write(
                        $"Can not match {logTargetConfigurationName} with configurations files. LogTargetConfigurationDefault was applied!", LogLevel.Error);
                    logTarget.ApplyConfiguration(new LogTargetConfigurationDefault(logTarget.GetType().ToString()));
                    continue;
                }

                logTarget.ApplyConfiguration(logTargetConfiguration);
                Diagnostics.Write(
                    "For LogTarget:" + logTarget.GetType() + " was applied logtargetconfiguration:" + logTargetConfiguration.GetType());
            }

            Diagnostics.Write("end");
        }

        private static IReadOnlyList<ILogTarget> TryDecorateLogTargets(
            IReadOnlyList<LogTarget> originalLogTargets,
            DateTime currentTimeUtc, UtilitySupplier utilitySupplier)
        {
            Diagnostics.Write("begin");

            var list = new List<ILogTarget>(originalLogTargets.Count);

            foreach (var originalLogTarget in originalLogTargets)
            {
                var config = originalLogTarget.Configuration;
                if (config == null)
                {
                    Diagnostics.Write("originalLogTarget.Configuration == null for " + originalLogTarget.GetType(), LogLevel.Error);
                    continue;
                }

                ILogTarget decoratedLogTarget = null;
                if (config.DispatchingLogsToMainThread.Enabled)
                {
                    if (config.IsThreadSafe)
                    {
                        FallbackLogger.LogWarning($" {config} IsThreadSafe but DispatchingLogsToMainThread.Enabled");
                    }

                    decoratedLogTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                                             config.DispatchingLogsToMainThread, originalLogTarget, SynchronizationContext.Current, utilitySupplier) as ILogTarget;

                    Diagnostics.Write(
                        originalLogTarget.GetType() + " was decorated by " +
                        decoratedLogTarget.GetType());
                }

                if (config.BatchLogs.Enabled)
                {
                    var toDecorate = decoratedLogTarget ?? originalLogTarget;
                    decoratedLogTarget = new LogTargetBatchLogsDecoration(
                                             config.BatchLogs, toDecorate, currentTimeUtc) as ILogTarget;

                    Diagnostics.Write(
                        toDecorate.GetType() + " was decorated by " +
                        decoratedLogTarget.GetType());

                }

                if (decoratedLogTarget != null)
                {
                    list.Add(decoratedLogTarget);
                }
                else
                {
                    list.Add(originalLogTarget);
                }

            }

            Diagnostics.Write("end");
            return list.AsReadOnly();
        }

        private static List<IScheduledUpdate> TrySubscribeForUpdates(
            IReadOnlyList<ILogTarget> logTargets)
        {
            var list = new List<IScheduledUpdate>(logTargets.Count);
            foreach (var logTarget in logTargets)
            {
                if (logTarget is IScheduledUpdate update)
                {
                    Diagnostics.Write(logTarget.GetType() + " is scheduled for periodical updates with PeriodMs:" + update.PeriodMs);
                    list.Add(update);
                }
            }

            return list;
        }

        private static async Task RunUpdates(IReadOnlyList<IScheduledUpdate> targetUpdates,
                                             uint minUpdatesPeriodMs,
                                             DateTime currentTimeUtc,
                                             CancellationToken cancellationToken)
        {
            Diagnostics.Write(" starting");

            _isRunningUpdates = true;
            var previousTimeStamp = currentTimeUtc;
            var minUpdate = (int) Math.Min(targetUpdates.Min(k => k.PeriodMs), minUpdatesPeriodMs);

            while (_isRunningUpdates)
            {
                await Task.Delay(minUpdate, cancellationToken).ConfigureAwait(true);

                if (cancellationToken.IsCancellationRequested)
                {
                    Diagnostics.Write("isCancellationRequested");
                    return;
                }

                var currentTimeStamp = _utilitySupplier.GetTimeStamp();
                var deltaMs = (uint) (currentTimeStamp.currentTimeUtc - previousTimeStamp).TotalMilliseconds;
                previousTimeStamp = currentTimeStamp.currentTimeUtc;

                //_defaultLogger?.LogDebug("Update: "+deltaMs+", "+currentTimeStamp.timeStampCached);
                foreach (var target in targetUpdates)
                {
                    target.Update(currentTimeStamp.currentTimeUtc, deltaMs);
                }
            }

            Diagnostics.Write(" finished");
        }

        private static void SetApplicationLogTypesStackTrace(LogManagerConfiguration logManagerConfiguration)
        {
            foreach (var entry in logManagerConfiguration.ApplicationLogTypesStackTrace)
            {
                Application.SetStackTraceLogType(entry.LogType, entry.StackTraceLevel);
            }
        }

        private static IReadOnlyList<LogTarget> PatchUnityEditorConsoleLogTarget(IReadOnlyList<LogTarget> logTargets)
        {
            LogTarget unityEditorConsoleLogTarget = default;
            foreach (var logTarget in logTargets)
            {
                if (logTarget is UnityEditorConsoleLogTarget)
                {
                    Diagnostics.Write("LogTargets contain UnityEditorConsoleLogTarget of type " + logTarget.GetType().Name);
                    unityEditorConsoleLogTarget = logTarget;
                }
            }

#if LOGGER_UNITY_EDITOR
            if (unityEditorConsoleLogTarget == null)
            {
                var logTargetsTemp = logTargets.ToList();
                logTargetsTemp.Add(new UnityEditorConsoleLogTarget());
                logTargets = logTargetsTemp.AsReadOnly();
                Diagnostics.Write(
                    "In Unity editor runtime was added UnityEditorConsoleLogTarget into logTargets because missed any inheritances of UnityEditorConsoleLogTarget");
            }
#else
            if (unityEditorConsoleLogTarget != null)
            {
                var logTargetsTemp = logTargets.ToList();
                logTargetsTemp.Remove(unityEditorConsoleLogTarget);
                logTargets = logTargetsTemp.AsReadOnly();
                Diagnostics.Write(
                    $"In platform runtime was removed {unityEditorConsoleLogTarget.GetType().Name} because an issue with Application.logMessageRecived ",
                    LogLevel.Warning);
            }
#endif
            return logTargets;
        }

        public static void Dispose()
        {
            if (_wasDisposed)
            {
                return;
            }

            _wasDisposed = true;
            Diagnostics.Write("is disposing!");

            _isInitialized = false;

            _isRunningUpdates = false;
            _configuration = null;
            _utilitySupplier = null;
            if (_targetUpdates != null)
            {
                _targetUpdates.Clear();
            }

            if (_logSources != null)
            {
                foreach (var l in _logSources)
                {
                    l.Dispose();
                }

                _logSources = null;
            }

            if (_loggers != null)
            {
                foreach (var logger in _loggers)
                {
                    logger.Value.Dispose();
                }

                _loggers.Clear();
                _loggers = null;
            }

            if (_originalLogTargets != null)
            {
                foreach (var logTarget in _originalLogTargets)
                {
                    logTarget.Dispose();
                }

                _originalLogTargets = null;
            }

            if (_decoratedLogTargets != null)
            {
                foreach (var logTarget in _decoratedLogTargets)
                {
                    logTarget.Dispose();
                }

                _decoratedLogTargets = null;
            }

            Diagnostics.Write("has disposed!");
            Diagnostics.Cancel();
        }
    }
}
