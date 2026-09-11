using System;
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
    private bool _startupSmokeRequested;

    public override void _Ready()
    {
        _startupSmokeRequested = Array.IndexOf(OS.GetCmdlineUserArgs(), "--stellar-startup-smoke") >= 0;
        AddChild(new ResponsiveDisplay { Name = "ResponsiveDisplay" });
        RunIntegratedCampaignReady();
        InitializeSpatialPresentation();
        InitializeSurfacePresentation();
        InitializeDeveloperTools();
        InitializeVoicePresentation();
        _runtimeReady = true;
    }

    public override void _Process(double delta)
    {
        if (!RunMassiveCombatFrame(delta))
            RunIntegratedSimulationFrame(delta);
        RefreshSpatialPresentation(delta);
        RefreshSurfacePresentation();
        RefreshVoicePresentation(delta);
        if (_runtimeReady && !_startupReported)
        {
            // Prove that the actual scene entry point initialized its campaign and ran a frame.
            // CI also rejects engine errors before or after this marker, including child scripts.
            _startupReported = true;
            GD.Print("STELLAR_RUNTIME_READY IntegratedMain");
            if (_startupSmokeRequested)
                HandleIntegratedCloseRequest();
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

        if (UiIsMassiveCombatActive && @event is InputEventKey { Pressed: true, Echo: false } tacticalKey)
        {
            var handled = true;
            switch (tacticalKey.Keycode)
            {
                case Key.Space: UiSetPaused(!UiIsPaused); break;
                case Key.Key1: UiSetTacticalSpeed(.25); break;
                case Key.Key2: UiSetTacticalSpeed(.5); break;
                case Key.Key3: UiSetTacticalSpeed(1); break;
                case Key.Key4: UiSetTacticalSpeed(2); break;
                case Key.Key5: UiSetTacticalSpeed(4); break;
                default: handled = false; break;
            }
            if (handled)
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }

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
