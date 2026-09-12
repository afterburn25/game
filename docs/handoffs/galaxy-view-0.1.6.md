# Galaxy presentation repair — 0.1.6 Alpha

Base: integration `7be839d3` (0.1.5 Alpha, PR #318).
Branch: `work/galaxy-view-repair`.

## Reported regressions

The 500-system profile made the overview artwork rectangle zero-sized and explicitly
excluded that profile from galaxy rendering. This removed the galaxy rather than
fitting the existing artwork to the new map. The regional zoom ceiling was 48, and
unidentified selections exposed synthetic labels such as `CATALOG 001` and
`ASTRONOMICAL TARGET` instead of player-facing unknown status.

## Repair scope

Restore the existing spiral galaxy artwork in the overview, using a shared fitted
frame for artwork and camera navigation. Preserve the measured 3D travel distances,
star identities, save data, survey rules, and established solar-system sun rendering.
The spiral is a strategic presentation of the playable map; the physical source
remains the 500 nearby-star catalogue documented in the 0.1.5 handoff.

Expand cursor-anchored regional zoom and retain selection, dragging, known-system
entry, and unknown-system privacy. Use `Unknown` consistently for unidentified
map selections and inspection output. Keep the cached star rendering introduced
by the preceding performance repair.

## Release verification

Record focused native overview/zoom/privacy captures, affected validation suites,
performance evidence, and hosted release/package receipts here or in the linked PR
before delivering the repaired Windows build. Do not reuse the 0.1.5 ZIP.
