using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
namespace StabilityHub.PerformanceTracking
{
/// <summary>
/// High-performance standalone metrics collector designed for production mobile builds.
/// Zero-allocation design with adaptive sampling and intelligent throttling.
/// Uses pure UniTask approach without Player Loop modifications.
/// </summary>
public sealed class PerformanceMetricsCollectorDraft : IDisposable
{
    [Serializable]
    public struct MetricsConfig
    {
        [Header("Performance Settings")]
        public bool enableInProduction;

        public ProfileLevel profileLevel;
        public float adaptiveSamplingMultiplier;

        [Header("Thresholds")]
        public float lowFpsThreshold;

        public float criticalFpsThreshold;
        public long memorySpikeMB;
        public float batteryWarningLevel;

        [Header("Anti-Spam")]
        public int maxEventsPerSecond;

        public int maxTotalEventsPerMinute;

        [Header("Intervals (seconds)")]
        public float fpsCheckInterval;

        public float batteryCheckInterval;
        public float thermalCheckInterval;
        public float adaptiveSamplingUpdateInterval;
        public float spamCounterResetInterval;

        public static MetricsConfig Default => new()
        {
            enableInProduction = false,
            profileLevel = ProfileLevel.Essential,
            adaptiveSamplingMultiplier = 1f,
            lowFpsThreshold = 30f,
            criticalFpsThreshold = 15f,
            memorySpikeMB = 25,
            batteryWarningLevel = 0.15f,
            maxEventsPerSecond = 3,
            maxTotalEventsPerMinute = 20,
            fpsCheckInterval = 1f,
            batteryCheckInterval = 20f,
            thermalCheckInterval = 15f,
            adaptiveSamplingUpdateInterval = 5f,
            spamCounterResetInterval = 1f
        };
    }

    public enum ProfileLevel : byte
    {
        Disabled = 0,
        Essential = 1, // Only critical metrics (memory warnings, crashes)
        Standard = 2, // + FPS, lifecycle events
        Detailed = 3, // + All available metrics
        Debug = 4 // + High-frequency sampling (dev only)
    }

    // Use structs to minimize GC pressure
    private struct MetricEvent
    {
        public readonly uint timestamp;
        public readonly byte eventType;
        public readonly float value;
        public readonly LogType logType;

        public MetricEvent(byte type,
                           float val,
                           LogType log)
        {
            timestamp = (uint) (Time.realtimeSinceStartup * 1000); // ms precision
            eventType = type;
            value = val;
            logType = log;
        }
    }

    // Event type constants to avoid string allocations
    private static class EventTypes
    {
        public const byte LowMemory = 1;
        public const byte FocusChanged = 2;
        public const byte PauseChanged = 3;
        public const byte DeepLink = 4;
        public const byte QualityChanged = 5;
        public const byte LowFPS = 6;
        public const byte CriticalFPS = 7;
        public const byte MemorySpike = 8;
        public const byte LowBattery = 9;
        public const byte ThermalThrottle = 10;
        public const byte GCSpike = 11;
        public const byte SystemStarted = 12;
        public const byte SceneLoaded = 13;
        public const byte SceneUnloaded = 14;
        public const byte ActiveSceneChanged = 15;
    }

    // Configuration
    private MetricsConfig config;
    private CancellationToken cancellationToken;
    private CancellationTokenSource internalCancellationSource;
    private CancellationTokenSource linkedCancellationSource;
    private ILogger logger;

    // Circular buffer for events (no allocations)
    private MetricEvent[] eventBuffer;
    private int bufferIndex = 0;
    private const int BUFFER_SIZE = 256;

    // Anti-spam with minimal overhead
    private readonly Dictionary<byte, uint> lastEventTime = new(16);
    private readonly Dictionary<byte, byte> eventCountThisSecond = new(16);
    private uint lastSecondReset = 0;
    private byte totalEventsThisMinute = 0;
    private uint lastMinuteReset = 0;

