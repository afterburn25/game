# New-Chat Handoff Protocol

This file exists so Stellar Continuum development can move between ChatGPT conversations without relying on fragile conversational memory.

## Exact bootstrap prompt for a new chat

Copy and paste this as the first project message in a new chat:

> **Open the public GitHub repository `afterburn25/stellar-continuum`. Before changing any code or design data, read `docs/CHAT_HANDOFF.md`, `docs/PROJECT_STATE.md`, `docs/WORKSTREAMS.md`, `docs/GAME_DIRECTION.md`, `docs/ENGINEERING_GUARDRAILS.md`, `docs/DEVELOPMENT_HISTORY.md`, `docs/DECISION_LOG.md`, `docs/ROADMAP.md`, `docs/BRANDING.md`, `docs/ADAPTIVE_RESEARCH_SYSTEM.md`, `docs/RESEARCH_ECONOMY.md`, `docs/RESEARCH_CAPACITY_MODEL.md`, `docs/RESEARCH_EMERGENCE_MODEL.md`, `docs/RESEARCH_MATURATION_MODEL.md`, `docs/RESEARCH_COMPETENCE_MODEL.md`, `docs/RESEARCH_FOREIGN_TECH_MODEL.md`, `docs/RESEARCH_UI_MODEL.md`, `docs/RESEARCH_START_RUNTIME_MODEL.md`, `docs/RESEARCH_AGENDA_AI_MODEL.md`, `docs/ARCHITECTURE.md`, and `docs/AI.md` from `main`. For research/technology work also read `data/research/v1/index.json`, `data/research/v1/research_economy.json`, `data/research/v1/research_capacity.json`, `data/research/v1/emergence_model.json`, `data/research/v1/applicability_traits.json`, `data/research/v1/evidence_types.json`, `data/research/v1/pressure_dynamics.json`, `data/research/v1/capability_model.json`, `data/research/v1/technology_grants.json`, `data/research/v1/maturation_model.json`, `data/research/v1/knowledge_fields.json`, `data/research/v1/research_competence_model.json`, `data/research/v1/research_facility_model.json`, `data/research/v1/tacit_knowledge_model.json`, `data/research/v1/project_readiness_model.json`, `data/research/v1/foreign_technology_model.json`, `data/research/v1/technology_exchange_model.json`, `data/research/v1/research_ui_contract.json`, `data/research/v1/starting_research_profile_contract.json`, `data/research/v1/starting_research_fragments.json`, `data/research/v1/starting_reference_profiles.json`, `data/research/v1/starting_profile_index.json`, `data/research/v1/research_runtime_contract.json`, `data/research/v1/research_view_model_contract.json`, `data/research/v1/research_agenda_model.json`, `data/research/v1/scientific_culture_model.json`, and `data/research/v1/research_ai_planning_contract.json`, plus relevant domain JSON. Treat these records as the project source of truth, then inspect current `main`, active workstream branches, open pull requests, VERSION/GameVersion, and recent validation workflow results. Tell me the authoritative validated gameplay baseline, current working title/naming status, active branch/workstream ownership, Adaptive Research catalog counts and RP + Pressure + Labs / emergence / capability / maturation / competence / foreign-tech / UI / starting-history / runtime / agenda / scientific-culture / AI-planning rules, any paused or unvalidated work, and the next intended research milestone before making changes. Do not reverse a locked design rule, modify another active chat's owned workstream without coordination, rename the project, replace Adaptive Research with a fixed universal visible tech tree, expose secret research content, add hidden research rubber-banding/AI knowledge cheats, or promote unvalidated work unless I explicitly tell you to. Then continue from the recorded state.**

## Required reload procedure

1. Read every canonical document above from `main`.
2. Read `WORKSTREAMS.md` before selecting or changing a development branch.
3. For research tasks, load the full research support-model set above and relevant domain data.
4. Inspect current `main`, version files, active branches, open PRs, and relevant CI.
5. Compare repository reality with `PROJECT_STATE.md`; report discrepancies instead of guessing.
6. State the last validated gameplay baseline separately from design/data merges.
7. State current research counts and the RP + Pressure + Labs / emergence / maturation / competence / foreign-tech / starting-history / runtime / agenda / AI rules before altering research architecture.
8. Respect branch/workstream ownership and coordinate cross-workstream interface changes rather than making competing edits.
9. Only then begin development.

## Canonical research specifications

