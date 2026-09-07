# Development History

This is the concise chronological record of validated gameplay milestones and major design/data state changes. It keeps accepted design foundations separate from gameplay-version promotion.

## Gameplay milestones through 0.0.6

- **0.0.1 — Simulation foundation:** merged/validated Godot 4.7.2 .NET/C# foundation, plain-C# simulation, deterministic galaxy generation, real-time clock/backlog protection, persistence, diagnostics, CI.
- **0.0.2-dev.1 — Civilizations / fog:** merged/validated at `2aaa6d3aeead400882c8214005e3bd78cfe17eaa`.
- **0.0.3-dev.1 — Exploration / first contact:** merged/validated at `8683acebb1860ff3d94838656c65c8a82cd90d68`.
- **0.0.4-dev.1 — Colonies / basic economy:** merged/validated at `ebcba9239a0d12d0c5c99f41937193176b6c8664`.
- **0.0.5-dev.1 — 2050 Pre-Warp Dawn:** merged/validated at `c457f5e57c51057e86b857d0d828ff42d75e8f8b`.
- **0.0.6-dev.1 — Construction-driven development:** validated gameplay baseline recorded by this research workstream at `91a2204b96ed08c2178875cbc8d5b0bc378372ad`.

Other gameplay workstreams should update the canonical baseline if they merge later accepted gameplay versions. Adaptive Research does not own or reinterpret other gameplay branches; see `WORKSTREAMS.md` and current repository state.

## Durable research direction

- realism-first causes/consequences
- meaningful 2050 solar-system civilization phase
- Adaptive Research rather than fixed universal or per-species future trees
- RP + contextual Pressure + physical Effective Research Labs
- species/environment/history-relative research applicability
- capability interoperability across different implementation lineages
- competence/tacit knowledge/institutions rather than arbitrary modifier stacking
- foreign technology can be incompatible, dependent, dangerous, tradable, or useful only to third parties
- visible research UI contains only the civilization's current scientific horizon
- long campaigns require sparse/event-driven state and history compression
- early paid-release design horizon roughly 500 years with 1,000-year engineering soak testing

## Adaptive Research milestone #1 — possibility graph / RP + Pressure + Labs

**Merged/validated** through PR #11 at `f70e122134e87c1449582b573c5e2db8b045d311`.

Established 330 public possibilities, 20 domains, 59 Research Pressure types, 15 alternative-solution sets, RP + Pressure + Labs, staged 1 -> 2 -> 4 -> lab-capacity-only concurrency, and `validate_research_catalog.py`.

## Milestone #2 — emergence / evidence / pressure dynamics

**Merged/validated** through PR #12 at `95fa5c9e77642479eecc8f4183c91c06b3709f7e`.

Established 6 applicability traits, 9 evidence types, rules for all 59 pressures, sparse indexed candidate emergence, no calendar unlocks, no rank-based catch-up, fair-information observations, and population-scoped applicability.

## Milestone #3 — capability interoperability / maturation

**Merged/validated** through PR #13 at `101b01a1d6407fee2912c7e8b9175f196bb75ca9`.

Established implementation knowledge vs functional capabilities, scoped capabilities, FTL/logistics path-lock repairs, knowledge vs deployment, Experimental -> Demonstrated -> Engineering -> Mature/Archived maturation, hypothesis outcomes, non-destructive setbacks, bounded side discoveries, and `validate_research_maturation.py`.

## Milestone #4 — competence / institutions / tacit knowledge

**Merged/validated** through PR #17 at `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`.

Established 35 knowledge fields, theoretical/experimental/engineering competence, limited related-field transfer, active-practice atrophy without deleting archived knowledge, bottleneck-sensitive Project Readiness, specialized scientific facilities, tacit knowledge/expert cohorts/training, Access -> Interpreted -> Codified -> Trained -> Native Practice assimilation, and `validate_research_competence.py`.

## Milestone #5 — foreign technology / exchange / research UI

**Merged/validated** through PR #30 at `d7bdaa8ee67461ba1811121e9af4719de6583a8d`.

Established Understanding / Operability / Reproduction / Adaptation axes, real compatibility dependencies, technology-transfer packages mapped to evidence/tacit assets, legal rights separate from technical ability, buyer-specific technology value, visible-only evolving research UI, and `validate_research_transfer_ui.py`.

