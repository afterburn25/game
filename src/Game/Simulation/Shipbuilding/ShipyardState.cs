using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Shipbuilding;

public sealed class ShipyardState
{
    public const int MaxPendingBuilds = 8;

    private readonly List<ShipBuildOrderState> _queuedBuilds = new();

    public required int CivilizationId { get; init; }
    public string? ActiveDesignId { get; set; }
    public double ActiveBuildProgress { get; set; }
    public double ReservedPopulationMillions { get; set; }
    public string? ReservedPopulationSpeciesId { get; set; }

    /// <summary>
    /// Queue access validates only the invariants needed to prevent population-bearing build
    /// records from being silently omitted by bounded persistence. Runtime order creation keeps
    /// the queue within bounds already; these checks are defensive against corrupt/injected state.
    /// </summary>
    public List<ShipBuildOrderState> QueuedBuilds
    {
        get
        {
            ValidatePopulationPersistenceSafety();
            return _queuedBuilds;
        }
    }

    public int PendingBuildCount => (ActiveDesignId is null ? 0 : 1) + _queuedBuilds.Count;

    private void ValidatePopulationPersistenceSafety()
    {
        var activePopulation = Math.Max(0.0, ReservedPopulationMillions);
        if (activePopulation > 0.0 &&
            (string.IsNullOrWhiteSpace(ActiveDesignId) ||
             !ShipDesignRegistry.All.Any(design => design.Id == ActiveDesignId)))
        {
            throw new InvalidOperationException(
                $"Shipyard {CivilizationId} has {activePopulation:0.###} million reserved population without a valid active design; refusing a state transition that could discard reserved colonists.");
        }

        var queueCapacity = Math.Max(0, MaxPendingBuilds - (ActiveDesignId is null ? 0 : 1));
        for (var index = 0; index < _queuedBuilds.Count; index++)
        {
            var build = _queuedBuilds[index];
            var population = Math.Max(0.0, build.ReservedPopulationMillions);
            if (population <= 0.0)
                continue;

            if (string.IsNullOrWhiteSpace(build.DesignId) ||
                !ShipDesignRegistry.All.Any(design => design.Id == build.DesignId))
            {
                throw new InvalidOperationException(
                    $"Shipyard {CivilizationId} queued build '{build.DesignId}' has {population:0.###} million reserved population but no valid design; refusing to discard reserved colonists.");
            }

            if (index >= queueCapacity)
            {
                throw new InvalidOperationException(
                    $"Shipyard {CivilizationId} queue exceeds its bounded capacity while overflow build '{build.DesignId}' retains {population:0.###} million reserved population; refusing to truncate reserved colonists.");
            }
        }
    }
}

public sealed class ShipBuildOrderState
{
    public required string DesignId { get; init; }
    public double ReservedPopulationMillions { get; init; }
    public string? ReservedPopulationSpeciesId { get; init; }
}