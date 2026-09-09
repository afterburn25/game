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

## Immediate roadmap — Civilization currencies and Credits

Status: planned. Existing prototype Credit values remain an internal compatibility concern
until this progression is implemented; they should not force every early civilization's
player-facing economy to display a universal currency prematurely.

Currency progression:

1. **Sovereign currency:** Each civilization begins with its own named currency, symbol and
   denomination. Humans begin with their contemporary national or chosen starting currency;
   nonhuman civilizations use culturally appropriate currencies. Economy, construction,
   upkeep, wages and domestic trade show only that civilization's currency.
2. **Interstellar exchange:** Contact and trade expose foreign currencies and exchange rates.
   Foreign denominations appear only where they are relevant, such as trade agreements,
   market conversion, diplomacy and financial intelligence. Ordinary domestic screens still
   show the local currency.
3. **Credit development:** Credits become visible only after the civilization can actually
   hold and use them through the required research, institutions, trade network or diplomatic
   agreement. Unlocking the concept without access to a Credit market is not sufficient.
4. **Transition:** Local currency and Credits coexist only while both are genuinely usable.
   The interface shows both balances on exchange and transition screens, provides the current
   conversion rate and clearly identifies which currency will pay a quoted cost.
5. **Credit adoption:** Once a civilization fully replaces its sovereign currency, convert
   balances, contracts, prices, upkeep, debts and queued costs using a recorded transition
   rate. Remove the obsolete currency from ordinary play screens while retaining it in
   historical records and old transaction details.

Rules and safeguards:

- Currency is owned by a civilization or issuing institution; its name, symbol and formatting
  are presentation data separate from the underlying economic quantity.
- Never show local currency and Credits together merely because the simulation stores a
  compatibility Credit value. The player interface derives the visible denomination from the
  civilization's current monetary stage.
- Avoid a fixed universal Dollar-to-Credit conversion. Exchange rates should reflect the
  issuing economy, monetary policy, trade access, stability and market conditions. Provide a
  stable starting reference value for understandable prices, then allow bounded movement.
- Maintain a separate Credit conversion for every actively issued civilization currency.
  For example, the Terran Dollar, a foreign Union Mark and an alien Exchange Unit can each
  buy a different fraction of one Credit at the same moment. Civilizations of the same species
  still receive separate rates when they operate separate economies or issue separate money.
- Establish a currency's first rate from a comparable basket of real output and costs, such as
  energy, food or life support, industrial production, labor and transport. Species needs can
  change the basket, but a racial label alone must never arbitrarily make a currency stronger
  or weaker.
- Store authoritative rates against Credits as the common settlement reference and derive
  foreign-to-foreign quotes from those rates. This avoids maintaining contradictory conversion
  tables for every possible pair while still presenting direct local quotes to the player.
- Rates may move within readable bounds from production, reserves, trade balance, debt,
  stability, shortages, war, sanctions and market access. Use smoothing and update intervals
  so normal play cannot create meaningless second-to-second price flicker.
- Model exchange availability and liquidity as well as the numerical rate. Unknown, isolated,
  embargoed or collapsed currencies may have no trustworthy conversion; thin markets may add
  a larger fee or spread. The interface must say `no available exchange` rather than inventing
  a rate.
- A displayed conversion must include its direction and unit, for example `1 Credit = 4.20
  Terran Dollars`, plus any fee or spread before the player confirms a transaction.
- Player and AI civilizations follow the same adoption, exchange and settlement rules. A race
  may retain its own currency indefinitely if it can support foreign settlement or refuses
  Credit adoption, though this creates real trade friction rather than a hidden penalty.
- Currency replacement is a deliberate institutional transition, not a UI rename. Existing
  saves and active contracts must migrate without creating or destroying purchasing power.
- Developer mode may inspect canonical internal values and conversion calculations, while
  Player mode shows only currencies legitimately known and usable by that civilization.

Initial acceptance criteria:

- A new early Human campaign displays the selected Human currency and no Credit balance.
- Every nonhuman civilization can display its own currency without changing shared economy
  rules or duplicating the entire economy implementation.
- Credits do not appear anywhere in Player mode before they are usable.
- During transition, every price identifies its payment currency and conversions reconcile to
  the authoritative balance.
- Two civilizations with different issued currencies can hold different Credit rates, and
  converting through Credits produces a consistent direct quote after declared fees.
- Saving and reloading preserves every known rate, its last update time, availability and
  market spread without revealing rates the player has not legitimately discovered.
