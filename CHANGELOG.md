# Changelog

This is the repository timeline. The canonical changelog for each release line is package-local: [Dev Acceleration System](./DevAccelerationSystem/Assets/DevAccelerationSystem/CHANGELOG.md), [TheBestLogger](./DevAccelerationSystem/Assets/TheBestLogger/CHANGELOG.md), and [Loqui](./DevAccelerationSystem/Assets/Loqui/CHANGELOG.md).

## Unreleased

- Established package-specific metadata, documentation, validation, and future tag strategy without publishing a release.
- Fixed Dev Acceleration System's build-target check to use Unity's public API and fixed its successful configuration-create return value.
- Isolated Logger's reflective Editor-console implementation from player compilation.

## [4.4.0] - 2026-07-11

Repository snapshot containing `com.foxsterdev.devaccelerationsystem` `1.0.1`, `com.foxsterdev.thebestlogger` `4.4.0`, and `com.foxsterdev.loqui` `0.3.0`. This is a historical global tag; future releases use package-specific tags.

## [4.0.0] - 2026-06-24

Introduces the **Loqui** localization package (`com.foxsterdev.loqui` 0.1.0) and hardens it per a full-SDK review.

### Loqui — added
- Fallback-first localization for Unity: `Loc.Get(key, literal)` keeps the code literal as the default and never throws on a missing key.
- Per-language and per-platform (`iOS`/`Android`) overrides from a ScriptableObject catalog; TMP integration (`LocalizedText`), language dropdown, runtime font swap.
- Locale-aware number/currency/percent/date formatting; `JsonUtility` remote overrides; editor scanner with deterministic key generation.

### Loqui — hardening (full-SDK review remediation)
- Logging decoupled behind a package-owned `ILoquiLog`; `TheBestLogger` is now optional via a version-define-gated adapter (`LOQUI_THEBESTLOGGER`), not a hard dependency.
- Packaging: declares `com.unity.ugui` + `com.unity.textmeshpro`; added a Unity-recognized `link.xml` for IL2CPP managed stripping; Unity floor raised to `2022.3`.
- Opt-in remote-override apply (`Loc.ApplyOverrides` / `ClearOverrides`) with per-language and per-platform (`iOS`→`IOS`) resolution that survives language switches.
- `Loc.Ready` is level-triggered (late subscribers fire); `LocalizationEvent.Raise` is re-entrancy-safe; main-thread contract documented and asserted in editor/development builds.
- `FormatCurrency` caches its `NumberFormatInfo` and groups by the currency's country (`USD`/`BRL`/`GBP`/`JPY`); non-ASCII source no longer produces colliding keys; the C# scanner ignores comments; scan order and key disambiguation are deterministic.
- `LanguageDropdown` lifecycle null-guards; `LocalizationService` constructor is internal (`Loc` is the entry point); `SetLanguage` no-ops on the already-active language; active-table dictionary allocated with a capacity hint.
- Added EditMode hardening tests and a DemoProject PlayMode lane; author prompts moved to `Documentation~`.
- Verified on Unity 2022.3.62f3: `Loqui.Tests` EditMode suite 96/96 passing. PlayMode and an IL2CPP+stripping device build remain to be run.

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
