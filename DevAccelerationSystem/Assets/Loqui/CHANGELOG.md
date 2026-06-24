# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-06-24

### Added
- Initial open-source release of Loqui.
- Fallback-first runtime API via `Loc` (`Get`, `TryGet`, `SetLanguage`, `ResetToSystemLanguage`, `Apply`, locale formatters, `LanguageChanged` / `Ready` events).
- ScriptableObject catalog (`LocalizationCatalog`, `LocalizationTextTable`, `LocalizationEntry`) with per-language and per-platform (`Default` / `iOS` / `Android`) values.
- `LocalizedText` component and language-picker UI (`LanguageDropdown`, `LanguagePickerController`).
- `JsonUtility`-compatible remote overrides.
- Editor tooling: text scanner, deterministic key generation, approved-attach mode, and build-time validation.
