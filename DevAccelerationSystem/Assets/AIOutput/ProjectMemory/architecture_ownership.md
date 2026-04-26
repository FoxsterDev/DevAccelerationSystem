# Architecture Ownership

## Purpose
Capture the durable ownership boundaries inside `DevAccelerationSystem`.

## Current Source Surfaces
- `Assets/TheBestLogger/`
  - package-style logging surface
  - runtime plus editor-test-backed logger functionality
- `Assets/DevAccelerationSystem/`
  - project/editor/tooling-oriented surface
  - editor code, shell scripts, and editor-test-backed support utilities
- `Assets/Scenes/`
  - workspace scene content

## Ownership Rules
- Changes to logger package behavior should start under `Assets/TheBestLogger/`.
- Changes to editor tooling, project compilation checks, or workspace automation should start under `Assets/DevAccelerationSystem/`.
- If a change touches both trees, preserve the narrowest owner for each behavior instead of collapsing package and tooling concerns together.

## Validation Implications
- Logger behavior changes require logger test review and consumer validation planning.
- Tooling or editor behavior changes require editor-test review and, when relevant, project-level validation in a consumer workspace.

## Consumer Relationship
- Canonical source lives here.
- Representative consumers:
  - `DevAccelerationSystem.DemoProject/`
  - `DAS.LocalProject/`
