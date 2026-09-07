using System;
using System.IO;
using System.Linq;
using Godot;
using Game.Diagnostics;
using Game.Persistence;
using Game.Simulation;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Presentation;

public partial class Main : Node2D
{
    private readonly SimulationClock _clock = new();
    private readonly DiagnosticsBuffer _diagnostics = new();
    private readonly CampaignSaveService _saveService = new();
    private GalaxyState _galaxy = null!;
    private Font _font = null!;
    private int _selectedSystemId = -1;
    private Godot.Vector2 _pan = Godot.Vector2.Zero;
    private float _zoom = 0.55f;
    private bool _panning;
    private double _performanceLogTimer;
    private string _statusText = string.Empty;
    private double _statusTimer;

    private string AutosavePath => ProjectSettings.GlobalizePath("user://saves/autosave.json");

    public override void _Ready()
    {
        _font = ThemeDB.FallbackFont;
        SupportLogger.Initialize();

        if (File.Exists(AutosavePath))
        {
            try
            {
                var loaded = _saveService.Load(AutosavePath);
                _galaxy = loaded.Galaxy;
                _clock.Restore(loaded.SimulationSeconds);
                SetStatus($"Loaded autosave from {loaded.SavedAtUtc.LocalDateTime:g}");
                SupportLogger.Log("save", $"Loaded autosave seed={_galaxy.Seed} systems={_galaxy.Systems.Count}");
            }
            catch (Exception ex)
            {
                SupportLogger.Log("save-error", ex.ToString());
                GenerateNewGalaxy();
                SetStatus("Autosave could not be loaded; generated a new galaxy.");
            }
        }
        else
        {
            GenerateNewGalaxy();
        }

        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _clock.Advance(delta);
        _performanceLogTimer += delta;
        _statusTimer = Math.Max(0.0, _statusTimer - delta);

        if (_performanceLogTimer >= 5.0)
        {
            _performanceLogTimer = 0.0;
            SupportLogger.Log(
                "performance",
                $"fps={Engine.GetFramesPerSecond()} requested={_clock.RequestedMultiplier:0.00}x effective={_clock.EffectiveMultiplier:0.00}x backlog={_clock.BacklogSeconds:0.000}s managedMemory={GC.GetTotalMemory(false)}");
        }

        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && _galaxy is not null)
        {
            TryAutosave();
            GetTree().Quit();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    _clock.SetSpeed(_clock.Speed == SimulationClock.SpeedLevel.Paused
                        ? SimulationClock.SpeedLevel.Normal
                        : SimulationClock.SpeedLevel.Paused);
                    break;
                case Key.Key1:
                    _clock.SetSpeed(SimulationClock.SpeedLevel.Normal);
                    break;
                case Key.Key2:
                    _clock.SetSpeed(SimulationClock.SpeedLevel.Fast);
                    break;
                case Key.Key3:
                    _clock.SetSpeed(SimulationClock.SpeedLevel.VeryFast);
                    break;
                case Key.Key4:
                    _clock.SetSpeed(SimulationClock.SpeedLevel.Maximum);
                    break;
                case Key.F6:
                    TryAutosave();
                    break;
                case Key.F8:
                    var bundle = SupportLogger.ExportSupportBundle(File.Exists(AutosavePath) ? AutosavePath : null);
                    SetStatus($"Support bundle exported: {bundle}", 8.0);
                    _diagnostics.Add("support", $"Support bundle exported to {bundle}");
                    break;
            }

            QueueRedraw();
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
                _zoom = Math.Clamp(_zoom * 1.12f, 0.18f, 2.5f);
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
                _zoom = Math.Clamp(_zoom / 1.12f, 0.18f, 2.5f);
            else if (mouseButton.ButtonIndex == MouseButton.Middle)
                _panning = mouseButton.Pressed;
            else if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
                SelectNearestSystem(mouseButton.Position);

