using System;
using Game.Simulation;
using Godot;

namespace Game.Presentation;

/// <summary>One compact, consistent simulation clock control for strategic and surface views.</summary>
public sealed partial class PlaybackControl : HBoxContainer
{
    private readonly Button _button;
    private readonly Label _state;
    private readonly Func<PlaybackState> _readState;
    private readonly Action _cycle;
    private readonly Action _togglePause;

    public PlaybackControl(string name, Func<PlaybackState> readState, Action cycle, Action togglePause)
    {
        Name = name;
        _readState = readState;
        _cycle = cycle;
        _togglePause = togglePause;
        AddThemeConstantOverride("separation", 5);
        _button = VisualUi.Button("▶", string.Empty, _cycle);
        _button.Name = name + "Button";
        _button.CustomMinimumSize = new Vector2(42, 36);
        _button.GuiInput += HandlePointerInput;
        AddChild(_button);
        _state = VisualUi.Text("", 11, VisualUi.Muted, true);
        _state.Name = name + "State";
        _state.CustomMinimumSize = new Vector2(54, 0);
        _state.VerticalAlignment = VerticalAlignment.Center;
        AddChild(_state);
        Refresh();
    }

    public Button Button => _button;
    public Label StateLabel => _state;

    public void Refresh()
    {
        var state = _readState();
        var displayed = state.IsPaused ? state.ResumeSpeed : state.CurrentSpeed;
        _button.Text = IconFor(state.IsPaused ? SimulationClock.SpeedLevel.Paused : state.CurrentSpeed, state.IsDeveloperMode);
        _button.Modulate = state.IsPaused ? VisualUi.Gold : Colors.White;
        _state.Text = StateText(state.IsPaused, displayed, state.IsDeveloperMode);
        _state.TooltipText = state.IsPaused
            ? $"Paused. Right-click resumes at {SpeedText(state.ResumeSpeed, state.IsDeveloperMode)}."
            : $"Running at {SpeedText(state.CurrentSpeed, state.IsDeveloperMode)}.";
        _button.TooltipText = state.IsPaused
            ? $"Paused. Left-click starts at 1×. Right-click resumes at {SpeedText(state.ResumeSpeed, state.IsDeveloperMode)}. Keyboard: Space."
            : $"{SpeedText(state.CurrentSpeed, state.IsDeveloperMode)}. Left-click: {NextAction(state)}. Right-click pauses immediately. Keyboard: Space.";
    }

    public static SimulationClock.SpeedLevel NextSpeed(SimulationClock.SpeedLevel current, bool isDeveloperMode) => current switch
    {
        SimulationClock.SpeedLevel.Paused => SimulationClock.SpeedLevel.Normal,
        SimulationClock.SpeedLevel.Normal => SimulationClock.SpeedLevel.Fast,
        SimulationClock.SpeedLevel.Fast => SimulationClock.SpeedLevel.VeryFast,
        SimulationClock.SpeedLevel.VeryFast => SimulationClock.SpeedLevel.Maximum,
        SimulationClock.SpeedLevel.Maximum when isDeveloperMode => SimulationClock.SpeedLevel.Demo,
        SimulationClock.SpeedLevel.Maximum => SimulationClock.SpeedLevel.Paused,
        SimulationClock.SpeedLevel.Demo => SimulationClock.SpeedLevel.Paused,
        _ => SimulationClock.SpeedLevel.Normal,
    };

    private void HandlePointerInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }) return;
        _togglePause();
        AcceptEvent();
    }

    private static string IconFor(SimulationClock.SpeedLevel speed, bool developer) => speed switch
    {
        SimulationClock.SpeedLevel.Paused => "▶",
        SimulationClock.SpeedLevel.Normal => "▶",
        SimulationClock.SpeedLevel.Fast => "▶▶",
        SimulationClock.SpeedLevel.VeryFast => "▶▶▶",
        SimulationClock.SpeedLevel.Maximum when !developer => "Ⅱ",
        SimulationClock.SpeedLevel.Maximum => "▶▶▶▶",
        SimulationClock.SpeedLevel.Demo => "Ⅱ",
        _ => "▶",
    };

    private static string StateText(bool paused, SimulationClock.SpeedLevel speed, bool developer) => paused
        ? "PAUSED"
        : SpeedText(speed, developer);

    private static string SpeedText(SimulationClock.SpeedLevel speed, bool developer) => speed switch
    {
        SimulationClock.SpeedLevel.Normal => "1×",
        SimulationClock.SpeedLevel.Fast => "2×",
        SimulationClock.SpeedLevel.VeryFast => "3×",
        SimulationClock.SpeedLevel.Maximum => "8×",
        SimulationClock.SpeedLevel.Demo when developer => "24× DEV",
        _ => "1×",
    };

    private static string NextAction(PlaybackState state)
    {
        var next = NextSpeed(state.CurrentSpeed, state.IsDeveloperMode);
        return next == SimulationClock.SpeedLevel.Paused ? "pause" : $"set {SpeedText(next, state.IsDeveloperMode)}";
    }
}

public readonly record struct PlaybackState(bool IsPaused, SimulationClock.SpeedLevel CurrentSpeed,
    SimulationClock.SpeedLevel ResumeSpeed, bool IsDeveloperMode);
