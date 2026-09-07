# New-Chat Handoff Protocol

This file exists so Stellar Continuum development can move between ChatGPT conversations without relying on fragile conversational memory.

## Exact bootstrap prompt for a new chat

Copy and paste this as the first project message in a new chat:

> **Open the public GitHub repository `afterburn25/stellar-continuum`. Before changing any code or design data, read `docs/CHAT_HANDOFF.md`, `docs/PROJECT_STATE.md`, `docs/WORKSTREAMS.md`, `docs/GAME_DIRECTION.md`, `docs/ENGINEERING_GUARDRAILS.md`, `docs/DEVELOPMENT_HISTORY.md`, `docs/DECISION_LOG.md`, `docs/ROADMAP.md`, `docs/BRANDING.md`, `docs/ADAPTIVE_RESEARCH_SYSTEM.md`, `docs/RESEARCH_ECONOMY.md`, `docs/RESEARCH_CAPACITY_MODEL.md`, `docs/RESEARCH_EMERGENCE_MODEL.md`, `docs/RESEARCH_MATURATION_MODEL.md`, `docs/RESEARCH_COMPETENCE_MODEL.md`, `docs/RESEARCH_FOREIGN_TECH_MODEL.md`, `docs/RESEARCH_UI_MODEL.md`, `docs/ARCHITECTURE.md`, and `docs/AI.md` from `main`. For research/technology work also read `data/research/v1/index.json`, `data/research/v1/research_economy.json`, `data/research/v1/research_capacity.json`, `data/research/v1/emergence_model.json`, `data/research/v1/applicability_traits.json`, `data/research/v1/evidence_types.json`, `data/research/v1/pressure_dynamics.json`, `data/research/v1/capability_model.json`, `data/research/v1/technology_grants.json`, `data/research/v1/maturation_model.json`, `data/research/v1/knowledge_fields.json`, `data/research/v1/research_competence_model.json`, `data/research/v1/research_facility_model.json`, `data/research/v1/tacit_knowledge_model.json`, `data/research/v1/project_readiness_model.json`, `data/research/v1/foreign_technology_model.json`, `data/research/v1/technology_exchange_model.json`, and `data/research/v1/research_ui_contract.json`, plus relevant domain JSON. Treat these records as the project source of truth, then inspect current `main`, active workstream branches, open pull requests, VERSION/GameVersion, and recent validation workflow results. Tell me the authoritative validated gameplay baseline, current working title/naming status, active branch/workstream ownership, Adaptive Research catalog counts and RP + Pressure + Labs / emergence / capability / maturation / competence / foreign-tech / UI rules, any paused or unvalidated work, and the next intended research milestone before making changes. Do not reverse a locked design rule, modify another active chat's owned workstream without coordination, rename the project, replace Adaptive Research with a fixed universal visible tech tree, expose secret research content, or promote unvalidated work unless I explicitly tell you to. Then continue from the recorded state.**

## Required reload procedure

1. Read every canonical document above from `main`.
2. Read `WORKSTREAMS.md` before selecting or changing a development branch.
3. For research tasks, load the full research support-model set above and relevant domain data.
4. Inspect current `main`, version files, active branches, open PRs, and relevant CI.
5. Compare repository reality with `PROJECT_STATE.md`; report discrepancies instead of guessing.
6. State the last validated gameplay baseline separately from design/data merges.
7. State current research counts and the RP + Pressure + Labs / emergence / maturation / competence / foreign-tech rules before altering research architecture.
8. Respect branch/workstream ownership and coordinate cross-workstream interface changes rather than making competing edits.
9. Only then begin development.

## Canonical continuity files

- `PROJECT_STATE.md` — validated gameplay baseline, branch/design state, immediate next action.
- `WORKSTREAMS.md` — concurrent branch ownership and integration boundaries.
- `GAME_DIRECTION.md` — durable game identity/design rules.
- `ENGINEERING_GUARDRAILS.md` — scalability, persistence, bounded-state, CI rules.
- `DEVELOPMENT_HISTORY.md` — accepted milestones and important incomplete work.
- `DECISION_LOG.md` — dated durable decisions and supersessions.
- `ROADMAP.md` — public milestone direction.
- `BRANDING.md` — working title/clearance status.
- `ADAPTIVE_RESEARCH_SYSTEM.md` — hidden possibility-graph architecture.
- `RESEARCH_ECONOMY.md` — RP + Pressure + Labs.
- `RESEARCH_CAPACITY_MODEL.md` — lab capacity and staged parallel directed research.
- `RESEARCH_EMERGENCE_MODEL.md` — pressure/evidence/applicability-driven branch emergence.
- `RESEARCH_MATURATION_MODEL.md` — capability interoperability, maturation, hypotheses, side discoveries, and deployment-vs-knowledge rules.
- `RESEARCH_COMPETENCE_MODEL.md` — field competence, specialist facilities, tacit expertise, and project readiness.
- `RESEARCH_FOREIGN_TECH_MODEL.md` — foreign-tech compatibility/reproduction, exchange packages, licensing, and brokerage.
- `RESEARCH_UI_MODEL.md` — evolving-tree, project, pressure, foreign-tech, and exchange UI contract.
- `ARCHITECTURE.md` — technical architecture.
- `AI.md` — fair-information AI rules.

