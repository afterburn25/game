using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.AI;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class CivilizationSeeder
{
    private static readonly CivilizationTemplate[] Templates =
    {
        new("Aster Union", CivilizationArchetype.Adaptive,
            new CivilizationTraits(0.35, 0.25, 0.30, 0.55, 0.45, 1.00)),
        new("Kesh Exchange", CivilizationArchetype.Mercantile,
            new CivilizationTraits(0.20, 0.15, 0.80, 0.45, 0.30, 1.00)),
        new("Velari Institute", CivilizationArchetype.Scientific,
            new CivilizationTraits(0.18, 0.20, 0.25, 0.95, 0.25, 1.00)),
        new("Dravak Compact", CivilizationArchetype.Militarist,
            new CivilizationTraits(0.82, 0.58, 0.30, 0.30, 0.62, 1.00)),
        new("Orryn Enclave", CivilizationArchetype.Isolationist,
            new CivilizationTraits(0.12, 0.55, 0.18, 0.58, 0.18, 1.00)),
        new("Tarkesh Reach", CivilizationArchetype.Territorial,
            new CivilizationTraits(0.56, 0.92, 0.38, 0.32, 0.48, 1.00)),
        new("Seren Accord", CivilizationArchetype.Diplomatic,
            new CivilizationTraits(0.12, 0.08, 0.22, 0.52, 0.22, 1.00)),
        new("Kor Vow", CivilizationArchetype.HonorBound,
            new CivilizationTraits(0.66, 0.38, 0.16, 0.28, 0.78, 0.88, HonorBound: true)),
    };

    public IReadOnlyList<CivilizationState> Seed(
        IReadOnlyList<StarSystemState> systems,
        int civilizationCount,
        long seed)
    {
        if (civilizationCount < 1)
            throw new ArgumentOutOfRangeException(nameof(civilizationCount));
        if (civilizationCount > Templates.Length)
            throw new ArgumentOutOfRangeException(nameof(civilizationCount), $"Prototype supports at most {Templates.Length} civilizations.");
        if (systems.Count < civilizationCount)
            throw new InvalidOperationException("There are fewer star systems than civilizations.");

        var candidates = systems.Where(s => s.HasHabitableWorld).ToList();
        if (candidates.Count < civilizationCount)
            candidates = systems.ToList();

        var random = new Random(unchecked((int)((seed * 397) ^ (seed >> 32) ^ 0x51A7C0DE)));
        var homes = PickSpreadHomes(candidates, civilizationCount, random);

        var templateDeck = Templates.OrderBy(_ => random.Next()).Take(civilizationCount).ToArray();
        var civilizations = new List<CivilizationState>(civilizationCount);
        for (var i = 0; i < civilizationCount; i++)
        {
            var template = templateDeck[i];
            civilizations.Add(new CivilizationState(
                Id: i,
                Name: template.Name,
                HomeSystemId: homes[i].Id,
                Archetype: template.Archetype,
                Traits: template.Traits,
                IsPlayer: i == 0));
        }

        return civilizations;
    }

    private static List<StarSystemState> PickSpreadHomes(
        IReadOnlyList<StarSystemState> candidates,
        int count,
        Random random)
    {
        var remaining = candidates.ToList();
        var chosen = new List<StarSystemState>(count);

        var firstIndex = random.Next(remaining.Count);
        chosen.Add(remaining[firstIndex]);
        remaining.RemoveAt(firstIndex);

        while (chosen.Count < count)
        {
            var best = remaining
                .Select(system => new
                {
                    System = system,
                    MinimumDistance = chosen.Min(existing =>
                        System.Numerics.Vector2.DistanceSquared(existing.Position, system.Position)),
                    Jitter = random.NextDouble() * 0.0001,
                })
                .OrderByDescending(candidate => candidate.MinimumDistance + candidate.Jitter)
                .First();

            chosen.Add(best.System);
            remaining.Remove(best.System);
        }

        return chosen;
    }

    private sealed record CivilizationTemplate(
        string Name,
        CivilizationArchetype Archetype,
        CivilizationTraits Traits);
}
