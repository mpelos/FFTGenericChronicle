namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativePostApplyTicket(
    long ActionInstanceId,
    DclUnitKey Source,
    DclUnitKey Target,
    int StrikeIndex,
    DclCanonicalNativePoolSnapshot TargetBefore,
    DclCanonicalNativePoolSnapshot TargetAfter,
    DclCanonicalNativeActionApplication Application);

internal sealed record DclCanonicalNativePostApplyCommitResult(
    DclCanonicalNativePostApplyTicket Ticket,
    DclCanonicalNativeSourceEffectCommitResult? SourceEffect,
    DclCanonicalNativePaymentCommitResult Payment,
    DclCanonicalNativePaymentReservation PaymentReservation);

/// <summary>
/// Correlates the final canonical target/Strike staged at pre-clamp with the same target after the
/// native state-apply routine has committed HP/MP and lifecycle state. Only the final Strike of an
/// outer ActionInstance receives a ticket; earlier target/Strike applies cannot advance source
/// effects or payment.
/// </summary>
internal sealed class DclCanonicalNativePostApplyQueue
{
    private readonly object _gate = new();
    private readonly Dictionary<(int BattleGeneration, int TargetSlot), DclCanonicalNativePostApplyTicket> _tickets = [];

    public int Count { get { lock (_gate) return _tickets.Count; } }

    public bool TryReserveTerminal(
        DclCanonicalNativeApplyReservation reservation,
        DclCanonicalNativePoolSnapshot targetBefore,
        out DclCanonicalNativePostApplyTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ticket = null!;
        bool alreadyCommitted = reservation.Application.Stage == DclCanonicalNativeActionStage.StrikesApplied;
        bool pendingTerminal = reservation.Application.Stage is
                DclCanonicalNativeActionStage.Published or DclCanonicalNativeActionStage.ApplyingStrikes &&
            reservation.Application.AppliedStrikeCount + 1 == reservation.Application.Plan.Strikes.Count &&
            reservation.Application.PeekNextStrike() == reservation.Strike;
        if (!alreadyCommitted && !pendingTerminal) return false;
        if (reservation.Application.Plan.ActionInstanceId != reservation.ActionInstanceId ||
            reservation.Application.Plan.Source != reservation.Source ||
            reservation.Strike.Target != reservation.Target ||
            reservation.Strike.StrikeIndex != reservation.StrikeIndex)
            throw new InvalidOperationException("The terminal native apply reservation lost its canonical identity.");

        DclCanonicalNativeNumericChannels channels = reservation.Strike.Channels;
        int nextHp = Math.Clamp(
            checked(targetBefore.CurrentHp - channels.HpDebit + channels.HpCredit),
            0,
            targetBefore.MaxHp);
        int nextMp = Math.Clamp(
            checked(targetBefore.CurrentMp - channels.MpDebit + channels.MpCredit),
            0,
            targetBefore.MaxMp);
        var targetAfter = new DclCanonicalNativePoolSnapshot(
            nextHp,
            targetBefore.MaxHp,
            nextMp,
            targetBefore.MaxMp);
        if (reservation.Strike.TargetKoAfterStrike != (targetAfter.CurrentHp == 0))
            throw new InvalidOperationException(
                "The canonical target KO projection disagrees with the exact post-apply HP pool.");

        ticket = new DclCanonicalNativePostApplyTicket(
            reservation.ActionInstanceId,
            reservation.Source,
            reservation.Target,
            reservation.StrikeIndex,
            targetBefore,
            targetAfter,
            reservation.Application);
        var key = (reservation.Target.BattleGeneration, reservation.Target.UnitSlot);
        lock (_gate)
        {
            if (_tickets.ContainsKey(key))
                throw new InvalidOperationException(
                    "A target slot already owns an unresolved canonical post-apply ticket.");
            _tickets.Add(key, ticket);
        }
        return true;
    }

    public bool TryTakeExact(
        int battleGeneration,
        DclUnitKey observedTarget,
        DclCanonicalNativePoolSnapshot observedPools,
        out DclCanonicalNativePostApplyTicket ticket)
    {
        ticket = null!;
        if (battleGeneration <= 0 || !observedTarget.IsValid ||
            observedTarget.BattleGeneration != battleGeneration)
            return false;
        var key = (battleGeneration, observedTarget.UnitSlot);
        lock (_gate)
        {
            if (!_tickets.Remove(key, out DclCanonicalNativePostApplyTicket? pending))
                return false;
            ticket = pending;
        }
        if (ticket.Target != observedTarget || ticket.TargetAfter != observedPools ||
            ticket.Application.Plan.ActionInstanceId != ticket.ActionInstanceId ||
            ticket.Application.Stage != DclCanonicalNativeActionStage.StrikesApplied)
            throw new InvalidOperationException(
                "Native post-apply readback does not match the terminal canonical target/Strike ticket.");
        return true;
    }

