using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalStatusRemovalEvaluationInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclStateRegistry StateRegistry);

internal sealed record DclCanonicalStatusRemovalOutcome(
    IReadOnlyList<long> RemovedInstanceIds,
    DclRational Probability);

internal sealed record DclCanonicalStatusRemovalEvaluationResult(
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    string StateKind,
    string ForecastBoundary,
    string AiBoundary,
    bool Legal,
    IReadOnlyList<string> Failures,
    DclUnitKey Target,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    IReadOnlyList<long> MatchingInstanceIds,
    IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> DeliveryOutcomeProbability,
    IReadOnlyList<DclCanonicalStatusRemovalOutcome> RemovalOutcomes,
    DclRational RemovalProbability,
    DclRational ExpectedRemovedCount,
    DclCanonicalResourceEvaluation Resources,
    bool ResourceFailed);

internal sealed record DclCanonicalStatusRemovalForecastProjection(
    int AbilityId,
    string ActionId,
    string StateKind,
    string ForecastBoundary,
    bool Legal,
    DclUnitKey Target,
    int RemovalPercent,
    IReadOnlyList<long> MatchingInstanceIds,
    IReadOnlyList<DclCanonicalStatusRemovalOutcome> RemovalOutcomes,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing);

internal sealed record DclCanonicalStatusRemovalAiProjection(
    int AbilityId,
    string ActionId,
    string StateKind,
    string AiBoundary,
    bool Legal,
    DclUnitKey Target,
    DclRational RemovalProbability,
    DclRational ExpectedRemovedCount,
    IReadOnlyList<long> MatchingInstanceIds,
    IReadOnlyList<DclCanonicalStatusRemovalOutcome> RemovalOutcomes,
    DclRational ExpectedCasterMpDebit,
    DclRational ExpectedCasterHpDebit,
    DclRational PayerKoProbability);

