using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Simulation.Combat;
using Game.Simulation.Combat.Massive;
using Godot;

namespace Game.Tools;

/// <summary>
/// Maintained native stress capture for the production tactical renderer. The 100,000-ship
/// battle is a synthetic engine scenario and is not evidence of campaign inventory reconciliation.
/// </summary>
public sealed partial class MassiveCombatCapture : Node
{
    private readonly List<string> _checks = new();
    private readonly List<CaptureRecord> _captures = new();
    private readonly MassiveCombatEngine _engine = new();
    private readonly CaptureSensors _sensors = new();
    private MassiveCombatBattleState _battle = null!;
    private MassiveCombatView _view = null!;
    private string _output = string.Empty;
    private double _tacticalSpeed = 1;
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
        _battle = BuildBattle(50_000);
        _view = new MassiveCombatView
        {
            Name = "MassiveCombatView",
            OrderRequested = order => _engine.IssueOrder(_battle, 0, order),
            TacticalSpeedRequested = speed => _tacticalSpeed = speed,
        };
        AddChild(_view);
        await Frames(8);
        Require(GetViewport().GetVisibleRect().Size == new Vector2(1280, 720), "capture-starts-native-720p");

        Present();
        await Frames(5);
        var initial = MassiveCombatObserver.BuildSnapshot(_battle, 0, _sensors);
        var hidden = initial.Formations.Where(x => x.CivilizationId == 1).ToArray();
        Require(initial.ExactOwnShips == 50_000, "observer-reports-exact-50k-friendly-ships");
        Require(hidden.Sum(x => x.ShipCountLow) <= 50_000 && hidden.Sum(x => x.ShipCountHigh) >= 50_000 &&
                hidden.All(x => x.StrengthLow is null && x.StrengthHigh is null && x.ImportantVessels.Count == 0 &&
                    x.Cohorts.Count == 1 && !x.Cohorts[0].Identified && x.Cohorts[0].DisplayClass == "Unidentified ships"),
            "unknown-hostiles-mask-strength-vessels-and-cohort-composition");
        Require(_view.RenderedOrdinaryTokens is > 0 and <= MassiveCombatFormationPool.MaximumTokens,
            "ordinary-ship-rendering-is-pooled-and-bounded");
        await SaveAsync("01-720p-unknown-contact.png");

        var own = initial.Formations.Where(x => x.CivilizationId == 0).Take(10).ToArray();
        var firstPoint = Position(own[5].FormationId);
        await Click(firstPoint, MouseButton.Left);
        Require(_view.SelectedFormationIds.SequenceEqual(new[] { own[5].FormationId }), "single-click-selects-exact-friendly-formation");

        var points = own.Skip(3).Take(6).Select(x => Position(x.FormationId)).ToArray();
        var upperLeft = new Vector2(points.Min(x => x.X), points.Min(x => x.Y)) - new Vector2(18, 18);
        var lowerRight = new Vector2(points.Max(x => x.X), points.Max(x => x.Y)) + new Vector2(18, 18);
        await Drag(upperLeft, lowerRight);
        Require(_view.SelectedFormationIds.Count >= 2, "drag-selects-multiple-friendly-formations");
        await SaveAsync("02-720p-multiselect.png");

        var hostile = initial.Formations.Where(x => x.CivilizationId == 1).Skip(5).First();
        var beforeOrder = _battle.Events.Select(x => x.Sequence).DefaultIfEmpty(0).Max();
        await Click(Position(hostile.FormationId), MouseButton.Right);
        Present(); await Frames(3);
        Require(_battle.Events.Any(x => x.Sequence > beforeOrder && x.Type == MassiveCombatEventType.OrderChanged &&
                    _view.SelectedFormationIds.Contains(x.ActorFormationId)),
            "right-click-routes-typed-engage-orders-through-engine");

        var pause = Buttons().Single(button => button.Text == "Pause");
        await Click(pause.GetGlobalRect().GetCenter(), MouseButton.Left);
        Require(_tacticalSpeed == 0, "pause-control-uses-tactical-speed-callback");
        var fast = Buttons().Single(button => button.Text == "2×");
        await Click(fast.GetGlobalRect().GetCenter(), MouseButton.Left);
        Require(_tacticalSpeed == 2, "speed-control-uses-tactical-speed-callback");

        await AdvanceUntilEffectsAsync();
        await SaveAsync("03-720p-live-weapons.png");
        _sensors.AuthorizeHostileDetails = true;
        Present(); await Frames(4);
        var known = MassiveCombatObserver.BuildSnapshot(_battle, 0, _sensors).Formations.Where(x => x.CivilizationId == 1).ToArray();
        Require(known.All(x => x.StrengthLow.HasValue && x.Cohorts.Any(c => c.Identified)),
            "authorized-contact-reveals-real-bounded-strength-and-cohorts");

        var center = GetViewport().GetVisibleRect().Size * .5f;
        for (var step = 0; step < 6; step++) await Wheel(center, MouseButton.WheelUp);
        await SaveAsync("04-720p-zoom-detail.png");
        Require(_view.RenderedOrdinaryTokens <= MassiveCombatFormationPool.MaximumTokens, "zoom-detail-keeps-token-pool-bounded");

