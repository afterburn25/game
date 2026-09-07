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
        "grants": root / "capability_grants.json",
        "maturation": root / "research_maturation.json",
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
        ("capability_grants", grants),
        ("research_maturation", maturation),
    ):
        if payload.get("catalog_id") != catalog_id:
            fail(f"{label}: catalog_id does not match index")

    # Load all domain nodes using the canonical index.
    all_nodes = []
    for domain in index.get("domains", []):
        domain_id = domain["id"]
        filename = index.get("domain_files", {}).get(domain_id)
        if not filename:
            fail(f"domain {domain_id} missing domain file mapping")
        payload = load_json(root / filename)
        all_nodes.extend(payload.get("nodes", []))
    node_ids = unique_ids(all_nodes, "node")

    # Maturation state machine remains aligned with the canonical index.
    index_states = index.get("maturation_states", [])
    maturation_states = maturation.get("states", [])
    if index_states != maturation_states:
        fail(f"maturation states differ from index: index={index_states}, maturation={maturation_states}")
    state_set = set(maturation_states)
    required_states = {
        "unknown", "rumored", "hypothesized", "investigable", "experimental",
        "demonstrated", "engineering", "mature", "archived",
    }
    if state_set != required_states:
        fail(f"unexpected maturation-state set: {sorted(state_set)}")

    archive_resolutions = maturation.get("archive_resolutions", [])
    if "disproven" not in archive_resolutions:
        fail("research_maturation must support archived resolution 'disproven'")

    outcomes = maturation.get("outcomes", {})
    outcome_ids = set(outcomes)
    if not outcome_ids:
        fail("research_maturation has no outcomes")
    profiles = maturation.get("uncertainty_profiles", {})
    for profile_id, profile in profiles.items():
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
    stage_ids = {
        row.get("stage_id")
        for row in capacity.get("directed_program_model", {}).get("progression", [])
        if row.get("stage_id")
    }

    grant_rows = grants.get("node_grants", {})
    valid_grant_stages = {"demonstrated", "engineering", "mature"}
    for node_id, grant in grant_rows.items():
        if node_id not in node_ids:
            fail(f"capability grant references unknown node {node_id!r}")
        at_stage = grant.get("at_stage")
        if at_stage not in valid_grant_stages:
            fail(f"{node_id}: invalid capability grant stage {at_stage!r}")
        missing_caps = sorted(set(grant.get("capabilities", [])) - cap_ids)
        if missing_caps:
            fail(f"{node_id}: grants undefined capabilities {missing_caps}")
        missing_traits = sorted(set(grant.get("grant_civilization_traits", [])) - trait_ids)
        if missing_traits:
            fail(f"{node_id}: grants undefined traits {missing_traits}")
        capacity_stage = grant.get("research_capacity_stage")
        if capacity_stage is not None and capacity_stage not in stage_ids:
            fail(f"{node_id}: grants unknown research-capacity stage {capacity_stage!r}")

    # Required explicit capability/trait grants that define the architecture.
    expected = {
        "prototype_warp_drive": ("experimental_interstellar_transit", "demonstrated"),
        "stable_warp_drive": ("interstellar_transit", "mature"),
        "wormhole_stabilization": ("interstellar_transit", "mature"),
        "synthetic_cognition": (None, "mature"),
        "biofabrication": (None, "mature"),
    }
    for node_id, (cap_id, stage) in expected.items():
        row = grant_rows.get(node_id)
        if not row:
            fail(f"required architectural grant missing for {node_id}")
        if row.get("at_stage") != stage:
            fail(f"{node_id}: expected grant stage {stage}")
        if cap_id is not None and cap_id not in row.get("capabilities", []):
            fail(f"{node_id}: expected capability grant {cap_id}")
    if "machine_cognition_present" not in grant_rows["synthetic_cognition"].get("grant_civilization_traits", []):
        fail("synthetic_cognition must grant machine_cognition_present")
    if "biological_fabrication_possible" not in grant_rows["biofabrication"].get("grant_civilization_traits", []):
        fail("biofabrication must grant biological_fabrication_possible")

    print(
        "research maturation OK: "
        f"{len(cap_ids)} cross-lineage capabilities, {len(implications)} implications, "
        f"{len(grant_rows)} node grants, {len(outcome_ids)} outcomes"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