- After full adoption, the retired currency disappears from current economy panels without
  corrupting saves, contracts, queues or historical records.

## Immediate roadmap — Playable species identities and balance

Status: planned balance layer over the four implemented biological profiles. The physical
facts already in `SpeciesCatalog` remain the source of effects; the values below are initial
tuning targets and must be proven in full campaign simulation before being treated as final.

Design rules:

- Build advantages and disadvantages from biology, environment, infrastructure and history.
  Do not attach arbitrary universal research, industry, combat or income percentages to a
  species name.
- Give every playable species a distinct strategic opportunity, a meaningful operating cost
  and at least one environment where it excels. No species should be best across population,
  colonization, logistics, research and warfare at once.
- Keep biology separate from culture and government. Two civilizations of the same species
  can develop different economies, doctrines, institutions, currencies and research paths.
- Balance comparable outcomes over varied maps rather than making every starting number
  identical. Founding population, adapted infrastructure, reserves and ships should provide
  comparable early productive capacity and survival runway while preserving different needs.
- Show causes to the player. A tooltip should say that immersion infrastructure or thermal
  control creates a cost, rather than presenting an unexplained `racial penalty`.

Pressure tolerance, settlement cost and adapted lineages:

- Every species has an atmospheric-pressure profile measured in kPa with four readable bands:
  preferred, comfortable, marginally survivable and naturally lethal. High pressure reduces
  suitability progressively before the lethal threshold; it must not behave as a single
  habitable/uninhabitable switch.
- Use these implemented unadapted pressure ranges as the initial physical baseline:

  | Species | Preferred | Comfortable range | Natural survival range |
  | --- | ---: | ---: | ---: |
  | Terran Baseline | 101.3 kPa | 66.3–136.3 kPa | 16.3–186.3 kPa |
  | Pelagic High-Pressure | 350 kPa | 225–475 kPa | 50–650 kPa |
  | Compact High-Gravity | 160 kPa | 100–220 kPa | 20–300 kPa |
  | Cryogenic Hydrocarbon | 150 kPa | 85–215 kPa | 15–285 kPa |

- A population at its locally adapted preferred pressure receives the full baseline. Any
  meaningful departure from that preference creates a negative cost: small and manageable
  within the comfortable range, then increasingly severe across the wider natural survival
  range. Pressure mismatch creates explicit health, productivity,
  reproduction, mortality, medical and life-support costs. Beyond that range, unprotected
  settlement is lethal and requires a sealed pressure-controlled habitat; technology keeps
  the population alive but does not pretend its biology is naturally comfortable.
- Settlement viability considers pressure together with gravity, temperature, radiation,
  atmosphere, solvent and immersion. A planet is not simply `habitable`: the colony panel
  shows the limiting factor, expected support demand and projected demographic effect before
  settlement is confirmed.
- Short residence produces reversible acclimatization over years. Heritable changes require
  locally born populations and many generations. Default tuning begins preference movement
  around 12 generations and broader tolerance around 18 generations, modified by each
  species' developmental plasticity and multigenerational adaptability.
- Adaptation belongs to the local population cohort, not instantly to the entire species.
  Migrants retain their inherited range; locally born descendants gradually form a distinct
  adapted lineage. Movement and intermarriage between colonies blend lineages gradually using
  bounded cohorts rather than creating one record per individual.
- A mature high-pressure lineage can become naturally comfortable above its ancestor's range,
  receive lower support costs and improved health, reproduction and local operations there,
  and eventually open still higher-pressure settlement candidates within its biological
  ceiling. It may become less comfortable near the ancestral pressure, preventing adaptation
  from becoming a permanent universal bonus.
- Natural adaptation cannot cross a lethal gap, change biological solvent, create a breathable
  atmosphere or remove an immersion requirement. Pressure-controlled habitats, medicine and
  deliberate biological engineering can establish a survivable bridge, but engineered change
  has its own research, time, risk and infrastructure requirements.
- When inherited divergence crosses maintained thresholds, present the population as a named
  derived lineage or subspecies of its parent species. It retains ancestry and shared identity
  while gaining its own pressure range, appearance variations, home-environment advantages and
  compatibility data. Reserve `hybrid species` for actual mixed ancestry where reproduction
  and xenobiology permit it; environmental descendants are adapted lineages.
- Keep natural expansion bounded. Using the current adaptation ceilings, initial fully matured
  high-pressure targets are approximately 233 kPa for Terrans, 779 kPa for Pelagics, 349 kPa
  for Compact High-Gravity populations and 326 kPa for Cryogenic populations. These are tuning
  ceilings, not immediate settlement limits, and require sustained viable residence across the
  lineage's full generational timescale.
