using Game.Simulation.Generation;
using Godot;

namespace Game.Presentation;

public partial class Main
{
    /// <summary>Observer-safe public landmark metadata for the always-visible galactic core.</summary>
    public GalacticCoreMetadata? UiGalacticCore => _galaxy?.GalacticCore;

    public Vector2? UiGalacticCoreScreenPosition => _galaxy?.GalacticCore is { } core
        ? ToScreen(new System.Numerics.Vector2(core.X, core.Y), UiMapOriginScreen)
        : null;

    public float UiGalacticCoreScreenRadius => _galaxy?.GalacticCore is { } core
        ? core.ExclusionRadius * UiMapZoom
        : 0;

    private bool IsInsideGalacticCoreMarker(Vector2 screenPoint) =>
        UiGalacticCoreScreenPosition is { } center &&
        screenPoint.DistanceTo(center) <= UiGalacticCoreScreenRadius;

    private void ExplainUnavailableGalacticCore() => SetStatus(
        "The supermassive black hole is catalogued, but no safe approach route is available yet.", 6.0);
}
