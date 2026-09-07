#!/usr/bin/env python3
"""Validate Adaptive Research capability interoperability and maturation data."""

from __future__ import annotations

import json
import sys
from collections import Counter, defaultdict, deque
from pathlib import Path


def fail(message: str) -> None:
    raise SystemExit(f"research-maturation validation failed: {message}")


def load_json(path: Path):
    try:
        with path.open("r", encoding="utf-8") as handle:
            return json.load(handle)
    except Exception as exc:
        fail(f"could not parse {path}: {exc}")


def unique_ids(rows, label: str) -> set[str]:
    ids = [row.get("id") for row in rows]
    if None in ids or "" in ids:
        fail(f"{label} row missing id")
    dupes = [key for key, count in Counter(ids).items() if count > 1]
    if dupes:
        fail(f"duplicate {label} ids: {dupes}")
    return set(ids)


def main() -> int:
    root = Path(sys.argv[1] if len(sys.argv) > 1 else "data/research/v1")
    paths = {
        "index": root / "index.json",
        "traits": root / "applicability_traits.json",
        "capacity": root / "research_capacity.json",
        "capabilities": root / "capability_model.json",
        "grants": root / "technology_grants.json",
        "maturation": root / "maturation_model.json",
    }
    for path in paths.values():
        if not path.is_file():
            fail(f"missing {path}")

    index = load_json(paths["index"])
    traits = load_json(paths["traits"])
    capacity = load_json(paths["capacity"])
    capabilities = load_json(paths["capabilities"])
    grants = load_json(paths["grants"])
    maturation = load_json(paths["maturation"])

    catalog_id = index.get("catalog_id")
    for label, payload in (
        ("applicability_traits", traits),
        ("research_capacity", capacity),
        ("capability_model", capabilities),
        ("technology_grants", grants),
        ("maturation_model", maturation),
    ):
        if payload.get("catalog_id") != catalog_id:
            fail(f"{label}: catalog_id does not match index")

    all_nodes = []
    for domain in index.get("domains", []):
        domain_id = domain["id"]
        filename = index.get("domain_files", {}).get(domain_id)
        if not filename:
            fail(f"domain {domain_id} missing domain file mapping")
        payload = load_json(root / filename)
        all_nodes.extend(payload.get("nodes", []))
    node_ids = unique_ids(all_nodes, "node")
    node_by_id = {node["id"]: node for node in all_nodes}

    # Maturation state machine remains aligned with the canonical index.
    index_states = index.get("maturation_states", [])
    maturation_states = maturation.get("states", [])
    if index_states != maturation_states:
        fail(f"maturation states differ from index: index={index_states}, maturation={maturation_states}")
    required_states = {
        "unknown", "rumored", "hypothesized", "investigable", "experimental",
        "demonstrated", "engineering", "mature", "archived",
    }
    state_set = set(maturation_states)
    if state_set != required_states:
        fail(f"unexpected maturation-state set: {sorted(state_set)}")
    if "disproven" not in maturation.get("archive_resolutions", []):
        fail("maturation_model must support archived resolution 'disproven'")

    outcomes = maturation.get("outcomes", {})
    outcome_ids = set(outcomes)
    if not outcome_ids:
        fail("maturation_model has no outcomes")
    for profile_id, profile in maturation.get("uncertainty_profiles", {}).items():
        refs = profile.get("normal_outcomes", [])
        if not refs:
            fail(f"uncertainty profile {profile_id} has no outcomes")
        missing = sorted(set(refs) - outcome_ids)
        if missing:
            fail(f"uncertainty profile {profile_id} references unknown outcomes {missing}")

    directed = maturation.get("directed_project_stages", {})
    for stage_id in directed:
        if stage_id not in state_set:
            fail(f"directed project stage {stage_id!r} is not a known maturation state")
    previous_end = 0.0
    for stage_id in ("experimental", "demonstrated", "engineering"):
        row = directed.get(stage_id)
        if not row:
            fail(f"missing directed project stage {stage_id}")
        start = row.get("typical_rp_fraction_start")
        end = row.get("typical_rp_fraction_end")
        if not isinstance(start, (int, float)) or not isinstance(end, (int, float)):
            fail(f"{stage_id}: RP fractions must be numeric")
        if not 0 <= start <= end <= 1:
            fail(f"{stage_id}: invalid RP fraction range {start}..{end}")
        if abs(start - previous_end) > 1e-9:
            fail(f"{stage_id}: RP stage starts at {start}, previous stage ended at {previous_end}")
        previous_end = end
    if abs(previous_end - 1.0) > 1e-9:
        fail("directed maturation RP stages must end at 1.0")

    # Cross-lineage capabilities.
    cap_rows = capabilities.get("cross_lineage_capabilities", [])
    cap_ids = unique_ids(cap_rows, "cross-lineage capability")
    allowed_scopes = set(capabilities.get("allowed_scopes", []))
    if not allowed_scopes:
        fail("capability_model has no allowed scopes")
    for row in cap_rows:
        if row.get("scope") not in allowed_scopes:
            fail(f"capability {row['id']}: invalid scope {row.get('scope')!r}")

    implications = capabilities.get("implications", [])
    implication_graph = defaultdict(set)
    indegree = {cap_id: 0 for cap_id in cap_ids}
    for edge in implications:
        source = edge.get("from")
        target = edge.get("to")
        if source not in cap_ids or target not in cap_ids:
            fail(f"capability implication references unknown capability: {source!r}->{target!r}")
        if source == target:
            fail(f"capability {source}: self implication")
        if target not in implication_graph[source]:
            implication_graph[source].add(target)
            indegree[target] += 1
    queue = deque(sorted(key for key, degree in indegree.items() if degree == 0))
    visited = 0
    while queue:
        current = queue.popleft()
        visited += 1
        for target in implication_graph[current]:
            indegree[target] -= 1
            if indegree[target] == 0:
                queue.append(target)
    if visited != len(cap_ids):
        cyclic = sorted(key for key, degree in indegree.items() if degree > 0)
        fail(f"capability implication cycle detected: {cyclic}")

    # Inline functional requirements must use registered cross-lineage capabilities.
    for node in all_nodes:
        node_id = node["id"]
        requirements = node.get("capability_requirements")
        if requirements is None:
            continue
        for mode in ("all_of", "any_of"):
            refs = requirements.get(mode, [])
            if not isinstance(refs, list):
                fail(f"{node_id}: capability_requirements.{mode} must be a list")
            missing = sorted(set(refs) - cap_ids)
            if missing:
                fail(f"{node_id}: undefined capability requirements {missing}")
        context = requirements.get("context", "civilization")
        if not isinstance(context, str) or not context:
            fail(f"{node_id}: capability requirement context must be a nonempty string")

    trait_ids = unique_ids(traits.get("traits", []), "applicability trait")
    research_stage_ids = {
        row.get("stage_id")
        for row in capacity.get("directed_program_model", {}).get("progression", [])
        if row.get("stage_id")
    }

    # Default maturity behavior and explicit structural/early grants.
    default_stage = grants.get("default_rules", {}).get("declared_node_capabilities_grant_at_stage")
    if default_stage != "mature":
        fail("technology_grants must grant declared node capabilities at Mature by default")

    on_demonstrated = grants.get("on_demonstrated", {})
    for node_id, row in on_demonstrated.items():
        if node_id not in node_ids:
            fail(f"on_demonstrated references unknown node {node_id!r}")
        caps = row.get("grant_capabilities", [])
        missing = sorted(set(caps) - cap_ids)
        if missing:
            fail(f"{node_id}: demonstrated grant uses undefined capabilities {missing}")
        declared = set(node_by_id[node_id].get("capabilities", []))
        undeclared = sorted(set(caps) - declared)
        if undeclared:
            fail(f"{node_id}: demonstrated grant capability not declared by node {undeclared}")

    deployment_events = grants.get("deployment_events", {})
    deployment_ids = set(deployment_events)
    if "persistent_machine_cognition_instantiated" not in deployment_ids:
        fail("technology_grants must define persistent_machine_cognition_instantiated")

    on_mature = grants.get("on_mature", {})
    for node_id, row in on_mature.items():
        if node_id not in node_ids:
            fail(f"on_mature references unknown node {node_id!r}")
        missing_traits = sorted(set(row.get("add_civilization_traits", [])) - trait_ids)
        if missing_traits:
            fail(f"{node_id}: on_mature grants undefined traits {missing_traits}")
        capacity_stage = row.get("set_research_capacity_stage")
        if capacity_stage is not None and capacity_stage not in research_stage_ids:
            fail(f"{node_id}: on_mature sets unknown research-capacity stage {capacity_stage!r}")
        missing_events = sorted(set(row.get("unlock_deployment_events", [])) - deployment_ids)
        if missing_events:
            fail(f"{node_id}: on_mature unlocks undefined deployment events {missing_events}")

    for event_id, event in deployment_events.items():
        required_tech = event.get("requires_any_mature_technology", [])
        missing_tech = sorted(set(required_tech) - node_ids)
        if missing_tech:
            fail(f"deployment event {event_id}: unknown technology refs {missing_tech}")
        missing_traits = sorted(set(event.get("add_civilization_traits", [])) - trait_ids)
        if missing_traits:
            fail(f"deployment event {event_id}: undefined trait grants {missing_traits}")

    # Architectural interoperability requirements.
    for node_id, cap_id in (
        ("stable_warp_drive", "interstellar_transit"),
        ("wormhole_stabilization", "interstellar_transit"),
        ("long_range_warp", "extended_interstellar_transit"),
        ("orbital_shipyard", "spacecraft_construction"),
        ("megastructure_fabrication", "megastructure_construction"),
        ("interstellar_logistics_network", "interstellar_supply_network"),
    ):
        if cap_id not in node_by_id[node_id].get("capabilities", []):
            fail(f"{node_id}: expected cross-lineage capability output {cap_id}")

    prototype = on_demonstrated.get("prototype_warp_drive", {})
    if "experimental_interstellar_transit" not in prototype.get("grant_capabilities", []):
        fail("prototype_warp_drive must grant experimental_interstellar_transit at Demonstrated")

    bio = on_mature.get("biofabrication", {})
    if "biological_fabrication_possible" not in bio.get("add_civilization_traits", []):
        fail("biofabrication must grant biological_fabrication_possible at Mature")

    machine_event = deployment_events["persistent_machine_cognition_instantiated"]
    if "machine_cognition_present" not in machine_event.get("add_civilization_traits", []):
        fail("persistent machine cognition deployment must grant machine_cognition_present")
    for route in ("synthetic_cognition", "whole_mind_emulation"):
        if "persistent_machine_cognition_instantiated" not in on_mature.get(route, {}).get("unlock_deployment_events", []):
            fail(f"{route}: must unlock persistent_machine_cognition_instantiated")
        if "machine_cognition_present" in on_mature.get(route, {}).get("add_civilization_traits", []):
            fail(f"{route}: research alone must not claim persistent machine cognition already exists")

    for node_id, stage_id in (
        ("coordinated_research_networks", "coordinated_research_networks"),
        ("distributed_scientific_portfolios", "distributed_scientific_portfolios"),
        ("autonomous_research_portfolios", "autonomous_research_portfolios"),
    ):
        if on_mature.get(node_id, {}).get("set_research_capacity_stage") != stage_id:
            fail(f"{node_id}: must set research-capacity stage {stage_id}")

    print(
        "research maturation OK: "
        f"{len(cap_ids)} cross-lineage capabilities, {len(implications)} implications, "
        f"{len(on_mature)} maturity grants, {len(deployment_events)} deployment events, "
        f"{len(outcome_ids)} outcomes"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
