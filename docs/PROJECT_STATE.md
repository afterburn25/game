# Canonical Project State

This is the authoritative continuity record for Stellar Continuum. `WORKSTREAMS.md` defines branch ownership; Adaptive Research design/data merges do not promote gameplay VERSION.

## Current full-game milestone — 2026-09-08/09

The user has replaced the demo-only goal with one full game and explicit Player /
Developer modes. Core PR #239 contains cinematic direction B, continuous map scale
navigation, 3D free-placement colonies, separate mode saves and a marked Developer
toolbox. See [GAME_MODES.md](GAME_MODES.md) and the Core handoff for current validation.
The starting baseline for this milestone is PR #233 / integration `334004d15c1f0cff7ee6dc345c8de225a2603de4`.
Acceptance, exact source revision and native validation results are recorded on PR #239; merging requires passing combined CI and native input validation.

Priorities are a playable ordinary campaign, meaningful economy expenses, deeper
colony decisions and the maintained Adaptive Research gameplay cutover. The active
Core continuation now uses a 100-system campaign profile and charges Credits for
infrastructure, ships, settlement expeditions and surface buildings. Colony services,
administration and active fleets create ongoing costs; powered surface trade hubs add
player-controlled revenue. The dedicated Economy page reconciles gross income, costs
and net flow against the authoritative simulation. `EARLY_ECONOMY.md` records the scale,
cost table and tuning basis. The current gameplay version is `0.0.7-dev.1`.
Player saves preserve versions 8/9, 10/11 and 12/13; Developer wraps the validated campaign
in a separate version 1 envelope. Earlier recovery details below are historical.

