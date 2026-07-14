# Changelog

All notable changes to `com.foxsterdev.devaccelerationsystem` are documented here. The package follows [Semantic Versioning](https://semver.org/).

## [1.0.1] - 2024-05-06

- Updated compilation-output refresh behavior, menus, README, and batch-mode guidance.

## [1.0.0] - 2024-05-04

- Initial release of project compilation checks for custom scripting-define and build-target configurations.

## Unreleased

- Package metadata, documentation, validation, and release policy are being modernized without changing the public package API.
- Replaced internal Unity module reflection with `BuildPipeline.IsBuildTargetSupported`.
- Fixed `ProjectCompiler.CreateConfiguration` to report success after it persists a new configuration.
