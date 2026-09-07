# Canonical Project State

This file is the authoritative continuity record for Stellar Continuum's current development state. Update it whenever the validated gameplay baseline, paused/resumed work, repository location, or major implementation direction changes.

## Repository / identity

- Working title: **Stellar Continuum**
- Repository: **`afterburn25/stellar-continuum`**
- Visibility: public
- Naming status: canonical working title; commercial trademark/domain clearance remains pending. See `BRANDING.md`.
- Engine: Godot 4.7.2 .NET
- Language/runtime: C# / .NET 8
- Architecture: Godot for presentation/input/audio/platform integration; plain C# for authoritative simulation state/services.

## Authority order

When records disagree:

1. explicit new user instruction
2. this `PROJECT_STATE.md` for current baseline/work status
3. canonical specs: `GAME_DIRECTION.md`, `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, `ENGINEERING_GUARDRAILS.md`
4. `DECISION_LOG.md` for historical decisions/reasons
5. `ROADMAP.md`
6. current validated source on `main`
7. unvalidated branches/commits

A newer accepted rule may supersede older Decision Log wording; do not silently resurrect the older rule.

## Authoritative validated gameplay baseline

As of 2026-09-07:

- gameplay version on `main`: **`0.0.6-dev.1`**
- validated gameplay merge commit: **`91a2204b96ed08c2178875cbc8d5b0bc378372ad`**
- save format: v6
- validation gate: .NET restore/build, pinned Godot 4.7.2 .NET download/SHA verification, headless editor smoke, headless runtime smoke

Documentation/design-data merges after that commit do **not** automatically promote the gameplay version.

### Implemented through 0.0.6

- Godot/C# foundation with simulation separate from scene-tree presentation
- deterministic procedural galaxy generation
- continuous real-time simulation with controlled speeds/backlog protection
- per-civilization fog of war and fair-information AI foundation
- civilization traits/archetypes
- exploration and first contact
- colonies/population/basic credits-industry-science economy
- 2050 calendar
- normal major civilizations pre-warp; remote old powers may begin spacefaring/non-expansionist/neutral unless provoked
- prototype fixed pre-warp research chain
- industry-funded construction/infrastructure-gated research
- versioned saves, migrations, diagnostics/support bundles, automated CI

## Paused gameplay work

Gameplay development remains explicitly paused on:

- branch: **`dev/0.0.7-shipbuilding`**
- branch head when paused: **`cb553e5b22bcb50be5725223f6ecc79e9561eb97`**
- milestone intent: physical shipbuilding

This branch is incomplete/unvalidated and is **not** the authoritative gameplay baseline.

Known detached/integration-risk commits include:

- `66e5406b70f6f7aebc58963becd93fe359d10d10`
- `07b868759c9df5cf75113023bea3cde641c5d58c`
- `91b5cae136023b1851285e1d82ea4eebda86d3ea`

Do not blindly move the branch ref to those commits. Reconcile intentionally when gameplay resumes.

### Intended 0.0.7 behavior

- FTL research unlocks ship designs; it does not gift ships
- orbital shipyard physically builds vessels
- first interstellar roles include scout, science, colony
- ship production consumes real industry
- colony ships consume/reserve real population
- AI/player follow the same core production rules
- shipyard state survives save/load

## Current design direction beyond the prototype

The existing 0.0.5/0.0.6 pre-warp implementation is only a prototype.

Current durable direction includes:

- campaign begins January 1, 2050
- human-like start: mature homeworld, substantial orbital infrastructure, permanent lunar presence, young Mars colony still materially dependent
- meaningful solar-system development before practical interstellar expansion
- realistic-ish travel times, logistics, supply/endurance, life support, radiation and gravity management without turning the game into orbital-mechanics software
- prototype FTL has limited practical reach; logistics/support infrastructure matter as much as drive technology
- player operational scale expands with civilization reach
- mature lower layers become automatable/delegable
- planet mass/radius -> surface gravity; environmental mismatch and multigenerational adaptation matter
- species-relative habitability and technological applicability
- long-campaign scalability and bounded state are release requirements

## Canonical Adaptive Research direction

The old fixed prototype research progression is **not** the final research architecture.

Current research/design work lives on research-design branches until validated/merged. Canonical implementation must follow `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, and `data/research/v1/` once those records are merged to `main`.

