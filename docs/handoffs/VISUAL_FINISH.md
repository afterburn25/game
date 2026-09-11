# Visual finish acceptance

User priority, 2026-09-11: continue implementation of the visual finish. This takes precedence over further pioneer-economy expansion. Existing simulation ownership, save compatibility, and the older-campaign performance repair remain required.

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
