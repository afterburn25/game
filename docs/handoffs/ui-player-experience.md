# UI / Player Experience handoff

Date: 2026-09-08
Branch: `work/ui-player-experience`
Accepted baseline: `c529a1a765776c0940f88002410bc70db740d05a` (`integration`)
Tracking: UI #27; Visual #217; Galaxy #219 / PR #220

## Completed milestone

Resolved the placement cause identified by Visual's real Godot screenshot run
`34253094688`: the 1200-pixel-wide shipyard label on CanvasLayer 20 at y=198
previously crossed the command panel beginning at y=194.

- `Main.Shipbuilding.cs` publishes the existing formatted status through
  `UiShipbuildingSummary`, updated at the same physics cadence. `PlayerControls`
  renders it as a wrapping row between supply status and time controls. The old
  independently positioned label has been removed.
- `CampaignSidebar` owns the layout of the existing command and exploration
  panels in one vertical container with a 16-pixel gap. Their existing script
  nodes remain direct children of `Main`, preserving screenshot-driver lookups.
- The sidebar keeps the existing x=16 / y=194 origin and preferred width of 684.
  It reserves the right panels' 390-pixel inset plus a 16-pixel gap, reduces width
  when needed, and ends 40 pixels above the viewport bottom. At the project's
  1280x720 logical viewport it is 684x486; at 1024x720 it is 602x486; at 960x540
  it is 538x306. These are structural dimensions, not measured screenshots.
- Horizontal control rows now wrap. Status labels no longer impose a 650-pixel
  minimum on exploration. Vertical overflow scrolls and keyboard focus follows
  the focused control, so extra status lines or wrapped controls cannot push the
  exploration panel over the command panel or make its actions unreachable.

No simulation ownership, command handlers, shipyard state, save format, science
marker implementation, foreign information boundary, button labels or command
bindings changed. The shipyard row inherits the common Label theme rather than
retaining a separately positioned cyan/font-size override.

## Checks and limits

- Reviewed the latest recovered #27 comments and Visual candidate `4bdbd8a`,
  including `docs/SCREENSHOT_VISUAL_QA_2026-09-08.md` and icon-button changes.
- `git diff --check` passes. Inspected all modified layout and scene wiring.
- Compared the shipbuilding command block, science-marker method and formatted
  status strings against the accepted baseline; they are preserved.
- The sidebar appears before the two consuming script nodes in `Main.tscn`, so
  its container is ready when their `_Ready` methods register their panels.
- Local Godot executable / Godot.NET.Sdk 4.7.2 package is unavailable. This
  milestone has **not** been compiled or rendered locally. Core must run the
  pinned build/editor/runtime CI and real screenshot workflow on the combined
  candidate before acceptance. No visual-pass claim is made here.
- The existing right-side panels and top text retain their own layouts; this is
  a left-column fix. Extremely narrow logical viewports below the individual
  controls' minimum widths are not an accessibility redesign or a new supported
  viewport guarantee.

## Integration dependencies

- No dependency on unmerged Visual symbols is introduced. Visual's shared theme
  and icons should remain when Core combines these changes; the wrapping rows
  accommodate their button widths. Keep Visual's higher main-menu CanvasLayer.
- Galaxy is separately guarding system-view input and science-marker visibility.
  Preserve that work in `Main.Shipbuilding.cs`; this milestone changes only the
  status label lifecycle and renames its layer-initialization helper to
  `EnsureScienceFleetMarkerLayer`. The layer and science marker method remain.
- The status remains read-only at the UI boundary. New military controls must
  use `GalaxySimulationStepCoordinator.GetOwnCombatFleetStatus`, Core preview,
  and Core issuance. Exact foreign Attack target discovery remains blocked on
  an observer-safe target-identity contract, as recorded in #27.

## Next milestone

Core: combine with Visual and Galaxy, run CI and capture campaign/colony/relations
screens. Verify the shipyard text stays inside its row for locked designs,
selected designs and active construction; verify wheel and keyboard scrolling
keep the colony controls reachable at the default viewport and after resizing.
Then continue UI-owned navigation/accessibility work in coordination with the
Galaxy spatial-navigation adapter, without introducing another spatial canvas.

This workstream did not push or modify `main` / `integration`.
