<h1 align="center">
<img alt="logo" src="Docs/Img7.png" height="200px" />
<br/>
Development Acceleration System for Unity projects
</h1>

This repository currently publishes two Unity package surfaces:

- `com.foxsterdev.devaccelerationsystem`
- `com.foxsterdev.thebestlogger`

Latest tagged releases in this repository:

- `DevAccelerationSystem`: `1.0.1`
- `TheBestLogger`: `2.2.15`

## Packages In This Repository

### DevAccelerationSystem

- Package id: `com.foxsterdev.devaccelerationsystem`
- Package path: `DevAccelerationSystem/Assets/DevAccelerationSystem`
- Latest tagged release: `1.0.1`
- Declared Unity baseline in package manifest: `2020.3`
- Current authoring workspace in this repo: `2022.3.67f2`

Purpose:

- run script-compilation checks for multiple build targets
- validate different scripting define symbol combinations without doing full Unity builds
- review compilation output in-editor or from batch mode

### TheBestLogger

- Package id: `com.foxsterdev.thebestlogger`
- Package path: `DevAccelerationSystem/Assets/TheBestLogger`
- Latest tagged release: `2.2.15`
- Declared Unity baseline in package manifest: `2022.3`
- Package README: [DevAccelerationSystem/Assets/TheBestLogger/README.md](./DevAccelerationSystem/Assets/TheBestLogger/README.md)
- Package changelog: [DevAccelerationSystem/Assets/TheBestLogger/CHANGELOG.md](./DevAccelerationSystem/Assets/TheBestLogger/CHANGELOG.md)

Purpose:

- configurable runtime logging
- multiple log sources and targets
- platform targets and example integrations
- `StabilityHub` integration for stability-oriented flows
- current repository state also includes editor, playmode, performance, and tracked consumer-validation coverage around the logger package

Operational notes:

- `DebugMode` is target-specific and has two independent activation paths:
  - target-owned session rollout from `DebugMode.SessionDebugRolloutPercentage`, rolled once on `LogManager.Initialize(...)` for that target
  - explicit allowlist activation when the client passes a matching `debugId` into `LogManager.Initialize(...)` or `LogManager.SetDebugMode(...)`
- effective rule is `session rollout OR explicit debugId match`
- startup no longer falls back to `SystemInfo.deviceUniqueIdentifier` for explicit debug activation
- `DebugMode.SessionDebugRolloutPercentage` is a `float`, so rollout values such as `2.5` are supported
- runtime remote config can be applied immediately and can also be restored from startup cache on the next launch
- for partial remote updates, prefer the raw JSON config overloads
- detailed `DebugMode` and remote-config behavior is documented in the package README:
  - [TheBestLogger README](./DevAccelerationSystem/Assets/TheBestLogger/README.md)

Additional public docs:

- [TheBestLogger Integration Best Practices](./Docs/TheBestLogger_Integration_Best_Practices.md)
- [TheBestLogger AI Integration Audit Prompt](./Docs/TheBestLogger_AI_Integration_Audit_Prompt.md)

## DevAccelerationSystem Overview

`DevAccelerationSystem` is focused on one main workflow: compile-checking the same project under different target and define-symbol combinations before you spend time on a real build.

The current package surface includes:

- `ProjectCompilationConfig` asset creation from Unity menu
- editor menu items under `Window/DevAccelerationSystem/ProjectCompilationCheck`
- compilation output viewer window
- support for EditorMode and BatchMode execution
- default config presets for:
  - Android
  - iOS
  - WebGL
  - StandaloneOSX
  - StandaloneWindows64

## Why Use It

Typical failure mode:

- a code path is wrapped in `#if DEVELOPMENT_BUILD` or custom store-specific symbols
- the branch is rarely compiled locally
- Rider or manual cleanup removes something that is only needed in one compilation mode
- the project stays apparently healthy until CI or a real build compiles that combination

`DevAccelerationSystem` reduces that gap by compiling representative configurations directly from the editor tooling or batch-mode entrypoint.

## Install DevAccelerationSystem

### Install via UPM

```text
https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/DevAccelerationSystem#1.0.1
```

### Install via `manifest.json`

