# Alpha Playthrough Handoff

## Current checkpoint

PR #314 on `work/alpha-playthrough-integration` combines the visual expedition source `dd18ea96`,
the diplomacy source `4f368634` and the combat source `086c5c5f` (combined foundation `7e84f29d`). The runtime includes the
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
also passed. The ordinary Player opening exposed territory triangulation errors and search
centering a description match instead of the exact technology title. Exact-title selection
was repaired at `ee71b14b` and passed native pointer/search checks at 720p and 1080p.

Removing duplicate boundary vertices was insufficient: the next ordinary Player attempt
still produced native triangulation errors. The preserved autosave isolated a valid convex
triangle with coordinates `(-18.147076,-55.087666)`, `(-18.150757,-55.083984)`,
`(-18.15281,-55.086037)` and area approximately `7.56e-6` world units squared. Godot's
general polygon triangulator rejected this nonzero sliver. `2fcfc292` replaces repeated
general triangulation with one cached explicit triangle-fan mesh per civilization; it
preserves the geometry instead of suppressing or discarding errors. `c45d3ed7` disposes
the cached meshes on refresh and scene exit. Exact failed-save native replay passed with
exit 0 and empty stderr (`work/territory-replay-mesh-ee71/`); the fresh complete Player
opening must still pass before the download can be called playthrough-ready.

The follow-up also makes the renderer and gate-click handler share canonical travel-edge
validation: known or nearby unconnected systems cannot create arrows. Hosted diplomacy
capture now receives an explicit requested resolution from its runner, restores the native
window after production fullscreen initialization, and asserts both window and image size
before capturing the flow. Earlier runs inferred 1920x1080 even for requested 1280x720;
their successful Windows packages remain held because the screenshot gate failed.
The production game continues to start fullscreen. The earlier Windows candidate passed
startup but is superseded by these follow-up repairs.

The user-supplied Earth-orbit splash is now a dedicated loading asset. Its baked 61% bar
was removed so the runtime can show actual resource readiness with a smoothly paced
minimum seven-second presentation. The main-menu backdrop is retained. Startup and
campaign transitions are covered; autosaves never invoke the loading screen. Artwork
provenance and exact edit prompt are in `docs/art/LOADING_SPLASH_PROVENANCE.md`.

Hosted CI packaging, native capture and the final integrated playthrough still need their
source-matched receipts. The current evidence does not claim Windows release approval,
native visual acceptance or a complete player expedition. Review remains open for pacing,
art polish, accessibility and the breadth of connected diplomacy/combat outcomes.

Historical project-state entries below the current checkpoint remain historical and should
not override this source or its pending gates.
