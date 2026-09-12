# Stellar Engine foundation handoff

Date: 2026-09-12. Branch `engine/stellar-engine-migration`; status in [MIGRATION_STATUS.md](MIGRATION_STATUS.md). Tracking [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Engine 0.1.1 ports physical catalog generation, not a full migration. Do not remove Godot or call the native output playable.

## Read first

1. [ARCHITECTURE.md](ARCHITECTURE.md): Engine/Core/Application ownership, stable interfaces and threading rules.
2. [MIGRATION_INVENTORY.md](MIGRATION_INVENTORY.md): audited current code/dependencies and concrete migration seams.
3. [MIGRATION_STATUS.md](MIGRATION_STATUS.md): baseline proof, parity matrix, local tests/benchmarks and next target.
4. [WINDOWS_EXPORT.md](WINDOWS_EXPORT.md): reproducible toolchain/build/package commands and remaining release gates.

## Implementation map

- `engine/include/stellar/engine/foundation.hpp`, `engine/src/foundation.cpp`: generational registry, fixed clock, SplitMix64 utility, owner-thread event queue, persistent worker pool and diagnostics. Entity destruction preserves coherent state if free-list allocation fails; partial thread startup joins started workers before propagating failure. Worker futures retain task errors; application drains workers before captured result buffers unwind.
- `core/*/interstellar_distance.*`: actual legacy 2D and physical 3D distance rules, C++23, renderer independent. No generation/gameplay rule redesign.
- `tests/Stellar.Distance.ParityGenerator`: calls the actual preserved C# `InterstellarDistance`, emitting 505 fixtures. Regenerate from repository root using `dotnet run --project tests/Stellar.Distance.ParityGenerator/Stellar.Distance.ParityGenerator.csproj -c Release -- native-tests/fixtures/interstellar-distance.csv`; review any diff. Never regenerate from a rewritten expected formula to make a failing native port pass.
- `native-tests`: five foundation behavior groups and distance parity; CTest also invokes valid/invalid CLI cases.
- `app/headless_main.cpp`: deterministic synthetic distance scenario, explicit ticks/seed/worker count, standalone checkpoint and JSON timing. No renderer, campaign loader, civilization AI or real galaxy generation is claimed.
- `tools/stellar-export`: build/export/validation commands plus ten Python integrity/recovery checks. `export/runtime-config.json` supplies engine/game versions to CMake resources and manifests. `export/stellar-presets.json` keeps graphical presets blocked until parity.
- `.github/workflows/stellar-engine.yml`: Windows native build, tests, portable export and artifact upload. Existing game CI is retained.

## 0.1.1 boundary

Seeded .NET-compatible RNG, HYG96-backed 250/500/1000/2500 placement, default classes/names/companions/archetypes/flags, Sol/Pluto, procedural planets/moons, and environmental diversity are ported. C# fixture helpers and nine-planet scenarios remain authoritative. Local validation is green at 6/6 CTest plus 13 Python checks, with release export and relocated restricted-PATH generation succeeding. Exact clean-commit export/CI remain pending. `--headless --generate-galaxy` ends before civilizations; custom shapes/options, civilization/homeworld creation, campaign loop, and save-v16 remain open. These are not playable-native or 60 FPS results.

## Evidence and limitations

The preserved game built Debug with zero warnings/errors and reached `STELLAR_RUNTIME_READY IntegratedMain` in a headless launch. Native RelWithDebInfo and Release builds passed four CTest entries; Release and Debug exports also passed all ten Python checks and relocated launch/restore validation. Initial distance microbenchmark results are in the status matrix. They establish foundation behavior only, not complete game performance.

Local development evidence under ignored `work/`: `baseline-editor-launch.log`, `baseline-build.log`, `baseline-game-launch.log`, `native-benchmark-export.log`, `native-development-export.log`. Versioned standalone packages and validation sidecars are under ignored `Builds/Windows/`. CI exports provide portable evidence on GitHub; attach their exact workflow/artifact status to the PR. Source commit plus manifest dirty flag identifies precommit local probes; distribute a clean commit export for a milestone.

Runtime asset imports, full save-v16 adapter, SDL/Vulkan platform/rendering, gameplay systems and graphical release are **not implemented**. Windows import analysis plus a restricted-PATH relocated launch is useful evidence but not a separate clean-machine certification. A graphical release request deliberately fails with a useful error until these gates are met. Runtime config is metadata, not yet a player settings loader.

## Paused work to preserve

Territorial draft PR #323 is checkpointed at `fddd4763f2cadc11cec088d84b23ec2563eed2e7`. It restored measured native regional overlay FPS from 48.0 to 60.6, but fully revealed large regions retain enclosed dark visual pockets. This known user-reported defect is open. Keep its simulation/privacy contracts and source; evaluate its remaining geometry during the territory migration. Do not overwrite it with a clean baseline or report the gaps eliminated.

## Next atomic milestone

Freeze galaxy/body model and RNG/data identity contracts, export C# reference fixtures for actual seeded catalog/homeworld generation, then port that behavior into Core with old/new comparisons. Preserve known star distances and observer privacy. Add real scenario benchmarks only as those systems migrate. Use Luna for inventories/test execution, Terra for ordinary ports, and escalate specific failures rather than spending Core reasoning while workers run.

Keep durable progress on GitHub: commit/push coherent slices, update parity/test evidence and the handoff, and leave incomplete systems explicitly open. Normal gameplay expansion stays paused until full native game parity and standalone graphical Windows export are complete.
