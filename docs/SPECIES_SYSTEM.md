# Species and Race Mechanics

This document defines the public architecture for biological species mechanics in **Stellar Continuum**.

The player may casually think of this as the game's "race system," but the simulation distinguishes several things that many strategy games collapse together:

1. **species biology** — inherited physiology and environmental requirements;
2. **population adaptation** — acclimatization and longer-lived changes affecting a particular population;
3. **civilization traits/culture** — learned political, social, strategic, and institutional behavior;
4. **technology/capabilities** — ways a civilization compensates for or exploits biological/environmental conditions.

These layers must remain separate enough that biology does not automatically dictate culture or politics.

## Core rule: contextual consequences, not arbitrary racial bonuses

A species does not receive a generic permanent modifier such as `+10% combat`, `+15% science`, or `-10% diplomacy` simply because of its species identity.

Advantages and disadvantages should arise from physical circumstances wherever practical.

Examples:

- A population evolved for high gravity may function naturally on a high-gravity world while a low-gravity population requires substantial mitigation.
- The same high-gravity species can be outside its own comfortable range on a low-gravity colony.
- An aquatic species may require immersed habitats even when temperature and atmospheric chemistry are otherwise compatible.
- A cryogenic hydrocarbon species can find a world lethal that a water/oxygen species considers ideal, and vice versa.
- Radiation tolerance changes how much shielding is required in a hazardous environment; it is not a universal military bonus.
- Longer lifespan affects generation time, adaptation tempo, demographics, institutional memory, and other later systems rather than being converted into a single abstract percentage.

This keeps species differences mechanically meaningful without turning biology into stereotypes about intelligence, morality, aggression, greed, diplomacy, or political organization.

## Layer 1: immutable species definition

The static `SpeciesDefinition` describes inherited biological facts shared by a baseline member of a species.

Current foundation fields include:

- stable species ID;
- display name;
- biochemical basis;
- habitat mode;
- typical adult mass;
- baseline lifespan;
- maturity age;
- baseline metabolic demand;
- radiation tolerance;
- musculoskeletal robustness;
- preferred and survivable gravity range;
- preferred and survivable temperature range;
- preferred and survivable pressure range;
- breathable atmosphere classes;
- compatible biological solvents;
- immersion requirement;
- unprotected vacuum capability when biologically appropriate;
- baseline generation length.

Species definitions are static shared game data. They should not be copied into every population or save record.

## Layer 2: habitat/environment description

`HabitatEnvironment` is the physical input used when asking whether a species can live or operate somewhere.

The current foundation represents:

- gravity in Earth gravities (`g`);
- temperature in kelvin;
- pressure in kilopascals;
- atmosphere class;
- available solvent;
- normalized radiation hazard;
- whether the population is immersed.

This is intentionally an interface-sized environmental model. The current gameplay prototype still represents a star system with a simple `HasHabitableWorld` flag; a later planetary/environment workstream can provide real planet values without species code taking ownership of planet generation.

## Layer 3: population adaptation

`PopulationAdaptationState` belongs to a population, not to the immutable species definition.

The initial compact state can shift/widen:

- gravity preference/tolerance;
- temperature preference/tolerance;
- pressure preference/tolerance;
- radiation tolerance;
- short/medium-term acclimatization.

This is the foundation for the project's longer-term adaptation direction:

- individual acclimatization;
- developmental adaptation among locally born generations;
- multigenerational natural adaptation;
- deliberate medical/genetic/cybernetic intervention where technology permits.

Those later processes must change population state through actual simulation over time. They should not silently rewrite the base species template.

## Derived environmental assessment

`SpeciesEnvironmentEvaluator` is deterministic and returns a `SpeciesEnvironmentAssessment`.

Current outputs include:

- natural habitability;
- unprotected operational capacity;
- gravity suitability;
- temperature suitability;
- pressure suitability;
- atmosphere suitability;
- solvent suitability;
- immersion suitability;
- radiation suitability;
- the strongest limiting factor;
- whether gravity mitigation is required;
- whether thermal control is required;
- whether pressure control is required;
- whether a sealed habitat is required;
- whether an artificial biosphere is required;
- whether radiation shielding is required;
- derived health stress.

**Natural habitability is limited by the worst essential physical requirement.** A population cannot average its way out of an unbreathable atmosphere or incompatible solvent because the temperature happens to be excellent.

**Unprotected operational capacity uses a geometric mean** of the environmental factors. This gives later systems a smooth general-purpose measure while still preserving hard limiting factors separately.

Technology may later turn required mitigations into infrastructure/life-support costs and risks, but the species evaluator itself does not grant free technological solutions.

## Initial mechanical proving-ground species

The current branch contains four prototype definitions used to exercise substantially different mechanics:

- `terran_baseline` — water/carbon terrestrial baseline near Earth-like gravity, temperature, and pressure;
- `pelagic_high_pressure` — water/carbon aquatic biology requiring immersion and substantially higher pressure;
- `compact_high_gravity` — water/carbon terrestrial physiology centered on much stronger gravity and denser conditions;
- `cryogenic_hydrocarbon` — carbon biology using hydrocarbon chemistry in a very cold reducing environment.

