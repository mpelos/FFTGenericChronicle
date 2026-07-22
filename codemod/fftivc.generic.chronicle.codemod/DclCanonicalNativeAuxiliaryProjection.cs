namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Projects canonical non-damage action families into the same ordered native Action ledger used
/// by numeric carriers. State, CT, and KO lifecycle effects remain explicit beside empty or HP
/// channels so later native hooks cannot infer them from presentation flags.
/// </summary>
internal static class DclCanonicalNativeAuxiliaryProjector
{
    public static DclCanonicalNativeMultiCarrierProjection ProjectStatus(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalStatusExecutionResult result)
    {
        DclCanonicalNativeAuxiliaryEffects auxiliary =
            ProjectStateApplications(result.StateApplication is null ? [] : [result.StateApplication]) ??
            Effects([], [], ctCredit: null, clearKo: false);
        return ProjectSingle(
            runtime,
            abilityId,
            result.Declaration,
            result.Outcome,
            result.Payment,
            result.Transaction,
            result.Reactions,
            result.Delivery?.Outcome,
            result.StateApplication is not null,
            new DclCanonicalNativeNumericChannels(0, 0, 0, 0),
            auxiliary);
    }

    public static DclCanonicalNativeMultiCarrierProjection ProjectStatusRemoval(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalStatusRemovalExecutionResult result)
    {
        (_, DclActionProfile profile) = runtime.ResolveAbility(abilityId);
        string stateKind = profile.Effects.Single().ReferencedStateKind ??
            throw new InvalidOperationException("Named StatusRemoval lost its state kind during native projection.");
        DclCanonicalNativeAuxiliaryEffects auxiliary = Effects(
            added: [],
            result.RemovedInstanceIds.Select(id => Mutation(id, stateKind)).ToArray(),
            ctCredit: null,
            clearKo: false);
        return ProjectSingle(
            runtime,
            abilityId,
            result.Declaration,
            result.Outcome,
            result.Payment,
            result.Transaction,
            result.Reactions,
            result.Delivery?.Outcome,
            result.Delivery?.Delivered == true,
            new DclCanonicalNativeNumericChannels(0, 0, 0, 0),
            auxiliary);
    }

    public static DclCanonicalNativeMultiCarrierProjection ProjectDispel(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalDispelExecutionResult result)
    {
        DclCanonicalNativeStateMutationProjection[] removed = result.Instances
            .Where(instance => instance.Removed)
            .Select(instance => Mutation(instance.InstanceId, instance.StateKind))
            .ToArray();
        DclMagicDeliveryOutcome? outcome = result.CastingGate is not { } gate
            ? null
            : !gate.BaseSucceeded ? DclMagicDeliveryOutcome.BaseFailure
            : !gate.TargetSucceeded ? DclMagicDeliveryOutcome.TargetFailure
            : removed.Length > 0 ? DclMagicDeliveryOutcome.Delivered
            : DclMagicDeliveryOutcome.Resisted;
        return ProjectSingle(
            runtime,
            abilityId,
            result.Declaration,
            result.Outcome,
            result.Payment,
            result.Transaction,
            result.Reactions,
            outcome,
            removed.Length > 0,
            new DclCanonicalNativeNumericChannels(0, 0, 0, 0),
            Effects([], removed, ctCredit: null, clearKo: false));
    }

    public static DclCanonicalNativeMultiCarrierProjection ProjectQuick(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalQuickExecutionResult result)
    {
        bool delivered = result.Delivery?.Delivered == true && result.QuickLock is not null;
        return ProjectSingle(
            runtime,
            abilityId,
            result.Declaration,
            result.Outcome,
            result.Payment,
            result.Transaction,
            result.Reactions,
            result.Delivery?.Outcome,
            delivered,
            new DclCanonicalNativeNumericChannels(0, 0, 0, 0),
            Effects(
                Added(result.QuickLock),
                removed: [],
                delivered ? result.CtGrant : null,
                clearKo: false));
    }

