using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Economy;

public sealed record ColonySustenanceCapacitySnapshot(
    double NaturalFoodCapacityMillions,
    double NaturalWaterCapacityMillions,
    double BuiltFoodCapacityMillions,
    double BuiltWaterCapacityMillions,
    double FoodCapacityMillions,
    double WaterCapacityMillions,
    double SupportedPopulationMillions,
    double SupportRatio,
    string LimitingSupply);

public static class ColonySustenanceCapacity
{
    public const double SealedBaselineCapacityPerInfrastructureMillions = 500.0;
    public const double NaturalBiosphereCapacityPerEarthAreaMillions = 12_000.0;

    public static ColonySustenanceCapacitySnapshot GetSnapshot(GalaxyState galaxy, ColonyState colony)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(colony);
        var infrastructure = Math.Clamp(colony.Infrastructure, 0.1, 5.0);
        var sealedCapacity = SealedBaselineCapacityPerInfrastructureMillions * infrastructure;
        var body = colony.PlanetaryBodyId is int bodyId
            ? galaxy.PlanetaryBodies.FirstOrDefault(candidate => candidate.Id == bodyId && candidate.SystemId == colony.SystemId)
            : null;
        double naturalFood;
        double naturalWater;
        if (body is null)
        {
            // Legacy orbital/bodyless settlements retain their current population as a fixed
            // supported baseline, but gain no automatic headroom for endless growth.
            naturalFood = naturalWater = Math.Max(0.0, colony.PopulationMillions - sealedCapacity);
        }
        else
        {
            var assessment = new SpeciesPlanetaryHabitabilityEvaluator().Evaluate(body, colony.PopulationSpeciesId);
            var area = Math.Clamp(body.RadiusEarth * body.RadiusEarth, 0.02, 25.0);
            var biologicalFit = Math.Clamp(assessment.Environment.NaturalHabitability, 0.0, 1.0);
            var naturalBase = NaturalBiosphereCapacityPerEarthAreaMillions * area * infrastructure;
            naturalFood = naturalBase * biologicalFit;
            naturalWater = naturalBase * biologicalFit * Math.Clamp(assessment.Environment.SolventSuitability, 0.0, 1.0);
        }

        var surface = SurfaceConstruction.GetOutput(colony);
        var food = sealedCapacity + naturalFood + surface.FoodCapacityMillions;
        var water = sealedCapacity + naturalWater + surface.WaterCapacityMillions;
        var supported = Math.Max(0.001, Math.Min(food, water));
        var ratio = colony.PopulationMillions <= 0.0 ? 1.0 : supported / colony.PopulationMillions;
        var limiting = Math.Abs(food - water) <= 0.001 ? "food and potable water"
            : food < water ? "food" : "potable water";
        return new(naturalFood, naturalWater, surface.FoodCapacityMillions, surface.WaterCapacityMillions,
            food, water, supported, ratio, limiting);
    }
}
