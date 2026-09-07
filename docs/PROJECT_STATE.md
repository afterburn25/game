# Canonical Project State

This file is the authoritative continuity record for the current development state of the project. Update it whenever a milestone is merged, a development branch is paused/resumed, or an implementation decision changes the active baseline.

## Repository

- Repository: `afterburn25/game`
- Repository visibility: public
- Engine: Godot 4.7.2 .NET
- Language/runtime: C# / .NET 8
- Architecture: Godot for presentation/input/audio/platform integration; plain C# simulation core for galaxy state, AI, economy, fleets, knowledge, history, persistence, and long-running simulation.

## Authority order

When documents or code appear to disagree, use this order:

1. Explicit new instruction from the user.
2. This `PROJECT_STATE.md` file for current baseline/work status.
3. `GAME_DIRECTION.md` and `ENGINEERING_GUARDRAILS.md` for locked design/engineering rules.
4. `DECISION_LOG.md` for historical decisions and why they were made.
5. `ROADMAP.md` for planned milestone direction.
6. Current validated source on `main`.
7. Unvalidated development branches/commits.

Do not silently replace a newer explicit user decision with an older document entry. Update the documents instead.

## Authoritative validated baseline

As of 2026-09-07:

- Validated version on `main`: **`0.0.6-dev.1`**
- Validated merge commit: **`91a2204b96ed08c2178875cbc8d5b0bc378372ad`**
- CI gate passed before merge: .NET restore/build, pinned Godot 4.7.2 .NET download, headless editor smoke test, and headless runtime smoke test.
- Save format on this baseline: v6.

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

See `GAME_DIRECTION.md` and `DECISION_LOG.md` for the full design rules.

## Next action when development resumes

1. Read all continuity documents listed in `CHAT_HANDOFF.md`.
2. Inspect `main`, `dev/0.0.7-shipbuilding`, open PRs, and relevant commits before modifying source.
3. Treat `main` 0.0.6-dev.1 as the last validated baseline unless the repository has since advanced and this file has been updated.
4. Reconcile 0.0.7 cleanly instead of assuming the paused branch contains all intended shipbuilding changes.
5. Preserve the newer realism-first/pre-warp/logistics/species-technology decisions while implementing future milestones.
6. Never publish exact hidden discovery triggers, probabilities, secret artifact chains, or rare secret AI outcomes in this public repository.
