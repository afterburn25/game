# New-Chat Handoff Protocol

This file exists so Stellar Continuum development can move between ChatGPT conversations without relying on fragile conversational memory.

## Exact bootstrap prompt for a new chat

Copy and paste this as the first project message in a new chat:

> **Open the public GitHub repository `afterburn25/stellar-continuum`. Before changing any code or design data, read `docs/CHAT_HANDOFF.md`, `docs/PROJECT_STATE.md`, `docs/WORKSTREAMS.md`, `docs/GAME_DIRECTION.md`, `docs/ENGINEERING_GUARDRAILS.md`, `docs/DEVELOPMENT_HISTORY.md`, `docs/DECISION_LOG.md`, `docs/ROADMAP.md`, `docs/BRANDING.md`, `docs/ADAPTIVE_RESEARCH_SYSTEM.md`, `docs/RESEARCH_ECONOMY.md`, `docs/RESEARCH_CAPACITY_MODEL.md`, `docs/RESEARCH_EMERGENCE_MODEL.md`, `docs/RESEARCH_MATURATION_MODEL.md`, `docs/ARCHITECTURE.md`, and `docs/AI.md` from `main`. For research/technology work also read `data/research/v1/index.json`, `data/research/v1/research_economy.json`, `data/research/v1/research_capacity.json`, `data/research/v1/emergence_model.json`, `data/research/v1/applicability_traits.json`, `data/research/v1/evidence_types.json`, `data/research/v1/pressure_dynamics.json`, `data/research/v1/capability_model.json`, `data/research/v1/technology_grants.json`, and `data/research/v1/maturation_model.json`, plus relevant domain JSON. Treat these records as the project source of truth, then inspect current `main`, active workstream branches, open pull requests, VERSION/GameVersion, and recent validation workflow results. Tell me the authoritative validated gameplay baseline, current working title/naming status, active branch/workstream ownership, Adaptive Research catalog counts and RP + Pressure + Labs/emergence/maturation rules, any paused/unvalidated work, and the next intended milestone before making changes. Do not reverse a locked design rule, modify another active chat's owned workstream without coordination, rename the project, replace Adaptive Research with a fixed universal visible tech tree, expose secret research content, or promote unvalidated work unless I explicitly tell you to. Then continue from the recorded state.**

## Required reload procedure

1. Read every canonical document above from `main`.
2. Read `WORKSTREAMS.md` before selecting or changing a development branch.
3. For research tasks, load the research index/economy/capacity/emergence/trait/evidence/pressure/capability/grant/maturation files and relevant domain data.
4. Inspect current `main`, version files, active branches, open PRs, and relevant CI.
5. Compare repository reality with `PROJECT_STATE.md`; report discrepancies rather than guessing.
6. State the last validated gameplay baseline separately from design/data merges.
7. State current research counts and the RP + Pressure + Labs / emergence / maturation rules before altering research architecture.
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
- `RESEARCH_MATURATION_MODEL.md` — capability interoperability, maturation, setbacks, hypotheses, side discoveries, and deployment-vs-knowledge rules.
- `ARCHITECTURE.md` — technical architecture.
- `AI.md` — fair-information AI rules.

## Research source of truth

Machine-readable research data lives under `data/research/v1/`.

Durable rules:

- the full Technology Possibility Graph is never player-visible
- civilizations materialize only currently plausible/known/relevant branches
- Research Labs generate RP
- only explicitly configured nodes are hard-gated by Research Pressure
- every directed project requires minimum Effective Research Labs
- early game has one directed major project while unassigned labs continue background science
- `coordinated_research_networks` -> 2 directed programs
- `distributed_scientific_portfolios` -> 4
- `autonomous_research_portfolios` -> no artificial slot ceiling; lab capacity is the practical limit
- Research Pressure is sparse/event or low-frequency metric driven and never rank-based rubber-banding
- evidence is stored as campaign instances with provenance/quality and never instantly grants technology
- applicability uses capability/biology traits, never named race IDs
- population/species traits apply to the relevant population; mutable civilization traits can be acquired through technological history and real deployment state
- foreign/enemy-relative pressure requires legitimate observations
- implementation-specific prerequisites represent actual knowledge lineage; generic functional requirements use cross-lineage capabilities
- node capability outputs normally become available at Mature; explicit technology-grant rules can expose selected capabilities earlier when a demonstrated prototype genuinely provides them
- researching something does not automatically create infrastructure/populations/entities it merely makes possible; deployment events can be required after research
- ordinary engineering can suffer setbacks but does not randomly become physically impossible
- true hypotheses can be supported/refined/disproven; disproof is Archived scientific knowledge, not a zero-value reset
- side discoveries may reveal evidence/hypotheses/competence but never bypass normal applicability/prerequisite rules or hand out unrelated mature tech
- secret/rare discovery details remain outside public research data

## Adaptive Research workstream

The persistent research branch is **`dev/adaptive-research`** and is owned by the dedicated Adaptive Research chat/workstream.

Other concurrent branches may consume research interfaces/capabilities but should not independently modify canonical research graph/schema files while this workstream is active. See `WORKSTREAMS.md`.

## Update protocol

After gameplay milestone merges, update project state/history/roadmap/version-facing docs.

After major design changes, update the relevant canonical specification and Decision Log.

After research-data changes:

- keep static counts/references synchronized
- run `python3 scripts/validate_research_catalog.py data/research/v1`
- run `python3 scripts/validate_research_maturation.py data/research/v1`
- update relevant research specifications
- keep validation in CI

A documentation/design-data merge does **not** promote the gameplay version.

## Public-repository secrecy rule

Never add exact hidden discovery triggers/probabilities, complete secret artifact chains, hidden special-AI eligibility, secret evidence catalogs, or intentionally undisclosed rare technologies to public continuity/research files.

## Short bootstrap version

> **Reload Stellar Continuum from `afterburn25/stellar-continuum` using the canonical docs on `main`, especially `WORKSTREAMS.md`; for tech work also reload all Adaptive Research support data in `data/research/v1/`. Verify branches/PRs/CI against `PROJECT_STATE.md`, summarize the validated gameplay baseline, title, active workstream ownership, 330-node Adaptive Research/RP+Pressure+Labs/emergence/maturation rules, and paused work, then continue without changing locked decisions.**
