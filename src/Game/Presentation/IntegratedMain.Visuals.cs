namespace Game.Presentation;

public partial class IntegratedMain
{
    public override void _Draw()
    {
        if (UiIsSystemSpatialView)
            return;
        DrawVisualMapOverlay();
    }
}
