# Research Competence, Institutions & Tacit Knowledge

This document defines how Stellar Continuum represents scientific expertise after a research possibility becomes known.

The goal is to make technological history matter without creating dozens of arbitrary `+10% research` modifiers.

A civilization that has spent centuries building fusion reactors, operating orbital laboratories, studying alien materials, or maintaining biological shipyards should become genuinely better at related work. A civilization that possesses ancient blueprints but lost the institutions and specialists that once understood them should retain the knowledge while suffering reduced active readiness.

## Four distinct things must not be collapsed into one number

### 1. Research Points

RP is scientific work produced by assigned Effective Research Labs.

### 2. Research Pressure

Pressure is recognized need/opportunity/evidence. It can expose or gate selected possibilities but does not make labs work faster.

### 3. Field competence

Competence is the civilization's active ability to understand, experimentally investigate, and engineer within a knowledge field.

### 4. Research infrastructure / tacit expertise

Institutions, instruments, protocols, tooling, expert cohorts, and training pipelines determine whether the civilization can actually perform difficult work efficiently.

These concepts interact but are never interchangeable.

## Knowledge fields

Every normal research node already carries `knowledge_fields`.

The canonical public field catalog is `data/research/v1/knowledge_fields.json`.

Current fields include physical science, engineering, life science, computing, social/institutional science, xenoscience, research infrastructure, and megastructure engineering.

Fields are not technology trees and do not unlock technologies merely because their competence becomes high. They are indexes describing what expertise a project actually uses.

## Three competence dimensions

Each civilization can maintain sparse competence state for relevant fields across three components.

### Theoretical competence

Ability to:

- understand models and equations
- interpret observations
- criticize hypotheses
- connect ideas
- predict behavior

This is generally the easiest component to preserve through archives and education.

### Experimental competence

Ability to:

- design useful experiments
- operate specialized instruments
- calibrate measurements
- identify systematic error
- reproduce results
- recognize anomalous behavior

This requires active laboratories and practical experience.

### Engineering competence

Ability to:

- build reliable implementations
- turn prototypes into production systems
- maintain them
- solve manufacturing problems
- understand failure modes
- train technicians/operators

This can atrophy considerably if a civilization stops building or operating the technology even while the theory remains perfectly documented.

## Competence is not universal technological level

A value near 100 means the field is currently one of that civilization's strongest active traditions. It does **not** mean the civilization has discovered every possible technology in that field.

The scientific frontier keeps moving.

A civilization may have extraordinary physics competence and still have no evidence that a particular hidden phenomenon exists.

## How competence grows

Competence should grow from actual activity rather than purchasing a permanent bonus.

Examples:

- basic science and mathematical work -> theoretical competence
- experiments and observations -> experimental competence
- prototypes and manufacturing -> engineering competence
- failed tests -> experimental/engineering knowledge about what does not work
- disproven hypotheses -> theoretical/experimental competence
- operating mature systems -> engineering practice
- reverse engineering -> relevant theory/experiment/engineering competence depending on what was learned
- foreign scientific records -> primarily theoretical competence
- foreign expert teams/tooling -> experimental and engineering competence

Updates are event-driven or periodically aggregated. We do not run one competence calculation per scientist or per simulation frame.

## Related-field transfer

Fields have explicit related-field relationships.

Working in one field can produce **limited** spillover into related fields, but it is much smaller than direct practice.

For example:

- materials work can help manufacturing
- mathematics can help physics/computing/economics
- biology can help medicine/ecology/biotechnology

This avoids absurd isolation between disciplines without allowing expertise in one area to become universal scientific mastery.

The seed transfer rate is deliberately small and remains balance data.

## Active competence can atrophy; historical knowledge does not vanish

A civilization can know how a technology works but lose active institutional ability to extend or reproduce it efficiently.

Theoretical competence generally decays slowly because publications and education preserve it.

Experimental competence can decline when laboratories, instruments, or active programs disappear.

Engineering competence can decline faster when factories close, specialists retire, training ends, or nobody operates the technology for generations.

The floor depends on things such as:

- archives
- institutional memory
- training pipelines
- surviving facilities
- expert cohorts

Rebuilding dormant expertise should therefore be much easier than discovering the field from nothing if good records survive.

This distinction becomes especially important in very long campaigns, civilizational collapse, distant successor states, abandoned colonies, and return-to-old-galaxy gameplay.

## Stage-specific competence

Different research stages care about different expertise.

Seed weighting:

- **Experimental:** theory matters, but experimental competence dominates.
- **Demonstrated:** experimentation still dominates while engineering becomes more important.
- **Engineering:** engineering competence dominates because the question is no longer merely whether the effect exists, but whether it can become reliable technology.

For multidisciplinary projects, Stellar Continuum uses a bottleneck-sensitive score. The weakest required field matters substantially so excellence in one discipline cannot completely hide ignorance in another.

## Specialized research institutions

`data/research/v1/research_facility_model.json` defines the **research-facing** capabilities of institutions.

Construction/economy branches can later decide how the physical facilities are built and paid for. Adaptive Research owns what scientific capability the project requires.

Examples include:

