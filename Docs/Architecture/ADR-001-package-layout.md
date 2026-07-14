# ADR-001: Retain package roots under `Assets` for the current release line

## Status

Accepted — 2026-07-14.

## Context

The repository has three valid package roots under `DevAccelerationSystem/Assets/`. Existing Git UPM URLs use `?path=/DevAccelerationSystem/Assets/<package>`. The project's `Packages/` directory is a consumer project's dependency directory, not the public package source location.

## Decision

Keep the three roots in place for the current release line. Complete each root as a UPM package and retain its Git UPM path. Do not move files or GUIDs as part of release-foundation work.

OpenUPM supports a package in a subfolder including `Assets/package-name`; it also supports per-package tag prefixes in a monorepo. The target tag format is `com.foxsterdev.<package>/<version>`.

## Consequences

- Existing Git UPM consumers remain compatible.
- A future move to `Packages/com.foxsterdev.*` is a major-release migration: preserve GUIDs, support documented old URLs through the prior release, test upgrade and fresh installation, and publish a migration guide before changing paths.
