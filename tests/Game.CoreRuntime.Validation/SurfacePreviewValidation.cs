using Game.Presentation;
using Game.Simulation.Construction;

namespace Game.CoreRuntime.Validation;

internal static class SurfacePreviewValidation
{
    public static void Run()
    {
        var snapshot = Snapshot(("power_generator", true), ("power_generator", true), ("science_lab", false));
        var before = snapshot.Buildings.ToArray();
        var generator = SurfaceOrderPreview.Create(snapshot, "power_generator")!;
        Require(generator.Supply == 17 && generator.Demand == 0 && generator.Powered,
            "Third generator must include the real energy-district bonus and exclude the unfinished lab.");
        Require(generator.CreditCost == 25 && generator.IndustryCost == 300 && generator.MinimumDays == 10,
            "Placement cost and minimum work duration differ from the catalogue.");
        Require(snapshot.Buildings.SequenceEqual(before) && snapshot.Credits == 1000 && snapshot.Industry == 1000,
            "Preview mutated source buildings or reserves.");

        var upgrade = SurfaceOrderPreview.Create(Snapshot(("science_lab", true)), "science_lab", 1)!;
        Require(upgrade.Supply == 2 && upgrade.Demand == 3 && !upgrade.Powered,
            "An upgraded lab that exceeds available power must be shown offline.");
        Require(upgrade.CreditCost == 50 && upgrade.IndustryCost == 320 && upgrade.MinimumDays == 0,
            "Current upgrades consume stored resources immediately, not a fabricated construction time.");
        Require(SurfaceOrderPreview.Create(Snapshot(("science_lab", false)), "science_lab", 1) is null,
            "An unfinished building cannot be previewed as an upgrade.");
        Require(SurfaceOrderPreview.Create(snapshot, "missing") is null &&
            SurfaceOrderPreview.Create(snapshot, "advanced_science_lab") is null,
            "Unknown and upgrade-only construction options must be rejected.");
    }

    private static UiSurfaceSnapshot Snapshot(params (string TypeId, bool Complete)[] buildings) => new(
        1, 3, "Earth", "Preview test", 1000, 1000, 2, 0,
        buildings.Select((item, index) => new UiSurfaceBuilding(index + 1, item.TypeId, item.TypeId,
            60 + index * 60, 60, 0, item.Complete ? 1 : .5,
            SurfaceBuildingCatalog.Find(item.TypeId)!.IndustryCost, item.Complete, item.Complete)).ToArray(),
        Array.Empty<UiSurfaceBuildOption>(), 0, 0, 0, 0, "General", "", 0, false, "temperate", 1, 0, 0);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
