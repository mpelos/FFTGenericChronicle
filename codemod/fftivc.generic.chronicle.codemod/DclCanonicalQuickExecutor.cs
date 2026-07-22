namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalQuickExecutionInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int? SharedCasterRoll,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclCtState TargetCt,
    DclQuickLockController QuickLocks,
    DclStateRegistry StateRegistry,
    DclCanonicalStateMaterialization LockMaterialization,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    DclCanonicalReactionWindowRequest? ReactionWindow = null);

internal sealed record DclCanonicalQuickExecutionResult(
    DclActionDeclaration Declaration,
    DclCastingOutcome Outcome,
    DclMagicDeliveryResult? Delivery,
    DclResourcePayment Payment,
    DclRational CtGrant,
    DclStateApplicationResult? QuickLock,
    DclActionTransaction Transaction,
    DclCanonicalReactionWindowResult? Reactions = null);

internal readonly record struct DclCanonicalQuickUnlockResult(
    bool LockControllerCleared,
    IReadOnlyList<long> RemovedStateInstanceIds);

internal readonly record struct DclCanonicalQuickUnlockReservation(
    DclUnitKey Target,
    string StateKind,
    long? StateInstanceId);

internal static class DclCanonicalQuickExecutor
{
    public static DclCanonicalQuickExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalQuickExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket))
            throw new InvalidOperationException("The canonical Quick executor received an incompatible native carrier.");
        if (profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.DeliveryProfile.Delivery != DclDelivery.Beneficial ||
            profile.MagnitudeProfile is not DclFixedResourceMagnitude
            {
                Resource: DclResourceKind.Ct,
            } ctMagnitude ||
            profile.Effects.Count != 2 || profile.Effects[0] is not
            {
                Kind: DclEffectKind.CtChange,
                Role: DclEffectRole.Carrier,
            } || profile.Effects[1] is not
            {
                Kind: DclEffectKind.StatusApplication,
                Role: DclEffectRole.Independent,
                ReferencedStateKind: { } lockStateKind,
            })
            throw new InvalidOperationException("Quick requires one CTChange Carrier followed by its independent QuickLock state.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Quick declaration does not match the bound profile revision.", nameof(input));
        if (input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit ||
            input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration)
            throw new ArgumentException("Quick target/declaration/registry identities must agree.", nameof(input));
        if (!runtime.Authoring.States.TryGetValue(lockStateKind, out DclStateDefinition? lockDefinition))
            throw new InvalidOperationException("Quick lost its normalized QuickLock definition.");
        if (!StringComparer.Ordinal.Equals(input.LockMaterialization.Payload.SchemaId, lockDefinition.PayloadSchema))
            throw new ArgumentException("QuickLock materialization payload does not match its definition.", nameof(input));
        DclRational ctGrant = DclRational.ParseExactDecimal(ctMagnitude.Expression);
        if (ctGrant <= DclRational.FromInteger(0))
            throw new InvalidOperationException("Quick requires a positive exact CT magnitude.");
        if (input.QuickLocks.IsLocked(input.Target.Unit))
            throw new InvalidOperationException("Quick is illegal while the target already owns QuickLock.");
        if (input.TargetCt.CurrentCt + ctGrant < DclRational.FromInteger(DclCtState.TurnThreshold))
            throw new InvalidOperationException("The authored Quick magnitude cannot grant the target's next turn.");
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
                "Confirmed Quick execution requires battle-owned state/current identities and no pre-supplied caster roll.",
                nameof(input));
        if (executionBattle is not null)
            DclCanonicalReactionWindow.RequireConfirmedRequest(runtime, executionBattle, input.ReactionWindow);

        DclCanonicalStateMaterialization lockMaterialization = input.LockMaterialization;
        var lockApplication = new DclStateApplication(
            lockDefinition,
            input.Target.Unit,
            lockMaterialization.BindSource ? input.DeclarationRequest.Caster : null,
            input.StateRegistry.CurrentGlobalCt,
            lockMaterialization.AppliedBeforeTurnSerial,
            lockMaterialization.FirstEligibleTargetTurnSerial,
            lockMaterialization.FirstEligibleSourceTurnSerial,
            lockMaterialization.DurationUnits,
            lockMaterialization.Strength,
            WinningMargin: null,
            lockMaterialization.StackDiscriminator,
            lockMaterialization.ContributionIdentity,
            lockMaterialization.Payload,
            lockDefinition.PresentationProfile);
        if (executionBattle is not null)
            executionBattle.ValidateStateApplication(lockApplication);
        else
            input.StateRegistry.ValidateGenericApplication(lockApplication);

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, input.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical Quick declaration failed: {string.Join(",", attempt.Failures)}");
        if (input.ReactionWindow is { } quickReactionWindow)
            DclCanonicalReactionWindow.Preflight(attempt.Declaration, quickReactionWindow);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
        {
            if (input.SharedCasterRoll is not null)
                throw new ArgumentException("ResourceFailure occurs before Quick's caster draw.", nameof(input));
            DclResourcePayment failedPayment = DclMagicResources.Settle(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment,
                DclCastingOutcome.ResourceFailure);
            var failed = new DclActionTransaction(attempt.Declaration, profile);
            failed.FailResourceCommitment();
            return new DclCanonicalQuickExecutionResult(
                attempt.Declaration,
                DclCastingOutcome.ResourceFailure,
                Delivery: null,
                failedPayment,
                ctGrant,
                QuickLock: null,
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
            throw new ArgumentNullException(nameof(input), "A payable Quick requires its caster draw.");
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
            throw new InvalidOperationException("A prechecked Quick cost became illegal during settlement.");

        DclStateApplicationResult? quickLock = null;
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
                AppliedEffectIndexes: delivery.Delivered ? [0, 1] : [],
                TargetKoAfterStrike: false)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                if (!delivery.Delivered) return;
                if (!input.QuickLocks.TryApplyQuick(input.Target.Unit, input.TargetCt, ctGrant))
                    throw new InvalidOperationException("QuickLock changed between planning and atomic commit.");
                try
                {
                    quickLock = executionBattle is not null
                        ? executionBattle.ApplyState(lockApplication)
                        : input.StateRegistry.Apply(lockApplication);
                }
                catch
                {
                    bool controllerCleared = input.QuickLocks.OnGrantedTurnResolved(input.Target.Unit);
                    input.TargetCt.ApplyExplicitCtChange(DclRational.FromInteger(0) - ctGrant);
                    if (!controllerCleared)
                        throw new InvalidOperationException(
                            "QuickLock persistent-state failure could not roll back its controller.");
                    throw;
                }
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: input.ExecutionBattle?.States);
        return new DclCanonicalQuickExecutionResult(
            attempt.Declaration,
            delivery.Gate.BaseOutcome,
            delivery,
            payment,
            ctGrant,
            quickLock,
            committed.Transaction,
            Reactions: committed.Reactions);
    }

    public static DclCanonicalQuickUnlockResult CompleteGrantedTurn(
        DclUnitKey target,
        string lockStateKind,
        DclQuickLockController quickLocks,
        DclStateRegistry stateRegistry)
        => CommitGrantedTurnCompletion(
            PrepareGrantedTurnCompletion(target, lockStateKind, quickLocks, stateRegistry),
            quickLocks,
            stateRegistry);

    public static DclCanonicalQuickUnlockReservation PrepareGrantedTurnCompletion(
        DclUnitKey target,
        string lockStateKind,
        DclQuickLockController quickLocks,
        DclStateRegistry stateRegistry)
    {
        ArgumentNullException.ThrowIfNull(quickLocks);
        ArgumentNullException.ThrowIfNull(stateRegistry);
        if (!target.IsValid || target.BattleGeneration != stateRegistry.BattleGeneration)
            throw new ArgumentException("QuickLock completion requires the registry's current target identity.", nameof(target));
        if (string.IsNullOrWhiteSpace(lockStateKind))
            throw new ArgumentException("QuickLock completion requires its normalized state kind.", nameof(lockStateKind));
        DclStateInstance[] persistentLocks = stateRegistry.Instances
            .Where(instance => instance.Target == target &&
                               StringComparer.Ordinal.Equals(instance.Kind, lockStateKind))
            .OrderBy(instance => instance.InstanceId)
            .ToArray();
        bool controllerLocked = quickLocks.IsLocked(target);
        if (controllerLocked != (persistentLocks.Length == 1))
            throw new InvalidOperationException("QuickLock controller and persistent state lost atomic ownership agreement.");
        return new DclCanonicalQuickUnlockReservation(
            target,
            lockStateKind,
            persistentLocks.SingleOrDefault()?.InstanceId);
    }

    public static DclCanonicalQuickUnlockResult CommitGrantedTurnCompletion(
        DclCanonicalQuickUnlockReservation reservation,
        DclQuickLockController quickLocks,
        DclStateRegistry stateRegistry)
    {
        ArgumentNullException.ThrowIfNull(quickLocks);
        ArgumentNullException.ThrowIfNull(stateRegistry);
        if (reservation.StateInstanceId is null)
        {
            if (quickLocks.IsLocked(reservation.Target) || stateRegistry.Instances.Any(instance =>
                    instance.Target == reservation.Target &&
                    StringComparer.Ordinal.Equals(instance.Kind, reservation.StateKind)))
                throw new InvalidOperationException("QuickLock changed after its turn-completion reservation.");
            return new DclCanonicalQuickUnlockResult(false, []);
        }
        if (!quickLocks.IsLocked(reservation.Target) ||
            !stateRegistry.TryGet(reservation.StateInstanceId.Value, out DclStateInstance persistentLock) ||
            persistentLock.Target != reservation.Target ||
            !StringComparer.Ordinal.Equals(persistentLock.Kind, reservation.StateKind))
            throw new InvalidOperationException("QuickLock changed after its turn-completion reservation.");
        IReadOnlyList<long> removed = stateRegistry.RemoveInstances([reservation.StateInstanceId.Value]);
        if (!quickLocks.OnGrantedTurnResolved(reservation.Target))
            throw new InvalidOperationException("QuickLock controller changed during its reserved turn completion.");
        return new DclCanonicalQuickUnlockResult(true, removed);
    }
}
