# Stellar Continuum — Development Roadmap

This is the public roadmap for **Stellar Continuum**, the working title for the real-time space civilization strategy game. Commercial naming clearance remains pending; see `BRANDING.md`. Exact hidden discoveries and secret outcomes are intentionally omitted.

For durable design rules, current baseline status, and engineering constraints, also read the continuity records linked from the repository README.

## Current pre-demo priority — visual quality and polish

**Priority updated 2026-09-09 at the player's request.** Visual quality is an explicit
deliverable for the first public demo. It must progress alongside playability and
stability, without waiting for later government, crisis, species or technology expansion.
After outstanding integration and critical fixes, **VP1 is the next visual implementation
milestone**. New optional features should not displace this sequence without an explicit
priority change.

The target is a coherent, realistic science-fiction presentation: detailed photographic-style
illustrations, believable 3D ships and settlements, and a clear, readable GUI. Keep strategic
icons and controls crisp. A finished portrait or concept image does not complete the matching
interactive 3D asset.

### Existing foundation — integrated, final art review still required

- Cinematic galaxy/background and loading artwork; original NASA imagery for Sol bodies.
- Four human ship portraits, four species portraits and three human leadership portraits.
- Shared interface theme, graphical department pages, icons and research emblems.
- Procedural colony buildings, roads, parks, terrain materials, lighting and civilian activity.

These are foundations, not evidence that the entire game has reached final visual quality.
Track individual asset status and provenance in [ASSET_MANIFEST.md](ASSET_MANIFEST.md).

### VP1 — finished Human reference scene

**Status: next; not complete.**

- [ ] Finish one playable Earth colony area with a detailed hub and representative facility,
  terrain, roads, vegetation, building materials and believable lighting/shadows.
- [ ] Finish one detailed Human scout with materials and engine effects suitable for its
  intended in-game viewing distance; keep it consistent with the existing ship portrait.
- [ ] Review both inside Godot at gameplay scale and close range, with real UI and camera input.
- [ ] Record the accepted visual target, reusable model/material rules, asset production effort
  and measured performance on a named reference machine.

**Done means:** an actual playable scene and ship presentation establish the quality to reuse.
Standalone generated images, offline renders or screenshots of an unplayable mockup are not
completion evidence. Preserve authoritative collision footprints and construction rules.

### VP2 — complete the Human demo art set

**Status: planned after VP1.**

- [ ] Extend the accepted style to the demo's visible colony facilities and upgrade variants.
- [ ] Complete Earth, Luna and Mars surface treatments, including sealed settlements where
  required by the existing environmental rules.
- [ ] Complete coherent scout, science vessel, colony ship and patrol corvette visuals,
  plus the orbital infrastructure visible during the opening campaign.
- [ ] Refine planetary atmosphere, surface and stellar effects at their actual viewing scales.
- [ ] Review portrait crops, lighting and presentation consistency across the ship, species,
  leadership, colony and diplomatic pages.

**Done means:** the opening Human campaign no longer changes abruptly between finished art
and visibly unfinished core assets. New ship roles, species architecture, terrain-hazard
mechanics and story cinematics remain separately scoped work; they do not block this art set.

### VP3 — GUI, motion and packaged visual acceptance

**Status: planned; layout foundations already exist.**

- [ ] Reconcile the pending surface-interface work with the latest colony presentation before
  judging final layouts; an unmerged preview is not shared-game completion.
- [ ] Finish panel spacing, typography, icons, tooltips, notifications, loading transitions and
  selection feedback across the opening campaign.
- [ ] Keep camera transitions smooth and test reduced-motion behavior in the completed build.
- [ ] Polish construction, engine, scanning and combat feedback without obscuring commands.
- [ ] Review the complete research → construction → ships → survey → settlement → save/resume
  journey at 1280×720 and 1920×1080 in the packaged game.
- [ ] Measure frame time and memory on a named reference machine; correct art-related stalls,
  distracting pop-in, clipping, unreadable overlays and missing imports before acceptance.
- [ ] Mark approved assets Production ready and record build revision, captures and limitations.

