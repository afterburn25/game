# Adaptive Research System

## Purpose

Stellar Continuum does not use a conventional fixed technology tree that the player can inspect from beginning to end.

The simulation contains a broad **Technology Possibility Graph** describing discoveries that may be physically/scientifically possible in the setting. Each civilization materializes only the small portion of that graph it currently understands well enough to investigate.

The visible research tree therefore changes as the civilization changes.

A civilization may open the research screen in 2080 and see branches that did not exist for it in 2050. Another civilization beginning from similar broad knowledge can reach 2400 with a radically different visible tree because its biology, environment, wars, resources, discoveries, culture, institutions, and contact history were different.

The system is intended to create technological divergence without maintaining a separate handcrafted tree for every playable species.

## Core rules

1. **The player never sees the complete possibility graph.**
2. **Need creates research pressure, but need is not the only source of discovery.**
3. **Basic science can expose possibilities before a practical use is known.**
4. **Observation, anomalies, alien contact, captured devices, and foreign science can expose branches absent from the native research horizon.**
5. **A known possibility is not automatically researchable.** Prerequisite knowledge, evidence, infrastructure, biology, materials, and scientific maturity can still be missing.
6. **A researched idea is not instantly mature technology.** Hypothesis, experiment, demonstration, engineering, and mature deployment are separate states.
7. **Speculative hypotheses can fail or produce partial/side discoveries.**
8. **Similar capabilities can have different technological implementations.**
9. **Foreign technology is not an instant unlock and may be incompatible, dangerous, or beyond current understanding.**
10. **Dominant civilizations do not receive an arbitrary research penalty.** Complacency/catch-up emerges from reduced need, frontier difficulty, institutions, culture, intelligence, competition, and observed rival progress.
11. **A civilization can remain ahead if it continues investing intelligently.** There is no forced rubber-band equalization.
12. **Research must be scalable.** The runtime must not evaluate the whole graph every simulation tick for every civilization.

## Player-facing tree

The research UI should show only nodes that are currently relevant to the civilization.

Typical visible states:

- **Rumored** — weak/uncertain evidence suggests a phenomenon or possibility.
- **Hypothesized** — scientists can describe a testable idea.
- **Investigable** — prerequisites/evidence are sufficient for a serious program.
- **Experimental** — prototypes/tests are underway.
- **Demonstrated** — the principle works under controlled conditions.
- **Engineering** — civilization is making it practical, reliable, and manufacturable.
- **Mature** — established capability/technology.
- **Archived** — mature or historically important knowledge no longer needs to occupy the active research horizon.

`Unknown` possibilities are not rendered to the player.

The UI is therefore an evolving tree, not a checklist. Branches can appear after discoveries, become more urgent after events, become dormant when irrelevant, and reconnect through cross-domain discoveries.

## What creates a research branch?

### Problem-driven pressure

Examples:

- high-gravity health problems
- low-gravity developmental problems
- radiation exposure
- food/water shortages
- long mission duration
- supply-line overstretch
- maintenance failures
- hostile missiles
- armor failures
- enemy mobility superiority
- sensor blindness
- stealth threats
- communication delay
- administrative distance
- ecological damage
- resource scarcity
- labor shortages
- AI safety incidents

A problem does not guarantee a specific solution. It increases pressure on applicable scientific domains and candidate solutions.

### Basic/exploratory science

Civilizations can fund broad fields without knowing the application in advance.

Examples include:

- high-energy physics
- gravitational physics
- quantum measurement
- exoplanetary science
- complex systems
- materials characterization

This allows discoveries that are not immediate responses to a crisis.

### Observation/discovery

New evidence can permanently alter the research horizon:

- alien signal
- observed foreign propulsion
- captured foreign device
- alien biology
- anomalous astrophysical phenomenon
- battlefield telemetry
- unexpected experimental result

Observation can prove that a capability is possible without revealing how it works.

## Research pressure and natural catch-up

Research intensity is contextual.

A civilization with overwhelmingly superior warships may gradually experience less military research pressure if:

- existing weapons remain effective
- no credible opponent is visible
- doctrine has repeatedly succeeded
- political/cultural institutions become complacent
- funding shifts to other urgent needs
- further progress is at a difficult scientific frontier

A weaker rival can simultaneously gain pressure because:

- it suffers losses
- it observes superior enemy capabilities
- it has a concrete performance target
- wreckage/telemetry provides evidence
- political support for military research rises

This can narrow a technological gap without granting a hidden catch-up multiplier.

When the leader detects credible rival progress, threat pressure can increase again and create an arms race.

Species/culture/government modifies this behavior. A paranoid or strongly innovation-oriented civilization may maintain high research pressure even while dominant.

## Capability vs implementation

The runtime should reason about capabilities separately from technologies.

Examples:

### Long-duration habitation

Possible solution families include:

- closed-loop/bioregenerative life support
- rotating habitats
- metabolic torpor
- symbiotic biological life support
- machine habitats for synthetic populations

### FTL access

Possible implementations in the public seed graph include:

- warp-field development
- infrastructure-heavy wormhole stabilization

Additional rare/secret routes are intentionally not stored in this public dataset.

### Spacecraft survivability

Possible approaches include:

- layered/reactive/adaptive armor
- active protection
- localized defensive fields
- living/self-repairing hulls

A civilization can therefore satisfy a strategic capability without following another civilization's exact research history.

## Species and biology

The possibility graph is shared as a universe-scale catalog, but individual nodes can have applicability requirements.

Examples:

- gravity medicine requires gravity-sensitive metabolic biology
- induced torpor requires compatible metabolism
- biological fabrication requires a civilization capable of engineered biological manufacturing
- synthetic-civilization branches require machine cognition to exist

This is not a species-specific fixed tree. Species traits filter/weight what can plausibly emerge.

A biological civilization can create synthetic minds and later gain access to machine-civilization branches. A machine civilization does not waste research effort on human cardiovascular medicine.

## Foreign technology

Foreign science can create evidence tokens and new branches.

Suggested progression:

1. observe a foreign capability
2. gather telemetry/sample/device
3. identify relevant scientific domain
4. perform device/material/software/biological analysis
5. determine compatibility
6. reproduce subsystems where possible
7. develop a native adaptation
8. potentially create hybrid technology

Some steps may be impossible with current science or biology.

A technology useless to the holder may remain highly valuable to another civilization, preserving future technology-trade/brokerage gameplay.

## Runtime scalability

The static catalog is not an active per-civilization tree.

Each civilization should persist only compact research state such as:

- mature technology IDs
- known hypotheses
- currently visible/investigable candidates
- active research programs
- field competencies
- evidence tokens
- current research pressures
- cultural/government research priorities
- recent discovery history

### Candidate indexing

Do not scan every node every simulation tick.

Build indexes once from static data:

- prerequisite -> child nodes
- pressure -> candidate nodes
- knowledge field -> candidate nodes
- evidence type -> candidate nodes
- trait/applicability -> candidate nodes
- capability/solution family -> candidate nodes

Re-evaluate candidates only when relevant state changes, for example:

- technology matures
- new evidence arrives
- a pressure crosses a meaningful band
- species/civilization traits change
- research institution/funding policy changes
- a periodic low-frequency research review occurs

A civilization with 244 or eventually thousands of universal possibilities may therefore have only a few dozen active research-state records.

### Persistence

The full static catalog belongs in game data, not duplicated in every save.

Campaign saves/databases should store IDs plus civilization-specific state.

Old detailed research events can be archived/compressed into meaningful historical milestones.

## Seed dataset v1

The public v1 design dataset contains:

- **244 possibility nodes**
- **15 research domains**
- **59 research-pressure types**
- **10 explicit alternative-solution sets**
- prerequisite links forming a validated acyclic graph
- applicability/evidence tags
- research-pressure affinities
- knowledge-field tags
- solution-family tags
- capability outputs

Domains:

1. Foundational Science
2. Energy & Power
3. Propulsion & Transit
4. Space Industry & Habitats
5. Life Support & Medicine
6. Planetary Engineering
7. Materials Science
8. Computing, AI & Robotics
9. Sensors & Communications
10. Military Engineering
11. Logistics & Fabrication
12. Administration & Institutions
13. Xenoscience & Foreign Technology
14. Biotechnology & Living Systems
15. Synthetic Civilization Systems

The dataset is seed design data, not final balance. Numerical research costs/times are deliberately not frozen yet because they should eventually depend on civilization context, field competence, need, infrastructure, funding, evidence, and frontier difficulty.

## Public repository boundary

This public dataset contains normal research possibilities only.

It must not expose:

- exact hidden discovery triggers
- exact rare probabilities
- complete secret artifact chains
- intentionally hidden special-AI conditions
- rare secret technologies intended for player discovery

Those can plug into the same runtime architecture from non-public/obfuscated content sources later.

## Validation requirements

Before accepting changes to the public possibility graph:

- node IDs must be unique
- every prerequisite reference must exist
- graph must remain acyclic
- pressure references must exist
- solution-set node references must exist
- domain counts/index must match files
- static data must not contain campaign/player state
- no secret-content leakage into public data

## Early-release requirement

The Early Access build does not need every node fully implemented as gameplay content.

It does need the runtime architecture to support:

- evolving visible research tree
- research pressure
- hidden unknown possibilities
- multiple discovery sources
- applicability/evidence filtering
- capability-vs-implementation distinction
- foreign-tech research state
- archival/mature research state
- bounded candidate evaluation
- save-safe IDs

This avoids rebuilding the research system after long campaigns and additional species already exist.
