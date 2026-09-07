# New-Chat Handoff Protocol

This file exists so Stellar Continuum development can move between ChatGPT conversations without relying on fragile conversational memory.

## Exact bootstrap prompt for a new chat

Copy and paste this as the first project message in a new chat:

> **Open the public GitHub repository `afterburn25/stellar-continuum`. Before changing any code or design data, read `docs/CHAT_HANDOFF.md`, `docs/PROJECT_STATE.md`, `docs/GAME_DIRECTION.md`, `docs/ENGINEERING_GUARDRAILS.md`, `docs/DEVELOPMENT_HISTORY.md`, `docs/DECISION_LOG.md`, `docs/ROADMAP.md`, `docs/BRANDING.md`, `docs/ADAPTIVE_RESEARCH_SYSTEM.md`, `docs/RESEARCH_ECONOMY.md`, `docs/RESEARCH_CAPACITY_MODEL.md`, `docs/ARCHITECTURE.md`, and `docs/AI.md` from `main`. Also read `data/research/v1/index.json`, `data/research/v1/research_economy.json`, and `data/research/v1/research_capacity.json` when the task involves research/technology. Treat these records as the project source of truth, then inspect the current `main` branch, active development/design branches, open pull requests, VERSION/GameVersion, and recent validation workflow results. Tell me the authoritative validated gameplay baseline, current working title/naming status, adaptive-research catalog version/counts and RP + Pressure + Labs rules, any paused/unvalidated work, and the next intended milestone before you make changes. Do not reverse a locked design rule, rename the project, replace Adaptive Research with a fixed universal visible tech tree, or promote unvalidated work unless I explicitly tell you to. Then continue from the recorded state.**

## Required reload procedure

When the bootstrap prompt is received:

1. Fetch this file from `main`.
2. Read every canonical document listed above.
3. For research tasks, read the research index/economy/capacity files and inspect the relevant domain JSON.
4. Inspect the current `main` commit and version files.
5. Inspect active `dev/**`, `docs/**`, or other relevant branches.
6. Inspect open PRs and validation status where relevant.
7. Compare repository reality with `PROJECT_STATE.md`.
8. Report discrepancies instead of guessing.
9. State which gameplay version is the last validated baseline and which work is merely design/in-progress/paused/unvalidated.
10. State the current research model and catalog counts before changing research architecture.
11. Only then begin development.

## Canonical continuity files

- `PROJECT_STATE.md` — current authoritative baseline, branch status, research state, and next action.
- `GAME_DIRECTION.md` — durable game identity and design rules.
- `ENGINEERING_GUARDRAILS.md` — scalability, bounded-memory, persistence, CI, and long-campaign rules.
- `DEVELOPMENT_HISTORY.md` — validated milestone history and important incomplete work.
- `DECISION_LOG.md` — dated decisions and why they were made.
- `ROADMAP.md` — public milestone direction.
- `BRANDING.md` — working title and commercial-clearance status.
- `ADAPTIVE_RESEARCH_SYSTEM.md` — evolving hidden possibility-graph architecture.
- `RESEARCH_ECONOMY.md` — RP + Pressure + Labs player-facing model.
- `RESEARCH_CAPACITY_MODEL.md` — lab capacity and staged parallel directed research.
- `ARCHITECTURE.md` — technical architecture.
- `AI.md` — fair-information AI rules.

## Research source of truth

For technology/research work, the machine-readable source is `data/research/v1/`.

Current design principles:

- the player never sees the complete Technology Possibility Graph
- visible trees materialize from current knowledge, conditions, evidence, species applicability, and scientific history
- Research Labs generate RP
- only explicitly configured technologies are hard-gated by Research Pressure
- every directed project needs minimum allocated Effective Research Labs
- early game supports one directed major project while unassigned labs continue background science
- `coordinated_research_networks` unlocks 2 directed programs
- `distributed_scientific_portfolios` unlocks 4
- `autonomous_research_portfolios` removes the artificial slot ceiling and leaves lab capacity as the practical limit
- foreign technology is not an instant unlock
- secret/rare discovery details are excluded from public research data

## Update protocol

After a validated gameplay milestone:

- update `PROJECT_STATE.md`
- append `DEVELOPMENT_HISTORY.md`
- update roadmap/README/version-facing docs as appropriate

After a major accepted design decision:

- update the relevant canonical specification
- append/update `DECISION_LOG.md`
- update `PROJECT_STATE.md` if immediate implementation direction changes

After research-catalog changes:

- update `data/research/v1/index.json`
- run `python3 scripts/validate_research_catalog.py data/research/v1`
- update Adaptive Research/Research Economy/Capacity docs if architecture or counts change
- keep CI validation enabled

After naming/branding changes:

- update `BRANDING.md`, `DECISION_LOG.md`, `PROJECT_STATE.md`, and player-facing docs as appropriate

## Validation discipline

Preserve the distinction between:

- idea/design direction
- design data
- in-progress source
- prepared development branch
- CI-validated candidate
- merged authoritative gameplay baseline

A documentation/design-data merge does not by itself promote a gameplay version.

## Public-repository secrecy rule

Never add exact hidden discovery triggers, exact rare probabilities, complete secret artifact chains, secret special-AI eligibility, or intentionally undisclosed rare technologies to the public continuity/research files.

## Short bootstrap version

> **Reload Stellar Continuum from `afterburn25/stellar-continuum` using the canonical `docs/` records on `main`; for tech work also reload `data/research/v1/`. Verify branches/PRs/CI against `PROJECT_STATE.md`, summarize the validated gameplay baseline, working title, Adaptive Research catalog/RP+Pressure+Labs rules, and paused work, then continue without changing locked decisions.**
