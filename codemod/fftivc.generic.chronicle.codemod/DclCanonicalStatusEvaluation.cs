using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalStatusEvaluationInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int ResistanceScore,
    bool Immune,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    int? MaterializedDurationUnits = null);

internal sealed record DclCanonicalStatusEvaluationResult(
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    string StateKind,
    string ForecastBoundary,
    string AiBoundary,
    bool Legal,
    IReadOnlyList<string> Failures,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> DeliveryOutcomeProbability,
    IReadOnlyDictionary<int, DclRational> WinningMarginProbability,
    IReadOnlyDictionary<int, DclRational> DurationProbability,
    DclRational? ExpectedDurationUnits,
    DclCanonicalResourceEvaluation Resources,
    bool ResourceFailed)
{
    public DclRational ApplicationProbability
        => Get(DclMagicDeliveryOutcome.Delivered) + Get(DclMagicDeliveryOutcome.CriticalDelivered);

    private DclRational Get(DclMagicDeliveryOutcome outcome)
        => DeliveryOutcomeProbability.GetValueOrDefault(outcome, DclRational.FromInteger(0));
}

internal sealed record DclCanonicalStatusForecastProjection(
    int AbilityId,
    string ActionId,
    string StateKind,
    string ForecastBoundary,
    bool Legal,
    int ApplicationPercent,
    IReadOnlyDictionary<int, DclRational> WinningMarginProbability,
    IReadOnlyDictionary<int, DclRational> DurationProbability,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing);

internal sealed record DclCanonicalStatusAiProjection(
    int AbilityId,
    string ActionId,
    string StateKind,
    string AiBoundary,
    bool Legal,
    DclRational ApplicationProbability,
    IReadOnlyDictionary<int, DclRational> DurationProbability,
    DclRational? ExpectedDurationUnits,
    DclRational ExpectedCasterMpDebit,
    DclRational ExpectedCasterHpDebit,
    DclRational PayerKoProbability);

