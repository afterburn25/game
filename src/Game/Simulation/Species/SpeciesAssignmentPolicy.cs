using System;

namespace Game.Simulation.Species;

public static class SpeciesAssignmentPolicy
{
    public static string Assign(long campaignSeed, int civilizationId)
    {
        if (civilizationId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(civilizationId));
        }

        var species = SpeciesCatalog.All;
        if (species.Count == 0)
        {
            throw new InvalidOperationException("No species definitions are available.");
        }

        var mixed = Mix(unchecked((ulong)campaignSeed) ^ ((ulong)(uint)civilizationId * 0xD1B54A32D192ED03UL));
        return species[(int)(mixed % (ulong)species.Count)].Id;
    }

    private static ulong Mix(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
