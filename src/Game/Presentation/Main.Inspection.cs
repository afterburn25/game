using System.Linq;
using System.Text;

namespace Game.Presentation;

/// <summary>
/// Player-facing inspection text assembled only from information the player is currently
/// allowed to know. Authoritative simulation state remains outside presentation controls.
/// </summary>
public partial class Main
{
    public int UiSelectedSystemId => _selectedSystemId;

    public string UiSelectedSystemInspection
    {
        get
        {
            if (_galaxy is null)
                return "Initializing campaign…";
            if (_selectedSystemId < 0)
                return "Select a star system to inspect it.";

            var playerId = _galaxy.PlayerCivilizationId;
            var selected = _galaxy.Systems.FirstOrDefault(system => system.Id == _selectedSystemId);
            if (selected is null)
                return "Selected system is no longer available.";

            var known = _galaxy.Knowledge.IsSystemKnown(playerId, selected.Id);
            if (!known)
            {
                return $"ASTRONOMICAL TARGET {selected.Id + 1:000}\n"
                     + "Status: Unsurveyed\n"
                     + (PlayerCivilization.DevelopmentStage == Game.Simulation.Models.CivilizationDevelopmentStage.PreWarp
                         ? "Interstellar operations are not yet available."
                         : "Send a scout to establish reliable local information.");
            }

            var builder = new StringBuilder();
            builder.AppendLine(selected.Name);
            builder.Append("Star region: ").AppendLine(selected.Archetype.ToString());
            builder.Append("Habitable world detected: ").AppendLine(YesNo(selected.HasHabitableWorld));
            builder.Append("Anomaly detected: ").AppendLine(YesNo(selected.HasAnomaly));
            builder.Append("Rare resource signature: ").AppendLine(YesNo(selected.HasRareResource));
            builder.Append("Known pre-warp civilization: ").AppendLine(YesNo(selected.HasPreWarpCivilization));

            var colony = _galaxy.Colonies.FirstOrDefault(candidate => candidate.SystemId == selected.Id);
            if (colony is null)
            {
                builder.Append("Colony: none known");
                return builder.ToString();
            }

            var ownColony = colony.CivilizationId == playerId;
            var foreignColonyKnown = ownColony || _galaxy.Knowledge.IsCivilizationKnown(playerId, colony.CivilizationId);
            if (!foreignColonyKnown)
            {
                builder.Append("Colony: presence not reliably identified");
                return builder.ToString();
            }

            builder.Append("Colony: ").AppendLine(colony.Name);
            builder.Append("Population: ").Append(colony.PopulationMillions.ToString("0.0")).AppendLine("M");
            builder.Append("Infrastructure: ").AppendLine(colony.Infrastructure.ToString("0.00"));
            builder.Append("Stability: ").AppendLine(colony.Stability.ToString("P0"));

            if (ownColony)
            {
                var logistics = _economyLogisticsView.GetSnapshot(_galaxy, playerId);
                var colonyLogistics = logistics.Colonies.FirstOrDefault(item => item.ColonyId == colony.Id);
                if (colonyLogistics is not null)
                {
                    builder.Append("Supply: ").Append(colonyLogistics.Condition)
                        .Append(" · local coverage ").AppendLine(colonyLogistics.CoverageRatio.ToString("P0"));
                    builder.Append("Imported support needed: ")
                        .Append(colonyLogistics.ImportedSupportRequiredPerDay.ToString("0.00"))
                        .Append("/day");
                }
            }

            return builder.ToString();
        }
    }
}
