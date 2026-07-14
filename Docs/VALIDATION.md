# Validation

## License-free gate

Run from the repository root:

```bash
python3 scripts/validate_repo.py
```

The command validates package JSON, required metadata and documents, package IDs and semantic versions, unique assembly definitions, editor/runtime boundaries, package-local absolute paths, stale root-version references, internal documentation links, release-tag syntax, and tracked generated files. To validate a proposed release tag without creating it:

```bash
python3 scripts/validate_repo.py --release-tag com.foxsterdev.loqui/0.3.1
```

## Unity lanes

The release-foundation matrix is complete for the locally installed editors: Unity `2022.3.62f3`, `6000.0.61f1`, and `6000.3.3f1`. The detailed evidence is recorded in [the release-foundation report](../DevAccelerationSystem/Assets/AIOutput/ValidationReports/2026-07-14_release_foundation_validation.md).

- Unity `2022.3.62f3`: Dev Acceleration System EditMode `7/7`, Loqui EditMode `101/101`, TheBestLogger EditMode `266/266`, and TheBestLogger PlayMode `14/14` passed.
- Unity `6000.0.61f1` and `6000.3.3f1`: full-project EditMode `385/385` and PlayMode `14/14` passed on each editor.
- Clean local-file UPM imports passed for Dev Acceleration System + Loqui and for TheBestLogger on all three editors.

Git UPM URLs using the new package-specific tags cannot be tested until a maintainer authorizes and publishes those tags. Do not treat local-file imports as proof that an unpublished public URL resolves. No physical-device or IL2CPP result is claimed.

The tracked consumer target is `DevAccelerationSystem.DemoProject/`. `DAS.LocalProject/` is local-only evidence.
