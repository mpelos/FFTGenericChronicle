namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalReviveExecutionInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int? SharedCasterRoll,
    int? ResistanceScore,
    int? ResistanceRoll,
    bool Immune,
    IReadOnlyList<int>? RestoredHpDice,
    DclRational FaithMultiplier,
    bool TargetUndead,
    int TargetMaxHp,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclUndeadInteractionTable UndeadInteractions,
    DclStateRegistry? StateRegistry = null,
    DclCanonicalStateMaterialization? StateMaterialization = null,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    DclCanonicalReactionWindowRequest? ReactionWindow = null);

internal sealed record DclCanonicalReviveExecutionResult(
    DclActionDeclaration Declaration,
    DclCastingOutcome Outcome,
    DclMagicDeliveryResult? Delivery,
    DclResourcePayment Payment,
    DclReviveResult? Revive,
    DclStateApplicationResult? StoredReraise,
    DclActionTransaction Transaction,
    DclCanonicalReactionWindowResult? Reactions = null);

internal static class DclCanonicalReviveExecutor
{
    public static DclCanonicalReviveExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalReviveExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.LifecycleTransaction or
            DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket))
            throw new InvalidOperationException("The canonical revive executor received an incompatible native carrier.");
        if (profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            {
                Kind: DclEffectKind.Revive,
                Role: DclEffectRole.Carrier,
            } || profile.ReviveProfile is null ||
            profile.MagnitudeProfile is not DclFixedResourceMagnitude
            {
                Resource: DclResourceKind.Hp,
            } restoredMagnitude)
            throw new InvalidOperationException("This lifecycle vertical requires one unit-targeted Revive Carrier and its restored-HP profile.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Revive declaration does not match the bound profile revision.", nameof(input));
        if (input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("Revive target must match the declared tracked target.", nameof(input));
        if (input.TargetMaxHp < 1 || input.Target.CurrentHp > input.TargetMaxHp)
            throw new ArgumentOutOfRangeException(nameof(input), "Revive MaxHP must contain current HP.");
        DclCanonicalBattleRuntime? executionBattle = input.ExecutionBattle;
        if (executionBattle is not null)
        {
            if (!ReferenceEquals(executionBattle.Catalog, runtime) ||
                executionBattle.BattleGeneration != input.DeclarationRequest.Caster.BattleGeneration ||
                !executionBattle.TryGetObservedUnit(input.DeclarationRequest.Caster.UnitSlot, out DclUnitKey observedSource) ||
                observedSource != input.DeclarationRequest.Caster ||
                !executionBattle.TryGetObservedUnit(input.Target.Unit.UnitSlot, out DclUnitKey observedTarget) ||
                observedTarget != input.Target.Unit || input.SharedCasterRoll is not null ||
                input.ResistanceRoll is not null || input.RestoredHpDice is not null)
                throw new ArgumentException(
                    "Confirmed Revive execution requires current identities and accepts no pre-supplied random result.",
                    nameof(input));
            if (input.StateRegistry is not null && !ReferenceEquals(input.StateRegistry, executionBattle.States))
                throw new ArgumentException("Confirmed Stored Reraise must use the battle-owned state registry.", nameof(input));
            DclCanonicalReactionWindow.RequireConfirmedRequest(runtime, executionBattle, input.ReactionWindow);
        }

        DclReviveProfile reviveProfile = profile.ReviveProfile;
        DclDiceExpression restoredExpression = DclDiceExpression.ParseAuthored(restoredMagnitude.Expression);
        DclEffectProfile reviveEffect = profile.Effects[0];
        DclStateDefinition? reraiseDefinition = null;
        if (reviveProfile.Mode == DclReviveMode.StoredReraise)
        {
            if (reviveEffect.ReferencedStateKind is null ||
                !runtime.Authoring.States.TryGetValue(reviveEffect.ReferencedStateKind, out reraiseDefinition))
                throw new InvalidOperationException("Stored Reraise lost its normalized trigger-state definition.");
            if (input.StateRegistry is null || input.StateMaterialization is null)
                throw new ArgumentException("Stored Reraise requires its battle registry and state materialization.", nameof(input));
            if (input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration ||
                !StringComparer.Ordinal.Equals(input.StateMaterialization.Payload.SchemaId, reraiseDefinition.PayloadSchema))
                throw new ArgumentException("Stored Reraise state ownership does not match the target/definition.", nameof(input));
        }
        else if (input.StateRegistry is not null || input.StateMaterialization is not null)
        {
            throw new ArgumentException("Immediate revive cannot own a stored-state materialization.", nameof(input));
        }

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, input.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical revive declaration failed: {string.Join(",", attempt.Failures)}");
        if (input.ReactionWindow is { } reviveReactionWindow)
            DclCanonicalReactionWindow.Preflight(attempt.Declaration, reviveReactionWindow);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
        {
            if (input.SharedCasterRoll is not null || input.ResistanceRoll is not null || input.RestoredHpDice is not null)
                throw new ArgumentException("ResourceFailure occurs before revive casting, resistance, and magnitude random sites.", nameof(input));
            DclResourcePayment failedPayment = DclMagicResources.Settle(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment,
                DclCastingOutcome.ResourceFailure);
            var failed = new DclActionTransaction(attempt.Declaration, profile);
            failed.FailResourceCommitment();
            return new DclCanonicalReviveExecutionResult(
                attempt.Declaration,
                DclCastingOutcome.ResourceFailure,
                Delivery: null,
                failedPayment,
                Revive: null,
                StoredReraise: null,
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
            throw new ArgumentNullException(nameof(input), "A payable revive requires its shared caster draw.");
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
            DclDelivery.Beneficial when input.ResistanceScore is null && input.ResistanceRoll is null && !input.Immune =>
                DclSpellResolution.ResolveBeneficial(
                    sharedCasterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore),
            DclDelivery.InternalDirect when input.ResistanceScore is { } resistanceScore =>
                DclSpellResolution.ResolveInternal(
                    sharedCasterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore,
                    resistanceScore,
                    resistanceRoll,
                    input.Immune),
            DclDelivery.Beneficial => throw new ArgumentException(
                "Beneficial revive has no hostile resistance or immunity gate.", nameof(input)),
            _ => throw new InvalidOperationException("Revive delivery must be Beneficial or InternalDirect."),
        };

        DclReviveResult revive = DclReviveRules.Resolve(
            reviveProfile,
            input.UndeadInteractions,
            input.Target.States,
            input.TargetUndead,
            delivery.Delivered,
            rawRestoredHp: 0,
            input.FaithMultiplier,
            input.Target.CurrentHp,
            input.TargetMaxHp);
        if (revive.Route == DclReviveRoute.ImmediateHpCredit)
        {
            IReadOnlyList<int>? restoredHpDice = input.RestoredHpDice ?? executionBattle?.ExecutionRandom.RollD6Pool(
                executionBattle.RollIdentity(
                    input.ActionInstanceId,
                    input.DeclarationRequest.Caster,
                    input.Target.Unit,
                    strikeIndex: 0,
                    DclRollSite.HealingDie,
                    drawIndex: 0),
                restoredExpression.Dice);
            if (restoredHpDice is null)
                throw new ArgumentNullException(nameof(input), "A delivered immediate revive requires exact restored-HP dice.");
            int rawRestoredHp = Math.Max(0, DclInjury.RollDamage(restoredExpression, restoredHpDice));
            revive = DclReviveRules.Resolve(
                reviveProfile,
                input.UndeadInteractions,
                input.Target.States,
                input.TargetUndead,
                deliverySucceeded: true,
                rawRestoredHp,
                input.FaithMultiplier,
                input.Target.CurrentHp,
                input.TargetMaxHp);
        }
        else if (input.RestoredHpDice is not null)
        {
            throw new ArgumentException("A failed, rejected, ineligible, or stored revive cannot consume restored-HP RNG.", nameof(input));
        }

        DclResourcePayment payment = DclMagicResources.Settle(
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment,
            delivery.Gate.BaseOutcome);
        if (!payment.Legal)
            throw new InvalidOperationException("A prechecked revive cost became illegal during settlement.");

        bool effectApplied = revive is
        {
            Route: DclReviveRoute.ImmediateHpCredit,
            ClearKoAfterPositiveCredit: true,
        } or
        {
            Route: DclReviveRoute.StoredReraise,
            StoreReraise: true,
        };
        DclStateApplicationResult? storedReraise = null;
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
                AppliedEffectIndexes: effectApplied ? [0] : [],
                TargetKoAfterStrike: false)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                if (!revive.StoreReraise) return;
                DclStateRegistry registry = input.StateRegistry!;
                DclCanonicalStateMaterialization materialization = input.StateMaterialization!;
                storedReraise = registry.Apply(new DclStateApplication(
                    reraiseDefinition!,
                    input.Target.Unit,
                    materialization.BindSource ? input.DeclarationRequest.Caster : null,
                    registry.CurrentGlobalCt,
                    materialization.AppliedBeforeTurnSerial,
                    materialization.FirstEligibleTargetTurnSerial,
                    materialization.FirstEligibleSourceTurnSerial,
                    materialization.DurationUnits,
                    materialization.Strength,
                    delivery.WinningMargin,
                    materialization.StackDiscriminator,
                    materialization.ContributionIdentity,
                    new DclReraiseStatePayload(
                        reraiseDefinition!.PayloadSchema,
                        input.AbilityId,
                        restoredExpression,
                        input.FaithMultiplier,
                        reviveProfile.FaithAxis is DclReviveFaithAxis.RestoredHp or DclReviveFaithAxis.BothExplicit),
                    reraiseDefinition!.PresentationProfile));
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: input.ExecutionBattle?.States);
        return new DclCanonicalReviveExecutionResult(
            attempt.Declaration,
            delivery.Gate.BaseOutcome,
            delivery,
            payment,
            revive,
            storedReraise,
            committed.Transaction,
            Reactions: committed.Reactions);
    }
}
