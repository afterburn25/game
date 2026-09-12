# Stellar Engine Windows export

Engine 0.1.0 foundation; game reference 0.1.7 Alpha. The native output is a console/headless distance-validation host, **not the graphical Stellar Continuum game**. Existing Godot exports remain the playable baseline. Full migration is blocked until the graphical release and clean-machine gates below pass.

## Developer setup

Windows x64; Visual Studio 2022 Build Tools (Desktop development with C++, x64 compiler and Windows SDK), Python 3.12+, CMake 3.28+ and Ninja. Tested locally with MSVC 19.44.35228, CMake 4.4.3 and Ninja 1.13.2. The exporter locates a registered C++ toolchain through vswhere and configures its environment; an incomplete Visual Studio installation is not a valid compiler.

From repository root:

```powershell
python -m venv .tools/build-tools
.tools/build-tools/Scripts/python.exe -m pip install cmake==4.4.3 ninja==1.13.2
python tools/stellar-export/stellar.py build windows-testing
python tools/stellar-export/stellar.py export windows-headless
python tools/stellar-export/stellar.py export windows-benchmark
```

The native build also supports standard CMake configure/build/CTest presets from an x64 developer prompt. `--fresh` refreshes compiler detection so a stale/incomplete toolchain cache cannot silently persist. Configuration takes place in ignored `build-native/`; no existing game project is overwritten.

| Export preset | Current behavior |
| --- | --- |
| windows-development | Debug native foundation; application PDB included |
| windows-testing | RelWithDebInfo foundation; runtime package excludes symbols |
| windows-headless | Release foundation; runtime package excludes symbols |
| windows-benchmark | Release foundation plus 100/500/1000/2500/5000-position distance microbenchmarks |
| windows-release | Fails with an explicit graphical-parity explanation; never substitutes headless output for a playable game |
| windows-steam | Deferred and explicitly rejected |

Presets live in `export/stellar-presets.json`; CMake configurations live in `CMakePresets.json`. Each successful export produces a unique directory and ZIP under `Builds/Windows`, plus a validation JSON sidecar. The directory contains `stellar-continuum.exe`, `Configuration/runtime-config.json`, a README and `build-manifest.json`. The configuration currently describes the host/version; scenario options are command-line controlled. There is no native video/settings subsystem yet.

## Package guarantees and validation

The runtime links the C++ runtime statically and only imports allowlisted Windows system DLLs. PE machine type must be AMD64. No Godot, .NET, Python, compiler, CMake, Ninja or Vulkan SDK is needed by this native executable. Non-system dependencies cause export failure until deliberately supported and packaged; the exporter does not copy arbitrary developer DLL directories.

Version resources embed game/engine versions and source commit. The manifest records source commit, dirty state, configuration, UTC build ID, content version, mode, architecture, runtime imports and SHA-256/size of every runtime file. Explicit fields state gameplay parity is false and graphics/shaders are not yet required by this headless host. Only selected runtime files are copied. Manifest checks reject tampering, omitted/extra files, duplicate/escaping paths, source/object files and public symbols. Validate an existing directory with:

```powershell
python tools/stellar-export/stellar.py validate Builds/Windows/<export-directory>
```

Build/export runs CTest and Python integrity/recovery checks. Then an independent copy launches in a temporary folder with a Windows-system-only PATH. It creates a foundation checkpoint, restores it using a different worker count and compares against uninterrupted execution. Failures produce a terminal error and nonzero status; partial output is marked `EXPORT_FAILED.txt` and is not zipped as validated.

This relocated test is **not clean-machine certification**: it runs on the development machine. A separate Windows VM/device without development tools remains a required release gate. Hashes detect integrity changes; they are not a digital signature/authenticity guarantee.

## Headless use

```powershell
.\stellar-continuum.exe --version
.\stellar-continuum.exe --headless --systems 500 --ticks 100 --workers 4
.\stellar-continuum.exe --headless --ticks 5 --save first.scf
.\stellar-continuum.exe --headless --ticks 5 --load first.scf --save second.scf
```

Foundation checkpoints are versioned/checksummed synthetic distance scenarios. They deliberately reject game save-v16, malformed/truncated data and existing output paths. They are not campaign saves. An existing file is preserved; choose a new output path. Pending writes are retained for diagnosis. CLI failures report exception type, message, engine/source and working directory.

Benchmark results measure distance work, job dispatch and deterministic merging only. They do not establish full simulation throughput, render FPS, fleet battle, economy, civilization AI or save-v16 performance. Those scenarios must be ported before adding representative benchmark presets. JSON contains real measured elapsed/tick mean time, worker count and deterministic result checksums.

## Remaining graphical release gates

1. SDL3/window/input and Vulkan renderer parity, then UI/audio adapters, reviewed licenses and explicit runtime dependency collection.
2. Asset-ID preserving runtime packs, texture/audio conversion and precompiled shader deployment; validate missing assets/shaders before player launch.
3. Real campaign/save-v16 migration and full game-loop regression checks.
4. Branded executable icon, graphical startup, settings defaults, crash dumps/build correlation and separate developer symbols.
5. Exported visual/input/audio/save smoke tests plus separate clean Windows machine launch and realistic CPU/GPU/memory benchmarks.
6. Only then enable `windows-release` and consider old-engine removal. Linux/macOS exports remain future backends; Core and Engine foundations contain no Windows gameplay logic.
