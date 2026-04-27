# XUUnity Knowledge Intake Review

## Source
- Type: engineering workstream and iterative UI/validation hardening
- Topic: `TheBestLoggerSample` scene-authored validation UI, crash-lab preservation, and Unity-version-safe sample behavior
- Scope: `DevAccelerationSystem.DemoProject/Assets/TheBestLoggerSample/`
- Source summary:
  - replaced runtime-generated sample UI with a scene-authored editable Unity Canvas surface
  - kept logger actions, managed exception actions, and native iOS crash triggers available through the new scene UI
  - moved dynamic action lists to pooled button instances using `UnityEngine.Pool.ObjectPool<T>`
  - hardened template button binding against missing serialized references
  - kept the sample compatible with Unity `2021`, `2022`, and `6000.3` constraints discovered during the pass

## Extracted Knowledge
- Durable rules:
  - In a Unity consumer-validation workspace, use scene-authored UI for manual QA and product-facing sample screens when the owner explicitly needs hand-editable layout iteration in the Editor.
    - Runtime-generated layout is a poor fit for premium UX tuning, manual spacing fixes, and direct scene ownership.
  - Keep presentation and action wiring separate for sample validation surfaces.
    - The scene should own cards, rows, colors, anchors, and typography.
    - A binder component may populate labels and wire button callbacks without creating the UI structure itself.
  - For dynamic UGUI action lists in editable scenes, prefer a pre-authored hidden template plus `ObjectPool<T>` over runtime `AddComponent` construction.
    - This preserves Editor ownership of visuals while still allowing flexible lists.
  - Pooled scene-template buttons must tolerate partially missing serialized references and self-heal before binding.
    - Template instances created from scene-authored prefabs should re-resolve `Button`, `Image`, and `Text` references during bind/reset paths to avoid one bad scene reference killing the whole list build.
  - Consumer sample UI that is used for runtime-failure drills must preserve dangerous test coverage during redesign.
    - Exception buttons and native crash triggers are not secondary decoration; they are part of the validation contract and must survive UI refactors.
  - When the same sample must work across Unity `2021`, `2022`, and `6000.3`, treat built-in font differences and sample-scene rendering behavior as validation-relevant platform constraints, not just visual polish issues.
- Non-durable examples or narrative:
  - exact button labels, card titles, and palette values
  - one-off layout polish decisions such as specific paddings or scrollbar widths
  - intermediate YAML surgery mistakes and broken scene states during refactor
  - specific NRE and warning cleanup incidents used to stabilize this pass
- Project-specific details:
  - `LoggerSampleScene.unity` is the manual validation surface for `TheBestLoggerSample`
  - the sample currently uses scene-authored UGUI plus `GameLoggerSampleSceneUi` as the binder
  - managed exception and native crash coverage are explicit parts of the sample surface, not hidden debug leftovers

## Candidate Outputs
- Review artifact candidate:
  - this report as a project-bound extraction package
- Project-only candidates:
  - update `Assets/AIOutput/ProjectMemory/testing_strategy.md`
    - add explicit manual validation expectation for `LoggerSampleScene`
    - state that sample-UI refactors must preserve crash/exception test affordances
  - update `Assets/AIOutput/ProjectMemory/platform_constraints.md`
    - record Unity `2021` / `2022` / `6000.3` compatibility expectation for the sample workspace
    - record that sample-scene visual validation differs across engine lines and should be checked in Editor
  - update `Assets/AIOutput/ProjectMemory/release_rules.md`
    - clarify that editor-open visual proof for `LoggerSampleScene` is required for meaningful sample-UI changes
- Internal-shared or public-core candidate:
  - existing UI skill guidance could absorb one reusable rule:
    - when a Unity user explicitly wants to hand-edit a validation UI in scene, prefer scene-authored UGUI plus binder/pooling over code-generated hierarchy
  - existing UGUI or layout guidance could absorb one reusable rule:
    - for dynamic UGUI lists in authored scenes, use a template + `ObjectPool<T>` instead of runtime component synthesis
- No-action remainder:
  - exact light-theme palette
  - exact section copy
  - one-off sample-specific hierarchy choices
  - intermediate bug-fix chronology

