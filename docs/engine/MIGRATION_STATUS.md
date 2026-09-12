# Stellar Engine migration status

Updated 2026-09-12. Engine **0.1.2 species/homeworld planning slice**; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). The 0.1.1 baseline is exact commit `dc289005199768e23b89490e1822ee54f7b480df`; native CI run `34721937038` passed with artifact `10305779920`. This slice is not a playable native game.

## Current slice

The native generator adds four environmental projection profiles, environmental assessment/adaptation from supplied state, normal homeworld planning, and legacy fresh assignment. The default `--plan-homes` preview plans seven factions while preserving human/Earth origin and distinct viable worlds across all four galaxy sizes. Retained C# oracle coverage: 4 profiles, 68 environments, 192 assignments, 64 planet assessments, and 3 home scenarios.

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` emits the physical catalog before civilizations, including systems, planetary bodies, and Sol bodies. It is not a campaign, civilization simulation, game save-v16, or full parity result.

## Parity boundary

| System | Status |
| --- | --- |
| Foundation, distance rules, seeded catalog/body generation | Ported; local 7/7 CTest + 14 Python checks green; exact 0.1.2 CI pending |
| Species/environment/homeworld planning | Ported preview and legacy assignment; full civilization seeder and constrained expansion fallback open |
| Leaders, colonies, economy, logistics | Not ported |
| Fleets, research, diplomacy, AI, combat, territory, events | Open |
| Save/load/recovery | Foundation checkpoint only; no game-save-v16 adapter |
| Rendering/UI/input/audio/assets | Godot retained; no native graphical release or 60 FPS claim |
| Windows export | Headless package embeds astronomy JSON/README and dependency licenses; separate clean-machine release gate remains |

Fullgame Godot UI/audio/render remains the playable baseline. Territorial draft [PR #323](https://github.com/afterburn25/stellar-continuum/pull/323), checkpoint `fddd4763f2cadc11cec088d84b23ec2563eed2e7`, remains paused with enclosed visual pockets unresolved.

## Evidence policy and next step

Local 0.1.2 validation is green: 7/7 CTest and 14 Python checks; release export, relocation, and `--plan-homes` validation succeeded. Exact 0.1.2 CI is pending. Full civilization seeding, constrained nearby expansion fallback, leaders, colonies, economy, save-v16, and fullgame parity remain open. No performance or FPS claim is made.
