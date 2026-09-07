# Development History

This is the concise chronological record of validated gameplay milestones and major design/data state changes. It keeps research design acceptance separate from gameplay-version promotion.

## Gameplay milestones known to this research workstream

- 0.0.1 simulation foundation — merged/validated.
- 0.0.2-dev.1 civilizations/fog — `2aaa6d3aeead400882c8214005e3bd78cfe17eaa`.
- 0.0.3-dev.1 exploration/first contact — `8683acebb1860ff3d94838656c65c8a82cd90d68`.
- 0.0.4-dev.1 colonies/basic economy — `ebcba9239a0d12d0c5c99f41937193176b6c8664`.
- 0.0.5-dev.1 2050 Pre-Warp Dawn — `c457f5e57c51057e86b857d0d828ff42d75e8f8b`.
- 0.0.6-dev.1 construction-driven development — gameplay baseline recorded here at `91a2204b96ed08c2178875cbc8d5b0bc378372ad`.

Other gameplay workstreams own later gameplay status if changed.

## Adaptive Research milestone #1 — possibility graph / RP + Pressure + Labs

**Merged/validated:** PR #11 -> `f70e122134e87c1449582b573c5e2db8b045d311`.

Established 330 public possibilities / 20 domains / 59 pressures / 15 alternative solution sets, RP + Pressure + Labs, staged directed-program concurrency, and `validate_research_catalog.py`.

## Milestone #2 — emergence / evidence / pressure dynamics

**Merged/validated:** PR #12 -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`.

Established 6 applicability traits, 9 evidence types, complete pressure dynamics, sparse indexed emergence, no calendar unlocks/rank-based catch-up, fair-information observations, and scoped applicability.

## Milestone #3 — capability interoperability / maturation

**Merged/validated:** PR #13 -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`.

Established implementation knowledge vs cross-lineage functional capability requirements, scoped capabilities, path-lock repairs, knowledge vs deployment, research maturation/hypothesis outcomes, non-destructive setbacks/side discoveries, and `validate_research_maturation.py`.

## Milestone #4 — competence / facilities / tacit knowledge

**Merged/validated:** PR #17 -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`.

Established 35 knowledge fields, theory/experiment/engineering competence, specialist research facilities, tacit knowledge/expert cohorts/training, bounded Project Readiness, and `validate_research_competence.py`.

## Milestone #5 — foreign technology / exchange / research UI

**Merged/validated:** PR #30 -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`.

Established four foreign-tech axes, real compatibility/dependency constraints, technology transfer/licensing/brokerage, buyer-specific value, visible-only evolving research UI, and `validate_research_transfer_ui.py`.

## Milestone #6 — starting histories / runtime / materialized view

**Merged/validated:** PR #44 -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`.

History divergence was resolved explicitly with two-parent commit `93930301055cbf0e0c2cd15c1733fecb7b853e12`, then revalidated before merge.

Established 12 reusable starting scientific-history fragments, 4 reference starts, prerequisite-closed starting histories, sparse runtime event/query contracts, visible-only materialized view, and `validate_research_start_runtime.py`.

## Milestone #7 — research agenda / scientific culture / fair AI planning

**Merged/validated:** PR #53 -> **`8e47ef537af6d35f8d60e9cf2c9953064d6858ef`**.

Final head passed all six research validators, .NET build, and shared Godot process smokes.

Established:

- recognized domain/field/problem/capability Research Agenda priorities
- Deprioritized / Routine / Important / Strategic / Critical priority levels
- background attention/capacity planning without direct RP multipliers or hidden-node unlocks
- 12 mutable scientific-culture axes
- natural complacency/catch-up from perceived adequacy, real pressure, legitimate observations, and institutions
- no hidden technology rank, leader penalty, catch-up multiplier, or forced convergence
- fair-information AI planning using the same visible horizon/blockers as the player
- bounded visible candidate shortlist (12), contextual utility components, explainable choices, no universal fixed weight vector
- harder AI improves planning, not free knowledge/resources
- `validate_research_agenda_ai.py`

## Milestone #8 — long-horizon divergence / state soak

**Current:** PR #59 — Adaptive Research long-horizon divergence and soak benchmarks.

Added deterministic offline research design/CI scenarios and `validate_research_benchmarks.py`. The harness is explicitly **not** gameplay runtime; it may scan the public graph offline while production research remains event/index-driven.

Initial passing CI baseline (run `34163593221`, job `101870178023`):

### 500-year same-origin divergence

- minimum pairwise Mature-tree Jaccard distance: **0.457**
- common Mature nodes: **47**
- Mature public-catalog fractions: Orbital Industrialist **27.6%**, Defense Engineer **31.8%**, Biosphere Adaptor **38.2%**
- unique Mature nodes: **11 / 36 / 68** respectively

### 350-year military complacency / response

- initial hegemon military lead: **10.85** benchmark units
- gap at year 180: **-12.65** — challenger leapfrogged without hidden catch-up
- leader military attention: **1.0** during perceived adequacy -> **3.0** after legitimate catch-up observation
- final scores: leader **18.55**, challenger **33.40**
- no hidden catch-up multiplier, leader penalty, forced parity, or guaranteed comeback

### Foreign-tech asymmetric value

Synthetic holder is incompatible with the metabolic technology while a metabolic buyer is compatible; 3-component package retains brokerage/research value without instant native maturity.

### 1,000-year research-state soak

Final per-civilization records:

- Generalist A: **89** node states / 124 recent events / 64 detailed history
- Generalist B: **105 / 156 / 80**
- Specialist C: **81 / 108 / 56**

All remained below reference bounds of 330 node states, 256 recent events, 512 detailed-history records.

Canonical benchmark files include `research_benchmark_scenarios.json`, `RESEARCH_BENCHMARK_MODEL.md`, `RESEARCH_BENCHMARK_BASELINE.md`, `RESEARCH_BENCHMARK_RESULTS_V1.json`, and `RESEARCH_BENCHMARK_NOTES.md`.

## Shared CI limitation discovered during milestone #8

GitHub issue **#61** tracks an existing shared false-positive runtime-smoke problem: Godot can log failure to instantiate `res://src/Game/Presentation/Main.cs` while returning exit code 0, so the current workflow reports the runtime smoke step as passed.

This is outside Adaptive Research ownership. Until #61 is fixed, research validation reports the Godot runtime **process step** separately from semantic runtime health. Research-owned acceptance depends on the research validators/benchmarks, .NET build, and research-only changed-file audit.

## Persistent workstream rule

Adaptive Research uses `dev/adaptive-research` and remains owned by this dedicated chat. Research data merges do not promote gameplay VERSION.
