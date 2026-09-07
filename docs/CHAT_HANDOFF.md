# New-Chat Handoff Protocol

This file lets Stellar Continuum development move between chats without relying on conversational memory.

## Bootstrap prompt for a new Adaptive Research chat

> **Open public repo `afterburn25/stellar-continuum`. Before changing code/data, read `docs/CHAT_HANDOFF.md`, `docs/PROJECT_STATE.md`, `docs/WORKSTREAMS.md`, `docs/DEVELOPMENT_HISTORY.md`, `docs/DECISION_LOG.md`, `docs/GAME_DIRECTION.md`, `docs/ENGINEERING_GUARDRAILS.md`, `docs/ROADMAP.md`, `docs/BRANDING.md`, `docs/ARCHITECTURE.md`, `docs/AI.md`, and all Adaptive Research specifications on `main`: `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, `RESEARCH_EMERGENCE_MODEL.md`, `RESEARCH_MATURATION_MODEL.md`, `RESEARCH_COMPETENCE_MODEL.md`, `RESEARCH_FOREIGN_TECH_MODEL.md`, `RESEARCH_UI_MODEL.md`, `RESEARCH_START_RUNTIME_MODEL.md`, `RESEARCH_AGENDA_AI_MODEL.md`, `RESEARCH_BENCHMARK_MODEL.md`, `RESEARCH_BENCHMARK_BASELINE.md`, `RESEARCH_BIOCHEMISTRY_MODEL.md`, `RESEARCH_BIOCHEMISTRY_MULTISPECIES.md`, `RESEARCH_BIOCHEMISTRY_BENCHMARK.md`, `RESEARCH_DISTRIBUTED_CONTINUITY_MODEL.md`, `RESEARCH_SECRECY_MODEL.md`, and `RESEARCH_CI_KNOWN_ISSUES.md`. Also load relevant `data/research/v1/` JSON, especially the base catalog/support models plus biochemical, distributed-continuity, and secrecy models/runtime extensions/scenarios. Inspect current `main`, `dev/adaptive-research`, open PRs/issues, VERSION/GameVersion, and CI. State the validated gameplay baseline separately from research design/data milestones; summarize the **360-node / 21-domain** research architecture, benchmark status, biochemical/multispecies rules, distributed-knowledge rules, secrecy rules, branch ownership, current research milestone, and #61 CI caveat before making changes. Do not reveal hidden future research, create fixed species trees, add hidden catch-up/rank cheats, duplicate the graph per region/population/security compartment, weaken validators just to pass CI, edit another workstream without coordination, expose secret rare-content details, or promote gameplay VERSION from research-only work. Then continue from recorded state.**

## Workstream

- Branch: **`dev/adaptive-research`**
- Owner: dedicated Adaptive Research / Technology chat
- Other workstreams consume stable research events/queries/capabilities; they should not independently edit canonical research schemas while this workstream is active.

## Public seed

- **360 nodes / 21 domains / 59 Pressures / 16 alternative-solution sets / 14 applicability traits / 9 evidence types / 36 knowledge fields / 17 cross-lineage capabilities**

## Non-negotiable architecture

- one hidden shared Technology Possibility Graph; no fully visible universal tree and no giant fixed species trees
- RP from physical Effective Research Labs; Pressure contextual and only a hard gate where explicitly configured
- directed concurrency 1 -> 2 -> 4 -> lab-capacity-only through actual institutional development
- functional dependencies use cross-lineage capabilities when implementation does not matter
- scientific knowledge != physical deployment
- competence = theory / experiment / engineering; facilities/tacit expertise matter; Project Readiness is bounded
- foreign tech = Understanding / Operability / Reproduction / Adaptation; acquisition never instantly matures a native node
- technology exchange uses actual records/data/hardware/tooling/experts/training/institutions; legal rights != technical ability
- UI/AI only see legitimate current horizon; unknown placeholders never render
- starting civilizations compose history fragments; future tree is recomputed
- agenda/scientific culture guides attention/capacity requests, not direct RP or hidden visibility
- fair AI receives better planning, never hidden graph/enemy tech/free RP/labs/evidence
- runtime sparse/event-indexed; no full-graph per-tick or per-frame scan
- biochemical traits are composable population context; carbon-water common but not universal; ammonia/cryogenic-hydrocarbon/silicon-mineral share the same graph
- multiple biochemical populations can coexist in one civilization without graph copies
- scientific truth, local codified access, local active practice, and deployment are separate
- regional research contexts exist only for material divergence; normal synchronized colonies have no explicit context
- records/data obey real communication paths/latency; experts/tooling/prototypes/institutions do not teleport as data
- no universal distance research penalty
- successor states inherit factual local archives/assets/expertise, not full former-polity tech lists
- classification/security policy applies to records/projects/assets, not physics or maturity
- classification has no direct RP multiplier; any slowdown comes from actual authorized-capacity/validation/compartment/communications constraints
- classified deployed effects can still be legitimately observed
- reclassification cannot recall already distributed copies or un-leak records
- factual compromise routes through evidence/tacit/foreign-tech; never instant native maturity; owners do not know undetected leaks
- Intelligence/Security owns espionage/theft/interception/compromise detection/protection; Research owns research-side access/dissemination/assimilation consequences
- secret/rare discovery details remain out of public data

## Validated milestones

- #11 graph/RP/Pressure/Labs -> `f70e122134e87c1449582b573c5e2db8b045d311`
- #12 emergence/evidence/pressure -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`
- #13 capabilities/maturation -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`
- #17 competence/facilities/tacit -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`
- #30 foreign tech/exchange/UI -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`
- #44 starting histories/runtime/view -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`
- #53 agenda/culture/fair AI -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`
- #59 long-horizon benchmarks -> `d3916d3e6551c7a8b606716858d2d782581b1dac`
- #63 alternative biochemistry/multispecies -> `1e69fa65c2ec0df2feef16bd30794b08f0b2000a`
- #73 distributed scientific continuity -> `6c832fc07359ebb4fbe40c4dc079f19e13c11fca`
- #78 research secrecy/compartmentalization/compromise -> **`172c9c364b161e2e3deac88337429b5880a34e68`**

