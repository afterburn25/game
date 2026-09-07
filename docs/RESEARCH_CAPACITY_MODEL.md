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

Research Pressure represents a recognized need, opportunity, hazard, strategic gap, or compelling body of evidence.

Examples:

- high-gravity health burden
- food shortage
- repeated reactor failure
- enemy speed superiority
- missile threat
- supply-line overstretch
- observed foreign technology
- unexplained physical anomaly

Pressure is not spent and does not directly add RP.

Some technologies require a pressure threshold before they become **Investigable**. This is what causes the visible research tree to grow in response to history.

A technology may have no pressure requirement if ordinary/basic science can expose it.

Pressure values are normalized and bounded so they remain cheap to simulate. They can rise from real conditions and decay when those conditions disappear.

Do not expose pressure meters for fields the civilization does not yet know exist; doing so would reveal hidden future branches.

### 3. Research Lab Capacity

Every directed research project requires a minimum number of Effective Research Labs.

A civilization may understand a technology and have enough pressure for it to become investigable but still be physically unable to begin the project because it lacks sufficient research infrastructure.

Early facilities can represent one effective lab each. Later research institutes, orbital laboratories, machine-science arrays, biological research organisms, alien facilities, or other species-specific equivalents can contribute multiple effective lab units without requiring thousands of individually simulated buildings.

Lab allocations to directed projects are exclusive.

## Early-game simplicity

A human-like 2050 civilization begins with a simple research interface:

- current RP/year
- total effective labs
- one major Directed Research project
- minimum/recommended labs for that project
- RP required
- estimated completion date

This does **not** mean every scientist in the civilization is researching one subject.

Unassigned laboratories continue diffuse work such as:

- basic science
- field competence
- hypothesis generation
- observation/evidence analysis
- low-intensity exploratory research

The player initially directs only one major civilization-scale priority program.

## Parallel research progression

As scientific institutions, communications, automation, and administrative coordination improve, the civilization becomes capable of directing several major programs simultaneously.

Working progression:

1. **Single Priority Program** — 1 directed project.
2. **Coordinated Research Networks** — 2 directed projects.
3. **Distributed Scientific Portfolios** — up to 4 directed projects.
4. **Autonomous Research Portfolios** — no arbitrary research-slot ceiling; physical lab capacity becomes the practical concurrency limit.

The exact technologies/institutional requirements that create these stages remain balance/content decisions, but the architecture must support them.

## Example

A civilization owns 8 effective labs and has unlocked Coordinated Research Networks.

It can run:

- Advanced Fusion Propulsion — 3 labs
- High-Gravity Cardiovascular Medicine — 5 labs

at the same time.

If it instead owns only 6 labs, both projects cannot run simultaneously at those minimum allocations even though its institutional system supports two concurrent programs.

Thus parallel research requires **both** institutional coordination and physical research infrastructure.

## Node requirements

A research node may therefore require all of the following:

- prerequisite knowledge/capability
- evidence, where relevant
- species/biology/environment applicability
- one or more Research Pressure thresholds
- minimum Effective Research Labs
- Research Points to complete the program
- special facilities/materials where appropriate

This produces an evolving visible tree without creating a separate handcrafted tech tree for every species.

## Natural catch-up and complacency

A dominant military civilization receives no artificial research penalty.

Instead, its military Research Pressure can decline naturally when:

- current designs keep winning
- no credible rival is visible
- doctrine appears proven
- political funding moves elsewhere
- the civilization becomes complacent

A weaker rival can simultaneously gain strong pressure by observing superior ships, losing battles, analyzing wreckage, or facing an existential threat.

When the dominant civilization detects genuine rival progress, military pressure rises again.

Culture/government can alter these responses. A paranoid or innovation-focused civilization may continue heavy research even while dominant.

## Performance/scalability

The research economy must remain cheap at late-game scale.

- The universal possibility catalog is static data.
- Each civilization stores only known/mature IDs, visible candidates, active programs, compact pressure values, field competence, and evidence.
- Research candidates are reevaluated when relevant events occur, not by scanning the entire graph every simulation tick.
- Late-game physical research infrastructure can be aggregated into effective-lab capacity rather than individual active building objects.
- Archived mature research does not need full active simulation state.

Machine-readable companion data is stored in:

- `data/research/v1/research_economy.json`
- `data/research/v1/research_capacity.json`

These seed values are architectural/balancing starting points, not final commercial balance numbers.
