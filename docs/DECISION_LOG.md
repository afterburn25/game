# Design Decision Log

This file records durable project decisions that should not be casually reversed in a later chat. New entries should be appended when the user explicitly changes direction or locks a new design principle.

This is a public repository. Do not record exact secret-discovery probabilities, hidden artifact triggers, rare secret AI eligibility, or other intentionally undisclosed mechanics here.

## 2026-09-07 — Engine and simulation architecture

**Decision:** Continue with Godot 4.7.2 .NET + C# and a custom data-oriented simulation core.

**Reasoning:** The game is simulation/UI heavy rather than primarily a high-end 3D action game. Godot handles presentation while plain C# state/services keep the galaxy scalable, testable, serializable, and independent of scene-tree overhead.

**Guardrail:** Do not create one Godot Node per simulation entity.

## 2026-09-07 — Real-time strategy clock

**Decision:** Use continuous real-time simulation with pause and modest speed levels rather than extreme 16x/32x speeds.

**Guardrail:** If hardware cannot sustain requested speed, lower effective simulation speed rather than allowing unbounded backlog/memory growth.

## 2026-09-07 — Fair AI

**Decision:** AI uses only information it could legitimately possess.

**Allowed sources:** sensors, scouts, intelligence, trade/treaty information, memory, estimates, known history.

**Forbidden routine behavior:** querying exact unseen fleets, hidden colony state, unknown technology, or authoritative map state behind fog of war.

**Difficulty direction:** harder AI should primarily mean better decisions/planning/coordination rather than massive invisible cheats.

## 2026-09-07 — Borders do not automatically create hostility

**Decision:** Shared borders do not create universal automatic hatred.

Border reactions depend on culture, personality, government, history, strategic interests, treaties, claims, resources, perceived threat, and incidents.

Some civilizations should value neighboring trade; others may tolerate shared zones; some may be highly territorial.

## 2026-09-07 — Pre-warp civilizations are meaningful actors

**Decision:** Pre-warp societies are not free territory or decorative primitives.

They can develop, remember treatment, become allies/traders/rivals, receive or reject help, be exploited, protected, manipulated, conquered, or eventually become major powers.

Their history with advanced civilizations persists and affects later behavior.

## 2026-09-07 — Political defeat is not automatic game over

**Decision:** Vassalage, protectorate status, territorial loss, revolution, fragmentation, or loss of great-power status can become new campaign chapters rather than immediate defeat screens.

A subject civilization can rebuild, negotiate, cooperate with other subjects, rebel, or break free.

## 2026-09-07 — Intergalactic continuation

**Decision:** Very late game can extend beyond one galaxy.

A civilization may construct a colossal generation ark / intergalactic project, leave a galaxy, rebuild elsewhere, and eventually return.

Scientific knowledge can survive an exodus while industrial capacity must be rebuilt from actual resources/infrastructure.

Departed galaxies evolve through compressed causal strategic simulation rather than freezing.

## 2026-09-07 — Hidden discoveries remain undocumented

**Decision:** Rare discoveries/artifacts/secret chains should not be comprehensively listed in public feature documentation or guides.

Players should discover, debate, and sometimes doubt them.

Public docs may describe only the framework, not exact triggers/probabilities/rare hidden outcomes.

## 2026-09-07 — 2050 campaign start

**Decision:** New campaigns begin on January 1, 2050.

This is a calendar anchor, not a claim that every species shares human history.

Each playable species begins at an era broadly comparable to an early spacefaring/pre-FTL civilization appropriate to its own biology, home system, and technological history.

## 2026-09-07 — Remote old powers as early buffer

**Decision:** A small number of already-spacefaring seeded civilizations can exist at game start as remote old powers.

They are initially non-expansionist and neutral unless provoked, preventing young civilizations from immediately being crushed or boxed in by mature expansionist empires.

They remain hidden until legitimate discovery/contact.

## 2026-09-07 — Realism-first rule

**Decision:** Prefer believable constraints/consequences over arbitrary game restrictions.

Before adding a rule, ask what would realistically prevent, discourage, or complicate the action.

