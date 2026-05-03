# Review: TheBestLogger session debug rollout top-practice refactor

Date: 2026-05-03

## Findings

### 1. High: existing rollout configuration is not migrated, so package upgrades silently drop rollout to `0`

The rollout field was removed from `DebugModeConfiguration` and added to `LogManagerConfiguration`, but there is no migration path for existing serialized assets or cached target patches.

Evidence:
- `DebugModeConfiguration` no longer contains `RolloutPercentage`: `Assets/TheBestLogger/Runtime/Core/Configuration/DebugModeConfiguration.cs`
- new field lives only in `LogManagerConfiguration.SessionDebugRolloutPercentage`: `Assets/TheBestLogger/Runtime/Core/Configuration/LogManagerConfiguration.cs`
- cached target patches still use the old target-patch document shape and schema version stays `1`: `Assets/TheBestLogger/Runtime/Core/Configuration/LogTargetConfigurationCacheStore.cs`

Impact:
- existing shipped `LogManagerConfiguration.asset` files default the new field to `0`
- existing target configs that previously carried rollout percentage lose that behavior after upgrade
- cached remote patches that previously contained `DebugMode.RolloutPercentage` also lose effect after upgrade

This is a behavior regression on upgrade, not just a documentation change.

### 2. Medium: rollout percentage is no longer remotely controllable through the public runtime update path

After the refactor, the only runtime update APIs operate on `LogTargetConfiguration`, while the rollout percentage now lives on `LogManagerConfiguration`.

Evidence:
- rollout setting moved to `LogManagerConfiguration.SessionDebugRolloutPercentage`: `Assets/TheBestLogger/Runtime/Core/Configuration/LogManagerConfiguration.cs`
- public runtime update APIs only accept target configurations or raw target JSON patches: `Assets/TheBestLogger/Runtime/Core/LogManager.Public.cs`
- startup cache persists target patches only: `Assets/TheBestLogger/Runtime/Core/Configuration/LogTargetConfigurationCacheStore.cs`

Impact:
- ops can no longer raise or lower rollout from remote config without shipping a new `LogManagerConfiguration` asset
- startup cache can no longer persist a remote rollout change across restart

This may be acceptable if explicitly intended, but it is a product-surface regression relative to the previous target-level rollout control.

## Open questions

1. Is losing remote control over rollout percentage an intentional product decision, or should there be a manager-level remote patch path?
2. Is one-time upgrade migration required for existing package consumers that already configured rollout in target assets?

## Risk assessment

- Breakage probability for existing consumers already using rollout: high
- Breakage probability for consumers that only use explicit `debugId`: low
- Runtime stability risk for fresh installs with manually updated assets: low

## QA and validation recommendations

1. Upgrade an existing sample/project that has non-zero target rollout in serialized assets and verify rollout still behaves as intended after package update.
2. Test startup-cache restore on an upgrade path where the previous cache contains target-level rollout fields.
3. Validate whether product requirements still include remote rollout tuning. If yes, add a manager-level runtime patch/update path and corresponding persistence story.

## Residual risk

Even if the runtime logic is internally consistent now, the upgrade path and remote-operations story are not yet closed.