The operations interface has direct pages for Economy, Research, Industry, Ships,
Exploration, Colonies, Logistics and Relations. Ship and colony pages now list owned
fleets/worlds and link back to their map location or colony surface. Affordability is
shown before capital orders. These local Core milestones require a fresh exact-head
Godot render/input and Windows package gate before publication or integration acceptance.
Relations now exposes declaration of war for legitimately identified contacts. Armed
owned fleets expose integrity plus Engage, Hold, Defend and Retreat commands on the Ships
page. Engage keeps foreign identity out of presentation: the matched Combat runtime chooses
the first deterministic co-located target that passes its hostility and attack preview.
Rejected engagement attempts disclose no peaceful or hidden target identity.
Military fleets can deploy to the star selected on the strategic map. Core validates the
fleet, destination and operational reach, clears system-local tactical orders, and then uses
the existing authoritative strategic movement path to travel and arrive.
On colony surfaces, the player can click a rendered building to select it. Incomplete sites
can be cancelled for half of their authorization credits while spent Industry remains spent;
completed buildings can be demolished without a refund. Removal is ownership checked and
immediately updates local power and production.
Completed surface complexes now contribute type-specific maintenance to the same daily
cash-flow calculation shown on the Economy page, including when a completed complex lacks
power. This makes unused surface capacity an ongoing economic decision.
The surface header exposes the ordinary 1× through 4× simulation speeds alongside pause,
so a player can manage construction pacing without leaving the planet view.
PR #241 merged the complete 100-system economy, operations and surface-management slice
into `integration` at `a7a1ab4096fb5ca706778942dbf5b30703c93d73` after exact-head build,
Windows package and real Godot input/render/save/reload gates passed. The active follow-up
adds a first in-place upgrade tier: completed complexes consume visible Credit and stored
Industry costs, switch to advanced output/upkeep, retain position, and persist without a
save-format change.
The Research page follow-up introduces a reusable graphical horizon that contains only
completed, active and currently investigable knowledge. Unknown possibilities are absent
from the presentation model. Players can start an investigable program directly from its
node; the legacy six-step progression remains authoritative until Adaptive Research state,
effects and persistence are cut over together.
Colony specialization is now derived from completed surface construction: three matching
complexes form a Research, Industrial, Commercial or Energy district with a 25% matching
output bonus. The surface and colony list show current specialization and progress. Because
this is derived from existing saved buildings, it adds a real placement decision without a
new save field or mode-specific rule.
The 3D colony scene now derives its terrain, exposed rock, sky, fog and sunlight palette
from the occupied world's canonical environment. Temperate, frozen, hot, airless, oceanic,
reducing-atmosphere and generic rocky worlds are visually distinct while sharing the same
authoritative placement heightfield and saved coordinates.
Fresh human campaigns now begin with Earth plus dependent settlements of 100,000 people on
Luna and 250,000 on Mars. Both appear on the Colonies page, can be opened in orbit or landed
on, use their actual body environments, survive ordinary saves, and add scaled administration
costs. Campaign completion now requires an extrasolar colony rather than merely a second owned
settlement.
Owned surfaces render their established population around the protected colony hub. Hostile
worlds such as Luna and Mars use sealed habitat modules; naturally supported colonies use open
settlement towers. The cluster changes only across bounded population bands and does not alter
construction footprints or saves.
Players can now place Habitat complexes on hostile colony surfaces. A powered base complex
reduces that colony's explicit life-support cost by 20%; its closed-loop arcology upgrade
reduces 40%. Reductions stack only to 75%, consume power, add maintenance, and use the same
construction, upgrade, demolition and save rules as other surface buildings.
The Colonies page reports each world's actual post-infrastructure life-support charge and,
when reduced, its gross cost and reduction. It also exposes local building count and power
demand/supply so the player can identify a power shortage before landing.
The home system now offers an optional Asteroid Resource Network after Orbital Industry and
the Launch Complex. It costs 320 Credits plus 1,800 Industry, produces 1.50 Industry/day,
costs 0.18 Credits/day to operate, and appears in both the orbital map and logistics graph.
Completed Launch Complexes, Shipyards, and Warp Test Facilities also carry explicit upkeep.
Locked orbital markers and rejected construction orders now identify their missing technology
and prerequisite infrastructure by name instead of returning a generic unavailable message.
The opening guide uses the direct Research and Industry pages rather than obsolete cycling
instructions and presents asteroid extraction as an optional output/upkeep tradeoff when the
next required infrastructure is still technology-locked.
The Research and Industry pages no longer render redundant Next/Start controls; named horizon
nodes and project buttons are the player-facing order path.
Ships follows the same direct interaction: named design buttons start or queue vessels, the idle
card asks for a choice instead of implying a default selection, and locked shipyards show no
inert cycle/build controls.
Locked shipyards and rejected ship orders name the missing Spacecraft Construction,
Experimental Interstellar Transit and Orbital Shipyard requirements directly.
The visible Research horizon now uses full-width two-column program cards with description,
state color, progress and a clear graphical start affordance instead of small text-like buttons.
The Colonies page now presents each owned world as a compact visual card with grouped population,
administration, life support, power and specialization status plus normal View and Land controls.
The Economy page now renders income and each operating-cost category as aligned labeled rows,
replacing tab-delimited text that could collapse names and values together at runtime.
Industry and Ships now render available orders as three-column graphical cards with separate
title, cost, description and authorization state while retaining their direct command paths.
The top bar now includes a persistent recent-events center. Accepted capital orders and
player-visible research, construction, ship, exploration, colony and combat outcomes remain
available after the temporary command message disappears. The feed is ordered, limited to
32 entries, displays the newest 16, and clears when changing campaign or game mode so events
cannot leak between Player and Developer sessions.

## Current shared integration recovery — 2026-09-08

