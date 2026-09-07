# Design Decision Log

This file records durable project decisions that should not be casually reversed in later chats. New decisions that supersede older ones must say so explicitly. This repository is public: never record exact secret-discovery probabilities, hidden artifact triggers, rare secret-AI eligibility, or intentionally undisclosed technology chains here.

## 2026-09-07 — Engine and simulation architecture

**Decision:** Godot 4.7.2 .NET + C# with a custom data-oriented plain-C# simulation core. Godot handles presentation/input/audio/platform integration.

**Guardrail:** Do not create one Godot Node per simulation entity.

## 2026-09-07 — Real-time strategy clock

**Decision:** Continuous real-time simulation with pause and modest speed levels.

**Guardrail:** If hardware cannot sustain requested speed, reduce effective simulation speed instead of accumulating unbounded backlog/memory.

## 2026-09-07 — Fair AI

**Decision:** AI acts only from legitimate knowledge: sensors, scouts, intelligence, trade/treaty information, memory, estimates, and known history.

**Guardrail:** No routine hidden access to exact unseen fleets, colonies, economy, technology, or authoritative fogged map state. Harder AI should primarily mean better decisions/planning, not huge cheats.

## 2026-09-07 — Borders do not automatically create hostility

**Decision:** Shared borders are context, not an automatic opinion penalty. Reactions depend on culture, government, history, interests, treaties, claims, resources, incidents, and perceived threat.

## 2026-09-07 — Pre-warp civilizations are meaningful actors

**Decision:** Pre-warp societies are not free territory. They can develop, remember treatment, trade, be protected/exploited/manipulated/conquered, and later become allies, rivals, or major powers.

## 2026-09-07 — Political defeat is not automatic game over

**Decision:** Vassalage, territorial loss, revolution, fragmentation, protectorate status, or loss of great-power status can become new campaign chapters.

## 2026-09-07 — Intergalactic continuation

**Decision:** Very late game can extend beyond one galaxy. Intergalactic ark/exodus projects preserve scientific knowledge while industrial capability must be rebuilt. Departed galaxies continue through compressed causal strategic simulation.

## 2026-09-07 — Hidden discoveries remain undocumented

**Decision:** Rare artifacts/discoveries/secret chains are not comprehensively listed in public docs or data. Players should genuinely discover and debate them.

## 2026-09-07 — 2050 campaign start

**Decision:** New campaigns begin January 1, 2050. This is a calendar anchor, not a claim every species shares human history.

## 2026-09-07 — Remote old powers as early buffer

**Decision:** A small number of already-spacefaring remote old powers may exist at start. They are initially non-expansionist and neutral unless provoked and remain hidden until legitimately discovered.

## 2026-09-07 — Realism-first rule

**Decision:** Prefer believable physical, logistical, economic, political, cultural, and diplomatic constraints/consequences over arbitrary game restrictions.

Examples: no abstract claim token required before conquest, no closed-border force fields, no arbitrary opinion threshold just to make a diplomatic request, no generic empire-size punishment when real logistics/administration/politics can create the challenge.

## 2026-09-07 — Conquest and claims

**Decision:** Claims are historical/legal/cultural assertions affecting legitimacy, resistance, diplomacy, and negotiation; they are not permission tokens required to invade.

Conquest difficulty comes from actual resistance, logistics, occupation, population response, legitimacy, sanctions, coalitions, insurgency, economic damage, internal politics, and history.

## 2026-09-07 — Borders are warnings, not force fields

**Decision:** Civilizations declare access rules and can warn, intercept, escort, sanction, fire, or escalate. Intruders can still violate the rule and accept consequences.

## 2026-09-07 — Late game remains a living game

**Decision:** Late-game challenge comes from history and scale rather than only inflated enemy stats: government/cultural change, fragmentation, migration, regional identity, shifting alliances, subjects, rebellions, logistics, infrastructure vulnerability, rising powers, decline/recovery, and multi-galaxy growth.

