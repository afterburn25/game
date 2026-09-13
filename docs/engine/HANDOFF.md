# Stellar Engine migration handoff

Clean engine 0.1.13 is committed as `33a0f639f2ba58807bbaccc57395c8d127c49174`. Its benchmark package is `Builds/Windows/StellarContinuum-windows-benchmark-33a0f639-20260913T050731985374Z`, with `sourceDirty: false`, 33/33 CTest and 19/19 Python checks, seven sealed runtime files, and successful relocated fresh-campaign validation. See EXPLORATION_PLANNING_VALIDATION.md. Prior 0.1.12 clean evidence remains in FRESH_CAMPAIGN_VALIDATION.md. Five 0.1.12 CI workflows passed; screenshot CI was cancelled, not passed.

Engine 0.1.14 integrates actual exploration movement/discoveries and legacy research progression. The maintained testing build passes 35/35 CTest and 19/19 Python checks. See EXPLORATION_ADVANCE_VALIDATION.md and LEGACY_RESEARCH_VALIDATION.md for exact fixtures and boundaries. The next exact-commit export establishes the clean package; do not label earlier pre-commit evidence as clean.

Next: verify the exact-commit 0.1.14 export while freight gate026 fixes review gaps around real deposits, exception classification and complete partial-state checks. Settlement knowledge gate028 is underway; settlement planning gate029 has a retained contract ready for implementation. Gate numbers identify work, not engine versions. Full campaign scheduling still requires combat, colonization, strategic AI, adaptive research and persistence work; do not claim partial advancement is a full game loop.

PR #325 remains a draft against integration. No integration/main merge or Godot removal is authorized before full parity. The existing Godot 0.1.7 Alpha remains the playable baseline. Territorial PR #323 is paused with enclosed pockets unresolved. No native graphical, player-save, playable-runtime or FPS parity claim applies.
