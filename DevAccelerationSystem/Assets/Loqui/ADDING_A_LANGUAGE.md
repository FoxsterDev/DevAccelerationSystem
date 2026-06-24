# Loqui — Adding a Language

How to add a new language to a Loqui catalog and validate it end-to-end. A human or an AI agent can follow these steps verbatim.

## Non-negotiable principles

1. **English (`en`) is the hard fallback.** Never remove it, never disable it, never let a change drop it. Every key must keep an English value.
2. **No runtime crash, ever.** Missing key / missing language / missing font / disabled localization / service-not-ready must all fall back, never throw. Validate before shipping; do not rely on runtime.
3. **JSON stays JsonUtility-compatible.** The remote contract (`Remote/LocalizationOverridesDto`) is parsed with `UnityEngine.JsonUtility`. Only `[Serializable]` classes, `string`/`int` fields, and arrays of those — no `Dictionary`, properties, polymorphism, or top-level arrays.
4. **Remote can override copy for existing keys only.** It cannot introduce new keys or assets in V1.
5. **Do not convert dynamic/server-owned or analytics strings into static keys.** Localize product-facing UI copy only.
6. **Composition, not a god asset.** Locale/font config, text, and orchestration are separate ScriptableObjects.

## Architecture (where things live)

The package namespace is `Loqui` (runtime asmdef `Loqui`, editor drawer in `Loqui.Editor`, tests in `Loqui.Tests`). The runtime facade is `Loc` — each project calls `Loc.Initialize(...)` once from its own bootstrap. Project-specific glue (e.g. build-config validation) lives in **your** project and references the package.

- `LocalizationLocaleSet` (ScriptableObject) — the languages: code, display name, native name, culture name, font profile, enabled flag. Owns locale-level validation.
- `LocalizationTextTable` (ScriptableObject) — one per feature/screen group (e.g. `settings`, `shop`). Holds `LocalizationEntry` rows: key, English fallback, per-language + per-platform values, context, max-length.
- `LocalizationCatalog` (ScriptableObject) — **the orchestrator.** References one locale set + N text tables, exposes the unified lookup/validation API, detects duplicate keys across tables. Holds no raw data itself.
- `LocalizationSettingsScope` — your project points it at the `LocalizationCatalog` and holds `EnabledByDefault` + `DefaultLanguageCode`; set it on your build configuration.
- `LocalizationLanguageResolver` — pure system-language → code mapping + user-choice priority.
- `LocalizationValidator` — runtime-safe validator usable from a build pre-step and from EditMode tests.
- Code-owned localization keys live in per-feature const-key classes next to the presenter, builder, or ViewData owner that calls `Loc.Get`. The grouping should match the catalog table `Group`. Domain models do not own localization keys or localized strings.

## Steps to add a language (example: French `fr-FR`)

1. **Code constant + system mapping**
   - Add the code to `LocalizationLanguageCodes` (e.g. `public const string French = "fr-FR";`) and append it to `LocalizationLanguageCodes.All` (the source of truth for `IsKnown` and the inspector language-code drawer).
   - Add the `SystemLanguage` case in `LocalizationLanguageResolver.MapSystemLanguage` (e.g. `case SystemLanguage.French: return LocalizationLanguageCodes.French;`).

2. **Font coverage**
   - Ensure a TMP font asset covers the language's glyphs; reference it in the locale's font profile. Reuse an existing font if coverage is sufficient.
   - Add platform overrides only if iOS and Android genuinely need different fonts.
   - RTL languages (Arabic/Hebrew) are out of scope and need a separate RTL font + layout design first.

3. **Locale profile**
   - Open the `LocalizationLocaleSet` asset. Add a `LocalizationLocaleProfile`: `LanguageCode = fr-FR`, `DisplayName`, `NativeDisplayName`, `CultureName = fr-FR`, `FontProfile.PrimaryFont = <tmp font>`, `Enabled = true`.

4. **Translations**
   - For each relevant `LocalizationTextTable`, add the new language value to every key (`LocalizationLanguageValue { LanguageCode = "fr-FR", Values.Default = "..." }`).
   - Keep the English fallback intact. Set platform-specific values only when the platform copy differs.
   - Respect each entry's `MaxLength` hint to avoid clipping/overflow.

5. **(Optional) remote overrides** — copy-only, existing keys only, JsonUtility-shaped, schema version 1.

## Validate

1. Let Unity import the new assets/scripts and resolve packages.
2. Run the `Loqui.Tests` EditMode suite (Window ▸ General ▸ Test Runner) — expect all green. In your project, add a catalog-gate test that runs `LocalizationValidator` over your build catalog(s) and **fails on any error** (catalog invalid, default language missing, no enabled languages). Warnings (e.g. missing font) need not fail it.
3. Build for your target platforms and confirm the build succeeds.

Optionally run `LocalizationValidator` at build time via an `IPreprocessBuildWithReport` in your project, so the build is **blocked** on any localization error. In the editor you can validate manually via menu `Tools/Loqui/Validate`.

## Culture-aware formatting on IL2CPP

`Loc.FormatNumber` / `FormatCurrency` / `FormatPercent` / `FormatShortDate` / `FormatDateTime` resolve a `CultureInfo` from each locale's `CultureName`, which on IL2CPP depends on ICU/globalization data:

- Do **not** enable **Player ▸ Use Invariant Culture** (`System.Globalization.Invariant`) if you rely on culture-aware number/currency/date formatting. Under invariant globalization named cultures collapse to the invariant culture, so grouping, decimal separators, and currency layout silently fall back to invariant output. Text localization itself is unaffected.
- `FormatCurrency` groups by the **currency's** country for `USD`, `BRL`, `GBP`, `JPY`; other currencies (including `EUR`) group by the active UI culture. The currency symbol is always applied.
- The editor (Mono) always has full ICU and is **not** representative — verify formatting on a real IL2CPP device build for your shipped locales.

## Acceptance

- All EditMode tests green, including your configured-catalog gate.
- Builds green for your target platforms.
- No missing-glyph boxes on priority screens for the new language (visual check / interactive smoke).
- English fallback still present and enabled.
