# The Best Logger

Configurable logging package for Unity projects with runtime logging, structured target configuration, and stability-oriented integration points.

## Current Package Baseline

- Package id: `com.foxsterdev.thebestlogger`
- Latest tagged release: `2.2.15`
- Declared Unity baseline: `2022.3`
- This workspace currently resolves `com.cysharp.zstring` from `Packages/ZString.Unity.2.6.0.tgz`
- This workspace also includes `com.unity.test-framework.performance@3.1.0` for package performance measurements

## Installation

### Install via UPM

Use the tagged package path:

```text
https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/TheBestLogger#2.2.15
```

### Install manually via `manifest.json`

```json
{
  "dependencies": {
    "com.foxsterdev.thebestlogger": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/TheBestLogger#2.2.15"
  }
}
```

## Runtime Capabilities

- Central `LogManager.Initialize(...)` entrypoint for setting up logger targets and log sources
- Category-based logger creation through `LogManager.CreateLogger(...)`
- Per-target configuration, muting, minimum log-level control, stack-trace control, and debug-mode overrides
- Optional batching and main-thread dispatch decorations for compatible log targets
- Built-in capture for:
  - `UnityEngine.Debug` logs
  - `Application.logMessageReceived` and threaded application logs
  - unobserved `Task` exceptions
  - unobserved `UniTask` exceptions
  - current-domain unhandled exceptions
  - optional `System.Diagnostics` debug and console sources

## Built-In And Example Targets

- Unity Editor console target
- Android Logcat target
- Apple unified logging target
- Fallback logger for initialization or configuration failures
- IMGUI runtime log viewer example
- OpenSearch example target
- Safe third-party target base class for guarded integrations
- Background file writer utility

## Validation Surface

- Editor test coverage for deterministic logger logic, configuration behavior, delivery contracts, and fault isolation
- PlayMode coverage for dispatch, batching, runtime target execution paths, and frame-based runtime behavior
- Performance coverage via Unity Performance Testing for hot-path allocation and frame-time-sensitive regressions
- Tracked consumer validation in `DevAccelerationSystem.DemoProject/`

Important constraint:

- editor, playmode, and package performance evidence are not equal to physical-device proof for Android/iOS/macOS native-target observability or device-facing performance claims

## StabilityHub

- Optional `StabilityHub` integration for retrieving and logging previous-session crash data
- Current source includes iOS crash-reporter support behind platform-specific wiring

## Integration And Audit Docs

- Repository-level integration guide:
  - `DevAccelerationSystem/Docs/TheBestLogger_Integration_Best_Practices.md`
- Repository-level audit prompt:
  - `DevAccelerationSystem/Docs/TheBestLogger_AI_Integration_Audit_Prompt.md`
- Current package changelog:
  - `CHANGELOG.md`

## Release Highlights Since `2.2.4`

- `2.2.5`
  - optional `ZString`-based stack trace building
  - `UniTask` unobserved-exception log source
- `2.2.6`
  - max-length truncation for log messages and stack traces
- `2.2.7`
  - stack trace formatter exception fix
- `2.2.8`
  - logger subcategories
  - profiler markers for target updates
  - allocation cleanup for batch processing
- `2.2.9`
  - hidden API key in OpenSearch example serialization
  - fallback logger
- `2.2.10`
  - `LogMessageFormatter` fix when subcategory is used
- `2.2.11`
  - Android system log target
  - log attributes visible in Unity Editor console target
  - direct Unity console notification when OpenSearch target fails
- `2.2.13`
  - `LogTrace` extension path for editor and development builds
  - formatting improvements and allocation-aware `LogFormat` overloads
- `2.2.14`
  - `[HideInCallstack]` on `LogTrace` extension method for cleaner Unity Console navigation
- `2.2.15`
  - target-specific `DebugMode.SessionDebugRolloutPercentage`
  - sticky session-random debug activation
  - expanded partial remote-config documentation for `OpenSearch`

## Getting Started

1. Create or load a `LogManagerConfiguration` asset.
2. Construct the log targets you want to use.
3. Initialize `LogManager` from the Unity main thread.
4. Create category loggers through `LogManager.CreateLogger(...)`.

The example entrypoint in `Runtime/Examples/GameLoggerEntryPoint.cs` shows one package-owned initialization path that wires logger targets and `StabilityHub`.

