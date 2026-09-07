# Development Roadmap

This is the public roadmap for the untitled real-time space civilization strategy game. Exact hidden discoveries and secret outcomes are intentionally omitted.

## 0.0.x — Foundation / playable simulation prototype

- Godot 4 + C# project foundation.
- Simulation isolated from the Godot scene tree.
- Deterministic seeded procedural galaxy generation with quota-controlled archetypes.
- Continuous real-time simulation with pause and adjustable speeds.
- Sustainable-speed/backlog protection for late-game performance.
- Clickable/pannable/zoomable galaxy prototype.
- Fair-information AI contract and civilization trait model.
- Bounded diagnostics, system-spec logging, performance logging, support-bundle export.
- Save format/versioning and autosave foundation.
- Automated headless/stress testing.

## 0.1 — First sellable core target

- Galaxy setup and seeded generation.
- Playable civilization selection/customization.
- Exploration and fog of war.
- Colonization and population growth.
- Economy, construction, science, and technology progression.
- Fleet construction and movement.
- Initial ship design and combat.
- Multiple AI civilization personalities whose behavior changes with strength, knowledge, culture, and circumstance.
- Diplomacy, trade, treaties, war, peace, and territorial negotiation.
- Pre-warp civilizations with independent development paths.
- Save/load/autosave and robust recovery.
- Player-facing support diagnostics and log-folder access.
- Late-game performance benchmark targets.

## 0.2 — Living civilizations

- Government and leadership change over time.
- Cultural/political evolution.
- Civilizations can fracture, reform, merge, collapse, and create successor states.
- Historical memory influences diplomacy without forcing permanent hostility.
- Race/culture-specific attitudes toward borders, trade, expansion, surrender, and war.
- Survival-first strategic behavior by default, with explicit cultural exceptions such as honor-bound societies.

## 0.3 — Emerging powers

- Pre-warp societies progress through technological stages and can become interstellar powers.
- Protection, exploitation, trade, technology assistance, and non-interference create persistent consequences.
- Former pre-warp civilizations can become allies, rivals, major powers, or emergent threats.
- Information quality and sensor sophistication determine whether civilizations can verify threats, bluffs, fleet estimates, and unusual technology.

## 0.4 — Subjects, coercion, and asymmetric power

- Vassals, protectorates, tributaries, client states, and other culturally distinct subject relationships.
- Political defeat does not automatically end the campaign.
- Subject civilizations can rebuild, negotiate autonomy, cooperate with other subjects, rebel, or break free.
- Coercive diplomacy depends on credibility, intelligence, culture, risk tolerance, and actual strategic position.

## 0.5 — Emergent crises

- Crises arise from simulation history rather than only scripted timers.
- Expansion, technological imbalance, economic concentration, ideology, civilizational collapse, and political domination can create galaxy-scale threats.
- Rival civilizations may cooperate against a common threat based on their own knowledge and interests.
- Crisis resolution can include war, containment, diplomacy, regime change, fragmentation, or accommodation.

## 0.6 — Civilization ark megaproject

- Colossal generation ark requiring a civilization-scale industrial commitment and decades of construction.
- Intergalactic propulsion hardware is megastructure-scale and not a normal ship module.
- Ark is extraordinarily durable but extremely slow, poorly maneuverable, and not designed for combat.
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
- Outcomes are causally derived from each civilization's economy, population, technology, logistics, alliances, wars, stability, leadership, expansion, and overextension.
- A former dominant power may conquer most of a galaxy, collapse from overextension, fracture into successors, or be replaced by a rising civilization.
- Returning players receive a historical summary and a reconstructed current galaxy state.

## 0.9 — Deep discovery framework

- Rare undocumented discoveries, artifacts, research chains, unusual technologies, lore, and emergent strategic consequences.
- Discovery chains interact with exploration, research, diplomacy, intelligence, trade, theft, and war rather than behaving as simple collectible checklists.
- Civilizations value unknown artifacts only according to what they legitimately know about them.
- Exact chains, triggers, probabilities, and rare AI outcomes are intentionally excluded from this public roadmap.

## 1.0 — Full release target

- Stable long-campaign simulation.
- Mature fair-information AI.
- Rich civilization evolution and diplomacy.
- Strong late-game performance on target hardware.
- Intergalactic progression and persistent historical continuity.
- Extensive procedural and handcrafted content.
- Mod-friendly data boundaries where secrecy/security constraints permit.
- Production support, crash recovery, diagnostics, accessibility, localization, Steam integration, achievements, and workshop/community planning.

## Development philosophy

- Playable builds before feature sprawl.
- Fix severe player-reported bugs quickly and communicate clearly.
- Stable and experimental branches once Early Access begins.
- Frequent development activity; builds ship when they improve the game rather than to satisfy an arbitrary daily-build quota.
- Diagnostics and player-provided saves/logs are first-class development inputs.
- Optimize from real measurements, especially long-running campaigns.
