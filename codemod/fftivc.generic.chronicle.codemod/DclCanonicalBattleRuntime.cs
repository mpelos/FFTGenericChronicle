namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalTurnCompletion(
    DclUnitKey Unit,
    long TurnSerial,
    IReadOnlyList<DclStateInstance> RemovedStates)
{
    public IReadOnlyList<long> RemovedStateInstanceIds =>
        RemovedStates.Select(instance => instance.InstanceId).ToArray();
}

internal sealed record DclCanonicalWeaponRuntimeSnapshot(
    DclUnitKey Unit,
    string ResourceKey,
    DclWeaponBalance Balance,
    DclWeaponReadinessProperty ReadinessProperty,
    bool Ready,
    bool ParrySuppressedAfterAttack);

internal sealed record DclCanonicalDefenseRuntimeSnapshot(
    DclUnitKey Unit,
    DclDefenseResourceSnapshot Resources);

internal sealed record DclCanonicalDefenseResourceCommit(
    DclUnitKey Unit,
    DclDefenseResourceSnapshot Expected,
    DclDefenseResourceSnapshot Updated);

internal sealed record DclCanonicalStateTickInvocation(
    long ActionInstanceId,
    int AbilityId,
    string EffectActionId,
    long EffectInstanceId,
    DclUnitKey Source,
    DclUnitKey Target,
    long GlobalCt,
    bool ImmediatePayload);

internal sealed record DclCanonicalGlobalScheduleStep(
    DclStateSchedulePending Pending,
    DclCanonicalStateTickInvocation? TickInvocation);

/// <summary>
/// Owns mutable DCL state for exactly one native battle generation. Static authoring/catalog data
/// remains in <see cref="DclCanonicalRuntimeCatalog"/>; nothing in this object survives a generation
/// boundary or a unit-slot identity replacement.
/// </summary>
internal sealed class DclCanonicalBattleRuntime
{
    private sealed class DefenseCadenceState
    {
        public Dictionary<string, int> ParryAttempts { get; } = new(StringComparer.Ordinal);
        public bool BlockAvailable { get; set; } = true;
        public long Revision { get; set; }
    }

    private readonly object _gate = new();
    private readonly Dictionary<int, DclUnitKey> _unitsBySlot = [];
    private readonly Dictionary<DclUnitKey, DclTurnResources> _turnResources = [];
    private readonly Dictionary<DclUnitKey, long> _turnSerials = [];
    private readonly Dictionary<(DclUnitKey Unit, string ResourceKey), DclWeaponActionState> _weapons = [];
    private readonly Dictionary<DclUnitKey, DefenseCadenceState> _defenseCadences = [];
    private readonly Dictionary<long, DclCanonicalStateTickInvocation> _pendingImmediatePayloads = [];
    private readonly HashSet<long> _completedImmediatePayloads = [];
    private readonly List<DclCanonicalPreparedActionTicket> _pendingPreparedActions = [];
    private DclCanonicalGlobalScheduleStep? _pendingGlobalScheduleStep;
    private DclCanonicalTimelineScheduler? _timeline;
    private long _lastPreparedFinalTileSequence;

    public DclCanonicalBattleRuntime(
        DclCanonicalRuntimeCatalog catalog,
        int battleGeneration,
        long initialGlobalCt = 0,
        IDclUniformRandomSource? executionRandomSource = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (battleGeneration <= 0) throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        if (initialGlobalCt < 0) throw new ArgumentOutOfRangeException(nameof(initialGlobalCt));
        Catalog = catalog;
        BattleGeneration = battleGeneration;
        ActionInstances = new DclActionInstanceSequence(battleGeneration);
        ExecutionRandom = new DclExecutionRandomLedger(battleGeneration, executionRandomSource);
        States = catalog.CreateBattleStateRegistry(battleGeneration, initialGlobalCt);
        PreparedStates = new DclCanonicalPreparedStateRuntime(catalog, States);
        NativeActions = new DclCanonicalNativeActionLedger(battleGeneration);
        NativeAdmissions = new DclCanonicalNativeOuterActionAdmission();
        NativeAdmittedActions = new DclCanonicalNativeAdmittedActionLedger(battleGeneration);
        NativePolicySources = new DclCanonicalNativePolicySourceLedger(battleGeneration);
    }

    public DclCanonicalRuntimeCatalog Catalog { get; }
    public int BattleGeneration { get; }
    public DclActionInstanceSequence ActionInstances { get; }
    public DclExecutionRandomLedger ExecutionRandom { get; }
    public DclStateRegistry States { get; }
    public DclCanonicalPreparedStateRuntime PreparedStates { get; }
    public DclCanonicalNativeActionLedger NativeActions { get; }
    public DclCanonicalNativeOuterActionAdmission NativeAdmissions { get; }
    public DclCanonicalNativeAdmittedActionLedger NativeAdmittedActions { get; }
    public DclCanonicalNativePolicySourceLedger NativePolicySources { get; }
    public long CurrentGlobalCt => States.CurrentGlobalCt;
    public int PendingPreparedActionCount { get { lock (_gate) return _pendingPreparedActions.Count; } }

    public DclStateApplicationResult ApplyState(DclStateApplication application)
    {
        ValidateStateApplication(application);
        lock (_gate)
        {
            DclStateApplication materialized = ReservePreparedActions(application);
            DclStateApplicationResult result = States.Apply(materialized);
            if (result.Instance.Payload is DclOverwatchStatePayload)
                PreparedStates.Attach(result.Instance.InstanceId);
            return result;
        }
    }

