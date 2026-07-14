# Changelog

All notable changes to `com.foxsterdev.devaccelerationsystem` are documented here. The package follows [Semantic Versioning](https://semver.org/).

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
