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
