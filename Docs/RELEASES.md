# Release policy

Each package's `package.json` is canonical for its version. Package changelog headings, release notes, package-specific tags, and OpenUPM metadata must agree exactly. Use SemVer: patch for compatible fixes/docs, minor for compatible features, major for breaking public API, behavior, or Unity-floor changes.

## Package-specific tags

New releases use these exact tag names:

- `com.foxsterdev.devaccelerationsystem/<version>`
- `com.foxsterdev.thebestlogger/<version>`
- `com.foxsterdev.loqui/<version>`

For example, `com.foxsterdev.loqui/0.3.1`. The prefix is the OpenUPM `gitTagPrefix` value for that package. Historical bare tags remain historical snapshots and must not be treated as independent package releases.

## Lifecycle

1. Update the affected package manifest, changelog, README, migration note, and draft release notes.
2. Run `python3 scripts/validate_repo.py` and the applicable Unity test/install lanes.
3. Review [RELEASE_CHECKLIST.md](./RELEASE_CHECKLIST.md).
4. With maintainer authorization, create the package-specific tag and publish the matching GitHub/OpenUPM release.

No tag, push, GitHub release, or registry submission is created by repository CI.
