# Ordinary Player Expedition Handoff

Status: the ordinary Player expedition remains pending final native acceptance. Published PR #312
is still a draft at `3059fd3`: research, voice and Windows passed, while runtime generation
failed without a recorded seed and the screenshot gate failed its obsolete Normal-on-resume
assertion. The current Core tip is `d5dcc53234557913696c6e93e6b5bd682ac56d81`; do not call it
a combined native pass.

Current pure validation is clean: CoreRuntime 79/79, Simulation 70/70, Quality 19/19,
Logistics 4/4, Species checks passed, and Python evidence checks 32/32, all exit 0. A focused
startup-failure capture from source `122f268`, integrated by `d5dcc53`, is separately validated
at 720p with the exact diagnostic tip, strict validator and numeric exit 1; the source save is
unchanged. The latest focused UI checks total 36 Python tests and the Debug build has zero
warnings or errors. This focused result does not replace the pending combined native journey,
checkpoint, fresh schema 3 Player run, package verification or hosted gates.

The maintained pure Player case uses the actual `CampaignSessionService.CreateNew("20260908")`
bootstrap, including the 100-system Barred Spiral profile, Adaptive Research and diplomacy.
It reaches a first extrasolar colony through paid research, construction, physical ships,
reconnaissance, detailed surveys and authoritative settlement. This proves deterministic
simulation progression, not the visible Player journey. The native acceptance path remains:
research → construction → physical scout/science/colony orders → named-star travel →
reconnaissance and detailed survey → named-world settlement → save/reload with the same
people, ship identities, simulation day and application revision.

Generation investigation reproduced seeds `1789000000017`, `1789000000154` and `20`, where
physical planet conditions were lost for `SOL-ASCENDANT-42`. Galaxy catalog v16, campaign
catalog v17 and fresh-home fallback v2 are implemented and reviewed; old saves retain legacy
reconstruction. The combined native and hosted evidence is still pending, so these changes are
not by themselves Player acceptance.

The focused early startup-failure path is now covered by the separate 720p proof above. The
ordinary Player journey, late-stage failure proof and Hold/Return 383 remain pending native
validation. Human pacing/fun review and production-quality content review remain open.

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
