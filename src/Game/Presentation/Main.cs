using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Game.Diagnostics;
using Game.Persistence;
using Game.Simulation;
using Game.Simulation.Colonization;
using Game.Simulation.Economy;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Presentation;

public partial class Main : Node2D
{
    private readonly SimulationClock _clock = new();
    private readonly ExplorationSimulation _exploration = new();
    private readonly ColonizationSimulation _colonization = new();
    private readonly EconomySimulation _economy = new();
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
    private CivilizationState PlayerCivilization => _galaxy.Civilizations.First(c => c.Id == _galaxy.PlayerCivilizationId);
    private CivilizationEconomyState PlayerEconomy => _galaxy.Economies.First(e => e.CivilizationId == _galaxy.PlayerCivilizationId);
    private FleetState PlayerScout => _galaxy.Fleets.First(f => f.CivilizationId == _galaxy.PlayerCivilizationId && f.Role == FleetRole.Scout);
    private FleetState? PlayerColonyShip => _galaxy.Fleets.FirstOrDefault(f => f.IsActive && f.CivilizationId == _galaxy.PlayerCivilizationId && f.Role == FleetRole.Colony);

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
                SupportLogger.Log("save", $"Loaded autosave seed={_galaxy.Seed} systems={_galaxy.Systems.Count} civilizations={_galaxy.Civilizations.Count} fleets={_galaxy.Fleets.Count} colonies={_galaxy.Colonies.Count} format={CampaignSaveService.CurrentFormatVersion}");
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
        var simulationDelta = _clock.Advance(delta);
        HandleExplorationEvents(_exploration.Advance(_galaxy, simulationDelta));
        HandleColonizationEvents(_colonization.Advance(_galaxy));
        _economy.Advance(_galaxy, simulationDelta);

        _performanceLogTimer += delta;
        _statusTimer = Math.Max(0.0, _statusTimer - delta);

        if (_performanceLogTimer >= 5.0)
        {
            _performanceLogTimer = 0.0;
            var knownCount = _galaxy.Knowledge.GetKnownSystems(_galaxy.PlayerCivilizationId).Count;
            SupportLogger.Log("performance", $"fps={Engine.GetFramesPerSecond()} requested={_clock.RequestedMultiplier:0.00}x effective={_clock.EffectiveMultiplier:0.00}x backlog={_clock.BacklogSeconds:0.000}s managedMemory={GC.GetTotalMemory(false)} knownSystems={knownCount}/{_galaxy.Systems.Count} fleets={_galaxy.Fleets.Count(f => f.IsActive)} colonies={_galaxy.Colonies.Count}");
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
                case Key.Space: _clock.SetSpeed(_clock.Speed == SimulationClock.SpeedLevel.Paused ? SimulationClock.SpeedLevel.Normal : SimulationClock.SpeedLevel.Paused); break;
                case Key.Key1: _clock.SetSpeed(SimulationClock.SpeedLevel.Normal); break;
                case Key.Key2: _clock.SetSpeed(SimulationClock.SpeedLevel.Fast); break;
                case Key.Key3: _clock.SetSpeed(SimulationClock.SpeedLevel.VeryFast); break;
                case Key.Key4: _clock.SetSpeed(SimulationClock.SpeedLevel.Maximum); break;
                case Key.N: GenerateNewGalaxy(); SetStatus("Generated a new seeded galaxy."); break;
                case Key.F6: TryAutosave(); break;
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
                SelectNearestCatalogSystem(mouseButton.Position);
            else if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed && mouseButton.ShiftPressed)
                IssueColonyOrderAt(mouseButton.Position);
            else if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
                IssueScoutOrderAt(mouseButton.Position);
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
        var player = PlayerCivilization;
        var home = _galaxy.Systems.First(s => s.Id == player.HomeSystemId);
        var knownIds = _galaxy.Knowledge.GetKnownSystems(player.Id);
        var economy = PlayerEconomy;

