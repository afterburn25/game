# Development History

This is the concise chronological record of validated gameplay milestones and major design/data state changes. It explains what was actually accepted and keeps design foundations separate from gameplay-version promotion.

## 0.0.1 — Simulation foundation

Status: merged/validated.

- Godot 4.7.2 .NET / C# foundation.
- Plain-C# simulation separated from Godot presentation.
- Deterministic galaxy generation.
- Real-time clock with pause/speed control and sustainable-backlog protection.
- Versioned persistence foundation.
- Diagnostics/support bundles.
- GitHub Actions .NET + pinned Godot validation.

PR #1 merged after CI passed.

## 0.0.2-dev.1 — Civilizations and authoritative fog of war

Status: merged/validated.

Merge commit: `2aaa6d3aeead400882c8214005e3bd78cfe17eaa`

- prototype civilization archetypes/temperaments
- deterministically distributed homes
- per-civilization knowledge
- fair-information AI scaffolding
- uncertain/stale observations
- save migration for civilization/knowledge state

## 0.0.3-dev.1 — Exploration and first contact

Status: merged/validated.

Merge commit: `8683acebb1860ff3d94838656c65c8a82cd90d68`

- physical scout fleets
- player movement orders
- AI exploration from legitimate knowledge only
- sensor discovery/survey behavior
- first contact from actual detection/encounter
- persisted fleets/orders/discoveries

## 0.0.4-dev.1 — Colonies and basic economy

Status: merged/validated.

Merge commit: `ebcba9239a0d12d0c5c99f41937193176b6c8664`

- home colonies
- population growth
- credits/industry/science
- physical colony ships
- knowledge-constrained AI colonization
- pre-warp/native systems protected from ordinary empty-world colonization
- colony/economy persistence

## 0.0.5-dev.1 — 2050 Pre-Warp Dawn

Status: merged/validated.

Merge commit: `c457f5e57c51057e86b857d0d828ff42d75e8f8b`

- January 1, 2050 campaign start
- player/normal majors begin pre-warp
- prototype fixed research progression toward FTL
- remote seeded old powers can begin spacefaring
- old powers initially non-expansionist/neutral unless provoked
- old powers remain hidden until legitimate discovery
- save v5 preserved calendar/research/development state

Later design substantially deepened the 2050 solar-system opening; this implementation is a prototype, not the final pre-warp design.

## 0.0.6-dev.1 — Construction-driven development

Status: **current authoritative validated gameplay baseline on `main` as of 2026-09-07**.

Merge commit: `91a2204b96ed08c2178875cbc8d5b0bc378372ad`

- industry-funded construction/project queue
- Planetary Research Network
- Industrial Automation Program
- Orbital Launch Complex
- Orbital Shipyard
- Warp Test Facility
- infrastructure-gated prototype research
- AI follows same prerequisite framework
- save v6 construction persistence
- full .NET + Godot headless CI passed

## 0.0.7 — Shipbuilding

Status: **paused / incomplete / unvalidated**.

Branch: `dev/0.0.7-shipbuilding`

Head when paused: `cb553e5b22bcb50be5725223f6ecc79e9561eb97`

Intended direction:

- FTL research unlocks designs rather than gifting ships
- physical orbital-shipyard production
- scout/science/colony roles
- real industry cost
- colony ships consume/reserve population
- science ships gain real survey/anomaly role
- same core production rules for AI/player
- shipyard state persists

Known detached/integration-risk commits:

- `66e5406b70f6f7aebc58963becd93fe359d10d10`
- `07b868759c9df5cf75113023bea3cde641c5d58c`
- `91b5cae136023b1851285e1d82ea4eebda86d3ea`

Do not blindly repoint the branch. Reconcile intentionally when gameplay development resumes.

## Major design evolution after 0.0.6

While gameplay code was paused, durable direction became substantially more specific:

- realism-first causes/consequences instead of arbitrary restrictions
- conquest without mandatory abstract claim tokens
- borders as political warnings rather than force fields
- power can create natural overextension/coalition/logistics/political consequences without artificial empire penalties
- relationships/intelligence fade into history and rumor
- human-like 2050 start includes meaningful orbital/lunar/Mars presence
- pre-warp gameplay becomes a real solar-system civilization phase
- FTL practical range depends on logistics/endurance/support infrastructure
- gravity, life support, radiation, food/fabrication, and long-duration habitation matter
- mature lower-level systems become automatable
- planetary gravity and multigenerational adaptation matter
- species-relative habitability and biological diversity matter
- technology becomes civilization-specific through an Adaptive Research system rather than separate handcrafted species trees
- foreign technology can be incompatible, dangerous, incomprehensible, or tradable to third parties
- long campaigns require bounded state, history compression, tiered simulation detail, and persistent cold storage
- initial paid Early Access design horizon targets roughly 500 meaningful years, with 1,000-year engineering soak testing

## Adaptive Research design/data foundation

Status: design/data/tooling work; **does not by itself change gameplay version**.

The research architecture now uses a hidden **Technology Possibility Graph**. Each civilization materializes only its current visible research horizon from knowledge, conditions, evidence, biology, institutions, basic science, warfare, and contact history.

Research economy:

- **Research Points (RP)** are generated by Effective Research Labs.
- **Research Pressure** is contextual need/evidence; only explicitly configured nodes are hard-gated by pressure.
- **Effective Research Labs** are physical scientific capacity; directed projects require minimum allocations.
- Early civilizations formally direct one major project while unassigned labs continue background science.
- `coordinated_research_networks` unlocks 2 directed programs.
- `distributed_scientific_portfolios` unlocks 4.
- `autonomous_research_portfolios` removes the artificial slot ceiling; lab capacity becomes the practical limit.

The expanded public seed under `data/research/v1/` contains:

- **330 normal/public possibility nodes**
- **20 research domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**
- stable IDs, prerequisites, applicability/evidence tags, pressure affinities, capabilities, solution families, graph depth/complexity metadata
- selective lab/RP/pressure requirements

The five newest domains are:

- Agriculture & Biosphere Engineering
- Economic & Trade Systems
- Cybernetics & Augmentation
- Scientific Infrastructure & Metrology
- Megastructure & Stellar Engineering

Secret/rare discovery chains and intentionally hidden technologies are excluded from this public catalog.

CI runs `scripts/validate_research_catalog.py` before .NET/Godot validation and checks domain/node counts, duplicate IDs, prerequisites, pressure references, alternative-solution references, research-economy overrides, research-capacity technology references, and dependency cycles.

Until gameplay code is intentionally changed/validated/merged, **`0.0.6-dev.1` remains the authoritative gameplay baseline**.

Canonical detail lives in `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, `GAME_DIRECTION.md`, and `PROJECT_STATE.md`.

## Update rule

After every validated gameplay milestone merge:

1. record version/merge commit/acceptance here
2. update `PROJECT_STATE.md`
3. record durable design changes in `DECISION_LOG.md`
4. keep incomplete/unvalidated work clearly labeled
5. keep design/data merges distinct from gameplay version promotion
