# Skill Overrides

## Purpose
Add project-specific overrides only when `DevAccelerationSystem` needs to narrow or refine shared `xuunity` skill behavior.

## Allowed Override Files
- `async.md`
- `ui.md`
- `ui_tweens.md`
- `sdk.md`
- `native.md`
- `performance.md`
- `tests.md`
- `architecture.md`

## Rules
- Add an override only when the shared rule is correct in general but incomplete for this project.
- Keep each override narrowly scoped and evidence-backed.
- If an override weakens a shared safety rule, document the rationale and the local constraint clearly.
- Delete stale overrides instead of letting them silently drift.
