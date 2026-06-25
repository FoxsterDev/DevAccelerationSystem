# Loqui — AI Migration Prompts

A staged agent workflow for finding hardcoded **UI-facing** text in code, keying it, adding it to the localization catalog, and rewiring the code so the hardcoded literal becomes a **fallback** that an override (a shipped language or a remote payload) replaces. Hand each prompt to an agent in order. Each stage produces a review artifact and stops for human approval before the next.

| Stage | Prompt | Output | Mutates code/assets? |
| --- | --- | --- | --- |
| 1 | [`01_discover_ui_code_strings.md`](01_discover_ui_code_strings.md) | candidate inventory (MD/JSON/CSV) | No |
| 2 | [`02_key_and_catalog.md`](02_key_and_catalog.md) | catalog entries + translation bundle | Catalog asset only |
| 3 | [`03_migrate_code_to_localization.md`](03_migrate_code_to_localization.md) | rewired call sites + mutation report | Yes (approved files only) |
| 4 | [`04_translate_texts_principal.md`](04_translate_texts_principal.md) | studio-quality translated bundle | No |
| 5 | [`05_review_translation_quality.md`](05_review_translation_quality.md) | corrected import-ready bundle + QA report | No |

## The core idea

The runtime API is fallback-first:

```csharp
// before — hardcoded, not localizable
label.text = "Play Now";

// after — localized when a key/override exists, otherwise identical behavior
label.text = Loc.Get("home.play_now", "Play Now");
```

`Loc.Get(key, fallback)` returns the active-language value for `key` when localization is enabled, ready, and the key resolves; otherwise it returns `fallback` verbatim. **The English literal stays in code as the fallback** — so an un-keyed or override-less string keeps its exact current behavior, and there is never a startup or lookup crash. Migration is therefore *safe by construction*: worst case, nothing changes visually.

## Non-negotiable safety contract (applies to every stage)

1. **English literal stays as the fallback argument.** Never delete the source string; pass it to `Get(...)` / `Apply(...)` / `LocalizedText.fallback`. No behavior change when no override exists.
2. **Localize UI copy only.** Never key or rewrite:
   - server-owned / backend-returned strings,
   - analytics, logging, and internal/debug strings,
   - user input or echoed user data,
   - enum names, asset paths, keys, URLs, format identifiers,
   - dynamic data values (numbers, currency, dates) — those use the typed `Format*` helpers, not text keys.
3. **Static template, dynamic data.** For composed text, localize the *template* and keep the data culture-invariant: `string.Format(Loc.Get("rewards.you_won", "You won {0}"), money)`. Preserve every placeholder token (`{0}`, `{name}`) exactly.
4. **Prefer the component path for authored UI.** If a `TMP_Text`/`Text` is static and lives in a prefab/scene, attach `LocalizedText` (via the scanner's approved-attach mode) instead of editing code. Use the code path (`Get`/`Apply`) only for text set or composed in C#.
5. **Code wins ownership.** If code writes to a text component at runtime, localize at that code path with `Loc.Get(key, fallback)` or typed `Format*` helpers. Do not also attach `LocalizedText` to that label until the mutator is removed or explicitly made non-owner.
6. **Keys are stable and additive.** Reuse an existing catalog key for identical English copy; never rename or repurpose a shipped key. New keys only.
7. **Code keys live near the feature owner.** For strings used from C#, add per-feature `static` const-key classes next to the presenter/builder that owns the copy. Match the class grouping to the catalog entry's `Group`.
8. **Keep models localization-neutral.** Models expose raw data and semantic IDs/enums. Presenter/ViewData owns key selection, fallback text, formatting, and language-change rehydration for dynamic UI copy.
9. **Human review gates every stage.** Stage 1 and 2 output is reviewed before stage 3 touches code.
10. **Validate before declaring done.** Let Unity import, run the `Loqui.Tests` EditMode suite plus your own catalog-gate test, and build for your targets. See each prompt's validation section.

## Tooling the prompts build on

- **Scanner window:** `Tools/Loqui/Scan Texts` (`LocalizationScannerWindow`). Toggle **Include Scripts (advisory)** to seed code-literal candidates via `LocalizationCSharpScanner` (`.text = "..."` and `.SetText("...")`). Exports MD + JSON + CSV + a translation bundle. Every row includes `RecommendedApproach`:
  - `ComponentAttach` — no code mutator hint was found; use `LocalizedText`.
  - `CodeApi` — a code mutator hint was found; localize the presenter/call site.
  - `Conflict` — both component localization and a code mutator appear to target the row; resolve to one owner.
  - `Exclude` — empty, already localized without conflict, or not a localizable candidate.
- **Key generator:** `LocalizationKeyGenerator.Generate(group, source)` — `group.slug`, lowercase ASCII, `_`-joined, diacritics stripped, max 6 words. Agents must follow the same rules so proposed keys match the tool.
- **Approved attach mode:** `LocalizationAttachMode` attaches `LocalizedText` only to `RecommendedApproach = ComponentAttach` scene/prefab rows and returns a mutation record per processed row.
- **Runtime API:** `Loc.Get/TryGet/Apply`, `LocalizedText`, `LanguageDropdown`, `LanguagePickerController`, and `Format{Number,Currency,Percent,ShortDate,DateTime}`.
- **Catalog model:** a single self-contained `LocalizationCatalog` ScriptableObject holds everything inline — `Languages` (`List<LocalizationLocaleProfile>`), `Texts` (`List<LocalizationEntry>`), and `Bools` (`List<LocalizationBoolEntry>`). A `LocalizationEntry` is (`Key`, `EnglishFallback`, `Languages[ LocalizationLanguageValue{ LanguageCode, Values{Default,IOS,Android} } ]`, `Group`, `MaxLength`, `Context`). There is no separate locale-set / text-table / config-table asset; `Group` lives on each entry.

## Migration order (highest value first)

Pick the order that matches your product's value. A common ordering: 1. Settings + language picker. 2. Update / force-update dialogs. 3. Consent, support, FAQ, privacy, terms. 4. Purchase / checkout flow. 5. Home / main screen. 6. Tutorial. 7. Activity / rewards.

Migrate one surface per pass; validate green before the next.
