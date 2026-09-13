# Stellar Engine migration status

Updated 2026-09-13. Engine **0.1.10 ship-production migration** over the earlier industry, logistics, currency, and construction ports; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). The native slice is not a playable game.

## Current slice

The native pipeline retains physical catalog, founding catalog, seeded-colony catalog, funded economy, construction, and logistics projections. It now adds ship definitions, full fleet value state, starter-fleet seeding, shipyard value state, and shipbuilding commands. Shipbuilding handles supported queue, cancellation, progression, completion, and order-sequence operations while preserving population and sequence conservation.

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` remains a physical/founding/seed-colonies preview surface. In seed mode it emits authoritative `constructionStates` and the derived economic projection. It does not execute full shipyard production, fleet transit, a whole campaign tick, research, save-v16 state, or rendering.

## Parity boundary

| System | Status |
| --- | --- |
| Foundation, distance rules, seeded catalog/body generation | Ported and retained |
| Species/environment/homeworld planning and founding civilizations | Ported and retained |
| Colony seeding, surface support, biology, funded economy, construction, industry, logistics, and currency | Ported as native libraries/projections and retained |
| Ship definitions | Ported; 37 actual-C# oracle cases |
| Full fleet value state and combat helpers | Ported; 70 actual-C# oracle cases |
| Shipyard value state, persistence safety, identity helpers, and seeding | Ported; 72 actual-C# oracle cases |
| Starter-fleet seeding | Ported; 27 actual-C# oracle cases; two starter identity-exhaustion boundaries and production-completion fleet-ID exhaustion reject natively rather than wrap |
| Shipbuilding commands | Ported; 89 actual-C# oracle cases; supported queue sequence and population conservation |
| Fleet local transit, lane graph, operational reach/refueling | Next milestone |
| Whole-campaign tick, research, diplomacy, AI, territory, events, persistence | Open |
| Save/load/recovery | Foundation checkpoint only; no game-save-v16 adapter |
| Rendering/UI/input/audio/assets | Godot retained; no native graphical release or FPS claim |

## Validation evidence

Both 0.1.10 pre-commit exports used source commit `e80e87f90563801166aeb68d8d8b8b9ec6ce79f3` and `sourceDirty: true`: benchmark `Builds/Windows/StellarContinuum-windows-benchmark-e80e87f9-20260913T031134742468Z` and Debug development `Builds/Windows/StellarContinuum-windows-development-e80e87f9-20260913T031246682304Z`. Each passed 24/24 CTest and 16/16 Python export checks, has a validated manifest and ZIP, and is recorded in [SHIP_PRODUCTION_VALIDATION.md](SHIP_PRODUCTION_VALIDATION.md). A clean exact-source package follows the root commit.

Historical 0.1.9 evidence is retained separately: all six CI runs passed commit `e80e87f90563801166aeb68d8d8b8b9ec6ce79f3`—native `34732049773`, build `34732049751`, Windows playable `34732049737`, research `34732049693`, voice `34732049758`, and screenshots `34732049705`. `Builds/Windows/StellarContinuum-windows-benchmark-e80e87f9-20260913T020330870148Z` is its clean package with `sourceDirty: false`. Territorial draft PR [#323](https://github.com/afterburn25/stellar-continuum/pull/323) remains paused with enclosed pockets unresolved.
