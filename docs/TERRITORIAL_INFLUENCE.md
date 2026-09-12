# Territorial influence and supported expansion

Owner: feature/territorial-influence; coordination: #321 / #15. Base: integration 97091aee.

## Authority and units

Territory is a derived simulation read model. No influence currency is stored or spent.
Physical distances use the existing three-dimensional light-year coordinates; display conversion remains in the units layer.
Colony population, infrastructure, stability, administration tier and completed construction supply permanent anchors. Operational orbital installations extend those anchors. Fleets supply temporary military projection only. Existing Diplomacy claims remain separate political assertions, never automatic ownership.

Political influence uses bounded exponential distance falloff. Administration requires connection to a populated administrative anchor; a chain of relays can extend it, with attenuation on every segment. Operational supply follows actual travel lanes and functioning bases, independently of political influence. Trade reach reflects connected economic infrastructure, not assumed delivered freight. No cargo or colony sustenance is invented by this model.

## Expansion and consequences

Established systems have strong local administration and political support. Frontier systems can be settled with a support chain, at greater expedition cost and slower establishment. Remote settlement is rejected with the deficient scores and a suggested relay/depot step. Existing environmental suitability, surveys, foreign occupancy, ship fuel and passenger checks still apply.

Weak effective control reduces tax collection and increases administration expense. Installations have paid capital, industry, construction time and recurring upkeep. Funding shortages reduce their function. Passing fleets never establish sovereignty. Overlap retains an independent share and competing influence; close competitors produce contested control instead of arbitrary winner-by-ID ownership.

## Integration contract

`TerritorialRuntime` owns cached snapshots, initialized at campaign startup and advanced by simulation time. Rendering reads snapshots and does not calculate influence. Source changes invalidate on the next simulation step; periodic recomputation handles development/funding. Commands revalidate before mutation. Saves persist infrastructure, construction progress, expedition authorizations and the territorial clock; derived fields are reconstructed.

The feature consumes existing Research capabilities, AI traits, colony/outpost state, lane graph, economy and Diplomacy observer views. It does not replace those owners. Native orbital projects remain home-system infrastructure; new regional installations have explicit system identities and require a logistics vessel on site.

## Acceptance ledger

Implementation, regression tests, scenario evidence and performance results are recorded in `docs/handoffs/TERRITORIAL_INFLUENCE.md`. This initial contract is not a completion claim. Integration requires observer privacy, paid timed construction, deterministic save/load, actionable expansion gates, sensible AI infrastructure choices and measured 500-system performance.
