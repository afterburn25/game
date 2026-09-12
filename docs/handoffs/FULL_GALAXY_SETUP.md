# Full galaxy setup and map repair — 0.1.7 Alpha

Base: integration 4b25aba8. Integration branch: work/stellar-scale-and-core.

## Player behavior

New Player campaigns use the versioned Full galaxy profile. Setup offers Small 250,
Medium 500 (default), Large 1,000 and Huge 2,500 systems; seed/randomize; four species;
0/3/5/8/12 rival empires; None/Rare/Standard ancient empires; Rare/Uncommon/Common
habitable worlds; and Low/Standard/High anomalies. Confirmation captures one immutable
metadata record. Copy setup includes every selection, and defaults restore all choices.
The bounded setup scroll keeps Generate visible at 720p; 1080p remains the design reference.

Existing saves retain their systems, names, distances, discovery and generator identity.
The previous nearby-only 500 profile remains explicitly supported. A new campaign is
required for the wider population; loading never silently regenerates an existing galaxy.

## Generation rules and limits

All presets retain the nearest 96 classified HYG catalogue systems, exact source identities
and measured three-dimensional positions. Remaining systems have seeded names and approximate
physical positions in clustered spiral regions. Generated positions never claim HYG provenance.

The 500-system primary-class allocation is M375/K50/G25/F10/A5/hot-blue1/giant5/white-dwarf24/
neutron2/pulsar1/black-hole1/protostar1. Other sizes use largest-remainder rounding, preserving
at least one of each rare class. This is a science-informed gameplay distribution, not a
measured Milky Way census or an assertion that all catalogue classifications are error-free.
Pulsar is appended to the persisted enum and uses neutron-star radiation/planet rules.
Compact stellar class and gameplay archetype agree.

Radius is 50,000 × sqrt(systemCount / 500) light-years. Sol remains 26,000 light-years from
the centre. Smaller/larger presets deliberately scale the represented galaxy. The protected
centre is 14% of radius and has no ordinary systems, with its secret landmark masked by
unlabelled pale fog until the established access/discovery gates allow it. This star-free
playable core is a user-directed game rule; the real Galactic centre contains stars.

Each generated system has at least two actual lane neighbours within 340 light-years.
Each non-ancient starting civilization receives two viable nearby expansion worlds.
Longer backbone lanes connect regions, but many exceed early ship range. This work does
not add new propulsion research or claim that every region is immediately reachable.
The full-galaxy radius is not a promise of real-world galaxy density with only 500 nodes.

Reference context: [NASA red-dwarf neighbourhood fraction](https://science.nasa.gov/universe/exoplanets/small-stars-are-a-big-deal/),
[NASA stellar categories](https://science.nasa.gov/universe/stars/types/),
[NASA Milky Way scale](https://imagine.gsfc.nasa.gov/science/featured_science/milkyway/).
[Quasars are active galactic nuclei](https://science.nasa.gov/mission/webb/science-overview/science-explainers/what-are-active-galactic-nuclei/),
so they are not generated as ordinary stellar systems.

## Presentation and integration repairs

The camera and dust share a cached frame around the actual generated centre. Full-galaxy
overview blending follows that frame's fitted scale; regional zoom shows local stars/nebulae,
not resolved background galaxies. Measured coordinates use the established 14× presentation
multiplier, which never changes travel distances. Home and fleet centring apply it consistently.
Camera origin/zoom cancellation is computed in double precision before converting to screen
pixels, retaining subpixel mouse alignment around distant systems at 192× regional zoom.

Stellar cores now grow with zoom and class. Radial light textures have padded transparent
edges and mipmaps; galaxy dust fades on its elliptical extent, not a rectangular quad.
Unexplored-territory grid outlines no longer draw frames around individual stars.
Radius/class lookups and galaxy framing are cached rather than rescanned for each star.
System-view suns and actual simulation sizes are unchanged by the map marker scale.

## Scientist voice repair

A real 0.1.6 game log showed the chief scientist using US Microsoft Zira. The installed neural
manifest depended on installer-host absolute resource paths. Pack discovery now prefers
validated pack-local resources, newly installed manifests use relative paths, and logs retain
the reason for neural rejection. The scientist requires the British female bf_emma backend;
wrong-accent SAPI/cache playback is rejected with captions and diagnostics if the pack is absent.
Other allowed profiles retain their existing fallback. Voices remain synthetic.
The four female production roles retain distinct sources: scientist bf_emma, narrator
bf_isabella, commander af_kore and diplomat af_bella. Casting/audition scripts preserve
this selection. Python pack validation also resolves resources relative to the pack,
so installation checks cannot reintroduce the caller-working-directory defect.

Local neural proof synthesized the exact reconnaissance telemetry line with bf_emma, producing
156,000 PCM samples, and verified cache reuse and a copied-host manifest with unavailable old
paths. Proof: work/stellar-voice-neural-proof/work/voice-core-proof/neural-scientist-british.wav
in the parent workspace. Neural models remain an optional installed pack, not committed assets.

## Validation

Maintained Core runtime, quality and voice checks cover generation/determinism, exact counts,
legacy save round trips, option rejection, species starts, core exclusion, observer privacy,
camera precision, padded alpha, cache/backend identity and real British speech.
Native screenshot and performance receipts are recorded under this branch's work directory;
only completed receipts and fresh hosted checks authorize integration/release acceptance.
Do not substitute earlier 0.1.6 receipts for this revision.

Completed local receipts before publication:
- Core runtime 88/88; quality 20/20; actual installed neural voice 12/12.
- Full galaxy 500 native map/unknown privacy/zoom at 720p and 1080p: exit 0,
  work/full-galaxy-final; legacy nearby profile also previously exited 0.
- Species and setup controls, seed entry/copy/defaults, immutable confirmation and
  nonhuman generation at 720p/1080p: exit 0, work/galaxy-options-final.
- Huge 2,500-system native performance: exit 0, all 12 views meet 60 FPS/p95 target
  on RTX 3080 Ti at 2560×1440. Overview improved from 38.7 to 140.4 FPS by batching
  subpixel stars into two same-texture quads. One 104 ms outlier remains in that
  overview sample; this is not a guarantee of hitch-free performance or all hardware.
  Receipt work/huge-galaxy-performance-batched, source bb9896b1 plus the committed
  overview batching diff. Detailed regional stars retain the close-up rendering.
- Fresh hosted packaging/regressions and native live British scientist playback
  remain release gates; record final receipts in the pull request.
