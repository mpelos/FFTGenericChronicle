namespace fftivc.generic.chronicle.codemod;

internal sealed record DclOverwatchStatePayload(
    string SchemaId,
    DclOverwatchPayload Prepared,
    string EffectActionId,
    int EffectAbilityId,
    IReadOnlyList<long>? ReservedActionInstanceIds = null) : DclStatePayload(SchemaId);

internal sealed record DclProtectionStatePayload(
    string SchemaId,
    DclProtectionPayload Prepared) : DclStatePayload(SchemaId);

internal sealed record DclBulwarkStatePayload(
    string SchemaId,
    DclBulwarkPayload Prepared) : DclStatePayload(SchemaId);

internal sealed record DclCanonicalOverwatchTriggerResult(
    DclPreparedTriggerResult Trigger,
    DclActionProfile? EffectAction,
    DclAbilityBinding? EffectBinding,
    long? ReservedActionInstanceId,
    IReadOnlyList<long> RemovedStateInstanceIds);

internal sealed record DclCanonicalOverwatchEffectBinding(
    DclAbilityBinding Ability,
    DclActionProfile Action);

internal sealed record DclCanonicalProtectionTriggerResult(
    DclPreparedTriggerResult Trigger,
    DclUnitKey? RedirectTarget,
    IReadOnlyList<long> RemovedStateInstanceIds);

internal sealed record DclCanonicalProtectionRedirectCandidate(
    long StateInstanceId,
    DclUnitKey Attacker,
    DclUnitKey DeclaredTarget,
    DclUnitKey Protector,
    DclDelivery Delivery,
    bool RangeOrAdjacencyLegal,
    bool ProtectorCanReceiveHit);

internal sealed record DclCanonicalProtectionRedirectResolution(
    DclCanonicalProtectionRedirectCandidate Candidate,
    DclCanonicalProtectionTriggerResult Trigger,
    DclUnitKey FinalTarget);

internal sealed record DclCanonicalProtectionRedirectPlan(
    DclCanonicalProtectionRedirectCandidate Candidate,
    DclStateInstance ExpectedState,
    DclCanonicalProtectionRedirectResolution Resolution);

internal sealed record DclCanonicalOverwatchFinalTileCandidate(
    long StateInstanceId,
    DclUnitKey Source,
    DclBattleTile SourceTile,
    string WeaponSlot,
    bool WeaponStillValid,
    bool SourceCanExecute,
    bool TargetEligible,
    bool RangeLegal,
    bool VerticalLegal,
    bool TrajectoryLegal,
    bool TriggerConditionSatisfied);

internal sealed record DclCanonicalOverwatchFinalTileResolution(
    DclCanonicalOverwatchFinalTileCandidate Candidate,
    DclCanonicalOverwatchTriggerResult Trigger,
    DclActionDeclaration? ReservedAction);

internal sealed record DclCanonicalOverwatchFinalTileBatch(
    DclFinalTileSnapshot Movement,
    DclUnitKey Mover,
    DclBattleTile FinalTile,
    IReadOnlyList<DclCanonicalOverwatchFinalTileResolution> Resolutions);

internal enum DclCanonicalPreparedActionStage
{
    Reserved,
    Published,
}

internal sealed record DclCanonicalPreparedActionTicket(
    long MovementSequence,
    int NativeOrder,
    long StateInstanceId,
    DclAbilityBinding EffectBinding,
    DclActionDeclaration Declaration,
    DclCanonicalPreparedActionStage Stage,
    DclCanonicalNativeActionApplication? Application);

internal sealed class DclCanonicalPreparedStateRuntime
{
    private readonly DclCanonicalRuntimeCatalog _runtime;
    private readonly DclStateRegistry _states;