    internal void ValidateStateApplication(DclStateApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        RequireObserved(application.Target);
        if (application.Source is { } source)
            RequireObserved(source);
        // Prepared payloads carry executable identity and duplicate their initial finite-use count.
        // Validate them before the registry mutates; after application the registry is the sole
        // persistent count/lifetime owner used by the battle-owned prepared runtime.
        PreparedStates.ValidateApplication(application);
        States.ValidateGenericApplication(application);
    }

    public DclCanonicalOverwatchFinalTileBatch ResolveOverwatchFinalTile(
        DclFinalTileSnapshot movement,
        IEnumerable<DclCanonicalOverwatchFinalTileCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        DclFinalTileValidation movementValidation = DclFinalTileEvent.Validate(movement);
        if (!movementValidation.Accepted)
            throw new ArgumentException(
                $"Prepared movement resolution requires one accepted final-tile event: {movementValidation.Reason}.",
                nameof(movement));
        if (movement.BattleGeneration != BattleGeneration ||
            !TryGetObservedUnit(movement.MoverTableIndex, out DclUnitKey mover) ||
            mover.CharacterId != movement.MoverCharId)
            throw new ArgumentException("Final-tile mover is not the battle's current observed identity.", nameof(movement));
        var finalTile = new DclBattleTile(movement.UnitTile.X, movement.UnitTile.Y, movement.UnitTile.Layer);
        DclCanonicalOverwatchFinalTileCandidate[] ordered = candidates
            .OrderBy(candidate => candidate.StateInstanceId)
            .ToArray();
        if (ordered.Any(candidate => candidate.StateInstanceId <= 0) ||
            ordered.Select(candidate => candidate.StateInstanceId).Distinct().Count() != ordered.Length)
            throw new ArgumentException("Final-tile Overwatch candidates require unique positive state identities.", nameof(candidates));

        var effectsByState = new Dictionary<long, DclCanonicalOverwatchEffectBinding>();
        foreach (DclCanonicalOverwatchFinalTileCandidate candidate in ordered)
        {
            RequireObserved(candidate.Source);
            effectsByState.Add(
                candidate.StateInstanceId,
                PreparedStates.ValidateOverwatchFinalTileCandidate(candidate, mover, movement));
        }

        lock (_gate)
        {
            if (_pendingPreparedActions.Count != 0)
                throw new InvalidOperationException(
                    "A new final-tile event cannot overtake unresolved prepared Actions.");
            if (movement.Sequence <= _lastPreparedFinalTileSequence)
                throw new InvalidOperationException("Final-tile prepared resolution rejects duplicate or stale movement events.");
            _lastPreparedFinalTileSequence = movement.Sequence;

            var resolutions = new List<DclCanonicalOverwatchFinalTileResolution>(ordered.Length);
            var tickets = new List<DclCanonicalPreparedActionTicket>();
            foreach (DclCanonicalOverwatchFinalTileCandidate candidate in ordered)
            {
                DclCanonicalOverwatchTriggerResult trigger = PreparedStates.TryOverwatch(
                    candidate.StateInstanceId,
                    new DclOverwatchTriggerContext(
                        MovementEvent: true,
                        MovementSettled: true,
                        WeaponStillValid: candidate.WeaponStillValid,
                        SourceStillValid: candidate.SourceCanExecute,
                        TargetStillValid: candidate.TargetEligible,
                        RangeLegal: candidate.RangeLegal,
                        TrajectoryLegal: candidate.TrajectoryLegal,
                        TriggerConditionSatisfied: candidate.TriggerConditionSatisfied,
                        VerticalLegal: candidate.VerticalLegal));
                bool expectedTrigger = candidate.WeaponStillValid && candidate.SourceCanExecute &&
                    candidate.TargetEligible && candidate.RangeLegal && candidate.VerticalLegal &&
                    candidate.TrajectoryLegal && candidate.TriggerConditionSatisfied;
                if (trigger.Trigger.Triggered != expectedTrigger)
                    throw new InvalidOperationException("Overwatch trigger result diverged from its prevalidated final-tile plan.");
                DclActionDeclaration? declaration = null;
                if (trigger.Trigger.Triggered)
                {
                    DclCanonicalOverwatchEffectBinding effect = effectsByState[candidate.StateInstanceId];
                    DclActionProfile action = effect.Action;
                    if (trigger.EffectBinding != effect.Ability || trigger.EffectAction != action)
                        throw new InvalidOperationException(
                            "Overwatch trigger lost its prevalidated native ability/Action binding.");
                    long reservedActionInstanceId = trigger.ReservedActionInstanceId ??
                        throw new InvalidOperationException("Triggered Overwatch lost its declaration-time Action reservation.");
                    declaration = new DclActionDeclaration(
                        reservedActionInstanceId,
                        candidate.Source,
                        action.ActionId,
                        action.ProfileRevision,
                        DclTargetMode.Unit,
                        mover,
                        FixedTile: null,
                        candidate.SourceTile,
                        PassedRangeCheck: true,
                        PassedVerticalCheck: true,
                        FinalMpCost: 0,
                        ApprovedHpCap: 0,
                        CastCt: 0,
                        DeclaredAtGlobalCt: CurrentGlobalCt,
                        ResolvesAtGlobalCt: CurrentGlobalCt);
                    tickets.Add(new DclCanonicalPreparedActionTicket(
                        movement.Sequence,
                        tickets.Count,
                        candidate.StateInstanceId,
                        effect.Ability,
                        declaration,
                        DclCanonicalPreparedActionStage.Reserved,
                        Application: null));
                }
                resolutions.Add(new DclCanonicalOverwatchFinalTileResolution(candidate, trigger, declaration));
            }
            _pendingPreparedActions.AddRange(tickets);
            return new DclCanonicalOverwatchFinalTileBatch(movement, mover, finalTile, resolutions);
        }
    }

