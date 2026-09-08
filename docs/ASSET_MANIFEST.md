# Stellar Continuum Asset Manifest

Status: **Authoritative early-release asset registry**  
Owner: `work/visual-style-assets`

This registry prevents duplicate visual concepts, records provenance/status, and maps production-candidate visuals to the gameplay concepts that currently exist in shared integration.

## Status vocabulary

- **Production candidate** — intended for the playable build; import/runtime validation has passed, but final integrated-context review may still be pending.
- **Production ready** — validated in the actual integrated screen/background at intended sizes and approved for continued use.
- **Temporary** — intentionally provisional and expected to be replaced.
- **Superseded** — retained only for continuity/history; do not use in new work.

## Runtime visual system

| Asset / family | Path | Purpose | Provenance | Status |
|---|---|---|---|---|
| Deep-Space Instrumentation tokens | `assets/visual/ui/visual_tokens.json` | Canonical color, geometry, spacing, typography-size and motion roles | Original project-authored | Production candidate |
| Shared Godot Theme | `assets/visual/ui/stellar_continuum_theme.tres` | Project-wide PanelContainer/Button/Label treatment via `gui/theme/custom` | Original project-authored from tokens | Production candidate |
| Runtime palette | `src/Game/Presentation/VisualPalette.cs` | Semantic token-color mirror for direct-drawn presentation | Original project-authored | Production candidate |
| Runtime icon loader | `src/Game/Presentation/VisualIconLibrary.cs` | Lazy cached loading of committed SVG assets through stable `res://` paths | Original project-authored | Production candidate |
| Strategic visual map overlay | `src/Game/Presentation/Main.VisualMap.cs` | Shape-based survey, colony and player-fleet overlays using authoritative fair-information state | Original project-authored | Production candidate |
| Integrated map hook | `src/Game/Presentation/IntegratedMain.Visuals.cs` | Adds the visual map overlay after the inherited strategic draw pass | Original project-authored | Production candidate |
| Procedural main-menu backdrop | `src/Game/Presentation/MainMenuBackdrop.cs` | Deterministic star field, orbital arcs, distant stellar focus and planetary limb | Original project-authored | Production candidate |
| Main-menu presentation | `src/Game/Presentation/MainMenuLayer.cs` | Existing Continue/New Game/Quit behavior with v1 hierarchy/colors over procedural background | Original project-authored | Production candidate |
| Rendered visual QA record | `docs/SCREENSHOT_VISUAL_QA_2026-09-08.md` | Findings from real Godot screenshot run `34253094688` | Project QA record | Current |

The startup menu is intentionally above gameplay HUD layers (`MainMenuLayer` CanvasLayer 100) so dynamic gameplay labels cannot render through the menu.

## SVG technical contract

All production-candidate icons are original project-authored vectors with:

- `24 x 24` SVG viewBox and declared size;
- approximately 2-unit safe padding;
- `1.8` optical root stroke;
- rounded caps/joins;
- neutral `#E6F0F6` source color for semantic tint/modulate at runtime;
- transparent background;
- no embedded text, script, raster image, external href, or data URI.

They are validated by `scripts/validate_visual_assets.py` and have been rendered at 16/20/24/32/48 px during development.

## Core strategic icons — 16

Directory: `assets/visual/icons/core/`

| Asset | Filename | Current concept |
|---|---|---|
| Pause | `icon_hud_pause.svg` | Simulation pause/resume control |
| Speed | `icon_hud_speed.svg` | Simulation speed vocabulary |
| Save | `icon_hud_save.svg` | Save campaign |
| Support | `icon_hud_support.svg` | Support/diagnostics/help |
| Research | `icon_action_research.svg` | Generic research action |
| Construction | `icon_action_construction.svg` | Generic construction action |
| Exploration | `icon_system_exploration.svg` | Exploration/survey system |
| Logistics | `icon_system_logistics.svg` | Logistics network/system |
| Relations | `icon_system_relations.svg` | Diplomacy/relations system |
| Colony | `icon_map_colony.svg` | Established settlement/colony |
| Scout | `icon_map_scout.svg` | Scout role; reused by ship-role family |
| Information | `icon_status_info.svg` | Informational/inspection state |
| Warning | `icon_status_warning.svg` | Caution/strategic threat |
| Success | `icon_status_success.svg` | Success/completed/accept state |
| Unknown | `icon_status_unknown.svg` | Unknown/unresolved; reused by survey/diplomacy |
| Hostile | `icon_status_hostile.svg` | Hostile/dangerous; reused by diplomacy/combat |

