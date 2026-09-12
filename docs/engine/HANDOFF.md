# Stellar Engine migration handoff

Date: 2026-09-12. Branch `engine/stellar-engine-migration`; tracking [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.6 is a headless colony-biology migration slice; Godot remains the playable baseline.

## Read first

1. [ARCHITECTURE.md](ARCHITECTURE.md)
2. [MIGRATION_INVENTORY.md](MIGRATION_INVENTORY.md)
3. [MIGRATION_STATUS.md](MIGRATION_STATUS.md)
4. [WINDOWS_EXPORT.md](WINDOWS_EXPORT.md)
5. [CIVILIZATION_MIGRATION_CONTRACT.md](CIVILIZATION_MIGRATION_CONTRACT.md)
6. [COLONY_ECONOMY_MIGRATION_CONTRACT.md](COLONY_ECONOMY_MIGRATION_CONTRACT.md)

## Current implementation

- Galaxy, species, founding civilizations, leadership, constrained homeworld expansion, and nearby guarantees remain ported from prior slices.
- `core/src/colony_economy.cpp` seeds canonical and legacy colonies, complete default colony/economy state, labor snapshots, sustenance capacity, reserve preview/advance, and treasury health.
- `native-tests`: ten CTest entries; `tools/stellar-export/test_export.py`: sixteen package/recovery/planning checks.
- `core/src/colony_biology.cpp` adds authored species biology, metabolic/demographic factors, colonization policy, habitat burden, turnover, and colony-support reporting.
- `native-tests`: eleven CTest entries; `tools/stellar-export/test_export.py`: sixteen package/recovery/planning checks.
- The retained C# oracle covers 85 biology cases across four profiles, with the prior colony/economy cases preserved.

0.1.3 exact native CI passed as run `34724121949` from commit [`71d53ef19fb698560fca00afc540ec2979537bed`](https://github.com/afterburn25/stellar-continuum/commit/71d53ef19fb698560fca00afc540ec2979537bed). Current 0.1.6 local validation passes 11/11 CTest and 16/16 Python checks; biology validation covers four profiles and 85 cases.

## Boundary

The 0.1.6 stage ends at generated colony-biology and support reports: surface staffing/power/output, sustenance/reserves, habitat support, and turnover pressure. Reports are calculated after timed generation and excluded from benchmark means. It does not implement a full simulation tick, timed construction, funded economy, logistics, fleets, save-v16 persistence, rendering, or graphical parity. No playable-native or 60 FPS claim applies.

## Next atomic milestone

Next gate the funded economy, then supporting surface-wear/resource-outpost rules. Preserve stable IDs, observer privacy, and the C# oracle. Keep territorial draft PR #323 (`fddd4763f2cadc11cec088d84b23ec2563eed2e7`) and its enclosed-pocket defect open.

## Historical baseline

The preserved playable baseline is integration `97091aee84b782bdc917185307cd42b97b8fd7d0`, tag `migration-baseline/stellar-continuum-0.1.7`; .NET 8 / Godot 4.7.2 Mono build and headless startup proof are preserved there.
