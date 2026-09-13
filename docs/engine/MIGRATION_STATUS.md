# Stellar Engine migration status

Latest clean packaged checkpoint: engine 0.1.15, commit `c77d185e0e932bc2189dd4ee31ea84cf2e12ffd5`, package `Builds/Windows/StellarContinuum-windows-benchmark-c77d185e-20260913T053859427176Z`, `sourceDirty: false`, 37/37 CTest and 19/19 Python checks, seven sealed runtime files and successful relocated fresh-campaign validation. This includes freight and settlement eligibility over fresh initialization, not a full campaign tick or player save.

Engine 0.1.16 settlement planning and stateful combat are integrated and pass 39/39 CTest and 19/19 Python checks. Their validation documents identify the actual-C# cases, source-only null observations and explicit native safety boundaries. An exact-commit package is the next check.

Colony establishment/commands (031), strategic intent/providers (032) and observer-local strategy planning (033) are in progress. These identifiers are not release versions. Full native campaign scheduling, strategic AI composition, matched combat commands, adaptive research, persistence and SDL3/Vulkan presentation remain open. The coordinator contract records existing implementations separately from missing bridges. PR #325 is draft/unmerged; the Godot playable baseline remains intact. Territorial PR #323 is paused with enclosed pockets unresolved.
