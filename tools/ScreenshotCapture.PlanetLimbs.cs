using System;
using System.IO;
using System.Threading.Tasks;
using Game.Presentation.Spatial;
using Game.Simulation.Models;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private async Task VerifyPlanetLimbDiagnosticsAsync()
    {
        var earth = Marker(9301, "earth", PlanetaryAtmosphereRegime.OxygenNitrogen, SystemSpatialBodyVisualClass.Oceanic, true);
        var venus = Marker(9306, "venus", PlanetaryAtmosphereRegime.CarbonDioxideRich, SystemSpatialBodyVisualClass.HotRocky, true);
        var mercury = Marker(9302, "mercury", PlanetaryAtmosphereRegime.Vacuum, SystemSpatialBodyVisualClass.HotRocky, true);
        var rocky = Marker(9307, null, PlanetaryAtmosphereRegime.Vacuum, SystemSpatialBodyVisualClass.Rocky, true);
        var earthImage = await RenderPlanetMaterialAsync(earth, "planet-limbs-earth");
        _ = await RenderPlanetMaterialAsync(venus, "planet-limbs-venus");
        var mercuryImage = await RenderPlanetMaterialAsync(mercury, "planet-limbs-mercury");
        _ = await RenderPlanetMaterialAsync(rocky, "planet-limbs-rocky");
        Require(earthImage.GetPixel(128, 128).A > .98f && mercuryImage.GetPixel(128, 128).A > .98f,
            "planet material diagnostic found a transparent planetary centre");
        Require(mercuryImage.GetPixel(0, 0).A < .001f,
            "airless planet material left alpha outside its limb");
        // Light points upper-left for this marker. The globe radius is half-size / 1.08,
        // matching the shader's p=(UV*2-1)*1.08 geometry rather than testing an unlit edge.
        Require(earthImage.GetPixel(29, 78).A > .001f && mercuryImage.GetPixel(29, 78).A < .001f &&
                earthImage.GetPixel(12, 71).A < .001f,
            "illuminated Earth limb did not fade from atmosphere to transparent space continuously");
        var unknownImage = await RenderPlanetMaterialAsync(Marker(9303, "earth", PlanetaryAtmosphereRegime.OxygenNitrogen,
            SystemSpatialBodyVisualClass.UnknownPlanet, false), "planet-limbs-unknown");
        var knownCentre = earthImage.GetPixel(128, 128);
        var unknownCentre = unknownImage.GetPixel(128, 128);
        Require(MathF.Abs(unknownCentre.R - knownCentre.R) + MathF.Abs(unknownCentre.G - knownCentre.G) + MathF.Abs(unknownCentre.B - knownCentre.B) > .03f,
            "unknown body diagnostic rendered a known surface texture");
        var unknownOther = await RenderPlanetMaterialAsync(Marker(9303, "venus", PlanetaryAtmosphereRegime.OxygenNitrogen,
            SystemSpatialBodyVisualClass.UnknownPlanet, false), "planet-limbs-unknown-other-key");
        Require(unknownOther.GetPixel(128, 128) == unknownCentre,
            "unknown bodies changed their rendered appearance based on hidden texture keys");
        var vacuumEarth = earth with { Atmosphere = PlanetaryAtmosphereRegime.Vacuum };
        var vacuumImage = await RenderPlanetMaterialAsync(vacuumEarth, "planet-limbs-earth-vacuum");
        Require(vacuumImage.GetPixel(29, 78).A < .001f,
            "same-body atmosphere-to-vacuum refresh retained an atmospheric alpha shell");
        Check(true, "native-planet-limb-material-alpha-readback");
    }

    private async Task<Image> RenderPlanetMaterialAsync(SystemSpatialBodyMarker marker, string name)
    {
        var viewport = new SubViewport { Size = new Vector2I(256, 256), TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        viewport.AddChild(new TextureRect { Texture = CelestialBodyMaterials.WhiteTexture, Material = CelestialBodyMaterials.GetPlanetMaterial(marker), Size = new Vector2(256, 256), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize });
        AddChild(viewport);
        await WaitFramesAsync(4);
        var image = viewport.GetTexture().GetImage();
        var path = Path.Combine(_outputDirectory, name + ".png");
        Require(image.SavePng(path) == Error.Ok, $"Could not write {name} material diagnostic.");
        _captures.Add(name + ".png");
        viewport.QueueFree();
        return image;
    }

    private static SystemSpatialBodyMarker Marker(int id, string? key, PlanetaryAtmosphereRegime atmosphere, SystemSpatialBodyVisualClass visualClass, bool known) =>
        new(id, null, 0, key ?? "Rocky", PlanetaryBodyKind.Planet, visualClass, 20, 8, 1, 1, known, false, false, false, 1, 1, 1, 280, 100, atmosphere, key);
}
