using System.Collections.Generic;

namespace Game.Simulation.Shipbuilding;

public sealed class ShipyardState
{
    public const int MaxPendingBuilds = 8;

    public required int CivilizationId { get; init; }
    public string? ActiveDesignId { get; set; }
    public double ActiveBuildProgress { get; set; }
    public double ReservedPopulationMillions { get; set; }
    public List<ShipBuildOrderState> QueuedBuilds { get; } = new();

    public int PendingBuildCount => (ActiveDesignId is null ? 0 : 1) + QueuedBuilds.Count;
}

public sealed class ShipBuildOrderState
{
    public required string DesignId { get; init; }
    public double ReservedPopulationMillions { get; init; }
}
