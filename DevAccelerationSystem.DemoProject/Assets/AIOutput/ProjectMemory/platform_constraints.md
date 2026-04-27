<!-- Managed by AIRoot/scripts/init_ai_project.sh project-memory-baseline -->
# Platform Constraints

## Purpose
Record durable engine, platform, and integration constraints for `DevAccelerationSystem.DemoProject`.

## Fill In
- supported Unity baseline or engine lines
- platform-specific native or runtime constraints
- validation differences across engine versions or consumer projects

## Rules
- Keep current platform assumptions here when they affect routing, validation, or risk.
- Update this file when platform ownership, supported baselines, or native integration expectations change.

## Current Constraints
- `TheBestLoggerSample` consumer workspace must remain workable on Unity `2021`, Unity `2022`, and Unity `6000.3`.
- Sample-scene UI validation is engine-line-sensitive.
  - Built-in font behavior and scene rendering details can differ across supported Unity lines.
  - Do not treat successful code compilation alone as proof that `LoggerSampleScene` is healthy across supported editors.
- Native iOS crash actions are part of the sample surface but require platform-aware validation.
  - They should remain visible and routable in the sample UI.
  - Real execution proof for native crash triggers belongs to iOS player validation, not Editor-only checks.
