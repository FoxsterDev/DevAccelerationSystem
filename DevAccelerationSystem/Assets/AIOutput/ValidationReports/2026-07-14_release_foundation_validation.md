# Release Foundation Validation — 2026-07-14

## Scope

Validation of the release-foundation candidates in this repository. No package tag, push, GitHub release, or registry publication was performed.

## Environment

- Unity Editors `2022.3.62f3`, `6000.0.61f1`, and `6000.3.3f1`
- macOS batch-mode Unity Test Runner
- Repository validation: `python3 scripts/validate_repo.py`

## Unity Test Runner results

| Package lane | Result |
| --- | --- |
| Dev Acceleration System EditMode (`DevAccelerationSystem.Tests.Editor`) | 7 / 7 passed |
| Loqui EditMode (`Loqui.Tests`) | 101 / 101 passed |
| TheBestLogger EditMode (`TheBestLogger.Tests.Editor`) | 266 / 266 passed |
| TheBestLogger PlayMode (`TheBestLogger.Tests.PlayMode`) | 14 / 14 passed |
| Unity 6000.0.61f1 full-project EditMode | 385 / 385 passed |
| Unity 6000.0.61f1 full-project PlayMode | 14 / 14 passed |
| Unity 6000.3.3f1 full-project EditMode | 385 / 385 passed |
| Unity 6000.3.3f1 full-project PlayMode | 14 / 14 passed |

## Clean UPM import results

- Clean temporary projects imported the current Dev Acceleration System + Loqui candidates and the current TheBestLogger candidate through local `file:` package references successfully on Unity `2022.3.62f3`, `6000.0.61f1`, and `6000.3.3f1`.
- The historical public Git tag `4.4.0` was not used as a release result: a clean Loqui Git UPM import from that snapshot failed because `JsonUtility`'s module dependency was not declared. Candidate `0.3.1` declares `com.unity.modules.jsonserialize`; TheBestLogger candidate `4.4.1` also declares its required JSON, IMGUI, and UnityWebRequest modules.

## Remaining gaps

- Public Git UPM installs using `com.foxsterdev.devaccelerationsystem/1.0.2`, `com.foxsterdev.thebestlogger/4.4.1`, and `com.foxsterdev.loqui/0.3.1` cannot be verified until a maintainer authorizes and creates those tags.
- No physical-device or IL2CPP validation is claimed by this report.
