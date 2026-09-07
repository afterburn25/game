# Game

Public development repository for an original real-time space civilization strategy game.

## Current development

Version: `0.0.4-dev.1`

Engine: Godot 4.7.2 .NET / C# (`net8.0`)

Playable prototype systems now include procedural galaxies, distinct civilization temperaments, per-civilization fog of war, real-time scout exploration, legitimate first contact, home colonies, population growth, basic credits/industry/science production, and one colony ship per civilization.

Colony ships physically travel. The player uses `Shift+Right Click` on a surveyed habitable system to send the colony ship. Pre-warp inhabited systems are deliberately excluded from ordinary colonization rather than being treated as empty territory. AI colony ships can target only worlds their own civilization has actually discovered.

Save format v4 persists fleets, orders, knowledge, contacts, colonies, populations, and economy state while migrating earlier prototype saves.

## Controls

- `Space` — pause/resume
- `1` / `2` / `3` / `4` — simulation speed
- left click — inspect astronomical target
- right click — order scout
- `Shift` + right click — order colony ship to a valid surveyed colony target
- mouse wheel — zoom
- middle mouse drag — pan
- `N` — new generated galaxy
- `F6` — autosave
- `F8` — export diagnostics/support bundle

All pull requests are required to pass C# compilation plus pinned Godot 4.7.2 headless editor/runtime smoke tests before merging to `main`.
