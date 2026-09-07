#!/usr/bin/env python3
"""Validate Adaptive Research foreign-technology, exchange, and UI contract data."""

from __future__ import annotations

import json
import sys
from collections import Counter
from pathlib import Path


def fail(message: str) -> None:
    raise SystemExit(f"research-transfer-ui validation failed: {message}")


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
        "tacit": root / "tacit_knowledge_model.json",
        "foreign": root / "foreign_technology_model.json",
        "exchange": root / "technology_exchange_model.json",
        "ui": root / "research_ui_contract.json",
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

    # Load all public normal nodes.
    all_nodes = []
    for domain in index.get("domains", []):
        domain_id = domain.get("id")
        filename = index.get("domain_files", {}).get(domain_id)
        if not filename:
            fail(f"domain {domain_id}: missing file mapping")
        all_nodes.extend(load_json(root / filename).get("nodes", []))
    node_ids = unique_ids(all_nodes, "node")

    foreign = payloads["foreign"]
    expected_axes = {
        "understanding_axis": ["unknown", "observed", "characterized", "principle_understood", "engineering_understood"],
        "operability_axis": ["unknown", "unusable", "origin_only", "supported_operation", "adapted_operation", "native_operation"],
        "reproduction_axis": ["none", "component_replication", "subsystem_replication", "foreign_process_replication", "native_process_replication"],
        "adaptation_axis": ["none", "conceptual_inspiration", "interface_adaptation", "native_derivative", "hybrid_lineage"],
    }
    foreign_axis_ids = {}
    for key, expected_order in expected_axes.items():
        rows = foreign.get(key, [])
        ids = [row.get("id") for row in rows]
        if ids != expected_order:
            fail(f"{key} differs from canonical ordered states: {ids}")
        unique_ids(rows, key)
        foreign_axis_ids[key.removesuffix("_axis")] = set(ids)

    constraint_ids = unique_ids(foreign.get("compatibility_constraints", []), "foreign compatibility constraint")
    if not constraint_ids:
        fail("foreign technology model has no compatibility constraints")

    interfaces = foreign.get("research_node_interfaces", {})
    if not interfaces:
        fail("foreign technology model has no research-node interfaces")
    missing_interface_nodes = sorted(set(interfaces) - node_ids)
    if missing_interface_nodes:
        fail(f"foreign technology research interfaces reference unknown nodes {missing_interface_nodes}")

    for required_node in (
        "foreign_device_forensics",
        "reverse_engineering_methods",
        "technology_compatibility_science",
        "cross_lineage_engineering",
        "hybrid_design_methodology",
        "hazardous_foreign_tech_protocols",
    ):
        if required_node not in interfaces:
            fail(f"foreign technology interface missing required node {required_node}")

    foreign_rules = foreign.get("rules", {})
    for key in (
        "foreign_technology_never_auto_matures_native_node",
        "operability_does_not_imply_reproducibility",
        "understanding_does_not_imply_manufacturability",
        "reproduction_does_not_imply_biological_compatibility",
        "unusable_to_holder_does_not_mean_zero_trade_value",
        "ai_uses_same_legitimate_evidence_and_assessment_model",
    ):
        if foreign_rules.get(key) is not True:
            fail(f"foreign technology rule must be true: {key}")

    # Technology exchange packages and rights.
    exchange = payloads["exchange"]
    component_rows = exchange.get("transfer_package_components", [])
    component_ids = unique_ids(component_rows, "technology-transfer component")
    if len(component_ids) < 8:
        fail("technology exchange model needs a sufficiently decomposed transfer package model")

    tacit_ids = unique_ids(payloads["tacit"].get("knowledge_asset_types", []), "tacit knowledge asset")
    for row in component_rows:
        refs = row.get("creates_or_transfers_tacit_asset_types", [])
        if not isinstance(refs, list):
            fail(f"transfer component {row['id']}: tacit asset mapping must be a list")
        missing = sorted(set(refs) - tacit_ids)
        if missing:
            fail(f"transfer component {row['id']}: undefined tacit asset refs {missing}")
        if not isinstance(row.get("creates_or_references_evidence"), bool):
            fail(f"transfer component {row['id']}: creates_or_references_evidence must be boolean")

    expected_tacit_map = {
        "scientific_theory_dossier": {"codified_records"},
        "experimental_data_package": {"experimental_dataset"},
        "engineering_blueprint_package": {"codified_records"},
        "manufacturing_process_package": {"experimental_protocols"},
        "reference_hardware": {"intact_prototype"},
        "production_tooling_transfer": {"manufacturing_tooling"},
        "expert_assistance": {"expert_cohort"},
        "training_program": {"training_pipeline"},
        "operating_institution_transfer": {"operating_institution"},
    }
    by_component = {row["id"]: row for row in component_rows}
    for component_id, expected_refs in expected_tacit_map.items():
        if component_id not in by_component:
            fail(f"missing required technology-transfer component {component_id}")
        actual = set(by_component[component_id].get("creates_or_transfers_tacit_asset_types", []))
        if actual != expected_refs:
            fail(f"{component_id}: expected tacit mapping {sorted(expected_refs)}, found {sorted(actual)}")

    right_ids = unique_ids(exchange.get("rights", []), "technology-transfer right")
    for required_right in ("internal_research", "manufacture", "military_use", "export_to_third_party", "sublicense", "resell_package"):
        if required_right not in right_ids:
            fail(f"technology exchange model missing right {required_right}")

    if exchange.get("enforcement_rule", {}).get("legal_terms_are_not_physics") is not True:
        fail("technology licenses must be modeled as legal rules, not physics")
    if exchange.get("buyer_specific_value_assessment", {}).get("no_fixed_universal_technology_value") is not True:
        fail("technology exchange model must reject fixed universal technology value")
    if exchange.get("transfer_effects", {}).get("transfer_never_directly_sets_native_technology_mature") is not True:
        fail("technology transfer must never directly set a native technology Mature")
    if "technology_licensing_institutions" not in node_ids:
        fail("technology exchange architecture expects public node technology_licensing_institutions")

    # UI contract must match research/maturation and foreign-tech schemas.
    ui = payloads["ui"]
    maturation_states = index.get("maturation_states", [])
    expected_visible = [state for state in maturation_states if state != "unknown"]
    visible_states = ui.get("research_horizon", {}).get("visible_states", [])
    if visible_states != expected_visible:
        fail(f"UI visible research states differ from canonical maturation order: {visible_states}")
    horizon = ui.get("research_horizon", {})
    if horizon.get("render_unknown_nodes") is not False:
        fail("UI must not render unknown research nodes")
    if horizon.get("render_hidden_placeholder_slots") is not False:
        fail("UI must not render hidden future placeholder slots")
    if horizon.get("render_hidden_pressure_meters") is not False:
        fail("UI must not leak hidden research through pressure meters")

    foreign_panel_axes = ui.get("foreign_technology_panel", {}).get("axes", [])
    if foreign_panel_axes != ["understanding", "operability", "reproduction", "adaptation"]:
        fail(f"foreign-technology UI axes mismatch canonical model: {foreign_panel_axes}")
    for axis_name in foreign_panel_axes:
        if axis_name not in foreign_axis_ids:
            fail(f"UI foreign axis {axis_name} has no foreign-technology model axis")

    if ui.get("technology_exchange_panel", {}).get("never_display_universal_fixed_technology_value") is not True:
        fail("technology exchange UI must not display a universal fixed technology value")
    if ui.get("performance_contract", {}).get("no_per_frame_research_graph_scan") is not True:
        fail("research UI contract must prohibit per-frame full graph scans")
    if ui.get("tree_layout", {}).get("preserve_viewport_when_new_nodes_appear") is not True:
        fail("evolving research UI must preserve viewport when branches appear")
    if ui.get("tree_layout", {}).get("links_only_between_visible_nodes") is not True:
        fail("research UI may not draw links to hidden nodes")

    print(
        "research transfer/UI OK: "
        f"4 foreign-tech axes, {len(constraint_ids)} constraints, "
        f"{len(component_ids)} transfer components, {len(right_ids)} rights, "
        f"{len(visible_states)} visible research states"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
