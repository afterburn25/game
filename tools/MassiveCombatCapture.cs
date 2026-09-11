using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Game.Campaign;
using Game.Presentation;
using Game.Simulation.Combat;
using Game.Simulation.Combat.Massive;
using Game.Simulation.Models;
using Godot;

namespace Game.Tools;

/// <summary>
/// Maintained native stress capture for the production tactical renderer and campaign bridge.
/// Its disposable galaxy contains 100,000 real FleetState records; no player save is opened.
/// </summary>
public sealed partial class MassiveCombatCapture : Node
{
    private readonly List<string> _checks = new();
    private readonly List<CaptureRecord> _captures = new();
    private readonly MutableHostility _hostility = new();
    private CampaignMassiveCombat _bridge = null!;
    private GalaxyState _galaxy = null!;
    private MassiveCombatBattleState _battle = null!;
    private int _observerCivilizationId;
    private int _hostileCivilizationId;
    private MassiveCombatView _view = null!;
    private string _output = string.Empty;
    private double _tacticalSpeed = 1;
    private double _tacticalResumeSpeed = 1;
    private long _frames;

    public override async void _Ready()
    {
        try
        {
            await RunAsync();
            GD.Print("STELLAR_MASSIVE_COMBAT_CAPTURE_COMPLETE");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError("Massive combat capture failed: " + error);
            try { await SaveAsync("failure.png"); } catch { }
            GetTree().Quit(1);
        }
    }

