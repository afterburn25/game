using System;

namespace Game.Simulation.Species;

public enum BodyPlan
{
    UprightBilateral,
    HorizontalBilateral,
    Radial,
    Elongated,
    Amorphous,
    SyntheticFrame,
    Other,
}

public enum LocomotionMode
{
    Bipedal,
    Quadrupedal,
    Multipedal,
    AquaticSwimming,
    Amphibious,
    Slithering,
    Flight,
    Synthetic,
    Other,
}

public enum WorkOrientation
{
    Upright,
    Horizontal,
    FreeSwimming,
    Radial,
    OrientationIndependent,
}

/// <summary>
/// Baseline body geometry used for ergonomics, habitat layout and equipment
/// compatibility. It is not a direct combat or productivity modifier.
/// </summary>
public sealed record SpeciesMorphology(
    BodyPlan BodyPlan,
    LocomotionMode LocomotionMode,
    WorkOrientation PreferredWorkOrientation,
    double TypicalBodyLengthMeters,
    double TypicalBodyWidthMeters,
    double TypicalReachMeters,
    int PrimaryManipulatorCount,
    int FineManipulatorCount,
    bool RequiresBuoyantWorkspace = false)
{
    public SpeciesMorphology Validated()
    {
        ValidatePositive(TypicalBodyLengthMeters, nameof(TypicalBodyLengthMeters));
        ValidatePositive(TypicalBodyWidthMeters, nameof(TypicalBodyWidthMeters));
        ValidatePositive(TypicalReachMeters, nameof(TypicalReachMeters));

        if (PrimaryManipulatorCount < 0 || PrimaryManipulatorCount > 16)
        {
            throw new InvalidOperationException("Primary manipulator count must be between 0 and 16.");
        }

        if (FineManipulatorCount < 0 || FineManipulatorCount > PrimaryManipulatorCount)
        {
            throw new InvalidOperationException("Fine manipulator count must be between 0 and the primary manipulator count.");
        }

        return this;
    }

    private static void ValidatePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
        }
    }
}

public sealed record SpeciesEquipmentCompatibilityAssessment(
    string UserSpeciesId,
    string ReferenceSpeciesId,
    double DirectUseCompatibility,
    double BodyScaleCompatibility,
    double ReachCompatibility,
    double ManipulatorCompatibility,
    double OrientationCompatibility,
    bool RequiresControlAdaptation,
    bool RequiresWorkspaceAdaptation,
    bool RequiresEnvironmentalEnclosure)
{
    public bool DirectUsePractical => DirectUseCompatibility >= 0.75 &&
                                      !RequiresEnvironmentalEnclosure;
}

/// <summary>
/// Directional ergonomic assessment for a member of one species using equipment or
/// crew space designed around another species' baseline body. The result describes
/// adaptation work; it does not determine whether the underlying technology is
/// scientifically understandable or manufacturable.
/// </summary>
public static class SpeciesEquipmentCompatibilityEvaluator
{
    public static SpeciesEquipmentCompatibilityAssessment Evaluate(
        SpeciesDefinition user,
        SpeciesDefinition reference)
    {
        user.Validated();
        reference.Validated();

        var userMorphology = user.Morphology;
        var referenceMorphology = reference.Morphology;

        var bodyScale = RatioCompatibility(
            userMorphology.TypicalBodyLengthMeters,
            referenceMorphology.TypicalBodyLengthMeters,
            sensitivity: 1.35) *
            RatioCompatibility(
                userMorphology.TypicalBodyWidthMeters,
                referenceMorphology.TypicalBodyWidthMeters,
                sensitivity: 1.15);
        bodyScale = Math.Sqrt(bodyScale);

        var reach = RatioCompatibility(
            userMorphology.TypicalReachMeters,
            referenceMorphology.TypicalReachMeters,
            sensitivity: 1.35);

        var referenceFineManipulators = Math.Max(1, referenceMorphology.FineManipulatorCount);
        var manipulator = referenceMorphology.FineManipulatorCount == 0
            ? 1.0
            : Math.Clamp((double)userMorphology.FineManipulatorCount / referenceFineManipulators, 0.0, 1.0);

        var orientation = OrientationCompatibility(
            userMorphology.PreferredWorkOrientation,
            referenceMorphology.PreferredWorkOrientation);

        var direct = Math.Pow(
            Math.Max(0.0, bodyScale) *
            Math.Max(0.0, reach) *
            Math.Max(0.0, manipulator) *
            Math.Max(0.0, orientation),
            0.25);

        var requiresControlAdaptation = manipulator < 0.999 || reach < 0.75;
        var requiresWorkspaceAdaptation = bodyScale < 0.80 || orientation < 0.80;
        var requiresEnvironmentalEnclosure =
            user.Environment.RequiresImmersion != reference.Environment.RequiresImmersion ||
            user.Environment.BiologicalSolvent != reference.Environment.BiologicalSolvent ||
            !reference.BreathableAtmospheres.Contains(user.Environment.PreferredAtmosphere);

        return new SpeciesEquipmentCompatibilityAssessment(
            user.Id,
            reference.Id,
            Math.Clamp(direct, 0.0, 1.0),
            Math.Clamp(bodyScale, 0.0, 1.0),
            Math.Clamp(reach, 0.0, 1.0),
            Math.Clamp(manipulator, 0.0, 1.0),
            Math.Clamp(orientation, 0.0, 1.0),
            requiresControlAdaptation,
            requiresWorkspaceAdaptation,
            requiresEnvironmentalEnclosure);
    }

    private static double RatioCompatibility(double user, double reference, double sensitivity)
    {
        var logRatio = Math.Abs(Math.Log(user / reference));
        return Math.Exp(-sensitivity * logRatio);
    }

    private static double OrientationCompatibility(WorkOrientation user, WorkOrientation reference)
    {
        if (user == reference ||
            user == WorkOrientation.OrientationIndependent ||
            reference == WorkOrientation.OrientationIndependent)
        {
            return 1.0;
        }

        if ((user == WorkOrientation.Upright && reference == WorkOrientation.Horizontal) ||
            (user == WorkOrientation.Horizontal && reference == WorkOrientation.Upright))
        {
            return 0.62;
        }

        if (user == WorkOrientation.FreeSwimming || reference == WorkOrientation.FreeSwimming)
        {
            return 0.35;
        }

        if (user == WorkOrientation.Radial || reference == WorkOrientation.Radial)
        {
            return 0.55;
        }

        return 0.50;
    }
}
