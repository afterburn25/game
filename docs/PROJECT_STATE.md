# Canonical Project State

This file is the authoritative continuity record for the current development state of the project. Update it whenever a milestone is merged, a development branch is paused/resumed, or an implementation decision changes the active baseline.

## Repository

- Working title: **Stellar Continuum**
- Naming status: canonical project working title; commercial trademark/domain clearance is still pending. See `BRANDING.md`.
- Repository: `afterburn25/game`
- Repository visibility: public
- Engine: Godot 4.7.2 .NET
- Language/runtime: C# / .NET 8
- Architecture: Godot for presentation/input/audio/platform integration; plain C# simulation core for galaxy state, AI, economy, fleets, knowledge, history, persistence, and long-running simulation.

## Authority order

When documents or code appear to disagree, use this order:

1. Explicit new instruction from the user.
2. This `PROJECT_STATE.md` file for current baseline/work status.
3. `GAME_DIRECTION.md`, `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, and `ENGINEERING_GUARDRAILS.md` for locked design/engineering rules.
4. `DECISION_LOG.md` for historical decisions and why they were made.
5. `ROADMAP.md` for planned milestone direction.
6. Current validated source on `main`.
7. Unvalidated development branches/commits.

Do not silently replace a newer explicit user decision with an older document entry. Update the documents instead.

## Authoritative validated gameplay baseline

As of 2026-09-07:

- Validated gameplay version on `main`: **`0.0.6-dev.1`**
- Validated gameplay merge commit: **`91a2204b96ed08c2178875cbc8d5b0bc378372ad`**
- CI gate passed before merge: .NET restore/build, pinned Godot 4.7.2 .NET download, headless editor smoke test, and headless runtime smoke test.
- Save format on this baseline: v6.

Documentation/design-data merges after that commit do not by themselves promote a new gameplay version.

### Implemented through 0.0.6

- Godot/C# project foundation with simulation separated from scene-tree presentation.
- Deterministic procedural galaxy generation.
- Continuous real-time simulation with pause and controlled speed levels.
- Sustainable-speed/backlog protection.
- Per-civilization fog of war and fair-information AI contract.
- Distinct civilization traits/archetypes.
- Real-time exploration and first contact.
- Colonies, population growth, credits, industry, and science.
- 2050 campaign calendar.
- Normal major civilizations begin pre-warp; remote seeded old powers can begin spacefaring while remaining non-expansionist and neutral unless provoked.
- Pre-warp research progression into prototype FTL.
- Industry-funded construction projects and infrastructure-gated research.
- Versioned saves, migration support, bounded diagnostics, support bundles, and automated headless CI validation.

## Paused development work

Development was explicitly paused while working on:

- Branch: **`dev/0.0.7-shipbuilding`**
- Current branch head when paused: **`cb553e5b22bcb50be5725223f6ecc79e9561eb97`**
- Intended milestone: **0.0.7 — physical shipbuilding**

### Important 0.0.7 warning

`dev/0.0.7-shipbuilding` is **not validated, not complete, and must not be promoted to the baseline**.

During connector writes, raw Git-tree commits and GitHub Contents API commits advanced refs differently. Some intended shipbuilding-core work was created in detached commits instead of being cleanly incorporated into the live branch. Known detached work includes commits such as:

- `66e5406b70f6f7aebc58963becd93fe359d10d10`
- `07b868759c9df5cf75113023bea3cde641c5d58c`
- `91b5cae136023b1851285e1d82ea4eebda86d3ea`

Do **not** blindly move the branch ref to one of these commits. When work resumes, inspect/compare the source and reapply or integrate the intended shipbuilding changes cleanly on top of the current validated baseline/branch. Then run the full CI gate before merge.

### Intended 0.0.7 behavior

The milestone direction is:

- Researching prototype warp/FTL unlocks ship designs; it does not magically create ships.
- Orbital shipyard infrastructure is required to build early interstellar vessels.
- Initial designs include scout, science, and colony ships.
- Scout: relatively fast/cheap exploration vessel.
- Science vessel: stronger survey/anomaly role rather than a cosmetic duplicate of the scout.
- Colony ship: expensive and consumes/reserves real population rather than creating colonists from nothing.
- Ship production consumes civilization industry and competes with infrastructure construction.
- Player and AI use the same fundamental production/prerequisite rules.
- Shipbuilding state must survive save/load.

## Current game-direction changes that supersede older assumptions

Recent design discussion significantly deepened the intended pre-warp era. Future implementation should not assume the existing 0.0.5/0.0.6 simplified pre-warp prototype is the final design.

Key current direction:

- Campaign still begins in **2050**.
- A realistic human-like 2050 start is no longer "planet-bound with no space presence." The working human baseline is a mature homeworld with orbital infrastructure, a permanent lunar base/settlement, and a young Mars colony that remains logistically dependent.
- The player should experience meaningful in-system development before FTL: lunar/planetary settlements, outposts, orbital construction, asteroid/resource operations, life support, supply chains, long-duration habitation, and supporting technologies.
- Prototype FTL has limited practical reach. Operational range also depends on supply endurance, logistics, support colonies/outposts, life support, food production/replication, maintenance, and related technology.
- The playable/operational map should expand as the civilization becomes capable of reaching and meaningfully operating at larger scales.
- Mature earlier layers should become automatable/delegable as the player advances, preventing late-game micromanagement overload.

See `GAME_DIRECTION.md` and `DECISION_LOG.md` for the broader design rules.

## Canonical Adaptive Research direction

The old prototype fixed research progression is **not** the final research architecture.

Future research implementation must follow `ADAPTIVE_RESEARCH_SYSTEM.md` and `RESEARCH_ECONOMY.md`.

Core rules:

- The simulation has a broad **Technology Possibility Graph**; the player never sees the complete graph.
- Each civilization materializes only the possibilities it currently understands or has reason/evidence to investigate.
- The visible tree can grow, branch, and change over decades. Two civilizations starting from broadly similar science can have very different late-game trees.
- Need is important but not the only source of discovery; basic science, experiments, anomalies, foreign observation, captured technology, and contact can expose new branches.
- Strategic capabilities are separate from technological implementations so species can solve the same problem differently.
- Some technologies can remain permanently unavailable to a civilization because it never encounters the conditions/evidence or lacks biological/material applicability.
- Foreign technology is not an instant unlock and may be incompatible, incomprehensible, dangerous, or only useful as evidence/inspiration.

### Research economy

The durable player-facing model is:

1. **Research Points (RP)** — generated by operational Research Labs and applied to active projects.
2. **Research Pressure** — contextual need/evidence; certain technologies require relevant pressure to cross a threshold before becoming researchable.
3. **Research Labs** — physical/effective scientific capacity; every technology requires a minimum number of assignable labs.

There is **no arbitrary fixed research-slot cap**. Multiple projects can run simultaneously whenever enough unreserved lab capacity exists to satisfy their minimum requirements.

The first public seed catalog under `data/research/v1/` contains:

- 244 normal/public possibility nodes
- 15 research domains
- 59 research-pressure types
- explicit alternative solution families
- machine-readable prerequisites/applicability/evidence/capability metadata
- seeded lab/RP/pressure requirements

These counts and balance values can grow/change before gameplay implementation; stable IDs must be treated carefully once supported saves use them.

The public seed deliberately excludes exact secret discovery chains/probabilities and other intentionally hidden content.

## Next action when gameplay development resumes

1. Read all continuity documents listed in `CHAT_HANDOFF.md`, including the adaptive research and research-economy specs.
2. Inspect `main`, `dev/0.0.7-shipbuilding`, open PRs, and relevant commits before modifying source.
3. Treat gameplay `0.0.6-dev.1` as the last validated gameplay baseline unless the repository has since advanced and this file has been updated.
4. Reconcile 0.0.7 cleanly instead of assuming the paused branch contains all intended shipbuilding changes.
5. Do not build future research/ship prerequisites around the old fixed prototype tech chain; implement against the adaptive possibility/capability model when the research runtime is replaced.
6. Preserve the newer realism-first/pre-warp/logistics/species-technology decisions while implementing future milestones.
7. Preserve **Stellar Continuum** as the canonical working title unless the user explicitly supersedes it; naming clearance status lives in `BRANDING.md`.
8. Never publish exact hidden discovery triggers, probabilities, secret artifact chains, or rare secret AI outcomes in this public repository.
