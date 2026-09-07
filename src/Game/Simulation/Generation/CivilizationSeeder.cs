using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.AI;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class CivilizationSeeder
{
    private static readonly CivilizationTemplate[] PreWarpTemplates =
    {
        new("Aster Union", CivilizationArchetype.Adaptive, new CivilizationTraits(0.35, 0.25, 0.30, 0.55, 0.45, 1.00)),
        new("Kesh Exchange", CivilizationArchetype.Mercantile, new CivilizationTraits(0.20, 0.15, 0.80, 0.45, 0.30, 1.00)),
        new("Velari Institute", CivilizationArchetype.Scientific, new CivilizationTraits(0.18, 0.20, 0.25, 0.95, 0.25, 1.00)),
        new("Dravak Compact", CivilizationArchetype.Militarist, new CivilizationTraits(0.82, 0.58, 0.30, 0.30, 0.62, 1.00)),
        new("Orryn Enclave", CivilizationArchetype.Isolationist, new CivilizationTraits(0.12, 0.55, 0.18, 0.58, 0.18, 1.00)),
        new("Tarkesh Reach", CivilizationArchetype.Territorial, new CivilizationTraits(0.56, 0.92, 0.38, 0.32, 0.48, 1.00)),
        new("Seren Accord", CivilizationArchetype.Diplomatic, new CivilizationTraits(0.12, 0.08, 0.22, 0.52, 0.22, 1.00)),
        new("Kor Vow", CivilizationArchetype.HonorBound, new CivilizationTraits(0.66, 0.38, 0.16, 0.28, 0.78, 0.88, HonorBound: true)),
    };

    private static readonly CivilizationTemplate[] AncientTemplates =
    {
        new("Aurelian Custodians", CivilizationArchetype.AncientCustodian, new CivilizationTraits(0.08, 0.08, 0.05, 0.96, 0.10, 1.00)),
        new("Veyr Archive", CivilizationArchetype.AncientArchivist, new CivilizationTraits(0.04, 0.04, 0.04, 1.00, 0.08, 1.00)),
        new("Orison Keepers", CivilizationArchetype.AncientCustodian, new CivilizationTraits(0.10, 0.06, 0.03, 0.92, 0.12, 1.00)),
    };

    public IList<CivilizationState> Seed(
        IReadOnlyList<StarSystemState> systems,
        int preWarpCount,
        int ancientCount,
        long seed)
    {
        if (preWarpCount < 1 || preWarpCount > PreWarpTemplates.Length)
            throw new ArgumentOutOfRangeException(nameof(preWarpCount));
        if (ancientCount < 0 || ancientCount > AncientTemplates.Length)
            throw new ArgumentOutOfRangeException(nameof(ancientCount));
        if (systems.Count < preWarpCount + ancientCount)
            throw new InvalidOperationException("There are fewer star systems than seeded civilizations.");

        var candidates = systems.Where(system => system.HasHabitableWorld && !system.HasPreWarpCivilization).ToList();
        if (candidates.Count < preWarpCount + ancientCount)
            candidates = systems.Where(system => system.HasHabitableWorld).ToList();
        if (candidates.Count < preWarpCount + ancientCount)
            candidates = systems.ToList();

        var random = new Random(unchecked((int)((seed * 397) ^ (seed >> 32) ^ 0x51A7C0DE)));
        var homes = PickSpreadHomes(candidates, preWarpCount + ancientCount, random);
        var preWarpDeck = PreWarpTemplates.OrderBy(_ => random.Next()).Take(preWarpCount).ToArray();
        var ancientDeck = AncientTemplates.OrderBy(_ => random.Next()).Take(ancientCount).ToArray();

        var civilizations = new List<CivilizationState>(preWarpCount + ancientCount);
        for (var i = 0; i < preWarpCount; i++)
        {
            var template = preWarpDeck[i];
            civilizations.Add(new CivilizationState(
                Id: civilizations.Count,
                Name: template.Name,
                HomeSystemId: homes[i].Id,
                Archetype: template.Archetype,
                Traits: template.Traits,
                IsPlayer: i == 0,
                DevelopmentStage: CivilizationDevelopmentStage.PreWarp,
                IsSeededAncient: false,
                ExpansionAllowed: true,
                NeutralUnlessProvoked: false));
        }

        for (var i = 0; i < ancientCount; i++)
        {
            var template = ancientDeck[i];
            civilizations.Add(new CivilizationState(
                Id: civilizations.Count,
                Name: template.Name,
                HomeSystemId: homes[preWarpCount + i].Id,
                Archetype: template.Archetype,
                Traits: template.Traits,
                IsPlayer: false,
                DevelopmentStage: CivilizationDevelopmentStage.AncientSpacefaring,
                IsSeededAncient: true,
                ExpansionAllowed: false,
                NeutralUnlessProvoked: true));
        }

        return civilizations;
    }

    private static List<StarSystemState> PickSpreadHomes(IReadOnlyList<StarSystemState> candidates, int count, Random random)
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
                    MinimumDistance = chosen.Min(existing => System.Numerics.Vector2.DistanceSquared(existing.Position, system.Position)),
                    Jitter = random.NextDouble() * 0.0001,
                })
                .OrderByDescending(candidate => candidate.MinimumDistance + candidate.Jitter)
                .First();
            chosen.Add(best.System);
            remaining.Remove(best.System);
        }
        return chosen;
    }

    private sealed record CivilizationTemplate(string Name, CivilizationArchetype Archetype, CivilizationTraits Traits);
}
