# Changelog

All notable changes to `com.foxsterdev.devaccelerationsystem` are documented here. The package follows [Semantic Versioning](https://semver.org/).

## [1.1.0] - 2026-07-14

- Added the read-only UPM Package Doctor with deterministic human-readable and JSON findings for package metadata, layout, dependencies, release tags, tests, samples, and absolute paths.
- Added Define & Build Profile Doctor scans, deterministic previews, explicit apply/restore APIs with backups, and batch exit semantics for required and forbidden symbols, asmdef constraints, version defines, and discovered Build Profiles.
- Added a version-controlled lower-camel-case Project Baseline policy, deterministic audits, preview/apply/restore APIs, Unity Editor UI, and batch reports for package, project-file, color-space, scripting-backend, and define requirements.

## [1.0.3] - 2026-07-14

- Fixed `StandaloneOSX` configuration mapping so macOS compilation checks use the supported `Standalone` build-target group.
- Report `CompilePlayerScripts` exceptions as compilation failures and always remove the temporary compilation callback before returning a batch result.

## [1.0.2] - 2026-07-14

- Replaced internal Unity module reflection with `BuildPipeline.IsBuildTargetSupported`.
- Fixed `ProjectCompiler.CreateConfiguration` to report success after it persists a new configuration.
- Added complete package metadata, documentation, and release validation.

## [1.0.1] - 2024-05-06

- Updated compilation-output refresh behavior, menus, README, and batch-mode guidance.

## [1.0.0] - 2024-05-04

- Initial release of project compilation checks for custom scripting-define and build-target configurations.