## Existing Coverage
- Existing project override files:
  - `Assets/AIOutput/ProjectMemory/testing_strategy.md`
  - `Assets/AIOutput/ProjectMemory/platform_constraints.md`
  - `Assets/AIOutput/ProjectMemory/release_rules.md`
- Existing shared files:
  - `AIRoot/Modules/XUUnity/skills/ui/ugui.md`
  - `AIRoot/Modules/XUUnity/skills/ui/layout_and_rebuilds.md`
  - `AIRoot/Modules/XUUnity/skills/fx/pooling_and_spawn_budget.md`
- Overlap summary:
  - project memory already knows this workspace is a consumer-validation workspace, but it does not yet capture the manual sample-scene validation contract
  - shared UI skills likely already cover general UGUI practice, but they may not explicitly capture the scene-authored-versus-runtime-generated decision rule for Editor-owned validation surfaces
  - pooling guidance exists broadly, but not necessarily as a UI-template rule tied to scene editability
- Existing family sufficient:
  - yes for project memory
  - probably yes for shared UI skill families if anything is promoted
- New skill family needed:
  - no

## Quality Evaluation
- Technical quality: medium-high
- Reuse value: medium
- Project-memory fit: high
- Shared-skill fit: medium
- Public-safety: high
- Internal-sensitivity: low
- Routing confidence: high for project-only, medium for shared

## Impact Analysis
- What problem this knowledge solves:
  - prevents future regressions where sample validation UI becomes hard to edit, visually unstable, or decoupled from required crash/exception test coverage
  - clarifies that UX polish decisions for this sample are not cosmetic only; they affect validation usability
- What becomes better if integrated:
  - future UI work in this sample can be judged against a clearer Editor-ownership contract
  - Unity-version validation expectations become explicit instead of tribal knowledge
  - shared guidance could reuse the binder-plus-template-plus-pool pattern for other editable validation screens
- What does not improve even after integration:
  - this does not replace opening the scene in Unity Editor and visually validating it
  - this does not prove cross-device quality automatically
- Risk of semantic loss during merge:
  - medium if scene-specific validation rules are over-promoted into generic shared UI doctrine
  - low if the reusable pooling and authored-scene rules stay small and the rest stays project-local

## Recommendation
- Recommended action:
  - keep this as a reviewed extraction package first
  - after approval, apply project-only items into `testing_strategy.md`, `platform_constraints.md`, and possibly `release_rules.md`
  - do not promote shared-skill updates unless the team wants this pattern reused beyond this sample
- Candidate destination:
  - primary: `DevAccelerationSystem.DemoProject/Assets/AIOutput/ProjectMemory/`
  - optional shared candidate: `AIRoot/Modules/XUUnity/skills/ui/ugui.md` or `AIRoot/Modules/XUUnity/skills/ui/layout_and_rebuilds.md`
- Destination-specific recommendations:
  - `testing_strategy.md`:
    - add `LoggerSampleScene` as a manual validation surface for sample-UI and crash-lab work
    - add rule that UI refactors must preserve managed/native crash-test affordances
  - `platform_constraints.md`:
    - add explicit Unity `2021` / `2022` / `6000.3` compatibility expectation for this sample workspace
    - add note that engine-line visual checks matter for sample UI
  - `release_rules.md`:
    - add rule that visual/editor-open proof is part of release-facing confidence for meaningful sample-UI changes
  - shared UI skill:
    - only promote the narrow reusable rule about scene-authored validation surfaces and pooled template lists
    - do not promote `TheBestLoggerSample` naming, palette, or crash-lab-specific details
- External promotion candidate:
  - no
- Shared vs project-specific split:
  - shared:
    - scene-authored validation UI should stay scene-owned when manual editing is a requirement
    - dynamic UGUI lists in editable scenes should use template + pool rather than runtime hierarchy synthesis
  - project-specific:
    - `LoggerSampleScene` as the validation surface
    - crash/exception preservation as part of this sample’s contract
    - Unity-version expectations for this workspace

## Approval Options
- `apply project-only items`
- `apply testing_strategy only`
- `apply platform_constraints only`
- `apply release_rules only`
- `apply project-only items and prepare shared-skill draft`
- `apply review artifact only`
- `reject`
