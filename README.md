# Stellar Continuum

Public development repository for **Stellar Continuum**, an original real-time space civilization strategy game.

**Naming status:** Stellar Continuum is the canonical working title. Commercial trademark/domain clearance is still pending; see [`docs/BRANDING.md`](docs/BRANDING.md).

## Current development

Version: `0.0.6-dev.1`

Engine: Godot 4.7.2 .NET / C# (`net8.0`)

New campaigns begin on **January 1, 2050** with the player and normal major civilizations pre-warp. A small number of distant old powers begin already spacefaring but are non-expansionist and neutral unless provoked.

The current code is still a prototype of the pre-warp era. The canonical design direction is now broader: a human-like 2050 start is expected to include substantial orbital infrastructure, a permanent lunar presence, and a young Mars colony, followed by meaningful solar-system development before practical interstellar expansion.

### Research

Technologies consume accumulated science. Some technologies require completed infrastructure projects as well as earlier technologies.

### Construction

Construction projects consume accumulated industry. Current prototype projects include:

- Planetary Research Network — increases science output.
- Industrial Automation Program — increases industry output.
- Orbital Launch Complex — required before Orbital Industry can be researched.
- Orbital Shipyard — requires Orbital Industry.
- Warp Test Facility — requires Warp Field Control and must be completed before the Prototype Warp Drive can be researched.

This creates a progression chain from an early spacefaring 2050 civilization toward interstellar capability. Normal AI civilizations use the same technology/project prerequisite framework and choose priorities according to their traits.

## Project continuity records

Development continuity is kept in the repository so work can move between chats without relying on conversational memory.

Read these before resuming development:

- [`docs/CHAT_HANDOFF.md`](docs/CHAT_HANDOFF.md) — exact new-chat bootstrap prompt and reload protocol.
- [`docs/PROJECT_STATE.md`](docs/PROJECT_STATE.md) — authoritative validated baseline, paused work, and immediate next action.
- [`docs/GAME_DIRECTION.md`](docs/GAME_DIRECTION.md) — canonical design principles and current game direction.
- [`docs/ENGINEERING_GUARDRAILS.md`](docs/ENGINEERING_GUARDRAILS.md) — architecture, performance, save, diagnostics, and scalability rules.
- [`docs/DEVELOPMENT_HISTORY.md`](docs/DEVELOPMENT_HISTORY.md) — chronological milestone/acceptance record.
- [`docs/DECISION_LOG.md`](docs/DECISION_LOG.md) — dated durable design decisions.
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — public milestone roadmap.
- [`docs/BRANDING.md`](docs/BRANDING.md) — canonical working title, naming rationale, and clearance status.

The repository is public. Exact hidden-discovery triggers, probabilities, complete secret chains, and intentionally secret rare-AI outcomes are not recorded in public continuity documents.

## Controls

- `Space` — pause/resume
- `1` / `2` / `3` / `4` — simulation speed
- `T` — cycle available research
- `R` — start selected research
- `C` — cycle available construction
- `B` — begin selected construction project
- left click — inspect astronomical target
- right click — order scout after warp capability
- `Shift` + right click — order colony ship after warp capability
- mouse wheel — zoom
- middle mouse drag — pan
- `N` — generate a new 2050 campaign
- `F6` — autosave
- `F8` — export diagnostics/support bundle

Save format v6 persists construction completion/progress in addition to the calendar, research, civilization stages, fleets, colonies, economy, and fog-of-war knowledge.
