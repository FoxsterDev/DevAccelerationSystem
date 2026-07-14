# Contributing

Keep changes package-scoped. Preserve public APIs and Unity `.meta` GUIDs; do not add machine paths, private project content, secrets, Unity internal APIs, or runtime references to `UnityEditor`.

Run `python3 scripts/validate_repo.py` before opening a pull request. For Unity behavior, add focused test coverage and state the editor/version actually executed. Do not update package versions unless the package's API and compatibility impact is understood.