- Accepted shared baseline at recovery: `integration` / `c529a1a765776c0940f88002410bc70db740d05a`.
- Core branch safely synchronized from `6b50f879` (285 behind, no unique commits).
- Current gameplay VERSION is `0.0.7-dev.1`; campaign save format is v9. The earlier research-local v6/0.0.6 snapshot below is historical.
- Accepted seams include deterministic plain-C# stepping/Industry allocation, pause safety, physical shipbuilding, persisted Diplomacy, observer-safe command/read models, Exploration/Colonization and Species contracts, scheduled autosave/backup recovery/day-zero checkpoint, and exact-own Combat status.
- Adaptive Research M19 implementation is accepted side by side with legacy gameplay research. Canonical continuation is `research/adaptive-research`; a gameplay cutover has not been accepted.
- Runtime release blocker #61 was independently reproduced on the exact accepted build: seven logged C# script-instantiation errors were ignored by the old smoke command. Testing/Release PR #222 adds Debug build plus semantic startup/error checks. It remains a release blocker until changed CI passes.
- Existing PR #218/#220 visual milestones and recovered unique children require review, not automatic merging. The 1,000 real-system data milestone #221 is recorded and remains a separate uncompleted dependency.
- Full branch classification and sources: [BRANCH_INVENTORY_2026-09-08.md](BRANCH_INVENTORY_2026-09-08.md). Current ownership and flow: [WORKSTREAMS.md](WORKSTREAMS.md).

## Retained earlier research-local snapshot

The sections below preserve their original milestone context. Use current shared recovery records and specialist handoffs for present branch/validation status; do not act on stale main-first directions.

## Repository / identity

- Working title: **Stellar Continuum**
- Repository: **`afterburn25/stellar-continuum`** (public)
- Engine/runtime: Godot 4.7.2 .NET, C# / .NET 8
- Architecture: Godot presentation/platform layer + plain-C# authoritative simulation core

## Gameplay baseline known to Adaptive Research

As of 2026-09-07 this workstream still records:

- gameplay version: **`0.0.6-dev.1`**
- validated gameplay merge: `91a2204b96ed08c2178875cbc8d5b0bc378372ad`
- save format: v6

Other gameplay workstreams own later gameplay status if changed.

## Adaptive Research workstream

- persistent branch: **`dev/adaptive-research`**
- owner: dedicated Adaptive Research / Technology chat
- scope: possibility graph, RP/Pressure/Labs, emergence/evidence/applicability, capability/maturation, competence/facilities/tacit knowledge, foreign technology/exchange, starting histories/runtime/view contracts, agenda/scientific culture/fair AI, long-run benchmarks, biochemical diversity, distributed scientific continuity, secrecy/compartmentalization, cross-polity scientific collaboration, research-facing integration/UI/validation

Other workstreams may consume research events/queries/capabilities but should not independently edit canonical research graph/schema files without coordination.

## Current public Adaptive Research seed

- **360 public/normal possibility nodes**
- **21 domains**
- **59 Research Pressure types**
- **16 alternative-solution sets**
- **14 applicability traits**
- **9 evidence types**
- **36 knowledge fields**
- **17 cross-lineage capabilities**

Exact secret discoveries, artifact chains, rare probabilities, and hidden special-AI eligibility remain outside public data.

## Durable research rules