- general research laboratories
- precision metrology centers
- biological systems institutes
- materials prototyping centers
- orbital microgravity laboratories
- high-energy research complexes
- deep-space observatories
- gravitational observatories
- xenoscience containment institutes
- simulation engineering centers
- large-scale engineering test centers
- planetary environment research centers
- autonomous experiment arrays
- frontier research campuses
- machine discovery laboratories

Facilities provide **eligible Effective Research Lab capacity and capabilities**, not arbitrary percentage bonuses.

## Hard experimental requirements

Most ordinary research can use suitable general labs.

Some projects genuinely need specialized physical conditions.

Examples in the seed model:

- Prototype Warp experimental stage: high-energy + field-physics + precision measurement capability
- Prototype Warp engineering: large-scale prototyping
- Wormhole Stabilization: high-energy/field/gravity experiments plus large-scale prototyping
- Living Hull: biological plus materials facilities
- Whole-Mind Emulation: advanced computation and precision measurement
- hazardous foreign research: xenoscience containment
- planetary terraforming research: environment simulation and ecosystem experimentation

If the required facility does not exist, the stage can be blocked.

The UI should explain what is missing rather than silently applying a giant research penalty.

## Tacit knowledge

Not all technology fits into a blueprint.

`data/research/v1/tacit_knowledge_model.json` defines knowledge assets such as:

- codified records
- experimental datasets
- experimental protocols
- intact prototypes
- manufacturing tooling/process chains
- expert cohorts
- operating institutions
- training pipelines

This is especially important for alien technology.

### Example

Humanity captures a functioning alien shipyard.

It may possess:

- the factory
- several intact components
- partial technical records
- a population of alien engineers

Humanity may be able to **operate** parts of that facility while still being unable to reproduce it.

If the experts leave or die before their knowledge is codified and local specialists are trained, some capability can be lost.

Over time the knowledge can progress through:

**Access -> Interpreted -> Codified -> Trained -> Native Practice**

At Native Practice, the civilization no longer depends on the original foreign asset to maintain the competence.

## Expert cohorts are aggregated

We do not simulate each scientist individually.

A strategically meaningful expert cohort is an aggregate representing a body of difficult-to-replace expertise.

This keeps the system scalable while still allowing:

- scientific migration
- recruitment
- defections
- occupation/capture
- joint research
- technology licensing
- specialist loss
- training pipelines

Other systems own the political/economic consequences of acquiring those people. Adaptive Research only consumes the resulting knowledge asset.

## Project readiness

Stellar Continuum uses one derived **Project Readiness** value instead of stacking independent research bonuses.

Applicable inputs are:

- field competence
- facility readiness
- evidence readiness
- tacit expertise

If an input genuinely does not matter to that project, it is omitted and the remaining weights are renormalized.

For example, a normal domestic materials extension may not need a foreign-evidence component at all.

A difficult alien reverse-engineering project may need all four.

## Base RP vs readiness

Base RP represents the inherent amount of work in the project:

`base RP = complexity base × (1 + 0.08 × graph depth)`

The seed formula will be tuned later, but the architecture matters:

**Civilization history does not multiply total project cost with a stack of bonuses. It changes the efficiency with which assigned labs perform the work.**

Progress is approximately:

`assigned lab RP after lab-scaling × readiness efficiency`

Readiness efficiency is deliberately bounded.

Current seed bands range from 0.35 for severe competence/infrastructure gaps to 1.15 for exceptional readiness.

The upper bound is intentionally modest so an expert civilization cannot instantly complete frontier science merely through accumulated modifiers.

## Pressure is not a speed multiplier

A civilization being desperate to defeat an enemy can:

- expose relevant branches
- create political willingness to assign more labs/resources
- generate observations/evidence
- justify crash programs

But Research Pressure itself does **not** directly make scientists 50% smarter.

This preserves the natural catch-up model without hidden rubber-banding.

## Missing resources and facilities

A physically missing requirement should normally become a clear project constraint.

Examples:

- no high-energy test facility
- no compatible biological sample
- no alien manufacturing process
- no material required for prototype construction

Do not convert these into enormous opaque RP multipliers.

The project may pause at the relevant stage until the resource/facility exists.

## Early-game UI remains simple

The 2050 research UI does not need to display every competence component immediately.

Early presentation can show:

- RP/year
- total/assigned labs
- available project
- required labs
- estimated completion
- simple readiness label
- clear missing requirement

Advanced details can expose:

- theory/experiment/engineering competence
- contributing institutions
- evidence quality
- expert/tacit assets
- why the estimate changed

## Performance

Field state is sparse.

A civilization stores only fields it actually knows/uses meaningfully.

Project readiness is recalculated when relevant state changes, such as:

- project/stage change
- lab reassignment
- competence band change
- facility access change
- meaningful new evidence
- meaningful tacit-knowledge change

It is cached for active projects and is **not recomputed every simulation tick**.

## Canonical files

- `data/research/v1/knowledge_fields.json`
- `data/research/v1/research_competence_model.json`
- `data/research/v1/research_facility_model.json`
- `data/research/v1/tacit_knowledge_model.json`
- `data/research/v1/project_readiness_model.json`
- `scripts/validate_research_competence.py`

These work with the earlier research economy, emergence, capability, and maturation models rather than replacing them.