## Milestone #6 — starting histories / runtime / materialized view

**Merged/validated** through PR #44 at `859099ee3048a2788aa32b33c5ca46aeaee00df9`.

A history-only PR conflict was resolved with explicit two-parent commit `93930301055cbf0e0c2cd15c1733fecb7b853e12`; no force overwrite or content loss occurred. The resolved head then passed all five research validators, .NET build, and both Godot smokes.

Established:

- 12 reusable scientific-history fragments and 4 reference starting compositions
- one base-era fragment per complete start
- prerequisite-closed starting research history
- human-like 2050 Practical Fusion Investigable, no calendar-triggered FTL hypothesis
- synthetic machine cognition can be historical fact without human Synthetic Cognition lineage
- gravity history generates need/competence without preselecting a solution
- sparse authoritative research runtime state with stable cross-workstream events/queries
- other systems query capabilities rather than implementation source when appropriate
- read-only materialized visible-only research projection
- UI/AI commands revalidated authoritatively
- no full-graph per-tick scan / no per-frame hidden-graph query
- `validate_research_start_runtime.py`

## Milestone #7 — research agenda / scientific culture / fair AI planning

**Current in-progress design/data/tooling milestone.**

Persistent branch: `dev/adaptive-research`  
PR: **#53 — Adaptive Research agenda, scientific culture, and fair AI planning**

Current work establishes:

### Research agenda

- priorities over recognized domains, knowledge fields, Research Pressure problems, and known capabilities
- Deprioritized / Routine / Important / Strategic / Critical priority levels
- basic-vs-applied, competence-preservation, portfolio-diversity, and foreign-science orientations
- agenda changes background attention, visible-project recommendations, competence maintenance, and capacity requests
- priorities do not directly add RP, expose Unknown nodes, bypass requirements, or create physical resources
- research can request labs, specialist facilities, training, samples/assets, and foreign expertise; external systems decide fulfillment

### Scientific culture

Twelve small mutable research-facing civilization/institution axes:

- curiosity
- risk tolerance
- institutional conservatism
- threat sensitivity
- complacency tendency
- long-term orientation
- openness
- secrecy
- reproducibility rigor
- scientific prestige competition
- portfolio diversity
- commercialization orientation

These affect behavior/agenda only, never direct RP multipliers or hidden-node access. They can change through government, war, success/failure, research accidents, exchange, commercialization, censorship, collapse, and generational change.

### Natural complacency / catch-up

- perceived technological adequacy can lower agenda attention only from legitimate own/observed information plus culture/institutions
- weaker civilizations can develop urgent agendas from actual losses/problems and legitimately observed superior capabilities
- observed rival catch-up can trigger a leader's renewed urgency
- no global hidden rank, leader penalty, catch-up multiplier, or forced convergence

### Fair-information AI planning

- AI uses the same materialized visible horizon and blockers as player-facing research
- hidden graph, exact unseen enemy tech/projects, secret triggers, global hidden tech rank, and omniscient markets are forbidden
- event/low-frequency planning: agenda review -> bounded visible shortlist -> authoritative project request -> capacity planning
- utility considers recognized need, strategy, capability gaps, readiness, time to effect, opportunity cost, alternatives, diversity, uncertainty, long-term value, known foreign routes, and knowledge spillover
- no single universal fixed weight vector
- explanations retained for major choices
- harder AI improves planning/coordination, not resources or knowledge cheats

### Integration / validation

- runtime adds research-policy, scientific-culture-context, and strategic-goal events plus agenda/capacity queries
- materialized view adds agenda summary, capacity requests, and player-readable project alignment/reasons without exposing raw hidden AI scores
- `validate_research_agenda_ai.py` is the sixth research validator and passed its first PR run

## Persistent workstream rule

Adaptive Research/Technology uses `dev/adaptive-research`. Each validated milestone merges from this workstream and continued research remains owned by this dedicated chat.

## Update rule

After each validated Adaptive Research design/data merge:

1. record PR/merge commit here
2. update `PROJECT_STATE.md`
3. update durable research decisions
4. continue on `dev/adaptive-research`
5. keep design/data acceptance distinct from gameplay VERSION promotion
