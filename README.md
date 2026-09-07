# Game

Public development repository for an original real-time space civilization strategy game.

## Current development

Version: `0.0.2-dev.1`

Engine: Godot 4.7.2 .NET / C# (`net8.0`)

The prototype currently establishes and validates the architecture before content scale:

- deterministic seeded galaxy generation
- quota-controlled system archetypes
- eight distinct prototype civilization temperaments
- per-civilization knowledge state and player-facing fog of war
- civilization home placement designed to avoid immediate crowding
- continuous real-time clock with pause and 1x–4x controls
- bounded simulation backlog protection
- clickable, pannable, zoomable known-galaxy view
- fair-information AI scaffolding that cannot read authoritative enemy state behind fog of war
- bounded diagnostics and system-spec logging
- support-bundle ZIP export
- versioned atomic autosave format with migration from the 0.0.1 prototype save
- CI validation including C# compilation and headless Godot editor/runtime smoke tests

## Run

1. Install the Godot 4.7.2 .NET build.
2. Clone this repository.
3. Open `project.godot` in Godot.
4. Build the C# project when prompted.
5. Run the project.

Prototype controls:

- `Space` — pause/resume
- `1` / `2` / `3` / `4` — simulation speed
- mouse wheel — zoom
- middle mouse drag — pan
- left click — inspect a known star system
- `N` — generate a new galaxy
- `F6` — autosave
- `F8` — export a support bundle

## Design

See:

- `docs/ROADMAP.md`
- `docs/ARCHITECTURE.md`
- `docs/AI.md`
- `docs/SUPPORT_AND_PERFORMANCE.md`
- `docs/VALIDATION.md`

Some discoveries and rare outcomes are intentionally undocumented even though development is public.
