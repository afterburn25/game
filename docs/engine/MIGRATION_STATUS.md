# Stellar Engine migration status

Updated 2026-09-13. Engine **0.1.9 construction/currency migration** over the 0.1.8 industry/logistics ports; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Native 0.1.8 CI run `34729620833` passed. The native slice is not a playable game.

## Current slice

The native pipeline separates physical catalogs, founding catalogs, and seeded colony catalogs. The 0.1.9 library ports currency/state, construction commands/timers/upgrades/validation, and paid queues/cancel/timers/AI over the 0.1.7 funded economy and colony operations. In seed mode the CLI exports authoritative `constructionStates` and the economic projection derived from them. It remains limited to physical, founding, and seed-colonies previews; it does not advance a whole campaign, fleets, research, or save-v16 state.

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` emits the physical catalog before civilizations, including systems, planetary bodies, and Sol bodies. It is not a campaign, civilization simulation, game save-v16, or full parity result.

## Parity boundary

| System | Status |
| --- | --- |
| Foundation, distance rules, seeded catalog/body generation | Ported; current 0.1.9 validation 19/19 CTest + 16/16 Python green |
| Species/environment/homeworld planning | Ported normal planner and legacy/fresh assignment; founding slice adds constrained expansion and guarantees |
| Founding civilizations and leadership | Ported; 0.1.3 exact CI passed |
| Colony seeding, surface support, and biology | Ported; four profiles and 85 biology cases validated; surface construction 95 cases and projects 46 cases |
| Funded economy, surface wear, resource outposts, industry, logistics, currency, and construction | Ported as explicit libraries/projections; currency has 115 cases plus 10 `FixedOneDecimal`, two `FixedPadding`, and civilization lookup/seeding coverage |
| Whole-campaign tick, fleet setup, persistence | Open; the migrated logistics library does not constitute a campaign orchestrator |
| Fleets, research, diplomacy, AI, combat, territory, events | Open |
| Save/load/recovery | Foundation checkpoint only; no game-save-v16 adapter |
| Rendering/UI/input/audio/assets | Godot retained; no native graphical release or 60 FPS claim |
| Windows export | 0.1.9 Release benchmark and Debug development exports passed; separate clean-machine release gate remains |

Fullgame Godot UI/audio/render remains the playable baseline. Territorial draft [PR #323](https://github.com/afterburn25/stellar-continuum/pull/323), checkpoint `fddd4763f2cadc11cec088d84b23ec2563eed2e7`, remains paused with enclosed visual pockets unresolved.

## Evidence policy and next step

0.1.9 validation passed 19/19 CTest and 16/16 Python checks. Release: `Builds/Windows/StellarContinuum-windows-benchmark-6a8b2b7d-20260913T015506607374Z`; Debug: `Builds/Windows/StellarContinuum-windows-development-6a8b2b7d-20260913T015610178368Z`. Both manifests record engine 0.1.9, source commit `6a8b2b7d39e6f4dc07696c75264a397442eb1c77`, and `sourceDirty: true` from pre-commit validation. Both exports passed relocated/restricted-path/checkpoint/preview validation; native CI 0.1.9 is not yet known. No whole-campaign tick, fleet simulation, save-v16, graphics parity, normal gameplay, performance, or FPS claim is made.

