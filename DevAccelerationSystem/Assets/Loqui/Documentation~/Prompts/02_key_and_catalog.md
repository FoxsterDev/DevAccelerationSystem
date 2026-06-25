# Prompt 2 — Key The Approved Strings And Add Catalog Entries

> Input: the **approved** `Localize`/`Template` rows from Prompt 1 (excluded rows dropped, ambiguous rows resolved by product). Copy below the line into a fresh agent.

---

You are a principal Unity engineer turning approved discovery rows into **catalog entries** and a **translation bundle**. You may modify the localization catalog assets only — no code, no scenes, no prefabs in this stage.

## Add entries to the catalog

The shipped data is a single self-contained `LocalizationCatalog` ScriptableObject that holds everything inline: `Languages`, a flat `Texts` list of `LocalizationEntry`, and a flat `Bools` list. There are no separate locale-set / text-table assets — add each entry to `Texts` and set its `Group` to the surface slug. For keys consumed from C#, add or update a per-feature const-key class next to the presenter, builder, or ViewData owner that will call `Loc.Get`. Keep the class grouped 1:1 with the entry `Group`. Do not put localization keys on domain models.

For each approved row add a `LocalizationEntry`:

- `Key` — the approved `proposedKey`. Keys are unique across the whole catalog; if the scanner / `LocalizationCatalog.IsValid` reports a duplicate, the duplicate is wrong — reuse the existing entry instead of adding a second.
- `EnglishFallback` — the exact English source (the template, with placeholders, for composed text).
- `Languages` — add the `en` value (`LocalizationLanguageValue { LanguageCode = "en", Values.Default = <source> }`). Leave target languages empty here; they are filled from the translation bundle after human review.
- `Group` — the surface slug.
- `MaxLength` — the visible-character budget before the UI clips/overflows, from discovery (0 = none). Always fill it when discovery found a width or character constraint; it gates translation length downstream.
- `Context` — **always fill this** (it is the only author/translator-facing note now that `Notes` is merged in). One or two lines covering both where/how the string appears *and* placeholder semantics for templates (e.g. `{0}` = coin amount). An empty `Context` produces worse translations — never leave it blank for a non-trivial string.
- Platform variants (`Values.IOS` / `Values.Android`) only when iOS and Android copy genuinely differ.

**English is the hard fallback** — every entry must keep its English value; never add an entry that removes or empties English.

## Produce the translation bundle

Export the bundle from `Tools/Loqui/Scan Texts` (or build it for just these keys). The bundle is `JsonUtility`-shaped (`[Serializable]` classes, `string`/`int` fields, arrays — no `Dictionary`, no properties, no top-level arrays) so it round-trips through the remote contract. Each entry carries `Key, Group, Source, Context, MaxLength, TargetLanguage` and empty `TargetDefault/TargetIOS/TargetAndroid` for the translator to fill.

Rules for the bundle:

- Keep every placeholder token byte-identical between `Source` and the translation.
- Respect `MaxLength` so translated copy will not clip/overflow.
- Do not translate brand names, in-game proper nouns, or format identifiers.
- **Human review is required** before any AI translation is written back into the catalog's target-language values. The bundle is a proposal, not an import.

## Validate

1. Let Unity reimport assets and resolve packages.
2. Run the `Loqui.Tests` EditMode suite plus your project's catalog-gate test, which runs `LocalizationValidator` over your build catalog(s) and **fails on any error** (invalid catalog, missing default language, duplicate keys, missing English). Green = the new entries are well-formed.

Output: the list of added keys (with group + English), the path to the translation bundle, and any keys you reused instead of adding. Stop for human review before Prompt 3.