    // Performance tracking - zero allocation
    private int frameCount = 0;
    private float deltaTimeAccumulator = 0f;
    private float currentFPS = 60f;
    private bool isApplicationFocused = true;

    // Scene tracking
    private string currentSceneName = string.Empty;
    private int currentSceneBuildIndex = -1;
    private float sceneLoadStartTime = 0f;
    private readonly Dictionary<string, float> sceneLoadTimes = new();

    // Adaptive sampling based on device performance
    private float currentSamplingRate = 1f;
    private int consecutiveLowFPSFrames = 0;

    // String builders for zero-allocation logging
    private static readonly StringBuilder logBuilder = new(256);

    // State management
    private bool isInitialized = false;
    private readonly bool isDisposed = false;

    /// <summary>
    /// Initialize the performance metrics collector with configuration and cancellation token
    /// </summary>
    /// <param name="configuration">Metrics collection configuration</param>
    /// <param name="externalLogger">Logger instance for writing metrics (optional, uses Debug.Log if null)</param>
    /// <param name="cancelToken">Cancellation token for stopping all operations</param>
    public async UniTask<bool> Initialize(MetricsConfig configuration,
                                          ILogger externalLogger = null,
                                          CancellationToken cancelToken = default)
    {
        if (isInitialized)
        {
            LogMessage("[PerformanceMetrics] Already initialized", LogType.Warning);
            return false;
        }

        // Production safety check
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        if (!configuration.enableInProduction)
        {
            LogMessage("[PerformanceMetrics] Disabled in production build", LogType.Log);
            return false;
        }
#endif

        config = configuration;
        logger = externalLogger;

        // Setup cancellation tokens
        internalCancellationSource = new CancellationTokenSource();
        linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancelToken, internalCancellationSource.Token);
        cancellationToken = linkedCancellationSource.Token;

        try
        {
            // Initialize buffers and state
            InitializeBuffers();

            // Subscribe to Unity events
            SubscribeToUnityEvents();

            // Initialize adaptive sampling
            InitializeAdaptiveSampling();

            // Start all monitoring tasks
            await StartAllMonitoringTasks();

            isInitialized = true;
            LogEvent(EventTypes.SystemStarted, (float) config.profileLevel, LogType.Log);

            LogMessage($"[PerformanceMetrics] Initialized with profile level: {config.profileLevel}", LogType.Log);
            return true;
        }
        catch (Exception ex)
        {
            LogMessage($"[PerformanceMetrics] Initialization failed: {ex.Message}", LogType.Error);
            Dispose();
            return false;
        }
    }

    /// <summary>
    /// Get current performance metrics without allocations
    /// </summary>
    public void GetPerformanceSummary(out float fps,
                                      out float memoryMB,
                                      out float batteryPercent,
                                      out float samplingRate)
    {
        fps = currentFPS;
        memoryMB = GetMemoryUsageMB();
        batteryPercent = SystemInfo.batteryLevel * 100f;
        samplingRate = currentSamplingRate;
    }

    /// <summary>
    /// Get current scene information
    /// </summary>
    public void GetSceneInfo(out string sceneName,
                             out int buildIndex,
                             out float lastLoadTime)
    {
        sceneName = currentSceneName;
        buildIndex = currentSceneBuildIndex;

        // Get last load time for current scene
        sceneLoadTimes.TryGetValue(currentSceneName, out lastLoadTime);
    }

    /// <summary>
    /// Get scene load time statistics
    /// </summary>
    public Dictionary<string, float> GetSceneLoadTimes()
    {
        return new Dictionary<string, float>(sceneLoadTimes);
    }

