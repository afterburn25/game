# Stellar Engine migration handoff

Date: 2026-09-13. Branch `engine/stellar-engine-migration`; tracking [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.9 is a headless construction/currency migration slice over the 0.1.8 industry/logistics ports; Godot remains the playable baseline.

## Read first

1. [ARCHITECTURE.md](ARCHITECTURE.md)
2. [MIGRATION_INVENTORY.md](MIGRATION_INVENTORY.md)
3. [MIGRATION_STATUS.md](MIGRATION_STATUS.md)
4. [WINDOWS_EXPORT.md](WINDOWS_EXPORT.md)
5. [CIVILIZATION_MIGRATION_CONTRACT.md](CIVILIZATION_MIGRATION_CONTRACT.md)
6. [COLONY_ECONOMY_MIGRATION_CONTRACT.md](COLONY_ECONOMY_MIGRATION_CONTRACT.md)
7. [FUNDED_ECONOMY_MIGRATION_CONTRACT.md](FUNDED_ECONOMY_MIGRATION_CONTRACT.md)

## Current implementation

- Galaxy, species, founding civilizations, leadership, constrained homeworld expansion, and nearby guarantees remain ported from prior slices.
- `core/src/colony_economy.cpp` seeds canonical and legacy colonies, complete default colony/economy state, labor snapshots, sustenance capacity, reserve preview/advance, and treasury health.
- 0.1.9 validation passed 19/19 CTest and 16/16 Python export checks on source commit `6a8b2b7d39e6f4dc07696c75264a397442eb1c77` with `sourceDirty: true` during pre-commit validation.
- `core/src/colony_biology.cpp` adds authored species biology, metabolic/demographic factors, colonization policy, habitat burden, turnover, and colony-support reporting.
- `core/src/campaign_economy.cpp` and `core/src/colony_operations.cpp` implement the frozen funded-economy/operations projections, including credit flow, funding and arrears, storage, wear, resource outposts, and colony advance ordering.
- The retained C# oracle includes 115 currency cases plus 10 `FixedOneDecimal` and two `FixedPadding` cases, actual civilization lookup/seeding coverage, 95 surface-construction cases, 46 construction-project cases, and 85 biology cases across four profiles.

Native 0.1.8 CI passed as run `34729620833`. The 0.1.9 Release benchmark export is `Builds/Windows/StellarContinuum-windows-benchmark-6a8b2b7d-20260913T015506607374Z`; the Debug development export is `Builds/Windows/StellarContinuum-windows-development-6a8b2b7d-20260913T015610178368Z`. Both passed their export validation. Native 0.1.9 is not committed and has no CI result yet.

## Boundary

The 0.1.9 stage adds construction/currency APIs and authoritative construction state, with paid queue/timer coverage. `--seed-colonies` retains authoritative construction state, derives an economic projection, and emits `constructionStates` only in seed mode. It does not implement whole-campaign ticking, fleets, research, save-v16 persistence, rendering, or graphical parity. No playable-native or 60 FPS claim applies.

## Next atomic milestone

Next work is ship design prerequisites and propulsion, then complete fleet state and shipyard queue ordering/population conservation before campaign integration. The logistics library is migrated, but it remains a projection rather than a whole-campaign orchestrator. Logistics contract: [LOGISTICS_MIGRATION_CONTRACT.md](LOGISTICS_MIGRATION_CONTRACT.md). Keep territorial draft PR #323 (`fddd4763f2cadc11cec088d84b23ec2563eed2e7`) and its enclosed-pocket defect open.

## Historical baseline

The preserved playable baseline is integration `97091aee84b782bdc917185307cd42b97b8fd7d0`, tag `migration-baseline/stellar-continuum-0.1.7`; .NET 8 / Godot 4.7.2 Mono build and headless startup proof are preserved there.