- one shared hidden Technology Possibility Graph; never one fully visible universal tree or giant fixed per-species trees
- player and AI see only their current legitimate scientific horizon
- RP comes from physical Effective Research Labs; Pressure is contextual need/evidence and only a hard gate where explicitly configured
- early directed concurrency is 1 -> 2 -> 4 -> lab-capacity-only as real institutional capability matures
- functional dependencies use cross-lineage capabilities when implementation does not matter
- research knowledge is distinct from physical deployment
- field competence is theoretical / experimental / engineering; specialist facilities and tacit expertise matter
- Project Readiness is a bounded derived context efficiency, not stacks of arbitrary research modifiers
- foreign technology uses Understanding / Operability / Reproduction / Adaptation and never instantly matures a native node
- technology exchange is composed from actual records/data/hardware/tooling/experts/training/institutions; licenses are law, not physics; value is recipient-specific
- starting civilizations compose scientific history; future research remains adaptive and is not stored as a species tree
- agenda/scientific culture changes recognized attention and real capacity requests, not hidden-node visibility or direct RP multipliers
- fair AI uses the same visible horizon and authoritative blockers; harder AI gets planning quality, not hidden knowledge/free RP/labs/evidence
- runtime is sparse/event-index driven; no full graph per simulation tick and no per-frame hidden-graph UI query
- biochemical identity is composable population context, not a race ID or flat research modifier
- carbon-water is common reference, not universal; ammonia-rich, cryogenic-hydrocarbon, silicon/mineral, synthetic, and mixed populations share the same graph through applicability/capability context
- one civilization may contain multiple incompatible biochemical populations without duplicating the graph
- mature biochemical knowledge may be civilization-wide while operational applicability remains population/context scoped
- scientific truth/maturity, local codified access, local active practice, and physical deployment are distinct
- distributed research creates sparse context exceptions only when regions materially diverge; synchronized colonies have no explicit research-context object
- records/data propagate through actual communications; experts/tooling/prototypes/operating institutions do not teleport as data
- no universal distance research penalty; only actual communication/archive/institutional/political/practice constraints matter
- successor states inherit research from actual local archives, received records, experts, facilities/tooling/prototypes, training, and projects—not the old polity's full tech-list checkbox set
- classification applies to records/projects/assets, not scientific truth or physics
- classification does not directly change RP/project cost and cannot make deployed physical effects unobservable
- if secrecy slows research, the cause must be real reduced authorized labs/facilities/experts, validation limits, compartment integration, or secure-communications/archive constraints
- reclassification can stop future dissemination but cannot recall already distributed copies or un-leak compromised records
- Intelligence/Security owns espionage, theft, interception, compromise detection and protection actions; Adaptive Research owns research-side access/dissemination/foreign-tech consequences of factual outcomes
- scientific collaboration creates permission/coordination, never RP or research-speed multipliers
- every joint-research contribution references real participant capacity/assets; contributions may be unequal
- active joint directed research consumes each scientifically participating polity's own directed-program capacity
- all labs on one joint project use one canonical diminishing-return curve; multiple flags cannot create multiple scaling buckets
- joint projects do not merge technology trees or expose hidden partner nodes
- genuine scientific co-developers may advance through normal maturation; passive funders/hosts/result recipients are not automatically Mature
- equal shared records do not imply equal operability/reproduction; biology, facilities, materials, infrastructure and tacit practice remain participant-specific
- withdrawal removes future real contribution but cannot roll back completed work or recall delivered records
- Diplomacy owns agreement negotiation, payments, rights, treaty breach and political consequences; Adaptive Research owns scientific contribution/progress/result semantics
- secret/rare discovery details stay outside public research data

## Validated Adaptive Research milestones

1. PR #11 -> `f70e122134e87c1449582b573c5e2db8b045d311`: possibility graph + RP/Pressure/Labs.
2. PR #12 -> `95fa5c9e77642479eecc8f4183c91c06b3709f7e`: emergence/evidence/pressure dynamics.
3. PR #13 -> `101b01a1d6407fee2912c7e8b9175f196bb75ca9`: capability interoperability + maturation/hypotheses/setbacks.
4. PR #17 -> `64a4aaa74ca526c8d6d69b9d905a2e9c3e3a6bc6`: competence + specialist facilities + tacit knowledge + Project Readiness.
5. PR #30 -> `d7bdaa8ee67461ba1811121e9af4719de6583a8d`: foreign tech + transfer/licensing/brokerage + visible-only research UI.
6. PR #44 -> `859099ee3048a2788aa32b33c5ca46aeaee00df9`: composable starting histories + runtime event/query boundary + materialized view.
7. PR #53 -> `8e47ef537af6d35f8d60e9cf2c9953064d6858ef`: agenda + 12-axis mutable scientific culture + causal complacency/catch-up + fair AI planning.
8. PR #59 -> `d3916d3e6551c7a8b606716858d2d782581b1dac`: deterministic 500/1,000-year research divergence/state-soak benchmark harness.
9. PR #63 -> `1e69fa65c2ec0df2feef16bd30794b08f0b2000a`: 360-node species-neutral alternative biochemistry/exotic biospheres + multispecies applicability.
10. PR #73 -> `6c832fc07359ebb4fbe40c4dc079f19e13c11fca`: distributed scientific knowledge/regional continuity + successor inheritance/reintegration.
11. PR #78 -> `172c9c364b161e2e3deac88337429b5880a34e68`: research secrecy, special-access compartments, compromise interpretation, declassification/reclassification.
12. PR #83 -> **`b56b47c1f23312abde93e04cd8ac9caf819f0176`**: real-capacity cross-polity joint research, participant-specific contributions/results/withdrawal, classified collaboration, collaboration benchmarks.

