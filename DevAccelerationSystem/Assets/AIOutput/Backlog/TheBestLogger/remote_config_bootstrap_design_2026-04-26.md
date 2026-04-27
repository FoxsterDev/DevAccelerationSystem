# TheBestLogger Remote Config Bootstrap Design

Date: `2026-04-26`
Scope: `TheBestLogger`
Components:
- `LogManager`
- `LogTargetConfiguration`
- `OpenSearchLogTarget`
Status: `design only`

## Problem

`TheBestLogger` currently boots from built-in `Resources` configuration and only later accepts runtime target updates.

That creates two production-facing problems:
- early-session logs can be emitted with stale built-in settings before remote overrides arrive
- operators cannot treat remote config as the true runtime source of truth across sessions

Observed product impact from field notes:
- categories such as `LoadingFunnel` can emit thousands of avoidable logs per hour before remote overrides land
- the resulting monthly volume can become materially expensive
- incident analysis becomes noisy because startup behavior depends on remote-config timing instead of deterministic bootstrap rules

## Review Findings

### 1. No startup bootstrap path exists for cached remote-effective config
- Severity: `high`
- Evidence:
  - `LogManager.Initialize(...)` always loads `LogManagerConfiguration` from `Resources` and immediately applies `configuration.LogTargetConfigs`
  - there is no package-local load of previously cached runtime overrides before the first targets and log sources are active
- Files:
  - [LogManager.Public.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogManager.Public.cs)
- Production impact:
  - unavoidable early-session noise
  - nondeterministic startup behavior depending on remote-config arrival timing
  - poor control over expensive remote sinks

### 2. Current merge semantics cannot reliably express "clear overrides"
- Severity: `high`
- Evidence:
  - `LogTargetConfiguration.Merge(...)` replaces `OverrideCategories` only when `Length > 0`
  - an empty remote array cannot intentionally clear built-in or previously resolved overrides
- Files:
  - [LogTargetConfiguration.cs](../../../../Assets/TheBestLogger/Runtime/Core/Configuration/LogTargetConfiguration.cs)
- Production impact:
  - remote control is incomplete for category-level suppression
  - a persistence layer built on top of current patch semantics can preserve stale category overrides unexpectedly

### 3. Runtime update surface only covers target configs, not full manager bootstrap policy
- Severity: `medium`
- Evidence:
  - runtime API is `UpdateLogTargetsConfigurations(Dictionary<string, LogTargetConfiguration>)`
  - startup also applies top-level manager policy such as Unity logger filtering and log-source subscription
- Files:
  - [LogManager.Public.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogManager.Public.cs)
- Production impact:
  - target thresholds can change at runtime, but the full startup logging policy cannot be reproduced from the same remote source
  - future consumers may assume "remote controls everything" when it currently controls only target config

### 4. Effective runtime policy is difficult to inspect during production investigation
- Severity: `medium`
- Evidence:
  - `GetCurrentLogTargetConfigurations()` returns current config objects, but there is no explicit package-level API or report describing effective source, active debug state, or per-category effective threshold
- Files:
  - [LogManager.Public.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogManager.Public.cs)
  - [LogTarget.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogTarget/LogTarget.cs)
- Production impact:
  - when expected `Error` or `Exception` logs do not appear, diagnosis requires code inspection instead of direct runtime evidence

### 5. OpenSearch debug-state telemetry must represent actual runtime state, not config capability
- Severity: `closed locally, keep in plan until merged`
- Evidence:
  - previous payload mapping used `Configuration.DebugMode.Enabled` instead of the target's actual debug-enabled state
- Files:
  - [OpenSearchLogTarget.cs](../../../../Assets/TheBestLogger/Runtime/Examples/LogTargets/OpenSearch/OpenSearchLogTarget.cs)
- Production impact:
  - production analytics can over-report logs as debug-enabled even when the target was not actually in debug mode

## Design Goals

- make startup behavior deterministic across sessions
- let remote policy override built-in target settings before first meaningful log emission
- keep built-in configs as a safe fallback when cache is missing or corrupt
- avoid widening public API more than necessary
- preserve compatibility with existing target config assets and current runtime update usage
- provide enough observability to explain why a log category was or was not emitted

## Non-Goals

- redesigning the full public logger API
- replacing the current remote-config transport used by consumers
- making all top-level `LogManagerConfiguration` fields remotely mutable in phase 1
- claiming physical device proof or final production hardening from this design alone

## Recommended Design

### 1. Use three configuration layers

Apply target configuration in this order:
1. built-in `Resources` config
2. cached effective remote config from previous successful session
3. fresh remote config received during current session

Rules:
- built-in config remains the fallback base
- cached config is treated as last-known effective truth for startup
- fresh remote config replaces cached truth after validation

### 2. Persist effective target snapshots, not raw patch payloads

Phase-1 recommendation:
- persist the fully resolved effective `LogTargetConfiguration` snapshot per target after remote has already been merged and validated by the integration layer
- do not persist raw partial remote patches in phase 1

Reasoning:
- current `Merge(...)` semantics do not safely distinguish omitted arrays from intentional empty arrays
- persisting resolved snapshots avoids replaying ambiguous patch semantics on the next app launch
- this keeps bootstrap behavior deterministic even if upstream remote transport uses partial updates

