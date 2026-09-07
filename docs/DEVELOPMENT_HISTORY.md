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

Status: **validated gameplay baseline recorded by this workstream as of 2026-09-07**.

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

Other gameplay workstreams should update the canonical baseline if they merge a later accepted gameplay version.

## 0.0.7 — Shipbuilding

The previous record marked this branch paused/incomplete/unvalidated. The Adaptive Research workstream does not own it and does not reinterpret its current state. See `WORKSTREAMS.md` / latest repository state for any later status.

## Major design evolution after 0.0.6

Durable direction established during design work includes:

- realism-first causes/consequences instead of arbitrary restrictions
- conquest without mandatory abstract claim tokens
- borders as political warnings rather than force fields
- natural logistics/political/administrative consequences of power
- relationships/intelligence fade into history and rumor
- human-like 2050 start includes meaningful orbital/lunar/Mars presence
- pre-warp gameplay is a real solar-system civilization phase
- FTL practical range depends on logistics/endurance/support infrastructure
- gravity, life support, radiation, food/fabrication, and long-duration habitation matter
- mature lower-level systems become automatable
- planetary gravity and multigenerational adaptation matter
- species-relative habitability and biological diversity matter
- technology becomes civilization-specific through Adaptive Research rather than separate handcrafted species trees
- foreign technology can be incompatible, dangerous, incomprehensible, or tradable to third parties
- long campaigns require bounded state, history compression, tiered simulation detail, and persistent cold storage
- initial paid Early Access design horizon targets roughly 500 meaningful years, with 1,000-year engineering soak testing

## Adaptive Research milestone #1 — Possibility graph / RP + Pressure + Labs

Status: **merged/validated design/data/tooling**.

PR: **#11**

Merge commit: **`f70e122134e87c1449582b573c5e2db8b045d311`**

Validation passed research catalog, .NET build, Godot editor smoke, and Godot runtime smoke.

Established:

- hidden Technology Possibility Graph
- **330 normal/public possibility nodes**
- **20 domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**
- RP generated by Effective Research Labs
- pressure is contextual/opt-in as a hard gate
- early one-program research with background science
- research-institution progression to 2 -> 4 -> lab-capacity-limited directed programs
- machine validation of nodes/prerequisites/pressures/solution sets/cycles

Gameplay VERSION was not promoted by this design/data merge.

## Adaptive Research milestone #2 — Emergence / evidence / pressure dynamics

Status: **merged/validated design/data/tooling**.

PR: **#12**

Merge commit: **`95fa5c9e77642479eecc8f4183c91c06b3709f7e`**

Validation passed strict research catalog, .NET build, Godot editor smoke, and Godot runtime smoke.

Established:

- **6 public applicability traits** based on biological/civilizational capability, not race IDs
- **9 public evidence types** with provenance/quality/confidence concepts
- generation/decay rules for all 59 pressures
- sparse event/index-driven candidate emergence
- no direct calendar-year unlocks
- no rank-based catch-up pressure
- legitimate observation required for enemy-relative pressure
- population/species applicability remains scoped
- mutable civilization traits can arise through technology/deployment history

Gameplay VERSION was not promoted.

## Adaptive Research milestone #3 — Capability interoperability / maturation

Status: **merged/validated design/data/tooling**.

PR: **#13 — Adaptive Research capability interoperability and maturation**

Merge commit: **`101b01a1d6407fee2912c7e8b9175f196bb75ca9`**

Final-head validation passed:

- Adaptive Research catalog validator
- research maturation/capability validator
- .NET restore/build
- Godot headless editor smoke
- Godot headless runtime smoke

Established:

- persistent Adaptive Research branch `dev/adaptive-research`
- cross-lineage functional capabilities with civilization/population/installation scope
- functional requirements separate from implementation-specific knowledge prerequisites
- Prototype Warp requires spacecraft-construction capability rather than one exact shipyard path
- Wormhole Stabilization requires megastructure-construction capability
- Interstellar Logistics requires reliable interstellar transit rather than Stable Warp specifically
- different FTL implementations can provide the same functional transit capability
- node capability outputs grant at Mature by default
- Prototype Warp can grant experimental transit at Demonstrated
- Biofabrication can grant `biological_fabrication_possible`
- Synthetic Cognition / Whole-Mind Emulation enable machine-cognition deployment; the machine-cognition trait requires actual instantiation
- Experimental -> Demonstrated -> Engineering -> Mature maturation
- genuine hypotheses can be supported/refined/disproven or yield anomalous results
- setbacks retain knowledge/progress; repeated failure teaches
- explicit hazard profiles instead of universal late-game catastrophe rolls
- bounded side discoveries
- campaign-seeded deterministic research uncertainty
- `validate_research_maturation.py` in CI
- canonical schemas consolidated to `capability_model.json`, `technology_grants.json`, and `maturation_model.json`

Gameplay VERSION was not promoted.

## Adaptive Research milestone #4 — Competence / institutions / tacit knowledge

Status: **current in-progress design/data/tooling milestone**.

Persistent branch: **`dev/adaptive-research`**

PR: **#17 — Adaptive Research competence, facilities, and tacit knowledge**

Current work adds:

- **35 canonical knowledge fields** used as real references by all 330 nodes
- theoretical / experimental / engineering competence components
- sparse competence state and limited related-field transfer
- active competence atrophy without deleting archived knowledge
- stage-specific, bottleneck-sensitive multidisciplinary readiness
- specialized research institutions/facility capabilities providing eligible lab capacity rather than percentage bonuses
- hard stage facility requirements where physical experimental/prototype conditions are genuinely necessary
- tacit knowledge assets: records, datasets, protocols, prototypes, tooling, expert cohorts, operating institutions, training pipelines
- foreign expertise assimilation: Access -> Interpreted -> Codified -> Trained -> Native Practice
- aggregate expert cohorts rather than one object per scientist
- replacement of vague contextual-cost stacking with inherent base project RP + one bounded Project Readiness efficiency
- readiness derives from applicable competence, facilities, evidence, and tacit expertise
- Research Pressure remains availability/urgency, not a speed multiplier
- hard missing facilities/evidence/materials block or pause relevant stages rather than becoming opaque RP taxes
- third validator `scripts/validate_research_competence.py` wired into CI

Initial PR #17 research-competence validation passed all 330 node field references and facility/tacit/readiness integrity checks before documentation finalization.

## Persistent workstream rule

Concurrent ownership is recorded in `WORKSTREAMS.md`.

Adaptive Research/Technology uses **`dev/adaptive-research`**. After each validated research milestone merges, advance this same branch from the new `main` and continue rather than creating a new permanent research branch per feature.

## Update rule

After validated gameplay merges, the owning workstream updates gameplay version/merge acceptance.

After each validated Adaptive Research design/data merge:

1. record the PR/merge commit here
2. update `PROJECT_STATE.md`
3. record durable rules in `DECISION_LOG.md`
4. advance `dev/adaptive-research` from new `main`
5. keep design/data acceptance distinct from gameplay VERSION promotion
