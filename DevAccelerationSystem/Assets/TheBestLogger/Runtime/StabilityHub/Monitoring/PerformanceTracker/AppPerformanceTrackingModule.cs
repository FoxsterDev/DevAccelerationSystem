using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using StabilityHub.Monitoring;
using TheBestLogger;
using UnityEngine;

namespace StabilityHub.PerformanceTracking
{
    internal class AppPerformanceTrackingModule : IDisposable
    {
        private readonly AppPerformanceTrackingModuleConfiguration _config;
        private readonly Dictionary<byte, byte> _eventCountThisSecond = new(EventTypes.Capacity);
        private readonly TheBestLogger.ILogger _logger;
        private CancellationToken _cancellationToken;
        private bool _isDisposed = false;
        private bool _isInitialized = false;

        private uint _lastMinuteReset, _lastSecondReset;

        private bool _minBatteryLevelTracked = false;
        private int _totalEventsThisMinute;

        public AppPerformanceTrackingModule(AppPerformanceTrackingModuleConfiguration config,
                                            TheBestLogger.ILogger logger)
        {
            _logger = logger;
            _config = config;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            UnSubscribeToUnityEvents();
            if (!_cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("Disposed but cancellationToken is not cancelled");
            }
        }

        internal async UniTask<bool> Initialize(CancellationToken cancelToken = default)
        {
            if (_isInitialized)
            {
                _logger.LogTrace("Already initialized");
                return false;
            }

            _cancellationToken = cancelToken;

            try
            {
                SubscribeToUnityEvents();

                await StartAllMonitoringTasks();

                _isInitialized = true;

                _logger?.LogTrace("Initialized");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug($"Initialization failed: {ex.Message}");
                Dispose();
                return false;
            }
        }

        private void OnQualityLevelChanged(int previousLevel, int currentLevel)
        {
            LogEvent(EventTypes.QualityChanged);
        }

        private void SubscribeToUnityEvents()
        {
            Application.lowMemory += OnLowMemory;

            QualitySettings.activeQualityLevelChanged += OnQualityLevelChanged;

#if UNITY_2021_2_OR_NEWER
            Application.memoryUsageChanged += OnMemoryUsageChanged;
#endif
        }

        private void UnSubscribeToUnityEvents()
        {
            Application.lowMemory -= OnLowMemory;

            QualitySettings.activeQualityLevelChanged -= OnQualityLevelChanged;

#if UNITY_2021_2_OR_NEWER
            Application.memoryUsageChanged -= OnMemoryUsageChanged;
#endif
        }

        private async UniTask StartAllMonitoringTasks()
        {
            var tasks = new List<UniTask>(5);

            tasks.Add(BatteryMonitoringTask().SuppressCancellationThrow());

            // Platform-specific thermal monitoring
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
          //  tasks.Add(ThermalMonitoringTask().SuppressCancellationThrow());
#endif

            foreach (var task in tasks)
            {
                task.Forget();
            }

            await UniTask.Yield();
            _logger.LogTrace($"Started {tasks.Count} monitoring tasks");
        }

        private async UniTask BatteryMonitoringTask()
        {
            var interval = TimeSpan.FromSeconds(_config.DrainRateTrackingIntervalInSeconds);

            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //If the battery level is not available on your target platform, this property returns -1.
                    var previousBatteryLevel = SystemInfo.batteryLevel;
                    if (previousBatteryLevel < 0f)
                    {
                        _logger.LogDebug("Battery level not available on this platform, skipping battery monitoring. You can segment the config");
                        return;
                    }

                    await UniTask.Delay(interval, cancellationToken: _cancellationToken);
                   
                    var currentBatteryLevel = SystemInfo.batteryLevel;
                    var drainRate = previousBatteryLevel - currentBatteryLevel;

                    if (currentBatteryLevel > 0)
                    {
                        if (!_minBatteryLevelTracked && drainRate * 100 >= _config.DrainRateInPercent)
                        {
                            _minBatteryLevelTracked = LogEvent(EventTypes.LowBattery);
                        }

                        if (currentBatteryLevel * 100 < _config.MinBatteryLevelInPercent)
                        {
                            LogEvent(EventTypes.BatteryDraining);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    //LogMessage($"[PerformanceMetrics] Battery monitoring error: {ex.Message}", LogType.Error);
                    // Continue with normal interval timing
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool LogEvent(byte eventType)
        {
            var currentTime = (uint) (Time.realtimeSinceStartupAsDouble * 1000);

            if (currentTime - _lastSecondReset > 1000)
            {
                _eventCountThisSecond.Clear();
                _lastSecondReset = currentTime;
            }

            if (currentTime - _lastMinuteReset > 60000)
            {
                _totalEventsThisMinute = 0;
                _lastMinuteReset = currentTime;
            }

            if ((_eventCountThisSecond.TryGetValue(eventType, out var count) &&
                 count >= _config.MaxLogEventsOfSomeTypeWithinOneSecond) || _totalEventsThisMinute >= _config.MaxTotalLogEventsPerMinute)
            {
                return false;
            }

            _totalEventsThisMinute++;
            _eventCountThisSecond[eventType] = (byte) (count + 1);

            _logger.LogFormat(
                _config.LogLevelToTrack, EventTypes.GetEventTypeName(eventType),
                new LogAttributes("CountPerSecond", count).Add("TotalEvents", _totalEventsThisMinute));
            return true;
        }

        private void OnLowMemory()
        {
            LogEvent(EventTypes.LowMemory);
        }

#if UNITY_2021_2_OR_NEWER
        /*
                /// <summary>
               ///   <para>The memory usage level of the application is not known.</para>
               /// </summary>
               Unknown,
               /// <summary>
               ///   <para>Application can safely allocate significant amounts of memory.</para>
               /// </summary>
               Low,
               /// <summary>
               ///   <para>Application is at safe memory usage level and has some margin to allocate more.</para>
               /// </summary>
               Medium,
               /// <summary>
               ///   <para>Application is at risk of getting low on memory. To prevent this, avoid allocating significant amounts of memory, and release some resources.</para>
               /// </summary>
               High,
               /// <summary>
               ///   <para>Application is dangerously low on memory and is at risk of being closed by the Operating System. To prevent this, release some resources immediately.</para>
               /// </summary>
               Critical,
             */
        private void OnMemoryUsageChanged(in ApplicationMemoryUsageChange change)
        {
            if (_config.MemoryUsage == ApplicationMemoryUsage.Unknown || change.memoryUsage == ApplicationMemoryUsage.Unknown)
            {
                return;
            }

            if (_config.MemoryUsage >= change.memoryUsage)
            {
                LogEvent(EventTypes.MemoryCriticalUsage);
            }
        }
#endif

        private class EventTypes
        {
            public const byte LowMemory = 1;
            public const byte QualityChanged = 2;
            public const byte MemoryCriticalUsage = 3;
            public const byte LowBattery = 4;
            public const byte BatteryDraining = 5;

            public const int Capacity = 6;

            public static string GetEventTypeName(byte eventType)
            {
                return eventType switch
                {
                    LowMemory => "MEM_LOW",
                    QualityChanged => "QUAL",
                    MemoryCriticalUsage => "MEM_CRIT",
                    LowBattery => "BAT_LOW",
                    BatteryDraining => "BAT_DRAIN",
                    _ => "UNK"
                };
            }
        }
    }
}
