# ProjectMemory: DevAccelerationSystem

## Purpose
Durable project-local AI memory for `DevAccelerationSystem`.
Bootstrap status: seeded from `WORKSPACE.md`, repo layout, and current AI routing setup. Verify uncertain claims against source before promoting them into stronger local rules.

## Project Role
- Workspace type: canonical package source
- Shared protocol: `xuunity`
- Project router: `Agents.md`
- Repo router alias: `Agents.repo.md`
- Skill override root: `SkillOverrides/`

## Verified Current Claims
- Primary package identity currently present in source: `com.foxsterdev.thebestlogger`
- Package version: `2.2.4`
- Declared Unity baseline in package manifest: `2022.3`
- Package source root currently present: `Assets/TheBestLogger/`
- Additional source surface present under `Assets/DevAccelerationSystem/`
- Test surface present:
  - `Assets/DevAccelerationSystem/Tests/Editor/`
  - `Assets/TheBestLogger/Tests/Editor/`
- Consumer validation targets currently mapped in the hub:
  - `DevAccelerationSystem.DemoProject/`
  - `DAS.LocalProject/`

## Memory Files
- `architecture_ownership.md`
- `feature_surface.md`
- `known_issues.md`
- `testing_strategy.md`
- `platform_constraints.md`
- `release_rules.md`
- `SkillOverrides/README.md`

## Routing Rules
- Shared package source of truth: `DevAccelerationSystem/DevAccelerationSystem/`
- Use this project for shared package code changes, package-facing tests, manifests, and durable implementation ownership.
- Validate behavior changes in representative consumer workspaces after editing here.
- Related consumers:
  - `DevAccelerationSystem/DevAccelerationSystem.DemoProject/`
  - `DevAccelerationSystem/DAS.LocalProject/`

## Maintenance Rules
- Keep only durable project-specific truth here.
- If project memory and source disagree, source wins and this memory must be refreshed.
- Store temporary investigations and one-off reports under `Assets/AIOutput/`, not in `ProjectMemory/`.
- Update this file first when package identity, supported Unity baseline, canonical-source ownership, or validation-target mapping changes.
