# Release checklist

- [ ] Package `package.json` version, changelog, README, and draft notes agree.
- [ ] `python3 scripts/validate_repo.py --release-tag <package-id>/<version>` passes.
- [ ] Relevant Unity compile/Test Runner lanes have recorded results.
- [ ] Git UPM clean installation is tested from the exact tag/commit.
- [ ] Public API and Unity-support changes have migration notes.
- [ ] Proposed OpenUPM metadata has the package-specific `gitTagPrefix`.
- [ ] Maintainer explicitly authorized tag, push, GitHub release, and any OpenUPM submission.