internal static class DclCanonicalStatusEvaluation
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);

    public static DclCanonicalStatusEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalStatusEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.StatusPacket or
                DclNativeCarrierKind.ConditionalStatusProducer or DclNativeCarrierKind.SingleResult) ||
            profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            {
                Kind: DclEffectKind.StatusApplication,
                ReferencedStateKind: { } stateKind,
            })
            throw new InvalidOperationException("Status evaluation requires one unit-targeted status Carrier.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision ||
            input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("Status evaluation lost its declaration target/profile revision.", nameof(input));
        if (!runtime.Authoring.States.TryGetValue(stateKind, out DclStateDefinition? stateDefinition))
            throw new InvalidOperationException("Status evaluation cannot resolve its referenced state definition.");
        if (profile.DeliveryProfile.Delivery == DclDelivery.Beneficial && input.Immune)
            throw new ArgumentException(
                "A Beneficial status with immunity requires an explicit effect-owned policy.", nameof(input));
        if (profile.DeliveryProfile.Delivery is not (DclDelivery.InternalDirect or DclDelivery.Beneficial))
            throw new InvalidOperationException("Status evaluation supports Internal Direct or Beneficial delivery.");

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, actionInstanceId: 1);
        if (!attempt.Legal)
            return Empty(binding, stateKind, attempt, attempt.Failures, ResourceFailed: false);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
            return Empty(binding, stateKind, attempt, ["resource-failure-at-resolution"], ResourceFailed: true);

        (Dictionary<DclMagicDeliveryOutcome, DclRational> delivery,
            Dictionary<int, DclRational> margins,
            Dictionary<DclCastingOutcome, DclRational> baseOutcomes) =
            Enumerate(profile, input);
        DclCanonicalResourceEvaluation resources = DclCanonicalMagicEvaluationExecutor.EvaluateResources(
            baseOutcomes,
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment);
        (IReadOnlyDictionary<int, DclRational> durations, DclRational? expectedDuration) =
            EvaluateDuration(stateDefinition, input.MaterializedDurationUnits, margins, delivery);
        return new DclCanonicalStatusEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            stateKind,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: true,
            Failures: [],
            attempt.CostCommitment,
            attempt.Timing,
            delivery,
            margins,
            durations,
            expectedDuration,
            resources,
            ResourceFailed: false);
    }

    public static DclCanonicalStatusForecastProjection ProjectPlayer(
        DclCanonicalStatusEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalStatusForecastProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.StateKind,
            evaluation.ForecastBoundary,
            evaluation.Legal,
            checked((int)(evaluation.ApplicationProbability * DclRational.FromInteger(100)).RoundNearest()),
            evaluation.WinningMarginProbability,
            evaluation.DurationProbability,
            evaluation.CostCommitment,
            evaluation.Timing);
    }

    public static DclCanonicalStatusAiProjection ProjectAi(
        DclCanonicalStatusEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalStatusAiProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.StateKind,
            evaluation.AiBoundary,
            evaluation.Legal,
            evaluation.ApplicationProbability,
            evaluation.DurationProbability,
            evaluation.ExpectedDurationUnits,
            evaluation.Resources.ExpectedMpDebit,
            evaluation.Resources.ExpectedHpDebit,
            evaluation.Resources.PayerKoProbability);
    }

    private static (Dictionary<DclMagicDeliveryOutcome, DclRational> Delivery,
        Dictionary<int, DclRational> Margins,
        Dictionary<DclCastingOutcome, DclRational> BaseOutcomes) Enumerate(
        DclActionProfile profile,
        DclCanonicalStatusEvaluationInput input)
    {
        var deliveryWeights = new Dictionary<DclMagicDeliveryOutcome, BigInteger>();
        var marginWeights = new Dictionary<int, BigInteger>();
        var baseWeights = new Dictionary<DclCastingOutcome, BigInteger>();
        BigInteger total = profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect && !input.Immune
            ? 216 * 216
            : 216;
        for (int casterRoll = DclSuccessRoll.MinRoll; casterRoll <= DclSuccessRoll.MaxRoll; casterRoll++)
        {
            int casterWeight = DclSuccessRoll.OutcomeMultiplicity(casterRoll);
            DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
                casterRoll,
                input.BaseSpellScore,
                input.TargetSpellScore);
            Add(baseWeights, gate.BaseOutcome, casterWeight);
            if (profile.DeliveryProfile.Delivery == DclDelivery.Beneficial)
            {
                DclMagicDeliveryOutcome outcome = DclSpellResolution.ResolveBeneficial(
                    casterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore).Outcome;
                Add(deliveryWeights, outcome, casterWeight);
                continue;
            }
            if (!gate.BaseSucceeded)
            {
                Add(deliveryWeights, DclMagicDeliveryOutcome.BaseFailure,
                    input.Immune ? casterWeight : casterWeight * 216);
                continue;
            }
            if (!gate.TargetSucceeded)
            {
                Add(deliveryWeights, DclMagicDeliveryOutcome.TargetFailure,
                    input.Immune ? casterWeight : casterWeight * 216);
                continue;
            }
            if (input.Immune)
            {
                Add(deliveryWeights, DclMagicDeliveryOutcome.Resisted, casterWeight);
                continue;
            }
            for (int resistanceRoll = DclSuccessRoll.MinRoll;
                 resistanceRoll <= DclSuccessRoll.MaxRoll;
                 resistanceRoll++)
            {
                int branchWeight = casterWeight * DclSuccessRoll.OutcomeMultiplicity(resistanceRoll);
                DclMagicDeliveryResult result = DclSpellResolution.ResolveInternal(
                    casterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore,
                    input.ResistanceScore,
                    resistanceRoll,
                    immune: false);
                Add(deliveryWeights, result.Outcome, branchWeight);
                if (result.Delivered)
                    Add(marginWeights, result.WinningMargin!.Value, branchWeight);
            }
        }
        return (
            deliveryWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, total)),
            marginWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, total)),
            baseWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, 216)));
    }

    internal static (IReadOnlyDictionary<int, DclRational> Probability, DclRational? Expected)
        EvaluateDuration(
            DclStateDefinition definition,
            int? materializedDurationUnits,
            IReadOnlyDictionary<int, DclRational> marginProbability,
            IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> deliveryProbability)
    {
        DclStateDurationRule rule = DclStateDurationRules.Parse(definition.Duration);
        if (rule.Kind == DclStateDurationRuleKind.None)
        {
            _ = DclStateDurationRules.Resolve(definition.Duration, winningMargin: null, materializedDurationUnits);
            return (new Dictionary<int, DclRational>(), null);
        }
        var durations = new Dictionary<int, DclRational>();
        if (rule.Kind == DclStateDurationRuleKind.WinningMargin)
        {
            foreach ((int margin, DclRational probability) in marginProbability)
            {
                int duration = DclStateDurationRules.Resolve(
                    definition.Duration,
                    margin,
                    materializedDurationUnits)!.Value;
                durations[duration] = durations.GetValueOrDefault(duration, Zero) + probability;
            }
        }
        else
        {
            DclRational applicationProbability = deliveryProbability.GetValueOrDefault(
                    DclMagicDeliveryOutcome.Delivered,
                    Zero) +
                deliveryProbability.GetValueOrDefault(DclMagicDeliveryOutcome.CriticalDelivered, Zero);
            int duration = DclStateDurationRules.Resolve(
                definition.Duration,
                winningMargin: null,
                materializedDurationUnits)!.Value;
            if (applicationProbability > Zero)
                durations.Add(duration, applicationProbability);
        }
        DclRational expected = durations.Aggregate(
            Zero,
            (total, pair) => total + DclRational.FromInteger(pair.Key) * pair.Value);
        return (durations, expected);
    }

    private static DclCanonicalStatusEvaluationResult Empty(
        DclAbilityBinding binding,
        string stateKind,
        DclCastDeclarationAttempt attempt,
        IReadOnlyList<string> failures,
        bool ResourceFailed)
        => new(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            stateKind,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: false,
            failures,
            attempt.CostCommitment,
            attempt.Timing,
            new Dictionary<DclMagicDeliveryOutcome, DclRational>(),
            new Dictionary<int, DclRational>(),
            new Dictionary<int, DclRational>(),
            null,
            new DclCanonicalResourceEvaluation(
                new Dictionary<DclCastingOutcome, DclRational>(),
                new Dictionary<DclCastingOutcome, DclResourcePayment>(),
                Zero,
                Zero,
                Zero),
            ResourceFailed);

    private static void Add<TKey>(Dictionary<TKey, BigInteger> weights, TKey key, BigInteger value)
        where TKey : notnull
        => weights[key] = weights.GetValueOrDefault(key) + value;
}
