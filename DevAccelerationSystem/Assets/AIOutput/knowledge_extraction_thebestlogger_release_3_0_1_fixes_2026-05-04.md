# XUUnity Knowledge Extract

## Source
- Type: implementation and release workstream
- Topic: `TheBestLogger` `3.0.1` stabilization fixes across runtime package and tracked demo consumer
- Scope:
  - `Assets/TheBestLogger/`
  - `../DevAccelerationSystem.DemoProject/Assets/TheBestLoggerSample/`
- Source summary:
  - fixed Apple native-log bridge compile fallout
  - fixed editor-only `UnityEditor` sample code compiling inside runtime assembly scope
  - fixed missing `UniTask` assembly reference in sample asmdef despite active compile symbols
  - fixed diagnostics-enabled player code referencing editor-only reflective console logger
  - fixed iOS preprocess build hook null-config crash
  - packaged the result as release `3.0.1`

## Problem
- Unity assembly-definition boundaries and preprocessor branches allowed editor-only or package-optional code to compile in the wrong assembly or platform context.
- Build tooling assumed optional monitoring config was always present and crashed the iOS preprocess step when it was absent.
- Release knowledge risk:
  - the fixes are small individually
  - the underlying failure family is reusable and likely to recur if not captured as durable rules

## Solution
- Isolated sample editor-only crash-reporting code into a dedicated editor asmdef.
- Added an explicit `UniTask` asmdef reference to the sample assembly instead of relying only on compile symbols.
- Narrowed editor-only diagnostics logging to `UNITY_EDITOR` even when diagnostics are enabled for player builds.
- Made the crash-reporting preprocess hook treat missing monitoring config as a disabled path instead of a build-time exception.
- Released the grouped stabilization changes as `3.0.1`.

## Extracted Knowledge
- Durable rules:
  - Under an asmdef-managed source tree, an `Editor/` folder name alone does not guarantee editor-only compilation.
    - If editor-only source lives under a runtime asmdef scope, it needs its own editor asmdef or equivalent assembly/platform boundary.
  - `versionDefines` and scripting symbols do not create package assembly references.
    - If a code path behind a define uses `UniTask` or another package type, the owning asmdef must still reference that package assembly explicitly.
  - Diagnostics-enabled player builds must not reference editor-only helper types just because the same error-reporting block also runs in editor.
    - Nest `UNITY_EDITOR` around editor-only calls inside wider diagnostics blocks.
  - Build preprocess hooks that load optional `Resources` assets must treat a missing asset as disabled configuration rather than throwing during the build pipeline.
  - For tracked consumer validation, sample-assembly wiring errors are release-relevant even when the package runtime code itself is correct.
- Non-durable details:
  - exact file paths of the current sample assets
  - the local uncommitted `OpenSearchLogTargetConfiguration.asset` change that was intentionally excluded from the release commit

## Retrospective
- The failure family was not one bug but a cluster around Unity compile boundaries:
  - asmdef ownership
  - optional package references
  - editor-only helper leakage
  - optional config assumptions in build hooks
- The reusable lesson is to review Unity changes not only by file intent but by actual assembly context and preprocessor surface.

## Candidate Outputs
- Project-local memory candidate:
  - update `ProjectMemory/platform_constraints.md` with asmdef, optional-package-reference, diagnostics, and build-hook rules
  - refresh `ProjectMemory/README.md` and `ProjectMemory/release_rules.md` so package version memory matches current source
- Review-artifact-only remainder:
  - the grouped release `3.0.1` summary itself
- Shared knowledge candidate:
  - none yet; these rules are validated here but still tightly shaped by this workspace's Unity package-and-consumer layout

## Destination File
- Primary report:
  - `Assets/AIOutput/knowledge_extraction_thebestlogger_release_3_0_1_fixes_2026-05-04.md`
- Applied durable rules:
  - `Assets/AIOutput/ProjectMemory/platform_constraints.md`
  - `Assets/AIOutput/ProjectMemory/README.md`
  - `Assets/AIOutput/ProjectMemory/release_rules.md`

## Confidence
- High for project-local reuse
- Medium for broader shared-prompt promotion
