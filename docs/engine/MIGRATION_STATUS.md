# Stellar Engine migration status

Latest clean packaged checkpoint: engine 0.1.13, commit `33a0f639f2ba58807bbaccc57395c8d127c49174`, package `Builds/Windows/StellarContinuum-windows-benchmark-33a0f639-20260913T050731985374Z`, `sourceDirty: false`, 33/33 CTest and 19/19 Python checks. This includes civilian recovery and exploration planning over complete fresh initialization, not a full campaign tick or player save.

Engine 0.1.14 exploration advancement and legacy research progression are integrated and pass 35/35 CTest and 19/19 Python checks. Their validation documents identify the actual-C# cases, source-only null observations and explicit native safety boundaries. An exact-commit package is the next check.

Freight (026) is addressing review gaps; settlement knowledge (028) is underway and settlement planning (029) has an implementation contract. These identifiers are not release versions. Full native campaign scheduling, combat, colonization, strategic AI, adaptive research, persistence and SDL3/Vulkan presentation remain open. PR #325 is draft/unmerged; the Godot playable baseline remains intact. Territorial PR #323 is paused with enclosed pockets unresolved.
