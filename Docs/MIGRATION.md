# Migration guide

## Current package layout

No package has moved. Existing URLs with `?path=/DevAccelerationSystem/Assets/<package>` remain the installation route for the current release line.

## Future standard-layout migration

A future major release may move package roots to `Packages/com.foxsterdev.*`. Before that happens, release notes will state the old and new Git URLs, GUID preservation plan, migration steps, and clean-install plus upgrade evidence. Consumers must not switch paths before that release is published.

For TheBestLogger's existing public API migration, see its package-local `MIGRATION_3_0_0.md`. Loqui remains pre-1.0; its catalog schema migration is documented under `Documentation~/Prompts/`.