- `ADAPTIVE_RESEARCH_SYSTEM.md` — hidden possibility-graph architecture.
- `RESEARCH_ECONOMY.md` — RP + Pressure + Labs.
- `RESEARCH_CAPACITY_MODEL.md` — lab capacity and staged parallel directed research.
- `RESEARCH_EMERGENCE_MODEL.md` — pressure/evidence/applicability-driven branch emergence.
- `RESEARCH_MATURATION_MODEL.md` — capability interoperability, maturation, hypotheses, side discoveries, deployment-vs-knowledge.
- `RESEARCH_COMPETENCE_MODEL.md` — field competence, specialist facilities, tacit expertise, Project Readiness.
- `RESEARCH_FOREIGN_TECH_MODEL.md` — foreign-tech compatibility/reproduction, exchange packages, licensing, brokerage.
- `RESEARCH_UI_MODEL.md` — evolving-tree and foreign-tech/exchange UI contract.
- `RESEARCH_START_RUNTIME_MODEL.md` — starting scientific histories, runtime event/query boundary, materialized view.
- `RESEARCH_AGENDA_AI_MODEL.md` — high-level research agenda, mutable scientific culture, natural complacency/catch-up, fair-information AI planning.

Also reload `PROJECT_STATE.md`, `WORKSTREAMS.md`, `GAME_DIRECTION.md`, `ENGINEERING_GUARDRAILS.md`, `DEVELOPMENT_HISTORY.md`, `DECISION_LOG.md`, `ROADMAP.md`, `BRANDING.md`, `ARCHITECTURE.md`, and `AI.md`.

## Durable Adaptive Research rules

- full Technology Possibility Graph is never player-visible
- civilizations materialize only plausible/known/relevant branches
- Research Labs generate RP; only explicit nodes are hard-gated by Research Pressure
- every directed project requires minimum Effective Research Labs
- early game has one directed major project; research institutions later unlock 2 -> 4 -> lab-capacity-only concurrency
- Research Pressure is sparse/contextual and never a direct speed bonus or hidden catch-up mechanism
- evidence has provenance/quality and never instantly grants technology
- applicability uses biology/capability traits, not named race IDs
- functional dependencies use cross-lineage capabilities when implementation does not matter
- research knowledge does not automatically create physical deployments/populations/institutions
- established engineering may suffer setbacks but does not randomly become physically impossible
- hypotheses can be supported/refined/disproven; disproof retains scientific value
- side discoveries stay related and pass normal rules
- field competence is theoretical + experimental + engineering; active practice can atrophy while archived knowledge persists
- specialist facilities provide eligible physical scientific capacity rather than flat percentage bonuses
- foreign/tacit expertise uses records, data, protocols, prototypes, tooling, expert cohorts, institutions, training; assimilation progresses Access -> Interpreted -> Codified -> Trained -> Native Practice
- project context uses one bounded Project Readiness, not modifier stacking
- foreign technology uses Understanding / Operability / Reproduction / Adaptation axes
- technology transfer is composed from real knowledge/physical/expert assets; licenses are law, not physics; technology has no universal fixed value
- unknown research nodes/placeholders never render; UI uses stable visible-only materialized views
- starting civilizations compose one base-era history plus reusable historical fragments, not separate species future trees
- starting compositions are prerequisite-closed and future horizons are recomputed rather than stored
- cross-workstream integration occurs through factual events, queries, capabilities, and read-only materialized views
- **research agenda controls recognized scientific attention and capacity requests, never hidden-node visibility or direct RP multiplication**
- **scientific culture is a small mutable civilization/institution behavioral state, not an immutable species research bonus**
- **natural complacency/catch-up uses perceived adequacy, real pressure, culture, and legitimately observed rivals—never hidden rank penalties or catch-up multipliers**
- **AI plans only over its visible horizon with the same blockers as the player; harder AI improves planning, not hidden knowledge/free RP/labs/evidence**
- secret/rare discovery details remain outside public research data

## Adaptive Research workstream

Persistent branch: **`dev/adaptive-research`**, owned by this dedicated Adaptive Research chat/workstream.

Other concurrent branches may consume research interfaces/capabilities but should not independently modify canonical research graph/schema files while this workstream is active. See `WORKSTREAMS.md`.

## Validation for research changes

Run the full validator stack before accepting milestone #7+ research data:

```text
python3 scripts/validate_research_catalog.py data/research/v1
python3 scripts/validate_research_maturation.py data/research/v1
python3 scripts/validate_research_competence.py data/research/v1
python3 scripts/validate_research_transfer_ui.py data/research/v1
python3 scripts/validate_research_start_runtime.py data/research/v1
python3 scripts/validate_research_agenda_ai.py data/research/v1
```

CI then runs .NET restore/build plus pinned Godot editor/runtime smoke tests.

A documentation/design-data merge does **not** promote gameplay VERSION.

## Public-repository secrecy rule

Never add exact hidden discovery triggers/probabilities, complete secret artifact chains, hidden special-AI eligibility, secret evidence catalogs, or intentionally undisclosed rare technologies to public continuity/research files.

## Short bootstrap version

> **Reload Stellar Continuum from `afterburn25/stellar-continuum` using canonical docs on `main`, especially `WORKSTREAMS.md`; for tech work reload all Adaptive Research support data including agenda/culture/AI contracts. Verify branches/PRs/CI against `PROJECT_STATE.md`, summarize the validated gameplay baseline, active workstream ownership, 330-node Adaptive Research architecture/current milestone, then continue without changing locked decisions.**
