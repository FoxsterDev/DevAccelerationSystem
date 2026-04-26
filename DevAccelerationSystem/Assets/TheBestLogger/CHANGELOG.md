# Changelog

## [Unreleased]
- Added broad hardening coverage across editor, playmode, performance, and tracked consumer-validation surfaces.
- Added production-oriented regression coverage for logger lifecycle, concurrency, batching, main-thread dispatch, target fault isolation, `OpenSearch` config compatibility, delivery behavior, and `StabilityHub`.
- Added performance measurements through Unity Performance Testing.
- Added repository-level logger integration and audit docs for public use.
- Fixed generic `LogFormat` forwarding for multi-argument overloads.
- Fixed batch snapshot and duplicate-delivery risks in concurrent batch-drain paths.
- Fixed Apple exception-message handling and safer crash-path intake behavior.
- Fixed async-disposal deadlock risk in `FileBackgroundAsyncWriter`.
- Fixed `UnityDebugLogSource` handler replacement to be instance-safe under repeated test or runtime setup.

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
