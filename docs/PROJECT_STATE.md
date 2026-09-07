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
4. canonical system specifications
5. `DECISION_LOG.md` for durable decisions/supersessions
6. `ROADMAP.md`
7. current validated source/data on `main`
8. unvalidated branches/commits

Do not silently resurrect a superseded rule.

## Gameplay baseline known to this research workstream

As of 2026-09-07, the gameplay baseline recorded by the Adaptive Research workstream remains:

- gameplay version: **`0.0.6-dev.1`**
- validated gameplay merge commit: **`91a2204b96ed08c2178875cbc8d5b0bc378372ad`**
- save format: v6

Research documentation/design-data merges do **not** promote gameplay VERSION. Other workstream owners must update the canonical gameplay baseline if they merge a later accepted gameplay version.

The Adaptive Research workstream does not own or reinterpret other development branches. See `WORKSTREAMS.md` and current repository/PR state for those branches.

## Adaptive Research workstream

- persistent branch: **`dev/adaptive-research`**
- owner: dedicated Adaptive Research / Technology chat
- scope: technology graph/data, RP/Pressure/Labs, emergence, evidence, applicability, capabilities, maturation, competence, research facilities, tacit knowledge, foreign technology, technology transfer/licensing/brokerage, starting research histories, research runtime/view contracts, research UI/data contracts, and research validators/docs

Other concurrent branches may consume research interfaces but should not independently edit the canonical research graph/schema files while this workstream is active without coordination.

## Durable research direction

- no fully visible universal technology tree
- no giant separate fixed tree per species
- hidden universe-scale Technology Possibility Graph
- player sees only the civilization's current scientific horizon
- branches emerge from legitimate knowledge, need, evidence, environment, experiments, conflict, institutions, and foreign contact
- Research Labs generate RP
- Research Pressure is contextual need/evidence and is a hard gate only where explicitly configured
- Effective Research Labs are physical scientific capacity
- early directed research is intentionally simple; later institutional technology unlocks parallel programs
- functional requirements use cross-lineage capabilities when the implementation does not matter
- research knowledge is distinct from physical deployment
- mature foreign technology is not automatically usable/reproducible by another civilization
- scientific history creates field competence and tacit expertise rather than arbitrary research bonus stacking
- starting civilizations are composed from past scientific history, not assigned fixed future trees
- all research systems remain bounded/event-driven for long-campaign performance

## Public Adaptive Research catalog

Current normal/public seed:

- **330 possibility nodes**
- **20 domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**
- **6 public applicability traits**
- **9 public evidence types**
- **35 canonical knowledge fields**

Exact secret discovery triggers, probabilities, rare secret technologies, artifact chains, and hidden special-AI conditions are intentionally excluded from public data.

## Milestone #1 — possibility graph / RP + Pressure + Labs

Validated and merged through PR #11 at **`f70e122134e87c1449582b573c5e2db8b045d311`**.

Established the 330-node/20-domain possibility graph, RP + Pressure + Labs research economy, staged directed-program concurrency, alternative solution sets, and `validate_research_catalog.py`.

## Milestone #2 — emergence / evidence / pressure dynamics

Validated and merged through PR #12 at **`95fa5c9e77642479eecc8f4183c91c06b3709f7e`**.

Established formal applicability traits/evidence types, rules for all 59 pressures, sparse event/index-driven candidate emergence, no calendar unlocks, no rank-based catch-up pressure, legitimate-information requirements, and population-scoped applicability.

## Milestone #3 — capability interoperability / maturation

Validated and merged through PR #13 at **`101b01a1d6407fee2912c7e8b9175f196bb75ca9`**.

Established implementation knowledge vs functional capability prerequisites, capability scopes, path-lock repairs, Mature/early/deployment grants, knowledge-vs-deployment distinction, maturation/hypothesis outcomes, non-destructive setbacks, bounded side discoveries, and `validate_research_maturation.py`.

## Milestone #4 — competence / institutions / tacit knowledge

Validated and merged through PR #17 at **`64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`**.

Established 35 knowledge fields, theoretical/experimental/engineering competence, limited related-field transfer, practice atrophy without deletion of archived knowledge, bottleneck-sensitive readiness, specialized scientific facilities, tacit-knowledge assets/expert cohorts/training pipelines, Access -> Interpreted -> Codified -> Trained -> Native Practice assimilation, one bounded Project Readiness efficiency, and `validate_research_competence.py`.

## Milestone #5 — foreign technology / exchange / research UI

Validated and merged through PR #30 at **`d7bdaa8ee67461ba1811121e9af4719de6583a8d`**.

Final-head validation passed all four research validators, .NET restore/build, Godot headless editor smoke, and Godot headless runtime smoke.

Established:

- four foreign-tech axes: Understanding / Operability / Reproduction / Adaptation
- real compatibility/dependency constraints and event-driven reassessment
- composed technology-transfer packages mapped to canonical evidence/tacit assets
- legal rights separate from technical ability; licenses are law, not physics
- buyer-specific technology value rather than a fixed universal price
- foreign acquisition never directly sets native technology Mature
- evolving research UI with no unknown placeholders, visible-only edges, stable branch anchoring, blocker explanations, four-axis foreign-tech display, and no per-frame hidden-graph scan
- `validate_research_transfer_ui.py`

