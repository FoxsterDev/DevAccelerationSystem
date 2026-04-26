# Release Rules

## Purpose
Define the durable release-facing rules for `DevAccelerationSystem`.

## Release Identity
- Current package identity present in source: `com.foxsterdev.thebestlogger`
- Current package version present in source: `2.2.4`
- Declared Unity baseline present in source: `2022.3`

## Release Gates
- Confirm package manifest accuracy after release-impacting changes:
  - package id
  - version
  - Unity baseline
  - package description when scope changes materially
- Confirm relevant editor tests still represent the changed logic.
- Confirm at least one representative consumer validation target was selected for integration-sensitive changes.

## Consumer Rules
- Prefer `DevAccelerationSystem.DemoProject/` as tracked release evidence.
- Use `DAS.LocalProject/` as supplementary local evidence only.

## Memory Rules
- If release expectations change, update:
  - `README.md`
  - `architecture_ownership.md`
  - `testing_strategy.md`
  - `platform_constraints.md`
  - this file