    private async Task RunAsync()
    {
        _output = System.Environment.GetEnvironmentVariable("STELLAR_MASSIVE_CAPTURE_DIR")
            ?? ProjectSettings.GlobalizePath("user://massive-combat-capture");
        Directory.CreateDirectory(_output);
        AddChild(new ResponsiveDisplay { Name = "ResponsiveDisplay" });
        _galaxy = BuildCampaign(50_000, out _observerCivilizationId, out _hostileCivilizationId);
        _bridge = new CampaignMassiveCombat(_hostility);
        var begin = _bridge.Begin(_galaxy, _observerCivilizationId, 1, 88);
        Require(begin.Accepted, "real-100k-fleet-inventory-enters-campaign-bridge");
        var encounter = _galaxy.ActiveCombatEncounter ?? throw new InvalidOperationException("Campaign bridge did not retain its encounter.");
        _battle = encounter.Battle;
        ArrangeFormations();
        Require(encounter.Vessels.Count == 100_000 && _battle.Formations.Sum(x => x.InitialShipCount) == 100_000,
            "campaign-bridge-binds-and-conserves-100k-real-vessels");
        _view = new MassiveCombatView
        {
            Name = "MassiveCombatView",
            OrderRequested = order => _bridge.Engine.IssueOrder(_battle, _observerCivilizationId, order),
            TacticalSpeedRequested = speed => _tacticalSpeed = speed,
            TacticalResumeSpeedRequested = speed => _tacticalResumeSpeed = speed,
        };
        AddChild(_view);
        await Frames(8);
        Require(GetViewport().GetVisibleRect().Size == new Vector2(1280, 720), "capture-starts-native-720p");

        Present();
        await Frames(5);
        var initial = _bridge.Observe(_galaxy, _observerCivilizationId, scanningCapability: false);
        var hidden = initial.Formations.Where(x => x.CivilizationId == _hostileCivilizationId).ToArray();
        Require(initial.ExactOwnShips == 50_000, "observer-reports-exact-50k-friendly-ships");
        Require(hidden.Sum(x => x.ShipCountLow) <= 50_000 && hidden.Sum(x => x.ShipCountHigh) >= 50_000 &&
                hidden.All(x => x.StrengthLow is null && x.StrengthHigh is null && x.ImportantVessels.Count == 0 &&
                    x.Cohorts.Count == 1 && !x.Cohorts[0].Identified && x.Cohorts[0].DisplayClass == "Unidentified ships"),
            "unknown-hostiles-mask-strength-vessels-and-cohort-composition");
        Require(_view.RenderedOrdinaryTokens is > 0 and <= MassiveCombatFormationPool.MaximumTokens,
            "ordinary-ship-rendering-is-pooled-and-bounded");
        await SaveAsync("01-720p-unknown-contact.png");

        var own = initial.Formations.Where(x => x.CivilizationId == _observerCivilizationId).Take(10).ToArray();
        var firstPoint = Position(own[5].FormationId);
        await Click(firstPoint, MouseButton.Left);
        Require(_view.SelectedFormationIds.SequenceEqual(new[] { own[5].FormationId }), "single-click-selects-exact-friendly-formation");

        var points = own.Skip(3).Take(6).Select(x => Position(x.FormationId)).ToArray();
        var upperLeft = new Vector2(points.Min(x => x.X), points.Min(x => x.Y)) - new Vector2(18, 18);
        var lowerRight = new Vector2(points.Max(x => x.X), points.Max(x => x.Y)) + new Vector2(18, 18);
        await Drag(upperLeft, lowerRight);
        Require(_view.SelectedFormationIds.Count >= 2, "drag-selects-multiple-friendly-formations");
        await SaveAsync("02-720p-multiselect.png");

        var hostile = initial.Formations.Where(x => x.CivilizationId == _hostileCivilizationId).Skip(5).First();
        var beforeOrder = _battle.Events.Select(x => x.Sequence).DefaultIfEmpty(0).Max();
        await Click(Position(hostile.FormationId), MouseButton.Right);
        Present(); await Frames(3);
        Require(_battle.Events.Any(x => x.Sequence > beforeOrder && x.Type == MassiveCombatEventType.OrderChanged &&
                    _view.SelectedFormationIds.Contains(x.ActorFormationId)),
            "right-click-routes-typed-engage-orders-through-engine");

        var pause = Buttons().Single(button => button.Name == "TacticalPlaybackButton");
        await Click(pause.GetGlobalRect().GetCenter(), MouseButton.Left);
        Present();
        Require(_tacticalSpeed == 0, "pause-control-uses-tactical-speed-callback");
        var speedButton = Buttons().Single(button => button.Name == "TacticalPlaybackSpeedButton");
        await Click(speedButton.GetGlobalRect().GetCenter(), MouseButton.Left);
        Present();
        Require(_tacticalSpeed == 0 && _tacticalResumeSpeed == 2,
            "paused-speed-control-selects-resume-rate-without-resuming");
        await Click(pause.GetGlobalRect().GetCenter(), MouseButton.Left);
        Present();
        Require(_tacticalSpeed == 2, "play-control-resumes-selected-tactical-rate");
        Push(new InputEventKey { Keycode = Key.Space, PhysicalKeycode = Key.Space, Pressed = true });
        Push(new InputEventKey { Keycode = Key.Space, PhysicalKeycode = Key.Space, Pressed = false });
        await Frames(3);
        Present();
        Require(_tacticalSpeed == 0, "space-toggles-tactical-pause-like-play-control");

        await AdvanceUntilEffectsAsync();
        await SaveAsync("03-720p-live-weapons.png");
        Present(); await Frames(4);
        var known = _bridge.Observe(_galaxy, _observerCivilizationId, scanningCapability: false).Formations
            .Where(x => x.CivilizationId == _hostileCivilizationId).ToArray();
        var engaged = known.Single(x => x.FormationId == hostile.FormationId);
        Require(engaged.StrengthLow.HasValue && engaged.Cohorts.Any(c => c.Identified),
            "engaged-contact-reveals-real-bounded-strength-and-cohorts");
        Require(known.Any(x => x.FormationId != hostile.FormationId && x.StrengthLow is null &&
                    x.Cohorts.Count == 1 && !x.Cohorts[0].Identified),
            "unengaged-contact-remains-observer-masked");

        var center = GetViewport().GetVisibleRect().Size * .5f;
        await Pan(Position(own[5].FormationId), center);
        for (var step = 0; step < 6; step++) await Wheel(center, MouseButton.WheelUp);
        await SaveAsync("04-720p-zoom-detail.png");
        Require(_view.RenderedOrdinaryTokens <= MassiveCombatFormationPool.MaximumTokens, "zoom-detail-keeps-token-pool-bounded");

        GetWindow().Size = new Vector2I(1920, 1080);
        await Frames(12); Present(); await Frames(4);
        Require(GetViewport().GetVisibleRect().Size == new Vector2(1920, 1080), "capture-renders-native-1080p");
        await Click(Buttons().Single(button => button.Text == "Fit").GetGlobalRect().GetCenter(), MouseButton.Left);
        await SaveAsync("05-1080p-tactical-overview.png");

        await Click(GetViewport().GetVisibleRect().Size * .5f, MouseButton.Left);
        for (var step = 0; step < 14; step++) await Wheel(GetViewport().GetVisibleRect().Size * .5f, MouseButton.WheelDown);
        await SaveAsync("06-1080p-reduced-lod.png");
        Require(_view.RenderedOrdinaryTokens <= 100, "reduced-zoom-collapses-to-formation-level-lod");
        var performance = await MeasureFramesAsync(240);
        VerifyCampaignReconciliation();
        await WriteManifestAsync(performance);
    }

