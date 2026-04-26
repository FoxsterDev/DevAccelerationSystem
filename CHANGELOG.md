# Changelog

## TheBestLogger

## [2.2.14] - 2025-11-19
- Added `[HideInCallstack]` to the `LogTrace` extension method to improve Unity Console navigation.

## [2.2.13] - 2025-09-02
- Added the `LogTrace` extension method for editor and development builds.
- Improved formatting and added allocation-aware `LogFormat` overload support.
- Added caller-opening improvements for Unity Console navigation.

## [2.2.11] - 2025-07-09
- Added `AndroidSystemLogTarget`.
- Added log attributes to Unity Editor console target output.
- Added direct Unity Console notification when the OpenSearch target fails.

## [2.2.10] - 2025-06-13
- Fixed `System.FormatException` when formatting messages with subcategories.

## [2.2.9] - 2025-05-22
- Hid OpenSearch API keys in serialized example payloads.
- Added fallback logger support.

## [2.2.8] - 2025-05-15
- Added logger subcategories.
- Added profiler markers for target updates.
- Reduced allocations in batch-processing flow.

## [2.2.7] - 2025-01-08
- Fixed stack trace formatter exception path.

## [2.2.6] - 2024-12-29
- Added max-length truncation for log messages and stack traces.

## [2.2.5] - 2024-12-29
- Added optional `ZString` stack-trace integration.
- Added `UniTask` unobserved-exception log source.
- Added `SafeThirdPartyLogTarget`.

## [2.2.4] - 2024-12-08
- Removed `Debug.isDebugBuild` from a path that could execute off the Unity main thread.

## [2.2.2] - 2024-11-26
- Added `StabilityHub` with iOS crash-reporting support.
- Added Apple unified log target support.
- Fixed log-source ordering.

## [2.2.0] - 2024-10-23
- Added scoped log tags.
- Added dispatching from background threads to the main thread.
- Changed log-level resolution priority to prefer debug mode, then category overrides, then minimum level.

## [2.1.0] - 2024-10-07
- Added stack trace configuration by level.
- Improved remote config application.
- Fixed known issues from early October 2024 iteration.

## [2.0.1] - 2024-09-26
- Added public API to get and set log target configurations for remote changes.
- Included minor styling cleanup.

## [2.0.0] - 2024-09-22
- Added the preview release of `TheBestLogger` for Unity projects.

## DevAccelerationSystem

## [1.0.1] - 2024-05-06
- Autoupdate compilation output when new compilation is done. Added a flag into ProjectCompilationConfig to open th Compilation viewer automatically when compilation is finished
- Updated menuitems 
- Updated readme. Added a section about batchmode
- Tested on Windows and MacOS

## [1.0.0] - 2024-05-04
This is the first release of DevAccelerationSystem with one included package to check compilation errors for your custom set of scripting define symbols.