    public DclCanonicalProtectionRedirectResolution ResolveProtectionRedirect(
        DclCanonicalProtectionRedirectCandidate candidate)
    {
        DclCanonicalProtectionRedirectPlan plan = PlanProtectionRedirect(candidate);
        return CommitProtectionRedirect(plan);
    }

    public DclCanonicalProtectionRedirectPlan PlanProtectionRedirect(
        DclCanonicalProtectionRedirectCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        RequireObserved(candidate.Attacker);
        RequireObserved(candidate.DeclaredTarget);
        RequireObserved(candidate.Protector);
        lock (_gate)
            return PreparedStates.PlanProtectionRedirect(candidate);
    }

    public DclCanonicalProtectionRedirectResolution CommitProtectionRedirect(
        DclCanonicalProtectionRedirectPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        RequireObserved(plan.Candidate.Attacker);
        RequireObserved(plan.Candidate.DeclaredTarget);
        RequireObserved(plan.Candidate.Protector);
        lock (_gate)
            return PreparedStates.CommitProtectionRedirect(plan);
    }

    public DclCanonicalPreparedActionTicket PeekNextPreparedAction()
    {
        lock (_gate)
            return _pendingPreparedActions.Count == 0
                ? throw new InvalidOperationException("No prepared Action is pending execution.")
                : _pendingPreparedActions[0];
    }

    public DclCanonicalConfirmedExecutionDispatchResult ResolveAndPublishNextPreparedAction(
        object familyInput,
        IReadOnlyList<DclCanonicalReactionCandidate>? auxiliaryReactionCandidates = null)
    {
        ArgumentNullException.ThrowIfNull(familyInput);
        lock (_gate)
        {
            DclCanonicalPreparedActionTicket ticket = PeekNextPreparedAction();
            DclCanonicalConfirmedExecutionDispatcher.ValidatePreparedActionInput(ticket, familyInput);
            DclCanonicalConfirmedExecutionDispatchResult result =
                DclCanonicalConfirmedExecutionDispatcher.ResolveAndPublish(
                    this,
                    ticket.EffectBinding.AbilityId,
                    familyInput,
                    auxiliaryReactionCandidates);
            DclCanonicalNativeActionApplication application = result.Application ??
                throw new InvalidOperationException("A prepared Action cannot use native passthrough execution.");
            RequirePreparedApplicationIdentity(ticket, application);
            if (!ReferenceEquals(NativeActions.Get(ticket.Declaration.ActionInstanceId), application))
                throw new InvalidOperationException("Prepared execution did not publish its exact native application.");
            DclCanonicalPreparedActionTicket published = ticket with
            {
                Stage = DclCanonicalPreparedActionStage.Published,
                Application = application,
            };
            _pendingPreparedActions[0] = published;
            return result;
        }
    }

    public DclCanonicalPreparedActionTicket CompleteNextPreparedAction(
        DclCanonicalNativeActionApplication settledApplication)
    {
        ArgumentNullException.ThrowIfNull(settledApplication);
        lock (_gate)
        {
            DclCanonicalPreparedActionTicket ticket = PeekNextPreparedAction();
            if (ticket.Stage != DclCanonicalPreparedActionStage.Published ||
                !ReferenceEquals(ticket.Application, settledApplication) ||
                settledApplication.Stage != DclCanonicalNativeActionStage.Settled)
                throw new InvalidOperationException(
                    "A prepared Action completes only after its exact head application settles.");
            RequirePreparedApplicationIdentity(ticket, settledApplication);
            if (!ReferenceEquals(NativeActions.Get(ticket.Declaration.ActionInstanceId), settledApplication))
                throw new InvalidOperationException("The settled prepared application is absent from the native ledger.");
            NativeActions.Retire(ticket.Declaration.ActionInstanceId);
            _pendingPreparedActions.RemoveAt(0);
            return ticket;
        }
    }

    private DclStateApplication ReservePreparedActions(DclStateApplication application)
    {
        if (application.Payload is not DclOverwatchStatePayload payload)
            return application;
        int reservationCount = payload.Prepared.RemainingTriggers;
        if (reservationCount <= 0 || ActionInstances.NextId > long.MaxValue - reservationCount)
            throw new OverflowException("The declared Overwatch ActionInstance reservation is exhausted.");
        long[] reservations = Enumerable.Range(0, reservationCount)
            .Select(_ => ActionInstances.Next())
            .ToArray();
        return application with
        {
            Payload = payload with { ReservedActionInstanceIds = reservations },
        };
    }

    private static void RequirePreparedApplicationIdentity(
        DclCanonicalPreparedActionTicket ticket,
        DclCanonicalNativeActionApplication application)
    {
        DclCanonicalNativeActionPlan plan = application.Plan;
        if (plan.ActionInstanceId != ticket.Declaration.ActionInstanceId ||
            plan.Source != ticket.Declaration.Source ||
            plan.AbilityId != ticket.EffectBinding.AbilityId ||
            !StringComparer.Ordinal.Equals(plan.ActionId, ticket.Declaration.ActionId) ||
            plan.ProfileRevision != ticket.Declaration.ProfileRevision)
            throw new InvalidOperationException(
                "Prepared native publication diverged from its reserved Action identity.");
    }

    internal void AttachTimeline(DclCanonicalTimelineScheduler timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        lock (_gate)
        {
            if (_timeline is not null && !ReferenceEquals(_timeline, timeline))
                throw new InvalidOperationException("A canonical battle can own only one timeline scheduler.");
            _timeline = timeline;
        }
    }