    private async Task AdvanceUntilEffectsAsync()
    {
        var startSequence = _battle.Events.Select(x => x.Sequence).DefaultIfEmpty(0).Max();
        for (var step = 0; step < 80; step++)
        {
            _bridge.Advance(_galaxy, .1 * Math.Max(.25, _tacticalSpeed));
            Present();
            await Frames(1);
            if (_battle.Events.Any(x => x.Sequence > startSequence && x.Type is MassiveCombatEventType.BeamVolley or
                    MassiveCombatEventType.KineticVolley or MassiveCombatEventType.MissileSalvo))
                return;
        }
        throw new InvalidOperationException("Real engine did not produce a visible weapon event within eight tactical seconds.");
    }

    private void Present()
    {
        _view.UpdateSnapshot(_bridge.Observe(_galaxy, _observerCivilizationId, scanningCapability: false), _observerCivilizationId);
        _view.SetTacticalSpeedState(_tacticalSpeed);
    }

    private async Task<PerformanceRecord> MeasureFramesAsync(int frameCount)
    {
        var samples = new double[frameCount];
        var startTick = _battle.Tick;
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < samples.Length; index++)
        {
            var before = stopwatch.Elapsed.TotalMilliseconds;
            if (index % 6 == 0) { _bridge.Advance(_galaxy, .1); Present(); }
            await Frames(1);
            samples[index] = stopwatch.Elapsed.TotalMilliseconds - before;
        }
        Array.Sort(samples);
        var average = samples.Average();
        var p95 = samples[(int)Math.Ceiling(samples.Length * .95) - 1];
        var result = new PerformanceRecord(frameCount, average, p95, samples[^1], Engine.GetFramesPerSecond(),
            GC.GetTotalMemory(false), _battle.Tick - startTick, _view.RenderedOrdinaryTokens);
        GD.Print($"STELLAR_MASSIVE_PERFORMANCE frames={frameCount} averageMs={average:F2} p95Ms={p95:F2} maxMs={samples[^1]:F2} engineFps={result.EngineFramesPerSecond:F1} tokens={result.RenderedTokens}");
        return result;
    }

    private IEnumerable<Button> Buttons() => Descendants(_view).OfType<Button>().Where(x => x.IsVisibleInTree());
    private Vector2 Position(long formationId) => _view.GetFormationScreenPosition(formationId)
        ?? throw new InvalidOperationException($"Formation {formationId} has no rendered position.");

    private async Task Click(Vector2 point, MouseButton button)
    {
        Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = true,
            ButtonMask = button == MouseButton.Left ? MouseButtonMask.Left : MouseButtonMask.Right });
        Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = false });
        await Frames(3);
    }

    private async Task Drag(Vector2 from, Vector2 to)
    {
        Push(new InputEventMouseMotion { Position = from, GlobalPosition = from });
        Push(new InputEventMouseButton { Position = from, GlobalPosition = from, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
        for (var step = 1; step <= 8; step++)
        {
            var point = from.Lerp(to, step / 8f);
            Push(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (to - from) / 8, ButtonMask = MouseButtonMask.Left });
            await Frames(1);
        }
        Push(new InputEventMouseButton { Position = to, GlobalPosition = to, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }

    private async Task Pan(Vector2 from, Vector2 to)
    {
        Push(new InputEventMouseMotion { Position = from, GlobalPosition = from });
        Push(new InputEventMouseButton { Position = from, GlobalPosition = from, ButtonIndex = MouseButton.Middle, Pressed = true, ButtonMask = MouseButtonMask.Middle });
        for (var step = 1; step <= 8; step++)
        {
            var point = from.Lerp(to, step / 8f);
            Push(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (to - from) / 8, ButtonMask = MouseButtonMask.Middle });
            await Frames(1);
        }
        Push(new InputEventMouseButton { Position = to, GlobalPosition = to, ButtonIndex = MouseButton.Middle, Pressed = false });
        await Frames(3);
    }

    private async Task Wheel(Vector2 point, MouseButton direction)
    {
        Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = true });
        await Frames(1);
    }

    private void Push(InputEvent input) { GetViewport().PushInput(input, true); _frames++; }
    private async Task Frames(int count) { for (var index = 0; index < count; index++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }

    private async Task SaveAsync(string name)
    {
        await Frames(3);
        using var image = GetViewport().GetTexture().GetImage();
        if (image is null) throw new InvalidOperationException("Viewport image was unavailable.");
        var path = Path.Combine(_output, name);
        if (image.SavePng(path) != Error.Ok) throw new IOException("Could not save " + path);
        _captures.Add(new(name, image.GetWidth(), image.GetHeight(), new FileInfo(path).Length));
        GD.Print($"STELLAR_MASSIVE_CAPTURE {name} {image.GetWidth()}x{image.GetHeight()}");
    }

    private async Task WriteManifestAsync(PerformanceRecord performance)
    {
        var revision = System.Environment.GetEnvironmentVariable("STELLAR_SOURCE_REVISION") ?? "unreported";
        var manifest = new
        {
            schema = "stellar-massive-combat-capture-v1",
            sourceRevision = revision,
            scenario = "disposable-real-campaign-inventory-50k-versus-50k",
            campaignReconciliationProven = true,
            initialShipsPerSide = 50_000,
            formationCount = _battle.Formations.Count,
            checks = _checks,
            captures = _captures,
            injectedPointerEvents = _frames,
            performance,
        };
        await File.WriteAllTextAsync(Path.Combine(_output, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void Require(bool condition, string check)
    {
        if (!condition) throw new InvalidOperationException(check);
        _checks.Add(check); GD.Print("STELLAR_MASSIVE_CHECK_PASS " + check);
    }

    private static GalaxyState BuildCampaign(int shipsPerSide, out int observerCivilizationId, out int hostileCivilizationId)
    {
        var galaxy = new CampaignSessionService().CreateNew(41005).Galaxy;
        galaxy.Fleets.Clear();
        var system = galaxy.Systems[0];
        var observer = galaxy.PlayerCivilizationId;
        observerCivilizationId = observer;
        hostileCivilizationId = galaxy.Civilizations.Select(x => x.Id).First(x => x != observer);
        var profile = CombatProfileRegistry.Get(CombatProfileIds.PatrolCorvetteMk1);
        var loadouts = new MassiveCombatLoadout[50];
        for (var group = 0; group < loadouts.Length; group++)
        {
            var loadout = MassiveCombatLoadouts.FromLegacy(profile);
            loadout.Weapons[0].Range = 1_200;
            loadout.Weapons[0].ShotsPerSecond = 1;
            loadout.Weapons[0].DamagePerShot = .01f;
            loadout.Weapons[0].Accuracy = .60f + group * .001f;
            if (group % 4 == 0)
                loadout.Weapons.Add(new() { Id = MassiveEquipmentIds.MissileBattery, Kind = MassiveWeaponKind.Missile,
                    DamagePerShot = .01f, ShotsPerSecond = .35f, Range = 1_200, Accuracy = .68f });
            if (group % 5 == 0)
                loadout.Weapons.Add(new() { Id = MassiveEquipmentIds.PointDefense, Kind = MassiveWeaponKind.PointDefense,
                    ShotsPerSecond = 1.4f, Range = 500, Accuracy = .74f });
            if (group % 10 == 0) loadout.Modules.Add(MassiveCombatLoadouts.WarpInterdictor(800, 55));
            loadouts[group] = loadout;
        }
        for (var index = 0; index < shipsPerSide * 2; index++)
        {
            var fleetId = index + 1;
            var side = index / shipsPerSide;
            var withinSide = index % shipsPerSide;
            var group = withinSide / 1_000;
            var name = side == 0 ? $"ISS Line Vessel {withinSide + 1:N0}" : $"Hostile Vessel {withinSide + 1:N0}";
            galaxy.Fleets.Add(new FleetState
            {
                Id = fleetId, CivilizationId = side == 0 ? observerCivilizationId : hostileCivilizationId,
                Name = name, Role = FleetRole.Military, DesignId = $"Line combatant G{group + 1:00}",
                Position = system.Position, CurrentSystemId = system.Id,
                Combat = CombatProfileRegistry.CreateInitialState(profile.Id, FleetRole.Military),
                TacticalLoadout = loadouts[group],
                TacticalVessel = withinSide % 1_000 == 0 ? new MassiveVesselState
                {
                    Id = fleetId, Name = name, DesignId = $"Line combatant G{group + 1:00}", IsFlagship = true,
                    IsInterdictor = group % 10 == 0,
                } : null,
            });
        }
        return galaxy;
    }

    private void ArrangeFormations()
    {
        foreach (var side in _battle.Formations.GroupBy(x => x.CivilizationId))
        {
            var friendly = side.Key == _observerCivilizationId;
            var ordered = side.OrderBy(x => x.Id).ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                ordered[index].Position = new(friendly ? -420 - index / 10 * 110 : 420 + index / 10 * 110,
                    (index % 10 - 4.5f) * 70);
                ordered[index].Heading = new(friendly ? 1 : -1, 0);
                ordered[index].Shape = index % 3 == 0 ? MassiveFormationShape.Wedge : MassiveFormationShape.Line;
            }
        }
    }

    private void VerifyCampaignReconciliation()
    {
        var expectedSurvivors = _battle.Formations.Sum(x => x.SurvivingShipCount);
        var expectedShields = _battle.Formations.Sum(x => (double)x.ShieldPool);
        var expectedArmor = _battle.Formations.Sum(x => (double)x.ArmorPool);
        var expectedHull = _battle.Formations.Sum(x => (double)x.HullPool);
        _hostility.Hostile = false;
        var result = _bridge.Advance(_galaxy, 0);
        Require(result.Any(x => x.Type == CombatEventType.EngagementEnded) && _galaxy.ActiveCombatEncounter?.Reconciled == true,
            "ceasefire-reconciles-live-campaign-encounter");
        Require(_galaxy.Fleets.Count == 100_000 && _galaxy.Fleets.Count(x => x.IsActive) == expectedSurvivors &&
                Math.Abs(_galaxy.Fleets.Sum(x => x.Combat!.Shields) - expectedShields) < .1 &&
                Math.Abs(_galaxy.Fleets.Sum(x => x.Combat!.Armor) - expectedArmor) < .1 &&
                Math.Abs(_galaxy.Fleets.Sum(x => x.Combat!.Hull) - expectedHull) < .1,
            "campaign-reconciliation-preserves-real-fleet-count-and-durability");
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed class MutableHostility : ICombatHostilityView
    {
        public bool Hostile { get; set; } = true;
        public bool AreHostile(int firstCivilizationId, int secondCivilizationId) =>
            Hostile && firstCivilizationId != secondCivilizationId;
    }

    private sealed record CaptureRecord(string FileName, int Width, int Height, long Bytes);
    private sealed record PerformanceRecord(int Frames, double AverageFrameMilliseconds, double P95FrameMilliseconds,
        double MaximumFrameMilliseconds, double EngineFramesPerSecond, long ManagedBytes, long TicksAdvanced, int RenderedTokens);
}
