namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalDispelInstanceInput(
    long InstanceId,
    int? EffectResistanceRoll);

internal sealed record DclCanonicalDispelInstanceResult(
    long InstanceId,
    DclQuickContestResult? Contest,
    bool Removed,
    string StateKind = "");

internal sealed record DclCanonicalDispelExecutionInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int FinalDispelScore,
    int? SharedCasterRoll,
    IReadOnlyList<DclCanonicalDispelInstanceInput> SelectedInstances,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclStateRegistry StateRegistry,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    DclCanonicalReactionWindowRequest? ReactionWindow = null);

internal sealed record DclCanonicalDispelExecutionResult(
    DclActionDeclaration Declaration,
    DclCastingOutcome Outcome,
    DclSpellGateResult? CastingGate,
    DclResourcePayment Payment,
    IReadOnlyList<DclCanonicalDispelInstanceResult> Instances,
    IReadOnlyList<long> RemovedInstanceIds,
    DclActionTransaction Transaction,
    DclCanonicalReactionWindowResult? Reactions = null);

internal static class DclCanonicalDispelExecutor
{
    public static DclCanonicalDispelExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalDispelExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.StatusPacket or
            DclNativeCarrierKind.ConditionalStatusProducer or DclNativeCarrierKind.SingleResult))
            throw new InvalidOperationException("The canonical Dispel executor received an incompatible native carrier.");
        if (profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            {
                Kind: DclEffectKind.Dispel,
                Role: DclEffectRole.Carrier,
            } || profile.DispelProfile is null)
            throw new InvalidOperationException("This vertical requires one unit-targeted Dispel Carrier and its explicit selection profile.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Dispel declaration does not match the bound profile revision.", nameof(input));
        if (input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("Dispel target must match the declared tracked target.", nameof(input));
        if (input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration)
            throw new ArgumentException("Dispel registry and target must share one battle generation.", nameof(input));
        if (input.FinalDispelScore < 1)
            throw new ArgumentOutOfRangeException(nameof(input), "Final DispelScore must be positive.");
        DclCanonicalBattleRuntime? executionBattle = input.ExecutionBattle;
        if (executionBattle is not null &&
            (!ReferenceEquals(executionBattle.Catalog, runtime) ||
             !ReferenceEquals(executionBattle.States, input.StateRegistry) ||
             executionBattle.BattleGeneration != input.DeclarationRequest.Caster.BattleGeneration ||
             !executionBattle.TryGetObservedUnit(input.DeclarationRequest.Caster.UnitSlot, out DclUnitKey observedSource) ||
             observedSource != input.DeclarationRequest.Caster ||
             !executionBattle.TryGetObservedUnit(input.Target.Unit.UnitSlot, out DclUnitKey observedTarget) ||
             observedTarget != input.Target.Unit || input.SharedCasterRoll is not null ||
             input.SelectedInstances.Any(instance => instance.EffectResistanceRoll is not null)))
            throw new ArgumentException(
                "Confirmed Dispel execution requires battle-owned state/current identities and no pre-supplied random result.",
                nameof(input));
        if (executionBattle is not null)
            DclCanonicalReactionWindow.RequireConfirmedRequest(runtime, executionBattle, input.ReactionWindow);

        DclDispelProfile dispelProfile = profile.DispelProfile;
        HashSet<string> eligibleFamilies = dispelProfile.EligibleCureFamilies.ToHashSet(StringComparer.Ordinal);
        DclStateInstance[] eligibleInstances = input.StateRegistry.Instances
            .Where(instance => instance.Target == input.Target.Unit &&
                instance.Definition.CureFamilies.Any(eligibleFamilies.Contains) &&
                (!dispelProfile.SourceMatchedOnly || instance.Source == input.DeclarationRequest.Caster))
            .OrderBy(instance => instance.InstanceId)
            .ToArray();
        DclCanonicalDispelInstanceInput[] selectedInputs = input.SelectedInstances.ToArray();
        Dictionary<long, DclCanonicalDispelInstanceInput> selectedById = selectedInputs.ToDictionary(instance => instance.InstanceId);
        if (selectedById.Count != selectedInputs.Length)
            throw new ArgumentException("A Dispel action cannot select the same state instance twice.", nameof(input));
        HashSet<long> eligibleIds = eligibleInstances.Select(instance => instance.InstanceId).ToHashSet();
        if (selectedById.Keys.Any(id => !eligibleIds.Contains(id)))
            throw new ArgumentException("Dispel selected an instance outside its target/family/source policy.", nameof(input));
        if (dispelProfile.Scope == DclDispelScope.AllEligible &&
            !selectedById.Keys.ToHashSet().SetEquals(eligibleIds))
            throw new ArgumentException("AllEligible Dispel must select the complete eligible snapshot.", nameof(input));
        if (dispelProfile.Scope == DclDispelScope.OneInstance && selectedInputs.Length > 1)
            throw new ArgumentException("OneInstance Dispel may select at most one eligible instance.", nameof(input));

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, input.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical Dispel declaration failed: {string.Join(",", attempt.Failures)}");
        if (input.ReactionWindow is { } dispelReactionWindow)
            DclCanonicalReactionWindow.Preflight(attempt.Declaration, dispelReactionWindow);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
        {
            if (input.SharedCasterRoll is not null || selectedInputs.Any(instance => instance.EffectResistanceRoll is not null))
                throw new ArgumentException("ResourceFailure occurs before Dispel casting and per-effect resistance random sites.", nameof(input));
            DclResourcePayment failedPayment = DclMagicResources.Settle(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment,
                DclCastingOutcome.ResourceFailure);
            var failed = new DclActionTransaction(attempt.Declaration, profile);
            failed.FailResourceCommitment();
            return new DclCanonicalDispelExecutionResult(
                attempt.Declaration,
                DclCastingOutcome.ResourceFailure,
                CastingGate: null,
                failedPayment,
                Instances: [],
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
            throw new ArgumentNullException(nameof(input), "A payable Dispel requires its shared caster draw.");
        DclSpellGateResult castingGate = DclSpellResolution.ClassifySharedRoll(
            sharedCasterRoll,
            input.BaseSpellScore,
            input.TargetSpellScore);
        var instanceResults = new List<DclCanonicalDispelInstanceResult>();
        int resistanceDrawIndex = 0;
        foreach (DclStateInstance instance in eligibleInstances.Where(instance => selectedById.ContainsKey(instance.InstanceId)))
        {
            DclCanonicalDispelInstanceInput selected = selectedById[instance.InstanceId];
            if (!castingGate.BaseSucceeded || !castingGate.TargetSucceeded)
            {
                if (selected.EffectResistanceRoll is not null)
                    throw new ArgumentException("A failed Dispel casting gate cannot consume effect-resistance RNG.", nameof(input));
                instanceResults.Add(new DclCanonicalDispelInstanceResult(
                    instance.InstanceId,
                    Contest: null,
                    Removed: false,
                    instance.Kind));
                continue;
            }
            if (instance.Strength is not { } effectStrength)
                throw new InvalidOperationException($"Dispellable state instance {instance.InstanceId} has no stored EffectStrength.");
            int? resolvedResistanceRoll = selected.EffectResistanceRoll ?? executionBattle?.ExecutionRandom.Roll3D6(
                executionBattle.RollIdentity(
                    input.ActionInstanceId,
                    input.DeclarationRequest.Caster,
                    input.Target.Unit,
                    strikeIndex: 0,
                    DclRollSite.Resistance,
                    drawIndex: resistanceDrawIndex++));
            int effectResistanceRoll = resolvedResistanceRoll ??
                throw new ArgumentException("Each selected effect requires one resistance draw after successful casting.", nameof(input));
            DclQuickContestResult contest = DclStatusRules.ResolveDispel(
                input.FinalDispelScore,
                sharedCasterRoll,
                effectStrength,
                effectResistanceRoll);
            instanceResults.Add(new DclCanonicalDispelInstanceResult(
                instance.InstanceId,
                contest,
                Removed: contest.ActingSideWon,
                instance.Kind));
        }

        DclResourcePayment payment = DclMagicResources.Settle(
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment,
            castingGate.BaseOutcome);
        if (!payment.Legal)
            throw new InvalidOperationException("A prechecked Dispel cost became illegal during settlement.");
        long[] plannedRemovals = instanceResults.Where(result => result.Removed).Select(result => result.InstanceId).ToArray();
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
                AppliedEffectIndexes: plannedRemovals.Length > 0 ? [0] : [],
                TargetKoAfterStrike: false)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                if (plannedRemovals.Length > 0)
                    removed = input.StateRegistry.RemoveInstances(plannedRemovals);
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: input.ExecutionBattle?.States);
        return new DclCanonicalDispelExecutionResult(
            attempt.Declaration,
            castingGate.BaseOutcome,
            castingGate,
            payment,
            instanceResults,
            removed,
            committed.Transaction,
            Reactions: committed.Reactions);
    }
}
