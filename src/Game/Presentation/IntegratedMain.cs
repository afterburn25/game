namespace Game.Presentation;

/// <summary>
/// Scene entry point for the integrated early-release runtime. It replaces only the timing
/// callbacks from Main; all existing drawing, input, save and command behavior is inherited.
/// </summary>
public partial class IntegratedMain : Main
{
    public override void _Process(double delta) => RunIntegratedSimulationFrame(delta);

    public override void _PhysicsProcess(double delta)
    {
        _ = delta;
        RefreshIntegratedShipbuildingPresentation();
    }
}
