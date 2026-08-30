# Unity Unified Harness Adapter: DevAccelerationSystem

## Scope

This repository-native adapter turns the compact Unity Harness contract into
project selection and truthful proof for `DevAccelerationSystem`. It is prompt
guidance, not a runtime, orchestrator, workflow engine, or task store.

## Journey Zero

The minimum representative path is:

1. route shared implementation to `DevAccelerationSystem/`;
2. run deterministic package-source proof in exact Unity `2022.3.62f3`;
3. when integration is affected, prove the local-file UPM boundary in
   `DevAccelerationSystem.DemoProject/` on the same exact editor;
4. report the exact build target and evidence ceiling;
5. produce zero outcome records when nothing durable is learned, otherwise one
   compact dated outcome in the existing project report owner.

`DAS.LocalProject/` may accelerate local repro when it exists, but it is never
required and never substitutes for tracked consumer proof.

## Four Lanes

### `docs`

Use for prose, comments, routing, Harness configuration, frozen eval data, and
other non-product changes.

Required proof:

- inspect the changed paths and references;
- run deterministic router/eval/static checks owned by the change;
- run `python3 scripts/validate_repo.py` when repository metadata or public
  package documentation may be affected;
- do not launch Unity or claim product regression coverage.

### `ordinary`

Use for contained C# or editor-tooling changes with a narrow behavior owner.

Required proof, selected rather than accumulated blindly:

- asmdef/source-boundary inspection;
- the narrowest relevant EditMode suite for deterministic/editor behavior;
- the narrowest relevant PlayMode suite for frame, dispatch, batching, scene,
  or runtime behavior;
- a package-source compile for each build target actually affected;
- tracked consumer proof only when package wiring or integration changed.

### `high-risk`

Use when failure could corrupt user state, silently change compatibility, or
escape narrow package-local reasoning. This includes:

- scenes, prefabs, ScriptableObjects, and other serialized assets;
- domain reload, editor startup/shutdown, play-mode transitions, or callbacks;
- save formats, persisted data, schema/config migrations, backup/restore;
- Addressables groups, content catalogs, remote content, or bundle compatibility;
- native SDK bridges, permissions, manifests, post-processors, stripping, or ABI;
- package Unity-floor, asmdef, scripting-define, or API compatibility changes.

Required proof:

- all relevant ordinary proof;
- before/after serialized or migration fixtures and recovery behavior;
- manual Editor inspection for scene/prefab layout or wiring changes;
- domain-reload/editor-lifecycle proof when callbacks or state retention matter;
- representative consumer proof for package or serialization compatibility;
- platform/player proof at the real boundary before platform claims.

### `release`

Use for package release readiness, supported-Unity changes, or broad rollout
claims.

Required proof:

- `python3 scripts/validate_repo.py --release-tag <package-id>/<version>`;
- exact source and tracked-consumer editor versions recorded;
- relevant EditMode, PlayMode, compile, and performance lanes;
- explicit build targets and scripting/build options;
- clean package-install proof from the exact proposed tag/commit when claimed;
- player/IL2CPP, native, Addressables, migration, or physical-device proof only
  when the release claim includes those surfaces.

Historical green reports are context, not a substitute for a current required
lane.

## Authoring And Consumer Boundary

- `DevAccelerationSystem/Assets/DevAccelerationSystem/` owns editor tooling and
  deterministic project-compilation checks.
- `DevAccelerationSystem/Assets/TheBestLogger/` owns reusable logger runtime,
  Editor, PlayMode, performance, and native-target integration.
- `DevAccelerationSystem/Assets/Loqui/` owns localization runtime/editor behavior.
- `DevAccelerationSystem.DemoProject/` proves local-file UPM consumption,
  integration, runtime flows, and tracked scene/serialized behavior.
- A consumer-only repair must remain consumer-local. A reusable behavior fix
  must land in its package source first.

Package-source success proves the authored package surface. Consumer success
proves only the selected consumer wiring and flow. Neither proves all supported
Unity versions or physical devices.

## Proof Routing And Ceilings

### Unity version and build target

- Both tracked projects declare Unity `2022.3.62f3` revision `96770f904ca7`.
- Package floors are `2020.3` for Dev Acceleration System and `2022.3` for
  TheBestLogger and Loqui; a project run does not by itself prove those floors.
- Record the exact executable version, not only `2022` or `Unity 6`.
- Record the exact target: Android, iOS, WebGL, StandaloneOSX,
  StandaloneWindows64, or another explicitly selected target.
