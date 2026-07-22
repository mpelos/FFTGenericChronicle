namespace fftivc.generic.chronicle.codemod;

internal abstract record DclStatePayload(string SchemaId);

internal sealed record DclPropertyStatePayload(
    string SchemaId,
    IReadOnlyDictionary<string, string> Values) : DclStatePayload(SchemaId);

internal sealed record DclReraiseStatePayload(
    string SchemaId,
    int AbilityId,
    DclDiceExpression RestoredHpExpression,
    DclRational FaithMultiplier,
    bool FaithModifiesRestoredHp) : DclStatePayload(SchemaId);

internal sealed record DclStateApplication(
    DclStateDefinition Definition,
    DclUnitKey Target,
    DclUnitKey? Source,
    long AppliedAtGlobalCt,
    long AppliedBeforeTurnSerial,
    long? FirstEligibleTargetTurnSerial,
    long? FirstEligibleSourceTurnSerial,
    int? DurationUnits,
    int? Strength,
    int? WinningMargin,
    string StackDiscriminator,
    string? ContributionIdentity,
    DclStatePayload Payload,
    string PresentationId);

internal sealed record DclStateInstance(
    long InstanceId,
    DclStateDefinition Definition,
    DclUnitKey Target,
    DclUnitKey? Source,
    long AppliedAtGlobalCt,
    long AppliedBeforeTurnSerial,
    long? ExpiresAtGlobalCt,
    long? ExpiresAfterTargetTurnSerial,
    long? ExpiresAfterSourceTurnSerial,
    int? RemainingUses,
    long? NextTickGlobalCt,
    int? Strength,
    int? WinningMargin,
    string StackDiscriminator,
    string? ContributionIdentity,
    DclStatePayload Payload,
    string PresentationId)
{
    public string Kind => Definition.Kind;
    public string StackIdentity => $"{Definition.StackKey}\u001f{StackDiscriminator}";
}

internal enum DclStateApplicationOutcome
{
    Added,
    Replaced,
    Refreshed,
    StrongerReplaced,
    WeakerRejected,
    EqualExtended,
    ContributionAdded,
    ContributionReplaced,
    ContributionRefreshed,
    ExplicitMerged,
}

internal sealed record DclStateApplicationResult(
    DclStateApplicationOutcome Outcome,
    DclStateInstance Instance,
    IReadOnlyList<long> RemovedInstanceIds,
    IReadOnlyList<DclStateInstance> RemovedInstances);

internal enum DclStateScheduleEventKind
{
    Tick,
    Expire,
}

internal readonly record struct DclStateScheduleEvent(
    long GlobalCt,
    DclStateScheduleEventKind Kind,
    long InstanceId);

internal sealed record DclStateScheduleCommit(
    DclStateScheduleEvent Event,
    DclStateInstance Instance,
    bool Expired);

internal sealed record DclStateSchedulePending(
    DclStateScheduleEvent Event,
    DclStateInstance Instance);

internal sealed record DclStateRegistryTargetSnapshot(
    long Revision,
    IReadOnlyList<DclStateInstance> Instances);

