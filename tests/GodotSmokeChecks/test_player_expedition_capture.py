"""CLI regression coverage for the ordinary Player expedition evidence contract."""

import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from test_screenshot_capture import chunk, sample_png

SCRIPTS = Path(__file__).resolve().parents[2] / "scripts"
VALIDATOR = SCRIPTS / "validate_player_expedition_capture.py"
SHA = "a" * 40


class PlayerExpeditionCaptureChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.write_fixture()

    def write_fixture(self):
        png = sample_png()
        captures = []
        names = (
            "player-expedition-01-opening-research.png",
            "player-expedition-02-first-warp-shipyard.png",
            "player-expedition-03-settlement-authorized.png",
            "player-expedition-04-reloaded-colony.png",
        )
        for index, name in enumerate(names):
            image = png[:-12] + chunk(b"tEXt", f"fixture-{index}".encode()) + png[-12:]
            (self.directory / name).write_bytes(image)
            captures.append({
                "file": name, "width": 1280, "height": 720,
                "bytes": len(image), "sha256": hashlib.sha256(image).hexdigest(),
            })

        save = {
            "Galaxy": {
                "Fleets": [{
                    "Id": "fleet-1", "DestinationPlanetaryBodyId": "body-1",
                    "SettlementBodyId": None, "SettlementDaysCompleted": 0,
                    "EmbarkedPopulationMillions": 10.0,
                }],
                "Colonies": [{"Id": "earth-colony"}],
            },
            "padding": "evidence fixture " * 400,
        }
        save_bytes = json.dumps(save).encode()
        (self.directory / "player-expedition-authorization-save.json").write_bytes(save_bytes)
        checks = {
            "player-expedition-fresh-ordinary-sandbox",
            "player-expedition-active-research-control-retains-focus-and-refreshes-progress",
            "player-expedition-research-pointer-pause-halts-canonical-spend",
            "player-expedition-paused-research-progress-remains-stable",
            "player-expedition-research-pointer-resume-restores-canonical-progress-and-spend",
            "player-expedition-pauses-active-research-for-shipbuilding-capital",
            "player-expedition-first-warp-completed",
            "player-expedition-colony-transit-right-click-order",
            "player-expedition-colony-body-right-click-authorizes-settlement",
            "player-expedition-authorization-save-preserves-ship-id-target-and-embarked-people",
            "player-expedition-exact-body-founded-and-colony-ship-consumed",
            "player-expedition-settlement-observes-canonical-timer",
            "player-expedition-settlement-timed-and-complete",
            "player-expedition-save-reload-preserves-colony-people-and-ships",
            "player-expedition-build-warp_scout", "player-expedition-build-science_vessel",
            "player-expedition-build-colony_ship",
        }
        checks.update(f"player-expedition-construction-{design}" for design in (
            "research_network", "industrial_automation", "orbital_launch_complex",
            "orbital_shipyard", "warp_test_facility"))
        checks.update({"player-expedition-scout-right-click-order-1",
                       "player-expedition-science-right-click-order-1"})
        checks.update(f"player-expedition-research-{research}" for research in (
            "in_space_assembly", "asteroid_prospecting", "asteroid_mining", "vacuum_refining",
            "orbital_manufacturing", "orbital_shipyard", "gravitational_physics", "field_theory",
            "warp_metric_theory", "exotic_energy_coupling", "micro_field_distortion",
            "warp_field_control", "prototype_warp_drive"))
        self.manifest = {
            "schema_version": 2, "git_sha": SHA, "seed": "20260908",
            "system_count": 100, "player_mode": True,
            "input_mode": "Input.ParseInputEvent",
            "scope": "focused ordinary Player Sandbox opening; no Developer mode",
            "mouse_actions": 35, "checks": sorted(checks), "captures": captures,
            "simulation_days": 5978.5, "elapsed_wall_seconds": 12.46,
            "authorization_save": {
                "captured_while_paused": True, "save_bytes": len(save_bytes),
                "save_sha256": hashlib.sha256(save_bytes).hexdigest(),
                "file": "player-expedition-authorization-save.json", "fleet_id": "fleet-1",
                "body_id": "body-1", "embarked_population_millions": 10.0,
            },
            "settlement": {
                "colony_ship_consumed": True, "observation_paused": True,
                "colonies_before": 1, "colonies_after": 2, "fleet_id": "fleet-1",
                "body_id": "body-1", "authorized_population_millions": 10.0,
                "observed_population_millions": 10.0,
                "observed_simulation_days": 6008.5, "authorization_simulation_days": 5978.5,
            },
        }
        lines = ["Godot Engine v4.7.2", "STELLAR_RUNTIME_READY IntegratedMain"]
        lines += ["STELLAR_MOUSE_INPUT synthetic-test-fixture"] * 35
        lines += [f"STELLAR_UI_CHECK_PASS {check}" for check in self.manifest["checks"]]
        lines.append("STELLAR_FOCUSED_PLAYER_EXPEDITION_COMPLETE")
        (self.directory / "godot-capture.log").write_text("\n".join(lines) + "\n", encoding="utf-8")
        (self.directory / "player-expedition-manifest.json").write_text(
            json.dumps(self.manifest), encoding="utf-8")

    def run_validator(self, expected_sha=SHA):
        return subprocess.run(
            [sys.executable, "-B", str(VALIDATOR), str(self.directory), "--expected-sha", expected_sha],
            capture_output=True, text=True, check=False)

    def test_complete_evidence_passes_cli(self):
        result = self.run_validator()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("player expedition capture evidence valid", result.stdout)

    def test_tampered_capture_bytes_fail_the_receipt(self):
        path = self.directory / "player-expedition-01-opening-research.png"
        path.write_bytes(path.read_bytes()[:-12] + chunk(b"tEXt", b"tampered") + path.read_bytes()[-12:])
        result = self.run_validator()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("manifest length, dimensions, or SHA-256 disagrees", result.stderr)

    def test_missing_step_and_wrong_expected_sha_fail(self):
        self.manifest["checks"].remove("player-expedition-first-warp-completed")
        (self.directory / "player-expedition-manifest.json").write_text(json.dumps(self.manifest), encoding="utf-8")
        result = self.run_validator("b" * 40)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("git SHA does not match", result.stderr)

    def test_dirty_runtime_log_fails_even_with_completion_marker(self):
        path = self.directory / "godot-capture.log"
        path.write_text(path.read_text(encoding="utf-8") + "ERROR: late runtime failure\n", encoding="utf-8")
        result = self.run_validator()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("Godot error", result.stderr)

    def test_authorization_receipt_mismatch_fails(self):
        self.manifest["authorization_save"]["embarked_population_millions"] = 11.0
        (self.directory / "player-expedition-manifest.json").write_text(json.dumps(self.manifest), encoding="utf-8")
        result = self.run_validator()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("preserved save does not contain", result.stderr)


if __name__ == "__main__":
    unittest.main()
