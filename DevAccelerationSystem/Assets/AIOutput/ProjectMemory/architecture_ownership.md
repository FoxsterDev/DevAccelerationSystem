# Architecture Ownership

## Purpose
Capture the durable ownership boundaries inside `DevAccelerationSystem`.

## Current Source Surfaces
- `Assets/TheBestLogger/`
  - package-style logging surface
  - runtime plus editor, playmode, and performance-test-backed logger functionality
- `Assets/DevAccelerationSystem/`
  - project/editor/tooling-oriented surface
  - editor code, shell scripts, and editor-test-backed support utilities
- `Assets/Scenes/`
  - workspace scene content
- `Assets/AIOutput/ProjectMemory/`
  - durable project-local memory for package ownership, validation doctrine, and release expectations
- `../Docs/`
  - public repository-facing logger guidance and audit prompts

## Ownership Rules
- Changes to logger package behavior should start under `Assets/TheBestLogger/`.
- Changes to editor tooling, project compilation checks, or workspace automation should start under `Assets/DevAccelerationSystem/`.
- Changes to public logger integration guidance should start under `../Docs/` and stay aligned with package README and source-backed behavior.
- If a change touches both trees, preserve the narrowest owner for each behavior instead of collapsing package and tooling concerns together.

## Validation Implications
- Logger behavior changes require logger test review across editor, playmode, and when relevant performance suites, plus consumer validation planning.
- Tooling or editor behavior changes require editor-test review and, when relevant, project-level validation in a consumer workspace.

## Consumer Relationship
- Canonical source lives here.
- Representative consumers:
  - `DevAccelerationSystem.DemoProject/`
- Optional local-only consumer:
  - `DAS.LocalProject/`
