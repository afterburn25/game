using System;

namespace Game.Simulation.Species;

/// <summary>
/// Biological/physical prerequisites that a diplomacy, first-contact, medical, or
/// hospitality system may need to satisfy before two species can interact comfortably.
/// This does not determine language understanding, political willingness, trust, opinion,
/// xenophobia, friendliness, diplomatic success, or legal status.
/// </summary>
public sealed record SpeciesFirstContactInterfaceRequirements(
    string FirstSpeciesId,
    string SecondSpeciesId,
    double NaturalCommunicationCompatibility,
    bool HasAnyNaturalCommunicationChannel,
    bool RequiresSensoryTranslation,
    bool RequiresCommunicationMediation,
    bool RequiresMutualEnvironmentalAccommodation,
    double NutritionalCrossCompatibility,
    bool RequiresDedicatedNutrition,
    double CrossPathogenTransmissionPotential,
    bool RequiresCrossSpeciesQuarantineAssessment,
    double TissueIntegrationPotential,
    bool RequiresXenomedicalInterfaceAdaptation,
    bool NaturalHybridizationPossible)
{
    public bool RequiresTechnicalMediation =>
        RequiresSensoryTranslation ||
        RequiresCommunicationMediation ||
        RequiresMutualEnvironmentalAccommodation ||
        RequiresXenomedicalInterfaceAdaptation;
}

public static class SpeciesFirstContactInterfaceEvaluator
{
    public static SpeciesFirstContactInterfaceRequirements Evaluate(
        SpeciesDefinition first,
        SpeciesDefinition second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        first.Validated();
        second.Validated();

        var communication = SpeciesCommunicationCompatibilityEvaluator.Evaluate(first, second);
        var xenobiology = SpeciesXenobiologyCompatibilityEvaluator.Evaluate(first, second);
        var firstUsingSecond = SpeciesEquipmentCompatibilityEvaluator.Evaluate(first, second);
        var secondUsingFirst = SpeciesEquipmentCompatibilityEvaluator.Evaluate(second, first);

        return new SpeciesFirstContactInterfaceRequirements(
            first.Id,
            second.Id,
            communication.NaturalCommunicationCompatibility,
            communication.HasAnyNaturalCommunicationChannel,
            communication.RequiresSensoryTranslation,
            communication.RequiresCommunicationMediation,
            RequiresMutualEnvironmentalAccommodation:
                firstUsingSecond.RequiresEnvironmentalEnclosure ||
                secondUsingFirst.RequiresEnvironmentalEnclosure,
            xenobiology.NutritionalCrossCompatibility,
            xenobiology.RequiresDedicatedNutrition,
            xenobiology.CrossPathogenTransmissionPotential,
            xenobiology.RequiresCrossSpeciesQuarantineAssessment,
            xenobiology.TissueIntegrationPotential,
            xenobiology.RequiresXenomedicalInterfaceAdaptation,
            xenobiology.NaturalHybridizationPossible);
    }
}
