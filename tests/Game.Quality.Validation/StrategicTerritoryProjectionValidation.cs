using System;
using System.Linq;
using Game.Presentation;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;

namespace Game.Quality.Validation;

internal static class StrategicTerritoryProjectionValidation
{
    [Game.Validation.RegressionCheck]
    internal static void Run()
    {
        var galaxy = new GalaxyGenerator().Generate(0x5445_5252L, new GalaxyGenerationSettings
        {
            SystemCount = 48,
            Radius = 620,
            PreWarpCivilizationCount = 3,
            AncientCivilizationCount = 0,
        });
        var player = galaxy.PlayerCivilizationId;
        var foreign = galaxy.Civilizations.First(civilization => civilization.Id != player);
        var foreignClaimSystem = galaxy.Systems.First(system => system.Id != foreign.HomeSystemId &&
            system.Id != galaxy.Civilizations.Single(civilization => civilization.Id == player).HomeSystemId).Id;
        var claims = new[] { new TerritorialClaimSnapshot(7, foreign.Id, foreignClaimSystem, 1, true, new[] { player, foreign.Id }) };

        var hidden = StrategicTerritoryProjection.Build(galaxy, player, claims);
        Require(hidden.Territories.All(region => region.CivilizationId == player),
            "unknown foreign owner or name leaked into territory projection");
        Require(hidden.FogPatches.Any(patch => patch.SystemId == foreign.HomeSystemId),
            "unknown foreign system was removed from exploration fog");
        Require(hidden.Territories.Single(region => region.CivilizationId == player).Anchors.Count > 0,
            "own home territory was not projected");

        galaxy.Knowledge.RevealCivilization(player, foreign.Id);
        galaxy.Knowledge.MarkSystemFullySurveyed(player, foreign.HomeSystemId);
        galaxy.Knowledge.MarkSystemFullySurveyed(player, foreignClaimSystem);
        var discovered = StrategicTerritoryProjection.Build(galaxy, player, claims);
        var foreignRegion = discovered.Territories.Single(region => region.CivilizationId == foreign.Id);
        var ownRegion = discovered.Territories.Single(region => region.CivilizationId == player);
        Require(foreignRegion.CivilizationName == foreign.Name &&
                foreignRegion.Anchors.Any(anchor => anchor.SystemId == foreign.HomeSystemId) &&
                foreignRegion.Anchors.Any(anchor => anchor.SystemId == foreignClaimSystem),
            "completed survey and known civilization did not reveal lawful foreign territory");
        Require(discovered.FogPatches.All(patch => patch.SystemId != foreign.HomeSystemId && patch.SystemId != foreignClaimSystem),
            "newly surveyed territory anchors remained fogged");
        var nearestOpposing = (from ownIndex in Enumerable.Range(0, ownRegion.Anchors.Count)
                               from foreignIndex in Enumerable.Range(0, foreignRegion.Anchors.Count)
                               let distance = System.Numerics.Vector2.Distance(ownRegion.Anchors[ownIndex].Position,
                                   foreignRegion.Anchors[foreignIndex].Position)
                               orderby distance
                               select (distance, ownRegion.Radii[ownIndex], foreignRegion.Radii[foreignIndex])).First();
        Require(nearestOpposing.distance > 0 &&
                (nearestOpposing.distance < 64 || nearestOpposing.Item2 + nearestOpposing.Item3 <= nearestOpposing.distance * 1.02f),
            "adjacent visible empires projected overlapping giant territory circles");

        var repeated = StrategicTerritoryProjection.Build(galaxy, player, claims);
        Require(Fingerprint(discovered) == Fingerprint(repeated),
            "territory projection changed without campaign or knowledge changes");
        foreach (var region in discovered.Territories)
            Require(region.Radii.All(radius => radius is >= 28 and <= 78),
                "territory projection produced an unbounded claim radius");
        Console.WriteLine("PASS: observer-safe territory projection keeps unknown ownership fogged and projects discovered holdings deterministically");
    }

    private static string Fingerprint(StrategicTerritoryProjection projection)
    {
        return string.Join("|", projection.Territories.Select(region =>
        {
            var anchors = string.Join(',', region.Anchors.Select(anchor => anchor.SystemId));
            var radii = string.Join(',', region.Radii.Select(radius => radius.ToString("0.###")));
            return $"{region.CivilizationId}:{anchors}:{radii}";
        }));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