    /// <summary>
    /// Change profile level at runtime
    /// </summary>
    public async UniTask SetProfileLevel(ProfileLevel newLevel)
    {
        if (config.profileLevel == newLevel)
        {
            return;
        }

        var oldLevel = config.profileLevel;
        config.profileLevel = newLevel;

        if (newLevel == ProfileLevel.Disabled)
        {
            await StopAllTasks();
        }
        else if (isInitialized)
        {
            // Restart monitoring with new level if needed
            if (ShouldRestartTasks(oldLevel, newLevel))
            {
                await StopAllTasks();
                await StartAllMonitoringTasks();
            }
        }

        LogEvent(EventTypes.QualityChanged, (float) newLevel, LogType.Log);
    }

    /// <summary>
    /// Force immediate memory snapshot
    /// </summary>
    public void ForceMemorySnapshot()
    {
        if (!isInitialized || isDisposed)
        {
            return;
        }

        var memoryMB = GetMemoryUsageMB();
        LogEvent(EventTypes.MemorySpike, memoryMB, LogType.Log);
    }

    /// <summary>
    /// Export metrics to external analytics system
    /// </summary>
    public void FlushToAnalytics()
    {
        if (!isInitialized || isDisposed)
        {
            return;
        }

        LogMessage($"[PerformanceMetrics] Flushing {bufferIndex} events to analytics", LogType.Log);
        // TODO: Implement integration with analytics SDK
    }

    private void InitializeBuffers()
    {
        eventBuffer = new MetricEvent[BUFFER_SIZE];
        bufferIndex = 0;

        // Clear dictionaries
        lastEventTime.Clear();
        eventCountThisSecond.Clear();

        // Reset counters
        totalEventsThisMinute = 0;
        lastSecondReset = 0;
        lastMinuteReset = 0;

        // Reset performance tracking
        frameCount = 0;
        deltaTimeAccumulator = 0f;
        currentFPS = 60f;
        consecutiveLowFPSFrames = 0;
        isApplicationFocused = true;

        // Initialize scene tracking
        var activeScene = SceneManager.GetActiveScene();
        currentSceneName = activeScene.name ?? "Unknown";
        currentSceneBuildIndex = activeScene.buildIndex;
        sceneLoadStartTime = 0f;
        sceneLoadTimes.Clear();
    }

    private void SubscribeToUnityEvents()
    {
        // Essential events (always subscribed)
        Application.lowMemory += OnLowMemory;
        Application.focusChanged += OnFocusChanged;

        // Scene management events (always subscribed for performance impact tracking)
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        if (config.profileLevel >= ProfileLevel.Standard)
        {
            //Application.focusChanged .pauseChanged += OnPauseChanged;
            QualitySettings.activeQualityLevelChanged += OnQualityLevelChanged;

#if UNITY_2021_2_OR_NEWER
            Application.memoryUsageChanged += OnMemoryUsageChanged;
#endif
        }

        if (config.profileLevel >= ProfileLevel.Detailed)
        {
            Application.deepLinkActivated += OnDeepLinkActivated;

#if UNITY_2022_2_OR_NEWER
            //Application.systemLanguageChanged += OnSystemLanguageChanged;
#endif
        }
    }

    private void InitializeAdaptiveSampling()
    {
        var deviceTier = GetDevicePerformanceTier();

        currentSamplingRate = deviceTier switch
        {
            1 => 0.5f, // Low-end device
            2 => 1.0f, // Mid-range device  
            3 => 1.5f, // High-end device
            _ => 1.0f // Unknown
        } * config.adaptiveSamplingMultiplier;
    }

    private bool ShouldRestartTasks(ProfileLevel oldLevel, ProfileLevel newLevel)
    {
        // Restart if crossing Standard threshold (when tasks change)
        return (oldLevel < ProfileLevel.Standard && newLevel >= ProfileLevel.Standard) ||
               (oldLevel >= ProfileLevel.Standard && newLevel < ProfileLevel.Standard) ||
               (oldLevel < ProfileLevel.Detailed && newLevel >= ProfileLevel.Detailed) ||
               (oldLevel >= ProfileLevel.Detailed && newLevel < ProfileLevel.Detailed);
    }

