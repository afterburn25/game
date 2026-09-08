using System;
using System.Linq;
using System.Text;
using Game.Simulation.Colonization;
using Game.Simulation.Models;

namespace Game.Presentation;

/// <summary>
/// Player-facing colony-opportunity adapter. Candidate eligibility, passenger-species viability,
/// knowledge filtering and reach reasons come from the authoritative Core/Colonization planner;
/// presentation only bounds and formats those already-filtered results.
/// </summary>
public partial class Main
{
    public string UiColonyOpportunityDetails
    {
        get
        {
            if (_galaxy is null)
                return "Colony opportunities are initializing…";

            var playerId = _galaxy.PlayerCivilizationId;
            var colonyFleets = _galaxy.Fleets
                .Where(fleet =>
                    fleet.IsActive &&
                    fleet.CivilizationId == playerId &&
                    fleet.Role == FleetRole.Colony &&
                    fleet.EmbarkedPopulationMillions > 0.0)
                .OrderBy(fleet => fleet.Id)
                .ToArray();

            if (colonyFleets.Length == 0)
                return "No populated colony ships are currently available for settlement planning.";

            var builder = new StringBuilder();
            builder.Append("Populated colony ships: ").AppendLine(colonyFleets.Length.ToString());

            foreach (var fleet in colonyFleets.Take(2))
            {
                var plan = _coreSimulation.GetColonyOpportunityPlan(
                    _galaxy,
                    fleet.Id,
                    maximumCandidates: 8);

                builder.Append("• ").Append(fleet.Name);
                if (!string.IsNullOrWhiteSpace(plan.PassengerSpeciesName))
                    builder.Append(" — ").Append(plan.PassengerSpeciesName);
                builder.Append(" — ").Append(fleet.EmbarkedPopulationMillions.ToString("0.#")).AppendLine("M aboard");

                if (!plan.CanReceiveOrders)
                {
                    builder.Append("  ").AppendLine(plan.Status);
                    continue;
                }

                var sites = plan.Candidates.Take(2).ToArray();
                if (sites.Length == 0)
                {
                    builder.Append("  ").AppendLine(plan.Status);
                    continue;
                }

                foreach (var site in sites)
                {
                    builder.Append(site.CanOrder ? "  ✓ " : "  · ")
                        .Append(site.SystemName)
                        .Append(" / ").Append(site.PlanetaryBodyName)
                        .Append(" — ").Append(FormatColonizationViability(site.ColonizationViability))
                        .Append(" — fit ").Append(site.NaturalHabitability.ToString("P0"))
                        .AppendLine();
                    builder.Append("    ").AppendLine(CompactPlannerReason(site.Reason));
                }
            }

            if (colonyFleets.Length > 2)
                builder.Append("• +").Append(colonyFleets.Length - 2).Append(" more populated colony ship").Append(colonyFleets.Length - 2 == 1 ? string.Empty : "s");

            return builder.ToString().TrimEnd();
        }
    }

    private static string FormatColonizationViability(Game.Simulation.Species.SpeciesColonizationViability viability) => viability switch
    {
        Game.Simulation.Species.SpeciesColonizationViability.NaturallyViable => "natural",
        Game.Simulation.Species.SpeciesColonizationViability.HabitatSupportedFallback => "habitat support",
        _ => "unsuitable",
    };

    private static string CompactPlannerReason(string reason)
    {
        const int maximumLength = 118;
        if (string.IsNullOrWhiteSpace(reason) || reason.Length <= maximumLength)
            return reason;

        return reason[..(maximumLength - 1)].TrimEnd() + "…";
    }
}
