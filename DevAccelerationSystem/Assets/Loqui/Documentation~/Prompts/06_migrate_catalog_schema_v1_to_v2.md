# Prompt 6 — Migrate A Catalog From Schema v1 (Separate Assets) To v2 (Single Self-Contained Catalog)

> One-time migration when upgrading Loqui from the multi-asset catalog (`SchemaVersion 1`) to the single self-contained catalog (`SchemaVersion 2`). Run it once per project, then delete the temporary migration scripts. Copy below the line into a fresh agent.

---

You are a principal Unity engineer migrating an existing Loqui localization catalog from **schema v1** to **schema v2** **without losing any data**. You may modify only the localization catalog asset, add/remove temporary editor migration scripts, and re-pin the package — no gameplay code, scenes, or prefabs.

## What changed (and why data is at risk)

- **v1:** `LocalizationCatalog` referenced separate ScriptableObjects — `LocalizationLocaleSet`, `LocalizationTextTable`(s), `LocalizationConfigTable`(s) — each a sibling `.asset` file.
- **v2:** `LocalizationCatalog` is one self-contained asset holding everything inline: `Languages` (`List<LocalizationLocaleProfile>`), `Texts` (`List<LocalizationEntry>`), `Bools` (`List<LocalizationBoolEntry>`). The three wrapper SO types are **deleted from the package**.
- The leaf data classes are **unchanged** across versions: `LocalizationLocaleProfile`, `LocalizationEntry`, `LocalizationLanguageValue`, `LocalizationPlatformValues`, `LocalizationFontProfile`, `LocalizationBoolEntry`, `LocalizationBoolValues`. So migration is a **flatten**, not a re-type.
- **The risk:** once v2 is resolved, the v1 wrapper types no longer exist. The old catalog's `Locales` / `TextTables` / `ConfigTables` fields stop deserializing and the sibling `.asset` files become *missing-script* — i.e. unreadable. **You must capture the data while still on v1.**

## Non-negotiable safety contract

1. **Capture before upgrade.** Never re-pin to v2 before the v1 data is captured into the holder asset. This is the whole game.
2. **Copy objects, never retype text.** Move the live `LocalizationEntry` / `LocalizationLocaleProfile` / `LocalizationBoolEntry` objects. Retyping strings risks losing invisible characters (e.g. `U+2028` line separators) and breaks placeholder tokens. Object-copy preserves them, and preserves `TMP_FontAsset` references (which a JSON/text round-trip would drop).
3. **Keep the catalog GUID stable.** Migrate **into the existing catalog asset** (same `.meta` GUID) so every `Catalog`-field reference (build configs, settings scopes) still resolves. Do not create a new catalog asset.
4. **English stays the hard fallback.** Every entry keeps its `EnglishFallback`; the locale set keeps English.
5. **Git checkpoint.** Commit or stash before starting; the rollback is `git restore`.
6. **Builder-generated catalogs take the fast path.** If the catalog is produced by a project build script (the script is the source of truth, not the asset), do **not** run the capture/holder dance below — re-pin to v2 and re-run the builder, which writes the v2 shape directly. See the ApperfunHub worked example next.

## Worked example — ApperfunHub (builder-generated, the fast path)

ApperfunHub is the reference integration. Its catalog `Assets/_Hub/Configs/Localization/BlingzLocalizationCatalog.asset` is **generated** by `BlingzLocalizationCatalogBuilder` (13 text keys + 2 bool flags, an iOS store-review reskin) and is referenced by GUID from three build profiles (`DevBuild` / `ReleaseDebug` / `ReleaseStore`) in `BlingzBuildConfiguration.asset`. Because the builder is the source of truth, the v1→v2 migration is just:

