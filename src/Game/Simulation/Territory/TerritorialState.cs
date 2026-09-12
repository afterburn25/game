using System;
using System.Collections.Generic;

namespace Game.Simulation.Territory;

public enum TerritorialInstallationKind { Relay, SupplyDepot, TradeHub, NavalBase, ResearchStation, Administration }
public enum ExpansionRegion { Established, Frontier, Remote }
public enum TerritorialControlStatus { Independent, Influenced, Dominant, Controlled, Contested }

/// <summary>Persistent facts only. Scores, territorial polygons and path caches are never saved.</summary>
public sealed class TerritorialState
{
    public int SchemaVersion { get; set; } = 1;
    public double ElapsedDays { get; set; }
    public double NextReviewDay { get; set; }
    public double NextDiplomaticReviewDay { get; set; }
    public List<TerritorialBorderIncident> BorderIncidents { get; set; } = new();
    public List<TerritorialInstallation> Installations { get; set; } = new();
    public List<TerritorialExpedition> Expeditions { get; set; } = new();
}

public sealed class TerritorialInstallation
{
    public int Id { get; set; }
    public int CivilizationId { get; set; }
    public int SystemId { get; set; }
    public TerritorialInstallationKind Kind { get; set; }
    public int BuilderFleetId { get; set; }
    public double CompletedDays { get; set; }
    public double RequiredDays { get; set; }
    public double PaidCredits { get; set; }
    public double PaidIndustry { get; set; }
    public bool IsComplete => CompletedDays >= RequiredDays;
}

public sealed record TerritorialExpedition(int FleetId, int BodyId, double PaidCredits, double RequiredDays);
public sealed record TerritorialBorderIncident(int FirstCivilizationId, int SecondCivilizationId, double LastDay);
public sealed record TerritorialInstallationDefinition(TerritorialInstallationKind Kind, string Name,
    double Credits, double Industry, double Days, double Upkeep, double Political, double Administration,
    double Trade, double Military, double Range);

/// <summary>One balance surface. All reach distances are physical light years.</summary>
public static class TerritorialBalance
{
    public const double ReviewDays = 5;
    public const double ColonyRange = 32;
    public const double OutpostRange = 16;
    public const double RelayConnectionRange = 22;
    public const double RelayAttenuation = .78;
    public const double IndependentWeight = 14;
    public const double FrontierMinimumPolitical = 4;
    public const double FrontierMinimumAdministration = .12;
    public const double FrontierMinimumSupply = .12;
    public const double MinimumTaxCollection = .70;
    public static IReadOnlyList<TerritorialInstallationDefinition> Installations { get; } = new[]
    {
        new TerritorialInstallationDefinition(TerritorialInstallationKind.Relay, "Communications relay", 45, 90, 15, .045, 14, .75, .12, 0, 22),
        new TerritorialInstallationDefinition(TerritorialInstallationKind.SupplyDepot, "Supply depot", 80, 160, 25, .08, 18, .35, .35, .10, 20),
        new TerritorialInstallationDefinition(TerritorialInstallationKind.TradeHub, "Trade station", 100, 200, 30, .10, 24, .35, .85, 0, 22),
        new TerritorialInstallationDefinition(TerritorialInstallationKind.NavalBase, "Naval base", 150, 300, 40, .18, 26, .45, .15, .75, 24),
        new TerritorialInstallationDefinition(TerritorialInstallationKind.ResearchStation, "Research station", 90, 180, 28, .09, 20, .30, .10, 0, 20),
        new TerritorialInstallationDefinition(TerritorialInstallationKind.Administration, "Regional administration", 130, 240, 35, .12, 32, .90, .30, .10, 26),
    };
    public static TerritorialInstallationDefinition Definition(TerritorialInstallationKind kind)
    {
        foreach (var definition in Installations) if (definition.Kind == kind) return definition;
        throw new ArgumentOutOfRangeException(nameof(kind));
    }
}

public sealed record TerritorialContribution(int SystemId, string Kind, double Political, double Administration);
public sealed record CivilizationSystemTerritory(int CivilizationId, int SystemId, double Political,
    double Share, double Administration, double Supply, double Trade, double Military, double EffectiveControl,
    double TaxCollection, double AdministrationMultiplier, ExpansionRegion Expansion,
    IReadOnlyList<TerritorialContribution> Sources)
{
    public double ExpeditionMultiplier => Expansion == ExpansionRegion.Established ? 1 : 1.35 + (1 - Administration) * .65;
    public double EstablishmentMultiplier => Expansion == ExpansionRegion.Established ? 1 : 1.25 + (1 - Supply) * .75;
    public double InstabilityRisk => Math.Clamp((1 - EffectiveControl) * .45 + (1 - Supply) * .25, 0, 1);
}
public sealed record SystemTerritory(int SystemId, int? ControllerId, TerritorialControlStatus Status,
    double IndependentShare, IReadOnlyList<CivilizationSystemTerritory> Civilizations);
public sealed record TerritorialOrderResult(bool Accepted, string Message, int? InstallationId = null);
