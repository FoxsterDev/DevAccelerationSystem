# ProjectMemory: DevAccelerationSystem.DemoProject

## Purpose
Durable project-local AI memory for `DevAccelerationSystem.DemoProject`.
Bootstrap status: seeded from `WORKSPACE.md`, repo layout, and current AI routing setup. Verify uncertain claims against source before promoting them into stronger local rules.

## Project Role
- Workspace type: consumer validation workspace
- Shared protocol: `xuunity`
- Project router: `Agents.md`
- Repo router alias: `Agents.repo.md`
- Skill override root: `SkillOverrides/`

## Routing Rules
- Shared package source of truth: `DevAccelerationSystem/DevAccelerationSystem/`
- Use this project for tracked consumer repro and validation.
- Keep tracked integration-validation reports under `Assets/AIOutput/ValidationReports/`.
- Land durable shared package fixes in `DevAccelerationSystem/DevAccelerationSystem/`, not here, unless the issue is demo-project-local.
- Compare demo-project behavior against the canonical package source before deciding ownership of a fix.

## Maintenance Rules
- Keep only durable project-specific truth here.
- If project memory and source disagree, source wins and this memory must be refreshed.
- Store temporary investigations and one-off reports under `Assets/AIOutput/`, not in `ProjectMemory/`.