1. **Re-pin** `com.foxsterdev.loqui` to `#4.2.0` in `ApperfunHub/Packages/manifest.json`; let Unity resolve. (During package development, point it at the local Loqui via a `file:` dependency instead, then switch back to the git pin before closeout.)
2. **Re-run the builder.** `Tools/Loqui/Build Blingz Catalog`, or the MCP project action `localization.build_blingz_catalog` (`allowMutating: true`). It reuses the existing catalog asset (**GUID stays `7b3bb87…`**, so the three build-config references hold), writes `Languages` / `Texts` / `Bools` inline (`SchemaVersion 2`), and deletes the three loose v1 siblings (`BlingzLocaleSet` / `BlingzReskinTextTable` / `BlingzReskinConfigTable`).
3. **Validate:** `localization.validate_config` = 0 errors; the build-config compile matrix passes 6/6 (`Android`+`iOS` × the three profiles); `_Hub.Localization.Tests` is green; the 8 `LocalizedText` prefab keys still resolve; the catalog GUID is unchanged. The `U+2028` in `tutorial.earn_real_money` survives because the builder emits it as the `\u2028` C# escape (serialized as YAML `\L`).

No data capture is needed for ApperfunHub — re-running the builder *is* the migration. The procedure below is for projects whose catalog was **hand-authored in the Inspector** (no builder), where the asset itself is the only copy of the data.

## Stage 1 — Capture the v1 data (while still on the v1 package)

Add two temporary editor scripts. The **holder** compiles on both versions (it only touches unchanged leaf types); the **export** script compiles on v1 only.

`Assets/Editor/LoquiMigrationHolder.cs` (keep through the upgrade):
```csharp
using System.Collections.Generic;
using Loqui;
using UnityEngine;

public sealed class LoquiMigrationHolder : ScriptableObject
{
    public int SchemaVersionFrom;
    public List<LocalizationLocaleProfile> Languages = new();
    public List<LocalizationEntry> Texts = new();
    public List<LocalizationBoolEntry> Bools = new();
}
```

`Assets/Editor/LoquiMigrationExport.cs` (delete this one **before** upgrading — it references v1-only members):
```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loqui;
using UnityEditor;
using UnityEngine;

public static class LoquiMigrationExport
{
    const string HolderPath = "Assets/LoquiMigrationHolder.asset";
    const string BaselinePath = "Assets/LoquiMigrationBaseline.txt";

    [MenuItem("Tools/Loqui/Migration/1 - Capture v1 Catalog")]
    public static void Capture()
    {
        var catalog = ResolveCatalog();
        if (catalog == null) return;

        var holder = ScriptableObject.CreateInstance<LoquiMigrationHolder>();
        holder.SchemaVersionFrom = catalog.SchemaVersion;
        if (catalog.Locales != null && catalog.Locales.Languages != null)
            holder.Languages = new List<LocalizationLocaleProfile>(catalog.Locales.Languages);
        if (catalog.TextTables != null)
            foreach (var t in catalog.TextTables)
                if (t != null && t.Entries != null) holder.Texts.AddRange(t.Entries);
        if (catalog.ConfigTables != null)
            foreach (var c in catalog.ConfigTables)
                if (c != null && c.Bools != null) holder.Bools.AddRange(c.Bools);

        AssetDatabase.CreateAsset(holder, HolderPath);

        var sb = new StringBuilder();
        sb.AppendLine("catalogGuid=" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(catalog)));
        sb.AppendLine("languages=" + holder.Languages.Count + " [" + string.Join(",", holder.Languages.Select(l => l?.LanguageCode)) + "]");
        sb.AppendLine("texts=" + holder.Texts.Count);
        sb.AppendLine("bools=" + holder.Bools.Count);
        foreach (var e in holder.Texts.OrderBy(e => e.Key)) sb.AppendLine("text:" + e.Key + " len=" + (e.EnglishFallback?.Length ?? 0));
        foreach (var b in holder.Bools.OrderBy(b => b.Key)) sb.AppendLine("bool:" + b.Key);
        System.IO.File.WriteAllText(BaselinePath, sb.ToString());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LoquiMigration] Captured {holder.Languages.Count} languages, {holder.Texts.Count} texts, {holder.Bools.Count} bools.");
    }

    static LocalizationCatalog ResolveCatalog()
    {
        if (Selection.activeObject is LocalizationCatalog selected) return selected;
        var guids = AssetDatabase.FindAssets("t:LocalizationCatalog");
        if (guids.Length == 1) return AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        Debug.LogError($"[LoquiMigration] Select the LocalizationCatalog asset first ({guids.Length} found).");
        return null;
    }
}
```