These are **mechanical proving grounds, not final lore commitments or the final playable-species roster**. Names, biology, presentation, home systems, cultures, and exact starting conditions can be refined while preserving the underlying mechanics.

Synthetic/post-biological life is supported by the type system but is intentionally not treated as merely another biological reskin; its population, maintenance, energy, reproduction, and environmental model will need explicit design before becoming a finished playable species.

## Civilization personality remains separate

The existing `CivilizationTraits` values such as aggression, territoriality, greed, scientific curiosity, risk tolerance, survival priority, and honor-bound behavior describe civilization/AI behavior.

They are not inherited biological race statistics.

A single species should be capable of producing civilizations with different cultures, governments, histories, research priorities, diplomacy, and strategic behavior. Likewise a multi-species civilization may eventually contain populations with different biology under one political state.

New seeded civilizations now receive a stable founding `SpeciesId` through `SpeciesAssignmentPolicy`. The assignment is based only on campaign seed and civilization ID, not on civilization archetype or personality. This avoids making, for example, a militarist template biologically high-gravity by definition.

## Adaptive Research boundary

The Adaptive Research workstream owns its research possibility graph, applicability schema, capability schema, and research data.

Species mechanics owns biological facts.

Research may map those facts into applicability conditions such as biological fabrication compatibility, environmental engineering needs, unusual solvent requirements, or other legitimate research constraints. Species code must not create a second research system or edit canonical research schema independently while that workstream owns it.

## Colony/population boundary

The current `ColonyState` has one aggregate population value and no species cohorts yet.

Long-term population representation should remain aggregated and scalable. A future colony may contain a bounded set of meaningful population cohorts distinguished only where gameplay requires it, such as species, adaptation, culture, or role.

Species mechanics should provide suitability/physiology contracts; the colony/population system should decide population growth, migration, demographic composition, housing, and colony-level consequences.

A civilization-level `SpeciesId` is currently the founding-species identity needed by the prototype. It is **not** intended to imply that a mature civilization can only ever contain one species. Multi-species population composition belongs in bounded colony/population cohorts later.

## Logistics, life support, shipbuilding, and combat boundaries

Later consumers can derive real consequences from species biology, for example:

- habitat pressure/temperature/atmosphere requirements;
- food/chemical feedstock and life-support demand;
- water or immersion mass/volume requirements;
- radiation shielding requirement;
- gravity/rotation/acceleration management;
- evacuation and transport constraints;
- environmental exposure risk;
- ground-force performance in a specific local environment.

Those systems remain owned by their respective workstreams. Species mechanics exposes physical inputs and assessments rather than implementing a second logistics, ship, or combat engine.

## Persistence and migration

The intended long-campaign save representation is compact:

- static species definitions live in shared data/code and are referenced by stable ID;
- civilization/population state stores species IDs only where needed;
- populations persist compact adaptation state once population cohorts exist;
- reconstructible environmental assessment results are not serialized as permanent cache data.

The shared `integration` branch now contains shipbuilding save format v7. This species branch deliberately builds on that integrated v7 baseline and introduces **candidate save format v8** for civilization founding-species identity.

Save v8 behavior:

- `CivilizationSaveDto.SpeciesId` persists each civilization's known species definition ID;
- saving rejects an unknown species ID rather than writing an invalid biological reference;
- loading a v8 save rejects unknown species IDs rather than silently substituting a different species;
- v1–v7 saves migrate deterministically by assigning species from campaign seed + civilization ID;
- the migration intentionally does not use civilization archetype, aggression, greed, scientific curiosity, government, or other cultural/AI properties;
- shipyard v7 state remains intact and unchanged in the v8 migration.

`PopulationAdaptationState` is not persisted yet because the current colony model has no species population cohorts to own it. Adding adaptation to saves before a real owning population exists would create ambiguous state and duplicated sources of truth.

## Scalability rules

- Never create one simulation object per individual person.
- Do not duplicate full species definitions into every population.
- Environmental assessments are deterministic and reconstructible.
- Population adaptation state must remain compact and bounded.
- Future cohort splitting must have merge/cleanup rules so thousands of campaign years cannot create infinite micro-cohorts.
- AI should consume derived summaries relevant to its legitimate knowledge rather than recomputing every species/environment combination every frame.

## Current implementation status

Implemented on `work/species-race-mechanics`:

- species definition and validation model;
- physical habitat input model;
- compact population adaptation state;
- deterministic environment/habitability evaluator;
- four mechanically distinct prototype species;
- deterministic species assignment independent from civilization personality;
- civilization founding `SpeciesId` runtime state;
- candidate save format v8 with species-ID round trip and v1–v7 migration;
- executable simulation checks for environmental mechanics, species assignment, and save migration.

Still intentionally deferred:

- detailed planetary environment data beyond the prototype `HasHabitableWorld` flag;
- multi-species colony/population cohorts;
- persisted population adaptation state;
- population growth/economy consequences driven by species biology;
- player-facing species selection/customization UI;
- final species lore, art, names, and roster.
