#!/usr/bin/env python3
"""Reject incomplete or stale focused ordinary-player expedition evidence."""
import argparse
import json
from pathlib import Path

REQUIRED_CAPTURES = {
    "player-expedition-01-opening-research.png",
    "player-expedition-02-first-warp-shipyard.png",
    "player-expedition-03-settlement-authorized.png",
    "player-expedition-04-reloaded-colony.png",
}
REQUIRED_CHECKS = {
    "player-expedition-fresh-ordinary-sandbox",
    "player-expedition-first-warp-completed",
    "player-expedition-settlement-timed-and-complete",
    "player-expedition-save-reload-preserves-colony-people-and-ships",
}

parser = argparse.ArgumentParser()
parser.add_argument("directory", type=Path)
parser.add_argument("--expected-sha")
args = parser.parse_args()
manifest_path = args.directory / "player-expedition-manifest.json"
manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
if manifest.get("seed") != "20260908" or manifest.get("system_count") != 100 or not manifest.get("player_mode"):
    raise SystemExit("focused evidence is not the fixed ordinary 100-system Player Sandbox")
if args.expected_sha and manifest.get("git_sha") != args.expected_sha:
    raise SystemExit("focused evidence git SHA does not match the requested revision")
captures = {capture["file"]: capture for capture in manifest.get("captures", [])}
missing = REQUIRED_CAPTURES - captures.keys()
if missing:
    raise SystemExit("missing expedition captures: " + ", ".join(sorted(missing)))
for name, capture in captures.items():
    path = args.directory / name
    if not path.is_file() or capture.get("bytes", 0) < 4096 or not capture.get("sha256"):
        raise SystemExit("invalid captured evidence: " + name)
checks = set(manifest.get("checks", []))
missing = REQUIRED_CHECKS - checks
if missing:
    raise SystemExit("missing expedition checks: " + ", ".join(sorted(missing)))
if manifest.get("simulation_days", 0) < 5900 or manifest.get("elapsed_wall_seconds", 0) <= 0:
    raise SystemExit("expedition evidence did not reach the expected ordinary opening horizon")
print("player expedition capture evidence valid")
