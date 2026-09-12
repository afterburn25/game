"""Maintained exporter integrity and native checkpoint regression checks."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("stellar_export", Path(__file__).with_name("stellar.py"))
exporter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exporter)

class PackageIntegrity(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory(prefix="stellar-package-test-")
        self.addCleanup(self.scratch.cleanup)
        self.root = Path(self.scratch.name)
        (self.root / "Configuration").mkdir()
        (self.root / "stellar-continuum.exe").write_bytes(b"fixture, not an executable")
        (self.root / "Configuration/runtime-config.json").write_text("{}")
        self.seal()

    def seal(self, **overrides):
        self.manifest = dict(files=exporter.hashes(self.root), includeSymbols=False, **overrides)
        (self.root / "build-manifest.json").write_text(json.dumps(self.manifest))

    def test_valid_manifest(self):
        exporter.validate_manifest(self.root)

    def test_modified_or_missing_runtime_is_rejected(self):
        (self.root / "stellar-continuum.exe").write_bytes(b"tampered")
        with self.assertRaisesRegex(RuntimeError, "manifest"):
            exporter.validate_manifest(self.root)
        (self.root / "stellar-continuum.exe").unlink()
        with self.assertRaises(RuntimeError): exporter.validate_manifest(self.root)

    def test_extra_file_is_rejected(self):
        (self.root / "unexpected.dll").write_bytes(b"unlisted")
        with self.assertRaisesRegex(RuntimeError, "manifest"): exporter.validate_manifest(self.root)

    def test_sources_and_public_symbols_are_rejected_even_when_manifested(self):
        for name in ("source.cpp", "private.pdb"):
            with self.subTest(name=name):
                item=self.root / name; item.write_bytes(b"development only"); self.seal()
                with self.assertRaises(RuntimeError): exporter.validate_manifest(self.root)
                item.unlink()

    def test_manifest_cannot_escape_export_or_duplicate_entries(self):
        for paths in (["../outside"], ["stellar-continuum.exe", "stellar-continuum.exe"]):
            self.manifest["files"]=[dict(path=p, bytes=0, sha256="") for p in paths]
            (self.root / "build-manifest.json").write_text(json.dumps(self.manifest))
            with self.assertRaises(RuntimeError): exporter.validate_manifest(self.root)

    def test_graphical_release_cannot_be_exported_before_parity(self):
        with self.assertRaisesRegex(RuntimeError, "(?i)graphical"):
            exporter.export("windows-release")

@unittest.skipUnless(os.environ.get("STELLAR_NATIVE_EXE"), "Set STELLAR_NATIVE_EXE to run native integration checks")
class NativeRecovery(unittest.TestCase):
    def setUp(self):
        self.exe=Path(os.environ["STELLAR_NATIVE_EXE"]).resolve()
        self.scratch=tempfile.TemporaryDirectory(prefix="stellar-recovery-test-")
        self.addCleanup(self.scratch.cleanup); self.root=Path(self.scratch.name)

    def invoke(self, *args):
        return subprocess.run([str(self.exe), "--headless", *map(str,args)], cwd=self.root,
                              capture_output=True, text=True, timeout=30)

    def test_roundtrip_across_worker_counts(self):
        checkpoint=self.root / "save.scf"
        self.assertEqual(self.invoke("--ticks",5,"--workers",2,"--save",checkpoint).returncode,0)
        resumed=self.invoke("--ticks",5,"--workers",1,"--load",checkpoint)
        direct=self.invoke("--ticks",10,"--workers",4)
        self.assertEqual(resumed.returncode,0,resumed.stderr)
        a=json.loads(resumed.stdout); b=json.loads(direct.stdout)
        self.assertEqual(a["completedTicks"],10)
        self.assertEqual((a["checkpointHash"],a["distanceSum"]),(b["checkpointHash"],b["distanceSum"]))

    def test_corruption_and_old_game_format_fail_cleanly(self):
        checkpoint=self.root / "bad.scf"
        for content in ('{"schemaVersion":16}', "STELLAR_FOUNDATION_V1\n1", "STELLAR_FOUNDATION_V1\n1 500 2 0\n"):
            with self.subTest(content=content):
                checkpoint.write_text(content)
                result=self.invoke("--load",checkpoint)
                self.assertEqual(result.returncode,1)
                self.assertIn("error [",result.stderr)
                self.assertIn("Working directory:",result.stderr)
                self.assertEqual(checkpoint.read_text(),content)

    def test_existing_checkpoint_is_preserved(self):
        checkpoint=self.root / "preserve.scf"; checkpoint.write_text("previous valid game")
        result=self.invoke("--ticks",0,"--save",checkpoint)
        self.assertEqual(result.returncode,1)
        self.assertIn("Refusing to overwrite",result.stderr)
        self.assertEqual(checkpoint.read_text(),"previous valid game")

    def test_invalid_cli_inputs_fail_without_native_crash(self):
        for args in (("--systems","-1"),("--workers","0"),("--ticks","1000001"),("--unrecognized","1")):
            with self.subTest(args=args):
                result=self.invoke(*args)
                self.assertEqual(result.returncode,1)
                self.assertIn("error [",result.stderr)

if __name__ == "__main__": unittest.main()