internal sealed class DclStateRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<long, DclStateInstance> _instances = new();
    private readonly Dictionary<DclUnitKey, long> _targetRevisions = new();
    private long _nextInstanceId = 1;

    public DclStateRegistry(int battleGeneration, long initialGlobalCt = 0)
    {
        if (battleGeneration <= 0) throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        if (initialGlobalCt < 0) throw new ArgumentOutOfRangeException(nameof(initialGlobalCt));
        BattleGeneration = battleGeneration;
        CurrentGlobalCt = initialGlobalCt;
    }

    private DclStateRegistry(DclStateRegistry source)
    {
        BattleGeneration = source.BattleGeneration;
        CurrentGlobalCt = source.CurrentGlobalCt;
        _nextInstanceId = source._nextInstanceId;
        foreach ((long id, DclStateInstance instance) in source._instances)
            _instances.Add(id, instance);
        foreach ((DclUnitKey target, long revision) in source._targetRevisions)
            _targetRevisions.Add(target, revision);
    }

    public int BattleGeneration { get; }
    public long CurrentGlobalCt { get; private set; }
    internal long NextInstanceId { get { lock (_gate) return _nextInstanceId; } }
    internal IReadOnlyDictionary<DclUnitKey, long> TargetRevisions
    {
        get
        {
            lock (_gate) return new Dictionary<DclUnitKey, long>(_targetRevisions);
        }
    }

    public IReadOnlyList<DclStateInstance> Instances
    {
        get
        {
            lock (_gate) return _instances.Values.OrderBy(instance => instance.InstanceId).ToArray();
        }
    }

    public DclStateRegistry CloneForEvaluation()
    {
        lock (_gate) return new DclStateRegistry(this);
    }

    public DclStateRegistryCheckpoint CaptureCheckpoint()
        => DclStateRegistryCheckpointCodec.Capture(this);

    internal void RestoreCheckpoint(
        DclStateRegistryCheckpoint checkpoint,
        IReadOnlyDictionary<string, DclStateDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        ArgumentNullException.ThrowIfNull(definitions);
        lock (_gate)
        {
            if (_instances.Count != 0 || _targetRevisions.Count != 0 || _nextInstanceId != 1 ||
                CurrentGlobalCt != checkpoint.CurrentGlobalCt)
                throw new InvalidOperationException("A state checkpoint can restore only into a fresh registry at the saved CT.");
            DclStateInstance[] restored = checkpoint.Instances
                .Select(instance => DclStateRegistryCheckpointCodec.RestoreInstance(
                    instance,
                    BattleGeneration,
                    definitions))
                .OrderBy(instance => instance.InstanceId)
                .ToArray();
            if (restored.Select(instance => instance.InstanceId).Distinct().Count() != restored.Length ||
                restored.Any(instance => instance.InstanceId >= checkpoint.NextInstanceId))
                throw new InvalidOperationException("State checkpoint instance identities or next identity are invalid.");
            foreach (DclStateInstance instance in restored)
                ValidateRestoredInstance(instance);
            foreach (IGrouping<(DclUnitKey Target, string StackIdentity), DclStateInstance> group in restored
                         .GroupBy(instance => (instance.Target, instance.StackIdentity)))
            {
                DclStateStackPolicy policy = group.First().Definition.StackPolicy;
                if (group.Any(instance => instance.Definition.StackPolicy != policy))
                    throw new InvalidOperationException("State checkpoint stack identity mixes incompatible policies.");
                if (policy is DclStateStackPolicy.Replace or DclStateStackPolicy.Refresh or
                    DclStateStackPolicy.StrongestWins or DclStateStackPolicy.Explicit && group.Count() != 1)
                    throw new InvalidOperationException("State checkpoint contains duplicate singular stack identities.");
                if (policy == DclStateStackPolicy.StackToCap &&
                    group.Select(instance => instance.ContributionIdentity).Distinct(StringComparer.Ordinal).Count() != group.Count())
                    throw new InvalidOperationException("State checkpoint contains duplicate StackToCap contributions.");
            }
            foreach (DclStateInstance instance in restored)
                _instances.Add(instance.InstanceId, instance);
            foreach (DclStateRegistryRevisionCheckpoint revision in checkpoint.TargetRevisions)
            {
                if (revision.UnitSlot is < 0 or >= 64 || revision.CharacterId < 0 || revision.Revision < 0)
                    throw new InvalidOperationException("State checkpoint contains an invalid target revision.");
                var target = new DclUnitKey(BattleGeneration, revision.UnitSlot, revision.CharacterId);
                if (!_targetRevisions.TryAdd(target, revision.Revision))
                    throw new InvalidOperationException("State checkpoint contains a duplicate target revision.");
            }
            if (restored.Any(instance => !_targetRevisions.ContainsKey(instance.Target)))
                throw new InvalidOperationException("State checkpoint omitted the revision of a live target.");
            _nextInstanceId = checkpoint.NextInstanceId;
        }
    }

    private void ValidateRestoredInstance(DclStateInstance instance)
    {
        DclAuthoringValidation definition = DclAuthoringContract.Validate(instance.Definition);
        if (!definition.IsValid)
            throw new InvalidOperationException(
                $"Restored state '{instance.Kind}' has invalid authoring: {string.Join("; ", definition.Findings)}");
        if (instance.Definition.SourceRequired && instance.Source is null ||
            instance.AppliedAtGlobalCt < 0 || instance.AppliedAtGlobalCt > CurrentGlobalCt ||
            instance.AppliedBeforeTurnSerial < 0 || instance.Strength < 0 ||
            instance.RemainingUses <= 0 || instance.NextTickGlobalCt <= CurrentGlobalCt)
            throw new InvalidOperationException($"Restored state instance {instance.InstanceId} has invalid common lifecycle data.");
        bool hasGlobal = instance.ExpiresAtGlobalCt is not null;
        bool hasTarget = instance.ExpiresAfterTargetTurnSerial is not null;
        bool hasSource = instance.ExpiresAfterSourceTurnSerial is not null;
        bool hasUses = instance.RemainingUses is not null;
        switch (instance.Definition.Duration.Clock)
        {
            case DclStateDurationClock.GlobalCt:
                if (!hasGlobal || hasTarget || hasSource || hasUses || instance.ExpiresAtGlobalCt <= CurrentGlobalCt)
                    throw new InvalidOperationException("Restored GlobalCT state has an invalid expiry shape.");
                if ((instance.Definition.TickProfile is null) != (instance.NextTickGlobalCt is null) ||
                    instance.NextTickGlobalCt > instance.ExpiresAtGlobalCt)
                    throw new InvalidOperationException("Restored periodic state has an invalid next-tick boundary.");
                break;
            case DclStateDurationClock.TargetTurn:
                if (hasGlobal || !hasTarget || hasSource || hasUses || instance.NextTickGlobalCt is not null)
                    throw new InvalidOperationException("Restored TargetTurn state has an invalid expiry shape.");
                break;
            case DclStateDurationClock.SourceTurn:
                if (hasGlobal || hasTarget || !hasSource || hasUses || instance.NextTickGlobalCt is not null)
                    throw new InvalidOperationException("Restored SourceTurn state has an invalid expiry shape.");
                break;
            case DclStateDurationClock.UsesOrTriggers:
                if (hasGlobal || hasTarget || hasSource || !hasUses || instance.NextTickGlobalCt is not null)
                    throw new InvalidOperationException("Restored UsesOrTriggers state has an invalid expiry shape.");
                break;
            case DclStateDurationClock.ExplicitCommand:
            case DclStateDurationClock.Permanent:
            case DclStateDurationClock.Explicit:
                if (hasGlobal || hasTarget || hasSource || hasUses || instance.NextTickGlobalCt is not null)
                    throw new InvalidOperationException("Restored explicit/permanent state has an invalid expiry shape.");
                break;
            default:
                throw new InvalidOperationException("Restored state has an unknown duration clock.");
        }
    }

    public DclStateRegistryTargetSnapshot CaptureTarget(DclUnitKey target)
    {
        ValidateTarget(target);
        lock (_gate)
        {
            return new DclStateRegistryTargetSnapshot(
                _targetRevisions.GetValueOrDefault(target),
                _instances.Values
                    .Where(instance => instance.Target == target)
                    .OrderBy(instance => instance.InstanceId)
                    .ToArray());
        }
    }

    public DclStateApplicationResult Apply(DclStateApplication application)
    {
        ValidateGenericApplication(application);
        lock (_gate)
        {
            DclStateInstance newcomer = CreateInstance(application, _nextInstanceId);
            DclStateInstance[] competing = _instances.Values
                .Where(instance => instance.Target == newcomer.Target && instance.StackIdentity == newcomer.StackIdentity)
                .OrderBy(instance => instance.InstanceId)
                .ToArray();

            return application.Definition.StackPolicy switch
            {
                DclStateStackPolicy.Replace => Replace(competing, newcomer, DclStateApplicationOutcome.Replaced),
                DclStateStackPolicy.Refresh => Refresh(competing, newcomer),
                DclStateStackPolicy.StrongestWins => StrongestWins(competing, newcomer),
                DclStateStackPolicy.StackToCap => StackToCap(competing, newcomer),
                DclStateStackPolicy.Independent => Add(newcomer, DclStateApplicationOutcome.Added),
                DclStateStackPolicy.Explicit => throw new InvalidOperationException(
                    $"State '{application.Definition.Kind}' requires a named stacking handler."),
                _ => throw new InvalidOperationException("The state definition has no usable stacking policy."),
            };
        }
    }

    internal void ValidateGenericApplication(DclStateApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        ValidateApplication(application);
        if (application.Definition.StackPolicy == DclStateStackPolicy.Explicit)
            throw new InvalidOperationException(
                $"State '{application.Definition.Kind}' requires a named stacking handler.");
    }

    public DclStateApplicationResult ApplyShock(DclStateApplication application, int maxHp)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp));
        ValidateApplication(application);
        if (!StringComparer.Ordinal.Equals(application.Definition.Kind, "shock") ||
            application.Definition.StackPolicy != DclStateStackPolicy.Explicit ||
            application.Definition.Duration.Clock != DclStateDurationClock.TargetTurn ||
            application.Payload is not DclShockStatePayload incoming)
            throw new ArgumentException("The named Shock handler requires the canonical explicit Shock/TargetTurn definition and typed payload.", nameof(application));
        lock (_gate)
        {
            DclStateInstance newcomer = CreateInstance(application, _nextInstanceId) with
            {
                Strength = incoming.PenaltyMagnitude(maxHp),
            };
            DclStateInstance[] competing = _instances.Values
                .Where(instance => instance.Target == newcomer.Target && instance.StackIdentity == newcomer.StackIdentity)
                .OrderBy(instance => instance.InstanceId)
                .ToArray();
            if (competing.Length == 0)
                return Add(newcomer, DclStateApplicationOutcome.Added);
            if (competing.Length != 1 || competing[0].Payload is not DclShockStatePayload current)
                throw new InvalidOperationException("The canonical Shock stack identity must own exactly one typed accumulator.");
            DclShockStatePayload mergedPayload = current.AddInjury(incoming.UnexpiredInjury);
            DclStateInstance merged = competing[0] with
            {
                Payload = mergedPayload,
                Strength = mergedPayload.PenaltyMagnitude(maxHp),
            };
            _instances[merged.InstanceId] = merged;
            BumpTargetRevision(merged.Target);
            return new DclStateApplicationResult(DclStateApplicationOutcome.ExplicitMerged, merged, [], []);
        }
    }

    public int AggregateStrength(DclUnitKey target, string stackKey, string stackDiscriminator)
    {
        lock (_gate)
        {
            DclStateInstance[] matching = _instances.Values.Where(instance =>
                    instance.Target == target && instance.Definition.StackKey == stackKey &&
                    instance.StackDiscriminator == stackDiscriminator)
                .ToArray();
            if (matching.Length == 0) return 0;
            DclStateDefinition definition = matching[0].Definition;
            if (definition.StackPolicy != DclStateStackPolicy.StackToCap)
                return matching.Max(instance => instance.Strength ?? 0);
            int sum = matching.Sum(instance => Math.Max(0, instance.Strength ?? 0));
            return Math.Min(definition.StackCap!.Value, sum);
        }
    }

    public bool TryGet(long instanceId, out DclStateInstance instance)
    {
        lock (_gate) return _instances.TryGetValue(instanceId, out instance!);
    }

    public bool ConsumeUse(long instanceId)
    {
        lock (_gate)
        {
            if (!_instances.TryGetValue(instanceId, out DclStateInstance? instance) || instance.RemainingUses is null)
                return false;
            int remaining = instance.RemainingUses.Value - 1;
            if (remaining <= 0)
            {
                bool removed = _instances.Remove(instanceId);
                if (removed) BumpTargetRevision(instance.Target);
                return removed;
            }
            _instances[instanceId] = instance with { RemainingUses = remaining };
            BumpTargetRevision(instance.Target);
            return true;
        }
    }

    public IReadOnlyList<long> Cure(DclUnitKey target, IEnumerable<string> cureFamilies)
    {
        ArgumentNullException.ThrowIfNull(cureFamilies);
        HashSet<string> families = cureFamilies.Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        lock (_gate)
            return RemoveWhere(instance => instance.Target == target &&
                instance.Definition.CureFamilies.Any(families.Contains));
    }

    public IReadOnlyList<long> RemoveInstances(IEnumerable<long> instanceIds)
    {
        ArgumentNullException.ThrowIfNull(instanceIds);
        long[] ids = instanceIds.Distinct().Order().ToArray();
        lock (_gate)
        {
            foreach (long id in ids)
                if (!_instances.ContainsKey(id))
                    throw new ArgumentException($"State instance {id} is not present in this registry.", nameof(instanceIds));
            DclUnitKey[] affectedTargets = ids.Select(id => _instances[id].Target).Distinct().ToArray();
            foreach (long id in ids) _instances.Remove(id);
            foreach (DclUnitKey target in affectedTargets) BumpTargetRevision(target);
            return ids;
        }
    }

    public IReadOnlyList<long> RemoveKind(DclUnitKey target, string stateKind)
    {
        if (!target.IsValid || target.BattleGeneration != BattleGeneration)
            throw new ArgumentException("State-removal target is invalid for this registry.", nameof(target));
        if (string.IsNullOrWhiteSpace(stateKind))
            throw new ArgumentException("State kind is required.", nameof(stateKind));
        lock (_gate)
            return RemoveWhere(instance => instance.Target == target &&
                StringComparer.Ordinal.Equals(instance.Kind, stateKind));
    }

    public IReadOnlyList<long> CompleteTargetTurn(DclUnitKey target, long completedTurnSerial)
    {
        if (completedTurnSerial < 0) throw new ArgumentOutOfRangeException(nameof(completedTurnSerial));
        lock (_gate)
            return RemoveWhere(instance => instance.Target == target &&
                instance.ExpiresAfterTargetTurnSerial is { } expiry && expiry <= completedTurnSerial);
    }

    public IReadOnlyList<long> CompleteSourceTurn(DclUnitKey source, long completedTurnSerial)
    {
        if (completedTurnSerial < 0) throw new ArgumentOutOfRangeException(nameof(completedTurnSerial));
        lock (_gate)
            return RemoveWhere(instance => instance.Source == source &&
                instance.ExpiresAfterSourceTurnSerial is { } expiry && expiry <= completedTurnSerial);
    }

    public IReadOnlyList<long> OnTargetKo(DclUnitKey target)
    {
        lock (_gate)
            return RemoveWhere(instance => instance.Target == target && instance.Definition.RemoveOnTargetKo);
    }

    public IReadOnlyList<long> OnSourceKo(DclUnitKey source)
    {
        lock (_gate)
            return RemoveWhere(instance => instance.Source == source && instance.Definition.RemoveOnSourceKo);
    }

    public IReadOnlyList<long> OnSourceLoss(DclUnitKey source)
    {
        lock (_gate)
            return RemoveWhere(instance => instance.Source == source && instance.Definition.RemoveOnSourceLoss);
    }

    public IReadOnlyList<long> OnUnitRemoved(DclUnitKey unit)
    {
        lock (_gate)
            return RemoveWhere(instance => instance.Target == unit || instance.Source == unit);
    }

    public IReadOnlyList<long> OnUnitIdentityObserved(DclUnitKey current)
    {
        if (!current.IsValid || current.BattleGeneration != BattleGeneration)
            throw new ArgumentException("Observed UnitKey is invalid for this battle generation.", nameof(current));
        lock (_gate)
            return RemoveWhere(instance =>
                (instance.Target.UnitSlot == current.UnitSlot && instance.Target != current) ||
                (instance.Source is { } source && source.UnitSlot == current.UnitSlot && source != current));
    }

    internal DclStateSchedulePending? BeginNextGlobalScheduleStepThrough(long globalCt)
    {
        if (globalCt < CurrentGlobalCt) throw new ArgumentOutOfRangeException(nameof(globalCt));
        lock (_gate)
        {
            DclStateScheduleEvent? next = FindNextScheduleEvent(globalCt);
            if (next is null)
            {
                CurrentGlobalCt = globalCt;
                return null;
            }
            CurrentGlobalCt = next.Value.GlobalCt;
            DclStateInstance instance = _instances.TryGetValue(next.Value.InstanceId, out DclStateInstance? found)
                ? found
                : throw new InvalidOperationException("The selected state schedule event lost its instance.");
            return new DclStateSchedulePending(next.Value, instance);
        }
    }

    internal DclStateScheduleEvent? PeekNextGlobalScheduleEventThrough(long globalCt)
    {
        if (globalCt < CurrentGlobalCt) throw new ArgumentOutOfRangeException(nameof(globalCt));
        lock (_gate) return FindNextScheduleEvent(globalCt);
    }

    internal void AdvanceGlobalClockTo(long globalCt)
    {
        if (globalCt < CurrentGlobalCt) throw new ArgumentOutOfRangeException(nameof(globalCt));
        lock (_gate)
        {
            DclStateScheduleEvent? pending = FindNextScheduleEvent(globalCt);
            if (pending is { } due && due.GlobalCt < globalCt)
                throw new InvalidOperationException(
                    $"GlobalCT cannot advance past pending {due.Kind} event {due.InstanceId} at {due.GlobalCt}.");
            CurrentGlobalCt = globalCt;
        }
    }

    internal DclStateScheduleCommit CommitGlobalScheduleStep(DclStateSchedulePending pending)
    {
        ArgumentNullException.ThrowIfNull(pending);
        lock (_gate)
        {
            if (pending.Event.GlobalCt != CurrentGlobalCt ||
                !_instances.TryGetValue(pending.Event.InstanceId, out DclStateInstance? current) ||
                current != pending.Instance)
                throw new InvalidOperationException("The pending global schedule step became stale before commit.");
            if (pending.Event.Kind == DclStateScheduleEventKind.Tick)
            {
                if (current.NextTickGlobalCt != pending.Event.GlobalCt)
                    throw new InvalidOperationException("The pending tick no longer matches NextTickGlobalCT.");
                int interval = current.Definition.TickProfile!.Interval;
                _instances[current.InstanceId] = current with
                {
                    NextTickGlobalCt = checked(current.NextTickGlobalCt.Value + interval),
                };
                BumpTargetRevision(current.Target);
                return new DclStateScheduleCommit(pending.Event, current, Expired: false);
            }
            if (current.ExpiresAtGlobalCt != pending.Event.GlobalCt)
                throw new InvalidOperationException("The pending expiry no longer matches ExpiresAtGlobalCT.");
            _instances.Remove(current.InstanceId);
            BumpTargetRevision(current.Target);
            return new DclStateScheduleCommit(pending.Event, current, Expired: true);
        }
    }

    public IReadOnlyList<long> Clear()
    {
        lock (_gate)
        {
            long[] ids = _instances.Keys.Order().ToArray();
            DclUnitKey[] affectedTargets = _instances.Values.Select(instance => instance.Target).Distinct().ToArray();
            _instances.Clear();
            foreach (DclUnitKey target in affectedTargets) BumpTargetRevision(target);
            return ids;
        }
    }

    private void ValidateApplication(DclStateApplication application)
    {
        DclAuthoringValidation definitionValidation = DclAuthoringContract.Validate(application.Definition);
        if (!definitionValidation.IsValid)
            throw new ArgumentException($"State definition is invalid: {string.Join("; ", definitionValidation.Findings)}", nameof(application));
        if (!application.Target.IsValid || application.Target.BattleGeneration != BattleGeneration)
            throw new ArgumentException("Target UnitKey is invalid for this battle generation.", nameof(application));
        if (application.Definition.SourceRequired && application.Source is null)
            throw new ArgumentException("This source-bound state requires a SourceUnitKey.", nameof(application));
        if (application.Source is { } source && (!source.IsValid || source.BattleGeneration != BattleGeneration))
            throw new ArgumentException("Source UnitKey is invalid for this battle generation.", nameof(application));
        if (application.AppliedAtGlobalCt != CurrentGlobalCt || application.AppliedBeforeTurnSerial < 0)
            throw new ArgumentException("Application must use the registry's current global CT and a nonnegative turn serial.", nameof(application));
        if (!StringComparer.Ordinal.Equals(application.Payload.SchemaId, application.Definition.PayloadSchema))
            throw new ArgumentException("Typed payload schema does not match the state definition.", nameof(application));
        if (string.IsNullOrWhiteSpace(application.PresentationId) ||
            !StringComparer.Ordinal.Equals(application.PresentationId, application.Definition.PresentationProfile))
            throw new ArgumentException("Presentation identity does not match the state definition.", nameof(application));
        if (application.Definition.StackPolicy == DclStateStackPolicy.StrongestWins && application.Strength is null)
            throw new ArgumentException("StrongestWins requires an integer Strength.", nameof(application));
        if (application.Definition.StackPolicy == DclStateStackPolicy.StackToCap)
        {
            if (application.Strength is null or < 0 || string.IsNullOrWhiteSpace(application.ContributionIdentity))
                throw new ArgumentException("StackToCap requires nonnegative Strength and one contribution identity.", nameof(application));
        }
        else if (!string.IsNullOrWhiteSpace(application.ContributionIdentity))
        {
            throw new ArgumentException("Contribution identity is legal only for StackToCap.", nameof(application));
        }
        ValidateDurationInputs(application);
    }

    private static void ValidateDurationInputs(DclStateApplication application)
    {
        int? resolvedDuration = DclStateDurationRules.Resolve(
            application.Definition.Duration,
            application.WinningMargin,
            application.DurationUnits);
        if (resolvedDuration != application.DurationUnits)
            throw new ArgumentException("State duration does not match its normalized authored rule.");
        bool positiveDuration = application.DurationUnits is > 0;
        switch (application.Definition.Duration.Clock)
        {
            case DclStateDurationClock.GlobalCt:
                if (!positiveDuration) throw new ArgumentException("GlobalCT duration requires positive resolved units.");
                break;
            case DclStateDurationClock.TargetTurn:
                if (!positiveDuration || application.FirstEligibleTargetTurnSerial is null or < 0)
                    throw new ArgumentException("TargetTurn duration requires positive units and the first turn that begins active.");
                break;
            case DclStateDurationClock.SourceTurn:
                if (!positiveDuration || application.FirstEligibleSourceTurnSerial is null or < 0 || application.Source is null)
                    throw new ArgumentException("SourceTurn duration requires a source, positive units, and the first source turn that begins active.");
                break;
            case DclStateDurationClock.UsesOrTriggers:
                if (!positiveDuration) throw new ArgumentException("UsesOrTriggers requires a positive resolved use count.");
                break;
            case DclStateDurationClock.ExplicitCommand:
            case DclStateDurationClock.Permanent:
                if (application.DurationUnits is not null)
                    throw new ArgumentException("Permanent and ExplicitCommand states do not accept duration units.");
                break;
            case DclStateDurationClock.Explicit:
                throw new InvalidOperationException("An Explicit duration clock requires a named lifecycle handler.");
            default:
                throw new InvalidOperationException("The state definition has no usable duration clock.");
        }
        if (application.Definition.TickProfile is not null && application.Definition.Duration.Clock != DclStateDurationClock.GlobalCt)
            throw new ArgumentException("Periodic ticks require the GlobalCT duration clock.");
    }

    private DclStateInstance CreateInstance(DclStateApplication application, long instanceId)
    {
        int? duration = application.DurationUnits;
        long? globalExpiry = application.Definition.Duration.Clock == DclStateDurationClock.GlobalCt
            ? checked(application.AppliedAtGlobalCt + duration!.Value)
            : null;
        long? targetExpiry = application.Definition.Duration.Clock == DclStateDurationClock.TargetTurn
            ? checked(application.FirstEligibleTargetTurnSerial!.Value + duration!.Value - 1)
            : null;
        long? sourceExpiry = application.Definition.Duration.Clock == DclStateDurationClock.SourceTurn
            ? checked(application.FirstEligibleSourceTurnSerial!.Value + duration!.Value - 1)
            : null;
        int? remainingUses = application.Definition.Duration.Clock == DclStateDurationClock.UsesOrTriggers
            ? duration
            : null;
        long? nextTick = application.Definition.TickProfile is { } tick
            ? checked(application.AppliedAtGlobalCt + tick.Interval)
            : null;
        return new DclStateInstance(
            instanceId,
            application.Definition,
            application.Target,
            application.Source,
            application.AppliedAtGlobalCt,
            application.AppliedBeforeTurnSerial,
            globalExpiry,
            targetExpiry,
            sourceExpiry,
            remainingUses,
            nextTick,
            application.Strength,
            application.WinningMargin,
            application.StackDiscriminator ?? "",
            application.ContributionIdentity,
            application.Payload,
            application.PresentationId);
    }

    private DclStateApplicationResult Add(DclStateInstance newcomer, DclStateApplicationOutcome outcome)
    {
        long id = _nextInstanceId++;
        newcomer = newcomer with { InstanceId = id };
        _instances.Add(id, newcomer);
        BumpTargetRevision(newcomer.Target);
        return new DclStateApplicationResult(outcome, newcomer, [], []);
    }

    private DclStateApplicationResult Replace(
        IReadOnlyList<DclStateInstance> competing,
        DclStateInstance newcomer,
        DclStateApplicationOutcome outcome)
    {
        long[] removed = competing.Select(instance => instance.InstanceId).ToArray();
        foreach (long id in removed) _instances.Remove(id);
        DclStateApplicationResult added = Add(newcomer, competing.Count == 0 ? DclStateApplicationOutcome.Added : outcome);
        return added with { RemovedInstanceIds = removed, RemovedInstances = competing.ToArray() };
    }

    private DclStateApplicationResult Refresh(IReadOnlyList<DclStateInstance> competing, DclStateInstance newcomer)
    {
        if (competing.Count == 0) return Add(newcomer, DclStateApplicationOutcome.Added);
        DclStateInstance existing = competing[0];
        DclStateInstance refreshed = existing with
        {
            AppliedAtGlobalCt = newcomer.AppliedAtGlobalCt,
            AppliedBeforeTurnSerial = newcomer.AppliedBeforeTurnSerial,
            ExpiresAtGlobalCt = newcomer.ExpiresAtGlobalCt,
            ExpiresAfterTargetTurnSerial = newcomer.ExpiresAfterTargetTurnSerial,
            ExpiresAfterSourceTurnSerial = newcomer.ExpiresAfterSourceTurnSerial,
            RemainingUses = newcomer.RemainingUses,
            NextTickGlobalCt = newcomer.NextTickGlobalCt,
            // Re-declaring Overwatch replaces the prepared Action reservation while preserving
            // the stable state instance used by Refresh stacking.
            Payload = newcomer.Payload is DclOverwatchStatePayload ? newcomer.Payload : existing.Payload,
        };
        _instances[existing.InstanceId] = refreshed;
        BumpTargetRevision(refreshed.Target);
        return new DclStateApplicationResult(DclStateApplicationOutcome.Refreshed, refreshed, [], []);
    }

    private DclStateApplicationResult StrongestWins(
        IReadOnlyList<DclStateInstance> competing,
        DclStateInstance newcomer)
    {
        if (competing.Count == 0) return Add(newcomer, DclStateApplicationOutcome.Added);
        DclStateInstance existing = competing[0];
        int comparison = newcomer.Strength!.Value.CompareTo(existing.Strength!.Value);
        if (comparison > 0)
            return Replace(competing, newcomer, DclStateApplicationOutcome.StrongerReplaced);
        if (comparison < 0)
            return new DclStateApplicationResult(DclStateApplicationOutcome.WeakerRejected, existing, [], []);

        DclStateInstance extended = existing with
        {
            ExpiresAtGlobalCt = Later(existing.ExpiresAtGlobalCt, newcomer.ExpiresAtGlobalCt),
            ExpiresAfterTargetTurnSerial = Later(existing.ExpiresAfterTargetTurnSerial, newcomer.ExpiresAfterTargetTurnSerial),
            ExpiresAfterSourceTurnSerial = Later(existing.ExpiresAfterSourceTurnSerial, newcomer.ExpiresAfterSourceTurnSerial),
            RemainingUses = Later(existing.RemainingUses, newcomer.RemainingUses),
        };
        _instances[existing.InstanceId] = extended;
        BumpTargetRevision(extended.Target);
        return new DclStateApplicationResult(DclStateApplicationOutcome.EqualExtended, extended, [], []);
    }

    private DclStateApplicationResult StackToCap(
        IReadOnlyList<DclStateInstance> competing,
        DclStateInstance newcomer)
    {
        DclStateInstance? sameContribution = competing.FirstOrDefault(instance =>
            StringComparer.Ordinal.Equals(instance.ContributionIdentity, newcomer.ContributionIdentity));
        if (sameContribution is null)
            return Add(newcomer, DclStateApplicationOutcome.ContributionAdded);
        return newcomer.Definition.ContributionReapplicationPolicy switch
        {
            DclStateStackPolicy.Refresh => Refresh([sameContribution], newcomer) with
            {
                Outcome = DclStateApplicationOutcome.ContributionRefreshed,
            },
            DclStateStackPolicy.Replace => Replace([sameContribution], newcomer, DclStateApplicationOutcome.ContributionReplaced),
            _ => throw new InvalidOperationException("StackToCap has no valid contribution reapplication policy."),
        };
    }

    private IReadOnlyList<long> RemoveWhere(Func<DclStateInstance, bool> predicate)
    {
        DclStateInstance[] removed = _instances.Values.Where(predicate).OrderBy(instance => instance.InstanceId).ToArray();
        long[] ids = removed.Select(instance => instance.InstanceId).ToArray();
        foreach (long id in ids) _instances.Remove(id);
        foreach (DclUnitKey target in removed.Select(instance => instance.Target).Distinct())
            BumpTargetRevision(target);
        return ids;
    }

    private void ValidateTarget(DclUnitKey target)
    {
        if (!target.IsValid || target.BattleGeneration != BattleGeneration)
            throw new ArgumentException("State target is invalid for this registry.", nameof(target));
    }

    private void BumpTargetRevision(DclUnitKey target)
        => _targetRevisions[target] = checked(_targetRevisions.GetValueOrDefault(target) + 1);

    private DclStateScheduleEvent? FindNextScheduleEvent(long throughGlobalCt)
    {
        return _instances.Values.SelectMany(instance => ScheduleCandidates(instance, throughGlobalCt))
            .OrderBy(candidate => candidate.GlobalCt)
            .ThenBy(candidate => candidate.Kind)
            .ThenBy(candidate => candidate.InstanceId)
            .Cast<DclStateScheduleEvent?>()
            .FirstOrDefault();
    }

    private static IEnumerable<DclStateScheduleEvent> ScheduleCandidates(
        DclStateInstance instance,
        long throughGlobalCt)
    {
        if (instance.NextTickGlobalCt is { } tick && tick <= throughGlobalCt &&
            (instance.ExpiresAtGlobalCt is null || tick <= instance.ExpiresAtGlobalCt))
            yield return new DclStateScheduleEvent(tick, DclStateScheduleEventKind.Tick, instance.InstanceId);
        if (instance.ExpiresAtGlobalCt is { } expiry && expiry <= throughGlobalCt)
            yield return new DclStateScheduleEvent(expiry, DclStateScheduleEventKind.Expire, instance.InstanceId);
    }

    private static long? Later(long? left, long? right)
        => left is null || right is null ? null : Math.Max(left.Value, right.Value);

    private static int? Later(int? left, int? right)
        => left is null || right is null ? null : Math.Max(left.Value, right.Value);
}