        DrawCircle(ToScreen(home.Position, center), 230.0f * _zoom, new Color(0.28f, 0.62f, 0.95f, 0.14f), false, 1.0f);

        foreach (var system in _galaxy.Systems)
        {
            var known = _galaxy.Knowledge.IsSystemKnown(player.Id, system.Id);
            var position = ToScreen(system.Position, center);
            var isHome = system.Id == player.HomeSystemId;
            var radius = system.Id == _selectedSystemId ? 6.0f : isHome ? 5.0f : known ? 3.3f : 2.0f;
            DrawCircle(position, radius, known ? GetStarColor(system.Archetype) : new Color(0.38f, 0.42f, 0.50f, 0.55f));
            if (isHome) DrawCircle(position, 10.5f, new Color(0.30f, 0.76f, 1.0f, 0.75f), false, 2.0f);
            if (system.Id == _selectedSystemId) DrawCircle(position, 13.0f, new Color(0.95f, 0.95f, 1.0f, 0.38f), false, 1.5f);
        }

        DrawKnownColonies(center, player.Id);
        DrawKnownCivilizationHomes(center, player.Id);
        DrawPlayerFleet(center, PlayerScout, new Color(0.38f, 0.88f, 1.0f));
        if (PlayerColonyShip is { } colonyShip)
            DrawPlayerFleet(center, colonyShip, new Color(0.45f, 1.0f, 0.55f));

        DrawString(_font, new Godot.Vector2(18, 28), $"SPACE STRATEGY PROTOTYPE {GameVersion.Current}", HorizontalAlignment.Left, -1, 18, Colors.White);
        DrawString(_font, new Godot.Vector2(18, 52), $"{player.Name} | {player.Archetype} | Colonies: {_galaxy.Colonies.Count(c => c.CivilizationId == player.Id)} | Surveyed: {knownIds.Count}/{_galaxy.Systems.Count} | Contacts: {_galaxy.Knowledge.GetKnownCivilizations(player.Id).Count}", HorizontalAlignment.Left, -1, 15, new Color(0.78f, 0.83f, 0.92f));
        DrawString(_font, new Godot.Vector2(18, 74), $"Credits {economy.Credits:0.0} (+{economy.LastCreditsPerSecond:0.00}/s) | Industry {economy.Industry:0.0} (+{economy.LastIndustryPerSecond:0.00}/s) | Science {economy.Science:0.0} (+{economy.LastSciencePerSecond:0.00}/s) | Speed {_clock.Speed}", HorizontalAlignment.Left, -1, 14, new Color(0.72f, 0.82f, 0.72f));
        DrawString(_font, new Godot.Vector2(18, 96), "Right click: scout | Shift+Right click: colony ship | Pre-warp worlds cannot be colonized | Space pause | 1-4 speed | F6 save | F8 diagnostics", HorizontalAlignment.Left, -1, 13, new Color(0.62f, 0.70f, 0.82f));