If you do not want `Resources` paths at bootstrap time, use the direct configuration overloads instead:

```csharp
using System.Threading;
using StabilityHub;
using StabilityHub.Monitoring;
using TheBestLogger;
using TheBestLogger.Examples;
using UnityEngine;

var logTargets = new LogTarget[]
{
#if UNITY_EDITOR
    new UnityEditorConsoleLogTarget(),
#endif
#if UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
    new AppleSystemLogTarget(Application.identifier, "Unity")
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

var loggerConfiguration = LogManagerConfigurationPresets.CreateQa(appleConfigurationSo);
loggerConfiguration.DefaultUnityLogsCategoryName = "Game";

var disposingToken = CancellationToken.None;
#if UNITY_2022_3_OR_NEWER
disposingToken = Application.exitCancellationToken;
#endif

LogManager.Initialize(logTargets, loggerConfiguration, disposingToken, "qa-device");

var stabilityConfiguration = MonitoringConfigurationPresets.CreateQa();
StabilityHubService.Initialize(LogManager.CreateLogger("Stability"), stabilityConfiguration);
```

The demo sample scene under `DevAccelerationSystem.DemoProject/Assets/TheBestLoggerSample/Scenes/LoggerSampleScene.unity` now supports both resource-driven bootstrap and scripted preset-driven bootstrap through the `GameLoggerSample` component settings.

At runtime the sample also generates a tabbed control screen with:

- bootstrap switching between `ResourcesDev`, scripted production, and scripted QA flows
- main-thread, background-thread, exception, and Unity console log emission actions
- runtime target patching and debug-mode toggling
- `StabilityHub` previous-session retrieval flow
- a safe mock `OpenSearch` delivery path that captures payload previews without talking to a real backend

## How DebugMode Works

`DebugMode` is a per-target override. It is not a global switch that automatically makes the whole logger verbose.

The logger always has a normal baseline configuration:

- `MinLogLevel`
- `OverrideCategories`
- other regular target settings

`DebugMode` can become active for a target in two independent ways:

1. session rollout
2. explicit user match by `debugId`

The effective rule is:

- `DebugMode active = session rollout OR explicit debugId match`

If neither condition is active, the target stays on the normal baseline config.

In practice this means:

- top-level `MinLogLevel` is your normal production or default behavior
- `DebugMode.MinLogLevel` is used only after debug really becomes active
- top-level `OverrideCategories` are your normal category rules
- `DebugMode.OverrideCategories` are applied only while debug is active

Important rule:

- if you want production to stay quiet, keep the top-level target config strict, for example `Warning` or `Error`
- put more verbose logging such as `Debug` inside `DebugMode`

### Session rollout behavior

`DebugMode.SessionDebugRolloutPercentage` is evaluated once when `LogManager.Initialize(...)` runs for that target.

Important rollout rules:

- session rollout is target-specific
- the rollout percentage lives inside that target's `DebugMode` block
- `SessionDebugRolloutPercentage` is a `float`, so values such as `2.5` are supported
- after the rollout decision is made for that target, it is not rerolled again until the next `LogManager.Initialize(...)`
- after `LogManager.Dispose()` and the next `Initialize(...)`, a new session rollout decision is made

The runtime does this at startup:

- read `DebugMode.SessionDebugRolloutPercentage`
- if that percentage is `<= 0`, session rollout is inactive
- if that percentage is `>= 100`, session rollout is active
- otherwise the logger rolls `Random.value < SessionDebugRolloutPercentage / 100f`

How that session rollout is applied:

- if the session rollout is active, that `OpenSearch` target becomes debug-active when `DebugMode.Enabled = true`
- if that target has `DebugMode.Enabled = false`, session rollout does not activate it
- if remote config later turns `DebugMode.Enabled` off for a target, that target stops using session debug immediately

Recommendation:

- keep session rollout inside the target's `DebugMode` block when the rollout belongs to that target's debug policy

Migration note:

- session rollout percentage now lives in `DebugMode.SessionDebugRolloutPercentage`

This is intended for cases such as:

- observe `Debug`-level behavior in `2.5%` of sessions
- keep the rest of production on the normal top-level target config for that sink

### Explicit `debugId` behavior

