using Game.Presentation;
using Game.Simulation.Diplomacy;

static class Program
{
    static int Main()
    {
        var tests = new (string, Action)[]
        {
            ("unknown contacts remain anonymous", UnknownContactsRemainAnonymous),
            ("unknown contacts expose no relationships", UnknownContactsExposeNoRelationships),
            ("selected proposal flags follow observer direction", ProposalDirectionIsObserverSafe),
        };
        foreach (var (name, test) in tests)
        {
            test(); Console.WriteLine($"PASS {name}");
        }
        Console.WriteLine($"{tests.Length} diplomacy workspace checks passed");
        return 0;
    }

    static void UnknownContactsRemainAnonymous()
    {
        var state = View(new DiplomaticContactView("contact-7", null, ContactAwareness.ContactEstablished,
            ContactCondition.Active, false, .42, 4, 12));
        var result = new DiplomacyRelationsPresenter().Build(state, 0, 0, _ => throw new Exception("hidden name requested"));
        Require(result.ContactName == "UNIDENTIFIED CONTACT contact-7", result.ContactName);
        Require(result.TargetCivilizationId is null && result.Trust is null, "hidden target data leaked");
    }

    static void UnknownContactsExposeNoRelationships()
    {
        var state = View(new DiplomaticContactView("contact-8", null, ContactAwareness.DetectedUnidentified,
            ContactCondition.Active, false, .11, 2, null), new DiplomaticRelationshipView(99,
            DiplomaticPoliticalState.AtWar, 1, 1, 1, 1, 1, Array.Empty<DiplomaticGrievanceSnapshot>()));
        var result = new DiplomacyRelationsPresenter().Build(state, 0, 0, _ => "SECRET");
        Require(result.Trust is null && result.PoliticalStatus != "AtWar", "unpaired relationship leaked");
    }

    static void ProposalDirectionIsObserverSafe()
    {
        var proposal = new DiplomaticProposalSnapshot(1, 2, 7, DiplomaticProposalKind.Agreement,
            DiplomaticAgreementType.NonAggression, DiplomaticProposalStatus.Pending, 1, null, "request", null);
        var state = View(new DiplomaticContactView("known", 2, ContactAwareness.Identified,
            ContactCondition.Active, true, 1, 3, null), proposals: new[] { proposal });
        var result = new DiplomacyRelationsPresenter().Build(state, 0, 0, _ => "Known");
        Require(result.PendingProposalId == 1 && result.CanAcceptProposal, "incoming proposal not exposed as accept");
        Require(!result.CanWithdrawProposal, "incoming proposal exposed as withdraw");
    }

    static DiplomaticStateView View(DiplomaticContactView contact,
        DiplomaticRelationshipView? relationship = null,
        IReadOnlyList<DiplomaticProposalSnapshot>? proposals = null) =>
        new(7, new[] { contact }, relationship is null ? Array.Empty<DiplomaticRelationshipView>() : new[] { relationship },
            Array.Empty<DiplomaticAccessSnapshot>(), Array.Empty<TerritorialClaimSnapshot>(), Array.Empty<TerritorialClaimResponseSnapshot>(),
            Array.Empty<DiplomaticAgreementSnapshot>(), proposals ?? Array.Empty<DiplomaticProposalSnapshot>(), Array.Empty<DiplomaticHistoryEventSnapshot>());

    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
