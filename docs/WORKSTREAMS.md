# Concurrent Development Workstreams

This file records branch/workstream ownership rules for concurrent ChatGPT development. It exists to reduce branch collisions, duplicate implementation, and cross-chat design drift.

## Adaptive Research / Technology

- Branch: **`dev/adaptive-research`**
- Scope owner: **the dedicated Adaptive Research chat/workstream**
- Status: active design/data development

This branch owns:

- Technology Possibility Graph
- research-domain node data
- Adaptive Research visibility/emergence rules
- Research Points / Research Pressure / Research Labs
- research laboratory capacity and parallel research
- evidence and applicability traits used by research
- cross-lineage capabilities used to avoid implementation-path lock-in
- research maturation, hypotheses, setbacks, failures, side discoveries
- foreign-technology research/compatibility architecture
- research-specific validation tooling and research design documentation
- future research UI/data contracts when that work begins

Canonical research paths currently include:

- `data/research/v1/`
- `scripts/validate_research_catalog.py`
- `scripts/validate_research_maturation.py`
- `docs/ADAPTIVE_RESEARCH_SYSTEM.md`
- `docs/RESEARCH_ECONOMY.md`
- `docs/RESEARCH_CAPACITY_MODEL.md`
- `docs/RESEARCH_EMERGENCE_MODEL.md`
- `docs/RESEARCH_MATURATION_MODEL.md`

## Rules for other concurrent branches

Other workstreams may **read and depend on** the research interfaces but should not independently edit the files above while `dev/adaptive-research` is active.

Examples:

- shipbuilding may ask whether a design requires `spacecraft_construction`, `interstellar_transit`, or a mature research node
- colony/logistics work may consume research capabilities
- diplomacy may consume technology/evidence/trade interfaces
- species work may define biological facts that later map to research applicability traits

If another branch needs a research change, record/request the needed interface and let the Adaptive Research branch implement the research-side change. This avoids two branches editing the same canonical graph/schema differently.

## Capability interface rule

Other systems should prefer functional **capabilities** when they do not care which technological lineage produced the result.

For example, a logistics system should ask for reliable `interstellar_transit` rather than hard-coding `stable_warp_drive` unless the feature is specifically about warp technology.

This keeps alien, biological, synthetic, foreign, hybrid, and future secret technological lineages interoperable.

## Merge discipline

- Research milestone PRs merge `dev/adaptive-research` into `main` only after all research validators and the normal .NET/Godot gates pass.
- A research design/data merge does not promote the gameplay VERSION by itself.
- After a research milestone merges, `dev/adaptive-research` should be advanced from the new `main` before further research work so it remains the persistent branch for this chat.
- Do not create a new research branch for every small research feature unless a temporary recovery/experiment branch is specifically needed.

## Ownership is organizational, not a permanent code wall

Once a feature is integrated into runtime gameplay, implementation may touch shared simulation APIs. Branch ownership exists to coordinate those edits, not to prohibit necessary integration.

When cross-workstream source changes become necessary, keep the research schema/meaning authoritative here and coordinate the consuming branch around that interface.