**Done means:** the public-demo visual milestone is accepted in a playable Windows package,
with clear controls, consistent artwork and measured performance. A successful build or
headless startup alone does not establish visual polish.

### Scheduling and completion reporting

Work on the first reference scene is next in the visual sequence; the full demo art set and
final polish are the two following milestones. This is a work order, not a calendar promise.
There is no committed completion date yet: final 3D quality, the usable asset pipeline and
available production capacity have not been established. Use VP1's measured asset-production
and integration effort to publish an estimate for VP2/VP3, with assumptions and a range.
Do not substitute image-generation turnaround for the time needed to model, texture, animate,
integrate and validate interactive assets.

Report each milestone as planned, in progress, in review, or accepted. Include the exact
playable build and remaining items; do not equate a roadmap entry, source commit or open PR
with delivery in the player's downloaded game. The older version sections below remain
historical/product targets; this near-term sequence governs visual work for the current demo.

## 0.0.x — Foundation / playable simulation prototype

Completed/ongoing foundations include:

- Godot 4 + C# project foundation.
- Simulation isolated from the Godot scene tree.
- Deterministic seeded procedural galaxy generation with quota-controlled archetypes.
- Continuous real-time simulation with pause and adjustable speeds.
- Sustainable-speed/backlog protection for late-game performance.
- Fair-information AI contract and civilization trait model.
- Authoritative per-civilization fog of war.
- Real-time fleet exploration and first contact.
- Colonies, population, basic economy, research, and construction.
- A focused 100-system playable campaign profile with Earth/Sol as the Human origin.
- A 2050 human multi-world opening with Earth, a 100,000-person Luna settlement and a
  250,000-person young Mars settlement under ordinary colony, logistics and surface rules.
- Credit-funded infrastructure, ships, settlement and surface construction plus visible
  colony/fleet operating costs and powered trade revenue.
- Direct Economy and department pages, owned-fleet location controls, and owned-colony
  orbital/surface access.
- A graphical observer-safe Research horizon showing completed, active and currently
  investigable nodes without rendering unknown possibilities.
- Construction, shipbuilding, strategic AI and campaign guidance consume Adaptive Research
  capabilities directly, so retired prototype flags cannot unlock player operations early.
- Adaptive campaigns preserve legacy save data without accumulating the retired Science
  currency; research growth comes from powered, finite Effective Research Labs.
- Industry uses visible physical reserve capacity derived from colony infrastructure and
  completed industrial/orbital projects; idle production is curtailed at the cap.
- Direct 3D surface-building selection, construction cancellation and demolition with
  authoritative ownership, production and partial-refund rules.
- In-place surface upgrades with visible credit/industry requirements, stronger output,
  higher upkeep, distinct 3D presentation and save/load continuity.
- Surface districts derived from matching completed complexes, with visible specialization
  progress and bounded science, industry, trade or energy bonuses.
- Environment-driven 3D colony palettes for temperate, frozen, hot, airless, oceanic,
  reducing-atmosphere and rocky worlds without changing placement physics.
- Player-built powered habitat complexes and closed-loop upgrades that reduce exact-world
  life-support costs, with a bounded 75% maximum reduction.
- Colony overview rows reconcile gross and post-infrastructure life-support costs and show
  local surface power, turning the Colonies page into a direct landing/building decision view.
- The home-system orbital map visually represents launch-complex and shipyard plans, status,
  and active progress using the authoritative construction state; each marker directly opens
  Industry operations.
- Optional asteroid extraction now requires Orbital Industry plus a completed Launch Complex,
  adds bounded industrial output, recurring orbital upkeep, and a connected logistics node.
- Bounded diagnostics, system-spec logging, performance logging, support-bundle export.
- Save format/versioning and migration foundation.
- Automated .NET + pinned-Godot headless validation.
- Machine-validated public Adaptive Research possibility data and RP/Pressure/Lab research-economy design foundation.

## 0.0.5 — Pre-Warp Dawn / 2050 opening

Implemented prototype foundation:

- New campaigns begin on January 1, 2050.
- Player and normal major AI civilizations begin pre-warp.
- Research progression into prototype faster-than-light capability.
- A small number of remote seeded old powers begin already spacefaring.
- Seeded old powers are initially non-expansionist and neutral unless provoked.
- Old powers remain hidden until legitimately detected.

