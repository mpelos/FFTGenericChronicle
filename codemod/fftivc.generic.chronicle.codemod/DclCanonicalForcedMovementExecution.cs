namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalForcedMovementExecutionInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int? SharedCasterRoll,
    DclDefenseOption Defense,
    int? DefenseRoll,
    int? ResistanceScore,
    int? ResistanceRoll,
    bool Immune,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclCanonicalNativeMovementVerdict? NativeMovementVerdict,
    DclCanonicalReactionWindowRequest? ReactionWindow = null,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    DclCanonicalConcentrationTargetContext? ConcentrationContext = null,
    int? ConcentrationRoll = null);

internal sealed record DclCanonicalForcedMovementExecutionResult(
    DclActionDeclaration Declaration,
    DclCastingOutcome Outcome,
    DclMagicDeliveryResult? Delivery,
    DclCanonicalForcedMovementResult? Movement,
    DclResourcePayment Payment,
    DclActionTransaction Transaction,
    DclCanonicalReactionWindowResult? Reactions = null,
    DclDefenseResourceSnapshot? FinalDefenseResources = null,
    DclCanonicalAimLifecycleResult? AimCancellation = null,
    DclConcentrationResult? Concentration = null,
    DclCanonicalChargedCancellation? ChargingCancellation = null);

internal sealed record DclCanonicalForcedMovementExecutionRequest(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    DclDefenseOption Defense,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclCanonicalNativeMovementVerdict NativeMovementVerdict,
    int? ResistanceScore = null,
    bool Immune = false,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    DclCanonicalConcentrationTargetContext? ConcentrationContext = null);

internal sealed record DclCanonicalPublishedForcedMovementExecution(
    DclCanonicalForcedMovementExecutionResult Result,
    DclCanonicalNativeActionApplication Application);

