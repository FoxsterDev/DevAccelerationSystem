# DevAccelerationSystem Workspace Router

## Purpose
This file is the nested-repo AI entrypoint for `DevAccelerationSystem` inside `AIFoxsterDevHub`.
Use it when a session starts from this nested repo root instead of the hub root.

## Load Order
1. Hub repo router at `../Agents.md`
2. Public `xuunity` core at `../AIRoot/Modules/XUUnity/`
3. Host-local overlay at `../AIModules/XUUnityInternal/` when relevant
4. This file
5. Target project router under this workspace
6. Target project memory under the selected project

## Default Protocol
- Use `xuunity` for implementation, review, SDK work, native work, validation planning, and product-facing engineering explanation.

## Workspace Map
- Canonical package source: `DevAccelerationSystem/`
- Consumer validation projects:
  - `DevAccelerationSystem.DemoProject/`
  - `DAS.LocalProject/`

## Routing Rules
- If a change affects shared package code, edit `DevAccelerationSystem/` first.
- Use `DevAccelerationSystem.DemoProject/` as the default tracked consumer validation workspace.
- Use `DAS.LocalProject/` for local repro or local validation when helpful, but remember it is ignored by this nested repo's `.gitignore`.
- When a task spans package source and a consumer project, keep one implementation target and state the validation target explicitly.

## Git Boundary
- `DevAccelerationSystem/` is its own git repo inside the hub.
- Review and commit changes here from this nested repo, not only from the hub root.
