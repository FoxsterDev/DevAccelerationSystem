<!-- Managed by AIRoot/scripts/init_ai_project.sh -->
# Project Agent Router: DevAccelerationSystem.DemoProject

## Purpose
This file is the project-level routing layer.
Keep it short. Route the work first, then load the minimum required protocol files.

## Load Order
1. Local repo router alias `Agents.repo.md` if available, otherwise repo-level `../../Agents.md`
2. Shared protocol modules from `../../AIRoot/Modules/` and optional host-local prompt families from `../../AIModules/` when that root exists. Use local alias `AIModules/` only for host-local families when it exists and mirrors the same structure.
3. This project file
4. Project memory from `Assets/AIOutput/ProjectMemory/`
5. Existing project outputs from `Assets/AIOutput/`

After loading this router, always verify the real on-disk prompt structure before opening files. Do not assume older flat paths still exist.


## Project Context
- Project: `DevAccelerationSystem.DemoProject`
- Engine context: Unity project
- Project kind: `unity_package_validation_demo`
- Priority: Stability > Performance > Maintainability
- Project memory path: `Assets/AIOutput/ProjectMemory/`
- AI output path: `Assets/AIOutput/`

## Routing

### `xuunity`
Use `xuunity` as the default protocol for project work, including bug fixing, refactoring, feature development, code review, SDK integration, SDK review, native plugin work, native plugin review, Unity runtime safety, JNI, Objective-C, Swift, performance audits, and store compliance.

Load in this order:
1. `../../AIRoot/Modules/XUUnity/role/base_role.md`
2. One or more relevant files from `../../AIRoot/Modules/XUUnity/codestyle/`
3. One task file from `../../AIRoot/Modules/XUUnity/tasks/`
4. One or more review or utility files from `../../AIRoot/Modules/XUUnity/reviews/` or `../../AIRoot/Modules/XUUnity/utilities/`
5. Platform files from `../../AIRoot/Modules/XUUnity/platforms/` only if relevant
6. Monorepo-internal `xuunity` overlay from `../../AIModules/XUUnityInternal/` only when it exists and is relevant to the task
7. Project memory from `Assets/AIOutput/ProjectMemory/`
8. Relevant prior analysis outputs from `Assets/AIOutput/`

### `host-local protocols`
Use host-local protocol families only when the repo explicitly attaches them under `../../AIModules/`.
Load those families from verified on-disk paths instead of assuming any named private modules.

## Override Rules
- Project memory in `Assets/AIOutput/ProjectMemory/` overrides shared prompts when there is a conflict.
- Follow the repo-level AI output storage rule from `../../Agents.md` instead of redefining local storage semantics.
- For feature work, bug fixing, refactoring, and code review, load durable guidance from `Assets/AIOutput/ProjectMemory/` by default.
- Load historical analysis outputs from `Assets/AIOutput/` only for behavior investigation, legacy reconstruction, or old bug research.
- For shared prompts, prefer repo-level canonical files and validate their actual folder layout before loading.
- If legacy text conflicts with this router, this router wins.
