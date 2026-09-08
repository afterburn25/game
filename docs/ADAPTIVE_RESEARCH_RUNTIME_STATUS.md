# Adaptive Research Runtime Status

This research-owned record tracks executable Adaptive Research milestones separately from gameplay VERSION. The legacy prototype research loop remains the active gameplay path until an explicit coordinated cutover.

## Current accepted baseline

- Repository: `afterburn25/stellar-continuum`
- Exclusive Adaptive Research branch for this workstream: `research/adaptive-research`
- Public Technology Possibility Graph: **370 nodes / 21 domains**
- Gameplay VERSION remains `0.0.6-dev.1`
- No Core gameplay research cutover yet
- Do not use `dev/adaptive-research` as the authoritative research branch; that ref was contaminated by a shared `integration` merge during Milestone #19 and is preserved only as historical/concurrent-integration state.

## Accepted milestones

### #13 — executable sparse runtime foundation

PR #86, merge `2b5e1e30783db67524fac3788552f667c69879c0`.

Established immutable catalog/index loading, sparse per-civilization visible state, authoritative eligibility/blockers, Effective Research Lab allocation, staged RP progression, visible-only views, starting-history composition, basic hypothesis resolution, and standalone snapshot v1.

### #14 — competence, institutions, tacit expertise and authoritative readiness

PR #115, merge `0aea9db86f74e95084e0f26430bc223afee13a5c`.

Established 36 shared knowledge fields with theoretical/experimental/engineering competence, specialist research institutions, scoped tacit knowledge, causal Project Readiness, competence growth/atrophy, `AdaptiveResearchAuthority`, and snapshot v2.

Human-like 2050 reference: 19 active competence fields, 12 Effective Research Labs, Fusion Experimental readiness 70.1/100; equal raw four-lab facility readiness measured 65/100 for general labs vs 100/100 for a matching high-energy complex.

### #15 — causal Research Pressure and fair strategic planning

PR #141, merge `7eb1ceb96a8487beccacebc805071093ad8f436d`.

Established executable causal runtime for all 59 Research Pressure rules, sparse metric/event support, five-level research agenda, 12-axis mutable scientific culture, visible-only bounded planning shortlists, natural complacency/challenger response, and snapshot v3. No hidden technology rank, weak-civilization catch-up multiplier, or leader penalty.

Validated strategic soak: one focused active Pressure record; shortlist bound 12; 0.9 sustained energy-shortage support produced Pressure 40 after one year; Deprioritized military attention at complacency index 95 reversed to Critical with legitimate challenger index 90; 1,000-year four-signal soak peaked at 2 Pressure records / 1 live metric signal and ended with a 1,413-byte strategic snapshot.

### #16 — executable foreign-technology assimilation and brokerage

PR #158, merge `9fefe82fd6fe79cfd081f0e54655070b0907c3c2`.

Established four independent foreign-technology axes (Understanding / Operability / Reproduction / Adaptation), sparse assessments/packages, evidence/tacit package ingestion, law-vs-physics transfer rights, explicit assimilation, recipient-specific brokerage value, native derivatives without source cloning, and snapshot v4.

Validated: 2 sparse assessments / 2 packages; captured drive Observed / SupportedOperation / ComponentReplication; foreign reactor EngineeringUnderstood / NativeDerivative; incompatible-holder brokerage utility 98.3/100; v4 snapshot 14,527 bytes and unchanged after a 1,000-year reassessment soak.

### #17 — foreign discovery materializes the native visible tree

PR #165, merge `38c279e498dd5695483c9680df17afbec7b3d254`.

Foreign evidence can expose public nodes as Rumored/Hypothesized without granting researchability; normal eligibility alone promotes to Investigable. Evidence-specific candidate indexes and four explicit generic cross-lineage rules avoid full graph scans. Rumored/Hypothesized nodes hide project lab costs and capability outputs. Incompatible biology may still create legitimate scientific awareness.

Validated identical human-like starts at 82 visible nodes; one characterized alien-drive contact produced 89 visible nodes while the control remained 82. Foreign Device Forensics matured normally; Reverse-Engineering Methodology promoted from Hypothesized to Investigable after Forensics matured.

### #18 — foreign-derived and hybrid engineering branches

PR #168, merge `23cfeacd4556d85b1b4c8b402bde6e635c7d1ccd`.

Expanded graph **360 -> 370 nodes**, still 21 domains. Xenoscience expanded from 14 to 24 nodes with six foreign-derived engineering branches (propulsion, power, materials, manufacturing, control systems, biosystems) and four deeper hybrid architecture branches (propulsion, power, manufacturing, biosystems).

Foreign contact/evidence can reveal derivative branches but not grant maturity. Derivatives require real cross-lineage/source-analysis knowledge. Deep hybrids are not revealed directly by contact and require Hybrid Technology Design plus mature derivative knowledge. All use ordinary RP, Effective Labs, Pressure, Project Readiness, facilities, evidence and maturation.

Validated: 92 Effective Labs in stress fixture, 19 prerequisite/branch projects matured normally, Foreign-Derived Propulsion Engineering Mature, Hybrid Propulsion Architecture Mature only after derivative + Hybrid Design, 7 derivative/hybrid nodes materialized on the exercised path, uncontacted control did not receive that path. All 12 workflows passed exact head `ff925c652b95829d8e138d3818cfd44132bffaeb`.

