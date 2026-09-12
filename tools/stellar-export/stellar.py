"""Development-only Windows exporter. The produced native runtime needs no Python/toolchain."""
from __future__ import annotations
import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import shutil
import struct
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
SYSTEM_DLLS = {"kernel32.dll", "user32.dll", "advapi32.dll", "shell32.dll", "ole32.dll", "oleaut32.dll", "ws2_32.dll", "bcrypt.dll", "ntdll.dll", "msvcrt.dll", "ucrtbase.dll", "version.dll"}

def run(args, *, env=None, cwd=ROOT, capture=False, timeout=300):
    command = [str(a) for a in args]
    command[0] = shutil.which(command[0], path=(env or os.environ).get("PATH")) or command[0]
    result = subprocess.run(command, cwd=cwd, env=env, text=True,
                            capture_output=capture, timeout=timeout, check=True)
    return result.stdout if capture else ""

def build_environment():
    if platform.system() != "Windows":
        raise RuntimeError("This export platform currently supports Windows x64 hosts only")
    env = os.environ.copy()
    local_tools = ROOT / ".tools/build-tools/Scripts"
    if local_tools.exists():
        env["PATH"] = str(local_tools) + os.pathsep + env.get("PATH", "")
    if not shutil.which("cl", path=env.get("PATH")):
        vswhere = Path(os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")) / "Microsoft Visual Studio/Installer/vswhere.exe"
        if not vswhere.is_file():
            raise RuntimeError("Developer build requires the Visual C++ x64 toolchain; players do not")
        install = run([vswhere, "-latest", "-products", "*", "-requires", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-property", "installationPath"], capture=True).strip()
        setup = Path(install) / "Common7/Tools/VsDevCmd.bat"
        if not install or not setup.is_file() or any(c in str(setup) for c in '\r\n"&|<>'):
            raise RuntimeError("No valid Visual C++ developer environment found")
        # Only a verified toolchain-owned path enters cmd. Never log the captured environment.
        command = f'call "{setup}" -arch=x64 -host_arch=x64 >nul && set'
        # cmd uses its own quoting grammar; Python's argv quoting escapes embedded
        # quotes in a way cmd does not understand. This command contains only the
        # validated VS path above and fixed flags, never user arguments.
        text = subprocess.run('cmd.exe /d /s /c "' + command + '"', cwd=ROOT,
                              env=env, capture_output=True, text=True, check=True,
                              timeout=60).stdout
        for line in text.splitlines():
            if "=" in line and not line.startswith("="):
                key, value = line.split("=", 1)
                if key.upper() == "PATH": key = "PATH"
                env[key] = value
    for tool in ("cmake", "ninja", "cl", "dumpbin"):
        if not shutil.which(tool, path=env.get("PATH")):
            raise RuntimeError(f"Developer tool not found: {tool}. See docs/engine/WINDOWS_EXPORT.md")
    return env

def native_build(preset, env):
    run(["cmake", "--fresh", "--preset", preset], env=env)
    run(["cmake", "--build", "--preset", preset, "--parallel", "4"], env=env)
    run(["ctest", "--preset", preset], env=env)
    suffix = {"windows-testing": "testing", "windows-development": "development", "windows-headless": "headless"}[preset]
    directory = ROOT / "build-native" / suffix
    test_env = dict(env, STELLAR_NATIVE_EXE=str(directory / "stellar-continuum.exe"))
    run([sys.executable, ROOT / "tools/stellar-export/test_export.py", "-v"], env=test_env)
    return directory

def executable_dependencies(executable, env):
    data = executable.read_bytes()
    if len(data) < 64 or data[:2] != b"MZ": raise RuntimeError("Export is not a Windows PE executable")
    offset = struct.unpack_from("<I", data, 0x3c)[0]
    if offset + 6 > len(data) or data[offset:offset+4] != b"PE\0\0" or struct.unpack_from("<H", data, offset+4)[0] != 0x8664:
        raise RuntimeError("Export executable is not AMD64/x86_64")
    output = run(["dumpbin", "/nologo", "/dependents", executable], env=env, capture=True)
    dependencies = sorted(set(re.findall(r"(?im)^\s+([a-z0-9_.-]+\.dll)\s*$", output)))
    if not dependencies: raise RuntimeError("Could not identify executable imports")
    unsupported = [name for name in dependencies if name.lower() not in SYSTEM_DLLS and not name.lower().startswith("api-ms-win-")]
    if unsupported: raise RuntimeError("Unpackaged runtime dependencies: " + ", ".join(unsupported))
    return dependencies

def hashes(folder):
    return [{"path": p.relative_to(folder).as_posix(), "bytes": p.stat().st_size,
             "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
            for p in sorted(folder.rglob("*")) if p.is_file() and p.relative_to(folder).as_posix() != "build-manifest.json"]

def validate_manifest(folder):
    folder = folder.resolve()
    manifest = json.loads((folder / "build-manifest.json").read_text(encoding="utf-8"))
    expected = manifest["files"]
    if len({row["path"] for row in expected}) != len(expected): raise RuntimeError("Duplicate manifest paths")
    for row in expected:
        relative = Path(row["path"])
        path = folder / relative
        if relative.is_absolute() or ".." in relative.parts or not path.resolve().is_relative_to(folder):
            raise RuntimeError("Unsafe manifest path")
        if any(parent.is_symlink() or parent.is_junction() for parent in [path, *path.parents] if parent != folder and parent.is_relative_to(folder)):
            raise RuntimeError("Linked files are not allowed in runtime exports")
    if hashes(folder) != expected: raise RuntimeError("Export files do not match the manifest")
    for row in expected:
        path = Path(row["path"])
        if path.suffix.lower() in {".cs", ".cpp", ".h", ".hpp", ".py", ".obj", ".lib", ".gd", ".gdshader"}:
            raise RuntimeError("Development-only file in runtime export")
        if path.suffix.lower() == ".pdb" and not manifest["includeSymbols"]:
            raise RuntimeError("Symbols in a public headless preset")
    if not (folder / "stellar-continuum.exe").is_file() or not (folder / "Configuration/runtime-config.json").is_file():
        raise RuntimeError("Missing required executable/configuration")
    return manifest

def relocated_smoke(folder):
    # Run an independent copy with only Windows system paths, no repository/toolchain cwd.
    with tempfile.TemporaryDirectory(prefix="stellar-export-smoke-") as temporary:
        root = Path(temporary); copy = root / "runtime"; shutil.copytree(folder, copy)
        env = {key: value for key, value in os.environ.items() if key.upper() in {"SYSTEMROOT", "WINDIR", "TEMP", "TMP", "USERPROFILE", "LOCALAPPDATA"}}
        windows = env.get("SystemRoot", env.get("SYSTEMROOT", r"C:\Windows"))
        env["PATH"] = str(Path(windows) / "System32")
        exe = copy / "stellar-continuum.exe"
        first = json.loads(run([exe, "--headless", "--systems", "500", "--ticks", "5", "--workers", "2", "--save", root / "roundtrip.scf"], cwd=root, env=env, capture=True, timeout=30))
        second = json.loads(run([exe, "--headless", "--ticks", "5", "--workers", "1", "--load", root / "roundtrip.scf"], cwd=root, env=env, capture=True, timeout=30))
        reference = json.loads(run([exe, "--headless", "--systems", "500", "--ticks", "10", "--workers", "4"], cwd=root, env=env, capture=True, timeout=30))
        if second["checkpointHash"] != reference["checkpointHash"] or second["distanceSum"] != reference["distanceSum"] or first["completedTicks"] != 5:
            raise RuntimeError("Relocated headless save/restore or worker determinism failed")
        return {"relocatedLaunch": True, "restrictedPath": True, "checkpointRoundtrip": True,
                "cleanMachineTest": "Separate machine/VM still required; restricted-PATH test is not full clean-machine certification"}

def export(preset_name):
    config = json.loads((ROOT / "export/stellar-presets.json").read_text(encoding="utf-8"))
    preset = config["presets"].get(preset_name)
    if preset is None: raise RuntimeError("Unknown export preset")
    if "blocked" in preset: raise RuntimeError(preset["blocked"])
    env = build_environment(); directory = native_build(preset["configurePreset"], env)
    exe = directory / "stellar-continuum.exe"; dependencies = executable_dependencies(exe, env)
    version = json.loads((ROOT / "export/runtime-config.json").read_text(encoding="utf-8"))
    commit = run(["git", "rev-parse", "HEAD"], capture=True).strip()
    dirty = bool(run(["git", "status", "--porcelain", "--untracked-files=normal"], capture=True).strip())
    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    output = ROOT / "Builds/Windows" / f"StellarContinuum-{preset_name}-{commit[:8]}-{stamp}"
    output.mkdir(parents=True, exist_ok=False)
    try:
        shutil.copy2(exe, output / exe.name)
        (output / "Configuration").mkdir()
        shutil.copy2(ROOT / "export/runtime-config.json", output / "Configuration/runtime-config.json")
        (output / "README.txt").write_text(f"Stellar Engine {version['engineVersion']} headless migration foundation.\nGame reference {version['gameVersion']}.\nRun stellar-continuum.exe --headless. This is not the graphical game.\nWindows 10/11 x64 required. No Godot, .NET, compiler, CMake, Ninja, Vulkan SDK or Python required at runtime.\n", encoding="utf-8")
        if preset["includeSymbols"]:
            for symbol in directory.glob("stellar-continuum*.pdb"): shutil.copy2(symbol, output / symbol.name)
        manifest = {"schemaVersion": 1, "gameVersion": version["gameVersion"], "engineVersion": version["engineVersion"],
                    "sourceCommit": commit, "sourceDirty": dirty, "contentVersion": "foundation-1", "preset": preset_name,
                    "configuration": preset["configurePreset"], "architecture": "x86_64", "mode": preset["mode"],
                    "includeSymbols": preset["includeSymbols"], "builtAtUtc": stamp, "windowsSystemDependencies": dependencies,
                    "gameplayParity": False, "shadersRequired": False, "graphicalAssetsRequired": False,
                    "files": hashes(output)}
        (output / "build-manifest.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
        validate_manifest(output)
        smoke = relocated_smoke(output)
        if preset.get("benchmark"):
            smoke["foundationBenchmarks"] = [json.loads(run([exe, "--headless", "--systems", count, "--ticks", "100", "--workers", "4"], env=env, capture=True)) for count in (100, 500, 1000, 2500, 5000)]
        (output.parent / (output.name+"-validation.json")).write_text(json.dumps(smoke, indent=2)+"\n", encoding="utf-8")
        archive = shutil.make_archive(str(output), "zip", output)
        print(json.dumps({"export": str(output), "archive": archive, "validation": smoke}, indent=2))
    except Exception:
        # Retain failed output for diagnosis, but never label/package it as validated.
        (output / "EXPORT_FAILED.txt").write_text("Export failed validation; do not distribute. See terminal diagnostics.\n", encoding="utf-8")
        raise

def main():
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument("command", choices=["export","validate","build"]); parser.add_argument("target")
    args=parser.parse_args()
    if args.command=="export": export(args.target)
    elif args.command=="validate": validate_manifest(Path(args.target)); print("Manifest validated")
    else: native_build(args.target,build_environment())

if __name__ == "__main__":
    try: main()
    except Exception as error:
        print(f"Stellar export failed [{type(error).__name__}]: {error}", file=sys.stderr); sys.exit(1)
