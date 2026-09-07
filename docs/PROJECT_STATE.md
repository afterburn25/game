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
3. canonical specs: `GAME_DIRECTION.md`, `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, `RESEARCH_EMERGENCE_MODEL.md`, `ENGINEERING_GUARDRAILS.md`
4. `DECISION_LOG.md` for historical decisions/reasons and explicit supersessions
5. `ROADMAP.md`
6. current validated source/data on `main`
7. unvalidated branches/commits

Do not silently resurrect an older superseded rule.

## Authoritative validated gameplay baseline

As of 2026-09-07:

- gameplay version on `main`: **`0.0.6-dev.1`**
- validated gameplay merge commit: **`91a2204b96ed08c2178875cbc8d5b0bc378372ad`**
- save format: v6
- gameplay validation gate: .NET restore/build, pinned Godot 4.7.2 .NET download/SHA verification, headless editor smoke, headless runtime smoke

Documentation/design-data merges do **not** automatically promote the gameplay version.

### Implemented gameplay through 0.0.6

- Godot/C# foundation with simulation separate from scene-tree presentation
- deterministic procedural galaxy generation
- continuous real-time simulation with controlled speeds/backlog protection
- per-civilization fog of war and fair-information AI foundation
- civilization traits/archetypes
- exploration and first contact
- colonies/population/basic credits-industry-science economy
- 2050 calendar
- normal major civilizations pre-warp; remote old powers may begin spacefaring/non-expansionist/neutral unless provoked
- temporary fixed pre-warp research chain
- industry-funded construction/infrastructure-gated prototype research
- versioned saves, migrations, diagnostics/support bundles, automated CI

## Paused gameplay work

Gameplay development remains explicitly paused on:

- branch: **`dev/0.0.7-shipbuilding`**
- branch head when paused: **`cb553e5b22bcb50be5725223f6ecc79e9561eb97`**
- milestone intent: physical shipbuilding

This branch is incomplete/unvalidated and is **not** the authoritative gameplay baseline.

Known detached/integration-risk commits:

- `66e5406b70f6f7aebc58963becd93fe359d10d10`
- `07b868759c9df5cf75113023bea3cde641c5d58c`
- `91b5cae136023b1851285e1d82ea4eebda86d3ea`

Do not blindly repoint the branch. Reconcile intentionally when gameplay resumes.

Intended 0.0.7 behavior remains physical orbital-shipyard production, scout/science/colony roles, real industry cost, colony population reservation, same core AI/player production rules, and save-safe shipyard state. FTL research unlocks designs rather than gifting ships.

## Durable game direction beyond the prototype

- campaign begins January 1, 2050
- human-like start: mature homeworld, substantial orbital infrastructure, permanent lunar presence, young Mars colony still materially dependent
- meaningful solar-system development before practical interstellar expansion
- realistic-ish travel time, logistics, supply/endurance, life support, radiation and gravity management without becoming orbital-mechanics software
- prototype FTL practical reach depends on logistics/support infrastructure as well as drive technology
- player operational scale expands with civilization reach
- mature lower layers become automatable/delegable
- planet mass/radius determines surface gravity; environmental mismatch and multigenerational adaptation matter
- species-relative habitability and technology applicability
- fair-information AI
- borders/claims are political rather than invisible physical locks
- long-campaign scalability, bounded state, and history compression are release requirements

## Canonical Adaptive Research foundation — merged

The old fixed prototype research progression is **not** the final research architecture.

PR #11, **Adaptive Research possibility graph and research economy**, was validated and merged to `main` at:

- merge commit: **`f70e122134e87c1449582b573c5e2db8b045d311`**
- catalog validation: passed
- .NET restore/build: passed
- Godot headless editor/runtime smoke tests: passed
- gameplay version: unchanged at `0.0.6-dev.1`

### Public possibility catalog on `main`

- **330 normal/public possibility nodes**
- **20 research domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**
- stable prerequisite/applicability/evidence/capability metadata
- selective RP/Lab/Pressure requirements
- CI structural validator

The five newest domains are Agriculture & Biosphere Engineering, Economic & Trade Systems, Cybernetics & Augmentation, Scientific Infrastructure & Metrology, and Megastructure & Stellar Engineering.

These are design/data possibilities, not a promise that every node is finished gameplay content in Early Access.

### RP + Pressure + Labs — accepted rule

1. **Research Points (RP)** — generated by Effective Research Labs and applied to active directed projects.
2. **Research Pressure** — bounded contextual need/evidence. **Only explicitly configured technologies are hard-gated by pressure; complexity alone never creates a pressure requirement.**
3. **Effective Research Labs** — physical scientific capacity; each directed project requires a minimum assigned amount.

Early play formally directs **one major research program**, while unassigned labs continue diffuse/basic science.

Parallel directed research is unlocked by real research nodes:

- `coordinated_research_networks` -> 2 programs
- `distributed_scientific_portfolios` -> up to 4
- `autonomous_research_portfolios` -> no artificial slot ceiling; physical lab capacity becomes the practical limit

This supersedes older wording implying multiple directed projects were available immediately whenever labs existed.

## Adaptive Research emergence layer — current design work

Current branch: **`docs/adaptive-research-emergence`**

Open PR: **#12 — Adaptive Research emergence, evidence, and pressure dynamics**

This second layer makes the 330-node graph actually emerge from civilization history rather than merely existing as a large DAG.

Current design/data includes:

- **6 public applicability traits**
- **9 public evidence types**
- generation/decay rules for **all 59 Research Pressures**
- sparse event/index-driven candidate-emergence pipeline
- validator coverage for trait/evidence references and complete pressure-rule coverage

### Emergence rules

- no direct calendar-year technology unlocks
- no global-rank catch-up pressure
- owning simulation subsystems generate normalized metrics/events; research does not scan every technology every tick
- pressure band crossings, evidence acquisition, prerequisite maturation, trait changes, and bounded basic-science reviews wake only indexed candidate nodes
- enemy-relative pressure requires legitimate observation; hidden authoritative enemy statistics cannot generate exact pressure
- evidence proves/suggests possibilities but never instantly grants technology
- evidence is stored as campaign instances with provenance/quality/confidence/intactness where relevant
- applicability traits are capabilities/biological facts, not race IDs
- population/species traits apply to the relevant population, not automatically to every citizen in a multispecies civilization
- mutable civilization traits can be acquired through technological history, e.g. machine cognition or biological fabrication capability

Canonical files after PR #12 is accepted:

- `docs/RESEARCH_EMERGENCE_MODEL.md`
- `data/research/v1/emergence_model.json`
- `data/research/v1/applicability_traits.json`
- `data/research/v1/evidence_types.json`
- `data/research/v1/pressure_dynamics.json`

Public research data deliberately excludes exact secret discovery chains/probabilities, rare secret technologies/evidence, and hidden special-AI conditions.

## Early-release campaign horizon direction

- public demo: roughly 100–150 meaningful in-game years
- initial paid Early Access: roughly **500 years of officially supported meaningful content/simulation**
- engineering soak requirement: at least **1,000 simulated years** without unbounded memory/save/performance failure
- no hard year-based game-over

## Persistence/scalability direction

Long campaigns should use tiered state:

- hot active state in RAM
- bounded warm summaries/caches
- cold/dormant/historical state in a proven embedded persistent datastore behind a game-owned `CampaignStore`-style abstraction

Do not build a bespoke database engine unless a proven need appears.

Adaptive Research specifically keeps the universal catalog static and persists only compact civilization state: mature IDs, visible candidates, active projects, sparse pressure values, evidence instances, applicable mutable traits, lab allocations, and field competence.

## Next action

For current research work:

1. finish/validate PR #12 on `docs/adaptive-research-emergence`
2. keep trait/evidence/pressure references machine-validated
3. merge only after catalog + .NET + Godot gates pass
4. do not promote gameplay version
5. next research-design layer should formalize capability relationships/technology grants and branch maturation/side-discovery behavior before gameplay integration

When gameplay development later resumes:

1. reload all canonical records from `afterburn25/stellar-continuum`
2. inspect `main`, paused shipbuilding, open PRs, and CI
3. keep `0.0.6-dev.1` as gameplay baseline unless a later gameplay merge explicitly changes it
4. reconcile shipbuilding cleanly
5. build future research/ship prerequisites on the Adaptive Research/capability architecture rather than the temporary fixed 0.0.6 chain
