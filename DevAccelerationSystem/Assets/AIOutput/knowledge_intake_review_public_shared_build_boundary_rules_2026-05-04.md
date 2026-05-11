# XUUnity Knowledge Intake Review

## Source
- Type: implementation bug-fix and release hardening pass
- Topic: public-safe Unity assembly-boundary, optional-package, diagnostics, and build-hook rules
- Scope:
  - package runtime code
  - tracked demo consumer assembly wiring
  - release `3.0.1` stabilization fixes
- Source summary:
  - editor-only code compiled in runtime asmdef scope because folder naming was relied on instead of assembly boundaries
  - compile symbols enabled `UniTask` code without an explicit asmdef reference to the `UniTask` assembly
  - diagnostics-enabled player code referenced an editor-only reflective console helper
  - iOS preprocess build hook crashed when optional config was absent

## Extracted Knowledge
- Durable rules:
  - Under asmdef ownership, an `Editor/` folder name alone does not guarantee editor-only compilation.
  - `versionDefines` and scripting symbols do not add assembly references; package types still require explicit asmdef references.
  - Diagnostics-enabled player branches must not reference editor-only helpers outside a narrower `UNITY_EDITOR` guard.
  - Build preprocess hooks that load optional config must treat missing config as a disabled path instead of a build-stopping exception.
  - For package or SDK repos with tracked sample or consumer workspaces, sample assembly wiring and compile health are release-relevant evidence, not optional polish.
- Non-durable examples or narrative:
  - current project names, GUIDs, file paths, and release tag details
  - one specific `UniTask` or crash-reporter implementation
- Project-specific details:
  - none required for the approved shared rules after narrowing

## Candidate Outputs
- Review artifact candidate:
  - this intake-review report
- Skill candidate:
  - no new skill family needed
- Shared knowledge candidate:
  - update `AIRoot/Modules/XUUnity/knowledge/decision_rules.md` with the four compile-boundary and build-hook rules
- Project-only candidate:
  - none required beyond already-applied local memory updates
- No-action remainder:
  - project-local release narrative and exact file inventory

## Existing Coverage
- Existing shared files:
  - `AIRoot/Modules/XUUnity/knowledge/decision_rules.md`
  - `AIRoot/Modules/XUUnity/reviews/release_readiness_review.md`
- Existing project override files:
  - `Assets/AIOutput/ProjectMemory/platform_constraints.md`
- Overlap summary:
  - current shared rules already cover validation overclaiming, stale build artifacts, and public-safe specificity
  - current shared rules do not explicitly cover asmdef folder-boundary traps, symbol-versus-reference traps, or optional-config behavior in build preprocess hooks
- Existing family sufficient:
  - yes
- New skill family needed:
  - no

## Quality Evaluation
- Technical quality: 5
- Production safety: 5
- Unity `6000+` relevance: 4
- Mobile relevance: 4
- Zero-crash and zero-ANR alignment: 4
- Performance and microfreeze impact: 2
- Novelty: 4
- Merge fitness: 5
- Expected usefulness: 5

## Impact Analysis
- What problem this knowledge solves:
  - prevents recurring Unity compile-surface mistakes around asmdefs, optional packages, and editor-only leakage
  - hardens build-hook behavior against optional-config absence
  - raises release-review quality for package repos with tracked samples
- What becomes better if integrated:
  - future `xuunity` bug-fix and review work can catch these failure families earlier
  - shared public guidance becomes more concrete for Unity package and SDK work
- What does not improve even after integration:
  - this does not replace real Unity validation or device proof
  - this does not by itself resolve private project-specific package wiring
- Risk of semantic loss during merge:
  - low if merged as compact rules into existing shared files

## Recommendation
- Recommended action:
  - update shared public knowledge only
- Candidate destination:
  - primary: `AIRoot/Modules/XUUnity/knowledge/decision_rules.md`
  - secondary: `AIRoot/Modules/XUUnity/reviews/release_readiness_review.md`
- Destination-specific recommendations:
  - `decision_rules.md`:
    - add the asmdef boundary, symbol-versus-reference, diagnostics guard, and optional-config build-hook rules
  - `release_readiness_review.md`:
    - add one release-evidence check for tracked sample or consumer workspace compile health in package-style repos
- External promotion candidate:
  - no
- Target external repo id:
  - none
- New family or topic proposal:
  - none
- Shared vs project-specific split:
  - shared:
    - all approved rules above
  - project-specific:
    - none needed in this integration step
- Required narrowing or cleanup:
  - keep wording project-agnostic

## Approval Options
- `yes, integrate`
- `apply all approved items`
- `apply shared knowledge only`
- `apply only these destinations`
- `update shared only`
- `reject`
