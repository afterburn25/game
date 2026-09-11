using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Simulation.Diplomacy;

namespace Game.Presentation;

public sealed record DiplomacyWorkspaceContact(
    string ContactId, string DisplayName, string Status, string Communication,
    double Confidence, bool Identified, int? CivilizationId, int SourceIndex,
    int? LastObservedSystemId, bool CommunicationAvailable, double? Cooperation,
    int PendingProposalCount);

public sealed record DiplomacyWorkspaceProposal(
    long ProposalId, string Direction, string Kind, string Summary, bool CanAccept,
    bool CanReject, bool CanWithdraw);

public sealed record DiplomacyWorkspaceAgreement(long AgreementId, string Type, string Status);

public enum DiplomacyContactFilter { All, Identified, Unidentified, Cooperative, Neutral, Hostile, AtWar, PendingProposal, CommunicationAvailable }

public sealed record DiplomacyWorkspaceModel(
    IReadOnlyList<DiplomacyWorkspaceContact> Contacts,
    RelationsPresentationState Selected,
    IReadOnlyList<DiplomacyWorkspaceProposal> Proposals,
    IReadOnlyList<DiplomacyWorkspaceAgreement> Agreements);

/// <summary>Observer-safe, standalone Relations workspace. It consumes only DiplomaticStateView.</summary>
public partial class DiplomacyWorkspaceView : Control
{
    public const string ContactListNode = "ContactList";
    public const string SelectedContactNode = "SelectedContact";
    public const string RelationshipPanelNode = "RelationshipPanel";
    public const string AgreementsPanelNode = "AgreementsPanel";
    public const string ProposalsPanelNode = "ProposalsPanel";

    public Action<int>? ContactSelected { get; set; }
    public Action<long>? ProposalSelected { get; set; }
    public Action<string, int?>? ActionRequested { get; set; }

    public DiplomacyWorkspaceModel? Model { get; private set; }

