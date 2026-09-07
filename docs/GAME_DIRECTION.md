# Canonical Game Direction

This file records the high-level design principles that should survive chat changes and prevent design drift. It is intentionally more durable than a feature checklist.

## Core identity

The game is an original real-time space civilization / grand-strategy / 4X game about guiding a civilization through centuries or millennia of history.

The goal is not to clone Stellaris or Master of Orion. Those games are reference points for scale only. The project should favor believable cause-and-effect, emergent history, fair AI, long campaign continuity, and meaningful technological/cultural differences.

## Design Rule 1 — Realism first, arbitrary restrictions last

Do not add a rule merely because it is convenient for balance or because another strategy game uses it.

When the game needs to discourage an action, first ask:

> What would physically, economically, politically, logistically, culturally, or diplomatically make this difficult in a believable universe?

Prefer consequences over invisible prohibitions.

Examples:

- No magical claim token is required before conquering a system. A civilization can invade if it can physically do so, but unjustified conquest can create occupation costs, resistance, sanctions, fear, coalitions, diplomatic isolation, internal opposition, refugee flows, logistics burdens, and long-term historical consequences.
- Borders are political claims, not force fields. A civilization can warn another not to enter. The other side can ignore the warning, but doing so creates an incident and may lead to interception, expulsion, sanctions, hostility, or war.
- Closed borders do not physically stop ships.
- Treaties grant permissions such as transit, supply, basing, or civilian access; violating them is possible and consequential.
- Diplomacy should rarely disable a request just because an opinion threshold was not met. The player can make an unreasonable demand; the other civilization decides how to react.
- Knowledge of a technology does not guarantee the industrial ability to manufacture it.

The universe should usually say "you can try, and here are the risks" rather than "the button is disabled."

## Design Rule 2 — Power does not remove challenge; it changes the challenge

A powerful empire should feel powerful. Do not punish success with arbitrary empire-size penalties or hidden enemy stat inflation.

Late-game difficulty should arise naturally from scale and consequences:

- long borders
- distant logistics
- occupation requirements
- regional political identities
- subject relationships
- coalition formation
- internal factions
- succession/government change
- infrastructure vulnerability
- trade dependence
- overextension
- rebellion/separatism
- rivals reacting rationally to a hegemon

A dominant civilization may knowingly accept huge consequences because it believes it is strong enough to survive them. The game should allow that gamble.

## Design Rule 3 — The galaxy never becomes permanently static

The late game must remain interesting after ordinary expansion slows.

Civilizations continue to change:

- governments reform or collapse
- cultures evolve
- allies drift apart
- enemies reconcile
- states fracture, merge, and create successors
- minor/pre-warp societies can rise into major powers
- great powers can decline
- populations migrate
- technology changes strategic constraints
- historical events alter future politics

Political defeat is not automatically game over. Vassalage, fragmentation, revolution, territorial loss, exile, or rebuilding can become new chapters of the same campaign.

## Design Rule 4 — Relationships and knowledge decay

Diplomacy is not a permanent opinion number.

Separate:

- active relationship
- institutional/historical record
- cultural memory
- legend/rumor

Without continued interaction, current trust/hostility and intelligence become stale. Centuries later, descendants may know only incomplete stories or rumors about a former ally or enemy.

Important events can persist much longer than routine interaction. The persistence of memory should depend on factors such as:

- severity/importance of the event
- species lifespan
- archival technology
- government continuity
- culture and oral/written traditions
- deliberate censorship or propaganda
- destruction of archives
- continued contact

A civilization may eventually encounter an ancient former ally almost like a second first contact.

## Design Rule 5 — The player’s operational scale expands with civilization capability

The game should not begin in 2050 by giving the player a fully operational galactic map.

There is a difference between astronomical knowledge and operational reach.

A civilization may know that a distant star exists while being unable to inspect it with perfect information, issue orders there, or operate there.

The player-facing scale should grow approximately like:

1. Homeworld / local planetary civilization
2. Orbital and cislunar space
3. Home solar system
4. Nearby interstellar space
5. Galactic civilization
6. Multi-galaxy civilization

Older layers remain accessible, but mature routine work becomes increasingly automatable/delegable.

## 2050 opening direction

The campaign begins on **January 1, 2050**.

For a human-like civilization, the current working start is:

- mature homeworld civilization
- significant orbital infrastructure
- permanent lunar base/early settlement
- young Mars colony
- Mars still meaningfully dependent on homeworld logistics
- no FTL capability yet

The game should not make the player rediscover basic rocketry that humanity already possesses. Instead, the pre-warp game is about turning an early multi-world civilization into a self-sustaining solar civilization capable of supporting interstellar missions.

Other playable species should begin at a comparable broad era, not with identical human infrastructure. Their home systems, biology, environment, and technological history can create very different starting situations.

## Pre-warp gameplay must be entertaining on its own

The pre-warp phase is not a tutorial waiting room.

Potential meaningful systems include:

