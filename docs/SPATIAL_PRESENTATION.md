# Galaxy and Star-System Spatial Presentation

Status: **early-release milestone 1 implementation contract**

Owner: `work/galaxy-star-system-visuals`

## Purpose

The spatial presentation layer turns authoritative simulation state into a strategically readable Godot view. It does not own star systems, planetary bodies, fleets, colonies, survey state, claims, logistics, movement or combat.

The first implemented hierarchy is:

`STELLAR REGION -> STAR SYSTEM`

A selected system with at least reconnaissance-grade knowledge can be opened by double-clicking it. Double-clicking empty system space returns to the stellar map while preserving the selected-system ID plus the stellar map's existing pan/zoom context.

## Fair-information boundary

System visualization consumes `ExplorationReadModel`, not raw `GalaxyState.PlanetaryBodies`.

That distinction is deliberate:

- **Detected:** star target only; system spatial view is not available.
- **Partially surveyed / reconnaissance:** basic orbital catalog is available (body identity, parentage, orbit order, broad body kind and approximate radius), but body visual class remains explicitly unknown. Positive resource/anomaly/activity signatures may be shown only where the read model exposes a positive reconnaissance signature. Absence is never inferred.
- **Fully surveyed:** the presentation may derive broad visual classes from the now-legitimate environment fields exposed by the read model.

The system projection does not retain mass, gravity, temperature, pressure, resource values or native-civilization facts in its render marker records. It retains only geometry, an allowed visual class and positive signature flags needed to draw the current view.

## Simulation distance vs display distance

Stellar Continuum spans incompatible physical scales. Milestone 1 therefore uses explicit schematic display scaling.

### Simulation distance

Authoritative travel/movement remains whatever the simulation and logistics systems calculate. This branch does not change it.

### Stellar-map display distance

The existing strategic map continues to use simulation-owned star positions transformed by the existing pan/zoom camera.

### Star-system display distance

Planetary orbit spacing is **schematic**:

- first planet display orbit: 94 presentation units;
- subsequent planet display orbits: +68 units by authoritative orbit index;
- moon display orbits: 17 units plus 8 units per moon orbit index, with a small allowance for the displayed parent radius.

These values are not kilometers, AU, transfer windows or travel time. They exist only to make orbital hierarchy readable.

### Object display scale

Planet/moon screen radii use a bounded square-root transform of the observer-safe approximate Earth-radius value. A body is therefore recognizable without claiming that rendered diameter and rendered orbital radius share one physical scale.

### Icon / marker scale

Strategic signatures use fixed-size presentation shapes so they remain legible when celestial objects are small. A marker never changes the underlying simulation fact.

## Determinism

Body placement angle is deterministically derived from stable body ID. The same known system therefore keeps the same schematic layout across redraws and reloads as long as the authoritative body identity/catalog is unchanged.

The presentation does not serialize orbital display geometry. It is reconstructible.

## Rendering and performance

Milestone 1 renders only the currently opened star system in body detail. The distant stellar map remains the lower-detail strategic representation.

The system snapshot is rebuilt only when entering a system or when its survey level/progress changes; ordinary frames redraw from the compact presentation snapshot. This avoids repeatedly rebuilding the full observer-local exploration read model on every camera/render frame.

Future scale work should continue toward:

- batched/instanced strategic star markers for very large visible catalogs;
- zoom-dependent label density;
- pooled fleet/colony markers;
- region culling / spatial indexing;
- authoritative operational-range, claim and combat overlays supplied by their owning systems;
- planet/moon selection and closer orbital-infrastructure detail after the required simulation/read interfaces are stable.

## Visual language

The current system canvas follows the `work/visual-style-assets` **Deep-Space Instrumentation** standard: deep navy canvas, restrained keylines, cyan selection/focus, explicit unknown treatment and shape-based positive signatures.

Until the visual-assets PR is integrated, this milestone uses matching in-code presentation colors and simple geometric temporary markers rather than copying or vendoring unmerged assets. Once the shared theme/icons are present on `integration`, this workstream should consume those stable resources directly.
