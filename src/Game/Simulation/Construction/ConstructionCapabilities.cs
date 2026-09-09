using System.Linq;
using Game.Simulation.Models;
using Game.Simulation.Research.Adaptive;

namespace Game.Simulation.Construction;

public interface IConstructionCapabilityView
{
    bool HasCivilizationCapability(GalaxyState galaxy, int civilizationId, string capabilityId);
}

/// <summary>Compatibility adapter for isolated legacy simulations and old-save validation.</summary>
public sealed class PrototypeConstructionCapabilityView : IConstructionCapabilityView
{
    public bool HasCivilizationCapability(GalaxyState galaxy, int civilizationId, string capabilityId) =>
        galaxy.Technologies.FirstOrDefault(state => state.CivilizationId == civilizationId)?
            .CompletedTechnologyIds.Contains(capabilityId) == true;
}

/// <summary>Authoritative construction prerequisites for an Adaptive Research campaign.</summary>
public sealed class AdaptiveResearchConstructionCapabilityView : IConstructionCapabilityView
{
    private readonly AdaptiveResearchCampaignState _campaign;

    public AdaptiveResearchConstructionCapabilityView(AdaptiveResearchCampaignState campaign) =>
        _campaign = campaign;

    public bool HasCivilizationCapability(GalaxyState galaxy, int civilizationId, string capabilityId)
    {
        var state = _campaign.GetCivilization(civilizationId);
        return capabilityId switch
        {
            "orbital_industry" => state.HasCapability("orbital_industry") ||
                state.HasEstablishedKnowledge("orbital_manufacturing"),
            "warp_field_control" => state.HasEstablishedKnowledge("warp_field_control"),
            _ => state.HasCapability(capabilityId),
        };
    }
}
