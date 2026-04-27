using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheBestLogger;
using TheBestLogger.Examples;
using TheBestLogger.Examples.LogTargets;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GameLoggerSample : MonoBehaviour
{
    public event Action StateChanged;

    private static readonly string[] NativeCrashTestNames = typeof(TheBestLoggerSample.CrashReporting.NativeExceptionsiOS)
                                                            .GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                            .Where(method => method.Name.StartsWith("Trigger", StringComparison.Ordinal))
                                                            .Select(method => method.Name)
                                                            .OrderBy(name => name, StringComparer.Ordinal)
                                                            .ToArray();

    [SerializeField]
    private GameLoggerExampleBootstrapMode _bootstrapMode = GameLoggerExampleBootstrapMode.ResourcesDev;

    [SerializeField]
    private bool _useRuntimeConsoleTarget = true;

    [SerializeField]
    private bool _retrievePreviousSessionIssuesOnStart = true;

    [SerializeField]
    private bool _useOpenSearchMockTarget = true;

    public GameLoggerExampleBootstrapMode CurrentBootstrapMode => _bootstrapMode;
    public bool UseRuntimeConsoleTarget => _useRuntimeConsoleTarget;
    public bool RetrievePreviousSessionIssuesOnStart => _retrievePreviousSessionIssuesOnStart;
    public bool UseOpenSearchMockTarget => _useOpenSearchMockTarget;
    public bool SampleDebugModeEnabled { get; private set; }
    public string LastActionStatus { get; private set; } = "Ready";

    private readonly AggregateExceptionExample _aggregateExceptionExample = new();
    private readonly InnerExceptionExample _innerExceptionExample = new();
    private readonly ManagedException _managedExceptionExample = new();

    private void Awake()
    {
        InitializeLogger(_bootstrapMode,
                         _useRuntimeConsoleTarget,
                         _retrievePreviousSessionIssuesOnStart,
                         _useOpenSearchMockTarget);
        SampleDebugModeEnabled = LogManager.SetDebugMode(GameLoggerExampleBootstrap.DefaultDebugId, true);
        NotifyStateChanged();
    }

    private async void Start()
    {
        await Task.Delay(100);
        EmitInfoLog();
        EmitStructuredWarningLog();
    }

    private void OnApplicationQuit()
    {
        LogManager.Dispose();
    }

    public static void InitializeLogger()
    {
        InitializeLogger(GameLoggerExampleBootstrapMode.ResourcesDev,
                         useRuntimeConsoleTarget: true,
                         retrievePreviousSessionIssuesOnStart: true,
                         useOpenSearchMockTarget: true);
    }

    public static void InitializeLogger(GameLoggerExampleBootstrapMode bootstrapMode,
                                        bool useRuntimeConsoleTarget,
                                        bool retrievePreviousSessionIssuesOnStart,
                                        bool useOpenSearchMockTarget)
    {
        GameLoggerExampleBootstrap.Initialize(bootstrapMode,
                                             useRuntimeConsoleTarget,
                                             retrievePreviousSessionIssuesOnStart,
                                             useOpenSearchMockTarget);
    }

    public void EmitInfoLog()
    {
        Console.WriteLine("Sample info log emitted through Console.WriteLine.");
        GameLogger.GameLoading.LogInfo("Sample info log", new LogAttributes(LogImportance.Critical));
        LogManager.CreateLogger("UI", "SampleStudio").LogTrace("UI trace from sample panel");
        SetStatus("Info log emitted");
    }

    public void EmitStructuredWarningLog()
    {
        GameLogger.GameLoading.LogWarning(
            "Structured sample warning",
            new LogAttributes("sample_mode", _bootstrapMode.ToString())
                .Add("runtime_console", _useRuntimeConsoleTarget)
                .Add("debug_mode", SampleDebugModeEnabled));
        SetStatus("Structured warning emitted");
    }

    public void EmitErrorLog()
    {
        GameLogger.GameLoading.LogError("Sample error log");
        SetStatus("Error log emitted");
    }

    public void EmitHandledExceptionLog()
    {
        try
        {
            throw new ArgumentException("Sample handled exception");
        }
        catch (Exception ex)
        {
            GameLogger.Main.LogException(ex, new LogAttributes(LogImportance.Critical));
        }

        SetStatus("Handled exception emitted");
    }

    public void EmitUnityConsoleError()
    {
        Console.Error.WriteLine("Sample Console.Error log");
        Debug.LogError("Sample Unity console error");
        SetStatus("Unity console error emitted");
    }

    public void EmitBackgroundLog()
    {
        ThrowAsyncOnBackThread("Sample background exception");
        Task.Run(() => GameLogger.Main.LogInfo("Background worker info log"));
        SetStatus("Background log scheduled");
    }

    public void EmitUnobservedTaskException()
    {
        CreateFaultedTask("Sample unobserved task exception");
        SetStatus("Unobserved task scheduled");
    }

    public void EmitMainThreadUnobservedTaskException()
    {
        _ = Task.Factory.StartNew(
            () =>
            {
                throw new InvalidOperationException(
                    "This exception will be unobserved, running on the main thread! " + Thread.CurrentThread.ManagedThreadId);
            }, CancellationToken.None,
            TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

        GC.Collect();
        GC.WaitForPendingFinalizers();
        SetStatus("Main-thread unobserved task scheduled");
    }

    public void EmitObservedTaskException()
    {
        RunObservedTaskException();
        SetStatus("Observed task exception scheduled");
    }

    public void EmitAggregateHandledException()
    {
        _aggregateExceptionExample.ThrowHandledException();
        SetStatus("Aggregate handled exception emitted");
    }

    public void EmitInnerHandledException()
    {
        _innerExceptionExample.ThrowHandledException();
        SetStatus("Inner handled exception emitted");
    }

    public void EmitHandledAsyncVoidException()
    {
        _managedExceptionExample.ThrowHandledExceptionInAsyncVoid();
        SetStatus("Handled async void exception scheduled");
    }

    public void EmitHandledVoidException()
    {
        _managedExceptionExample.ThrowHandledExceptionInVoid();
        SetStatus("Handled void exception emitted");
    }

    public void EmitUnhandledAsyncVoidException()
    {
        SetStatus("Unhandled async void exception triggered");
        RunUnhandledAsyncVoidException();
    }

    public void EmitBurstLogs()
    {
        for (var index = 0; index < 20; index++)
        {
            GameLogger.GameLoading.LogInfo($"Burst log #{index + 1}");
        }

        SetStatus("Burst logs emitted");
    }

    public void ApplyQaRuntimePatch()
    {
        ApplyGlobalMinLevel(LogLevel.Debug);
        SetStatus("QA runtime patch applied");
    }

    public void ApplyProductionRuntimePatch()
    {
        ApplyGlobalMinLevel(LogLevel.Warning);
        SetStatus("Production runtime patch applied");
    }

    public void ToggleSampleDebugMode()
    {
        var nextState = !SampleDebugModeEnabled;
        var changed = LogManager.SetDebugMode(GameLoggerExampleBootstrap.DefaultDebugId, nextState);
        SampleDebugModeEnabled = changed && nextState;

        SetStatus(changed
            ? $"Debug mode set to {nextState}"
            : $"Debug mode toggle had no matching target for id {GameLoggerExampleBootstrap.DefaultDebugId}");
    }

    public void LogCurrentConfigurationSummary()
    {
        Dictionary<string, LogTargetConfiguration> configurations = LogManager.GetCurrentLogTargetConfigurations();
        foreach (KeyValuePair<string, LogTargetConfiguration> pair in configurations)
        {
            Debug.Log($"{pair.Key}: MinLevel={pair.Value.MinLogLevel}, Muted={pair.Value.Muted}, ThreadSafe={pair.Value.IsThreadSafe}");
        }

        SetStatus($"Logged {configurations.Count} target configurations");
    }

    public void RetrievePreviousSessionIssues()
    {
        StabilityHub.StabilityHubService.RetrieveAndLogPreviousSessionIssues();
        SetStatus("Requested previous session issues");
    }

    public void ReinitializeLogger()
    {
        ReinitializeLoggerInternal("Logger reinitialized");
    }

    public void UseResourcesBootstrap()
    {
        _bootstrapMode = GameLoggerExampleBootstrapMode.ResourcesDev;
        ReinitializeLoggerInternal("Switched bootstrap to ResourcesDev");
    }

    public void UseScriptedProductionBootstrap()
    {
        _bootstrapMode = GameLoggerExampleBootstrapMode.ScriptedProductionPreset;
        ReinitializeLoggerInternal("Switched bootstrap to ScriptedProductionPreset");
    }

    public void UseScriptedQaBootstrap()
    {
        _bootstrapMode = GameLoggerExampleBootstrapMode.ScriptedQaPreset;
        ReinitializeLoggerInternal("Switched bootstrap to ScriptedQaPreset");
    }

    public void ToggleRuntimeConsoleTarget()
    {
        _useRuntimeConsoleTarget = !_useRuntimeConsoleTarget;
        ReinitializeLoggerInternal($"Runtime console target set to {_useRuntimeConsoleTarget}");
    }

    public void TogglePreviousSessionIssueRetrievalOnStart()
    {
        _retrievePreviousSessionIssuesOnStart = !_retrievePreviousSessionIssuesOnStart;
        ReinitializeLoggerInternal($"Retrieve previous issues on start set to {_retrievePreviousSessionIssuesOnStart}");
    }

    public void ToggleOpenSearchMockTarget()
    {
        _useOpenSearchMockTarget = !_useOpenSearchMockTarget;
        ReinitializeLoggerInternal($"OpenSearch mock target set to {_useOpenSearchMockTarget}");
    }

    public void EmitOpenSearchMockLog()
    {
        if (!TryRequireOpenSearchMockTarget())
        {
            return;
        }

        GameLogger.Main.LogInfo(
            "Sample OpenSearch mock log",
            new LogAttributes("transport", "mock-opensearch")
                .Add("bootstrap_mode", _bootstrapMode.ToString())
                .Add("runtime_console", _useRuntimeConsoleTarget)
                .Add("debug_mode", SampleDebugModeEnabled));
        SetStatus("Mock OpenSearch log emitted");
    }

    public void ApplyOpenSearchMockQaPatch()
    {
        if (!TryRequireOpenSearchMockTarget())
        {
            return;
        }

        LogManager.UpdateLogTargetConfiguration(nameof(OpenSearchLogTargetConfiguration),
                                                CreateOpenSearchMockConfiguration(LogLevel.Debug,
                                                                                  "mock://qa-opensearch",
                                                                                  "thebestlogger-qa-",
                                                                                  "qa-demo-key"));
        SetStatus("OpenSearch mock QA patch applied");
    }

    public void ApplyOpenSearchMockProductionPatch()
    {
        if (!TryRequireOpenSearchMockTarget())
        {
            return;
        }

        LogManager.UpdateLogTargetConfiguration(nameof(OpenSearchLogTargetConfiguration),
                                                CreateOpenSearchMockConfiguration(LogLevel.Warning,
                                                                                  "mock://prod-opensearch",
                                                                                  "thebestlogger-prod-",
                                                                                  "prod-demo-key"));
        SetStatus("OpenSearch mock production patch applied");
    }

    public void ClearOpenSearchMockCapture()
    {
        MockOpenSearchLogTarget.ClearCapturedPayloads();
        SetStatus("Cleared OpenSearch mock captured payloads");
    }

    public string GetBootstrapSummary()
    {
        return $"Mode: {_bootstrapMode}\n" +
               $"Config source: {GetBootstrapSourceLabel()}\n" +
               $"Runtime console target: {FormatEnabled(_useRuntimeConsoleTarget)}\n" +
               $"Previous session retrieval on start: {FormatEnabled(_retrievePreviousSessionIssuesOnStart)}\n" +
               $"OpenSearch mock target: {FormatEnabled(_useOpenSearchMockTarget)}";
    }

    public string GetRuntimePatchSummary()
    {
        Dictionary<string, LogTargetConfiguration> configurations = LogManager.GetCurrentLogTargetConfigurations();
        if (configurations.Count == 0)
        {
            return "No active log target configurations.";
        }

        var builder = new StringBuilder(256);
        builder.AppendLine("Active targets:");
        foreach (KeyValuePair<string, LogTargetConfiguration> pair in configurations)
        {
            builder.Append("- ");
            builder.Append(pair.Key);
            builder.Append(": MinLevel=");
            builder.Append(pair.Value.MinLogLevel);
            builder.Append(", Muted=");
            builder.Append(pair.Value.Muted);
            builder.Append(", ThreadSafe=");
            builder.Append(pair.Value.IsThreadSafe);
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    public string GetLiveOverviewSummary()
    {
        Dictionary<string, LogTargetConfiguration> configurations = LogManager.GetCurrentLogTargetConfigurations();
        return $"Preset: {_bootstrapMode}\n" +
               $"Source: {GetBootstrapSourceLabel()}\n" +
               $"Targets: {configurations.Count}  |  Runtime console: {FormatEnabled(_useRuntimeConsoleTarget)}\n" +
               $"Debug mode: {FormatEnabled(SampleDebugModeEnabled)}  |  OpenSearch: {FormatEnabled(_useOpenSearchMockTarget)}";
    }

    public string GetDebugModeSummary()
    {
        return $"Debug id: {GameLoggerExampleBootstrap.DefaultDebugId}\n" +
               $"Sample debug mode: {FormatEnabled(SampleDebugModeEnabled)}";
    }

    public string GetStabilityHubSummary()
    {
        return $"Monitoring source: {GetBootstrapSourceLabel()}\n" +
               $"Retrieve previous issues on start: {FormatEnabled(_retrievePreviousSessionIssuesOnStart)}\n" +
               "Manual retrieval available: Enabled";
    }

    public string GetOpenSearchMockSummary()
    {
        if (!_useOpenSearchMockTarget)
        {
            return "OpenSearch mock target is disabled. Enable it in Bootstrap and Routing, then reinitialize.";
        }

        var builder = new StringBuilder(256);
        if (TryGetTargetConfiguration(nameof(OpenSearchLogTargetConfiguration), out OpenSearchLogTargetConfiguration configuration))
        {
            builder.AppendLine($"Min level: {configuration.MinLogLevel}");
            builder.AppendLine($"Endpoint: {configuration.OpenSearchHostUrl}{configuration.OpenSearchSingleLogMethod}");
            builder.AppendLine($"Index prefix: {configuration.IndexPrefix}");
        }
        else
        {
            builder.AppendLine("OpenSearch mock target configuration is not active.");
        }

        builder.Append(MockOpenSearchLogTarget.GetSummary());
        return builder.ToString().TrimEnd();
    }

    public string GetOpenSearchMockPayloadPreview()
    {
        return MockOpenSearchLogTarget.GetRecentPayloadsPreview();
    }

    public string GetCrashLabSummary()
    {
        var nativeCrashTestCount = NativeCrashTestNames.Length;
        return $"Managed exception actions: 9\n" +
               $"Native iOS crash actions: {nativeCrashTestCount}\n" +
               $"Native trigger availability: {(SupportsNativeCrashTests() ? "iOS runtime only" : "Unavailable on current platform")}\n" +
               "Warning: native crash actions intentionally terminate the app process.";
    }

    public string[] GetNativeCrashTestNames()
    {
        return NativeCrashTestNames;
    }

    public void InvokeNativeCrashTest(string methodName)
    {
        if (!SupportsNativeCrashTests())
        {
            SetStatus("Native crash tests are available only in iOS player builds.");
            return;
        }

        if (string.IsNullOrEmpty(methodName))
        {
            SetStatus("Native crash test name is empty.");
            return;
        }

        MethodInfo method = typeof(TheBestLoggerSample.CrashReporting.NativeExceptionsiOS)
                            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            SetStatus($"Native crash test method was not found: {methodName}");
            return;
        }

        SetStatus($"Triggering native crash test: {methodName}");
        method.Invoke(null, null);
    }

    private static void CreateFaultedTask(string message)
    {
        Task.Run(() => { throw new InvalidOperationException(message); });
    }

    private static async void RunObservedTaskException()
    {
        try
        {
            await Task.Run(() => { throw new InvalidOperationException("This exception is observed using await!"); });
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static async void RunUnhandledAsyncVoidException()
    {
        await Task.Delay(100);
        throw new InvalidOperationException("This exception is unhandled in async void.");
    }

    private void ApplyGlobalMinLevel(LogLevel minLevel)
    {
        Dictionary<string, LogTargetConfiguration> configurations = LogManager.GetCurrentLogTargetConfigurations();
        foreach (KeyValuePair<string, LogTargetConfiguration> pair in configurations)
        {
            pair.Value.MinLogLevel = minLevel;
        }

        LogManager.UpdateLogTargetsConfigurations(configurations);
    }

    private void ReinitializeLoggerInternal(string status)
    {
        LogManager.Dispose();
        MockOpenSearchLogTarget.ClearCapturedPayloads();
        InitializeLogger(_bootstrapMode,
                         _useRuntimeConsoleTarget,
                         _retrievePreviousSessionIssuesOnStart,
                         _useOpenSearchMockTarget);
        SampleDebugModeEnabled = LogManager.SetDebugMode(GameLoggerExampleBootstrap.DefaultDebugId, true);
        SetStatus(status);
    }

    private void ThrowAsyncOnBackThread(string message)
    {
        Task.Run(() => ThrowHandledAsync(message));
    }

    private void ThrowHandledAsync(string message)
    {
        try
        {
            Debug.Log($"Log about next ThrowAsync() throwing exception log: {message}");
            throw new Exception($"ThrowAsync() Exception: {message}");
        }
        catch (Exception ex)
        {
            GameLogger.Main.LogException(ex);
        }
    }

    private bool TryRequireOpenSearchMockTarget()
    {
        if (!_useOpenSearchMockTarget)
        {
            SetStatus("OpenSearch mock target is disabled. Enable it in Bootstrap and Routing, then reinitialize.");
            return false;
        }

        if (TryGetTargetConfiguration(nameof(OpenSearchLogTargetConfiguration), out OpenSearchLogTargetConfiguration _))
        {
            return true;
        }

        SetStatus("OpenSearch mock target configuration is not active after initialization.");
        return false;
    }

    private static OpenSearchLogTargetConfiguration CreateOpenSearchMockConfiguration(LogLevel minLogLevel,
                                                                                      string hostUrl,
                                                                                      string indexPrefix,
                                                                                      string apiKey)
    {
        return new OpenSearchLogTargetConfiguration
        {
            MinLogLevel = minLogLevel,
            IsThreadSafe = true,
            OpenSearchHostUrl = hostUrl,
            OpenSearchSingleLogMethod = "/logs",
            IndexPrefix = indexPrefix,
            ApiKey = apiKey,
            DebugMode = new DebugModeConfiguration(),
            BatchLogs = new LogTargetBatchLogsConfiguration(),
            DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
        };
    }

    private static bool TryGetTargetConfiguration<TConfiguration>(string configurationName, out TConfiguration configuration)
        where TConfiguration : LogTargetConfiguration
    {
        Dictionary<string, LogTargetConfiguration> configurations = LogManager.GetCurrentLogTargetConfigurations();
        if (configurations.TryGetValue(configurationName, out LogTargetConfiguration currentConfiguration) &&
            currentConfiguration is TConfiguration typedConfiguration)
        {
            configuration = typedConfiguration;
            return true;
        }

        configuration = null;
        return false;
    }

    private string GetBootstrapSourceLabel()
    {
        return _bootstrapMode == GameLoggerExampleBootstrapMode.ResourcesDev
                   ? "Resources asset + scripted sample targets"
                   : "Scripted preset";
    }

    private static string FormatEnabled(bool value)
    {
        return value ? "Enabled" : "Disabled";
    }

    private static bool SupportsNativeCrashTests()
    {
        return Application.platform == RuntimePlatform.IPhonePlayer;
    }

    private void SetStatus(string status)
    {
        LastActionStatus = $"[{DateTime.Now:HH:mm:ss}] {status}";
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
