# Playable demo milestone

The current milestone is one understandable, reliable campaign loop:
start a demo, develop warp capability, build physical ships, reconnoitre a star,
complete a science survey, settle an eligible body, then save and resume.
The Windows download must run without a developer environment.

## Priorities

1. Stable startup and safe progress: complete resource imports, reject semantic
   runtime errors, confirm campaign replacement, and keep the game open if an
   exit save fails. Demo and normal campaigns use separate save slots.
2. A short, discoverable opening: explicit Play Demo / Continue Demo, a
   reproducible scenario and optional accelerated clock, visible next steps,
   ship building and selected-star commands. Normal campaign rules remain intact.
3. A complete loop: real research, construction, resources, passenger population,
   observer knowledge and valid settlement commands. Tests must not grant the
   completion state they are supposed to verify.
4. A usable Windows package: self-contained runtime and public data, exact
   source revision, checksums, an actual Windows startup check and rendered
   interface review.

## Evidence behind the focus

Normal progression passed seeds 20260908, 12345 and 1337, including physical
ships, reconnaissance, detailed survey and settlement with 250M conserved
passengers. First settlement took 16.5–17.75 minutes at uninterrupted 4x.
Research waiting dominated the opening; accumulated Industry then completed
ships almost immediately. Additional logistics features do not solve this.

The first combined runtime failed because an editor scan was stopped before
SVG imports finished. Startup now waits for import completion and rejects
engine errors even when the process exits zero.

The recurring topology-checker CLR crash was repaired and validated separately
on the canonical research branch. That incident does not justify importing
unfinished research expansion into the demo.

## Deferred until the loop is playable

- Further Adaptive Research expansion or replacement of current gameplay research.
- The 1,000-system astronomy catalog and large-galaxy performance milestone.
- New diplomacy, combat, AI or logistics features that do not unblock this loop.
- Release to main. Reviewed work continues through integration.

Existing work and branch histories stay preserved. The Core handoff records
the exact candidate, completed checks and remaining demo gates.
