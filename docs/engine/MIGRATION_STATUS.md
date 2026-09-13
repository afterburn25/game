# Stellar Engine migration status

Latest clean packaged checkpoint: engine 0.1.14, commit `e4e37db8072e1ef8f3b139582dbade640338e463`, package `Builds/Windows/StellarContinuum-windows-benchmark-e4e37db8-20260913T052237075636Z`, `sourceDirty: false`, 35/35 CTest and 19/19 Python checks. This includes exploration advancement and legacy research over complete fresh initialization, not a full campaign tick or player save.

Engine 0.1.15 freight and settlement knowledge are integrated and pass 37/37 CTest and 19/19 Python checks. Their validation documents identify the actual-C# cases, source-only null observations and explicit native safety boundaries. An exact-commit package is the next check.

Settlement planning (029) and combat simulation (030) are completing source parity. These identifiers are not release versions. Full native campaign scheduling, colonization, strategic AI, matched combat commands, adaptive research, persistence and SDL3/Vulkan presentation remain open. The coordinator contract records existing implementations separately from missing bridges. PR #325 is draft/unmerged; the Godot playable baseline remains intact. Territorial PR #323 is paused with enclosed pockets unresolved.
