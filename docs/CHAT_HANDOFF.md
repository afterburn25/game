# New-Chat Handoff Protocol

This file exists so Stellar Continuum can move between ChatGPT conversations without relying on fragile conversational memory.

## Exact bootstrap prompt for a new chat

> **Open the public GitHub repository `afterburn25/stellar-continuum`. Before changing code or design data, read `docs/CHAT_HANDOFF.md`, `docs/PROJECT_STATE.md`, `docs/WORKSTREAMS.md`, `docs/GAME_DIRECTION.md`, `docs/ENGINEERING_GUARDRAILS.md`, `docs/DEVELOPMENT_HISTORY.md`, `docs/DECISION_LOG.md`, `docs/ROADMAP.md`, `docs/BRANDING.md`, `docs/ADAPTIVE_RESEARCH_SYSTEM.md`, `docs/RESEARCH_ECONOMY.md`, `docs/RESEARCH_CAPACITY_MODEL.md`, `docs/RESEARCH_EMERGENCE_MODEL.md`, `docs/RESEARCH_MATURATION_MODEL.md`, `docs/RESEARCH_COMPETENCE_MODEL.md`, `docs/RESEARCH_FOREIGN_TECH_MODEL.md`, `docs/RESEARCH_UI_MODEL.md`, `docs/RESEARCH_START_RUNTIME_MODEL.md`, `docs/RESEARCH_AGENDA_AI_MODEL.md`, `docs/RESEARCH_BENCHMARK_MODEL.md`, `docs/RESEARCH_BENCHMARK_BASELINE.md`, `docs/RESEARCH_CI_KNOWN_ISSUES.md`, `docs/ARCHITECTURE.md`, and `docs/AI.md` from `main`. For research work also load all support JSON under `data/research/v1/` relevant to the task, especially the catalog/economy/capacity/emergence/applicability/evidence/pressure/capability/grant/maturation/knowledge-field/competence/facility/tacit/readiness/foreign-tech/exchange/UI/starting-history/runtime/view/agenda/culture/AI-planning/benchmark files. Inspect current `main`, `dev/adaptive-research`, open PRs, VERSION/GameVersion, CI, and open research-related issues. State the authoritative gameplay baseline separately from research design/data milestones; summarize the 330-node Adaptive Research architecture, active research milestone, branch ownership, benchmark status, and any validation caveats before making changes. Do not edit another workstream without coordination, reveal the hidden future tree, introduce hidden catch-up/rank cheats, weaken research validators merely to pass CI, expose secret rare-content details, or promote gameplay VERSION from a research-only merge. Then continue from the recorded research state.**

## Research workstream

- Persistent branch: **`dev/adaptive-research`**
- Owner: dedicated Adaptive Research / Technology chat
- Other branches consume research events/queries/capabilities; they should not independently edit canonical research schemas while this workstream is active.

## Canonical research specifications

- `ADAPTIVE_RESEARCH_SYSTEM.md`
- `RESEARCH_ECONOMY.md`
- `RESEARCH_CAPACITY_MODEL.md`
- `RESEARCH_EMERGENCE_MODEL.md`
- `RESEARCH_MATURATION_MODEL.md`
- `RESEARCH_COMPETENCE_MODEL.md`
- `RESEARCH_FOREIGN_TECH_MODEL.md`
- `RESEARCH_UI_MODEL.md`
- `RESEARCH_START_RUNTIME_MODEL.md`
- `RESEARCH_AGENDA_AI_MODEL.md`
- `RESEARCH_BENCHMARK_MODEL.md`
- `RESEARCH_BENCHMARK_BASELINE.md`
- `RESEARCH_CI_KNOWN_ISSUES.md`

## Durable Adaptive Research rules

- one shared hidden Technology Possibility Graph; never a fully visible universal tree or fixed separate species trees
- current public seed is 330 nodes / 20 domains / 59 Research Pressures / 15 alternative-solution sets / 35 knowledge fields
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
- secret/rare discovery details stay outside public research data

## Validated research milestones

- #11 possibility graph/RP+Pressure+Labs -> `f70e122134e87c1449582b573c5e2db8b045d311`
- #12 emergence/evidence/pressure -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`
- #13 capabilities/maturation -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`
- #17 competence/facilities/tacit knowledge -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`
- #30 foreign technology/exchange/UI -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`
- #44 starting histories/runtime/view -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`
- #53 agenda/scientific culture/fair AI -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`

## Current research milestone

PR **#59 — long-horizon divergence and soak benchmarks**.

Initial benchmark CI baseline:

- 500y minimum Mature-tree Jaccard distance: **0.457**
- unique Mature nodes for three same-origin divergent histories: **11 / 36 / 68**
- Mature catalog fractions: **27.6% / 31.8% / 38.2%**
- military leader attention fell **1.0**, later rose **3.0** after legitimate catch-up evidence; challenger leapfrogged without hidden catch-up
- 1000y final node-state records: **89 / 105 / 81**, with bounded recent/history buffers

These are regression/design references, not final commercial balance.

## Validation stack

```text
python3 scripts/validate_research_catalog.py data/research/v1
python3 scripts/validate_research_maturation.py data/research/v1
python3 scripts/validate_research_competence.py data/research/v1
python3 scripts/validate_research_transfer_ui.py data/research/v1
python3 scripts/validate_research_start_runtime.py data/research/v1
python3 scripts/validate_research_agenda_ai.py data/research/v1
python3 scripts/validate_research_benchmarks.py data/research/v1
```

Then .NET restore/build and shared Godot process smokes run.

## Known shared validation limitation

**GitHub issue #61** tracks a false-positive Godot runtime smoke: the process can return success while logging failure to instantiate `res://src/Game/Presentation/Main.cs`.

Until #61 is fixed, do not say the runtime is semantically healthy solely because that step is green. Report research validators/benchmark/.NET separately and describe the Godot runtime command only as a process-level smoke with the known #61 caveat.

## Public-repository secrecy rule

Never publish exact hidden discovery probabilities/triggers, secret artifact chains, hidden special-AI eligibility, secret evidence catalogs, or intentionally undisclosed rare technologies.
