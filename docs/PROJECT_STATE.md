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
4. canonical specs: `GAME_DIRECTION.md`, `ADAPTIVE_RESEARCH_SYSTEM.md`, `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, `RESEARCH_EMERGENCE_MODEL.md`, `RESEARCH_MATURATION_MODEL.md`, `RESEARCH_COMPETENCE_MODEL.md`, `ENGINEERING_GUARDRAILS.md`
5. `DECISION_LOG.md` for historical decisions/reasons and explicit supersessions
6. `ROADMAP.md`
7. current validated source/data on `main`
8. unvalidated branches/commits

Do not silently resurrect an older superseded rule.

## Authoritative validated gameplay baseline

As of 2026-09-07:

- gameplay version recorded on this research baseline: **`0.0.6-dev.1`**
- validated gameplay merge commit: **`91a2204b96ed08c2178875cbc8d5b0bc378372ad`**
- save format: v6
- validation gate: .NET restore/build, pinned Godot 4.7.2 .NET download/SHA verification, headless editor smoke, headless runtime smoke

Documentation/design-data merges do **not** automatically promote the gameplay version. Other workstreams must update this section if they merge a later validated gameplay milestone.

## Concurrent workstreams

Canonical workstream ownership is recorded in `WORKSTREAMS.md`.

### Adaptive Research / Technology

- persistent branch: **`dev/adaptive-research`**
- owner: dedicated Adaptive Research chat/workstream
- scope: technology graph/data, RP/Pressure/Labs, emergence, evidence, applicability, capabilities, maturation, competence, research facilities, tacit knowledge, foreign-tech research architecture, research validation/docs

Other branches may consume the research interfaces but should not independently edit canonical research graph/schema files while this workstream is active without coordination.

## Other workstream status preserved from prior record

The prior project-state record listed `dev/0.0.7-shipbuilding` as paused/incomplete/unvalidated with integration-risk commits. This research workstream does not update or reinterpret that branch. A dedicated owner of that branch should update the canonical record if its status changes.

## Durable game direction relevant to research

- campaign begins January 1, 2050
- human-like start already has meaningful orbital/lunar/Mars infrastructure
- pre-FTL solar-system development must be meaningful
- logistics/endurance/life support/radiation/gravity matter alongside propulsion
- species-relative habitability and technological applicability
- technology paths diverge because of actual biology, environment, history, needs, evidence, institutions, and foreign contact
- foreign technology can be incompatible, dangerous, incomprehensible, or valuable to third parties
- long-campaign scalability and bounded state are release requirements

## Adaptive Research milestone #1 — possibility graph / research economy

Validated and merged through PR #11 at **`f70e122134e87c1449582b573c5e2db8b045d311`**.

Established:

- **330 normal/public possibility nodes**
- **20 domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**
- Research Points + Research Pressure + Effective Research Labs
- pressure is an explicit selective gate, never implied by complexity
- staged directed research: 1 -> 2 -> 4 -> lab-capacity-limited programs
- catalog validator in CI

## Adaptive Research milestone #2 — emergence / evidence / pressure dynamics

Validated and merged through PR #12 at **`95fa5c9e77642479eecc8f4183c91c06b3709f7e`**.

Established:

- **6 public applicability traits**
- **9 public evidence types**
- generation/decay rules for all 59 Research Pressures
- sparse event/index-driven candidate emergence
- no calendar-year unlocks or rank-based catch-up pressure
- enemy-relative pressure requires legitimate observation
- population/species applicability remains population-scoped
- mutable civilization traits can emerge from real technological/deployment history

## Adaptive Research milestone #3 — capability interoperability / maturation

**Validated and merged** through PR #13 at:

- merge commit: **`101b01a1d6407fee2912c7e8b9175f196bb75ca9`**
- gameplay VERSION was not promoted by this design/data merge

Established:

- implementation-specific prerequisites mean genuine knowledge lineage
- generic functional dependencies use cross-lineage capabilities
- capability scopes distinguish civilization / population-or-species / colony-or-installation context
- Prototype Warp requires `spacecraft_construction` rather than one exact shipyard knowledge path
- Wormhole Stabilization requires `megastructure_construction`
- Interstellar Logistics requires `interstellar_transit` rather than Stable Warp specifically
- Stable Warp and stabilized wormholes can both grant reliable interstellar transit
- node capability outputs grant at Mature by default
- `technology_grants.json` handles early grants, structural changes, and deployment events
- Prototype Warp can grant `experimental_interstellar_transit` at Demonstrated
- Biofabrication can grant `biological_fabrication_possible`
- Synthetic Cognition / Whole-Mind Emulation enable machine-cognition deployment; `machine_cognition_present` requires actual persistent machine cognition to be instantiated
- maturation uses Experimental -> Demonstrated -> Engineering -> Mature with Archived resolutions
- genuine hypotheses can be supported/refined/disproven/anomalous
- setbacks preserve scientific progress and repeated failures create learning
- side discoveries are related/bounded and never grant unrelated mature technology
- `validate_research_maturation.py` is part of CI

Canonical files: `capability_model.json`, `technology_grants.json`, `maturation_model.json`, `RESEARCH_MATURATION_MODEL.md`.

## Adaptive Research milestone #4 — competence / institutions / tacit knowledge

**Current in-progress research milestone.**

- persistent branch: **`dev/adaptive-research`**
- current PR: **#17 — Adaptive Research competence, facilities, and tacit knowledge**

Current design/data adds:

- **35 canonical knowledge fields** referenced by the 330 nodes
- sparse civilization-specific competence in three dimensions: theoretical, experimental, engineering
- competence grows from actual research/experimentation/engineering and limited related-field transfer
- active competence can atrophy while archived knowledge remains known
- multidisciplinary readiness is bottleneck-sensitive so one excellent field cannot erase a severe gap in another
- specialized research institutions provide eligible Effective Research Lab capacity and physical research capabilities rather than flat percentage bonuses
- explicit stage-level facility requirements for projects that genuinely need special experiments/prototypes
- tacit knowledge assets: records, datasets, protocols, prototypes, tooling, expert cohorts, operating institutions, training pipelines
- foreign/tacit knowledge can progress Access -> Interpreted -> Codified -> Trained -> Native Practice
- expert cohorts are aggregated rather than simulated one scientist at a time
- legacy contextual-cost multiplier is replaced by inherent base project RP plus one bounded Project Readiness efficiency
- readiness is derived from applicable field competence, facility readiness, evidence, and tacit expertise
- non-applicable readiness components are omitted/renormalized instead of inventing neutral bonuses
- Research Pressure remains an urgency/availability input, **not a research-speed multiplier**
- hard missing facilities/evidence/materials can block/pause a stage rather than becoming giant opaque RP penalties
- `validate_research_competence.py` is added as a third research CI gate

Canonical milestone #4 files:

- `knowledge_fields.json`
- `research_competence_model.json`
- `research_facility_model.json`
- `tacit_knowledge_model.json`
- `project_readiness_model.json`
- `RESEARCH_COMPETENCE_MODEL.md`
- `validate_research_competence.py`

## Early-release campaign horizon direction

- public demo: roughly 100–150 meaningful in-game years
- initial paid Early Access: roughly **500 years of officially supported meaningful content/simulation**
- engineering soak requirement: at least **1,000 simulated years** without unbounded memory/save/performance failure
- no hard year-based game-over

## Persistence/scalability direction

Long campaigns should use tiered state: hot active RAM state, bounded warm summaries/caches, and cold/dormant/historical state in proven embedded storage behind a game-owned abstraction.

Adaptive Research keeps the universal graph/static support catalogs shared and persists compact civilization state only: mature/archived IDs, visible candidates, active projects, sparse pressures/evidence/traits, lab allocations, capabilities, relevant field competence, strategically meaningful knowledge assets, and compressed maturation history.

## Next action for this workstream

1. finish PR #17 and keep all three research validators green
2. verify .NET + Godot gates on the final PR head
3. merge only after validation
4. advance `dev/adaptive-research` from the new `main` merge commit and continue on the same persistent branch
5. next research layer after milestone #4: formal foreign-technology compatibility/reproduction states, technology trade/licensing knowledge packages, and research UI contracts using the already-defined evidence/tacit/capability models
6. do not change gameplay VERSION until actual research runtime/gameplay integration is intentionally implemented and validated
