using System;
using System.Collections.Generic;

namespace Game.Simulation.Diplomacy;

public sealed record DiplomaticProposalLifecycleReviewResult(
    int PendingProposalsReviewed,
    int NewlyExpiredProposals);

/// <summary>
/// Low-frequency/event-scheduled proposal lifecycle maintenance. Diplomacy owns proposal state
/// transitions, while the campaign caller owns policy for how long each proposal kind remains
/// actionable. This service therefore takes a caller-supplied default lifetime plus optional
/// per-kind overrides and delegates the authoritative transition to ExpireProposal.
/// </summary>
public sealed class DiplomaticProposalLifecycleService
{
    private readonly DiplomacyState _state;
    private readonly DiplomacySimulation _diplomacy;

    public DiplomaticProposalLifecycleService(DiplomacyState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _diplomacy = new DiplomacySimulation(_state);
    }

    public DiplomaticProposalLifecycleReviewResult Review(
        long nowTick,
        long defaultLifetimeTicks,
        IReadOnlyDictionary<DiplomaticProposalKind, long>? lifetimeByKind = null)
    {
        if (nowTick < 0)
            throw new ArgumentOutOfRangeException(nameof(nowTick));
        if (defaultLifetimeTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(defaultLifetimeTicks));

        if (lifetimeByKind is not null)
        {
            foreach (var pair in lifetimeByKind)
            {
                if (pair.Value <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(lifetimeByKind),
                        $"Proposal lifetime for {pair.Key} must be positive.");
            }
        }

        var snapshot = _state.Snapshot();
        var reviewed = 0;
        var expired = 0;

        foreach (var proposal in snapshot.Proposals)
        {
            if (proposal.Status != DiplomaticProposalStatus.Pending)
                continue;

            reviewed++;
            if (nowTick < proposal.CreatedAtTick)
                throw new InvalidOperationException("Proposal lifecycle review cannot precede proposal creation.");

            var lifetime = lifetimeByKind is not null && lifetimeByKind.TryGetValue(proposal.Kind, out var overrideLifetime)
                ? overrideLifetime
                : defaultLifetimeTicks;

            if (nowTick - proposal.CreatedAtTick < lifetime)
                continue;

            _diplomacy.ExpireProposal(proposal.ProposalId, nowTick);
            expired++;
        }

        return new DiplomaticProposalLifecycleReviewResult(reviewed, expired);
    }
}