internal static class DclCanonicalForcedMovementExecutor
{
    public static DclCanonicalForcedMovementExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalForcedMovementExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket) ||
            profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.MagnitudeProfile is not null || profile.ForcedMovementProfile is null ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            { Kind: DclEffectKind.ForcedMovement, Role: DclEffectRole.Carrier })
            throw new InvalidOperationException("ForcedMovement execution requires one unit-targeted nonnumeric Carrier.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision ||
            input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("ForcedMovement declaration/target identity does not match its binding.", nameof(input));
        DclMagicDefensePolicy.Validate(profile.DeliveryProfile, input.Defense, input.Target.DefenseResources);
        input.ConcentrationContext?.Validate();
        if (input.ExecutionBattle is { } concentrationBattle &&
            (input.ConcentrationContext?.Charging ?? false) != concentrationBattle.IsCharging(input.Target.Unit))
            throw new ArgumentException(
                "ForcedMovement concentration snapshot diverges from the canonical timeline before RNG.", nameof(input));

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, input.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical ForcedMovement declaration failed: {string.Join(",", attempt.Failures)}");
        if (input.ReactionWindow is { } movementReactionWindow)
            DclCanonicalReactionWindow.Preflight(attempt.Declaration, movementReactionWindow);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
        {
            if (input.SharedCasterRoll is not null || input.DefenseRoll is not null ||
                input.ResistanceRoll is not null || input.NativeMovementVerdict is not null ||
                input.ConcentrationRoll is not null)
                throw new ArgumentException(
                    "ResourceFailure occurs before ForcedMovement casting, defense/resistance, and map resolution.",
                    nameof(input));
            DclResourcePayment failedPayment = DclMagicResources.Settle(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment,
                DclCastingOutcome.ResourceFailure);
            var failed = new DclActionTransaction(attempt.Declaration, profile);
            failed.FailResourceCommitment();
            return new DclCanonicalForcedMovementExecutionResult(
                attempt.Declaration,
                DclCastingOutcome.ResourceFailure,
                Delivery: null,
                Movement: null,
                failedPayment,
                failed);
        }

        int casterRoll = input.SharedCasterRoll ??
            throw new ArgumentNullException(nameof(input), "A payable ForcedMovement action requires one caster draw.");
        DclMagicDeliveryResult delivery = ResolveDelivery(profile, input, casterRoll);
        DclDefenseResourceSnapshot finalDefenseResources = DclMagicDefensePolicy.ResolveFinalResources(
            delivery,
            input.Target.DefenseResources);
        DclCanonicalForcedMovementResult? movement = delivery.Delivered
            ? DclCanonicalForcedMovement.Resolve(
                profile,
                input.Target.Unit,
                input.Target.Tile,
                targetKo: false,
                input.NativeMovementVerdict)
            : null;
        if (!delivery.Delivered && input.NativeMovementVerdict is not null)
            throw new ArgumentException("A failed/defended ForcedMovement delivery cannot consume a native map verdict.", nameof(input));
        DclResourcePayment payment = DclMagicResources.Settle(
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment,
            delivery.Gate.BaseOutcome);
        if (!payment.Legal)
            throw new InvalidOperationException("A prechecked ForcedMovement cost became illegal during settlement.");
        DclCanonicalAimLifecycleResult? aimCancellation = null;
        long? plannedAimRemovalId = null;
        if (movement?.CancelAim == true && input.ExecutionBattle is { } executionBattle)
        {
            DclCanonicalAimLifecycleResult planned = DclCanonicalAimLifecycle.PlanCancelOwner(
                executionBattle.States,
                input.Target.Unit,
                "forced-movement-cancelled");
            if (planned.HadAim)
            {
                aimCancellation = planned;
                plannedAimRemovalId = planned.InstanceId ??
                    throw new InvalidOperationException("A planned ForcedMovement Aim cancellation lost its state identity.");
            }
        }
        DclConcentrationResult? concentration = null;
        if (movement?.CreatesConcentrationIncident == true)
        {
            DclCanonicalConcentrationTargetContext context = input.ConcentrationContext ??
                new DclCanonicalConcentrationTargetContext(
                    Charging: false,
                    Will: 1,
                    ConcentrationModifier: 0,
                    StatePenaltyMagnitude: 0);
            int? concentrationRoll = input.ConcentrationRoll;
            if (context.Charging && input.ExecutionBattle is { } randomBattle)
                concentrationRoll = randomBattle.ExecutionRandom.Roll3D6(randomBattle.RollIdentity(
                    input.ActionInstanceId,
                    input.DeclarationRequest.Caster,
                    input.Target.Unit,
                    strikeIndex: 0,
                    DclRollSite.Concentration,
                    drawIndex: 0));
            concentration = DclConcentration.ResolveStrikeIncident(
                context.Charging,
                directCancellation: false,
                injury: 0,
                forcedDisplacement: movement.MovedTiles,
                context.Will,
                context.ConcentrationModifier,
                context.StatePenaltyMagnitude,
                concentrationRoll);
        }
        else if (input.ConcentrationRoll is not null)
            throw new ArgumentException(
                "A nonmoving ForcedMovement result cannot consume concentration RNG.", nameof(input));
        DclCanonicalChargedCancellation? chargingCancellation = null;
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
                AppliedEffectIndexes: delivery.Delivered ? [0] : [],
                TargetKoAfterStrike: false)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: transaction =>
            {
                transaction.ApplyFinalDefenseResources(input.Target.Unit, finalDefenseResources);
                if (plannedAimRemovalId is { } aimId)
                {
                    IReadOnlyList<long> removed = input.ExecutionBattle!.States.RemoveInstances([aimId]);
                    if (removed.Count != 1)
                        throw new InvalidOperationException("Target Aim state changed before ForcedMovement commit.");
                }
                if (concentration is { } incident)
                    chargingCancellation = input.ExecutionBattle?.ResolveConcentrationIncident(
                        input.Target.Unit,
                        incident) ?? (incident.Outcome == DclConcentrationOutcome.NoIncident
                            ? null
                            : throw new InvalidOperationException(
                                "An executable ForcedMovement concentration incident lost its timeline owner."));
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: input.ExecutionBattle?.States);
        return new DclCanonicalForcedMovementExecutionResult(
            attempt.Declaration,
            delivery.Gate.BaseOutcome,
            delivery,
            movement,
            payment,
            committed.Transaction,
            Reactions: committed.Reactions,
            FinalDefenseResources: finalDefenseResources,
            AimCancellation: aimCancellation,
            Concentration: concentration,
            ChargingCancellation: chargingCancellation);
    }

    private static DclMagicDeliveryResult ResolveDelivery(
        DclActionProfile profile,
        DclCanonicalForcedMovementExecutionInput input,
        int casterRoll)
        => profile.DeliveryProfile.Delivery switch
        {
            DclDelivery.ExternalProjectile => DclSpellResolution.ResolveExternal(
                casterRoll,
                input.BaseSpellScore,
                input.TargetSpellScore,
                input.Defense,
                input.DefenseRoll),
            DclDelivery.InternalDirect => DclSpellResolution.ResolveInternal(
                casterRoll,
                input.BaseSpellScore,
                input.TargetSpellScore,
                input.ResistanceScore ?? throw new ArgumentException(
                    "Internal Direct ForcedMovement requires one resistance score.", nameof(input)),
                input.ResistanceRoll,
                input.Immune),
            DclDelivery.Beneficial => input.Defense.Kind == DclDefenseKind.None
                ? DclSpellResolution.ResolveBeneficial(casterRoll, input.BaseSpellScore, input.TargetSpellScore)
                : throw new ArgumentException("Beneficial ForcedMovement cannot own active defense.", nameof(input)),
            _ => throw new InvalidOperationException(
                "Generic ForcedMovement supports ExternalProjectile, InternalDirect, or Beneficial delivery."),
        };
}