Run **Tools/Loqui/Migration/1 - Capture v1 Catalog** (select the catalog first if the project has more than one). Confirm `LoquiMigrationHolder.asset` and `LoquiMigrationBaseline.txt` were written and the logged counts match what you expect. **Record the baseline counts and key list — they are the no-loss yardstick.**

## Stage 2 — Upgrade the package

1. Delete `Assets/Editor/LoquiMigrationExport.cs` (it will not compile on v2).
2. Re-pin the package to the v2 version in `Packages/manifest.json`, e.g. `#4.2.0`, and let Unity re-resolve.
3. Expect the old sibling `.asset` files (locale set / text tables / config tables) to now show **missing script**, and the catalog's old data to read as empty. This is normal — the data is safe in the holder.

## Stage 3 — Apply into the v2 catalog (same GUID)

Add `Assets/Editor/LoquiMigrationApply.cs` (v2-only):
```csharp
using System.Collections.Generic;
using Loqui;
using UnityEditor;
using UnityEngine;

public static class LoquiMigrationApply
{
    const string HolderPath = "Assets/LoquiMigrationHolder.asset";

    [MenuItem("Tools/Loqui/Migration/2 - Apply To v2 Catalog")]
    public static void Apply()
    {
        var holder = AssetDatabase.LoadAssetAtPath<LoquiMigrationHolder>(HolderPath);
        if (holder == null) { Debug.LogError("[LoquiMigration] Holder not found at " + HolderPath); return; }

        var catalog = Selection.activeObject as LocalizationCatalog;
        if (catalog == null)
        {
            var guids = AssetDatabase.FindAssets("t:LocalizationCatalog");
            if (guids.Length == 1) catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        if (catalog == null) { Debug.LogError("[LoquiMigration] Select the target LocalizationCatalog asset first."); return; }

        catalog.SchemaVersion = 2;
        catalog.Languages = new List<LocalizationLocaleProfile>(holder.Languages);
        catalog.Texts = new List<LocalizationEntry>(holder.Texts);
        catalog.Bools = new List<LocalizationBoolEntry>(holder.Bools);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        if (!catalog.IsValid(out var error))
            Debug.LogError("[LoquiMigration] Catalog INVALID after apply: " + error);
        else
            Debug.Log($"[LoquiMigration] Applied: {catalog.Languages.Count} languages, {catalog.Texts.Count} texts, {catalog.Bools.Count} bools. Verify against the baseline, then delete the holder, the migration scripts, and the old sibling .asset files.");
    }
}
```

Run **Tools/Loqui/Migration/2 - Apply To v2 Catalog** against the same catalog asset.

## Stage 4 — Verify nothing was lost, then clean up

1. **Counts + keys:** open `Assets/LoquiMigrationBaseline.txt` and confirm `languages` / `texts` / `bools` counts and every `text:`/`bool:` key now exist in the catalog. Counts must match exactly.
2. **Values:** `git diff` the catalog `.asset`. Every English/per-language/per-platform value and every bool override must be byte-identical to what the v1 siblings held; invisible separators survive as YAML escapes (`U+2028` → `\L`). Spot-check any entry that had special characters or placeholders.
3. **Fonts:** if any locale used a `FontProfile`, confirm the `TMP_FontAsset` references are still assigned (not `None`).
4. **Validation:** run `LocalizationValidator` / your project's catalog-gate (or the MCP `validate_config`) — expect **0 errors**.
5. **Compile + tests:** let Unity reimport, build for your targets (or run the build-config compile matrix), and run the `Loqui.Tests` EditMode suite.
6. **Runtime:** confirm every `LocalizedText` key and every `Loc.Get` / `Loc.GetBool` call still resolves from the catalog (the keys are unchanged, so bindings are unaffected).
7. **Clean up only after green:** delete `LoquiMigrationHolder.asset`, `LoquiMigrationBaseline.txt`, the two migration scripts, and the orphaned v1 sibling `.asset` files (the missing-script locale-set / text-table / config-table assets).

## Rollback

If any count, value, or font reference does not match, do not delete anything: `git restore` the catalog asset and manifest, return to Stage 1, and re-capture. The migration is safe by construction only while the v1 data is still present.

Output: the baseline vs. post-migration counts, the validation result, the list of deleted v1 assets, and confirmation that all `LocalizedText` / `Loc.Get` keys resolve.
