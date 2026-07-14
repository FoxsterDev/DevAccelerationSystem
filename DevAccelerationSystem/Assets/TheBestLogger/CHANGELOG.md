# Changelog

## Unreleased

- Fixed the performance-test assembly reference to `ZString`, allowing the suite to compile when its public utility API exposes `Utf8ValueStringBuilder`.

## [4.4.0] - 2026-07-11
- Hardened log-source, diagnostics, update-loop, and target delivery boundaries so consumer or target exceptions cannot escape into application callbacks.
- Added per-target failure isolation, recursive-log suppression, and automatic quarantine after repeated target failures.
- Added bounded batch and main-thread dispatch queues with importance-aware eviction, dropped-log telemetry, and guaranteed retained-payload progress under sustained overload.
- Kept batching and main-thread dispatch decorators installed across the target lifetime so remote configuration can safely enable either behavior after initialization.
- Clamped unsafe remote batch/update values and aligned the built-in development and production presets with non-thread-safe target dispatch requirements.
- Disabled crash-reporter auto-reporting by default so applications must opt in explicitly.
- Preserved automatic `THEBESTLOGGER_ZSTRING_ENABLED` activation in `TheBestLogger.Core.Utilities` and the assemblies that directly consume ZString-backed types.
- Expanded Editor, PlayMode, stress, overflow, quarantine, remote-configuration, and fallback-builder performance coverage.

## [4.3.0] - 2026-07-10
- Fixed captured exceptions forwarded by the global async/unhandled sources (`UnobservedUniTaskExceptionLogSource`, `UnobservedTaskExceptionLogSource`, `CurrentDomainUnhandledExceptionLogSource`) so they no longer ship with an empty `Message`; the message is now derived from the exception through `LogMessageFormatter`.
- Changed the emitted `Category` for captured exceptions from the default `Uncategorized` bucket to the originating log-source id so unhandled-exception logs are groupable per source. Non-exception logs keep their existing category.
- Added structured exception attributes (`ExceptionType`, `Fingerprint`) for every captured exception. `AggregateException` is unwrapped to its inner exception first, so Task-sourced faults type and group by the real exception instead of collapsing to `System.AggregateException`. `Fingerprint` is a stable FNV-1a hash of the exception type plus the first non-framework stack frame, intended as a group-by key in log backends. Attribute preparation is exception-safe and never throws.
- Kept non-exception log records byte-identical (message, category, and attributes unchanged); only exception-carrying records are enriched, and no public API or call site changed.

## [3.0.1] - 2026-05-04
- Fixed Apple-system logger native bridge compilation on Apple player targets by restoring valid access to the imported native entry points.
- Fixed `OpenSearchLogTarget` diagnostics error logging so player builds no longer reference the editor-only reflective Unity Console logger.
- Fixed the `StabilityHub` iOS crash-reporting preprocess build step so builds no longer throw when the monitoring configuration asset is missing.
- Fixed tracked demo-project sample assembly wiring so the crash-reporting postprocess script stays editor-only and the `UniTask` exception sample resolves the `UniTask` assembly correctly.

## [3.0.0] - 2026-05-03
- Breaking change: removed the public `GetCurrentLogTargetConfigurations()` API.
- Breaking change: removed the public typed runtime-update APIs based on `LogTargetConfiguration` objects.
- Added `TryApplyRemoteConfigurationPatch(string targetName, string rawJsonPatch, out string error)` as the single-target public remote-config entrypoint.
- Added `TryApplyRemoteConfigurationDocument(IReadOnlyDictionary<string, string> rawJsonPatches, out string error)` as the batch public remote-config entrypoint.
- Added explicit client-facing error reporting for remote-config application through `bool` + `out string error`.
- Made batch remote-config document application atomic so mixed valid/invalid batches are rejected without partial apply.
- Added package-owned generic integration examples for:
  - app-level logger facade
  - remote-config normalization into `targetName -> rawJsonPatch`
- Added `MIGRATION_3_0_0.md` as a human-readable upgrade guide for existing project integrations.
- Added a package-owned AI prompt for upgrading project integrations to `3.0.0` and reviewing log usage against package best practices.
- Updated `README.md` with generic integration wiring examples and references to both the human migration guide and the package-owned AI migration/review prompt.
- Hardened remote-config application so malformed or rejected public patches are not persisted into startup cache.
- Hardened target apply flow so one bad applied config restores the previous target state or falls back to a muted quarantine configuration instead of poisoning logger runtime state.
- Hardened `IMGUIRuntimeLogTargetConfiguration` numeric inputs and `IMGUIRuntimeLogTarget` fallback behavior against semantically invalid external config values.
- Added regression coverage for public-API hardening, cache behavior, snapshot isolation, and invalid remote-config inputs.

