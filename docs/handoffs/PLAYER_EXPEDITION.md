# Ordinary Player Expedition Handoff

Status: bounded runtime foundation and a fast Developer-labelled card-stability fixture
validated; full ordinary Player expedition evidence remains pending.

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

Pure validation recorded at `7cda986`:

- CoreRuntime: 77/77, exit 0.
- Simulation: 70/70, exit 0.
- Quality: 19/19, exit 0.
- Logistics: 4/4, exit 0.
- Python Godot smoke validators: 22 tests, exit 0.

The Player-expedition evidence validators later reached **27/27** at
`f906fe2`; that count is not evidence from `7cda986`.
The focused `STELLAR_CAPTURE_FOCUS=project-card-stability` fixture passed at
`96e7891`, exit 0, with clean terminal diagnostics. It uses an explicitly
Developer-labelled campaign and visible production
callbacks to verify that ship choice cards retain their button identities, current labels,
current affordability, current callbacks, grid identity, and keyboard focus across queue
insertion, cancellation, promotion, and refund. It is a fast presentation regression, not
ordinary Player progression proof.

Superseded native attempts are not successful evidence: an earlier standard screenshot
suite let valid 24× milestone notifications arrive after the notification center cleared
unread items. Separately, an ordinary Player expedition exposed
`ProjectCard.UpdateChoices` rebuilding and disposing a visible ship command
between a real pointer reveal and click. The production fixes preserve keyed controls and
focus; they do not alter the simulation. The next ordinary Player capture is still pending
on the integrated candidate and must produce a fresh manifest and logs before it is claimed
as passing evidence.

The later ordinary run at runtime `96e7891` reached the first scout order, then failed
because a deferred scroll restoration left the next science card clipped. The test had
not pressed that command yet. The capture helper now waits for enclosure by the whole
scroll chain before pointer input; its expanded fast fixture is still being validated.
The hosted standard screenshot run at `07cd924` separately exhausted its 600-second
software-rendering allowance while progressing through surface output and save/reload.
The allowance is now bounded at 1200 seconds, with all evidence checks retained.

At `9548232`, CoreRuntime 77/77, Simulation 70/70, Quality 19/19, Logistics 4/4,
and Python evidence validators 27/27 all passed. That revision also changes the guide to
offer ordinary 8× fast-forwarding with pause-to-review instructions. The display reports
approximately 12–13 active minutes before decisions. This guidance does not establish
a guaranteed 30-minute player experience; the full native journey and human pacing
review remain required.

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

The bundled main score is the user-provided `claimed-by-the-void-loop.mp3`, whose original
SHA-256 is `25C81BEE74C37DC91F0895FA68DB72B026C028C65D951634D07CD4AE0B325FA2`.
The `Dummy` audio driver is appropriate for UI/capture determinism only and cannot support
a claim about actual audio playback or mixing.
