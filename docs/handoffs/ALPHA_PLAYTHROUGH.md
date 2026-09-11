# Alpha Playthrough Handoff

## Current checkpoint

The candidate integration is `7e84f29d`, combining the visual expedition source `dd18ea96`,
the diplomacy source `4f368634` and the combat source `086c5c5f`. The runtime includes the
mouse-driven alpha map, adaptive research and construction timing, diplomacy workspace,
colony surface and the current combat presentation.

## CPU evidence

The Release game build completed with zero warnings and zero errors. Maintained validation
projects completed successfully: Simulation 71/71, CoreRuntime, Quality, DiplomacyWorkspace,
MassiveCombat and MassiveCombat.Persistence. The exact logs are under
`work/alpha-integration-validation/`.

## Remaining receipts

The first combined native run at `fe6ef1bb` passed 35 screenshots and 141 input checks,
including the audio resource GC stress and autosave artwork check. Its tactical menu focus
also passed. The ordinary Player opening then exposed duplicated zero-boundary territory
vertices (repeated Godot triangulation errors) and search centering a description match
instead of the exact technology title. Both defects are repaired in the follow-up candidate;
the full Player opening must pass before publishing its download as playthrough-ready.

The follow-up also makes the renderer and gate-click handler share canonical travel-edge
validation: known or nearby unconnected systems cannot create arrows. Hosted diplomacy
capture restores its requested native window after production fullscreen initialization.
The production game continues to start fullscreen. The earlier Windows candidate passed
startup but is superseded by these follow-up repairs.

Hosted CI packaging, native capture and the final integrated playthrough still need their
source-matched receipts. The current evidence does not claim Windows release approval,
native visual acceptance or a complete player expedition. Review remains open for pacing,
art polish, accessibility and the breadth of connected diplomacy/combat outcomes.

Historical project-state entries below the current checkpoint remain historical and should
not override this source or its pending gates.