None of these research design/data/tooling milestones promoted gameplay VERSION.

## Long-horizon benchmark baseline

### 500-year same-origin divergence

- minimum Mature-tree Jaccard distance: **0.457**
- Mature catalog fractions: orbital industrialist **0.253**, defense engineer **0.292**, biosphere adaptor **0.350**
- unique Mature nodes: **11 / 36 / 68**
- common Mature nodes: **47**

### 350-year military complacency/response

- initial hegemon lead: **10.85** benchmark units
- gap at year 180: **-12.65**
- leader attention **1.0 -> 3.0** after legitimate rival catch-up evidence
- final benchmark scores: leader **18.55**, challenger **33.40**
- no hidden catch-up multiplier, leader penalty, forced parity, or guaranteed comeback

### 1,000-year core research-state soak

- Generalist A: **89** node states / **124** recent events / **64** detailed history
- Generalist B: **105 / 156 / 80**
- Specialist C: **81 / 108 / 56**

## Biochemical applicability benchmark

- human carbon-water: **4 shared / 0 exotic native-specific**
- ammonia-rich: **10 / 6**
- cryogenic-hydrocarbon: **10 / 6**
- silicon/mineral: **12 / 8**
- exotic native-specific pairwise Jaccard distance: **1.000**

## Distributed-continuity benchmark

- communication: 1.75y latency; Absent -> Codified; no automatic practice
- 40y partition: theoretical loss 2, experimental loss 8–14, engineering loss 16–24
- successor fracture: shared foundation 10; A 15 total/5 specialist; B 14/4; Jaccard **0.474**
- 1000y / 120 regions: peak **5** contexts, **17** node-access exceptions, **10** field-practice exceptions, **9** pending transmissions; final contexts 3

## Secrecy benchmark

- restricted program: 48 scientifically eligible labs -> 14 authorized, direct secrecy multiplier **1.0**
- compartment integration blocked by missing manufacturing-process access until real authorization supplied
- classified capability observation: Understanding Unknown -> Observed, Reproduction None, records 0
- partial blueprint compromise: Characterized + Component Replication, native maturity false
- declassification: contexts 2 -> 6, archive copies 2 -> 6 after delivery, maturity unchanged
- reclassification: 4 existing copies remain 4 despite policy narrowing to 2 contexts
- 1000y secrecy soak peaks: **18** security records / **6** compartments / **3** known compromises

## Milestone #12 — cross-polity joint research / scientific collaboration

**Merged/validated through PR #83 at `b56b47c1f23312abde93e04cd8ac9caf819f0176`.**

Final validated PR head: `a6e34f9b53ef3874e4ce95faabd548fc455d4b3b`.

Established:

- five collaboration forms: Joint Directed Project / Shared Observation / Shared Facility / Expert Exchange / Joint Foreign Technology Study
- agreements create permission and contribution commitments but never RP/research-speed multipliers
- joint directed work consumes participant directed-program capacity when a participant actually performs directed research
- real labs/facilities/experts/data/evidence/samples/materials/tooling/computation only; same asset cannot be counted twice
- combined labs use one canonical project-wide diminishing-return curve
- no generic cross-polity coordination penalty; only actual communication/security/data/facility dependencies matter
- contribution and result rights are separate and may be unequal/asymmetric
- actual participation can build relevant field competence; passive treaty membership/payment/result receipt does not create practice
- genuine co-developers advance through normal maturation; passive participants use normal transfer/assimilation
- result applicability/operability/reproduction remains participant-specific
- classified joint projects can distribute only selected compartments while using an authorized integration context
- withdrawal removes real future capacity/assets but cannot reverse completed progress or recall delivered records
- stable collaboration runtime extension exposes **7 factual input events / 6 queries**
- collaboration state stores aggregate active contributions, references physical assets, compresses completed history, and never copies partner technology graphs

### Collaboration benchmark

