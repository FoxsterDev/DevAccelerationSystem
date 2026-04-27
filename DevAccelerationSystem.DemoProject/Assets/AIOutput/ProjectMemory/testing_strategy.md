<!-- Managed by AIRoot/scripts/init_ai_project.sh project-memory-baseline -->
# Testing Strategy

## Purpose
Define the durable validation expectations for `DevAccelerationSystem.DemoProject`.

## Fill In
- local test surfaces that represent changed logic
- consumer or runtime validation targets if package integration matters
- release-gating validation paths

## Rules
- Use the narrowest representative test surface first.
- When a change affects integration, lifecycle, native behavior, or runtime wiring, pair local validation with representative consumer validation.
- For integration-sensitive changes reviewed in this consumer workspace, write a tracked report under `Assets/AIOutput/ValidationReports/`.
- Use `Assets/AIOutput/ValidationReports/integration_validation_report_template.md` as the default report shape.
- Update this file when validation ownership changes materially.
- For `TheBestLoggerSample` UI work, treat `LoggerSampleScene` as the manual validation surface for sample layout, readability, and action wiring.
- Sample-UI refactors must preserve logger actions, managed exception actions, and native iOS crash triggers as part of the validation contract, not as optional demo chrome.
- For scene-authored sample UI changes, visual Editor proof is required in addition to code review because layout regressions are not reliably proven by source diffs alone.

## Current Consumer Gate
- `TheBestLogger.ConsumerValidation.PlayModeTests` is the tracked runtime gate for `TheBestLogger` consumer validation in this workspace.
- The gate must cover:
  - bootstrap through demo entry flow
  - main-thread logging
  - background-thread logging
  - exception logging
  - config update simulation
  - scene transition while a queued background log is in flight
- Record each meaningful validation pass in `Assets/AIOutput/ValidationReports/integration_validation_YYYY-MM-DD.md`.
- `Assets/TheBestLoggerSample/Scenes/LoggerSampleScene.unity` is the tracked manual validation surface for:
  - sample logger controls
  - managed exception actions
  - native iOS crash trigger visibility and routing
  - scene-authored UGUI readability on supported Unity lines
