using System;
using System.Globalization;

namespace Game.Simulation.Time;

public static class CampaignCalendar
{
    public const int StartingYear = 2050;
    private static readonly DateTime Epoch = new(StartingYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DateTime GetDate(double simulationDays)
    {
        var safeDays = Math.Clamp(simulationDays, 0.0, (new DateTime(9999, 12, 31) - Epoch).TotalDays);
        return Epoch.AddDays(safeDays);
    }

    public static string FormatDate(double simulationDays) =>
        GetDate(simulationDays).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static int GetYear(double simulationDays) => GetDate(simulationDays).Year;
}
