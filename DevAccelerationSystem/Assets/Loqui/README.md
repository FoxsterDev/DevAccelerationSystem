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
- **Remote overrides** — apply a sparse key→value override at runtime (`JsonUtility`-compatible) without a rebuild.
- **Editor tooling** — text scanner, deterministic key generation, and approved-attach mode.

## Requirements

- Unity `2021.3+`
- TextMeshPro
- [TheBestLogger](https://github.com/FoxsterDev/DevAccelerationSystem/tree/master/DevAccelerationSystem/Assets/TheBestLogger) — Loqui logs through it.

> **Note:** UPM does not auto-resolve git transitive dependencies. If you install Loqui from a git URL, add TheBestLogger to your project manifest too:
> ```json
> "com.foxsterdev.thebestlogger": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/TheBestLogger#3.0.1"
> ```

## Install (UPM, git)

Add to `Packages/manifest.json`:
```json
"com.foxsterdev.loqui": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/Loqui#0.1.0"
```

## Quick start

```csharp
using Loqui;

// 1) Initialize once at startup (catalog comes from your settings scope).
Loc.Initialize(settings, Application.systemLanguage, LocalizationPlatformResolver.Current, logger);

// 2) Read text, fallback-first.
string play = Loc.Get("home.play_now", "Play Now");

// 3) Static labels: add a LocalizedText component (key + fallback) to the TMP object.

// 4) Switch language at runtime.
Loc.SetLanguage("pt-BR");
```

## License

MIT — see [LICENSE.md](LICENSE.md).
