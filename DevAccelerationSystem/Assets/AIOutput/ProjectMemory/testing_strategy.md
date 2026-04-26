# Testing Strategy

## Purpose
Define the durable validation expectations for `DevAccelerationSystem`.

## Test Surface
- Editor tests for tooling-oriented source:
  - `Assets/DevAccelerationSystem/Tests/Editor/`
- Editor tests for logger package source:
  - `Assets/TheBestLogger/Tests/Editor/`

## Current Test Focus Seen In Source
- build target utilities
- command-line argument parsing
- logger target behavior
- stack-trace formatting
- utility supplier behavior

## Validation Rules
- Validate narrow package or tooling logic in editor tests first when the change stays inside one source surface.
- Validate cross-surface or integration-sensitive changes in a representative consumer workspace after source edits.
- Prefer `DevAccelerationSystem.DemoProject/` as tracked consumer evidence.
- Use `DAS.LocalProject/` for local repro or fast validation when helpful, but do not treat it as tracked release proof by default.

## Evidence Rules
- Editor tests are good proof for deterministic logic and editor tooling behavior.
- Consumer validation is required evidence for integration-sensitive changes, package wiring, and runtime-facing flows.
