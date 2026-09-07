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

## Adaptive Research milestone #1 — Possibility graph / RP + Pressure + Labs

Status: **merged/validated design/data/tooling**.

PR: **#11 — Adaptive Research possibility graph and research economy**

Merge commit: **`f70e122134e87c1449582b573c5e2db8b045d311`**

Validation passed:

- research catalog validator
- .NET restore/build
- Godot headless editor smoke
- Godot headless runtime smoke

Added/established:

- hidden Technology Possibility Graph architecture
- **330 normal/public possibility nodes**
- **20 domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**
- Research Points generated by Effective Research Labs
- pressure is contextual and opt-in as a hard gate, never implied just by complexity
- early one-program research with background science
- research-institution progression to 2 -> 4 -> lab-capacity-limited directed programs
- five additional domains: Agriculture/Biosphere, Economic/Trade, Cybernetics, Research Infrastructure/Metrology, Stellar Engineering
- machine validation of node IDs, counts, prerequisites, pressures, solution sets, research-capacity refs, and cycles

Gameplay VERSION remained `0.0.6-dev.1`.

## Adaptive Research milestone #2 — Emergence / evidence / pressure dynamics

Status: **merged/validated design/data/tooling**.

PR: **#12 — Adaptive Research emergence, evidence, and pressure dynamics**

Merge commit: **`95fa5c9e77642479eecc8f4183c91c06b3709f7e`**

Validation passed:

- strict research catalog validator
- .NET restore/build
- Godot headless editor smoke
- Godot headless runtime smoke

Added/established:

- **6 public applicability traits** based on biological/civilizational capability, not race IDs
- **9 public evidence types** with provenance/quality/confidence/intactness/custody concepts
- generation/decay rules for all **59 Research Pressures**
- sparse event/index-driven candidate emergence instead of scanning 330 nodes every tick
- no direct calendar-year technology unlocks
- no rank-based catch-up pressure
- enemy-relative pressure requires legitimate observation
- population/species traits apply only to relevant populations in multispecies civilizations
- mutable civilization traits can be acquired through technology and real deployment history
- CI validation for undefined traits/evidence and exact pressure-rule coverage

Gameplay VERSION remained `0.0.6-dev.1`.

## Adaptive Research milestone #3 — Capability interoperability / maturation

Status: **current in-progress design/data/tooling milestone**.

Persistent branch: **`dev/adaptive-research`**

PR: **#13 — Adaptive Research capability interoperability and maturation**

This milestone establishes the persistent research branch owned by the dedicated Adaptive Research chat/workstream. Other concurrent branches consume research interfaces rather than independently editing the canonical research graph/schema.

Current work includes:

- cross-lineage functional capabilities with civilization/population/installation scope
- functional capability requirements separate from implementation-specific knowledge prerequisites
- Prototype Warp requires spacecraft-construction capability rather than one exact shipyard knowledge path
- Wormhole Stabilization requires megastructure-construction capability
- Interstellar Logistics requires reliable interstellar transit rather than Stable Warp Drive specifically
- Stable Warp and stabilized wormholes can both provide reliable interstellar transit
- node capability outputs grant at Mature by default, avoiding duplicated grant definitions
- Prototype Warp can grant experimental interstellar transit early at Demonstrated
- Biofabrication can grant mutable civilization trait `biological_fabrication_possible`
- Synthetic Cognition and Whole-Mind Emulation enable a persistent-machine-cognition deployment event; `machine_cognition_present` is granted only after persistent autonomous machine cognition is actually instantiated
- research maturation through Experimental -> Demonstrated -> Engineering -> Mature
- ordinary engineering setbacks without random fundamental impossibility
- true hypotheses can be supported/refined/disproven or yield anomalous results
- disproven hypotheses are Archived with preserved negative knowledge
- setbacks do not erase all RP/progress and repeated identical failures become less likely through learning
- explicit hazard profiles rather than universal late-game disaster rolls
- side discoveries create related evidence/hypotheses/competence, never unrelated mature technologies
- campaign-seeded deterministic uncertainty for reproducible simulation/debugging
- second validator `scripts/validate_research_maturation.py` added to CI
- canonical maturation/grant files consolidated to `maturation_model.json` and `technology_grants.json`

Gameplay VERSION remains `0.0.6-dev.1` unless future runtime implementation intentionally changes it.

## Persistent workstream rule

Concurrent branch ownership is now recorded in `WORKSTREAMS.md`.

Adaptive Research/Technology uses persistent branch **`dev/adaptive-research`**. After each validated research milestone merges, this branch should be advanced from the new `main` and reused for the next research milestone rather than creating a new research branch for every feature.

## Update rule

After every validated gameplay milestone merge:

1. record version/merge commit/acceptance here
2. update `PROJECT_STATE.md`
3. record durable design changes in `DECISION_LOG.md`
4. keep incomplete/unvalidated work clearly labeled
5. keep design/data merges distinct from gameplay version promotion

After each validated Adaptive Research design/data merge, also update this history with the PR/merge commit and advance `dev/adaptive-research` from the new `main` before continuing.
