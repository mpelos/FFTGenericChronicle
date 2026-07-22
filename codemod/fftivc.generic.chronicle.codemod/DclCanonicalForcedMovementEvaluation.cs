namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalForcedMovementEvaluationInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    DclDefenseOption Defense,
    int? ResistanceScore,
    bool Immune,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclCanonicalNativeMovementVerdict NativeMovementVerdict,
    DclCanonicalConcentrationTargetContext? ConcentrationContext = null);

internal sealed record DclCanonicalForcedMovementEvaluationResult(
    int AbilityId,
    string ActionId,
    string ForecastBoundary,
    string AiBoundary,
    bool Legal,
    IReadOnlyList<string> Failures,
    DclUnitKey Target,
    IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> DeliveryOutcomeProbability,
    DclCanonicalForcedMovementResult? DeliveredMovement,
    DclRational DeliveryProbability,
    DclRational DefenseAttemptProbability,
    DclRational BlockSpendProbability,
    DclRational ExpectedMovedTiles,
    DclCanonicalResourceEvaluation Resources,
    bool ResourceFailed,
    DclRational ConcentrationInterruptionProbability = default);

internal sealed record DclCanonicalForcedMovementForecast(
    int AbilityId,
    string ActionId,
    string ForecastBoundary,
    bool Legal,
    DclUnitKey Target,
    int DeliveryPercent,
    int DefenseAttemptPercent,
    int BlockSpendPercent,
    int RequestedTiles,
    DclForcedMovementDirection Direction,
    DclBattleTile? Origin,
    DclBattleTile? Destination,
    int MovedTilesOnDelivery,
    DclRational ExpectedMovedTiles,
    bool FellOnDelivery,
    int ConcentrationInterruptionPercent = 0);

internal sealed record DclCanonicalForcedMovementAiProjection(
    int AbilityId,
    string ActionId,
    string AiBoundary,
    bool Legal,
    DclUnitKey Target,
    DclRational DeliveryProbability,
    DclRational DefenseAttemptProbability,
    DclRational BlockSpendProbability,
    DclRational ExpectedMovedTiles,
    DclRational ExpectedCasterMpDebit,
    DclRational ExpectedCasterHpDebit,
    DclRational PayerKoProbability,
    DclRational ConcentrationInterruptionProbability = default);

