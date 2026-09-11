using System;
using System.Globalization;

namespace Game.Presentation;

/// <summary>Display-only SI formatting. Simulation ratios and schematic map coordinates stay untouched.</summary>
public static class MetricFormat
{
    public const double EarthRadiusKilometres = 6_371.0;
    public const double EarthMassKilograms = 5.9722e24;
    public const double StandardGravityMetresPerSecondSquared = 9.80665;
    public const double KilometresPerLightYear = 9.4607304725808e12;

    public static string Radius(double earthRadii, bool confirmed) =>
        confirmed && double.IsFinite(earthRadii) && earthRadii > 0.0
            ? $"{earthRadii * EarthRadiusKilometres:N0} km"
            : "Unconfirmed";

    public static string Mass(double? earthMasses, bool confirmed) =>
        confirmed && earthMasses is double mass && double.IsFinite(mass) && mass >= 0.0
            ? Scientific(mass * EarthMassKilograms) + " kg"
            : "Unconfirmed";

    public static string Gravity(double? gravityG, bool confirmed) =>
        confirmed && gravityG is double gravity && double.IsFinite(gravity) && gravity >= 0.0
            ? $"{gravity * StandardGravityMetresPerSecondSquared:0.##} m/s²"
            : "Unconfirmed";

    public static string Temperature(double? kelvin, bool confirmed) =>
        confirmed && kelvin is double temperature && double.IsFinite(temperature) && temperature >= 0.0
            ? $"{temperature:0} K"
            : "Unconfirmed";

    public static string Pressure(double? kiloPascals, bool confirmed) =>
        confirmed && kiloPascals is double pressure && double.IsFinite(pressure) && pressure >= 0.0
            ? $"{pressure:0.##} kPa"
            : "Unconfirmed";

    public static string PhysicalSummary(double radiusEarth, double? massEarth, double? gravityG, bool confirmed) =>
        confirmed && massEarth.HasValue && gravityG.HasValue
            ? $"{Radius(radiusEarth, true)} · {Mass(massEarth, true)} · {Gravity(gravityG, true)}"
            : "Physical data unconfirmed";

    public static string InterstellarDistance(double lightYears, double parsecs) =>
        double.IsFinite(lightYears) && lightYears >= 0.0
            ? InterstellarLength(lightYears) + $" · {parsecs:0.0} pc"
            : "Distance unconfirmed";

    public static string InterstellarDistance(double lightYears) =>
        InterstellarDistance(lightYears, lightYears / 3.26156);

    public static string InterstellarLength(double lightYears) =>
        double.IsFinite(lightYears) && lightYears >= 0.0
            ? $"{Scientific(lightYears * KilometresPerLightYear)} km · {lightYears:0.#} ly"
            : "Distance unconfirmed";

    public static string InterstellarSpeed(double lightYearsPerDay) =>
        double.IsFinite(lightYearsPerDay) && lightYearsPerDay >= 0.0
            ? $"{Scientific(lightYearsPerDay * KilometresPerLightYear)} km/day · {lightYearsPerDay:0.#} ly/day"
            : "Speed unconfirmed";

    private static string Scientific(double value) => value >= 1_000_000.0
        ? value.ToString("0.###e+0", CultureInfo.InvariantCulture)
        : value.ToString("N0", CultureInfo.InvariantCulture);
}
