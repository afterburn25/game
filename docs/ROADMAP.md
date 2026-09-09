# Stellar Continuum — Development Roadmap

This is the public roadmap for **Stellar Continuum**, the working title for the real-time space civilization strategy game. Commercial naming clearance remains pending; see `BRANDING.md`. Exact hidden discoveries and secret outcomes are intentionally omitted.

For durable design rules, current baseline status, and engineering constraints, also read the continuity records linked from the repository README.

## Immediate roadmap — Sandbox generation setup

Status: planned; this section records the agreed design and does not indicate that the
setup controls have been implemented.

Priority: build this after the current graphical New Game selector and before expanding
the 100-system Sandbox with additional content. Keep the ordinary path simple: choose
Sandbox, review the generated setup, then start. Put optional controls behind Advanced
Settings.

Recommended initial configuration:

| Setting | Default | Initial choices |
| --- | --- | --- |
| Galaxy size | 100 systems | 100 only for the current milestone |
| Galaxy shape | Barred spiral | Barred spiral initially; spiral, elliptical, ring and irregular later |
| Seed | Random | Randomize, enter, and copy |
| Stellar variety | Balanced | Realistic, Balanced, Exotic |
| Planet-bearing systems | Common | Sparse, Common, Crowded |
| Habitable worlds | Uncommon | Rare, Uncommon, Common |
| Guaranteed nearby habitable worlds | 2 per major civilization | 0, 1, 2 |
| Other civilizations | 5 | 3, 5, 8 |
| Ancient civilizations | Rare | None, Rare, Standard |
| Space hazards | Standard | Low, Standard, High |
| Starting development | Early Space Age | Fixed initially |
| Difficulty | Standard | Explorer, Standard, Strategist |

Seed and generation rules:

- Treat the seed and selected generation options as separate inputs. Reproducing a
  galaxy requires the same seed, options and generator version.
- Accept both whole numbers and normalized text such as `MY-FIRST-GALAXY` or
  `SOL-ASCENDANT-42`; convert text deterministically to the internal numeric seed.
- Generate a fresh seed by default and provide Randomize, Copy Setup and Restore Defaults.
- Store the entered seed, internal seed, complete option snapshot, generator version,
  game version, creation time, player civilization/species and starting home location in
  campaign metadata.
- Preserve legacy numeric seeds and existing saves.
- Show a spoiler-free summary, for example: `100 systems · balanced distribution ·
  uncommon habitable worlds · 5 civilizations · rare ancient powers`.
- Difficulty must preserve common simulation rules. It may adjust AI planning quality,
  aggression, coordination and tolerance for mistakes, but must not grant hidden free
  resources or exempt a participant from normal costs.

Stellar and planetary composition:

- Keep physical stellar classification separate from gameplay content. Star class, number
  of stars, planetary architecture, resources, ruins, anomalies and hazards are separate
  seeded layers that may overlap. A resource-rich system is not a star type.
- For the 100-system Balanced profile, use an exact quota deck as the initial tuning target:

  | Primary object or life stage | Systems | Percent |
  | --- | ---: | ---: |
  | M-type red dwarf | 48 | 48% |
  | K-type orange dwarf | 20 | 20% |
  | G-type yellow dwarf | 11 | 11% |
  | F-type yellow-white dwarf | 6 | 6% |
  | A-type white star | 3 | 3% |
  | Hot blue B/O star | 1 | 1% |
  | Red/orange giant | 4 | 4% |
  | White dwarf | 3 | 3% |
  | Neutron star or pulsar | 2 | 2% |
  | Black hole | 1 | 1% |
  | Young star or protostar | 1 | 1% |

- This Balanced profile deliberately enriches rare landmarks for a fun 100-system strategy
  map. Realistic shifts more of the quota to long-lived dwarf stars and may generate zero
  black holes or pulsars; Exotic permits up to 2 black holes, 3 pulsars and more young-star,
  giant and nebular systems. The setup page shows resulting counts before play.
- Treat multiple-star structure separately from primary type. The initial Balanced target is
  76 single-star, 20 binary and 4 triple-star systems. Companion stars do not increase the
  selected system count.
