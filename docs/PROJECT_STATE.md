# Canonical Project State

This is the authoritative continuity record for Stellar Continuum. `WORKSTREAMS.md` defines branch ownership; research design/data merges do not promote gameplay VERSION.

## Repository / identity

- Working title: **Stellar Continuum**
- Repository: **`afterburn25/stellar-continuum`** (public)
- Engine/runtime: Godot 4.7.2 .NET, C# / .NET 8
- Architecture: Godot presentation/platform layer + plain-C# authoritative simulation core

## Gameplay baseline known to Adaptive Research

As of 2026-09-07 this workstream still records:

- gameplay version: **`0.0.6-dev.1`**
- validated gameplay merge: `91a2204b96ed08c2178875cbc8d5b0bc378372ad`
- save format: v6

Other gameplay workstreams own later gameplay status if they change it.

## Adaptive Research workstream

- persistent branch: **`dev/adaptive-research`**
- owner: dedicated Adaptive Research / Technology chat
- scope: possibility graph, RP/Pressure/Labs, emergence/evidence/applicability, capabilities/maturation, competence/facilities/tacit knowledge, foreign tech/exchange, starting histories/runtime/view contracts, agenda/scientific culture/fair AI planning, long-run benchmarks, species-neutral biochemical diversity, distributed scientific continuity, research-facing UI/contracts/validation

Other workstreams may consume research events/queries/capabilities but should not independently edit canonical research graph/schema files without coordination.

## Durable research rules

- hidden shared Technology Possibility Graph; player/AI never see the complete future tree
- current public seed: **360 nodes / 21 domains / 59 pressures / 16 alternative-solution sets / 14 applicability traits / 9 evidence types / 36 knowledge fields / 17 cross-lineage capabilities**
- RP comes from physical Effective Research Labs; Pressure is contextual need/evidence and only an explicit hard gate where configured
- one directed program early, later 2 -> 4 -> lab-capacity-only concurrency
- functional requirements use cross-lineage capabilities when implementation does not matter
- knowledge != physical deployment
- competence is theoretical / experimental / engineering; specialist facilities and tacit expertise matter
- foreign technology uses separate Understanding / Operability / Reproduction / Adaptation axes and never instantly becomes native Mature technology
- starting civilizations compose past scientific history, not species-specific future trees
- agenda guides recognized attention/capacity requests, not hidden-node visibility or direct RP multipliers
- scientific culture is mutable institutional behavior, not an immutable species research bonus
- AI plans from the same visible horizon and blockers as the player; no hidden rank/catch-up/knowledge cheats
- runtime is sparse/event-index driven; no full-graph per-tick scan and no per-frame hidden-graph UI scan
- biochemical identity is composable population context, never a named-race tech tree or flat research modifier
- mature biochemical knowledge may be civilization-wide while operational applicability/capabilities remain population-context scoped
- one civilization can contain multiple incompatible biochemical populations without duplicating the full research graph
- distributed research uses sparse exception contexts rather than one tree/state copy per colony or region
- scientific maturity, local codified access, local active practice, and physical deployment are distinct
- data can propagate through real communications; experts/tooling/prototypes/institutions do not teleport as information
- there is no universal distance research penalty; only actual communication, archive, institutional, political, and practice constraints
- successor states inherit research from real local archives/assets/expertise, not an old empire's global technology checkbox set
- secret/rare discovery details remain outside public research data

## Validated Adaptive Research milestones

