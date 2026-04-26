# TheBestLogger Integration Best Practices

This guide is for teams integrating `com.foxsterdev.thebestlogger` into a real Unity project and trying to keep three things balanced:

- correct runtime logging
- low main-thread overhead
- safe production behavior under exceptions, threading, batching, and remote delivery

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

### 2. Category Rules

- Use stable domain categories such as `Gameplay`, `Networking`, `Economy`, `UI`, `Stability`.
- Use subcategories only when they help with filtering or ownership.
- Do not create ad-hoc category names from dynamic data.

Why:

- categories drive filtering and override rules in `LogTargetConfiguration`
- loggers are cached by category and subcategory key

### 3. Log Target Configuration Rules

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

### 4. Batching and Main-Thread Dispatch

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

### 5. Debug Mode Strategy

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