- Persist lineage origin, founding date, residence duration, generations, local-born fraction,
  pressure preference shift and pressure-tolerance expansion. The UI should show progress as
  biological history and forecast ranges, never as a rapidly filling generic experience bar.
- Apply the same cohort adaptation rules to player and AI populations. AI settlement planning
  must price present support costs and future adaptation potential without knowing hidden
  planet data.

Low-gravity deconditioning and combat consequences:

- Track short-term physical conditioning separately from inherited gravity adaptation.
  Adults living below their adapted gravity gradually lose bone strength, muscle capacity,
  cardiovascular tolerance and ability to carry heavy equipment unless the colony pays for
  exercise, medicine or artificial-gravity facilities. Much of this adult deconditioning can
  recover after return and rehabilitation.
- Locally born populations developing across many low-gravity generations gradually shift
  toward lighter frames and lower musculoskeletal robustness. This inherited change belongs
  to the local cohort and can eventually form a named low-gravity lineage; it must not weaken
  every remote population of the parent species.
- Feed actual gravity suitability, current conditioning, body mass and musculoskeletal
  robustness into personal-combat and local-operations calculations. Low-gravity populations
  should have reduced load carrying, recoil control, close-combat force, injury resistance and
  endurance when fighting in stronger gravity, making unassisted infantry and boarding duty a
  poor fit over time.
- Keep the result environment-relative. A low-gravity lineage understands movement in its own
  habitat and may maneuver effectively there, while a heavy-world opponent also faces altered
  traction and movement. Do not turn either outcome into a universal racial combat percentage.
- Show the distinction between civilian habitability and military suitability. A world can
  support a healthy low-gravity-adapted population while that population remains poorly suited
  to high-load personal combat, planetary assault or work on a stronger-gravity world.
- Provide costly countermeasures rather than immunity: resistance training, centrifuge
  habitats, pharmaceuticals, developmental gravity programs, powered armor and exoskeletons.
  These consume space, energy, maintenance, medical capacity and equipment, and can reduce or
  compensate for weakness only while supplied and operational.
- Allow player and AI forces to recruit from suitable populations, use mixed-species units or
  assign low-gravity personnel to roles that depend less on raw physical force. Crew quality,
  doctrine, morale, weapons and tactics remain separate from biological capacity.
- Migration back toward stronger gravity creates a visible rehabilitation period, elevated
  injury/health risk and, for strongly inherited lineages, a long-term environmental support
  burden. Natural readaptation again requires viable residence and generations.
- Persist cohort conditioning and inherited gravity range through save/load. Keep the model
  bounded by aggregating nearby adaptation states and never tracking individual citizens.

Initial biological stat cards:

| Species | Metabolic demand | Lifespan | Relative demographic pace | Radiation tolerance | Adaptation responsiveness |
| --- | ---: | ---: | ---: | ---: | ---: |
| Terran Baseline | 1.00 | 82 years | 1.00 | 0.10 | 1.00 |
| Pelagic High-Pressure | 0.85 | 140 years | about 0.79 | 0.18 | 0.85 |
| Compact High-Gravity | 1.20 | 96 years | about 0.79 | 0.26 | 0.90 |
| Cryogenic Hydrocarbon | 0.22 | 360 years | about 0.31 | 0.38 | 0.35 |

The demographic pace is the current bounded result derived from generation length, maturity
age and reproductive-event throughput; it is not a free population-growth modifier. Final
growth still depends on health, capacity, living conditions, policy and resources.

### Terran Baseline

Advantages:

- Fastest acclimatization and multigenerational environmental adaptation of the initial four.
- Baseline demographic replacement and familiar carbon-water biosphere compatibility.
- Flexible upright workspaces and mature starting infrastructure in the authored Sol system.

Disadvantages:

- Lowest natural radiation tolerance.
- No natural dormancy and full baseline metabolic support demand.
- Poor unprotected performance in high gravity, extreme pressure, severe heat/cold and
  non-water biospheres.

Strategic identity: adaptable generalists with the easiest initial Human learning curve, but
no inherent immunity to hostile space environments.

### Pelagic High-Pressure

Advantages:

- Lower routine metabolic demand, long lifespan and four capable manipulators.
- Excellent natural operation in immersed, high-pressure water environments that are costly
  or inaccessible to terrestrial populations.
- Torpor can reduce demand during limited emergencies and long low-activity operations.

Disadvantages:

- Requires buoyant immersed workspaces; dry ships, stations and colonies need specialized
  life support and construction.
- Slower demographic replacement than Terrans.
- Low-pressure terrestrial environments and incompatible biospheres impose severe support
  burdens despite broadly carbon-water chemistry.

Strategic identity: efficient aquatic infrastructure and access to oceanic niches in exchange
for expensive operation outside them.

### Compact High-Gravity

Advantages:

- Highest musculoskeletal robustness of the initial four and strong performance on heavy
  worlds where other species need gravity mitigation.
- Better natural radiation resilience than Terrans or Pelagics.
- Dense horizontal body plan suits compact, high-load environments and physically demanding
  local operations when conditions match its biology.

Disadvantages:

- Highest routine metabolic demand of the initial four.
- Slower demographic replacement than Terrans.
- Low-gravity habitats and ordinary Terran-pressure environments require adapted workspace,
  health support or gravity systems; robust biology does not grant a universal combat bonus.

Strategic identity: capable heavy-world operators whose population and fleets are expensive
to sustain away from appropriately engineered environments.

### Cryogenic Hydrocarbon

Advantages:

- Extremely low routine metabolic demand, longest lifespan and highest natural radiation
  tolerance of the initial four.
- Natural deep dormancy can reduce biological demand to 8% for up to roughly 180 days, making
  carefully planned long-duration missions unusually efficient.
- Can exploit cryogenic hydrocarbon environments that are extremely hostile to water-based
  species.

Disadvantages:

- By far the slowest demographic replacement: approximately 31% of the Terran baseline pace,
  with maturity around age 55 and very long recovery from population loss.
- Lowest adaptation responsiveness and narrow compatibility with cold reducing-atmosphere,
  hydrocarbon-solvent environments.
- Warm carbon-water worlds, shared habitats and conventional allied infrastructure require
  extensive thermal isolation, sealed biospheres and specialized industry.

Strategic identity: patient, resilient and logistically efficient in its native conditions,
but exceptionally vulnerable to demographic losses and costly environmental incompatibility.

Starting-equivalence rules:

- Every species starts with one viable homeworld, two viable nearby expansion candidates under
  the Standard setup, adapted home infrastructure and ships that can support its own biology.
- Compare useful output and reserve duration rather than raw population counts. A species with
  heavier biological demand may begin with more support capacity; a slow-growing species may
  begin with a stable mature population, but must still bear the long-term cost of casualties.
- Starting differences may change building types, habitat volume, workforce organization and
  resource mix. They must not secretly grant free upkeep, impossible technology or recurring
  resources after play begins.
- Species selection presents clear strengths, constraints, preferred environments and an
  estimated complexity level without a misleading single overall power score.

Balance validation before enabling all four player starts:

- Run deterministic campaign batches across representative barred-spiral seeds, homeworld
  environments and neighboring-system layouts using the same AI planning quality.
- Measure 5-, 10-, 20- and 50-year survival, economic output, support burden, population,
  research capacity, exploration reach, colonization opportunities, fleet readiness and
  recovery from equivalent disasters.
- Run mirrored one-on-one and four-way AI campaigns. On neutral mirrored starts, no species
  should sustain a win rate outside 45–55% without an explainable map interaction; across the
  full varied-map suite, investigate any result outside 40–60%.
- Test each species in favorable, average and hostile regions. Its favorable environment
  should feel valuable, while hostile starts remain playable through visible engineering and
  strategy rather than hidden compensation.
- Validate every pressure boundary just below, at and just above comfortable, survivable and
  fully adapted limits. Confirm that marginal colonies pay real costs, lethal exposure cannot
  pass as natural settlement, and adaptation cannot advance without viable sustained
  population residence and new generations.
- Run long migrations in both directions to confirm a high-pressure lineage gains a local
  advantage, retains a meaningful ancestral-pressure tradeoff and never rewrites the immutable
  base species or every remote population.
- Run low-gravity residence, return-migration and combat-readiness benchmarks over months,
  years and generations. Verify reversible deconditioning, inherited lineage divergence,
  countermeasure operating costs and severe high-gravity infantry limitations without making
  a peaceful low-gravity colony nonviable.
- Stress the Cryogenic profile specifically for runaway low-upkeep expansion and stress the
  Compact profile for excessive support costs. Tune causal inputs, infrastructure and starting
  capacity before considering any narrow explicit modifier.
- Re-run the full balance suite whenever physiology, population, logistics, habitability,
  surface construction, ship support or starting-generation rules change.

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