- homeworld development
- orbital infrastructure
- Moon/moon bases and settlements
- Mars/planetary settlements where appropriate
- asteroid mining
- fuel depots
- outpost ships
- orbital construction
- solar-system science missions
- life support
- food independence
- radiation protection
- gravity management
- supply/endurance
- communication delay
- transfer windows/orbital logistics
- automation/administration
- internal politics and competing priorities
- robotic and crewed exploration

Exact implementation should remain playable and avoid pointless detail.

## FTL should not be the only gate to interstellar expansion

Researching a prototype FTL drive should not instantly make the entire galaxy practically reachable.

Early interstellar capability depends on multiple constraints:

- drive speed/range/reliability
- fuel or energy requirements
- life support
- food production
- maintenance
- radiation protection
- gravity management
- manufacturing/replication
- navigation
- communications
- orbital shipyard capability
- supply network
- nearby colonies/outposts/depots

Early fleets should have operational endurance and may require resupply. Range should emerge from technology + ship design + logistics rather than from a single arbitrary empire-range circle.

A player may be allowed to attempt missions beyond safe logistical limits and accept the consequences.

## Automation should rise with civilization scale

As tasks become routine, the player should be able to delegate them.

Approximate progression:

- Pre-warp: direct control of major home-system decisions.
- Early interstellar: automate mature home-system tasks and focus on first extrasolar expansion.
- Interstellar power: delegate routine colony development; manage fleets, diplomacy, research, strategic infrastructure, and major projects.
- Galactic empire: manage sectors, theaters, governors, policies, trade/logistics networks, and major political problems rather than individual mines.
- Intergalactic civilization: entire regions/galaxies may require high-level autonomous administration.

Automation should be configurable and overridable, not an AI that takes control away from the player.

## Species and technology are not universal

Do not build a single universal technology tree and recolor it for every species.

Use the concept of **capabilities** versus **technological implementations**.

Different civilizations may achieve similar capabilities through fundamentally different technology.

Examples:

- human-style mechanical/industrial engineering
- biological fabrication and living ships
- machine/synthetic infrastructure
- high-pressure aquatic technology
- high- or low-gravity engineering
- unusual-solvent life support
- alternative FTL architectures

Some civilizations may never independently discover FTL. Contact, trade, reverse engineering, hybrid research, or rare discoveries may alter their development path.

Foreign technology can be:

- directly compatible
- adaptable
- conceptually useful only
- dependent on alien infrastructure/materials/personnel
- biologically incompatible
- beyond current understanding
- dangerous to study/use
- useless to the holder but extremely valuable to another civilization

Conquest must not instantly grant universal technology unlocks. Technology can exist in people, institutions, manufacturing methods, biology, materials, and tacit knowledge as well as in blueprints.

Technology therefore becomes a major diplomatic/economic commodity through trade, licensing, research partnerships, espionage, capture, and brokerage.

## Biology direction

Most naturally evolved intelligent life should probably be carbon-based, with carbon/water life common.

Rarer forms can include:

- carbon life using substantially different solvents/environments
- cryogenic/hydrocarbon-environment life
- silicon-centered or other highly speculative biochemical lineages
- synthetic/post-biological civilizations

Exotic life should feel rare and mechanically meaningful rather than becoming a cosmetic gimmick.

Habitability is species-relative. A world that is ideal for one species may be useless or lethal to another.

## Starting species count — current planning target

This is a working target, not yet a locked production commitment:

- Demo: roughly 3–4 deeply differentiated playable species may be enough.
- Early Access: roughly 6 polished playable species.
- 1.0 target: around 12 deeply differentiated playable major species if quality can be maintained.
- Additional procedural/minor/pre-warp species can create variety without requiring every one to have the same depth as a player start.

Prefer fewer deep species over many shallow species with +10% bonuses.

## Territory, conquest, and sovereignty

- Territory is a political/legal concept, not an invisible physical barrier.
- Claims can exist as historical/legal/cultural assertions and influence legitimacy, resistance, and diplomacy, but they are not permission tokens required to invade.
- Occupation and control must be modeled separately from formal recognition/ownership when appropriate.
- The challenge of conquest should come from military reality, logistics, occupation, population response, diplomacy, economics, legitimacy, and long-term history.
- A civilization can ignore another's border warning; the other side reacts according to its interests, personality, strength, history, and the nature of the incursion.

## Fair AI remains mandatory

AI may be smart, cautious, aggressive, deceptive, or culturally unusual, but it must act from legitimate information.

No routine omniscient access to hidden fleets, exact unseen economy, unknown technologies, or secret map state.

Harder AI should primarily be better decision-making, planning, coordination, and economy—not massive invisible cheats.

## Hidden discoveries

Rare undocumented discoveries remain part of the game identity.

Public repository documents must not enumerate exact hidden triggers, probabilities, complete secret chains, or rare secret AI eligibility rules. Keep the public framework broad enough that players can genuinely discover and debate secrets.

## Expansion philosophy

The base game must be complete and enjoyable. Do not intentionally cripple systems so they can be sold back later.

Expansions should deepen existing simulation systems. Deep alien-technology compatibility, technology brokerage, licensing, hybrid research, and scientific divergence are strong candidates for a later expansion, but the base architecture must support technological divergence from the beginning.
