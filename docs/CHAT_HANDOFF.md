# New-Chat Handoff Protocol

This file exists so development can move between ChatGPT conversations without relying on fragile conversational memory.

## Exact bootstrap prompt for a new chat

Copy and paste the following message as the first project message in a new chat:

> **Open the public GitHub repository `afterburn25/game`. Before changing any code, read `docs/CHAT_HANDOFF.md`, `docs/PROJECT_STATE.md`, `docs/GAME_DIRECTION.md`, `docs/ENGINEERING_GUARDRAILS.md`, `docs/DEVELOPMENT_HISTORY.md`, `docs/DECISION_LOG.md`, `docs/ROADMAP.md`, `docs/BRANDING.md`, `docs/ARCHITECTURE.md`, and `docs/AI.md` from `main`. Treat those records as the project source of truth, then inspect the current `main` branch, active development branches, open pull requests, VERSION/GameVersion, and recent validation workflow results. Tell me the authoritative validated baseline, current working title/naming status, any paused/unvalidated work, and the next intended milestone before you make changes. Do not reverse a locked design rule, rename the project, or promote unvalidated work unless I explicitly tell you to. Then continue development from the recorded state.**

That wording is intentionally explicit. It tells the assistant to use the repository itself rather than reconstructing project state from memory.

## Required reload procedure

When the bootstrap prompt is received, the assistant should:

1. Fetch this file from `main`.
2. Read all canonical documents listed above, including `BRANDING.md`.
3. Inspect the repository's current `main` commit and version files.
4. Inspect active `dev/**` or relevant development branches.
5. Inspect open PRs and validation status where relevant.
6. Compare repository reality with `PROJECT_STATE.md`.
7. If the repository has advanced but `PROJECT_STATE.md` was not updated, report the discrepancy rather than guessing.
8. State clearly which version is the last validated baseline and which work is merely prepared/paused/unvalidated.
9. State the recorded working title and whether naming clearance is complete or still pending.
10. Only then begin development.

## Canonical continuity files

### `PROJECT_STATE.md`
Current authoritative baseline, active/paused branches, warnings, naming state, and immediate next action.

### `GAME_DIRECTION.md`
Durable game identity and design rules: realism-first systems, pre-warp direction, logistics, borders, late game, automation, species/technology divergence, etc.

### `ENGINEERING_GUARDRAILS.md`
Architecture, performance, bounded-memory rules, save discipline, diagnostics, CI, and long-campaign scalability requirements.

### `DEVELOPMENT_HISTORY.md`
Chronological record of validated milestones and important incomplete work.

### `DECISION_LOG.md`
Dated durable decisions and the reasons behind them. New contradictory decisions should supersede earlier ones explicitly rather than silently deleting history.

### `ROADMAP.md`
Public milestone plan.

### `BRANDING.md`
Canonical working title, naming rationale, preliminary conflict findings, and commercial clearance status.

### `ARCHITECTURE.md`
Core technical architecture.

### `AI.md`
Fair-AI behavior/knowledge rules.

## Update protocol during development

The repository records must evolve with the project.

After a validated milestone merge:

- update `PROJECT_STATE.md` with the new authoritative baseline/commit/version
- append the milestone to `DEVELOPMENT_HISTORY.md`
- update `ROADMAP.md` if milestone planning changed
- update `README.md` if player/developer-facing current-version information changed

After a major accepted design decision:

- append a dated entry to `DECISION_LOG.md`
- update `GAME_DIRECTION.md` when the decision affects a durable design principle
- update `PROJECT_STATE.md` when it changes the immediate implementation direction

After a naming/branding decision:

- update `BRANDING.md`
- append the decision to `DECISION_LOG.md`
- update `PROJECT_STATE.md`, `README.md`, and other player-facing docs where appropriate

After an architectural/performance rule changes:

- update `ENGINEERING_GUARDRAILS.md`
- update `ARCHITECTURE.md` where appropriate

Do not let these continuity records lag several milestones behind the code.

## Validation discipline

A development branch, local/detached commit, or prepared feature is not the authoritative baseline merely because code exists.

The assistant should preserve the distinction between:

- idea/design direction
- in-progress source
- prepared development branch
- CI-validated candidate
- merged authoritative baseline

When in doubt, inspect GitHub rather than guessing.

## Public-repository secrecy rule

This repository is public.

Never add exact hidden discovery triggers, exact rare probabilities, full secret artifact chains, or intentionally secret special-AI conditions to these continuity documents. The public docs may record only the broad framework for undocumented discoveries.

## Short version

If the full bootstrap sentence is inconvenient, this shorter wording is acceptable, but the full version above is preferred:

> **Reload the `afterburn25/game` project from the canonical continuity files in `docs/` on `main`, including `BRANDING.md`; verify the live GitHub branches/PRs/CI against `PROJECT_STATE.md`, summarize the validated baseline, working title/naming status, and paused work, and continue without changing locked decisions.**