### 3. Add a package-local cache store for target snapshots

Recommended storage path:
- `Application.persistentDataPath/TheBestLogger/ConfigCache/`

Recommended files:
- one manifest file for version and bookkeeping
- one JSON file per target config type, keyed by `LogTargetConfigurationName`

Recommended manifest fields:
- schema version
- saved-at UTC timestamp
- package version when saved
- list of target config file names

Write rules:
- validate config before save
- write to temp file first, then replace atomically when possible
- if save fails, continue using in-memory config without crashing logging

Read rules:
- if cache is missing, boot from built-ins
- if one cached target is corrupt, ignore only that target and keep booting
- log diagnostics about cache rejection without failing startup

### 4. Bootstrap cached target snapshots before targets become active

Recommended startup flow:
1. load built-in `LogManagerConfiguration` from `Resources`
2. convert built-in target configs to runtime config dictionary
3. load cached effective target configs from persistent storage
4. overlay cached effective target configs onto the built-in dictionary
5. apply the resulting dictionary to targets
6. decorate targets and subscribe sources
7. later apply fresh remote target configs and persist the new effective snapshot

This design keeps the startup path deterministic without requiring a new public initialization API.

### 5. Keep phase-1 remote scope limited to target configs

Phase 1 should explicitly cover:
- `MinLogLevel`
- `OverrideCategories`
- `Muted`
- batching and dispatch target options
- stack-trace target options
- target-specific fields such as OpenSearch host, API key, index prefix, and timeout once supported
- `DebugModeConfiguration`

Phase 1 should explicitly not promise remote control of:
- `UnityEngine.Debug.unityLogger.filterLogType`
- top-level log-source enablement
- `DefaultUnityLogsCategoryName`
- `MinTimestampPeriodMs`
- `MinUpdatesPeriodMs`

Those belong either to:
- a later phase with a separate top-level remote bootstrap contract
- or a deliberate product decision that startup policy stays local

### 6. Make debug-mode semantics explicit

Rules:
- `DebugModeConfiguration.Enabled` means the feature may be used by matching IDs
- actual active debug mode is the runtime state after ID matching
- OpenSearch telemetry must report actual active runtime state
- after target config changes, runtime debug state must be recomputed using the current debug ID

### 7. Add an effective-config diagnostic surface

Minimum phase-1 requirement:
- be able to produce a concise diagnostics snapshot for each target:
  - target name
  - config source used at startup: built-in or cached remote
  - current active debug state
  - base `MinLogLevel`
  - category overrides count
  - whether the target is muted

This can be:
- a small debug report API
- or a diagnostics log emitted once after initialization in diagnostics-enabled builds

The point is not UI polish.
The point is production explainability.

## Merge Strategy Guidance

Phase-1 persistence should not depend on improving public `Merge(...)`.

Recommended approach:
- integration layer resolves remote patch plus local base into an effective target snapshot
- package persists that effective snapshot
- next session bootstrap reuses the effective snapshot directly

Optional later phase:
- introduce a dedicated patch DTO model that can represent field omission versus explicit clearing without ambiguity
- use that DTO only for remote-transport ingestion, not for the persisted effective snapshot

## Validation Plan

### Required editor tests

- cached target snapshot loads before built-in config is applied to targets
- missing cache falls back to built-in config
- corrupt cache for one target does not poison other targets
- effective snapshot save and reload preserves:
  - `MinLogLevel`
  - empty and non-empty `OverrideCategories`
  - `DebugMode`
  - OpenSearch-specific fields
- runtime target update persists new effective snapshot
- debug state is recomputed after runtime config update

Suggested suites:
- `LogManagerCachedConfigBootstrapTests`
- `LogTargetConfigurationPersistenceTests`
- `OpenSearchCachedConfigPersistenceTests`

### Required playmode or consumer validation

- consumer boots with cached low-noise config and does not emit the known early-session noisy category before fresh remote arrives
- remote update during runtime changes target behavior and survives restart
- error and exception delivery remain present when `MinLogLevel = Error` and debug mode is inactive

Suggested target:
- `DevAccelerationSystem.DemoProject/`

## Rollout Plan

### Step 1
- merge the debug-state telemetry fix and the debug-state recomputation fix
- treat this as observability correction, not as the full remote-bootstrap solution

### Step 2
- add package-local effective target snapshot persistence
- bootstrap from cached effective target snapshots before target activation

### Step 3
- add diagnostics snapshot for effective runtime config

### Step 4
- validate in tracked consumer project with a noisy startup category such as `LoadingFunnel`

## Open Questions

- Is the upstream remote system already capable of delivering full effective target snapshots, or only partial patches?
- Should cache expiration exist, or is "latest valid effective snapshot wins" sufficient for phase 1?
- Should cached config be scoped by environment, user cohort, or app flavor when the same package ships in multiple variants?
- Does product want remote control to remain target-only, or should a later phase include top-level `LogManagerConfiguration` bootstrap policy?

## Direct Recommendation

Do not go straight from the current notes into implementation of persistence without first locking the contract above.

The safest phase-1 implementation target is:
- persist effective target snapshots
- bootstrap them before first target activation
- keep top-level manager policy out of scope
- add diagnostics that explain the active target policy at runtime

That gives immediate cost and observability value without pretending the package already supports a full remote-managed logging policy.
