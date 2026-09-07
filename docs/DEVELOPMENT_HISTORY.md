# Development History

This is the concise chronological record of validated gameplay milestones and major design/data state changes. It keeps accepted design foundations separate from gameplay-version promotion.

## Gameplay milestones through 0.0.6

### 0.0.1 — Simulation foundation

Status: merged/validated.

Godot 4.7.2 .NET/C# foundation, plain-C# simulation core, deterministic galaxy generation, real-time clock/backlog protection, versioned persistence, diagnostics/support bundles, and .NET + pinned Godot CI.

### 0.0.2-dev.1 — Civilizations / fog of war

Status: merged/validated. Merge commit: `2aaa6d3aeead400882c8214005e3bd78cfe17eaa`.

### 0.0.3-dev.1 — Exploration / first contact

Status: merged/validated. Merge commit: `8683acebb1860ff3d94838656c65c8a82cd90d68`.

### 0.0.4-dev.1 — Colonies / basic economy

Status: merged/validated. Merge commit: `ebcba9239a0d12d0c5c99f41937193176b6c8664`.

### 0.0.5-dev.1 — 2050 Pre-Warp Dawn

Status: merged/validated. Merge commit: `c457f5e57c51057e86b857d0d828ff42d75e8f8b`.

### 0.0.6-dev.1 — Construction-driven development

Status: validated gameplay baseline recorded by this research workstream. Merge commit: `91a2204b96ed08c2178875cbc8d5b0bc378372ad`.

Other gameplay workstreams should update the canonical baseline if they merge later accepted gameplay versions. Adaptive Research does not own or reinterpret other gameplay branches; see `WORKSTREAMS.md` and current repository state.

## Major design evolution relevant to research

Durable direction established after the early prototype includes:

- realism-first consequences instead of arbitrary restrictions
- meaningful 2050 solar-system civilization phase
- logistics/endurance/gravity/life support/radiation/food/fabrication alongside propulsion
- species-relative habitability and biological diversity
- Adaptive Research rather than fixed universal or per-species trees
- foreign technology can be incompatible, dangerous, incomprehensible, dependent, or valuable to third parties
- technology can become a diplomatic/economic commodity
- long campaigns require bounded state and history compression
- initial paid Early Access design horizon roughly 500 meaningful years with 1,000-year engineering soak testing

## Adaptive Research milestone #1 — possibility graph / RP + Pressure + Labs

Status: **merged/validated design/data/tooling**.

PR #11, merge commit **`f70e122134e87c1449582b573c5e2db8b045d311`**.

Established the hidden 330-node / 20-domain Technology Possibility Graph, 59 Research Pressure types, 15 alternative-solution sets, RP + Pressure + Effective Research Labs, staged directed-program concurrency, and `validate_research_catalog.py`.

Gameplay VERSION was not promoted.

## Adaptive Research milestone #2 — emergence / evidence / pressure dynamics

Status: **merged/validated design/data/tooling**.

PR #12, merge commit **`95fa5c9e77642479eecc8f4183c91c06b3709f7e`**.

Established 6 public applicability traits, 9 evidence types, rules for all 59 pressures, sparse event/index candidate emergence, no calendar unlocks, no rank-based catch-up pressure, fair-information foreign observation, and population-scoped applicability.

Gameplay VERSION was not promoted.

## Adaptive Research milestone #3 — capability interoperability / maturation

Status: **merged/validated design/data/tooling**.

PR #13, merge commit **`101b01a1d6407fee2912c7e8b9175f196bb75ca9`**.

Established implementation-specific knowledge prerequisites vs functional cross-lineage capability requirements, scoped capabilities, removal of hidden FTL/logistics path locks, knowledge vs physical deployment, Experimental -> Demonstrated -> Engineering -> Mature/Archived maturation, meaningful hypothesis resolution, non-destructive setbacks, side discoveries, and `validate_research_maturation.py`.

Gameplay VERSION was not promoted.

## Adaptive Research milestone #4 — competence / institutions / tacit knowledge

Status: **merged/validated design/data/tooling**.

PR #17, merge commit **`64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`**.

Final-head validation passed all three research validators, .NET restore/build, Godot editor smoke, and Godot runtime smoke.

Established:

- 35 canonical knowledge fields used by all 330 public nodes
- theoretical / experimental / engineering competence
- sparse competence, related-field spillover, and active-practice atrophy without deleting archived knowledge
- bottleneck-sensitive Project Readiness
- specialized research institutions/facility capabilities instead of `+research%` buildings
- tacit assets: records, datasets, protocols, prototypes, tooling, expert cohorts, operating institutions, training pipelines
- Access -> Interpreted -> Codified -> Trained -> Native Practice foreign/tacit assimilation
- one bounded Project Readiness efficiency rather than modifier stacking
- `validate_research_competence.py`

