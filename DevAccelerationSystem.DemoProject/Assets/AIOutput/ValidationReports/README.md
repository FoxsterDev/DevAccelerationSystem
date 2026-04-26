# Validation Reports

## Purpose
Store tracked consumer-validation evidence for `DevAccelerationSystem.DemoProject`.

## Use This Folder For
- integration-sensitive validation runs
- runtime or package-wiring checks performed in the demo consumer
- tracked evidence that supports release-facing or regression-facing claims for shared package changes

## Naming
- Prefer `integration_validation_YYYY-MM-DD.md` for normal tracked reports
- Prefer one report per reviewed integration-sensitive change or validation pass

## Rules
- Keep durable validation rules in `ProjectMemory/`
- Keep mutable validation evidence here
- Do not treat local-only `DAS.LocalProject/` evidence as equivalent to tracked demo-project validation
