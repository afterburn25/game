# Development History

This is the concise chronological record of validated milestones and major development-state changes. It is not a substitute for Git history; it explains what each milestone meant and which versions were actually accepted into `main`.

## 0.0.1 — Simulation foundation

Status: merged/validated.

Key work:

- Godot 4.7.2 .NET / C# project foundation.
- Plain-C# simulation separated from Godot presentation.
- Deterministic seeded galaxy generation.
- Real-time simulation clock with pause/speed levels.
- Sustainable-speed/backlog protection.
- 2D galaxy prototype.
- Versioned persistence foundation.
- Diagnostics/support bundle infrastructure.
- GitHub Actions validation with .NET build plus pinned Godot headless editor/runtime tests.

Foundation PR #1 was merged after CI passed.

## 0.0.2-dev.1 — Civilizations and authoritative fog of war

Status: merged/validated.

Merge commit: `2aaa6d3aeead400882c8214005e3bd78cfe17eaa`

Key work:

- Eight prototype civilization archetypes/temperaments.
- Deterministically spread home systems.
- Per-civilization knowledge state.
- Player UI and AI forbidden from treating authoritative galaxy state as automatically known.
- Initial sensor-based knowledge.
- Fair-information AI scaffolding with uncertain/stale enemy estimates.
- Save format migration preserving knowledge/civilization state.

## 0.0.3-dev.1 — Real-time exploration and first contact

Status: merged/validated.

Merge commit: `8683acebb1860ff3d94838656c65c8a82cd90d68`

Key work:

- Physical scout fleets.
- Player movement orders.
- AI exploration limited to each civilization's own legitimate knowledge.
- Astronomical target positions can exist without leaking detailed system information.
- Sensor discovery/survey behavior.
- First contact occurs from actual encounter/detection rather than automatic galaxy knowledge.
- Fleet positions/orders and discoveries persisted.

## 0.0.4-dev.1 — Colonies and basic economy

Status: merged/validated.

Merge commit: `ebcba9239a0d12d0c5c99f41937193176b6c8664`

Key work:

- Home colonies.
- Population growth.
- Credits, industry, and science accumulation.
- Physical colony ships.
- Player colony movement/orders.
- AI colonization constrained by legitimate exploration knowledge.
- Native/pre-warp inhabited systems excluded from ordinary empty-world colonization.
- Colony/economy state persisted.

## 0.0.5-dev.1 — 2050 Pre-Warp Dawn

Status: merged/validated.

Merge commit: `c457f5e57c51057e86b857d0d828ff42d75e8f8b`

Key work:

- New campaigns begin January 1, 2050.
- Player and normal major civilizations begin pre-warp.
- Research progression into FTL.
- Normal AI civilizations use the same broad research prerequisites.
- Small number of remote seeded old powers can begin already spacefaring.
- Old powers are non-expansionist and neutral unless provoked.
- Old powers remain hidden until legitimate detection.
- Save format v5 preserved calendar/research/development state.
- Older prototype saves remained already spacefaring rather than being forced backward into pre-warp progression.

Important later design evolution: the implementation used a simplified pre-warp prototype. The current design direction now calls for a much richer 2050 solar-system phase (see `GAME_DIRECTION.md` and `DECISION_LOG.md`).

## 0.0.6-dev.1 — Construction-driven pre-warp progression

Status: **current authoritative validated baseline on `main` as of 2026-09-07**.

Merge commit: `91a2204b96ed08c2178875cbc8d5b0bc378372ad`

Key work:

- Industry-funded construction state/project queue.
- Planetary Research Network.
- Industrial Automation Program.
- Orbital Launch Complex.
- Orbital Shipyard.
- Warp Test Facility.
- Technologies can require completed infrastructure projects.
- AI construction follows the same prerequisite framework.
- Player construction controls.
- Save format v6 persists construction progress/completion.
- Full .NET + Godot headless CI gate passed.

## 0.0.7 — Shipbuilding

Status: **paused / incomplete / unvalidated**.

Active development branch when paused: `dev/0.0.7-shipbuilding`

Branch head when paused: `cb553e5b22bcb50be5725223f6ecc79e9561eb97`

Intended direction:

- Prototype FTL unlocks designs rather than gifting ships.
- Physical orbital shipyard production.
- Scout, science, and colony ship roles.
- Production draws from civilization industry.
- Colony ships consume/reserve real population.
- Science ships have a meaningful survey/anomaly role.
- Player/AI use the same core ship-production rules.
- Shipyard build state survives save/load.

### Pause warning

This milestone experienced GitHub connector sequencing issues: raw tree commits were created while Contents API writes moved the live branch independently. Some intended core work exists in detached commits and the live branch must not be assumed complete.

Known detached commits include:

- `66e5406b70f6f7aebc58963becd93fe359d10d10`
- `07b868759c9df5cf75113023bea3cde641c5d58c`
- `91b5cae136023b1851285e1d82ea4eebda86d3ea`

When development resumes, inspect/compare and integrate the intended changes cleanly. Do not promote or blindly repoint the branch to one of these commits.

## Major design evolution after 0.0.6

While code work was paused, the game direction became substantially more specific. These are design decisions, not yet fully implemented milestones:

- Realism-first design: replace arbitrary restrictions with believable consequences.
- Conquest does not require abstract claim tokens; legitimacy, occupation, resistance, logistics, and diplomacy create the challenge.
- Borders are not force fields; warnings can be ignored and produce consequences.
- Powerful empires can knowingly accept large consequences, creating organic late-game challenge.
- Late-game complexity should come from history, scale, politics, logistics, civilizational change, and multi-galaxy growth rather than inflated stats.
- Relationships/intelligence fade with time; ancient allies/enemies can become uncertain history or rumor.
- 2050 human-like start should assume meaningful existing space infrastructure, including a permanent lunar presence and young Mars colony rather than no meaningful off-world presence.
- Pre-warp gameplay should include a real solar-system civilization phase before FTL.
- Prototype FTL range is constrained by logistics, endurance, life support, food/replication, maintenance, infrastructure, and support nodes, not just drive rating.
- Artificial-gravity/gravity-management, closed-loop life support, radiation protection, manufacturing/replication, and similar systems can be prerequisites/enablers for deep-space settlement.
- Mature early-game systems should become automatable as the civilization grows.
- Different species must have different technology lineages; similar capabilities do not imply identical technologies.
- Some species may never independently achieve FTL.
- Foreign technologies may be incompatible, dangerous, incomprehensible, valuable only to third parties, or require alien personnel/infrastructure.
- Technology can become a major diplomatic/trade commodity.
- Most natural intelligent life is expected to be carbon-based, with rarer silicon-centered/unusual-solvent/synthetic lineages.
- Working planning target: a smaller number of deeply differentiated playable species rather than many shallow bonus-based species.

See `DECISION_LOG.md` for the dated decision record and `GAME_DIRECTION.md` for canonical principles.

## Update rule

After every validated milestone merge:

1. Add the version, merge commit, and acceptance status here.
2. Update `PROJECT_STATE.md` to make the new baseline authoritative.
3. Record any design change introduced by the milestone in `DECISION_LOG.md` if it changes a durable rule.
4. Keep incomplete/unvalidated work clearly labeled as such.