### Adaptive Research architecture

- a broad hidden **Technology Possibility Graph** exists as static game data
- the player never sees the complete future graph
- each civilization materializes only currently known/plausible/relevant branches
- branches emerge from need, basic science, experiments, observations, anomalies, environmental conditions, war, foreign contact, captured devices, biology, and institutional history
- capabilities are separate from technological implementations
- some possibilities may never appear for a civilization
- foreign technology is not an instant unlock and can be incompatible, dangerous, incomprehensible, or only scientifically informative

### Research economy — current accepted rule

Research uses **RP + Pressure + Labs**:

1. **Research Points (RP)** — generated by Effective Research Labs and applied to active directed projects.
2. **Research Pressure** — bounded contextual need/evidence. **Only explicitly configured technologies are hard-gated by pressure; complexity alone never creates a pressure requirement.**
3. **Effective Research Labs** — physical scientific capacity; each directed project requires a minimum assigned amount.

Early play deliberately allows only **one formally directed major research program**, while unassigned labs continue diffuse/basic science.

Parallel directed research is unlocked by actual research nodes:

- `coordinated_research_networks` → 2 directed programs
- `distributed_scientific_portfolios` → up to 4
- `autonomous_research_portfolios` → no artificial slot ceiling; lab capacity becomes the practical limit

This staged concurrency rule supersedes older wording that implied multiple player-directed projects were available immediately whenever labs existed.

### Public possibility catalog

Current expanded design catalog target on `docs/adaptive-research-expansion`:

- **330 normal/public possibility nodes**
- **20 domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**
- static prerequisite/applicability/evidence/capability metadata
- selective RP/Lab/Pressure requirements
- CI structural validator

New expansion domains include:

- Agriculture & Biosphere Engineering
- Economic & Trade Systems
- Cybernetics & Augmentation
- Scientific Infrastructure & Metrology
- Megastructure & Stellar Engineering

These are design-data counts, not a promise that all 330 nodes are fully implemented gameplay content in Early Access.

The public catalog deliberately excludes exact secret discovery chains, probabilities, rare secret technologies, and hidden special-AI conditions.

## Early-release campaign horizon direction

Working targets:

- public demo: roughly 100–150 meaningful in-game years
- initial paid Early Access: roughly **500 years of officially supported content/simulation**
- engineering soak requirement: at least **1,000 simulated years** without unbounded memory/save/performance failure
- no hard year-based game-over; later years may initially be less content-rich

## Persistence/scalability direction

Long campaigns should use tiered state:

- hot active state in RAM
- bounded warm summaries/caches
- cold/dormant/historical state in an embedded persistent campaign datastore behind a `CampaignStore`-style abstraction

Do not write a bespoke database engine unless a proven need appears. Use proven embedded storage while keeping the simulation/storage interface under our control.

## Next action

For current research-tree work:

1. continue on the adaptive-research design branch, not paused gameplay source
2. keep `index.json` counts synchronized
3. validate all node IDs/prerequisites/pressures/solution sets/cycles with `scripts/validate_research_catalog.py`
4. run the full repository CI gate before merging research design data
5. merge documentation/design data only after validation; do not promote gameplay version

When gameplay development later resumes:

1. reload all continuity records from `afterburn25/stellar-continuum`
2. inspect `main`, `dev/0.0.7-shipbuilding`, open PRs, and CI
3. treat `0.0.6-dev.1` as last validated gameplay baseline unless this file records a later one
4. reconcile shipbuilding cleanly
5. do not build future research/ship prerequisites around the old fixed prototype tree; use the Adaptive Research/capability architecture
6. preserve realism-first, fair-AI, logistics, species divergence, automation, long-campaign scalability, and public-secret boundaries
