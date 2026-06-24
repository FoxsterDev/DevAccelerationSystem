# XUUnity Full Review Report — Loqui

## Review Metadata
- Date: 2026-06-24
- Repo: DevAccelerationSystem (FoxsterDev)
- Target project: `DevAccelerationSystem/DevAccelerationSystem` (canonical package source)
- Branch: `master`
- Commit: `d36cc84` ("Init Loqui package")
- Review type: FullReview
- Review scope: `Assets/Loqui` — package `com.foxsterdev.loqui` v0.1.0 (~4,690 LOC, 54 C# files: Runtime + Editor scanner + 15 EditMode tests)
- Target kind: `current_state_sdk` (whole-package audit; package just added, no diff)
- Active risk families: `sdk_sensitive` (primary), `save_load_sensitive`, `ui_heavy_sensitive`, `core_flow_sensitive`, `release_sensitive`
- Policy packs active: `sdk_changes` (primary), `save_load_changes`, `ui_heavy_changes`, `startup_changes`
- Review bundle run: `architecture_review` · `sdk_code_review` · `sdk_breakage_review` · `delivery_risk_review` · `release_readiness_review` · `test_quality_review`
- Review bundle skipped: `native_plugin_review` (*not relevant* — no JNI/ObjC/Swift; managed-stripping `link.xml` folded into the breakage lens); `git_change_review` (*target-kind mismatch* — current-state, not a diff)
- Doctrine source: `../AIRoot/Modules/XUUnity/` (per user instruction)
- Method: 9 doctrine-aligned review lenses over real source → per-finding adversarial re-verification against cited lines (58 agents). 49 raw findings → **39 verifier-confirmed**, 10 refuted/invalid. Deduped to **23 canonical findings**.

## Assumptions
- Unity baseline for review = **2022.3.62f3 LTS** (host `ProjectVersion.txt`); package manifest *declares* `2021.3`. Mono + IL2CPP mobile both in scope; managed stripping active; TextMeshPro is the text backend.
- This package is intended for reuse/publishing (it ships `package.json`, `README`, `CHANGELOG`, `LICENSE`, keywords). Findings are weighted as for a public reusable SDK.

---

## Stability-First Executive Summary
- **Overall verdict:** Competently engineered core, **not yet release-ready as a public package.** No crashes, ANRs, or data-corruption paths were found. The risks cluster as *unfinished / unwired / undeclared*, not *broken-in-production*.
- **Severity distribution (post-verification, deduped):** **0 Critical · 0 High · 10 Medium · 13 Low.**
- **Highest-risk production groups:**
  1. **Advertised feature is dead** — the entire `Runtime/Remote/` overrides pipeline is parsed/validated but never applied; `README`/`package.json` present it as a shipping capability.
  2. **Packaging blocks clean reuse** — `package.json` omits the `com.unity.ugui` (TMP/UGUI) dependency the runtime hard-references (clean import fails to compile); the IL2CPP stripping descriptor is inert (`linkmerge.xml`, not `link.xml`); a localization package hard-depends on a logging package; declared `2021.3` floor is unvalidated.
  3. **Validation cannot back the package's own claims** — the suite is EditMode-only (Mono JIT); no PlayMode lifecycle, no IL2CPP/stripping build-context, no tracked consumer validation, yet allocation and JsonUtility-deserialization claims are backend-sensitive.
- **Confirmed deterministic bugs:** `Loc.Ready` never replays for late subscribers (CF-02); key generator collapses all non-ASCII source text to colliding group-only keys (CF-18); `package.json` is missing the UGUI dependency (CF-04); IL2CPP preservation file is inert (CF-05).
- **Release blockers (must close before publishing):** CF-01, CF-04, CF-05, CF-06, CF-07, CF-22.
- **Strengths (kept the score out of the danger band):** exception-safe list-backed event dispatch with reference dedup; size-capped + schema-validated + try/catch remote parser; fallback-first design (literal survives missing key); symmetric `OnEnable/OnDisable/OnDestroy` subscription on UI components; 15 deterministic EditMode logic tests; `InternalsVisibleTo` test seams.

---

## Remediation Status — Implemented 2026-06-24

All P0 + P1 + (requested) P2 canonical findings were implemented in the working tree (Loqui package + DemoProject), **not yet committed**. **No compile/EditMode/PlayMode run was performed** (Unity MCP bridge disabled this session) — code-complete but unverified-at-runtime. Run the suites with the bridge enabled to confirm and lift scoring confidence above Low.

| ID | Status | Change |
| --- | --- | --- |
| CF-01 | **Fixed** | `LocalizationService.ApplyOverrides`/`ClearOverrides` (opt-in) overlay accepted payload per current language+platform (`iOS`→`IOS`), survive language switch, re-raise `LanguageChanged`; `Loc` facade delegation; README corrected to opt-in. |
| CF-02 | **Fixed** | `Loc.Ready` latched — a late subscriber fires immediately when already ready. |
| CF-03 | **Fixed** | `FontProfile.FallbackFonts` applied to the resolved TMP font's `fallbackFontAssetTable`; `LegacyFont` applied in `Loc.Apply(Text)`. |
| CF-04 | **Fixed** | `package.json` declares `com.unity.ugui` + `com.unity.textmeshpro`. |
| CF-05 | **Fixed** | Added `link.xml` copy (kept `linkmerge.xml`) so the IL2CPP linker preserves the `Loqui` assembly. |
| CF-06 | **Fixed** | Logger decoupled behind package-owned `ILoquiLog`; hard `thebestlogger` dep removed; optional `Loqui.Integrations.TheBestLogger` adapter gated by `versionDefines(com.foxsterdev.thebestlogger)→LOQUI_THEBESTLOGGER` + `defineConstraints`. |
| CF-07 | **Fixed** | `package.json` + README Unity floor → `2022.3`. |
| CF-08 | **Fixed** | `LocalizationEvent.Raise` re-entrancy-safe: shared buffer only for the top-level raise; nested raises snapshot into a local array. |
| CF-09 | **Fixed** | Main-thread contract documented; `LocalizationMainThread` (`[Conditional]` UNITY_EDITOR/DEVELOPMENT_BUILD) captures at Initialize and verifies in Set/Reset/Apply/ApplyOverrides. |
| CF-10 | **Open (intentionally)** | Naive "Clear listeners in `Shutdown`" would break the subscribe-before-init path because `Initialize` calls `Shutdown` first. Safe fix = a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` reset only; deferred (Low; cross-session leak was already refuted — UI unsubscribes via OnDisable/OnDestroy). |
| CF-11 | **Fixed** | `LanguageDropdown.OnEnable/OnDisable` null-guard `_dropdown`; still unsubscribes Loc events if the dropdown is gone. |
| CF-12 | **Fixed** | `SetLanguage` skips rebuild + broadcast when the target equals the current language (still persists the explicit choice). |
| CF-13 | **Fixed** | `LocalizationService` constructor is now `internal` — `Loc` is the sole construction owner (tests via `InternalsVisibleTo`). |
| CF-14 | **Fixed** | `FormatCurrency` caches the symbol-substituted `NumberFormatInfo` per currency code. |
| CF-15 | **Partially fixed** | Active-table `Dictionary` now allocated with a capacity hint (`entryBuffer.Count`); per-language table cache still deferred (Low perf). |
| CF-16 | **Fixed** | `FormatCurrency` groups by the currency's country for `USD/BRL/GBP/JPY`; UI culture for others (incl. `EUR`); documented. |
| CF-17 | **Fixed** | IL2CPP invariant-globalization requirement documented in `ADDING_A_LANGUAGE.md`. |
| CF-18 | **Fixed** | Key generator appends a deterministic FNV-1a hash suffix when the slug is empty for non-empty (non-ASCII) source. |
| CF-19 | **Fixed** | C# scanner strips comments (string/char/verbatim-aware, layout-preserving) before regex matching → no commented-code false positives; `//` inside string literals preserved. |
| CF-20 | **Fixed** | `CompareItems` adds `TextComponentId` → `ProposedKey` → `LineNumber` tie-breakers for a total, deterministic order. |
| CF-21 | **Fixed** | `DisambiguateKeys` re-checks synthesized keys against `seen` and inserts them — no organic/synthesized key collision regardless of order. |
| CF-22 | **Fixed** | PlayMode lane added in `DemoProject` (Loqui wired via `file:`): real `OnEnable/OnDisable`, live language switch, target destruction, scene reload (programmatic catalog). |
| Prompts/ | **Fixed** | Moved to `Documentation~/Prompts/` (excluded from the imported package payload); `.meta` files dropped. |

**Publish gate (from Release Recommendation below):** all listed blockers (CF-01, CF-04, CF-05, CF-06, CF-07, CF-22) are now code-complete; verification (compile + EditMode + PlayMode + an IL2CPP build) is the remaining gate before the score/verdict can be revised.

New tests: EditMode — `LocalizationHardeningTests` (Ready latch, re-entrant + mid-Remove event order, ApplyOverrides incl. `iOS`→`IOS`, SetLanguage no-op, persisted-choice restore + stale fallback), `LocalizationFormatterHardeningTests`, `LocalizationKeyGeneratorHardeningTests`, `LocalizationScannerHardeningTests`; PlayMode — `LoquiSample.PlayModeTests` (DemoProject). This covers the high-value CF-23 paths; a font-swap-on-language-change assertion needs real `TMP_FontAsset` assets and is left for manual/asset-backed verification.

Only **CF-10** remains intentionally open (see its row). Everything else is code-complete pending the runtime verification noted at the top of this section.

---

## Bundle Rationale
- **Candidate protocols:** architecture, sdk_code, sdk_breakage, native_plugin, delivery_risk, release_readiness, feature_code, git_change, test_quality.
- **Why this bundle:** `current_state_sdk` + `sdk_sensitive` force `architecture_review` + `sdk_code_review` + `delivery_risk_review` + `release_readiness_review`, with `sdk_breakage_review` added (public API surface meant for reuse). `save_load_changes`/`ui_heavy_changes` policy packs pulled persistence and TMP-binding lenses; `test_quality_review` added because the package is test-heavy and release confidence depends on test trustworthiness.
- **Skipped:** `native_plugin_review` — *not relevant* (no native bridge; only managed stripping, reviewed in the breakage lens). `git_change_review` — *target-kind mismatch*. `feature_code_review` — *subsumed* (SDK-wrapper boundary dominates, so `sdk_code_review` is the correct primary code-surface review).

---

## Canonical Findings Summary

| ID | Sev | Conf | Class | Group | Affected lenses | Issue | Why stable prod cares | Required action |
|----|-----|------|-------|-------|-----------------|-------|-----------------------|-----------------|
| CF-01 | Medium | High | functional gap | A Feature | arch, breakage, persistence, tests | Remote overrides parsed/validated but **never applied**; docs say it works | Headline feature is non-functional; README/CHANGELOG mislead integrators | Wire an apply path **or** remove the feature from docs for 0.1.0 |
| CF-02 | Medium | High | deterministic bug | A Feature | sdk_core | `Loc.Ready` is edge-triggered; late subscribers (post-`Initialize`) never fire | Consumers wiring after sync init silently never initialize their UI | Latch `Ready`: fire immediately on subscribe when already ready |
| CF-03 | Low | High | functional gap | A Feature | breakage | `FontProfile.FallbackFonts`/`LegacyFont` serialized but never applied | Non-Latin glyphs render as tofu despite configured fallback fonts | Apply fallback/legacy fonts in `Loc.Apply`, or remove the fields |
| CF-04 | Medium | High | deterministic bug | B Packaging | delivery | `package.json` omits `com.unity.ugui` though asmdef hard-refs TMP/UGUI | Clean import into a UGUI-less project fails to compile | Declare `com.unity.ugui` in `dependencies` |
| CF-05 | Medium | High | breakage risk | B Packaging | breakage, delivery | `linkmerge.xml` (not `link.xml`) imported as TextAsset → IL2CPP stripping protection **inert** | Stripping can remove preserved types on IL2CPP device builds | Rename to `link.xml` (or wire the merge); validate with an IL2CPP build |
| CF-06 | Medium | High | coupling | B Packaging | arch, delivery | Hard UPM dep on `thebestlogger@3.0.1` for an optional logger (3 calls) | Forces a logging package on every consumer; git transitive deps don't auto-resolve | Decouple behind a tiny package-owned log interface; drop the hard dep |
| CF-07 | Medium | High | coverage gap | B Packaging | delivery | Declared `unity: "2021.3"` floor never compiled/tested (only 2022.3.62f3) | Consumers on 2021.3 may hit uncompiled code | Raise floor to `2022.3` **or** add a 2021.3 compile lane |
| CF-08 | Medium | High | breakage risk | C Runtime | sdk_core | `LocalizationEvent` re-entrant `Raise` reuses a shared `_raiseBuffer` | A `LanguageChanged` handler that re-enters → double-dispatch + swallowed NREs + recursion risk | Snapshot per-`Raise` (stack-local/pooled) or guard re-entrancy |
| CF-09 | Medium | High | inference-heavy | C Runtime | breakage, sdk_core | No documented/enforced main-thread contract (PlayerPrefs/TMP/non-sync collections) | Off-main-thread misuse silently reachable via public API (no trigger today) | Document main-thread-only; add a dev-build main-thread assertion |
| CF-10 | Low | High | breakage risk | C Runtime | arch, breakage, ui | `Loc.Shutdown` doesn't `Clear()` listener lists; no domain-reload reset | Hygiene gap for manual subscribers / edit-mode reuse (cross-session leak refuted) | `Clear()` events in `Shutdown`; add `[RuntimeInitializeOnLoadMethod]` reset |
| CF-11 | Low | High | breakage risk | C Runtime | ui | `LanguageDropdown` `OnEnable/OnDisable` deref `_dropdown` unguarded (others guard it) | NRE if component used without its dropdown (RequireComponent → low prob) | Add the same `if (_dropdown == null) return;` guard |
| CF-12 | Low | High | perf | C Runtime | sdk_core | `SetLanguage` not idempotent: re-selecting active language re-runs full rebuild + `PlayerPrefs.Save` + spurious broadcast | Wasted O(N) alloc + disk flush + UI churn on no-op set | Early-return when matched == current |
| CF-13 | Low | High | inference-heavy | C Runtime | arch | `LocalizationService` publicly constructible parallel to `Loc` facade | Two init owners; raw service bypasses facade event wiring (no active misuse) | Make ctor `internal` if `Loc` is the sole entrypoint |
| CF-14 | Low | High | perf | D Perf | perf | `FormatCurrency` clones a full `NumberFormatInfo` per call | GC on price/coin/reward labels on mass-market Android | Cache the symbol-substituted `NumberFormatInfo` per culture/currency |
| CF-15 | Low | High | perf | D Perf | perf, sdk_core | Language switch reallocates the active-table `Dictionary` (no capacity hint) + new formatter | Avoidable alloc/rehash churn per switch and at startup | Pass capacity hint; optionally cache per-language tables |
| CF-16 | Low | High | inference-heavy | E Culture | breakage | `FormatCurrency` uses UI-language culture for money grouping (currency-country mismatch) + ICU/AOT-sensitive | Wrong money grouping for cross-currency display | Derive `NumberFormatInfo` from currency region; document ICU need |
| CF-17 | Low | Med | coverage gap | E Culture | delivery | No documented IL2CPP invariant-globalization gate for culture formatting | Silent invariant fallback if consumer enables Invariant Globalization | Document the requirement; warn once on invariant fallback |
| CF-18 | Medium | High | deterministic bug | F Scanner | editor | Key generator collapses all non-ASCII source text (CJK/Cyrillic/Arabic) to group-only keys | Core failure for the tool's non-English source case → meaningless keys | Hash-suffix fallback when slug is empty for non-empty source |
| CF-19 | Low | High | breakage risk | F Scanner | editor | C# scanner regex: no comment/verbatim/interpolation handling | False positives (commented code) + truncated/garbage keys in the bundle | Lex strings/comments, or keep strictly advisory + human-gated |
| CF-20 | Low | High | coverage gap | F Scanner | editor | Determinism test pins only a pure sort, not the real nondeterminism sources | Asset-enumeration / key-gen / non-ASCII nondeterminism unproven | Test sibling-collision ordering + non-ASCII key repeatability |
| CF-21 | Low | High | inference-heavy | F Scanner | editor | `DisambiguateKeys` can still collide when an organic key ends in the suffix pattern | Two distinct labels can map to one key | Re-check synthesized key against `seen` and insert it |
| CF-22 | Medium | High | coverage gap | G Validation | tests, ui | Suite is EditMode-only: no PlayMode, no IL2CPP/stripping, no consumer validation; allocation/deserialization claims under Mono JIT | Release confidence is not backed for mobile/IL2CPP claims | Add PlayMode + IL2CPP build-context + DemoProject consumer lanes |
| CF-23 | Low | High | coverage gap | G Validation | tests, persistence | Untested risky paths: Ready fan-out, subscribe-before-init, font swap, persisted-choice restore/stale, override apply, DTO `iOS` roundtrip + `iOS→IOS`, event order after Remove | These are the exact paths most likely to regress unnoticed | Add the named EditMode/PlayMode tests |

---

## Grouped Findings (most → least critical)

### Group A — Functional gaps (does not do what the docs say)

**CF-01 [Medium] Remote-overrides subsystem is parsed/validated but never applied** — *functional gap, release blocker*
- Files: `Runtime/Remote/LocalizationOverridesParser.cs:64`, `Runtime/Remote/LocalizationOverridesResult.cs:12`, `Runtime/LocalizationActiveTable.cs:30`, `Runtime/LocalizationService.cs:159`, `README.md:17`, `package.json:19`
- Evidence: `Parse()` returns `Accepted=true` with `Payload=dto`, but a package-wide grep shows **no runtime/editor consumer reads `.Payload`** — only `Runtime/Remote/*`, tests, and the docs reference the Overrides symbols. `LocalizationActiveTable.Build` sources values exclusively from the catalog ScriptableObjects; `RebuildActive` never takes an override input. `README.md:17` advertises "Remote overrides — apply a sparse key→value override at runtime … without a rebuild" as shipped.
- What breaks: a documented headline feature is inert; integrators wire fetch→parse and observe nothing change.
- Fix: (a) implement `LocalizationService.ApplyOverrides(LocalizationOverridesResult)` that overlays accepted entries on the active table (platform-aware, with its own change event + revert), and add `Loc` delegation; **or** (b) for 0.1.0, remove the feature from `README`/`CHANGELOG`/`package.json` description and mark `Runtime/Remote/` experimental.
- Test obligation: EditMode — accepted override changes `Loc.Get`; rejected/partial leaves base intact; non-catalog key ignored. PlayMode — applied override re-raises `LanguageChanged` to live `LocalizedText`.

**CF-02 [Medium] `Loc.Ready` is a one-shot edge with no replay** — *confirmed deterministic bug*
- Files: `Runtime/Loc.cs:38,49,24`, `Runtime/LocalizationService.cs:76-77`, `Runtime/LocalizedText.cs:72`
- Evidence: `LocalizationService.Initialize()` synchronously sets `IsReady=true` then `Ready?.Invoke()` (`:76-77`); `Loc.Initialize` runs this synchronously. `LocalizationEvent.Add` does not reconcile against an already-true ready state, so any subscriber added *after* `Initialize` returns never receives `Ready`.
- What breaks: late subscribers (common: UI created after bootstrap) never get the ready callback; their first localized refresh is skipped unless they also catch `LanguageChanged`.
- Fix: in `Ready`'s `add` accessor (or service), invoke the new listener immediately (guarded) when `IsReady`. Document `Ready` as level-triggered.
- Test: EditMode — `Initialize`, then subscribe to `Ready`, assert it fires once for the late subscriber; assert no double-fire for early subscribers.

**CF-03 [Low] `FontProfile.FallbackFonts`/`LegacyFont` are serialized but never applied** — *functional gap*
- Files: `Runtime/Model/LocalizationFontProfile.cs:11-12`, `Runtime/LocalizationService.cs:165`, `Runtime/Loc.cs:100`
- Evidence: `ResolveTmpFont` returns only `PrimaryFont`/platform override; grep finds **zero read sites** for `FallbackFonts`/`LegacyFont`/`fallbackFontAssetTable`.
- What breaks: configured per-language fallback fonts and legacy-UI fonts are silently ignored — non-Latin glyph fallback (the localization core case) is missing.
- Fix: apply `FallbackFonts` to the resolved `TMP_FontAsset.fallbackFontAssetTable` and `LegacyFont` in `Loc.Apply(Text,…)`, or remove the inert fields.

### Group B — Packaging / build correctness (block clean reuse)

**CF-04 [Medium] `package.json` omits `com.unity.ugui`** — *confirmed deterministic bug, release blocker*
- Files: `Runtime/Loqui.asmdef:5,7`, `package.json:20`, `README.md:23`
- Evidence: the runtime asmdef hard-references `Unity.TextMeshPro` and `UnityEngine.UI`; `package.json` `dependencies` lists only `com.foxsterdev.thebestlogger`. A clean import into a project without UGUI/TMP will not compile.
- Fix: add `"com.unity.ugui": "1.0.0"` (2022.3-appropriate minimum) to `dependencies`.
- Test: clean-project import smoke that compiles `Loqui` with only its declared deps.

**CF-05 [Medium] IL2CPP stripping descriptor is inert (`linkmerge.xml`, not `link.xml`)** — *breakage risk, release blocker*
- Files: `linkmerge.xml:1`, `linkmerge.xml.meta:3`, `Runtime/AssemblyInfo.cs:3`, `Runtime/Remote/LocalizationOverridesDto.cs:5`
- Evidence: Unity's IL2CPP linker honors files named exactly `link.xml`; `linkmerge.xml.meta` declares `TextScriptImporter`, so Unity treats it as a plain TextAsset. A project-wide search finds **zero** files named `link.xml`. The `<assembly fullname="Loqui" preserve="all"/>` directive is therefore never fed to the linker.
- What breaks: under managed stripping on IL2CPP device builds, preserved types (notably the JsonUtility override DTOs, once an apply path exists) can be stripped.
- Fix: ship a real `link.xml` (exact name) with the Loqui preserve directive and explicit `<type>` entries for the JsonUtility DTOs; validate with an IL2CPP iOS+Android build at the host stripping level.

**CF-06 [Medium] Hard dependency on `com.foxsterdev.thebestlogger@3.0.1` for an optional logger** — *coupling / adoption blocker*
- Files: `package.json:21`, `Runtime/Loqui.asmdef:6`, `Runtime/LocalizationService.cs:5,61,198`, `Runtime/LocalizationEvent.cs:3,71`, `Runtime/Loc.cs:6`, `Runtime/Remote/LocalizationOverridesParser.cs:3`
- Evidence: the entire external surface used is three calls (`LogError`/`LogWarning`/`LogException`); five runtime files alias `using ILogger = TheBestLogger.ILogger`. Every consumer is forced to also adopt a logging package — and per the repo's own note, git transitive deps don't auto-resolve, so the consumer must add a second git URL just to compile. The logger is otherwise optional (`logger == null` is handled everywhere).
- Fix: define a tiny package-owned `ILoquiLog` (LogWarning/LogError/LogException), default no-op; offer an optional adapter to TheBestLogger in a separate asmdef/`#if`. Remove the hard `package.json` dependency.
- Test: EditMode — construct/use with `logger == null` and assert full functionality with no NRE.

**CF-07 [Medium] Declared `unity: "2021.3"` floor is unvalidated** — *coverage gap, release blocker*
- Files: `package.json:5`, `README.md:22`
- Evidence: UPM treats `unity` as a hard minimum surfaced to consumers; all three `ProjectVersion.txt` in the workspace are 2022.3.x. No 2021.3 compile evidence exists.
- Fix: raise the floor to `2022.3`, or add a 2021.3 compile/EditMode lane and keep the claim only if it passes.

### Group C — Runtime safety / API contract

**CF-08 [Medium] `LocalizationEvent` shared `_raiseBuffer` is not re-entrancy-safe** — *breakage risk* (independently confirmed)
- Files: `Runtime/LocalizationEvent.cs:11,55,60-67`, `Runtime/LocalizationService.cs:111`, `Runtime/Loc.cs:143`
- Evidence/trace: `Raise` copies listeners into the single instance field `_raiseBuffer`, nulling each slot before invoking. If a listener re-enters `Raise` on the same instance (e.g. a `LanguageChanged` handler that calls `SetLanguage`), the nested call (when count ≤ buffer length) **reuses the same buffer**: it re-dispatches the full set, then the outer loop resumes reading slots the nested pass already nulled → `NullReferenceException`s swallowed by the per-listener `try/catch`, plus double-dispatch and unbounded-recursion risk if the re-entry is unconditional.
- Note: the "silently *drops* UI listeners" framing from the UI lens was **refuted** — listeners still fire via the nested pass; the real defects are double-dispatch + swallowed NREs + recursion.
- Fix: snapshot into a stack-local/pooled array per `Raise`, or guard with an in-flight flag that defers nested raises.
- Test: EditMode — a listener that triggers a nested `Raise`; assert each original listener fires exactly once, no exceptions.

**CF-09 [Medium] No documented or enforced main-thread affinity** — *inference-heavy (no active trigger today)*
- Files: `Runtime/Loc.cs:38,66,82`, `Runtime/LocalizationService.cs:102,109`, `Runtime/LocalizationPreferences.cs:18`, `Runtime/LocalizationEvent.cs:15,26`, `README.md:40-44`
- Evidence: mutating entries (`Initialize`/`SetLanguage`/`Apply`/event add-remove) carry no thread contract; `SetLanguage→PlayerPrefs.Save` and `LocalizationEvent` mutate non-synchronized `List`+`Dictionary`. Today all callers are main-thread (only `LocalizedText`/`LanguageDropdown` via Unity lifecycle) and the remote-override apply path that could marshal off-thread doesn't exist — so it's a latent contract gap, not a live bug.
- Fix: document main-thread-only on the facade and events; add a cheap main-thread assertion in Editor/Development builds.

**CF-10 [Low] `Loc.Shutdown` doesn't `Clear()` listener lists; no domain-reload reset** — *hygiene*
- Files: `Runtime/Loc.cs:13-16,53-64`, `Runtime/LocalizationEvent.cs:41`
- Evidence: `Shutdown` detaches service↔facade handlers and nulls `_service`/`_logger` but never `Clear()`s `_languageChanged`/`_ready`; no `[RuntimeInitializeOnLoadMethod]`. The package's own `LocalizedTextTests` `TearDown` manually calls `HandleDisable()` before `Shutdown` precisely because `Shutdown` doesn't clear listeners.
- Note: the broader "leaks across Play sessions / accumulates duplicates" claim was **downgraded** — `LocalizedText`/`LanguageDropdown` unsubscribe in `OnDisable`/`OnDestroy` (which Unity still fires on Play-exit even with domain reload off), and `Initialize` calls `Shutdown` first, so the normal runtime path self-drains. Residual impact is confined to manual subscribers / edit-mode reuse.
- Fix: `Clear()` both events in `Shutdown`; add a `SubsystemRegistration` reset for the statics.

**CF-11 [Low] `LanguageDropdown.OnEnable/OnDisable` deref `_dropdown` unguarded** — *breakage risk (low probability)*
- Files: `Runtime/LanguageDropdown.cs:40,51,60,88`
- Evidence: `OnEnable`/`OnDisable` call `_dropdown.onValueChanged.Add/RemoveListener` with no null check, while `Rebuild`/`SyncSelection` guard `if (_dropdown == null) return;`. `[RequireComponent(typeof(TMP_Dropdown))]` + `Awake` `GetComponent` make null unlikely but not impossible (component removed in editor).
- Fix: add the same null guard at the top of `OnEnable`/`OnDisable`.

**CF-12 [Low] `SetLanguage` is not idempotent** — *perf/correctness*
- Files: `Runtime/LocalizationService.cs:102-111`, `Runtime/LocalizationActiveTable.cs:36`, `Runtime/LanguagePickerController.cs:39`
- Evidence: no equality short-circuit — re-selecting the current language re-persists PlayerPrefs (`+ Save()` disk flush), rebuilds the whole active table (new `Dictionary` + re-resolve), clears the missing-key cache, and fires `LanguageChanged` to all subscribers.
- Fix: early-return when matched == `CurrentLanguageCode`; expose a separate explicit refresh if needed.

**CF-13 [Low] Two valid init owners** — *inference-heavy (no active misuse)*
- Files: `Runtime/LocalizationService.cs:9,52`, `Runtime/Loc.cs:38,46`
- Evidence: `LocalizationService` is `public sealed` with a public ctor + `Initialize`/`SetLanguage`, parallel to the `Loc` facade. Grep shows the only `new LocalizationService` is `Loc` itself + tests, so no second instance ships today.
- Fix: make the ctor `internal` (tests already have `InternalsVisibleTo`) if `Loc` is the sole entrypoint; otherwise document multi-instance use.

### Group D — Performance / GC

**CF-14 [Low] `FormatCurrency` clones a full `NumberFormatInfo` per call** — `Runtime/LocalizationFormatter.cs:29-33`. Heavyweight clone + result string on every currency format; hot on Android price/coin labels. The other four formatters reuse `_culture` without cloning. Fix: cache the symbol-substituted `NumberFormatInfo` per culture/currency.

**CF-15 [Low] Language switch reallocates the whole active-table `Dictionary` (no capacity hint) + a new formatter** — `Runtime/LocalizationActiveTable.cs:36,51`, `Runtime/LocalizationService.cs:159-164`. Fix: pass `entryBuffer.Count` capacity; optionally cache built tables/formatters per language.

### Group E — Correctness: culture & fonts

**CF-16 [Low] `FormatCurrency` uses the UI-language culture for money grouping** — `Runtime/LocalizationFormatter.cs:29-34`. Clones the active culture's `NumberFormatInfo` and only swaps `CurrencySymbol`, so a USD amount shown to a pt-BR user renders with pt-BR grouping. Also `GetCultureInfo` + `"C"`/`"P2"` are ICU/AOT-sensitive on IL2CPP. (The current formatter test asserts the locale-driven-grouping behavior, i.e. it pins the questionable behavior.) Fix: derive the currency `NumberFormatInfo` from a currency-region culture, or document the limitation.

**CF-17 [Low] No documented IL2CPP invariant-globalization gate** — `Runtime/LocalizationFormatter.cs:63-77`, `ADDING_A_LANGUAGE.md:48`. `ResolveCulture` falls back to `InvariantCulture` on `CultureNotFoundException` with no diagnostic; if a consumer enables Invariant Globalization, configured locales silently format invariant. Fix: document the requirement; warn once on invariant fallback.

### Group F — Editor scanner robustness

**CF-18 [Medium] Key generator collapses all non-ASCII source text to group-only keys** — *confirmed deterministic bug*
- Files: `Editor/Scanner/LocalizationKeyGenerator.cs:29-66` (`:42` `char.IsLetterOrDigit(c) && c < 128`), `Editor/Scanner/LocalizationTextScanner.cs:138-159`
- Evidence: the verifier extracted `Slug`/`Generate` into a standalone program and ran it: `开始游戏 → 'panel'`, `设置 → 'panel'`, `退出 → 'panel'`, `Привет → 'panel'`, `プレイ → 'panel'`, `مرحبا → 'panel'` — every non-ASCII label collapses to the bare group slug; ASCII/accented work (`Olá → panel.ola`). After `DisambiguateKeys` these become `panel`, `panel_2`, `panel_3` — distinct but semantically meaningless.
- What breaks: for the tool's core use case (non-English source strings), generated keys are useless/colliding.
- Fix: when `Slug` is empty for non-empty source, append a deterministic culture-invariant content hash suffix.

**CF-19 [Low] C# scanner regex has no comment/verbatim/interpolation handling** — `Editor/Scanner/LocalizationCSharpScanner.cs:13-19,32-103`. Two compiled regexes over raw text with value groups `[^;\r\n]+`/`[^,\)\r\n]+`: commented-out `.text = "x";` lines become live candidates; verbatim/interpolated/multiline strings truncate. Output is advisory but `ProposedKey` is still generated and bundled. Fix: lex strings/comments, or keep strictly advisory + human-gated before any attach/catalog write.

**CF-20 [Low] Determinism test pins only a pure sort** — `Tests/Editor/LocalizationScannerDeterminismTests.cs`. It asserts `Finalize` sorts a hand-built list and `DisambiguateKeys` appends `_2` stably, but never exercises `AssetDatabase.FindAssets` ordering, component traversal order, or non-ASCII hashing — where nondeterminism actually enters. Fix: add sibling-collision ordering + non-ASCII key-repeatability tests; add a final stable tie-break to `CompareItems`.

**CF-21 [Low] `DisambiguateKeys` can still collide** — `Editor/Scanner/LocalizationTextScanner.cs:138-159`. Synthesized `_2` keys aren't re-checked against / inserted into `seen`, so an organic slug ending in `_2` can collide with a disambiguated one. Fix: loop until the candidate isn't in `seen`, then insert it.

### Group G — Validation / test-surface gaps

**CF-22 [Medium] Suite is EditMode-only** — *coverage gap, release blocker for mobile claims*
- Files: `Tests/Editor/Loqui.Tests.asmdef:13` (`includePlatforms:["Editor"]`), `Tests/Editor/LocalizationServiceTests.cs:130` (`WarmTryGet_DoesNotAllocate` under Mono JIT), `Runtime/Remote/LocalizationOverridesParser.cs:28`, `linkmerge.xml:1`
- Evidence: all 15 tests run as EditMode under the Mono JIT backend; no PlayMode asmdef, no `[UnityTest]`, no `SetActive`, no DemoProject consumer wiring. Allocation (`Is.Not.AllocatingGCMemory`) and `JsonUtility.FromJson` deserialization are inherently backend-sensitive and untested on IL2CPP/stripping.
- Fix: add a PlayMode lane (real `OnEnable/OnDisable`, language-switch fan-out, scene reload), an IL2CPP+stripping build-context check that round-trips the override DTO, and a DemoProject consumer scene as tracked integration evidence; move allocation/deserialization claims off Mono-JIT-only.

**CF-23 [Low] Specific untested risky paths** — `Tests/Editor/*`. Missing: `Ready` fan-out + subscribe-before-`Initialize` (S12), font swap on language change (S12), persisted-choice restore + stale/unsupported code at `Initialize` (S25/S33/S26), remote-override apply (no path exists) (S11), DTO `iOS` field roundtrip + `iOS→IOS` mapping (S31), `LocalizationEvent` delivery order after mid-list `Remove` swap (S32). Fix: add the named EditMode/PlayMode tests; pin the event-order contract.

---

## Per-Protocol Condensed Notes

| Protocol | What it added | Overlap / merge notes | Remaining unknowns |
|----------|---------------|-----------------------|--------------------|
| architecture_review | Orphaned `Remote/` boundary; dual init owners; logger coupling; static lifetime contract | Remote-dead merged with breakage+persistence+tests (CF-01); Shutdown-hygiene merged (CF-10) | Whether Remote was intentionally deferred for 0.1.0 |
| sdk_code_review | `Ready` replay bug; re-entrancy; `SetLanguage` non-idempotency; sync build cost | Re-entrancy merged with UI lens (CF-08); main-thread merged (CF-09) | Real catalog scale → startup build cost |
| sdk_breakage_review | Public API contract map; `link.xml` inert; font fallback inert; currency culture | link.xml merged with delivery (CF-05) | Actual IL2CPP stripping behavior (no build run) |
| ui_heavy (ui_lifecycle) | Dropdown null-guard asymmetry; confirmed UI subscription symmetry is correct | Its re-entrancy + leak claims were refuted/downgraded vs sdk_core | Real `OnEnable` ordering (no PlayMode run) |
| save_load (persistence_remote) | Remote parse-only; persisted-restore untested; stale-code silent drop | Remote-dead merged (CF-01); restore-gap merged with tests (CF-23) | Merge semantics — none exist yet |
| editor scanner | Non-ASCII key collapse (with executable repro); regex false positives; weak determinism test | Self-contained | Real project scan corpus |
| perf_gc | Per-call `NumberFormatInfo` clone; rebuild alloc churn | Dropdown-rebuild + ToString allocation claims refuted | No device/IL2CPP allocation measurement |
| test_quality_review | EditMode-only ceiling; named missing tests | Many gaps merged into CF-22/CF-23 | — |
| delivery_risk + release_readiness | Missing UGUI dep; 2021.3 unverified; logger dep; Prompts/ in payload; link.xml | Logger + link.xml merged | No clean-room import / compile run |

---

## Public API Contract Map (thread affinity · async · lifecycle · null · error · stripping)
- `Loc.Initialize(settings, systemLanguage, platform, logger)` — **main-thread-only (undocumented)**, sync; single init entry, internally calls `Shutdown()` first (idempotent for the service, **not** for external subscribers); null settings → `IsEnabled=false`, English fallback; never throws (invalid catalog → log + disabled).
- `Loc.Shutdown()` — main-thread; detaches service handlers, nulls `_service`/`_logger`; **does not** clear static `_languageChanged`/`_ready`.
- `Loc.TryGet` / `Loc.Get(key, fallback)` — main-thread, sync; null key tolerated; never throws; pre-init returns fallback; missing key reported once via logger. Hot path.
- `Loc.SetLanguage(code)` / `ResetToSystemLanguage()` — **main-thread-only** (PlayerPrefs + `Save`, TMP font rebuild), sync, returns bool; needs `IsEnabled && IsReady` else silent false; raises `LanguageChanged` synchronously.
- `Loc.Apply(TMP_Text…)` / `Apply(Text…)` — **main-thread-only**; Unity-null-checked (handles destroyed targets); silent no-op on null/destroyed.
- `Loc.FormatNumber/Currency/Percent/ShortDate/DateTime` — culture+`ToString`; invariant fallback before init; **AOT/ICU-sensitive on IL2CPP**.
- `Loc.LanguageChanged` / `Loc.Ready` — process-static `LocalizationEvent` (type-init); reference-dedup add/remove; main-thread raise; **per-listener `try/catch`**; **`Ready` does not replay** for late subscribers; not cleared by `Shutdown`.
- `public LocalizationService` — same contract; **externally constructible** (bypasses facade event wiring).
- `public LocalizationCatalog : ScriptableObject` — config; `IsValid(out error)` is the only runtime gate.
- `Remote.LocalizationOverridesParser.Parse` / `Result` / `Dto` — sync, pure, size-capped, schema-validated; **`Result.Payload` is dead (never consumed)**; DTO is a `[Serializable]` JsonUtility target (stripping-sensitive); DTO field `iOS` (lowercase) vs model `IOS`.
- `LocalizedText`, `LanguageDropdown` (MonoBehaviours) — subscribe in `OnEnable`, unsubscribe in `OnDisable`/`OnDestroy` via `_subscribed` flag (symmetry verified correct).

---

## QA Manual Validation Recommendations

| Priority | Scenario | Variants | What to verify | Failure signal |
|----------|----------|----------|----------------|----------------|
| P0 | Clean-room package import | Fresh 2022.3 project, only declared deps | `Loqui` compiles | Missing-type / UGUI compile errors (CF-04) |
| P0 | IL2CPP device build | iOS + Android, Medium+ stripping | Catalog + (future) override types survive; no missing-type/serialization exception | Tofu/empty text or runtime exception (CF-05) |
| P0 | Remote override applied | Valid payload fetched at runtime | Strings actually change | No change → feature dead (CF-01) |
| P1 | Runtime language switch | Many live `LocalizedText`, font-swapped locale | All labels + fonts update; no flicker/leak | Stale text, wrong font, duplicate updates |
| P1 | Late `Ready` subscriber | Subscribe after `Initialize` | Handler fires | Never fires (CF-02) |
| P1 | Persisted choice restart | Set language, kill app, relaunch | Choice restored; removed-language code → graceful fallback | Wrong/blank language (CF-23) |
| P2 | Non-Latin glyphs | CJK/Arabic locale | Glyphs render via fallback fonts | Tofu boxes (CF-03) |
| P2 | Invariant-globalization build | IL2CPP + Invariant Globalization on | Documented behavior | Silent invariant formatting (CF-17) |

## Candidate Test Cases

| Title | Level | Preconditions | Steps | Expected |
|-------|-------|---------------|-------|----------|
| Ready replays for late subscriber | EditMode | Initialized service | Subscribe to `Loc.Ready` post-init | Fires exactly once |
| Re-entrant Raise is safe | EditMode | `LocalizationEvent` + 3 listeners | L0 triggers nested `Raise` | Each listener once, no exception |
| SetLanguage(current) is no-op | EditMode | Active language X | `SetLanguage(X)` | No rebuild alloc, no `LanguageChanged` |
| Non-ASCII keys are distinct | EditMode | Key generator | Generate CJK/Cyrillic labels in one group | Distinct, repeatable keys |
| Override applies + falls back | EditMode | Apply path (CF-01) | Apply valid payload | `Get` returns override; non-catalog key ignored |
| Persisted restore round-trip | EditMode | `SetExplicitChoice(pt-BR)` | New service + Initialize (system=English) | `CurrentLanguageCode == pt-BR` |
| UI lifecycle under real OnEnable | PlayMode | Scene with `LocalizedText` | Toggle active, switch language, reload scene | Correct text, no leak/duplicate Apply |
| IL2CPP override deserialization | Build-context | IL2CPP + stripping | Deserialize override DTO in player | No missing-type/serialization exception |
| Clean import compiles | Build-context | Fresh project, declared deps only | Add package | Compiles |

---

## Quality Score

- **Overall score: 72 / 100**
- **Distance from top tier: 18**
- **Scope note:** the current `com.foxsterdev.loqui` v0.1.0 package surface (runtime + editor scanner + tests) as it exists at commit `d36cc84`. Not a project-wide score.
- **Scoring confidence: Low** — entirely static/source-inspection (broad and adversarially verified), but **no compile, EditMode, PlayMode, or IL2CPP runtime validation was possible** (Unity MCP bridge disabled, no editor running; ad-hoc batchmode disallowed by project memory). Per the doctrine, runtime-sensitive claims (allocation, stripping, culture, lifecycle) remain unproven.

### Dimension Breakdown
| Dimension | Weight | Score | Why |
|-----------|--------|-------|-----|
| Correctness & data integrity | 20 | 14 | Solid fallback-first core + defensive parser; but a dead advertised feature, `Ready` replay bug, non-ASCII key collapse, inert font fallback. No data corruption. |
| Architecture & ownership | 15 | 11 | Clean layering and exception-safe event; weakened by orphaned `Remote/` boundary, dual init owners, hard logger coupling, undefined static lifetime. |
| Safety, resilience, runtime stability | 15 | 12 | No crash/ANR/corruption found; minor re-entrancy and dropdown-null edges; missing main-thread contract is latent. |
| Security, privacy, abuse resistance | 15 | 13 | Remote payload size-capped + schema-validated; no PII in logs; trust surface small (overrides unapplied). |
| Validation & release confidence | 15 | **7** | **Capped ≤59%** per doctrine: EditMode-only, no IL2CPP/PlayMode/consumer evidence; packaging gaps (UGUI dep, 2021.3, inert link.xml). |
| Observability & operability | 10 | 7 | Reasonable logging seam, but several silent drops (stale pref, invariant fallback, rejected override). |
| Maintainability & change safety | 10 | 8 | Small focused files (except the 397-LOC scanner), good logic tests, clean `InternalsVisibleTo` seams. |
| **Total** | **100** | **72** | |

### Product Interpretation
Loqui is a **well-built localization core with a careful, defensible design** — but it is **not a top-tier, ship-ready public package yet.** Its main problems are things that are *advertised but unfinished* (remote overrides do nothing, fallback fonts aren't applied), *packaging that won't import cleanly elsewhere* (a missing TMP/UGUI dependency, an inert IL2CPP stripping file, a forced logging dependency), and *tests that can't prove the mobile/IL2CPP behavior they claim*. None of this corrupts data or crashes today; all of it is fixable with focused, low-risk work before publishing.

---

## Release Recommendation
- **Verdict:** **Do NOT publish as a public reusable package yet.** Internal use inside this repo is acceptable with the blockers tracked.
- **Why:** a documented feature is non-functional, the package won't import cleanly into a vanilla project, IL2CPP stripping protection is inert, and there is no runtime/IL2CPP validation behind the package's own claims.
- **Required next actions (publish gate):**
  1. **Packaging:** add `com.unity.ugui` (CF-04); decouple the logger behind a package-owned interface and drop the hard `thebestlogger` dep (CF-06); rename `linkmerge.xml → link.xml` + add DTO preserve entries (CF-05); verify or lower the `2021.3` floor (CF-07).
  2. **Honest feature surface:** either implement the remote-overrides apply path or remove it from `README`/`CHANGELOG`/`package.json` for 0.1.0 (CF-01); apply or remove the fallback-font fields (CF-03).
  3. **API contract:** latch `Loc.Ready` for late subscribers (CF-02); document main-thread-only + add a dev-build assertion (CF-09); make `LocalizationEvent.Raise` re-entrancy-safe (CF-08).
  4. **Validation ladder:** add PlayMode lifecycle tests, an IL2CPP+stripping build-context check, and a DemoProject consumer scene; re-home allocation/deserialization claims off Mono-JIT-only (CF-22, CF-23).
  5. **Editor tool:** hash-suffix fallback for non-ASCII keys (CF-18); scope the C# scanner as strictly advisory until lexer-backed (CF-19).

---

## Appendix — Verified-and-Dismissed (audit trail)
10 raw findings were rejected by adversarial re-verification (kept here so the bundle is reconstructable):
1. *State split CurrentLanguageCode vs PlayerPrefs* — Invalid: `RebuildActive` sets `CurrentLanguageCode` as part of the same call; divergence mechanism unreachable.
2. *Throw mid-`RebuildActive` leaves committed-but-stale state* — downgraded: no reachable exception in the rebuild body under 2022.3/IL2CPP (formatter/culture catch internally).
3. *Re-entrant raise silently drops UI listeners* (UI lens) — downgraded: listeners fire via the nested pass (the real defect is double-dispatch + swallowed NREs — kept as CF-08).
4. *First-frame flashes the inspector literal* — Invalid: `Initialize` is synchronous; `HandleEnable` applies current text synchronously.
5. *Parser missing key/empty/count validation* — Invalid: moot (no apply path consumes the payload).
6. *CSV/markdown export flattens newlines inconsistently* — Invalid: markdown flatten is correct-by-design; lossless JSON source-of-truth coexists.
7. *`LanguageDropdown.Rebuild` allocates per language change* — Invalid: `Rebuild` runs only on `OnEnable`/`Ready`; `LanguageChanged` is wired to `SyncSelection` (no realloc).
8. *Formatter `ToString` allocation untested for budget* — Invalid: describes correct idiomatic code, no defect.
9. *Destroyed `LocalizedText` stays subscribed* — Invalid: `OnDestroy` unsubscribes via `HandleDisable`.
10. *0.1.0 maturity / thin CHANGELOG* — Invalid: intentional pre-1.0 state, not a defect.

## Validation Contract
- `primary_validation_lane`: `batch_compile` · `secondary_validation_lane`: `interactive_mcp`
- `lane_selection_reason`: compile health + EditMode determinism + PlayMode UI-lifecycle are the real proof surface for this package.
- `expected_evidence_class`: compile + EditMode/PlayMode run via Unity MCP; IL2CPP build-context for stripping/deserialization claims.
- `validation_gaps`: **Unity MCP bridge disabled and no editor running this session; project memory forbids ad-hoc batchmode. No compile/EditMode/PlayMode/IL2CPP run was performed — all findings are source-inspection grade (adversarially cross-verified). Re-run with the bridge enabled to confirm CF-04/CF-05/CF-22 and lift scoring confidence above Low.**