Allowlist-based debug activation is independent from rollout.

`DebugMode` becomes active through explicit `debugId` only when all of these conditions are true:

1. the target config has `DebugMode.Enabled = true`
2. the current `debugId` is present in `DebugMode.IDs`
3. debug was explicitly requested through `LogManager.Initialize(..., debugId)` or `LogManager.SetDebugMode(debugId, true)`

Important explicit-match rules:

- the logger no longer falls back to `SystemInfo.deviceUniqueIdentifier`
- if `debugId` is empty, explicit allowlist activation does not happen
- `LogManager.SetDebugMode(debugId, true)` enables debug only for targets where that `debugId` matches `DebugMode.IDs`
- `LogManager.SetDebugMode(debugId, false)` disables only the explicit runtime request
- if a session rollout is already active, `SetDebugMode(debugId, false)` does not turn that session rollout off

### Persistence behavior

- runtime config updates can be cached and restored on the next app launch
- this includes persisted target `DebugMode` settings such as `Enabled`, `IDs`, `MinLogLevel`, and `OverrideCategories`
- `SessionDebugRolloutPercentage` belongs to `DebugMode`, so target runtime patches can update it
- persisted `IDs` participate only when the client again passes an explicit matching `debugId`

Short examples:

- built-in config says `MinLogLevel = Warning`
- `DebugMode.SessionDebugRolloutPercentage = 2.5`
- `DebugMode.Enabled = true`
- `DebugMode.IDs = ["player-1234"]`
- `DebugMode.MinLogLevel = Debug`

Then:

- `Initialize(...)` may enable debug for about `2.5%` of sessions for that `OpenSearch` target
- `Initialize(..., "player-1234")` also enables debug explicitly for that allowlisted id even if the session rollout did not hit
- `Initialize(..., "")` or `Initialize(...)` still allows the rollout path, but not the explicit `debugId` path

## How Remote Config Apply Works

There are two layers of config in runtime:

1. built-in config shipped with the client
2. runtime overrides applied later, for example from remote config

The logger starts from the built-in config first. After that it can overlay cached runtime patches from the previous session if startup cache is enabled.

At runtime you can update config in two ways:

- typed config objects: `UpdateLogTargetsConfigurations(Dictionary<string, LogTargetConfiguration>)`
- raw JSON patches: `UpdateLogTargetsConfigurations(Dictionary<string, string>)`

Recommended rule:

- use the raw JSON overload for remote config whenever you can

Why:

- raw JSON preserves "field was absent" semantics
- typed objects are already deserialized before they reach the logger, so omitted primitive fields can no longer be distinguished from default values

What happens when a runtime update is applied:

1. the incoming patch is merged onto the current target config
2. the updated config is applied to existing log targets immediately
3. current debug state is reevaluated against the new config, the current session rollout flag, and current `debugId`
4. if startup cache is enabled, the effective incoming patches are saved to persistent cache

What happens on next launch:

1. built-in config is loaded from the client
2. cached runtime patches are overlaid on top of it
3. targets are initialized from that effective config
4. a new session rollout decision is made for the new `LogManager` session from the target's `DebugMode.SessionDebugRolloutPercentage`
5. if the client passes an explicit `debugId`, explicit allowlist activation is checked against the resulting config

Practical guidance:

- use built-in config for safe defaults
- use remote config for temporary overrides and targeted diagnostics
- prefer raw JSON patches for partial remote updates
- if you send only one target update, use the single-target overloads
- if startup cache should not survive restarts for a given product flow, disable `RemoteOverrideStartupCache`

### DebugMode And Partial Remote Config Behavior

There are three important cases to distinguish.

#### 1. Full remote patch replaces the `DebugMode` section

If the incoming patch contains a full `DebugMode` object, that object replaces the current `DebugMode` config for the target.

Example:

```json
{
  "DebugMode": {
    "Enabled": false,
    "IDs": []
  }
}
```

Current-session result:

- explicit `debugId` activation stops working immediately because `DebugMode.Enabled = false`
- session rollout also stops affecting that target immediately because the target is no longer rollout-eligible

Next-launch result when startup cache is enabled:

- the cached `DebugMode.Enabled = false` state is loaded before fresh remote config arrives
- session rollout still comes from the cached or built-in `DebugMode.SessionDebugRolloutPercentage`
- this target will not participate in that rollout while `DebugMode.Enabled = false`
- explicit `debugId` allowlist activation will also stay inactive until config changes again