Examples:

- no abstract claim token required before conquest
- no invisible wall that physically enforces closed borders
- no arbitrary opinion threshold required merely to ask for a diplomatic deal
- no universal empire-size penalty if logistics/administration/politics can create the challenge naturally

The game should usually allow the action and model the consequences.

## 2026-09-07 — Conquest and claims

**Decision:** Claims can exist as historical/legal/cultural assertions but do not function as permission tokens required to conquer a system.

A civilization that can physically invade can do so.

Difficulty/consequences come from:

- military resistance
- logistics
- occupation
- local population response
- legitimacy
- diplomacy
- sanctions
- coalitions
- insurgency
- economic damage
- internal politics
- long-term historical memory

A powerful empire may knowingly accept these consequences because it believes it can survive them.

## 2026-09-07 — Borders are warnings, not force fields

**Decision:** Civilizations cannot physically switch off territory to outsiders.

They can declare access rules, issue warnings, deny legal permission, intercept intruders, escort them out, sanction them, fire on them, or escalate to war.

An outsider can ignore the warning and accept the consequences.

Ship type, route, depth of incursion, prior behavior, relations, relative strength, and culture all affect the response.

## 2026-09-07 — Late game must remain a living game

**Decision:** Late-game challenge should come from increased civilizational complexity and history rather than merely larger enemy numbers/stat bonuses.

The galaxy should continue changing after ordinary expansion slows.

Relevant systems include:

- government/cultural change
- fragmentation/successors
- migration
- regional identities
- alliances drifting
- rivals reconciling
- subjects and rebellions
- logistics and infrastructure vulnerability
- new rising civilizations
- decline and recovery
- multi-galaxy expansion

Early game asks whether the civilization can reach/survive the stars. Late game asks whether it can manage what it has become.

## 2026-09-07 — Design every feature for long-campaign growth

**Decision:** Scalability must be considered early.

Every feature should have a strategy for:

- bounded memory
- bounded caches/queues
- cleanup/expiry
- reduced-detail simulation when appropriate
- save size
- late-game CPU cost
- graceful slowdown rather than crashes

Old low-level history should compress into meaningful consequences instead of accumulating forever.

## 2026-09-07 — Relationships, intelligence, and history fade

**Decision:** Relationships are not permanent opinion numbers.

Without continued interaction:

- current trust/hostility can decay
- intelligence becomes stale
- institutional records may survive
- cultural memory can distort events
- distant history may become rumor/legend

Very important events may persist for centuries or millennia depending on species lifespan, archives, culture, government continuity, censorship, and historical trauma/significance.

A civilization may eventually re-encounter an ancient former ally with only uncertain stories remaining.

## 2026-09-07 — Player-facing scale grows with capability

**Decision:** The operational game map expands as civilization reach expands.

Astronomical knowledge does not equal actionable map access/perfect survey information.

Approximate scale progression:

- homeworld/local planetary layer
- orbital/cislunar layer
- solar-system layer
- nearby interstellar layer
- galactic layer
- multi-galaxy layer

Mature lower-level systems can later be automated/delegated rather than disappearing.

## 2026-09-07 — Revised human-like 2050 start

**Decision:** A human-like civilization in 2050 should not begin as if it has never meaningfully entered space.

Working baseline:

- mature homeworld
- substantial orbital infrastructure
- permanent lunar base/early settlement
- young Mars colony
- Mars still materially dependent on homeworld supply/industry

The pre-FTL challenge is turning this early multi-world civilization into a self-sustaining solar civilization capable of interstellar operations.

The exact starting infrastructure for alien species can be completely different.

## 2026-09-07 — Realistic in-system travel matters

**Decision:** Pre-warp travel should model meaningful travel time, orbital logistics, and transfer constraints without becoming tedious orbital-mechanics software.

Working human-like assumptions discussed:

- Earth–Moon travel can vary by vehicle/trajectory; modern/future fast crew missions are on the order of days, with cargo potentially slower.
- Earth–Mars nominal future crew transfer can be around six months under suitable conditions.
- Mars transfer windows/orbital geometry can matter.

