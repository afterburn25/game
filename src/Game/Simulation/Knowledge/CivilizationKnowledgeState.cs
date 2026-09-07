using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Knowledge;

/// <summary>
/// Stores what each civilization has legitimately discovered. Presentation and AI
/// must query this state rather than treating GalaxyState as automatically known.
/// Star coordinates are part of the common astronomical catalog; system details,
/// fleets and civilizations remain hidden until legitimately observed.
/// </summary>
public sealed class CivilizationKnowledgeState
{
    private readonly Dictionary<int, HashSet<int>> _knownSystems = new();
    private readonly Dictionary<int, HashSet<int>> _knownCivilizations = new();

    public bool IsSystemKnown(int civilizationId, int systemId) =>
        _knownSystems.TryGetValue(civilizationId, out var systems) && systems.Contains(systemId);

    public bool IsCivilizationKnown(int observerCivilizationId, int targetCivilizationId) =>
        observerCivilizationId == targetCivilizationId ||
        (_knownCivilizations.TryGetValue(observerCivilizationId, out var civilizations) &&
         civilizations.Contains(targetCivilizationId));

    public IReadOnlyCollection<int> GetKnownSystems(int civilizationId)
    {
        if (!_knownSystems.TryGetValue(civilizationId, out var systems))
            return Array.Empty<int>();
        return systems.OrderBy(id => id).ToArray();
    }

    public IReadOnlyCollection<int> GetKnownCivilizations(int civilizationId)
    {
        if (!_knownCivilizations.TryGetValue(civilizationId, out var civilizations))
            return Array.Empty<int>();
        return civilizations.OrderBy(id => id).ToArray();
    }

    public bool RevealSystem(int civilizationId, int systemId)
    {
        if (!_knownSystems.TryGetValue(civilizationId, out var systems))
        {
            systems = new HashSet<int>();
            _knownSystems[civilizationId] = systems;
        }

        return systems.Add(systemId);
    }

    public bool RevealCivilization(int observerCivilizationId, int targetCivilizationId)
    {
        if (observerCivilizationId == targetCivilizationId)
            return false;

        if (!_knownCivilizations.TryGetValue(observerCivilizationId, out var civilizations))
        {
            civilizations = new HashSet<int>();
            _knownCivilizations[observerCivilizationId] = civilizations;
        }

        return civilizations.Add(targetCivilizationId);
    }

    public int RevealWithinSensorRange(
        int civilizationId,
        int originSystemId,
        IReadOnlyList<StarSystemState> systems,
        float sensorRange)
    {
        var origin = systems.FirstOrDefault(s => s.Id == originSystemId)
            ?? throw new InvalidOperationException($"Unknown sensor origin system {originSystemId}.");

        var revealed = 0;
        var rangeSquared = sensorRange * sensorRange;
        foreach (var system in systems)
        {
            if (System.Numerics.Vector2.DistanceSquared(origin.Position, system.Position) <= rangeSquared &&
                RevealSystem(civilizationId, system.Id))
            {
                revealed++;
            }
        }

        return revealed;
    }

    public KnowledgeSnapshotData Snapshot() => new(
        _knownSystems.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.OrderBy(id => id).ToArray()),
        _knownCivilizations.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.OrderBy(id => id).ToArray()));

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

public sealed record KnowledgeSnapshotData(
    IReadOnlyDictionary<int, int[]> Systems,
    IReadOnlyDictionary<int, int[]> Civilizations
);
