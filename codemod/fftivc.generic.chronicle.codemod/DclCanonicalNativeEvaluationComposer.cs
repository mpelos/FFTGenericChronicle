namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeComposedPlan(
    DclCanonicalNativeComposedExecution Execution,
    DclCanonicalComposedEvaluation Evaluation);

/// <summary>
/// Native forecast/AI composition boundary. It consumes the same classified native admission,
/// synchronized snapshots, and explicit family-policy inputs as confirmed execution, then projects
/// the deterministic request into the family's RNG-free evaluation input. Execution-only identity,
/// Reaction windows, state materialization side effects, and sampled rolls do not cross this
/// boundary.
/// </summary>
internal static class DclCanonicalNativeEvaluationComposer
{
    public static DclCanonicalNativeComposedPlan ComposePlanCaptured(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action,
        IEnumerable<DclCanonicalNativeUnitPolicyInput> unitPolicyInputs,
        object familyPolicyInputs)
    {
        DclCanonicalNativeComposedExecution execution = DclCanonicalNativeConfirmedRequestComposer.ComposeCaptured(
            battle,
            action,
            unitPolicyInputs,
            familyPolicyInputs);
        return Plan(execution);
    }

    public static DclCanonicalNativeComposedPlan ComposePlan(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        DclCanonicalNativeSnapshotBatch snapshots,
        object familyPolicyInputs)
    {
        DclCanonicalNativeComposedExecution execution = DclCanonicalNativeConfirmedRequestComposer.Compose(
            battle,
            admissions,
            snapshots,
            familyPolicyInputs);
        return Plan(execution);
    }

    public static DclCanonicalComposedEvaluation ComposeCaptured(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action,
        IEnumerable<DclCanonicalNativeUnitPolicyInput> unitPolicyInputs,
        object familyPolicyInputs)
    {
        DclCanonicalNativeComposedExecution execution = DclCanonicalNativeConfirmedRequestComposer.ComposeCaptured(
            battle,
            action,
            unitPolicyInputs,
            familyPolicyInputs);
        return FromExecution(execution);
    }

    public static DclCanonicalComposedEvaluation Compose(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        DclCanonicalNativeSnapshotBatch snapshots,
        object familyPolicyInputs)
    {
        DclCanonicalNativeComposedExecution execution = DclCanonicalNativeConfirmedRequestComposer.Compose(
            battle,
            admissions,
            snapshots,
            familyPolicyInputs);
        return FromExecution(execution);
    }

    public static DclCanonicalNativeComposedPlan Plan(DclCanonicalNativeComposedExecution execution)
    {
        DclCanonicalComposedEvaluation evaluation = FromExecution(execution);
        if (evaluation.AbilityId != execution.AbilityId || evaluation.Family != execution.Family)
            throw new InvalidOperationException("Native execution and evaluation composition lost their shared family identity.");
        return new DclCanonicalNativeComposedPlan(execution, evaluation);
    }

    public static DclCanonicalComposedEvaluation FromExecution(DclCanonicalNativeComposedExecution execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        object evaluation = execution.FamilyInput switch
        {
            DclCanonicalPhysicalExecutionRequest request => Physical(request),
            DclCanonicalMagicExecutionRequest request => Direct(request),
            DclCanonicalAreaMagicExecutionRequest request => Area(request),
            DclCanonicalStatusExecutionInput request => Status(request),
            DclCanonicalStatusRemovalExecutionInput request => StatusRemoval(request),
            DclCanonicalDispelExecutionInput request => Dispel(request),
            DclCanonicalQuickExecutionInput request => Quick(request),
            DclCanonicalReviveExecutionInput request => Revive(request),
            DclCanonicalForcedMovementExecutionRequest request => ForcedMovement(request),
            _ => throw new ArgumentException(
                $"Native execution input {execution.FamilyInput.GetType().Name} has no canonical forecast/AI projection.",
                nameof(execution)),
        };
        return new DclCanonicalComposedEvaluation(execution.AbilityId, execution.Family, evaluation);
    }

