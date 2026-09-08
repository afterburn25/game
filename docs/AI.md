# AI Design Contract

## No privileged information

The AI must never receive information unavailable under the same fog-of-war and intelligence rules that govern the player. Difficulty comes from decision quality, planning, coordination, culture, and risk evaluation—not omniscience.

## Circumstance changes behavior

A civilization's culture creates tendencies, not a script. A militaristic civilization may bully weaker neighbors when powerful and negotiate carefully when weak. Scarcity creates pressure; it does not automatically cause border hatred or war.

## Survival default

Civilization survival is normally the strongest strategic priority. Aggression, greed, territoriality, trade dependence, trust, war exhaustion, logistics, allies, uncertainty, and perceived strength all modify decisions.

Explicit cultural traits can override normal priorities. An honor-bound society may accept otherwise irrational battle risk when its beliefs define retreat or broken oaths as worse than death.

## Same facts, different conclusions

Two civilizations can receive the same imperfect intelligence and make different choices because of culture, leadership, risk tolerance, history, and objectives.

## Information can be wrong

Observed strength decays in reliability over time. Intelligence can be incomplete. AI should sometimes make understandable mistakes because its information is stale or uncertain.

## Active early-release strategic runtime

The early-release runtime performs scheduled strategic reviews for ordinary non-player civilizations and feeds the resulting intent into Core's shared Industry priority provider. Core remains authoritative for construction/shipbuilding demand, fair allocation, resource spending, simulation ordering, and costs.

The current runtime review is intentionally **own-state only**. It can react to authoritative information about its own:

- logistics/supply coverage;
- Industry reserve and construction pressure;
- research availability/capacity;
- spacecraft construction capability;
- fleet-capacity shortfall;
- legitimately surveyed colonization opportunities; and
- legitimately known unexplored catalog space.

Strategic reviews are bounded derived state. They run only at scheduled review boundaries, are not persisted, and are reset when the campaign changes.

### Foreign-information boundary

Diplomacy currently has observer-filtered contracts and validation, but it does not yet have one authoritative persisted runtime owner in the campaign/Core lifecycle. Until that ownership exists, the strategic runtime supplies an empty foreign-information snapshot. Therefore it must not create defensive/war priorities from authoritative rival fleets, economies, IDs, or other hidden state.

When a persisted Diplomacy/intelligence runtime is introduced, it should build `KnowledgeSnapshot` strictly from observer-visible contacts/relationships/intelligence estimates. Do not derive exact enemy strength directly from `GalaxyState` as a shortcut.

## Difficulty

Higher AI difficulty should primarily improve planning, coordination, resource allocation, threat assessment, and reaction quality. Economic or production bonuses, if offered at all, belong only to explicitly selected challenge modes and must not masquerade as intelligence.
