# Development History

This is the concise chronological record of validated gameplay milestones and major design/data state changes. It keeps research design acceptance separate from gameplay-version promotion.

## Gameplay milestones known to this research workstream

- 0.0.1 simulation foundation — merged/validated.
- 0.0.2-dev.1 civilizations/fog — `2aaa6d3aeead400882c8214005e3bd78cfe17eaa`.
- 0.0.3-dev.1 exploration/first contact — `8683acebb1860ff3d94838656c65c8a82cd90d68`.
- 0.0.4-dev.1 colonies/basic economy — `ebcba9239a0d12d0c5c99f41937193176b6c8664`.
- 0.0.5-dev.1 2050 Pre-Warp Dawn — `c457f5e57c51057e86b857d0d828ff42d75e8f8b`.
- 0.0.6-dev.1 construction-driven development — gameplay baseline recorded here at `91a2204b96ed08c2178875cbc8d5b0bc378372ad`.

Other gameplay workstreams own later gameplay status if changed.

## Adaptive Research milestone #1 — possibility graph / RP + Pressure + Labs

**Merged/validated:** PR #11 -> `f70e122134e87c1449582b573c5e2db8b045d311`.

Established the original 330-node hidden shared possibility graph, RP + contextual Pressure + physical Effective Research Labs, staged research-program concurrency, and the first catalog validator.

## Milestone #2 — emergence / evidence / pressure dynamics

**Merged/validated:** PR #12 -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`.

Established applicability traits/evidence, complete pressure dynamics, sparse indexed emergence, no calendar unlocks/rank-based catch-up, fair-information observations, and scoped applicability.

## Milestone #3 — capability interoperability / maturation

**Merged/validated:** PR #13 -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`.

Established implementation knowledge vs functional cross-lineage capabilities, scoped capability context, maturation/hypothesis resolution, non-destructive setbacks, side discoveries, and knowledge-vs-deployment distinction.

## Milestone #4 — competence / facilities / tacit knowledge

**Merged/validated:** PR #17 -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`.

Established theory/experiment/engineering field competence, specialist research facilities, tacit knowledge/expert cohorts/training, bounded Project Readiness, and no flat modifier-stack approach.

## Milestone #5 — foreign technology / exchange / research UI

**Merged/validated:** PR #30 -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`.

Established Understanding / Operability / Reproduction / Adaptation axes, real compatibility/dependency constraints, technology exchange/licensing/brokerage, buyer-specific value, and visible-only evolving research UI.

## Milestone #6 — starting histories / runtime / materialized view

**Merged/validated:** PR #44 -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`.

Established reusable starting scientific-history fragments, prerequisite-closed starting state, sparse event/query runtime boundary, read-only materialized visible research view, and authoritative UI/AI command revalidation.

## Milestone #7 — research agenda / scientific culture / fair AI planning

**Merged/validated:** PR #53 -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`.

Established recognized research priorities, 12 mutable scientific-culture axes, natural complacency/catch-up from legitimate conditions, fair-information AI planning, explainable project choice, and no hidden leader/catch-up bonuses.

## Milestone #8 — long-horizon divergence / state soak

**Merged/validated:** PR #59 -> `d3916d3e6551c7a8b606716858d2d782581b1dac`.

Added deterministic offline design/CI benchmarks. The harness may scan the public graph offline but is explicitly not production runtime.

### 360-node benchmark baseline after milestone #9

The original benchmark still passes after catalog expansion:

- 500-year same-origin Mature-tree minimum Jaccard distance: **0.457**
- Mature catalog fractions: **25.3% / 29.2% / 35.0%**
- unique Mature nodes: **11 / 36 / 68**
- common Mature nodes: **47**
- 350-year military scenario still produces causal challenger leapfrog and leader re-attention without hidden catch-up mechanics
- 1,000-year state soak remains bounded at **81–105 node-state records** per reference civilization

## Milestone #9 — alternative biochemistry / exotic biospheres

**Merged/validated:** PR #63 -> **`1e69fa65c2ec0df2feef16bd30794b08f0b2000a`**.

Final head: `7730dccf89cb5548aea90f25de6ac0541cf2106d`.

Expanded the public research architecture from:

- 330 -> **360 nodes**
- 20 -> **21 domains**
- 15 -> **16 alternative-solution sets**
- 6 -> **14 applicability traits**
- 35 -> **36 knowledge fields**
- cross-lineage capabilities -> **17 total**

The Research Pressure catalog intentionally remains **59**; existing causal pressures are reused rather than inventing exotic-biochemistry meters.

Established:

- species-neutral biochemical dimensions: metabolic context, molecular substrate, dominant solvent, active temperature regime, mineral-structural biology, liquid-medium nativeness
- carbon-water as common reference, not a universal/default race tree
- silicon-centered life as explicitly speculative/rare without fantasy automatic superiority
- one new 30-node public domain: **Alternative Biochemistry & Exotic Biospheres**
- 4 shared biochemical foundations
- 6 ammonia-rich native-specific possibilities
- 6 cryogenic-hydrocarbon native-specific possibilities
- 8 silicon/mineral native-specific possibilities
- 6 comparative cross-biochemistry possibilities requiring legitimate alien-biology evidence
- `biochemistry` as a 36th competence field
- alternative biochemical implementations that satisfy generic habitation/food/biosphere capabilities
- cross-biochemistry scoped capabilities for multispecies habitation, medical support, and biofabrication
- modular biochemical research-facility extension
- modular starting-profile loader validating **15 fragments / 7 reference starts** across multiple files
- explicit human-like carbon-water reference plus ammonia, cryogenic-hydrocarbon, and silicon/mineral early-space references
- mixed-biochemistry civilization model: one shared civilization knowledge graph plus sparse population-context applicability/capability records where biology matters

### Biochemical applicability benchmark

- human carbon-water: **4** applicable shared biochemical nodes, **0** exotic native-specific
- ammonia-rich: **10** applicable domain nodes, **6** native-specific
- cryogenic-hydrocarbon: **10** applicable domain nodes, **6** native-specific
- silicon/mineral: **12** applicable domain nodes, **8** native-specific
- pairwise native-specific Jaccard distance among the three exotic starts: **1.000** at this seed stage

### Validation

Final milestone #9 head passed:

- all normal Adaptive Research validators
- long-horizon research benchmark
- biochemical structural validator
- biochemical shared-graph applicability benchmark
- .NET restore/build with 0 warnings / 0 errors
- research-only changed-file audit

Gameplay VERSION was not promoted.

## Shared CI limitation — issue #61

GitHub issue **#61** tracks an existing shared false-positive runtime-smoke problem: Godot can log failure to instantiate `res://src/Game/Presentation/Main.cs` while returning exit code 0.

This is outside Adaptive Research ownership. Until fixed, research acceptance treats the shared Godot runtime process step separately from semantic runtime health.

## Persistent workstream rule

Adaptive Research uses `dev/adaptive-research` and remains owned by the dedicated research chat. Research data merges do not promote gameplay VERSION.

## Next research milestone

Milestone #10: **distributed scientific knowledge and regional research continuity**.

The objective is to model communication delay, isolated institutions, regional competence/tacit practice, censorship/archive loss, political fracture, successor-state knowledge inheritance, and later reintegration without ever creating a full 360-node graph copy per colony/region.