## Durable Adaptive Research rules

- full Technology Possibility Graph is never player-visible
- civilizations materialize only plausible/known/relevant branches
- Research Labs generate RP
- only explicitly configured nodes are hard-gated by Research Pressure
- every directed project requires minimum Effective Research Labs
- early game has one directed major project while unassigned labs continue background science
- research institution progression unlocks 2 -> 4 -> lab-capacity-limited directed programs
- Research Pressure is sparse/contextual and never rank-based rubber-banding or a direct speed bonus
- evidence has provenance/quality and never instantly grants technology
- applicability uses biology/capability traits, never named race IDs
- functional dependencies use cross-lineage capabilities when the implementation does not matter
- researching something does not automatically create infrastructure/populations/entities it merely makes possible
- ordinary engineering can suffer setbacks but does not randomly become physically impossible
- true hypotheses can be supported/refined/disproven; disproof is Archived scientific value, not a total reset
- side discoveries remain related and must pass normal applicability/prerequisite rules
- all 330 normal nodes reference canonical knowledge fields
- civilization field competence is theoretical + experimental + engineering, not one universal tech-level number
- active competence can atrophy while historical knowledge remains archived
- specialist institutions provide eligible lab/facility capabilities rather than flat research percentage bonuses
- foreign/tacit expertise is represented by records, datasets, protocols, prototypes, tooling, expert cohorts, operating institutions, and training pipelines
- foreign expertise can be assimilated Access -> Interpreted -> Codified -> Trained -> Native Practice
- project context uses one bounded readiness efficiency derived from applicable competence/facility/evidence/tacit inputs instead of independent modifier stacking
- missing hard facilities/evidence/materials block or pause a stage rather than becoming enormous opaque RP penalties
- foreign technology uses separate Understanding / Operability / Reproduction / Adaptation axes rather than one reverse-engineering percentage
- technology exchange packages are composed from actual evidence/records/hardware/tooling/experts/training/institutions and map to canonical knowledge assets
- licenses are legal constraints, not physical force fields
- technology has no universal fixed trade value; buyer-specific value depends on legitimate need, compatibility, alternatives, readiness, rights, dependencies, risk, scarcity, and known demand
- unknown research nodes and placeholder slots never render in the player UI
- new visible branches preserve stable layout anchors/viewport where possible
- foreign-tech UI shows the four assessment axes separately; exchange UI separates contents, rights, and technical ability
- secret/rare discovery details remain outside public research data

## Adaptive Research workstream

The persistent research branch is **`dev/adaptive-research`**, owned by the dedicated Adaptive Research chat/workstream.

Other concurrent branches may consume research interfaces/capabilities but should not independently modify canonical research graph/schema files while this workstream is active. See `WORKSTREAMS.md`.

## Validation for research changes

Run all four before accepting milestone #5+ research data:

```text
python3 scripts/validate_research_catalog.py data/research/v1
python3 scripts/validate_research_maturation.py data/research/v1
python3 scripts/validate_research_competence.py data/research/v1
python3 scripts/validate_research_transfer_ui.py data/research/v1
```

CI then runs normal .NET and Godot gates.

A documentation/design-data merge does **not** promote gameplay VERSION.

## Public-repository secrecy rule

Never add exact hidden discovery triggers/probabilities, complete secret artifact chains, hidden special-AI eligibility, secret evidence catalogs, or intentionally undisclosed rare technologies to public continuity/research files.

## Short bootstrap version

> **Reload Stellar Continuum from `afterburn25/stellar-continuum` using canonical docs on `main`, especially `WORKSTREAMS.md`; for tech work reload all Adaptive Research support data in `data/research/v1/`. Verify branches/PRs/CI against `PROJECT_STATE.md`, summarize the validated gameplay baseline, title, active workstream ownership, 330-node Adaptive Research/RP+Pressure+Labs/emergence/maturation/competence/foreign-tech/UI rules, and continue without changing locked decisions.**
