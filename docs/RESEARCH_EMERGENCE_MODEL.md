# Research Emergence Model

This document defines how Stellar Continuum turns the static Technology Possibility Graph into a different evolving visible research tree for each civilization.

## Core idea

The 330-node public possibility graph is **not** evaluated as a full tree every tick and is never shown in full to the player.

A civilization's visible research horizon changes when real conditions change:

- a problem becomes serious enough to create Research Pressure
- new evidence is legitimately obtained
- prerequisite science matures
- biological/civilizational applicability changes
- basic science produces a new hypothesis
- foreign observations prove an unfamiliar capability is possible

Only candidate nodes connected to the changed condition are reconsidered.

## Emergence pipeline

1. A simulation subsystem updates one of its own normalized condition metrics or emits a meaningful event.
2. Only the affected Research Pressure records are updated.
3. A pressure band crossing, evidence acquisition, prerequisite completion, trait change, or low-frequency basic-science review wakes a small candidate set through static indexes.
4. Candidate prerequisites, applicability, evidence, explicit pressure requirements, and current knowledge are evaluated.
5. A successful candidate can become **Rumored**, **Hypothesized**, or **Investigable** depending on evidence/understanding.
6. Minimum Effective Research Labs and directed-program capacity matter only when the civilization attempts to begin a directed project.
7. RP advances the active project through experimentation, demonstration, engineering, and maturity.

Unknown failed candidates create no player notification and do not reveal future branches.

## Research Pressure dynamics

Research Pressure is a sparse 0–100 civilization state.

The public catalog defines 59 pressure types and one ordinary generation/decay rule for each in `pressure_dynamics.json`.

### Sources

Two source forms exist:

- **metric signals** — normalized conditions maintained by the subsystem that owns the underlying simulation, such as food deficit, armor penetration rate, grid instability, or observed enemy speed advantage
- **event signals** — bounded urgency pulses from meaningful events such as a fleet defeat, famine, life-support failure, alien signal detection, or major archive loss

The research subsystem does not calculate the underlying economy, battle, health, ecology, or diplomatic metric. It consumes normalized signals from those systems.

### Seed update model

For implementation planning, a simple robust first model is:

- each relevant subsystem reports severity in the range 0–1
- the strongest current sustaining metric establishes a target pressure of approximately `severity × 100`
- pressure approaches that target during scheduled reviews
- meaningful events add a bounded pulse scaled by event severity
- when sustaining conditions disappear, pressure decays by the per-pressure `decay_per_year` rate
- selected historically significant knowledge pressures can retain a small configured `memory_floor`
- final pressure is always clamped to 0–100

Exact approach/pulse coefficients are balancing data, not a reason to alter the architecture.

### Performance

Do not update all 59 pressures every simulation tick.

- event-based pressures update when the owning subsystem emits an event
- continuous-condition pressures are reviewed quarterly/yearly or when the source metric crosses a meaningful band
- zero/irrelevant pressures need not have active records
- pressure -> candidate-node indexes are built once from static data

This keeps pressure cost proportional to actual changing conditions rather than `civilizations × pressures × technologies × ticks`.

## No hidden rubber-banding

Pressure is never generated because a civilization is “behind” in a global ranking.

For example, **Enemy Mobility Superiority** can rise only from legitimately observed facts such as:

- enemy strategic speed observed above current own capability
- failed interceptions attributable to enemy mobility
- battle/operation outcomes where superior transit speed was causal

An unseen enemy's authoritative statistics cannot create exact pressure.

This allows natural catch-up while preserving fair AI and fog of war.

## Applicability traits

`applicability_traits.json` defines stable public trait IDs used by research nodes.

Traits are **not species IDs**. They describe facts that make a research path meaningful or possible.

Current public traits include:

- `metabolic_biology`
- `gravity_sensitive_biology`
- `heritable_genetics`
- `metabolic_suppression_possible`
- `machine_cognition_present`
- `biological_fabrication_possible`