#### 2. Partial remote patch does not include any `DebugMode` section

If the incoming raw JSON patch omits `DebugMode` completely, the current `DebugMode` config is preserved as-is.

Example `OpenSearchLogTargetConfiguration` patch:

```json
{
  "OpenSearchHostUrl": "https://remote.example",
  "Muted": true
}
```

Result:

- current `DebugMode.Enabled` is preserved
- current `DebugMode.IDs` is preserved
- current `DebugMode.MinLogLevel` and `OverrideCategories` are preserved
- current `SessionDebugRolloutPercentage` is preserved

This is the main reason raw JSON patches are recommended for remote partial updates.

#### 3. Partial remote patch includes `DebugMode`, but only part of it

If the patch includes `DebugMode`, but only one nested field, the other nested `DebugMode` fields are preserved.

Example `OpenSearchLogTargetConfiguration` patch:

```json
{
  "DebugMode": {
    "Enabled": false
  }
}
```

Result:

- `DebugMode.Enabled` becomes `false`
- existing `DebugMode.IDs` is preserved
- existing `DebugMode.MinLogLevel` is preserved
- existing `DebugMode.OverrideCategories` is preserved
- existing `SessionDebugRolloutPercentage` is preserved

### Practical DebugMode Scenarios With `OpenSearchLogTargetConfiguration`

Assume the current effective config for `OpenSearchLogTargetConfiguration` is:

```json
{
  "Muted": false,
  "MinLogLevel": 3,
  "DebugMode": {
    "Enabled": true,
    "IDs": [ "player-1234" ],
    "MinLogLevel": 0
  },
  "OpenSearchHostUrl": "https://logs-a.example",
  "OpenSearchSingleLogMethod": "/bulk",
  "IndexPrefix": "prod-",
  "ApiKey": "secret-a"
}
```

Scenario A. Remote patch changes only the endpoint:

```json
{
  "OpenSearchHostUrl": "https://logs-b.example"
}
```

Result:

- OpenSearch host changes to `https://logs-b.example`
- `DebugMode` stays exactly as it was
- `SessionDebugRolloutPercentage` stays exactly as it was

Scenario B. Remote patch disables OpenSearch target participation in session debug:

```json
{
  "DebugMode": {
    "Enabled": false
  }
}
```

Result in the current session:

- current session keeps its already rolled session-debug flag for this target
- this target stops participating in that session rollout immediately
- explicit `debugId` allowlist also stops for this target because `DebugMode.Enabled = false`

Scenario C. Remote patch disables `DebugMode` completely:

```json
{
  "DebugMode": {
    "Enabled": false,
    "IDs": []
  }
}
```

Result in the current session:

- explicit `debugId` activation stops
- session rollout also stops for that target immediately because `DebugMode.Enabled = false`

Result on the next session:

- startup rollout still comes from `DebugMode.SessionDebugRolloutPercentage`
- this target still does not participate until `DebugMode.Enabled` becomes `true` again
- no explicit allowlist activation

### Partial Remote Patch Combinations For `OpenSearchLogTargetConfiguration`

Assume the current effective target config is:

```json
{
  "Muted": false,
  "MinLogLevel": 3,
  "DebugMode": {
    "Enabled": true,
    "SessionDebugRolloutPercentage": 2.5,
    "IDs": [ "player-1234" ],
    "MinLogLevel": 0
  },
  "OpenSearchHostUrl": "https://logs-a.example",
  "OpenSearchSingleLogMethod": "/bulk",
  "IndexPrefix": "prod-",
  "ApiKey": "secret-a"
}
```

If the current logger session was already initialized, remember this rule:

- changing `DebugMode.SessionDebugRolloutPercentage` updates config immediately
- but it does not reroll the already decided session rollout for the current session
- the new percentage takes effect on the next `LogManager.Initialize(...)`

Combination 1. Change only transport settings:

```json
{
  "OpenSearchHostUrl": "https://logs-b.example",
  "ApiKey": "secret-b"
}
```

Result:

- endpoint changes immediately
- API key changes immediately
- current `DebugMode` values are preserved
- current session rollout decision is preserved

