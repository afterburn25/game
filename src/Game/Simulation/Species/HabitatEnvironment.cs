using System;

namespace Game.Simulation.Species;

public sealed record HabitatEnvironment(
    double GravityG,
    double TemperatureKelvin,
    double PressureKPa,
    AtmosphereClass Atmosphere,
    SolventClass AvailableSolvent,
    double RadiationHazard,
    bool IsImmersed = false)
{
    public HabitatEnvironment Validated()
    {
        if (!double.IsFinite(GravityG) || GravityG < 0.0)
        {
            throw new InvalidOperationException("Habitat gravity must be finite and non-negative.");
        }

        if (!double.IsFinite(TemperatureKelvin) || TemperatureKelvin <= 0.0)
        {
            throw new InvalidOperationException("Habitat temperature must be finite and above absolute zero.");
        }

        if (!double.IsFinite(PressureKPa) || PressureKPa < 0.0)
        {
            throw new InvalidOperationException("Habitat pressure must be finite and non-negative.");
        }

        if (!double.IsFinite(RadiationHazard) || RadiationHazard < 0.0 || RadiationHazard > 1.0)
        {
            throw new InvalidOperationException("Radiation hazard must be between 0 and 1.");
        }

        return this;
    }
}
