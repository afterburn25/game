# Stellar Engine migration handoff

Date: 2026-09-12. Branch `engine/stellar-engine-migration`; tracking [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.4 adds colony founding, labor, sustenance, reserves, and treasury support. It remains a headless migration slice; Godot remains the playable baseline.

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
- `native-tests`: nine CTest entries; `tools/stellar-export/test_export.py`: sixteen package/recovery/planning checks.
- The retained C# oracle covers 44 cases, including actual shortages, reserve exhaustion/replenishment, invalid inputs, and full colony/economy state.

0.1.3 exact native CI passed as run `34724121949` from commit [`71d53ef19fb698560fca00afc540ec2979537bed`](https://github.com/afterburn25/stellar-continuum/commit/71d53ef19fb698560fca00afc540ec2979537bed). Current 0.1.4 local validation passes 9/9 CTest and 16/16 Python checks; colony validation covers 9 colonies and 7 economies.

## Boundary

The 0.1.4 stage ends at initial colonies and economy support. It does not implement the full economy tick, surface power/workforce allocation, demographics, logistics, fleet setup, save-v16 persistence, rendering, or graphical parity. No playable-native or 60 FPS claim applies.

## Next atomic milestone

Port ColonySeeder integration into the campaign, then starting economy flow, budgets, population mutation, full economy, logistics, and persistence/save-v16. Preserve stable IDs, observer privacy, and the C# oracle. Keep territorial draft PR #323 (`fddd4763f2cadc11cec088d84b23ec2563eed2e7`) and its enclosed-pocket defect open.

## Historical baseline

The preserved playable baseline is integration `97091aee84b782bdc917185307cd42b97b8fd7d0`, tag `migration-baseline/stellar-continuum-0.1.7`; .NET 8 / Godot 4.7.2 Mono build and headless startup proof are preserved there.