Current runtime consumption includes PlayerControls, ExplorationMissionPanel, SystemInspectionPanel, LogisticsNetworkPanel, RelationsPanel, main strategic map markers, and fleet/colony overlays.

## Economy resource icons — 3

Directory: `assets/visual/icons/resources/`

This family intentionally mirrors only resources present in `CivilizationEconomyState`; no speculative food/supply/mineral family is pre-created.

| Asset | Filename | Authoritative relationship |
|---|---|---|
| Credits | `icon_resource_credits.svg` | `Credits` / `LastCreditsPerSecond` |
| Industry | `icon_resource_industry.svg` | `Industry` / `LastIndustryPerSecond` |
| Science | `icon_resource_science.svg` | `Science` / `LastSciencePerSecond` |

## Construction project icons — 5

Directory: `assets/visual/icons/construction/`

This family mirrors the current `ConstructionRegistry` exactly.

| Asset | Filename | Registry ID |
|---|---|---|
| Planetary Research Network | `icon_construction_research_network.svg` | `research_network` |
| Industrial Automation Program | `icon_construction_industrial_automation.svg` | `industrial_automation` |
| Orbital Launch Complex | `icon_construction_orbital_launch_complex.svg` | `orbital_launch_complex` |
| Orbital Shipyard | `icon_construction_orbital_shipyard.svg` | `orbital_shipyard` |
| Warp Test Facility | `icon_construction_warp_test_facility.svg` | `warp_test_facility` |

## Ship-role icons — 3 new + shared Scout

Directory: `assets/visual/icons/ships/` plus shared Scout.

| Asset | Filename | Registry/role |
|---|---|---|
| Scout | `icon_map_scout.svg` | `warp_scout` / `FleetRole.Scout` |
| Science vessel | `icon_ship_science_vessel.svg` | `science_vessel` / `FleetRole.Science` |
| Patrol corvette | `icon_ship_patrol_corvette.svg` | `patrol_corvette` / `FleetRole.Military` |
| Colony ship | `icon_ship_colony_ship.svg` | `colony_ship` / `FleetRole.Colony` |

The Scout asset is deliberately reused rather than cloned. The map overlay renders all four player fleet roles with distinct silhouettes and role colors; route lines remain derived from legitimate own-fleet destination state.

## Survey map-state icons — 3 new + shared Unknown

Directory: `assets/visual/icons/map/` plus shared Unknown.

| State | Filename | Authoritative relationship |
|---|---|---|
| Unknown | `icon_status_unknown.svg` | `SystemSurveyLevel.Unknown` |
| Detected | `icon_map_detected.svg` | `SystemSurveyLevel.Detected` |
| Partially Surveyed | `icon_map_partially_surveyed.svg` | `SystemSurveyLevel.PartiallySurveyed` |
| Fully Surveyed | `icon_map_fully_surveyed.svg` | `SystemSurveyLevel.FullySurveyed` |

Runtime rule: common-catalog Unknown stars remain visually quiet at map scale; a selected Unknown target receives the explicit Unknown icon. Detected/Partial/Full states use dedicated shapes so survey progress is not brightness/color-only.

## Diplomacy icons — 10 + shared Unknown/Hostile

Directory: `assets/visual/icons/diplomacy/`.

