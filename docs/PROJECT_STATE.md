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

Established:

- implementation-specific knowledge prerequisites vs functional cross-lineage capability requirements
- capability scopes for civilization / population-or-species / colony-or-installation
- FTL/logistics path-lock repairs
- default Mature capability grants plus explicit early/structural deployment rules
- knowledge-vs-physical-deployment distinction
- Experimental -> Demonstrated -> Engineering -> Mature/Archived maturation
- hypothesis support/refinement/disproof/anomalous results
- non-destructive setbacks and bounded side discoveries
- `validate_research_maturation.py`

## Milestone #4 — competence / institutions / tacit knowledge

**Validated and merged** through PR #17:

- merge commit: **`64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`**

Final-head validation passed all three research validators, .NET restore/build, Godot headless editor smoke, and Godot headless runtime smoke.

Established:

- 35 canonical knowledge fields referenced by all 330 public nodes
- sparse field competence in theoretical / experimental / engineering dimensions
- limited related-field transfer
- active competence atrophy without deletion of archived historical knowledge
- bottleneck-sensitive multidisciplinary readiness
- specialist research institutions/facility capabilities instead of `+research%` buildings
- explicit stage facility requirements where real experiments/prototypes require them
- tacit knowledge assets: records, datasets, protocols, prototypes, tooling, expert cohorts, operating institutions, training pipelines
- foreign expertise assimilation: Access -> Interpreted -> Codified -> Trained -> Native Practice
- aggregate expert cohorts rather than per-scientist objects
- replacement of legacy contextual-cost stacking with inherent base project RP plus one bounded Project Readiness efficiency
- Project Readiness derived from applicable field competence, facility readiness, evidence, and tacit expertise
- Research Pressure explicitly remains availability/urgency, not a speed multiplier
- `validate_research_competence.py`

Canonical milestone #4 files:

- `knowledge_fields.json`
- `research_competence_model.json`
- `research_facility_model.json`
- `tacit_knowledge_model.json`
- `project_readiness_model.json`
- `RESEARCH_COMPETENCE_MODEL.md`

## Milestone #5 — foreign technology / exchange / research UI

**Current in-progress research milestone on `dev/adaptive-research`.**

Current design/data includes:

### Foreign technology assessment

Foreign technology uses four separate axes rather than one reverse-engineering percentage:

- **Understanding:** Unknown -> Observed -> Characterized -> Principle Understood -> Engineering Understood
- **Operability:** Unknown -> Unusable -> Origin Only -> Supported Operation -> Adapted Operation -> Native Operation
- **Reproduction:** None -> Component -> Subsystem -> Foreign-Process -> Native-Process Replication
- **Adaptation:** None -> Conceptual Inspiration -> Interface Adaptation -> Native Derivative -> Hybrid Lineage

Known compatibility constraints can include scientific, material, energy, manufacturing, infrastructure, biological, environmental, cognitive-interface, software/identity, consumable, expertise, and hazard dependencies.

Operability does not imply understanding/reproduction; understanding does not imply manufacturability; unusable technology can retain scientific or third-party trade value.

Canonical file: `foreign_technology_model.json`.

### Technology transfer / licensing / brokerage

A technology deal is a composed package, not a universal `Sell Technology` item.

Possible package components include observations, theory, datasets, blueprints, process documentation, reference hardware, tooling, experts, training, and operating institutions. Components map to the canonical tacit-knowledge assets.

Legal rights are modeled separately from technical ability, including internal research, operation, manufacture, modification, civilian/military use, export, sublicense, and resale.

**License terms are law, not physics.** A technically capable civilization may violate a restriction and face consequences from diplomacy/law/intelligence systems; research does not create invisible contractual force fields.

Technology has no fixed universal value. Buyer-specific research/operational/resale value depends on legitimate need, alternatives, compatibility, readiness, package completeness, dependencies, rights, scarcity, and known market demand.

Canonical file: `technology_exchange_model.json`.

### Research UI contract

The UI preserves the core evolving-tree experience:

- unknown nodes and hidden placeholder slots never render
- only visible-state nodes/links appear
- new branches attach near stable visible anchors
- viewport/selection should remain stable when branches emerge
- early game stays simple; advanced competence/facility/evidence/tacit details are available on demand
- foreign technology displays Understanding / Operability / Reproduction / Adaptation separately
- technology exchange separates package contents, legal rights, and recipient technical ability
- no universal fixed tech-price display
- mature/archive-heavy history can collapse outside the active research horizon
- UI consumes a materialized civilization view rather than scanning/rendering the full hidden graph

Canonical file: `research_ui_contract.json`.

### Milestone #5 validation

A fourth research validator is being added:

- `scripts/validate_research_transfer_ui.py`

It validates foreign-tech axes/constraints/research-node interfaces, transfer component/tacit-asset mappings, legal-right IDs, anti-instant-unlock rules, buyer-specific valuation rules, UI maturation-state consistency, hidden-tree secrecy, and evolving-layout guardrails.

## Research CI contract

Research changes must pass:

1. `validate_research_catalog.py`
2. `validate_research_maturation.py`
3. `validate_research_competence.py`
4. `validate_research_transfer_ui.py` once milestone #5 is merged
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

1. finish milestone #5 docs/continuity and fourth validator
2. open milestone #5 PR from `dev/adaptive-research` to `main`
3. require all research validators + .NET + Godot gates on the final PR head
4. verify changed files remain research-owned
5. merge only after validation
6. fast-forward `dev/adaptive-research` to the resulting `main` merge commit
7. next layer after milestone #5: research runtime/view-model interface and seed starting research horizons/competence profiles for initial playable species without hardcoding separate species tech trees
8. do not promote gameplay VERSION until actual research runtime/gameplay integration is intentionally implemented and validated
