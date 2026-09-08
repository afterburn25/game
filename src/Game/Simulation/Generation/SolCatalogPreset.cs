using System;
using System.Collections.Generic;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

/// <summary>
/// Versioned physical starting catalog. Its explicit key is saved beside the star, so legacy
/// procedural catalogs and current Sol catalogs reconstruct independently of display names.
/// Values are rounded broad physical references, not precision ephemerides or a surface atlas.
/// </summary>
public static class SolCatalogPreset
{
    public const string PresetId = "sol-v1";
    public const int SystemId = 0;
    public const int EarthBodyId = 3;
    public const int MoonBodyId = 9;

    public static bool IsSol(StarSystemState system) => system.CatalogPresetId == PresetId;

    public static IReadOnlyList<PlanetaryBodyState> Create(StarSystemState system)
    {
        if (!IsSol(system) || system.Id != SystemId)
            throw new InvalidOperationException("The Sol v1 catalog requires its reserved system identity.");
        // Radius/mass ratios and approximate temperatures: NASA GSFC planet tables and NASA
        // Science planet facts (sources and model conventions in docs/SOL_STARTING_CATALOG.md).
        return new[]
        {
            Planet(1, 0, "Mercury", 0.383, 0.0553, 0.38, 440, 0, PlanetaryAtmosphereRegime.Vacuum),
            Planet(2, 1, "Venus", 0.950, 0.815, 0.907, 735, 9200, PlanetaryAtmosphereRegime.CarbonDioxideRich),
            Planet(3, 2, "Earth", 1.0, 1.0, 1.0, 288, 101.3, PlanetaryAtmosphereRegime.OxygenNitrogen, water: true),
            Planet(4, 3, "Mars", 0.532, 0.107, 0.377, 210, 0.61, PlanetaryAtmosphereRegime.CarbonDioxideRich),
            Planet(5, 4, "Jupiter", 10.86, 317.8, 2.364, 120, 100, PlanetaryAtmosphereRegime.Reducing, solid: false),
            Planet(6, 5, "Saturn", 9.0, 95.15, 0.916, 88, 100, PlanetaryAtmosphereRegime.Reducing, solid: false),
            Planet(7, 6, "Uranus", 3.97, 14.54, 0.886, 60, 100, PlanetaryAtmosphereRegime.Reducing, solid: false),
            Planet(8, 7, "Neptune", 3.86, 17.15, 1.14, 55, 100, PlanetaryAtmosphereRegime.Reducing, solid: false),
            new PlanetaryBodyState(MoonBodyId, SystemId, EarthBodyId, 0, "Moon", PlanetaryBodyKind.Moon,
                0.273, 0.0123,
                new PlanetaryEnvironmentState(0.165, 250, 0, PlanetaryAtmosphereRegime.Vacuum,
                    PlanetarySolventRegime.None, 0.22, false, true),
                false, false, false, false).Validated(),
        };
    }

    private static PlanetaryBodyState Planet(int id, int orbit, string name, double radius, double mass,
        double gravity, double temperature, double pressure, PlanetaryAtmosphereRegime atmosphere,
        bool water = false, bool solid = true) => new PlanetaryBodyState(
            id, SystemId, null, orbit, name, PlanetaryBodyKind.Planet, radius, mass,
            new PlanetaryEnvironmentState(gravity, temperature, pressure, atmosphere,
                water ? PlanetarySolventRegime.Water : PlanetarySolventRegime.None,
                water ? 0.06 : 0.22, IsImmersedEnvironment: false, HasSolidSurface: solid),
            LegacyColonizationCandidate: id == EarthBodyId,
            HasRareResource: false, HasAnomaly: false, HasPreWarpCivilization: false).Validated();
}
