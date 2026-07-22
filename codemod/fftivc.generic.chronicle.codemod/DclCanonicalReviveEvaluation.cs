using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalReviveEvaluationInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int? ResistanceScore,
    bool Immune,
    DclRational FaithMultiplier,
    bool TargetUndead,
    int TargetMaxHp,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclUndeadInteractionTable UndeadInteractions,
    DclStateRegistry? StateRegistry = null,
    DclCanonicalStateMaterialization? StateMaterialization = null);

internal sealed record DclCanonicalReviveEvaluationResult(
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    DclReviveMode Mode,
    string? StoredStateKind,
    string ForecastBoundary,
    string AiBoundary,
    bool Legal,
    IReadOnlyList<string> Failures,
    DclUnitKey Target,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    DclDiceExpression RestoredHpExpression,
    IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> DeliveryOutcomeProbability,
    IReadOnlyDictionary<DclReviveRoute, DclRational> RouteProbability,
    DclExactValueForecast RawRestoredHp,
    DclExactValueForecast FinalRestoredHp,
    DclExactValueForecast AppliedHpCredit,
    DclRational EffectApplicationProbability,
    DclRational ClearKoProbability,
    DclRational StoreReraiseProbability,
    DclCanonicalResourceEvaluation Resources,
    bool ResourceFailed);

internal sealed record DclCanonicalReviveForecastProjection(
    int AbilityId,
    string ActionId,
    DclReviveMode Mode,
    string? StoredStateKind,
    string ForecastBoundary,
    bool Legal,
    DclUnitKey Target,
    int EffectApplicationPercent,
    int ClearKoPercent,
    int StoreReraisePercent,
    DclDiceExpression RestoredHpExpression,
    DclExactValueForecast AppliedHpCredit,
    IReadOnlyDictionary<DclReviveRoute, DclRational> RouteProbability,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing);

internal sealed record DclCanonicalReviveAiProjection(
    int AbilityId,
    string ActionId,
    DclReviveMode Mode,
    string? StoredStateKind,
    string AiBoundary,
    bool Legal,
    DclUnitKey Target,
    DclRational EffectApplicationProbability,
    DclRational ClearKoProbability,
    DclRational StoreReraiseProbability,
    DclRational ExpectedAppliedHpCredit,
    DclExactValueForecast AppliedHpCredit,
    IReadOnlyDictionary<DclReviveRoute, DclRational> RouteProbability,
    DclRational ExpectedCasterMpDebit,
    DclRational ExpectedCasterHpDebit,
    DclRational PayerKoProbability);

