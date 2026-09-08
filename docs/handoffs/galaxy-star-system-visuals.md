# Galaxy / Star-System Visuals handoff

Date: 2026-09-08

## Continuation and recovery

- Current branch: `work/galaxy-star-system-visuals` (ACTIVE), existing PR #220 to `integration`.
- Canonical child: none. The recovered branch inventory has no other Galaxy/spatial continuation branch.
- Accepted comparison baseline: `integration` at `c529a1a765776c0940f88002410bc70db740d05a`.
- Recovered candidate: `f546d0f77d845322a16c21a16e8d73c76df3d0b4`, one unique commit over that baseline. Its seven-file spatial foundation has not yet been accepted into integration.
- Related records reviewed: workstream #219 and its progress/readiness/direction comments; PR #220 (no review comments); child data issue #221 and its simulation-completion/habitability comment; `SPATIAL_PRESENTATION.md`; `ENGINEERING_GUARDRAILS.md`.
- Historical branch/PR validation reports are evidence for the recovered candidate only. They do not validate this follow-up or substitute for runtime error checks.

## Completed in this review

The recovered foundation supplies deterministic observer-safe star/planet/moon geometry and stellar-region/system navigation. The focused follow-up fixes:

- system-entry double-click interception before GUI handling and without checking the selected star under the pointer;
- Ctrl+right-click science orders passing through the system canvas or other UI controls;
- celestial-object double-clicks incorrectly triggering an empty-space return;
- stale context across observer/campaign identity changes and indefinitely stale visible facts at unchanged survey progress;
- stellar-coordinate science labels appearing above the system canvas as if they were local positions;
- the full strategic draw pass running underneath the opaque system canvas;
- unknown stellar archetypes rendered as ordinary yellow stars;
- a minimum display scale that could clip large schematic systems; shared rendering/hit-test transform now fits the current extent;
- repeated moon-parent dictionary allocation on redraw.

The one-view cache has a one-second ordinary refresh interval and immediate confidence-gate refresh. Campaign reset clears it synchronously. No simulation state, exploration-authority logic, save format, astronomy data, tokens or icon assets changed.

## Checks

- `git diff --check`: passed.
- New quality regressions cover observer/campaign/selection invalidation, bounded refresh and immediate confidence changes, celestial versus empty-space hit tests, large-system fit, and real `ExplorationReadModel` hidden-environment perturbation versus legitimate signature changes at unchanged survey progress.
- Pure-C# spatial regression execution: passed. Compiled every actual `src/Game/Simulation` source plus the three pure spatial classes and `SpatialPresentationValidation.cs` directly with SDK 10.0.302 Roslyn against installed .NET 8.0.29 reference assemblies; ran on .NET 8.0.29. This bypasses package restore without stubbing simulation types. Four pre-existing nullable warnings came from Colonization/Exploration sources; no compilation errors. Logs and response file are in workspace `work/galaxy-pure-checks/`.
- Full Godot Release build and full quality runner: pending CI validation. Child execution cannot read the global NuGet configuration; the permission granted to Core does not propagate to this child. Failure occurs before compilation, documented in workspace `work/galaxy-build-before.log` and `work/galaxy-build-permission.log`. Core also reports network-blocked package restore after isolating its profile.
- Local Godot input/render execution: not performed; no engine executable available. Input routing has been reviewed against `_Input`, GUI, and `_UnhandledInput` paths. The pure tests do not claim to verify Godot event dispatch.

## Dependencies and shared interfaces

- Exploration owns the authoritative observer-safe body read model. Core approved campaign/observer identity plus bounded refresh for this wave. A selected-system read API with a knowledge/content revision is requested for the next performance milestone; do not duplicate Exploration filtering in presentation.
- Visual Style / Assets owns PR #218 and shared resources. This branch consumes no unmerged asset copies. On integration, preserve the spatial visibility guard in `Main.Shipbuilding.cs`, the spatial draw guard in `Main.cs`, and the existing `UiIsSystemSpatialView`/scale hooks.
- UI owns panels and global navigation controls. Pointer navigation now respects GUI consumption. Existing `UiSpatialScale`, `UiIsSystemSpatialView`, and `UiSpatialScaleLabel` remain available.
- Testing / Release is repairing the shared false-green Godot runtime gate and should independently validate the combined branch before integration acceptance.

## Known limitations and integration request

This remains a candidate for Core review, with no push or merge performed by this specialist. Core authorized committing the scoped fix for exact-tree publication to the existing branch and real CI. Full Release build, quality validation and actual Godot event/runtime checks remain necessary before acceptance. The one-second full exploration refresh is bounded but its 500/2,000-system cost has not been benchmarked; selected-system/revision support remains the preferred solution. Rendering fit does not guarantee crowded labels never overlap.

Planet/moon selection, system-local fleet/colony/station placement, operational/claims/combat overlays and strategic-map batching/label density remain follow-up scope. Issue #221 astronomy import remains separate and must preserve observed-versus-simulation provenance.

## Next milestone

After Core acceptance and Testing validation, consume the integrated visual resources and establish Exploration's selected-system read/revision interface. Then replace the legacy strategic marker pass with a fog-safe dedicated map projection/canvas and benchmark 500/2,000-system interaction. Resume #221 only as an explicitly scoped data milestone with its existing truth/provenance rules.
