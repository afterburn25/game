#!/usr/bin/env python3
"""Validate the Stellar Continuum visual asset contract using stdlib only."""

from __future__ import annotations

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ICON_ROOT = ROOT / "assets" / "visual" / "icons"
TOKENS = ROOT / "assets" / "visual" / "ui" / "visual_tokens.json"
THEME = ROOT / "assets" / "visual" / "ui" / "stellar_continuum_theme.tres"
MANIFEST = ROOT / "docs" / "ASSET_MANIFEST.md"
GUIDE = ROOT / "docs" / "VISUAL_STYLE_GUIDE.md"
PROJECT = ROOT / "project.godot"
MAIN_SCENE = ROOT / "scenes" / "Main.tscn"
RUNTIME_PALETTE = ROOT / "src" / "Game" / "Presentation" / "VisualPalette.cs"
ICON_LIBRARY = ROOT / "src" / "Game" / "Presentation" / "VisualIconLibrary.cs"
VISUAL_MAP = ROOT / "src" / "Game" / "Presentation" / "Main.VisualMap.cs"
INTEGRATED_VISUALS = ROOT / "src" / "Game" / "Presentation" / "IntegratedMain.Visuals.cs"
MAIN_MENU_BACKDROP = ROOT / "src" / "Game" / "Presentation" / "MainMenuBackdrop.cs"
MAIN_MENU_LAYER = ROOT / "src" / "Game" / "Presentation" / "MainMenuLayer.cs"

ICON_FAMILIES = {
    "core": {
        "icon_hud_pause.svg",
        "icon_hud_speed.svg",
        "icon_hud_save.svg",
        "icon_hud_support.svg",
        "icon_action_research.svg",
        "icon_action_construction.svg",
        "icon_system_exploration.svg",
        "icon_system_logistics.svg",
        "icon_system_relations.svg",
        "icon_map_colony.svg",
        "icon_map_scout.svg",
        "icon_status_info.svg",
        "icon_status_warning.svg",
        "icon_status_success.svg",
        "icon_status_unknown.svg",
        "icon_status_hostile.svg",
    },
    "resources": {
        "icon_resource_credits.svg",
        "icon_resource_industry.svg",
        "icon_resource_science.svg",
    },
    "construction": {
        "icon_construction_research_network.svg",
        "icon_construction_industrial_automation.svg",
        "icon_construction_orbital_launch_complex.svg",
        "icon_construction_orbital_shipyard.svg",
        "icon_construction_warp_test_facility.svg",
    },
    "ships": {
        "icon_ship_science_vessel.svg",
        "icon_ship_patrol_corvette.svg",
        "icon_ship_colony_ship.svg",
    },
    "map": {
        "icon_map_detected.svg",
        "icon_map_partially_surveyed.svg",
        "icon_map_fully_surveyed.svg",
    },
    "diplomacy": {
        "icon_diplomacy_contact.svg",
        "icon_diplomacy_peace.svg",
        "icon_diplomacy_war.svg",
        "icon_diplomacy_ceasefire.svg",
        "icon_diplomacy_access_granted.svg",
        "icon_diplomacy_access_denied.svg",
        "icon_diplomacy_trade.svg",
        "icon_diplomacy_agreement.svg",
        "icon_diplomacy_claim.svg",
        "icon_diplomacy_dispute.svg",
    },
    "combat": {
        "icon_combat_hold.svg",
        "icon_combat_defend.svg",
        "icon_combat_attack.svg",
        "icon_combat_retreat.svg",
        "icon_combat_damage.svg",
        "icon_combat_destroyed.svg",
    },
}

FORBIDDEN_SVG_TAGS = {"text", "image", "script", "foreignObject"}
ALLOWED_SOURCE_COLORS = {"none", "#E6F0F6"}