| Concept | Filename | Authoritative relationship |
|---|---|---|
| Contact | `icon_diplomacy_contact.svg` | `ContactAwareness` / observer-visible contact |
| Peace | `icon_diplomacy_peace.svg` | `DiplomaticPoliticalState.Peace` / peace agreement |
| War | `icon_diplomacy_war.svg` | `DiplomaticPoliticalState.AtWar` |
| Ceasefire | `icon_diplomacy_ceasefire.svg` | `DiplomaticPoliticalState.Ceasefire` / ceasefire agreement |
| Access granted | `icon_diplomacy_access_granted.svg` | `AccessPermission.Granted` |
| Access denied | `icon_diplomacy_access_denied.svg` | `AccessPermission.Denied` |
| Trade | `icon_diplomacy_trade.svg` | Trade agreement/offer |
| Agreement | `icon_diplomacy_agreement.svg` | Generic agreement/proposal |
| Claim | `icon_diplomacy_claim.svg` | `TerritorialClaimSnapshot` |
| Dispute | `icon_diplomacy_dispute.svg` | `TerritorialClaimResponse.Disputed` |

Shared `icon_status_unknown.svg` and `icon_status_hostile.svg` remain the canonical Unknown/Hostile political-state symbols. The Relations panel currently consumes contact/proposal/peace/ceasefire/accept/reject vocabulary without altering diplomatic rules.

## Combat icons — 6 + shared Warning

Directory: `assets/visual/icons/combat/`.

| Concept | Filename | Authoritative relationship |
|---|---|---|
| Hold | `icon_combat_hold.svg` | `MilitaryOrderType.Hold` |
| Defend | `icon_combat_defend.svg` | `MilitaryOrderType.Defend` |
| Attack | `icon_combat_attack.svg` | `MilitaryOrderType.Attack` |
| Retreat | `icon_combat_retreat.svg` | `MilitaryOrderType.Retreat` / retreat events |
| Damage | `icon_combat_damage.svg` | `CombatEventType.DamageApplied` |
| Destroyed | `icon_combat_destroyed.svg` | `CombatEventType.FleetDestroyed` |

Shared `icon_status_warning.svg` remains the canonical strategic threat symbol.

## Current production-candidate screen treatment

The main menu uses `MainMenuBackdrop.cs`: a deterministic 92-star procedural field, restrained orbital arcs around a distant stellar focus, and a dark lower-left planetary limb. It uses no large texture, continuous animation, fake interface text, third-party art, or hidden gameplay data.

Existing menu controls and actions remain UI-owned and unchanged in function. The visual workstream only changes hierarchy, color, theme, backdrop, and z-order needed to keep the overlay visually intact.

## Known temporary / fallback presentation

- `Main.cs` still draws legacy prototype star circles, colony rings and some fleet marks underneath the new `Main.VisualMap.cs` overlays. These are temporary fallback marks until captured-context review confirms the new shapes are sufficient by themselves.
- `Main.Shipbuilding.cs` still owns a dedicated Shipbuilding HUD line and legacy science-marker presentation. A screenshot-confirmed layout collision between that HUD and PlayerControls is reported to UI workstream #27 rather than silently repositioned here.
- Godot fallback font remains temporary; no font with uncertain redistribution terms has been added.
- `Stellar Continuum` remains a working title; no trademark symbol or supposedly cleared commercial logo is authorized by this manifest.

## Research iconography boundary

Technology-specific icons remain intentionally deferred. Shared integration still contains a small legacy technology registry while Adaptive Research #179 owns an evolving possibility graph. Visual work will wait for a stable presentation/category contract rather than hardening transitional technology IDs or creating hundreds of speculative icons.

## Provenance rules

Production assets must be one of:

- original project-authored vectors/code;
- original generated artwork with generation provenance recorded;
- project-owned commissioned/contributed artwork with rights recorded;
- appropriately licensed third-party material with license/source recorded.

Public visibility on the web is not sufficient provenance.

For future generated raster art, record generator/tool family, creation date, intended crop/aspect ratio, and whether the committed file is source-quality or runtime-optimized.

## Validation

Run:

```bash
python3 scripts/validate_visual_assets.py
```

The normal `work/**` build validates the visual contract and runs Godot 4.7.2 headless editor/runtime smoke tests. `.github/workflows/screenshots.yml` also runs on `work/visual-style-assets` so real integrated main-menu/campaign/colony/relations frames are available for visual review.

No icon or coherent visual family is promoted to **Production ready** solely because it imports; integrated rendered-context review is required.
