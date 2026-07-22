namespace fftivc.generic.chronicle.codemod;

internal sealed record DclBulwarkPayload(
    int BlockModifier,
    int DrModifier,
    int DisplacementResistance,
    string PassabilityPolicy);

internal static class DclBulwarkRules
{
    public static bool Cancels(
        bool movedVoluntarily,
        bool knockedDown,
        bool stanceCancellingStun,
        bool ko)
        => movedVoluntarily || knockedDown || stanceCancellingStun || ko;
}

internal sealed record DclOverwatchPayload(
    string WeaponSlot,
    string TriggerArcOrTiles,
    string TriggerCondition,
    int RemainingTriggers,
    long ExpiresAfterSourceTurnSerial);

internal readonly record struct DclOverwatchTriggerContext(
    bool MovementEvent,
    bool MovementSettled,
    bool WeaponStillValid,
    bool SourceStillValid,
    bool TargetStillValid,
    bool RangeLegal,
    bool TrajectoryLegal,
    bool TriggerConditionSatisfied,
    bool VerticalLegal = true);

internal readonly record struct DclPreparedTriggerResult(
    bool Triggered,
    bool StateRemains,
    int RemainingUses,
    string Reason);

internal sealed class DclOverwatchState
{
    public DclOverwatchState(DclOverwatchPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (string.IsNullOrWhiteSpace(payload.WeaponSlot) ||
            string.IsNullOrWhiteSpace(payload.TriggerArcOrTiles) ||
            string.IsNullOrWhiteSpace(payload.TriggerCondition) ||
            payload.RemainingTriggers <= 0 || payload.ExpiresAfterSourceTurnSerial < 0)
            throw new ArgumentException("Overwatch requires complete weapon, trigger, use, and expiry metadata.", nameof(payload));
        Payload = payload;
        RemainingTriggers = payload.RemainingTriggers;
    }

    public DclOverwatchPayload Payload { get; }
    public int RemainingTriggers { get; private set; }
    public bool Active { get; private set; } = true;

    public DclPreparedTriggerResult TryTrigger(DclOverwatchTriggerContext context)
    {
        if (!Active) return Result(false, "overwatch-inactive");
        if (context.MovementEvent && !context.MovementSettled)
            return Result(false, "movement-not-settled-no-tile-by-tile-trigger");
        if (!context.WeaponStillValid || !context.SourceStillValid || !context.TargetStillValid ||
            !context.RangeLegal || !context.VerticalLegal || !context.TrajectoryLegal)
        {
            Active = false;
            return Result(false, "trigger-revalidation-failed-state-cancelled");
        }
        if (!context.TriggerConditionSatisfied) return Result(false, "trigger-condition-not-satisfied");
        RemainingTriggers--;
        if (RemainingTriggers == 0) Active = false;
        return Result(true, "prepared-action-fired");
    }

    public void CancelForMovementWeaponStunKnockdownKoOrExpiry() => Active = false;

    private DclPreparedTriggerResult Result(bool triggered, string reason)
        => new(triggered, Active, RemainingTriggers, reason);
}

internal sealed record DclProtectionPayload(
    DclUnitKey ProtectedUnit,
    IReadOnlySet<DclDelivery> EligibleDeliveryClasses,
    string AdjacencyOrRangeRule,
    int RemainingIntercepts,
    long ExpiresAfterSourceTurnSerial);

internal readonly record struct DclProtectionTriggerContext(
    bool SourceValid,
    bool ProtectedUnitValid,
    bool RangeOrAdjacencyLegal,
    DclDelivery Delivery,
    bool SourceCanReceiveHit);

internal sealed class DclProtectionState
{
    public DclProtectionState(DclProtectionPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (!payload.ProtectedUnit.IsValid || payload.EligibleDeliveryClasses is null ||
            payload.EligibleDeliveryClasses.Count == 0 || string.IsNullOrWhiteSpace(payload.AdjacencyOrRangeRule) ||
            payload.RemainingIntercepts <= 0 || payload.ExpiresAfterSourceTurnSerial < 0)
            throw new ArgumentException("Cover/Bodyguard requires a protected unit, delivery set, range rule, uses, and expiry.", nameof(payload));
        if (payload.EligibleDeliveryClasses.Any(delivery => delivery != DclDelivery.PhysicalAttack))
            throw new ArgumentException(
                "Cover/Bodyguard intercepts only PhysicalAttack delivery; tracked spell deliveries ignore physical cover.",
                nameof(payload));
        Payload = payload;
        RemainingIntercepts = payload.RemainingIntercepts;
    }

    public DclProtectionPayload Payload { get; }
    public int RemainingIntercepts { get; private set; }
    public bool Active { get; private set; } = true;

    public DclPreparedTriggerResult TryIntercept(DclProtectionTriggerContext context)
    {
        if (!Active) return Result(false, "protection-inactive");
        bool valid = context.SourceValid && context.ProtectedUnitValid && context.RangeOrAdjacencyLegal &&
            context.SourceCanReceiveHit && Payload.EligibleDeliveryClasses.Contains(context.Delivery);
        if (!valid)
        {
            Active = false;
            return Result(false, "protection-invalid-ended-without-redirect");
        }
        RemainingIntercepts--;
        if (RemainingIntercepts == 0) Active = false;
        return Result(true, "redirect-to-protector");
    }

    private DclPreparedTriggerResult Result(bool triggered, string reason)
        => new(triggered, Active, RemainingIntercepts, reason);
}

internal sealed class DclQuickLockController
{
    private readonly HashSet<DclUnitKey> _locked = [];
    internal IReadOnlySet<DclUnitKey> LockedUnits => new HashSet<DclUnitKey>(_locked);

    public bool IsLocked(DclUnitKey unit) => _locked.Contains(unit);

    public bool TryApplyQuick(DclUnitKey unit, DclCtState clock, DclRational authoredCtGrant)
    {
        if (!unit.IsValid) throw new ArgumentException("Quick requires a stable target identity.", nameof(unit));
        ArgumentNullException.ThrowIfNull(clock);
        if (authoredCtGrant <= DclRational.FromInteger(0)) throw new ArgumentOutOfRangeException(nameof(authoredCtGrant));
        if (_locked.Contains(unit)) return false;
        if (clock.CurrentCt + authoredCtGrant < DclRational.FromInteger(DclCtState.TurnThreshold))
            throw new ArgumentException("Quick's authored magnitude must grant enough CT for the target's next turn.", nameof(authoredCtGrant));
        clock.ApplyExplicitCtChange(authoredCtGrant);
        _locked.Add(unit);
        return true;
    }

    public bool OnGrantedTurnResolved(DclUnitKey unit) => _locked.Remove(unit);

    public bool OnUnitRemoved(DclUnitKey unit) => _locked.Remove(unit);

    internal void RestoreLocked(DclUnitKey unit)
    {
        if (!unit.IsValid) throw new ArgumentException("QuickLock restore requires a stable unit identity.", nameof(unit));
        if (!_locked.Add(unit)) throw new InvalidOperationException("QuickLock restore cannot duplicate a unit lock.");
    }

    public void ClearBattle() => _locked.Clear();
}
