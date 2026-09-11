# Release version policy

The next published release is **0.1.0 Alpha**.

- `VERSION` and `GameVersion.Current` use technical SemVer (`0.1.0-alpha`). This value is written
  to save metadata, manifests and Windows package filenames.
- `GameVersion.Display` is the friendly in-game label (`0.1.0 Alpha`). It may be shown in menus,
  diagnostics and the title header without changing the technical identifier.
- Each future published build increments the version manually and records a short changelog entry:
  `0.1.1-alpha`, `0.1.2-alpha`, and so on. Development-only commits do not bump the published
  version automatically.

## 0.1.0 Alpha

Initial tracked Alpha release containing the fullscreen research workspace, stellar and gate
presentation, perimeter/territory and colorful navigation work, and save/window lifecycle
hardening. Build, runtime, and native visual checks remain release-gate evidence rather than a
claim that every final acceptance lane is complete.
