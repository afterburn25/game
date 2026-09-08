# Stellar Continuum Asset Manifest

Status: **Authoritative early-release asset registry**

Owner: `work/visual-style-assets`

This manifest prevents duplicate visual concepts and records provenance for production assets. Before adding a new production visual, search this document and the existing asset tree.

## Status vocabulary

- **Production candidate** — intended for the playable build; must pass import/legibility validation.
- **Production ready** — validated in the integrated game at intended sizes/usages.
- **Temporary** — intentionally provisional and expected to be replaced.
- **Superseded** — retained only for continuity/history; do not use in new work.

## Visual resource families

| Asset / family | Repository path | Category | Purpose | Dimensions | Format | Provenance | Status | Intended usage |
|---|---|---|---|---|---|---|---|---|
| Deep-Space Instrumentation tokens | `assets/visual/ui/visual_tokens.json` | UI system | Canonical colors, spacing, geometry, typography sizes and motion timing | n/a | JSON | Original project-authored | Production candidate | Shared source for visual values; UI implementations should consume/translate rather than invent variants |
| Stellar Continuum core Theme | `assets/visual/ui/stellar_continuum_theme.tres` | UI system | Shared Godot treatment for PanelContainer, Button and Label | n/a | Godot Theme `.tres` | Original project-authored from visual tokens | Production candidate | Project-wide runtime default via `gui/theme/custom`; layout remains UI-owned |
| Core strategic icon family | `assets/visual/icons/core/*.svg` | Iconography | First production symbols for current HUD, strategic systems, map markers and statuses | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Buttons, compact labels, strategic map overlays, status indicators |
| Core economy resource icon family | `assets/visual/icons/resources/*.svg` | Economy iconography | Canonical icons for the three resources currently exposed by `CivilizationEconomyState` | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Economy HUD, logistics/economy panels, costs and production readouts |
| Early-release construction project icon family | `assets/visual/icons/construction/*.svg` | Construction iconography | Canonical symbols for the five currently registered construction projects | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Construction chooser, active-project/status rows and project prerequisites |
| Early-release ship-role icon family | `assets/visual/icons/ships/*.svg` plus Scout in `assets/visual/icons/core/icon_map_scout.svg` | Ship/fleet iconography | Distinct silhouettes for all four currently registered first-generation ship roles | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Shipyard/design lists, fleet markers, mission/status rows |

## Core strategic icon family

All icons use the shared 24-unit grid, approximately 2-unit safe padding, 1.8-unit optical stroke, rounded caps/joins and neutral `#E6F0F6` source color so consuming UI may tint/modulate them by semantic state.

| Asset | Path | Concept | Current implementation relationship | Status |
|---|---|---|---|---|
| Pause | `assets/visual/icons/core/icon_hud_pause.svg` | Pause simulation | Current top control has Pause text button | Production candidate |
| Simulation speed | `assets/visual/icons/core/icon_hud_speed.svg` | Increase/indicate simulation speed | Current top control has x1/x5/x20 text buttons | Production candidate |
| Save | `assets/visual/icons/core/icon_hud_save.svg` | Save game | Current top control has Save text button | Production candidate |
| Support / information | `assets/visual/icons/core/icon_hud_support.svg` | Support/help/info entry | Current top control has Support text button | Production candidate |
| Research | `assets/visual/icons/core/icon_action_research.svg` | Research/science action | Current top control exposes research action | Production candidate |
| Construction | `assets/visual/icons/core/icon_action_construction.svg` | Construction action | Current top control exposes build action | Production candidate |
| Exploration | `assets/visual/icons/core/icon_system_exploration.svg` | Exploration / survey system | Current Exploration/Colonization panel | Production candidate |
| Logistics | `assets/visual/icons/core/icon_system_logistics.svg` | Logistics network/system | Current Logistics Network panel | Production candidate |
| Relations | `assets/visual/icons/core/icon_system_relations.svg` | Diplomacy / relations system | Current Relations panel | Production candidate |
| Colony | `assets/visual/icons/core/icon_map_colony.svg` | Established colony / settlement | Current map uses colored colony rings | Production candidate |
| Scout | `assets/visual/icons/core/icon_map_scout.svg` | Scout / exploration fleet role | Current map uses colored fleet circles; also completes the ship-role family | Production candidate |
| Information | `assets/visual/icons/core/icon_status_info.svg` | Informational state | Shared UI status vocabulary | Production candidate |
| Warning | `assets/visual/icons/core/icon_status_warning.svg` | Caution / attention state | Shared UI status vocabulary | Production candidate |
| Success | `assets/visual/icons/core/icon_status_success.svg` | Success / completed state | Shared UI status vocabulary | Production candidate |
| Unknown | `assets/visual/icons/core/icon_status_unknown.svg` | Unknown / unresolved state | Fog/survey/diplomacy unknown state | Production candidate |
| Hostile | `assets/visual/icons/core/icon_status_hostile.svg` | Hostile / dangerous state | Combat/diplomacy/map hostile state | Production candidate |

