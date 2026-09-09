using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Campaign;
using Game.Diagnostics;
using Game.Persistence;
using Game.Simulation;

namespace Game.Presentation;

public sealed record UiDeveloperCommand(string Id, string Title, string Description);
public sealed record UiDeveloperCommandResult(bool Accepted, string Message);

public partial class Main
{
    private readonly DeveloperCampaignSessionService _developerSessions = new();
    private readonly DeveloperCampaignPersistenceService _developerPersistence = new();
    private string DeveloperSavePath => Path.Combine(Path.GetDirectoryName(AutosavePath)!, DeveloperCampaignSessionService.SaveFileName);
    public bool UiIsDeveloperMode => _galaxy?.DeveloperSession is not null;
    public string UiModeLabel => UiIsDeveloperMode ? "Developer mode" : "Player mode";
    public bool UiDeveloperToolsUsed => _galaxy?.DeveloperSession?.ToolsUsed == true;
    public bool UiHasPlayerSave => File.Exists(AutosavePath) || File.Exists(AutosavePath + ".bak");
    public bool UiHasDeveloperSave => File.Exists(DeveloperSavePath) || File.Exists(DeveloperSavePath + ".bak") || UiHasDemoSave;
    public bool UiIsDeveloperToolsOpen => GetNodeOrNull<DeveloperToolsLayer>("DeveloperToolsLayer")?.IsOpen == true;
    public IReadOnlyList<UiDeveloperCommand> UiDeveloperCommands { get; } = DeveloperCommandService.Commands
        .Select(command => new UiDeveloperCommand(command.Id, command.Title, command.Description)).ToArray();

    protected void InitializeDeveloperTools()
    {
        if (GetNodeOrNull<DeveloperToolsLayer>("DeveloperToolsLayer") is null)
            AddChild(new DeveloperToolsLayer { Name = "DeveloperToolsLayer" });
    }

    public void UiOpenDeveloperTools()
    {
        if (!UiIsDeveloperMode || UiIsMenuOpen) return;
        InitializeDeveloperTools();
        GetNode<DeveloperToolsLayer>("DeveloperToolsLayer").Open();
    }

    public bool UiSwitchToPlayerMode()
    {
        if (!UiIsDeveloperMode) return true;
        if (!UiCheckpointBeforeCampaignSwitch()) return false;
        GetNodeOrNull<DeveloperToolsLayer>("DeveloperToolsLayer")?.Close();
        var bootstrap = _campaignSessionService.LoadOrCreate(AutosavePath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        ApplyIntegratedCampaign(bootstrap);
        _clock.SetSpeed(SimulationClock.SpeedLevel.Normal);
        CheckpointModeSwitch(bootstrap);
        return true;
    }

    public bool UiSwitchToDeveloperMode()
    {
        if (UiIsDeveloperMode) return true;
        if (!UiCheckpointBeforeCampaignSwitch()) return false;
        var bootstrap = _developerSessions.LoadOrCreate(DeveloperSavePath, PlayableDemoScenario.Seed);
        ApplyIntegratedCampaign(bootstrap);
        _clock.SetSpeed(SimulationClock.SpeedLevel.Demo);
        CheckpointModeSwitch(bootstrap);
        return true;
    }

    public void UiCreateDeveloperCampaignConfirmed(long seed)
    {
        GetNodeOrNull<DeveloperToolsLayer>("DeveloperToolsLayer")?.Close();
        var bootstrap = _developerSessions.CreateNew(seed);
        ApplyIntegratedCampaign(bootstrap);
        _clock.SetSpeed(SimulationClock.SpeedLevel.Demo);
        CheckpointModeSwitch(bootstrap);
    }

    private void CheckpointModeSwitch(CampaignBootstrapResult bootstrap)
    {
        if (!string.IsNullOrWhiteSpace(bootstrap.LoadFailure)) SupportLogger.Log("mode-load", bootstrap.LoadFailure);
        if (TryPersistIntegratedCampaign("mode-checkpoint", false, "Campaign opened, but its checkpoint failed. Retry Save; your other mode has its own save."))
        {
            var recovery = bootstrap.RecoveredFromBackup ? " Recovered the previous backup." :
                bootstrap.RecoveredFromInvalidSave ? " The save and backup could not be loaded; a fresh campaign was generated." : "";
            SetStatus(UiModeLabel + (UiIsDeveloperMode ? " · 24x. Developer tools run only when you choose them." : " · Ordinary gameplay rules.") + recovery, 10);
        }
        QueueRedraw();
    }

    public UiDeveloperCommandResult UiRunDeveloperCommand(string id)
    {
        if (_galaxy is null) return new(false, "No campaign is open.");
        if (UiIsMenuOpen) return new(false, "Close the campaign menu before running Developer tools.");
        try
        {
            var result = DeveloperCommandService.Execute(_galaxy, id, AdvanceDeveloperDays);
            if (!result.Accepted) return new(false, result.Message);
            QueueRedraw();
            if (!TryPersistIntegratedCampaign("developer-command", false, "Developer changes are active, but saving failed. Retry Save before switching campaigns."))
                return new(true, result.Message + " Saving failed; retry Save before switching.");
            SetStatus(result.Message, 8);
            return new(true, result.Message);
        }
        catch (Exception ex)
        {
            SupportLogger.Log("developer-command-error", ex.ToString());
            return new(false, $"Developer action failed: {ex.GetType().Name}: {ex.Message}. Tools used remains marked; reload a checkpoint if needed. See the support log.");
        }
    }
}
