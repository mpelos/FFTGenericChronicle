using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal enum DclCanonicalTimelineStepKind
{
    ChargedAction,
    StateSchedule,
    TurnGrant,
}

internal sealed record DclCanonicalScheduledCast(
    int AbilityId,
    DclActionDeclaration Declaration);

internal enum DclChargingCancellationReason
{
    Voluntary,
    Ko,
    Stun,
    KnockedDown,
    Silence,
    DontAct,
    ConcentrationFailure,
    ExplicitEffect,
    SourceLost,
}

internal sealed record DclCanonicalChargedCancellation(
    DclCanonicalScheduledCast ChargedAction,
    DclChargingCancellationReason Reason);

internal sealed record DclCanonicalTimelineStep(
    DclCanonicalTimelineStepKind Kind,
    long GlobalCt,
    DclCanonicalScheduledCast? ChargedAction,
    DclCanonicalGlobalScheduleStep? StateSchedule,
    DclUnitKey? TurnUnit);

internal sealed record DclCanonicalTimelineTurnCompletion(
    DclCanonicalTurnCompletion Turn,
    DclCanonicalQuickUnlockResult? QuickUnlock);

internal sealed class DclCanonicalTimelineUnit
{
    public DclCanonicalTimelineUnit(DclUnitKey unit, int initiativeRank, DclCtState clock, DclCtRate rate)
    {
        if (!unit.IsValid) throw new ArgumentException("Timeline unit requires a stable UnitKey.", nameof(unit));
        if (initiativeRank < 0) throw new ArgumentOutOfRangeException(nameof(initiativeRank));
        ArgumentNullException.ThrowIfNull(clock);
        Unit = unit;
        InitiativeRank = initiativeRank;
        Clock = clock;
        Rate = rate;
    }

    public DclUnitKey Unit { get; }
    public int InitiativeRank { get; }
    public DclCtState Clock { get; }
    public DclCtRate Rate { get; internal set; }
}

/// <summary>
/// Owns canonical GlobalCT ordering above the battle-local state, action, and turn controllers.
/// The scheduler reserves exactly one next step. A caller must commit that exact step before time
/// can advance again, so a charged action or periodic tick cannot be overtaken by expiry or a turn.
/// </summary>
internal sealed class DclCanonicalTimelineScheduler
{
    private readonly object _gate = new();
    private readonly DclCanonicalBattleRuntime _battle;
    private readonly DclQuickLockController? _quickLocks;
    private readonly string? _quickLockStateKind;
    private readonly Dictionary<DclUnitKey, DclCanonicalTimelineUnit> _units = [];
    private readonly Dictionary<long, DclCanonicalScheduledCast> _chargedActions = [];
    private DclCanonicalTimelineStep? _pendingStep;
    private DclUnitKey? _activeTurn;