    public void CancelExact(DclCanonicalNativePostApplyTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var key = (ticket.Target.BattleGeneration, ticket.Target.UnitSlot);
        lock (_gate)
        {
            if (!_tickets.TryGetValue(key, out DclCanonicalNativePostApplyTicket? pending) ||
                !ReferenceEquals(pending, ticket))
                throw new InvalidOperationException("The canonical post-apply ticket is not the current exact reservation.");
            _tickets.Remove(key);
        }
    }

    public void Clear() { lock (_gate) _tickets.Clear(); }
}

/// <summary>
/// Completes the source-owned portion of one outer action after exact native target readback. The
/// target result is already durable here; Drain/source ResourceChange commits before the separate
/// MP/HP action payment. Reaction acknowledgement and presentation settlement remain later native
/// boundaries and are deliberately not advanced by this coordinator.
/// </summary>
internal static class DclCanonicalNativePostApplyCoordinator
{
    public static DclCanonicalNativePostApplyCommitResult CommitSourceEffectAndPayment(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativePostApplyTicket ticket,
        Func<DclCanonicalNativePoolSnapshot> readSourcePools,
        Action<int> writeCurrentMp,
        Action<int> writeCurrentHp)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(readSourcePools);
        ArgumentNullException.ThrowIfNull(writeCurrentMp);
        ArgumentNullException.ThrowIfNull(writeCurrentHp);
        if (battle.BattleGeneration != ticket.Source.BattleGeneration ||
            !battle.TryGetObservedUnit(ticket.Source.UnitSlot, out DclUnitKey observedSource) ||
            observedSource != ticket.Source ||
            !ReferenceEquals(battle.NativeActions.Get(ticket.ActionInstanceId), ticket.Application) ||
            ticket.Application.Stage != DclCanonicalNativeActionStage.StrikesApplied)
            throw new InvalidOperationException(
                "Canonical post-apply source/payment ownership became stale before commit.");

        DclCanonicalNativeSourceEffectCommitResult? sourceCommit = null;
        if (ticket.Application.Plan.SourceEffect is not null)
        {
            if (!DclCanonicalNativeApplyRouter.TryPrepareSourceEffect(
                    battle,
                    ticket.ActionInstanceId,
                    out DclCanonicalNativeSourceEffectReservation sourceReservation))
                throw new InvalidOperationException("The terminal action did not expose its declared source effect.");
            DclCanonicalNativeSourceEffectApplyPlan sourcePlan =
                DclCanonicalNativeApplyRouter.PlanSourceEffect(battle, sourceReservation, readSourcePools());
            DclCanonicalNativeSourceEffectApplyPlanner.ValidateKoCleanup(sourcePlan, battle.States);
            DclCanonicalNativeSourceEffectWriter.Apply(
                sourcePlan,
                readSourcePools,
                writeCurrentMp,
                writeCurrentHp);
            sourceCommit = DclCanonicalNativeApplyRouter.CommitSourceEffect(
                battle,
                sourceReservation,
                sourcePlan);
        }

        if (!DclCanonicalNativeApplyRouter.TryPreparePayment(
                battle,
                ticket.ActionInstanceId,
                out DclCanonicalNativePaymentReservation paymentReservation))
            throw new InvalidOperationException("The terminal action did not expose its resource-payment carrier.");
        DclCanonicalNativePaymentApplyPlan paymentPlan = DclCanonicalNativeApplyRouter.PlanPayment(
            battle,
            paymentReservation,
            ticket.Source,
            readSourcePools());
        DclCanonicalNativePaymentApplyPlanner.ValidatePayerKoCleanup(paymentPlan, battle.States);
        DclCanonicalNativePaymentWriter.Apply(
            paymentPlan,
            readSourcePools,
            writeCurrentMp,
            writeCurrentHp);
        DclCanonicalNativePaymentCommitResult paymentCommit = DclCanonicalNativeApplyRouter.CommitPayment(
            battle,
            paymentReservation,
            paymentPlan);
        return new DclCanonicalNativePostApplyCommitResult(
            ticket,
            sourceCommit,
            paymentCommit,
            paymentReservation);
    }
}
