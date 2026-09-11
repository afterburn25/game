# Visual finish acceptance

User priority, 2026-09-11: continue implementation of the visual finish. This takes precedence over further pioneer-economy expansion. Existing simulation ownership, save compatibility, and the older-campaign performance repair remain required.

## Current combined checkpoint (September 11, morning)

Core through `bcad8e0` integrates observer-safe territory/fog, persisted local gate travel,
spectral stars and local companion groups, the compact shared clock, a continuous planetary
limb shader, a much better NASA/JPL cloud-covered Venus photograph, and fullscreen startup
with a main-menu Settings hub and Exit to Windows. The original approved Earth photograph
remains unchanged. This checkpoint is an implementation candidate, not visual-finish acceptance.

At combined `8286748`, build exits 0 with no warnings/errors, CoreRuntime passes 81/81,
Simulation 71/71 and Quality 20/20. Receipts are `work/visual-combined-{build,core,simulation,quality}.log`.
The subsequent fog-star opacity connection has not yet had combined native review.

Still required before publication as a playable build:

- Native planetary alpha readback on the illuminated limb, including vacuum, unknown
  surfaces, atmosphere changes, and visible dark/light background comparisons.
- Real close-vessel rendering, moving camera recovery, conditional engine thrust, binary
  versus triple fixtures, and a genuinely irregular close stellar photosphere. Earlier
  map captures show a repetitive pastel pattern and do not establish the requested quality.
- Main-menu Settings category navigation, fullscreen startup and save/Exit to Windows.
  The latest responsive run regressed at 4K readback; the older `af3894c` run passed exact
  4K on this host, so do not waive the new failure as a proven host limitation.
- Combined native navigation, territory visibility, older-save performance, full Player
  journey and final package validation. Current source is ahead of the published draft.

Current user display contract: start fullscreen every time, expose no player windowed mode
or title-bar X, and group Audio, Video, Voice/subtitles and existing control help under one
main-menu Settings entry. Explicit Exit to Windows uses the established save/shutdown flow.
Test-only window resizing must not become a production startup option.

## Evidence and direction

Baseline reviewed: `f83128e` source; last complete rendered set `3286b78` (the latter does not validate subsequent source). The main menu artwork already provides a useful cinematic direction. Three weaknesses are visible in the actual gameplay captures:

- Galaxy: narrow, evenly spaced spiral stripes read as a diagram. Replace this with a coherent luminous mass, irregular dust lanes, a central bulge and softer broken arms. Keep the existing generated system coordinates, recognizable stellar colors, full-frame distant background and clear selection.
- Colony: washed-out lighting and a sparse radial arrangement read as a model on a paved disc. Improve material contrast, coherent urban blocks, facade depth, street detail and the surrounding landscape. Preserve buildable land, placement validity, actual building state and bounded scene cost.
- Interface: large flat panels and stacked instruction strips compete with the world. Use compact, consistent hierarchy, aligned grouped facts, deliberate art placement and legible actions. Costs and outcomes must remain visible before commitment; all existing actions retain their input targets and recovery behavior.

## Scope and ownership

1. Caption/UI owner repairs settled caption measurement and prevents drawer collapse. A passing build alone is insufficient: actual active captions, readable full text and reachable controls at 720p/1080p are required.
2. Map owner improves galaxy and restored 2D system presentation. Earth keeps the requested photograph. System/planet skies contain stars and their own deterministic local scenery; external background galaxies remain galaxy-view-only.
3. Surface owner improves lighting, materials and settlement dressing, including roads. Dense urban dressing belongs to developed homeworlds; uninhabited worlds and small colonies must not receive invented cities. Cosmetic objects must not conceal real placements or act as gameplay structures.
4. Core reviews screenshots, interaction and performance together, then integrates a coherent candidate. No owner edits authoritative economy/research rules to make a visual check pass.

## Acceptance evidence

- Primary composition: 1920x1080. At 1280x720 controls reflow, remain readable and fit their scroll view; at 1440p/4K world rendering retains detail. Do not fix fit by shrinking essential text.
- Review full galaxy, regional stars, 2D Sol orbits, Earth/Saturn focus, colony overview/street, building selection/placement, research/ship cards and campaign confirmation. Compare before/after from the running game, not mockups alone.
- Preserve left-drag pan, middle-drag look, wheel navigation, right-click fleet orders, survey visibility, moons/stations and reversible save/load flows.
- Record exact source revision, actual process exit, shader/runtime errors, screenshots and focused interaction results. Import source assets in new Godot worktrees before native capture. Keep one native GPU job at a time.
- Recheck old-save navigation and surface performance after rendering changes. Avoid per-frame model generation, unbounded particle/mesh counts or scene rebuilding caused by ordinary selection.
- Run the relevant combined capture and package checks before offering a new download. Earlier revision receipts do not certify the new candidate.

