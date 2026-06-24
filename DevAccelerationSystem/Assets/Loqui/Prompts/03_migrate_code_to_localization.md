# Prompt 3 — Rewire Code To Localization (Literal Stays As Fallback)

> Input: the approved keys from Prompt 2 (already in the catalog) and the discovery rows with their `filePath`/`lineNumber`/`callKind`. Copy below the line into a fresh agent.

---

You are a principal Unity engineer performing a **behavior-preserving migration**. For each approved row, replace the hardcoded UI string with a localization lookup **keeping the original English literal as the fallback argument**. If no override exists, behavior is byte-identical. Touch only the files listed in the approved set; produce a mutation report.

Respect the approved owner from discovery:

- `ComponentAttach` — attach/configure `LocalizedText` on the authored label; do not add a C# lookup.
- `CodeApi` — call `Loc.Get` / `Format*` where the presenter, builder, or ViewData creates the text; do not attach `LocalizedText` to the same target.
- `Conflict` — resolve to one owner before mutating. Code wins unless the mutator is removed or proven unrelated.
- `Exclude` — do not migrate.

## Rewrite patterns

**Simple label / setter** — pass the literal as fallback:
```csharp
// before
label.text = "Play Now";
title.SetText("Settings");
// after
label.text = Loc.Get("home.play_now", "Play Now");
title.SetText(Loc.Get("settings.title", "Settings"));
```

**Composed / formatted text** — localize the template, keep the data invariant, preserve tokens:
```csharp
// before
status.text = $"You won {coins} coins";
// after
status.text = string.Format(Loc.Get("rewards.you_won_coins", "You won {0} coins"), coins);
```
For numbers/money/dates use the typed helpers for the data, the key for the template:
```csharp
amount.text = Loc.FormatCurrency(value, "USD");
won.text = string.Format(Loc.Get("rewards.you_won", "You won {0}"),
                         Loc.FormatCurrency(value, "USD"));
```

**Dialog / popup / view-data params** — wrap each user-visible argument at the call site:
```csharp
ShowDialog(Loc.Get("update.title", "Update available"),
           Loc.Get("update.body",  "A new version is ready."));
```

When a screen uses ViewData, the presenter/builder should assemble the localized strings and pass ready-to-render values to the view. Models must stay localization-neutral and expose raw data or semantic IDs/enums, not localized strings or localization keys.

## Refresh on language change

`Loc.Get(...)` is evaluated when the line executes — it does not auto-update if the player switches language while the text is already on screen. For code-driven dynamic text this is fine (it re-runs when the value changes). For code-driven **static** labels that stay on screen across a language switch, either prefer the `LocalizedText` component, or re-apply in the view's `Loc.LanguageChanged` handler. Never scan the scene to refresh.

## Hard rules

- Never delete the English literal — it is the fallback. No behavior change without an override.
- Do not migrate anything classified `Exclude` in Prompt 1 (server/analytics/log/input/dynamic).
- Keep placeholder tokens byte-identical. Do not reorder format arguments.
- Add `using Loqui;` where needed. The consuming assembly must reference the `Loqui` asmdef — add the reference if missing.
- Use per-feature const-key classes for code-owned keys, placed near the presenter/builder that owns the copy and grouped to match the catalog table. Do not scatter raw key literals through models.
- One surface per pass. Keep the diff minimal and mechanical; no opportunistic refactors.

## Validate — required before declaring done

1. Let Unity reimport assets and resolve packages.
2. Compile for one desktop target — expect 0 errors.
3. Run the `Loqui.Tests` EditMode suite plus your project's catalog-gate test — all green.
4. Build for your target platforms (e.g. Android + iOS, across your build profiles).
5. (Interactive) Open the migrated surface in `en`, switch language via the picker, confirm migrated copy updates (component path) or re-renders on next show (code path), and that the missing-key report is empty for this surface.

Output: a mutation report — one row per change (`filePath`, `lineNumber`, `key`, `before` → `after`, or `attachedLocalizedText`), plus the validation results. List any call sites you skipped and why.
