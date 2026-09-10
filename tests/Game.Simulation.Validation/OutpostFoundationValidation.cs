using Game.Persistence;
using Game.Simulation.Economy;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class OutpostFoundationValidation
{
    public static void ValidateOutpostRulesAndPersistence()
    {
        var galaxy = new GalaxyGenerator().Generate(0x4F55_5450_4F53_54L, new GalaxyGenerationSettings
        {
            SystemCount = 32,
            PreWarpCivilizationCount = 4,
            AncientCivilizationCount = 1,
            Radius = 420.0f,
        });
        var playerId = galaxy.PlayerCivilizationId;
        var outpost = galaxy.Colonies.First(colony => colony.CivilizationId == playerId);
        foreach (var settlement in galaxy.Colonies.Where(colony =>
                     colony.CivilizationId == playerId && colony.SystemId == outpost.SystemId))
            settlement.Kind = SettlementKind.ResourceOutpost;
        outpost.PopulationMillions = 8.0;
        outpost.Infrastructure = 0.15;
        var populationBefore = outpost.PopulationMillions;

        var flow = EconomySimulation.GetCreditFlow(galaxy, playerId, includeResearchOperations: false);
        var expectedCivilianRevenue = galaxy.Colonies
            .Where(colony => colony.CivilizationId == playerId && colony.Kind == SettlementKind.Colony)
            .Sum(colony => Math.Max(0.01, colony.PopulationMillions / 1000.0) * 0.70 *
                Math.Clamp(colony.Infrastructure, 0.1, 5.0) * Math.Clamp(colony.Stability, 0.1, 1.2));
        Require(Math.Abs(flow.ColonyRevenuePerDay - expectedCivilianRevenue) < 0.000001,
            "staffed outpost generated ordinary civilian tax revenue");
        Require(flow.ColonyAdministrationPerDay >= EconomySimulation.OutpostAdministrationCreditsPerDay,
            "staffed outpost had no administration cost");
        new EconomySimulation().Advance(galaxy, 100.0);
        Require(Math.Abs(outpost.PopulationMillions - populationBefore) < 0.000001,
            "rotating outpost crew grew as a civilian population");

        var system = galaxy.Systems.First(candidate => candidate.Id == outpost.SystemId);
        var fleet = new FleetState
        {
            Id = galaxy.Fleets.Count == 0 ? 7000 : galaxy.Fleets.Max(candidate => candidate.Id) + 7000,
            CivilizationId = playerId,
            Name = "Outpost Service Validation Ship",
            Role = FleetRole.Military,
            Position = system.Position,
            CurrentSystemId = system.Id,
            FuelCapacityLightYears = 200.0,
            FuelRemainingLightYears = 10.0,
            IsActive = true,
        };
        galaxy.Fleets.Add(fleet);
        new ExplorationSimulation().Advance(galaxy, 0.1);
        Require(Math.Abs(fleet.FuelRemainingLightYears - 100.0) < 0.000001,
            "resource outpost did not provide exactly half-capacity refueling support");

        var directory = Path.Combine(Path.GetTempPath(), "stellar-outpost-validation-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(directory, "campaign.json");
            var saves = new CampaignSaveService();
            saves.Save(path, galaxy, 100.0);
            var restored = saves.Load(path).Galaxy.Colonies.Single(colony => colony.Id == outpost.Id);
            Require(restored.Kind == SettlementKind.ResourceOutpost,
                "save/load changed a resource outpost into a civilian colony");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
