# Research Capacity Model

This document defines how Stellar Continuum turns the Adaptive Research System into a playable research economy.

## Three separate gates

Research uses three different concepts that must not be collapsed into one number.

### 1. Research Points (RP)

Research Points are the simplest and most visible early-game resource.

- Operational Research Labs generate RP.
- RP advances active directed research projects.
- A project has an RP requirement.
- More assigned laboratory capacity can accelerate progress, with diminishing returns at very large scale.
- The game should not allow a civilization to stockpile centuries of generic RP and instantly complete a newly discovered technology.

### 2. Research Pressure

Research Pressure represents recognized need, opportunity, hazard, strategic gap, or compelling evidence.

Pressure is not spent and does not directly add RP. It is bounded, can rise from real conditions, and can decay when those conditions disappear.

**Only explicitly configured technologies are hard-gated by Research Pressure.** A technology's complexity or its `pressure_affinities` never creates a pressure requirement by itself. This preserves curiosity-driven/basic science while letting need-driven branches emerge from history.

Do not expose pressure meters for fields the civilization does not yet know exist.

### 3. Effective Research Lab Capacity

Every directed research project requires a minimum number of Effective Research Labs.

A civilization may understand that a technology is possible and satisfy any pressure/evidence requirements but still be unable to start because it lacks sufficient scientific infrastructure.

An Effective Research Lab is a normalized capacity unit rather than necessarily one literal room. Early facilities may supply one unit; later institutes, orbital labs, machine-science arrays, biological research organisms, alien facilities, and regional science campuses can supply multiple units while remaining aggregate simulation objects.

Lab allocations to directed projects are exclusive.

## Early-game simplicity

A human-like 2050 civilization begins with:

- RP/year
- total effective labs
- one major Directed Research project
- minimum/recommended labs for that project
- RP required
- estimated completion date

This does **not** mean every scientist works on one subject. Unassigned laboratories continue basic science, field competence, hypothesis generation, observation/evidence analysis, and low-intensity exploratory research.

## Parallel research is itself researched

Directed-project coordination is now represented by actual nodes in the possibility graph:

1. **Single Priority Program** — starting capability; 1 directed project.
2. **Coordinated Research Networks** — research node `coordinated_research_networks`; 2 directed projects.
3. **Distributed Scientific Portfolios** — node `distributed_scientific_portfolios`; up to 4 directed projects.
4. **Autonomous Research Portfolios** — node `autonomous_research_portfolios`; no arbitrary slot ceiling, with physical lab capacity becoming the practical concurrency limit.

These nodes live in `research_infrastructure.json`. Their appearance is itself adaptive: a civilization that suffers a research bottleneck, administrative distance, or growing automation demand has stronger reason to develop them.

## Example

A civilization owns 8 effective labs and has matured **Coordinated Research Networks**.

It can assign:

- Advanced Fusion Propulsion — 3 labs
- High-Gravity Cardiovascular Adaptation — 5 labs

Both proceed simultaneously because the civilization supports two directed programs and has enough lab capacity.

If it owns only 6 labs, it cannot maintain those allocations simultaneously even though it has institutional capacity for two projects.

Parallel research therefore requires **both institutional coordination and physical scientific capacity**.

## Default lab scale

Seed defaults:

| Complexity | Minimum labs | Recommended labs | Seed base RP cost |
|---|---:|---:|---:|
| Foundation | 1 | 2 | 250 |
| Developing | 2 | 4 | 800 |
| Advanced | 4 | 8 | 2,600 |
| Frontier | 8 | 16 | 9,000 |

Individual technologies can override these values.

Assigning labs beyond the recommended amount remains possible, but extra labs have diminishing returns because coordination and specialist bottlenecks grow.

## Node requirements

A research node may require any combination of:

- prerequisite knowledge/capability
- evidence
- species/biology/environment applicability
- explicit Research Pressure thresholds
- special facilities/materials
- minimum Effective Research Labs
- available directed-program capacity
- RP completion

A node with no explicit pressure gate can still arise through basic science even if it is Advanced or Frontier complexity.

## Natural catch-up and complacency

A dominant military civilization receives no artificial research penalty. Its military urgency may fall naturally if current ships keep winning, no rival appears credible, doctrine seems proven, and political resources shift elsewhere.

A weaker rival can simultaneously gain pressure from losses, observed superior systems, wreckage, telemetry, espionage, and strategic urgency. When the leader detects credible catch-up, its own pressure can increase again.

Culture/government modifies this behavior; a paranoid or innovation-focused civilization may maintain heavy investment while dominant.

## Performance/scalability

- The possibility catalog is static game data.
- Each civilization stores only known/mature IDs, visible candidates, active programs, allocated labs, compact pressure values, field competence, and evidence.
- Candidates are reevaluated when relevant events occur, not by scanning the whole graph every tick.
- Late-game physical research infrastructure aggregates into Effective Research Lab capacity rather than thousands of active building objects.
- Archived mature research does not need full active simulation state.

Machine-readable companion data:

- `data/research/v1/research_economy.json`
- `data/research/v1/research_capacity.json`
- `data/research/v1/research_infrastructure.json`

Seed values are architectural/balancing starting points, not final commercial balance numbers.