Current design direction substantially deepens this phase beyond the first prototype:

- A human-like 2050 civilization begins with a permanent lunar presence and a young Mars
  colony. Substantial orbital infrastructure and its deeper construction choices remain next.
- Pre-warp gameplay grows through home-system settlement, outposts, orbital construction, resource extraction, logistics, life support, long-duration habitation, supply, and automation.
- Other species begin at a comparable broad era but can have radically different home-system infrastructure and technological history.
- The player-facing operational scale expands from homeworld/local space into the solar system and eventually nearby stars as reach increases.

## 0.0.6 — Construction-driven development

Current validated gameplay baseline on `main` as of 2026-09-07.

- Industry-funded construction projects.
- Planetary Research Network.
- Industrial Automation Program.
- Orbital Launch Complex.
- Orbital Shipyard.
- Warp Test Facility.
- Research can require completed infrastructure.
- Normal AI uses the same prerequisite framework.
- Save format v6 persists construction progress/completion.

The current construction/research list is a prototype and will evolve as the richer solar-system phase and Adaptive Research runtime are implemented.

## 0.0.7 — Physical shipbuilding

Status: paused/incomplete/unvalidated; see `PROJECT_STATE.md` before resuming.

Intended milestone:

- Prototype FTL unlocks ship designs rather than gifting ships.
- Orbital shipyards physically construct spacecraft.
- Initial interstellar roles: scout, science, colony.
- Ship production consumes real industry.
- Colony ships consume/reserve real population.
- Science ships gain a meaningful survey/anomaly role.
- AI and player follow the same core production/prerequisite rules.
- Shipyard queues survive save/load.

When implementation resumes, future ship/research prerequisites must be reconciled with the Adaptive Research capability/possibility model rather than hard-wiring the old prototype tech chain as the final architecture.

## Pre-demo solar-system expansion work

Before the first public demo is considered complete, the pre-warp/early-space phase should become a real game rather than a short technology timer.

Planned direction includes a manageable subset of:

- homeworld and orbital development
- lunar/moon settlements appropriate to the species
- planetary colonies such as a young Mars settlement for a human-like start
- asteroid/resource extraction
- outpost ships and supply nodes
- orbital yards and transport infrastructure
- realistic-ish travel times and transfer constraints without turning the game into orbital-mechanics software
- long-duration life support
- radiation protection
- gravity management
- species-relative planet gravity/habitability effects and long-term population adaptation foundations
- food independence / advanced fabrication / replication progression
- fleet operational endurance and resupply
- prototype FTL with short practical reach
- automation of mature home-system tasks as the player becomes interstellar

## Pre-demo Adaptive Research foundation

The first public demo does not need all public possibility nodes implemented, but it should demonstrate the real research architecture rather than the temporary fixed prototype chain.

Required direction:

- hidden universe-scale Technology Possibility Graph
- player sees only the civilization's currently known/plausible research tree
- branches can appear from need, basic science, observations, discoveries, warfare, environmental conditions, and foreign evidence
- Research Labs generate Research Points
- technologies have minimum lab requirements
- certain technologies require relevant Research Pressure thresholds before becoming available
- multiple projects can run simultaneously when enough unreserved lab capacity exists; no arbitrary fixed research-slot count
- player and AI use the same core research-capacity/availability rules
- only the active civilization-specific research horizon is materialized in runtime/save state

Public seed design data lives under `data/research/v1/`; canonical rules live in `ADAPTIVE_RESEARCH_SYSTEM.md` and `RESEARCH_ECONOMY.md`.

## 0.1.0 — First public playable-demo target

The first public demo should present a coherent civilization arc rather than a technology showcase.

Target experience:

- begin in 2050 as an early multi-world/pre-FTL civilization
- develop the home system
- make strategic construction and adaptive-research choices
- watch the visible research tree change as conditions/discoveries change
- achieve practical FTL
- build the first interstellar spacecraft
- explore legitimately through fog of war
- establish first contact
- colonize at least one extrasolar destination
- encounter meaningful diplomacy and sovereignty/border decisions
- construct basic military forces
- experience an initial combat/conflict loop
- save/load/recover a campaign

