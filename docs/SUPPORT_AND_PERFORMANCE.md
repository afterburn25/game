# Support, Logging, and Performance

## Player support goals

Players should not need technical knowledge to provide useful diagnostics. The game will expose an Open Logs Folder action and Export Support Bundle action.

A support bundle may include, with the player's knowledge:

- game/build version
- session identifier
- operating system/version
- CPU and logical processor count
- GPU/vendor/graphics API
- display configuration
- settings relevant to rendering/simulation
- bounded recent game/simulation logs
- optional save/campaign context
- performance summaries

Avoid unnecessary personal data.

## Performance telemetry

Long campaigns should periodically sample:

- simulation tick duration
- subsystem timings (AI, economy, pathfinding, fleet movement, diplomacy/events)
- managed/native memory where available
- civilization, colony, fleet, and ship counts
- pathfinding queue depth
- route/cache sizes
- cleanup/compaction events
- requested versus effective game speed

## Cache discipline

Caches must be categorized as reusable, bounded, invalidated-on-change, or disposable under pressure. Old tactical detail may be compacted into historical summaries when full fidelity no longer affects gameplay. Persistent historical facts needed for diplomacy remain preserved.

## Late-game acceptance principle

A recommended-size mature galaxy on target mid-range hardware must maintain responsive UI and stable simulation progress without unbounded backlog. Maximum selectable game speed may gracefully reduce effective simulation speed when the machine cannot sustain the requested rate.
