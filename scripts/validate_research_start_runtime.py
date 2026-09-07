#!/usr/bin/env python3
"""Validate Adaptive Research starting profiles, runtime boundary, and view model."""

from __future__ import annotations

import json
import sys
from collections import Counter
from pathlib import Path


def fail(message: str) -> None:
    raise SystemExit(f"research-start-runtime validation failed: {message}")


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
        "capabilities": root / "capability_model.json",
        "fields": root / "knowledge_fields.json",
        "facilities": root / "research_facility_model.json",
        "pressures": root / "pressure_dynamics.json",
        "evidence": root / "evidence_types.json",
        "tacit": root / "tacit_knowledge_model.json",
        "capacity": root / "research_capacity.json",
        "profile_contract": root / "starting_research_profile_contract.json",
        "fragments": root / "starting_research_fragments.json",
        "profiles": root / "starting_reference_profiles.json",
        "runtime": root / "research_runtime_contract.json",
        "view": root / "research_view_model_contract.json",
    }
    for path in paths.values():
        if not path.is_file():
            fail(f"missing {path}")

    data = {key: load_json(path) for key, path in paths.items()}
    catalog_id = data["index"].get("catalog_id")
    for label, payload in data.items():
        if payload.get("catalog_id") != catalog_id:
            fail(f"{label}: catalog_id does not match index")

    # Canonical research references.
    all_nodes = []
    for domain in data["index"].get("domains", []):
        domain_id = domain["id"]
        filename = data["index"].get("domain_files", {}).get(domain_id)
        if not filename:
            fail(f"domain {domain_id}: missing file mapping")
        all_nodes.extend(load_json(root / filename).get("nodes", []))
    node_ids = unique_ids(all_nodes, "node")
    node_by_id = {node["id"]: node for node in all_nodes}
    state_order = data["index"].get("maturation_states", [])
    state_rank = {state: idx for idx, state in enumerate(state_order)}
    if "mature" not in state_rank or "investigable" not in state_rank:
        fail("canonical index missing required maturation states")

    trait_ids = unique_ids(data["traits"].get("traits", []), "applicability trait")
    cap_ids = unique_ids(data["capabilities"].get("cross_lineage_capabilities", []), "cross-lineage capability")
    field_ids = unique_ids(data["fields"].get("fields", []), "knowledge field")
    institution_rows = data["facilities"].get("institution_archetypes", [])
    institution_ids = unique_ids(institution_rows, "research institution")
    institution_by_id = {row["id"]: row for row in institution_rows}
    pressure_ids = set(data["pressures"].get("rules", {}))
    evidence_ids = unique_ids(data["evidence"].get("evidence_types", []), "evidence type")
    tacit_ids = unique_ids(data["tacit"].get("knowledge_asset_types", []), "tacit asset type")
    capacity_stage_ids = {
        row.get("stage_id")
        for row in data["capacity"].get("directed_program_model", {}).get("progression", [])
        if row.get("stage_id")
    }

    # Starting profile contract guardrails.
    contract = data["profile_contract"]
    if contract.get("future_tree_rule", {}).get("starting_profile_must_not_list_hidden_future_nodes") is not True:
        fail("starting profile contract must forbid hidden future nodes")
    if contract.get("future_tree_rule", {}).get("profile_does_not_create_species_specific_research_catalog") is not True:
        fail("starting profiles must not create species-specific research catalogs")
    if contract.get("starting_visibility", {}).get("unknown_future_nodes_not_seeded_as_placeholders") is not True:
        fail("starting profile contract must forbid unknown placeholder seeding")
    if contract.get("performance", {}).get("composed_profile_not_repeatedly_evaluated_during_campaign") is not True:
        fail("starting profile composition must be initialization-only")

    fragment_rows = data["fragments"].get("fragments", [])
    fragment_ids = unique_ids(fragment_rows, "starting research fragment")
    fragment_by_id = {row["id"]: row for row in fragment_rows}
    base_fragment_ids = {row["id"] for row in fragment_rows if row.get("kind") == "base_era"}
    if not base_fragment_ids:
        fail("at least one base-era starting fragment is required")
    composition_cap = data["fragments"].get("composition_cap_per_competence_component")
    if not isinstance(composition_cap, (int, float)) or not 1 <= composition_cap <= 100:
        fail("invalid starting-fragment competence composition cap")

    def validate_seed_refs(row: dict, label: str) -> None:
        node_states = row.get("starting_node_states", {})
        if not isinstance(node_states, dict):
            fail(f"{label}: starting_node_states must be an object")
        for node_id, state_value in node_states.items():
            state = state_value.get("state") if isinstance(state_value, dict) else state_value
            if node_id not in node_ids:
                fail(f"{label}: unknown starting node {node_id!r}")
            if state not in state_rank:
                fail(f"{label}: invalid starting state {state!r} for {node_id}")
            if state in {"unknown", "rumored"}:
                fail(f"{label}: must not seed {node_id} as {state}; omit non-visible future state instead")
        for field_id, values in row.get("starting_field_competence", {}).items():
            if field_id not in field_ids:
                fail(f"{label}: unknown knowledge field {field_id!r}")
            if not isinstance(values, dict):
                fail(f"{label}: competence for {field_id} must be an object")
            for component in ("theoretical", "experimental", "engineering"):
                value = values.get(component, 0)
                if not isinstance(value, (int, float)) or not 0 <= value <= 100:
                    fail(f"{label}: invalid {field_id}.{component} competence {value!r}")
        for trait_id in row.get("starting_applicability_traits", []):
            if trait_id not in trait_ids:
                fail(f"{label}: unknown starting applicability trait {trait_id!r}")
        for cap in row.get("starting_capabilities", []):
            cap_id = cap.get("id") if isinstance(cap, dict) else cap
            if cap_id not in cap_ids:
                fail(f"{label}: unknown starting capability {cap_id!r}")
        for inst in row.get("starting_research_institutions", []):
            inst_id = inst.get("institution_archetype_id")
            if inst_id not in institution_ids:
                fail(f"{label}: unknown research institution {inst_id!r}")
            count = inst.get("count", 0)
            if not isinstance(count, int) or count < 1:
                fail(f"{label}: invalid institution count for {inst_id}")
        for pressure_id, value in row.get("starting_pressure_state", {}).items():
            if pressure_id not in pressure_ids:
                fail(f"{label}: unknown starting pressure {pressure_id!r}")
            if not isinstance(value, (int, float)) or not 0 <= value <= 100:
                fail(f"{label}: invalid starting pressure value for {pressure_id}")
        for evidence in row.get("starting_evidence", []):
            evidence_id = evidence.get("type_id") if isinstance(evidence, dict) else evidence
            if evidence_id not in evidence_ids:
                fail(f"{label}: unknown starting evidence type {evidence_id!r}")
        for asset in row.get("starting_tacit_assets", []):
            asset_type = asset.get("type_id") if isinstance(asset, dict) else asset
            if asset_type not in tacit_ids:
                fail(f"{label}: unknown starting tacit asset type {asset_type!r}")
        stage = row.get("starting_directed_program_stage")
        if stage is not None and stage not in capacity_stage_ids:
            fail(f"{label}: unknown research-capacity stage {stage!r}")

    for row in fragment_rows:
        validate_seed_refs(row, f"fragment {row['id']}")
        if row.get("kind") not in {"base_era", "scientific_history_fragment", "environmental_history_fragment"}:
            fail(f"fragment {row['id']}: invalid kind {row.get('kind')!r}")

    profile_rows = data["profiles"].get("profiles", [])
    profile_ids = unique_ids(profile_rows, "starting reference profile")
    if len(profile_ids) < 3:
        fail("starting reference profiles should demonstrate multiple divergent starts")

    for profile in profile_rows:
        label = f"profile {profile['id']}"
        fragment_refs = profile.get("fragment_ids", [])
        missing = sorted(set(fragment_refs) - fragment_ids)
        if missing:
            fail(f"{label}: unknown fragment ids {missing}")
        bases = [frag_id for frag_id in fragment_refs if frag_id in base_fragment_ids]
        if len(bases) != 1:
            fail(f"{label}: requires exactly one base-era fragment, found {bases}")

        # Merge composition using the documented rules.
        composed_states: dict[str, str] = {}
        composed_traits: set[str] = set()
        composed_caps: set[str] = set()
        composed_institutions: Counter[str] = Counter()
        composed_pressures: dict[str, float] = {}
        composed_competence: dict[str, dict[str, float]] = {}
        capacity_stages: list[str] = []

        for frag_id in fragment_refs:
            frag = fragment_by_id[frag_id]
            for node_id, state_value in frag.get("starting_node_states", {}).items():
                state = state_value.get("state") if isinstance(state_value, dict) else state_value
                old = composed_states.get(node_id)
                if old is None or state_rank[state] > state_rank[old]:
                    composed_states[node_id] = state
            composed_traits.update(frag.get("starting_applicability_traits", []))
            for cap in frag.get("starting_capabilities", []):
                composed_caps.add(cap.get("id") if isinstance(cap, dict) else cap)
            for inst in frag.get("starting_research_institutions", []):
                composed_institutions[inst["institution_archetype_id"]] += inst["count"]
            for pressure_id, value in frag.get("starting_pressure_state", {}).items():
                composed_pressures[pressure_id] = max(composed_pressures.get(pressure_id, 0), value)
            for field_id, components in frag.get("starting_field_competence", {}).items():
                target = composed_competence.setdefault(field_id, {"theoretical":0,"experimental":0,"engineering":0})
                for component in target:
                    target[component] = min(composition_cap, max(target[component], components.get(component, 0)))
            if frag.get("starting_directed_program_stage"):
                capacity_stages.append(frag["starting_directed_program_stage"])

        # Add profile-level seed facts.
        profile_seed = {
            "starting_node_states": profile.get("additional_starting_node_states", {}),
            "starting_field_competence": profile.get("additional_starting_field_competence", {}),
            "starting_applicability_traits": profile.get("additional_starting_applicability_traits", []),
            "starting_capabilities": profile.get("additional_starting_capabilities", []),
            "starting_research_institutions": profile.get("additional_starting_research_institutions", []),
            "starting_pressure_state": profile.get("additional_starting_pressure_state", {}),
            "starting_evidence": profile.get("additional_starting_evidence", []),
            "starting_tacit_assets": profile.get("additional_starting_tacit_assets", []),
            "starting_directed_program_stage": profile.get("additional_starting_directed_program_stage"),
        }
        validate_seed_refs(profile_seed, label)
        composed_traits.update(profile_seed["starting_applicability_traits"])
        for pressure_id, value in profile_seed["starting_pressure_state"].items():
            composed_pressures[pressure_id] = max(composed_pressures.get(pressure_id, 0), value)

        # Starting visible/current research must be historically prerequisite-closed.
        for node_id, state in composed_states.items():
            if state_rank[state] < state_rank["investigable"]:
                continue
            prereqs = node_by_id[node_id].get("prerequisites", {})
            for parent in prereqs.get("all_of", []):
                if composed_states.get(parent) != "mature":
                    fail(f"{label}: {node_id} is {state} but required prerequisite {parent} is not Mature")
            any_of = prereqs.get("any_of", [])
            if any_of and not any(composed_states.get(parent) == "mature" for parent in any_of):
                fail(f"{label}: {node_id} is {state} but no any_of prerequisite is Mature: {any_of}")

        # Institution enablers must be historically known when the institution is seeded.
        for inst_id, count in composed_institutions.items():
            if count < 1:
                fail(f"{label}: invalid composed institution count {inst_id}={count}")
            enabled_by = institution_by_id[inst_id].get("enabled_by")
            if enabled_by and composed_states.get(enabled_by) != "mature":
                fail(f"{label}: starting institution {inst_id} requires Mature technology {enabled_by}")

        if any(value < 0 or value > 100 for value in composed_pressures.values()):
            fail(f"{label}: composed pressure outside 0..100")
        if not composed_states:
            fail(f"{label}: composition produced no known research state")

    # Runtime boundary guardrails.
    runtime = data["runtime"]
    runtime_rules = runtime.get("tick_policy", {})
    required_true = [
        "no_full_graph_per_tick_scan",
        "foreign_assessments_recompute_on_relevant_change_only",
        "UI_projection_rebuilds_on_research_state_change_not_every_frame",
    ]
    for key in required_true:
        if runtime_rules.get(key) is not True:
            fail(f"runtime tick policy must set {key}=true")
    if runtime.get("save_contract", {}).get("do_not_serialize_static_catalog") is not True:
        fail("runtime save contract must not serialize static research catalog")
    if runtime.get("fair_information_contract", {}).get("enemy_exact_hidden_technology_never_used_as_research_input") is not True:
        fail("runtime fair-information contract must forbid exact hidden enemy technology input")
    unique_ids(runtime.get("external_input_events", []), "research runtime input event")
    unique_ids(runtime.get("external_queries", []), "research runtime query")
    output_events = runtime.get("research_output_events", [])
    if len(output_events) != len(set(output_events)) or not output_events:
        fail("research runtime output events must be unique and nonempty")

    # Materialized view must remain secrecy-safe and compatible with canonical states.
    view = data["view"]
    projection = view.get("projection_rules", {})
    for key in ("read_only", "unknown_nodes_never_projected", "hidden_node_count_never_exposed", "visible_edges_only_between_projected_nodes", "no_per_frame_hidden_graph_query"):
        source = projection if key != "no_per_frame_hidden_graph_query" else view.get("performance", {})
        if source.get(key) is not True:
            fail(f"research view contract must set {key}=true")
    node_view_state_text = view.get("ResearchNodeView", {}).get("state", "")
    for forbidden in ("unknown",):
        if forbidden in [token.strip() for token in node_view_state_text.split("|")]:
            fail("ResearchNodeView must never project Unknown nodes")
    project_stage_text = view.get("ResearchProjectView", {}).get("stage", "")
    for stage in [token.strip() for token in project_stage_text.split("|") if token.strip()]:
        if stage not in state_rank:
            fail(f"ResearchProjectView references unknown stage {stage!r}")
    blocker_ids = view.get("blocker_ids", [])
    if len(blocker_ids) != len(set(blocker_ids)) or not blocker_ids:
        fail("research view blocker ids must be unique/nonempty")
    command_ids = unique_ids(view.get("command_contract_from_UI_or_AI", []), "research view command")
    required_commands = {"start_directed_research", "pause_directed_research", "resume_directed_research", "reallocate_research_labs"}
    if not required_commands.issubset(command_ids):
        fail(f"research view missing core commands {sorted(required_commands - command_ids)}")

    print(
        "research start/runtime OK: "
        f"{len(fragment_ids)} history fragments, {len(profile_ids)} reference starts, "
        f"{len(runtime.get('external_input_events', []))} input events, "
        f"{len(runtime.get('external_queries', []))} integration queries"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
