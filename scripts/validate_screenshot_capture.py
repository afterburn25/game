#!/usr/bin/env python3
"""Validate the exact rendered UI capture contract, input proof, PNGs, and runtime logs."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import sys
import zlib

from validate_godot_smoke import validate_log

CAPTURES = (
    "01-main-menu.png", "02-region-map.png", "03-research-card.png",
    "04-industry-card.png", "05-relations.png", "06-demo-confirmation.png",
    "07-demo-guidance.png", "08-ships-card.png", "09-colonies.png",
    "10-system-planets.png", "11-region-map-demo.png", "12-menu-drawer.png",
    "13-earth-selected.png", "14-galaxy-overview.png", "15-zoomed-region.png",
    "16-earth-focus.png", "17-surface-placement.png", "18-surface-colony.png",
)
SECTIONS = ("research", "industry", "ships", "explore", "colonies", "inspection",
            "logistics", "relations", "menu")
REQUIRED_CHECKS = {
    "normal-startup-menu-paused", "menu-blocks-gameplay-keyboard",
    "menu-blocks-gameplay-pointer", "continue-resumes-normal-campaign",
    "navigation-default-closed", "drawer-close-returns-map", "controls-fit-1280x720",
    "map-selection-positive-control", "map-order-positive-control",
    "drawer-blocks-map-selection", "drawer-blocks-map-orders",
    "rail-blocks-map-input", "dock-blocks-map-input",
    "demo-confirmation-wraps-inside-viewport", "cancel-demo-preserves-normal-campaign",
    "confirm-starts-guided-demo-at-24x", "demo-guidance-visible-with-objective",
    "research-card-starts-project", "industry-card-starts-project",
    "early-game-ship-buttons-dispatch", "home-selects-known-star",
    "open-system-enters-home-orbits", "command-feedback-visible-over-system-view",
    "back-to-region-preserves-selection", "menu-preserves-demo-state",
    "continue-restores-demo-speed", "normal-save-unchanged-by-demo",
    "normal-human-earth-sol-start", "demo-human-earth-sol-start", "sol-catalog-worlds-visible",
    "earth-selected-by-mouse", "demo-sol-identity-survives-reload",
    "icon-only-controls-visible", "project-icons-crisp",
}
REQUIRED_CHECKS.update(f"drawer-{section}-exclusive" for section in SECTIONS)
CAMERA_CHECKS = {
    "galaxy-overview-reachable-by-wheel", "galaxy-overview-shows-public-catalog",
    "regional-wheel-button-zoom-parity", "galaxy-region-zoom-roundtrip-restores",
    "regional-pan-inverse-hit", "drawer-blocks-camera-wheel",
    "system-wheel-button-zoom-parity", "system-pan-inverse-hit",
    "planet-focus-by-real-double-click", "planet-focus-back-restores-system-camera",
    "system-back-restores-region-camera", "unknown-system-entry-preserves-privacy",
    "unknown-body-materials-redacted", "resize-preserves-star-hit",
    "resize-preserves-body-hit", "resize-restores-minimum-layout",
    "camera-transitions-settle-smoothly",
    "focused-menu-blocks-camera", "planet-wheel-button-route-parity",
    "wheel-enters-system-and-restores-region",
}
REQUIRED_CHECKS.update(CAMERA_CHECKS)
SURFACE_CHECKS = {
    "earth-surface-opens-from-real-breadcrumb", "surface-controls-fit-1280x720",
    "surface-camera-input-and-hud-shielding", "surface-valid-free-placement-preview",
    "surface-real-ground-click-places-unfunded-site", "surface-collision-rejected-without-charge",
    "surface-save-keeps-normal-campaign-separate", "surface-back-restores-orbit-without-map-input",
    "surface-ordinary-progress-completes-powered-buildings", "surface-real-save-reload-retains-buildings",
}
REQUIRED_CHECKS.update(SURFACE_CHECKS)
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def png_size(data: bytes) -> tuple[int, int]:
    """Read a complete, CRC-checked PNG rather than trusting its extension or log."""
    if not data.startswith(PNG_SIGNATURE):
        raise ValueError("invalid PNG signature")
    offset = len(PNG_SIGNATURE)
    dimensions = None
    compressed = bytearray()
    ended = False
    while offset < len(data):
        if offset + 12 > len(data):
            raise ValueError("truncated PNG chunk")
        size = struct.unpack_from(">I", data, offset)[0]
        kind = data[offset + 4:offset + 8]
        end = offset + 12 + size
        if end > len(data):
            raise ValueError("truncated PNG payload")
        payload = data[offset + 8:offset + 8 + size]
        crc = struct.unpack_from(">I", data, offset + 8 + size)[0]
        if zlib.crc32(kind + payload) & 0xFFFFFFFF != crc:
            raise ValueError("PNG CRC mismatch")
        if dimensions is None:
            if kind != b"IHDR" or size != 13:
                raise ValueError("PNG must begin with IHDR")
            width, height, depth, color, compression, filtering, interlace = struct.unpack(">IIBBBBB", payload)
            if (width, height) != (1280, 720):
                raise ValueError(f"expected minimum-layout capture 1280x720, got {width}x{height}")
            if depth != 8 or color not in (2, 6) or compression or filtering or interlace:
                raise ValueError("expected ordinary 8-bit RGB/RGBA capture")
            dimensions = (width, height, 3 if color == 2 else 4)
        elif kind == b"IHDR":
            raise ValueError("duplicate PNG header")
        if kind == b"IDAT":
            compressed.extend(payload)
        if kind == b"IEND":
            if payload or end != len(data):
                raise ValueError("invalid PNG end")
            ended = True
            break
        offset = end
    if not ended or dimensions is None or not compressed:
        raise ValueError("incomplete PNG")
    width, height, channels = dimensions
    expected_size = height * (1 + width * channels)
    decoder = zlib.decompressobj()
    decoded = decoder.decompress(bytes(compressed), expected_size + 1)
    if (len(decoded) != expected_size or not decoder.eof or decoder.unused_data
            or decoder.unconsumed_tail):
        raise ValueError("PNG image data is truncated or has an unexpected size")
    stride = 1 + width * channels
    if any(decoded[row * stride] > 4 for row in range(height)):
        raise ValueError("invalid PNG row filter")
    return width, height


def validate_capture(directory: Path, expected_sha: str) -> list[str]:
    failures = []
    try:
        manifest = json.loads((directory / "capture-manifest.json").read_text(encoding="utf-8"))
        log = (directory / "godot-capture.log").read_text(encoding="utf-8")
        import_log = (directory / "godot-import.log").read_text(encoding="utf-8")
    except (OSError, ValueError) as error:
        return [f"Cannot read capture evidence: {error}"]
    failures.extend(validate_log(import_log))
    failures.extend(validate_log(log, require_runtime_ready=True))
    lines = {line.strip() for line in log.splitlines()}
    if "STELLAR_SCREENSHOT_CAPTURE_COMPLETE" not in lines:
        failures.append("Screenshot driver did not finish.")
    if not re.fullmatch(r"[0-9a-f]{40}", expected_sha) or manifest.get("git_sha") != expected_sha:
        failures.append("Capture does not match the exact workflow commit.")
    if manifest.get("schema_version") != 2 or manifest.get("input_mode") != "Input.ParseInputEvent":
        failures.append("Capture does not prove the real-input schema.")
    checks = manifest.get("checks", [])
    if not isinstance(checks, list) or any(not isinstance(item, str) for item in checks):
        failures.append("Invalid check evidence.")
        checks = []
    if len(checks) != len(set(checks)):
        failures.append("Duplicate check evidence.")
    missing = REQUIRED_CHECKS - set(checks)
    if missing:
        failures.append("Missing required checks: " + ", ".join(sorted(missing)))
    for check in REQUIRED_CHECKS:
        if f"STELLAR_UI_CHECK_PASS {check}" not in lines:
            failures.append(f"Missing runtime check marker: {check}")
    mouse_count = manifest.get("mouse_actions")
    mouse_lines = [line for line in log.splitlines() if line.startswith("STELLAR_MOUSE_INPUT ")]
    if not isinstance(mouse_count, int) or mouse_count < 35 or mouse_count != len(mouse_lines):
        failures.append("Insufficient or inconsistent real mouse input evidence.")
    captures = manifest.get("captures", [])
    if not isinstance(captures, list) or any(not isinstance(item, dict) for item in captures):
        return failures + ["Invalid screenshot evidence."]
    names = [item.get("file") for item in captures]
    if (any(not isinstance(name, str) for name in names)
            or len(names) != len(CAPTURES) or set(names) != set(CAPTURES)):
        return failures + ["Screenshot manifest differs from the required views."]
    if {path.name for path in directory.glob("*.png")} != set(CAPTURES):
        failures.append("Screenshot directory contains missing or unexpected images.")
    for capture in captures:
        name = capture["file"]
        try:
            data = (directory / name).read_bytes()
            width, height = png_size(data)
            if len(data) < 4096:
                raise ValueError("PNG is unexpectedly small")
            if (capture.get("width"), capture.get("height")) != (width, height):
                raise ValueError("manifest dimensions disagree with PNG")
            if capture.get("bytes") != len(data) or capture.get("sha256") != hashlib.sha256(data).hexdigest():
                raise ValueError("manifest checksum/length disagree with PNG")
            marker = f"STELLAR_SCREENSHOT_CAPTURED {name} {width}x{height} {len(data)} bytes"
            if marker not in lines:
                raise ValueError("runtime capture marker missing or inconsistent")
        except (OSError, ValueError, zlib.error, struct.error) as error:
            failures.append(f"{name}: {error}")
    return failures


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("--expected-sha", required=True)
    args = parser.parse_args()
    failures = validate_capture(args.directory, args.expected_sha)
    for failure in failures:
        print(f"Screenshot validation failed: {failure}", file=sys.stderr)
    if failures:
        return 1
    print(f"Validated {len(CAPTURES)} exact-head rendered captures and {len(REQUIRED_CHECKS)} real-input checks.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