- The initial Balanced planetary-architecture target is 18 systems with no major planets,
  22 with 1–2, 42 with 3–6, 14 with 7–10 and 4 with 11–14. A system without major planets
  may still contain asteroid belts, debris, accretion material, stations or discoveries.
- Weight planetary architecture by stellar age and type. Compact objects and very young or
  short-lived stars should usually have no conventional planets; stable dwarf stars should
  supply most ordinary planetary systems. Do not guarantee every star a planet.
- Place rare objects with seeded spacing rules and outside every new major civilization's
  protected opening area. A black hole or pulsar should be a strategic landmark rather than
  an accidental immediate-start hazard.

Starting-world fairness and habitability:

- Every major civilization starts on one species-compatible homeworld. Humans always start
  on Earth in the authored Sol catalog; every other playable race receives its own persistent
  named homeworld and starting position.
- Standard setup guarantees 2 additional colonizable worlds compatible with each major
  civilization within practical early exploration range. These are candidates to discover
  and colonize, not free starting colonies. The player may select 0, 1 or 2 in Advanced
  Settings, and the same selected rule applies to player and AI major civilizations.
- Guarantee suitability for the civilization's species rather than using a universal
  `habitable` flag. A world suitable for one biology may be marginal or hostile to another.
  One physical world may satisfy more than one civilization's guarantee when appropriate,
  but generation must prevent overlapping starts or contested guaranteed opening space.
- Apply the galaxy-wide Habitable Worlds setting only after homeworld and nearby guarantees.
  It controls additional naturally compatible discoveries, while terraforming, habitats and
  life support can make otherwise unsuitable worlds useful later.
- Minor pre-space societies and ancient powers use their own scenario rules and do not
  consume a major civilization's nearby-world guarantee unless they are configured as a
  normal competing start.
- Show only aggregate setup counts. Exact locations, planetary environments and which race
  can thrive on each undiscovered world remain hidden behind exploration.

Generation order and safeguards:

1. Reserve Sol and all other home systems with minimum separation and fair access.
2. Allocate physical stellar quotas, then place rare objects using safety and spacing rules.
3. Assign single, binary and triple structures without changing the system total.
4. Generate planetary architecture from star type, age and seeded variation, including
   legitimately planetless systems.
5. Satisfy each major civilization's species-relative nearby-world guarantee.
6. Add remaining environments, resources, hazards, ruins and anomalies as independent layers.
7. Validate exact totals, homeworld viability, reachable expansion choices, start separation
   and deterministic reproduction before accepting the generated galaxy.

Advanced controls should use presets and bounded counts rather than a page of independent raw
percentages. Changing one exact star count must rebalance the remaining 100-system allocation,
show a live count preview and prevent impossible totals. The seed, preset, resolved counts and
generator version all belong in the saved/shareable setup data.

Galaxy form, artwork and star placement:

- Replace the single fixed galaxy picture with a shape-driven galaxy presentation. The same
  seeded shape field must control both the luminous galaxy artwork and playable system
  coordinates, so stars appear embedded in the arms, central bar, core, ring or irregular
  concentrations instead of being scattered over an unrelated image.
- Use a barred spiral matching the Milky Way as the default 100-system Sandbox. Its systems
  must cover the full visible galactic form: central bar and bulge, multiple spiral arms and
  a sparse outer edge. Leave only a small presentation margin around the occupied galaxy.
- Treat the 100 systems as the campaign's strategically significant navigable systems across
  the galaxy, rather than implying that they are every physical star in the Milky Way. Dense
  unresolved star fields in the artwork communicate the much larger background population.
- Give each supported shape its own art construction and placement rules:
  barred spiral has a bright elongated core and curved arms; spiral uses a rounder nucleus;
  elliptical uses a smooth concentrated distribution; ring places most systems around a
  luminous annulus; irregular uses asymmetric clouds and knots. Shape changes must alter both
  appearance and travel topology.
- Galaxy size must change composition and density rather than stretching one image. The
  current 100-system map receives purpose-built framing and system-marker scale. Future larger
  maps receive more detailed arms, a larger navigable canvas and additional system density,
  with their own tuned art profile.
- Build the galaxy from high-resolution layered assets and procedural fields: distant stars,
  dust lanes, emission regions, nebula color, central glow and foreground system markers.
  Render those layers at suitable detail levels so zooming inward reveals detail without
  enlarging a low-resolution bitmap or making the image blurry.