def fail(message: str) -> None:
    print(f"visual-assets: ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def validate_svg(path: Path) -> None:
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as exc:
        fail(f"{path.relative_to(ROOT)} is not valid XML: {exc}")

    if local_name(root.tag) != "svg":
        fail(f"{path.relative_to(ROOT)} root element is not svg")

    if root.get("viewBox") != "0 0 24 24":
        fail(f"{path.relative_to(ROOT)} must use viewBox='0 0 24 24'")
    if root.get("width") != "24" or root.get("height") != "24":
        fail(f"{path.relative_to(ROOT)} must declare width='24' height='24'")
    if root.get("stroke-width") != "1.8":
        fail(f"{path.relative_to(ROOT)} must use the v1 1.8 root stroke")
    if root.get("stroke") != "#E6F0F6":
        fail(f"{path.relative_to(ROOT)} must use the neutral source stroke #E6F0F6")

    for element in root.iter():
        name = local_name(element.tag)
        if name in FORBIDDEN_SVG_TAGS:
            fail(f"{path.relative_to(ROOT)} contains forbidden <{name}>")
        for key, value in element.attrib.items():
            lower_key = key.lower()
            lower_value = value.lower()
            if lower_key.endswith("href") and (
                lower_value.startswith("http:")
                or lower_value.startswith("https:")
                or lower_value.startswith("data:")
            ):
                fail(f"{path.relative_to(ROOT)} contains an external/embedded href")
            if key in {"stroke", "fill"} and value not in ALLOWED_SOURCE_COLORS:
                fail(
                    f"{path.relative_to(ROOT)} contains non-contract {key} color {value!r}"
                )


def require_contains(path: Path, snippets: tuple[str, ...]) -> None:
    text = path.read_text(encoding="utf-8")
    for snippet in snippets:
        if snippet not in text:
            fail(f"{path.relative_to(ROOT)} is missing required contract entry {snippet!r}")


def main() -> int:
    required = [
        ICON_ROOT,
        TOKENS,
        THEME,
        MANIFEST,
        GUIDE,
        PROJECT,
        MAIN_SCENE,
        RUNTIME_PALETTE,
        ICON_LIBRARY,
        VISUAL_MAP,
        INTEGRATED_VISUALS,
        MAIN_MENU_BACKDROP,
        MAIN_MENU_LAYER,
    ]
    missing = [str(path.relative_to(ROOT)) for path in required if not path.exists()]
    if missing:
        fail("missing required visual resources: " + ", ".join(missing))

    expected_paths: list[Path] = []
    for family, expected_names in ICON_FAMILIES.items():
        family_root = ICON_ROOT / family
        if not family_root.is_dir():
            fail(f"missing icon family directory {family_root.relative_to(ROOT)}")
        actual_names = {path.name for path in family_root.glob("*.svg")}
        if actual_names != expected_names:
            missing_icons = sorted(expected_names - actual_names)
            unexpected_icons = sorted(actual_names - expected_names)
            fail(
                f"{family} icon family mismatch; missing={missing_icons!r} "
                f"unexpected={unexpected_icons!r}"
            )
        expected_paths.extend(family_root / name for name in expected_names)

    for path in sorted(expected_paths):
        validate_svg(path)

    try:
        token_data = json.loads(TOKENS.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        fail(f"visual token JSON is invalid: {exc}")

    if token_data.get("schema") != "stellar-continuum.visual-tokens.v1":
        fail("visual token schema must be stellar-continuum.visual-tokens.v1")
    if token_data.get("geometry", {}).get("icon_grid") != 24:
        fail("visual token icon_grid must remain 24 for v1")
    if token_data.get("geometry", {}).get("icon_stroke") != 1.8:
        fail("visual token icon_stroke must remain 1.8 for v1")

    require_contains(
        THEME,
        (
            '[gd_resource type="Theme"',
            'Button/styles/normal',
            'Button/styles/hover',
            'Button/styles/pressed',
            'Button/styles/disabled',
            'Button/styles/focus',
            'PanelContainer/styles/panel',
            'Label/colors/font_color',
        ),
    )
    require_contains(
        PROJECT,
        (
            '[gui]',
            'theme/custom="res://assets/visual/ui/stellar_continuum_theme.tres"',
        ),
    )
    require_contains(
        MAIN_SCENE,
        (
            '[node name="MainMenuLayer" type="CanvasLayer" parent="."]',
            'layer = 100',
        ),
    )
    require_contains(
        RUNTIME_PALETTE,
        (
            'public static class VisualPalette',
            'public static readonly Color Canvas',
            'public static readonly Color Selected',
            'public static readonly Color Danger',
        ),
    )
    require_contains(
        ICON_LIBRARY,
        (
            'public static class VisualIconLibrary',
            'Visual asset could not be loaded',
            'icon_map_detected.svg',
            'icon_ship_patrol_corvette.svg',
            'icon_diplomacy_contact.svg',
        ),
    )
    require_contains(
        VISUAL_MAP,
        (
            'protected void DrawVisualMapOverlay()',
            'SystemSurveyLevel.PartiallySurveyed',
            'DrawVisualColonies',
            'DrawVisualPlayerFleets',
            'VisualIconLibrary.SurveyDetected',
        ),
    )
    require_contains(
        INTEGRATED_VISUALS,
        (
            'public override void _Draw()',
            'base._Draw();',
            'DrawVisualMapOverlay();',
        ),
    )
    require_contains(
        MAIN_MENU_BACKDROP,
        (
            'public partial class MainMenuBackdrop : Control',
            'private const int StarCount = 92;',
            'new Random(2050)',
            'VisualPalette.Canvas',
        ),
    )
    require_contains(
        MAIN_MENU_LAYER,
        (
            'var backdrop = new MainMenuBackdrop();',
            'title.AddThemeFontSizeOverride("font_size", 28);',
            'VisualPalette.TextMuted',
        ),
    )

    manifest_text = MANIFEST.read_text(encoding="utf-8")
    for path in sorted(expected_paths):
        if path.name not in manifest_text:
            fail(f"asset manifest does not list {path.name}")
    for path in (RUNTIME_PALETTE, ICON_LIBRARY, VISUAL_MAP, MAIN_MENU_BACKDROP):
        if path.name not in manifest_text:
            fail(f"asset manifest does not list {path.name}")

    print(
        f"visual-assets: validated {len(expected_paths)} SVG icons across "
        f"{len(ICON_FAMILIES)} families, visual tokens, Godot Theme binding, "
        "runtime palette/icon loader, strategic map overlay, procedural main-menu backdrop, "
        "style guide and manifest"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