## [2.2.15] - 2026-05-03
- Added broad hardening coverage across editor, playmode, performance, and tracked consumer-validation surfaces.
- Added production-oriented regression coverage for logger lifecycle, concurrency, batching, main-thread dispatch, target fault isolation, `OpenSearch` config compatibility, delivery behavior, and `StabilityHub`.
- Added performance measurements through Unity Performance Testing.
- Added repository-level logger integration and audit docs for public use.
- Added direct scripted bootstrap paths and sample-scene support for switching between resource-driven, QA, and production logger initialization flows.
- Added `DebugMode.SessionDebugRolloutPercentage` for per-target session-random debug activation.
- Added `LogTargetCategory.SessionRolloutPercentage` for per-category rollout-gated target overrides, including `DebugMode.OverrideCategories`.
- Replaced Unity global-random rollout usage with deterministic rollout sampling so logger rollout decisions no longer advance shared `UnityEngine.Random` state.
- Made `DebugMode` rollout sticky for the current logger session and explicit `debugId` activation target-specific.
- Added runtime configuration startup-cache overlay support for target patches across launches.
- Updated remote-config behavior so partial nested `DebugMode` patches preserve absent fields, partial target patches keep absent values intact, and `DebugMode.Enabled = false` turns target debug off immediately.
- Expanded logger README guidance with concrete partial `OpenSearchLogTargetConfiguration` remote-patch scenarios, rollout examples, and category-rollout partial-patch cases.
- Fixed generic `LogFormat` forwarding for multi-argument overloads.
- Fixed batch snapshot and duplicate-delivery risks in concurrent batch-drain paths.
- Fixed Apple exception-message handling and safer crash-path intake behavior.
- Fixed `AppleSystemLogTarget` payload decoration so default empty `LogAttributes` no longer change plain batch/runtime messages.
- Fixed async-disposal deadlock risk in `FileBackgroundAsyncWriter`.
- Fixed `TaskExtensions.HandleExceptions(...)` fallback scheduling so missing or unusable `SynchronizationContext` no longer emits an internal exception log before falling back.
- Fixed `UnityDebugLogSource` handler replacement to be instance-safe under repeated test or runtime setup.
- Fixed repeated pre-initialization usage so `LogManager` warns only once before a valid initialize.

## [2.2.14] - 2025-11-19
- Added `[HideInCallstack]` to the `LogTrace` extension method to improve click-through behavior in the Unity Console.

## [2.2.13] - 2025-09-02
- Deprecated the older `LogFormat(LogLevel logLevel, string message, LogAttributes logAttributes = null, params object[] args)` path in favor of the current `LogFormat` usage.
- Added the `LogTrace` extension method for Unity Editor and development-build-only trace logging.
- Improved message formatting and added allocation-aware `LogFormat` overload support.
- Added aggressive inlining so Unity Console navigation opens closer to the caller site.

## [2.2.11] - 2025-07-09
- Added `AndroidSystemLogTarget`.
- Added log-attribute rendering in the Unity Editor console target.
- Added direct Unity Console notification when the OpenSearch target fails because of a network issue.

## [2.2.10] - 2025-06-13
- Fixed `System.FormatException` in `LogMessageFormatter` when subcategory formatting is used.

## [2.2.9] - 2025-05-22
- Hid the API key in the OpenSearch example target serialization path.
- Added a fallback logger for initialization or configuration failure paths.

## [2.2.8] - 2025-05-15
- Added subcategory support when creating loggers.
- Added `ProfilerMarker` instrumentation for log-target update profiling.
- Reduced batch-processing allocations.
- Performed general cleanup around target update flow.

## [2.2.7] - 2025-01-08
- Fixed an exception in `StackTraceFormatter` that could surface as `IndexOutOfRangeException` while formatting nested exception traces.

## [2.2.6] - 2024-12-29
- Added max-length truncation for log messages and log stack traces.

## [2.2.5] - 2024-12-29
- Made `ZString` optional and used it for stack-trace construction when the package is present.
- Added `UniTask` unobserved-exception log source support.
- Added `SafeThirdPartyLogTarget`.
- Added unit tests and stack-trace fixes for nested exceptions.

## [2.2.4] - 2024-12-08
- Removed `Debug.isDebugBuild` from a path that could execute off the Unity main thread.
- Fixed `UnityException: get_isDebugBuild can only be called from the main thread`.
- Moved unsafe initialization expectations away from constructor and field-initializer paths.

## [1.0.0] - 2024-10-06
- Added stack-trace configuration for Unity application log types and specific log targets.
- Added the option to inherit Unity Editor Console log target behavior and customize print messages.
- Added explicit configuration-name access for log targets.
- Fixed remote-config application behavior by clearing stale values before merging known properties.

## [0.0.9] - 2024-09-22
This is the first release of TheBestLogger as a package.
