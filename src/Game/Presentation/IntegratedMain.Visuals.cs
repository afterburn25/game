namespace Game.Presentation;

public partial class IntegratedMain
{
    public override void _Draw()
    {
        if (UiIsSystemSpatialView && UiSystemViewBlend >= 1)
            return;
        DrawVisualMapOverlay();
    }
}
