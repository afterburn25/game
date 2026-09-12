# Stellar Engine migration status

Updated 2026-09-12. Engine **0.1.1 physical-catalog slice**; existing game **0.1.7 Alpha**. Tracking issue: [#324](https://github.com/afterburn25/stellar-continuum/issues/324). The first native foundation remains preserved at integration `97091aee84b782bdc917185307cd42b97b8fd7d0`; first milestone PR #325 is based on that baseline. This slice is not a playable native game.

## Current slice

The native generator now ports the seeded .NET-compatible RNG, measured HYG96 identity from the embedded 500-record catalog, full-galaxy sizes 250/500/1000/2500, default placement/class/name/companion behavior, archetypes and flags, Sol and Pluto, procedural planets/moons, and environmental diversity. Retained C# fixture helpers and nine-planet scenarios remain the oracle. Other shapes and custom generation options are not migrated.

`--headless --generate-galaxy --systems 500 --seed <signed-int64> --repeat <1..100> --catalog-output <new-file>` emits the physical catalog before civilizations, including systems, planetary bodies, and Sol bodies. It is not a campaign, civilization simulation, game save-v16, or full parity result.

## Parity boundary

| System | Status |
| --- | --- |
| Foundation, distance rules, seeded catalog/body generation | Ported slices; local RelWithDebInfo 6/6 CTest + 13 Python checks green; clean-commit export/CI pending |
| Civilizations, species, population, economy, colonies, logistics | Open; next step is civilization/homeworld integration |
| Fleets, research, diplomacy, AI, combat, territory, events | Open |
| Save/load/recovery | Foundation checkpoint only; no game-save-v16 adapter |
| Rendering/UI/input/audio/assets | Godot retained; no native graphical release or 60 FPS claim |
| Windows export | Headless package embeds astronomy JSON/README and dependency licenses; separate clean-machine release gate remains |

Fullgame Godot UI/audio/render remains the playable baseline. Territorial draft [PR #323](https://github.com/afterburn25/stellar-continuum/pull/323), checkpoint `fddd4763f2cadc11cec088d84b23ec2563eed2e7`, remains paused with enclosed visual pockets unresolved.

## Evidence policy and next step

Local validation is green: RelWithDebInfo 6/6 CTest and 13 Python checks; release export and relocated restricted-PATH catalog generation succeeded. Oracle coverage includes 5500 systems/33717 procedural bodies, 500 source-catalog records, 768 RNG triplets, and 505 distance cases. Generation means for seed 8374837 repeat 10 were 1.571/2.643/6.512/44.384 ms for 250/500/1000/2500 systems (2030/3554/7102/17895 bodies); this excludes JSON serialization/catalog read and is not campaign throughput or FPS. Exact clean-commit export/CI remain pending. Next is civilization/homeworld creation.
