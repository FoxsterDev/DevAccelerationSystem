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
- [x] Record Unity 2022.3.62f3 Test Runner results: DAS EditMode `7/7`, Loqui EditMode `101/101`, TheBestLogger EditMode `266/266`, and TheBestLogger PlayMode `14/14`.
- [x] Record clean-project local-file UPM imports for DAS + Loqui and TheBestLogger on Unity 2022.3, 6000.0, and 6000.3; package-specific tags are now published, while the fresh clean-project Git-tag import matrix remains pending.
- [x] Record Unity 6000.0.61f1 and 6000.3.3f1 full-project test results: EditMode `385/385` and PlayMode `14/14` on each editor.

## Later phases

- [x] Stabilize Compilation Matrix, TheBestLogger, and Loqui with executed Unity evidence: DAS EditMode `8/8`; Loqui EditMode `101/101`; TheBestLogger EditMode `269/269` and PlayMode `14/14`; source Android and StandaloneOSX compilation with zero errors; tracked consumer Android compilation with zero errors and Loqui PlayMode `4/4`.
- [x] Implement UPM Package Doctor, Define & Build Profile Doctor, and Project Baseline Audit as tested vertical slices. The default Unity Editor UI and batch runner are read-only; explicit preview/apply/backup/restore APIs cover define and color-space remediation.
- [ ] Prepare release candidate artifacts after Unity validation; never publish without authorization.
