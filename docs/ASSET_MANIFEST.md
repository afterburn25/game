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
| Runtime visual palette | `src/Game/Presentation/VisualPalette.cs` | UI/map rendering | Central runtime mirror of semantic visual-token colors for direct-drawn presentation | n/a | C# | Original project-authored | Production candidate | Main-menu/map procedural rendering; avoids scattered color literals |
| Procedural main-menu backdrop | `src/Game/Presentation/MainMenuBackdrop.cs` | Background / branding | Deterministic deep-space background with star field, distant stellar focus/orbital arcs and a planetary limb | Viewport-adaptive | C# procedural drawable | Original project-authored | Production candidate | Main startup menu; no external texture, animation loop, fake UI text or hidden gameplay data |
| Core strategic icon family | `assets/visual/icons/core/*.svg` | Iconography | First production symbols for current HUD, strategic systems, map markers and statuses | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Buttons, compact labels, strategic map overlays, status indicators |
| Core economy resource icon family | `assets/visual/icons/resources/*.svg` | Economy iconography | Canonical icons for the three resources currently exposed by `CivilizationEconomyState` | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Economy HUD, logistics/economy panels, costs and production readouts |
| Early-release construction project icon family | `assets/visual/icons/construction/*.svg` | Construction iconography | Canonical symbols for the five currently registered construction projects | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Construction chooser, active-project/status rows and project prerequisites |
| Early-release ship-role icon family | `assets/visual/icons/ships/*.svg` plus Scout in `assets/visual/icons/core/icon_map_scout.svg` | Ship/fleet iconography | Distinct silhouettes for all four currently registered first-generation ship roles | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Shipyard/design lists, fleet markers, mission/status rows |
| Survey knowledge map-state family | `assets/visual/icons/map/*.svg` plus Unknown in `assets/visual/icons/core/icon_status_unknown.svg` | Exploration/map iconography | Shape-based Detected / Partially Surveyed / Fully Surveyed progression matching authoritative knowledge state | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Galaxy/system map markers, legends and survey rows without color-only encoding |
| Diplomacy state/action family | `assets/visual/icons/diplomacy/*.svg` plus shared Unknown/Hostile status icons | Diplomacy iconography | Current contact, political, access, agreement/trade and territorial-claim concepts | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Relations panel, proposal/agreement rows, map claims and political-state indicators |
| Combat order/state family | `assets/visual/icons/combat/*.svg` plus shared Warning icon for threat | Combat iconography | Current military orders and persistent/meaningful combat outcomes | 24x24 viewBox; tested target sizes 16/20/24/32/48 px | SVG | Original project-authored vector artwork | Production candidate | Combat orders, fleet status, battle summaries and damage/destruction states |

## Icon technical contract

All production-candidate SVG icons use the shared `24 x 24` viewBox, approximately 2-unit safe padding, `1.8` optical stroke, rounded caps/joins and neutral `#E6F0F6` source color so consuming UI may tint/modulate them by semantic state. They are contract-checked by `scripts/validate_visual_assets.py` and visually rendered at 16/20/24/32/48 px before being listed here.

## Core strategic icon family

| Asset | Path | Concept / current relationship | Status |
|---|---|---|---|
| Pause | `assets/visual/icons/core/icon_hud_pause.svg` | Pause simulation | Production candidate |
| Simulation speed | `assets/visual/icons/core/icon_hud_speed.svg` | Simulation-speed controls | Production candidate |
| Save | `assets/visual/icons/core/icon_hud_save.svg` | Save game | Production candidate |
| Support / information | `assets/visual/icons/core/icon_hud_support.svg` | Support/help/info entry | Production candidate |
| Research | `assets/visual/icons/core/icon_action_research.svg` | Generic research action | Production candidate |
| Construction | `assets/visual/icons/core/icon_action_construction.svg` | Generic construction action | Production candidate |
| Exploration | `assets/visual/icons/core/icon_system_exploration.svg` | Exploration / survey system | Production candidate |
| Logistics | `assets/visual/icons/core/icon_system_logistics.svg` | Logistics network/system | Production candidate |
| Relations | `assets/visual/icons/core/icon_system_relations.svg` | Diplomacy / relations system | Production candidate |
| Colony | `assets/visual/icons/core/icon_map_colony.svg` | Established colony / settlement | Production candidate |
| Scout | `assets/visual/icons/core/icon_map_scout.svg` | Scout / exploration fleet role; reused by ship-role family | Production candidate |
| Information | `assets/visual/icons/core/icon_status_info.svg` | Informational state | Production candidate |
| Warning | `assets/visual/icons/core/icon_status_warning.svg` | Caution / attention / strategic threat | Production candidate |
| Success | `assets/visual/icons/core/icon_status_success.svg` | Success / completed state | Production candidate |
| Unknown | `assets/visual/icons/core/icon_status_unknown.svg` | Unknown / unresolved; reused by survey/diplomacy | Production candidate |
| Hostile | `assets/visual/icons/core/icon_status_hostile.svg` | Hostile / dangerous; reused by diplomacy/combat | Production candidate |

