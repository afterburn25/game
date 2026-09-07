# Canonical Project State

This file is the authoritative continuity record for Stellar Continuum's current development state. Update it whenever the validated gameplay baseline, active workstreams, paused/resumed work, repository location, or major implementation direction changes.

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
3. `WORKSTREAMS.md` for active branch ownership/integration boundaries
4. canonical specs: `GAME_DIRECTION.md`, `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, `RESEARCH_EMERGENCE_MODEL.md`, `RESEARCH_MATURATION_MODEL.md`, `ENGINEERING_GUARDRAILS.md`
5. `DECISION_LOG.md` for historical decisions/reasons and explicit supersessions
6. `ROADMAP.md`
7. current validated source/data on `main`
8. unvalidated branches/commits

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

## Concurrent workstreams

Canonical workstream ownership is recorded in `WORKSTREAMS.md`.

### Adaptive Research / Technology

- persistent branch: **`dev/adaptive-research`**
- owner: dedicated Adaptive Research chat/workstream
- scope: technology graph/data, RP/Pressure/Labs, emergence, evidence, applicability, capabilities, maturation, side discoveries, foreign-tech research architecture, research validation/docs

Other branches may consume the research interfaces but should not independently edit canonical research graph/schema files while this workstream is active without coordination.

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

## Adaptive Research milestone #1 — possibility graph / research economy

PR #11 was validated and merged to `main` at:

- merge commit: **`f70e122134e87c1449582b573c5e2db8b045d311`**
- gameplay VERSION remained `0.0.6-dev.1`

Public research foundation:

- **330 normal/public possibility nodes**
- **20 domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**
- stable prerequisites/applicability/evidence/capability metadata
- Research Points + Research Pressure + Effective Research Labs
- selective pressure gates only; complexity does not implicitly create pressure requirements
- staged directed research: 1 -> 2 -> 4 -> lab-capacity-limited programs
- catalog validator in CI

The five newest domains are Agriculture & Biosphere Engineering, Economic & Trade Systems, Cybernetics & Augmentation, Scientific Infrastructure & Metrology, and Megastructure & Stellar Engineering.

## Adaptive Research milestone #2 — emergence / evidence / pressure dynamics

PR #12 was validated and merged to `main` at:

- merge commit: **`95fa5c9e77642479eecc8f4183c91c06b3709f7e`**
- gameplay VERSION remained `0.0.6-dev.1`

Merged emergence layer:

- **6 public applicability traits**
- **9 public evidence types**
- generation/decay rules for all **59 Research Pressures**
- sparse event/index-driven candidate emergence
- no calendar-year unlocks
- no rank-based catch-up pressure
- enemy-relative pressure requires legitimate observation
- evidence has provenance/quality/confidence and never instantly grants technology
- population/species applicability is scoped to the relevant population
- mutable civilization traits can expose new branches through technological/deployment history
- validator checks trait/evidence references and exact pressure-rule coverage

Canonical emergence files include `RESEARCH_EMERGENCE_MODEL.md`, `emergence_model.json`, `applicability_traits.json`, `evidence_types.json`, and `pressure_dynamics.json`.

## Adaptive Research milestone #3 — capability interoperability / maturation

Current persistent branch: **`dev/adaptive-research`**

Current PR: **#13 — Adaptive Research capability interoperability and maturation**

Current design/data direction:

### Capability interoperability

- implementation-specific node prerequisites mean genuine knowledge lineage
- generic functional dependencies use cross-lineage capabilities
- capabilities have civilization/population/installation scope
- multiple technologies can grant the same capability without exposing all source technologies
- Prototype Warp requires `spacecraft_construction` rather than one specific shipyard knowledge path
- Artificial Wormhole Stabilization requires `megastructure_construction`
- Interstellar Logistics Network requires reliable `interstellar_transit` rather than Stable Warp Drive specifically
- Stable Warp and stabilized wormholes can both grant reliable `interstellar_transit`
- long-range warp can grant `extended_interstellar_transit`
- node capability outputs grant at Mature by default
- explicit `technology_grants.json` rules handle early capability timing, structural changes, and deployment events
- Prototype Warp can grant `experimental_interstellar_transit` at Demonstrated
- Biofabrication at Mature grants acquired civilization trait `biological_fabrication_possible`
- Synthetic Cognition and Whole-Mind Emulation make persistent machine cognition possible but **do not themselves claim a machine population already exists**
- `machine_cognition_present` is granted only after the persistent-machine-cognition deployment event actually occurs

### Maturation / uncertainty

Canonical top-level states remain:

`Unknown -> Rumored -> Hypothesized -> Investigable -> Experimental -> Demonstrated -> Engineering -> Mature/Archived`

A disproven hypothesis is **Archived with resolution `disproven`**, not a new top-level state.

Research outcome rules:

- ordinary established engineering can suffer setbacks but does not randomly become physically impossible
- frontier engineering can have serious setbacks/partial success; hazards require explicit hazard profiles
- true hypotheses can be supported, refined, disproven, or produce anomalous results
- setbacks never erase all RP/progress
- repeated identical failure becomes less likely as constraints are learned
- disproof retains negative knowledge/field competence and can expose alternate/side paths
- side discoveries can create evidence, hypotheses, field competence, or reduced uncertainty but never hand out unrelated mature technology
- any remaining uncertainty can use a campaign-seeded deterministic stream for reproducibility/debugging
- directed projects can pause while preserving RP/knowledge and releasing assigned labs

Canonical machine-readable files:

- `data/research/v1/capability_model.json`
- `data/research/v1/technology_grants.json`
- `data/research/v1/maturation_model.json`
- `scripts/validate_research_maturation.py`

There are intentionally no duplicate capability-grant or maturation schemas.

Human-readable spec:

- `docs/RESEARCH_MATURATION_MODEL.md`

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

Adaptive Research keeps the universal catalog static and persists compact civilization state: mature IDs, visible candidates, active project summaries, sparse pressures, evidence, applicable mutable traits, lab allocations, field competence, capabilities, and compressed maturation history.

## Next action

For the dedicated Adaptive Research workstream:

1. finish and validate PR #13
2. keep `dev/adaptive-research` as the persistent branch after the milestone merge
3. continue auditing implementation-specific prerequisites that should instead be functional capability requirements
4. next design layer after maturation: field competence/specialization, research institutions/facility specialization, foreign scientist/tacit-knowledge transfer, and how research costs adapt to civilization history without becoming arbitrary percentage stacking
5. do not change gameplay VERSION until actual runtime/gameplay integration is intentionally implemented/validated

Other workstreams should use `WORKSTREAMS.md` and consume capability/research interfaces rather than editing this branch independently.
