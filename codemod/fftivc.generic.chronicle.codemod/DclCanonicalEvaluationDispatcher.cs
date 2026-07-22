namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalEvaluationDispatchResult(
    int AbilityId,
    DclCanonicalActionFamily Family,
    object? Evaluation,
    object? PlayerProjection,
    object? AiProjection,
    bool NativePassthrough);

internal sealed record DclCanonicalComposedEvaluation(
    int AbilityId,
    DclCanonicalActionFamily Family,
    object FamilyInput);

/// <summary>
/// One RNG-free entry point for native player forecast and AI planning. Family selection comes only
/// from the atomically validated ability binding; the native formula, animation, and caller cannot
/// substitute a different evaluator.
/// </summary>
internal static class DclCanonicalEvaluationDispatcher
{
    public static DclCanonicalEvaluationDispatchResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        object familyInput)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(familyInput);
        DclCanonicalActionFamily family = runtime.ResolveAbilityFamily(abilityId);
        return family switch
        {
            DclCanonicalActionFamily.PhysicalDamage when familyInput is DclCanonicalPhysicalEvaluationInput input =>
                Physical(runtime, abilityId, input),
            DclCanonicalActionFamily.DirectNumeric when familyInput is DclCanonicalMagicEvaluationInput input =>
                Direct(runtime, abilityId, input),
            DclCanonicalActionFamily.AreaNumeric when familyInput is DclCanonicalAreaMagicEvaluationInput input =>
                Area(runtime, abilityId, input),
            DclCanonicalActionFamily.StatusApplication when familyInput is DclCanonicalStatusEvaluationInput input =>
                Status(runtime, abilityId, input),
            DclCanonicalActionFamily.StatusRemoval when familyInput is DclCanonicalStatusRemovalEvaluationInput input =>
                StatusRemoval(runtime, abilityId, input),
            DclCanonicalActionFamily.Dispel when familyInput is DclCanonicalDispelEvaluationInput input =>
                Dispel(runtime, abilityId, input),
            DclCanonicalActionFamily.Quick when familyInput is DclCanonicalQuickEvaluationInput input =>
                Quick(runtime, abilityId, input),
            DclCanonicalActionFamily.Revive when familyInput is DclCanonicalReviveEvaluationInput input =>
                Revive(runtime, abilityId, input),
            DclCanonicalActionFamily.ForcedMovement when familyInput is DclCanonicalForcedMovementEvaluationInput input =>
                ForcedMovement(runtime, abilityId, input),
            DclCanonicalActionFamily.NativeSpecialPreserved => new DclCanonicalEvaluationDispatchResult(
                abilityId,
                family,
                Evaluation: null,
                PlayerProjection: null,
                AiProjection: null,
                NativePassthrough: true),
            _ => throw new ArgumentException(
                $"Ability {abilityId} family {family} requires its exact canonical evaluation input, not {familyInput.GetType().Name}.",
                nameof(familyInput)),
        };
    }

    public static DclCanonicalEvaluationDispatchResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalComposedEvaluation composed)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(composed);
        DclCanonicalActionFamily runtimeFamily = runtime.ResolveAbilityFamily(composed.AbilityId);
        if (runtimeFamily != composed.Family)
            throw new ArgumentException(
                "A composed canonical evaluation must retain the same atomic ability family as the runtime catalog.",
                nameof(composed));
        return Evaluate(runtime, composed.AbilityId, composed.FamilyInput);
    }

    private static DclCanonicalEvaluationDispatchResult Physical(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalPhysicalEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalPhysicalEvaluationResult evaluation = DclCanonicalPhysicalEvaluation.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.PhysicalDamage,
            evaluation,
            DclCanonicalPhysicalEvaluation.ProjectPlayer(evaluation),
            DclCanonicalPhysicalEvaluation.ProjectAi(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult Direct(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalMagicEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalMagicEvaluationResult evaluation = DclCanonicalMagicEvaluationExecutor.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.DirectNumeric,
            evaluation,
            DclCanonicalMagicForecastProjection.From(evaluation),
            DclCanonicalMagicAiProjection.From(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult Area(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalAreaMagicEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalAreaMagicEvaluationResult evaluation = DclCanonicalAreaMagicEvaluationExecutor.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.AreaNumeric,
            evaluation,
            DclCanonicalAreaMagicForecastProjection.From(evaluation),
            DclCanonicalAreaMagicAiProjection.From(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult Status(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalStatusEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalStatusEvaluationResult evaluation = DclCanonicalStatusEvaluation.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.StatusApplication,
            evaluation,
            DclCanonicalStatusEvaluation.ProjectPlayer(evaluation),
            DclCanonicalStatusEvaluation.ProjectAi(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult StatusRemoval(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalStatusRemovalEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalStatusRemovalEvaluationResult evaluation =
            DclCanonicalStatusRemovalEvaluation.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.StatusRemoval,
            evaluation,
            DclCanonicalStatusRemovalEvaluation.ProjectPlayer(evaluation),
            DclCanonicalStatusRemovalEvaluation.ProjectAi(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult Dispel(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalDispelEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalDispelEvaluationResult evaluation = DclCanonicalDispelEvaluation.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.Dispel,
            evaluation,
            DclCanonicalDispelEvaluation.ProjectPlayer(evaluation),
            DclCanonicalDispelEvaluation.ProjectAi(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult Quick(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalQuickEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalQuickEvaluationResult evaluation = DclCanonicalQuickEvaluation.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.Quick,
            evaluation,
            DclCanonicalQuickEvaluation.ProjectPlayer(evaluation),
            DclCanonicalQuickEvaluation.ProjectAi(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult Revive(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalReviveEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalReviveEvaluationResult evaluation = DclCanonicalReviveEvaluation.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.Revive,
            evaluation,
            DclCanonicalReviveEvaluation.ProjectPlayer(evaluation),
            DclCanonicalReviveEvaluation.ProjectAi(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult ForcedMovement(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalForcedMovementEvaluationInput input)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalForcedMovementEvaluationResult evaluation =
            DclCanonicalForcedMovementEvaluation.Evaluate(runtime, input);
        return Result(
            abilityId,
            DclCanonicalActionFamily.ForcedMovement,
            evaluation,
            DclCanonicalForcedMovementEvaluation.ProjectPlayer(evaluation),
            DclCanonicalForcedMovementEvaluation.ProjectAi(evaluation));
    }

    private static DclCanonicalEvaluationDispatchResult Result(
        int abilityId,
        DclCanonicalActionFamily family,
        object evaluation,
        object player,
        object ai)
        => new(abilityId, family, evaluation, player, ai, NativePassthrough: false);

    private static void RequireAbility(int dispatchedAbilityId, int inputAbilityId)
    {
        if (dispatchedAbilityId != inputAbilityId)
            throw new ArgumentException(
                $"Dispatched ability {dispatchedAbilityId} does not match family input ability {inputAbilityId}.");
    }
}
