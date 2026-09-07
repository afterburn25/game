# Adaptive Research Validation Stack

Research design/data changes on `dev/adaptive-research` must pass the full validator stack before merge:

1. `validate_research_catalog.py` — possibility graph, IDs, prerequisites, pressures, evidence/applicability references, cycles, counts.
2. `validate_research_maturation.py` — cross-lineage capabilities, maturation states/outcomes, structural grants.
3. `validate_research_competence.py` — knowledge fields, competence, research facilities, tacit knowledge, Project Readiness.
4. `validate_research_transfer_ui.py` — foreign technology, transfer/licensing packages, research UI secrecy/layout rules.
5. `validate_research_start_runtime.py` — starting-history compositions, prerequisite closure, runtime integration boundary, materialized research view contract.

CI runs these before .NET restore/build and the pinned Godot editor/runtime smoke tests.

A validator should be strengthened when a new research schema gains stable references. Do not weaken validation merely to make a failing PR pass; correct the underlying data or explicitly revise the architecture.
