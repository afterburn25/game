# Stellar Engine migration handoff

Date: 2026-09-12. Branch `engine/stellar-engine-migration`; tracking [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.2 adds species/environment assessment and homeworld planning. It remains a headless migration slice; Godot remains the playable baseline.

## Read first

1. [ARCHITECTURE.md](ARCHITECTURE.md): Engine/Core/Application ownership and stable interfaces.
2. [MIGRATION_INVENTORY.md](MIGRATION_INVENTORY.md): audited code and migration seams.
3. [MIGRATION_STATUS.md](MIGRATION_STATUS.md): current parity boundary and evidence.
4. [WINDOWS_EXPORT.md](WINDOWS_EXPORT.md): reproducible package commands and release gates.

## Current implementation

- `app/galaxy_main.cpp`: physical catalog mode plus `--plan-homes` preview; output ends before the full civilization seeder.
- `core/src/galaxy_catalog.cpp`, `stellar_population.cpp`, `planetary_*`, and `stellar_traits.cpp`: seeded .NET-compatible generation, HYG96-backed placement, default classes/names/companions/archetypes/flags, Sol/Pluto, procedural bodies, and environmental diversity.
- `core/src/species_environment.cpp` and `species_homeworlds.cpp`: four environmental projection profiles, assessment/adaptation from supplied state, normal homeworld planning, and legacy and fresh species assignment.
- `native-tests`: seven CTest entries, including species parity; `tools/stellar-export/test_export.py`: fourteen package/recovery/planning checks.
- `tests/Stellar.Species.ParityGenerator`: retained C# oracle for 4 profiles, 68 environments, 192 assignments, 64 planet assessments, and 3 home scenarios.
- `tools/stellar-export`: embeds astronomy JSON/README and dependency licenses, resolves assets beside the executable, and validates relocated restricted-PATH generation and home preview.
- Contracts: [GALAXY_MIGRATION_CONTRACT.md](GALAXY_MIGRATION_CONTRACT.md) and [SPECIES_MIGRATION_CONTRACT.md](SPECIES_MIGRATION_CONTRACT.md).

The 0.1.1 exact commit [`dc289005199768e23b89490e1822ee54f7b480df`](https://github.com/afterburn25/stellar-continuum/commit/dc289005199768e23b89490e1822ee54f7b480df) passed native CI run `34721937038` with artifact `10305779920`. Current 0.1.2 local validation passes 7/7 CTest and 14/14 Python checks; exact 0.1.2 CI is pending.

## Boundary

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` emits physical data. `--plan-homes` previews the default seven factions while preserving human/Earth origin and distinct viable worlds across 250/500/1000/2500 systems. This does not provide full civilization seeding, constrained nearby expansion fallback, leaders, colonies, economy, campaign behavior, or save-v16 persistence. It is not a playable native build or a 60 FPS result.

## Next atomic milestone

Port constrained nearby expansion fallback and the full `CivilizationSeeder`, then leadership, colonies, economy, and persistence/save-v16. Preserve stable identity mappings, observer privacy, and the C# oracle. Keep territorial draft PR #323 (`fddd4763f2cadc11cec088d84b23ec2563eed2e7`) and its enclosed-pocket defect open.

## Historical baseline

The preserved playable baseline is integration `97091aee84b782bdc917185307cd42b97b8fd7d0`, tag `migration-baseline/stellar-continuum-0.1.7`. .NET 8 / Godot 4.7.2 Mono build and headless startup proof are preserved there. The 0.1.1 physical catalog baseline exact commit and CI evidence are recorded above.