## 2026-09-07 — Design every feature for long-campaign growth

**Decision:** Every long-lived system needs bounded memory/caches/queues, cleanup/expiry, reduced-detail simulation where appropriate, save-size strategy, late-game CPU strategy, and graceful slowdown.

Old low-level history compresses into meaningful consequences rather than accumulating forever.

## 2026-09-07 — Relationships, intelligence, and history fade

**Decision:** Relationships are not permanent opinion numbers. Without interaction, trust/hostility can fade, intelligence becomes stale, records persist imperfectly, cultural memory can distort events, and distant history may become rumor/legend. Important events can persist much longer depending on lifespan, archives, culture, continuity, censorship, and significance.

## 2026-09-07 — Player-facing scale grows with capability

**Decision:** Operational scale expands approximately homeworld/local -> orbital/cislunar -> solar system -> nearby interstellar -> galactic -> multi-galaxy. Astronomical knowledge is not the same as actionable surveyed access. Mature lower layers become delegable rather than disappearing.

## 2026-09-07 — Revised human-like 2050 start

**Decision:** A human-like civilization begins with a mature homeworld, substantial orbital infrastructure, permanent lunar presence, and young Mars colony still materially dependent on homeworld supply/industry. Alien starts can be completely different.

## 2026-09-07 — Realistic in-system travel matters

**Decision:** Pre-warp travel models meaningful travel time, orbital logistics, and transfer constraints without becoming orbital-mechanics software. Human-like working assumptions include days for Earth-Moon crew transit and roughly months for suitable Earth-Mars transfers; orbital geometry/windows matter.

## 2026-09-07 — Interstellar range is logistical, not only propulsion-based

**Decision:** Prototype FTL has limited practical reach. Operational range depends on supply endurance, food/water/life support, maintenance, radiation protection, gravity management, fuel/energy, fabrication, navigation, communications, and support nodes. Players may attempt dangerous missions beyond safe endurance.

## 2026-09-07 — Gravity management is part of deep-space maturity

**Decision:** Long-duration habitation accounts for gravity/health. Use realistic solutions such as rotation, acceleration profiles, exercise, and medicine before assuming fictional gravity generation.

## 2026-09-07 — Automation increases with scale

**Decision:** Routine mature systems become configurable/delegable as civilization scale grows. Player focus shifts from projects -> systems -> sectors -> theaters/regions -> galaxy/intergalactic strategy.

## 2026-09-07 — No universal species technology tree

**Decision:** Similar capabilities do not imply identical technologies. Biology, environment, resources, culture, historical accidents, needs, and discoveries create divergent technological histories. Some civilizations may never independently discover FTL.

## 2026-09-07 — Foreign technology is not automatically usable

**Decision:** Foreign technology can be directly compatible, adaptable, conceptually useful, infrastructure-dependent, biologically incompatible, incomprehensible, dangerous, or unusable to the holder. Knowledge can depend on alien personnel, materials, biology, factories, environmental conditions, or unfamiliar science.

## 2026-09-07 — Technology as a strategic commodity

**Decision:** Technology can be traded/licensed/stolen/brokered rather than being only generic research points. Future depth can include blueprints, manufacturing rights, civilian/military restrictions, joint research, embargoes, resale, and monopolies.

## 2026-09-07 — Biological diversity

**Decision:** Carbon/water life is expected to be common, with rarer unusual-solvent carbon life, silicon-centered speculative life, and synthetic/post-biological civilizations. Habitability and biological-technology compatibility are species-relative.

## 2026-09-07 — Playable species count direction

**Planning target:** roughly 3–4 deep starts for a first demo, ~6 polished species for Early Access, and around 12 deeply differentiated major playable species for 1.0 if quality supports it.

## 2026-09-07 — Working title: Stellar Continuum

**Decision:** Canonical working title is **Stellar Continuum**. It remains pending commercial trademark/domain/social clearance. See `BRANDING.md`.

## 2026-09-07 — Planet gravity is a persistent environmental variable

