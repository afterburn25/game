# Game

Public development repository for an original real-time space civilization strategy game.

## Current development

Version: `0.0.7-dev.1`

Engine: Godot 4.7.2 .NET / C# (`net8.0`)

The prototype currently includes:

- campaign calendar beginning January 1, 2050
- player and normal major civilizations beginning pre-warp
- remote seeded old powers that start spacefaring but are non-expansionist and neutral unless provoked
- deterministic procedural galaxies and authoritative per-civilization fog of war
- fair-information AI with no hidden-map access
- population, economy, research, and construction progression
- infrastructure-gated warp research
- real orbital shipbuilding after warp technology is achieved
- Pathfinder scout, deep-space science vessel, and interstellar colony ship designs
- colony ships reserve 250 million colonists when construction begins
- real-time fleet movement, exploration, first contact, and colonization
- save/load migration through format v7, including shipyard build progress
- diagnostics/support bundles and full Godot headless CI validation

## Prototype controls

- `Space` — pause/resume
- `1` / `2` / `3` / `4` — simulation speed
- `T` / `R` — cycle/start research
- `C` / `B` — cycle/start infrastructure construction
- `V` / `G` — cycle/start ship construction
- `F` — cycle the selected scout/science exploration vessel
- right click — order selected scout/science vessel to a star
- Shift + right click — order a colony ship to a surveyed habitable system
- mouse wheel — zoom
- middle mouse drag — pan
- left click — inspect a star system
- `N` — generate a new 2050 campaign
- `F6` — autosave
- `F8` — export support bundle

## Design

See:

- `docs/ROADMAP.md`
- `docs/ARCHITECTURE.md`
- `docs/AI.md`
- `docs/SUPPORT_AND_PERFORMANCE.md`
- `docs/VALIDATION.md`

Some discoveries and rare outcomes are intentionally undocumented even though development is public.
