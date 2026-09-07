using Game.Simulation.Economy;

namespace Game.Logistics.Validation;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("priority wins shared corridor", ValidatePriorityAndSharedCapacity),
            ("allocation never exceeds supply", ValidateSupplyBound),
            ("disabled corridor leaves demand unmet", ValidateDisabledCorridor),
        };

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS: {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"FAIL: {test.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Logistics flow validation: {tests.Length - failures}/{tests.Length} passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void ValidatePriorityAndSharedCapacity()
    {
        var nodes = new[]
        {
            new LogisticsNode(1, 1, 10, "Source", LogisticsNodeKind.Homeworld),
            new LogisticsNode(2, 1, 10, "Hub", LogisticsNodeKind.OrbitalHub),
            new LogisticsNode(3, 1, 10, "Critical", LogisticsNodeKind.PlanetarySettlement),
            new LogisticsNode(4, 1, 10, "Routine", LogisticsNodeKind.PlanetarySettlement),
        };
        var links = new[]
        {
            new LogisticsLink(1, 1, 1, 2, CapacityPerDay: 10.0, TransitDays: 1.0),
            new LogisticsLink(2, 1, 2, 3, CapacityPerDay: 10.0, TransitDays: 1.0),
            new LogisticsLink(3, 1, 2, 4, CapacityPerDay: 10.0, TransitDays: 1.0),
        };

        var planner = new LogisticsRoutePlanner(nodes, links);
        var allocator = new LogisticsFlowAllocator(planner, links);
        var plan = allocator.AllocateDaily(
            new[] { new LogisticsSupplyOffer(1, 20.0) },
            new[]
            {
                new LogisticsDemand(4, RequiredPerDay: 8.0, Priority: 10),
                new LogisticsDemand(3, RequiredPerDay: 8.0, Priority: 100),
            });

        var critical = plan.Allocations.Where(allocation => allocation.DestinationNodeId == 3).Sum(allocation => allocation.AllocatedPerDay);
        var routine = plan.Allocations.Where(allocation => allocation.DestinationNodeId == 4).Sum(allocation => allocation.AllocatedPerDay);

        Require(Math.Abs(critical - 8.0) < 0.000001, $"critical demand received {critical} instead of 8");
        Require(Math.Abs(routine - 2.0) < 0.000001, $"routine demand received {routine} instead of remaining shared capacity 2");
        Require(Math.Abs(plan.TotalAllocatedPerDay - 10.0) < 0.000001, "shared 10/day corridor was oversubscribed");
        Require(Math.Abs(plan.UnmetDemandPerDay[4] - 6.0) < 0.000001, "routine unmet demand was not reported correctly");
        Require(Math.Abs(plan.UnmetDemandPerDay[3]) < 0.000001, "critical demand should have been fully served first");
    }

    private static void ValidateSupplyBound()
    {
        var nodes = new[]
        {
            new LogisticsNode(1, 1, 10, "Source", LogisticsNodeKind.Homeworld),
            new LogisticsNode(2, 1, 10, "Demand", LogisticsNodeKind.PlanetarySettlement),
        };
        var links = new[]
        {
            new LogisticsLink(1, 1, 1, 2, CapacityPerDay: 100.0, TransitDays: 1.0),
        };

        var allocator = new LogisticsFlowAllocator(new LogisticsRoutePlanner(nodes, links), links);
        var plan = allocator.AllocateDaily(
            new[] { new LogisticsSupplyOffer(1, 3.5) },
            new[] { new LogisticsDemand(2, RequiredPerDay: 12.0, Priority: 100) });

        Require(Math.Abs(plan.TotalAllocatedPerDay - 3.5) < 0.000001, "allocator moved more than available source supply");
        Require(Math.Abs(plan.UnmetDemandPerDay[2] - 8.5) < 0.000001, "supply-limited unmet demand was incorrect");
        Require(Math.Abs(plan.UnusedSupplyPerDay[1]) < 0.000001, "fully consumed source still reported unused supply");
    }

    private static void ValidateDisabledCorridor()
    {
        var nodes = new[]
        {
            new LogisticsNode(1, 1, 10, "Source", LogisticsNodeKind.Homeworld),
            new LogisticsNode(2, 1, 10, "Demand", LogisticsNodeKind.PlanetarySettlement),
        };
        var links = new[]
        {
            new LogisticsLink(1, 1, 1, 2, CapacityPerDay: 50.0, TransitDays: 1.0, Enabled: false),
        };

        var allocator = new LogisticsFlowAllocator(new LogisticsRoutePlanner(nodes, links), links);
        var plan = allocator.AllocateDaily(
            new[] { new LogisticsSupplyOffer(1, 10.0) },
            new[] { new LogisticsDemand(2, RequiredPerDay: 7.0, Priority: 100) });

        Require(plan.Allocations.Count == 0, "allocator moved cargo across a disabled corridor");
        Require(Math.Abs(plan.UnmetDemandPerDay[2] - 7.0) < 0.000001, "disabled corridor did not leave demand unmet");
        Require(Math.Abs(plan.UnusedSupplyPerDay[1] - 10.0) < 0.000001, "disabled corridor consumed source supply");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