    public bool IsCharging(DclUnitKey unit)
    {
        RequireObserved(unit);
        DclCanonicalTimelineScheduler? timeline;
        lock (_gate) timeline = _timeline;
        return timeline?.IsCharging(unit) == true;
    }

    public DclCanonicalChargedCancellation? ResolveConcentrationIncident(
        DclUnitKey unit,
        DclConcentrationResult concentration)
    {
        RequireObserved(unit);
        DclCanonicalTimelineScheduler? timeline;
        lock (_gate) timeline = _timeline;
        if (timeline is null)
        {
            if (concentration.Outcome != DclConcentrationOutcome.NoIncident)
                throw new InvalidOperationException(
                    "A concentration incident requires the battle's canonical timeline owner.");
            return null;
        }
        return timeline.ResolveConcentrationIncident(unit, concentration);
    }

    public DclCanonicalChargedCancellation? ResolveInjuryConcentration(
        DclUnitKey unit,
        DclInjuryConsequenceResult consequences)
    {
        RequireObserved(unit);
        DclCanonicalTimelineScheduler? timeline;
        lock (_gate) timeline = _timeline;
        if (timeline is null)
        {
            if (consequences.Concentration.Outcome != DclConcentrationOutcome.NoIncident)
                throw new InvalidOperationException(
                    "An Injury concentration incident requires the battle's canonical timeline owner.");
            return null;
        }
        return timeline.ResolveInjuryConcentration(unit, consequences);
    }

    public long NextActionInstanceId() => ActionInstances.Next();

    public DclRollIdentity RollIdentity(
        long actionInstanceId,
        DclUnitKey source,
        DclUnitKey? target,
        int strikeIndex,
        DclRollSite rollSite,
        int drawIndex)
    {
        if (actionInstanceId <= 0 || strikeIndex < 0 || drawIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(actionInstanceId));
        RequireObserved(source);
        if (target is { } targetUnit)
            RequireObserved(targetUnit);
        return new DclRollIdentity(
            BattleGeneration,
            actionInstanceId,
            source.UnitSlot,
            source.CharacterId,
            target?.UnitSlot ?? -1,
            target?.CharacterId ?? -1,
            strikeIndex,
            rollSite,
            drawIndex);
    }

    public bool TryGetObservedUnit(int unitSlot, out DclUnitKey unit)
    {
        lock (_gate) return _unitsBySlot.TryGetValue(unitSlot, out unit);
    }

    public IReadOnlyList<long> ObserveUnit(DclUnitKey unit)
    {
        RequireGeneration(unit);
        lock (_gate)
        {
            if (_unitsBySlot.TryGetValue(unit.UnitSlot, out DclUnitKey previous) && previous != unit)
            {
                CancelReservedPreparedActionsForUnit(previous);
                long[] removed = DclCanonicalAimLifecycle.CancelTrackedTargetLoss(States, previous)
                    .Concat(States.OnUnitRemoved(previous))
                    .Distinct()
                    .Order()
                    .ToArray();
                RemoveMutableUnitState(previous);
                _unitsBySlot[unit.UnitSlot] = unit;
                return removed;
            }
            _unitsBySlot[unit.UnitSlot] = unit;
            return [];
        }
    }

    public IReadOnlyList<long> RemoveUnit(DclUnitKey unit)
    {
        RequireObserved(unit);
        lock (_gate)
        {
            CancelReservedPreparedActionsForUnit(unit);
            long[] removed = DclCanonicalAimLifecycle.CancelTrackedTargetLoss(States, unit)
                .Concat(States.OnUnitRemoved(unit))
                .Distinct()
                .Order()
                .ToArray();
            _unitsBySlot.Remove(unit.UnitSlot);
            RemoveMutableUnitState(unit);
            return removed;
        }
    }

    private void CancelReservedPreparedActionsForUnit(DclUnitKey unit)
    {
        if (_pendingPreparedActions.Any(ticket =>
                ticket.Stage == DclCanonicalPreparedActionStage.Published &&
                (ticket.Declaration.Source == unit || ticket.Declaration.TrackedTarget == unit)))
            throw new InvalidOperationException(
                "A unit identity cannot disappear while its prepared native application is in flight.");
        _pendingPreparedActions.RemoveAll(ticket =>
            ticket.Stage == DclCanonicalPreparedActionStage.Reserved &&
            (ticket.Declaration.Source == unit || ticket.Declaration.TrackedTarget == unit));
    }

    public DclTurnResources TurnResources(DclUnitKey unit)
    {
        RequireObserved(unit);
        lock (_gate)
        {
            if (!_turnResources.TryGetValue(unit, out DclTurnResources? resources))
            {
                resources = new DclTurnResources();
                _turnResources.Add(unit, resources);
            }
            return resources;
        }
    }

    public DclWeaponActionState WeaponState(
        DclUnitKey unit,
        string resourceKey,
        DclWeaponBalance balance,
        DclWeaponReadinessProperty readiness,
        bool initiallyReady = true)
    {
        RequireObserved(unit);
        if (string.IsNullOrWhiteSpace(resourceKey))
            throw new ArgumentException("A weapon resource key is required.", nameof(resourceKey));
        var key = (unit, resourceKey);
        lock (_gate)
        {
            if (_weapons.TryGetValue(key, out DclWeaponActionState? existing))
            {
                if (existing.Balance != balance || existing.ReadinessProperty != readiness)
                    throw new InvalidOperationException("A live weapon resource cannot change its normalized balance/readiness identity in place.");
                return existing;
            }
            var created = new DclWeaponActionState(resourceKey, balance, readiness, initiallyReady);
            _weapons.Add(key, created);
            return created;
        }
    }

