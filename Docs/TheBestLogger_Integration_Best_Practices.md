# TheBestLogger Integration Best Practices

This guide is for teams integrating `com.foxsterdev.thebestlogger` into a real Unity project and trying to keep three things balanced:

- correct runtime logging
- low main-thread overhead
- safe production behavior under exceptions, threading, batching, and remote delivery

Core production rule:

- the logger should observe failures, format them, and route them to healthy targets
- the logger should not become a new source of runtime exceptions in gameplay, startup, shutdown, or crash-reporting paths

Use the package README for the package surface and release info:

- [TheBestLogger package README](../DevAccelerationSystem/Assets/TheBestLogger/README.md)

## Integration Profiles

### Simple Integration

Use this profile when you need a fast, maintainable integration with low operational complexity.

Recommended shape:

- initialize `LogManager` once from the Unity main thread
- keep the target set small
- keep categories stable and human-readable
- keep stack traces enabled mainly for `Error` and `Exception`
- avoid enabling every optional log source unless you actually need it
- keep `OpenSearch` or other remote delivery out of the first rollout unless you already operate that pipeline

Typical target set:

- `UnityEditorConsoleLogTarget` for editor visibility
- one platform-native target for build visibility:
  - `AndroidSystemLogTarget`
  - `AppleSystemLogTarget`

Typical bootstrap shape:

```csharp
using System.Collections.Generic;
using System.Threading;
using TheBestLogger;
using UnityEngine;

public static class LoggerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Initialize()
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

        var disposingToken = CancellationToken.None;
#if UNITY_2022_3_OR_NEWER
        disposingToken = Application.exitCancellationToken;
#endif

        LogManager.Initialize(logTargets.AsReadOnly(), "GameLogger/Dev/", disposingToken);
    }
}
```

Note:

- in non-Android and non-Apple runtime builds, replace the platform branch with your own runtime-capable target so the target list is never empty at initialization time

Why this profile is safe:

- `LogManager.Initialize(...)` is designed to run on the Unity main thread
- the package resolves `LogManagerConfiguration` from `Resources`
- category loggers are cached by `LogManager.CreateLogger(...)`
- default stack-trace policy already favors `Error` and `Exception`

### Scripted Integration Without Resources Paths

Use this profile when you want the bootstrap to stay fully in code and avoid implicit `Resources.Load(...)` dependencies, while still using the package's real public configuration types.

Typical bootstrap shape:

```csharp
using System.Threading;
using StabilityHub;
using StabilityHub.Monitoring;
using TheBestLogger;
using TheBestLogger.Examples;
using UnityEngine;

public static class LoggerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Initialize()
    {
        var logTargets = new LogTarget[]
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

        var appleConfigurationSo = ScriptableObject.CreateInstance<AppleSystemLogTargetConfigurationSO>();
        appleConfigurationSo.SpecificConfiguration = new AppleSystemLogTargetConfiguration
        {
            MinLogLevel = LogLevel.Warning,
            IsThreadSafe = true,
            DebugMode = new DebugModeConfiguration(),
            BatchLogs = new LogTargetBatchLogsConfiguration(),
            DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
        };

        var configuration = LogManagerConfigurationPresets.CreateProduction(appleConfigurationSo);
#if UNITY_ANDROID
        configuration.DebugUnityLoggerFilterLogType = LogType.Warning;
#elif UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
        appleConfigurationSo.SpecificConfiguration.MinLogLevel = LogLevel.Error;
#endif

        var disposingToken = CancellationToken.None;
#if UNITY_2022_3_OR_NEWER
        disposingToken = Application.exitCancellationToken;
#endif

        LogManager.Initialize(logTargets, configuration, disposingToken);
        StabilityHubService.Initialize(LogManager.CreateLogger("Stability"), MonitoringConfigurationPresets.CreateProduction());
    }
}
```

Why this profile is useful:

- you can start from `Production` or `Qa` presets
- you can apply point overrides directly on the real config objects before initialization
- bootstrap does not depend on `Resources` folder layout
- the old `ScriptableObject` path still remains available for asset-driven projects

If you want a consumer-facing reference implementation, the demo scene in `DevAccelerationSystem.DemoProject/Assets/TheBestLoggerSample/Scenes/LoggerSampleScene.unity` now exposes this profile through a runtime tabbed UI. It lets you switch bootstrap modes, emit representative logs, patch target configs live, exercise `StabilityHub`, and inspect a safe mock `OpenSearch` flow without wiring a real remote endpoint.

