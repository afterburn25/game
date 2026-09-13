# Stellar Engine migration handoff

Date: 2026-09-13. Branch `engine/stellar-engine-migration`; tracking [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.10 is a headless ship-production migration over the earlier construction, industry, and logistics ports. Godot remains the playable baseline.

## Read first

1. [ARCHITECTURE.md](ARCHITECTURE.md)
2. [MIGRATION_INVENTORY.md](MIGRATION_INVENTORY.md)
3. [MIGRATION_STATUS.md](MIGRATION_STATUS.md)
4. [SHIP_PRODUCTION_VALIDATION.md](SHIP_PRODUCTION_VALIDATION.md)
5. [WINDOWS_EXPORT.md](WINDOWS_EXPORT.md)
6. [SHIPYARD_COMMAND_CONTRACT.md](SHIPYARD_COMMAND_CONTRACT.md)

## Current implementation

- All prior foundation, galaxy, species, founding, colony, biology, funded-economy, construction, industry, currency, and logistics ports remain available as native libraries and CLI previews.
- Native ship definitions cover 37 actual-C# oracle cases. Full fleet value state and combat helpers cover 70 cases, and shipyard state, persistence safety, identity helpers, and seeding cover 72 cases.
- Starter-fleet seeding covers 27 actual-C# cases. It preserves source ordering and colony/species conservation, while the two starter identity-exhaustion boundaries and the production-completion fleet-ID exhaustion boundary are explicit native rejections instead of unchecked wrapped identities.
- Shipbuilding commands cover 89 actual-C# cases: availability, design resolution, queued orders, cancellation, progression, completion, and sequence handling. Production conserves order sequence and population through the supported state operations.
- `--seed-colonies` still emits authoritative seed defaults and `constructionStates`; it does not execute shipyard production. The CLI remains limited to physical catalog, founding, and seed-colonies previews.

Pre-commit 0.1.10 validation used source `e80e87f90563801166aeb68d8d8b8b9ec6ce79f3` with `sourceDirty: true`. Benchmark and Debug development packages each passed 24/24 CTest and 16/16 Python export checks. Their package paths, manifests, fixture hashes, and command record are in [SHIP_PRODUCTION_VALIDATION.md](SHIP_PRODUCTION_VALIDATION.md). Root will publish a clean package after committing this source state.

Historical 0.1.9 evidence remains valid: all six CI runs passed commit `e80e87f90563801166aeb68d8d8b8b9ec6ce79f3`—native `34732049773`, build `34732049751`, Windows playable `34732049737`, research `34732049693`, voice `34732049758`, and screenshots `34732049705`. Its clean benchmark package is `Builds/Windows/StellarContinuum-windows-benchmark-e80e87f9-20260913T020330870148Z` with `sourceDirty: false`.

## Boundary

The native implementation now has ship definitions, fleet value state, starter fleets, and shipyard commands. It does not yet run fleet local transit or lane movement, campaign orchestration, research, save-v16, rendering, graphical parity, or a playable native game. No timer-driven full campaign or FPS claim applies.

## Next atomic milestone

The next milestone is local transit, lane graph traversal, and operational reach/refueling. Local transit is verified in drafts; lane graph and operational reach are in progress; none is included in this checkpoint. Campaign orchestration has not started. The migrated logistics library remains a projection rather than a campaign orchestrator.

The territorial draft PR [#323](https://github.com/afterburn25/stellar-continuum/pull/323) remains paused with enclosed pockets unresolved.

## Historical baseline

The preserved playable baseline is integration `97091aee84b782bdc917185307cd42b97b8fd7d0`, tag `migration-baseline/stellar-continuum-0.1.7`; .NET 8 / Godot 4.7.2 Mono build and headless startup proof are preserved there.
