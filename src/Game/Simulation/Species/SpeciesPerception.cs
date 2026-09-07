using System;
using System.Numerics;

namespace Game.Simulation.Species;

[Flags]
public enum SensoryModality
{
    None = 0,
    VisibleLight = 1 << 0,
    NearInfrared = 1 << 1,
    Ultraviolet = 1 << 2,
    AirborneSound = 1 << 3,
    WaterborneSound = 1 << 4,
    Vibration = 1 << 5,
    Chemoreception = 1 << 6,
    Electrosense = 1 << 7,
    MagneticSense = 1 << 8,
    PressureSense = 1 << 9,
    DirectDigital = 1 << 10,
}

[Flags]
public enum CommunicationModality
{
    None = 0,
    AirborneVocal = 1 << 0,
    WaterborneVocal = 1 << 1,
    VisualGesture = 1 << 2,
    Bioluminescent = 1 << 3,
    Chemical = 1 << 4,
    Electrical = 1 << 5,
    Vibration = 1 << 6,
    DirectDigital = 1 << 7,
}

/// <summary>
/// Natural perception/communication channels. This says nothing about intelligence,
/// language complexity, culture, truthfulness or diplomatic skill.
/// </summary>
public sealed record SpeciesPerceptionProfile(
    SensoryModality SensoryModalities,
    CommunicationModality CommunicationModalities)
{
    public SpeciesPerceptionProfile Validated(bool synthetic)
    {
        if (!synthetic && SensoryModalities == SensoryModality.None)
        {
            throw new InvalidOperationException("Biological species must define at least one sensory modality.");
        }

        if (!synthetic && CommunicationModalities == CommunicationModality.None)
        {
            throw new InvalidOperationException("Biological species must define at least one natural communication modality.");
        }

        return this;
    }
}

public sealed record SpeciesCommunicationCompatibilityAssessment(
    string FirstSpeciesId,
    string SecondSpeciesId,
    double SharedSensoryCompatibility,
    double SharedCommunicationCompatibility,
    double NaturalCommunicationCompatibility,
    int SharedSensoryModalities,
    int SharedCommunicationModalities,
    bool RequiresSensoryTranslation,
    bool RequiresCommunicationMediation)
{
    public bool HasAnyNaturalCommunicationChannel => SharedCommunicationModalities > 0;
}

/// <summary>
/// Symmetric physical channel compatibility only. Language, meaning, cognition and
/// diplomacy remain separate systems; technology can mediate even a zero-channel pair.
/// </summary>
public static class SpeciesCommunicationCompatibilityEvaluator
{
    public static SpeciesCommunicationCompatibilityAssessment Evaluate(
        SpeciesDefinition first,
        SpeciesDefinition second)
    {
        first.Validated();
        second.Validated();

        var firstSenses = (uint)first.Perception.SensoryModalities;
        var secondSenses = (uint)second.Perception.SensoryModalities;
        var firstCommunications = (uint)first.Perception.CommunicationModalities;
        var secondCommunications = (uint)second.Perception.CommunicationModalities;

        var sharedSenseCount = BitOperations.PopCount(firstSenses & secondSenses);
        var senseUnionCount = Math.Max(1, BitOperations.PopCount(firstSenses | secondSenses));
        var sharedCommunicationCount = BitOperations.PopCount(firstCommunications & secondCommunications);
        var communicationUnionCount = Math.Max(1, BitOperations.PopCount(firstCommunications | secondCommunications));

        var sensoryCompatibility = (double)sharedSenseCount / senseUnionCount;
        var communicationCompatibility = (double)sharedCommunicationCount / communicationUnionCount;
        var naturalCompatibility = sharedCommunicationCount == 0
            ? 0.0
            : Math.Sqrt(sensoryCompatibility * communicationCompatibility);

        return new SpeciesCommunicationCompatibilityAssessment(
            first.Id,
            second.Id,
            sensoryCompatibility,
            communicationCompatibility,
            naturalCompatibility,
            sharedSenseCount,
            sharedCommunicationCount,
            RequiresSensoryTranslation: sensoryCompatibility < 0.50,
            RequiresCommunicationMediation: naturalCompatibility < 0.50);
    }
}
