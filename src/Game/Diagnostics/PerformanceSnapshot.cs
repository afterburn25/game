namespace Game.Diagnostics;

public sealed record PerformanceSnapshot(
    long SimulationTick,
    double SimulationTickMs,
    double AiMs,
    double PathfindingMs,
    double EconomyMs,
    double FleetMovementMs,
    long ManagedMemoryBytes,
    int ActiveCivilizations,
    int ActiveColonies,
    int ActiveFleets,
    int ActiveShips,
    int PathfindingQueueDepth,
    int RouteCacheEntries
);