Public-demo polish should include:

- main menu and New Game flow
- proper player-facing panels replacing most keyboard-only prototype controls
- usable evolving research-tree / research-lab allocation UI
- clear tooltips/event notifications
- completion of the [VP1–VP3 visual-quality milestones](#current-pre-demo-priority--visual-quality-and-polish)
- a separately tracked first sound/music pass
- tutorial/help sufficient for a new tester
- Windows packaged test build
- visible build/version information
- support-bundle export and diagnostics

Working playable-species scope for the first demo: roughly 3–4 deeply differentiated starts can be sufficient. Quality/depth matters more than species count.

## 0.1+ — Adaptive research / technology-divergence foundation

- Do not use one fully visible universal tree or one giant separately authored fixed tree per species.
- Similar strategic capabilities can come from different technological implementations.
- Each civilization materializes a changing visible tree from a broader hidden possibility graph.
- Biology, environment, resources, culture, history, need, warfare, observations, and discoveries influence which branches emerge.
- Basic science can expose possibilities without immediate practical pressure where appropriate.
- Research Pressure provides contextual availability/urgency without hidden underdog rubber-banding.
- Research Labs provide physical research capacity and determine natural simultaneous-project concurrency.
- Some civilizations may never independently discover FTL.
- Foreign technology can require evidence, analysis, adaptation, reverse engineering, and compatible manufacturing rather than instant unlocking.
- Some foreign technologies may be incompatible, dangerous, incomprehensible, or valuable mainly to third parties.
- Technology can become a diplomatic/economic commodity.
- Research state must remain bounded: static possibility data is shared; campaign saves keep only civilization-specific state.

A much deeper technology-market/licensing/brokerage/hybrid-research system is a strong candidate for a later expansion, but the base architecture must support divergence from the beginning.

## 0.2 — Living civilizations

- Government and leadership change over time.
- Cultural/political evolution.
- Civilizations can fracture, reform, merge, collapse, and create successor states.
- Historical memory influences diplomacy without forcing permanent hostility.
- Relationships and intelligence can fade when contact ends.
- Old relationships may decay from active diplomacy to historical record, cultural memory, and eventually rumor/legend.
- Species lifespan, archives, cultural tradition, government continuity, censorship, and historical significance influence what is remembered.
- Species/culture-specific attitudes toward borders, trade, expansion, surrender, and war.
- Survival-first strategic behavior by default, with explicit cultural exceptions.

## 0.3 — Emerging powers

- Major and minor pre-warp societies progress through technological stages at different rates.
- Adaptive Research allows their development paths to diverge from the player's rather than following a synchronized fixed ladder.
- Some may plateau without native FTL.
- Protection, exploitation, trade, technology assistance, and non-interference create persistent consequences.
- Former pre-warp civilizations can become allies, rivals, major powers, or emergent threats.
- Information quality and sensor sophistication determine whether civilizations can verify threats, bluffs, fleet estimates, and unusual technology.
- Already-spacefaring seeded old powers do not receive automatic expansion behavior simply because they are technologically advanced.

## 0.4 — Subjects, coercion, sovereignty, and asymmetric power

- Vassals, protectorates, tributaries, client states, and culturally distinct subject relationships.
- Political defeat does not automatically end the campaign.
- Subject civilizations can rebuild, negotiate autonomy, cooperate with other subjects, rebel, or break free.
- Coercive diplomacy depends on credibility, intelligence, culture, risk tolerance, and actual strategic position.
- Borders are political warnings/claims rather than physical force fields.
- Civilizations can violate access restrictions and accept resulting diplomatic/military consequences.
- Historical/legal claims influence legitimacy and diplomacy but are not mandatory permission tokens for conquest.
- Occupation, formal ownership, recognition, resistance, logistics, sanctions, and coalition reactions create the real cost of expansion.

## 0.5 — Emergent crises and great-power consequences

- Crises arise from simulation history rather than only scripted timers.
- Expansion, technological imbalance, economic concentration, ideology, civilizational collapse, and political domination can create galaxy-scale threats.
- Powerful empires may deliberately accept huge diplomatic/occupation/logistical consequences because they believe they can survive them.
- Rival civilizations may cooperate against a hegemon/common threat based on legitimate information and their own interests.
- Technological leaders can become complacent naturally, but are not forced to fall behind; observed rival progress can restart urgent research/arms races.
- Crisis resolution can include war, containment, diplomacy, regime change, fragmentation, accommodation, subject relationships, or internal collapse.

## 0.6 — Civilization ark megaproject

- Colossal generation ark requiring a civilization-scale industrial commitment and decades of construction.
- Intergalactic propulsion hardware is megastructure-scale and not a normal ship module.
- Ark is extraordinarily durable but extremely slow, poorly maneuverable, and not designed for conventional combat.
- Limited industrial/defensive lasers and electronic-warfare support; no capital-ship offensive loadout.
- Limited onboard construction for scout, science, and colony craft while anchored.
- Ark construction/operation consumes population, resources, industry, and strategic opportunity.
- Severe drive/core damage can create catastrophic system-scale consequences.
- Deliberate scuttling/core-breach capability becomes a high-stakes strategic/diplomatic tool with enormous cost and consequences.

## 0.7 — Intergalactic exodus

- First-generation intergalactic transit is effectively one-use: transit stresses destroy the specialized drive.
- The ark carries scientific knowledge but not a fully rebuilt industrial civilization.
- Arrival requires settlement and reconstruction before advanced technology can be manufactured at previous scale.
- Intergalactic voyages remain long enough for generational events aboard the ark.
- Returning to the original galaxy requires rebuilding intergalactic capability as another civilization-scale project.

## 0.8 — Persistent multi-galaxy history

- Departed galaxies autosave at departure.
- While the player is away, they advance through compressed strategic historical simulation.
- Outcomes are causally derived from economy, population, technology, logistics, alliances, wars, stability, leadership, expansion, and overextension.
- A former dominant power may conquer most of a galaxy, collapse, fracture into successors, or be replaced by a rising civilization.
- Returning players receive a historical summary and reconstructed current galaxy state.
- Mature lower-level administration can become increasingly automated/delegated so multi-galaxy scale does not become unmanageable micromanagement.

## 0.9 — Deep discovery framework

- Rare undocumented discoveries, artifacts, research chains, unusual technologies, lore, and emergent strategic consequences.
- Discovery chains interact with exploration, Adaptive Research, diplomacy, intelligence, trade, theft, and war rather than behaving as simple collectible checklists.
- Civilizations value unknown artifacts only according to what they legitimately know about them.
- Secret discoveries plug into the same research architecture without being enumerated in the public research catalog.
- Exact chains, triggers, probabilities, and rare AI outcomes are intentionally excluded from this public roadmap.

## 1.0 — Full release target

- Stable long-campaign simulation.
- Mature fair-information AI.
- Rich civilization evolution and diplomacy.
- Mature Adaptive Research with strongly divergent civilization-specific technological histories.
- Deeply differentiated playable species rather than shallow bonus variants.
- Working planning target around 12 major playable species if quality/depth can be maintained.
- Strong late-game performance on target hardware.
- Intergalactic progression and persistent historical continuity.
- Extensive procedural and handcrafted content.
- Mod-friendly data boundaries where secrecy/security constraints permit.
- Production support, crash recovery, diagnostics, accessibility, localization, Steam integration, achievements, and workshop/community planning.

## Development philosophy

- Realism-driven causes/consequences before arbitrary restrictions.
- Playable builds before feature sprawl.
- Fewer deep systems/species rather than many shallow ones.
- The late game should change the player's problems rather than simply inflate numbers.
- Older routine tasks become automatable as civilization scale grows.
- Fix severe player-reported bugs quickly and communicate clearly.
- Stable and experimental branches once Early Access begins.
- Diagnostics and player-provided saves/logs are first-class development inputs.
- Optimize from real measurements, especially long-running campaigns.
- Every major system needs a bounded-memory, cleanup, save-size, and late-game CPU strategy before it is considered architecturally mature.
- Research-catalog changes must pass machine validation for stable IDs/prerequisites/pressure references/cycles before merge.