- At the full-galaxy zoom level, surround the playable galaxy with a seeded deep-space field
  containing many distant galaxies. Vary their apparent size, distance, brightness, color,
  rotation and morphology, including spirals, barred spirals, ellipticals, lenticular forms,
  irregulars, edge-on discs, small companions and faint galaxy clusters.
- Distribute background galaxies at convincing depths rather than as evenly spaced icons.
  Use scale, haze, red-shifted color, reduced contrast and subtle parallax to communicate
  distance. A few nearby companions may show structure, while the most distant objects appear
  as small diffuse lights and clustered smudges.
- Keep the playable galaxy visually dominant and unmistakable. Background galaxies must not
  resemble selectable star-system markers or imply that they can be entered during the
  initial single-galaxy campaign. Hover and selection behavior applies only when later
  intergalactic gameplay makes a galaxy a real destination.
- Generate the deep-space composition from the campaign seed while preserving important
  authored identity, such as the Milky Way's recognizable companion galaxies. Save its art
  profile version so the same campaign restores the same surrounding universe.
- Render distant galaxies in performance-bounded layers with reusable high-resolution source
  art, seeded variations and aggressive detail scaling. They should remain clean at supported
  resolutions without drawing hundreds of full-detail objects every frame.
- Keep system markers, routes, fleets, selection effects and labels in a separate sharp map
  layer above the galaxy art. Marker brightness and size remain readable at every zoom level
  without changing the underlying coordinates.
- Preserve continuous left-drag panning and wheel zoom. Zooming toward a selected system must
  keep it under the cursor/focus point and transition into its solar-system view without
  requiring a double-click. Zooming back out restores the same galactic position and scale.
- Fog of war may hide system identity, routes, hazards and ownership, but it should not replace
  the galaxy with a blank field. Unsurveyed regions retain atmospheric galaxy art and only the
  information the civilization could legitimately know.
- Derive all shape variation from the campaign seed and store the shape/art profile version in
  campaign metadata so the visual layout reproduces exactly after save/load and setup sharing.

Implementation order:

1. Add the Sandbox setup page with random and custom seed entry and the barred-spiral preview.
2. Persist the complete generation configuration, galaxy shape and generator/art profile
   versions in saves.
3. Add deterministic same-seed/same-options tests for simulation data, system coordinates and
   galaxy-art parameters.
4. Replace the fixed galaxy image and circular random scatter with a shared shape-driven art
   and coordinate field sized specifically for the 100-system map.
5. Separate physical star types from content tags; add stellar-variety, planetary-density,
   species-relative habitability, guaranteed-nearby-world, civilization, ancient-power and
   hazard controls.
6. Add the spoiler-free summary and shareable setup code.
7. Add additional galaxy shapes and sizes only after the 100-system barred spiral is readable,
   attractive and fully navigable.
8. Add difficulty profiles after AI behavior can express meaningful differences.

Acceptance criteria:

- The default path starts a recommended 100-system Sandbox without requiring Advanced
  Settings.
- Randomize visibly changes the seed; copying and re-entering a setup reproduces the same
  starting galaxy on the same generator version.
- Every selected option is visible before confirmation and survives save/load.
- Invalid seeds fail cleanly in the setup page with a useful message.
- Generation options never reveal undiscovered systems, species, hazards or outcomes.
- The selected stellar and planetary quotas total exactly 100 systems, including legitimately
  planetless systems, and every major civilization receives the selected number of viable
  nearby expansion candidates without overlapping protected starts.
- The default map clearly reads as a complete barred-spiral Milky Way; playable systems occupy
  its core, arms and edge, and no large decorative region is left disconnected from the map.
- Galaxy art remains crisp through its supported zoom range, and continuous zoom can enter a
  focused solar system and return to the same galaxy position without a double-click.
- The whole-galaxy view includes a varied, convincing deep-space population of distant
  galaxies at multiple apparent depths; none can be mistaken for a selectable system or an
  immediately playable destination.
- Player and AI remain subject to the same authoritative economy, research, construction,
  movement and combat rules.

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
- basic sound/visual polish
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
