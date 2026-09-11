using System;
using System.Linq;
using Game.Presentation;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Quality.Validation;

internal static class StrategicTerritoryProjectionValidation
{
    [Game.Validation.RegressionCheck]
    internal static void Run()
    {
        var galaxy = new GalaxyGenerator().Generate(0x54455252L, new GalaxyGenerationSettings { SystemCount = 48, Radius = 620, PreWarpCivilizationCount = 3, AncientCivilizationCount = 0 });
        var player = galaxy.PlayerCivilizationId; var foreign = galaxy.Civilizations.First(x => x.Id != player);
        Require(player == 0, "projection regression fixture must exercise civilization zero as the human player");
        var home = galaxy.Civilizations.Single(x => x.Id == player).HomeSystemId;
        var writableSystems = (System.Collections.Generic.IList<StarSystemState>)galaxy.Systems;
        var companionIndex = Enumerable.Range(0, writableSystems.Count).First(index => writableSystems[index].Id != home && !galaxy.Civilizations.Any(c => c.HomeSystemId == writableSystems[index].Id));
        var companion = writableSystems[companionIndex] with { Position = writableSystems.Single(x => x.Id == home).Position + new System.Numerics.Vector2(40, 0) };
        writableSystems[companionIndex] = companion;
        galaxy.Colonies.Add(new ColonyState { Id = galaxy.Colonies.Max(x => x.Id) + 1, CivilizationId = player, SystemId = companion.Id, Name = "Projection companion" });
        var claimSystem = galaxy.Systems.First(x => x.Id != foreign.HomeSystemId && x.Id != galaxy.Civilizations.Single(y => y.Id == player).HomeSystemId).Id;
        var claims = new[] { new TerritorialClaimSnapshot(7, foreign.Id, claimSystem, 1, true, new[] { player, foreign.Id }) };
        var hidden = StrategicTerritoryProjection.Build(galaxy, player, claims);
        Require(hidden.Territories.All(x => x.CivilizationId == player) && hidden.Claims.Count == 0, "hidden ownership or claim leaked into projection");
        Require(hidden.UnownedCellCount > 0, "unclaimed cells were treated as civilization zero territory");
        Require(hidden.UnexploredSystemIds.Contains(foreign.HomeSystemId) && hidden.FogRuns.Count > 0, "unexplored space did not retain a fog veil");

        galaxy.Knowledge.RevealCivilization(player, foreign.Id); galaxy.Knowledge.MarkSystemFullySurveyed(player, foreign.HomeSystemId); galaxy.Knowledge.MarkSystemFullySurveyed(player, claimSystem);
        var visible = StrategicTerritoryProjection.Build(galaxy, player, claims); var region = visible.Territories.Single(x => x.CivilizationId == foreign.Id);
        Require(region.Anchors.Any(x => x.SystemId == foreign.HomeSystemId), "surveyed foreign settlement was not projected");
        Require(visible.Claims.Single().SystemId == claimSystem && !region.Anchors.Any(x => x.SystemId == claimSystem), "diplomatic claim was merged into filled ownership");
        Require(visible.Territories.All(x => x.Contours.Count > 0 && x.FillRuns.Count > 0), "territory cells did not create exterior contours");
        var own = visible.Territories.Single(x => x.CivilizationId == player);
        var midpoint = (writableSystems.Single(x => x.Id == home).Position + companion.Position) * .5f;
        Require(own.Anchors.Any(x => x.SystemId == companion.Id) && own.FillRuns.Any(run => midpoint.X >= run.Position.X && midpoint.X <= run.Position.X + run.Size.X && midpoint.Y >= run.Position.Y && midpoint.Y <= run.Position.Y + run.Size.Y), "nearby same-owner settlements did not union into an exterior territory patch");
        Require(own.FillRuns.Any(run => own.LabelPosition.X >= run.Position.X && own.LabelPosition.X <= run.Position.X + run.Size.X && own.LabelPosition.Y >= run.Position.Y && own.LabelPosition.Y <= run.Position.Y + run.Size.Y), "disconnected territory label was not placed inside an owned patch");
        Require(NoOverlappingCells(visible), "opposing owners overlap in territory cell geometry");
        Require(visible.FogRuns.Count > 0, "overview-capable fog geometry was not built");
        var repeat = StrategicTerritoryProjection.Build(galaxy, player, claims);
        Require(Fingerprint(visible) == Fingerprint(repeat), "territory geometry changed without a state change");
        writableSystems[companionIndex] = companion with { Position = new System.Numerics.Vector2(50_000, 50_000) };
        for (var index = 0; index < writableSystems.Count; index++) if (index != companionIndex && !galaxy.Colonies.Any(colony => colony.SystemId == writableSystems[index].Id)) galaxy.Colonies.Add(new ColonyState { Id = galaxy.Colonies.Max(colony => colony.Id) + 1, CivilizationId = player, SystemId = writableSystems[index].Id, Name = "Dense projection holding" });
        var largeDense = StrategicTerritoryProjection.Build(galaxy, player);
        Require(largeDense.GridCellCount <= 25_600 && largeDense.Territories.Single(x => x.CivilizationId == player).Anchors.Count >= 20, "large dense campaign exceeded the bounded projection grid");
        Console.WriteLine("PASS: observer-safe territory uses contiguous exterior cells, separate claims, clipped opponents, and exploration fog");
    }
    private static bool NoOverlappingCells(StrategicTerritoryProjection projection) => projection.Territories.SelectMany(x => x.FillRuns.Select(run => (x.CivilizationId, run))).GroupBy(x => (x.run.Position, x.run.Size)).All(x => x.Select(y => y.CivilizationId).Distinct().Count() == 1);
    private static string Fingerprint(StrategicTerritoryProjection p) => string.Join("|", p.Territories.Select(x => $"{x.CivilizationId}:{x.FillRuns.Count}:{x.Contours.Count}").Concat(p.Claims.Select(x => $"C{x.CivilizationId}:{x.SystemId}")).Concat(p.FogRuns.Select(x => $"F{x.Position.X:0}")));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
