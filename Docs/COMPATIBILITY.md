# Compatibility policy

`package.json` declares each package's minimum compatible Unity version. Compatibility is only *verified* after the relevant compile and test lanes complete.

| Package | Declared floor | Modernization verification target |
| --- | --- | --- |
| Dev Acceleration System | 2020.3 | 2022.3 LTS, 6000.0 LTS, 6000.3 LTS |
| TheBestLogger | 2022.3 | 2022.3 LTS, 6000.0 LTS, 6000.3 LTS |
| Loqui | 2022.3 | 2022.3 LTS, 6000.0 LTS, 6000.3 LTS |

Unity 2022.3 remains a declared floor while it is tested. Unity 6.0 LTS is supported through October 2026; Unity 6.3 LTS is the current LTS and is supported through December 2027. The requested `6000.3` lane must be checked against the exact installed editor before being called supported.

Raising a minimum Unity version is a breaking package change. It requires a major version, changelog entry, migration note, and successful installation/test evidence. No floor has been raised by the release-foundation changes.
