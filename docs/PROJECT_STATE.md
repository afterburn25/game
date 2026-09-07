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
- scope: technology graph/data, RP/Pressure/Labs, emergence, evidence, applicability, capabilities, maturation, competence, research facilities, tacit knowledge, foreign technology, technology transfer/licensing/brokerage, research UI/data contracts, and research validators/docs

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

Validated and merged through PR #11:

- merge commit: **`f70e122134e87c1449582b573c5e2db8b045d311`**

Established the 330-node/20-domain possibility graph, RP + Pressure + Labs research economy, staged directed-program concurrency, alternative solution sets, and `validate_research_catalog.py`.

## Milestone #2 — emergence / evidence / pressure dynamics

Validated and merged through PR #12:

- merge commit: **`95fa5c9e77642479eecc8f4183c91c06b3709f7e`**

Established formal applicability traits/evidence types, rules for all 59 pressures, sparse event/index-driven candidate emergence, no calendar unlocks, no rank-based catch-up pressure, legitimate-information requirements, and population-scoped applicability.

## Milestone #3 — capability interoperability / maturation

Validated and merged through PR #13:

- merge commit: **`101b01a1d6407fee2912c7e8b9175f196bb75ca9`**

Established implementation knowledge vs functional capability prerequisites, capability scopes, path-lock repairs, Mature/early/deployment grants, knowledge-vs-deployment distinction, maturation/hypothesis outcomes, non-destructive setbacks, bounded side discoveries, and `validate_research_maturation.py`.

## Milestone #4 — competence / institutions / tacit knowledge

Validated and merged through PR #17:

- merge commit: **`64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`**

Established 35 knowledge fields, theoretical/experimental/engineering competence, limited related-field transfer, practice atrophy without deletion of archived knowledge, bottleneck-sensitive readiness, specialized scientific facilities, tacit-knowledge assets/expert cohorts/training pipelines, Access -> Interpreted -> Codified -> Trained -> Native Practice assimilation, one bounded Project Readiness efficiency, and `validate_research_competence.py`.

## Milestone #5 — foreign technology / exchange / research UI

**Validated and merged** through PR #30:

- merge commit: **`d7bdaa8ee67461ba1811121e9af4719de6583a8d`**

Final-head validation passed:

- Adaptive Research catalog validator
- maturation/capability validator
- competence/facility/tacit validator
- foreign-tech/transfer/UI validator
- .NET restore/build
- Godot headless editor smoke
- Godot headless runtime smoke

Established:

### Foreign technology assessment

- four independent axes: **Understanding / Operability / Reproduction / Adaptation**
- real compatibility constraints for science, materials, energy, manufacturing, infrastructure, biology/environment, cognition/interface, software/identity, consumables, expertise, and hazards
- operation without understanding, understanding without reproduction, foreign-process replication without native-process mastery, and native derivatives without exact copying
- event-driven reassessment using existing xenoscience/reverse-engineering research nodes
- foreign acquisition never directly sets a native technology Mature

### Technology transfer / licensing / brokerage

- technology deals are composed packages, not universal tech tokens
- package components include observations, theory, datasets, blueprints, process documentation, hardware, tooling, experts, training, and operating institutions
- package components map to canonical evidence/tacit-knowledge assets
- legal rights are distinct from technical ability
- license terms are **law, not physics**; technically possible violations remain possible and external diplomacy/law/intelligence systems own consequences
- no fixed universal technology value; buyer-specific research/operational/brokerage value depends on legitimate need, compatibility, alternatives, readiness, completeness, dependencies, rights, risk, scarcity, and known demand
- technology unusable to the current holder can retain third-party brokerage value

### Evolving research UI contract

- unknown nodes/hidden placeholder slots never render
- visible links only connect visible nodes
- newly visible branches attach near stable anchors without globally rearranging unrelated branches
- viewport/selection preserved where possible
- early research UI stays simple; advanced competence/facility/evidence/tacit detail is on demand
- visible pressure only for recognized fields
- blocker explanations distinguish labs, facilities, evidence, pressure, capability, and coordination limits
- foreign-tech UI shows the four assessment axes separately
- exchange UI separates package contents, legal rights, and recipient technical ability
- mature/archive-heavy history can collapse outside the active horizon
- UI consumes materialized civilization research state rather than scanning/rendering the full hidden graph every frame

Canonical milestone #5 files:

- `foreign_technology_model.json`
- `technology_exchange_model.json`
- `research_ui_contract.json`
- `RESEARCH_FOREIGN_TECH_MODEL.md`
- `RESEARCH_UI_MODEL.md`
- `validate_research_transfer_ui.py`

## Research CI contract

Research changes now pass four research validators before normal build/smoke gates:

1. `validate_research_catalog.py`
2. `validate_research_maturation.py`
3. `validate_research_competence.py`
4. `validate_research_transfer_ui.py`
5. .NET restore/build
6. pinned Godot editor smoke
7. Godot runtime smoke

## Early-release campaign horizon direction

- demo: roughly 100–150 meaningful in-game years
- initial paid Early Access: roughly **500 years** of officially supported meaningful simulation/content
- engineering soak: at least **1,000 simulated years** without unbounded memory/save/performance failure
- no hard year-based game-over

## Persistence / performance direction

Adaptive Research keeps static catalogs shared and saves compact civilization-specific state only: visible/mature/archived node state, active projects, lab allocations, sparse pressure/evidence/traits, capabilities, relevant field competence, strategically meaningful knowledge assets, foreign-tech assessments/packages, and compressed history.

Candidate emergence, competence/readiness, foreign-tech assessment, and UI layout updates are event-driven or low-frequency and must not scan/process the full graph every simulation tick/frame.

## Next action for this workstream

1. fast-forward `dev/adaptive-research` from the accepted milestone #5 main state
2. continue on the same persistent branch
3. next milestone: define the research runtime/view-model interface and seed starting research horizons/field-competence profiles for initial playable-species archetypes **without** creating separate fixed species trees
4. define how starting 2050 knowledge differs from starting mature capabilities/infrastructure so species can start from different histories while sharing the adaptive system
5. preserve public-secret boundaries and all four research validators
6. do not promote gameplay VERSION until actual research runtime/gameplay integration is intentionally implemented and validated
