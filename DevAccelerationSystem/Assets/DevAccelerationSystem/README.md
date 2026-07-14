# Dev Acceleration System

`com.foxsterdev.devaccelerationsystem` is an Editor-only Unity package for running script-compilation checks over representative build targets and scripting-define combinations. It does not create player builds and does not mutate project settings merely to inspect a configuration.

## Package identity

- Version: `1.0.2` (from `package.json`)
- Declared Unity floor: `2020.3`
- Current source path: `DevAccelerationSystem/Assets/DevAccelerationSystem`

## Install

```json
{
  "dependencies": {
    "com.foxsterdev.devaccelerationsystem": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=/DevAccelerationSystem/Assets/DevAccelerationSystem#com.foxsterdev.devaccelerationsystem/1.0.2"
  }
}
```

This release-candidate URL becomes installable when the documented package-specific tag is authorized and published. The historical `4.4.0` repository tag is not a package-specific release tag.

## Use

1. Create a configuration from `Assets > DevAccelerationSystem > Create ProjectCompilationConfig`.
2. Open `Window > DevAccelerationSystem > ProjectCompilationCheck`.
3. Select representative targets and define symbols, then run the checks.

For batch mode, invoke `DevAccelerationSystem.ProjectCompilationCheck.BatchModeRunner.Run`. The shell helper accepts a project path, configuration name, and Unity version; it writes output under the project's `Library/ProjectCompilationCheckOutput` folder.

## Limits

- Compilation checks are not substitutes for player builds, IL2CPP builds, or platform-device verification.
- A target's Unity module must be installed before it can be checked.
- Unity 6 Build Profile coverage is a modernization target; do not claim it is verified until it is executed in a supported editor.

See the repository [compatibility policy](../../../Docs/COMPATIBILITY.md), [migration guide](../../../Docs/MIGRATION.md), and [release policy](../../../Docs/RELEASES.md).
