#!/usr/bin/env python3
"""Validate Stellar Continuum's public adaptive research catalog.

This validator intentionally checks structure/integrity, not game balance.
"""

from __future__ import annotations

import json
import sys
from collections import Counter, defaultdict, deque
from pathlib import Path


ALLOWED_COMPLEXITY = {"foundation", "developing", "advanced", "frontier"}


def fail(message: str) -> None:
    raise SystemExit(f"research-catalog validation failed: {message}")


def load_json(path: Path):
    try:
        with path.open("r", encoding="utf-8") as handle:
            return json.load(handle)
    except Exception as exc:  # pragma: no cover - CLI diagnostic
        fail(f"could not parse {path}: {exc}")


def main() -> int:
    root = Path(sys.argv[1] if len(sys.argv) > 1 else "data/research/v1")
    index_path = root / "index.json"
    economy_path = root / "research_economy.json"
    if not index_path.is_file():
        fail(f"missing {index_path}")
    if not economy_path.is_file():
        fail(f"missing {economy_path}")

    index = load_json(index_path)
    economy = load_json(economy_path)

    domains = index.get("domains", [])
    domain_files = index.get("domain_files", {})
    pressure_rows = index.get("research_pressures", [])
    solution_sets = index.get("alternative_solution_sets", [])

    pressure_ids = [row.get("id") for row in pressure_rows]
    pressure_dupes = [key for key, count in Counter(pressure_ids).items() if count > 1]
    if pressure_dupes:
        fail(f"duplicate pressure ids: {pressure_dupes}")
    pressure_id_set = set(pressure_ids)

    all_nodes = []
    expected_domain_counts = {row["id"]: int(row["node_count"]) for row in domains}
    for domain_id, expected_count in expected_domain_counts.items():
        filename = domain_files.get(domain_id)
        if not filename:
            fail(f"domain {domain_id} has no file mapping")
        path = root / filename
        if not path.is_file():
            fail(f"domain file missing for {domain_id}: {path}")
        payload = load_json(path)
        if payload.get("domain") != domain_id:
            fail(f"{path} declares domain {payload.get('domain')!r}, expected {domain_id!r}")
        nodes = payload.get("nodes", [])
        if len(nodes) != expected_count:
            fail(f"domain {domain_id} has {len(nodes)} nodes, index expects {expected_count}")
        all_nodes.extend(nodes)

    if len(all_nodes) != int(index.get("node_count", -1)):
        fail(f"catalog has {len(all_nodes)} nodes, index says {index.get('node_count')}")

    ids = [node.get("id") for node in all_nodes]
    duplicate_ids = [key for key, count in Counter(ids).items() if count > 1]
    if duplicate_ids:
        fail(f"duplicate node ids: {duplicate_ids}")
    node_ids = set(ids)

    for node in all_nodes:
        node_id = node.get("id")
        if not node_id:
            fail("node missing id")
        complexity = node.get("complexity")
        if complexity not in ALLOWED_COMPLEXITY:
            fail(f"{node_id}: invalid complexity {complexity!r}")
        if not isinstance(node.get("graph_depth"), int) or node["graph_depth"] < 0:
            fail(f"{node_id}: graph_depth must be a nonnegative integer")
        if not node.get("solution_family"):
            fail(f"{node_id}: missing solution_family")
        prereqs = node.get("prerequisites", {})
        for mode in ("all_of", "any_of"):
            refs = prereqs.get(mode, [])
            for ref in refs:
                if ref not in node_ids:
                    fail(f"{node_id}: missing prerequisite {ref!r}")
                if ref == node_id:
                    fail(f"{node_id}: self prerequisite")
        for pressure_id in node.get("pressure_affinities", []):
            if pressure_id not in pressure_id_set:
                fail(f"{node_id}: unknown pressure affinity {pressure_id!r}")

    for solution_set in solution_sets:
        set_id = solution_set.get("id", "<unnamed>")
        for ref in solution_set.get("candidate_nodes", []):
            if ref not in node_ids:
                fail(f"alternative solution set {set_id}: unknown node {ref!r}")

    complexity_defaults = economy.get("complexity_defaults", {})
    for complexity in ALLOWED_COMPLEXITY:
        cfg = complexity_defaults.get(complexity)
        if not cfg:
            fail(f"research_economy missing defaults for {complexity}")
        minimum = int(cfg.get("minimum_labs", 0))
        recommended = int(cfg.get("recommended_labs", 0))
        base_rp = int(cfg.get("base_research_points", 0))
        if minimum < 1 or recommended < minimum or base_rp < 1:
            fail(f"invalid lab/RP defaults for {complexity}")

    overrides = economy.get("node_requirement_overrides", {})
    for node_id, override in overrides.items():
        if node_id not in node_ids:
            fail(f"research_economy override references unknown node {node_id!r}")
        for key in ("required_pressure", "required_pressure_any"):
            for pressure_id, threshold in override.get(key, {}).items():
                if pressure_id not in pressure_id_set:
                    fail(f"{node_id}: override references unknown pressure {pressure_id!r}")
                if not isinstance(threshold, (int, float)) or threshold < 0 or threshold > 100:
                    fail(f"{node_id}: invalid pressure threshold {threshold!r}")
        minimum = override.get("minimum_labs")
        recommended = override.get("recommended_labs")
        if minimum is not None and (not isinstance(minimum, int) or minimum < 1):
            fail(f"{node_id}: invalid minimum_labs override")
        if recommended is not None and (not isinstance(recommended, int) or recommended < 1):
            fail(f"{node_id}: invalid recommended_labs override")
        if minimum is not None and recommended is not None and recommended < minimum:
            fail(f"{node_id}: recommended_labs below minimum_labs")

    # Detect cycles using every explicit prerequisite edge. For an any_of list, every
    # candidate is still a dependency edge that must point backward through an acyclic graph.
    indegree = {node_id: 0 for node_id in node_ids}
    children = defaultdict(set)
    for node in all_nodes:
        child = node["id"]
        prereqs = node.get("prerequisites", {})
        refs = set(prereqs.get("all_of", [])) | set(prereqs.get("any_of", []))
        for parent in refs:
            if child not in children[parent]:
                children[parent].add(child)
                indegree[child] += 1

    queue = deque(sorted(node_id for node_id, degree in indegree.items() if degree == 0))
    visited = 0
    while queue:
        current = queue.popleft()
        visited += 1
        for child in children[current]:
            indegree[child] -= 1
            if indegree[child] == 0:
                queue.append(child)

    if visited != len(node_ids):
        cyclic = sorted(node_id for node_id, degree in indegree.items() if degree > 0)
        fail(f"dependency graph contains cycle(s), unresolved nodes: {cyclic}")

    print(
        "research catalog OK: "
        f"{len(all_nodes)} nodes, {len(expected_domain_counts)} domains, "
        f"{len(pressure_id_set)} pressure types, {len(solution_sets)} alternative solution sets"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
