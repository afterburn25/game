# New-Chat Handoff Protocol

This file exists so Stellar Continuum can move between ChatGPT conversations without relying on fragile conversational memory.

## Exact bootstrap prompt for a new chat

> **Open the public GitHub repository `afterburn25/stellar-continuum`. Before changing code or design data, read `docs/CHAT_HANDOFF.md`, `docs/PROJECT_STATE.md`, `docs/WORKSTREAMS.md`, `docs/GAME_DIRECTION.md`, `docs/ENGINEERING_GUARDRAILS.md`, `docs/DEVELOPMENT_HISTORY.md`, `docs/DECISION_LOG.md`, `docs/ROADMAP.md`, `docs/BRANDING.md`, all canonical Adaptive Research specifications, especially `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, `RESEARCH_EMERGENCE_MODEL.md`, `RESEARCH_MATURATION_MODEL.md`, `RESEARCH_COMPETENCE_MODEL.md`, `RESEARCH_FOREIGN_TECH_MODEL.md`, `RESEARCH_UI_MODEL.md`, `RESEARCH_START_RUNTIME_MODEL.md`, `RESEARCH_AGENDA_AI_MODEL.md`, `RESEARCH_BENCHMARK_MODEL.md`, `RESEARCH_BENCHMARK_BASELINE.md`, `RESEARCH_BIOCHEMISTRY_MODEL.md`, `RESEARCH_BIOCHEMISTRY_MULTISPECIES.md`, `RESEARCH_BIOCHEMISTRY_BENCHMARK.md`, `RESEARCH_CI_KNOWN_ISSUES.md`, plus `ARCHITECTURE.md` and `AI.md` from `main`. For research work also load all support JSON under `data/research/v1/`, especially the catalog/index, economy/capacity/emergence, applicability/evidence/pressure, capability/grant/maturation, knowledge-field/competence/facility/tacit/readiness, foreign-tech/exchange/UI, starting-history/runtime/view, agenda/culture/AI-planning, benchmark, alternative-biochemistry, biochemical applicability/facility/start/benchmark files. Inspect current `main`, `dev/adaptive-research`, open PRs, VERSION/GameVersion, CI, and open research-related issues. State the authoritative gameplay baseline separately from research design/data milestones; summarize the **360-node / 21-domain** Adaptive Research architecture, current research milestone, branch ownership, benchmark status, biochemical/multispecies rules, and validation caveats before making changes. Do not edit another workstream without coordination, reveal the hidden future tree, introduce hidden catch-up/rank cheats, create race-specific fixed tech trees, weaken validators merely to pass CI, expose secret rare-content details, or promote gameplay VERSION from a research-only merge. Then continue from the recorded research state.**

## Research workstream

- Persistent branch: **`dev/adaptive-research`**
- Owner: dedicated Adaptive Research / Technology chat
- Other branches consume research events/queries/capabilities; they should not independently edit canonical research schemas while this workstream is active.

## Current public research seed

- **360 nodes**
- **21 domains**
- **59 Research Pressures**
- **16 alternative-solution sets**
- **14 applicability traits**
- **9 evidence types**
- **36 knowledge fields**
- **17 cross-lineage capabilities**

## Durable Adaptive Research rules

- one shared hidden Technology Possibility Graph; never a fully visible universal tree or fixed separate species trees
- RP comes from physical Effective Research Labs; Pressure is contextual and only a hard gate where explicitly configured
- early directed concurrency 1 -> 2 -> 4 -> lab-capacity-only
- evidence/applicability/capability/pressure/prerequisite indexes expose only legitimately reachable current candidates
- functional dependencies use cross-lineage capabilities when implementation does not matter
- knowledge does not magically instantiate physical infrastructure/populations
- field competence = theory / experiment / engineering; specialist facilities and tacit expertise matter
- Project Readiness is one bounded context efficiency, not stacks of `+research%`
- foreign technology has separate Understanding / Operability / Reproduction / Adaptation states
- technology exchange is composed from real knowledge/data/hardware/tooling/experts/institutions; legal rights != technical ability; licenses are law, not physics; value is buyer-specific
- unknown nodes/placeholder slots never render; UI consumes a visible-only revision-cached materialized view
- starting civilizations compose one base-era history plus reusable history fragments; future trees are recomputed, not stored
- agenda changes recognized scientific attention and capacity requests, never direct RP or hidden-node visibility
- scientific culture is mutable institutional behavior, not an immutable species bonus
- natural complacency/catch-up comes from perceived adequacy, real need, culture, and legitimate observations—never hidden rank penalties or catch-up multipliers
- AI plans only over its visible horizon with the same authoritative blockers as the player; harder AI improves planning, not hidden knowledge/free RP/labs/evidence
- runtime remains sparse/event-index driven; offline benchmark tooling may scan the public graph but is not gameplay runtime
- biochemical identity is composable population context, not a race ID
- carbon-water is common reference, not universal biological default
- ammonia-rich, cryogenic-hydrocarbon, and silicon/mineral biological lineages occupy the same shared graph through applicability traits
- silicon-centered life remains speculative/rare and receives no fantasy automatic superiority
- one civilization may contain multiple incompatible biochemical populations
- mature biochemical knowledge can be civilization-level while operational applicability/capabilities remain sparse population-context state
- migration/federation/conquest/uplift can add new applicability context/tacit expertise but never instant technology simply because ownership changed
- secret/rare discovery details stay outside public research data

## Validated research milestones

- #11 possibility graph/RP+Pressure+Labs -> `f70e122134e87c1449582b573c5e2db8b045d311`
- #12 emergence/evidence/pressure -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`
- #13 capabilities/maturation -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`
- #17 competence/facilities/tacit knowledge -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`
- #30 foreign technology/exchange/UI -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`
- #44 starting histories/runtime/view -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`
- #53 agenda/scientific culture/fair AI -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`
- #59 long-horizon divergence/state soak -> `d3916d3e6551c7a8b606716858d2d782581b1dac`
- #63 alternative biochemistry/exotic biospheres/multispecies applicability -> `1e69fa65c2ec0df2feef16bd30794b08f0b2000a`