    public DclCanonicalTimelineScheduler(
        DclCanonicalBattleRuntime battle,
        IEnumerable<DclCanonicalTimelineUnit> units,
        DclQuickLockController? quickLocks = null,
        string? quickLockStateKind = null)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(units);
        _battle = battle;
        if ((quickLocks is null) != string.IsNullOrWhiteSpace(quickLockStateKind))
            throw new ArgumentException("QuickLock controller and state kind must be configured together.");
        if (quickLockStateKind is not null && !battle.Catalog.Authoring.States.ContainsKey(quickLockStateKind))
            throw new ArgumentException("The timeline QuickLock state kind is not loaded in canonical authoring.", nameof(quickLockStateKind));
        _quickLocks = quickLocks;
        _quickLockStateKind = quickLockStateKind;
        foreach (DclCanonicalTimelineUnit unit in units)
        {
            if (unit.Unit.BattleGeneration != battle.BattleGeneration ||
                !battle.TryGetObservedUnit(unit.Unit.UnitSlot, out DclUnitKey observed) || observed != unit.Unit)
                throw new ArgumentException("Every timeline unit must be currently observed in this battle.", nameof(units));
            if (!_units.TryAdd(unit.Unit, unit))
                throw new ArgumentException("Timeline units must have unique UnitKeys.", nameof(units));
        }
        if (_units.Values.Select(unit => unit.InitiativeRank).Distinct().Count() != _units.Count)
            throw new ArgumentException("Timeline initiative ranks must be unique.", nameof(units));
        _battle.AttachTimeline(this);
    }

    public long CurrentGlobalCt => _battle.CurrentGlobalCt;
    internal DclCanonicalBattleRuntime Battle => _battle;
    public DclUnitKey? ActiveTurn { get { lock (_gate) return _activeTurn; } }
    public IReadOnlyList<DclCanonicalScheduledCast> ChargedActions
    {
        get { lock (_gate) return _chargedActions.Values.OrderBy(entry => entry.Declaration.ActionInstanceId).ToArray(); }
    }
    internal IReadOnlyList<DclCanonicalTimelineUnit> TimelineUnits
    {
        get { lock (_gate) return _units.Values.OrderBy(unit => unit.InitiativeRank).ToArray(); }
    }
    internal string? QuickLockStateKind => _quickLockStateKind;
    internal DclQuickLockController? QuickLocks => _quickLocks;

    public bool IsCharging(DclUnitKey source)
    {
        lock (_gate)
            return _chargedActions.Values.Any(entry => entry.Declaration.Source == source);
    }

    internal void RequireCheckpointBoundary()
    {
        lock (_gate)
        {
            if (_pendingStep is not null || _activeTurn is not null || _battle.NativeActions.Count != 0 ||
                _battle.PendingPreparedActionCount != 0 ||
                _battle.NativeAdmissions.HasActiveRepeat)
                throw new InvalidOperationException(
                    "A timeline checkpoint requires a settled between-event, between-turn boundary with no native action in flight.");
        }
    }

    public void SetCtRate(DclUnitKey unit, DclCtRate rate)
    {
        lock (_gate)
        {
            if (!_units.TryGetValue(unit, out DclCanonicalTimelineUnit? entry))
                throw new KeyNotFoundException("The unit is not registered in the canonical timeline.");
            entry.Rate = rate;
        }
    }

    public void ScheduleChargedAction(int abilityId, DclActionDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        lock (_gate)
        {
            (DclAbilityBinding binding, DclActionProfile profile) = _battle.Catalog.ResolveAbility(abilityId);
            _ = new DclActionTransaction(declaration, profile);
            if (declaration.Source.BattleGeneration != _battle.BattleGeneration ||
                !_units.ContainsKey(declaration.Source) || declaration.ActionId != binding.ActionId ||
                declaration.ProfileRevision != binding.ProfileRevision || declaration.ActionId != profile.ActionId)
                throw new ArgumentException("Charged declaration identity does not match this timeline or ability binding.", nameof(declaration));
            if (declaration.CastCt <= 0 || declaration.DeclaredAtGlobalCt != CurrentGlobalCt ||
                declaration.ResolvesAtGlobalCt != checked(CurrentGlobalCt + declaration.CastCt))
                throw new ArgumentException("Only a newly declared positive-CastCT action can enter the timeline.", nameof(declaration));
            if (declaration.TrackedTarget is { } tracked &&
                (!_battle.TryGetObservedUnit(tracked.UnitSlot, out DclUnitKey observed) || observed != tracked))
                throw new ArgumentException("A tracked charged target must be currently observed at declaration.", nameof(declaration));
            if (_chargedActions.ContainsKey(declaration.ActionInstanceId) ||
                _chargedActions.Values.Any(entry => entry.Declaration.Source == declaration.Source))
                throw new InvalidOperationException("An ActionInstance or Charging source cannot be scheduled twice.");
            _chargedActions.Add(declaration.ActionInstanceId, new DclCanonicalScheduledCast(abilityId, declaration));
        }
    }

    internal void RestoreChargedAction(DclCanonicalScheduledCast charged)
    {
        ArgumentNullException.ThrowIfNull(charged);
        ArgumentNullException.ThrowIfNull(charged.Declaration);
        lock (_gate)
        {
            DclActionDeclaration declaration = charged.Declaration;
            (DclAbilityBinding binding, DclActionProfile profile) = _battle.Catalog.ResolveAbility(charged.AbilityId);
            _ = new DclActionTransaction(declaration, profile);
            if (declaration.Source.BattleGeneration != _battle.BattleGeneration ||
                !_units.ContainsKey(declaration.Source) || declaration.ActionId != binding.ActionId ||
                declaration.ProfileRevision != binding.ProfileRevision || declaration.ActionId != profile.ActionId ||
                declaration.CastCt <= 0 || declaration.DeclaredAtGlobalCt < 0 ||
                declaration.DeclaredAtGlobalCt > CurrentGlobalCt ||
                declaration.ResolvesAtGlobalCt <= CurrentGlobalCt ||
                declaration.ResolvesAtGlobalCt != checked(declaration.DeclaredAtGlobalCt + declaration.CastCt))
                throw new InvalidOperationException("Saved charged declaration is incompatible with the restored timeline or authoring.");
            if (declaration.TrackedTarget is { } tracked &&
                (!_battle.TryGetObservedUnit(tracked.UnitSlot, out DclUnitKey observed) || observed != tracked))
                throw new InvalidOperationException("Saved tracked target is absent from the restored battle.");
            if (_chargedActions.ContainsKey(declaration.ActionInstanceId) ||
                _chargedActions.Values.Any(entry => entry.Declaration.Source == declaration.Source))
                throw new InvalidOperationException("Saved timeline duplicates an ActionInstance or Charging source.");
            _chargedActions.Add(declaration.ActionInstanceId, charged);
        }
    }

    public DclCanonicalScheduledCast CancelChargedAction(long actionInstanceId)
    {
        lock (_gate)
        {
            if (_pendingStep?.ChargedAction is { } pending &&
                pending.Declaration.ActionInstanceId == actionInstanceId)
                throw new InvalidOperationException("A charged action already reserved for resolution cannot be cancelled out from under its step.");
            if (!_chargedActions.Remove(actionInstanceId, out DclCanonicalScheduledCast? removed))
                throw new KeyNotFoundException($"Charged ActionInstance {actionInstanceId} is not scheduled.");
            return removed;
        }
    }

    public bool TryGetChargedAction(DclUnitKey source, out DclCanonicalScheduledCast chargedAction)
    {
        lock (_gate)
        {
            chargedAction = _chargedActions.Values.SingleOrDefault(entry => entry.Declaration.Source == source)!;
            return chargedAction is not null;
        }
    }

    public DclCanonicalChargedCancellation? CancelChargedActionForSource(
        DclUnitKey source,
        DclChargingCancellationReason reason)
    {
        lock (_gate)
        {
            DclCanonicalScheduledCast? charged = _chargedActions.Values
                .SingleOrDefault(entry => entry.Declaration.Source == source);
            if (charged is null) return null;
            if (_pendingStep?.ChargedAction == charged)
                throw new InvalidOperationException("A charged action already reserved for delivery cannot be cancelled.");
            _chargedActions.Remove(charged.Declaration.ActionInstanceId);
            return new DclCanonicalChargedCancellation(charged, reason);
        }
    }

    public DclCanonicalChargedCancellation? ResolveInjuryConcentration(
        DclUnitKey source,
        DclInjuryConsequenceResult consequences)
    {
        lock (_gate)
        {
            bool charging = _chargedActions.Values.Any(entry => entry.Declaration.Source == source);
            if (!charging)
            {
                if (consequences.Concentration.Outcome != DclConcentrationOutcome.NoIncident)
                    throw new InvalidOperationException(
                        "An Injury result claims a Charging incident for a source absent from the timeline.");
                return null;
            }
            DclChargingCancellationReason? reason = consequences switch
            {
                { MajorWound.RemainingHp: 0 } => DclChargingCancellationReason.Ko,
                { ApplyStun: true } => DclChargingCancellationReason.Stun,
                { ApplyKnockedDown: true } => DclChargingCancellationReason.KnockedDown,
                _ => null,
            };
            if (reason is not null)
                return CancelChargedActionForSource(source, reason.Value) ??
                    throw new InvalidOperationException("The Charging entry disappeared during Injury cancellation.");
            return ResolveConcentrationIncident(source, consequences.Concentration);
        }
    }

    public DclCanonicalChargedCancellation? ResolveConcentrationIncident(
        DclUnitKey source,
        DclConcentrationResult concentration)
    {
        lock (_gate)
        {
            bool charging = _chargedActions.Values.Any(entry => entry.Declaration.Source == source);
            if (!charging)
            {
                if (concentration.Outcome != DclConcentrationOutcome.NoIncident)
                    throw new InvalidOperationException(
                        "A concentration result claims a Charging incident for a source absent from the timeline.");
                return null;
            }
            DclChargingCancellationReason? reason = concentration.Outcome switch
            {
                DclConcentrationOutcome.NoIncident => null,
                DclConcentrationOutcome.Preserved => null,
                DclConcentrationOutcome.DirectCancellation => DclChargingCancellationReason.ExplicitEffect,
                DclConcentrationOutcome.Interrupted => DclChargingCancellationReason.ConcentrationFailure,
                _ => throw new InvalidOperationException("The concentration outcome is not executable."),
            };
            if (reason is null) return null;
            return CancelChargedActionForSource(source, reason.Value) ??
                throw new InvalidOperationException("The Charging entry disappeared during concentration cancellation.");
        }
    }

    public DclCanonicalTimelineStep? BeginNextStepThrough(long globalCt)
    {
        lock (_gate)
        {
            if (globalCt < CurrentGlobalCt) throw new ArgumentOutOfRangeException(nameof(globalCt));
            if (_battle.PendingPreparedActionCount != 0)
                throw new InvalidOperationException(
                    "GlobalCT cannot advance while final-tile prepared Actions remain unresolved.");
            if (_activeTurn is not null)
                throw new InvalidOperationException("GlobalCT cannot advance while a granted unit turn remains active.");
            if (_pendingStep is { } pending)
            {
                if (globalCt < pending.GlobalCt) throw new ArgumentOutOfRangeException(nameof(globalCt));
                return pending;
            }

            long? castCt = _chargedActions.Values
                .Select(entry => entry.Declaration.ResolvesAtGlobalCt)
                .Where(ct => ct <= globalCt)
                .Cast<long?>()
                .Min();
            DclStateScheduleEvent? stateEvent = _battle.States.PeekNextGlobalScheduleEventThrough(globalCt);
            long? turnCt = NextTurnEligibilityGlobalCt(globalCt);
            long? nextCt = new long?[] { castCt, stateEvent?.GlobalCt, turnCt }
                .Where(value => value is not null)
                .Min();
            if (nextCt is null)
            {
                AdvanceTo(globalCt);
                return null;
            }

            AdvanceTo(nextCt.Value);
            DclCanonicalScheduledCast? charged = _chargedActions.Values
                .Where(entry => entry.Declaration.ResolvesAtGlobalCt == nextCt.Value)
                .OrderBy(entry => entry.Declaration.ActionInstanceId)
                .FirstOrDefault();
            if (charged is not null)
            {
                _pendingStep = new DclCanonicalTimelineStep(
                    DclCanonicalTimelineStepKind.ChargedAction,
                    nextCt.Value,
                    charged,
                    StateSchedule: null,
                    TurnUnit: null);
                return _pendingStep;
            }

            if (_battle.States.PeekNextGlobalScheduleEventThrough(nextCt.Value) is { GlobalCt: var dueCt } &&
                dueCt == nextCt.Value)
            {
                DclCanonicalGlobalScheduleStep state = _battle.BeginNextGlobalScheduleStepThrough(nextCt.Value) ??
                    throw new InvalidOperationException("The selected state event disappeared before reservation.");
                _pendingStep = new DclCanonicalTimelineStep(
                    DclCanonicalTimelineStepKind.StateSchedule,
                    nextCt.Value,
                    ChargedAction: null,
                    state,
                    TurnUnit: null);
                return _pendingStep;
            }

            DclCanonicalTimelineUnit turn = SelectEligibleTurn();
            _pendingStep = new DclCanonicalTimelineStep(
                DclCanonicalTimelineStepKind.TurnGrant,
                nextCt.Value,
                ChargedAction: null,
                StateSchedule: null,
                turn.Unit);
            return _pendingStep;
        }
    }

    public void CommitChargedAction(
        DclCanonicalTimelineStep step,
        DclCanonicalNativeActionApplication settledApplication)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(settledApplication);
        lock (_gate)
        {
            RequirePending(step, DclCanonicalTimelineStepKind.ChargedAction);
            DclCanonicalScheduledCast charged = step.ChargedAction!;
            DclActionDeclaration declaration = charged.Declaration;
            if (settledApplication.Stage != DclCanonicalNativeActionStage.Settled ||
                !ReferenceEquals(_battle.NativeActions.Get(declaration.ActionInstanceId), settledApplication) ||
                settledApplication.Plan.ActionInstanceId != declaration.ActionInstanceId ||
                settledApplication.Plan.AbilityId != charged.AbilityId ||
                settledApplication.Plan.ActionId != declaration.ActionId ||
                settledApplication.Plan.ProfileRevision != declaration.ProfileRevision ||
                settledApplication.Plan.Source != declaration.Source)
                throw new InvalidOperationException("A charged action completes only after its exact declared outer ActionInstance settles.");
            _battle.NativeActions.Retire(declaration.ActionInstanceId);
            _chargedActions.Remove(declaration.ActionInstanceId);
            _pendingStep = null;
        }
    }

    public DclStateScheduleCommit CommitStateSchedule(
        DclCanonicalTimelineStep step,
        DclCanonicalNativeActionApplication? settledTickApplication = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        lock (_gate)
        {
            RequirePending(step, DclCanonicalTimelineStepKind.StateSchedule);
            DclStateScheduleCommit result =
                _battle.CommitGlobalScheduleStep(step.StateSchedule!, settledTickApplication);
            _pendingStep = null;
            return result;
        }
    }

    public long CommitTurnGrant(DclCanonicalTimelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        lock (_gate)
        {
            RequirePending(step, DclCanonicalTimelineStepKind.TurnGrant);
            DclUnitKey unit = step.TurnUnit!.Value;
            DclCanonicalTimelineUnit entry = _units[unit];
            if (!entry.Clock.IsTurnEligible)
                throw new InvalidOperationException("The reserved turn is no longer CT-eligible.");
            entry.Clock.GrantTurnAndReset();
            long serial = _battle.BeginTurn(unit);
            _activeTurn = unit;
            _pendingStep = null;
            return serial;
        }
    }

    public DclCanonicalTimelineTurnCompletion CompleteActiveTurn(DclUnitKey unit)
    {
        lock (_gate)
        {
            if (_activeTurn != unit)
                throw new InvalidOperationException("Only the timeline's active unit can complete a turn.");
            DclCanonicalQuickUnlockReservation? quickUnlockReservation = _quickLocks is null
                ? null
                : DclCanonicalQuickExecutor.PrepareGrantedTurnCompletion(
                    unit,
                    _quickLockStateKind!,
                    _quickLocks,
                    _battle.States);
            DclCanonicalTurnCompletion completion = _battle.CompleteTurn(unit);
            DclCanonicalQuickUnlockResult? quickUnlock = quickUnlockReservation is null
                ? null
                : DclCanonicalQuickExecutor.CommitGrantedTurnCompletion(
                    quickUnlockReservation.Value,
                    _quickLocks!,
                    _battle.States);
            _activeTurn = null;
            return new DclCanonicalTimelineTurnCompletion(completion, quickUnlock);
        }
    }

    private void AdvanceTo(long globalCt)
    {
        long delta = checked(globalCt - CurrentGlobalCt);
        if (delta == 0) return;
        _battle.States.AdvanceGlobalClockTo(globalCt);
        foreach (DclCanonicalTimelineUnit unit in _units.Values)
            unit.Clock.AdvanceTicks(unit.Rate, delta);
    }

    private long? NextTurnEligibilityGlobalCt(long throughGlobalCt)
    {
        long? earliest = null;
        foreach (DclCanonicalTimelineUnit unit in _units.Values)
        {
            long? ticks = TicksUntilEligible(unit);
            if (ticks is null) continue;
            long due = checked(CurrentGlobalCt + ticks.Value);
            if (due > throughGlobalCt) continue;
            earliest = earliest is null ? due : Math.Min(earliest.Value, due);
        }
        return earliest;
    }

    private static long? TicksUntilEligible(DclCanonicalTimelineUnit unit)
    {
        DclRational threshold = DclRational.FromInteger(DclCtState.TurnThreshold);
        if (unit.Clock.CurrentCt >= threshold) return 0;
        DclRational gain = unit.Rate switch
        {
            DclCtRate.Stopped => DclRational.FromInteger(0),
            DclCtRate.Slow => new DclRational(15, 2),
            DclCtRate.Normal => DclRational.FromInteger(10),
            DclCtRate.Haste => DclRational.FromInteger(15),
            _ => throw new ArgumentOutOfRangeException(nameof(unit.Rate)),
        };
        if (gain == DclRational.FromInteger(0)) return null;
        BigInteger ticks = ((threshold - unit.Clock.CurrentCt) / gain).Ceiling();
        return checked((long)ticks);
    }

    private DclCanonicalTimelineUnit SelectEligibleTurn()
        => _units.Values
            .Where(unit => unit.Clock.IsTurnEligible)
            .OrderByDescending(unit => unit.Clock.CurrentCt)
            .ThenBy(unit => unit.InitiativeRank)
            .ThenBy(unit => unit.Unit.UnitSlot)
            .FirstOrDefault() ?? throw new InvalidOperationException("No CT-eligible unit exists at the selected turn boundary.");

    private void RequirePending(DclCanonicalTimelineStep step, DclCanonicalTimelineStepKind kind)
    {
        if (!ReferenceEquals(_pendingStep, step) || step.Kind != kind || step.GlobalCt != CurrentGlobalCt)
            throw new InvalidOperationException("The timeline step is not the exact current pending step.");
    }
}
