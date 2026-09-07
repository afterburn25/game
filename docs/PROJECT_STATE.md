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
- scope: technology graph/data, RP/Pressure/Labs, emergence, evidence, applicability, capabilities, maturation, competence, research facilities, tacit knowledge, foreign technology, technology transfer/licensing/brokerage, starting research histories, runtime/view contracts, research agendas/scientific culture/fair AI planning, research UI/data contracts, validators/docs

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
- functional requirements use cross-lineage capabilities when implementation does not matter
- research knowledge is distinct from physical deployment
- mature foreign technology is not automatically usable/reproducible by another civilization
- scientific history creates field competence and tacit expertise rather than arbitrary research bonus stacking
- starting civilizations are composed from past scientific history, not assigned fixed future trees
- high-level research policy guides attention/capacity planning but cannot reveal unknown technology or directly multiply RP
- scientific culture is mutable civilization/institution behavior, not an immutable species research bonus
- AI research planning uses the same materialized visible horizon and authoritative blockers as player-facing systems
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

Established four foreign-tech axes (Understanding / Operability / Reproduction / Adaptation), compatibility/dependency constraints, composed technology-transfer packages, legal rights separate from technical ability, buyer-specific technology value, evolving visible-only research UI, and `validate_research_transfer_ui.py`.

## Milestone #6 — starting histories / runtime / materialized view

**Validated and merged** through PR #44 at:

- merge commit: **`859099ee3048a2788aa32b33c5ca46aeaee00df9`**

The original PR had a history-only conflict because the research branch continued from the PR #30 head while `main` contained the PR #30 merge/continuity commits. This was resolved with explicit two-parent commit `93930301055cbf0e0c2cd15c1733fecb7b853e12` using the already-validated research tree; no force overwrite or content loss occurred. The conflict-resolved head then passed the full validation gate before merge.

Final validation passed:

- all five research validators
- .NET restore/build
- Godot headless editor smoke
- Godot headless runtime smoke

Established:

### Starting scientific histories

- one base-era fragment plus reusable historical fragments rather than species-specific future trees
- **12 reusable history fragments** covering early-space science, orbital industry, fission/storage, fusion transition, automation, deep-space observation/comms, closed-loop metabolic habitation, structural materials, economic/logistical competence, machine-origin science, and high-/low-gravity experience
- **4 reference starts**: human-like Solar 2050, synthetic early-space, high-gravity metabolic, low-gravity metabolic
- human-like 2050 leaves `fusion_power` Investigable and seeds no FTL hypothesis by date alone
- synthetic starts may have `machine_cognition_present` as historical reality without following human Synthetic Cognition lineage
- high-/low-gravity history creates need/competence without preselecting a solution
- complete starting compositions are prerequisite-closed and institution enablers are validated
- competence fragments combine by strongest justified component with a cap, never additive percentages
- future horizon is recomputed after composition; unknown future nodes/placeholders are never stored

### Runtime / view contracts

- sparse per-civilization authoritative research state; static catalogs/indexes remain shared
- other workstreams push factual events/metrics and consume stable queries/capabilities rather than mutating research internals
- no full-graph per-tick scan and no per-frame hidden-graph UI rebuild
- saves exclude static catalog, reconstructible indexes, and UI cache
- materialized UI projection is read-only and visible-only
- UI/AI commands are requests revalidated authoritatively
- `validate_research_start_runtime.py` became the fifth validator

Canonical files include `starting_research_profile_contract.json`, `starting_research_fragments.json`, `starting_reference_profiles.json`, `starting_profile_index.json`, `research_runtime_contract.json`, `research_view_model_contract.json`, and `RESEARCH_START_RUNTIME_MODEL.md`.

## Milestone #7 — research agenda / scientific culture / fair AI planning

**Current in-progress research milestone.**

- persistent branch: **`dev/adaptive-research`**
- current PR: **#53 — Adaptive Research agenda, scientific culture, and fair AI planning**

Current design/data establishes:

### Research agenda