Production finish is a visual acceptance requirement, not a label awarded by a passing test count. Original custom meshes, animation and environmental art may still require further work after this pass; record visible remaining shortcomings honestly.

## September 11 reference additions and integration contract

- Galaxy stars: small bright core, clear spectral color and fine rays; genuine binary/triple groups. Local stars: detailed photosphere, hot rim and restrained irregular corona. Planet materials retain terrain and day/night depth. References are visual direction, not shipped assets.
- Empire space: continuous exterior borders and stable colors, readable names, sensible enclaves; disputed claims remain distinct from actual ownership. Full overview and regional views must both work. Fog reflects observer knowledge and must never reveal foreign territory, labels, claims or strategic facts prematurely.
- System lane arrows: only actual lane neighbors, placed in their catalog directions. Left click changes the viewed system without issuing a ship order or granting survey knowledge. They are also warp entry points: ships arrive on the origin-facing edge, traverse each intermediate system to its onward exit, then warp. This requires timed saved simulation phases, honest ETA and input proof, not a presentation-only animation.

### Stellar catalog implementation

New `galaxy-v3` campaigns retain the same primary spectral quotas, positions and planetary generation, and add persisted optional secondary/tertiary stellar classes. An independent seeded stream assigns approximately 20% binary and 5% triple systems among eligible ordinary non-authored stars. These are initial game tuning, not an astronomical population claim. Sol remains single; remnants and protostars retain their existing single model.

Save fields are optional additive catalog data in the existing envelope. Missing fields in older saves stay absent; loading never regenerates companions. Invalid enum values, incomplete A/B/C configurations and companions on canonical Sol fail with a system-specific terminal diagnostic. Companions are schematic stellar catalog detail; no new multi-body gravitation, stellar evolution or orbital stability model is implied. Detailed system projections retain the existing full-survey gate.

Validation: `dotnet run --project tests/Game.CoreRuntime.Validation/Game.CoreRuntime.Validation.csproj --no-restore` exited 0, 81/81 passed, including seeded multiplicity, unchanged planets, old-save defaults, save round trip, fog-safe projection and malformed catalog rejection. Log: local `work/stellar-companion-validation.log`. Native companion rendering is pending the map owner's implementation.

### Combined review checkpoints

- Core `ada8363`: imports/build succeed; maintained immersive focus exits 0 with empty stderr, nine captures and actual rotation, descent, atmosphere, surface and recovery checks. `work/visual-surface-ada8363` is the exact local receipt. Roads, colony density and output grouping improve, but the reviewed street image still has primitive tower silhouettes, simple facade depth and hard shadows. This is not photoreal production acceptance. Original NASA Earth brightness is preserved; an unregistered spherical night-map overlay was rejected, and airless worlds cannot gain cloud veils.
- Core `af3894c`: responsive focus exits 0 with empty stderr and native PNGs at 1280x720, 1920x1080, 2560x1440 and 3840x2160. Caption/drawer clearance passes at 720p and 1080p; research reflows and map clicks remain aligned at each size. `work/visual-responsive-af3894c` contains exact window/scale/texture diagnostics. An earlier resize race failed cleanly and is retained at `work/visual-responsive-ada8363`; expected resolution checks were not weakened.
- Isolated map `bed3fb1`: post-import galaxy capture exits 0, empty stderr. Root reviewed the more irregular dust mass and spectral cores as a visible improvement. Local companion, gates and green fleet-marker evidence is still required before combined acceptance.
- Territory is under further review for civilization ID zero, sparse/extreme grid bounds, disconnected holdings, ownership versus claims, fog edges and performance. No unreviewed territory implementation is integrated at this checkpoint.
- Local gate transit is under further review for intermediate sensors/refueling, queued returns, exact ETA/progress, malformed saves and partial-time partition tests. No unreviewed travel implementation is integrated at this checkpoint.

Own ships and fleets must now use clearly visible green map markers, with role silhouettes, grouped counts when appropriate, dark backing over bright nebulae and a distinct selection outline. Their local and strategic positions must reflect the same saved travel state used by simulation. Foreign markers must not be mislabeled as player-owned.