1. **PR #11** -> `f70e122134e87c1449582b573c5e2db8b045d311`: possibility graph + RP/Pressure/Labs + catalog validator.
2. **PR #12** -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`: emergence/evidence/pressure dynamics.
3. **PR #13** -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`: capability interoperability + maturation/hypotheses/setbacks.
4. **PR #17** -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`: competence + specialist facilities + tacit expertise + Project Readiness.
5. **PR #30** -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`: foreign-tech axes + exchange/licensing/brokerage + evolving visible-only UI.
6. **PR #44** -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`: composable starting histories + runtime/event-query contract + materialized research view.
7. **PR #53** -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`: research agenda + mutable scientific culture + natural complacency/catch-up + fair-information AI planning.
8. **PR #59** -> `d3916d3e6551c7a8b606716858d2d782581b1dac`: deterministic 500/1,000-year research divergence/state-soak benchmark harness.
9. **PR #63** -> `1e69fa65c2ec0df2feef16bd30794b08f0b2000a`: species-neutral alternative biochemistry/exotic biospheres, modular biochemical starts/facilities, multispecies applicability, biochemical validators/benchmarks.
10. **PR #73** -> `6c832fc07359ebb4fbe40c4dc079f19e13c11fca`: distributed scientific knowledge/regional continuity, sparse context access/practice, successor inheritance, reintegration, and continuity benchmarks.

None of these design/data milestones promoted gameplay VERSION.

## Long-horizon benchmark baseline

After the 360-node expansion:

### 500-year same-origin divergence

- minimum pairwise Mature-tree Jaccard distance: **0.457**
- Mature catalog fractions: orbital industrialist **0.253**, defense engineer **0.292**, biosphere adaptor **0.350**
- unique Mature nodes: **11 / 36 / 68**
- common Mature nodes: **47**

### 350-year military complacency / response

- initial hegemon lead: **10.85** benchmark units
- gap at year 180: **-12.65**
- leader attention **1.0 -> 3.0** after legitimate rival catch-up evidence
- final benchmark scores: leader **18.55**, challenger **33.40**
- no hidden catch-up multiplier, leader penalty, forced parity, or guaranteed comeback

### 1,000-year civilization research-state soak

- Generalist A: **89** node states / **124** recent events / **64** detailed history
- Generalist B: **105 / 156 / 80**
- Specialist C: **81 / 108 / 56**

## Milestone #9 — alternative biochemistry / exotic biospheres

Merged/validated through PR #63 at `1e69fa65c2ec0df2feef16bd30794b08f0b2000a`.

Key results:

- 30-node **Alternative Biochemistry & Exotic Biospheres** domain
- carbon-water common reference but not universal default
- ammonia-rich, cryogenic-hydrocarbon, and silicon/mineral native-specific research in the same graph
- 36th competence field: `biochemistry`
- cross-biochemistry multispecies habitation/medical/biofabrication capabilities
- **15 starting fragments / 7 reference starts** in modular files
- mixed-biochemistry civilizations use sparse population-context applicability/capability records, not per-population graph copies

Biochemical applicability benchmark:

- human carbon-water: 4 shared / 0 exotic native-specific
- ammonia-rich: 10 / 6
- cryogenic-hydrocarbon: 10 / 6
- silicon/mineral: 12 / 8
- exotic native-specific pairwise Jaccard distance: **1.000** at this seed stage

## Milestone #10 — distributed scientific knowledge / regional continuity

**Merged/validated through PR #73 at `6c832fc07359ebb4fbe40c4dc079f19e13c11fca`.**

Final validated PR head: `5bf74ba39baf57b1e41e8230ab0251d376c10055`.

Established:

- four explicit distinctions: scientific maturity/truth, local codified access, local active practice, physical deployment
- four access/practice states: Absent / Reference Only / Codified / Practiced
- `research_context` records only for materially divergent communication/institution regions; normal synchronized colonies allocate no context
- context state stores only sparse node-access/field-practice exceptions, located research assets, pending transmissions, archive/training summaries, isolation state, compressed history
- structural guardrails do not hardcode current catalog or field counts
- information disseminates through actual communication paths/latency/bandwidth/security policy
- expert cohorts, prototypes, manufacturing tooling, operating institutions, and other physical/tacit assets do not teleport as data
- no universal distance research penalty
- isolation alone does not lower competence; practice declines only when real institutions/training/activity disappear
- redundant archive copies prevent magical one-site knowledge erasure
- censorship can restrict access without destroying every archive copy
- successor states inherit from actual local archives, received discoveries, experts, facilities/tooling/prototypes, training, active projects, and external access
- federations/alliances do not merge technology trees; science sharing remains component/right/access based
- stable extension exposes **7 input events / 6 queries** to communications, governance, population, construction, diplomacy, UI, and AI
- fair-information AI cannot use unsynchronized core knowledge in an isolated context

