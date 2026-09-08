# Species / Planetary Integration

This document records the shared contract between deterministic planetary physics, Species & Race Mechanics, Exploration/Colonization, persistence, AI, logistics, Combat and Diplomacy.

## Ownership boundaries

Planet/environment generation owns authoritative physical world facts. Species owns biological interpretation of those facts. Colonization owns mission ordering and settlement founding. Logistics/research/construction own the capabilities and costs that make hostile worlds supportable.

Species must not generate a competing planet model, and planet generation must not label a world universally habitable for all species.

## Planetary physics input

`PlanetaryBodyState.Environment` is authoritative physical state:

- gravity
- temperature
- pressure
- atmosphere regime
- naturally available solvent
- radiation hazard
- immersion state
- solid-surface availability

`PlanetaryHabitatEnvironmentMapper` converts those facts into the narrow `HabitatEnvironment` consumed by species evaluation. It does not alter the world or infer political/economic consequences.

## Species-relative habitability

`SpeciesPlanetaryHabitabilityEvaluator` evaluates a body for a specific species.

A current naturally viable colony body must:

- have a solid settlement surface;
- have no native pre-warp civilization;
- meet the current natural-habitability threshold;
- meet the current unprotected-operation threshold;
- have nonzero atmosphere and solvent compatibility.

The result retains the complete `SpeciesEnvironmentAssessment`, so callers can inspect the actual limiting axes and mitigation requirements rather than relying on a generic race bonus.

### Temporary compatibility fallback

`LegacyColonizationCandidate` is no longer treated as universal natural habitability. It is only a temporary `HabitatSupportedFallback` for the existing early-release progression path.

That fallback represents an assumed prototype habitat-support package while research/construction/logistics support capabilities are not yet connected end-to-end. It must not be interpreted as the species naturally tolerating the external environment.

Once explicit habitat-support capability and operating cost are wired into colony construction, this fallback should be removed and replaced by `SpeciesSupportedHabitatEvaluator` with real support inputs.

## Survey / fair-information boundary

Species-relative body suitability may only drive strategic colony opportunity after the acting civilization has legitimately completed the detailed system survey that exposes body environmental facts.

Detection alone exposes no planetary catalog. Reconnaissance can expose a basic orbital catalog but not detailed environment. Full science survey exposes the physical facts needed for habitability evaluation.

AI and player colony order validation therefore use the same knowledge boundary.

## Body-specific colony missions

Because different species can prefer different bodies in the same star system, a colony mission must remember the exact selected world.

Current v8 bridge fields:

- `FleetState.DestinationSystemId`
- `FleetState.DestinationPlanetaryBodyId`
- `FleetState.EmbarkedPopulationMillions`
- `FleetState.EmbarkedPopulationSpeciesId`

A positive population payload must have a known species identity. A body target must belong to the destination system.

New body-aware colony orders persist both system and body. Observer-local mission read models expose the explicit body target. Legacy v7 missions that genuinely lack a body ID may still resolve the unique deterministic compatibility candidate.

## Colony body identity

`ColonyState.PlanetaryBodyId` records the exact occupied body for new body-aware settlements.

`ColonyState.PopulationSpeciesId` records the species represented by the current scalar population bridge.

Founding transfers exactly:

`embarked population + embarked species + selected body` -> `colony population + population species + occupied body`

The consumed colony fleet clears its population, species and body-target payload.

Legacy/home colonies may have a null body ID and resolve through the deterministic compatibility body until starting-colony generation becomes explicitly body-native.

## Save v8

Candidate save v8 composes all shared state rather than replacing another workstream's schema:

- species IDs;
- scalar colony population species IDs;
- shipyard reserved-population species IDs;
- embarked population species IDs;
- exact colony-mission body targets;
- exact founded-colony body IDs;
- staged survey knowledge;
- physical embarked population quantities;
- shipyard state;
- Fleet Combat state;
- the rest of current integration state.

The deterministic full planet/moon catalog is reconstructible from campaign seed + systems and is deliberately not serialized.

V7 migration leaves explicit body IDs null because v7 never knew which body a system-level colony mission meant. It reconstructs species identity deterministically without inventing body-specific historical facts.

V8 rejects invalid body references rather than silently redirecting a colony or mission to a different world.

## AI behavior

`CivilizationStrategicInputBuilder` considers a known colonization opportunity only when:

- the system is fully surveyed;
- the system is not already colonized under the current single-colony-per-system early-release rule;
- at least one population species actually available to that civilization can use a body under the current species-relative viability contract.

AI colony fleets evaluate the species actually embarked on that fleet. Naturally viable worlds rank above the temporary habitat-supported fallback, then normal strategic/resource priorities can distinguish among viable choices.

This does not make biology dictate expansionism. Whether a civilization wants to expand remains a civilization/AI decision; Species only answers whether and how a particular population can physically use a world.

## Current population limitation

The economy still mutates `ColonyState.PopulationMillions` directly. Therefore authoritative multi-species colony cohorts are not introduced yet; doing so now would create two competing population sources of truth.

The transitional single-species scalar bridge remains authoritative for current colonies, while `CurrentPopulationSpeciesSnapshot` and `SpeciesPopulationCohort` provide read-only/future-compatible interfaces for consumers.

When the colony/population owner replaces the scalar population model, existing population/body/species fields should migrate into bounded cohorts rather than multiplying parallel scalar fields.