These are design directions subject to later scientific/balance validation, not hard-coded final numbers yet.

## 2026-09-07 — Interstellar range is logistical, not only propulsion-based

**Decision:** Prototype FTL should have limited practical reach, and a better drive is not the only expansion requirement.

Operational range may depend on:

- ship supply endurance
- food/water/life support
- maintenance/spare parts
- radiation protection
- gravity management
- fuel/energy
- fabrication/replicators
- navigation
- communications
- nearby colonies/outposts/depots

Early ships may require periodic resupply and stay near the support network unless the player accepts a dangerous mission beyond safe endurance.

## 2026-09-07 — Gravity management is part of deep-space maturity

**Decision:** Long-duration habitation should account for gravity/health.

Do not assume fictional gravity generation is required if realistic solutions exist.

Early solutions can include rotation, acceleration profiles, exercise, and medical countermeasures. Genuine artificial gravity, if it exists in the game's physics, can be a later breakthrough with major design consequences.

## 2026-09-07 — Automation increases as scale increases

**Decision:** Once older tasks become routine, the player can delegate them.

A warp-capable civilization should not still require constant manual management of every lunar mine or mature home-system facility.

Automation should be configurable, policy-driven, and overridable.

The player's focus moves from projects → systems → sectors → theaters/regions → galaxy/intergalactic strategy as civilization scale grows.

## 2026-09-07 — No universal species technology tree

**Decision:** Similar strategic capabilities do not imply identical technology paths.

Architecture should distinguish a capability from the technology that implements it.

Biology, environment, resources, culture, historical accidents, and discoveries can create radically different lineages.

Some civilizations may never independently discover FTL.

Contact, trade, capture, espionage, hybrid research, or unusual discoveries can open paths absent from a native tree.

## 2026-09-07 — Foreign technology is not automatically usable

**Decision:** Captured/traded alien technology may be compatible, adaptable, conceptually useful, infrastructure-dependent, biologically incompatible, incomprehensible, dangerous, or useless to the current holder.

Understanding what something does is not the same as being able to manufacture or safely operate it.

Foreign knowledge may depend on:

- alien scientists/technicians
- unique materials
- biological processes
- specialized factories
- environmental conditions
- mathematics/physics beyond current understanding

Even unusable technology can be valuable to another civilization, creating technology trade/brokerage opportunities.

## 2026-09-07 — Technology as a strategic commodity

**Decision:** Technology can become an important trade/diplomatic asset rather than a universal research-point abstraction.

Future systems may include:

- blueprint exchange
- manufacturing rights
- limited licensing
- civilian/military restrictions
- joint research
- embargoes
- espionage/theft
- brokerage/resale
- technological monopolies

A deep version of this system is a strong later-expansion candidate, but the base architecture should not make it impossible.

## 2026-09-07 — Biological diversity

**Decision:** Most naturally evolved intelligent life is expected to be carbon-based, with carbon/water life common, but the universe should permit rarer fundamentally different lineages.

Potential rarer categories include:

- unusual-solvent carbon life
- cryogenic/hydrocarbon-environment life
- silicon-centered speculative life
- synthetic/post-biological civilizations

Exotic life should be rare enough to feel meaningful.

Habitability and biological technology compatibility are species-relative.

## 2026-09-07 — Playable species count direction

**Decision status:** planning target, not final lock.

Prefer a smaller number of deeply differentiated playable species over dozens of shallow +percentage variants.

Working scope discussed:

- first demo: ~3–4 playable species can be sufficient
- Early Access: ~6 polished playable species
- 1.0 aspiration: ~12 deeply differentiated playable major species if quality supports it

Procedural/minor/pre-warp species can add variety beyond the major playable starts.

## How to change a locked decision

A later chat must not silently reinterpret one of these decisions.

If the user explicitly changes a decision:

1. follow the new instruction
2. append a new dated entry explaining that it supersedes the earlier one
3. update `GAME_DIRECTION.md` and `PROJECT_STATE.md` if affected
4. update `ROADMAP.md` when milestone planning changes

Do not delete the old historical entry; preserve why the direction changed.