internal static class DclCanonicalReviveEvaluation
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);
    private static readonly DclRational One = DclRational.FromInteger(1);

    public static DclCanonicalReviveEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalReviveEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.LifecycleTransaction or
                DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket) ||
            profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            {
                Kind: DclEffectKind.Revive,
                Role: DclEffectRole.Carrier,
            } reviveEffect || profile.ReviveProfile is null ||
            profile.MagnitudeProfile is not DclFixedResourceMagnitude
            {
                Resource: DclResourceKind.Hp,
            } restoredMagnitude)
            throw new InvalidOperationException(
                "Revive evaluation requires one unit-targeted Revive Carrier and its restored-HP profile.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision ||
            input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("Revive evaluation lost its declaration target/profile revision.", nameof(input));
        if (input.TargetMaxHp < 1 || input.Target.CurrentHp > input.TargetMaxHp || input.FaithMultiplier < Zero)
            throw new ArgumentOutOfRangeException(nameof(input), "Revive MaxHP/Faith inputs are invalid.");

        DclReviveProfile reviveProfile = profile.ReviveProfile;
        DclReviveRules.Validate(reviveProfile);
        DclDiceExpression restoredExpression = DclDiceExpression.ParseAuthored(restoredMagnitude.Expression);
        string? storedStateKind = null;
        if (reviveProfile.Mode == DclReviveMode.StoredReraise)
        {
            if (reviveEffect.ReferencedStateKind is not { } referencedStateKind ||
                !runtime.Authoring.States.TryGetValue(referencedStateKind, out DclStateDefinition? definition))
                throw new InvalidOperationException("Stored Reraise evaluation lost its trigger-state definition.");
            storedStateKind = referencedStateKind;
            if (input.StateRegistry is null || input.StateMaterialization is null ||
                input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration ||
                !StringComparer.Ordinal.Equals(input.StateMaterialization.Payload.SchemaId, definition.PayloadSchema))
                throw new ArgumentException(
                    "Stored Reraise evaluation requires matching registry/materialization ownership.", nameof(input));
        }
        else if (input.StateRegistry is not null || input.StateMaterialization is not null)
        {
            throw new ArgumentException("Immediate Revive evaluation cannot own stored-state materialization.", nameof(input));
        }

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, actionInstanceId: 1);
        if (!attempt.Legal)
            return Empty(
                binding,
                reviveProfile.Mode,
                storedStateKind,
                input.Target.Unit,
                attempt,
                attempt.Failures,
                restoredExpression,
                false);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
            return Empty(
                binding,
                reviveProfile.Mode,
                storedStateKind,
                input.Target.Unit,
                attempt,
                ["resource-failure-at-resolution"],
                restoredExpression,
                true);

        (IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> delivery,
            IReadOnlyDictionary<DclCastingOutcome, DclRational> baseOutcomes) =
            EnumerateDelivery(profile.DeliveryProfile.Delivery, input);
        var routeProbability = new Dictionary<DclReviveRoute, DclRational>();
        foreach ((DclMagicDeliveryOutcome outcome, DclRational probability) in delivery)
        {
            bool delivered = outcome is DclMagicDeliveryOutcome.Delivered or
                DclMagicDeliveryOutcome.CriticalDelivered;
            DclReviveRoute route = DclReviveRules.Resolve(
                reviveProfile,
                input.UndeadInteractions,
                input.Target.States,
                input.TargetUndead,
                delivered,
                rawRestoredHp: 0,
                input.FaithMultiplier,
                input.Target.CurrentHp,
                input.TargetMaxHp).Route;
            routeProbability[route] = routeProbability.GetValueOrDefault(route, Zero) + probability;
        }

        DclRational immediateProbability = routeProbability.GetValueOrDefault(
            DclReviveRoute.ImmediateHpCredit,
            Zero);
        DclRational storeProbability = routeProbability.GetValueOrDefault(DclReviveRoute.StoredReraise, Zero);
        DclExactIntegerDistribution rolled = DclExactIntegerDistribution.Roll(restoredExpression)
            .Transform(raw => Math.Max(0, raw));
        DclExactIntegerDistribution final = rolled.Transform(raw =>
            reviveProfile.FaithAxis is DclReviveFaithAxis.RestoredHp or DclReviveFaithAxis.BothExplicit
                ? checked((int)(DclRational.FromInteger(raw) * input.FaithMultiplier).Floor())
                : raw);
        DclExactIntegerDistribution applied = final.Transform(value =>
            Math.Min(value, input.TargetMaxHp - input.Target.CurrentHp));
        DclExactValueForecast rawForecast = DclExactValueForecast.Mixture(
            One - immediateProbability,
            [(immediateProbability, rolled)]);
        DclExactValueForecast finalForecast = DclExactValueForecast.Mixture(
            One - immediateProbability,
            [(immediateProbability, final)]);
        DclExactValueForecast appliedForecast = DclExactValueForecast.Mixture(
            One - immediateProbability,
            [(immediateProbability, applied)]);
        DclRational clearKoProbability = One - appliedForecast.ProbabilityOf(0);
        DclRational effectApplicationProbability = clearKoProbability + storeProbability;
        DclCanonicalResourceEvaluation resources = DclCanonicalMagicEvaluationExecutor.EvaluateResources(
            baseOutcomes,
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment);
        return new DclCanonicalReviveEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            reviveProfile.Mode,
            storedStateKind,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: true,
            Failures: [],
            input.Target.Unit,
            attempt.CostCommitment,
            attempt.Timing,
            restoredExpression,
            delivery,
            routeProbability,
            rawForecast,
            finalForecast,
            appliedForecast,
            effectApplicationProbability,
            clearKoProbability,
            storeProbability,
            resources,
            ResourceFailed: false);
    }

    public static DclCanonicalReviveForecastProjection ProjectPlayer(
        DclCanonicalReviveEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalReviveForecastProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.Mode,
            evaluation.StoredStateKind,
            evaluation.ForecastBoundary,
            evaluation.Legal,
            evaluation.Target,
            Percent(evaluation.EffectApplicationProbability),
            Percent(evaluation.ClearKoProbability),
            Percent(evaluation.StoreReraiseProbability),
            evaluation.RestoredHpExpression,
            evaluation.AppliedHpCredit,
            evaluation.RouteProbability,
            evaluation.CostCommitment,
            evaluation.Timing);
    }

    public static DclCanonicalReviveAiProjection ProjectAi(
        DclCanonicalReviveEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalReviveAiProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.Mode,
            evaluation.StoredStateKind,
            evaluation.AiBoundary,
            evaluation.Legal,
            evaluation.Target,
            evaluation.EffectApplicationProbability,
            evaluation.ClearKoProbability,
            evaluation.StoreReraiseProbability,
            evaluation.AppliedHpCredit.ExpectedValue,
            evaluation.AppliedHpCredit,
            evaluation.RouteProbability,
            evaluation.Resources.ExpectedMpDebit,
            evaluation.Resources.ExpectedHpDebit,
            evaluation.Resources.PayerKoProbability);
    }

    private static (
        IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> Delivery,
        IReadOnlyDictionary<DclCastingOutcome, DclRational> BaseOutcomes) EnumerateDelivery(
        DclDelivery deliveryKind,
        DclCanonicalReviveEvaluationInput input)
    {
        if (deliveryKind == DclDelivery.Beneficial &&
            (input.ResistanceScore is not null || input.Immune))
            throw new ArgumentException("Beneficial Revive evaluation has no resistance or immunity gate.", nameof(input));
        if (deliveryKind == DclDelivery.InternalDirect && input.ResistanceScore is null)
            throw new ArgumentException("Internal Direct Revive evaluation requires ResistanceScore.", nameof(input));
        if (deliveryKind is not (DclDelivery.Beneficial or DclDelivery.InternalDirect))
            throw new InvalidOperationException("Revive evaluation delivery must be Beneficial or InternalDirect.");

        var deliveryWeights = new Dictionary<DclMagicDeliveryOutcome, BigInteger>();
        var baseWeights = new Dictionary<DclCastingOutcome, BigInteger>();
        BigInteger total = deliveryKind == DclDelivery.InternalDirect && !input.Immune ? 216 * 216 : 216;
        for (int casterRoll = DclSuccessRoll.MinRoll; casterRoll <= DclSuccessRoll.MaxRoll; casterRoll++)
        {
            int casterWeight = DclSuccessRoll.OutcomeMultiplicity(casterRoll);
            DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
                casterRoll,
                input.BaseSpellScore,
                input.TargetSpellScore);
            Add(baseWeights, gate.BaseOutcome, casterWeight);
            if (deliveryKind == DclDelivery.Beneficial)
            {
                Add(deliveryWeights, DclSpellResolution.ResolveBeneficial(
                    casterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore).Outcome, casterWeight);
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
                int weight = casterWeight * DclSuccessRoll.OutcomeMultiplicity(resistanceRoll);
                Add(deliveryWeights, DclSpellResolution.ResolveInternal(
                    casterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore,
                    input.ResistanceScore!.Value,
                    resistanceRoll,
                    immune: false).Outcome, weight);
            }
        }
        return (
            deliveryWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, total)),
            baseWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, 216)));
    }

    private static DclCanonicalReviveEvaluationResult Empty(
        DclAbilityBinding binding,
        DclReviveMode mode,
        string? storedStateKind,
        DclUnitKey target,
        DclCastDeclarationAttempt attempt,
        IReadOnlyList<string> failures,
        DclDiceExpression restoredExpression,
        bool resourceFailed)
    {
        DclExactValueForecast zero = DclExactValueForecast.Mixture(One, []);
        return new DclCanonicalReviveEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            mode,
            storedStateKind,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: false,
            failures,
            target,
            attempt.CostCommitment,
            attempt.Timing,
            restoredExpression,
            new Dictionary<DclMagicDeliveryOutcome, DclRational>(),
            new Dictionary<DclReviveRoute, DclRational>(),
            zero,
            zero,
            zero,
            Zero,
            Zero,
            Zero,
            new DclCanonicalResourceEvaluation(
                new Dictionary<DclCastingOutcome, DclRational>(),
                new Dictionary<DclCastingOutcome, DclResourcePayment>(),
                Zero,
                Zero,
                Zero),
            resourceFailed);
    }

    private static int Percent(DclRational probability)
        => checked((int)(probability * DclRational.FromInteger(100)).RoundNearest());

    private static void Add<TKey>(Dictionary<TKey, BigInteger> weights, TKey key, BigInteger value)
        where TKey : notnull
        => weights[key] = weights.GetValueOrDefault(key) + value;
}