- `CompilePlayerScripts` proves compilation for that target and option set. It
  does not prove a player build, IL2CPP, stripping, startup, runtime, or device.

### asmdef and compilation

- Verify references, `includePlatforms`, version defines, test assembly wiring,
  and editor/runtime boundaries before interpreting compile results.
- An `Editor/` directory under asmdef ownership is not automatically an
  editor-only assembly.
- Scripting symbols and version defines do not create assembly references.
- Compile only the targets affected by an ordinary change; use a wider matrix
  for compatibility or release claims.

### EditMode and PlayMode

- EditMode proves deterministic logic and editor tooling in the executed editor.
- PlayMode proves selected frame/runtime paths in the executed editor.
- A helper-only test does not prove the real runtime path.
- Neither mode proves native device observability, IL2CPP, or player startup.

### Scenes, prefabs, and serialized assets

- Text diff proves serialized text changed; it does not prove Unity can import,
  deserialize, or render the result.
- Open and inspect affected scenes/prefabs in the exact editor for layout,
  missing references, component wiring, and migration warnings.
- `LoggerSampleScene` is the tracked manual UI/wiring surface for the demo.
- Preserve `.meta` identity and verify that intended serialization changes do
  not include incidental reserialization noise.

### Domain reload and editor lifecycle

- Prove registration/unregistration, reload recovery, play-mode entry/exit, and
  editor shutdown for code owning static state, callbacks, background work, or
  package bridge lifecycle.
- A clean compile before reload is not proof of correct post-reload state.
- Use existing reliable Unity MCP lifecycle lanes; do not add a watchdog or daemon.

### Save data and migrations

- Treat destructive or compatibility-sensitive save/config schema changes as
  high-risk.
- Prove old-to-new migration, already-new idempotence, malformed/partial input,
  backup or recovery behavior, and downgrade limits where supported.
- Never use production user data to manufacture a pilot.

### Addressables and content catalogs

- A source compile does not prove Addressables groups, bundle build, catalog
  compatibility, remote paths, cache behavior, or content update restrictions.
- When these files are touched, inspect serialized settings, build the relevant
  content lane, and validate a representative consumer/player flow.
- If the repository has no affected Addressables surface, record the lane as
  not applicable instead of inventing proof.

### Native SDKs, permissions, and devices

- Inspect managed/native assembly boundaries, manifests/entitlements,
  post-processing, permission timing, callback lifetime, and stripping.
- Editor and PlayMode tests may prove managed routing through a test seam.
- Simulator/emulator proof is not physical-device proof.
- Claim native logging, crash capture, permissions, ABI, performance, or OS
  lifecycle behavior only after the required player and physical-device lane.
- Do not import iOS accessibility or generic Apple-surface requirements unless
  the Unity change actually owns an accessible UI or platform contract.

## Tooling Route

Prefer existing native surfaces:

- `scripts/validate_repo.py` for license-free deterministic repository checks;
- Unity Test Runner assemblies already declared by the package roots;
- Dev Acceleration System compilation checks for their narrow editor-tooling contract;
- XUUnity Light Unity MCP readiness, compile, EditMode, PlayMode, lifecycle, and
  version-matrix lanes when Hub augmentation is available.

Do not use the legacy macOS shell compilation runner as a new Harness broker.
Do not change MCP pins/configuration or product runtime only to force a pilot.

## Harness-Owned Gate Scope

A Codex Stop gate may validate only Harness-owned routing/config/eval paths,
including:

- `AGENTS.md` and legacy router compatibility files;
- `Docs/ai/unity-unified-harness-adapter.md`;
- `scripts/refresh_harness_routing.py`;
- a future frozen Harness eval/scorer owned by the same contract.

The gate may run deterministic generator, schema, scorer, and static checks. It
must not launch Unity, run product regression, mutate packages, or block normal
Unity development when no Harness-owned path changed. Hook trust and enablement
remain a native owner action through `/hooks`.

## Version Decision

Harness-only routing and configuration changes do not change product package
versions. Current source versions remain:

- `com.foxsterdev.devaccelerationsystem` `1.1.0`;
- `com.foxsterdev.thebestlogger` `4.4.2`;
- `com.foxsterdev.loqui` `0.3.2`.

The two tracked projects currently pin XUUnity Light Unity MCP `v0.3.43`.
Changing that development dependency is a separate, explicit tool-upgrade task
requiring package resolution and live proof; this Harness refresh does not do it.
