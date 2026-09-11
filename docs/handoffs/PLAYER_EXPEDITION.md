# Ordinary Player Expedition Handoff

Status: the ordinary Player expedition remains pending final native acceptance. Published 3059
hosted gates were mixed: research, voice and Windows passed; the build gate failed during
runtime generation (the failing seed was not logged); and the screenshot gate failed at
`tools/ScreenshotCapture.Surface.cs:126` because its fixture still expected pause to resume
at Normal after the player had selected another speed. The reviewed local 2x pause/resume
fixture is present at current local `bf55fe7`; it records the selected running speed, pauses,
resumes it, then selects 8x for construction.

Pure validation for source `3059fd3` is complete and reproducible: CoreRuntime 77/77,
Simulation 70/70, Quality 19/19, Logistics 4/4 and Python evidence checks 32/32, all exit 0.
The real Load/revision/day-rollback evidence is implemented through `d11020c`, `a3762f2`
and `7f791e3`, with schema 3 reload receipts, but a fresh native Player journey is still
required. The local checkpoint capture at 3059 reached settlement with 20 checks, 3 images
and clean stderr, but its exit file was empty, so it is partial evidence only. The `f81c308`
package and checkpoint are superseded receipts.

The maintained pure Player case uses the actual `CampaignSessionService.CreateNew("20260908")`
bootstrap, including the 100-system Barred Spiral profile, Adaptive Research and diplomacy.
It reaches a first extrasolar colony through paid research, construction, physical ships,
reconnaissance, detailed surveys and authoritative settlement. This proves deterministic
simulation progression, not the visible Player journey. The native acceptance path remains:
research → construction → physical scout/science/colony orders → named-star travel →
reconnaissance and detailed survey → named-world settlement → save/reload with the same
people, ship identities, simulation day and application revision.

Generation investigation reproduced seeds `1789000000017`, `1789000000154` and `20`, where
physical planet conditions were lost for `SOL-ASCENDANT-42`. Fresh-home fallback `ff8848`
is under pruning review. Versioned full-catalog work v16/v17 is on a separate implementation
branch; old saves must retain legacy reconstruction. Do not treat any of these investigation
receipts as Player acceptance.

The early startup-failure path on Terra `f872b3f` exits 1 in about 1.7 seconds after import,
with the intended nested diagnostic, seed and save paths and no `STELLAR_RUNTIME_READY`.
The ordinary failure UI and a late-stage failure proof remain pending. Hold/Return 383 is a
separate unpublished change pending native validation.

The focused native run must use ordinary pointer controls and a canonical checkpoint, with no
Developer grants or hidden targeting. Use the pinned Godot 4.7.2 GUI under an offscreen
display, `--audio-driver Dummy`, an isolated writable profile, and the maintained validator:

```sh
STELLAR_CAPTURE_FOCUS=player-expedition \
STELLAR_SCREENSHOT_DIR="$PWD/player-expedition-artifacts" \
xvfb-run -a -s "-screen 0 3840x2160x24" godot \
  --audio-driver Dummy --path . --resolution 1280x720 \
  --rendering-method gl_compatibility tools/ScreenshotCapture.tscn
python3 scripts/validate_player_expedition_capture.py \
  player-expedition-artifacts --expected-sha "$EXPECTED_SHA"
```

On Windows, use the equivalent isolated `APPDATA`/`LOCALAPPDATA` profile. Dummy audio supports
capture determinism only; it does not establish real-audio playback. The native journey,
human pacing/fun review and production-quality content review remain open.

The bundled main score is the user-provided `claimed-by-the-void-loop.mp3`, whose original
SHA-256 is `25C81BEE74C37DC91F0895FA68DB72B026C028C65D951634D07CD4AE0B325FA2`.
The `Dummy` audio driver is appropriate for UI/capture determinism only and cannot support
a claim about actual audio playback or mixing.