internal static class DclCanonicalStatusRemovalEvaluation
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);
    private static readonly DclRational One = DclRational.FromInteger(1);

    public static DclCanonicalStatusRemovalEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalStatusRemovalEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.StatusPacket or
                DclNativeCarrierKind.ConditionalStatusProducer or DclNativeCarrierKind.SingleResult) ||
            profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.DeliveryProfile.Delivery != DclDelivery.Beneficial || profile.MagnitudeProfile is not null ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            {
                Kind: DclEffectKind.StatusRemoval,
                Role: DclEffectRole.Carrier,
                ReferencedStateKind: { } stateKind,
            })
            throw new InvalidOperationException(
                "Status-removal evaluation requires one Beneficial named StatusRemoval Carrier.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision ||
            input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit ||
            input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration)
            throw new ArgumentException(
                "Status-removal evaluation target/declaration/registry identities must agree.", nameof(input));

        long[] matchingIds = input.StateRegistry.CaptureTarget(input.Target.Unit).Instances
            .Where(instance => StringComparer.Ordinal.Equals(instance.Kind, stateKind))
            .Select(instance => instance.InstanceId)
            .ToArray();
        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, actionInstanceId: 1);
        if (!attempt.Legal)
            return Empty(binding, stateKind, input.Target.Unit, attempt, attempt.Failures, false);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
            return Empty(
                binding,
                stateKind,
                input.Target.Unit,
                attempt,
                ["resource-failure-at-resolution"],
                true);

        (IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> delivery,
            IReadOnlyDictionary<DclCastingOutcome, DclRational> baseOutcomes) =
            Enumerate(input.BaseSpellScore, input.TargetSpellScore);
        DclRational deliveryProbability = delivery.GetValueOrDefault(
                DclMagicDeliveryOutcome.Delivered,
                Zero) +
            delivery.GetValueOrDefault(DclMagicDeliveryOutcome.CriticalDelivered, Zero);
        DclRational removalProbability = matchingIds.Length == 0 ? Zero : deliveryProbability;
        var outcomes = new List<DclCanonicalStatusRemovalOutcome>();
        if (removalProbability < One)
            outcomes.Add(new DclCanonicalStatusRemovalOutcome([], One - removalProbability));
        if (removalProbability > Zero)
            outcomes.Add(new DclCanonicalStatusRemovalOutcome(matchingIds, removalProbability));
        DclCanonicalResourceEvaluation resources = DclCanonicalMagicEvaluationExecutor.EvaluateResources(
            baseOutcomes,
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment);
        return new DclCanonicalStatusRemovalEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            stateKind,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: true,
            Failures: [],
            input.Target.Unit,
            attempt.CostCommitment,
            attempt.Timing,
            matchingIds,
            delivery,
            outcomes,
            removalProbability,
            removalProbability * DclRational.FromInteger(matchingIds.Length),
            resources,
            ResourceFailed: false);
    }

    public static DclCanonicalStatusRemovalForecastProjection ProjectPlayer(
        DclCanonicalStatusRemovalEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalStatusRemovalForecastProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.StateKind,
            evaluation.ForecastBoundary,
            evaluation.Legal,
            evaluation.Target,
            checked((int)(evaluation.RemovalProbability * DclRational.FromInteger(100)).RoundNearest()),
            evaluation.MatchingInstanceIds,
            evaluation.RemovalOutcomes,
            evaluation.CostCommitment,
            evaluation.Timing);
    }

    public static DclCanonicalStatusRemovalAiProjection ProjectAi(
        DclCanonicalStatusRemovalEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalStatusRemovalAiProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.StateKind,
            evaluation.AiBoundary,
            evaluation.Legal,
            evaluation.Target,
            evaluation.RemovalProbability,
            evaluation.ExpectedRemovedCount,
            evaluation.MatchingInstanceIds,
            evaluation.RemovalOutcomes,
            evaluation.Resources.ExpectedMpDebit,
            evaluation.Resources.ExpectedHpDebit,
            evaluation.Resources.PayerKoProbability);
    }

    private static (
        IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> Delivery,
        IReadOnlyDictionary<DclCastingOutcome, DclRational> BaseOutcomes) Enumerate(
        int baseSpellScore,
        int targetSpellScore)
    {
        var deliveryWeights = new Dictionary<DclMagicDeliveryOutcome, BigInteger>();
        var baseWeights = new Dictionary<DclCastingOutcome, BigInteger>();
        for (int casterRoll = DclSuccessRoll.MinRoll; casterRoll <= DclSuccessRoll.MaxRoll; casterRoll++)
        {
            BigInteger weight = DclSuccessRoll.OutcomeMultiplicity(casterRoll);
            DclMagicDeliveryResult result = DclSpellResolution.ResolveBeneficial(
                casterRoll,
                baseSpellScore,
                targetSpellScore);
            Add(deliveryWeights, result.Outcome, weight);
            Add(baseWeights, result.Gate.BaseOutcome, weight);
        }
        return (
            deliveryWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, 216)),
            baseWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, 216)));
    }

    private static DclCanonicalStatusRemovalEvaluationResult Empty(
        DclAbilityBinding binding,
        string stateKind,
        DclUnitKey target,
        DclCastDeclarationAttempt attempt,
        IReadOnlyList<string> failures,
        bool resourceFailed)
        => new(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            stateKind,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: false,
            failures,
            target,
            attempt.CostCommitment,
            attempt.Timing,
            MatchingInstanceIds: [],
            new Dictionary<DclMagicDeliveryOutcome, DclRational>(),
            RemovalOutcomes: [],
            Zero,
            Zero,
            new DclCanonicalResourceEvaluation(
                new Dictionary<DclCastingOutcome, DclRational>(),
                new Dictionary<DclCastingOutcome, DclResourcePayment>(),
                Zero,
                Zero,
                Zero),
            resourceFailed);

    private static void Add<TKey>(Dictionary<TKey, BigInteger> weights, TKey key, BigInteger value)
        where TKey : notnull
        => weights[key] = weights.GetValueOrDefault(key) + value;
}
