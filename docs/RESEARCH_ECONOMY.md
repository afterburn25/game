# Research Economy

This document defines the player-facing resource model that powers Stellar Continuum's Adaptive Research System.

The design goal is to keep early research understandable while allowing the system to become much deeper as civilizations, species, institutions, and technology diverge.

## The three research quantities

### 1. Research Points (RP)

Research Points measure scientific/engineering work applied to an active project.

Operational **Research Labs** generate RP.

Early-game rule:

> More operational labs = more research output.

The seed balance value is **100 RP per effective lab per in-game year**. This is a balancing starting point, not a locked commercial value.

RP should normally be applied directly to active projects rather than accumulated into a giant civilization-wide stockpile. This prevents a civilization from saving centuries of generic research and instantly completing a newly discovered technology.

Labs that are not assigned to a specific project still contribute at a lower-level to basic science, scientific competence, hypothesis generation, and exploratory work.

### 2. Research Pressure

Research Pressure represents recognized need, opportunity, or compelling evidence.

It is not spent like currency.

Examples:

- repeated high-gravity health problems generate High-Gravity Health Pressure
- enemy missiles generate Missile Threat Pressure
- repeated fleet losses generate Fleet Loss Pressure
- inability to reach distant systems generates Interstellar Distance Pressure
- broken supply lines generate Supply-Line Overstretch Pressure
- an observed alien propulsion system creates Foreign Technology / Enemy Mobility pressure and evidence

Certain technologies remain unavailable until relevant pressure reaches a required threshold.

A civilization therefore cannot simply browse the full universe of possible inventions and choose what it wants centuries in advance.

Pressure is normally represented on a 0–100 scale and can decay when the underlying problem disappears.

Important events/evidence can keep a field relevant even after immediate pressure falls.

### 3. Research Labs

A Research Lab is an **effective scientific-capacity unit**, not necessarily one literal room/building.

Early facilities may provide one effective lab unit each. Later research institutes, orbital laboratories, advanced AI research centers, biological research networks, or alien equivalents can provide multiple effective lab units.

Every research project has a **minimum lab requirement**.

A civilization can understand that a technology may be possible but still be unable to undertake the program because it lacks sufficient scientific infrastructure.

## No arbitrary research-slot cap

Stellar Continuum should not use a fixed rule such as:

> You may research exactly 3 technologies at once.

Instead, laboratories are physically allocated to projects.

If a civilization has 10 effective labs:

- a project requiring 4 labs can reserve 4
- another project requiring 4 can reserve another 4
- a small project requiring 2 can use the remaining 2

All three can proceed simultaneously.

If all 10 labs are reserved, another project cannot begin until capacity is freed or new labs are built.

This creates natural research concurrency as a civilization grows.

## Minimum and recommended laboratories

Seed defaults by project complexity:

| Complexity | Minimum labs | Recommended labs | Seed base RP cost |
|---|---:|---:|---:|
| Foundation | 1 | 2 | 250 |
| Developing | 2 | 4 | 800 |
| Advanced | 4 | 8 | 2,600 |
| Frontier | 8 | 16 | 9,000 |

Individual technologies can override these values.

For example, early prototype FTL is intentionally a large program and may require more labs than another technology with similar theoretical complexity.

## Assigning more laboratories

Assigning more than the minimum speeds research.

Up to the recommended number, each additional lab contributes at normal efficiency.

Beyond the recommended size, coordination overhead creates diminishing returns rather than a hard cap.

Seed model:

- up to recommended labs: 100% efficiency per lab
- recommended → 2× recommended: 35% efficiency per extra lab
- above 2× recommended: 10% efficiency per extra lab

A civilization can still create an enormous crash program if it considers the technology important enough; it is simply increasingly inefficient.

That allows realistic projects resembling major national/civilizational scientific mobilization without a game rule saying “maximum 8 labs.”

## Technology availability

A technology can require several distinct conditions simultaneously:

1. **knowledge prerequisites** — prior scientific/engineering understanding
2. **evidence** — observations, samples, foreign devices, experimental results
3. **research pressure** — sufficient recognized need/opportunity
4. **species/biology applicability** — solution must make sense for that civilization
5. **infrastructure** — required laboratories or special facilities
6. **minimum labs** — enough research capacity must be assignable to begin
7. **RP completion** — assigned labs must perform the actual research work

This means “we know this might be possible” and “we can research this now” are different states.

## Basic science exception

Not every discovery requires an immediate practical need.

Foundational science can reveal hypotheses through curiosity, experiments, observatories, theory, and unexpected results.

A basic-science path can therefore expose some possibilities without a high research-pressure threshold.

This is necessary so the system does not incorrectly imply that civilizations can only invent technologies after suffering a crisis.

## Research Pressure and natural technological catch-up

Research Pressure is one of the mechanisms that can produce natural catch-up without rubber-banding.

A civilization whose navy has been overwhelmingly dominant for 150 years may experience:

- low perceived military need
- conservative procurement
- political pressure to spend elsewhere
- fewer urgent military programs
- increasing difficulty at the scientific frontier

A weaker civilization facing those ships may experience:

- high Fleet Loss Pressure
- high Weapon Ineffectiveness Pressure
- high Enemy Mobility Pressure
- captured wreckage and telemetry
- strong political support for military research

The weaker civilization can therefore concentrate far more research effort on closing the specific gap.

There is no hidden “underdog +50% research” rule.

The dominant civilization can maintain its lead if its culture/government remains innovative or if the player deliberately keeps investing.

Once it observes rivals becoming dangerous, its own pressure can rise again and an arms race can begin.

## Player UI — early game

The early research interface should emphasize only the simple quantities players need immediately:

- available projects
- total research labs
- assigned/unassigned labs
- RP/year
- minimum labs required by selected project
- labs currently assigned
- estimated completion time

Research Pressure should appear when it is meaningful to a **known** field.

Do not show hidden pressure meters that reveal undiscovered technologies.

Example:

A player can see:

> High-Gravity Health Pressure: 38/25 required

only after the civilization has recognized high-gravity physiology as a meaningful research field.

They should not see:

> Wormhole Pressure: 0/60

in 2050 when their scientists do not know controlled wormholes are a plausible engineering path.

## Later research infrastructure

The same lab-capacity system can support more sophisticated institutions later without changing the core rules.

Examples:

- planetary research laboratory: +1 effective lab
- major research institute: multiple effective labs
- orbital microgravity laboratory: specialized/bonus capacity for relevant fields
- high-energy physics complex: relevant facility requirement and/or effective lab capacity
- xenoscience institute: improved foreign-tech analysis
- machine research network: high effective lab capacity for compatible synthetic/computational fields
- biological research ecosystem: specialized biological capacity

The UI can still summarize these as effective Research Labs while advanced players inspect specialization when desired.

## Persistence and performance

Do not simulate individual scientists.

A civilization needs only compact research-state data such as:

- effective lab capacity
- lab assignments
- RP progress on active projects
- pressure values relevant to known fields
- evidence tokens
- field competence
- visible hypotheses
- completed/mature technology IDs

The universal technology possibility graph remains static game data and is not copied into every campaign save.

## Seed data

Machine-readable seed values are stored in:

`data/research/v1/research_economy.json`

The possibility catalog is stored by domain under:

`data/research/v1/`

The values in the seed dataset establish architecture and relative scale. They remain subject to balancing through simulation/testing without changing the underlying RP + Pressure + Labs model.
