# Development History

This is the concise chronological record of validated gameplay milestones and major research design/data changes. Research milestones do not promote gameplay VERSION.

## Gameplay milestones known to this research workstream

- 0.0.1 simulation foundation — merged/validated.
- 0.0.2-dev.1 civilizations/fog — `2aaa6d3aeead400882c8214005e3bd78cfe17eaa`.
- 0.0.3-dev.1 exploration/first contact — `8683acebb1860ff3d94838656c65c8a82cd90d68`.
- 0.0.4-dev.1 colonies/basic economy — `ebcba9239a0d12d0c5c99f41937193176b6c8664`.
- 0.0.5-dev.1 2050 Pre-Warp Dawn — `c457f5e57c51057e86b857d0d828ff42d75e8f8b`.
- 0.0.6-dev.1 construction-driven development — gameplay baseline recorded here at `91a2204b96ed08c2178875cbc8d5b0bc378372ad`.

Other gameplay workstreams own later gameplay status if changed.

## Adaptive Research milestone history

### #1 — possibility graph / RP + Pressure + Labs

**Merged/validated:** PR #11 -> `f70e122134e87c1449582b573c5e2db8b045d311`.

Established the original hidden shared possibility graph, RP + contextual Pressure + physical Effective Research Labs, staged concurrency, and catalog validation.

### #2 — emergence / evidence / pressure dynamics

**Merged/validated:** PR #12 -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`.

Established applicability/evidence, full pressure dynamics, sparse indexed emergence, no calendar unlocks or rank catch-up, and fair-information observations.

### #3 — capability interoperability / maturation

**Merged/validated:** PR #13 -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`.

Established specific-knowledge vs cross-lineage functional capability requirements, scoped capabilities, maturation/hypothesis outcomes, setbacks, side discoveries, and knowledge-vs-deployment separation.

### #4 — competence / facilities / tacit knowledge

**Merged/validated:** PR #17 -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`.

Established theory/experiment/engineering competence, specialist research facilities, tacit knowledge/expert cohorts/training, and bounded Project Readiness.

### #5 — foreign technology / exchange / research UI

**Merged/validated:** PR #30 -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`.

Established Understanding / Operability / Reproduction / Adaptation, compatibility dependencies, technology exchange/licensing/brokerage, buyer-specific value, and visible-only evolving research UI.

### #6 — starting histories / runtime / materialized view

**Merged/validated:** PR #44 -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`.

Established reusable starting scientific histories, prerequisite-closed starts, sparse event/query runtime boundary, materialized visible-only research view, and authoritative UI/AI command revalidation.

### #7 — research agenda / scientific culture / fair AI planning

**Merged/validated:** PR #53 -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`.

Established high-level research priorities, 12 mutable scientific-culture axes, causal complacency/catch-up, explainable fair-information AI planning, and no hidden rank/leader bonuses.

### #8 — long-horizon divergence / state soak

**Merged/validated:** PR #59 -> `d3916d3e6551c7a8b606716858d2d782581b1dac`.

Added deterministic offline design/CI benchmarks. The harness may scan public data offline but is explicitly not production runtime.

Current 360-node benchmark baseline:

- 500-year same-origin minimum Mature-tree Jaccard distance: **0.457**
- Mature catalog fractions: **25.3% / 29.2% / 35.0%**
- unique Mature nodes: **11 / 36 / 68**
- 350-year military benchmark still produces causal challenger leapfrog/re-attention without hidden catch-up
- 1,000-year civilization research state remains bounded at **81–105 node-state records** per reference civilization

### #9 — alternative biochemistry / exotic biospheres

**Merged/validated:** PR #63 -> `1e69fa65c2ec0df2feef16bd30794b08f0b2000a`.

Final head: `7730dccf89cb5548aea90f25de6ac0541cf2106d`.

Expanded the public research seed to:

- **360 nodes**
- **21 domains**
- **59 Research Pressures**
- **16 alternative-solution sets**
- **14 applicability traits**
- **36 knowledge fields**
- **17 cross-lineage capabilities**

Established a 30-node Alternative Biochemistry & Exotic Biospheres domain, carbon-water/common-but-not-universal assumptions, ammonia-rich/cryogenic-hydrocarbon/silicon-mineral native lineages, comparative cross-biochemistry research, modular biochemical facilities, modular 15-fragment/7-profile starting histories, and sparse multispecies biochemical applicability.

Biochemical applicability benchmark:

- human carbon-water: 4 shared / 0 exotic native-specific
- ammonia-rich: 10 / 6
- cryogenic-hydrocarbon: 10 / 6
- silicon/mineral: 12 / 8
- exotic native-specific pairwise Jaccard distance: **1.000** at this seed stage

### #10 — distributed scientific knowledge / regional continuity

**Merged/validated:** PR #73 -> **`6c832fc07359ebb4fbe40c4dc079f19e13c11fca`**.

Final validated PR head: `5bf74ba39baf57b1e41e8230ab0251d376c10055`.

Established:

- scientific truth/maturity separate from local codified access, active practice, and physical deployment
- access states: Absent / Reference Only / Codified / Practiced
- sparse `research_context` records only when a region materially diverges from synchronized civilization science
- no one-context-per-colony approach
- no complete node/field/static-graph copies per context
- codified records propagate through real communication path/latency/bandwidth/security inputs
- expert cohorts, prototypes, tooling, operating institutions, and hands-on practice remain physical/tacit
- no universal distance research penalty
- isolation only reduces practice when real institutions/training/activity disappear
- archive redundancy and censorship/access distinction
- successor states inherit real local archives, recent receipts, experts, facilities/tooling/prototypes, training, and active projects rather than the old polity's full technology set
- federations/alliances share science through records/datasets/experts/facility access/rights rather than merged technology trees
- stable research integration extension with **7 input events / 6 queries**
- fair-information AI cannot use unsynchronized core science in an isolated context

Distributed continuity benchmark:

- communication delivery: year **2200.0 + 1.75y -> 2201.75**, Absent -> Codified, no automatic practice
- 40-year partition: theoretical losses only **2**, experimental losses **8–14**, engineering losses **16–24**, showing archive preservation vs practice atrophy
- successor fracture: shared foundation **10 nodes**; A **15 total / 5 specialist**, B **14 / 4**, Jaccard distance **0.474**
- archive catastrophe: 4 nodes, Codified -> Reference Only -> Codified after **12 years**, no automatic practical recovery
- 1,000-year / 120-region soak: **5** peak explicit contexts, **17** peak node-access exceptions, **10** peak field-practice exceptions, **9** peak pending transmissions, **3** final contexts

Final #10 validation passed:

- distributed structural validator and benchmark
- all core research validators / long-horizon benchmark
- biochemical validators / applicability benchmark
- .NET restore/build with 0 warnings / 0 errors
- research-only changed-file audit

## Shared CI limitation — issue #61

GitHub issue **#61** tracks an existing shared false-positive runtime smoke: Godot can log failure to instantiate `res://src/Game/Presentation/Main.cs` while returning exit code 0.

This is outside Adaptive Research ownership. Until fixed, research acceptance treats the shared Godot runtime process step separately from semantic runtime health.

## Persistent workstream rule

Adaptive Research uses `dev/adaptive-research` and remains owned by the dedicated research chat. Research data merges do not promote gameplay VERSION.

## Next research milestone

Milestone #11: **research secrecy, compartmentalization, and protected/compromised science**.

Research owns the knowledge-access/classification consequences. Intelligence/security systems own how spying, surveillance, theft, infiltration, interception, coercion, and detection actions occur.
