using System.IO;
using Game.Campaign;
using Game.Simulation;

namespace Game.Presentation;

public partial class Main
{
    private bool _isPlayableDemo;
    private string CurrentCampaignSavePath => _isPlayableDemo ? PlayableDemoScenario.SavePathBeside(AutosavePath) : AutosavePath;
    public bool UiIsPlayableDemo => _isPlayableDemo;
    public bool UiHasDemoSave => File.Exists(PlayableDemoScenario.SavePathBeside(AutosavePath)) || File.Exists(PlayableDemoScenario.SavePathBeside(AutosavePath) + ".bak");
    public SimulationClock.SpeedLevel UiCurrentSpeed => _clock.Speed;
    public DemoObjectiveSnapshot? UiDemoObjective => _isPlayableDemo && _galaxy is not null ? DemoObjectiveView.Build(_galaxy, _clock.RequestedMultiplier) : null;
    public void UiResumeAtSpeed(SimulationClock.SpeedLevel speed) => _clock.SetSpeed(speed);
    public void UiResumeDemoSpeed()
    {
        if (_isPlayableDemo && !(GetNodeOrNull<MainMenuLayer>("MainMenuLayer")?.IsBlockingGameplay ?? false))
            _clock.SetSpeed(SimulationClock.SpeedLevel.Demo);
    }

    public bool UiCheckpointBeforeCampaignSwitch() => _galaxy is null || TryPersistIntegratedCampaign(
        "save-before-switch", false, "Campaign switch cancelled because saving failed. Your current campaign is still open.");

    public void UiCreateNewCampaignConfirmed() => CreateIntegratedNewCampaign();

    public void UiPlayDemoConfirmed()
    {
        var bootstrap = PlayableDemoScenario.Create(_campaignSessionService);
        _isPlayableDemo = true;
        ApplyIntegratedCampaign(bootstrap);
        _clock.SetSpeed(SimulationClock.SpeedLevel.Demo);
        if (TryPersistIntegratedCampaign("save-demo-start", false, "Demo checkpoint failed; retry scheduled. Your normal autosave is unchanged."))
            SetStatus("Playable Demo · 24x. Begin Fusion Propulsion research and build the Research Network. Your normal campaign has its own save slot.", 10);
        QueueRedraw();
    }

    public void UiContinueDemo()
    {
        var bootstrap = _campaignSessionService.LoadOrCreate(PlayableDemoScenario.SavePathBeside(AutosavePath), PlayableDemoScenario.Seed);
        _isPlayableDemo = true;
        ApplyIntegratedCampaign(bootstrap);
        _clock.SetSpeed(SimulationClock.SpeedLevel.Demo);
        if (bootstrap.WasLoaded)
            SetStatus("Demo resumed at 24x. Your normal campaign remains in its own save slot.", 8);
        else
        {
            TryPersistIntegratedCampaign("save-demo-recovery", false, "Demo recovery checkpoint failed; retry scheduled.");
            SetStatus("Demo save could not be loaded; a new demo was generated. Your normal campaign is unchanged.", 8);
        }
        QueueRedraw();
    }
}
