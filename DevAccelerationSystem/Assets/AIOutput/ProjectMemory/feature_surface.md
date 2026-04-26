# Feature Surface

## Purpose
Capture the durable capability map for `DevAccelerationSystem` so product-facing and engineering-facing explanations do not have to reconstruct the package from source every time.

## Package Runtime Surface
- `Assets/TheBestLogger/Runtime/Core/`
  - owns `LogManager`, logger creation, log-target configuration, log-source wiring, stack-trace formatting, and utility helpers
- The package is a reusable runtime logging surface, not only an editor helper
- `LogManager` should be initialized from the Unity main thread before category loggers are used

## Captured Log Sources
- Unity debug logs
- Unity application logs, including threaded application logs
- unobserved `Task` exceptions
- unobserved `UniTask` exceptions
- current-domain unhandled exceptions
- optional `System.Diagnostics` debug and console sources

## Target And Delivery Surface
- Built-in platform-facing targets:
  - Unity Editor console
  - Android system log target
  - Apple unified logging target
- Support utilities:
  - target muting
  - per-category minimum log levels
  - debug-mode overrides
  - stack-trace enablement by level
  - batch-log decoration
  - main-thread dispatch decoration
  - background file writing utility
- Example or integration-oriented targets:
  - IMGUI runtime viewer
  - OpenSearch target
  - safe third-party target base class

## Stability Surface
- `Assets/TheBestLogger/Runtime/StabilityHub/`
  - optional stability-oriented integration layer
  - current source includes iOS crash-reporter wiring and previous-session issue retrieval
- Treat this as runtime behavior, not only a sample-only artifact, when explaining package capabilities

## Tooling Surface
- `Assets/DevAccelerationSystem/Editor/`
  - owns editor and workflow utilities
  - current durable tooling surface includes project compilation check menus, configuration assets, and compilation runners
- Keep tooling explanations separate from logger-runtime explanations unless the user is explicitly asking how the workspace supports validation or package authoring

## Consumer And Validation Surface
- Shared package source is authored in `DevAccelerationSystem/DevAccelerationSystem/`
- Tracked consumer validation target is `DevAccelerationSystem.DemoProject/`
- `DAS.LocalProject/` is useful local evidence but not tracked release proof by default

## Product Explanation Rules
- When asked what this project does, describe both:
  - the reusable `TheBestLogger` runtime package
  - the `DevAccelerationSystem` editor and workflow tooling surface
- Do not collapse the project into only a logger package or only a tooling workspace
- For runtime-capability questions, verify source when the answer depends on exact active configuration or platform-specific behavior
