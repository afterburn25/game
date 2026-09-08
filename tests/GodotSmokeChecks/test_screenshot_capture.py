"""Fail closed on missing interaction proof, stale artifacts, and corrupt screenshot evidence."""

import hashlib
import json
from pathlib import Path
import random
import struct
import subprocess
import sys
import tempfile
import unittest
import zlib

SCRIPTS = Path(__file__).resolve().parents[2] / "scripts"
sys.path.insert(0, str(SCRIPTS))
import validate_screenshot_capture as capture

SHA = "a" * 40


def chunk(kind, payload):
    return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF)


def sample_png():
    row = bytes([0]) + random.Random(42).randbytes(1280 * 4)
    return (capture.PNG_SIGNATURE + chunk(b"IHDR", struct.pack(">IIBBBBB", 1280, 720, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(row * 720)) + chunk(b"IEND", b""))


class ScreenshotEvidenceChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.png = sample_png()

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.manifest = {
            "schema_version": 2, "git_sha": SHA, "input_mode": "Input.ParseInputEvent",
            "mouse_actions": 35, "checks": sorted(capture.REQUIRED_CHECKS), "captures": [],
        }
        lines = ["Godot Engine v4.7.2.stable.mono.official", "STELLAR_RUNTIME_READY IntegratedMain"]
        lines.extend("STELLAR_MOUSE_INPUT synthetic-test-fixture" for _ in range(35))
        lines.extend(f"STELLAR_UI_CHECK_PASS {name}" for name in self.manifest["checks"])
        for name in capture.CAPTURES:
            (self.directory / name).write_bytes(self.png)
            self.manifest["captures"].append({
                "file": name, "width": 1280, "height": 720, "bytes": len(self.png),
                "sha256": hashlib.sha256(self.png).hexdigest(),
            })
            lines.append(f"STELLAR_SCREENSHOT_CAPTURED {name} 1280x720 {len(self.png)} bytes")
        lines.append("STELLAR_SCREENSHOT_CAPTURE_COMPLETE")
        self.log = "\n".join(lines) + "\n"
        self.write_evidence()
        (self.directory / "godot-import.log").write_text("Godot import complete\n", encoding="utf-8")

    def write_evidence(self):
        (self.directory / "capture-manifest.json").write_text(json.dumps(self.manifest), encoding="utf-8")
        (self.directory / "godot-capture.log").write_text(self.log, encoding="utf-8")

    def failures(self):
        self.write_evidence()
        return capture.validate_capture(self.directory, SHA)

    def test_complete_evidence_passes(self):
        self.assertEqual([], self.failures())

    def test_old_commit_or_pressed_signal_only_is_rejected(self):
        self.manifest["git_sha"] = "b" * 40
        self.manifest["input_mode"] = "Pressed signals"
        failures = self.failures()
        self.assertTrue(any("exact workflow commit" in item for item in failures))
        self.assertTrue(any("real-input schema" in item for item in failures))

    def test_missing_or_forged_required_interaction_proof_is_rejected(self):
        required = "drawer-blocks-map-orders"
        self.manifest["checks"].remove(required)
        self.log = self.log.replace(f"STELLAR_UI_CHECK_PASS {required}\n", f'echo "STELLAR_UI_CHECK_PASS {required}"\n')
        failures = self.failures()
        self.assertTrue(any("Missing required checks" in item for item in failures))
        self.assertTrue(any("Missing runtime check marker" in item for item in failures))

    def test_duplicate_checks_and_mouse_count_mismatch_are_rejected(self):
        self.manifest["checks"].append(self.manifest["checks"][0])
        self.manifest["mouse_actions"] = 100
        failures = self.failures()
        self.assertTrue(any("Duplicate" in item for item in failures))
        self.assertTrue(any("mouse input evidence" in item for item in failures))

    def test_truncated_or_swapped_png_is_rejected(self):
        path = self.directory / capture.CAPTURES[0]
        path.write_bytes(self.png[:-9])
        self.assertTrue(any(capture.CAPTURES[0] in item for item in self.failures()))
        path.write_bytes(self.png)
        self.manifest["captures"][0]["sha256"] = "0" * 64
        self.assertTrue(any("checksum" in item for item in self.failures()))

    def test_wrong_dimensions_and_undecodable_png_are_rejected(self):
        wrong_size = (capture.PNG_SIGNATURE + chunk(b"IHDR", struct.pack(">IIBBBBB", 640, 360, 8, 6, 0, 0, 0))
                      + chunk(b"IDAT", zlib.compress(b"not pixels")) + chunk(b"IEND", b""))
        with self.assertRaisesRegex(ValueError, "1280x720"):
            capture.png_size(wrong_size)
        bad_pixels = (capture.PNG_SIGNATURE + chunk(b"IHDR", struct.pack(">IIBBBBB", 1280, 720, 8, 6, 0, 0, 0))
                      + chunk(b"IDAT", zlib.compress(b"not pixels")) + chunk(b"IEND", b""))
        with self.assertRaisesRegex(ValueError, "image data"):
            capture.png_size(bad_pixels)

    def test_png_crc_corruption_is_rejected(self):
        damaged = bytearray(self.png)
        damaged[20] ^= 1
        with self.assertRaisesRegex(ValueError, "CRC"):
            capture.png_size(bytes(damaged))

    def test_missing_or_extra_image_is_rejected(self):
        (self.directory / capture.CAPTURES[-1]).unlink()
        self.assertTrue(self.failures())
        (self.directory / capture.CAPTURES[-1]).write_bytes(self.png)
        (self.directory / "old-capture.png").write_bytes(self.png)
        self.assertTrue(any("unexpected images" in item for item in self.failures()))

    def test_late_runtime_error_or_aborted_import_cannot_hide_behind_complete_marker(self):
        self.log += "ERROR: Drawer script failed after capture.\n"
        self.assertTrue(any("Godot error" in item for item in self.failures()))
        self.log = self.log.removesuffix("ERROR: Drawer script failed after capture.\n")
        (self.directory / "godot-import.log").write_text("WARNING: Scan thread aborted...\n", encoding="utf-8")
        self.assertTrue(any("Godot error" in item for item in self.failures()))

    def test_cli_missing_evidence_fails(self):
        with tempfile.TemporaryDirectory() as empty:
            result = subprocess.run([sys.executable, "-B", str(SCRIPTS / "validate_screenshot_capture.py"),
                                     empty, "--expected-sha", SHA], capture_output=True, text=True)
            self.assertEqual(1, result.returncode)
            self.assertIn("Cannot read capture evidence", result.stderr)


if __name__ == "__main__":
    unittest.main()