**Decision:** Surface gravity comes from mass/radius and can affect health, infrastructure, launch cost, transport, migration, and ground combat. Population response can progress through acclimatization, developmental adaptation, long-term natural evolution, and medical/genetic/cybernetic intervention.

**Guardrail:** Do not reduce this to a permanent flat “high-gravity race +X% combat” modifier.

## 2026-09-07 — Adaptive Research replaces fixed visible species trees

**Decision:** Stellar Continuum uses a broad hidden **Technology Possibility Graph**. Each civilization materializes only currently known/plausible/relevant branches. The player never sees the complete future graph.

Branches can emerge from current need, basic science, experiments/anomalies, environment, resources, warfare, foreign contact, captured devices, and legitimate evidence. Capabilities remain separate from implementations.

**Guardrail:** Do not regress to one fully visible fixed universal tree or giant separately maintained fixed tree per species.

Canonical detail: `ADAPTIVE_RESEARCH_SYSTEM.md` and `data/research/v1/`.

## 2026-09-07 — Research economy is RP + Pressure + Labs

**Decision:** Research uses three separate quantities:

1. **Research Points (RP)** — applied scientific work generated by Effective Research Labs.
2. **Research Pressure** — bounded contextual need/evidence; it is not spent and does not directly produce RP.
3. **Effective Research Labs** — physical scientific capacity; every directed project requires a minimum assignable amount.

Large universal RP stockpiles should be avoided. More labs can accelerate a project with diminishing returns.

**Pressure rule:** only explicitly configured technologies are hard-gated by Research Pressure. Complexity and pressure affinities alone do not create a gate; curiosity-driven/basic science remains possible.

Canonical detail: `RESEARCH_ECONOMY.md`, `RESEARCH_CAPACITY_MODEL.md`, and machine-readable data under `data/research/v1/`.

## 2026-09-07 — Directed research starts simple and gains parallelism

**Decision:** This **supersedes the earlier wording that implied lab capacity alone allowed multiple player-directed projects immediately.**

Early civilizations formally direct **one major strategic research program** while unassigned laboratories continue diffuse/basic science.

Parallel directed research is unlocked by actual Adaptive Research nodes:

- `coordinated_research_networks` -> 2 directed programs
- `distributed_scientific_portfolios` -> up to 4
- `autonomous_research_portfolios` -> no artificial slot ceiling; available lab capacity becomes the practical limit

Parallelism therefore requires both institutional coordination and enough physical laboratory capacity.

## 2026-09-07 — Research Pressure creates contextual urgency, not rubber-banding

**Decision:** Research Pressure arises from actual conditions and can create natural technological convergence without hidden catch-up bonuses.

A dominant navy with no credible rival may become less urgent/complacent. A weaker navy suffering losses and observing superior systems can gain strong pressure and useful evidence. When the leader recognizes credible catch-up, its own urgency can rise again.

Culture/government/innovation values affect responses. A technological leader may remain ahead indefinitely if it continues investing effectively.

**Guardrail:** No hidden “behind = +research%” or “ahead = -research%” rule.

## 2026-09-07 — Early Access campaign-duration direction

**Decision:** Initial paid Early Access should target roughly **500 in-game years of officially supported meaningful simulation/content** without a hard year-based game-over. Engineering soak tests should survive at least **1,000 simulated years** without unbounded memory/save/performance failure. Later releases extend content depth further.

## 2026-09-07 — Tiered persistence instead of keeping the universe hot

**Decision:** Long campaigns should separate hot active RAM state, bounded warm summaries/caches, and cold/dormant/historical state in a proven embedded persistent datastore behind a game-owned storage abstraction. Do not build a bespoke database engine unless profiling/requirements later justify it.

## 2026-09-07 — Public Adaptive Research seed expansion

**Decision:** The public normal-research seed is designed as a maintainable multi-domain possibility catalog rather than a single monolithic tree.

Current expanded design target:

