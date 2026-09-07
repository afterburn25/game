# Development Roadmap

Current development version: **0.0.1-dev.1**

This roadmap captures the public design direction. Exact hidden discoveries, rare triggers, secret artifact solutions, and undisclosed crisis probabilities are intentionally not documented here.

## Product identity

This is an original real-time space civilization strategy game. It is not intended to reproduce the systems, economy, diplomacy, or content model of any existing title.

Core identity:

- Continuous real-time simulation with pause and adjustable speed.
- Sustainable simulation speed is more important than extreme speed multipliers.
- Civilizations act from information they legitimately possess. AI receives no fog-of-war omniscience.
- Species, governments, rulers, cultures, and circumstances produce materially different strategic behavior.
- Survival is the default highest strategic priority; explicit cultural traits such as Honor may override normal self-preservation in specific situations.
- History matters. Wars, betrayals, aid, trade, migration, collapse, succession, and ancient relationships leave persistent consequences.
- Borders do not generate universal hostility. Territorial pressure is interpreted through culture, claims, strategic need, trust, history, treaties, scarcity, and relative power.
- Political defeat is not necessarily game over. Vassalage, protectorates, fragmentation, exile, and eventual recovery are playable states.
- The galaxy should generate stories through interacting systems rather than relying only on scripted events.

## Phase 0 — Simulation foundation (ACTIVE)

Goal: establish architecture we can safely expand for years without rewriting the whole game.

- Godot 4.7 stable project foundation.
- Deterministic seeded procedural galaxy generation.
- Weighted star/system archetypes with validation rather than uncontrolled RNG.
- Real-time fixed-step simulation clock.
- Pause plus conservative speed controls.
- Per-frame simulation work budget to prevent a spiral-of-death at high speed.
- Clickable galaxy map prototype.
- Civilization profile scaffolding.
- Per-civilization knowledge/fog-of-war model.
- Survival-first AI decision framework.
- Session logging and hardware diagnostics.
- Performance counters for later late-game optimization.
- Versioned save-state architecture.

Exit criteria: a generated galaxy can run continuously, civilizations possess their own knowledge state, the UI remains responsive while simulation speed changes, and diagnostic logs capture enough context to trace failures.

## Phase 1 — First playable civilization loop

- Player civilization creation.
- Home system and initial population.
- Scouting and exploration.
- Unknown contacts and incomplete sensor information.
- Planet/system claiming and colonization.
- Population, production, science, energy/credits, and strategic resources.
- Construction queues and infrastructure.
- Research progression.
- Ship hulls and modular ship design.
- Scout, science, colony, civilian, and military ship roles.
- Fleet organization and orders.
- Basic real-time combat.
- First functional AI empires using only their own knowledge.
- Save/load and autosave.

Exit criteria: a complete small-galaxy campaign can be won or lost without debug intervention.

## Phase 2 — Distinct civilizations and diplomacy

- Race/culture strategic identities that affect what the AI values rather than merely adding numeric bonuses.
- Separate player difficulty, race mastery difficulty, and AI decision quality.
- Aggressive civilizations become more tolerant when materially weaker unless a defining trait overrides that behavior.
- Honor-bound cultures with distinct concepts of battle, retreat, surrender, oaths, and honorable conduct.
- Trade-oriented, scientific, defensive, opportunistic, isolationist, diplomatic, and expansionist strategic profiles.
- Ruler and government modifiers that can change a civilization's behavior over time.
- Trust, fear, respect, rivalry, grievances, promises, and historical memory.
- No universal border-friction penalty.
- Claims and disputed strategic locations.
- Trade agreements, access agreements, alliances, guarantees, threats, and negotiated settlements.
- Information uncertainty: estimates can be incomplete, stale, or wrong for both player and AI.

## Phase 3 — Living civilizations

- Political change and succession.
- Government reform, revolution, fragmentation, and successor states.
- Colony cultural divergence.
- Migration and demographic change.
- Internal stability and regional loyalty.
- Long-lived historical records compressed from detailed simulation into durable strategic memories.
- Civilizations may change temperament because something meaningful happened rather than because the map simply filled up.

## Phase 4 — Pre-FTL civilizations and emerging powers

- Civilizations can begin at different technological stages.
- Pre-FTL societies develop on their own timeline.
- Observation, protection, fair trade, exploitation, interference, and technological assistance have persistent consequences.
- A protected or aided civilization can later become warp-capable and emerge as a major ally, rival, trading power, or threat.
- Technological assistance can accelerate development but may create political, cultural, economic, or military instability.
- New interstellar powers can emerge long after a campaign begins.

## Phase 5 — Subjects, vassalage, and asymmetric power

- Vassal, protectorate, tributary, client-state, and other subject relationships appropriate to different cultures.
- Becoming a subject remains playable rather than causing immediate game over.
- Tribute, fleet limits, access, diplomatic restrictions, military obligations, protection, and autonomy can be negotiated or imposed.
- Subjects can cooperate, resist, rebuild, seek independence, or benefit from a comparatively benevolent overlord.
- Overlords can control large regions indirectly without annexing every system.

## Phase 6 — Emergent crises

- Crises arise from simulation history rather than exclusively from a calendar trigger.
- A civilization can become dangerously dominant through trade, technology, conquest, ideology, political influence, resource control, or a combination.
- A power that was once weak or pre-FTL can become a galaxy-level threat generations later.
- Overextension, logistics, succession, rebellion, resource shortages, and coalition responses can also destroy apparently unstoppable powers.
- Rival civilizations decide independently whether to oppose, support, exploit, appease, or submit to a rising threat.
- Crisis resolution can include military defeat, political fragmentation, sanctions, negotiated settlement, subject revolt, or regime change.