### Pro Integration

Use this profile when the project already has:

- background-thread logging
- remote log ingestion
- environment-specific configuration
- crash or previous-session recovery flows
- strict performance and validation requirements

Recommended shape:

- separate local visibility targets from remote delivery targets
- enable batching for expensive or remote sinks
- use main-thread dispatch only for targets that are not thread-safe
- use `DebugModeConfiguration` for scoped diagnostic amplification instead of permanently verbose logs
- keep consumer validation and device/runtime proof as separate release evidence
- treat `OpenSearch` and `StabilityHub` as production-sensitive surfaces

Typical target mix:

- `UnityEditorConsoleLogTarget` for editor/dev diagnostics
- platform-native target for device-local observability
- remote target such as `OpenSearchLogTarget`
- optional runtime viewer or custom guarded third-party target

## Configuration Rules

### 1. Bootstrap Rules

- Call `LogManager.Initialize(...)` once.
- Call it from the Unity main thread.
- Pass a non-empty target list.
- Point `resourceSubFolderThatContainsConfigs` to the `Resources` subfolder that contains `LogManagerConfiguration`.
- Use `Application.exitCancellationToken` on supported Unity versions so logger disposal is wired into shutdown.
- Do not call `LogManager.CreateLogger(...)` before initialization.
- Do not create loggers from constructors, static field initializers, or other early bootstrap paths that can run before logger initialization.
- If a startup system needs a logger, resolve it lazily after initialization or inject it after the logger bootstrap step.

Common bad pattern:

```csharp
public sealed class SomeStartupStep
{
    private readonly ILogger _logger = LogManager.CreateLogger("Startup");
}
```

Safer pattern:

```csharp
public sealed class SomeStartupStep
{
    private ILogger _logger;

    private ILogger Logger => _logger ??= LogManager.CreateLogger("Startup");
}
```

### 2. Category Rules

- Use stable domain categories such as `Gameplay`, `Networking`, `Economy`, `UI`, `Stability`.
- Use subcategories only when they help with filtering or ownership.
- Do not create ad-hoc category names from dynamic data.

Why:

- categories drive filtering and override rules in `LogTargetConfiguration`
- loggers are cached by category and subcategory key

### 3. Logger API Usage Rules

- Prefer direct `ILogger` methods such as `LogError(...)`, `LogWarning(...)`, `LogInfo(...)`, `LogDebug(...)`, and `LogException(...)`.
- If a call site already has a variable `LogLevel`, prefer `ILogger.LogFormat(logLevel, message, attributes)` over a project-local wrapper that re-dispatches the level manually.
- Do not add convenience wrappers that collapse multiple levels into fewer outputs or silently remap `Info`, `Exception`, or custom flows to `Debug`.
- Keep the call site explicit enough that a reviewer can see the intended severity without opening another helper.
- If a call site already has a real `Exception`, pass the real `Exception` object into the logger.
- Prefer `logger.LogException(exception)` or `logger.LogError("context message", exception)` over logging only `exception.Message` or only `exception.StackTrace`.
- Do not downgrade a structured exception into a plain string too early, because that loses type, inner-exception chain, and tooling support in targets.
- Do not unwrap `AggregateException` to only one `InnerException` unless that is a deliberate product decision and the lost context is acceptable.

Bad pattern:

```csharp
private static void Log(ILogger logger, string message, LogLevel logLevel, LogAttributes attributes)
{
    if (logger == null || string.IsNullOrEmpty(message))
    {
        return;
    }

    switch (logLevel)
    {
        case LogLevel.Error:
            logger.LogError(message, attributes);
            break;
        case LogLevel.Warning:
            logger.LogWarning(message, attributes);
            break;
        default:
            logger.LogDebug(message, attributes);
            break;
    }
}
```

Why this is bad:

- it hides the real logger API behind a local adapter
- it silently remaps `Info` to `Debug`
- it makes `Exception` handling unclear
- it makes audit and grep of real severity usage harder

Preferred patterns:

```csharp
logger.LogWarning("config is missing", attributes);
logger.LogError("startup failed", attributes);
logger.LogInfo("bootstrap completed", attributes);
```

```csharp
logger.LogFormat(logLevel, message, attributes);
```

### 3a. Exception Handling Rules

Use these rules for app code around the logger and for custom integrations that extend the package.

