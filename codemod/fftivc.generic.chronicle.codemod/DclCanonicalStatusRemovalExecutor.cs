namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalStatusRemovalExecutionInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int? SharedCasterRoll,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclStateRegistry StateRegistry,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    DclCanonicalReactionWindowRequest? ReactionWindow = null);

internal sealed record DclCanonicalStatusRemovalExecutionResult(
    DclActionDeclaration Declaration,
    DclCastingOutcome Outcome,
    DclMagicDeliveryResult? Delivery,
    DclResourcePayment Payment,
    IReadOnlyList<long> RemovedInstanceIds,
    DclActionTransaction Transaction,
    DclCanonicalReactionWindowResult? Reactions = null);

internal static class DclCanonicalStatusRemovalExecutor
{
    public static DclCanonicalStatusRemovalExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalStatusRemovalExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.StatusPacket or
            DclNativeCarrierKind.ConditionalStatusProducer or DclNativeCarrierKind.SingleResult))
            throw new InvalidOperationException("The canonical status-removal executor received an incompatible native carrier.");
        if (profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.DeliveryProfile.Delivery != DclDelivery.Beneficial || profile.MagnitudeProfile is not null ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            {
                Kind: DclEffectKind.StatusRemoval,
                Role: DclEffectRole.Carrier,
                ReferencedStateKind: { } stateKind,
            })
            throw new InvalidOperationException("This vertical requires one Beneficial named StatusRemoval Carrier without numeric magnitude.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Status-removal declaration does not match the bound profile revision.", nameof(input));
        if (input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit ||
            input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration)
            throw new ArgumentException("Status-removal target/declaration/registry identities must agree.", nameof(input));
        DclCanonicalBattleRuntime? executionBattle = input.ExecutionBattle;
        if (executionBattle is not null &&
            (!ReferenceEquals(executionBattle.Catalog, runtime) ||
             !ReferenceEquals(executionBattle.States, input.StateRegistry) ||
             executionBattle.BattleGeneration != input.DeclarationRequest.Caster.BattleGeneration ||
             !executionBattle.TryGetObservedUnit(input.DeclarationRequest.Caster.UnitSlot, out DclUnitKey observedSource) ||
             observedSource != input.DeclarationRequest.Caster ||
             !executionBattle.TryGetObservedUnit(input.Target.Unit.UnitSlot, out DclUnitKey observedTarget) ||
             observedTarget != input.Target.Unit || input.SharedCasterRoll is not null))
            throw new ArgumentException(
                "Confirmed status-removal execution requires battle-owned state/current identities and no pre-supplied caster roll.",
                nameof(input));
        if (executionBattle is not null)
            DclCanonicalReactionWindow.RequireConfirmedRequest(runtime, executionBattle, input.ReactionWindow);

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, input.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical status-removal declaration failed: {string.Join(",", attempt.Failures)}");
        if (input.ReactionWindow is { } removalReactionWindow)
            DclCanonicalReactionWindow.Preflight(attempt.Declaration, removalReactionWindow);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
        {
            if (input.SharedCasterRoll is not null)
                throw new ArgumentException("ResourceFailure occurs before the status-removal caster draw.", nameof(input));
            DclResourcePayment failedPayment = DclMagicResources.Settle(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment,
                DclCastingOutcome.ResourceFailure);
            var failed = new DclActionTransaction(attempt.Declaration, profile);
            failed.FailResourceCommitment();
            return new DclCanonicalStatusRemovalExecutionResult(
                attempt.Declaration,
                DclCastingOutcome.ResourceFailure,
                Delivery: null,
                failedPayment,
                RemovedInstanceIds: [],
                failed);
        }

        int sharedCasterRoll = input.SharedCasterRoll ?? executionBattle?.ExecutionRandom.Roll3D6(
            executionBattle.RollIdentity(
                input.ActionInstanceId,
                input.DeclarationRequest.Caster,
                target: null,
                strikeIndex: 0,
                DclRollSite.Casting,
                drawIndex: 0)) ??
            throw new ArgumentNullException(nameof(input), "A payable status removal requires its caster draw.");
        DclMagicDeliveryResult delivery = DclSpellResolution.ResolveBeneficial(
            sharedCasterRoll,
            input.BaseSpellScore,
            input.TargetSpellScore);
        DclResourcePayment payment = DclMagicResources.Settle(
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment,
            delivery.Gate.BaseOutcome);
        if (!payment.Legal)
            throw new InvalidOperationException("A prechecked status-removal cost became illegal during settlement.");

        bool statePresent = delivery.Delivered && input.StateRegistry.Instances.Any(instance =>
            instance.Target == input.Target.Unit && StringComparer.Ordinal.Equals(instance.Kind, stateKind));
        IReadOnlyList<long> removed = [];
        DclCanonicalTransactionResult committed = DclCanonicalTransactionExecutor.Commit(
            attempt.Declaration,
            profile,
            [new DclTargetResolutionSnapshot(
                input.Target.Unit,
                input.Target.CurrentHp,
                input.Target.CombatStateRevision,
                input.Target.DefenseResources)],
            [new DclCanonicalResolvedStrike(
                input.Target.Unit,
                StrikeIndex: 0,
                AppliedEffectIndexes: statePresent ? [0] : [],
                TargetKoAfterStrike: false)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                if (statePresent) removed = input.StateRegistry.RemoveKind(input.Target.Unit, stateKind);
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: input.ExecutionBattle?.States);
        return new DclCanonicalStatusRemovalExecutionResult(
            attempt.Declaration,
            delivery.Gate.BaseOutcome,
            delivery,
            payment,
            removed,
            committed.Transaction,
            Reactions: committed.Reactions);
    }
}
