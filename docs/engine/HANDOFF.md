# Stellar Engine migration handoff

Date: 2026-09-12. Branch `engine/stellar-engine-migration`; tracking [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.7 is a headless funded-economy/colony-operations migration slice; Godot remains the playable baseline.

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
- Current validation: thirteen CTest entries; `tools/stellar-export/test_export.py`: sixteen package/recovery/planning checks.
- `core/src/colony_biology.cpp` adds authored species biology, metabolic/demographic factors, colonization policy, habitat burden, turnover, and colony-support reporting.
- `core/src/campaign_economy.cpp` and `core/src/colony_operations.cpp` implement the frozen funded-economy/operations projections, including credit flow, funding and arrears, storage, wear, resource outposts, and colony advance ordering.
- The retained C# oracle covers 85 biology cases across four profiles, with the prior colony/economy cases preserved.

Engine 0.1.6 exact native CI passed as run `34726784397` from commit [`e8ef77b1c54e3e4adf4b44c7858c28edeb479716`](https://github.com/afterburn25/stellar-continuum/commit/e8ef77b1c54e3e4adf4b44c7858c28edeb479716). Current 0.1.7 validation passes 13 CTest and 16 Python checks, with 35 funded-economy actual-C# cases plus 1 native validation and 70 colony-operations cases; Release benchmark and Debug development exports passed.

## Boundary

The 0.1.7 stage ends at explicit funded-economy and colony-operations projections. It does not implement the whole-campaign tick orchestrator, construction authorization/timers, logistics, fleet simulation, full save-v16 persistence, rendering, or graphical parity. The CLI supports physical/founding/seed-colonies previews and does not advance whole campaigns or fleets. No playable-native or 60 FPS claim applies.

## Next atomic milestone

Next gate is source freeze after the remaining funded-economy and colony-operations fixture cases, then Release/Debug Windows export validation. Preserve stable IDs, observer privacy, actual-C# oracle comparisons, and frozen advance order. Keep territorial draft PR #323 (`fddd4763f2cadc11cec088d84b23ec2563eed2e7`) and its enclosed-pocket defect open.

## Historical baseline

The preserved playable baseline is integration `97091aee84b782bdc917185307cd42b97b8fd7d0`, tag `migration-baseline/stellar-continuum-0.1.7`; .NET 8 / Godot 4.7.2 Mono build and headless startup proof are preserved there.