- Treat logger delivery paths as `never-throw` in production runtime.
- A broken log target should fail in isolation and should not take down gameplay, app startup, app shutdown, or other healthy targets.
- A custom target, custom log source, or async helper that catches an exception should log the original exception object when possible, not only its string fields.
- If a logger callback, scheduler callback, or third-party SDK callback can throw, catch that exception inside the logging path and degrade to fallback logging instead of letting it escape into the app flow.
- If a target becomes unhealthy at runtime, mute or disable that target and continue routing logs to the remaining healthy targets.
- Keep exception logging explicit in code review. A reviewer should be able to see where the original exception object is preserved and where failure isolation is owned.

Bad pattern:

```csharp
catch (Exception ex)
{
    logger.LogError($"Request failed: {ex.Message}");
}
```

Why this is bad:

- it loses the exception type
- it loses the inner-exception chain
- it weakens downstream formatting and target-specific exception handling

Preferred pattern:

```csharp
catch (Exception ex)
{
    logger.LogError("Request failed.", ex);
}
```

If the app must rethrow after logging:

- use `throw;` when rethrowing the same caught exception from the same `catch`
- use `ExceptionDispatchInfo.Capture(exception).Throw();` when rethrowing later or rethrowing an extracted inner exception
- do not use `throw ex;`
- do not use `throw ex.InnerException;`

If your integration uses reflection invoke:

- prefer the `Invoke(...)` overload that allows `BindingFlags.DoNotWrapExceptions` when that is available in your target runtime
- otherwise assume reflection may wrap the real failure in `TargetInvocationException` and preserve the original exception object when logging or rethrowing

For fire-and-forget task usage inside integration code:

- prefer package-owned safe helpers such as `FireAndForget()` or `FireAndLogWhenExceptions(...)` over raw `async void`
- if you add your own task helpers, keep their logging path `never-throw`
- cancellation should stay a normal non-crash path and should not be promoted into a fatal logger failure

### 4. Log Target Configuration Rules

Every target inherits a common configuration surface:

- `Muted`
- `MinLogLevel`
- `OverrideCategories`
- `BatchLogs`
- `DebugMode`
- `StackTraces`
- `IsThreadSafe`
- `DispatchingLogsToMainThread`

Recommended defaults:

- keep `MinLogLevel` at `Warning` or higher for expensive remote sinks
- keep stack traces enabled mainly for `Error` and `Exception`
- use `OverrideCategories` for hot categories that need lower or higher verbosity
- mark thread-safe targets honestly
- do not enable `DispatchingLogsToMainThread` for thread-safe targets unless you have a specific reason

Why the last rule matters:

- the logger already warns when a target is marked `IsThreadSafe` and main-thread dispatch is still enabled
- dispatching adds queueing and main-thread work, so it should be intentional

### 5. Batching and Main-Thread Dispatch

Use `BatchLogs` when:

- the target is remote
- writes are expensive
- bursty logging would otherwise create too many individual operations

Use `DispatchingLogsToMainThread` when:

- the target must execute on the Unity main thread
- background-thread logs must still reach that target safely

Recommended combinations:

- thread-safe remote sink:
  - `BatchLogs.Enabled = true`
  - `DispatchingLogsToMainThread.Enabled = false`
- non-thread-safe Unity-facing sink:
  - `DispatchingLogsToMainThread.Enabled = true`
  - batching optional depending on how expensive sink calls are

Important nuance:

- `Critical` log importance forces immediate drain and send
- regular and `NiceToHave` logs can be batched

### 6. Debug Mode Strategy

`DebugModeConfiguration` supports:

- `Enabled`
- `MinLogLevel`
- `IDs`
- per-category overrides

Recommended use:

- keep production defaults conservative
- enable high-verbosity debug mode only for approved device or player identifiers
- use `SetDebugMode(...)` to amplify diagnostics without turning the whole fleet noisy

Important nuance:

- initialization already tries to enable debug mode for the provided `debugId`, or falls back to `SystemInfo.deviceUniqueIdentifier`

## Target-Specific Guidance

### UnityEditorConsoleLogTarget

Good for:

- local development
- editor diagnosis
- surfacing category-prefixed messages and exceptions into the standard Unity Console

Do not treat it as:

- a production sink
- a substitute for runtime or device observability

### AndroidSystemLogTarget

Good for:

- native Android Logcat visibility
- device-side diagnosis when Unity Console is not enough

Best practices:

- keep payloads concise
- do not rely on this target alone for long-term retention
- still validate on physical Android devices before making platform confidence claims

