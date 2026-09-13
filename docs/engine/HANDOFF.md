# Stellar Engine migration handoff

Clean engine 0.1.12 is committed as `73aaa81344bf15b77487dcd65fb1a543e3bc47d6`. Its benchmark package is `Builds/Windows/StellarContinuum-windows-benchmark-73aaa813-20260913T044030295559Z`, with `sourceDirty: false`, 30/30 CTest and 19/19 Python checks, seven sealed runtime files, and a successful relocated fresh-campaign diagnostic. See FRESH_CAMPAIGN_VALIDATION.md for exact evidence and scope. The preceding 0.1.11 checkpoint and all six successful CI workflows are recorded there as well.

Engine 0.1.13 adds civilian hold/resume/return commands, survey operational profiles, mission planning and AI destination reservations. The maintained testing build passes 33/33 CTest and 19/19 Python checks. See EXPLORATION_PLANNING_VALIDATION.md for retained fixture counts and safety-boundary distinctions.

Next: publish and verify an exact-commit 0.1.13 export, then promote reviewed gate 025 exploration advancement. Its ignored draft has 89 actual-C# cases plus three source-only null observations and two explicit native safety-boundary checks. Gate 026 freight and gate 027 legacy research progression are being ported against the authoritative C# source. Gate numbers identify work, not engine versions. Full campaign scheduling still requires the remaining combat, colonization, strategic AI and persistence work; do not claim partial advancement is a full game loop.

PR #325 remains a draft against integration. No integration/main merge or Godot removal is authorized before full parity. The existing Godot 0.1.7 Alpha remains the playable baseline. Territorial PR #323 is paused with enclosed pockets unresolved. No native graphical, player-save, playable-runtime or FPS parity claim applies.
