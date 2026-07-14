# Dev Acceleration System

Open-source Unity development tooling collected as three independently installable UPM packages. Each package has its own package ID, semantic version, README, changelog, and release line; `package.json` is the version source of truth.

| Package | Version | Purpose | Declared Unity floor |
| --- | --- | --- | --- |
| [`com.foxsterdev.devaccelerationsystem`](./DevAccelerationSystem/Assets/DevAccelerationSystem) | `1.0.1` | Editor-side compilation checks across build targets and define-symbol combinations. | 2020.3 |
| [`com.foxsterdev.thebestlogger`](./DevAccelerationSystem/Assets/TheBestLogger) | `4.4.0` | Configurable runtime logging, capture, target isolation, and stability integrations. | 2022.3 |
| [`com.foxsterdev.loqui`](./DevAccelerationSystem/Assets/Loqui) | `0.3.0` | Fallback-first localization with catalog, TMP, and editor tooling. | 2022.3 |

## Choose a package

- Install **Dev Acceleration System** to compile-check representative targets and scripting defines before a full build.
- Install **TheBestLogger** for a configurable Unity logging pipeline.
- Install **Loqui** for localizable text whose code literal is always a safe fallback.

## Installation

The current source snapshot is the existing Git tag `4.4.0`; it contains the package versions in the table above. It is a legacy repository-wide tag, not a future package-release convention. Use the package-specific path below.

```json
{
  "dependencies": {
    "com.foxsterdev.devaccelerationsystem": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=/DevAccelerationSystem/Assets/DevAccelerationSystem#4.4.0",
    "com.foxsterdev.thebestlogger": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=/DevAccelerationSystem/Assets/TheBestLogger#4.4.0",
    "com.foxsterdev.loqui": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=/DevAccelerationSystem/Assets/Loqui#4.4.0"
  }
}
```

Install only the package you need. For an unreleased change, replace `#4.4.0` with a commit SHA; do not use `master` for a production pin.

OpenUPM publication has not been performed. The repository is prepared for it, but registry submission and release tags require maintainer authorization. See [release policy](./Docs/RELEASES.md).

## Compatibility and support

The declared `unity` value in each package manifest is the compatibility floor, not proof of a tested matrix. The modernization target is Unity 2022.3 LTS, Unity 6000.0 LTS, and Unity 6000.3 LTS; actual verification evidence is recorded separately. See [compatibility policy](./Docs/COMPATIBILITY.md) and [validation](./Docs/VALIDATION.md).

## Repository layout

Package roots intentionally remain under `DevAccelerationSystem/Assets/` for the current release line, so existing Git UPM URLs remain valid. This is valid for OpenUPM and is documented in [ADR-001](./Docs/Architecture/ADR-001-package-layout.md). A future major release may move roots only with an explicit migration path.

## Development

Run the license-free repository gate from the repository root:

```bash
python3 scripts/validate_repo.py
```

For Unity-specific validation and release prerequisites, see [validation](./Docs/VALIDATION.md), [contributing](./Docs/CONTRIBUTING.md), [troubleshooting](./Docs/TROUBLESHOOTING.md), and the [release checklist](./Docs/RELEASE_CHECKLIST.md).