Research design/data milestones do not promote gameplay VERSION.

## Current benchmark status

After the 360-node expansion:

- 500y minimum Mature-tree Jaccard distance: **0.457**
- Mature catalog fractions: **25.3% / 29.2% / 35.0%**
- unique Mature nodes for three same-origin divergent histories: **11 / 36 / 68**
- 1000y final node-state records: **89 / 105 / 81**, with bounded recent/history buffers
- military complacency/catch-up scenario still allows challenger leapfrog and leader re-attention without hidden catch-up

Biochemical applicability benchmark:

- human carbon-water: 4 shared domain nodes / 0 exotic native-specific
- ammonia-rich: 10 / 6
- cryogenic-hydrocarbon: 10 / 6
- silicon/mineral: 12 / 8
- pairwise native-specific Jaccard distance among exotic starts: **1.000** at this seed stage

These are regression/design references, not final commercial balance.

## Validation stack

Core:

```text
python3 scripts/validate_research_catalog.py data/research/v1
python3 scripts/validate_research_maturation.py data/research/v1
python3 scripts/validate_research_competence.py data/research/v1
python3 scripts/validate_research_transfer_ui.py data/research/v1
python3 scripts/validate_research_start_runtime.py data/research/v1
python3 scripts/validate_research_agenda_ai.py data/research/v1
python3 scripts/validate_research_benchmarks.py data/research/v1
```

Biochemical gates:

```text
python3 scripts/validate_research_biochemistry.py data/research/v1
python3 scripts/validate_research_biochemistry_benchmarks.py data/research/v1
```

Then .NET restore/build and shared Godot process smokes run.

## Known shared validation limitation

**GitHub issue #61** tracks a false-positive Godot runtime smoke: the process can return success while logging failure to instantiate `res://src/Game/Presentation/Main.cs`.

Until #61 is fixed, do not say the runtime is semantically healthy solely because that step is green. Report research validators/benchmarks/.NET separately and describe the Godot runtime command only as a process-level smoke with the known #61 caveat.

## Current research milestone

**Milestone #10 — distributed scientific knowledge and regional research continuity.**

Design this without a complete per-region copy of the 360-node graph. Model communication delay, isolated institutions, regional competence/tacit practice, censorship/archive loss, political fracture, successor-state knowledge inheritance, and later reintegration using sparse bounded context state.

Add deterministic fragmentation/reintegration benchmark fixtures before gameplay runtime integration.

## Public-repository secrecy rule

Never publish exact hidden discovery probabilities/triggers, secret artifact chains, hidden special-AI eligibility, secret evidence catalogs, or intentionally undisclosed rare technologies.