- high-level priorities over recognized domains, knowledge fields, Research Pressure problems, and known capabilities
- five priority levels: Deprioritized / Routine / Important / Strategic / Critical
- basic-vs-applied, competence-preservation, portfolio-diversity, and foreign-science orientations
- agenda affects background attention, visible-project recommendations, competence maintenance, and planning requests for labs/facilities/training/samples/foreign expertise
- agenda never directly adds RP, reveals Unknown technology, bypasses authoritative requirements, or creates physical assets/resources

### Scientific culture

A small mutable 12-axis civilization/institution vector currently covers:

- curiosity
- risk tolerance
- institutional conservatism
- threat sensitivity
- complacency tendency
- long-term orientation
- openness
- secrecy
- reproducibility rigor
- competitive prestige
- portfolio diversity
- commercialization orientation

Axes affect agenda/behavior only. They can change through government/history/institutions and are not permanent species research bonuses or direct RP multipliers.

### Natural complacency / catch-up

- a dominant civilization can reduce attention to a field only because it legitimately perceives current performance as adequate and its culture/institutions support complacency
- a weaker civilization can acquire strong need/evidence through actual losses, resource constraints, or observed capability gaps
- legitimate evidence of a rival closing the gap can raise the leader's urgency again
- no hidden `#1` penalty, catch-up multiplier, forced convergence, or unseen global technology ranking
- gaps may narrow, remain, widen, or reverse naturally

### Fair-information AI planning

- AI uses its own materialized visible horizon, pressures, competence, facilities, capabilities, strategy, culture, legitimately observed foreign capabilities, and known packages/constraints
- AI never queries Unknown graph nodes, exact unseen enemy technologies/projects, global hidden tech rank, secret future triggers, or omniscient markets
- bounded planning layers: agenda review -> visible candidate shortlist -> project request -> capacity planning
- visible candidate utility can consider need, strategy, capability gap, readiness, time to effect, opportunity cost, alternatives, diversity, uncertainty, long-term value, foreign routes, and knowledge spillover
- no single universal fixed weight vector
- AI decisions retain explainable reasons
- harder AI improves planning/coordination rather than receiving free RP/labs/evidence or hidden knowledge

### Integration / validation

- runtime gains policy/culture/strategic-goal input events plus agenda/capacity-request queries
- materialized research view gains agenda/culture summaries, capacity requests, and player-readable alignment/reasons without exposing hidden AI scores
- `validate_research_agenda_ai.py` is the sixth research validator and passed on the first PR run before normal build/smoke validation

Canonical files:

- `research_agenda_model.json`
- `scientific_culture_model.json`
- `research_ai_planning_contract.json`
- `RESEARCH_AGENDA_AI_MODEL.md`
- `validate_research_agenda_ai.py`

## Research CI contract

Research changes now pass six research validators before normal build/smoke gates:

1. `validate_research_catalog.py`
2. `validate_research_maturation.py`
3. `validate_research_competence.py`
4. `validate_research_transfer_ui.py`
5. `validate_research_start_runtime.py`
6. `validate_research_agenda_ai.py`
7. .NET restore/build
8. pinned Godot editor smoke
9. Godot runtime smoke

## Early-release campaign horizon direction

- demo: roughly 100–150 meaningful in-game years
- initial paid Early Access: roughly **500 years** of officially supported meaningful simulation/content
- engineering soak: at least **1,000 simulated years** without unbounded memory/save/performance failure
- no hard year-based game-over

## Persistence / performance direction

Adaptive Research keeps static catalogs shared and saves compact civilization-specific state only. Starting-profile composition runs only at initialization/migration. Candidate emergence, competence/readiness, foreign-tech assessment, agenda/AI planning, and UI updates are event-driven or low-frequency and never scan/process the full graph every simulation tick/frame.

## Next action for this workstream

1. finish PR #53 continuity/final validation
2. require all six research validators + .NET + Godot gates on final head
3. verify changed files remain research-owned
4. merge only after validation
5. continue on `dev/adaptive-research`
6. next research layer after milestone #7: long-run research balance/simulation scenarios and public benchmark fixtures proving divergent trees, complacency/catch-up, foreign-tech assimilation, and bounded 500–1,000-year research-state growth before runtime gameplay integration
7. do not promote gameplay VERSION until actual research runtime/gameplay integration is intentionally implemented and validated