    public static DclCanonicalNativeMultiCarrierProjection ProjectForcedMovement(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalForcedMovementExecutionResult result)
    {
        DclCanonicalNativeAuxiliaryEffects auxiliary = Merge(
                ProjectForcedMovement(result.Movement),
                ProjectAimLifecycle(result.AimCancellation)) ??
            Effects([], [], ctCredit: null, clearKo: false);
        return ProjectSingle(
            runtime,
            abilityId,
            result.Declaration,
            result.Outcome,
            result.Payment,
            result.Transaction,
            result.Reactions,
            result.Delivery?.Outcome,
            result.Delivery?.Delivered == true,
            new DclCanonicalNativeNumericChannels(0, 0, 0, 0),
            auxiliary);
    }

    public static DclCanonicalNativeMultiCarrierProjection ProjectRevive(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalReviveExecutionResult result)
    {
        int hpCredit = result.Revive?.AppliedHpCredit ?? 0;
        bool clearKo = result.Revive?.ClearKoAfterPositiveCredit == true;
        bool stored = result.StoredReraise is not null;
        bool delivered = hpCredit > 0 || stored;
        return ProjectSingle(
            runtime,
            abilityId,
            result.Declaration,
            result.Outcome,
            result.Payment,
            result.Transaction,
            result.Reactions,
            result.Delivery?.Outcome,
            delivered,
            new DclCanonicalNativeNumericChannels(0, hpCredit, 0, 0),
            Effects(
                Added(result.StoredReraise),
                removed: [],
                ctCredit: null,
                clearKo));
    }

