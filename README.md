# Game

Public development repository for an original real-time space civilization strategy game.

## Current development

Version: `0.0.3-dev.1`

Engine: Godot 4.7.2 .NET / C# (`net8.0`)

The prototype currently includes:

- deterministic seeded galaxy generation
- quota-controlled system archetypes
- eight distinct prototype civilization temperaments
- per-civilization system knowledge and civilization-contact knowledge
- astronomy-visible star coordinates with hidden unsurveyed system details
- one real-time scout fleet per civilization
- player scout movement orders and automatic AI scout exploration
- sensor-based system discovery and legitimate first contact
- exploration events recorded in support logs
- real-time pause and 1x–4x simulation speeds with bounded backlog protection
- fleet positions, destinations, discoveries, and contacts persisted in save format v3
- deterministic migration of earlier prototype saves
- system-spec diagnostics and support-bundle ZIP export
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
- left click — inspect any astronomical target; details appear only when surveyed
- right click — send the player scout to a star
- mouse wheel — zoom
- middle mouse drag — pan
- `N` — generate a new galaxy
- `F6` — autosave
- `F8` — export a support bundle

## Design

See `docs/` for the public roadmap, architecture, AI contract, support/performance strategy, and validation gates.

Some discoveries and rare outcomes are intentionally undocumented even though development is public.
