using System;
using System.Collections.Generic;

namespace Game.Simulation.Species;

public enum MolecularChirality
{
    LeftHanded,
    RightHanded,
    Mixed,
    NotApplicable,
}

public enum HereditarySystem
{
    NucleicAcidLike,
    AlternativeBiopolymer,
    DistributedChemical,
    SyntheticCode,
    Other,
}

public enum CellularOrganization
{
    Cellular,
    Syncytial,
    Colonial,
    NonCellularBiological,
    Synthetic,
    Other,
}

/// <summary>
/// Molecular/medical facts used for xenobiological compatibility. These do not imply
/// social affinity, intelligence, morality, culture or diplomatic attitude.
/// </summary>
public sealed record SpeciesXenobiologyProfile(
    MolecularChirality NutrientChirality,
    HereditarySystem HereditarySystem,
    CellularOrganization CellularOrganization,
    bool UsesProteinLikeCatalysts,
    bool SupportsSelfReplicatingMicroscopicParasites)
{
    public SpeciesXenobiologyProfile Validated(bool synthetic)
    {
        if (synthetic && CellularOrganization != CellularOrganization.Synthetic)
        {
            throw new InvalidOperationException("Synthetic species must use synthetic xenobiological organization in the current model.");
        }

        if (!synthetic && CellularOrganization == CellularOrganization.Synthetic)
        {
            throw new InvalidOperationException("Biological species cannot use synthetic cellular organization.");
        }

        return this;
    }
}

public sealed record SpeciesBiologicalRelationship(
    string FirstSpeciesId,
    string SecondSpeciesId,
    double NaturalReproductiveCompatibility,
    bool NaturalViableHybridOffspringPossible)
{
    public SpeciesBiologicalRelationship Validated()
    {
        if (string.IsNullOrWhiteSpace(FirstSpeciesId) || string.IsNullOrWhiteSpace(SecondSpeciesId))
        {
            throw new InvalidOperationException("Biological relationship species IDs cannot be empty.");
        }

        if (!double.IsFinite(NaturalReproductiveCompatibility) ||
            NaturalReproductiveCompatibility < 0.0 ||
            NaturalReproductiveCompatibility > 1.0)
        {
            throw new InvalidOperationException("Natural reproductive compatibility must be between 0 and 1.");
        }

        if (NaturalViableHybridOffspringPossible && NaturalReproductiveCompatibility <= 0.0)
        {
            throw new InvalidOperationException("Viable natural hybrids require nonzero reproductive compatibility.");
        }

        return this;
    }
}

/// <summary>
/// Explicit pair catalog. Cross-species reproductive compatibility is NEVER inferred
/// from shared solvent, atmosphere or carbon chemistry. A pair must be deliberately
/// authored here (or in future data) before natural hybridization is possible.
/// </summary>
public static class SpeciesBiologicalRelationshipCatalog
{
    private static readonly IReadOnlyDictionary<string, SpeciesBiologicalRelationship> Relationships =
        new Dictionary<string, SpeciesBiologicalRelationship>(StringComparer.Ordinal);

    public static SpeciesBiologicalRelationship Get(string firstSpeciesId, string secondSpeciesId)
    {
        if (string.Equals(firstSpeciesId, secondSpeciesId, StringComparison.Ordinal))
        {
            return new SpeciesBiologicalRelationship(firstSpeciesId, secondSpeciesId, 1.0, true);
        }

        return Relationships.TryGetValue(Key(firstSpeciesId, secondSpeciesId), out var relationship)
            ? relationship.Validated()
            : new SpeciesBiologicalRelationship(firstSpeciesId, secondSpeciesId, 0.0, false);
    }

    private static string Key(string first, string second) =>
        string.CompareOrdinal(first, second) <= 0 ? $"{first}|{second}" : $"{second}|{first}";
}

public sealed record SpeciesXenobiologyCompatibilityAssessment(
    string FirstSpeciesId,
    string SecondSpeciesId,
    double BiochemicalInteroperability,
    double NutritionalCrossCompatibility,
    double CrossPathogenTransmissionPotential,
    double TissueIntegrationPotential,
    double NaturalReproductiveCompatibility,
    bool NaturalViableHybridOffspringPossible,
    bool RequiresDedicatedNutrition,
    bool RequiresCrossSpeciesQuarantineAssessment,
    bool RequiresXenomedicalInterfaceAdaptation)
{
    public bool NaturalHybridizationPossible =>
        NaturalViableHybridOffspringPossible && NaturalReproductiveCompatibility > 0.0;
}

