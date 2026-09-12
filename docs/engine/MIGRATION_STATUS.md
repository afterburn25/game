# Stellar Engine migration status

Updated 2026-09-12. Engine **0.1.0 foundation**; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). Status: initial native foundation validated locally; full migration remains open. Normal gameplay expansion is paused by user instruction. Main/integration and the existing game remain intact.

## Preserved reference

Integration `97091aee84b782bdc917185307cd42b97b8fd7d0`, pushed annotated tag `migration-baseline/stellar-continuum-0.1.7`. `dotnet build Game.csproj -c Debug --no-restore` passed with zero warnings/errors; Godot 4.7.2 Mono headless editor import and game startup both exited 0, and the game printed `STELLAR_RUNTIME_READY IntegratedMain`. This is startup/build evidence, not a new visual acceptance playthrough.

Reference procedure: install .NET SDK 8 and Godot 4.7.2 Mono, `dotnet restore Game.csproj`, `dotnet build Game.csproj -c Debug`, then `godot --path .`. Headless startup probe: `godot --headless --path . --quit-after 12`. Preserve existing export/testing workflows and assets.

Pending territorial work is preserved separately in draft [PR #323](https://github.com/afterburn25/stellar-continuum/pull/323), checkpoint `fddd4763f2cadc11cec088d84b23ec2563eed2e7`. Its native overlay performance fix is saved; fully revealed enclosed visual pockets remain unresolved. It is not part of the migration baseline and must not silently disappear or be marked visually accepted.

## Parity matrix

| System | Native status | Old implementation / acceptance gate |
| --- | --- | --- |
| Build, log/error handling, IDs, clock, RNG, events, jobs | Foundation implemented/tested | New engine facilities; no claim of whole-game scheduling parity |
| Interstellar distance | First actual rule port, 505 C# reference cases pass | Legacy 2D float and physical 3D double distances; fleet interpolation and units remain C# |
| Galaxy generation, star/body catalog, orbital rules | Not migrated | Exact seeded generation, catalog positions, multiplicity, homeworlds and data IDs |
| Civilizations, species, population | Not migrated | Species/environment/labor/demographic and observer-data tests |
| Economy, construction, colonies, logistics | Not migrated | Affordability, labor/supply, timed queues, shortage/recovery and command preview parity |
| Fleets, shipbuilding, exploration/colonization | Not migrated | Travel phases, warp endpoints, mission timers, duplicate-command prevention |
| Research, diplomacy, AI, knowledge | Not migrated | Costs, progression, privacy, deterministic decisions, bounded work |
| Territory | Not migrated | Baseline presentation plus separately pending PR #323 rules and visual defect |
| Combat, missions/events/artifacts | Not migrated | Resolution, casualties, once-only rewards, save restoration and observer privacy |
| Save/load/recovery | Foundation checkpoint only | Full existing save-v16/import/export roundtrip; no current game saves rewritten |
| Rendering/UI/input/window | Godot retained | Galaxy/system/surface, DPI/cursor alignment, fullscreen/restore, mouse navigation and visual acceptance |
| Audio/voice | Godot/legacy backends retained | Music, cues, UK scientist voice, lifecycle/device handling |
| Asset/shader pipeline | Godot retained | Runtime asset IDs, texture/audio packs, compiled shaders and licenses |
| Windows export | Native headless validated locally | Graphical release blocked; separate clean-machine test outstanding |

## Measured first milestone

- Four CTest entries pass: foundation behavior (five groups), 505-case legacy distance parity, headless execution, invalid-argument rejection.
- Ten Python checks pass: package hashes/allowlist/traversal/duplicates, blocked graphical release, cross-worker checkpoint resume, corrupt/legacy save rejection, existing-save preservation and invalid arguments.
- Portable x64 Release export launches with a system-only PATH in a relocated directory; checkpoint continuation equals uninterrupted output at 1/2/4 workers. No development dependencies are imported.
- MSVC 19.44 Release, 4 workers, 100 ticks: 100/500/1000/2500/5000 synthetic positions averaged 0.0071/0.0141/0.0243/0.0509/0.0755 ms per tick in the initial sample. These are **distance microbenchmarks, not complete gameplay or FPS measurements**. Re-run for new commits/hardware; do not set gameplay performance targets from this sample.
- Windows native CI builds/tests/exports a benchmark package; its run status must be checked on the submitted commit.

## Next migration target

Extract galaxy/body data contracts and deterministic generator behavior into Core, beginning with source-generated reference fixtures for the existing RNG/catalog/homeworld outputs. Extend headless scenarios to consume actual preserved data rather than synthetic positions. Freeze save identity mappings before porting command-bearing systems. Add no new generation mechanics and retain the C# oracle until parity is demonstrated.

## Completion gate

Full migration is not done until all rows reach behavioral/player-facing parity, performance is measured with real campaigns, legacy saves recover safely, and a standalone graphical Windows release runs on a clean machine without Godot or development tools. Only after that can the old runtime be removed and normal feature expansion resume.