    private async UniTask StartAllMonitoringTasks()
    {
        var tasks = new List<UniTask>();

        // Always run spam counter reset
        tasks.Add(SpamCounterResetTask().SuppressCancellationThrow());

        if (config.profileLevel >= ProfileLevel.Standard)
        {
            // Core monitoring tasks
            tasks.Add(FPSMonitoringTask().SuppressCancellationThrow());
            tasks.Add(BatteryMonitoringTask().SuppressCancellationThrow());
            tasks.Add(AdaptiveSamplingTask().SuppressCancellationThrow());

            // Note: Memory monitoring is handled via Application.memoryUsageChanged event
            // No separate memory monitoring task needed
        }

        if (config.profileLevel >= ProfileLevel.Detailed)
        {
            // Platform-specific thermal monitoring
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            tasks.Add(ThermalMonitoringTask().SuppressCancellationThrow());
#endif
        }

        // Start all tasks without awaiting (fire and forget)
        foreach (var task in tasks)
        {
            task.Forget();
        }

        await UniTask.Yield(); // Ensure tasks started
        LogMessage($"[PerformanceMetrics] Started {tasks.Count} monitoring tasks", LogType.Log);
    }

    private async UniTask StopAllTasks()
    {
        if (internalCancellationSource != null && !internalCancellationSource.IsCancellationRequested)
        {
            internalCancellationSource.Cancel();
        }

        // Give tasks a moment to gracefully shutdown
        await UniTask.Delay(100);

        LogMessage("[PerformanceMetrics] All monitoring tasks stopped", LogType.Log);
    }

    private async UniTask SpamCounterResetTask()
    {
        var interval = TimeSpan.FromSeconds(config.spamCounterResetInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await UniTask.Delay(interval, cancellationToken: cancellationToken);

                var currentTime = (uint) (Time.realtimeSinceStartup * 1000);

                // Reset per-second counters
                if (currentTime - lastSecondReset > 1000)
                {
                    eventCountThisSecond.Clear();
                    lastSecondReset = currentTime;
                }

                // Reset per-minute counters
                if (currentTime - lastMinuteReset > 60000)
                {
                    totalEventsThisMinute = 0;
                    lastMinuteReset = currentTime;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogMessage($"[PerformanceMetrics] Spam counter reset error: {ex.Message}", LogType.Error);
                // Continue with normal interval timing
            }
        }
    }

    private async UniTask FPSMonitoringTask()
    {
        var interval = TimeSpan.FromSeconds(config.fpsCheckInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await UniTask.Delay(interval, cancellationToken: cancellationToken);

                if (isApplicationFocused)
                {
                    UpdateFPSMetrics();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogMessage($"[PerformanceMetrics] FPS monitoring error: {ex.Message}", LogType.Error);
                // Continue with normal interval timing
            }
        }
    }

