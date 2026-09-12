# Stellar Engine migration status

Updated 2026-09-12. Engine **0.1.3 founding-civilizations slice**; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). The 0.1.2 baseline passed exact CI `34722838296` with artifact `10306633281`; current 0.1.3 validation is green. This slice is not a playable native game.

## Current slice

The native generator now adds founding civilizations, founding leadership, ancient flags, player-species selection, constrained homeworld fallback/within-system resolution, nearby habitable guarantees, and the original founding pipeline through `founding-before-colonies`. `--found-civilizations --civilizations 1..13 --ancients 0..3 --player-species <id>` selects this stage. Native oracle coverage has 17 cases; test totals for the current source are pending.

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` emits the physical catalog before civilizations, including systems, planetary bodies, and Sol bodies. It is not a campaign, civilization simulation, game save-v16, or full parity result.

## Parity boundary

| System | Status |
| --- | --- |
| Foundation, distance rules, seeded catalog/body generation | Ported; current 0.1.3 validation 8/8 CTest + 15/15 Python green |
| Species/environment/homeworld planning | Ported normal planner and legacy/fresh assignment; founding slice adds constrained expansion and guarantees |
| Founding civilizations and leadership | Current 0.1.3 slice; 17 scenarios, 3 expected failures, 60 civilizations, 30,591 body records |
| Leaders, colonies, economy, logistics | Not ported |
| Fleets, research, diplomacy, AI, combat, territory, events | Open |
| Save/load/recovery | Foundation checkpoint only; no game-save-v16 adapter |
| Rendering/UI/input/audio/assets | Godot retained; no native graphical release or 60 FPS claim |
| Windows export | Headless package embeds astronomy JSON/README and dependency licenses; separate clean-machine release gate remains |

Fullgame Godot UI/audio/render remains the playable baseline. Territorial draft [PR #323](https://github.com/afterburn25/stellar-continuum/pull/323), checkpoint `fddd4763f2cadc11cec088d84b23ec2563eed2e7`, remains paused with enclosed visual pockets unresolved.

## Evidence policy and next step

The 0.1.2 exact CI passed with 7/7 CTest and 14 Python checks. Current 0.1.3 validation passes 8/8 CTest and 15/15 Python checks. ColonySeeder, starting economy, budgets, population, full campaign, save-v16, graphics parity, and normal gameplay remain open. No performance or FPS claim is made.
