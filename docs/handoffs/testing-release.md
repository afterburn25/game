# Testing / Release handoff

## Scope and baseline

- Owner: Testing / Release, persistent branch `work/testing-release`; coordination issues #29 and #15.
- Reviewed baseline: `integration` at `c529a1a765776c0940f88002410bc70db740d05a`.
- This milestone repairs issue #61's false-positive ordinary Godot smoke gate. It does not change gameplay, saves, balance, or the pinned Godot version/hash.
- No acceptance or merge is implied by this handoff. Core owns promotion of the final validated candidate.

## Independently reproduced baseline defect

- Exact-head [integration build 34246893010](https://github.com/afterburn25/stellar-continuum/actions/runs/34246893010), job `102131057323`, reported success while its runtime log contained seven `Cannot instantiate C# script` errors, including `IntegratedMain`, controls, inspection, exploration, logistics, relations, and the menu.
- Exact-head [screenshot run 34246892848](https://github.com/afterburn25/stellar-continuum/actions/runs/34246892848), job `102131056391`, built both Release and Debug and emitted the four capture markers plus `STELLAR_SCREENSHOT_CAPTURE_COMPLETE`.
- This corroborates [issue #61's documented root cause](https://github.com/afterburn25/stellar-continuum/issues/61#issuecomment-5587484235): the ordinary project runner loads Debug, while the old build gate created only Release.
- The screenshot log also reports the CI host's absent ALSA output device. The headless smoke explicitly selects Dummy audio rather than suppressing an engine-error category.

## Candidate changes

- Preserve all existing research, Release build, simulation, Core, fair-information, Logistics, Species, pinned Godot download/hash, and process-timeout gates.
- Build Debug explicitly for the ordinary Godot editor/project runner.
- `IntegratedMain` emits one startup marker only after campaign initialization returns successfully and its first integrated frame returns successfully. All child-script errors remain failures through the log guard.
- Capture both editor/runtime streams with `pipefail`, reject engine/script/managed failures, and require the exact startup marker for runtime. Retain logs as a 14-day CI artifact even on failure.
- Add seven regression tests, including the historical exit-zero class-instantiation failure, missing startup proof, errors before/after proof, resource/child/managed errors, ANSI output, benign warning handling, and missing/empty-log CLI exits.

## Validation so far

- PASS: seven Python smoke regression tests.
- PASS: all seven research validators/benchmarks called by the shared build workflow.
- PASS: the new CLI rejects the seven actual accepted-head runtime errors extracted from job `102131057323` and exits 1 as required.
- PASS: `git diff --check`.
- Local machine has SDK `10.0.302`, .NET 8 runtime and reference pack `8.0.29`; no local Godot executable was found.
- Local .NET restore is blocked in this agent by access to the user's NuGet configuration/cache; an isolated workspace profile also cannot reach NuGet through the restricted network. Core has separately obtained narrow read permissions and will run the .NET checks from its permitted context.
- Required before acceptance: Release and Debug builds, all existing .NET gates, and the changed pinned-Godot CI smoke at the exact final candidate head. Existing green CI on the old baseline is not evidence that this candidate's new semantic gate passed.

## Next checks

- Independently validate Core's combined visual/runtime candidate when it is ready, including the recovered-backup preservation milestone.
- Keep #29's visual-asset import/legibility request and #219's observer-safe galaxy-map request visible. Renderer-specific 500/2,000-system map measurements remain follow-up work when the strategic-map milestone exists; do not claim measured rendering performance from static source review.

## Playable Windows demo packaging milestone

- Testing worktree fast-forwarded non-destructively to local Core candidate `42bce3f3b7824a5fbe9c80c7cb4d99598b90d89b`. Packaging changes belong to the existing `work/testing-release`; no gameplay, main-scene, or UI source changes.
- `export_presets.cfg` exports the same shared project as Windows x64 Release and Linux x64 Release. Godot's .NET exporter publishes self-contained runtime files; the packager rejects an export missing the Game assembly, GodotSharp, coreclr, hostfxr, runtime configuration, resource pack, or loose public research catalog.
- Dedicated `Windows Playable Demo` workflow runs on Core/testing branch pushes, integration PRs, and manual dispatch. It keeps the existing build/quality workflow intact.
- Official editor and .NET export templates are pinned to Godot 4.7.2. SHA-256 values were verified against [official release asset metadata](https://github.com/godotengine/godot/releases/tag/4.7.2-stable): editor `129f82db7bafd54ae14bb5bb284041c73860e8c7a009a3a026ca5e946cbff247`; templates `92f8681e349ef1f90891b792da95e3b2b0bd1ed610b78018c58feb2d87e15a9d`. CI verifies both downloads before extraction/execution.
- Linux CI builds Release and Debug, imports assets, proves source startup, exports both platforms, and runs the actual Linux export outside the checkout with fresh user data. It produces a one-day Windows **candidate** artifact.
- Hosted Windows CI verifies ZIP/per-file SHA-256 and exact workflow commit, extracts the candidate into a temporary directory, and launches only the canonical game executable once with hidden-window creation, Dummy audio, and a 60-second process timeout. It captures all output and requires clean logs plus `STELLAR_RUNTIME_READY IntegratedMain`. Timeout kills only that exact child; there are no retries or WER/global setting changes. The smoke command refuses to execute outside Windows GitHub Actions.
- Only successful Windows startup publishes final artifact `stellar-continuum-windows-x64-<full commit SHA>`, retained 30 days. It contains the complete ZIP and SHA-256 sidecar. `BUILD.json` records source commit, VERSION, CI URL, pinned tools, file checksums, and actual Windows/Linux startup status. Godot license/copyright notices and a player README ship in the ZIP; no canonical binaries are committed.
- Local validation: nine packaging/download/runner-guard regressions pass without executing Windows binaries; export preset names/options were checked against Godot 4.7.2 source; `git diff --check` passes. The local Python runtime lacks PyYAML, so CI is authoritative for workflow parsing and both exports.
- **Not yet claimed:** a successful export or downloadable artifact until Core publishes this tree and the exact-head workflow completes. Local Godot/NuGet access remains restricted. Core should monitor both jobs and return the final artifact URL; an export-only candidate is not the final demo.
- **Remaining acceptance limits:** hosted headless Windows startup proves actual executable/runtime/scene initialization; it does not certify keyboard/mouse interaction or graphics on the user's GPU. The package is unsigned and keeps the existing development version. Integration/main promotion still requires Core's explicit acceptance and the existing complete gates.

Export option/provenance references: [Godot Windows export documentation](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_windows.html), [pinned .NET export plugin](https://github.com/godotengine/godot/blob/4.7.2-stable/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs), and [pinned self-contained publish command](https://github.com/godotengine/godot/blob/4.7.2-stable/modules/mono/editor/GodotTools/GodotTools/Build/BuildSystem.cs).

### Concrete import failure and follow-up

- Remote Core head `8d3d746f11e1aa1d59155c329fe92e46df013fc6`: [PR build 34273236869](https://github.com/afterburn25/stellar-continuum/actions/runs/34273236869) passed source/.NET gates but correctly failed runtime on missing SVG loaders. Its editor log says `WARNING: Scan thread aborted...` after the fixed five-frame editor limit.
- [Screenshot run 34273230857](https://github.com/afterburn25/stellar-continuum/actions/runs/34273230857) had no import barrier, hit the same icon exceptions, then failed because the aborted Exploration panel never created its Colony Sites button. Artifact `10074669879` contains two incomplete captures, not a validated screenshot set; smoke artifact `10074686128` retains the PR failure logs.
- Follow-up replaces the fixed-frame editor exit with bounded `--editor --import`, adds the same complete-import barrier before screenshot capture, rejects the observed scan-aborted warning, and explicitly selects Dummy audio for all headless demo commands. No source/gameplay fallback or weakened runtime assertion was introduced.
- Local follow-up checks: seven smoke regressions and nine demo packaging regressions pass; exact-head fresh CI remains required.

### First export run and missing solution repair

- Remote Core head `ed67fc4459de6e98f04ea9dbbd0dc0f69c396044`: [PR build 34274317079](https://github.com/afterburn25/stellar-continuum/actions/runs/34274317079) passes all gates, and [screenshot run 34274310993](https://github.com/afterburn25/stellar-continuum/actions/runs/34274310993) passes all four captures and positive startup. Screenshot artifact: `10075097812`.
- [First Windows demo run 34274310937](https://github.com/afterburn25/stellar-continuum/actions/runs/34274310937) verified official editor/templates hashes and source startup, then failed because Godot's .NET exporter requires `Game.sln`. Existing `.csproj`-only ordinary builds do not satisfy that exporter requirement.
- Added `Game.sln` referencing only the existing shared `Game.csproj`, with Debug, Release, ExportDebug, and ExportRelease mappings. No gameplay code or project separation. The full export/Windows startup jobs must pass on the next published head before a final artifact can be claimed.

### Playable artifact and expanded visual acceptance

- Remote Core head `cc304a6e4c7cba90809169282d6572b1b42c4457`: [PR build 34275652857](https://github.com/afterburn25/stellar-continuum/actions/runs/34275652857), [screenshot capture 34275645746](https://github.com/afterburn25/stellar-continuum/actions/runs/34275645746), and [Windows demo 34275646050](https://github.com/afterburn25/stellar-continuum/actions/runs/34275646050) all pass. Both Linux export and actual hosted Windows executable startup succeeded.
- Final downloadable artifact `10075655432` is 73,805,209 bytes, expires October 8, and contains `StellarContinuum-0.0.7-dev.1-windows-x64-cc304a6e4c7c.zip` plus its SHA-256 sidecar. GitHub artifact digest: `3619530615c02580f0b02b5f15f894474795a79435261b6c475caead17a1d46f`. Runtime log artifact: `10075656161`; screenshot artifact: `10075605190`. This proves a real package exists for that exact snapshot; it does not validate later changes automatically.
- Visual inspection of the earlier `ed67` captures found horizontal Relations clipping and panels obscuring most of the map. Core added the map toolbar and hide/show controls. This follow-up makes Relations actions wrap and scroll inside the viewport while keeping Close fixed above the scrolling content.
- Screenshot driver now captures twelve views: menu, ordinary overview/colony sites, Relations overview/actions, demo confirmation, demo guidance, ship controls, cleared map, home orbits, return to region, and restored panels. Assertions cover injected N while menus are open, confirmation cancellation, starting at 24x, objective visibility, ordinary early-game command dispatch, Home/Open/Back navigation, panel restoration, and visible command feedback above the system view. Capture uses an isolated CI user-data directory.
- Local checks: seven smoke regressions, nine packaging regressions, and whitespace validation pass. Godot 4.7.2 source confirms native dialog button handlers and scrolling API behavior. No game executable was launched on the user's desktop. Local full Godot compile/render remains unavailable; fresh exact-head CI must validate the new capture and Relations code.
- Scope limit: screenshot command activation emits real button signals and injects the menu keyboard shortcut. It checks UI wiring and ordinary early-game prerequisite handling, not mouse hit testing or a completed colony progression. The artifact's hosted headless Windows startup does not certify graphical behavior on the player's GPU. Core owns final artifact publication and main/integration acceptance.
