# Game

Public development repository for an original real-time space civilization strategy game.

## Current development

Version: `0.0.6-dev.1`

Engine: Godot 4.7.2 .NET / C# (`net8.0`)

New campaigns begin on **January 1, 2050** with the player and normal major civilizations pre-warp. A small number of distant old powers begin already spacefaring but are non-expansionist and neutral unless provoked.

The pre-warp opening now has two linked progression systems instead of a passive research timer.

### Research

Technologies consume accumulated science. Some technologies require completed infrastructure projects as well as earlier technologies.

### Construction

Construction projects consume accumulated industry. Current prototype projects include:

- Planetary Research Network — increases science output.
- Industrial Automation Program — increases industry output.
- Orbital Launch Complex — required before Orbital Industry can be researched.
- Orbital Shipyard — requires Orbital Industry.
- Warp Test Facility — requires Warp Field Control and must be completed before the Prototype Warp Drive can be researched.

This creates a real progression chain from a 2050 planetary civilization into an interstellar power. Normal AI civilizations use the same technology/project prerequisites and choose priorities according to their traits.

## Controls

- `Space` — pause/resume
- `1` / `2` / `3` / `4` — simulation speed
- `T` — cycle available research
- `R` — start selected research
- `C` — cycle available construction
- `B` — begin selected construction project
- left click — inspect astronomical target
- right click — order scout after warp capability
- `Shift` + right click — order colony ship after warp capability
- mouse wheel — zoom
- middle mouse drag — pan
- `N` — generate a new 2050 campaign
- `F6` — autosave
- `F8` — export diagnostics/support bundle

Save format v6 persists construction completion/progress in addition to the calendar, research, civilization stages, fleets, colonies, economy, and fog-of-war knowledge.
