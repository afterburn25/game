# Ordinary Player Expedition Handoff

Status: bounded runtime foundation validated; full native expedition evidence pending.

Pure checks in this handoff were run at local revision `7cda986`.
The ordinary Player case now exercises the same string-seed bootstrap used by New Game:
`CampaignSessionService.CreateNew("20260908")` in
[`CampaignSessionService.cs`](../../src/Game/Campaign/CampaignSessionService.cs).
The legacy generator case remains separately covered, so its timing must not be reported
as proof for the real Sandbox profile.

The maintained CoreRuntime progression check is
[`DemoProgressionValidation.cs`](../../tests/Game.CoreRuntime.Validation/DemoProgressionValidation.cs).
It uses the authoritative Adaptive Research runtime, quarter-day coordinator steps, real
research/construction/ship authorizations, physical scout reconnaissance, detailed science
survey, and the authoritative colony order. It does not grant resources or target hidden
world state. The ordinary Sandbox reaches its first extrasolar colony at day 5978.5 after
6 surveys, approximately 12.46 active minutes at 8× (99.64 minutes at normal speed).

The opening guidance and public-name fixes are present in
[`DemoObjectiveView.cs`](../../src/Game/Campaign/DemoObjectiveView.cs),
[`Main.Shipbuilding.cs`](../../src/Game/Presentation/Main.Shipbuilding.cs), and
[`Main.Exploration.cs`](../../src/Game/Presentation/Main.Exploration.cs). Research guidance
now reports the active funding/commitment state, paused research explains its recovery
action, and route feedback resolves public catalog names instead of exposing internal
system numbers. Deferred research-scroll restoration is covered by the current candidate.

Pure validation at this head:

- CoreRuntime: 77/77, exit 0.
- Simulation: 70/70, exit 0.
- Quality: 19/19, exit 0.
- Logistics: 4/4, exit 0.
- Python Godot smoke validators: 22 tests, exit 0.

The full native Player expedition journey remains pending and should be run as its own
real-input evidence. It must use ordinary pointer controls and the canonical route:
research → construction → shipyard → select a physical scout/science/colony vessel →
right-click a named star → complete reconnaissance and detailed survey → right-click the
named world → save/reload. Do not use Developer grants as Player progression proof.

For the focused native run, use the pinned Godot 4.7.2 GUI under an offscreen display,
not `--headless`, with `--audio-driver Dummy`, an isolated writable profile, and
`STELLAR_CAPTURE_FOCUS=player-expedition`:

```sh
STELLAR_CAPTURE_FOCUS=player-expedition \
STELLAR_SCREENSHOT_DIR="$PWD/player-expedition-artifacts" \
xvfb-run -a -s "-screen 0 3840x2160x24" godot \
  --audio-driver Dummy --path . --resolution 1280x720 \
  --rendering-method gl_compatibility tools/ScreenshotCapture.tscn
python3 scripts/validate_player_expedition_capture.py \
  player-expedition-artifacts --expected-sha "$EXPECTED_SHA"
```

On Windows, provide the equivalent isolated `APPDATA`/`LOCALAPPDATA` profile and GUI
capture environment; do not claim real-audio validation from the Dummy driver. Keep the
focused expedition validator separate from `validate_screenshot_capture.py` and from
headless startup validation. The maintained screenshot harness is the source of native
interaction evidence; no visual-quality or “fun” completion claim follows from the pure
progression pass alone.
