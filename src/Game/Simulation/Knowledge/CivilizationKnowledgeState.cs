using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Knowledge;

/// <summary>
/// Stores what each civilization has legitimately discovered. Presentation and AI
/// must query this state rather than treating GalaxyState.Systems as automatically known.
/// </summary>
public sealed class CivilizationKnowledgeState
{
    private readonly Dictionary<int, HashSet<int>> _knownSystems = new();

    public bool IsSystemKnown(int civilizationId, int systemId) =>
        _knownSystems.TryGetValue(civilizationId, out var systems) && systems.Contains(systemId);

    public IReadOnlyCollection<int> GetKnownSystems(int civilizationId)
    {
        if (!_knownSystems.TryGetValue(civilizationId, out var systems))
            return Array.Empty<int>();

        return systems.OrderBy(id => id).ToArray();
    }

    public void RevealSystem(int civilizationId, int systemId)
    {
        if (!_knownSystems.TryGetValue(civilizationId, out var systems))
        {
            systems = new HashSet<int>();
            _knownSystems[civilizationId] = systems;
        }

        systems.Add(systemId);
    }

    public void RevealWithinSensorRange(
        int civilizationId,
        int originSystemId,
        IReadOnlyList<StarSystemState> systems,
        float sensorRange)
    {
        var origin = systems.FirstOrDefault(s => s.Id == originSystemId)
            ?? throw new InvalidOperationException($"Unknown sensor origin system {originSystemId}.");

        var rangeSquared = sensorRange * sensorRange;
        foreach (var system in systems)
        {
            if (System.Numerics.Vector2.DistanceSquared(origin.Position, system.Position) <= rangeSquared)
                RevealSystem(civilizationId, system.Id);
        }
    }

    public IReadOnlyDictionary<int, int[]> Snapshot() =>
        _knownSystems.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.OrderBy(id => id).ToArray());

    public static CivilizationKnowledgeState CreateInitial(
        IReadOnlyList<StarSystemState> systems,
        IReadOnlyList<CivilizationState> civilizations,
        float sensorRange)
    {
        var knowledge = new CivilizationKnowledgeState();
        foreach (var civilization in civilizations)
        {
            knowledge.RevealSystem(civilization.Id, civilization.HomeSystemId);
            knowledge.RevealWithinSensorRange(civilization.Id, civilization.HomeSystemId, systems, sensorRange);
        }

        return knowledge;
    }
}
