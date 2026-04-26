# Testing Strategy

## Purpose
Define the durable validation expectations for `DevAccelerationSystem`.

## Test Surface
- Editor tests for tooling-oriented source:
  - `Assets/DevAccelerationSystem/Tests/Editor/`
- Editor tests for logger package source:
  - `Assets/TheBestLogger/Tests/Editor/`
- PlayMode tests for logger runtime behavior:
  - `Assets/TheBestLogger/Tests/PlayMode/`
- Performance tests for logger hot paths and frame-time-sensitive behavior:
  - `Assets/TheBestLogger/Tests/Performance/`

## Current Test Focus Seen In Source
- build target utilities
- command-line argument parsing
- logger target behavior
- stack-trace formatting
- utility supplier behavior
- multithreaded logger stress paths
- dispatch and batch runtime behavior
- platform-target runtime-path harnesses
- OpenSearch configuration-compatibility and delivery-contract checks
- background file writer shutdown and stress behavior
- `StabilityHub` runtime behavior
- package performance measurements through Unity Performance Testing
- tracked consumer validation in `DevAccelerationSystem.DemoProject/`

## Validation Layers
- Deterministic editor tests:
  - use for local logic, configuration behavior, target filtering, merge semantics, and fault-isolation rules
- PlayMode runtime tests:
  - use for frame progression, dispatch-to-main-thread behavior, batch flushing, and runtime target execution paths
- Performance tests:
  - use for allocation-sensitive and frame-time-sensitive package behavior with explicit measurement tooling
- Tracked consumer validation:
  - use `DevAccelerationSystem.DemoProject/` for package-wiring proof and integration-sensitive runtime flows
- Physical device or native-platform proof:
  - still required before claiming full platform confidence for native log targets or device-facing performance behavior

## Validation Rules
- Validate narrow package or tooling logic in editor tests first when the change stays inside one source surface.
- Validate runtime-facing logger changes in package playmode tests before relying on consumer evidence alone.
- Validate performance-sensitive logger changes in the dedicated performance suite before making performance claims.
- Validate cross-surface or integration-sensitive changes in a representative consumer workspace after source edits.
- Prefer `DevAccelerationSystem.DemoProject/` as tracked consumer evidence.
- Use `DAS.LocalProject/` for local repro or fast validation when helpful, but do not treat it as tracked release proof by default.
- For `TheBestLogger`, production-facing confidence should be layered:
  - editor tests for deterministic behavior
  - playmode tests for runtime paths
  - performance tests for measured package hot paths
  - tracked consumer validation for package wiring
  - physical device proof for native or platform-specific claims
- When a change touches platform-native targets, prefer internal bridge seams or equivalent test seams so `Log()` and `LogBatch()` runtime paths can be proven without changing the public API.

## Evidence Rules
- Editor tests are good proof for deterministic logic and editor tooling behavior.
- PlayMode tests are good proof for runtime dispatch, batching, frame-based behavior, and execution-path safety inside Unity runtime.
- Performance tests are good proof for package-local allocation and frame-time regression tracking inside the measured environment, but they are still not a substitute for physical-device perf truth.
- Consumer validation is required evidence for integration-sensitive changes, package wiring, and runtime-facing flows.
- Editor and PlayMode evidence for platform targets is stronger than helper-only tests, but it is still not equal to physical device or native-log observability proof.
