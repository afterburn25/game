using System.IO;
using Game.Diagnostics;
using Game.Simulation;

namespace Game.Presentation;

/// <summary>
/// Narrow player-command surface used by Godot UI controls. The UI invokes the same
/// authoritative commands as the existing prototype keyboard shortcuts rather than
/// duplicating simulation rules in presentation code.
/// </summary>
public partial class Main
{
    public bool UiIsPaused => _clock.Speed == SimulationClock.SpeedLevel.Paused;
    public string UiSpeedLabel => $"{_clock.Speed} · {_clock.EffectiveMultiplier:0.00}x";
    public string UiBuildLabel => $"Stellar Continuum {GameVersion.Current}";

    public void UiTogglePause() => UiSetPaused(!UiIsPaused);

    public void UiSetPaused(bool paused, bool announce = true)
    {
        _clock.SetSpeed(paused ? SimulationClock.SpeedLevel.Paused : SimulationClock.SpeedLevel.Normal);
        if (announce)
            SetStatus(paused ? "Simulation paused." : "Simulation resumed.");
        QueueRedraw();
    }

    public void UiSetSpeed(int level)
    {
        if (level < (int)SimulationClock.SpeedLevel.Normal || level > (int)SimulationClock.SpeedLevel.Maximum)
            return;

        _clock.SetSpeed((SimulationClock.SpeedLevel)level);
        SetStatus($"Simulation speed set to {_clock.Speed}.");
        QueueRedraw();
    }

    public void UiCycleResearch()
    {
        CycleResearchCandidate();
        QueueRedraw();
    }

    public void UiStartResearch()
    {
        StartSelectedResearch();
        QueueRedraw();
    }

    public void UiCycleConstruction()
    {
        CycleConstructionCandidate();
        QueueRedraw();
    }

    public void UiStartConstruction()
    {
        StartSelectedConstruction();
        QueueRedraw();
    }

    public void UiNewCampaign() => CreateIntegratedNewCampaign();

    public void UiSave() => SaveIntegratedCampaign();

    public void UiExportDiagnostics()
    {
        var bundle = SupportLogger.ExportSupportBundle(File.Exists(AutosavePath) ? AutosavePath : null);
        SetStatus($"Support bundle exported: {bundle}", 8.0);
        _diagnostics.Add("support", $"Support bundle exported to {bundle}");
        QueueRedraw();
    }

    public void UiQuit() => HandleIntegratedCloseRequest();
}
