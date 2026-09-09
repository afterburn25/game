# Colony surface interface — first implementation

The surface now opens onto its 3D settlement with a compact header and command bar.
Build, Projects and Overview share one right-side panel. Selecting a catalogue
card replaces it with the actual placement costs and power consequences; selecting
a structure shows construction progress or operating status. Essential actions
remain outside the scrolling detail area.

## Implemented behaviour

- Build catalogue starts closed and contains the five existing building families.
- Credits authorize a site immediately; Industry is consumed as construction advances.
- Placement previews give a **minimum** construction duration derived from the real
  30-Industry/site/day ceiling. Shared Industry availability can extend it.
- Power previews run the existing power allocator on a copied colony. They include
  district bonuses and keep other unfinished sites unfinished. The UI states that
  these are consequences for the current colony, not a prediction of future work.
- Upgrades have a separate preview and confirmation. Existing upgrades apply
  immediately and consume stored Credits and Industry; no new upgrade queue is implied.
- Projects lists actual incomplete sites with progress and remaining Industry.
  Sites share resources rather than executing as a serial queue.
- Colony, Power and Construction layers use real completion and power state.
  Default labels appear on selected, incomplete or offline structures. Power rings
  pair colour with ON/OFFLINE or pending text; the game does not simulate power cables.
- The header shows population, power balance, reserves, facility count and readiness.
  Overview shows current output, specialization and habitat support-cost reduction.
- Center hub eases over 0.6 seconds and preserves yaw; manual camera input interrupts it.
- Less motion stops settlement traffic animation, the operating fabrication crane,
  and scaffold scanning, and makes Center hub immediate. It does not pause gameplay.
- Camera and UI preferences remain transient; simulation rules and save formats are unchanged.
- Cosmetic settlement towers yield to player facility footprints, avoiding visual overlap.

## Verification

- Game.csproj compiles with zero warnings/errors.
- Core runtime checks: 38/38, including mutation-free preview, third-generator
  district bonus, unfinished-neighbour exclusion and power-deficient upgrades.
- Visual asset validator and all 22 Godot evidence-contract checks pass.
- The full local screenshot journey stopped at the pre-surface
  `regional-wheel-button-zoom-parity` check. This is not a successful full-campaign gate.
- Focused real-engine verification uses `STELLAR_CAPTURE_SURFACE_ONLY=1` with
  `tools/ScreenshotCapture.tscn` and a separate `APPDATA` directory. It initializes
  an isolated Developer campaign and explicitly uses Developer completion commands;
  placement, project selection, upgrade confirmation, layers and camera actions use
  actual pointer input. Its completion marker and result file are distinct from
  the full campaign acceptance lane.
- The focused Windows run passed all ten interaction groups, with eight reviewed
  1280×720 captures. Upgrade confirmation is held across snapshot refreshes to
  guard against a visibility refresh cancelling its click.

The default full screenshot lane remains enabled and its surface journey has been
updated to open the collapsed catalogue and confirm upgrades through the real UI.
Hosted full-scene and exported Windows acceptance should be reviewed before integration.

## Next surface-design stages

These are approved design directions, not implemented mechanics in this change:

1. Planet overview with several physical regions and settlements, including established
   populations on Earth. Preserve the distinction between a planetary population and
   the bounded settlement illustration currently rendered.
2. Survey-backed resource and hazard layers, meaningful site tradeoffs, and exact-world
   geology for mining, flood protection and geothermal installations.
3. District expansion with automatic small buildings, services and roads. Visual growth
   must follow population and facility state; decorative geometry must not obstruct
   legitimate saved building footprints.
4. Human and Thalori architecture appropriate to atmosphere, pressure and ocean chemistry;
   later multi-species districts and compatible habitat support.
5. Exploration discoveries, settlement milestones and optional governor budgets.

Do not add illustrative mockup housing totals, arbitrary build costs, fake research
production, decorative resource deposits or nonfunctional hazard buttons to the live UI.
