# Prompt 1 — Discover UI-Facing Hardcoded Strings In Code

> Copy everything below the line into a fresh agent. Replace `<SURFACE>` with the migration surface (e.g. "Settings menu", "Shop flow") and `<FOLDERS>` with the asset folders to scope the search.

---

You are a principal Unity engineer doing a **read-only discovery pass**. Your job is to find every **UI-facing hardcoded string set or composed in C#** for the surface **`<SURFACE>`**, classify each, and produce a candidate inventory. You DO NOT modify any file in this stage.

## How to find candidates

Trace from **usages of text components and text setters**, not from arbitrary string literals:

1. Run the seed scan: open `Tools/Loqui/Scan Texts`, set Search Folder to `<FOLDERS>`, enable **Include Scripts (advisory)**, **Scan**, then **Export**. This seeds `.text = "..."`, `.text += ...`, `.SetText("...")`, and non-literal code mutators as `ComponentType = CSharpLiteral` items.
   - Treat `RecommendedApproach = ComponentAttach` as the only scanner-approved component path.
   - Treat `RecommendedApproach = CodeApi` as code-priority evidence; inspect the presenter/call site.
   - Treat `RecommendedApproach = Conflict` as a blocker until one owner is chosen.
   - Treat mutator hints as heuristic seed evidence, not full static proof of serialized bindings.
2. Extend the seed with deeper code reasoning the static scanner cannot do. Search `<FOLDERS>` for:
   - assignments to `TMP_Text` / `TextMeshProUGUI` / `UnityEngine.UI.Text` `.text`,
   - `.SetText(...)`, `.text +=`, and TMP rich-text builders,
   - dialog/popup/toast builders and their `title` / `body` / `message` / `button` string params (search for `Show(`, `Dialog`, `Popup`, `Toast`, `Alert`, `Confirm`, `Prompt`),
   - localizable string fields passed into UI view-data structs / `Initialize(...)` methods,
   - `string.Format`, interpolated strings (`$"..."`), and concatenations that end up on a label.

## Classify every candidate

For each string decide `Localize` vs `Exclude` and record the reason:

- **Localize** — player-visible UI copy: labels, button text, dialog titles/bodies, menu items, empty-state and error copy shown to the user, static parts of composed sentences.
- **Exclude** — server/backend strings, analytics/log/debug strings, user input or echoed user data, enum/asset/key/URL/format identifiers, and pure dynamic data (numbers, money, dates → typed `Format*` helpers, not keys). Record which exclusion category applies.
- **Template** — a composed string with placeholders: mark `Localize`, capture the template with tokens intact (`"You won {0} coins"`), and note that only the template is keyed.

Then decide the owner:

- **ComponentAttach** — static authored chrome; no code mutator hint exists. Use `LocalizedText`.
- **CodeApi** — any runtime code writes the label, builds the string, or supplies the ViewData value. Localize where that value is built. `LocalizedText` is not attached.
- **Conflict** — a row already has `LocalizedText` and code also appears to write it. Remove one owner. Code wins unless the mutator can be deleted or proven unrelated.

## Propose keys (must match the tool)

Use `group.slug`: `group` = the surface slug (e.g. `settings`, `shop`); `slug` = lowercase ASCII of the English source, non-alphanumerics collapsed to `_`, diacritics stripped, max 6 words. This mirrors `LocalizationKeyGenerator.Generate`. If two distinct strings collide, append `_2`, `_3` deterministically. Before proposing, check the existing catalog: if a key already holds identical English copy, reuse it.

## Output (review artifact, no mutations)

Produce a Markdown table and a JSON array. Each row/object has:

```
filePath, lineNumber, enclosingTypeAndMember, callKind ( .text = | SetText | dialogParam | format | viewData ),
englishSource, isTemplate, placeholders, proposedKey, group, classification ( Localize | Exclude ),
recommendedApproach ( ComponentAttach | CodeApi | Conflict | Exclude ), mutatorEvidence,
exclusionReason, maxLengthHint, componentPathIfAuthored ( prefab/scene + hierarchy, if the text is
actually a static authored label that should use LocalizedText instead of code ), notes
```

End with a summary: counts of Localize / Exclude / Template, and a flagged list of anything ambiguous that needs a product decision (server-owned? truly static?). **Do not edit code or assets.** Stop and hand the inventory back for human review before Prompt 2.
