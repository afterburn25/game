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
- owner: this dedicated Adaptive Research / Technology chat
- scope: possibility graph, RP/Pressure/Labs, emergence/evidence/applicability, capabilities/maturation, competence/facilities/tacit knowledge, foreign tech/exchange, starting histories/runtime/view contracts, agenda/scientific culture/fair AI planning, long-run research benchmarks, research-facing UI/contracts/validation

Other workstreams may consume research events/queries/capabilities but should not independently edit canonical research graph/schema files without coordination.

## Durable research rules

- hidden shared Technology Possibility Graph; player/AI never see the complete future tree
- current public seed: **330 nodes / 20 domains / 59 pressures / 15 alternative-solution sets / 6 applicability traits / 9 evidence types / 35 knowledge fields**
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
- secret/rare discovery details remain outside public research data

## Validated Adaptive Research milestones

1. **PR #11** -> `f70e122134e87c1449582b573c5e2db8b045d311`: possibility graph + RP/Pressure/Labs + catalog validator.
2. **PR #12** -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`: emergence/evidence/pressure dynamics.
3. **PR #13** -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`: capability interoperability + maturation/hypotheses/setbacks.
4. **PR #17** -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`: 35 fields + competence + specialist facilities + tacit expertise + Project Readiness.
5. **PR #30** -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`: foreign-tech axes + exchange/licensing/brokerage + evolving visible-only UI.
6. **PR #44** -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`: composable starting histories + runtime/event-query contract + materialized research view + fifth validator. A history-only conflict was resolved with two-parent commit `93930301055cbf0e0c2cd15c1733fecb7b853e12`, then revalidated before merge.
7. **PR #53** -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`: research agenda + 12-axis mutable scientific culture + natural complacency/catch-up + fair-information AI planning + sixth validator.

None of these design/data milestones promoted gameplay VERSION.

## Milestone #8 — long-horizon divergence / research-state soak

**Current PR: #59 — Adaptive Research long-horizon divergence and soak benchmarks.**

New research-only tooling/data:

- `research_benchmark_scenarios.json`
- `validate_research_benchmarks.py`
- `RESEARCH_BENCHMARK_MODEL.md`
- `RESEARCH_BENCHMARK_BASELINE.md`
- `RESEARCH_BENCHMARK_RESULTS_V1.json`
- `RESEARCH_BENCHMARK_NOTES.md`
- `RESEARCH_CI_KNOWN_ISSUES.md`

The benchmark harness is explicitly an **offline design/CI tool**, not gameplay runtime. It may scan the 330 public nodes; production runtime remains event/index-driven.

### First passing benchmark baseline

CI run `34163593221`, job `101870178023`:

**500-year same-origin divergence**
- minimum pairwise Mature-tree Jaccard distance: **0.457**
- Mature catalog fractions: orbital industrialist **0.276**, defense engineer **0.318**, biosphere adaptor **0.382**
- unique Mature nodes: orbital industrialist **11**, defense engineer **36**, biosphere adaptor **68**
- common Mature nodes: **47**

**350-year military complacency / response**
- initial hegemon lead: **10.85** benchmark units
- gap at year 180: **-12.65** (challenger leapfrogged)
- leader military attention fell to **1.0** during perceived adequacy and later rose to **3.0** after a legitimate catch-up observation
- final benchmark military scores: leader **18.55**, challenger **33.40**
- no hidden catch-up multiplier, leader penalty, forced parity, or guaranteed comeback

**Foreign-tech asymmetric value**
- synthetic holder metabolic compatibility: false
- metabolic buyer compatibility: true
- package components: 3
- brokerage value possible without direct holder operability or instant native maturity

**1,000-year state soak**
- Generalist A: 89 node states / 124 recent events / 64 detailed history
- Generalist B: 105 / 156 / 80
- Specialist C: 81 / 108 / 56
- all below hard reference bounds: 330 node states, 256 recent events, 512 detailed history per civilization

These are regression/design guardrails, not final Early Access balance.

## Known shared CI limitation — issue #61

Adaptive Research discovered that the current Godot runtime smoke command can exit successfully while logging:

`Cannot instantiate C# script ... res://src/Game/Presentation/Main.cs`

Tracked in **GitHub issue #61 — CI runtime smoke is false-positive on Main.cs C# instantiation error**.

This is outside Adaptive Research ownership. Until #61 is fixed, research PRs may say the Godot runtime **process step completed**, but must not claim runtime semantic health solely from that step. Research-owned acceptance relies on research validators/benchmarks, .NET build, and research-only changed-file audit, with the shared Godot limitation explicitly recorded.

## Research validation stack

1. `validate_research_catalog.py`
2. `validate_research_maturation.py`
3. `validate_research_competence.py`
4. `validate_research_transfer_ui.py`
5. `validate_research_start_runtime.py`
6. `validate_research_agenda_ai.py`
7. `validate_research_benchmarks.py` (offline long-horizon design benchmark)
8. .NET restore/build
9. shared Godot editor/runtime process smokes (runtime semantic limitation tracked by #61)

## Campaign horizon / scalability target

- demo: ~100–150 meaningful years
- initial paid Early Access: ~**500 meaningful years**
- engineering soak: at least **1,000 simulated years** without unbounded state/save/performance growth
- no hard year-based game-over

## Next Adaptive Research action

1. finish PR #59 final continuity/validation and merge if all research gates/benchmarks/.NET remain green; report Godot runtime step with issue #61 caveat
2. continue on `dev/adaptive-research`
3. next catalog-content milestone: expand biochemical/species diversity without race-specific trees—unusual-solvent carbon biology, cryogenic/hydrocarbon biospheres, silicon-centered speculative biology, alternative habitat/medicine/material/industry branches, and cross-biochemistry compatibility science
4. rerun divergence/soak benchmarks after node-count expansion and intentionally update counts/baselines
5. do not add secret rare-content details to the public graph and do not promote gameplay VERSION until actual runtime integration is implemented by the appropriate workstream and validated