    public DclWeaponActionState RegisteredWeaponState(DclUnitKey unit, string resourceKey)
    {
        RequireObserved(unit);
        if (string.IsNullOrWhiteSpace(resourceKey))
            throw new ArgumentException("A weapon resource key is required.", nameof(resourceKey));
        lock (_gate)
            return _weapons.TryGetValue((unit, resourceKey), out DclWeaponActionState? weapon)
                ? weapon
                : throw new KeyNotFoundException(
                    $"Weapon resource '{resourceKey}' is not registered for the confirmed unit.");
    }

    public IReadOnlyList<DclCanonicalWeaponRuntimeSnapshot> CaptureWeaponStates(DclUnitKey unit)
    {
        RequireObserved(unit);
        lock (_gate)
            return _weapons
                .Where(pair => pair.Key.Unit == unit)
                .OrderBy(pair => pair.Key.ResourceKey, StringComparer.Ordinal)
                .Select(pair => new DclCanonicalWeaponRuntimeSnapshot(
                    pair.Key.Unit,
                    pair.Key.ResourceKey,
                    pair.Value.Balance,
                    pair.Value.ReadinessProperty,
                    pair.Value.Ready,
                    pair.Value.ParrySuppressedAfterAttack))
                .ToArray();
    }

    public DclReequipDefenseSnapshot CaptureReequipDefenseSnapshot(DclUnitKey unit)
    {
        RequireObserved(unit);
        lock (_gate)
        {
            DclDefenseResourceSnapshot defense = CaptureDefenseResourcesUnsafe(unit, []);
            IReadOnlyList<DclCanonicalWeaponRuntimeSnapshot> weapons = CaptureWeaponStates(unit);
            return new DclReequipDefenseSnapshot(
                defense.ParryAttemptCounts,
                defense.BlockAvailable,
                weapons.ToDictionary(weapon => weapon.ResourceKey, weapon => weapon.Ready, StringComparer.Ordinal),
                weapons.ToDictionary(
                    weapon => weapon.ResourceKey,
                    weapon => weapon.ParrySuppressedAfterAttack,
                    StringComparer.Ordinal));
        }
    }

    public DclDefenseResourceSnapshot CaptureDefenseResources(
        DclUnitKey unit,
        IEnumerable<string>? requestedParryResourceKeys = null)
    {
        RequireObserved(unit);
        string[] requested = (requestedParryResourceKeys ?? [])
            .Select(key => string.IsNullOrWhiteSpace(key)
                ? throw new ArgumentException("A requested Parry resource key must be named.", nameof(requestedParryResourceKeys))
                : key)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        lock (_gate) return CaptureDefenseResourcesUnsafe(unit, requested);
    }

    public void CommitDefenseResourcesBatch(IEnumerable<DclCanonicalDefenseResourceCommit> commits)
    {
        ArgumentNullException.ThrowIfNull(commits);
        DclCanonicalDefenseResourceCommit[] batch = commits.ToArray();
        if (batch.Select(commit => commit.Unit).Distinct().Count() != batch.Length)
            throw new ArgumentException("A defense-resource batch cannot commit the same unit twice.", nameof(commits));
        lock (_gate)
        {
            foreach (DclCanonicalDefenseResourceCommit commit in batch)
            {
                RequireObserved(commit.Unit);
                ValidateDefenseSnapshot(commit.Expected, nameof(commits));
                ValidateDefenseSnapshot(commit.Updated, nameof(commits));
                if (commit.Updated.Revision != commit.Expected.Revision)
                    throw new InvalidOperationException("A defense-resource action result must retain its captured revision until commit.");
                string[] keys = commit.Expected.ParryAttemptCounts.Keys
                    .Concat(commit.Updated.ParryAttemptCounts.Keys)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                DclDefenseResourceSnapshot current = CaptureDefenseResourcesUnsafe(commit.Unit, keys);
                if (!DefenseResourcesEqual(current, commit.Expected))
                    throw new InvalidOperationException("Defense resources changed after the action snapshot was captured.");
                if (commit.Expected.BlockAvailable is false && commit.Updated.BlockAvailable)
                    throw new InvalidOperationException("An action cannot restore a spent Block resource.");
                foreach (string key in keys)
                    if (commit.Updated.ParryAttemptCounts.GetValueOrDefault(key) <
                        commit.Expected.ParryAttemptCounts.GetValueOrDefault(key))
                        throw new InvalidOperationException("An action cannot reduce a cumulative Parry-attempt counter.");
            }

            foreach (DclCanonicalDefenseResourceCommit commit in batch)
            {
                if (DefenseResourcesEqual(commit.Expected, commit.Updated)) continue;
                if (!_defenseCadences.TryGetValue(commit.Unit, out DefenseCadenceState? state))
                {
                    state = new DefenseCadenceState();
                    _defenseCadences.Add(commit.Unit, state);
                }
                state.ParryAttempts.Clear();
                foreach ((string key, int count) in commit.Updated.ParryAttemptCounts)
                    if (count > 0) state.ParryAttempts.Add(key, count);
                state.BlockAvailable = commit.Updated.BlockAvailable;
                state.Revision = checked(commit.Expected.Revision + 1);
            }
        }
    }

    internal static bool DefenseResourcesEqual(
        DclDefenseResourceSnapshot left,
        DclDefenseResourceSnapshot right)
    {
        if (left.Revision != right.Revision || left.BlockAvailable != right.BlockAvailable)
            return false;
        string[] keys = left.ParryAttemptCounts.Keys
            .Concat(right.ParryAttemptCounts.Keys)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return keys.All(key => left.ParryAttemptCounts.GetValueOrDefault(key) ==
            right.ParryAttemptCounts.GetValueOrDefault(key));
    }

