# Core integration — full-game modes, cinematic maps and colonies

## Active 100-system playable-foundation continuation

The current local Core continuation narrows new campaigns to 100 systems and builds
out the first playable management loop before any larger-galaxy expansion. Earth/Sol
remains the Human origin and other civilizations retain their distinct home systems.

Credits now fund infrastructure, ships, colony expeditions and freely placed surface
buildings. Administration, population services and active fleets create recurring costs.
A powered trade hub adds surface revenue. The new Economy page shows reserves, Earth
purchasing-power reference, gross revenue, each operating-cost category and reconciled
net daily flow. Construction, ship, surface and settlement screens expose affordability
before orders are placed.

The operations pages now connect owned state back to the map: Ships lists active fleets,
activity, location and upkeep with a Locate action; Colonies lists owned worlds with direct
orbital View and 3D Surface actions. Local validation after these changes passes the shared
build, Core runtime 26/26, simulation 22/22 and quality 8/8. A fresh exact-head Godot
screenshot/input gate and exported Windows startup gate remain required before publishing
or accepting this continuation. After the upgrade slice, local validation passes the shared
build with zero warnings/errors, Core runtime 28/28, simulation 22/22, quality 8/8, UI
contracts 22/22, Windows contracts 9/9 and the visual-asset validator.

The Relations page now issues war declarations through the observer-safe Diplomacy command
service. Armed fleet rows show integrity and current orders, and issue Hold, Defend and
Retreat through Core's matched Combat runtime. Engage Hostiles supplies the missing attack
action without passing foreign fleet IDs through presentation: the Combat command runtime
checks co-location, its live Diplomacy hostility policy and its normal attack preview, then
selects the first valid target in stable order. Failure is generic and non-mutating.
Deploy to Selected makes military movement player-accessible from the same fleet row. Core
requires an owned active military fleet, a real selected destination and supported operational
reach; accepted travel resets stale local Combat orders and flows through Exploration's shared
strategic movement and arrival processing.

The first follow-up colony-management slice makes rendered surface structures directly
selectable. An incomplete site can be cancelled with a 50% authorization-credit recovery and
no Industry recovery; a completed structure can be demolished without a refund. Core owns the
authorization and immediately removes the building's power demand/supply and production. The
native capture contract selects an actual 3D structure and exercises the visible action.

The next colony slice adds an upgrade action to selected completed structures. Base power,
science, fabrication and trade complexes each have one advanced form with explicit Credit and
available-Industry costs. Upgrades are authoritative, ownership checked and atomic; advanced
types remain unavailable in the build palette. They retain the exact saved position and use the
existing validated type identifier, so formats 12/13 need no schema change. Advanced models add
an illuminated crown, and the real-input capture selects and upgrades the rendered lab before
save/reload verification.

The next presentation seam replaces the Research page's repeated option list with a graphical
visible horizon. Its projection includes only completed, active and currently investigable
legacy nodes; future or blocked nodes never reach the UI. An investigable node is a real button
that calls the existing research command authority. This is intentionally a presentation seam,
not a partial Adaptive Research state cutover: the maintained runtime must enter campaign
persistence, stepping and gameplay capability effects as one separately validated milestone.

Colony placement now has an emergent specialization decision. Three completed complexes in
one functional family create a Research, Industrial, Commercial or Energy district and add
25% to that family's powered output. Advanced buildings count toward their base family.
Specialization and progress are visible on the surface and owned-world list, and are derived
entirely from existing saved construction state. Core validates activation and immediate
deactivation after demolition; no persistence version changes.

Colony surfaces now select a world palette from canonical environment facts. Temperate,
frozen, hot, airless, oceanic, reducing-atmosphere and rocky classes drive shader terrain,
exposed rock, sky, fog and sunlight colors. The visual class reaches presentation only after
the player has opened an owned colony; it does not change placement physics or persistence.

The human 2050 start is now multi-world: Earth, Luna (100,000 people) and a young Mars
settlement (250,000) are ordinary saved colonies in Sol. The Colonies page exposes orbital
and surface navigation for all three, including Mars's environment-driven 3D terrain. Tiny
dependent settlements pay 0.12 Credits/day administration, scaling to the established full
1 Credit/day at 250 million people. The guide and dashboard require an extrasolar settlement
for campaign completion, so the opening holdings cannot skip the exploration arc.
The surface hub now includes a bounded established-settlement cluster derived from population
and exact habitat requirements. Luna/Mars render sealed domes while Earth renders open towers;
all modules stay inside the already protected hub footprint and remain visual-only.
Habitat support is now an active colony decision: a fifth surface building reduces the local
cost 20% while powered, and its closed-loop upgrade reduces 40%. Multiple powered complexes
cap at 75%; loss of power immediately removes their reduction. Native capture places the
Mars habitat from the real build menu.
The orbital Colonies overview consumes the same derived surface output and reports the net
life-support bill, gross bill and active reduction together with building count and local
power demand/supply. It no longer displays an unreduced charge after habitat construction.
The home-system map also projects the established orbital construction projects beside
the star with distinct silhouettes and locked/available/active/complete state. Active work
uses the real construction progress; clicking a silhouette opens Industry operations. This
presentation adds no new save or simulation state.
Asteroid Resource Network is now an optional ordinary project after Orbital Industry and a
completed Launch Complex. It adds 1.50 Industry/day, 0.18 Credits/day upkeep, a connected
resource-site logistics node, and a distinct three-asteroid orbital marker. Launch Complex,
Shipyard, and Warp Test Facility upkeep is also included in the authoritative Economy flow.
Construction lock reasons are centralized in the registry and reused by command rejection,
orbital marker state and map feedback. Native capture clicks the locked asteroid marker and
requires both Orbital Industry and Orbital Launch Complex to be named.
The opening guide now points to direct project choices and surfaces the asteroid network's
1.50 Industry/day versus 0.18 Credits/day optional tradeoff between core-path builds.
Research and Industry removed their older cycling controls. Native capture now starts the
Research Network through its named `Chooseresearch_network` project control.