    private async UniTask BatteryMonitoringTask()
    {
        var interval = TimeSpan.FromSeconds(config.batteryCheckInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await UniTask.Delay(interval, cancellationToken: cancellationToken);
                CheckBatteryLevel();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogMessage($"[PerformanceMetrics] Battery monitoring error: {ex.Message}", LogType.Error);
                // Continue with normal interval timing
            }
        }
    }

    private async UniTask AdaptiveSamplingTask()
    {
        var interval = TimeSpan.FromSeconds(config.adaptiveSamplingUpdateInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await UniTask.Delay(interval, cancellationToken: cancellationToken);
                UpdateAdaptiveSampling();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerformanceMetrics] Adaptive sampling error: {ex.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
            }
        }
    }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    private async UniTask ThermalMonitoringTask()
    {
        var interval = TimeSpan.FromSeconds(config.thermalCheckInterval);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await UniTask.Delay(interval, cancellationToken: cancellationToken);
                
                // Cross-platform thermal monitoring using performance heuristics
                // Note: Real thermal monitoring requires native plugins for both platforms
                CheckThermalStateHeuristic();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogMessage($"[PerformanceMetrics] Thermal monitoring error: {ex.Message}", LogType.Error);
                break; // Stop monitoring on error
            }
        }
    }
    
    /// <summary>
    /// Thermal state detection using performance and battery heuristics
    /// Real implementation should use native plugins:
    /// - iOS: ProcessInfo.processInfo.thermalState
    /// - Android: ThermalManager API (API 29+)
    /// </summary>
    private void CheckThermalStateHeuristic()
    {
        float batteryLevel = SystemInfo.batteryLevel;
        
        // Thermal throttling detection heuristic
        bool potentialThermalThrottling = false;
        
        #if UNITY_ANDROID
        // Android-specific heuristics
        if (batteryLevel < 0.3f && currentFPS < config.lowFpsThreshold)
        {
            potentialThermalThrottling = true;
        }
        #elif UNITY_IOS  
        // iOS-specific heuristics (more aggressive thermal management)
        if (batteryLevel < 0.3f && currentFPS < config.lowFpsThreshold * 0.8f)
        {
            potentialThermalThrottling = true;
        }
        #endif
        
        if (potentialThermalThrottling)
        {
            LogEvent(EventTypes.ThermalThrottle, currentFPS, LogType.Warning);
            
            if (config.profileLevel >= ProfileLevel.Debug)
            {
                LogMessage($"[PERF][THERMAL] Potential thermal throttling detected - " +
                         $"Battery: {batteryLevel * 100:F0}%, FPS: {currentFPS:F1}", LogType.Log);
            }
        }
    }
#endif

    private void OnLowMemory()
    {
        LogEvent(EventTypes.LowMemory, GetMemoryUsageMB(), LogType.Warning);

        // Reduce sampling rate during memory pressure
        currentSamplingRate = Mathf.Max(0.1f, currentSamplingRate * 0.5f);

        // Force GC if in standard+ profile
        if (config.profileLevel >= ProfileLevel.Standard)
        {
            GC.Collect(0, GCCollectionMode.Optimized);
            Resources.UnloadUnusedAssets();
        }
    }

    private void OnFocusChanged(bool hasFocus)
    {
        isApplicationFocused = hasFocus;
        LogEvent(
            EventTypes.FocusChanged, hasFocus
                                         ? 1f
                                         : 0f, LogType.Log);

        // Adjust sampling when app loses focus
        if (!hasFocus && config.profileLevel >= ProfileLevel.Standard)
        {
            currentSamplingRate *= 0.1f;
        }
        else if (hasFocus)
        {
            InitializeAdaptiveSampling(); // Restore normal sampling
        }
    }

    private void OnPauseChanged(bool pauseStatus)
    {
        LogEvent(
            EventTypes.PauseChanged, pauseStatus
                                         ? 1f
                                         : 0f, LogType.Log);
    }

    private void OnDeepLinkActivated(string url)
    {
        LogEvent(EventTypes.DeepLink, url?.Length ?? 0, LogType.Log);
    }

    private void OnQualityLevelChanged(int previousLevel, int currentLevel)
    {
        LogEvent(EventTypes.QualityChanged, currentLevel, LogType.Log);

        // Adapt sampling based on quality level
        var qualityMultiplier = currentLevel switch
        {
            0 => 0.5f, // Very Low
            1 => 0.7f, // Low
            2 => 1.0f, // Medium
            3 => 1.2f, // High
            _ => 1.5f // Very High+
        };

        currentSamplingRate = Mathf.Clamp(
            config.adaptiveSamplingMultiplier * qualityMultiplier, 0.1f, 2f);
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
        // Convert to MB for consistent logging
        var memoryUsageMB = 0f; //change.memoryUsage .memoryUsage / (1024f * 1024f);

        LogEvent(EventTypes.MemorySpike, (float) change.memoryUsage, LogType.Warning);

        if (config.profileLevel >= ProfileLevel.Debug)
        {
            LogMessage(
                $"[PERF][MEM_DETAIL] Memory usage changed to: {memoryUsageMB:F1}MB " +
                $"(Scene: {currentSceneName}, FPS: {currentFPS:F1})", LogType.Log);
        }

        // Force memory snapshot on significant memory events
        ForceMemorySnapshot();
    }