            QueueRedraw();
        }

        if (@event is InputEventMouseMotion motion && _panning)
        {
            _pan += motion.Relative;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var viewport = GetViewportRect();
        var center = viewport.Size * 0.5f + _pan;

        foreach (var system in _galaxy.Systems)
        {
            var position = center + new Godot.Vector2(system.Position.X, system.Position.Y) * _zoom;
            var radius = system.Id == _selectedSystemId ? 6.0f : 3.3f;
            DrawCircle(position, radius, GetStarColor(system.Archetype));

            if (system.Id == _selectedSystemId)
                DrawCircle(position, 10.0f, new Color(0.9f, 0.9f, 1.0f, 0.32f), false, 1.5f);
        }

        DrawString(_font, new Godot.Vector2(18, 30), "SPACE STRATEGY PROTOTYPE 0.0.1-dev.1", HorizontalAlignment.Left, -1, 18, Colors.White);
        DrawString(_font, new Godot.Vector2(18, 55), $"Seed: {_galaxy.Seed}    Systems: {_galaxy.Systems.Count}    Speed: {_clock.Speed} ({_clock.EffectiveMultiplier:0.00}x effective)", HorizontalAlignment.Left, -1, 16, new Color(0.78f, 0.83f, 0.92f));
        DrawString(_font, new Godot.Vector2(18, 80), "Space pause/resume | 1-4 speed | Mouse wheel zoom | Middle-drag pan | Click star | F6 save | F8 support bundle", HorizontalAlignment.Left, -1, 14, new Color(0.62f, 0.70f, 0.82f));

        if (_selectedSystemId >= 0)
        {
            var selected = _galaxy.Systems.First(s => s.Id == _selectedSystemId);
            var text = $"{selected.Name}  |  {selected.Archetype}  |  Habitable: {YesNo(selected.HasHabitableWorld)}  |  Anomaly: {YesNo(selected.HasAnomaly)}  |  Rare resource: {YesNo(selected.HasRareResource)}  |  Pre-warp: {YesNo(selected.HasPreWarpCivilization)}";
            DrawString(_font, new Godot.Vector2(18, viewport.Size.Y - 24), text, HorizontalAlignment.Left, Math.Max(300, viewport.Size.X - 36), 15, new Color(0.88f, 0.90f, 0.96f));
        }

        if (_statusTimer > 0.0 && !string.IsNullOrWhiteSpace(_statusText))
            DrawString(_font, new Godot.Vector2(18, 108), _statusText, HorizontalAlignment.Left, Math.Max(300, viewport.Size.X - 36), 14, new Color(0.98f, 0.84f, 0.47f));
    }

    private void GenerateNewGalaxy()
    {
        var seed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _galaxy = new GalaxyGenerator().Generate(seed);
        _clock.Restore(0.0);
        SupportLogger.Log("startup", $"Generated galaxy seed={seed} systems={_galaxy.Systems.Count}");
        _diagnostics.Add("galaxy", $"Generated seed {seed} with {_galaxy.Systems.Count} systems.");
    }

    private void TryAutosave()
    {
        try
        {
            _saveService.Save(AutosavePath, _galaxy, _clock.SimulationSeconds);
            SupportLogger.Log("save", $"Autosaved seed={_galaxy.Seed} simulationSeconds={_clock.SimulationSeconds:0.000}");
            SetStatus("Autosave complete.");
        }
        catch (Exception ex)
        {
            SupportLogger.Log("save-error", ex.ToString());
            SetStatus("Autosave failed. See logs.", 8.0);
        }
    }

    private void SetStatus(string text, double seconds = 4.0)
    {
        _statusText = text;
        _statusTimer = seconds;
    }

    private void SelectNearestSystem(Godot.Vector2 mousePosition)
    {
        var center = GetViewportRect().Size * 0.5f + _pan;
        var nearest = _galaxy.Systems
            .Select(s => new
            {
                s.Id,
                Distance = mousePosition.DistanceTo(center + new Godot.Vector2(s.Position.X, s.Position.Y) * _zoom),
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        _selectedSystemId = nearest is not null && nearest.Distance <= 14.0f ? nearest.Id : -1;
    }

    private static string YesNo(bool value) => value ? "yes" : "no";

    private static Color GetStarColor(StarArchetype archetype) => archetype switch
    {
        StarArchetype.ResourceRich => new Color(0.93f, 0.74f, 0.31f),
        StarArchetype.HabitableRich => new Color(0.38f, 0.87f, 0.55f),
        StarArchetype.BarrenFrontier => new Color(0.62f, 0.60f, 0.58f),
        StarArchetype.Nebula => new Color(0.67f, 0.43f, 0.91f),
        StarArchetype.NeutronPulsar => new Color(0.48f, 0.76f, 1.0f),
        StarArchetype.BlackHole => new Color(0.78f, 0.30f, 0.34f),
        StarArchetype.AncientRuin => new Color(0.95f, 0.55f, 0.26f),
        StarArchetype.Dangerous => new Color(0.95f, 0.27f, 0.27f),
        StarArchetype.Legendary => new Color(0.98f, 0.91f, 0.42f),
        _ => new Color(0.82f, 0.86f, 0.95f),
    };
}
