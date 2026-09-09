using System.Collections.Generic;
using Game.Simulation.Models;

namespace Game.Simulation.Shipbuilding;

public sealed record ShipDesignPrerequisites(
    IReadOnlyList<string> AllCivilizationCapabilities,
    IReadOnlyList<string> AnyCivilizationCapabilities,
    IReadOnlyList<string> RequiredConstructionProjects);

public sealed record ShipDesignDefinition(
    string Id,
    string Name,
    string Description,
    FleetRole Role,
    double IndustryCost,
    double StrategicSpeed,
    float SensorRange,
    ShipDesignPrerequisites Prerequisites,
    double PopulationCostMillions = 0.0,
    string? CombatProfileId = null,
    int CrewComplementIndividuals = 0,
    double CreditCost = 0.0);
