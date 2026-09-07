#!/usr/bin/env python3
"""Validate Stellar Continuum's public adaptive research catalog.

This validator checks structural integrity and public-content guardrails, not game balance.
"""

from __future__ import annotations

import json
import sys
from collections import Counter, defaultdict, deque
from pathlib import Path

ALLOWED_COMPLEXITY = {"foundation", "developing", "advanced", "frontier"}


def fail(message: str) -> None:
    raise SystemExit(f"research-catalog validation failed: {message}")


def warn(message: str) -> None:
    print(f"research-catalog warning: {message}", file=sys.stderr)


def load_json(path: Path):
    try:
        with path.open("r", encoding="utf-8") as handle:
            return json.load(handle)
    except Exception as exc:  # pragma: no cover - CLI diagnostic
        fail(f"could not parse {path}: {exc}")


def unique_ids(rows, label: str) -> set[str]:
    ids = [row.get("id") for row in rows]
    if None in ids or "" in ids:
        fail(f"{label} row missing id")
    duplicates = [key for key, count in Counter(ids).items() if count > 1]
    if duplicates:
        fail(f"duplicate {label} ids: {duplicates}")
    return set(ids)


def main() -> int:
    root = Path(sys.argv[1] if len(sys.argv) > 1 else "data/research/v1")
    required_paths = {
        "index": root / "index.json",
        "economy": root / "research_economy.json",
        "capacity": root / "research_capacity.json",
        "traits": root / "applicability_traits.json",
        "evidence": root / "evidence_types.json",
        "pressure_dynamics": root / "pressure_dynamics.json",
        "emergence": root / "emergence_model.json",
    }
    for required in required_paths.values():
        if not required.is_file():
            fail(f"missing {required}")

    index = load_json(required_paths["index"])
    economy = load_json(required_paths["economy"])
    capacity = load_json(required_paths["capacity"])
    traits = load_json(required_paths["traits"])
    evidence = load_json(required_paths["evidence"])
    pressure_dynamics = load_json(required_paths["pressure_dynamics"])
    emergence = load_json(required_paths["emergence"])

    catalog_id = index.get("catalog_id")
    for label, payload in (
        ("research_economy", economy),
        ("research_capacity", capacity),
        ("applicability_traits", traits),
        ("evidence_types", evidence),
        ("pressure_dynamics", pressure_dynamics),
        ("emergence_model", emergence),
    ):
        if payload.get("catalog_id") != catalog_id:
            fail(f"{label}: catalog_id does not match index")

    if index.get("rules", {}).get("secret_content_included") is not False:
        fail("public index must explicitly set secret_content_included=false")

    domains = index.get("domains", [])
    domain_files = index.get("domain_files", {})
    pressure_rows = index.get("research_pressures", [])
    solution_sets = index.get("alternative_solution_sets", [])

    domain_id_set = unique_ids(domains, "domain")
    if set(domain_files) != domain_id_set:
        missing = sorted(domain_id_set - set(domain_files))
        extra = sorted(set(domain_files) - domain_id_set)
        fail(f"domain_files mismatch; missing={missing}, extra={extra}")

    pressure_id_set = unique_ids(pressure_rows, "pressure")
    if len(pressure_id_set) != int(index.get("pressure_count", -1)):
        fail(f"index pressure_count says {index.get('pressure_count')}, found {len(pressure_id_set)}")

    trait_rows = traits.get("traits", [])
    trait_id_set = unique_ids(trait_rows, "applicability trait")
    if int(emergence.get("applicability_trait_count", -1)) != len(trait_id_set):
        fail(
            "emergence_model applicability_trait_count says "
            f"{emergence.get('applicability_trait_count')}, found {len(trait_id_set)}"
        )
    for row in trait_rows:
        if row.get("scope") not in {"population_or_species", "civilization"}:
            fail(f"trait {row['id']}: invalid scope {row.get('scope')!r}")
        if not isinstance(row.get("mutable"), bool):
            fail(f"trait {row['id']}: mutable must be boolean")

    evidence_rows = evidence.get("evidence_types", [])
    evidence_id_set = unique_ids(evidence_rows, "evidence type")
    if int(emergence.get("evidence_type_count", -1)) != len(evidence_id_set):
        fail(
            "emergence_model evidence_type_count says "
            f"{emergence.get('evidence_type_count')}, found {len(evidence_id_set)}"
        )

    pressure_rules = pressure_dynamics.get("rules", {})
    if set(pressure_rules) != pressure_id_set:
        missing = sorted(pressure_id_set - set(pressure_rules))
        extra = sorted(set(pressure_rules) - pressure_id_set)
        fail(f"pressure dynamics mismatch; missing={missing}, extra={extra}")
    if int(emergence.get("pressure_rule_count", -1)) != len(pressure_rules):
        fail(
            "emergence_model pressure_rule_count says "
            f"{emergence.get('pressure_rule_count')}, found {len(pressure_rules)}"
        )
    for pressure_id, rule in pressure_rules.items():
        metric_signals = rule.get("metric_signals", [])
        event_signals = rule.get("event_signals", [])
        if not isinstance(metric_signals, list) or not isinstance(event_signals, list):
            fail(f"{pressure_id}: pressure signal lists must be arrays")
        if not metric_signals and not event_signals:
            fail(f"{pressure_id}: pressure rule has no signal sources")
        decay = rule.get("decay_per_year")
        floor = rule.get("memory_floor")
        if not isinstance(decay, (int, float)) or decay < 0 or decay > 100:
            fail(f"{pressure_id}: invalid decay_per_year {decay!r}")
        if not isinstance(floor, (int, float)) or floor < 0 or floor > 100:
            fail(f"{pressure_id}: invalid memory_floor {floor!r}")

    all_nodes = []
    expected_domain_counts = {row["id"]: int(row["node_count"]) for row in domains}
    for domain_id, expected_count in expected_domain_counts.items():
        filename = domain_files[domain_id]
        path = root / filename
        if not path.is_file():
            fail(f"domain file missing for {domain_id}: {path}")
        payload = load_json(path)
        if payload.get("catalog_id") != catalog_id:
            fail(f"{path}: catalog_id does not match index")
        if payload.get("domain") != domain_id:
            fail(f"{path} declares domain {payload.get('domain')!r}, expected {domain_id!r}")
        nodes = payload.get("nodes", [])
        if len(nodes) != expected_count:
            fail(f"domain {domain_id} has {len(nodes)} nodes, index expects {expected_count}")
        all_nodes.extend(nodes)

    if len(all_nodes) != int(index.get("node_count", -1)):
        fail(f"catalog has {len(all_nodes)} nodes, index says {index.get('node_count')}")

    node_ids = unique_ids(all_nodes, "node")
    node_by_id = {node["id"]: node for node in all_nodes}
    unknown_traits = defaultdict(set)
    unknown_evidence = defaultdict(set)

    for node in all_nodes:
        node_id = node["id"]
        if node.get("public_normal_research") is not True:
            fail(f"{node_id}: public catalog node must set public_normal_research=true")
        complexity = node.get("complexity")
        if complexity not in ALLOWED_COMPLEXITY:
            fail(f"{node_id}: invalid complexity {complexity!r}")
        if not isinstance(node.get("graph_depth"), int) or node["graph_depth"] < 0:
            fail(f"{node_id}: graph_depth must be a nonnegative integer")
        if not node.get("solution_family"):
            fail(f"{node_id}: missing solution_family")
        if node.get("domain") not in expected_domain_counts:
            fail(f"{node_id}: unknown domain {node.get('domain')!r}")

        prereqs = node.get("prerequisites", {})
        for mode in ("all_of", "any_of"):
            refs = prereqs.get(mode, [])
            if not isinstance(refs, list):
                fail(f"{node_id}: prerequisites.{mode} must be a list")
            for ref in refs:
                if ref not in node_ids:
                    fail(f"{node_id}: missing prerequisite {ref!r}")
                if ref == node_id:
                    fail(f"{node_id}: self prerequisite")

        for pressure_id in node.get("pressure_affinities", []):
            if pressure_id not in pressure_id_set:
                fail(f"{node_id}: unknown pressure affinity {pressure_id!r}")

        applicability = node.get("applicability", {})
        for trait_id in applicability.get("requires_traits", []):
            if trait_id not in trait_id_set:
                unknown_traits[trait_id].add(node_id)
        for evidence_id in applicability.get("requires_evidence", []):
            if evidence_id not in evidence_id_set:
                unknown_evidence[evidence_id].add(node_id)

    if unknown_traits:
        detail = {key: sorted(value) for key, value in sorted(unknown_traits.items())}
        fail(f"undefined applicability trait references: {detail}")
    if unknown_evidence:
        detail = {key: sorted(value) for key, value in sorted(unknown_evidence.items())}
        fail(f"undefined evidence type references: {detail}")

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
        if "default_pressure_threshold" in cfg:
            fail(f"{complexity}: default_pressure_threshold would make pressure an implicit universal gate")

    if economy.get("availability_rules", {}).get("pressure_is_opt_in") is not True:
        fail("research_economy must explicitly declare pressure_is_opt_in=true")

    overrides = economy.get("node_requirement_overrides", {})
    for node_id, override in overrides.items():
        if node_id not in node_ids:
            fail(f"research_economy override references unknown node {node_id!r}")
        for key in ("required_pressure", "required_pressure_any"):
            for pressure_id, threshold in override.get(key, {}).items():
                if pressure_id not in pressure_id_set:
                    fail(f"{node_id}: override references unknown pressure {pressure_id!r}")
                if not isinstance(threshold, (int, float)) or not 0 <= threshold <= 100:
                    fail(f"{node_id}: invalid pressure threshold {threshold!r}")
        for evidence_id in override.get("required_evidence", []):
            if evidence_id not in evidence_id_set:
                fail(f"{node_id}: override references unknown evidence type {evidence_id!r}")
        minimum = override.get("minimum_labs")
        recommended = override.get("recommended_labs")
        if minimum is not None and (not isinstance(minimum, int) or minimum < 1):
            fail(f"{node_id}: invalid minimum_labs override")
        if recommended is not None and (not isinstance(recommended, int) or recommended < 1):
            fail(f"{node_id}: invalid recommended_labs override")
        if minimum is not None and recommended is not None and recommended < minimum:
            fail(f"{node_id}: recommended_labs below minimum_labs")

    for stage in capacity.get("directed_program_model", {}).get("progression", []):
        required_technology = stage.get("required_technology")
        if required_technology is not None and required_technology not in node_ids:
            fail(f"research-capacity stage {stage.get('stage_id')}: unknown required technology {required_technology!r}")
        limit = stage.get("directed_program_limit")
        if limit is not None and (not isinstance(limit, int) or limit < 1):
            fail(f"research-capacity stage {stage.get('stage_id')}: invalid directed_program_limit")

    # Detect cycles using every explicit prerequisite edge. For any_of, every candidate
    # must still point through an acyclic possibility graph.
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

            parent_depth = node_by_id[parent]["graph_depth"]
            child_depth = node["graph_depth"]
            if child_depth <= parent_depth:
                warn(f"{child}: graph_depth {child_depth} is not greater than prerequisite {parent} depth {parent_depth}")

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
        f"{len(pressure_id_set)} pressure types/rules, {len(trait_id_set)} applicability traits, "
        f"{len(evidence_id_set)} evidence types, {len(solution_sets)} alternative solution sets"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
