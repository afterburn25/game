# Species and Race Mechanics

This document defines the public architecture for biological species mechanics in **Stellar Continuum**.

The player may casually think of this as the game's "race system," but the simulation deliberately separates several layers that many strategy games collapse together:

1. **species biology** — inherited physiology, chemistry, life history and environmental requirements;
2. **population adaptation** — acclimatization and longer-lived changes affecting a particular population;
3. **population/demography** — actual cohort size, growth, mortality, migration and composition;
4. **civilization traits/culture** — learned political, social, strategic and institutional behavior;
5. **technology/capabilities** — ways a civilization compensates for or exploits biological/environmental conditions.

These layers must remain separate enough that biology does not automatically dictate culture, politics, morality or intelligence.

## Core rule: contextual consequences, not arbitrary racial bonuses

A species does not receive a generic permanent modifier such as `+10% combat`, `+15% science`, or `-10% diplomacy` merely because of its species identity.

Advantages and disadvantages should arise from physical circumstances wherever practical.

Examples:

- A population evolved for high gravity may function naturally on a high-gravity world while another species needs substantial gravity mitigation.
- The same high-gravity species can be outside its own comfortable range on a low-gravity colony.
- An aquatic species may require immersed habitats even when temperature and atmospheric chemistry are otherwise compatible.
- A cryogenic hydrocarbon species can find a world lethal that a water/oxygen species considers ideal, and vice versa.
- Radiation tolerance changes how much shielding is required in a hazardous environment; it is not a universal military bonus.
- Body mass and metabolic demand create physical transport/life-support requirements rather than an abstract economy modifier.
- Lifespan, maturity and generation length constrain demographics and adaptation tempo rather than granting an automatic research or leader bonus.

This keeps species differences mechanically meaningful without turning biology into stereotypes about aggression, greed, diplomacy, scientific ability or political organization.

## Layer 1: immutable species definition

The static `SpeciesDefinition` describes inherited baseline biological facts.

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
- preferred/comfortable/survivable gravity range;
- preferred/comfortable/survivable temperature range;
- preferred/comfortable/survivable pressure range;
- breathable atmosphere classes;
- compatible biological solvents;
- immersion requirement;
- unprotected vacuum capability when biologically appropriate;
- biological life-history profile.

Species definitions are static shared game data. They must not be copied into every population or save record.

## Layer 2: biological life history

`SpeciesLifeHistory` records biological demographic constraints separately from actual population behavior.

It currently includes:

- reproductive mode;
- reproductive maturity age;
- typical offspring per reproductive event;
- minimum biological interval between events;
- dependent-development duration;
- reproductive span;
- baseline generation length.

`SpeciesDemographicEnvelopeEvaluator` derives quantities such as generations per century and an upper biological reproductive envelope.

These values are **not population growth rates**. Actual growth remains the responsibility of the population/colony simulation and must later account for mortality, sex/reproductive-role structure where relevant, health, food, housing, policy, environment, war, migration and social choices.

The demographic envelope therefore says what biology can physically constrain; it does not say what a civilization will choose or achieve.

## Layer 3: habitat/environment description

`HabitatEnvironment` is the physical input used when asking whether a species can live or operate somewhere.

The current foundation represents:

- gravity in Earth gravities (`g`);
- temperature in kelvin;
- pressure in kilopascals;
- atmosphere class;
- available solvent;
- normalized radiation hazard;
- whether the population is immersed.

This is intentionally an interface-sized environment model. A planet/environment workstream should eventually provide real physical values. Species code does not own planet generation and must not create a second competing planet model.

## Layer 4: derived environmental assessment

`SpeciesEnvironmentEvaluator` deterministically compares a species/population with a physical habitat and returns `SpeciesEnvironmentAssessment`.

Outputs include:

- natural habitability;
- unprotected operational capacity;
- gravity suitability;
- temperature suitability;
- pressure suitability;
- atmosphere suitability;
- solvent suitability;
- immersion suitability;
- radiation suitability;
- strongest limiting factor;
- gravity-mitigation requirement;
- thermal-control requirement;
- pressure-control requirement;
- sealed-habitat requirement;
- artificial-biosphere/immersion requirement;
- radiation-shielding requirement;
- derived health stress.

**Natural habitability is limited by the worst essential physical requirement.** A population cannot average its way out of an unbreathable atmosphere or incompatible solvent because its temperature is comfortable.

**Unprotected operational capacity uses a geometric mean** for a smooth general-purpose physical summary while retaining hard limiting factors separately.

Technology may later satisfy mitigation requirements through real infrastructure, equipment and logistics. The evaluator does not grant those solutions for free.

## Layer 5: population-scoped adaptation

`PopulationAdaptationState` belongs to a population, not to the immutable species definition.

It can currently represent bounded changes to:

- gravity preference and tolerance;
- temperature preference and tolerance;
- pressure preference and tolerance;
- radiation tolerance;
- short/medium-term acclimatization.

