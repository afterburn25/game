# Early economy scale

> **Legacy implemented prototype:** This document records the currently shipped balancing
> bridge so existing code and saves can be interpreted. The `1 Credit = $10 million` mapping
> has been rejected for the full economy and must not be used for new pricing or as the future
> Interstellar Credit exchange rate. The replacement and one-time save migration are specified
> in the Immediate roadmap sections “Realistic costs and lifecycle economics” and “Labor-backed
> production and public finance.” Population-derived revenue and building output without workforce
> or material constraints remain legacy placeholders scheduled for replacement. Food and potable-water
> carrying capacity are now authoritative; workforce, housing and material inputs remain incomplete. The replacement
> permits taxes from real employed household and business activity, charges construction and
> operating spending against the surplus, and caps sustainable population through delivered food,
> potable water, housing and environmental support.

The current prototype economy uses **1 legacy Credit = $10 million in 2050 Earth purchasing power**.
This is a player reference for the Human opening, not a claim that every civilization
uses dollars or has a fixed foreign-exchange market. A Credit represents a strategic
budget unit: finance, labor, contracts, scarce components and political authority bundled
at the scale of a planetary government.

New ordinary civilizations begin with 500 Credits, equivalent to a $5 billion strategic
reserve. Industry and Effective Research Labs remain separate physical work capacities.
Paying Credits authorizes construction; Industry completes it over simulation time,
while finite laboratory capacity is assigned to research programs.

## Capital authorizations

| Program | Credits | Earth reference |
|---|---:|---:|
| Planetary Research Network | 150 | $1.5B |
| Industrial Automation Program | 200 | $2B |
| Orbital Launch Complex | 250 | $2.5B |
| Orbital Shipyard | 350 | $3.5B |
| Asteroid Resource Network | 320 | $3.2B |
| Warp Test Facility | 450 | $4.5B |
| Pathfinder Scout | 70 | $700M |
| Deep-Space Science Vessel | 100 | $1B |
| Patrol Corvette | 120 | $1.2B |
| Interstellar Colony Ship | 180 | $1.8B |
| Settlement expedition | 120 | $1.2B |
| Surface power complex | 25 | $250M |
| Surface science complex | 40 | $400M |
| Surface fabricator complex | 50 | $500M |
| Surface trade hub | 45 | $450M |
| Surface habitat complex | 45 | $450M |

The Planetary Research Network adds four finite Effective Research Labs to the
Adaptive Research campaign. Laboratory capacity is allocated to projects and does
not accumulate as a spendable stockpile. A completed, powered surface Science Lab
adds one more Effective Research Lab; its advanced campus upgrade adds 2.5, and a
three-building Research District increases its powered lab capacity by 25%.

Surface entries represent complete operating complexes rather than one literal building.

Completed surface complexes can be upgraded in place. The upgrade consumes stored Industry
immediately as a deliberate stockpile decision; it does not create a second construction site.

| Surface upgrade | Credits | Industry | Earth reference |
|---|---:|---:|---:|
| Fusion power complex | 30 | 240 | $300M |
| Advanced science campus | 50 | 320 | $500M |
| Automated fabrication arcology | 60 | 360 | $600M |
| Interstellar trade exchange | 55 | 300 | $550M |

## Recurring cash flow

Colony economic activity produces 0.75 Credits per billion inhabitants per day, modified
by represented infrastructure and stability. Administration scales from 0.12 Credits per
day for a small dependent outpost to 1 Credit per day at 250 million inhabitants.
Population services cost 0.50 Credits per billion inhabitants per day and are also modified
by infrastructure. Occupied environments add life-support upkeep from the number of required physical
mitigation systems and supported population. The Colonies page shows both the requirement
count and the resulting daily cost for each holding.
This leaves a stable developed population economically
useful while ensuring that expansion and low-stability colonies are real commitments.

Active fleet operations cost 0.08 Credits/day for a scout, 0.12 for a science vessel,
0.16 for a colony ship, 0.35 for a patrol corvette and 0.14 for a bulk freighter. These represent crew, maintenance,
mission support and readiness across the entire vessel program.

Population growth is bounded by the lower of local food and potable-water capacity. A solid
world's surveyed environment supplies natural capacity from usable surface area, species-relative
habitability and solvent suitability; sealed baseline infrastructure supplies a small floor.
Powered Controlled Agriculture and Water Reclamation complexes each support another two billion
inhabitants in their respective category. Growth slows continuously as population approaches the
limit, and an over-cap population declines until supply is restored. The Colonies and Surface pages
show both capacities, sustainable population and the limiting shortage.

Completed strategic facilities carry their own operating commitments. The Orbital Launch
Complex costs 0.08 Credits/day, the Orbital Shipyard 0.12, the Asteroid Resource Network
0.18, and the Warp Test Facility 0.15. The extraction network adds 1.50 Industry/day after
completion; it requires Orbital Industry and a completed Launch Complex, costs 320 Credits
and 1,800 Industry to establish, and appears as a physical resource node in home-system
logistics. Its output is useful but cannot replace the broader population and automation base.

Completed surface complexes add daily upkeep whether or not local power is sufficient:
0.02 Credits for power, 0.04 for science, 0.05 for fabrication and 0.03 for trade. This
keeps excess or unpowered construction from being free to retain.
Their advanced forms cost 0.04, 0.08, 0.10 and 0.06 Credits per day respectively, while
providing 8 power, 2.5 science, 2.5 industry or 0.18 Credits per day when powered.

Three completed complexes in the same functional family create a surface district. A
Research, Industrial or Commercial district adds 25% to its powered science, industry or
trade output; an Energy district adds 25% to generator supply. Advanced complexes remain
in their original functional family. Upkeep and power demand are unchanged, so clustering
raises output efficiency without erasing operating commitments.

A completed powered trade hub produces 0.08 Credits/day, or about $292 million per Earth
year, and costs 0.03 Credits/day to operate. Its $450 million authorization therefore has
a simple net payback near 2.5 years before power and opportunity costs. It is useful but
does not overwhelm population revenue or fleet costs.

## Player-facing rules

The Economy page uses the same calculation as the simulation and separately displays
colony revenue, surface trade, administration, population services, fleet operations,
orbital maintenance, surface maintenance and
net flow. Capital choices show Credit and dollar-reference prices before ordering and are
disabled when reserves are insufficient. Credits cannot fall below zero; a depleted
treasury prevents new authorizations while existing recurring obligations continue to be
included in the displayed net flow.

This is the legacy playable balance profile. Until migration lands, changes must remain save-safe;
new work should use measured campaign pacing and the planned physical cost model rather than
extending the rejected Credit denomination into taxes, debt, trade, private markets or multiple
currencies.
