# TheBestLogger 3.0.0 AI Upgrade And Log Review Prompt

Use this prompt when an existing Unity project needs both:

- migration of its `TheBestLogger` integration to `3.0.0`
- review of current logging usage against the package best practices

This prompt assumes the package now uses the `3.0.0` public remote-config contract:

- `LogManager.TryApplyRemoteConfigurationPatch(...)`
- `LogManager.TryApplyRemoteConfigurationDocument(...)`

And no longer exposes:

- public `GetCurrentLogTargetConfigurations()`
- public typed runtime-config update APIs based on `LogTargetConfiguration`

## Ready-To-Run Prompt

```text
xuunity review and upgrade this project's TheBestLogger integration to package version 3.0.0.

Migration goals:
- remove usage of deleted public APIs:
  - LogManager.GetCurrentLogTargetConfigurations()
  - LogManager.UpdateLogTargetConfiguration(...)
  - LogManager.UpdateLogTargetsConfigurations(...)
- migrate runtime remote-config flows to:
  - LogManager.TryApplyRemoteConfigurationPatch(...)
  - LogManager.TryApplyRemoteConfigurationDocument(...)
- preserve production behavior where possible
- treat remote-config application as client-facing:
  - handle bool result
  - surface or log returned error text
- if the project previously merged runtime configs by reading current config objects from LogManager, redesign that flow so the app owns its desired patch state locally instead of reading logger internals back

Review goals:
- inspect bootstrap and initialization flow
- inspect whether any logger is created before LogManager.Initialize(...)
- inspect project-local logger facades and wrappers
- inspect category naming and whether categories are stable and human-readable
- inspect log-message quality:
  - useful message text
  - meaningful categories
  - use of LogAttributes where it adds value
  - exception logging that preserves real Exception objects
- inspect hot-path logs for noise, allocation risk, stack-trace misuse, or overly verbose remote delivery
- inspect target configuration and thread-safety coherence
- inspect remote-config integration and whether it follows the 3.0.0 write-only boundary

Best-practice expectations:
- project code does not read live logger runtime configs through a public getter
- project remote-config code normalizes external provider data into:
  - targetName -> rawJsonPatch
- project logger facades cache category loggers instead of recreating them repeatedly
- debug-mode enablement is explicit and uses a stable debug id if needed
- wrapper helpers do not silently remap severities or degrade exceptions to strings too early
- remote-config failures are observable to the client code calling the logger API

Expected output:
- findings first, ordered by severity
- include file references
- clearly separate:
  - migration-required breaks
  - production risks
  - logging-quality issues
  - performance/noise issues
- then implement the migration
- after implementation, summarize:
  - which old APIs were removed from the project
  - how remote-config is now normalized
  - what best-practice issues remain

Use these package files as source of truth:
- Assets/TheBestLogger/README.md
- Assets/TheBestLogger/MIGRATION_3_0_0.md
- Assets/TheBestLogger/AI_Upgrade_And_Log_Review_Prompt_3_0_0.md
- Assets/TheBestLogger/Runtime/Examples/Integration/GenericProjectLoggerExample.cs
- Assets/TheBestLogger/Runtime/Examples/Integration/GenericRemoteConfigAdapterExample.cs
```

## What A Good Upgrade Should Do

1. Replace deleted runtime-config APIs with `Try*` remote-config calls.
2. Move project-specific merge state out of `LogManager` and into project-owned integration code.
3. Keep category logger access simple and cached.
4. Preserve explicit debug-mode enablement where the product needs it.
5. Treat `TryApplyRemoteConfigurationDocument(...)` as atomic and avoid assuming partial success.
6. Log or surface remote-config rejection reasons instead of swallowing them.

## What A Good Log Review Should Answer

1. Are categories stable, readable, and useful for filtering?
2. Are important exceptions passed as real `Exception` objects?
3. Are hot-path logs too noisy or too expensive?
4. Are remote sinks protected from low-value traffic?
5. Are wrapper helpers preserving severity and intent?
6. Is debug-mode usage controlled and intentional?

## Related Package Files

- [TheBestLogger package README](./README.md)
- [Migration Guide 3.0.0](./MIGRATION_3_0_0.md)
- [GenericProjectLoggerExample](./Runtime/Examples/Integration/GenericProjectLoggerExample.cs)
- [GenericRemoteConfigAdapterExample](./Runtime/Examples/Integration/GenericRemoteConfigAdapterExample.cs)