Gameplay VERSION was not promoted.

## Adaptive Research milestone #5 — foreign technology / exchange / research UI

Status: **merged/validated design/data/tooling**.

PR #30, merge commit **`d7bdaa8ee67461ba1811121e9af4719de6583a8d`**.

Final-head validation passed all four research validators, .NET restore/build, Godot headless editor smoke, and Godot headless runtime smoke.

Established:

### Foreign technology

- four independent axes: Understanding / Operability / Reproduction / Adaptation
- real compatibility constraints across science, materials, power, manufacturing, infrastructure, biology/environment, interfaces/software, consumables, expertise, and hazards
- operation without understanding and understanding without reproduction are valid states
- foreign-process reproduction can exist without native-process mastery
- native derivatives/hybrid lineages do not require exact copying
- foreign-tech assessment is event-driven and never directly sets native technology Mature

### Technology exchange

- transfer packages compose observations, theory, datasets, blueprints, process documentation, hardware, tooling, experts, training, and operating institutions
- components map to canonical evidence/tacit assets
- legal rights are separate from technical ability
- licenses are law, not physics; technically possible violations remain possible and external diplomacy/law/intelligence systems own consequences
- technology has no universal fixed price; buyer value is contextual and can include brokerage value when the holder cannot use the technology itself

### Research UI

- unknown nodes/placeholder slots never render
- visible edges connect visible nodes only
- branches grow near stable anchors while preserving viewport/selection where practical
- blocker explanations distinguish labs, facilities, evidence, pressure, capabilities, and coordination
- foreign tech shows all four axes separately
- exchange UI separates contents, rights, and technical ability
- mature/archive-heavy history can collapse outside the active horizon
- no per-frame hidden-graph scan

Added `validate_research_transfer_ui.py` as the fourth research validator.

Gameplay VERSION was not promoted.

## Adaptive Research milestone #6 — starting histories / runtime / materialized view

Status: **current in-progress design/data/tooling milestone**.

Persistent branch: **`dev/adaptive-research`**  
PR: **#44 — Adaptive Research starting histories and runtime/view contracts**

Current work adds:

### Starting scientific histories

- one base-era fragment plus reusable historical fragments rather than species-specific future trees
- 12 reusable fragments covering early-space science, orbital industry, fission/storage, fusion transition, automation, deep-space observation/comms, closed-loop metabolic habitation, structural materials, economic/logistical competence, machine-origin science, and high-/low-gravity experience
- 4 reference compositions: human-like Solar 2050, synthetic early-space, high-gravity metabolic, low-gravity metabolic
- human-like 2050 leaves Practical Fusion only **Investigable** and seeds no FTL hypothesis from calendar date alone
- machine-origin start can possess machine cognition as a historical fact without following the human Synthetic Cognition research lineage
- gravity-history profiles create real pressure/competence without preselecting which solution branch wins
- starting compositions must be prerequisite-closed and starting institutions must have historically valid enablers
- starting competence fragments combine by strongest justified component with a cap, never additive bonus stacking
- visible starting research horizon is recomputed after composition; no hidden future placeholders are stored

### Runtime/integration boundary

- sparse per-civilization authoritative research state; static catalogs/indexes shared globally
- external workstreams push normalized factual events/metrics and consume stable capability/query interfaces
- no caller needs to know which implementation supplied a functional capability
- pressure/evidence/trait/capability/field/foreign indexes drive bounded candidate/reassessment work
- no full-graph per-tick scan
- save contract excludes static catalog, reconstructible indexes, and UI caches
- fair-information player/AI symmetry remains explicit

### Materialized research view

- read-only projection contains visible nodes/edges, active projects, recognized pressures/blockers, optional competence detail, known foreign technology, and bounded history
- unknown nodes and hidden node counts never project
- UI/AI commands are requests revalidated by authoritative runtime
- projections are revision-cached and large histories/lists are paged/virtualized

### Validation

Added `validate_research_start_runtime.py` as the fifth research validator. The first PR run successfully composed all four reference starts and passed all five research validators before normal build/smoke validation.

Canonical new files include:

- `starting_research_profile_contract.json`
- `starting_research_fragments.json`
- `starting_reference_profiles.json`
- `starting_profile_index.json`
- `research_runtime_contract.json`
- `research_view_model_contract.json`
- `RESEARCH_START_RUNTIME_MODEL.md`
- `validate_research_start_runtime.py`

## Persistent workstream rule

Adaptive Research/Technology uses **`dev/adaptive-research`**. Each validated research milestone merges from this workstream and continued research remains owned by this dedicated chat/workstream.

## Update rule

After each validated Adaptive Research design/data merge:

1. record PR/merge commit here
2. update `PROJECT_STATE.md`
3. update durable research decisions
4. continue on `dev/adaptive-research`
5. keep design/data acceptance distinct from gameplay VERSION promotion