internal static class DclCanonicalForcedMovementEvaluation
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);

    public static DclCanonicalForcedMovementEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalForcedMovementEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (profile.ForcedMovementProfile is null || profile.Effects.Count != 1 ||
            profile.Effects[0].Kind != DclEffectKind.ForcedMovement || profile.MagnitudeProfile is not null)
            throw new InvalidOperationException("ForcedMovement evaluation requires one normalized nonnumeric Carrier.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("ForcedMovement evaluation declaration/target identity diverged.", nameof(input));
        input.ConcentrationContext?.Validate();
        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, actionInstanceId: 1);
        DclMagicDefensePolicy.Validate(profile.DeliveryProfile, input.Defense, input.Target.DefenseResources);
        if (!attempt.Legal)
            return Empty(binding, input.Target.Unit, attempt.Failures, false);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution, input.CurrentHpAtResolution, attempt.CostCommitment))
            return Empty(binding, input.Target.Unit, ["resource-failure-at-resolution"], true);
        (IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> delivery,
            IReadOnlyDictionary<DclCastingOutcome, DclRational> baseOutcomes) =
            DclCanonicalMagicEvaluationExecutor.EnumerateDelivery(
                profile,
                input.BaseSpellScore,
                input.TargetSpellScore,
                input.Defense,
                input.ResistanceScore,
                input.Immune);
        DclRational deliveredProbability = delivery.GetValueOrDefault(DclMagicDeliveryOutcome.Delivered, Zero) +
            delivery.GetValueOrDefault(DclMagicDeliveryOutcome.CriticalDelivered, Zero);
        DclRational defenseAttemptProbability = DclMagicDefensePolicy.AttemptProbability(
            profile.DeliveryProfile,
            input.BaseSpellScore,
            input.TargetSpellScore,
            input.Defense);
        DclRational blockSpendProbability = input.Defense.Kind == DclDefenseKind.Block
            ? defenseAttemptProbability
            : Zero;
        DclCanonicalForcedMovementResult movement = DclCanonicalForcedMovement.Resolve(
            profile,
            input.Target.Unit,
            input.Target.Tile,
            targetKo: false,
            input.NativeMovementVerdict);
        DclCanonicalResourceEvaluation resources = DclCanonicalMagicEvaluationExecutor.EvaluateResources(
            baseOutcomes,
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment);
        DclRational concentrationInterruption = input.ConcentrationContext is
            { Charging: true } concentration && movement.CreatesConcentrationIncident
            ? deliveredProbability * new DclRational(
                216 - DclSuccessRoll.SuccessOutcomeCount(concentration.Score),
                216)
            : Zero;
        return new DclCanonicalForcedMovementEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: true,
            Failures: [],
            input.Target.Unit,
            delivery,
            movement,
            deliveredProbability,
            defenseAttemptProbability,
            blockSpendProbability,
            deliveredProbability * DclRational.FromInteger(movement.MovedTiles),
            resources,
            ResourceFailed: false,
            concentrationInterruption);
    }

    public static DclCanonicalForcedMovementForecast ProjectPlayer(
        DclCanonicalForcedMovementEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        DclCanonicalForcedMovementResult? movement = evaluation.DeliveredMovement;
        return new DclCanonicalForcedMovementForecast(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.ForecastBoundary,
            evaluation.Legal,
            evaluation.Target,
            checked((int)(evaluation.DeliveryProbability * DclRational.FromInteger(100)).RoundNearest()),
            checked((int)(evaluation.DefenseAttemptProbability * DclRational.FromInteger(100)).RoundNearest()),
            checked((int)(evaluation.BlockSpendProbability * DclRational.FromInteger(100)).RoundNearest()),
            movement?.RequestedTiles ?? 0,
            movement?.Direction ?? default,
            movement?.Origin,
            movement?.Destination,
            movement?.MovedTiles ?? 0,
            evaluation.ExpectedMovedTiles,
            movement?.Fell ?? false,
            checked((int)(evaluation.ConcentrationInterruptionProbability *
                DclRational.FromInteger(100)).RoundNearest()));
    }

    public static DclCanonicalForcedMovementAiProjection ProjectAi(
        DclCanonicalForcedMovementEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalForcedMovementAiProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.AiBoundary,
            evaluation.Legal,
            evaluation.Target,
            evaluation.DeliveryProbability,
            evaluation.DefenseAttemptProbability,
            evaluation.BlockSpendProbability,
            evaluation.ExpectedMovedTiles,
            evaluation.Resources.ExpectedMpDebit,
            evaluation.Resources.ExpectedHpDebit,
            evaluation.Resources.PayerKoProbability,
            evaluation.ConcentrationInterruptionProbability);
    }

    private static DclCanonicalForcedMovementEvaluationResult Empty(
        DclAbilityBinding binding,
        DclUnitKey target,
        IReadOnlyList<string> failures,
        bool resourceFailed)
        => new(
            binding.AbilityId,
            binding.ActionId,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: false,
            failures,
            target,
            new Dictionary<DclMagicDeliveryOutcome, DclRational>(),
            DeliveredMovement: null,
            DeliveryProbability: Zero,
            DefenseAttemptProbability: Zero,
            BlockSpendProbability: Zero,
            ExpectedMovedTiles: Zero,
            new DclCanonicalResourceEvaluation(
                new Dictionary<DclCastingOutcome, DclRational>(),
                new Dictionary<DclCastingOutcome, DclResourcePayment>(),
                Zero,
                Zero,
                Zero),
            resourceFailed,
            ConcentrationInterruptionProbability: Zero);
}
