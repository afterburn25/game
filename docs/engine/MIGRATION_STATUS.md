# Stellar Engine migration status

Updated 2026-09-12. Engine **0.1.4 colony/economy founding slice**; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). The 0.1.3 exact CI passed as run `34724121949` from commit `71d53ef19fb698560fca00afc540ec2979537bed`. This slice is not a playable native game.

## Current slice

The native generator now continues through initial colony/economy state: canonical and legacy colonies, labor, sustenance capacity, reserve preview/advance, and treasury health. The retained C# oracle covers 44 cases, including actual shortages, exhaustion/replenishment, invalid inputs, and complete colony/economy state.

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` emits the physical catalog before civilizations, including systems, planetary bodies, and Sol bodies. It is not a campaign, civilization simulation, game save-v16, or full parity result.

## Parity boundary

| System | Status |
| --- | --- |
| Foundation, distance rules, seeded catalog/body generation | Ported; current 0.1.4 validation 9/9 CTest + 16/16 Python green |
| Species/environment/homeworld planning | Ported normal planner and legacy/fresh assignment; founding slice adds constrained expansion and guarantees |
| Founding civilizations and leadership | Ported; 0.1.3 exact CI passed |
| Colonies, labor, sustenance, reserves, treasury | Current 0.1.4 slice; 9 colonies and 7 economies validated |
| Full economy, logistics, fleet setup, persistence | Open |
| Fleets, research, diplomacy, AI, combat, territory, events | Open |
| Save/load/recovery | Foundation checkpoint only; no game-save-v16 adapter |
| Rendering/UI/input/audio/assets | Godot retained; no native graphical release or 60 FPS claim |
| Windows export | Headless package embeds astronomy JSON/README and dependency licenses; separate clean-machine release gate remains |

Fullgame Godot UI/audio/render remains the playable baseline. Territorial draft [PR #323](https://github.com/afterburn25/stellar-continuum/pull/323), checkpoint `fddd4763f2cadc11cec088d84b23ec2563eed2e7`, remains paused with enclosed visual pockets unresolved.

## Evidence policy and next step

Current 0.1.4 local validation passes 9/9 CTest and 16/16 Python checks; release/debug exports and relocated validation are being recorded for this checkpoint. The implementation ends before the full economy tick, surface allocation, demographics, logistics, fleet setup, save-v16, graphics parity, and normal gameplay. No performance or FPS claim is made.
