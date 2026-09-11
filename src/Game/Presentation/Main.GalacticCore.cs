using Game.Simulation.Generation;
using Godot;

namespace Game.Presentation;

public partial class Main
{
    /// <summary>The landmark is secret until this observer has both unlocked access and explored it.</summary>
    public GalacticCoreMetadata? UiGalacticCore => _galaxy is not null &&
        _galaxy.Knowledge.IsGalacticCoreDiscovered(_galaxy.PlayerCivilizationId) ? _galaxy.GalacticCore : null;

    public Vector2? UiGalacticCoreScreenPosition => UiGalacticCore is { } core
        ? ToScreen(new System.Numerics.Vector2(core.X, core.Y), UiMapOriginScreen)
        : null;

    public float UiGalacticCoreScreenRadius => UiGalacticCore is { } core
        ? core.ExclusionRadius * UiMapZoom
        : 0;

    internal Vector2? UndisclosedCoreScreenPosition => _galaxy?.GalacticCore is { } core
        ? ToScreen(new System.Numerics.Vector2(core.X, core.Y), UiMapOriginScreen) : null;

    private bool IsInsideGalacticCoreMarker(Vector2 screenPoint) =>
        UndisclosedCoreScreenPosition is { } center &&
        screenPoint.DistanceTo(center) <= _galaxy!.GalacticCore!.ExclusionRadius * UiMapZoom;

    private void ExplainUnavailableGalacticCore()
    {
        if (UiGalacticCore is null) return; // Fog does not announce the secret or grant knowledge.
        SetStatus("No safe approach route is available yet.", 6.0);
    }
}
