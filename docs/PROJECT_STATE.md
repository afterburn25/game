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
- scope: possibility graph, RP/Pressure/Labs, emergence/evidence/applicability, capabilities/maturation, competence/facilities/tacit knowledge, foreign tech/exchange, starting histories/runtime/view contracts, agenda/scientific culture/fair AI planning, long-run benchmarks, species-neutral biochemical diversity, research-facing UI/contracts/validation

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
- secret/rare discovery details remain outside public research data

## Validated Adaptive Research milestones

1. **PR #11** -> `f70e122134e87c1449582b573c5e2db8b045d311`: possibility graph + RP/Pressure/Labs + catalog validator.
2. **PR #12** -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`: emergence/evidence/pressure dynamics.
3. **PR #13** -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`: capability interoperability + maturation/hypotheses/setbacks.
4. **PR #17** -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`: competence + specialist facilities + tacit expertise + Project Readiness.
5. **PR #30** -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`: foreign-tech axes + exchange/licensing/brokerage + evolving visible-only UI.
6. **PR #44** -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`: composable starting histories + runtime/event-query contract + materialized research view + fifth validator.
7. **PR #53** -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`: research agenda + 12-axis mutable scientific culture + natural complacency/catch-up + fair-information AI planning + sixth validator.
8. **PR #59** -> `d3916d3e6551c7a8b606716858d2d782581b1dac`: deterministic 500/1,000-year research divergence/state-soak benchmark harness and benchmark baseline.
9. **PR #63** -> `1e69fa65c2ec0df2feef16bd30794b08f0b2000a`: species-neutral alternative biochemistry/exotic biospheres, modular biochemical starts/facilities, multispecies population-scoped applicability, and biochemical validators/benchmarks.

None of these design/data milestones promoted gameplay VERSION.

## Milestone #8 benchmark baseline after 360-node expansion

The long-horizon benchmark still passes after milestone #9 increased the public catalog from 330 to 360 nodes.

### 500-year same-origin divergence

- minimum pairwise Mature-tree Jaccard distance: **0.457**
- Mature catalog fractions: orbital industrialist **0.253**, defense engineer **0.292**, biosphere adaptor **0.350**
- unique Mature nodes: orbital industrialist **11**, defense engineer **36**, biosphere adaptor **68**
- common Mature nodes: **47**

### 350-year military complacency / response

- initial hegemon lead: **10.85** benchmark units
- gap at year 180: **-12.65**; challenger can leapfrog through real pressure/attention differences
- leader military attention fell to **1.0** during perceived adequacy and later rose to **3.0** after legitimate catch-up observation
- final benchmark military scores: leader **18.55**, challenger **33.40**
- no hidden catch-up multiplier, leader penalty, forced parity, or guaranteed comeback

### 1,000-year state soak

- Generalist A: **89** node states / **124** recent events / **64** detailed history
- Generalist B: **105** / **156** / **80**
- Specialist C: **81** / **108** / **56**
- bounded despite catalog growth to 360 public possibilities

## Milestone #9 — alternative biochemistry / exotic biospheres

**Merged/validated through PR #63 at `1e69fa65c2ec0df2feef16bd30794b08f0b2000a`.**

Established:

- new **Alternative Biochemistry & Exotic Biospheres** public domain with **30 normal/public possibilities**
- composable biochemical traits: carbon-centered, water-solvent, ammonia-rich, hydrocarbon-solvent, cryogenic, silicon-centered, mineral-structural, liquid-medium-native, plus existing metabolic context
- `biochemistry` as the 36th knowledge field
- carbon-water remains the common reference, not a universal biological default
- silicon-centered biology is explicitly speculative/rare and receives no fantasy automatic superiority
- 4 shared alternative-biochemistry foundations
- 6 ammonia-rich biology/habitation/bioindustry possibilities
- 6 cryogenic-hydrocarbon biology/ecology/bioindustry possibilities
- 8 silicon/mineral-structural biology possibilities
- 6 comparative cross-biochemistry possibilities requiring legitimate alien-biology evidence
- existing causal Research Pressures reused; no unnecessary biochemical pressure meters were added
- alternative biochemical implementations can satisfy generic capabilities such as long-duration habitation, food independence, and biosphere independence
- new scoped cross-lineage capabilities for multispecies biochemical habitation, medical support, and biofabrication
- modular starting-history loader now validates **15 fragments across 2 files** and **7 reference starts across 2 files**
- human-like 2050 reference is explicitly carbon-centered + water-solvent
- ammonia-rich, cryogenic-hydrocarbon, and silicon/mineral early-space reference starts use the same base graph
- modular biochemical research-facility extension and facility index
- mixed-biochemistry civilizations keep one civilization knowledge graph plus sparse population-context applicability/capability records rather than per-population graph copies

### Biochemical applicability benchmark

- human carbon-water reference: **4** applicable domain foundations, **0** exotic native-specific nodes
- ammonia-rich reference: **10** applicable domain nodes, **6** native-specific
- cryogenic-hydrocarbon reference: **10** applicable domain nodes, **6** native-specific
- silicon/mineral reference: **12** applicable domain nodes, **8** native-specific
- pairwise native-specific Jaccard distance among the three exotic starts: **1.000** at this seed stage

### Milestone #9 validation

Final head `7730dccf89cb5548aea90f25de6ac0541cf2106d` passed:

- normal research catalog/maturation/competence/foreign-tech/start-runtime/agenda-AI validators
- long-horizon research benchmark
- biochemical structural validator
- biochemical shared-graph applicability benchmark
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

Biochemical milestone gates:

10. `validate_research_biochemistry.py`
11. `validate_research_biochemistry_benchmarks.py`

## Campaign horizon / scalability target

- demo: ~100–150 meaningful years
- initial paid Early Access: ~**500 meaningful years**
- engineering soak: at least **1,000 simulated years** without unbounded state/save/performance growth
- no hard year-based game-over

## Next Adaptive Research action

Milestone #10: define **distributed scientific knowledge and regional research continuity** for large/interstellar civilizations without creating a complete regional copy of the 360-node graph.

Goals:

- model communication delay, isolated institutions, regional expert/facility practice, censorship/archive loss, and scientific fragmentation as real causes
- keep Mature scientific knowledge distinct from where that knowledge is currently usable/maintained
- allow colonies/sectors/federations/successor states to inherit different subsets of tacit expertise and active competence after isolation or political fracture
- support later re-integration/knowledge exchange without instant magical homogenization
- preserve sparse bounded state and fair-information AI
- add deterministic fragmentation/reintegration benchmark fixtures before gameplay runtime integration
- do not promote gameplay VERSION until runtime/gameplay integration is intentionally owned and validated by the appropriate workstream
