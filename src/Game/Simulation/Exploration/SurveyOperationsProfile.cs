using System;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

public enum SurveyOperationalHazard
{
    Routine,
    Elevated,
    Severe,
}

/// <summary>
/// Reconstructible estimate of the effort required to turn reconnaissance into a complete
/// science survey. It uses only represented physical system/body state and contains no hidden
/// discovery chance or research reward logic.
/// </summary>
public sealed record SurveyOperationsProfile(
    int SystemId,
    int PlanetCount,
    int MoonCount,
    double EstimatedScienceSurveyDays,
    SurveyOperationalHazard OperationalHazard)
{
    public double ProgressPerDay => 1.0 / EstimatedScienceSurveyDays;
}

public sealed class SurveyOperationsProfiler
{
    public const double MinimumSurveyDays = 9.0;
    public const double MaximumSurveyDays = 28.0;

    public SurveyOperationsProfile Build(GalaxyState galaxy, int systemId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var system = galaxy.Systems.FirstOrDefault(candidate => candidate.Id == systemId)
            ?? throw new InvalidOperationException($"Unknown system {systemId}.");
        var bodies = galaxy.PlanetaryBodies.Where(body => body.SystemId == systemId).ToArray();
        var planets = bodies.Count(body => body.Kind == PlanetaryBodyKind.Planet);
        var moons = bodies.Length - planets;

        // Body count represents catalog/work volume. Absolute stellar environment adds scanning
        // difficulty, not biological habitability. Marked findings add follow-up workload only;
        // there are no random success rolls or secret discovery probabilities here.
        var days = 8.0 + planets * 0.85 + moons * 0.35;
        days += system.Archetype switch
        {
            StarArchetype.Nebula => 2.2,
            StarArchetype.NeutronPulsar => 3.6,
            StarArchetype.BlackHole => 4.2,
            StarArchetype.Dangerous => 3.0,
            StarArchetype.AncientRuin => 1.8,
            StarArchetype.Legendary => 2.0,
            _ => 0.0,
        };
        days += Math.Min(2.4, bodies.Count(body => body.HasAnomaly) * 0.8);
        days += Math.Min(1.5, bodies.Count(body => body.HasRareResource) * 0.5);
        days = Math.Clamp(days, MinimumSurveyDays, MaximumSurveyDays);

        var physicalHazard = bodies.Count == 0 ? 0.0 : bodies.Max(body => body.Environment.RadiationHazard);
        var hazard = system.Archetype is StarArchetype.NeutronPulsar or StarArchetype.BlackHole or StarArchetype.Dangerous || physicalHazard >= 0.72
            ? SurveyOperationalHazard.Severe
            : system.Archetype == StarArchetype.Nebula || physicalHazard >= 0.40
                ? SurveyOperationalHazard.Elevated
                : SurveyOperationalHazard.Routine;

        return new SurveyOperationsProfile(systemId, planets, moons, days, hazard);
    }
}
