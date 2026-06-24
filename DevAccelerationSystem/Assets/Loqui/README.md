# Loqui

Fallback-first localization for Unity. Your code literal stays the default; translations and platform variants are overlays on top of it — so missing keys never crash and an un-localized build renders byte-identical to before.

```csharp
label.text = Loc.Get("home.play_now", "Play Now");
```

## Features

- **Fallback-first** — `Loc.Get(key, literal)` returns the literal when no override exists; never throws on a missing key.
- **Per-language** — ship multiple languages from one catalog; pick by device locale or user choice at runtime.
- **Per-platform** — every entry can override per `iOS` / `Android` (e.g. store-review wording) on top of a shared default.
- **ScriptableObject catalog** — authored in the Inspector; organized into text tables; build-time validation.
- **TMP integration** — `LocalizedText` component for static prefab/scene labels; runtime font swap per language.
- **Locale formatting** — culture-aware number / currency / percent / date helpers.
- **Remote overrides (opt-in)** — parse and validate a sparse key→value payload (`JsonUtility`-compatible), then apply it on top of the catalog at runtime via `Loc.ApplyOverrides(...)` / `Loc.ClearOverrides()` without a rebuild.
- **Editor tooling** — text scanner, deterministic key generation, and approved-attach mode.

## Requirements

- Unity `2022.3+`
- TextMeshPro + uGUI (`com.unity.ugui`, `com.unity.textmeshpro`) — declared package dependencies, resolved automatically.

### Logging (optional)

Loqui has no hard logging dependency. Pass any `ILoquiLog` to `Loc.Initialize`, or `null` to stay silent.

If [TheBestLogger](https://github.com/FoxsterDev/DevAccelerationSystem/tree/master/DevAccelerationSystem/Assets/TheBestLogger) is present in your project, the optional `Loqui.Integrations.TheBestLogger` assembly compiles automatically (gated by the `LOQUI_THEBESTLOGGER` version define) and provides a ready adapter:

```csharp
ILoquiLog log = new Loqui.Integrations.TheBestLoggerLoquiLog(myTheBestLogger);
```

## Install (UPM, git)

Add to `Packages/manifest.json`:
```json
"com.foxsterdev.loqui": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/Loqui#0.1.0"
```

## Quick start

```csharp
using Loqui;

// 1) Initialize once at startup, on the main thread (catalog comes from your settings scope).
//    The logger is an optional ILoquiLog (pass null to stay silent).
Loc.Initialize(settings, Application.systemLanguage, LocalizationPlatformResolver.Current, logger: null);

// 2) Read text, fallback-first.
string play = Loc.Get("home.play_now", "Play Now");

// 3) Static labels: add a LocalizedText component (key + fallback) to the TMP object.

// 4) Switch language at runtime.
Loc.SetLanguage("pt-BR");

// 5) (Optional) apply a validated remote override on top of the catalog.
var result = Loqui.Remote.LocalizationOverridesParser.Parse(json);
if (result.Accepted)
{
    Loc.ApplyOverrides(result);
}
```

## License

MIT — see [LICENSE.md](LICENSE.md).
