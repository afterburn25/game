#!/usr/bin/env python3
"""Validate Adaptive Research field competence, facility, tacit-knowledge, and readiness data."""

from __future__ import annotations

import json
import sys
from collections import Counter, defaultdict
from pathlib import Path


def fail(message: str) -> None:
    raise SystemExit(f"research-competence validation failed: {message}")


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


def check_contiguous_bands(rows, label: str, min_value: int = 0, max_value: int = 100) -> None:
    if not rows:
        fail(f"{label} has no bands")
    ordered = sorted(rows, key=lambda row: row.get("min", -1))
    expected = min_value
    for row in ordered:
        low = row.get("min")
        high = row.get("max")
        if not isinstance(low, (int, float)) or not isinstance(high, (int, float)):
            fail(f"{label}: band bounds must be numeric")
        if low != expected or high < low:
            fail(f"{label}: non-contiguous/invalid band {low}..{high}, expected start {expected}")
        expected = high + 1
    if expected != max_value + 1:
        fail(f"{label}: bands do not end at {max_value}")


def main() -> int:
    root = Path(sys.argv[1] if len(sys.argv) > 1 else "data/research/v1")
    paths = {
        "index": root / "index.json",
        "economy": root / "research_economy.json",
        "maturation": root / "maturation_model.json",
        "fields": root / "knowledge_fields.json",
        "competence": root / "research_competence_model.json",
        "facilities": root / "research_facility_model.json",
        "tacit": root / "tacit_knowledge_model.json",
        "readiness": root / "project_readiness_model.json",
    }
    for path in paths.values():
        if not path.is_file():
            fail(f"missing {path}")

    payloads = {name: load_json(path) for name, path in paths.items()}
    index = payloads["index"]
    catalog_id = index.get("catalog_id")
    for name, payload in payloads.items():
        if name == "index":
            continue
        if payload.get("catalog_id") != catalog_id:
            fail(f"{name}: catalog_id does not match index")

    # Load all technology nodes.
    all_nodes = []
    for domain in index.get("domains", []):
        domain_id = domain.get("id")
        filename = index.get("domain_files", {}).get(domain_id)
        if not filename:
            fail(f"domain {domain_id}: missing file mapping")
        domain_payload = load_json(root / filename)
        all_nodes.extend(domain_payload.get("nodes", []))
    node_ids = unique_ids(all_nodes, "node")

    # Knowledge-field catalog and node references.
    field_rows = payloads["fields"].get("fields", [])
    field_ids = unique_ids(field_rows, "knowledge field")
    unknown_node_fields = defaultdict(set)
    for node in all_nodes:
        node_id = node.get("id")
        fields = node.get("knowledge_fields", [])
        if not fields:
            fail(f"{node_id}: knowledge_fields may not be empty")
        for field_id in fields:
            if field_id not in field_ids:
                unknown_node_fields[field_id].add(node_id)
    if unknown_node_fields:
        detail = {key: sorted(value) for key, value in sorted(unknown_node_fields.items())}
        fail(f"undefined node knowledge-field references: {detail}")

    for row in field_rows:
        field_id = row["id"]
        for related in row.get("related_fields", []):
            if related not in field_ids:
                fail(f"field {field_id}: unknown related field {related!r}")
            if related == field_id:
                fail(f"field {field_id}: cannot relate to itself")

    # Competence components and bands.
    competence = payloads["competence"]
    component_ids = set(competence.get("competence_components", {}))
    expected_components = {"theoretical", "experimental", "engineering"}
    if component_ids != expected_components:
        fail(f"competence components must be {sorted(expected_components)}, found {sorted(component_ids)}")
    check_contiguous_bands(competence.get("competence_bands", []), "competence_bands")

    transfer = competence.get("growth_rules", {}).get("related_field_transfer", {})
    fraction = transfer.get("default_transfer_fraction_of_direct_gain")
    if not isinstance(fraction, (int, float)) or not 0 <= fraction <= 0.5:
        fail("related-field transfer fraction must be between 0 and 0.5")

    stage_weights = competence.get("project_field_readiness", {}).get("stage_component_weights", {})
    expected_stage_ids = {"experimental", "demonstrated", "engineering"}
    if set(stage_weights) != expected_stage_ids:
        fail(f"competence stage weights must cover {sorted(expected_stage_ids)}")
    for stage_id, weights in stage_weights.items():
        if set(weights) != expected_components:
            fail(f"{stage_id}: competence weights must cover all three components")
        total = sum(weights.values())
        if abs(total - 1.0) > 1e-9:
            fail(f"{stage_id}: competence component weights sum to {total}, expected 1.0")
        if any(not isinstance(value, (int, float)) or value < 0 for value in weights.values()):
            fail(f"{stage_id}: invalid competence component weight")

    readiness_components = competence.get("project_readiness", {}).get("inputs", {})
    if set(readiness_components) != {"field_competence", "facility_readiness", "evidence_readiness", "tacit_expertise"}:
        fail("research_competence_model readiness inputs mismatch canonical four-component model")
    readiness_weight_total = sum(row.get("weight", 0) for row in readiness_components.values())
    if abs(readiness_weight_total - 1.0) > 1e-9:
        fail(f"research_competence_model readiness weights sum to {readiness_weight_total}, expected 1.0")
    check_contiguous_bands(competence.get("readiness_to_rp_efficiency", []), "competence readiness efficiency bands")
    for row in competence.get("readiness_to_rp_efficiency", []):
        efficiency = row.get("rp_efficiency")
        if not isinstance(efficiency, (int, float)) or not 0 < efficiency <= 1.5:
            fail(f"invalid competence readiness efficiency {efficiency!r}")

    # Project readiness model must agree with competence model and economy.
    readiness = payloads["readiness"]
    readiness_inputs = readiness.get("readiness_components", {})
    if set(readiness_inputs) != set(readiness_components):
        fail("project_readiness_model component IDs differ from research_competence_model")
    readiness_total = sum(row.get("default_weight", 0) for row in readiness_inputs.values())
    if abs(readiness_total - 1.0) > 1e-9:
        fail(f"project_readiness_model weights sum to {readiness_total}, expected 1.0")
    check_contiguous_bands(readiness.get("readiness_to_progress_efficiency", []), "project readiness efficiency bands")
    for row in readiness.get("readiness_to_progress_efficiency", []):
        efficiency = row.get("efficiency")
        if not isinstance(efficiency, (int, float)) or not 0 < efficiency <= 1.5:
            fail(f"invalid project readiness efficiency {efficiency!r}")

    economy = payloads["economy"]
    if "cost_formula" in economy:
        fail("research_economy still contains legacy contextual cost_formula; use base_work_formula + readiness")
    base_work = economy.get("base_work_formula", {})
    if base_work.get("companion_readiness_model") != "project_readiness_model.json":
        fail("research_economy must reference project_readiness_model.json")
    if economy.get("contextual_progress", {}).get("no_independent_percentage_modifier_stack") is not True:
        fail("research_economy must prohibit independent percentage modifier stacking")

    # Facility catalog.
    facilities = payloads["facilities"]
    facility_cap_rows = facilities.get("facility_capabilities", [])
    facility_cap_ids = unique_ids(facility_cap_rows, "research facility capability")
    institution_rows = facilities.get("institution_archetypes", [])
    unique_ids(institution_rows, "research institution archetype")
    provided_caps = set()
    for institution in institution_rows:
        enabled_by = institution.get("enabled_by")
        if enabled_by is not None and enabled_by not in node_ids:
            fail(f"institution {institution['id']}: enabled_by unknown technology {enabled_by!r}")
        labs = institution.get("effective_lab_units")
        if not isinstance(labs, (int, float)) or labs <= 0:
            fail(f"institution {institution['id']}: invalid effective_lab_units {labs!r}")
        for field_id in institution.get("specialized_fields", []):
            if field_id not in field_ids:
                fail(f"institution {institution['id']}: undefined specialized field {field_id!r}")
        for cap_id in institution.get("facility_capabilities", []):
            if cap_id not in facility_cap_ids:
                fail(f"institution {institution['id']}: undefined facility capability {cap_id!r}")
            provided_caps.add(cap_id)

    maturation_stages = set(payloads["maturation"].get("directed_project_stages", {}))
    required_caps = set()
    for node_id, stage_map in facilities.get("stage_requirements", {}).items():
        if node_id not in node_ids:
            fail(f"facility stage requirements reference unknown node {node_id!r}")
        for stage_id, requirement in stage_map.items():
            if stage_id not in maturation_stages:
                fail(f"{node_id}: unknown maturation stage in facility requirements {stage_id!r}")
            for mode in ("all_of", "any_of"):
                refs = requirement.get(mode, [])
                if not isinstance(refs, list):
                    fail(f"{node_id}/{stage_id}: {mode} facility requirement must be a list")
                for cap_id in refs:
                    if cap_id not in facility_cap_ids:
                        fail(f"{node_id}/{stage_id}: undefined facility capability {cap_id!r}")
                    required_caps.add(cap_id)
    unavailable = sorted(required_caps - provided_caps)
    if unavailable:
        fail(f"hard facility requirements have no institution provider: {unavailable}")

    # Tacit knowledge.
    tacit = payloads["tacit"]
    asset_rows = tacit.get("knowledge_asset_types", [])
    unique_ids(asset_rows, "knowledge asset type")
    for asset in asset_rows:
        support = asset.get("primary_support", [])
        if not support or not set(support).issubset(expected_components):
            fail(f"knowledge asset {asset['id']}: invalid primary_support {support}")
    assimilation = tacit.get("assimilation_progression", [])
    assimilation_ids = [row.get("stage") for row in assimilation]
    if assimilation_ids != ["access", "interpreted", "codified", "trained", "native_practice"]:
        fail(f"unexpected tacit-knowledge assimilation progression: {assimilation_ids}")

    print(
        "research competence OK: "
        f"{len(field_ids)} knowledge fields, {len(institution_rows)} institution archetypes, "
        f"{len(facility_cap_ids)} facility capabilities, {len(asset_rows)} tacit asset types"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
