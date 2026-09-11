# Ordinary Player Expedition Handoff

Status: the ordinary Player expedition remains pending final native acceptance. Published PR #312
is a draft at `a19e5f62944cc8bf1395771a2bb3052d95fbc45a`. Hosted build, research, voice and
Windows gates passed (`34585287234`, `34585287215`, `34585287230`, `34585287238`); screenshot
`34585287157` was pending at the last check. The full native run at a19 stopped in
`ScreenshotCapture.FleetOrders.cs:47` after 24 screenshots because travel did not satisfy the
unfinished-route assertion after 20 frames. No root cause or fix should be implied yet.

Pure validation belongs to source `d7460f9`: CoreRuntime 79/79, Simulation 70/70, Quality
19/19, Logistics 4/4, Species checks passed and Python 32/32, all exit 0. At a19, the Debug
build passed with 0 warnings/errors, Python checks were 36/36, import exited 0, and the focused
checkpoint exited 0 with 20 checks, 3 images, route-recovery completion and empty stderr. The
focused early UI proof from `122f268` integrated by `d5dcc53` exits 1 as intended; the late
failure proof from `69805fa` also exits 1 with the source save unchanged. These receipts are
separate from combined Player acceptance.

The maintained pure Player case uses the actual `CampaignSessionService.CreateNew("20260908")`
bootstrap, including the 100-system Barred Spiral profile, Adaptive Research and diplomacy.
It reaches a first extrasolar colony through paid research, construction, physical ships,
reconnaissance, detailed surveys and authoritative settlement. This proves deterministic
simulation progression, not the visible Player journey. The native acceptance path remains:
research → construction → physical scout/science/colony orders → named-star travel →
reconnaissance and detailed survey → named-world settlement → save/reload with the same
people, ship identities, simulation day and application revision.

Generation investigation reproduced seeds `1789000000017` and `1789000000154`; 20
guarantee-altered bodies lost physical planet conditions for `SOL-ASCENDANT-42`. Galaxy
catalog v16, campaign v17 and fresh-home fallback v2 are implemented and reviewed; old saves
retain legacy reconstruction. The combined native and hosted evidence is still pending.

The focused early and late startup-failure proofs are complete as separate receipts. The
ordinary Player journey, package verification, Hold/Return 383 native proof and human pacing/
fun review remain pending. Next, diagnose the FleetOrders timing assertion before any retry.

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