    public long BeginTurn(DclUnitKey unit)
    {
        RequireObserved(unit);
        lock (_gate)
        {
            long serial = checked(_turnSerials.GetValueOrDefault(unit) + 1);
            _turnSerials[unit] = serial;
            TurnResources(unit).ResetForGrantedTurn();
            foreach (DclWeaponActionState weapon in _weapons
                         .Where(pair => pair.Key.Unit == unit)
                         .Select(pair => pair.Value))
                weapon.BeginOwnerTurn();
            ResetDefenseResourcesForTurnUnsafe(unit);
            return serial;
        }
    }

    public long CurrentTurnSerial(DclUnitKey unit)
    {
        RequireObserved(unit);
        lock (_gate) return _turnSerials.GetValueOrDefault(unit);
    }

    internal IReadOnlyDictionary<DclUnitKey, long> CaptureTurnSerials()
    {
        lock (_gate) return new Dictionary<DclUnitKey, long>(_turnSerials);
    }

    internal void RestoreTurnSerials(IReadOnlyDictionary<DclUnitKey, long> serials)
    {
        ArgumentNullException.ThrowIfNull(serials);
        lock (_gate)
        {
            if (_turnSerials.Count != 0) throw new InvalidOperationException("Turn serials restore only into a fresh battle runtime.");
            foreach ((DclUnitKey unit, long serial) in serials)
            {
                RequireObserved(unit);
                if (serial < 0) throw new ArgumentOutOfRangeException(nameof(serials));
                if (serial > 0) _turnSerials.Add(unit, serial);
            }
        }
    }

    internal IReadOnlyList<DclCanonicalWeaponRuntimeSnapshot> CaptureWeaponStates()
    {
        lock (_gate)
            return _weapons
                .OrderBy(pair => pair.Key.Unit.UnitSlot)
                .ThenBy(pair => pair.Key.ResourceKey, StringComparer.Ordinal)
                .Select(pair => new DclCanonicalWeaponRuntimeSnapshot(
                    pair.Key.Unit,
                    pair.Key.ResourceKey,
                    pair.Value.Balance,
                    pair.Value.ReadinessProperty,
                    pair.Value.Ready,
                    pair.Value.ParrySuppressedAfterAttack))
                .ToArray();
    }

    internal void RestoreWeaponStates(IEnumerable<DclCanonicalWeaponRuntimeSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        lock (_gate)
        {
            if (_weapons.Count != 0) throw new InvalidOperationException("Weapon runtime restore requires a fresh battle runtime.");
            foreach (DclCanonicalWeaponRuntimeSnapshot snapshot in snapshots)
            {
                RequireObserved(snapshot.Unit);
                DclWeaponActionState state = WeaponState(
                    snapshot.Unit,
                    snapshot.ResourceKey,
                    snapshot.Balance,
                    snapshot.ReadinessProperty,
                    initiallyReady: snapshot.Ready);
                state.Restore(snapshot.Ready, snapshot.ParrySuppressedAfterAttack);
            }
        }
    }

    internal IReadOnlyList<DclCanonicalDefenseRuntimeSnapshot> CaptureDefenseStates()
    {
        lock (_gate)
            return _defenseCadences
                .OrderBy(pair => pair.Key.UnitSlot)
                .ThenBy(pair => pair.Key.CharacterId)
                .Select(pair => new DclCanonicalDefenseRuntimeSnapshot(
                    pair.Key,
                    CaptureDefenseResourcesUnsafe(pair.Key, [])))
                .ToArray();
    }

    internal void RestoreDefenseStates(IEnumerable<DclCanonicalDefenseRuntimeSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        lock (_gate)
        {
            if (_defenseCadences.Count != 0)
                throw new InvalidOperationException("Defense-resource restore requires a fresh battle runtime.");
            foreach (DclCanonicalDefenseRuntimeSnapshot snapshot in snapshots)
            {
                RequireObserved(snapshot.Unit);
                ValidateDefenseSnapshot(snapshot.Resources, nameof(snapshots));
                if (snapshot.Resources.Revision <= 0)
                    throw new InvalidOperationException("A persisted defense-resource state must have a positive revision.");
                var state = new DefenseCadenceState
                {
                    BlockAvailable = snapshot.Resources.BlockAvailable,
                    Revision = snapshot.Resources.Revision,
                };
                foreach ((string key, int count) in snapshot.Resources.ParryAttemptCounts)
                    if (count > 0) state.ParryAttempts.Add(key, count);
                if (!_defenseCadences.TryAdd(snapshot.Unit, state))
                    throw new InvalidOperationException("Defense-resource checkpoint duplicates a unit identity.");
            }
        }
    }

    public DclCanonicalTurnCompletion CompleteTurn(DclUnitKey unit)
    {
        RequireObserved(unit);
        lock (_gate)
        {
            long serial = _turnSerials.GetValueOrDefault(unit);
            if (serial <= 0)
                throw new InvalidOperationException("A unit cannot complete a turn before its first BeginTurn boundary.");
            Dictionary<long, DclStateInstance> before = States.Instances
                .ToDictionary(instance => instance.InstanceId);
            long[] removed = States.CompleteTargetTurn(unit, serial)
                .Concat(States.CompleteSourceTurn(unit, serial))
                .Distinct()
                .Order()
                .ToArray();
            DclStateInstance[] removedStates = removed.Select(id =>
                    before.TryGetValue(id, out DclStateInstance? instance)
                        ? instance
                        : throw new InvalidOperationException(
                            "Turn completion removed a state absent from its exact pre-commit snapshot."))
                .ToArray();
            return new DclCanonicalTurnCompletion(unit, serial, removedStates);
        }
    }