#endif

#if UNITY_2022_2_OR_NEWER
    private void OnSystemLanguageChanged(SystemLanguage language)
    {
        LogEvent(EventTypes.QualityChanged, (float) language, LogType.Log);
    }
#endif

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var loadTime = 0f;

        if (sceneLoadStartTime > 0f)
        {
            loadTime = Time.realtimeSinceStartup - sceneLoadStartTime;
            sceneLoadTimes[scene.name] = loadTime;
        }

        LogEvent(EventTypes.SceneLoaded, loadTime, LogType.Log);

        // Log scene load performance impact
        if (config.profileLevel >= ProfileLevel.Standard)
        {
            var memoryUsage = GetMemoryUsageMB();
            var loadMode = mode == LoadSceneMode.Additive
                               ? "Additive"
                               : "Single";

            logBuilder.Clear();
            logBuilder.Append($"Scene '{scene.name}' loaded ({loadMode}) - ");
            logBuilder.Append($"Time: {loadTime:F2}s, Memory: {memoryUsage:F1}MB, ");
            logBuilder.Append($"Objects: {scene.rootCount}, Valid: {scene.isLoaded}");

            LogMessage($"[PERF][SCENE] {logBuilder}", LogType.Log);

            // Force GC after scene load if memory usage is high
            if (memoryUsage > 100f) // 100MB threshold
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        sceneLoadStartTime = 0f; // Reset for next load
    }

    private void OnSceneUnloaded(Scene scene)
    {
        LogEvent(EventTypes.SceneUnloaded, scene.buildIndex, LogType.Log);

        if (config.profileLevel >= ProfileLevel.Standard)
        {
            var memoryUsage = GetMemoryUsageMB();

            logBuilder.Clear();
            logBuilder.Append($"Scene '{scene.name}' unloaded - ");
            logBuilder.Append($"Memory after unload: {memoryUsage:F1}MB");

            LogMessage($"[PERF][SCENE] {logBuilder}", LogType.Log);

            // Force resource cleanup after scene unload
            Resources.UnloadUnusedAssets();
        }
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        // Track scene load start time for performance measurement
        sceneLoadStartTime = Time.realtimeSinceStartup;

        var previousSceneName = currentSceneName;
        currentSceneName = newScene.name ?? "Unknown";
        currentSceneBuildIndex = newScene.buildIndex;

        LogEvent(EventTypes.ActiveSceneChanged, newScene.buildIndex, LogType.Log);

        if (config.profileLevel >= ProfileLevel.Standard)
        {
            var memoryUsage = GetMemoryUsageMB();

            logBuilder.Clear();
            logBuilder.Append($"Active scene changed: '{previousSceneName}' → '{currentSceneName}' - ");
            logBuilder.Append($"Memory: {memoryUsage:F1}MB, FPS: {currentFPS:F1}");

            LogMessage($"[PERF][SCENE] {logBuilder}", LogType.Log);
        }

        // Reset performance counters for new scene
        frameCount = 0;
        deltaTimeAccumulator = 0f;
        consecutiveLowFPSFrames = 0;

        // Adaptive sampling adjustment based on scene complexity
        AdjustSamplingForScene(newScene);
    }

    /// <summary>
    /// Adjust sampling rate based on scene complexity heuristics
    /// </summary>
    private void AdjustSamplingForScene(Scene scene)
    {
        if (config.profileLevel < ProfileLevel.Standard)
        {
            return;
        }

        try
        {
            // Heuristic: More root objects = potentially more complex scene
            var rootObjectCount = scene.rootCount;

            var sceneComplexityMultiplier = rootObjectCount switch
            {
                < 10 => 1.2f, // Simple scene - can sample more
                < 50 => 1.0f, // Normal scene
                < 100 => 0.8f, // Complex scene - sample less
                _ => 0.6f // Very complex scene
            };

            currentSamplingRate = Mathf.Clamp(
                currentSamplingRate * sceneComplexityMultiplier, 0.1f, 2f);

            if (config.profileLevel >= ProfileLevel.Debug)
            {
                LogMessage(
                    $"[PERF][SCENE] Sampling rate adjusted for scene '{scene.name}': " +
                    $"{currentSamplingRate:F2}x (objects: {rootObjectCount})", LogType.Log);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[PerformanceMetrics] Error adjusting sampling for scene: {ex.Message}", LogType.Error);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateFPSMetrics()
    {
        frameCount++;
        deltaTimeAccumulator += Time.unscaledDeltaTime;

        // Sample FPS at adaptive rate
        if (deltaTimeAccumulator >= 1f / currentSamplingRate)
        {
            var newFPS = frameCount / deltaTimeAccumulator;

            // Smooth FPS to avoid noise
            currentFPS = Mathf.Lerp(currentFPS, newFPS, 0.1f);

            CheckFPSThresholds(newFPS);

            frameCount = 0;
            deltaTimeAccumulator = 0f;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckFPSThresholds(float fps)
    {
        if (fps < config.criticalFpsThreshold)
        {
            consecutiveLowFPSFrames++;
            if (consecutiveLowFPSFrames == 1) // Only log once per streak
            {
                LogEvent(EventTypes.CriticalFPS, fps, LogType.Error);
            }
        }
        else if (fps < config.lowFpsThreshold)
        {
            consecutiveLowFPSFrames++;
            if (consecutiveLowFPSFrames == 1)
            {
                LogEvent(EventTypes.LowFPS, fps, LogType.Warning);
            }
        }
        else
        {
            consecutiveLowFPSFrames = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckMemorySpikes()
    {
        var currentMemoryMb = GetMemoryUsageMB();
        var memoryDelta = currentMemoryMb - lastMemoryMb;

        if (Math.Abs(memoryDelta) > config.memorySpikeMB)
        {
            LogEvent(EventTypes.MemorySpike, memoryDelta, LogType.Warning);
        }

        lastMemoryMb = currentMemoryMb;
    }

    private float lastMemoryMb;

    private void CheckBatteryLevel()
    {
        var batteryLevel = SystemInfo.batteryLevel;
        if (batteryLevel > 0 && batteryLevel < config.batteryWarningLevel)
        {
            LogEvent(EventTypes.LowBattery, batteryLevel * 100f, LogType.Warning);
        }
    }

    private void UpdateAdaptiveSampling()
    {
        // Reduce sampling rate if performance is poor
        if (currentFPS < config.lowFpsThreshold)
        {
            currentSamplingRate = Mathf.Max(0.1f, currentSamplingRate * 0.9f);
        }
        else if (currentFPS > config.lowFpsThreshold * 1.5f)
        {
            currentSamplingRate = Mathf.Min(2f, currentSamplingRate * 1.05f);
        }
    }

    /// <summary>
    /// Get memory usage with production build compatibility
    /// Development: Uses Profiler API for accurate Unity memory tracking
    /// Production: Uses GC.GetTotalMemory for managed memory only
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetMemoryUsageMB()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Use Profiler API in development builds for accurate memory tracking
        // Includes both managed and native Unity memory allocations
        return Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
#else
        // Use GC.GetTotalMemory in production builds (less accurate but available)
        // Only tracks managed memory, doesn't include native Unity allocations
        return System.GC.GetTotalMemory(false) / (1024f * 1024f);
#endif
    }

    private int GetDevicePerformanceTier()
    {
        var systemMemoryGB = SystemInfo.systemMemorySize / 1024;
        var processorCount = SystemInfo.processorCount;

        if (systemMemoryGB >= 6 && processorCount >= 6)
        {
            return 3; // High-end
        }

        if (systemMemoryGB >= 3 && processorCount >= 4)
        {
            return 2; // Mid-range
        }

        return 1; // Low-end
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogEvent(byte eventType,
                          float value,
                          LogType logType)
    {
        if (config.profileLevel == ProfileLevel.Disabled || ShouldThrottleEvent(eventType))
        {
            return;
        }

        // Add to circular buffer (no allocation)
        eventBuffer[bufferIndex] = new MetricEvent(eventType, value, logType);
        bufferIndex = (bufferIndex + 1) % BUFFER_SIZE;

        // Throttled console output
        if (config.profileLevel >= ProfileLevel.Debug || logType >= LogType.Warning)
        {
            WriteToConsole(eventType, value, logType);
        }

        // Update anti-spam counters
        UpdateEventCounters(eventType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldThrottleEvent(byte eventType)
    {
        // Check per-event-type throttling
        if (eventCountThisSecond.TryGetValue(eventType, out var count) &&
            count >= config.maxEventsPerSecond)
        {
            return true;
        }

        // Check total events throttling
        if (totalEventsThisMinute >= config.maxTotalEventsPerMinute)
        {
            return true;
        }

        return false;
    }

    private void WriteToConsole(byte eventType,
                                float value,
                                LogType logType)
    {
        logBuilder.Clear();
        logBuilder.Append("[PERF][");
        logBuilder.Append(GetEventTypeName(eventType));
        logBuilder.Append("] ");
        logBuilder.Append(value.ToString("F1"));

        var message = logBuilder.ToString();
        LogMessage(message, logType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogMessage(string message, LogType logType)
    {
        if (logger != null)
        {
            switch (logType)
            {
                case LogType.Error:
                    logger.LogError("PerformanceMetrics", message);
                    break;
                case LogType.Warning:
                    logger.LogWarning("PerformanceMetrics", message);
                    break;
                case LogType.Log:
                default:
                    logger.Log("PerformanceMetrics", message);
                    break;
            }
        }
    }

    private void UpdateEventCounters(byte eventType)
    {
        // Update per-second counter
        if (eventCountThisSecond.TryGetValue(eventType, out var count))
        {
            eventCountThisSecond[eventType] = (byte) (count + 1);
        }
        else
        {
            eventCountThisSecond[eventType] = 1;
        }

        // Update per-minute counter
        totalEventsThisMinute++;
    }

    private static string GetEventTypeName(byte eventType)
    {
        return eventType switch
        {
            EventTypes.LowMemory => "MEM",
            EventTypes.FocusChanged => "FOCUS",
            EventTypes.PauseChanged => "PAUSE",
            EventTypes.DeepLink => "LINK",
            EventTypes.QualityChanged => "QUAL",
            EventTypes.LowFPS => "FPS_LOW",
            EventTypes.CriticalFPS => "FPS_CRIT",
            EventTypes.MemorySpike => "MEM_SPIKE",
            EventTypes.LowBattery => "BAT_LOW",
            EventTypes.ThermalThrottle => "THERMAL",
            EventTypes.GCSpike => "GC",
            EventTypes.SystemStarted => "START",
            EventTypes.SceneLoaded => "SCENE_LOAD",
            EventTypes.SceneUnloaded => "SCENE_UNLOAD",
            EventTypes.ActiveSceneChanged => "SCENE_CHANGE",
            _ => "UNK"
        };
    }

    public void Dispose()
    {
        internalCancellationSource?.Dispose();
        linkedCancellationSource?.Dispose();
    }
}
}