## Phase 7 — Civilization ark megaproject

A late-game civilization can attempt an intergalactic exodus/expansion project.

- Megastructure-scale generation ark requiring a civilization-wide industrial commitment.
- Typical construction horizon measured in many decades (roughly 50–100+ years depending on industrial capability and circumstances).
- Carries a viable population, genetic/cultural archives, science, industry, agriculture, medical capability, supplies, and expedition infrastructure.
- Knowledge survives the journey, but manufacturing capability must be rebuilt after arrival.
- Intergalactic FTL is vastly faster than light yet still takes substantial in-game time across millions of light-years; the ark is designed for a generational voyage.
- First-generation intergalactic drive is effectively sacrificial: the crossing burns out the drive, preventing casual galaxy hopping.
- Ordinary intra-galaxy warp would require an effectively impossible timescale to reach another galaxy.
- Ark is extremely slow, cumbersome, and poorly maneuverable.
- Enormous hull, shielding, compartmentalization, redundancy, and repair capability allow it to absorb severe damage.
- Direct weapons are deliberately limited to basic industrial/defensive systems; it is not a capital warship.
- Optional defensive EWAR/support modules can disrupt targeting, missiles, sensors, and coordination without turning the ark into an offensive battleship.
- Limited onboard expedition foundry can produce scout, science, and colony vessels only.
- Ark must be anchored and immobile while constructing ships.
- Colony ships consume actual expedition population.

### Ark catastrophic failure and coercion

- Severe drive/power damage can cause a staged core-breach emergency.
- Automatic compartment sealing may save part of the ship at extreme population and infrastructure cost.
- Total failure can devastate nearby fleets, stations, colonies, infrastructure, and population through energy release and debris.
- A deliberate scuttle/core-breach protocol can be armed as a last-resort strategic act.
- Deliberate use has major diplomatic, historical, and strategic consequences and sacrifices an asset that required decades to build.
- Coercive diplomacy can use a genuinely armed or bluffed breach threat.
- A target civilization can evaluate only what its sensors, science, intelligence, history, and leadership allow it to know.
- Less advanced civilizations may be unable to verify the threat; advanced civilizations may detect actual core instability and call a bluff.

## Phase 8 — Intergalactic campaigns

- Arrival in another galaxy starts a materially weak but scientifically knowledgeable expedition civilization.
- Rebuild mining, refining, manufacturing, population, shipyards, and defense before advanced technology can be reproduced at scale.
- Reconstruct intergalactic capability only after civilization-scale infrastructure has been restored.
- Voluntary expansion and desperate escape use the same underlying mechanics.
- Eventually return to a lost home galaxy or continue farther outward.
- Long-term goal: multiple galaxies under one civilization's influence without requiring every distant entity to remain fully simulated.

### Departed-galaxy historical simulation

When the player leaves a galaxy:

1. Save its exact departure state.
2. Transition it to a compressed strategic simulation while the player is away.
3. Advance major outcomes from real departure conditions: technology, population, economy, fleets, logistics, alliances, rivalries, stability, leadership, expansion, overextension, and survival probabilities.
4. Preserve a historical timeline of major wars, collapses, territorial changes, emergent powers, and other defining events.
5. Reconstruct a detailed playable state when the player returns.

The dominant civilization that forced the player out may control most of the galaxy on return, or it may have overextended, fractured, collapsed, or been replaced by another power. Outcomes must be causally derived rather than arbitrary rerolls.

## Phase 9 — Hidden discovery framework

- Undocumented mysteries, artifacts, interactions, rare event chains, and exceptional technologies.
- Secrets interact with exploration, science, diplomacy, espionage, ideology, war, and incomplete information.
- Other civilizations may encounter unknown artifacts without automatically understanding their significance.
- Exact solutions, probabilities, hidden AI exceptions, and rare crisis triggers are intentionally excluded from the public roadmap.
- No required achievement should depend on an effectively unknowable ultra-rare secret.

## Phase 10 — Performance, diagnostics, and support maturity

Performance is a design requirement, not a final polishing task.

- Stress-test galaxies far larger and older than ordinary early-game saves.
- Track simulation milliseconds per subsystem separately from rendering FPS.
- Track memory growth, cache sizes, pathfinding queues, AI jobs, active entities, and allocation pressure.
- Use bounded and invalidated caches rather than caching forever or clearing everything blindly.
- Compress obsolete tactical history while preserving strategically meaningful historical memory.
- Use simulation levels so distant/inactive content receives cheaper updates.
- Cache shared paths and avoid redundant per-ship galaxy route calculations.
- Schedule strategic AI thinking instead of re-evaluating every goal every frame.
- Expose Normal, Detailed, and Developer/Trace log levels.
- Provide Open Logs Folder, Copy System Information, and Export Support Bundle flows.
- Support bundles may include logs, diagnostics, settings, and an optional save with explicit player consent.

## Early Access release discipline

The Early Access build must already be a game worth playing in its current state.

- Small but complete core loop first.
- Stable and opt-in experimental branches.
- Rapid hotfixes for reproduced player problems.
- Public known-issues/status communication.
- Major content arrives in meaningful updates rather than artificial daily feature churn.
- Daily development activity is desirable; unnecessary daily public builds are not required.
- Player diagnostics and saves are treated as first-class inputs for reproducing difficult bugs.
- Public updates should demonstrate that reports are being read, investigated, fixed, tested, and released quickly.

## Long-term rule

Do not add a feature merely because another space strategy game has it. Every major system must strengthen this game's own identity: fair information, distinct civilizations, consequential history, systemic storytelling, survival, exploration, and civilization-scale change.