```json
{
  "dependencies": {
    "com.foxsterdev.devaccelerationsystem": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/DevAccelerationSystem#1.0.1"
  }
}
```

## Install TheBestLogger

### Install via UPM

```text
https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/TheBestLogger#2.2.15
```

### Install via `manifest.json`

```json
{
  "dependencies": {
    "com.foxsterdev.thebestlogger": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/TheBestLogger#2.2.15"
  }
}
```

## Unity Workflow

### Create Configuration

1. Open any `Editor` folder in your project.
2. Create `Assets > DevAccelerationSystem > Create ProjectCompilationConfig`.
3. Keep one config asset for batch mode if you rely on shell execution.

### Configure Checks

The default asset starts with representative compilation configurations for multiple build targets and both development and non-development modes.

For each config you can control:

- build target
- script compilation option
- enabled state
- extra scripting define symbols

### Run In Editor

Open:

`Window > DevAccelerationSystem > ProjectCompilationCheck`

Available actions:

- `Run all compilations`
- `Focus config`
- `Show Compilation Output Viewer`

If no config exists yet, the menu also exposes the “first create a project compilation config” entry path.

## Batch Mode Workflow

Reference script:

`DevAccelerationSystem/Assets/DevAccelerationSystem/ShellScripts/unity_compilation_runner_mac.sh`

The current script runs:

- Unity in batch mode
- execute method `DevAccelerationSystem.ProjectCompilationCheck.BatchModeRunner.Run`
- output folder under `Library/ProjectCompilationCheckOutput`

Usage:

```bash
./unity_compilation_runner_mac.sh <project_path> [compile_config_name] [unity_version]
```

Example:

```bash
./unity_compilation_runner_mac.sh /Users/PUT_HERE_YOURUSER_IF_ANY/Projects/DevAccelerationSystem/DevAccelerationSystem.DemoProject RunAll 2022.3.13f1
```

Generated artifacts include:

- Unity compilation log
- `CompilationOutput.json`

## Known Good Uses

- validate `DEVELOPMENT_BUILD` branches before CI
- validate store-specific scripting define combinations
- catch missing platform-module issues early
- review compile-state drift without switching active platform in the editor

## Limitations

- this is script-compilation validation, not a full player build
- Unity editor compilation API is useful evidence, but not perfect proof of final build behavior
- IL2CPP and full build pipeline issues can still exist even when compilation checks pass
- build-target module availability still matters; for example, WebGL checks require the Unity WebGL module

## Recommendations

- keep your `ProjectCompilationConfig` narrow and representative instead of trying to model every theoretical permutation
- use extra scripting define symbols for store-specific or environment-specific branches that routinely break in CI
- remove stale define-symbol examples you do not use
- if you rely on third-party packages with optional compile-time features, explicitly represent those symbol combinations in your compilation config

## Demo Project

The repository includes `DevAccelerationSystem.DemoProject/` as a consumer validation workspace for package behavior and example usage.

## Related Package

If you need runtime logging rather than compilation tooling, use `TheBestLogger`.

Useful entry points for `TheBestLogger`:

- [Package README](./DevAccelerationSystem/Assets/TheBestLogger/README.md)
- [Integration Best Practices](./Docs/TheBestLogger_Integration_Best_Practices.md)
- [AI Integration Audit Prompt](./Docs/TheBestLogger_AI_Integration_Audit_Prompt.md)

Recent `TheBestLogger` highlights:

- `2.2.5`
  - optional `ZString` stack-trace building
  - `UniTask` unobserved-exception source
- `2.2.8`
  - logger subcategories
  - profiler markers
  - allocation cleanup
- `2.2.9`
  - fallback logger
  - hidden API key in OpenSearch example target
- `2.2.11`
  - Android system log target
  - log attributes in Unity Editor console target
- `2.2.13`
  - `LogTrace` extension method and formatting improvements
- `2.2.14`
  - `[HideInCallstack]` on `LogTrace` for better Unity Console navigation
- `2.2.15`
  - target-specific `DebugMode.SessionDebugRolloutPercentage`
  - sticky session-random debug activation
  - expanded partial remote-config examples for `OpenSearch`

## Contributing

Open an issue or send a PR in this repository.