    public DclCanonicalGlobalScheduleStep? BeginNextGlobalScheduleStepThrough(long globalCt)
    {
        lock (_gate)
        {
            if (_pendingGlobalScheduleStep is { } existing)
            {
                if (globalCt < existing.Pending.Event.GlobalCt)
                    throw new ArgumentOutOfRangeException(nameof(globalCt));
                return existing;
            }
            DclStateSchedulePending? pending = States.BeginNextGlobalScheduleStepThrough(globalCt);
            if (pending is null) return null;
            DclCanonicalStateTickInvocation? tick = pending.Event.Kind == DclStateScheduleEventKind.Tick
                ? BuildTickInvocation(pending.Instance, pending.Event.GlobalCt, ImmediatePayload: false)
                : null;
            _pendingGlobalScheduleStep = new DclCanonicalGlobalScheduleStep(pending, tick);
            return _pendingGlobalScheduleStep;
        }
    }

    public DclStateScheduleCommit CommitGlobalScheduleStep(
        DclCanonicalGlobalScheduleStep step,
        DclCanonicalNativeActionApplication? settledTickApplication = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        lock (_gate)
        {
            if (!ReferenceEquals(_pendingGlobalScheduleStep, step))
                throw new InvalidOperationException("The global schedule step is not the battle's current pending step.");
            if (step.TickInvocation is { } tick)
            {
                if (settledTickApplication is null ||
                    settledTickApplication.Stage != DclCanonicalNativeActionStage.Settled ||
                    !ReferenceEquals(NativeActions.Get(tick.ActionInstanceId), settledTickApplication) ||
                    settledTickApplication.Plan.ActionInstanceId != tick.ActionInstanceId ||
                    settledTickApplication.Plan.AbilityId != tick.AbilityId ||
                    !StringComparer.Ordinal.Equals(settledTickApplication.Plan.ActionId, tick.EffectActionId) ||
                    settledTickApplication.Plan.Source != tick.Source ||
                    settledTickApplication.Plan.ResourceFailed ||
                    settledTickApplication.Plan.Strikes.Count == 0 ||
                    settledTickApplication.Plan.Strikes.Any(strike => strike.Target != tick.Target))
                    throw new InvalidOperationException(
                        "A periodic tick advances only after its exact outer ActionInstance settles.");
            }
            else if (settledTickApplication is not null)
            {
                throw new ArgumentException("An expiry step cannot consume a tick application.", nameof(settledTickApplication));
            }
            DclStateScheduleCommit commit = States.CommitGlobalScheduleStep(step.Pending);
            if (settledTickApplication is not null)
                NativeActions.Retire(settledTickApplication.Plan.ActionInstanceId);
            _pendingGlobalScheduleStep = null;
            return commit;
        }
    }

    public DclCanonicalStateTickInvocation BeginImmediatePayloadInvocation(DclStateInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        lock (_gate)
        {
            if (!States.TryGet(instance.InstanceId, out DclStateInstance current) || current != instance)
                throw new ArgumentException("Immediate tick payload requires the exact current state instance.", nameof(instance));
            if (instance.Definition.TickProfile is not { ImmediatePayload: true })
                throw new InvalidOperationException("The state does not declare an immediate periodic payload.");
            if (_completedImmediatePayloads.Contains(instance.InstanceId))
                throw new InvalidOperationException("The state's immediate periodic payload has already settled.");
            if (_pendingImmediatePayloads.TryGetValue(instance.InstanceId, out DclCanonicalStateTickInvocation? pending))
                return pending;
            DclCanonicalStateTickInvocation invocation =
                BuildTickInvocation(instance, States.CurrentGlobalCt, ImmediatePayload: true);
            _pendingImmediatePayloads.Add(instance.InstanceId, invocation);
            return invocation;
        }
    }

