# Dev Acceleration System

`com.foxsterdev.devaccelerationsystem` is an Editor-only Unity package for running script-compilation checks over representative build targets and scripting-define combinations. It does not create player builds and does not mutate project settings merely to inspect a configuration.

## Package identity

- Source version: `1.1.0` (from `package.json`)
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

The latest published package-specific tag is `com.foxsterdev.devaccelerationsystem/1.0.2`. Pin that tag for a reproducible installation; do not use `master` as a production dependency. The source version `1.1.0` requires its matching tag after release validation. The local Phase 3 tag `1.0.3` still needs to be visible on `origin` before it is documented as an installable release. The historical `4.4.0` repository tag is not a package-specific release tag.

## Use

1. Create a configuration from `Assets > DevAccelerationSystem > Create ProjectCompilationConfig`.
2. Open `Window > DevAccelerationSystem > ProjectCompilationCheck`.
3. Select representative targets and define symbols, then run the checks.

For batch mode, invoke `DevAccelerationSystem.ProjectCompilationCheck.BatchModeRunner.Run`. The shell helper accepts a project path, configuration name, and Unity version; it writes output under the project's `Library/ProjectCompilationCheckOutput` folder.

## Project Doctors

Open `Window > DevAccelerationSystem > Project Doctors` to run read-only scans and inspect their deterministic JSON report. The window writes reports under `Library/DevAccelerationSystem/Reports`; it does not change PlayerSettings, defines, packages, or project files.

- **UPM Package Doctor** validates package identity, SemVer, root boundaries, required public documents and URLs, dependency declarations, test assemblies, samples, absolute paths, and package-specific release tags.
- **Define & Build Profile Doctor** compares defines across `Standalone`, `Android`, and `iOS`; reports required or forbidden symbols, drift, asmdef constraints, incomplete version defines, and Build Profiles discovered through Unity's public AssetDatabase query with a filesystem fallback.
- **Project Baseline Audit** evaluates a version-controlled lower-camel-case policy. Start with [`ProjectBaseline.example.json`](Documentation~/ProjectBaseline.example.json), copy it into the consumer project, and only add requirements the project intentionally owns.

All scans are read-only by default. `DefineBuildProfileDoctor` and `ProjectBaselineAudit` expose explicit preview, apply, backup, and restore APIs for a caller that has reviewed the change set. There is no command-line apply mode. Applying defines or PlayerSettings requires Unity recompilation and a new scan before treating the result as verified.

Run one Doctor in CI or batch mode with Unity's standard execute-method invocation:

```text
-executeMethod DevAccelerationSystem.ProjectAuditing.ProjectDoctorRunner.Run -dasDoctor upm
-executeMethod DevAccelerationSystem.ProjectAuditing.ProjectDoctorRunner.Run -dasDoctor defines -dasRequiredSymbols SYMBOL_A,SYMBOL_B -dasForbiddenSymbols SYMBOL_C -dasRequiredBuildProfiles AndroidProfile
-executeMethod DevAccelerationSystem.ProjectAuditing.ProjectDoctorRunner.Run -dasDoctor baseline -dasBaseline Assets/ProjectBaseline.json -dasDoctorOutput Library/DevAccelerationSystem/Reports/baseline.json
```

The runner exits non-zero in batch mode when the report contains an error. Its output is machine-readable JSON and includes a stable finding code, severity, path, and remediation.

## Limits

- Compilation checks are not substitutes for player builds, IL2CPP builds, or platform-device verification.
- A target's Unity module must be installed before it can be checked.
- Build Profile discovery is inspection only; it does not build, activate a profile, or prove Unity 6 Build Profile behavior until that editor lane is executed.

See the repository [compatibility policy](../../../Docs/COMPATIBILITY.md), [migration guide](../../../Docs/MIGRATION.md), and [release policy](../../../Docs/RELEASES.md).
