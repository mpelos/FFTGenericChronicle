namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeReactionCompletionTicket(
    int BattleGeneration,
    long ActionInstanceId,
    DclUnitKey Source,
    bool ReactionWindowDeclared,
    DclCanonicalNativeActionApplication Application);

internal sealed record DclCanonicalNativeReactionCompletionResult(
    DclCanonicalNativeReactionCompletionTicket Ticket,
    bool ReactionAcknowledged,
    bool Settled,
    bool Retired);

/// <summary>
/// Owns the single canonical action waiting for the native queue's terminal empty scan. Native FFT
/// executes outer actions serially, so more than one payment-committed completion ticket is an
/// integration divergence and must fail closed rather than choosing by age.
/// </summary>
internal sealed class DclCanonicalNativeReactionCompletionQueue
{
    private readonly object _gate = new();
    private DclCanonicalNativeReactionCompletionTicket? _pending;

    public int Count { get { lock (_gate) return _pending is null ? 0 : 1; } }

    public DclCanonicalNativeReactionCompletionTicket ReservePrepared(
        int battleGeneration,
        DclCanonicalNativeActionApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (battleGeneration <= 0 || application.Plan.Source.BattleGeneration != battleGeneration ||
            application.Plan.ResourceFailed || application.Stage != DclCanonicalNativeActionStage.StrikesApplied)
            throw new InvalidOperationException(
                "Only one terminal-strike canonical action from the current battle can prepare native Reaction completion.");
        var ticket = new DclCanonicalNativeReactionCompletionTicket(
            battleGeneration,
            application.Plan.ActionInstanceId,
            application.Plan.Source,
            application.Plan.ReactionWindowOpened,
            application);
        lock (_gate)
        {
            if (_pending is not null)
                throw new InvalidOperationException(
                    "A canonical ActionInstance already owns the native Reaction-completion boundary.");
            _pending = ticket;
        }
        return ticket;
    }

    public bool TryPeekSingleton(
        int battleGeneration,
        out DclCanonicalNativeReactionCompletionTicket ticket)
    {
        ticket = null!;
        if (battleGeneration <= 0) return false;
        lock (_gate)
        {
            if (_pending is null || _pending.BattleGeneration != battleGeneration)
                return false;
            ticket = _pending;
            return true;
        }
    }

    public void AcknowledgeExact(DclCanonicalNativeReactionCompletionTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        lock (_gate)
        {
            if (!ReferenceEquals(_pending, ticket))
                throw new InvalidOperationException(
                    "The completed canonical Reaction ticket is not the current exact reservation.");
            _pending = null;
        }
    }

    public void CancelExact(DclCanonicalNativeReactionCompletionTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        lock (_gate)
        {
            if (!ReferenceEquals(_pending, ticket))
                throw new InvalidOperationException(
                    "The canonical Reaction-completion ticket is not the current exact reservation.");
            _pending = null;
        }
    }

    public void Clear() { lock (_gate) _pending = null; }
}

internal static class DclCanonicalNativeReactionCompletionCoordinator
{
    public static DclCanonicalNativeReactionEffectAcknowledgement AcknowledgeEffect(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeReactionCompletionTicket ticket,
        DclUnitKey reactor,
        int nativeReactionAbilityId,
        int nativeEffectAbilityId)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(ticket);
        if (battle.BattleGeneration != ticket.BattleGeneration ||
            reactor.BattleGeneration != battle.BattleGeneration ||
            !battle.TryGetObservedUnit(reactor.UnitSlot, out DclUnitKey observedReactor) ||
            observedReactor != reactor ||
            !ReferenceEquals(battle.NativeActions.Get(ticket.ActionInstanceId), ticket.Application) ||
            ticket.Application.Stage is not (
                DclCanonicalNativeActionStage.PaymentCommitted or
                DclCanonicalNativeActionStage.ReactionOpened) ||
            !ticket.ReactionWindowDeclared)
            throw new InvalidOperationException(
                "Canonical Reaction-effect ownership became stale before the native effect completion boundary.");
        return ticket.Application.AcknowledgeReactionEffect(
            reactor,
            nativeReactionAbilityId,
            nativeEffectAbilityId);
    }

    public static DclCanonicalNativeReactionCompletionResult Commit(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeReactionCompletionTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(ticket);
        if (battle.BattleGeneration != ticket.BattleGeneration ||
            ticket.Source.BattleGeneration != battle.BattleGeneration ||
            !battle.TryGetObservedUnit(ticket.Source.UnitSlot, out DclUnitKey observedSource) ||
            observedSource != ticket.Source ||
            !ReferenceEquals(battle.NativeActions.Get(ticket.ActionInstanceId), ticket.Application) ||
            ticket.Application.Stage is not (
                DclCanonicalNativeActionStage.PaymentCommitted or
                DclCanonicalNativeActionStage.ReactionOpened) ||
            ticket.Application.Plan.ReactionWindowOpened != ticket.ReactionWindowDeclared)
            throw new InvalidOperationException(
                "Canonical Reaction-completion ownership became stale before the terminal native queue scan.");

        bool reactionAcknowledged = false;
        if (ticket.ReactionWindowDeclared)
        {
            ticket.Application.AcknowledgeReactionWindow();
            reactionAcknowledged = true;
        }
        ticket.Application.Settle();
        battle.NativeActions.Retire(ticket.ActionInstanceId);
        return new DclCanonicalNativeReactionCompletionResult(
            ticket,
            reactionAcknowledged,
            Settled: true,
            Retired: true);
    }
}
