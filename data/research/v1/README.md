# Adaptive Research Seed Catalog v1

This directory contains the public design-data seed for Stellar Continuum's Adaptive Research System.

## Files

- `index.json` — catalog metadata, research-pressure definitions, domain list, alternative-solution sets, and domain file map.
- `research_economy.json` — Research Point, Research Pressure, Research Lab, concurrency, lab-scaling, and seeded requirement rules.
- one JSON file per research domain — the technology possibility nodes.

## Important model distinction

This directory is **not** a fixed player-visible technology tree.

It is the universe-scale possibility graph from which each civilization materializes a much smaller changing research horizon.

A civilization save should store only civilization-specific research state such as:

- known/mature node IDs
- visible hypotheses
- active projects and RP progress
- effective Research Lab capacity/assignments
- relevant Research Pressure values
- evidence tokens
- field competencies/priorities

Do not copy all 244 nodes into every civilization save.

## Node fields

Common fields include:

- `id` — stable save-safe identifier
- `name` — player-facing working name
- `domain` — broad field
- `complexity` — foundation/developing/advanced/frontier
- `graph_depth` — approximate depth used by seed cost/scaling rules
- `solution_family` — groups alternative technological traditions
- `knowledge_fields` — indexing/competence fields
- `awareness_sources` — ways the possibility can become known
- `pressure_affinities` — contextual pressures that make the research more relevant
- `prerequisites.all_of` / `prerequisites.any_of`
- `applicability.requires_traits` — biological/civilizational applicability
- `applicability.requires_evidence` — required observation/sample/device evidence
- `capabilities` — outputs useful to the simulation independently of implementation
- `is_hypothesis` — indicates a speculative stage that can precede engineering technology

## Lab/RP requirements

Every node receives Research Lab and RP requirements from `research_economy.json`.

The default is derived from `complexity`, with individual overrides for projects that should require larger/smaller programs or explicit Research Pressure/evidence thresholds.

This avoids duplicating balance values across hundreds of node records and lets balancing change without changing stable technology IDs.

## Validation

Run:

```text
python3 scripts/validate_research_catalog.py data/research/v1
```

CI runs the same validator.

The validator checks at least:

- declared domain/node counts
- duplicate node IDs
- missing prerequisites
- unknown pressure references
- alternative-solution-set references
- research-economy override references
- basic lab/RP requirement validity
- prerequisite cycles

## Public-content boundary

This dataset contains normal/public research possibilities only.

Do not add exact secret-discovery triggers, rare hidden artifact chains, secret AI eligibility, or intentionally undisclosed rare technologies here. Hidden content can use the same runtime schema from a separate content source later.

## Balance status

The catalog establishes architecture and broad relative scale, not final commercial balance.

RP costs, pressure thresholds, lab output, and lab-efficiency curves should be tuned with long-run simulation data while preserving the core **RP + Pressure + Labs** model.