        DrawSelectionDetails(viewport, player);
        if (_statusTimer > 0.0 && !string.IsNullOrWhiteSpace(_statusText))
            DrawString(_font, new Godot.Vector2(18, 122), _statusText, HorizontalAlignment.Left, Math.Max(300, viewport.Size.X - 36), 14, new Color(0.98f, 0.84f, 0.47f));
    }

    private void DrawKnownColonies(Godot.Vector2 center, int playerId)
    {
        foreach (var colony in _galaxy.Colonies)
        {
            var own = colony.CivilizationId == playerId;
            if (!own && (!_galaxy.Knowledge.IsSystemKnown(playerId, colony.SystemId) || !_galaxy.Knowledge.IsCivilizationKnown(playerId, colony.CivilizationId)))
                continue;
            var system = _galaxy.Systems.First(s => s.Id == colony.SystemId);
            var color = own ? new Color(0.32f, 0.92f, 0.62f, 0.78f) : new Color(0.96f, 0.42f, 0.38f, 0.72f);
            DrawCircle(ToScreen(system.Position, center), 12.0f, color, false, 2.0f);
        }
    }

    private void DrawKnownCivilizationHomes(Godot.Vector2 center, int playerId)
    {
        foreach (var civilization in _galaxy.Civilizations)
        {
            if (civilization.Id == playerId || !_galaxy.Knowledge.IsCivilizationKnown(playerId, civilization.Id)) continue;
            var home = _galaxy.Systems.First(system => system.Id == civilization.HomeSystemId);
            DrawCircle(ToScreen(home.Position, center), 16.0f, new Color(0.95f, 0.36f, 0.36f, 0.55f), false, 1.0f);
        }
    }

    private void DrawPlayerFleet(Godot.Vector2 center, FleetState fleet, Color color)
    {
        if (!fleet.IsActive) return;
        var position = ToScreen(fleet.Position, center);
        if (fleet.DestinationSystemId is not null)
        {
            var destination = _galaxy.Systems.First(s => s.Id == fleet.DestinationSystemId.Value);
            DrawDashedLine(position, ToScreen(destination.Position, center), new Color(color.R, color.G, color.B, 0.50f), 1.0f, 6.0f);
        }
        DrawCircle(position, fleet.Role == FleetRole.Colony ? 5.5f : 5.0f, color);
        DrawCircle(position, 9.0f, new Color(color.R, color.G, color.B, 0.30f), false, 1.5f);
    }

    private void DrawSelectionDetails(Rect2 viewport, CivilizationState player)
    {
        if (_selectedSystemId < 0) return;
        var selected = _galaxy.Systems.First(s => s.Id == _selectedSystemId);
        var known = _galaxy.Knowledge.IsSystemKnown(player.Id, selected.Id);
        string text;

        if (!known)
        {
            text = $"Astronomical target {_selectedSystemId + 1:000} | UNSURVEYED | Right-click sends scout";
        }
        else
        {
            var colony = _galaxy.Colonies.FirstOrDefault(c => c.SystemId == selected.Id);
            var visibleColony = colony is not null && (colony.CivilizationId == player.Id || _galaxy.Knowledge.IsCivilizationKnown(player.Id, colony.CivilizationId));
            var ownerLabel = visibleColony ? $" | Colony: {colony!.Name} | Pop {colony.PopulationMillions:0.0}M" : string.Empty;
            var preWarpLabel = selected.HasPreWarpCivilization ? " | NATIVE PRE-WARP CIVILIZATION" : string.Empty;
            text = $"{selected.Name} | {selected.Archetype}{ownerLabel}{preWarpLabel} | Habitable: {YesNo(selected.HasHabitableWorld)} | Anomaly: {YesNo(selected.HasAnomaly)} | Rare: {YesNo(selected.HasRareResource)}";
        }
        DrawString(_font, new Godot.Vector2(18, viewport.Size.Y - 24), text, HorizontalAlignment.Left, Math.Max(300, viewport.Size.X - 36), 15, new Color(0.88f, 0.90f, 0.96f));
    }

    private void HandleExplorationEvents(IReadOnlyList<ExplorationEvent> events)
    {
        foreach (var e in events)
        {
            SupportLogger.Log("exploration", $"civilization={e.CivilizationId} fleet={e.FleetId} system={e.SystemId} type={e.Type} message={e.Message}");
            if (e.CivilizationId != _galaxy.PlayerCivilizationId) continue;
            if (e.Type == ExplorationEventType.FirstContact) SetStatus(e.Message, 9.0);
            else if (e.Type == ExplorationEventType.SystemSurveyed) SetStatus(e.Message, 4.0);
        }
    }

    private void HandleColonizationEvents(IReadOnlyList<ColonizationEvent> events)
    {
        foreach (var e in events)
        {
            SupportLogger.Log("colonization", $"civilization={e.CivilizationId} fleet={e.FleetId} system={e.SystemId} colony={e.ColonyId} message={e.Message}");
            if (e.CivilizationId == _galaxy.PlayerCivilizationId) SetStatus(e.Message, 8.0);
        }
    }

    private void IssueScoutOrderAt(Godot.Vector2 mousePosition)
    {
        var target = FindNearestCatalogSystem(mousePosition, 16.0f);
        if (target is null) return;
        if (_exploration.IssueMoveOrder(_galaxy, PlayerScout.Id, target.Id))
        {
            var known = _galaxy.Knowledge.IsSystemKnown(_galaxy.PlayerCivilizationId, target.Id);
            SetStatus($"{PlayerScout.Name}: course set for {(known ? target.Name : $"astronomical target {target.Id + 1:000}")}.");
            SupportLogger.Log("order", $"fleet={PlayerScout.Id} role=scout destination={target.Id} known={known}");
        }
    }

    private void IssueColonyOrderAt(Godot.Vector2 mousePosition)
    {
        var target = FindNearestCatalogSystem(mousePosition, 16.0f);
        if (target is null) return;
        var result = _colonization.IssuePlayerColonyOrder(_galaxy, _galaxy.PlayerCivilizationId, target.Id);
        SetStatus(result.Message, result.Accepted ? 5.0 : 7.0);
        SupportLogger.Log("order", $"role=colony destination={target.Id} accepted={result.Accepted} message={result.Message}");
    }

    private void SelectNearestCatalogSystem(Godot.Vector2 mousePosition) => _selectedSystemId = FindNearestCatalogSystem(mousePosition, 14.0f)?.Id ?? -1;

    private StarSystemState? FindNearestCatalogSystem(Godot.Vector2 mousePosition, float threshold)
    {
        var center = GetViewportRect().Size * 0.5f + _pan;
        var nearest = _galaxy.Systems.Select(system => new { System = system, Distance = mousePosition.DistanceTo(ToScreen(system.Position, center)) }).OrderBy(x => x.Distance).FirstOrDefault();
        return nearest is not null && nearest.Distance <= threshold ? nearest.System : null;
    }

    private Godot.Vector2 ToScreen(System.Numerics.Vector2 position, Godot.Vector2 center) => center + new Godot.Vector2(position.X, position.Y) * _zoom;

    private void GenerateNewGalaxy()
    {
        var seed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _galaxy = new GalaxyGenerator().Generate(seed);
        _clock.Restore(0.0);
        _selectedSystemId = -1;
        _pan = Godot.Vector2.Zero;
        _zoom = 0.55f;
        var player = PlayerCivilization;
        SupportLogger.Log("startup", $"Generated galaxy seed={seed} systems={_galaxy.Systems.Count} civilizations={_galaxy.Civilizations.Count} fleets={_galaxy.Fleets.Count} colonies={_galaxy.Colonies.Count} player={player.Name}/{player.Archetype}");
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

    private void SetStatus(string text, double seconds = 4.0) { _statusText = text; _statusTimer = seconds; }
    private static string YesNo(bool value) => value ? "yes" : "no";
    private static Color GetStarColor(StarArchetype archetype) => archetype switch
    {
        StarArchetype.ResourceRich => new Color(0.93f, 0.74f, 0.31f), StarArchetype.HabitableRich => new Color(0.38f, 0.87f, 0.55f),
        StarArchetype.BarrenFrontier => new Color(0.62f, 0.60f, 0.58f), StarArchetype.Nebula => new Color(0.67f, 0.43f, 0.91f),
        StarArchetype.NeutronPulsar => new Color(0.48f, 0.76f, 1.0f), StarArchetype.BlackHole => new Color(0.78f, 0.30f, 0.34f),
        StarArchetype.AncientRuin => new Color(0.95f, 0.55f, 0.26f), StarArchetype.Dangerous => new Color(0.95f, 0.27f, 0.27f),
        StarArchetype.Legendary => new Color(0.98f, 0.91f, 0.42f), _ => new Color(0.82f, 0.86f, 0.95f),
    };
}
