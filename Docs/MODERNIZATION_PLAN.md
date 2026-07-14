# Modernization plan

## Phase 0 — verified baseline

- [x] Verify package roots, manifests, history, tags, remote state, and tracked generated files.
- [x] Record the package-layout decision in ADR-001.
- [x] Identify legacy global-tag ambiguity and preserve current Git UPM paths.

## Phase 1 — release foundation

- [x] Make the root documentation represent all three packages and their manifest versions.
- [x] Add missing DAS package README, changelog, license, and package metadata.
- [x] Add compatibility, migration, contribution, release, troubleshooting, and checklist documentation.
- [x] Define package-specific tag strategy without creating tags.

## Phase 2 — reproducible validation

- [x] Add a single local license-free validation command and PR workflow.
- [ ] Record Unity Test Runner results on 2022.3, 6000.0, and 6000.3 (2022.3.62f3 compilation succeeded; no test result XML was emitted).
- [ ] Record clean-project Git UPM import results from Unity.

## Later phases

- [ ] Stabilize Compilation Matrix, TheBestLogger, and Loqui with executed Unity evidence.
- [ ] Implement UPM Package Doctor, Define & Build Profile Doctor, and Project Baseline Audit as tested vertical slices.
- [ ] Prepare release candidate artifacts after Unity validation; never publish without authorization.
