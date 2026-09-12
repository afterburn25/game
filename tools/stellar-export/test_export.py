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

    def test_declared_catalog_cannot_be_omitted_from_sealed_package(self):
        relative="Data/astronomy/hyg-nearby-500-v1.json"
        self.seal(requiredRuntimeFiles=[relative])
        with self.assertRaisesRegex(RuntimeError,"Missing required runtime"):
            exporter.validate_manifest(self.root)
        catalog=self.root/relative; catalog.parent.mkdir(parents=True); catalog.write_text("{}")
        self.seal(requiredRuntimeFiles=[relative])
        exporter.validate_manifest(self.root)
        catalog.write_text("tampered data")
        with self.assertRaisesRegex(RuntimeError,"manifest"):
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

    def test_galaxy_loads_assets_relative_to_executable(self):
        output=self.root / "galaxy.json"
        result=self.invoke("--generate-galaxy","--systems",250,"--seed",-1,"--catalog-output",output)
        self.assertEqual(result.returncode,0,result.stderr)
        report=json.loads(result.stdout); data=json.loads(output.read_text())
        self.assertEqual(Path(report["assetPath"]).resolve(),(self.exe.parent/"Data/astronomy/hyg-nearby-500-v1.json").resolve())
        self.assertEqual(len(data["systems"]),250)
        self.assertEqual(data["systems"][0]["name"],"Sol")
        self.assertEqual(data["solBodies"][-1]["name"],"Pluto")
        self.assertGreater(report["planetaryBodies"],10)
        self.assertEqual(report["planetaryBodies"],len(data["planetaryBodies"]))
        self.assertEqual(data["phase"],"physical-before-civilizations")
        self.assertFalse(report["gameplayParity"])
        bodies={b["id"]:b for b in data["planetaryBodies"]}
        self.assertEqual(len(bodies),len(data["planetaryBodies"]))
        system_ids={s["id"] for s in data["systems"]}
        for body in bodies.values():
            self.assertIn(body["systemId"],system_ids)
            if body["parentBodyId"] is not None:
                self.assertIn(body["parentBodyId"],bodies)
                self.assertEqual(bodies[body["parentBodyId"]]["systemId"],body["systemId"])
        repeated=self.root/"repeated.json"
        repeat=self.invoke("--generate-galaxy","--systems",250,"--seed",-1,"--repeat",2,"--catalog-output",repeated)
        self.assertEqual(repeat.returncode,0,repeat.stderr)
        self.assertEqual(output.read_bytes(),repeated.read_bytes())
        original=output.read_bytes()
        again=self.invoke("--generate-galaxy","--catalog-output",output)
        self.assertEqual(again.returncode,1)
        self.assertEqual(original,output.read_bytes())

    def test_missing_and_damaged_galaxy_assets_fail_cleanly(self):
        destination=self.root/"Data/astronomy/hyg-nearby-500-v1.json"
        missing=self.invoke("--generate-galaxy","--asset-root",self.root)
        self.assertEqual(missing.returncode,1)
        self.assertIn("hyg-nearby-500-v1.json",missing.stderr)
        destination.parent.mkdir(parents=True)
        valid=json.loads((self.exe.parent/"Data/astronomy/hyg-nearby-500-v1.json").read_text())
        duplicate=json.loads(json.dumps(valid)); duplicate["systems"][1]["hygId"]=0
        wrong_version=json.loads(json.dumps(valid)); wrong_version["catalogVersion"]="future"
        for content in ("{damaged",json.dumps(duplicate),json.dumps(wrong_version)):
            destination.write_text(content)
            result=self.invoke("--generate-galaxy","--asset-root",self.root)
            self.assertEqual(result.returncode,1,result.stderr)
            self.assertIn("Cannot load stellar catalog",result.stderr)
            self.assertIn(str(destination),result.stderr)

if __name__ == "__main__": unittest.main()
