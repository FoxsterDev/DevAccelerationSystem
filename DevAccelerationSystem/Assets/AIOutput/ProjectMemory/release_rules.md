# Release Rules

## Purpose
Define the durable release-facing rules for `DevAccelerationSystem`.

## Release Identity
- Package versions are sourced only from package manifests:
  - `com.foxsterdev.devaccelerationsystem` `1.0.1`
  - `com.foxsterdev.thebestlogger` `4.4.0`
  - `com.foxsterdev.loqui` `0.3.0`
- Future releases use `<package-id>/<version>` tags; existing bare tags are legacy repository snapshots.

## Release Gates
- Confirm package manifest accuracy after release-impacting changes:
  - package id
  - version
  - Unity baseline
  - package description when scope changes materially
- Confirm relevant package tests still represent the changed logic:
  - editor tests for deterministic logic
  - playmode tests for runtime behavior
  - performance tests when performance-sensitive paths changed
- Confirm at least one representative consumer validation target was selected for integration-sensitive changes.
- Do not describe editor or playmode proof for native targets as equivalent to physical-device proof.
- When runtime capability, integration guidance, or validation expectations change materially, update public logger docs alongside package docs.

## Consumer Rules
- Prefer `DevAccelerationSystem.DemoProject/` as tracked release evidence.
- Use `DAS.LocalProject/` as supplementary local-only evidence only when it exists.

## Memory Rules
- If release expectations change, update:
  - `README.md`
  - `architecture_ownership.md`
  - `testing_strategy.md`
  - `platform_constraints.md`
  - `../Docs/TheBestLogger_Integration_Best_Practices.md`
  - `../Docs/TheBestLogger_AI_Integration_Audit_Prompt.md`
  - this file
