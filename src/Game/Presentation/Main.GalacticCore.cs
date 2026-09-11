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
}
