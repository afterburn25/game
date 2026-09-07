# Research Maturation & Capability Interoperability

This document defines how Stellar Continuum turns an Adaptive Research possibility into reliable technology and how different technological lineages interoperate without collapsing back into one hidden universal tree.

## Core distinction: knowledge vs capability

A **technology prerequisite** means the later idea genuinely depends on that specific body of knowledge or implementation lineage.

A **capability requirement** means the later idea/system only needs a functional result, regardless of how the civilization achieved it.

Examples:

- `stable_warp_drive` may legitimately depend on `prototype_warp_drive` because it is the same warp lineage.
- `interstellar_logistics_network` should require **reliable interstellar transit**, not Stable Warp Drive specifically.
- `wormhole_stabilization` should require **megastructure construction capability**, not one exact human-style fabrication node.
- a machine or biological civilization can satisfy long-duration habitation through a compatible solution without researching a human rotating-habitat/medical path.

This is essential to late-game divergence. A civilization can reach strategically comparable abilities while its actual research history remains very different.

Machine-readable capability definitions live in `data/research/v1/capability_model.json`.

## Capability scope

Capabilities are scoped so multispecies civilizations do not erase biological incompatibility.

Current scopes include:

- **civilization** — e.g. reliable interstellar transit or spacecraft construction
- **population/species** — e.g. long-duration habitation or gravity management for a compatible population
- **colony/installation** — e.g. advanced local fabrication at a specific settlement/ship/installation

A machine population's long-duration habitat capability can satisfy generic long-duration habitation **for that machine population**, not automatically for biological citizens elsewhere in the same civilization.

## Capability and trait grants

Research can create effects beyond marking a node Mature.

Examples:

- Prototype Warp Drive demonstrated -> `experimental_interstellar_transit`
- Stable Warp Drive mature -> `interstellar_transit`
- Artificial Wormhole Stabilization mature -> `interstellar_transit`
- Long-Range Warp mature -> `extended_interstellar_transit`
- Orbital Shipyard mature -> `spacecraft_construction`
- Megastructure Fabrication mature -> `megastructure_construction`
- Biofabrication mature -> civilization gains mutable trait `biological_fabrication_possible`
- Synthetic Cognition mature -> civilization gains mutable trait `machine_cognition_present`
- research-institution technologies advance directed-program concurrency

The grant table lives in `data/research/v1/capability_grants.json`.

A grant does not reveal every other technology capable of producing the same capability.

## Research maturation states

The canonical visible states remain:

1. Unknown
2. Rumored
3. Hypothesized
4. Investigable
5. Experimental
6. Demonstrated
7. Engineering
8. Mature
9. Archived

A disproven hypothesis is **Archived with resolution `disproven`**, not a separate tenth top-level state.

That keeps the state machine compact while preserving negative scientific knowledge.

## Directed project progression

Once a possibility becomes Investigable and receives sufficient lab capacity, the directed project normally progresses through:

**Experimental -> Demonstrated -> Engineering -> Mature**

Seed progression divides total RP approximately into:

- Experimental: first ~45%
- Demonstrated: ~45–70%
- Engineering: ~70–100%

Those percentages are balancing seeds, not a final commercial lock.

A capability can be granted before full maturity when that is physically meaningful. For example, a demonstrated prototype FTL experiment can establish experimental interstellar transit without yet being a reliable fleet drive.

## Research is not guaranteed, but failure must make sense

Research uncertainty depends on what is being attempted.

### Established extension

Ordinary engineering based on established science can suffer delays, design errors, or failed prototypes, but it should not randomly become physically impossible.

### Frontier engineering

The principle is credible but difficult implementation may produce significant setbacks, partial successes, or explicitly modeled hazards.

### Scientific hypothesis

A node marked as a hypothesis is testing whether the proposed phenomenon/model is correct. It can be:

- supported
- refined
- disproven
- redirected through a side discovery

Disproof is a valid scientific result.

### Hazardous foreign/anomalous work

Dangerous outcomes are opt-in by explicit hazard profile. Ordinary late-game research does **not** automatically become catastrophic just because it is advanced.

## Setbacks do not erase civilization knowledge

A failed test can consume time/resources and delay the project, but it does not reset all RP to zero.

Repeated identical failure should become less likely because the civilization learns what did not work.

A major setback may create:

- stronger evidence
- increased field competence
- altered engineering assumptions
- side discoveries
- new safety requirements

## Disproven hypotheses

When a scientific hypothesis is disproven:

- the node becomes Archived with resolution `disproven`
- the civilization retains the negative result
- the same exact failed hypothesis does not immediately reappear as new
- related field competence can improve
- alternate solution branches can become more attractive/visible
- a side discovery may emerge

The player therefore loses time/opportunity, but not all scientific value.

## Side discoveries

Unexpected results can expose other legitimate possibilities.

Side discoveries can come from:

- direct children of tested knowledge
- strongly related knowledge fields
- alternative solutions to the same recognized problem
- experiment-generated evidence supporting another candidate

Possible results include:

- new evidence
- a new Rumored/Hypothesized node
- field competence
- lower uncertainty/cost on a visible project

Side discoveries never hand out an unrelated mature technology and never bypass species/evidence/prerequisite rules.

## Determinism and uncertainty

Where genuine uncertainty remains, outcome selection can use a **campaign-seeded deterministic random stream**.

This means:

- the simulation remains reproducible for debugging
- research is not a fixed predetermined universal timeline
- outcomes can still depend on evidence, competence, institutions, facilities, prior failures, and project profile

We should avoid a simple casino-style percentage displayed on every normal technology.

## Performance

Only active projects need detailed maturation state.

A mature technology normally collapses to compact persistent data such as:

- node ID
- maturity/archived state
- major capability/trait grants
- historical completion date

Old experiment detail can compress into a summary.

Capabilities are indexed separately from source technologies, allowing other simulation systems to ask "does this civilization/population have capability X?" without scanning the full research graph.

## Validation

Research changes must pass both:

```text
python3 scripts/validate_research_catalog.py data/research/v1
python3 scripts/validate_research_maturation.py data/research/v1
```

The maturation validator checks capability IDs/scopes, capability implications, capability requirements, capability/trait grants, research-capacity-stage grants, maturation states, outcome references, and implication cycles.

## Public repository boundary

The public capability model can define normal capability classes, but it must not enumerate secret technologies, secret artifact sources, exact hidden triggers, or hidden special-AI acquisition rules.

Secret/rare technology can plug into the same capability interfaces from non-public content later.
