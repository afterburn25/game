using Game.Persistence;
using Game.Simulation.Economy;
using Game.Simulation.Construction;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Models;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation.Validation;

internal static class OutpostFreightValidation
{
    public static void ValidateRepresentedCollectionAndDelivery()
    {
        var galaxy = new GalaxyGenerator().Generate(0x4652_4549_4748_544CL, new GalaxyGenerationSettings
        {
            SystemCount = 64,
            PreWarpCivilizationCount = 4,
            AncientCivilizationCount = 1,
            Radius = 560.0f,
        });
        var player = galaxy.Civilizations.First(candidate => candidate.Id == galaxy.PlayerCivilizationId);
        var home = galaxy.Systems.First(system => system.Id == player.HomeSystemId);
        var homeColony = galaxy.Colonies.First(colony => colony.CivilizationId == player.Id && colony.SystemId == home.Id && colony.Kind == SettlementKind.Colony);
        var resourceBody = galaxy.PlanetaryBodies.First(body => body.SystemId != home.Id && body.HasRareResource && body.Environment.HasSolidSurface && !body.HasPreWarpCivilization);
        var outpost = new ColonyState
        {
            Id = galaxy.Colonies.Max(colony => colony.Id) + 9000,
            CivilizationId = player.Id,
            SystemId = resourceBody.SystemId,
            PlanetaryBodyId = resourceBody.Id,
            Name = "Freight Validation Outpost",
            Kind = SettlementKind.ResourceOutpost,
            PopulationSpeciesId = player.SpeciesId,
            PopulationMillions = 8.0,
            Infrastructure = 0.15,
            Stability = 0.85,
            StoredExtractedMaterials = 80.0,
        };
        var fabricator = SurfaceBuildingCatalog.Find("fabricator")!;
        outpost.SurfaceBuildings.Add(new SurfaceBuildingState
        {
            Id = 1,
            TypeId = fabricator.Id,
            X = 80,
            IndustryProgress = fabricator.IndustryCost,
            IsComplete = true,
        });
        galaxy.Colonies.Add(outpost);
        var design = ShipDesignRegistry.Get(ShipDesignRegistry.BulkFreighterId);
        var freighter = new FleetState
        {
            Id = galaxy.Fleets.Count == 0 ? 8000 : galaxy.Fleets.Max(fleet => fleet.Id) + 8000,
            CivilizationId = player.Id,
            Name = "Lifeline Validation",
            Role = FleetRole.Logistics,
            DesignId = design.Id,
            Position = home.Position,
            CurrentSystemId = home.Id,
            StrategicSpeed = design.StrategicSpeed,
            MaximumLegRangeLightYears = 10000.0,
            FuelCapacityLightYears = 10000.0,
            FuelRemainingLightYears = 10000.0,
            SensorRange = design.SensorRange,
            CargoMaterialCapacity = design.CargoMaterialCapacity,
            IsActive = true,
        };
        galaxy.Fleets.Add(freighter);
        var freight = new FreightSimulation();
        var order = freight.IssueCollectionOrder(galaxy, player.Id, freighter.Id, outpost.Id);
        Require(order.Accepted && freighter.FreightHomeColonyId == homeColony.Id && freighter.FreightTargetOutpostId == outpost.Id,
            "freight collection order did not persist its origin and outpost destination");

        var outpostSystem = galaxy.Systems.First(system => system.Id == outpost.SystemId);
        freighter.Position = outpostSystem.Position;
        freighter.CurrentSystemId = outpostSystem.Id;
        FleetRouteOrders.Clear(freighter);
        var economy = galaxy.Economies.First(state => state.CivilizationId == player.Id);
        economy.LastBaseOperationsFundingFraction = 0.0;
        freight.Advance(galaxy);
        Require(freighter.CargoMaterials == 0.0 && outpost.StoredExtractedMaterials == 80.0,
            "unfunded freight service transferred physical cargo");
        economy.LastBaseOperationsFundingFraction = 1.0;
        freight.Advance(galaxy);
        Require(Math.Abs(freighter.CargoMaterials - 80.0) < 0.000001 && outpost.StoredExtractedMaterials == 0.0,
            "freighter did not transfer the exact local stockpile into bounded cargo");
        Require(freighter.DestinationSystemId == home.Id && freighter.FreightTargetOutpostId is null,
            "loaded freighter did not receive its represented return route");

        var directory = Path.Combine(Path.GetTempPath(), "stellar-freight-validation-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(directory, "campaign.json");
            var saves = new CampaignSaveService();
            saves.Save(path, galaxy, 200.0);
            var restored = saves.Load(path).Galaxy;
            var restoredFreighter = restored.Fleets.Single(fleet => fleet.Id == freighter.Id);
            Require(restoredFreighter.CargoMaterials == 80.0 && restoredFreighter.FreightHomeColonyId == homeColony.Id,
                "mid-return save/load lost freight cargo or delivery identity");
            var restoredHome = restored.Systems.First(system => system.Id == home.Id);
            restoredFreighter.Position = restoredHome.Position;
            restoredFreighter.CurrentSystemId = restoredHome.Id;
            FleetRouteOrders.Clear(restoredFreighter);
            var industryBefore = restored.Economies.First(state => state.CivilizationId == player.Id).Industry;
            freight.Advance(restored);
            var industryAfter = restored.Economies.First(state => state.CivilizationId == player.Id).Industry;
            Require(Math.Abs(industryAfter - industryBefore - 80.0) < 0.000001,
                "delivered freight did not become usable Industry at the developed colony");
            Require(restoredFreighter.CargoMaterials == 0.0 && restoredFreighter.FreightHomeColonyId is null,
                "completed freight run did not clear its cargo and mission state");
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