        GetWindow().Size = new Vector2I(1920, 1080);
        await Frames(12); Present(); await Frames(4);
        Require(GetViewport().GetVisibleRect().Size == new Vector2(1920, 1080), "capture-renders-native-1080p");
        await SaveAsync("05-1080p-tactical-overview.png");

        for (var step = 0; step < 14; step++) await Wheel(GetViewport().GetVisibleRect().Size * .5f, MouseButton.WheelDown);
        await SaveAsync("06-1080p-reduced-lod.png");
        Require(_view.RenderedOrdinaryTokens <= 100, "reduced-zoom-collapses-to-formation-level-lod");
        var performance = await MeasureFramesAsync(240);
        await WriteManifestAsync(performance);
    }

    private async Task AdvanceUntilEffectsAsync()
    {
        var startSequence = _battle.Events.Select(x => x.Sequence).DefaultIfEmpty(0).Max();
        for (var step = 0; step < 80; step++)
        {
            _engine.Advance(_battle, .1 * Math.Max(.25, _tacticalSpeed));
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
        _view.UpdateSnapshot(MassiveCombatObserver.BuildSnapshot(_battle, 0, _sensors), 0);
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
            if (index % 6 == 0) { _engine.Advance(_battle, .1); Present(); }
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
            scenario = "synthetic-engine-renderer-stress-50k-versus-50k",
            campaignReconciliationProven = false,
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

    private static MassiveCombatBattleState BuildBattle(int shipsPerSide)
    {
        const int formationsPerSide = 50;
        var formations = new List<MassiveFormationState>(formationsPerSide * 2);
        var profile = new CombatProfileDefinition("capture_mixed", 65, 90, 180, 1.1, .22, 1);
        for (var side = 0; side < 2; side++)
        for (var index = 0; index < formationsPerSide; index++)
        {
            var id = side * 10_000L + index + 1;
            var loadout = MassiveCombatLoadouts.FromLegacy(profile);
            loadout.Acceleration = 18; loadout.MaximumSpeed = 95; loadout.ReactorOutputPerShip = 140; loadout.CoolingPerShip = 30;
            loadout.Weapons.Add(new() { Id = MassiveEquipmentIds.MissileBattery, Kind = MassiveWeaponKind.Missile, DamagePerShot = .15f, ShotsPerSecond = .35f, Range = 1_200, Accuracy = .68f });
            loadout.Weapons.Add(new() { Id = MassiveEquipmentIds.PointDefense, Kind = MassiveWeaponKind.PointDefense, ShotsPerSecond = 1.4f, Range = 500, Accuracy = .74f });
            if (index % 10 == 0) loadout.Modules.Add(MassiveCombatLoadouts.WarpInterdictor(800, 55));
            var target = (1 - side) * 10_000L + index + 1;
            formations.Add(new()
            {
                Id = id, CivilizationId = side, FleetId = side * 1_000 + index + 1, TaskForceId = side * 10 + index / 10,
                Name = side == 0 ? $"Expeditionary Group {index + 1}" : $"Hostile Contact {index + 1}",
                Position = new(side == 0 ? -420 + index * 2 : 420 - index * 2, (index % 10 - 4.5f) * 54),
                Heading = new(side == 0 ? 1 : -1, 0), Objective = new(0, 0), Shape = index % 3 == 0 ? MassiveFormationShape.Wedge : MassiveFormationShape.Line,
                Order = MassiveCombatOrderType.Engage, TargetFormationId = target, Loadout = loadout,
                Cohorts =
                [
                    new() { Id = id * 10, DesignId = "line-combatant", InitialCount = 699, ActiveCount = 699 },
                    new() { Id = id * 10 + 1, DesignId = "missile-screen", InitialCount = 300, ActiveCount = 300 },
                ],
                ImportantVessels =
                [
                    new() { Id = 1_000_000 + id, Name = side == 0 ? $"ISS Resolute {index + 1}" : $"Contact Prime {index + 1}", DesignId = "command-cruiser", IsFlagship = true, IsInterdictor = index % 10 == 0 },
                ],
            });
        }
        return MassiveCombatBattleState.Create(0xC0B47UL + (ulong)shipsPerSide, formations);
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed class CaptureSensors : IMassiveCombatSensorView
    {
        public bool AuthorizeHostileDetails { get; set; }
        public float Confidence(int observerCivilizationId, long formationId) => observerCivilizationId == 0 ? .67f : 1;
        public bool IdentifiesCohorts(int observerCivilizationId, long formationId) => AuthorizeHostileDetails;
        public bool IdentifiesImportantVessels(int observerCivilizationId, long formationId) => AuthorizeHostileDetails;
        public bool CanEstimateCombatPower(int observerCivilizationId, long formationId) => AuthorizeHostileDetails;
    }

    private sealed record CaptureRecord(string FileName, int Width, int Height, long Bytes);
    private sealed record PerformanceRecord(int Frames, double AverageFrameMilliseconds, double P95FrameMilliseconds,
        double MaximumFrameMilliseconds, double EngineFramesPerSecond, long ManagedBytes, long TicksAdvanced, int RenderedTokens);
}
