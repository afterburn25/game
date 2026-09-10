# Premium art and visual identity

Branch: `work/premium-art-visual-identity`. Base: integration 5763a1e. This branch implements the rendering and identity foundation directly in Godot 4.7.2 .NET. It does **not** declare the entire game commercially finished.

## Direction and implementation

Human design uses exposed modular frames, docking gantries, radiators, pressure vessels, dark glass, titanium/slate hulls and restrained warm identification marks. CivilizationVisualStyles selects species palettes and design motifs without touching species capabilities or game state. Pelagic pressure-shell, compact armored, cryogenic crystalline and unknown-neutral hooks reuse the same role silhouettes. Complete bespoke alien fleets and architecture remain an art-production gap.

Six ShipGeometry designs distinguish scouts, science vessels, colony ships, corvettes, resource outpost ships and bulk freighters. Native 3D meshes appear in the fleet inspector and system scene. System ships use bounded detail and projected clickable role markers. Their cosmetic stationkeeping offsets never change travel distances, destinations or mission progress. At most 64 local ship models/icons are rendered.

OrbitalStructureGeometry provides the same staged engineering model for the inspector and real system world. Shipyards use large open gantries, lateral platforms, docks, radiators and service structures. The actual active ship order and progress control an assembly hull/scaffold and at most four service craft; inactive/completed orders remove that activity. Work effects stop with the simulation clock.

SystemScene3D replaces disc cutouts with perspective-rendered spheres and annular rings. Left-drag pans, middle-drag orbits, wheel approaches the focused body and enters the atmosphere of an eligible owned colony. Actual observer-visible moons and facilities remain in the scene. Star spectra drive the photosphere and corona. The planet shader shades from the stellar direction without treating compressed navigation distances as physical light attenuation.

Earth uses NASA global day/cloud/night maps. Only an owner-visible inhabited Earth gets night-side city emission; no hidden foreign colony is revealed. NASA lunar mapping appears in orbit and in the Earth surface sky. Saturn has cream/tan atmospheric bands, oblateness, tilted geometric rings, fine annular detail and broad divisions. Its initial approach frames the illuminated quarter and the complete ring geometry. Shader detail is procedural or mipmapped, rather than a stretched image cutout.

Each system sky uses its own stable star/nebula configuration; distant external galaxies belong only to the galaxy-level deep field. Galaxy dust is rendered at native display resolution from a bounded shader field, with layered arms and lanes around existing catalog positions. Observer-unknown stars retain neutral markers; known spectra receive colored cores.

The surface has richer capital towers, dark facade/glass detailing, distinct researched module tiers, ground albedo/normal/roughness maps, conforming avenues, pads, vegetation, rocks and utilities. Ground traffic follows point-to-point routes, capped at five vehicles, and avoids construction footprints. These are presentation objects; placement validation and terrain collision remain authoritative. Surface skies show only actual visible moon markers, with restrained apparent size, daylight fading and phase lighting.

The orbital descent is a continuous camera/render transition through a globe, atmosphere, regional terrain and the local colony heightfield. The regional/local landscape is representative terrain. It is **not** geospatially streamed Earth geography or a complete planet simulation.

## UI, resolution and settings

The primary reference is 1920×1080 with a responsive 1280×720 minimum and native 1440p/4K rendering. The existing compact left rail, grouped right inspectors, graphical orders and timed queues remain. Campaign confirmation now uses the game's framed modal treatment. Selected ship/station previews are native 3D, with viewport sizing following display pixels.

The cursor repair coalesces resize updates and avoids applying an additional aspect transform to an already fitted logical canvas. A native Windows message probe verified the real left rail and drawer hitboxes at 720p, 1080p, an odd window size, 1440p, 4K and maximized.

Video settings enumerate Windows modes and expose windowed/borderless/fullscreen, V-Sync, MSAA and 3D render scale. Fullscreen uses desktop resolution. Apply/Keep/Revert and a 15-second timeout recover bad choices. NVIDIA Control Panel is an optional detected launch action; no driver settings, DLSS or ray tracing are claimed.

## Validation and evidence

The full native Windows candidate run in `work/premium-full-4` exited 0 with empty stderr: 32 maintained rendered screenshots and 120 actual GUI-input checks. The established validator accepted the screenshots and semantic engine logs. It covers ordinary research/construction, timed surface work, save/recovery, separate Player/Developer campaigns, ship orders and routes, orbital infrastructure, and responsive 720p/1080p/1440p/4K behavior.

That local run identifies its base commit plus the working candidate; it must not be mistaken for a committed-head release artifact. The PR screenshot workflow now runs for integration PRs and records its tested SHA. Release build also passed locally with zero warnings/errors. Simulation 69/69, Core Runtime 70/70 and Quality 18/18 passed during this rendering work; no authoritative simulation changes are included.

Additional native evidence:
- immersive-review-9: complete orbit/atmosphere/ground/return journey.
- premium-saturn-2: reviewed planetary quarter lighting; final ring framing adjusted afterward.
- premium-surface-final: bird's-eye and street review, module-tier geometry and bounded city-detail checks.
- moon-sky-focused-4: mapped visible Earth Moon and world-switch/close lifecycle.
- final-video-720 and final-video-1080: seven settings/persistence/revert checks each.
- native-pointer-current: physical window-coordinate regression proof.

The maintained screenshot set includes menu, research, construction, relations, colonies, galaxy/region, Earth focus, surface placement, capital/Mars terrain, shipyard selection, fleet routing and orbital facility inspector. A curated warp close-up, first-colony cinematic, fully assembled shipyard close-up and human 30-minute subjective play review remain additional art gates; the current captures do not prove those are finished.

## Sources and licensing

See [Immersive texture provenance](IMMERSIVE_TEXTURE_PROVENANCE.md) for exact NASA/Poly Haven source URLs, credits, SHA-256 values and transformations. NASA imagery is credited to its source project; Poly Haven ground maps are CC0. Existing original ship/portrait/splash assets remain covered by their established provenance. No Stellaris screenshot or Stockcake reference image is included in game assets. New geometry and shaders are authored project code.

## Performance and remaining quality work

3D system/surface worlds use separate viewports updated only while visible. Hidden worlds are cleared when closed. Native preview sizes are capped, map dust caps at 4096 pixels, reusable materials and bounded instance sets avoid per-frame model regeneration. Profile/style changes rebuild only relevant visuals. Shader surfaces use mipmaps and anisotropic sampling. No effect changes a command outcome, resource budget, save format or observer knowledge.

This is a substantial 3D presentation step, with remaining visible gaps: custom production meshes/material baking, diverse alien architecture, believable pedestrians and vehicles, finer cloud/atmosphere scattering, geographically coherent globe-to-ground terrain, natural ring/planet shadow interaction, cinematic staging, and richer combat/warp effects. The current surface still reads as procedural architecture rather than photoreal commercial environment art. The branch is reviewable code and evidence, not a declaration that those gaps have vanished.