- **330 possibility nodes**
- **20 domains**
- **59 Research Pressure types**
- **15 alternative-solution sets**

Newest domains include Agriculture & Biosphere Engineering, Economic & Trade Systems, Cybernetics & Augmentation, Scientific Infrastructure & Metrology, and Megastructure & Stellar Engineering.

The catalog is static shared data; civilizations persist only their small materialized research state. Catalog changes must pass machine validation for IDs, prerequisites, pressure references, counts, solution sets, capacity references, and dependency cycles.

## 2026-09-07 — Adaptive Research emergence is event/evidence driven

**Decision:** The possibility graph is not scanned every simulation tick. Conditions/evidence update sparse Research Pressure and evidence state; pressure-band crossings, new evidence, prerequisite maturity, applicability changes, and bounded basic-science reviews wake only indexed candidates.

Applicability uses biological/civilizational capability traits rather than named race IDs. Evidence has provenance/quality/confidence and never directly grants mature technology. Enemy-relative pressure must come from legitimate observed information.

## 2026-09-07 — Functional capabilities replace hidden implementation lock-in

**Decision:** A later technology/system must distinguish **specific knowledge lineage** from **generic functional capability**.

Use an implementation-specific technology prerequisite only when the later idea genuinely depends on that implementation's knowledge. If the requirement is merely functional, use a cross-lineage capability.

Examples:

- Stable Warp can require Prototype Warp because they are the same propulsion lineage.
- Interstellar Logistics must require reliable `interstellar_transit`, not Stable Warp Drive specifically.
- Prototype Warp can require `spacecraft_construction`, not one exact shipyard technology lineage.
- Wormhole Stabilization can require `megastructure_construction`, allowing different industrial lineages to satisfy the engineering requirement.

Capabilities are scoped to civilization, compatible population/species, or colony/installation as appropriate. A synthetic population's habitation capability does not automatically make biological citizens compatible.

**Guardrail:** Do not silently force all civilizations back onto the human/default technological path through generic late-game prerequisites.

Canonical detail: `RESEARCH_MATURATION_MODEL.md`, `capability_model.json`, and `capability_grants.json`.

## 2026-09-07 — Research maturation can fail without becoming punitive roulette

**Decision:** Research progresses through the existing states Unknown -> Rumored -> Hypothesized -> Investigable -> Experimental -> Demonstrated -> Engineering -> Mature/Archived.

Ordinary established engineering can experience setbacks or partial success but cannot randomly become physically impossible. True hypothesis nodes can be supported, refined, or disproven.

A disproven hypothesis becomes **Archived with resolution `disproven`**. The civilization keeps negative knowledge, field competence, and possible side discoveries; all RP is not magically erased and the same exact failed hypothesis should not immediately reappear.

Explicit hazardous research can create incidents, but hazard risk must be intentionally attached to the research profile rather than automatically applied to all advanced technologies.

Side discoveries can create evidence, hypotheses, field competence, or reduced uncertainty on legitimately related possibilities. They never directly grant an unrelated mature technology or bypass applicability/prerequisite rules.

**Guardrail:** No universal “research roll failed, lose everything” mechanic.

## 2026-09-07 — Persistent Adaptive Research workstream ownership

**Decision:** The dedicated Adaptive Research chat owns persistent branch **`dev/adaptive-research`** for technology/research development.

Other concurrent branches may read and consume research interfaces/capabilities but should not independently edit the canonical research graph/schema files while this workstream is active without coordination. See `WORKSTREAMS.md`.

Research milestone PRs merge this persistent branch to `main` after research validators plus normal .NET/Godot gates pass; the same branch is then advanced from the new `main` for continued research work.

## How to change a locked decision

If the user explicitly changes a decision:

1. follow the new instruction
2. append a new dated entry explaining what it supersedes
3. update `GAME_DIRECTION.md` / relevant canonical spec / `PROJECT_STATE.md`
4. update `ROADMAP.md` when milestone planning changes

Do not silently resurrect superseded rules.
