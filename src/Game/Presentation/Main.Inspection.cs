using System.Linq;
using System.Text;
using Game.Simulation.Exploration;
using Game.Simulation.Knowledge;

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

            var surveyLevel = _galaxy.Knowledge.GetSystemSurveyLevel(playerId, selected.Id);
            if (surveyLevel == SystemSurveyLevel.Unknown)
            {
                return $"ASTRONOMICAL TARGET {selected.Id + 1:000}\n"
                     + "Status: Unknown / unsurveyed\n"
                     + (PlayerCivilization.DevelopmentStage == Game.Simulation.Models.CivilizationDevelopmentStage.PreWarp
                         ? "Interstellar operations are not yet available."
                         : "Send an exploration vessel to establish local information.");
            }

            // Consume the simulation-owned fog-safe read model rather than reading detailed
            // system facts directly. Detected/partial systems intentionally carry null details.
            var exploration = new ExplorationReadModel().Build(_galaxy, playerId);
            var inspection = exploration.KnownSystems.First(system => system.SystemId == selected.Id);
            var builder = new StringBuilder();
            builder.AppendLine(inspection.CatalogName);
            builder.Append("Survey status: ").AppendLine(inspection.SurveyLevel.ToString());
            builder.Append("Survey progress: ").AppendLine(inspection.SurveyProgress.ToString("P0"));

            if (!inspection.HasDetailedSurvey)
            {
                if (inspection.SurveyLevel == SystemSurveyLevel.Detected)
                    builder.Append("Detailed planet, resource, anomaly, and native-civilization data remain unknown. Send a scout for reconnaissance or a science vessel for a detailed survey.");
                else
                    builder.Append("Reconnaissance is incomplete. A science vessel must finish the detailed survey before colonization-grade facts are available.");
                return builder.ToString();
            }

            builder.Append("Star region: ").AppendLine(inspection.Archetype?.ToString() ?? "Unknown");
            builder.Append("Habitable world detected: ").AppendLine(YesNo(inspection.HasHabitableWorld == true));
            builder.Append("Anomaly detected: ").AppendLine(YesNo(inspection.HasAnomaly == true));
            builder.Append("Rare resource signature: ").AppendLine(YesNo(inspection.HasRareResource == true));
            builder.Append("Known pre-warp civilization: ").AppendLine(YesNo(inspection.HasPreWarpCivilization == true));

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