internal static class DclCanonicalForcedMovementExecutionCoordinator
{
    public static DclCanonicalPublishedForcedMovementExecution ResolveAndPublish(
        DclCanonicalBattleRuntime battle,
        DclCanonicalForcedMovementExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        (_, DclActionProfile profile) = battle.Catalog.ResolveAbility(request.AbilityId);
        if (request.ActionInstanceId <= 0 ||
            !battle.TryGetObservedUnit(request.DeclarationRequest.Caster.UnitSlot, out DclUnitKey source) ||
            source != request.DeclarationRequest.Caster ||
            !battle.TryGetObservedUnit(request.Target.Unit.UnitSlot, out DclUnitKey target) || target != request.Target.Unit)
            throw new ArgumentException("ForcedMovement execution requires current battle-owned source/target identities.", nameof(request));
        if (battle.States.CaptureTarget(target).Revision != request.Target.CombatStateRevision)
            throw new ArgumentException(
                "ForcedMovement execution received a stale target custom-state revision before RNG.",
                nameof(request));
        DclDefenseResourceSnapshot defenseBefore = battle.CaptureDefenseResources(target);
        if (!DclCanonicalBattleRuntime.DefenseResourcesEqual(defenseBefore, request.Target.DefenseResources))
            throw new ArgumentException(
                "ForcedMovement execution received a stale or noncanonical defense-resource snapshot before RNG.",
                nameof(request));
        DclTargetCandidate canonicalTarget = request.Target with { DefenseResources = defenseBefore };
        request.ConcentrationContext?.Validate();
        if ((request.ConcentrationContext?.Charging ?? false) != battle.IsCharging(target))
            throw new ArgumentException(
                "ForcedMovement concentration snapshot diverges from the canonical timeline before RNG.", nameof(request));
        DclMagicDefensePolicy.Validate(profile.DeliveryProfile, request.Defense, defenseBefore);
        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(
            request.DeclarationRequest,
            request.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical ForcedMovement declaration failed: {string.Join(",", attempt.Failures)}");
        DclCanonicalActionStateProjection.RequireActionLegal(
            DclCanonicalActionStateProjection.EvaluateTaunt(
                battle,
                source,
                profile.TimingProfile.ConsumesAction,
                isUniversalNormalAttack: false,
                target,
                normalAttackTargetLegal: false));
        bool payable = DclMagicResources.CanPayFullCost(
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            attempt.CostCommitment);
        int? casterRoll = null;
        int? defenseRoll = null;
        int? resistanceRoll = null;
        DclCanonicalNativeMovementVerdict? verdict = null;
        if (payable)
        {
            casterRoll = battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                request.ActionInstanceId,
                source,
                target: null,
                strikeIndex: 0,
                DclRollSite.Casting,
                drawIndex: 0));
            DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
                casterRoll.Value,
                request.BaseSpellScore,
                request.TargetSpellScore);
            if (profile.DeliveryProfile.Delivery == DclDelivery.ExternalProjectile &&
                gate.TargetSucceeded && !gate.TargetCritical && request.Defense.Kind != DclDefenseKind.None)
                defenseRoll = battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                    request.ActionInstanceId, source, target, 0, DclRollSite.ActiveDefense, 0));
            if (profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect &&
                gate.TargetSucceeded && !request.Immune)
                resistanceRoll = battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                    request.ActionInstanceId, source, target, 0, DclRollSite.Resistance, 0));
            DclMagicDeliveryResult preview = profile.DeliveryProfile.Delivery switch
            {
                DclDelivery.ExternalProjectile => DclSpellResolution.ResolveExternal(
                    casterRoll.Value, request.BaseSpellScore, request.TargetSpellScore, request.Defense, defenseRoll),
                DclDelivery.InternalDirect => DclSpellResolution.ResolveInternal(
                    casterRoll.Value,
                    request.BaseSpellScore,
                    request.TargetSpellScore,
                    request.ResistanceScore ?? throw new ArgumentException(
                        "Internal Direct ForcedMovement requires one resistance score.", nameof(request)),
                    resistanceRoll,
                    request.Immune),
                DclDelivery.Beneficial => DclSpellResolution.ResolveBeneficial(
                    casterRoll.Value, request.BaseSpellScore, request.TargetSpellScore),
                _ => throw new InvalidOperationException("Unsupported generic ForcedMovement delivery."),
            };
            if (preview.Delivered) verdict = request.NativeMovementVerdict;
        }
        DclCanonicalForcedMovementExecutionResult result = DclCanonicalForcedMovementExecutor.Resolve(
            battle.Catalog,
            new DclCanonicalForcedMovementExecutionInput(
                request.AbilityId,
                request.DeclarationRequest,
                request.ActionInstanceId,
                canonicalTarget,
                request.BaseSpellScore,
                request.TargetSpellScore,
                casterRoll,
                request.Defense,
                defenseRoll,
                request.ResistanceScore,
                resistanceRoll,
                request.Immune,
                request.CurrentMpAtResolution,
                request.CurrentHpAtResolution,
                verdict,
                DclCanonicalReactionWindow.ConfirmedRequest(battle, request.ReactionCandidates),
                battle,
                request.ConcentrationContext));
        if (result.Outcome != DclCastingOutcome.ResourceFailure)
        {
            DclDefenseResourceSnapshot finalDefense = result.FinalDefenseResources ??
                throw new InvalidOperationException("Confirmed ForcedMovement lost its final defense-resource snapshot.");
            if (!DclCanonicalBattleRuntime.DefenseResourcesEqual(defenseBefore, finalDefense))
                battle.CommitDefenseResourcesBatch([
                    new DclCanonicalDefenseResourceCommit(target, defenseBefore, finalDefense),
                ]);
        }
        DclCanonicalNativeActionApplication application =
            DclCanonicalNativeExecutionPublisher.PublishForcedMovement(battle, request.AbilityId, result);
        return new DclCanonicalPublishedForcedMovementExecution(result, application);
    }
}