The first published follow-up exposed an earlier 720p navigation regression before the surface
journey: adding Economy as the tenth rail destination left Menu partially below the rail after a
1600×900 to 1280×720 resize. The rail now uses compact icon-button height and spacing so every
destination remains fully visible without scrolling at the supported minimum viewport.
The native gate then reached the complete three-site surface journey and showed that 1×
construction exceeded its old 90-second render budget from ordinary starting Industry. The
surface header now provides real 1×–4× player controls; the capture uses the visible 4× control
and still advances the ordinary simulation with no Developer resource grant.

Starting baseline for this milestone: integration `334004d15c1f0cff7ee6dc345c8de225a2603de4`, PR #233,
exact tested source `2b450a02f2625ce408ff6f9f7f9f660b98ab97fa`. All six CI runs passed,
including 43 real-input acceptance checks, 13 Godot captures and native Windows
startup. The earlier Adaptive Research helper crash was repaired; no new scratch
executable is part of this work.

The user has expanded the objective to a full game with separate Player and Developer
modes. Ordinary rules remain shared; Developer alone exposes explicit test tools and
24x time. Its campaign has a separate version 1 envelope, independent backups and a
persistent ToolsUsed marker. PR #239 records exact-source acceptance and build evidence. Player save boundaries reject Developer state. Legacy
demo files import only when no Developer save or backup exists and remain intact.
See `docs/GAME_MODES.md` for user flow, exact commands and next full-game priorities.

The graphics milestone remains cinematic direction B plus freely navigable
3D planet surfaces. The map now connects Milky Way overview, stellar region, orbital
system and focused planet. Original Milky Way/nebula backgrounds, native-resolution
GPU planet materials, observer-safe canonical appearances and layered Saturn rings
support those views. The existing regional simulation catalog and travel coordinates
remain authoritative. Fresh humans still start on Earth; old saves keep their homes.

Owned solid-world colonies open a real 3D surface with graphical building previews,
free X/Z placement, rotation, terrain/footprint validation and distinct models.
Generators, science labs and fabricators spend shared industry over simulation time;
completed powered structures affect economy. Model state persists in formats 12/13,
while saves without placed structures retain formats 8/9 or 10/11. Surface navigation
blocks strategic-map commands, and campaign/context changes close stale surface views.

See `docs/CINEMATIC_MAP_AND_SURFACE.md` for controls, budget rules, scope, provenance
and acceptance requirements. This is a bounded colony area, not full-planet terrain
streaming. No unrelated subsystem expansion or main promotion is included.

The combined candidate requires new model/persistence tests, real wheel/drag/placement
input captures, visual review at 1600x900 and 1280x720, all existing research/runtime
suites, and native Windows package verification. Only the exact tested source may be
merged into integration or supplied as the new demo. Local C# compilation is useful
but does not replace shader/render validation.

The sections below preserve the recovery history and previous demo investigation.

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

## Focused milestone: playable Windows demo

The user has explicitly prioritized a playable demo. All requested branch families
have been recovered; further subsystem expansion is deferred. See
`docs/PLAYABLE_DEMO_MILESTONE.md` for the acceptance loop and priorities.

The ordinary campaign works through research, construction, physical scout/science/
colony ships, reconnaissance, full survey and settlement. Three seeds pass without
granted resources or knowledge. Opening pacing was the main weakness: first settlement
required 16.5–17.75 minutes at continuous 4x. Optional Play Demo uses seed 20260908,
the same rules and a bounded 24x clock (at most four quarter-day steps per frame),
reaching settlement in 164.98 active seconds plus player choices. Normal speeds
and balance remain unchanged. Demo save/restart/resume and backup tests preserve
the normal slot byte-for-byte.

The UI now gives next steps and ETA, visible ship building, Home and selected-star
commands, system navigation, and a clear-map panel toggle. Normal/demo replacement
requires confirmation and a successful checkpoint. Input is blocked behind the menu.
Exit is cancelled if saving fails; persistent menu errors and a command-feedback
strip explain failures above the active map view.

The first combined runtime exposed incomplete SVG imports. Build, screenshot and
export workflows now wait for import completion and reject engine errors and aborted
scans. The Windows exporter required the shared Game.sln; its single project retains
the existing shared assembly.

Published candidate `cc304a6e4c7cba90809169282d6572b1b42c4457` passed the complete
build, screenshot capture and actual exported Windows startup. Its verified package
is in [Windows demo run 34275646050](https://github.com/afterburn25/stellar-continuum/actions/runs/34275646050).
Subsequent interface/feedback fixes require fresh exact-candidate CI and visual review.
PR #223 is the combined review record; do not reuse an older artifact as evidence
for newer source.

After acceptance, prioritize actual player feedback on this short loop and Windows
hardware behavior. Defer the unconnected diplomatic presence producer, foreign-vessel
target identity, large-galaxy profiling, astronomy catalog #221 and Research M20
until they are required by a separately assigned milestone. Existing branches and
their unfinished work stay preserved. No promotion to main is part of this work.

Three specialist slots execute leads in waves; completed agents are not claimed as continuously running. Each branch family retains its existing ownership and handoff. Publication uses authenticated GitHub Git-data operations with exact tree verification and non-forced ref updates because local git push authentication is unavailable; local implementation commit metadata may differ from the published commit, but content and established remote ancestry are preserved.
