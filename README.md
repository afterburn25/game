# Game

Public development repository for an original real-time space civilization strategy game.

## Current development

Version: `0.0.5-dev.1`

Engine: Godot 4.7.2 .NET / C# (`net8.0`)

## Campaign opening

New campaigns begin on **January 1, 2050**. The player and the normal major civilizations begin **pre-warp** rather than starting with interstellar fleets.

Science must progress through an initial technology chain including orbital industry, fusion propulsion, deep-space sensors, exotic-field theory, warp-field control, and finally a prototype warp drive. Completing the prototype warp drive changes that civilization to warp-capable and creates its first scout and colony vessels.

A small number of remote seeded old powers begin already spacefaring. They are deliberately non-expansionist and neutral unless provoked, creating an early-game buffer instead of surrounding the player with mature expansionist empires. Their existence, territory, and identity are still subject to the normal fog-of-war/first-contact rules.

Most other major civilizations independently follow the same pre-warp-to-warp progression as the player. Their research choices are influenced by their own traits, and they do not receive free information about the player.

## Prototype controls

- `Space` — pause/resume
- `1` / `2` / `3` / `4` — simulation speed
- `T` — cycle currently available research choices
- `R` — begin the selected research project
- left click — inspect astronomical target
- right click — order scout after warp capability exists
- `Shift` + right click — order colony ship after warp capability exists
- mouse wheel — zoom
- middle mouse drag — pan
- `N` — generate a new campaign beginning in 2050
- `F6` — autosave
- `F8` — export diagnostics/support bundle

Save format v5 preserves the 2050 campaign calendar, civilization development stage, research progress, fleets, colonies, economy, and fog-of-war knowledge. Older prototype saves migrate without being discarded; civilizations from earlier spacefaring prototypes remain warp-capable when loaded.