## Core economy resource icon family

This family intentionally contains only resources currently present in the integrated playable economy model. It does not pre-create food, supply, minerals or other future resources.

| Asset | Path | Authoritative implementation relationship | Status |
|---|---|---|---|
| Credits | `assets/visual/icons/resources/icon_resource_credits.svg` | `CivilizationEconomyState.Credits` / `LastCreditsPerSecond` | Production candidate |
| Industry | `assets/visual/icons/resources/icon_resource_industry.svg` | `CivilizationEconomyState.Industry` / `LastIndustryPerSecond` | Production candidate |
| Science | `assets/visual/icons/resources/icon_resource_science.svg` | `CivilizationEconomyState.Science` / `LastSciencePerSecond` | Production candidate |

## Early-release construction project icon family

This family mirrors the five projects currently registered by `ConstructionRegistry`.

| Asset | Path | Registry ID | Status |
|---|---|---|---|
| Planetary Research Network | `assets/visual/icons/construction/icon_construction_research_network.svg` | `research_network` | Production candidate |
| Industrial Automation Program | `assets/visual/icons/construction/icon_construction_industrial_automation.svg` | `industrial_automation` | Production candidate |
| Orbital Launch Complex | `assets/visual/icons/construction/icon_construction_orbital_launch_complex.svg` | `orbital_launch_complex` | Production candidate |
| Orbital Shipyard | `assets/visual/icons/construction/icon_construction_orbital_shipyard.svg` | `orbital_shipyard` | Production candidate |
| Warp Test Facility | `assets/visual/icons/construction/icon_construction_warp_test_facility.svg` | `warp_test_facility` | Production candidate |

## Early-release ship-role icon family

The integrated `ShipDesignRegistry` currently exposes exactly one design per role. The existing core Scout symbol is reused rather than duplicated.

| Asset | Path | Role / design | Status |
|---|---|---|---|
| Scout | `assets/visual/icons/core/icon_map_scout.svg` | `warp_scout` / `FleetRole.Scout` / Pathfinder Scout | Production candidate |
| Science vessel | `assets/visual/icons/ships/icon_ship_science_vessel.svg` | `science_vessel` / `FleetRole.Science` / Deep-Space Science Vessel | Production candidate |
| Patrol corvette | `assets/visual/icons/ships/icon_ship_patrol_corvette.svg` | `patrol_corvette` / `FleetRole.Military` / Patrol Corvette | Production candidate |
| Colony ship | `assets/visual/icons/ships/icon_ship_colony_ship.svg` | `colony_ship` / `FleetRole.Colony` / Interstellar Colony Ship | Production candidate |

## Survey knowledge map-state family

The authoritative knowledge model uses four ordered survey levels. Unknown reuses the existing shared unknown symbol; the other three states have dedicated shapes so the sequence remains distinguishable without color.

| Asset | Path | Survey state | Status |
|---|---|---|---|
| Unknown | `assets/visual/icons/core/icon_status_unknown.svg` | `SystemSurveyLevel.Unknown` | Production candidate |
| Detected | `assets/visual/icons/map/icon_map_detected.svg` | `SystemSurveyLevel.Detected` | Production candidate |
| Partially Surveyed | `assets/visual/icons/map/icon_map_partially_surveyed.svg` | `SystemSurveyLevel.PartiallySurveyed` | Production candidate |
| Fully Surveyed | `assets/visual/icons/map/icon_map_fully_surveyed.svg` | `SystemSurveyLevel.FullySurveyed` | Production candidate |

## Diplomacy state/action family

These symbols mirror current `DiplomacySystem` contracts. Unknown and Hostile reuse shared status symbols. Peace, At War and Ceasefire have separate silhouettes, so political state is not a red/green-only signal.

