# Player galaxy generation

New Player campaign → Sandbox opens a setup screen before replacement confirmation. Players can choose
Spiral, Elliptical, or Ring; 50, 100, or 200 systems; 0–7 rival pre-warp empires; 0–3 ancient
empires; and any signed 64-bit integer seed. The human player is additional to both empire
counts and retains Sol/Earth. Existing Developer quick-start and legacy generation stay intact.

The preview draws the actual layout coordinates consumed by GalaxyGenerator. It shows the
public star catalog and Sol marker, without disclosing resources, worlds or foreign homes.
The generated map fits the chosen shape at galaxy scale and does not substitute Milky Way art.
Star size/color and world information retain the existing survey gates.

## Recreating a starting universe

Use Copy code and Apply code, or enter a seed and match all settings. Codes look like
`SCG1:20260909:0:100:7:2`: generator version, seed, shape, system count, rival count, ancient count.
Shape IDs are 0 Spiral, 1 Elliptical, 2 Ring. Invalid seeds/codes cannot start a world. Use current
settings restores an existing configured campaign's recipe to the preview.

GalaxyState and the canonical save payload keep optional GenerationOptions beside Seed.
Old saves lack this metadata and remain loadable without guessing their settings. Systems are
restored from their authoritative saved coordinates; loading does not reroll the layout.
The recipe recreates an initial world, not later player actions or campaign progress.

Generation version 1 uses explicit SplitMix64 layout/content streams, fixed four-arm / oval /
annular distributions, 24-unit minimum star separation, and a home position with a catalog
neighbor within starting sensor distance. Radius scales with the square root of system count.
Player recipes are the complete authority for generation settings. Legacy generation keeps its
original Random stream and rules. Existing species homeworld planning still assigns physically
viable, distinct homes; shapes do not create new species or ignore habitability.

Maintain the version-one algorithm and its content defaults when changing future generation.
Changes that alter generated worlds need a new recipe version and explicit compatibility policy;
do not silently reuse SCG1 for a changed generator. Test shared codes using the same game build.

## Validation

- Core runtime checks cover deterministic stars, planets and empires; preview/catalog equality;
  requested counts, human origin, seed high bits, signed seed limits, invalid codes, recipe save
  round-trip, old saves without metadata, and 720 layout/seed combinations.
- Simulation checks protect existing generation, planetary reconstruction and campaign rules.
- `STELLAR_CAPTURE_GALAXY_ONLY=1` runs a focused native UI journey in ScreenshotCapture.tscn.
  It opens the real Player menu, edits inputs, changes actual dropdowns, imports a recipe,
  cancels and confirms replacement, reads the resulting checkpoint, and captures the generated map.
  It emits `STELLAR_GALAXY_SETUP_CAPTURE_COMPLETE`, distinct from full campaign acceptance.

## Suggested next stages

Keep the initial setup short. Put later controls under Advanced, with a Reset defaults button
and descriptions explaining their gameplay effects.

1. **Star density:** changes travel distance and contact frequency; must be checked against
   ship reach so low-density starts remain explorable.
2. **Habitable worlds:** sparse / standard / abundant, applied to physical planet environments
   and evaluated per species. Preserve viable founding worlds and some expansion opportunities.
3. **Resources and anomalies:** abundance presets that affect real deposits and discoveries.
   Do not merely recolor stars or change system flags without updating their physical worlds.
4. **Starting neighborhood:** isolated / balanced / crowded, constrained by compatible homeworlds.
5. **Galaxy age and stellar mix:** younger/older distributions tied to actual supported star types.
6. **Crisis settings:** disabled / standard / intense, with timing and strength once crisis gameplay
   exists. Hive-minded non-playable factions can eventually use this system.
7. **Additional shapes:** barred spiral and irregular, with visibly distinct layouts and enough
   nearby stars for early exploration.

All new options must join the versioned recipe, preview where relevant, persistence and
reproducibility checks. A map preview is for evaluating geography; it should preserve discovery.
