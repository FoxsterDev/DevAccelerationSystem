# Validation

## License-free gate

Run from the repository root:

```bash
python3 scripts/validate_repo.py
```

The command validates package JSON, required metadata and documents, package IDs and semantic versions, unique assembly definitions, editor/runtime boundaries, package-local absolute paths, stale root-version references, internal documentation links, release-tag syntax, and tracked generated files. To validate a proposed release tag without creating it:

```bash
python3 scripts/validate_repo.py --release-tag com.foxsterdev.loqui/0.3.0
```

## Unity lanes

The intended lanes are compilation plus EditMode tests on Unity 2022.3, 6000.0, and 6000.3; PlayMode only where runtime behavior requires it; and a clean-project Git UPM install per package. Unity 2022.3.62f3 compiled the project after this foundation work, but the direct batch Test Runner invocation did not emit a result file, so no Unity test lane is recorded as passed. Unity 6000.0 and 6000.3 remain unverified. Do not describe a lane as passed until its result file exists.

The tracked consumer target is `DevAccelerationSystem.DemoProject/`. `DAS.LocalProject/` is local-only evidence.