Real labs/no treaty multiplier:
- A 20 labs -> **17.4** scaled units
- B 12 labs -> **12.0** scaled units alone
- correct combined 32-lab project -> **21.6** scaled units / **2160 RP/year before readiness**
- incorrect per-partner scaling would be **29.4** units and is explicitly rejected
- treaty multiplier **1.0**

Participant withdrawal:
- 24 labs -> **18.8** scaled units
- withdraw 8 labs -> 16 labs / **16.0** units
- capacity loss **2.8** units; stage progress preserved; delivered records not recalled

Hard facility withdrawal:
- `hazardous_foreign_tech_protocols` retains 10 labs but loses canonical `xenoscience_containment`
- project becomes `blocked_missing_specialized_facility`

Asymmetric foreign result:
- both participants Engineering Understood
- compatible participant: Adapted Operation / Subsystem Replication
- incompatible synthetic participant: Unusable / Component Replication
- native maturity false

Classified joint compartments:
- 4 compartments total
- A missing manufacturing process
- B missing theory + software/control
- designated integration context has all compartments; neither participant independently has complete package

Communication partition:
- 3-year data-link partition
- local observation continues
- cross-site correlation blocked pending remote dataset
- no generic penalty; existing local records preserved

1,000-year collaboration soak:
- **112** collaborations created
- peak **4** active collaborations
- peak **9** participant contribution records
- peak **4** pending result deliveries
- final active 2 / pending deliveries 0
- recent-event ring at bound 384; archived summaries 110/192

Final #12 validation passed collaboration + secrecy + distributed + biochemical + all core research validators/benchmarks and .NET restore/build. Exactly seven research-owned files changed.

## Known shared CI limitation — issue #61

The shared Godot runtime smoke can exit successfully while logging failure to instantiate `res://src/Game/Presentation/Main.cs`. Tracked in **GitHub issue #61** and outside Adaptive Research ownership.

Until fixed, do not claim semantic runtime health solely from that process step.

## Research validation stack

Core:

1. `validate_research_catalog.py`
2. `validate_research_maturation.py`
3. `validate_research_competence.py`
4. `validate_research_transfer_ui.py`
5. `validate_research_start_runtime.py`
6. `validate_research_agenda_ai.py`
7. `validate_research_benchmarks.py`
8. .NET restore/build
9. shared Godot process smokes with #61 caveat

Specialized:

10. `validate_research_biochemistry.py`
11. `validate_research_biochemistry_benchmarks.py`
12. `validate_research_distributed_continuity.py`
13. `validate_research_distributed_continuity_benchmarks.py`
14. `validate_research_secrecy.py`
15. `validate_research_secrecy_benchmarks.py`
16. `validate_research_collaboration.py`
17. `validate_research_collaboration_benchmarks.py`

## Campaign horizon / scalability target

- demo: ~100–150 meaningful years
- initial paid Early Access: ~**500 meaningful years**
- engineering soak: at least **1,000 simulated years** without unbounded state/save/performance growth
- no hard year-based game-over

## Next Adaptive Research action

**Milestone #13: plain-C# Adaptive Research runtime implementation foundation.**

Before editing shared simulation source, update `WORKSTREAMS.md` to reserve a dedicated research-owned runtime path and keep cross-workstream access behind the established event/query contracts.

Runtime foundation goals:

- load/validate the public research catalogs once into immutable indexed definitions
- introduce sparse per-civilization research state using stable IDs; do not copy the graph into each civilization
- implement visible node state, Research Pressure/evidence/applicability/capability indexes, active projects, Effective Research Lab allocation, directed-program capacity, and materialized-view revisions
- use events/indexes, never full graph scans per simulation tick
- implement capability/blocker/query interfaces first so shipbuilding/logistics/UI/AI can integrate without reaching into internal state
- keep distributed/secrecy/collaboration extensions as separately owned sparse modules layered on the same state model
- add deterministic plain-C# unit/smoke tests and save-serialization guards before wiring into presentation/gameplay
- do not replace the existing prototype gameplay research system until migration/acceptance is explicit
- gameplay VERSION remains unchanged until runtime integration is intentionally accepted
