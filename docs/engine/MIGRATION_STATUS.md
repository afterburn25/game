# Stellar Engine migration status

Updated 2026-09-12. Engine **0.1.8 industry and logistics migration**; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.7 exact CI run `34728214618` passed. The native slice is not a playable game.

## Current slice

The native pipeline separates physical catalogs, founding catalogs, and seeded colony catalogs. The 0.1.8 library adds industry allocation and logistics routing/views over the 0.1.7 funded economy and colony operations. The CLI remains limited to physical, founding, and seed-colonies previews; it does not advance a whole campaign or fleets.

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` emits the physical catalog before civilizations, including systems, planetary bodies, and Sol bodies. It is not a campaign, civilization simulation, game save-v16, or full parity result.

## Parity boundary

| System | Status |
| --- | --- |
| Foundation, distance rules, seeded catalog/body generation | Ported; current 0.1.8 validation 16/16 CTest + 16/16 Python green |
| Species/environment/homeworld planning | Ported normal planner and legacy/fresh assignment; founding slice adds constrained expansion and guarantees |
| Founding civilizations and leadership | Ported; 0.1.3 exact CI passed |
| Colony seeding, surface support, and biology | Ported; four profiles and 85 biology cases validated |
| Funded economy, surface wear, resource outposts, industry, and logistics | 0.1.8 validation passes 16/16 CTest and 16/16 Python checks; source coverage includes logistics route36 C# cases, views30, and industry24 |
| Whole-campaign tick, logistics, fleet setup, persistence | Open; this API uses explicit projections and makes no full-campaign claim |
| Fleets, research, diplomacy, AI, combat, territory, events | Open |
| Save/load/recovery | Foundation checkpoint only; no game-save-v16 adapter |
| Rendering/UI/input/audio/assets | Godot retained; no native graphical release or 60 FPS claim |
| Windows export | 0.1.8 Release benchmark and Debug development exports passed; separate clean-machine release gate remains |

Fullgame Godot UI/audio/render remains the playable baseline. Territorial draft [PR #323](https://github.com/afterburn25/stellar-continuum/pull/323), checkpoint `fddd4763f2cadc11cec088d84b23ec2563eed2e7`, remains paused with enclosed visual pockets unresolved.

## Evidence policy and next step

Current 0.1.8 validation passes 16 CTest and 16 Python checks. Release package: `Builds/Windows/StellarContinuum-windows-benchmark-28755ca1-20260913T010156889008Z`; Debug package: `Builds/Windows/StellarContinuum-windows-development-28755ca1-20260913T010257399404Z`. Both manifests record source commit `28755ca175b374420caeb1d28bf16a5b0ef42a0a`. No whole-campaign tick, fleet simulation, save-v16, graphics parity, normal gameplay, performance, or FPS claim is made.

