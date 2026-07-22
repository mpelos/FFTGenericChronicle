namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativePolicySourceTicket(
    long ActionInstanceId,
    IReadOnlyList<DclCanonicalNativeUnitPolicySource> UnitPolicySources,
    object FamilyPolicySource);

internal enum DclCanonicalNativePolicyTicketPublishStatus
{
    Published,
    MissingAdmittedAction,
    DuplicatePolicyTicket,
}

internal sealed record DclCanonicalNativePolicyTicketPublishResult(
    DclCanonicalNativePolicyTicketPublishStatus Status,
    DclCanonicalNativePolicySourceTicket? Ticket);

/// <summary>
/// Battle-scoped holding area for explicit policy sources captured for a retained native
/// ActionInstance. Publication validates the sources against the retained admission but does not
/// execute, draw RNG, publish carriers, or retire the admission.
/// </summary>
internal sealed class DclCanonicalNativePolicySourceLedger
{
    private readonly object _gate = new();
    private readonly int _battleGeneration;
    private readonly Dictionary<long, DclCanonicalNativePolicySourceTicket> _tickets = [];

    public DclCanonicalNativePolicySourceLedger(int battleGeneration)
    {
        if (battleGeneration <= 0) throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        _battleGeneration = battleGeneration;
    }

    public int Count
    {
        get { lock (_gate) return _tickets.Count; }
    }

    public void Publish(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativePolicySourceTicket ticket)
    {
        DclCanonicalNativePolicyTicketPublishResult result = PublishCore(
            battle,
            ticket,
            missingAdmittedActionIsNoWrite: false,
            duplicateTicketIsNoWrite: false);
        if (result.Status != DclCanonicalNativePolicyTicketPublishStatus.Published)
            throw new InvalidOperationException($"Native policy-source ticket was not published: {result.Status}.");
    }

    public DclCanonicalNativePolicyTicketPublishResult TryPublishForRetainedAdmission(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativePolicySourceTicket ticket)
        => PublishCore(
            battle,
            ticket,
            missingAdmittedActionIsNoWrite: true,
            duplicateTicketIsNoWrite: true);

    private DclCanonicalNativePolicyTicketPublishResult PublishCore(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativePolicySourceTicket ticket,
        bool missingAdmittedActionIsNoWrite,
        bool duplicateTicketIsNoWrite)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(ticket.UnitPolicySources);
        ArgumentNullException.ThrowIfNull(ticket.FamilyPolicySource);
        if (battle.BattleGeneration != _battleGeneration)
            throw new ArgumentException("Native policy-source ticket belongs to a different battle generation.", nameof(battle));
        if (!battle.NativeAdmittedActions.TryGet(ticket.ActionInstanceId, out DclCanonicalNativeAdmittedAction action))
        {
            if (missingAdmittedActionIsNoWrite)
                return new DclCanonicalNativePolicyTicketPublishResult(
                    DclCanonicalNativePolicyTicketPublishStatus.MissingAdmittedAction,
                    Ticket: null);
            action = battle.NativeAdmittedActions.Get(ticket.ActionInstanceId);
        }
        if (action.Source.BattleGeneration != _battleGeneration ||
            action.NativeRows.Keys.Any(unit => unit.BattleGeneration != _battleGeneration))
            throw new ArgumentException("Native policy-source ticket references a stale retained admission.", nameof(ticket));

        lock (_gate)
        {
            if (_tickets.ContainsKey(ticket.ActionInstanceId))
            {
                if (duplicateTicketIsNoWrite)
                    return new DclCanonicalNativePolicyTicketPublishResult(
                        DclCanonicalNativePolicyTicketPublishStatus.DuplicatePolicyTicket,
                        Ticket: _tickets[ticket.ActionInstanceId]);
                throw new InvalidOperationException("A native policy-source ticket cannot be published twice.");
            }
        }

        DclCanonicalNativeUnitPolicyProvider.BuildForCapturedAction(action, ticket.UnitPolicySources);
        DclCanonicalNativeFamilyPolicyProvider.BuildForCapturedAction(battle, action, ticket.FamilyPolicySource);

        lock (_gate)
        {
            if (_tickets.ContainsKey(ticket.ActionInstanceId))
            {
                if (duplicateTicketIsNoWrite)
                    return new DclCanonicalNativePolicyTicketPublishResult(
                        DclCanonicalNativePolicyTicketPublishStatus.DuplicatePolicyTicket,
                        Ticket: _tickets[ticket.ActionInstanceId]);
                throw new InvalidOperationException("A native policy-source ticket cannot be published twice.");
            }
            _tickets.Add(ticket.ActionInstanceId, ticket);
        }
        return new DclCanonicalNativePolicyTicketPublishResult(
            DclCanonicalNativePolicyTicketPublishStatus.Published,
            ticket);
    }

    public bool TryGet(long actionInstanceId, out DclCanonicalNativePolicySourceTicket ticket)
    {
        lock (_gate)
            return _tickets.TryGetValue(actionInstanceId, out ticket!);
    }

    public DclCanonicalNativePolicySourceTicket Get(long actionInstanceId)
    {
        lock (_gate)
            return _tickets.TryGetValue(actionInstanceId, out DclCanonicalNativePolicySourceTicket? ticket)
                ? ticket
                : throw new KeyNotFoundException("The native policy-source ticket is not retained.");
    }

    public DclCanonicalNativePolicySourceTicket Retire(long actionInstanceId)
    {
        lock (_gate)
        {
            DclCanonicalNativePolicySourceTicket ticket = Get(actionInstanceId);
            _tickets.Remove(actionInstanceId);
            return ticket;
        }
    }

    public void Clear()
    {
        lock (_gate)
            _tickets.Clear();
    }
}
