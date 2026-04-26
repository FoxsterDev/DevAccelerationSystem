# The Best Logger

Configurable logging package for Unity projects with runtime logging, structured target configuration, and stability-oriented integration points.

## Current Package Baseline

- Package id: `com.foxsterdev.thebestlogger`
- Latest tagged release: `2.2.14`
- Declared Unity baseline: `2022.3`
- This workspace currently resolves `com.cysharp.zstring` from `Packages/ZString.Unity.2.6.0.tgz`

## Installation

### Install via UPM

Use the tagged package path:

```text
https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/TheBestLogger#2.2.14
```

### Install manually via `manifest.json`

```json
{
  "dependencies": {
    "com.foxsterdev.thebestlogger": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/TheBestLogger#2.2.14"
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

## StabilityHub

- Optional `StabilityHub` integration for retrieving and logging previous-session crash data
- Current source includes iOS crash-reporter support behind platform-specific wiring

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

## Getting Started

1. Create or load a `LogManagerConfiguration` asset.
2. Construct the log targets you want to use.
3. Initialize `LogManager` from the Unity main thread.
4. Create category loggers through `LogManager.CreateLogger(...)`.

The example entrypoint in `Runtime/Examples/GameLoggerEntryPoint.cs` shows one package-owned initialization path that wires logger targets and `StabilityHub`.
