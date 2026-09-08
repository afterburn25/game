"""Process-level console contract: failures are diagnostics/exit codes, never CLR crashes."""
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
PROJECT = Path(__file__).with_name("AdaptiveResearchTopologyChecks.csproj")
ASSEMBLY = PROJECT.parent / "bin/Release/net8.0/AdaptiveResearchTopologyChecks.dll"


class TopologyConsoleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        build = subprocess.run(["dotnet", "build", str(PROJECT), "-c", "Release"],
                               cwd=ROOT, capture_output=True, text=True, timeout=120)
        if build.returncode:
            raise RuntimeError(build.stdout + build.stderr)

    def check_failure(self, arguments, expected_type):
        result = subprocess.run(["dotnet", str(ASSEMBLY), *arguments], cwd=ROOT,
                                capture_output=True, text=True, timeout=30)
        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        for label in ("Exception type:", "Message:", "Inner exception:", "Working directory:",
                      "Repository root:", "Catalog directory:", "Checker assembly:",
                      "Runtime:", "Full exception and stack trace:", "   at "):
            self.assertIn(label, result.stderr)
        self.assertIn(expected_type, result.stderr)
        self.assertNotIn("Unhandled exception.", result.stderr)

    def test_valid_topology_repeats_without_apphost(self):
        for _ in range(3):
            result = subprocess.run(["dotnet", "run", "--project", str(PROJECT), "-c", "Release",
                                     "--no-build", "--no-restore", "--", "--repository-root", str(ROOT)],
                                    cwd=ROOT, capture_output=True, text=True, timeout=30)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("topology regression checks OK", result.stdout)
        self.assertFalse(ASSEMBLY.with_suffix(".exe").exists(), "Obsolete apphost must not be produced")

    def test_launcher_uses_repository_from_another_directory(self):
        with tempfile.TemporaryDirectory() as other:
            result = subprocess.run([sys.executable, str(ROOT / "scripts/validate_research_topology.py")],
                                    cwd=other, capture_output=True, text=True, timeout=120)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("topology regression checks OK", result.stdout)

    def test_missing_catalog_reports_context(self):
        with tempfile.TemporaryDirectory() as temp:
            self.check_failure(["--repository-root", str(ROOT), "--catalog-directory", str(Path(temp)/"missing")],
                               "System.IO.DirectoryNotFoundException")

    def test_missing_index_reports_context(self):
        with tempfile.TemporaryDirectory() as temp:
            self.check_failure(["--repository-root", str(ROOT), "--catalog-directory", temp],
                               "System.IO.FileNotFoundException")

    def test_malformed_json_reports_context(self):
        with tempfile.TemporaryDirectory() as temp:
            (Path(temp)/"index.json").write_text("{invalid-json", encoding="utf-8")
            self.check_failure(["--repository-root", str(ROOT), "--catalog-directory", temp],
                               "System.Text.Json.JsonReaderException")

    def test_bad_arguments_fail_cleanly(self):
        self.check_failure(["--unknown-option"], "System.ArgumentException")


if __name__ == "__main__":
    unittest.main()