    public static DclCanonicalNativeMultiCarrierProjection ProjectReraiseTrigger(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalReraiseTriggerResult result)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(result);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(result.AbilityId);
        if (binding.CarrierKind != DclNativeCarrierKind.LifecycleTransaction ||
            profile.ReviveProfile?.Mode != DclReviveMode.StoredReraise ||
            result.ActionInstanceId <= 0 || !result.Target.IsValid || result.Revive.StoreReraise ||
            result.Channels.HpCredit != result.Revive.AppliedHpCredit ||
            result.ClearKoAfterPositiveCredit != result.Revive.ClearKoAfterPositiveCredit ||
            result.OpensReactionWindow)
            throw new ArgumentException("Stored Reraise trigger does not match its lifecycle binding or HP/KO plan.", nameof(result));
        var auxiliary = Effects(
            added: [],
            result.ConsumedStateInstanceIds.Select(id => Mutation(id, result.ConsumedStateKind)).ToArray(),
            ctCredit: null,
            result.ClearKoAfterPositiveCredit);
        var strike = new DclCanonicalNativeStrikeProjection(
            result.Target,
            StrikeIndex: 0,
            DclMagicDeliveryOutcome.Delivered,
            PhysicalOutcome: null,
            Delivered: result.Channels.HpCredit > 0,
            result.Channels,
            MagicalRoute: null,
            InjuryConsequences: null,
            TargetKoAfterStrike: false,
            KoShortCircuited: false,
            auxiliary);
        return new DclCanonicalNativeMultiCarrierProjection(
            result.ActionInstanceId,
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.CarrierKind,
            binding.RewritePolicy,
            [strike],
            ResourcePayment: null,
            ReactionWindowOpened: false,
            ResourceFailed: false);
    }

    private static DclCanonicalNativeMultiCarrierProjection ProjectSingle(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclActionDeclaration declaration,
        DclCastingOutcome outcome,
        DclResourcePayment payment,
        DclActionTransaction transaction,
        DclCanonicalReactionWindowResult? reactions,
        DclMagicDeliveryOutcome? deliveryOutcome,
        bool delivered,
        DclCanonicalNativeNumericChannels channels,
        DclCanonicalNativeAuxiliaryEffects auxiliary)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(transaction);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(abilityId);
        if (profile.ActionId != declaration.ActionId || profile.ProfileRevision != declaration.ProfileRevision ||
            transaction.Declaration != declaration)
            throw new ArgumentException("Auxiliary result does not match its normalized native binding.", nameof(declaration));
        if (binding.RewritePolicy is DclCarrierRewritePolicy.Unknown or DclCarrierRewritePolicy.PreserveNativeSpecial)
            throw new InvalidOperationException("Auxiliary canonical execution requires an owned native rewrite carrier.");
        if (!payment.Legal)
            throw new ArgumentException("Illegal resource settlement cannot reach auxiliary native projection.", nameof(payment));

        bool resourceFailed = outcome == DclCastingOutcome.ResourceFailure;
        if (resourceFailed)
        {
            if (transaction.Stage != DclActionTransactionStage.ResourceFailed || delivered ||
                reactions is not null || !channels.IsEmpty || !auxiliary.IsEmpty ||
                payment is not { CostDue: 0, MpPaid: 0, HpPaid: 0 })
                throw new ArgumentException("Auxiliary ResourceFailure must remain empty through native projection.", nameof(transaction));
            return new DclCanonicalNativeMultiCarrierProjection(
                declaration.ActionInstanceId,
                binding.AbilityId,
                binding.ActionId,
                binding.ProfileRevision,
                binding.CarrierKind,
                binding.RewritePolicy,
                Strikes: [],
                ResourcePayment: null,
                ReactionWindowOpened: false,
                ResourceFailed: true);
        }
        if (transaction.Stage != DclActionTransactionStage.Settled)
            throw new ArgumentException("Auxiliary native projection requires one settled outer transaction.", nameof(transaction));
        DclUnitKey target = declaration.TrackedTarget ??
            throw new ArgumentException("Auxiliary unit-targeted projection lost its tracked target.", nameof(declaration));
        var strike = new DclCanonicalNativeStrikeProjection(
            target,
            StrikeIndex: 0,
            deliveryOutcome,
            PhysicalOutcome: null,
            delivered,
            channels,
            MagicalRoute: null,
            InjuryConsequences: null,
            TargetKoAfterStrike: false,
            KoShortCircuited: false,
            auxiliary.IsEmpty ? null : auxiliary);
        return new DclCanonicalNativeMultiCarrierProjection(
            declaration.ActionInstanceId,
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.CarrierKind,
            binding.RewritePolicy,
            [strike],
            DclCanonicalNativeCarrierProjector.BuildPayment(declaration.Source, payment),
            transaction.ReactionWindowOpened,
            ResourceFailed: false,
            Reactions: reactions);
    }

    private static DclCanonicalNativeAuxiliaryEffects Effects(
        IReadOnlyList<DclCanonicalNativeStateMutationProjection> added,
        IReadOnlyList<DclCanonicalNativeStateMutationProjection> removed,
        DclRational? ctCredit,
        bool clearKo,
        DclCanonicalForcedMovementResult? forcedMovement = null)
    {
        if (added.Any(mutation => mutation.InstanceId <= 0 || string.IsNullOrWhiteSpace(mutation.StateKind)) ||
            removed.Any(mutation => mutation.InstanceId <= 0 || string.IsNullOrWhiteSpace(mutation.StateKind)) ||
            ctCredit is { } ct && ct <= DclRational.FromInteger(0))
            throw new ArgumentException("Auxiliary state/CT projection contains an invalid semantic effect.");
        return new DclCanonicalNativeAuxiliaryEffects(added, removed, ctCredit, clearKo, forcedMovement);
    }

    private static IReadOnlyList<DclCanonicalNativeStateMutationProjection> Added(
        DclStateApplicationResult? application)
        => application is null || application.Outcome == DclStateApplicationOutcome.WeakerRejected
            ? []
            : [Mutation(application.Instance.InstanceId, application.Instance.Kind)];

    private static DclCanonicalNativeStateMutationProjection Mutation(long instanceId, string stateKind)
        => new(instanceId, stateKind);

    internal static DclCanonicalNativeAuxiliaryEffects? ProjectStateApplications(
        IEnumerable<DclStateApplicationResult?> applications)
    {
        ArgumentNullException.ThrowIfNull(applications);
        DclStateApplicationResult[] materialized = applications
            .Where(application => application is not null)
            .Cast<DclStateApplicationResult>()
            .ToArray();
        DclCanonicalNativeStateMutationProjection[] added = materialized
            .Where(application => application.Outcome != DclStateApplicationOutcome.WeakerRejected)
            .Select(application => Mutation(application.Instance.InstanceId, application.Instance.Kind))
            .ToArray();
        DclCanonicalNativeStateMutationProjection[] removed = materialized
            .SelectMany(application => application.RemovedInstances)
            .Select(instance => Mutation(instance.InstanceId, instance.Kind))
            .ToArray();
        DclCanonicalNativeAuxiliaryEffects effects = Effects(
            added,
            removed,
            ctCredit: null,
            clearKo: false);
        return effects.IsEmpty ? null : effects;
    }

    internal static DclCanonicalNativeAuxiliaryEffects? ProjectInjuryStates(
        DclCanonicalInjuryStateCommitResult? injuryStates)
        => injuryStates is null
            ? null
            : Merge(
                ProjectStateApplications(
                    new DclStateApplicationResult?[]
                    {
                        injuryStates.Shock,
                        injuryStates.Stun,
                        injuryStates.KnockedDown,
                    }),
                injuryStates.RemovedOnTargetKo is not { Count: > 0 } removed
                    ? null
                    : Effects(
                        added: [],
                        removed.Select(instance => Mutation(instance.InstanceId, instance.Kind)).ToArray(),
                        ctCredit: null,
                        clearKo: false));

    internal static DclCanonicalNativeAuxiliaryEffects? ProjectAimLifecycle(
        DclCanonicalAimLifecycleResult? aim)
        => aim is { HadAim: true, Retained: false, InstanceId: { } instanceId }
            ? Effects(
                added: [],
                removed: [Mutation(instanceId, "aim")],
                ctCredit: null,
                clearKo: false)
            : null;

    internal static DclCanonicalNativeAuxiliaryEffects? ProjectForcedMovement(
        DclCanonicalForcedMovementResult? movement)
        => movement is { MovedTiles: > 0, SuppressedByTargetKo: false }
            ? Effects([], [], ctCredit: null, clearKo: false, forcedMovement: movement)
            : null;

    internal static DclCanonicalNativeAuxiliaryEffects? ProjectRemovedStates(
        IEnumerable<DclStateInstance> removedStates)
    {
        ArgumentNullException.ThrowIfNull(removedStates);
        DclStateInstance[] exact = removedStates
            .DistinctBy(instance => instance.InstanceId)
            .OrderBy(instance => instance.InstanceId)
            .ToArray();
        if (exact.Length == 0) return null;
        return Effects(
            added: [],
            exact.Select(instance => Mutation(instance.InstanceId, instance.Kind)).ToArray(),
            ctCredit: null,
            clearKo: false);
    }

    internal static DclCanonicalNativeAuxiliaryEffects? Merge(
        params DclCanonicalNativeAuxiliaryEffects?[] effects)
    {
        ArgumentNullException.ThrowIfNull(effects);
        DclCanonicalNativeAuxiliaryEffects[] materialized = effects
            .Where(effect => effect is not null && !effect.IsEmpty)
            .Cast<DclCanonicalNativeAuxiliaryEffects>()
            .ToArray();
        if (materialized.Length == 0) return null;
        DclRational? ctCredit = null;
        DclCanonicalForcedMovementResult? forcedMovement = null;
        foreach (DclCanonicalNativeAuxiliaryEffects effect in materialized)
        {
            if (effect.CtCredit is not null)
                ctCredit = (ctCredit ?? DclRational.FromInteger(0)) + effect.CtCredit.Value;
            if (effect.ForcedMovement is not null)
            {
                if (forcedMovement is not null)
                    throw new InvalidOperationException("One target/Strike cannot project two forced movements.");
                forcedMovement = effect.ForcedMovement;
            }
        }
        return Effects(
            materialized.SelectMany(effect => effect.AddedStates).ToArray(),
            materialized.SelectMany(effect => effect.RemovedStates).ToArray(),
            ctCredit,
            materialized.Any(effect => effect.ClearKoAfterPositiveHpCredit),
            forcedMovement);
    }
}
