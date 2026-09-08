# Core recovery / integration candidate — 2026-09-08

Current branch: `work/core-game-integration`. Recovered accepted baseline `c529a1a765776c0940f88002410bc70db740d05a`; Core was 285 commits behind with no unique work and was safely fast-forwarded. The complete 96-branch recovery snapshot is `docs/BRANCH_INVENTORY_2026-09-08.md`.

## Accepted after recovery

Testing/Release PR #222 passed full CI including Debug assembly, strict Godot error inspection and `STELLAR_RUNTIME_READY IntegratedMain` proof, then merged into `integration` at `32b8361c61f3c40965aa32629365e1a423befad6`. This repairs the independently reproduced #61 false-green runtime gate; prior process-exit-only results are insufficient evidence.

## Candidate assembled for combined validation

- Preserve recovered known-good backup during the first primary repair save, reusing the existing Core child `work/core-preserve-recovered-backup`.
- Existing Visual Assets PR #218 plus validator parity/safety/contrast checks, 46 unchanged original SVG resources and production-candidate manifest.
- Existing Galaxy PR #220 plus observer/campaign-safe bounded snapshots, GUI-first pointer routing and correct spatial-scale markers. Core resolved startup-hook conflicts while preserving strict runtime proof and hides the Visual strategic overlay while in a star-system view.
- Existing UI branch: dedicated wrapped shipyard status row and naturally stacked scrollable command/exploration panels. Core updated integrated refresh calls to the new presentation helper names; simulation ownership is unchanged.
- Existing Exploration child `work/colonization-shared-body-resolver`: shared bodyless compatibility resolution without retargeting explicit bodies, using transported population species and observer-visible availability; extended regressions validated by the family lead.
- Existing Species child `work/habitat-support-capability-foundation`: compiler-explicit deterministic reducer check and recovered lineage handoff. Original `work/species-race-mechanics` is superseded as confirmed by #16 and byte-identical accepted readiness code; its historical unique commits remain intact.
- Corrected shared WORKSTREAMS/CHAT_HANDOFF/PROJECT_STATE instructions that incorrectly pointed active research at the retired dev branch or main-first flow. No main modification or branch deletion.

## Validation and limits

Specialists ran actual-source topology/spatial/Species/Exploration checks and existing Python gates. Exploration's complete source-linked suite passed 22 central tests plus 51 initializer groups. Core ran eight visual contract regressions and seven smoke regressions. The combined candidate still requires full Release/Debug/Godot/quality validation and exact-head screenshots; the screenshot workflow now runs on Core and rejects semantic startup errors.

The local NuGet/Godot package path is not usable; full engine verification is performed through GitHub CI, not represented as a local pass. A Research scratch runner caused Windows CLR dialogs and was repaired separately on canonical `research/adaptive-research` at `aea15e410d75094b78ae2c41d12a58d599bcc7ef`; Windows/Linux topology CI and all23 local research suites passed. That branch's unfinished M20/catalog work is not pulled into this gameplay candidate.

## Dependencies / next priorities

1. Independently review combined CI/runtime/screenshots and fix any interaction failures before integration.
2. Complete AI/Combat/Diplomacy/Economy bounded recovery stages; avoid duplicating accepted internals.
3. Legitimate observer-specific vessel target identity remains the shared Exploration/Combat dependency. Do not infer it from civilization contact or raw foreign fleet IDs.
4. Galaxy needs an Exploration-owned selected-system/revision read interface and representative 500/2,000-system profiling. Its current cache refresh is bounded, not a final scale benchmark.
5. Real 1,000-system astronomy catalog #221 remains uncompleted; use only sourced facts and pinned snapshots when that data milestone is assigned.
6. Research M20 event-cause wiring and snapshot persistence remain incomplete; no legacy gameplay research cutover is accepted.

Three specialist slots execute leads in waves; completed agents are not claimed as continuously running. Each branch family retains its existing ownership and handoff. Publication uses authenticated GitHub Git-data operations with exact tree verification and non-forced ref updates because local git push authentication is unavailable; local implementation commit metadata may differ from the published commit, but content and established remote ancestry are preserved.