## Core economy resource icon family

This family intentionally contains only resources currently present in the integrated playable economy model: Credits, Industry and Science. It does not pre-create food, supply, minerals or other future resources.

| Asset | Path | Concept | Authoritative implementation relationship | Status |
|---|---|---|---|---|
| Credits | `assets/visual/icons/resources/icon_resource_credits.svg` | Liquid economic/currency stock and income | `CivilizationEconomyState.Credits` / `LastCreditsPerSecond` | Production candidate |
| Industry | `assets/visual/icons/resources/icon_resource_industry.svg` | Industrial capacity/stock and production | `CivilizationEconomyState.Industry` / `LastIndustryPerSecond` | Production candidate |
| Science | `assets/visual/icons/resources/icon_resource_science.svg` | Scientific output/stock | `CivilizationEconomyState.Science` / `LastSciencePerSecond` | Production candidate |

## Early-release construction project icon family

This family mirrors the five projects currently registered by `ConstructionRegistry`. It is intentionally limited to present playable construction rather than future infrastructure concepts.

| Asset | Path | Concept | Authoritative implementation relationship | Status |
|---|---|---|---|---|
| Planetary Research Network | `assets/visual/icons/construction/icon_construction_research_network.svg` | Distributed science/research infrastructure | `research_network` | Production candidate |
| Industrial Automation Program | `assets/visual/icons/construction/icon_construction_industrial_automation.svg` | Automated industrial/fabrication infrastructure | `industrial_automation` | Production candidate |
| Orbital Launch Complex | `assets/visual/icons/construction/icon_construction_orbital_launch_complex.svg` | Heavy-lift access to orbit | `orbital_launch_complex` | Production candidate |
| Orbital Shipyard | `assets/visual/icons/construction/icon_construction_orbital_shipyard.svg` | Orbital vessel assembly facility | `orbital_shipyard` | Production candidate |
| Warp Test Facility | `assets/visual/icons/construction/icon_construction_warp_test_facility.svg` | Hardened field/warp experimental facility | `warp_test_facility` | Production candidate |

## Early-release ship-role icon family

The integrated `ShipDesignRegistry` currently exposes exactly one design per role. The existing core Scout symbol is reused rather than duplicated.

| Asset | Path | Role / design | Authoritative implementation relationship | Status |
|---|---|---|---|---|
| Scout | `assets/visual/icons/core/icon_map_scout.svg` | Scout / Pathfinder Scout | `warp_scout` / `FleetRole.Scout` | Production candidate |
| Science vessel | `assets/visual/icons/ships/icon_ship_science_vessel.svg` | Science / Deep-Space Science Vessel | `science_vessel` / `FleetRole.Science` | Production candidate |
| Patrol corvette | `assets/visual/icons/ships/icon_ship_patrol_corvette.svg` | Military / Patrol Corvette | `patrol_corvette` / `FleetRole.Military` | Production candidate |
| Colony ship | `assets/visual/icons/ships/icon_ship_colony_ship.svg` | Colony / Interstellar Colony Ship | `colony_ship` / `FleetRole.Colony` | Production candidate |

## Current prototype visuals not yet production assets

The following are implementation treatments, not reusable art assets:

- direct-drawn star circles and survey-state colors in `src/Game/Presentation/Main.cs`;
- direct-drawn colony rings, fleet circles and route lines in `src/Game/Presentation/Main.cs`;
- Godot fallback font;
- programmatic prototype panel/layout structure (now inheriting the shared project Theme);
- plain working-title text in `MainMenuLayer.cs`;
- dark main-menu overlay color.

They are intentionally **not** given production asset entries until replaced or formalized.

## Provenance rules

Production assets must be one of:

- original project-authored vectors/code;
- original generated artwork with generation provenance recorded;
- project-owned commissioned/contributed artwork with rights recorded;
- appropriately licensed third-party material with license and source recorded.

Public visibility on the web is not sufficient provenance.

For generated raster art, add the generator/tool family, creation date, intended crop/aspect ratio, and whether the committed file is source-quality or runtime-optimized.

## Source vs runtime

The v1 SVG icons are both editable source and runtime assets because they are compact hand-authored vectors.

If future raster imagery requires a high-resolution source master plus optimized runtime WebP/PNG, list both paths and their relationship here.

## Validation

Run:

```bash
python3 scripts/validate_visual_assets.py
```

The main build workflow also runs on `work/**` branches and performs Godot 4.7.2 headless editor/runtime smoke tests, which provides the import-level gate after assets are committed.

An icon is not **Production ready** until it has additionally been viewed at the practical UI sizes and in the relevant integrated screen/background context.