    private static DclCanonicalPhysicalEvaluationInput Physical(DclCanonicalPhysicalExecutionRequest request)
    {
        RejectSampledPhysical(request);
        return new DclCanonicalPhysicalEvaluationInput(
            request.AbilityId,
            request.WeaponItemId,
            request.SourceSt,
            request.Targets,
            request.Strikes,
            request.TargetContexts,
            request.Strikes
                .SelectMany(strike => (strike.StatusRiders ?? []).Select(rider =>
                    new DclCanonicalPhysicalRiderForecast(
                        strike.Target,
                        strike.StrikeIndex,
                        rider.EffectIndex,
                        rider.ResistanceScore,
                        rider.Immune)))
                .ToArray(),
            request.StrikeWeapons);
    }

    private static DclCanonicalMagicEvaluationInput Direct(DclCanonicalMagicExecutionRequest request)
    {
        RejectSampledDirect(request);
        return new DclCanonicalMagicEvaluationInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.Target,
            request.BaseSpellScore,
            request.TargetSpellScore,
            request.Defense,
            request.MagnitudeAttribute,
            request.Affinity,
            request.FaithMagnitude,
            request.TargetHasShell,
            request.TargetMaxHp,
            request.FireEffect,
            request.OilContributed,
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            request.DeclaredTargetHasReflect,
            request.ReflectionAlreadyConsumed,
            request.ReflectedTarget,
            request.ApplicableDr,
            request.AdditionalMagnitudeIntegerModifier,
            request.ResistanceScore,
            request.Immune,
            request.ResourceTargetPools,
            request.ResourceSourcePools,
            request.TouchDefenseCandidates,
            request.TouchRouteVerdict,
            StatusRiders(request.StatusRiders),
            request.InjuryTargetContext,
            request.AuthoredForcedDisplacement,
            request.InjuryMovementBranches);
    }

    private static DclCanonicalAreaMagicEvaluationInput Area(DclCanonicalAreaMagicExecutionRequest request)
    {
        RejectSampledArea(request);
        return new DclCanonicalAreaMagicEvaluationInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.CurrentUnits,
            request.NativeGeometricMembers,
            request.BaseSpellScore,
            request.Targets.Select(target => new DclCanonicalAreaTargetEvaluationInput(
                target.Target,
                target.TargetSpellScore,
                target.Dodge?.Target ?? target.ResistanceScore,
                target.MagnitudeAttribute,
                target.Affinity,
                target.FaithMagnitude,
                target.TargetHasShell,
                target.TargetMaxHp,
                target.FireEffect,
                target.OilContributed,
                target.ApplicableDr,
                target.AdditionalMagnitudeIntegerModifier,
                StatusRiders(target.StatusRiders),
                target.InjuryTargetContext?.EffectiveHt,
                target.ResourceTargetPools,
                target.ForcedMovementVerdict,
                target.ForcedMovementImmune,
                target.ForcedMovementConcentrationContext,
                target.InjuryTargetContext,
                target.AuthoredForcedDisplacement,
                target.InjuryMovementBranchesByStrike,
                target.InjuryMovementBranchForestsByStrike)).ToArray(),
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            request.ResourceSourcePools);
    }

    private static DclCanonicalStatusEvaluationInput Status(DclCanonicalStatusExecutionInput request)
    {
        if (request.SharedCasterRoll is not null || request.ResistanceRoll is not null ||
            request.ExecutionBattle is not null || request.ReactionWindow is not null)
            throw new ArgumentException("Status forecast/AI composition cannot consume sampled rolls or execution-only owners.");
        return new DclCanonicalStatusEvaluationInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.Target,
            request.BaseSpellScore,
            request.TargetSpellScore,
            request.ResistanceScore,
            request.Immune,
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            request.StateMaterialization.DurationUnits);
    }

    private static DclCanonicalStatusRemovalEvaluationInput StatusRemoval(
        DclCanonicalStatusRemovalExecutionInput request)
    {
        if (request.SharedCasterRoll is not null || request.ExecutionBattle is not null ||
            request.ReactionWindow is not null)
            throw new ArgumentException(
                "StatusRemoval forecast/AI composition cannot consume sampled rolls or execution-only owners.");
        return new DclCanonicalStatusRemovalEvaluationInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.Target,
            request.BaseSpellScore,
            request.TargetSpellScore,
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            request.StateRegistry);
    }

    private static DclCanonicalDispelEvaluationInput Dispel(DclCanonicalDispelExecutionInput request)
    {
        if (request.SharedCasterRoll is not null ||
            request.SelectedInstances.Any(instance => instance.EffectResistanceRoll is not null) ||
            request.ExecutionBattle is not null || request.ReactionWindow is not null)
            throw new ArgumentException("Dispel forecast/AI composition cannot consume sampled rolls or execution-only owners.");
        return new DclCanonicalDispelEvaluationInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.Target,
            request.BaseSpellScore,
            request.TargetSpellScore,
            request.FinalDispelScore,
            request.SelectedInstances.Select(instance => instance.InstanceId).ToArray(),
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            request.StateRegistry);
    }

    private static DclCanonicalQuickEvaluationInput Quick(DclCanonicalQuickExecutionInput request)
    {
        if (request.SharedCasterRoll is not null || request.ExecutionBattle is not null ||
            request.ReactionWindow is not null)
            throw new ArgumentException("Quick forecast/AI composition cannot consume sampled rolls or execution-only owners.");
        return new DclCanonicalQuickEvaluationInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.Target,
            request.BaseSpellScore,
            request.TargetSpellScore,
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            request.TargetCt,
            request.QuickLocks,
            request.StateRegistry);
    }

    private static DclCanonicalReviveEvaluationInput Revive(DclCanonicalReviveExecutionInput request)
    {
        if (request.SharedCasterRoll is not null || request.ResistanceRoll is not null ||
            request.RestoredHpDice is not null || request.ExecutionBattle is not null ||
            request.ReactionWindow is not null)
            throw new ArgumentException("Revive forecast/AI composition cannot consume sampled rolls or execution-only owners.");
        return new DclCanonicalReviveEvaluationInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.Target,
            request.BaseSpellScore,
            request.TargetSpellScore,
            request.ResistanceScore,
            request.Immune,
            request.FaithMultiplier,
            request.TargetUndead,
            request.TargetMaxHp,
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            request.UndeadInteractions,
            request.StateRegistry,
            request.StateMaterialization);
    }

    private static DclCanonicalForcedMovementEvaluationInput ForcedMovement(
        DclCanonicalForcedMovementExecutionRequest request)
        => new(
            request.AbilityId,
            request.DeclarationRequest,
            request.Target,
            request.BaseSpellScore,
            request.TargetSpellScore,
            request.Defense,
            request.ResistanceScore,
            request.Immune,
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            request.NativeMovementVerdict,
            request.ConcentrationContext);

    private static IReadOnlyList<DclMagicStatusRiderForecast>? StatusRiders(
        IReadOnlyList<DclCanonicalStatusRiderExecutionRequest>? riders)
        => riders?.Select(rider => new DclMagicStatusRiderForecast(
            rider.EffectIndex,
            rider.ResistanceScore,
            rider.Immune)).ToArray();

    private static void RejectSampledPhysical(DclCanonicalPhysicalExecutionRequest request)
    {
        if (request.ReactionCandidates is not null)
            throw new ArgumentException("Physical forecast/AI composition cannot consume Reaction candidates.");
        foreach (DclCanonicalPhysicalStrikeInput strike in request.Strikes)
        {
            if (strike.AttackRoll is not null || strike.DefenseRoll is not null || strike.DamageDice is not null ||
                strike.MajorWoundHtRoll is not null || strike.ConcentrationRoll is not null ||
                strike.AimRetentionRoll is not null ||
                (strike.StatusRiders ?? []).Any(rider => rider.ResistanceRoll is not null))
                throw new ArgumentException("Physical forecast/AI composition cannot consume sampled Strike rolls.");
        }
    }

    private static void RejectSampledDirect(DclCanonicalMagicExecutionRequest request)
    {
        if (request.ReactionCandidates is not null)
            throw new ArgumentException("Direct forecast/AI composition cannot consume Reaction candidates.");
        if ((request.StatusRiders ?? []).Any(rider => rider.StateRegistry is null))
            throw new ArgumentException("Direct status Rider composition lost its state registry.");
    }

    private static void RejectSampledArea(DclCanonicalAreaMagicExecutionRequest request)
    {
        if (request.ReactionCandidates is not null)
            throw new ArgumentException("Area forecast/AI composition cannot consume Reaction candidates.");
        if (request.Targets.Select(target => target.Target.Unit).Distinct().Count() != request.Targets.Count)
            throw new ArgumentException("Area forecast/AI composition requires unique target snapshots.");
    }
}
