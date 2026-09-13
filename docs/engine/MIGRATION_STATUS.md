# Stellar Engine migration status

Updated 2026-09-12. Engine **0.1.7 funded-economy and colony-operations migration**; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.6 exact commit `e8ef77b1c54e3e4adf4b44c7858c28edeb479716` passed native GitHub CI run `34726784397`. The native slice is not a playable game.

## Current slice

The native pipeline separates physical catalogs, founding catalogs, and seeded colony catalogs. The 0.1.7 library adds funded economy and colony operations: credit flow, funding/arrears, storage, wear, resource outposts, and explicit colony advance projections. The CLI remains limited to physical, founding, and seed-colonies previews; it does not advance a whole campaign or fleets.

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` emits the physical catalog before civilizations, including systems, planetary bodies, and Sol bodies. It is not a campaign, civilization simulation, game save-v16, or full parity result.

## Parity boundary

| System | Status |
| --- | --- |
| Foundation, distance rules, seeded catalog/body generation | Ported; current 0.1.7 validation 13/13 CTest + 16/16 Python green |
| Species/environment/homeworld planning | Ported normal planner and legacy/fresh assignment; founding slice adds constrained expansion and guarantees |
| Founding civilizations and leadership | Ported; 0.1.3 exact CI passed |
| Colony seeding, surface support, and biology | Ported; four profiles and 85 biology cases validated |
| Funded economy, surface wear, and resource outposts | 0.1.7 focused validation passes 35 actual-C# cases plus 1 native validation; colony-operations covers 70 cases; final Release/Debug export gate pending |
| Whole-campaign tick, logistics, fleet setup, persistence | Open; this API uses explicit projections and makes no full-campaign claim |
| Fleets, research, diplomacy, AI, combat, territory, events | Open |
| Save/load/recovery | Foundation checkpoint only; no game-save-v16 adapter |
| Rendering/UI/input/audio/assets | Godot retained; no native graphical release or 60 FPS claim |
| Windows export | Headless package embeds astronomy JSON/README and dependency licenses; separate clean-machine release gate remains |

Fullgame Godot UI/audio/render remains the playable baseline. Territorial draft [PR #323](https://github.com/afterburn25/stellar-continuum/pull/323), checkpoint `fddd4763f2cadc11cec088d84b23ec2563eed2e7`, remains paused with enclosed visual pockets unresolved.

## Evidence policy and next step

Current 0.1.7 focused validation passes 13 CTest and 16 Python checks, with 35 funded-economy actual-C# cases plus 1 native validation and 70 colony-operations cases. Final Release/Debug Windows export validation remains pending. No whole-campaign tick, fleet simulation, save-v16, graphics parity, normal gameplay, performance, or FPS claim is made.