    public DclCanonicalPreparedStateRuntime(
        DclCanonicalRuntimeCatalog runtime,
        DclStateRegistry states)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(states);
        _runtime = runtime;
        _states = states;
    }

    public void Attach(long instanceId)
    {
        DclStateInstance instance = RequireInstance(instanceId);
        if (instance.Payload is not (DclOverwatchStatePayload or DclProtectionStatePayload or DclBulwarkStatePayload))
            throw new ArgumentException("The instance is not a prepared state.", nameof(instanceId));
        ValidatePreparedDefinition(
            instance.Definition,
            instance.Payload,
            instance.RemainingUses,
            applicationBoundary: false);
        if (instance.Payload is DclProtectionStatePayload protection)
            ValidateProtectionBinding(instance.Target, instance.Source, protection);
    }

    public void ValidateApplication(DclStateApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        ValidatePreparedDefinition(
            application.Definition,
            application.Payload,
            application.DurationUnits,
            applicationBoundary: true);
        if (application.Payload is DclProtectionStatePayload protection)
            ValidateProtectionBinding(application.Target, application.Source, protection);
    }

    private void ValidatePreparedDefinition(
        DclStateDefinition definition,
        DclStatePayload statePayload,
        int? resolvedUses,
        bool applicationBoundary)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(statePayload);
        switch (statePayload)
        {
            case DclOverwatchStatePayload payload:
                if (definition.Duration.Clock != DclStateDurationClock.UsesOrTriggers ||
                    resolvedUses is not > 0 ||
                    applicationBoundary && resolvedUses != payload.Prepared.RemainingTriggers ||
                    !applicationBoundary && resolvedUses > payload.Prepared.RemainingTriggers)
                    throw new InvalidOperationException("Overwatch state duration and payload trigger counts must agree.");
                if (applicationBoundary)
                {
                    if (payload.ReservedActionInstanceIds is not null)
                        throw new InvalidOperationException(
                            "Overwatch ActionInstance reservations are battle-owned and cannot be supplied by state materialization.");
                }
                else
                {
                    long[] reservations = payload.ReservedActionInstanceIds?.ToArray() ??
                        throw new InvalidOperationException("A materialized Overwatch state requires its declared Action reservations.");
                    if (reservations.Length != payload.Prepared.RemainingTriggers ||
                        reservations.Any(id => id <= 0) || reservations.Distinct().Count() != reservations.Length)
                        throw new InvalidOperationException(
                            "Overwatch requires one unique positive declared ActionInstance reservation per authored trigger.");
                }
                (DclAbilityBinding binding, DclActionProfile action) =
                    _runtime.ResolveAbility(payload.EffectAbilityId);
                if (!StringComparer.Ordinal.Equals(binding.ActionId, payload.EffectActionId) ||
                    !StringComparer.Ordinal.Equals(action.ActionId, payload.EffectActionId))
                    throw new InvalidOperationException(
                        "Overwatch effect ability is not bound to its declared effect Action.");
                _ = new DclOverwatchState(payload.Prepared);
                break;
            case DclProtectionStatePayload payload:
                if (definition.Duration.Clock != DclStateDurationClock.UsesOrTriggers ||
                    applicationBoundary && resolvedUses != payload.Prepared.RemainingIntercepts ||
                    !applicationBoundary && resolvedUses > payload.Prepared.RemainingIntercepts)
                    throw new InvalidOperationException("Protection state duration and payload intercept counts must agree.");
                if (payload.Prepared.ProtectedUnit.BattleGeneration != _states.BattleGeneration)
                    throw new InvalidOperationException("Protection payload belongs to a different battle generation.");
                _ = new DclProtectionState(payload.Prepared);
                break;
            case DclBulwarkStatePayload:
                if (definition.Duration.Clock != DclStateDurationClock.ExplicitCommand)
                    throw new InvalidOperationException("Bulwark requires explicit cancellation ownership.");
                break;
            default:
                return;
        }
    }

    public DclCanonicalOverwatchTriggerResult TryOverwatch(
        long instanceId,
        DclOverwatchTriggerContext context)
    {
        DclStateInstance instance = RequireInstance(instanceId);
        if (instance.Payload is not DclOverwatchStatePayload payload || instance.RemainingUses is not > 0)
            throw new ArgumentException("The instance is not a live Overwatch state.", nameof(instanceId));
        ValidatePreparedDefinition(
            instance.Definition,
            payload,
            instance.RemainingUses,
            applicationBoundary: false);
        // The registry is the sole persistent use owner. Rebuilding this small controller from the
        // current instance avoids a second mutable lifetime that could survive replacement,
        // checkpoint restore, KO cleanup, or expiry after the state itself has gone away.
        var state = new DclOverwatchState(payload.Prepared with
        {
            RemainingTriggers = instance.RemainingUses.Value,
        });
        DclPreparedTriggerResult trigger = state.TryTrigger(context);
        long? reservedActionInstanceId = null;
        if (trigger.Triggered)
        {
            int reservationIndex = checked(payload.Prepared.RemainingTriggers - instance.RemainingUses.Value);
            reservedActionInstanceId = payload.ReservedActionInstanceIds![reservationIndex];
        }
        IReadOnlyList<long> removed = SyncFiniteState(instanceId, trigger);
        DclAbilityBinding? effectBinding = null;
        DclActionProfile? effectAction = null;
        if (trigger.Triggered)
            (effectBinding, effectAction) = _runtime.ResolveAbility(payload.EffectAbilityId);
        return new DclCanonicalOverwatchTriggerResult(
            trigger,
            effectAction,
            effectBinding,
            reservedActionInstanceId,
            removed);
    }

    public DclCanonicalOverwatchEffectBinding ValidateOverwatchFinalTileCandidate(
        DclCanonicalOverwatchFinalTileCandidate candidate,
        DclUnitKey mover,
        DclFinalTileSnapshot movement)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        DclFinalTileValidation movementValidation = DclFinalTileEvent.Validate(movement);
        if (!movementValidation.Accepted)
            throw new ArgumentException(
                $"Overwatch requires one accepted final-tile event: {movementValidation.Reason}.",
                nameof(movement));
        if (movement.BattleGeneration != _states.BattleGeneration ||
            mover.BattleGeneration != _states.BattleGeneration ||
            mover.UnitSlot != movement.MoverTableIndex || mover.CharacterId != movement.MoverCharId)
            throw new ArgumentException("Overwatch mover identity does not match the final-tile event.", nameof(mover));
        DclStateInstance instance = RequireInstance(candidate.StateInstanceId);
        if (instance.Payload is not DclOverwatchStatePayload payload || instance.RemainingUses is not > 0)
            throw new ArgumentException("The candidate is not a live Overwatch state.", nameof(candidate));
        if (instance.Target != candidate.Source ||
            instance.Source is { } boundSource && boundSource != candidate.Source ||
            candidate.Source.BattleGeneration != _states.BattleGeneration)
            throw new ArgumentException("Overwatch source identity does not own the prepared state.", nameof(candidate));
        if (string.IsNullOrWhiteSpace(candidate.WeaponSlot) ||
            !StringComparer.Ordinal.Equals(candidate.WeaponSlot, payload.Prepared.WeaponSlot))
            throw new ArgumentException("Overwatch weapon slot does not match its prepared payload.", nameof(candidate));
        if (candidate.SourceTile.X is < 0 or > 255 || candidate.SourceTile.Y is < 0 or > 255 ||
            candidate.SourceTile.Layer is not (0 or 1))
            throw new ArgumentException("Overwatch source tile is outside the native map domain.", nameof(candidate));
        ValidatePreparedDefinition(
            instance.Definition,
            payload,
            instance.RemainingUses,
            applicationBoundary: false);
        (DclAbilityBinding binding, DclActionProfile action) =
            _runtime.ResolveAbility(payload.EffectAbilityId);
        if (!StringComparer.Ordinal.Equals(binding.ActionId, payload.EffectActionId) ||
            binding.ProfileRevision != action.ProfileRevision)
            throw new InvalidOperationException(
                "Overwatch effect ability lost its exact Action/revision binding.");
        if (action.TargetProfile.TargetMode != DclTargetMode.Unit ||
            action.TimingProfile.BaseCastCt != 0 || action.TimingProfile.CastCtModifier != 0 ||
            action.TimingProfile.ConcentrationRequired || action.ResourceProfile.BaseMpCost != 0)
            throw new InvalidOperationException(
                "Overwatch effect must be an immediate zero-MP unit-target Action; setup owns its earlier commitment.");
        return new DclCanonicalOverwatchEffectBinding(binding, action);
    }

    public DclCanonicalProtectionTriggerResult TryProtection(
        long instanceId,
        DclProtectionTriggerContext context)
    {
        DclStateInstance instance = RequireInstance(instanceId);
        if (instance.Payload is not DclProtectionStatePayload payload || instance.RemainingUses is not > 0)
            throw new ArgumentException("The instance is not a live Cover/Bodyguard state.", nameof(instanceId));
        ValidatePreparedDefinition(
            instance.Definition,
            payload,
            instance.RemainingUses,
            applicationBoundary: false);
        var state = new DclProtectionState(payload.Prepared with
        {
            RemainingIntercepts = instance.RemainingUses.Value,
        });
        DclPreparedTriggerResult trigger = state.TryIntercept(context);
        IReadOnlyList<long> removed = SyncFiniteState(instanceId, trigger);
        return new DclCanonicalProtectionTriggerResult(
            trigger,
            trigger.Triggered ? instance.Target : null,
            removed);
    }

    public void ValidateProtectionRedirectCandidate(
        DclCanonicalProtectionRedirectCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        DclStateInstance instance = RequireInstance(candidate.StateInstanceId);
        if (instance.Payload is not DclProtectionStatePayload payload || instance.RemainingUses is not > 0)
            throw new ArgumentException("The candidate is not a live Cover/Bodyguard state.", nameof(candidate));
        ValidatePreparedDefinition(
            instance.Definition,
            payload,
            instance.RemainingUses,
            applicationBoundary: false);
        ValidateProtectionBinding(instance.Target, instance.Source, payload);
        if (instance.Target != candidate.Protector ||
            payload.Prepared.ProtectedUnit != candidate.DeclaredTarget ||
            candidate.Attacker.BattleGeneration != _states.BattleGeneration ||
            candidate.DeclaredTarget.BattleGeneration != _states.BattleGeneration ||
            candidate.Protector.BattleGeneration != _states.BattleGeneration)
            throw new ArgumentException(
                "Protection candidate identities do not match the source-owned persistent link.",
                nameof(candidate));
    }

    public DclCanonicalProtectionRedirectPlan PlanProtectionRedirect(
        DclCanonicalProtectionRedirectCandidate candidate)
    {
        ValidateProtectionRedirectCandidate(candidate);
        DclStateInstance instance = RequireInstance(candidate.StateInstanceId);
        DclProtectionStatePayload payload = (DclProtectionStatePayload)instance.Payload;
        var state = new DclProtectionState(payload.Prepared with
        {
            RemainingIntercepts = instance.RemainingUses!.Value,
        });
        DclPreparedTriggerResult trigger = state.TryIntercept(new DclProtectionTriggerContext(
            SourceValid: true,
            ProtectedUnitValid: true,
            candidate.RangeOrAdjacencyLegal,
            candidate.Delivery,
            SourceCanReceiveHit: candidate.ProtectorCanReceiveHit));
        var plannedTrigger = new DclCanonicalProtectionTriggerResult(
            trigger,
            trigger.Triggered ? candidate.Protector : null,
            trigger.StateRemains ? [] : [candidate.StateInstanceId]);
        return new DclCanonicalProtectionRedirectPlan(
            candidate,
            instance,
            new DclCanonicalProtectionRedirectResolution(
                candidate,
                plannedTrigger,
                trigger.Triggered ? candidate.Protector : candidate.DeclaredTarget));
    }

    public DclCanonicalProtectionRedirectResolution CommitProtectionRedirect(
        DclCanonicalProtectionRedirectPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ValidateProtectionRedirectCandidate(plan.Candidate);
        DclStateInstance current = RequireInstance(plan.Candidate.StateInstanceId);
        if (current != plan.ExpectedState)
            throw new InvalidOperationException("Protection redirect plan is stale before intercept consumption.");
        DclCanonicalProtectionTriggerResult committed = TryProtection(
            plan.Candidate.StateInstanceId,
            new DclProtectionTriggerContext(
                SourceValid: true,
                ProtectedUnitValid: true,
                plan.Candidate.RangeOrAdjacencyLegal,
                plan.Candidate.Delivery,
                SourceCanReceiveHit: plan.Candidate.ProtectorCanReceiveHit));
        DclCanonicalProtectionRedirectResolution expected = plan.Resolution;
        DclUnitKey finalTarget = committed.Trigger.Triggered
            ? committed.RedirectTarget ?? throw new InvalidOperationException(
                "Committed protection redirect lost its protector identity.")
            : plan.Candidate.DeclaredTarget;
        if (committed.Trigger != expected.Trigger.Trigger ||
            committed.RedirectTarget != expected.Trigger.RedirectTarget ||
            !committed.RemovedStateInstanceIds.SequenceEqual(expected.Trigger.RemovedStateInstanceIds) ||
            finalTarget != expected.FinalTarget)
            throw new InvalidOperationException("Protection redirect commit diverged from its immutable plan.");
        return new DclCanonicalProtectionRedirectResolution(plan.Candidate, committed, finalTarget);
    }

    public IReadOnlyList<long> ApplyBulwarkCancellation(
        long instanceId,
        bool movedVoluntarily,
        bool knockedDown,
        bool stanceCancellingStun,
        bool ko)
    {
        DclStateInstance instance = RequireInstance(instanceId);
        if (instance.Payload is not DclBulwarkStatePayload)
            throw new ArgumentException("The instance is not a Bulwark state.", nameof(instanceId));
        if (!DclBulwarkRules.Cancels(movedVoluntarily, knockedDown, stanceCancellingStun, ko))
            return [];
        return _states.RemoveInstances([instanceId]);
    }

    private IReadOnlyList<long> SyncFiniteState(long instanceId, DclPreparedTriggerResult trigger)
    {
        if (trigger.Triggered)
        {
            if (!_states.ConsumeUse(instanceId))
                throw new InvalidOperationException("Prepared controller fired without a matching persistent use.");
        }
        else if (!trigger.StateRemains)
        {
            return _states.RemoveInstances([instanceId]);
        }

        if (!trigger.StateRemains)
            return [instanceId];
        if (!_states.TryGet(instanceId, out DclStateInstance current) ||
            current.RemainingUses != trigger.RemainingUses)
            throw new InvalidOperationException("Prepared controller and persistent remaining-use count diverged.");
        return [];
    }

    private static void ValidateProtectionBinding(
        DclUnitKey owner,
        DclUnitKey? source,
        DclProtectionStatePayload payload)
    {
        if (source != owner || payload.Prepared.ProtectedUnit == owner)
            throw new InvalidOperationException(
                "Cover/Bodyguard must be source-owned by the protector and bound to a different protected unit.");
    }

    private DclStateInstance RequireInstance(long instanceId)
        => _states.TryGet(instanceId, out DclStateInstance instance)
            ? instance
            : throw new KeyNotFoundException($"Prepared state instance {instanceId} is not present.");
}
