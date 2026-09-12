# Stellar Engine migration handoff

Date: 2026-09-12. Branch `engine/stellar-engine-migration`; tracking [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.3 adds founding civilizations and constrained homeworld expansion. It remains a headless migration slice; Godot remains the playable baseline.

## Read first

1. [ARCHITECTURE.md](ARCHITECTURE.md): Engine/Core/Application ownership and stable interfaces.
2. [MIGRATION_INVENTORY.md](MIGRATION_INVENTORY.md): audited code and migration seams.
3. [MIGRATION_STATUS.md](MIGRATION_STATUS.md): current parity boundary and evidence.
4. [WINDOWS_EXPORT.md](WINDOWS_EXPORT.md): reproducible package commands and release gates.

## Current implementation

- `app/galaxy_main.cpp`: physical catalog mode plus `--plan-homes` preview; output ends before the full civilization seeder.
- `core/src/galaxy_catalog.cpp`, `stellar_population.cpp`, `planetary_*`, and `stellar_traits.cpp`: seeded .NET-compatible generation, HYG96-backed placement, default classes/names/companions/archetypes/flags, Sol/Pluto, procedural bodies, and environmental diversity.
- `core/src/species_environment.cpp` and `species_homeworlds.cpp`: four environmental projection profiles, assessment/adaptation from supplied state, normal homeworld planning, and legacy and fresh species assignment.
- `core/include/stellar/core/civilization_catalog.hpp` and founding sources: founding civilizations, leadership, player species, ancient flags, constrained fallback, within-system resolution, nearby guarantees, and the founding pipeline ending before colonies.
- `native-tests`: eight CTest entries, including civilization parity; `tools/stellar-export/test_export.py`: fifteen package/recovery/planning checks.
- `tests/Stellar.Species.ParityGenerator`: retained C# oracle for 4 profiles, 68 environments, 192 assignments, 64 planet assessments, and 3 home scenarios.
- `tools/stellar-export`: embeds astronomy JSON/README and dependency licenses, resolves assets beside the executable, and validates relocated restricted-PATH generation and home preview.
- Contracts: [GALAXY_MIGRATION_CONTRACT.md](GALAXY_MIGRATION_CONTRACT.md) and [SPECIES_MIGRATION_CONTRACT.md](SPECIES_MIGRATION_CONTRACT.md).

The 0.1.2 exact CI passed as run `34722838296` with artifact `10306633281`. Current 0.1.3 validation passes 8/8 CTest and 15/15 Python checks. Direct civilization validation covers 17 scenarios with 3 expected failures, 60 civilization records, and 30,591 body records.

## Boundary

`--headless --generate-galaxy --found-civilizations --systems 500 --seed <signed-int64> --civilizations 6 --ancients 1 --player-species terran_baseline --catalog-output <new-file>` emits `founding-before-colonies` data. It does not provide ColonySeeder, full campaign behavior, save-v16 persistence, graphics parity, or a playable native build/60 FPS result.

## Next atomic milestone

Port ColonySeeder and starting economy, then budgets, population, economy, and persistence/save-v16. Preserve stable identity mappings, observer privacy, and the C# oracle. Keep territorial draft PR #323 (`fddd4763f2cadc11cec088d84b23ec2563eed2e7`) and its enclosed-pocket defect open.

## Historical baseline

The preserved playable baseline is integration `97091aee84b782bdc917185307cd42b97b8fd7d0`, tag `migration-baseline/stellar-continuum-0.1.7`. .NET 8 / Godot 4.7.2 Mono build and headless startup proof are preserved there. The 0.1.1 physical catalog baseline exact commit and CI evidence are recorded above.