| Asset | Path | Authoritative relationship | Status |
|---|---|---|---|
| Contact | `assets/visual/icons/diplomacy/icon_diplomacy_contact.svg` | `ContactAwareness` / contact views | Production candidate |
| Peace | `assets/visual/icons/diplomacy/icon_diplomacy_peace.svg` | `DiplomaticPoliticalState.Peace` / peace agreement | Production candidate |
| Hostile | `assets/visual/icons/core/icon_status_hostile.svg` | `DiplomaticPoliticalState.Hostile` / `ContactCondition.Hostile` | Production candidate |
| War | `assets/visual/icons/diplomacy/icon_diplomacy_war.svg` | `DiplomaticPoliticalState.AtWar` | Production candidate |
| Ceasefire | `assets/visual/icons/diplomacy/icon_diplomacy_ceasefire.svg` | `DiplomaticPoliticalState.Ceasefire` / ceasefire agreement | Production candidate |
| Access granted | `assets/visual/icons/diplomacy/icon_diplomacy_access_granted.svg` | `AccessPermission.Granted` | Production candidate |
| Access denied | `assets/visual/icons/diplomacy/icon_diplomacy_access_denied.svg` | `AccessPermission.Denied` | Production candidate |
| Trade | `assets/visual/icons/diplomacy/icon_diplomacy_trade.svg` | Trade agreement/offer | Production candidate |
| Agreement | `assets/visual/icons/diplomacy/icon_diplomacy_agreement.svg` | General agreement/proposal | Production candidate |
| Claim | `assets/visual/icons/diplomacy/icon_diplomacy_claim.svg` | `TerritorialClaimSnapshot` | Production candidate |
| Dispute | `assets/visual/icons/diplomacy/icon_diplomacy_dispute.svg` | `TerritorialClaimResponse.Disputed` | Production candidate |

## Combat order/state family

Combat's authoritative order enum is Hold / Defend / Attack / Retreat. Damage and destruction are current combat event/state concepts. Threat intentionally reuses the shared Warning symbol.

| Asset | Path | Authoritative relationship | Status |
|---|---|---|---|
| Hold | `assets/visual/icons/combat/icon_combat_hold.svg` | `MilitaryOrderType.Hold` | Production candidate |
| Defend | `assets/visual/icons/combat/icon_combat_defend.svg` | `MilitaryOrderType.Defend` | Production candidate |
| Attack | `assets/visual/icons/combat/icon_combat_attack.svg` | `MilitaryOrderType.Attack` | Production candidate |
| Retreat | `assets/visual/icons/combat/icon_combat_retreat.svg` | `MilitaryOrderType.Retreat` / retreat events | Production candidate |
| Damage | `assets/visual/icons/combat/icon_combat_damage.svg` | `CombatEventType.DamageApplied` | Production candidate |
| Destroyed | `assets/visual/icons/combat/icon_combat_destroyed.svg` | `CombatEventType.FleetDestroyed` | Production candidate |
| Threat | `assets/visual/icons/core/icon_status_warning.svg` | Shared warning vocabulary | Production candidate |

## Procedural main-menu visual

`src/Game/Presentation/MainMenuBackdrop.cs` is the first production-candidate atmospheric screen treatment. It draws a deterministic 92-star field, restrained orbital arcs around a distant stellar focus and a dark planetary limb. It does not animate continuously, depend on a raster texture, embed fake interface text or reveal simulation state. `MainMenuLayer.cs` keeps the existing Continue / New Game / Quit layout and actions while applying the 28 px title hierarchy and semantic text colors from the visual system.

`src/Game/Presentation/VisualPalette.cs` centralizes runtime semantic colors used by this and future direct-drawn map presentation. Its values mirror `assets/visual/ui/visual_tokens.json`; token JSON remains the authoritative design reference.

## Current prototype visuals not yet production assets

The following are implementation treatments, not yet reusable production art:

- direct-drawn star circles and survey-state colors in `src/Game/Presentation/Main.cs`;
- direct-drawn colony rings, fleet circles and route lines in `src/Game/Presentation/Main.cs`;
- Godot fallback font;
- programmatic prototype panel/layout structure (now inheriting the shared project Theme);
- text-only working-title treatment; no cleared commercial logo asset exists yet.

## Provenance rules

Production assets must be original project-authored vectors/code, original generated artwork with generation provenance recorded, project-owned commissioned/contributed artwork with rights recorded, or appropriately licensed third-party material with license/source recorded. Public visibility on the web is not sufficient provenance.

For generated raster art, add the generator/tool family, creation date, intended crop/aspect ratio, and whether the committed file is source-quality or runtime-optimized.

## Source vs runtime

The v1 SVG icons are both editable source and runtime assets because they are compact hand-authored vectors. The procedural main-menu background is source code and runtime presentation simultaneously.

If future raster imagery requires a high-resolution source master plus optimized runtime WebP/PNG, list both paths and their relationship here.

## Validation

Run:

```bash
python3 scripts/validate_visual_assets.py
```

The main build workflow also runs on `work/**` branches and performs Godot 4.7.2 headless editor/runtime smoke tests, providing the import/startup gate after assets are committed.

An icon or coherent visual family is not **Production ready** until it has additionally been viewed at the practical UI sizes and in the relevant integrated screen/background context.