`PopulationAdaptationProgression` provides low-frequency deterministic progression for a cohort.

### Acclimatization

Acclimatization is relatively fast and can move toward the conditions a population is repeatedly experiencing. It is reversible and never changes the base species definition.

It can only respond to a physically nonzero environment. It cannot make incompatible atmosphere, solvent or required-immersion chemistry disappear.

### Long-term adaptation

Longer-lived preference/tolerance changes use **local generations**, not arbitrary calendar bonuses. The progression tracks residence years, generations spent in the environment and the locally born fraction of the cohort.

Natural long-term adaptation:

- requires the habitat to retain nonzero biological compatibility;
- requires minimum natural habitability rather than allowing passive evolution through a lethal barrier;
- moves slowly over generations;
- is capped relative to the species' original survivable envelope;
- may shift preferred gravity/temperature/pressure and modestly widen tolerance;
- can improve radiation tolerance within bounded limits;
- never rewrites atmosphere/solvent/immersion chemistry.

A human-like population therefore cannot simply remain on a chemically incompatible methane world for centuries and eventually become methane-breathing through this passive system. Major biochemical transformation belongs to explicit future genetic/biotechnological or divergent-speciation systems.

This establishes the intended adaptation layers:

- individual acclimatization;
- developmental adaptation among locally born generations;
- multigenerational natural adaptation;
- explicit medical/genetic/cybernetic intervention where technology permits.

## Layer 6: bounded population cohorts

`SpeciesPopulationCohort` is the species-side aggregated state for one meaningful species/adaptation band.

It contains:

- species ID;
- population in millions;
- compact adaptation state;
- residence years;
- generations in the environment;
- locally born fraction.

It deliberately does **not** own:

- population growth;
- mortality;
- migration;
- culture;
- employment/class;
- politics;
- housing;
- colony economy.

Those remain population/colony responsibilities.

### Cohort-bounding rule

Adaptation must not create unlimited microscopic sub-populations over a thousand-year campaign.

`PopulationCohortReducer` therefore enforces bounded adaptation detail per species:

- default maximum detailed adaptation cohorts per species: **4**;
- hard supported maximum: **8**;
- only cohorts of the **same species** may be merged;
- different species are never blended into a synthetic average biology;
- the closest adaptation states are merged first using a species-normalized deterministic distance;
- population totals are conserved exactly aside from ordinary floating-point representation;
- adaptation/residence summaries are population-weighted;
- input enumeration order does not change the reduced result.

The bound is per species because preserving the existence of another species is more important than preserving many tiny adaptation bands within one species.

A future colony/population owner may impose an additional colony-wide bound if campaigns with many species prove to require one, but it must not silently erase strategically meaningful minority species.

## Physical requirement summaries for other systems

`SpeciesPopulationRequirementsEvaluator` translates a population cohort into read-only physical quantities suitable for other workstreams.

Current outputs include:

- population size;
- reference metabolic demand;
- aggregate adult biomass;
- typical adult mass;
- baseline lifespan;
- baseline generation length;
- generations per century;
- full environmental assessment;
- number of environmental mitigation categories currently required.

These are physical inputs, not finished gameplay bonuses.

Examples of intended consumers:

- economy/logistics can translate metabolic demand and habitat requirements into food/feedstock, water, gas, energy, cargo and support burdens;
- shipbuilding/habitation can use biomass, atmosphere, pressure, immersion and gravity requirements when designing crew habitats;
- medicine can use environment stress and life history;
- combat can consume local environmental suitability and body/physiology facts rather than a universal species combat multiplier;
- research can map legitimate biological facts into applicability/compatibility conditions.

The consuming subsystem remains responsible for its own rules and costs.

## Initial mechanical proving-ground species

The branch currently contains four prototype definitions chosen to exercise strongly different mechanics:

- `terran_baseline` — water/carbon terrestrial baseline near Earth-like conditions;
- `pelagic_high_pressure` — water/carbon aquatic biology requiring immersion and substantially higher pressure;
- `compact_high_gravity` — water/carbon terrestrial physiology centered on strong gravity and denser conditions;
- `cryogenic_hydrocarbon` — carbon biology using hydrocarbon chemistry in a very cold reducing environment.

They now differ in environment, physiology, metabolic demand and life history.

These are **mechanical proving grounds, not final lore commitments or the final playable-species roster**. Names, presentation, home systems, cultures and exact values may be refined while preserving the architecture.

Synthetic/post-biological life is represented by the type system but is intentionally not treated as merely another biological reskin. Maintenance, energy, fabrication/reproduction, consciousness continuity and environmental requirements need explicit design before a synthetic species becomes a finished playable start.

## Civilization personality remains separate

Existing `CivilizationTraits` such as aggression, territoriality, greed, scientific curiosity, risk tolerance, survival priority and honor-bound behavior describe civilization/AI behavior.

