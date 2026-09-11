# Visual, voice and music polish handoff

Source milestone: Core branch `work/cohesive-visual-polish`, based on `10bedd6`.

This handoff records the combined presentation milestone for integration review.
It describes validated behavior and known limits; it does not promote the
procedural presentation to finished production art.

## Validated presentation work

- Core's full native capture validated exactly 33 expected-head images and 120
  real input checks. The immersive camera review passed all nine captures.
- Surface presentation now includes more detailed procedural canopies, entrances,
  podiums and roofs. Completed modules receive automatically generated access-road
  presentation geometry. Roads are presentation-only: there is no manual road
  drawing and no production or transport effect. The route solver has a 32-route
  cap, a bounded 18,000-node search, and preserves exact route endpoints; its
  obstacle regressions pass.
- UI work refined the palette, cards and compact header. Galaxy presentation uses
  cached smooth colored stars, safer 2D orbit fitting and a continuous galaxy
  shader with a compact 100-system shape matching the catalog. Ship trails follow
  authoritative active route positions and naturally remain stationary while paused.
  Existing 2D/3D navigation and saves remain intact.
- The existing private aged-save fix remains required and must be preserved.

The surface remains procedural and the galaxy still needs future custom
production artwork. These changes do not claim photorealism or finished AAA
presentation quality.

## Voice validation

Core's processed voice capture (from `aa53fac`) reported narrator peak `0.6251`,
grey `0.5363`, and alien `0.8913`. Runtime validation confirms one non-spatial dry
voice player, with no Chorus, Reverb or Delay effects. The Voice bus uses a
`-1 dB` post-DSP hard limiter. The prior echo was traced to an added 18 ms /
27 ms chorus plus reverb; the source path is dry.

## Music integration

The main score is the user-supplied `claimed_by_the_void_loop.mp3`, copied
without transcoding to `assets/audio/music/claimed-by-the-void-loop.mp3`.
SHA-256:

`25C81BEE74C37DC91F0895FA68DB72B026C028C65D951634D07CD4AE0B325FA2`

The runtime uses one non-spatial primary music player and Godot's MP3 loop flag.
Menu/game context changes retain the same player and playback position. Existing
music volume, voice ducking, SFX and voice-bus behavior remain connected. The
older WAV score files are retained as superseded historical assets. Provenance
records the user-supplied filename and hash only; no creator, license, CC0 or
original-synthesis claim is made.

## Automated validation

The following checks are attributed to the corresponding validation runs:

- Graphics full native capture on `10bedd6`: 33/33 expected-head images and
  120/120 real-input checks; immersive camera: 9/9 captures.
- Voice runtime on `aa53fac`: processed capture and single-dry-player checks.
- Plain-suite validation on this branch before presentation-only follow-ups:
  Quality 19/19, Simulation 70/70, Core 72/72, VoiceCore 12/12, and Godot
  Smoke 22/22.

Automated checks use isolated user profiles and Dummy audio output to avoid
unsolicited clicks. They do not change the user's audio settings.

## Pending native results

Native music/full capture and aged performance captures at 1080p and 1440p
were pending at handoff time. Update this section with the native run IDs,
image/check counts, frame-time or performance results, and any music loop or
audio-bus findings after those runs. The pending work must retain the exact
33-image capture target and the existing real-input checks.