### AppleSystemLogTarget

Good for:

- Apple unified logging visibility on supported Apple targets

Best practices:

- pass stable subsystem and main category values
- still validate on physical Apple runtime before making production claims about native observability

### OpenSearchLogTarget

Treat this example target as production-sensitive if you use it in a real backend pipeline.

Current config surface:

- `OpenSearchHostUrl`
- `OpenSearchSingleLogMethod`
- `IndexPrefix`
- `ApiKey`

Useful current behavior:

- config merge only overwrites those fields when the incoming value is non-empty
- this is compatible with partial remote-config refresh patterns
- DTO payload includes platform, OS, device model, UUID, log level, category, message, stack trace, timestamp, tags, attributes, and debug mode state

Operational cautions:

- timeout policy is not yet part of the public config surface
- remote delivery success and failure behavior should be validated per environment
- use batching and conservative verbosity for remote sinks

### SafeThirdPartyLogTarget

Use this base class when a third-party SDK can fail at runtime and should not be allowed to take the logger down with it.

Best practices:

- prefer guarded integration over direct unsafe target calls
- isolate third-party initialization failures
- if a third-party sink throws at runtime, mute or disable that sink and keep the rest of the logger alive
- treat fault isolation as part of release validation, not just implementation detail

### FileBackgroundAsyncWriter

Use it as a utility for background file writes, not as a reason to ignore shutdown behavior.

Best practices:

- keep disposal asynchronous internally
- avoid sync-waiting on teardown paths that can capture Unity `SynchronizationContext`
- validate scene changes and shutdown while logs are still in flight

## Log Source Recommendations

The package can intake logs from several sources, including:

- `UnityDebugLogSource`
- `UnityApplicationLogSource`
- `UnityApplicationLogSourceThreaded`
- unobserved `Task` exceptions
- unobserved `UniTask` exceptions
- current-domain unhandled exceptions
- optional `System.Diagnostics` sources

Recommended approach:

- start with the smallest source set that gives you the evidence you need
- decide explicitly whether you want threaded application log capture
- enable extra exception-oriented sources in production when you need crash-path visibility
- validate source behavior with runtime tests, not only editor assumptions
- for unobserved task paths, prefer preserving the full exception object or flattened aggregate instead of collapsing the signal to one inner exception

## StabilityHub Guidance

Use `StabilityHub` when previous-session crash or stability recovery signals matter to the product.

Best practices:

- initialize `StabilityHubService` after logger bootstrap
- treat missing monitoring config as a valid disabled state
- keep stability logging on its own category so it is easy to filter and route

Example package-owned flow exists in:

- `Runtime/Examples/GameLoggerEntryPoint.cs`

## Performance Guidance

If the goal is low allocations and low impact on Unity main-thread smoothness:

- initialize once and early
- keep per-frame hot-path logs rare
- avoid stack traces on `Info` and `Debug` unless actively diagnosing
- use batching for remote sinks
- avoid unnecessary main-thread dispatch
- do not turn on broad debug mode for the whole population
- validate editor performance and playmode performance separately
- keep consumer validation and device proof in the release process

Recommended validation stack:

1. editor tests for deterministic logic and config behavior
2. playmode tests for runtime dispatch, batching, and frame behavior
3. consumer validation in a real integration workspace
4. physical device proof for native-platform confidence and device-facing perf claims

## Simple Rollout Checklist

- logger initializes from main thread
- at least one target is configured and resolved
- categories are stable
- stack traces are mostly limited to `Error` and `Exception`
- no obviously expensive hot-path logging
- logs are visible in editor or on target device

## Pro Rollout Checklist

- target mix is intentional
- thread-safety flags are reviewed
- batch and dispatch settings are reviewed per target
- debug mode IDs are configured intentionally
- remote target config can be updated safely
- consumer validation is green
- performance evidence exists
- native/device proof exists for platform-native targets

## What Another AI Should Check

If you ask another AI to review a project that already integrated `TheBestLogger`, make sure it checks these bootstrap-specific risks explicitly:

- `LogManager.Initialize(...)` order relative to the real app bootstrap
- any `LogManager.CreateLogger(...)` call that can run before initialization
- loggers created from constructors
- loggers created from static fields or static properties
- bootstrap steps that log during their own construction
- fallback-logger warnings that indicate early logger access instead of a properly initialized runtime
- whether logger creation is lazy or injected after bootstrap