public static class SpeciesXenobiologyCompatibilityEvaluator
{
    public static SpeciesXenobiologyCompatibilityAssessment Evaluate(
        SpeciesDefinition first,
        SpeciesDefinition second)
    {
        first.Validated();
        second.Validated();

        if (string.Equals(first.Id, second.Id, StringComparison.Ordinal))
        {
            return new SpeciesXenobiologyCompatibilityAssessment(
                first.Id,
                second.Id,
                1.0,
                1.0,
                first.Xenobiology.SupportsSelfReplicatingMicroscopicParasites ? 1.0 : 0.0,
                1.0,
                1.0,
                true,
                RequiresDedicatedNutrition: false,
                RequiresCrossSpeciesQuarantineAssessment: first.Xenobiology.SupportsSelfReplicatingMicroscopicParasites,
                RequiresXenomedicalInterfaceAdaptation: false);
        }

        var biochemistry = BiochemistryCompatibility(first, second);
        var chirality = ChiralityCompatibility(first.Xenobiology.NutrientChirality, second.Xenobiology.NutrientChirality);
        var hereditary = first.Xenobiology.HereditarySystem == second.Xenobiology.HereditarySystem ? 1.0 : 0.30;
        var organization = first.Xenobiology.CellularOrganization == second.Xenobiology.CellularOrganization ? 1.0 : 0.35;

        var nutrition = Math.Clamp(
            biochemistry *
            chirality *
            (first.Xenobiology.UsesProteinLikeCatalysts == second.Xenobiology.UsesProteinLikeCatalysts ? 1.0 : 0.45),
            0.0,
            1.0);

        // This is a potential for biological cross-transmission if compatible pathogens
        // are present, not an assertion that any specific pathogen currently exists.
        var pathogens =
            first.Xenobiology.SupportsSelfReplicatingMicroscopicParasites &&
            second.Xenobiology.SupportsSelfReplicatingMicroscopicParasites
                ? Math.Clamp(biochemistry * chirality * hereditary * organization, 0.0, 1.0)
                : 0.0;

        var tissue = Math.Clamp(
            biochemistry * chirality * hereditary * organization,
            0.0,
            1.0);

        var relationship = SpeciesBiologicalRelationshipCatalog.Get(first.Id, second.Id).Validated();

        return new SpeciesXenobiologyCompatibilityAssessment(
            first.Id,
            second.Id,
            biochemistry,
            nutrition,
            pathogens,
            tissue,
            relationship.NaturalReproductiveCompatibility,
            relationship.NaturalViableHybridOffspringPossible,
            RequiresDedicatedNutrition: nutrition < 0.80,
            RequiresCrossSpeciesQuarantineAssessment: pathogens >= 0.10,
            RequiresXenomedicalInterfaceAdaptation: tissue < 0.80);
    }

    private static double BiochemistryCompatibility(SpeciesDefinition first, SpeciesDefinition second)
    {
        if (first.Biochemistry == second.Biochemistry)
        {
            return 1.0;
        }

        var bothOrganicCarbon = first.Biochemistry is
                BiochemicalBasis.CarbonWater or
                BiochemicalBasis.CarbonAmmonia or
                BiochemicalBasis.CarbonHydrocarbon &&
            second.Biochemistry is
                BiochemicalBasis.CarbonWater or
                BiochemicalBasis.CarbonAmmonia or
                BiochemicalBasis.CarbonHydrocarbon;

        if (bothOrganicCarbon)
        {
            return 0.35;
        }

        if (first.Biochemistry == BiochemicalBasis.Synthetic || second.Biochemistry == BiochemicalBasis.Synthetic)
        {
            return 0.05;
        }

        return 0.15;
    }

    private static double ChiralityCompatibility(MolecularChirality first, MolecularChirality second)
    {
        if (first == MolecularChirality.NotApplicable || second == MolecularChirality.NotApplicable)
        {
            return first == second ? 1.0 : 0.10;
        }

        if (first == second || first == MolecularChirality.Mixed || second == MolecularChirality.Mixed)
        {
            return 1.0;
        }

        return 0.10;
    }
}
