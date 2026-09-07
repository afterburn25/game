# Adaptive Research UI Model

The research interface must communicate an evolving civilization-specific scientific horizon without revealing the hidden universal Technology Possibility Graph.

The central player experience is:

> **The tree you see today is what your civilization currently knows how to think about. Thirty years from now it can contain branches that did not exist on your screen today.**

## Unknown technology does not appear as a grey box

The player must never see:

- the full universal tree
- hidden node slots
- greyed-out secret technologies
- `???` boxes whose presence reveals that something exists
- pressure meters for scientific fields the civilization does not know exist
- a hidden-future node count

Unknown means genuinely unknown.

## Visible research states

The current horizon can display:

- Rumored
- Hypothesized
- Investigable
- Experimental
- Demonstrated
- Engineering
- Mature
- Archived

These states use text/icons as well as color so state is readable without color alone.

## The tree grows without throwing the player around

A dynamically evolving tree can become frustrating if every new branch rearranges the whole screen.

Layout rules:

- visible nodes receive stable IDs/anchors
- newly exposed nodes appear near their strongest visible prerequisite or solution-family anchor
- unrelated visible branches should not globally reflow just because one branch changed
- preserve viewport/zoom when possible
- preserve the selected node when possible
- animate a meaningful new branch appearing
- links are drawn only between currently visible nodes

At large scale:

- mature/archive-heavy historical branches can collapse by default
- filter by domain/state/current pressure/current project
- allow pinning important nodes/branches
- search only known/visible science
- add breadcrumbs/overview navigation when necessary

The goal is an evolving map of scientific history, not a constantly shuffling flowchart.

## Node cards

Always show:

- technology/hypothesis name
- current visible state
- domain
- short description based only on known information

When Investigable or active, show:

- minimum/recommended labs
- assigned labs if active
- estimated completion/stage duration
- simple readiness label
- known missing requirements

For active research, also show:

- maturation stage
- stage RP progress
- current lab allocation
- major setback/anomaly state if relevant

For Mature research, show where appropriate:

- maturity date
- known capability outputs
- construction/deployment enabled by the knowledge

Never show:

- secret future children
- hidden trigger probabilities
- unknown alternative capability sources
- exact hidden random outcome rolls

## Why did this branch appear?

When the civilization legitimately understands why a branch became visible, the UI should explain it.

Possible known causes:

- prior scientific prerequisite matured
- recognized problem / Research Pressure
- basic science or theoretical work
- experiment/anomalous result
- environmental condition
- observed foreign capability
- captured/traded evidence or records
- new applicability capability/trait
- side discovery

Example:

> **High-Gravity Cardiovascular Adaptation** became Investigable because long-term health data from a 1.42g colony raised High-Gravity Health Pressure beyond the recognized research threshold.

But the UI must not expose a pressure/evidence cause before the scientific field itself has become known.

## Early-game simplicity

In 2050 the primary research panel should remain straightforward.

Show prominently:

- available projects
- current directed project
- total Effective Research Labs
- assigned/unassigned labs
- RP/year
- estimated completion
- simple Project Readiness label
- clear known blocker

Do not force the player to manage theory/experimental/engineering competence separately from the first minute.

Advanced details can remain one click deeper.

## Advanced project detail

For players who want to understand the simulation, provide sections for:

- knowledge prerequisites
- cross-lineage capability requirements
- explicit pressure thresholds
- evidence/confidence
- theoretical competence
- experimental competence
- engineering competence
- contributing research institutions
- stage-specific facility requirements
- tacit expertise / foreign experts / tooling
- maturation history and major setbacks
- capability/deployment consequences

If the estimated research time changes, the player should be able to see why.

Example:

> Estimate improved because the new High-Energy Research Complex provides the required field-physics testing capability and 4 specialized Effective Labs.

That is much better than an unexplained `Research Speed +15%` icon.

## Research Pressure UI

Pressure is need/evidence, not currency.

Once a field is recognized, UI can show:

- pressure name/value
- required threshold for a visible pressure-gated possibility
- known sources raising it
- whether it is currently rising/falling

Example:

> Missile Threat: 47
> Point Defense threshold: 25
> Primary sources: 3 recent missile attacks, observed hostile missile doctrine

Do not display pressure for unknown scientific fields merely to foreshadow future branches.

## Research portfolios / lab allocation

At the starting Single Priority stage:

- emphasize one directed project
- summarize background/basic science separately

After parallelism is unlocked:

- portfolio screen shows active directed projects
- show labs assigned to each
- show eligible/specialized capacity
- allow reallocation while respecting minimum labs/facility capability

If another project cannot start, explicitly distinguish:

- directed-program coordination limit
- insufficient total labs
- insufficient eligible/specialized labs
- missing facility
- missing evidence
- unmet pressure threshold
- missing scientific/capability prerequisite

Late-game automation/policies can manage routine allocations while preserving player override.

## Foreign technology UI

Foreign technology never collapses to one reverse-engineering percentage.

Show four assessments:

### Understanding
How much of the underlying phenomenon/design is understood?

### Operability
Can we operate the source system, and under what dependencies?

### Reproduction
Can we make components/subsystems/full foreign-process copies/native equivalents?

### Adaptation
Are we merely inspired, using adapters, developing a native derivative, or creating a hybrid lineage?

Also show:

- known compatibility constraints
- uncertainty that unknown constraints remain
- assessment confidence
- foreign experts/tooling/institution dependence
- material/energy dependencies
- biological/environment compatibility
- consumable/software/identity dependencies
- legitimate next research actions

## Technology transfer / licensing UI

A trade screen should clearly separate:

**What is included physically/intellectually**

from

**What legal rights are granted**

from

**What the recipient is technically capable of doing.**

Show package components such as:

- theory
- data
- blueprints
- process documentation
- hardware
- tooling
- experts
- training
- operating institution

Then show rights separately:

- internal research
- operate supplied units
- manufacture
- modify
- civilian/military use
- export
- sublicense
- resale

Then show recipient-specific technical assessment:

- likely research utility
- likely operational utility
- dependencies
- compatibility
- expected assimilation horizon
- hazard risk
- resale/brokerage opportunity if legitimately known

There is never a universal `Technology Value: 500`.

And a contract does not physically prevent prohibited use. The UI can warn:

> Military manufacture would violate the current license.

rather than:

> Military manufacture unavailable.

unless some real technical copy protection actually prevents it.

## Notifications

Notify meaningful changes:

- important new branch appeared
- possibility became Investigable
- research moved to Demonstrated/Engineering/Mature
- major setback
- hypothesis refined/disproven
- anomalous result
- meaningful side discovery
- critical facility/expert dependency gained/lost
- new parallel research capacity unlocked

Do not spam:

- small competence fluctuations
- routine background-science ticks
- hidden failed candidate checks
- tiny pressure changes

Batch low-priority changes when useful.

## Research history

Mature and Archived science remains searchable as civilization history.

A current-horizon view should not have to keep thousands of ancient mature nodes expanded forever.

Archived records can show:

- maturity/archive date
- disproven/abandoned/superseded resolution
- major historical summary
- surviving capability/knowledge relationship

This supports centuries-long campaigns without turning the active research screen into an unreadable museum.

## Performance contract

The UI consumes a **materialized civilization research view model**, not the full universal graph.

- no per-frame scan of 330+ possibilities
- update only changed visible subgraphs where practical
- virtualize/collapse large old sections
- load deep evidence/package histories on demand
- retain stable node layout anchors

The public catalog can eventually grow much larger without requiring every hidden node to exist as an active UI object.

## Canonical machine-readable contract

`data/research/v1/research_ui_contract.json`
