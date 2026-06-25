# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-06-25

### Changed (breaking — `SchemaVersion` 1 → 2)
- A catalog is now a **single self-contained asset**. `LocalizationCatalog` holds all data inline: `Languages` (`List<LocalizationLocaleProfile>`), `Texts` (`List<LocalizationEntry>`), and `Bools` (`List<LocalizationBoolEntry>`).
- Removed the `LocalizationLocaleSet`, `LocalizationTextTable`, and `LocalizationConfigTable` ScriptableObject wrappers — they were data-only and forced a catalog to span several sibling `.asset` files. The per-table grouping layer is dropped; each `LocalizationEntry` keeps its own `Group`.
- `LocalizationBoolEntry` moved to `Runtime/Model`. Existing catalog assets must be re-authored (e.g. re-run your catalog builder) — there is no automatic migration.

### Added
- `[Tooltip]`s on `LocalizationEntry` (`Key`, `EnglishFallback`, `MaxLength`, `Context`) and `LocalizationBoolEntry`, and a `LocalizationCatalog` inspector that shows a live validity / counts summary.
- Top world languages in `LocalizationLanguageCodes` (English, Chinese Simplified/Traditional, Hindi, Spanish, Arabic, French, Russian, Portuguese + Brazil, Indonesian, German, Japanese, Korean, Turkish, Italian, Vietnamese) with display names; the language-code dropdown now shows friendly labels.
- `[LocalizationKey]` attribute + drawer: any string key field (including `LocalizedText.Key`) renders as a searchable dropdown of the keys available across the project's catalogs, with a raw-text toggle for not-yet-authored keys.
- **Advanced Usage** scanner in the catalog inspector — an on-demand, non-blocking project scan that resolves `Loc.Get`/`GetBool` call sites (literal and `const`-key) plus `LocalizedText` bindings, then reports per-key call counts, used/unused, owning modules (asmdef), files, generic (dynamic-key) calls, and per-platform overrides, with By Key / By Module / By Group / Generic API views and filters.
- Prompt `06_migrate_catalog_schema_v1_to_v2.md`: a no-data-loss v1→v2 migration guide (with the ApperfunHub builder fast-path as the worked example).

### Removed
- `LocalizationEntry.Notes` — merged into the single author-facing `Context` field.

### Notes
- Runtime resolution, validation, the `Loc` API, `LocalizedText`, and remote overrides are unchanged: callers go through `LocalizationCatalog`'s methods, which kept their signatures.

## [0.2.0] - 2026-06-24

### Added
- Per-platform bool configuration: `LocalizationConfigTable` (+ `LocalizationBoolEntry` / `LocalizationBoolValues` with `Default` / `iOS` / `Android` overrides) on `LocalizationCatalog.ConfigTables`, resolved at init for the active platform.
- `Loc.GetBool(key, fallback)` / `Loc.TryGetBool(key, out value)` — fallback-first non-text config lookup.

## [0.1.0] - 2026-06-24

### Added
- Initial open-source release of Loqui.
- Fallback-first runtime API via `Loc` (`Get`, `TryGet`, `SetLanguage`, `ResetToSystemLanguage`, `Apply`, locale formatters, `LanguageChanged` / `Ready` events).
- ScriptableObject catalog (`LocalizationCatalog`, `LocalizationTextTable`, `LocalizationEntry`) with per-language and per-platform (`Default` / `iOS` / `Android`) values.
- `LocalizedText` component and language-picker UI (`LanguageDropdown`, `LanguagePickerController`).
- `JsonUtility`-compatible remote overrides.
- Editor tooling: text scanner, deterministic key generation, approved-attach mode, and build-time validation.
