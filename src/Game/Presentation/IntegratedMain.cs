using Godot;

namespace Game.Presentation;

/// <summary>
/// Scene entry point for the integrated early-release runtime. Core owns campaign lifecycle
/// and strategic simulation timing here; all existing drawing, ordinary input and command
/// presentation behavior remains inherited from Main.
/// </summary>
public partial class IntegratedMain : Main
{
    private bool _runtimeReady;
    private bool _startupReported;

    public override void _Ready()
    {
        RunIntegratedCampaignReady();
        InitializeSpatialPresentation();
        InitializeSurfacePresentation();
        InitializeDeveloperTools();
        _runtimeReady = true;
    }

    public override void _Process(double delta)
    {
        RunIntegratedSimulationFrame(delta);
        RefreshSpatialPresentation(delta);
        RefreshSurfacePresentation();
        if (_runtimeReady && !_startupReported)
        {
            // Prove that the actual scene entry point initialized its campaign and ran a frame.
            // CI also rejects engine errors before or after this marker, including child scripts.
            _startupReported = true;
            GD.Print("STELLAR_RUNTIME_READY IntegratedMain");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _ = delta;
        RefreshIntegratedShipbuildingPresentation();
    }

    public override void _Input(InputEvent @event)
    {
        if (ShouldBlockGameplayInput())
            return;

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.N)
            {
                UiNewCampaign();
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

        // Pointer commands must wait until GUI controls have had the opportunity to consume them.
        if (@event is not InputEventMouse)
            base._Input(@event);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (ShouldBlockGameplayInput())
            return;

        // Record pointer commands only after GUI consumption, including rejected orders.
        // This read-only diagnostic lets runtime checks detect invisible click-through.
        if (@event is InputEventMouseButton { Pressed: true } pointer &&
            pointer.ButtonIndex is MouseButton.Left or MouseButton.Right)
            UiPointerCommandRevision++;

        if (HandleSpatialPresentationInput(@event) ||
            (UiIsSystemSpatialView && @event is InputEventMouse))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouse)
        {
            // Main's science-order shortcut is historically in _Input. Route it after GUI just
            // like scout/colony orders, so neither panels nor the system canvas can be clicked through.
            base._Input(@event);
            if (GetViewport().IsInputHandled())
                return;
        }

        base._UnhandledInput(@event);
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
