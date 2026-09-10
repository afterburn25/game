using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Economy;

public sealed record ColonySustenanceCapacitySnapshot(
    double NaturalFoodCapacityMillions,
    double NaturalWaterCapacityMillions,
    double NaturalHousingCapacityMillions,
    double BuiltFoodCapacityMillions,
    double BuiltWaterCapacityMillions,
    double BuiltHousingCapacityMillions,
    double FoodCapacityMillions,
    double WaterCapacityMillions,
    double HousingCapacityMillions,
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
        double naturalHousing;
        if (body is null)
        {
            // Legacy orbital/bodyless settlements retain their current population as a fixed
            // supported baseline, but gain no automatic headroom for endless growth.
            naturalFood = naturalWater = naturalHousing = Math.Max(0.0, colony.PopulationMillions - sealedCapacity);
        }
        else
        {
            var assessment = new SpeciesPlanetaryHabitabilityEvaluator().Evaluate(body, colony.PopulationSpeciesId);
            var area = Math.Clamp(body.RadiusEarth * body.RadiusEarth, 0.02, 25.0);
            var biologicalFit = Math.Clamp(assessment.Environment.NaturalHabitability, 0.0, 1.0);
            var naturalBase = NaturalBiosphereCapacityPerEarthAreaMillions * area * infrastructure;
            naturalFood = naturalBase * biologicalFit;
            naturalWater = naturalBase * biologicalFit * Math.Clamp(assessment.Environment.SolventSuitability, 0.0, 1.0);
            naturalHousing = naturalBase * biologicalFit;
        }

        var surface = SurfaceConstruction.GetOutput(colony);
        var food = sealedCapacity + naturalFood + surface.FoodCapacityMillions;
        var water = sealedCapacity + naturalWater + surface.WaterCapacityMillions;
        var housing = sealedCapacity + naturalHousing + surface.HousingCapacityMillions;
        var supported = Math.Max(0.001, Math.Min(food, Math.Min(water, housing)));
        var ratio = colony.PopulationMillions <= 0.0 ? 1.0 : supported / colony.PopulationMillions;
        var minimum = Math.Min(food, Math.Min(water, housing));
        var limiting = new[] { (Name: "food", Value: food), (Name: "potable water", Value: water), (Name: "housing", Value: housing) }
            .Where(item => Math.Abs(item.Value - minimum) <= 0.001).Select(item => item.Name).ToArray();
        return new(naturalFood, naturalWater, naturalHousing, surface.FoodCapacityMillions, surface.WaterCapacityMillions,
            surface.HousingCapacityMillions, food, water, housing, supported, ratio, string.Join(" and ", limiting));
    }
}