Research-only milestones do not promote gameplay VERSION.

## Baseline benchmarks

Long-horizon:
- 500y Mature-tree minimum Jaccard **0.457**
- Mature fractions **25.3% / 29.2% / 35.0%**
- unique Mature nodes **11 / 36 / 68**
- core 1000y node-state counts **89 / 105 / 81**

Biochemistry:
- human 4 shared/0 exotic-native; ammonia 10/6; cryogenic hydrocarbon 10/6; silicon/mineral 12/8
- exotic native-specific pairwise Jaccard **1.000**

Distributed continuity:
- 1000y / 120 regions peaks: **5 contexts / 17 node-access exceptions / 10 field-practice exceptions / 9 pending transmissions**; final contexts 3
- successor-state benchmark Jaccard **0.474**

Secrecy:
- restricted program: 48 scientifically eligible labs, 14 authorized, secrecy multiplier **1.0**
- compartment blocker removed only after required integration access exists
- classified deployed capability: foreign Understanding Unknown -> Observed, Reproduction None, records acquired 0
- partial compromise: Characterized + Component Replication, native maturity false
- declassification: contexts 2 -> 6 and archive copies 2 -> 6 after real delivery, maturity unchanged
- reclassification: 4 existing copies remain 4 despite policy narrowing to 2 authorized contexts
- 1000y security soak peaks: **18 nondefault security records / 6 compartments / 3 known compromise assessments**

## Validation

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

Specialized:

```text
python3 scripts/validate_research_biochemistry.py data/research/v1
python3 scripts/validate_research_biochemistry_benchmarks.py data/research/v1
python3 scripts/validate_research_distributed_continuity.py data/research/v1
python3 scripts/validate_research_distributed_continuity_benchmarks.py data/research/v1
python3 scripts/validate_research_secrecy.py data/research/v1
python3 scripts/validate_research_secrecy_benchmarks.py data/research/v1
```

Then .NET restore/build plus shared Godot process smokes.

## Known CI limitation

**Issue #61:** shared Godot runtime smoke can return success while logging inability to instantiate `res://src/Game/Presentation/Main.cs`. Until fixed, do not claim semantic runtime health from that step alone.

## Current milestone

**Milestone #12 — cross-polity joint research and scientific collaboration.**

Research owns scientific contribution/result/access semantics. Diplomacy owns negotiating/creating/canceling agreements, payments/obligations, trust and political consequences.

Required principles:

- no treaty-based `+research%`
- real contributed labs/facilities/experts/data/materials/contexts only
- contributions can be unequal
- real communications/distributed-continuity constraints apply
- secrecy/classification/compartments may apply
- result access can be asymmetric due to rights, compatibility, facilities, materials or tacit expertise
- withdrawal removes real contributed capacity/assets; already delivered records cannot be magically recalled
- actual participating work may build competence; absent partners do not gain practice by treaty
- project never merges whole trees or exposes unknown partner nodes
- fair-information AI
- deterministic collaboration/withdrawal/asymmetric-result/classified-program/1000y bounded-state benchmarks required before merge

## Public-repository secrecy rule

Never publish exact hidden discovery probabilities/triggers, secret artifact chains, hidden special-AI eligibility, secret evidence catalogs, or intentionally undisclosed rare technologies.
