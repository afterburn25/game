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

## Default capability grants

A research node's declared `capabilities` become available at **Mature** by default.

This keeps the node itself as the single source of truth and avoids duplicating every capability in another file.

`technology_grants.json` stores only exceptions/structural effects such as:

- a capability becoming usable before Mature
- a technology changing research-capacity institutions
- a technology enabling a deployment event
- a technology granting an acquired civilization-level research applicability trait

Examples:

- Prototype Warp Drive at **Demonstrated** -> `experimental_interstellar_transit`
- Stable Warp Drive at Mature -> `interstellar_transit` through its node output
- Artificial Wormhole Stabilization at Mature -> `interstellar_transit` through its node output
- Long-Range Warp at Mature -> `extended_interstellar_transit`
- Orbital Shipyard at Mature -> `spacecraft_construction`
- Megastructure Fabrication at Mature -> `megastructure_construction`
- Biofabrication at Mature -> acquired civilization trait `biological_fabrication_possible`
- research-institution technologies advance directed-program concurrency

### Technology knowledge does not mean physical deployment already exists

Research can make something possible without creating it automatically.

For example:

- Mature **Synthetic Cognition** unlocks the ability to instantiate persistent autonomous machine cognition.
- Mature **Whole-Mind Emulation** provides another possible route.
- The civilization gains `machine_cognition_present` only after a persistent machine cognition is actually instantiated through the deployment event.

This prevents "research completed" from magically creating a new population or physical infrastructure.

The structural grant/deployment table lives in `data/research/v1/technology_grants.json`.

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

A capability can be granted before full maturity when that is physically meaningful. A demonstrated prototype FTL experiment can establish experimental interstellar transit without yet being a reliable fleet drive.

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
- produce an anomalous result
- redirect through a side discovery

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

## Anomalous results

An experiment can fail to validate its intended hypothesis while producing a reproducible effect scientists cannot yet explain.

That can create legitimate anomaly evidence or Research Pressure and expose a different branch. It does not automatically make the original hypothesis true and does not award a mature technology.

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
- an Investigable node only if its normal requirements are already satisfied
- field competence
- lower uncertainty/contextual cost on a visible project

Side discoveries never hand out an unrelated mature technology and never bypass species/evidence/prerequisite rules.

## Pause / resume

Directed research can be paused.

- RP progress is preserved.
- assigned labs are released.
- a very long pause can reduce active-team efficiency or require reorganization.
- discovered scientific knowledge is not deleted simply because funding stopped.

## Determinism and uncertainty

Where genuine uncertainty remains, outcome selection can use a **campaign-seeded deterministic random stream**.

This means:

- the simulation remains reproducible for debugging
- research is not a fixed predetermined universal timeline
- outcomes can depend on evidence, competence, institutions, facilities, prior failures, and project profile

We should avoid a simple casino-style percentage displayed on every normal technology.

## Performance

Only active projects need detailed maturation state.

A mature or archived technology normally collapses to compact persistent data such as:

- node ID
- maturity/archive resolution
- capability/structural grants that matter
- historical completion/archive date

Old experiment detail can compress into a summary.

Capabilities are indexed separately from source technologies, allowing other simulation systems to ask "does this civilization/population have capability X?" without scanning the full research graph.

## Canonical machine-readable files

- `data/research/v1/capability_model.json`
- `data/research/v1/technology_grants.json`
- `data/research/v1/maturation_model.json`

There are intentionally **not** duplicate `capability_grants.json` or `research_maturation.json` schemas.

## Validation

Research changes must pass both:

```text
python3 scripts/validate_research_catalog.py data/research/v1
python3 scripts/validate_research_maturation.py data/research/v1
```

The maturation validator checks capability IDs/scopes, capability implications, capability requirements, early capability grants, structural/trait grants, deployment-event references, research-capacity-stage grants, maturation states, outcome references, and implication cycles.

## Public repository boundary

The public capability model can define normal capability classes, but it must not enumerate secret technologies, secret artifact sources, exact hidden triggers, or hidden special-AI acquisition rules.

Secret/rare technology can plug into the same capability interfaces from non-public content later.