### #19 — deterministic experimental outcomes and side discoveries

PR #207, merge `02797ef3783d7980efdbb1dba72f4f2553e86e15`.

Milestone #19 was rebuilt on an isolated research branch after unrelated `integration` history contaminated superseded PR #192 and a concurrent process retargeted superseded PR #201. Mixed histories were preserved on archive refs; the accepted PR contained exactly nine Adaptive Research-owned files.

Established:
- deterministic/replayable outcome stream keyed by catalog + campaign seed + civilization + node + checkpoint + persisted attempt index;
- scientific hypothesis outcomes: Supported / Refined / Disproven / Anomalous / Side Discovery;
- established engineering outcomes: Progress / Partial Success / Setback / Side Discovery; fundamental disproof forbidden;
- explicit hazardous research emits factual Hazard events while physical damage/casualties remain owned by physical simulation workstreams;
- setbacks reduce only effective current-stage progress and never erase total historical RP;
- partial success grants bounded effective stage credit without fabricating RP spent;
- refined hypotheses preserve **75%** of Experimental effective progress and all total RP before another test cycle;
- disproof archives as `disproven` while adding bounded scientific competence;
- anomalous results feed the normal causal `fundamental_anomaly` Pressure signal;
- side-discovery neighborhoods are precomputed once, bounded to max 8 related public candidates, max 1 materialized node per result, no full graph scan, and never Mature by surprise;
- Project Readiness can reduce setback/hazard/noisy-anomaly risk but does not change the fundamental disproof weight of a hypothesis;
- snapshot v5 layers compact per-node summaries + bounded recent outcome records over v4; v1-v4 remain readable and attempt indexes persist to prevent save/reload rerolls.

Exact accepted head `01fbd4c9a4a217eab2bb27339348ce39fcaeabfb`: all **13** research/build workflows passed; dedicated outcome checks built with 0 warnings / 0 errors.

Measured outcomes:
- Supported Warp-Metric Theory -> Demonstrated
- Refined Warp-Metric Theory: 334.8 / 446.4 Experimental work preserved = 75%
- Disproved Warp-Metric Theory -> Archived / `disproven`
- anomalous result -> fundamental-anomaly Pressure 45
- side discovery -> `micro_field_distortion` Hypothesized, not Mature
- Systems Engineering setback: effective progress 57.5 -> 37.3 with total RP preserved
- Systems Engineering partial success: 57.5 -> 66.5 with total RP preserved
- v5 example snapshot: 18,932 bytes
- after 1,000 explicit outcome resolutions: only 8 detailed recent records retained, snapshot 20,184 bytes

## Long-horizon baseline

- 500-year same-origin Mature-tree minimum Jaccard distance: 0.457
- 1,000-year core research-state soak remains bounded
- strategic/foreign/outcome sidecars remain sparse in dedicated soaks
- no hard year limit in Adaptive Research architecture

## Known shared CI caveat

GitHub issue #61 remains outside Adaptive Research ownership: the shared Godot runtime process can exit successfully while logging failure to instantiate `res://src/Game/Presentation/Main.cs`. Do not use that process exit code alone as proof of semantic runtime health.

## Next Adaptive Research milestone

**Milestone #20 — evolving visible-tree topology, provenance and history projection.**

Make the runtime-facing research tree understandable as it changes across decades without exposing the hidden graph or forcing UI to reconstruct history from raw events.

Requirements:
- every materialized node records bounded causal provenance such as starting history, basic science, recognized need/Pressure, evidence/contact, foreign discovery, side discovery, prerequisite/capability change, or explicit migration;
- provenance answers `why did this appear?` using only information the civilization/player legitimately knows;
- stable topology anchors/group IDs allow newly visible nodes to attach near existing visible relatives without globally reflowing the tree;
- unknown nodes, unknown branch counts, and hidden placeholder slots are never exposed;
- visible edges remain only between visible nodes;
- runtime emits bounded tree deltas: node added, state changed, branch attachment changed, project state changed, archived/collapsed history changed;
- UI can retain viewport/selection across a delta without Adaptive Research owning rendering/pixels;
- Mature/Archived old branches can collapse into history summaries while active/recently changed branches remain detailed;
- history collapse is information-preserving: completed technologies/capabilities remain queryable even when old detailed display records compress;
- node provenance/history must stay sparse and bounded over 1,000+ years;
- side discoveries and foreign-derived nodes must preserve their actual causal source rather than looking like generic basic-science unlocks;
- starting profiles produce stable initial anchor topology under the same rules;
- snapshot v6, if needed, must remain backward-readable and must not duplicate the 370-node static graph;
- dedicated executable validation must prove stable anchors, hidden-tree non-leakage, correct provenance for at least starting/basic-science/Pressure/evidence/foreign/side-discovery causes, delta boundedness, history collapse, and a 1,000-year topology/provenance soak.

Adaptive Research owns the data/read-model/provenance contract. Presentation/UI owns pixel layout, animation, controls, colors, and rendering.