    public void CommitImmediatePayloadInvocation(
        DclCanonicalStateTickInvocation invocation,
        DclCanonicalNativeActionApplication settledApplication)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(settledApplication);
        lock (_gate)
        {
            if (!invocation.ImmediatePayload ||
                !_pendingImmediatePayloads.TryGetValue(invocation.EffectInstanceId, out DclCanonicalStateTickInvocation? pending) ||
                !ReferenceEquals(pending, invocation) ||
                settledApplication.Stage != DclCanonicalNativeActionStage.Settled ||
                !ReferenceEquals(NativeActions.Get(invocation.ActionInstanceId), settledApplication) ||
                settledApplication.Plan.ActionInstanceId != invocation.ActionInstanceId ||
                settledApplication.Plan.AbilityId != invocation.AbilityId ||
                !StringComparer.Ordinal.Equals(settledApplication.Plan.ActionId, invocation.EffectActionId) ||
                settledApplication.Plan.Source != invocation.Source ||
                settledApplication.Plan.ResourceFailed ||
                settledApplication.Plan.Strikes.Count == 0 ||
                settledApplication.Plan.Strikes.Any(strike => strike.Target != invocation.Target))
                throw new InvalidOperationException(
                    "An immediate periodic payload completes only after its exact outer ActionInstance settles.");
            NativeActions.Retire(invocation.ActionInstanceId);
            _pendingImmediatePayloads.Remove(invocation.EffectInstanceId);
            _completedImmediatePayloads.Add(invocation.EffectInstanceId);
        }
    }

    private DclCanonicalStateTickInvocation BuildTickInvocation(
        DclStateInstance instance,
        long globalCt,
        bool ImmediatePayload)
    {
        DclStateTickProfile tick = instance.Definition.TickProfile ??
            throw new InvalidOperationException("A scheduled tick lost its normalized TickProfile.");
        if (!Catalog.Authoring.Actions.TryGetValue(tick.EffectActionId, out DclActionProfile? action) ||
            action.SourceProfile.Source != DclActionSource.PeriodicEffect)
            throw new InvalidOperationException("A scheduled tick lost its normalized PeriodicEffect action.");
        (DclAbilityBinding binding, DclActionProfile boundAction) = Catalog.ResolveAbility(tick.EffectAbilityId);
        if (!StringComparer.Ordinal.Equals(binding.ActionId, tick.EffectActionId) ||
            !ReferenceEquals(action, boundAction))
            throw new InvalidOperationException("A scheduled tick lost its native carrier binding.");
        DclUnitKey source = tick.ActionSource switch
        {
            DclStateTickSource.OriginalSource => instance.Source ??
                throw new InvalidOperationException("OriginalSource tick lost its source UnitKey."),
            DclStateTickSource.Target => instance.Target,
            _ => throw new InvalidOperationException("A scheduled tick has no normalized source policy."),
        };
        RequireObserved(source);
        RequireObserved(instance.Target);
        return new DclCanonicalStateTickInvocation(
            NextActionInstanceId(),
            tick.EffectAbilityId,
            tick.EffectActionId,
            instance.InstanceId,
            source,
            instance.Target,
            globalCt,
            ImmediatePayload);
    }

    public DclCanonicalAimLifecycleResult CancelAimForMovement(DclUnitKey unit, bool forcedMovement)
    {
        RequireObserved(unit);
        return DclCanonicalAimLifecycle.CancelOwner(
            States,
            unit,
            forcedMovement ? "forced-movement-cancelled" : "voluntary-movement-cancelled");
    }

    public DclCanonicalAimLifecycleResult CancelAimForPosture(DclUnitKey unit, string posture)
    {
        RequireObserved(unit);
        if (string.IsNullOrWhiteSpace(posture)) throw new ArgumentException("A cancelling posture is required.", nameof(posture));
        return DclCanonicalAimLifecycle.CancelOwner(States, unit, $"posture-cancelled:{posture.Trim().ToLowerInvariant()}");
    }

    public DclCanonicalAimLifecycleResult CancelAimForTrajectoryLoss(DclUnitKey unit)
    {
        RequireObserved(unit);
        return DclCanonicalAimLifecycle.CancelOwner(States, unit, "trajectory-lost");
    }

    public DclCanonicalAimLifecycleResult ResolveAimInjuryRetention(
        DclUnitKey unit,
        int injury,
        bool forcedMovement,
        int will,
        int aimRetentionModifier,
        int statePenaltyMagnitude,
        int? roll)
    {
        RequireObserved(unit);
        return DclCanonicalAimLifecycle.ResolveInjuryRetention(
            States,
            unit,
            injury,
            forcedMovement,
            will,
            aimRetentionModifier,
            statePenaltyMagnitude,
            roll);
    }

    private void RequireGeneration(DclUnitKey unit)
    {
        if (!unit.IsValid || unit.BattleGeneration != BattleGeneration)
            throw new ArgumentException("Unit identity does not belong to this canonical battle generation.", nameof(unit));
    }

    internal void RequireObserved(DclUnitKey unit)
    {
        RequireGeneration(unit);
        lock (_gate)
        {
            if (!_unitsBySlot.TryGetValue(unit.UnitSlot, out DclUnitKey observed) || observed != unit)
                throw new InvalidOperationException("The unit identity is not the currently observed owner of its native battle slot.");
        }
    }

    private void RemoveMutableUnitState(DclUnitKey unit)
    {
        _turnResources.Remove(unit);
        _turnSerials.Remove(unit);
        _defenseCadences.Remove(unit);
        foreach ((DclUnitKey Unit, string ResourceKey) key in _weapons.Keys
                     .Where(key => key.Unit == unit)
                     .ToArray())
            _weapons.Remove(key);
    }

    private DclDefenseResourceSnapshot CaptureDefenseResourcesUnsafe(
        DclUnitKey unit,
        IEnumerable<string> requestedParryResourceKeys)
    {
        _defenseCadences.TryGetValue(unit, out DefenseCadenceState? state);
        var attempts = state is null
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : new Dictionary<string, int>(state.ParryAttempts, StringComparer.Ordinal);
        foreach (string key in requestedParryResourceKeys) attempts.TryAdd(key, 0);
        return new DclDefenseResourceSnapshot(
            attempts
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            state?.BlockAvailable ?? true,
            state?.Revision ?? 0);
    }

    private void ResetDefenseResourcesForTurnUnsafe(DclUnitKey unit)
    {
        if (!_defenseCadences.TryGetValue(unit, out DefenseCadenceState? state))
        {
            state = new DefenseCadenceState();
            _defenseCadences.Add(unit, state);
        }
        state.ParryAttempts.Clear();
        state.BlockAvailable = true;
        state.Revision = checked(state.Revision + 1);
    }

    private static void ValidateDefenseSnapshot(DclDefenseResourceSnapshot snapshot, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Revision < 0)
            throw new ArgumentException("A defense-resource revision cannot be negative.", parameterName);
        foreach ((string key, int count) in snapshot.ParryAttemptCounts)
            if (string.IsNullOrWhiteSpace(key) || count < 0)
                throw new ArgumentException("Parry resource keys must be named and their counts nonnegative.", parameterName);
    }
}