### Distributed continuity benchmark

Communication latency:
- discovery year **2200.0**, one-way latency **1.75 years**, delivery **2201.75**
- destination Absent -> Codified; practice not auto-granted

40-year partition:
- missed core discoveries: **3**
- propulsion loss: theoretical **2**, experimental **14**, engineering **24**
- materials loss: theoretical **2**, experimental **8**, engineering **16**
- reconnect latency **0.25 years**, practice recovery reference **8 years**

Successor-state fracture:
- shared replicated foundation: **10 nodes**
- Successor A: **15 total / 5 specialist**
- Successor B: **14 total / 4 specialist**
- Jaccard distance: **0.474**
- one recent former-polity technology inherited by neither because actionable local records were absent

Archive catastrophe:
- **4 affected nodes**
- Codified -> Reference Only -> Codified after **12 years**
- practice not auto-restored

1,000-year distributed-context soak across **120 geographic regions**:
- discoveries processed: **334**
- peak explicit research contexts: **5** (bound 12)
- peak node-access exceptions: **17** (bound 320)
- peak field-practice exceptions: **10** (bound 120)
- peak pending transmissions: **9** (bound 192)
- final explicit contexts: **3**
- recent-event and archived-summary rings remained bounded

Final validation passed:

- distributed structural validator
- distributed continuity benchmark
- normal research validators and long-horizon benchmark
- biochemical structural/applicability gates
- .NET restore/build with **0 warnings / 0 errors**
- research-only changed-file audit

## Known shared CI limitation — issue #61

The current Godot runtime smoke command can exit successfully while logging:

`Cannot instantiate C# script ... res://src/Game/Presentation/Main.cs`

Tracked in **GitHub issue #61**. This is outside Adaptive Research ownership. Until fixed, research PRs may state that the Godot process step completed, but must not claim semantic runtime health solely from that exit code.

## Research validation stack

Core research/build pipeline:

1. `validate_research_catalog.py`
2. `validate_research_maturation.py`
3. `validate_research_competence.py`
4. `validate_research_transfer_ui.py`
5. `validate_research_start_runtime.py`
6. `validate_research_agenda_ai.py`
7. `validate_research_benchmarks.py`
8. .NET restore/build
9. shared Godot process smokes, with issue #61 caveat

Specialized research gates:

10. `validate_research_biochemistry.py`
11. `validate_research_biochemistry_benchmarks.py`
12. `validate_research_distributed_continuity.py`
13. `validate_research_distributed_continuity_benchmarks.py`

## Campaign horizon / scalability target

- demo: ~100–150 meaningful years
- initial paid Early Access: ~**500 meaningful years**
- engineering soak: at least **1,000 simulated years** without unbounded state/save/performance growth
- no hard year-based game-over

## Next Adaptive Research action

**Milestone #11: research secrecy, compartmentalization, and protected/compromised science.**

Goals:

- model public/restricted/classified/compartmented research access as information policy, not magic invisibility
- classified projects can reduce normal dissemination while creating real coordination/redundancy/continuity tradeoffs
- physical observation can reveal capabilities even when implementation records remain secret
- leaks, captured records, defectors, compromised facilities, and espionage outputs enter research as normal evidence/tacit/foreign-tech assets; Intelligence owns how they are obtained
- declassification/reclassification changes access/dissemination, not scientific truth
- security compartments must remain sparse and bounded rather than duplicating the graph
- add deterministic leakage/compartment-collapse/declassification benchmarks
- preserve fair-information AI and public-repository secret-content boundaries
- do not promote gameplay VERSION until runtime/gameplay integration is owned and validated by the appropriate workstream