They are **not inherited biological race statistics**.

A single species must be able to produce civilizations with different cultures, governments, histories, research priorities, diplomacy and strategic behavior. A mature civilization may also contain multiple species.

New seeded civilizations receive a stable founding `SpeciesId` through `SpeciesAssignmentPolicy`. Assignment is deterministic from campaign seed + civilization ID and deliberately does not use civilization archetype/personality.

## Adaptive Research boundary

Adaptive Research owns its possibility graph, applicability schema, capability schema, competence, foreign-technology transfer and research UI data.

Species mechanics owns biological facts.

Research may consume those facts for legitimate applicability or compatibility questions. Species code must not create a second research system or independently rewrite research-owned schemas.

## Colony/population boundary

The current gameplay `ColonyState` still has a single aggregate population value. The species branch now defines the species-side cohort/adaptation type but intentionally has **not** made it the colony system's authoritative population collection yet.

That integration should happen when the population/colony owner is ready to replace the single scalar with a bounded composition model.

The population system should decide:

- births/deaths;
- migration;
- demographic composition;
- cohort creation/splitting;
- housing and health consequences;
- colony-level population policy;
- when species/adaptation distinctions are strategically meaningful enough to retain.

The species system provides validated cohort state, deterministic reduction, adaptation progression and physical assessment.

## Logistics, life support, shipbuilding and combat boundaries

Later consumers can derive real consequences from species biology, including:

- habitat pressure/temperature/atmosphere requirements;
- food/chemical feedstock and life-support demand;
- water/immersion mass and volume;
- radiation shielding;
- gravity/rotation/acceleration management;
- evacuation and transport burden;
- environmental exposure risk;
- ground-force operation in a specific local environment;
- equipment/habitat compatibility.

Those systems remain owned by their respective workstreams. Species mechanics exposes physical inputs rather than implementing a duplicate economy, logistics, ship or combat engine.

## Persistence and migration

Static species definitions are shared code/data and are referenced by stable ID.

The branch builds on shipbuilding save format v7 and introduces **candidate save format v8** for civilization founding-species identity.

Save v8 behavior:

- `CivilizationSaveDto.SpeciesId` persists each civilization's known species definition ID;
- saving rejects unknown species IDs;
- loading v8 rejects unknown IDs instead of silently substituting another biology;
- v1–v7 saves migrate deterministically from campaign seed + civilization ID;
- migration does not infer biology from civilization personality/archetype;
- shipyard v7 state remains intact.

Core integration validation now exercises both a true v6→current migration and a v7→v8 species migration.

`SpeciesPopulationCohort` / `PopulationAdaptationState` are **not persisted yet** because current colony state has no authoritative cohort collection. Persisting the same adaptation in an unattached side table would create duplicated or orphaned sources of truth. When colonies gain cohort ownership, adaptation should be persisted there using a deliberate next migration if required.

## Scalability rules

- Never simulate one object per individual person.
- Do not duplicate full species definitions into every population.
- Environmental assessments are deterministic and reconstructible.
- Do not serialize reconstructible assessment caches.
- Population adaptation state remains compact.
- Detailed adaptation cohorts are bounded and deterministically reducible.
- Different species must not be erased merely to satisfy an adaptation-detail bound.
- Long-term adaptation runs on demographic/maintenance cadence, not every frame.
- AI should consume cached/derived summaries relevant to legitimate knowledge rather than recomputing every species/environment combination every tick.

## Current implementation status

Implemented on `work/species-race-mechanics`:

- immutable species definition/validation model;
- four mechanically distinct prototype species;
- physiology and environmental tolerance bands;
- biological life-history profiles and demographic envelopes;
- physical habitat input model;
- deterministic environment/habitability evaluator;
- compact population adaptation state;
- low-frequency multigenerational adaptation progression;
- bounded `SpeciesPopulationCohort` state;
- deterministic same-species cohort reduction with population conservation;
- physical population-requirements summary for consuming systems;
- deterministic civilization species assignment independent from personality;
- civilization founding `SpeciesId` runtime state;
- candidate save format v8 with species-ID round trip and v1–v7 migration;
- executable checks for environment mechanics, bounded cohorts, adaptation, life history, physical requirements, assignment and persistence;
- updated core integration save/migration validation.

Still intentionally deferred:

- detailed planet/environment state beyond the prototype system-level `HasHabitableWorld` flag;
- authoritative multi-species cohort ownership inside `ColonyState`;
- persisted cohort/adaptation state;
- actual species-driven population growth/mortality/migration;
- genetic compatibility, hybridization and deliberate biological redesign;
- full morphology/ergonomics/equipment compatibility;
- species-aware life-support/logistics cost calculation inside the economy/logistics system;
- species-aware local ground-operation model inside combat;
- player-facing species selection/customization UI;
- final species lore, art, names and roster.
