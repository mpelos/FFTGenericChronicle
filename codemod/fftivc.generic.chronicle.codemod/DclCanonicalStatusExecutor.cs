namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalStateMaterialization(
    bool BindSource,
    long AppliedBeforeTurnSerial,
    long? FirstEligibleTargetTurnSerial,
    long? FirstEligibleSourceTurnSerial,
    int? DurationUnits,
    int? Strength,
    string StackDiscriminator,
    string? ContributionIdentity,
    DclStatePayload Payload);

internal sealed record DclCanonicalStatusExecutionInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int? SharedCasterRoll,
    int ResistanceScore,
    int? ResistanceRoll,
    bool Immune,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclStateRegistry StateRegistry,
    DclCanonicalStateMaterialization StateMaterialization,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    DclCanonicalReactionWindowRequest? ReactionWindow = null);

internal sealed record DclCanonicalStatusExecutionResult(
    DclActionDeclaration Declaration,
    DclCastingOutcome Outcome,
    DclMagicDeliveryResult? Delivery,
    DclResourcePayment Payment,
    DclStateApplicationResult? StateApplication,
    DclActionTransaction Transaction,
    DclCanonicalReactionWindowResult? Reactions = null);

internal static class DclCanonicalStatusExecutor
{
    public static DclCanonicalStatusExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalStatusExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.StatusPacket or DclNativeCarrierKind.ConditionalStatusProducer or DclNativeCarrierKind.SingleResult))
            throw new InvalidOperationException("The canonical status executor received an incompatible native carrier.");
        if (profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.Effects.Count != 1 || profile.Effects[0].Kind != DclEffectKind.StatusApplication)
            throw new InvalidOperationException("This status vertical requires one unit-targeted status Carrier Strike.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Declaration request does not use the bound status profile revision.", nameof(input));
        if (input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("Status execution target must match the declared tracked target.", nameof(input));
        DclEffectProfile statusEffect = profile.Effects[0];
        if (statusEffect.ReferencedStateKind is null ||
            !runtime.Authoring.States.TryGetValue(statusEffect.ReferencedStateKind, out DclStateDefinition? definition))
            throw new InvalidOperationException("The bound status effect lost its normalized state definition.");
        if (input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration)
            throw new ArgumentException("State registry and status target must belong to the same battle generation.", nameof(input));
        if (!StringComparer.Ordinal.Equals(input.StateMaterialization.Payload.SchemaId, definition.PayloadSchema))
            throw new ArgumentException("State materialization payload does not match the bound definition.", nameof(input));
        DclCanonicalBattleRuntime? executionBattle = input.ExecutionBattle;
        if (executionBattle is not null)
        {
            if (!ReferenceEquals(executionBattle.Catalog, runtime) ||
                !ReferenceEquals(executionBattle.States, input.StateRegistry) ||
                executionBattle.BattleGeneration != input.DeclarationRequest.Caster.BattleGeneration ||
                !executionBattle.TryGetObservedUnit(input.DeclarationRequest.Caster.UnitSlot, out DclUnitKey observedSource) ||
                observedSource != input.DeclarationRequest.Caster ||
                !executionBattle.TryGetObservedUnit(input.Target.Unit.UnitSlot, out DclUnitKey observedTarget) ||
                observedTarget != input.Target.Unit || input.SharedCasterRoll is not null || input.ResistanceRoll is not null)
                throw new ArgumentException(
                    "Confirmed status execution requires battle-owned state/current identities and accepts no pre-supplied random result.",
                    nameof(input));
            DclCanonicalReactionWindow.RequireConfirmedRequest(runtime, executionBattle, input.ReactionWindow);
        }

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, input.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical status declaration failed: {string.Join(",", attempt.Failures)}");
        if (input.ReactionWindow is { } statusReactionWindow)
            DclCanonicalReactionWindow.Preflight(attempt.Declaration, statusReactionWindow);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
        {
            if (input.SharedCasterRoll is not null || input.ResistanceRoll is not null)
                throw new ArgumentException("ResourceFailure occurs before casting and resistance random sites.", nameof(input));
            DclResourcePayment failurePayment = DclMagicResources.Settle(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment,
                DclCastingOutcome.ResourceFailure);
            var failed = new DclActionTransaction(attempt.Declaration, profile);
            failed.FailResourceCommitment();
            return new DclCanonicalStatusExecutionResult(
                attempt.Declaration,
                DclCastingOutcome.ResourceFailure,
                Delivery: null,
                failurePayment,
                StateApplication: null,
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
            throw new ArgumentNullException(nameof(input), "A payable status cast requires its shared caster draw.");
        DclSpellGateResult castingGate = DclSpellResolution.ClassifySharedRoll(
            sharedCasterRoll,
            input.BaseSpellScore,
            input.TargetSpellScore);
        int? resistanceRoll = input.ResistanceRoll;
        if (executionBattle is not null && profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect &&
            castingGate.TargetSucceeded && !input.Immune)
        {
            resistanceRoll = executionBattle.ExecutionRandom.Roll3D6(executionBattle.RollIdentity(
                input.ActionInstanceId,
                input.DeclarationRequest.Caster,
                input.Target.Unit,
                strikeIndex: 0,
                DclRollSite.Resistance,
                drawIndex: 0));
        }

        DclMagicDeliveryResult delivery = profile.DeliveryProfile.Delivery switch
        {
            DclDelivery.InternalDirect => DclSpellResolution.ResolveInternal(
                sharedCasterRoll,
                input.BaseSpellScore,
                input.TargetSpellScore,
                input.ResistanceScore,
                resistanceRoll,
                input.Immune),
            DclDelivery.Beneficial when !input.Immune && resistanceRoll is null =>
                DclSpellResolution.ResolveBeneficial(
                    sharedCasterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore),
            DclDelivery.Beneficial when input.Immune => throw new ArgumentException(
                "A Beneficial status with immunity requires an explicit effect-owned policy.", nameof(input)),
            _ => throw new InvalidOperationException("This status vertical supports Internal Direct or Beneficial delivery."),
        };
        DclResourcePayment payment = DclMagicResources.Settle(
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment,
            delivery.Gate.BaseOutcome);
        if (!payment.Legal)
            throw new InvalidOperationException("A prechecked status cost became illegal during settlement.");

        DclStateApplicationResult? stateApplication = null;
        DclCanonicalResolvedStrike resolvedStrike = new(
            input.Target.Unit,
            StrikeIndex: 0,
            AppliedEffectIndexes: delivery.Delivered ? [0] : [],
            TargetKoAfterStrike: false);
        DclCanonicalTransactionResult committed = DclCanonicalTransactionExecutor.Commit(
            attempt.Declaration,
            profile,
            [new DclTargetResolutionSnapshot(
                input.Target.Unit,
                input.Target.CurrentHp,
                input.Target.CombatStateRevision,
                input.Target.DefenseResources)],
            [resolvedStrike],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                if (!delivery.Delivered) return;
                DclCanonicalStateMaterialization materialization = input.StateMaterialization;
                var application = new DclStateApplication(
                    definition,
                    input.Target.Unit,
                    materialization.BindSource ? input.DeclarationRequest.Caster : null,
                    input.StateRegistry.CurrentGlobalCt,
                    materialization.AppliedBeforeTurnSerial,
                    materialization.FirstEligibleTargetTurnSerial,
                    materialization.FirstEligibleSourceTurnSerial,
                    materialization.DurationUnits,
                    materialization.Strength,
                    delivery.WinningMargin,
                    materialization.StackDiscriminator,
                    materialization.ContributionIdentity,
                    materialization.Payload,
                    definition.PresentationProfile);
                stateApplication = executionBattle is not null
                    ? executionBattle.ApplyState(application)
                    : input.StateRegistry.Apply(application);
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: input.ExecutionBattle?.States);
        return new DclCanonicalStatusExecutionResult(
            attempt.Declaration,
            delivery.Gate.BaseOutcome,
            delivery,
            payment,
            stateApplication,
            committed.Transaction,
            Reactions: committed.Reactions);
    }
}
