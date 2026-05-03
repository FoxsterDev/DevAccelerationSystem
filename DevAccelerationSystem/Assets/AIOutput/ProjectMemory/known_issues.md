# Known Issues

## Purpose
Capture durable, recurring issues that repeatedly affect engineering work, package integration, or product-facing explanation quality in `DevAccelerationSystem`.

## Recording Rules
- Record an issue here only when it is durable enough to affect repeated work.
- One-off investigations and temporary breakages belong in `Assets/AIOutput/`, not here.
- If an issue is fixed and no longer shapes repeated work, remove it instead of leaving stale ballast.

## Current Durable Known Issues
- Unity test execution without a proper Unity MCP or similarly reliable editor-control layer is not a good use of turns here.
  - Symptoms seen in this workspace:
    - `-runTests` may fail to emit usable results for PlayMode.
    - ad hoc batchmode/test-runner workarounds can get stuck on domain reloads, project locks, or partial callback delivery.
    - repeated reruns consume time without materially increasing confidence.
  - Working rule:
    - do not keep hammering Unity batchmode when results control is unstable.
    - prefer code changes, test authoring, and explicit validation gaps until a stable Unity MCP path is available.

## When To Update
- Add an entry when the same runtime, packaging, validation, or consumer-integration issue appears often enough that future sessions should know it up front.