### Trait scopes

**Population/species traits** apply to the population for which a technology is being developed.

A multi-species civilization containing one high-gravity species does not automatically make every citizen high-gravity adapted. The civilization can research/use relevant medicine for the compatible population and may later adapt derived technologies to others.

**Civilization traits** represent scientific/industrial capabilities or institutional facts that can be acquired through history. Examples:

- a biological civilization develops genuine machine cognition and gains `machine_cognition_present`
- biotechnology matures into structural biofabrication and grants `biological_fabrication_possible`

A mutable trait change wakes only the research nodes indexed to that trait.

### Trait acquisition

The trait catalog intentionally separates **what a trait means** from **what grants it**.

Future implementation data can grant/remove mutable traits through:

- matured technologies/capabilities
- population integration
- deliberate biological modification
- synthetic-person creation
- major civilizational transformation

No calendar year grants a trait automatically.

## Evidence model

`evidence_types.json` defines stable ordinary evidence-type IDs. A campaign stores **evidence instances**, not just booleans.

An instance can preserve:

- type ID
- source entity/event
- acquisition date
- provenance
- quality
- confidence
- intactness
- custody location
- stale/superseded state

Examples include:

- alien signal
- alien biological sample
- captured foreign device
- foreign propulsion telemetry/wreckage
- foreign scientific records
- foreign manufacturing sample
- hazardous foreign device

Evidence does not grant a technology. It can prove that something exists, expose a scientific question, satisfy an evidence gate, improve research context, or provide material for reverse engineering.

Physical and informational evidence can later support trade, theft, copying, loss, sabotage, licensing, and scientific exchange.

## Evidence quality

The first implementation can treat node evidence requirements as “possess a sufficiently credible instance of this type.”

The data model already supports later thresholds on:

- confidence
- quality
- intactness
- provenance reliability

This means observing a blurry unidentified drive signature need not be equivalent to capturing an intact engine.

## Basic science

Not every branch requires pressure or foreign evidence.

Low-frequency basic-science reviews use known fields, prerequisite knowledge, research institutions, scientific curiosity, and field competence to expose plausible hypotheses.

This is how a civilization can discover something it did not know it “needed.”

The review must remain bounded:

- sample only candidates indexed to known fields/prerequisites
- do not scan the complete graph for every civilization every tick
- stronger research institutions can increase review breadth/frequency rather than generating flat magical technology bonuses

## Multi-species and hybrid civilizations

A civilization can hold several applicability contexts simultaneously.

Examples:

- biological citizens plus synthetic citizens
- several biological species with different gravity tolerances
- an aquatic minority whose environmental technology is useful only to that population initially
- alien specialists operating captured foreign manufacturing infrastructure

Research therefore needs a **target applicability context** where necessary, such as species/population/industrial lineage.

A discovery can later create an adapted/hybrid implementation usable by a broader population.

## Persistence

The static trait/evidence/pressure definitions stay in game data.

Campaign state stores only:

- sparse relevant pressure values
- evidence instances
- mutable civilization traits
- population/species trait references
- visible candidate states
- active project state
- mature node IDs
- field competence

Old evidence/discovery detail can be archived or summarized when no longer operationally relevant while preserving major historical consequences.

## Public/secret boundary

This model describes ordinary public research emergence only.

Do not put exact rare discovery triggers, rare probabilities, secret evidence types, hidden artifact chains, or secret special-AI rules in these public files.

Secret content can plug into the same runtime interfaces from a separate content source later.

## Machine-readable files

- `data/research/v1/emergence_model.json`
- `data/research/v1/applicability_traits.json`
- `data/research/v1/evidence_types.json`
- `data/research/v1/pressure_dynamics.json`
- `data/research/v1/index.json`
- `data/research/v1/research_economy.json`

All public references are validated by `scripts/validate_research_catalog.py` in CI.