Combination 2. Change only rollout percentage:

```json
{
  "DebugMode": {
    "SessionDebugRolloutPercentage": 20.0
  }
}
```

Result in the current session:

- current session rollout decision does not change
- current `DebugMode.Enabled`, `IDs`, and `MinLogLevel` are preserved

Result on the next session:

- this target rolls startup debug with `20.0%`

Combination 3. Disable target debug now, but keep rollout percentage for later:

```json
{
  "DebugMode": {
    "Enabled": false,
    "SessionDebugRolloutPercentage": 20.0
  }
}
```

Result in the current session:

- debug turns off immediately for this target
- explicit `debugId` activation also stops immediately
- the new `20.0%` is stored in config

Result on the next session:

- the target still stays debug-inactive while `Enabled = false`
- if some later patch sets `Enabled = true`, the stored `20.0%` rollout will be used

Combination 4. Change only explicit allowlist:

```json
{
  "DebugMode": {
    "IDs": [ "player-5678" ]
  }
}
```

Result:

- current rollout settings are preserved
- current `DebugMode.Enabled` and `MinLogLevel` are preserved
- if the current logger session already has `_currentDebugId = "player-5678"` from `Initialize(..., debugId)` or `SetDebugMode("player-5678", true)`, explicit debug becomes active immediately
- if the current debug id does not match, explicit debug stays inactive

Combination 5. Change only debug output verbosity:

```json
{
  "DebugMode": {
    "MinLogLevel": 1
  }
}
```

Result:

- target top-level `MinLogLevel` is preserved
- rollout settings are preserved
- allowlist settings are preserved
- if debug is already active for this target, the new debug min level starts affecting filtering immediately
- if debug is not active, this setting is stored and will be used the next time debug becomes active

Combination 6. Mix transport and debug changes in one partial patch:

```json
{
  "OpenSearchHostUrl": "https://logs-c.example",
  "Muted": true,
  "DebugMode": {
    "SessionDebugRolloutPercentage": 10.0,
    "IDs": [ "player-9999" ]
  }
}
```

Result in the current session:

- endpoint changes immediately
- target mute state changes immediately
- explicit allowlist changes immediately
- rollout percentage is updated in config but the current session rollout decision is not rerolled

Result on the next session:

- startup rollout uses `10.0%`
- explicit allowlist uses `player-9999`

## Integration Notes

- Keep target sets intentional instead of enabling every sink by default.
- Use batching for expensive or remote sinks.
- Use main-thread dispatch only for targets that are not safe to execute from worker threads.
- Keep stack traces mostly on `Error` and `Exception` unless you are actively diagnosing a hot issue.
- Treat `OpenSearch` as production-sensitive if you use it in a real backend pipeline.

## Exception Hardening Checklist

Use these rules if you want the logger to stay an observer of failures instead of becoming a new failure source.

- Keep logger-owned runtime paths `never-throw` in production.
- If a custom target or third-party sink fails, isolate that failure, mute or disable the broken sink, and keep healthy targets alive.
- If you already have a real `Exception`, pass the real exception object into the logger:
  - `logger.LogException(exception)`
  - `logger.LogError("context message", exception)`
- Do not reduce exceptions to only `exception.Message` or only `exception.StackTrace` too early.
- Do not collapse `AggregateException` to one `InnerException` unless that loss of context is intentional.
- Prefer package-safe helpers such as `FireAndForget()` or `FireAndLogWhenExceptions(...)` over raw `async void` logging helpers.
- If you rethrow after logging, use:
  - `throw;` for the same caught exception in the same `catch`
  - `ExceptionDispatchInfo.Capture(exception).Throw();` for delayed rethrow or rethrow of an extracted inner exception
- Do not use:
  - `throw ex;`
  - `throw ex.InnerException;`
- If a reflection-based integration uses `Invoke(...)`, prefer the overload that supports `BindingFlags.DoNotWrapExceptions` when available.

Minimal good pattern:

```csharp
try
{
    SendRemotePayload();
}
catch (Exception ex)
{
    logger.LogError("Remote payload send failed.", ex);
}
```

Minimal bad pattern:

```csharp
try
{
    SendRemotePayload();
}
catch (Exception ex)
{
    logger.LogError($"Remote payload send failed: {ex.Message}");
}
```
