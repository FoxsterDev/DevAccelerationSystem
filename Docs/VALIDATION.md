# Validation

## License-free gate

Run from the repository root:

```bash
python3 scripts/validate_repo.py
```

The command validates package JSON, required metadata and documents, package IDs and semantic versions, unique assembly definitions, editor/runtime boundaries, package-local absolute paths, stale root-version references, internal documentation links, release-tag syntax, and tracked generated files. To validate a proposed release tag without creating it:

```bash
python3 scripts/validate_repo.py --release-tag com.foxsterdev.loqui/0.3.2
```

## Unity lanes

The release-foundation matrix is complete for the locally installed editors: Unity `2022.3.62f3`, `6000.0.61f1`, and `6000.3.3f1`. The detailed evidence is recorded in [the release-foundation report](../DevAccelerationSystem/Assets/AIOutput/ValidationReports/2026-07-14_release_foundation_validation.md).

- Unity `2022.3.62f3`: Dev Acceleration System EditMode `7/7`, Loqui EditMode `101/101`, TheBestLogger EditMode `266/266`, and TheBestLogger PlayMode `14/14` passed.
- Unity `6000.0.61f1` and `6000.3.3f1`: full-project EditMode `385/385` and PlayMode `14/14` passed on each editor.
- Clean local-file UPM imports passed for Dev Acceleration System + Loqui and for TheBestLogger on all three editors.

The package-specific tags are published. The recorded matrix proves local-file imports; a fresh clean-project Git-tag import matrix is still a separate validation gap. No physical-device or IL2CPP result is claimed.

The tracked consumer target is `DevAccelerationSystem.DemoProject/`. `DAS.LocalProject/` is local-only evidence.

## Phase 3 stabilization evidence

- The source project passed DAS EditMode `8/8`, Loqui EditMode `101/101`, and TheBestLogger EditMode `269/269` on Unity `2022.3.62f3`.
- TheBestLogger PlayMode passed `14/14` through the Unity MCP bridge. The new lifecycle coverage exercises current-token cancellation, cancelled-token initialization, and replacement initialization after a previous lifetime is disposed.
- Source `CompilePlayerScripts` validation passed for `StandaloneOSX` and `Android`, each with zero compiler errors.
- The tracked consumer imports the package roots through local-file UPM dependencies. Its Android compilation passed with `22` compiled assemblies and zero errors; its Loqui PlayMode integration suite passed `4/4`.

This evidence does not prove physical-device behavior, IL2CPP stripping, or a fresh Git-tag installation of the proposed `1.0.3`, `4.4.2`, and `0.3.2` releases.