    public DiplomacyWorkspaceModel BuildModel(
        DiplomaticStateView view,
        int contactIndex,
        int proposalIndex,
        Func<int, string> identifiedCivilizationName)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(identifiedCivilizationName);
        var presenter = new DiplomacyRelationsPresenter();
        var selected = presenter.Build(view, contactIndex, proposalIndex, identifiedCivilizationName);
        var contacts = view.Contacts.Select((contact, sourceIndex) =>
        {
            var targetId = contact.TargetCivilizationId;
            var identified = targetId is not null;
            var name = identified ? identifiedCivilizationName(targetId!.Value) : "UNKNOWN CONTACT";
            var political = identified
                ? view.Relationships.FirstOrDefault(r => r.OtherCivilizationId == targetId!.Value)?.PoliticalState.ToString() ?? "NO FORMAL RELATIONSHIP"
                : "IDENTITY UNKNOWN";
            var relation = identified ? view.Relationships.FirstOrDefault(r => r.OtherCivilizationId == targetId!.Value) : null;
            var pending = identified ? view.Proposals.Count(p => p.Status == DiplomaticProposalStatus.Pending && PairMatches(p.ProposerCivilizationId, p.RecipientCivilizationId, view.ObserverCivilizationId, targetId!.Value)) : 0;
            return new DiplomacyWorkspaceContact(contact.ContactId, name, political,
                contact.CommunicationAvailable ? "CHANNEL AVAILABLE" : "CHANNEL UNAVAILABLE",
                Math.Clamp(contact.Confidence, 0, 1), identified, contact.TargetCivilizationId,
                sourceIndex, contact.LastObservedSystemId, contact.CommunicationAvailable && contact.Condition != ContactCondition.StaleOrLost,
                relation?.Cooperation, pending);
        }).ToArray();
        var target = selected.TargetCivilizationId;
        var proposals = target is null ? Array.Empty<DiplomacyWorkspaceProposal>() : view.Proposals
            .Where(p => p.Status == DiplomaticProposalStatus.Pending && PairMatches(p.ProposerCivilizationId, p.RecipientCivilizationId, view.ObserverCivilizationId, target.Value))
            .OrderBy(p => p.ProposalId)
            .Select((p, index) =>
            {
                var proposalState = presenter.Build(view, contactIndex, index, identifiedCivilizationName);
                return new DiplomacyWorkspaceProposal(p.ProposalId,
                    p.RecipientCivilizationId == view.ObserverCivilizationId ? "INCOMING" : "OUTGOING",
                    p.Kind.ToString(), p.Summary, proposalState.CanAcceptProposal,
                    proposalState.CanRejectProposal, proposalState.CanWithdrawProposal);
            }).ToArray();
        var agreements = target is null ? Array.Empty<DiplomacyWorkspaceAgreement>() : view.Agreements
            .Where(a => PairMatches(a.CivilizationAId, a.CivilizationBId, view.ObserverCivilizationId, target.Value))
            .OrderBy(a => a.AgreementId)
            .Select(a => new DiplomacyWorkspaceAgreement(a.AgreementId, a.Type.ToString(), a.Status.ToString())).ToArray();
        return Model = new DiplomacyWorkspaceModel(contacts, selected, proposals, agreements);
    }

    public static IReadOnlyList<DiplomacyWorkspaceContact> FilterContacts(
        DiplomacyWorkspaceModel model, DiplomacyContactFilter filter)
    {
        return model.Contacts.Where(contact => filter switch
        {
            DiplomacyContactFilter.Identified => contact.Identified,
            DiplomacyContactFilter.Unidentified => !contact.Identified,
            DiplomacyContactFilter.Cooperative => contact.Cooperation is >= 0.5,
            DiplomacyContactFilter.Neutral => contact.Status is "NO FORMAL RELATIONSHIP" or "Unknown",
            DiplomacyContactFilter.Hostile => contact.Status == "Hostile",
            DiplomacyContactFilter.AtWar => contact.Status == "AtWar",
            DiplomacyContactFilter.PendingProposal => contact.PendingProposalCount > 0,
            DiplomacyContactFilter.CommunicationAvailable => contact.CommunicationAvailable,
            _ => true,
        }).ToArray();
    }

    public override void _Ready() => BuildPremiumLayout();

    partial void BuildPremiumLayout();

    private void BuildLayout()
    {
        Name = "DiplomacyWorkspaceView";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var root = new VBoxContainer { Name = "WorkspaceRoot", SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(root);
        root.AddChild(new Label { Name = "WorkspaceTitle", Text = "RELATIONS", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var columns = new HBoxContainer { Name = "WorkspaceColumns", SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(columns);
        columns.AddChild(ScrollablePanel(ContactListNode, "CONTACTS", Control.SizeFlags.ShrinkBegin, 220));
        columns.AddChild(ScrollablePanel(SelectedContactNode, "SELECTED CONTACT", Control.SizeFlags.ExpandFill, 420));
        columns.AddChild(ScrollablePanel(RelationshipPanelNode, "RELATIONSHIP", Control.SizeFlags.ShrinkEnd, 260));
        var lower = new HBoxContainer { Name = "WorkspaceLowerPanels", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddChild(lower);
        lower.AddChild(ScrollablePanel(AgreementsPanelNode, "AGREEMENTS", Control.SizeFlags.ExpandFill, 0));
        lower.AddChild(ScrollablePanel(ProposalsPanelNode, "PROPOSALS", Control.SizeFlags.ExpandFill, 0));
    }

    private static Control ScrollablePanel(string name, string title, Control.SizeFlags flags, int width)
    {
        var panel = new PanelContainer { Name = name, SizeFlagsHorizontal = flags };
        if (width > 0) panel.CustomMinimumSize = new Vector2(width, 0);
        var scroll = new ScrollContainer { Name = name + "Scroll", SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var body = new VBoxContainer { Name = name + "Body", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddChild(new Label { Text = title, AutowrapMode = TextServer.AutowrapMode.WordSmart });
        scroll.AddChild(body); panel.AddChild(scroll); return panel;
    }

    private static bool PairMatches(int a, int b, int observer, int target) => (a == observer && b == target) || (a == target && b == observer);
}
