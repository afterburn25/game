using Godot;

namespace Game.Presentation;

/// <summary>
/// Scene entry point for the integrated early-release runtime. Core owns campaign lifecycle
/// and strategic simulation timing here; all existing drawing, ordinary input and command
/// presentation behavior remains inherited from Main.
/// </summary>
public partial class IntegratedMain : Main
{
    public override void _Ready()
    {
        RunIntegratedCampaignReady();
        InitializeSpatialPresentation();
    }

    public override void _Process(double delta)
    {
        RunIntegratedSimulationFrame(delta);
        RefreshSpatialPresentation(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _ = delta;
        RefreshIntegratedShipbuildingPresentation();
    }

    public override void _Input(InputEvent @event)
    {
        if (HandleSpatialPresentationInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.N)
            {
                CreateIntegratedNewCampaign();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (key.Keycode == Key.F6)
            {
                SaveIntegratedCampaign();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        base._Input(@event);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            HandleIntegratedCloseRequest();
            return;
        }

        base._Notification(what);
    }
}