Canonical milestone #5 files include `foreign_technology_model.json`, `technology_exchange_model.json`, `research_ui_contract.json`, `RESEARCH_FOREIGN_TECH_MODEL.md`, and `RESEARCH_UI_MODEL.md`.

## Milestone #6 — starting histories / runtime / materialized view

**Current in-progress research milestone.**

- persistent branch: **`dev/adaptive-research`**
- current PR: **#44 — Adaptive Research starting histories and runtime/view contracts**

Current design/data establishes:

### Starting scientific histories

- starting civilizations compose **one base-era fragment plus reusable historical fragments**; fragments are not races and do not define future trees
- **12 reusable history fragments** currently cover early-space science, orbital industry, fission/storage, fusion transition, automation, deep-space observation/comms, closed-loop metabolic habitation, structural materials, economic/logistical competence, machine-origin science, and high-/low-gravity experience
- **4 reference starts** validate the architecture: human-like Solar 2050, synthetic early-space, high-gravity metabolic, and low-gravity metabolic
- the human-like 2050 reference leaves practical `fusion_power` **Investigable**, not Mature, and does not seed an FTL hypothesis merely because the date is 2050
- synthetic starts can begin with `machine_cognition_present` as historical reality without claiming the human `synthetic_cognition` lineage
- high-/low-gravity history seeds real pressure/competence without preselecting medicine/genetics/cybernetics/habitat solutions
- complete starting compositions are prerequisite-closed and starting institutions must have valid historical enablers
- competence fragments combine by strongest justified component with a cap, never additive percentages
- future research horizon is recomputed from the composed current state; unknown future nodes/placeholders are never stored

Canonical files:

- `starting_research_profile_contract.json`
- `starting_research_fragments.json`
- `starting_reference_profiles.json`
- `starting_profile_index.json`

### Authoritative research runtime boundary

- research keeps sparse per-civilization runtime state while static catalogs/indexes remain shared
- other workstreams push normalized factual events/metrics and consume stable queries/capabilities rather than mutating graph internals
- cross-workstream inputs include condition metrics, evidence, facility changes, applicability changes, deployment events, foreign assets, and legitimate foreign capability observations
- queries include capability checks, visible maturity, recognized blockers, materialized research view, foreign-tech assessment, package utility, and eligible research capacity
- candidate emergence/readiness/foreign reassessment/UI projection remain indexed, event-driven, or low-frequency
- no full-graph per-tick scan and no per-frame research projection rebuild
- saves exclude the static catalog, reconstructible indexes, and UI cache
- fair-information player/AI symmetry remains mandatory

Canonical file: `research_runtime_contract.json`.

### Materialized view-model contract

- UI consumes a read-only civilization research projection containing visible nodes/edges only, active projects, recognized pressures/blockers, optional competence details, known foreign-tech summaries, and bounded history
- unknown future nodes and hidden node counts never project
- UI commands are requests; authoritative research revalidates visibility, capacity, facilities, evidence, pressure, capability, and applicability before acting
- projection is revision-cached, history/foreign lists are paged or virtualized, and field details are built on demand

Canonical file: `research_view_model_contract.json`.

### Milestone #6 validation

A fifth research validator is now wired into CI:

- `validate_research_start_runtime.py`

It composes all reference starts and validates node prerequisite closure, fields, traits, pressures, institutions, runtime event/query integrity, view secrecy, blocker IDs, and command contracts.

The first PR run passed all five research validators before the normal build/smoke stages.

## Research CI contract

Research changes now pass five research validators before normal build/smoke gates:

1. `validate_research_catalog.py`
2. `validate_research_maturation.py`
3. `validate_research_competence.py`
4. `validate_research_transfer_ui.py`
5. `validate_research_start_runtime.py`
6. .NET restore/build
7. pinned Godot editor smoke
8. Godot runtime smoke

## Early-release campaign horizon direction

- demo: roughly 100–150 meaningful in-game years
- initial paid Early Access: roughly **500 years** of officially supported meaningful simulation/content
- engineering soak: at least **1,000 simulated years** without unbounded memory/save/performance failure
- no hard year-based game-over

## Persistence / performance direction

Adaptive Research keeps static catalogs shared and saves compact civilization-specific state only: visible/mature/archived node state, active projects, lab allocations, sparse pressure/evidence/traits, capabilities, relevant field competence, strategically meaningful knowledge assets, foreign-tech assessments/packages, and compressed history.

Starting-profile composition runs only at new-game/migration boundaries. Candidate emergence, competence/readiness, foreign-tech assessment, and UI updates are event-driven or low-frequency and must not scan/process the full graph every simulation tick/frame.

## Next action for this workstream

1. finish PR #44 on the final continuity-aware branch head
2. require all five research validators + .NET + Godot gates
3. verify changed files remain research-owned
4. merge only after final validation
5. continue on `dev/adaptive-research`
6. next research layer after milestone #6: research priorities/funding behavior, civilization scientific culture/institution incentives, and AI research planning using the same adaptive inputs without creating hidden catch-up cheats or direct player tech selection
7. do not promote gameplay VERSION until actual research runtime/gameplay integration is intentionally implemented and validated
