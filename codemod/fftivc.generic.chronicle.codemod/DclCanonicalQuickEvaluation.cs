using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalQuickEvaluationInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclCtState TargetCt,
    DclQuickLockController QuickLocks,
    DclStateRegistry StateRegistry);

internal sealed record DclCanonicalQuickEvaluationResult(
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    string LockStateKind,
    string ForecastBoundary,
    string AiBoundary,
    bool Legal,
    IReadOnlyList<string> Failures,
    DclUnitKey Target,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    DclRational CtGrant,
    IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> DeliveryOutcomeProbability,
    IReadOnlyDictionary<DclRational, DclRational> CtCreditProbability,
    DclRational ExpectedCtCredit,
    DclCanonicalResourceEvaluation Resources,
    bool ResourceFailed)
{
    public DclRational GrantProbability
        => DeliveryOutcomeProbability.GetValueOrDefault(
                DclMagicDeliveryOutcome.Delivered,
                DclRational.FromInteger(0)) +
            DeliveryOutcomeProbability.GetValueOrDefault(
                DclMagicDeliveryOutcome.CriticalDelivered,
                DclRational.FromInteger(0));
}

internal sealed record DclCanonicalQuickForecastProjection(
    int AbilityId,
    string ActionId,
    string LockStateKind,
    string ForecastBoundary,
    bool Legal,
    DclUnitKey Target,
    int GrantPercent,
    DclRational CtGrant,
    IReadOnlyDictionary<DclRational, DclRational> CtCreditProbability,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing);

internal sealed record DclCanonicalQuickAiProjection(
    int AbilityId,
    string ActionId,
    string LockStateKind,
    string AiBoundary,
    bool Legal,
    DclUnitKey Target,
    DclRational GrantProbability,
    DclRational ExpectedCtCredit,
    IReadOnlyDictionary<DclRational, DclRational> CtCreditProbability,
    DclRational ExpectedCasterMpDebit,
    DclRational ExpectedCasterHpDebit,
    DclRational PayerKoProbability);

internal static class DclCanonicalQuickEvaluation
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);

    public static DclCanonicalQuickEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalQuickEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket) ||
            profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
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
            throw new InvalidOperationException(
                "Quick evaluation requires one CTChange Carrier followed by its independent QuickLock state.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision ||
            input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit ||
            input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration)
            throw new ArgumentException("Quick evaluation target/declaration/registry identities must agree.", nameof(input));
        if (!runtime.Authoring.States.ContainsKey(lockStateKind))
            throw new InvalidOperationException("Quick evaluation lost its normalized QuickLock definition.");
        DclRational ctGrant = DclRational.ParseExactDecimal(ctMagnitude.Expression);
        if (ctGrant <= Zero)
            throw new InvalidOperationException("Quick evaluation requires a positive exact CT magnitude.");

        bool controllerLocked = input.QuickLocks.IsLocked(input.Target.Unit);
        bool stateLocked = input.StateRegistry.CaptureTarget(input.Target.Unit).Instances.Any(instance =>
            StringComparer.Ordinal.Equals(instance.Kind, lockStateKind));
        if (controllerLocked != stateLocked)
            throw new InvalidOperationException(
                "QuickLock controller and persistent state must agree at the evaluation boundary.");

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, actionInstanceId: 1);
        if (!attempt.Legal)
            return Empty(binding, lockStateKind, input.Target.Unit, attempt, attempt.Failures, ctGrant, false);
        if (controllerLocked)
            return Empty(binding, lockStateKind, input.Target.Unit, attempt, ["quicklock-active"], ctGrant, false);
        if (input.TargetCt.CurrentCt + ctGrant < DclRational.FromInteger(DclCtState.TurnThreshold))
            return Empty(
                binding,
                lockStateKind,
                input.Target.Unit,
                attempt,
                ["ct-grant-below-turn-threshold"],
                ctGrant,
                false);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
            return Empty(
                binding,
                lockStateKind,
                input.Target.Unit,
                attempt,
                ["resource-failure-at-resolution"],
                ctGrant,
                true);

        (IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> delivery,
            IReadOnlyDictionary<DclCastingOutcome, DclRational> baseOutcomes) =
            Enumerate(input.BaseSpellScore, input.TargetSpellScore);
        DclRational grantProbability = delivery.GetValueOrDefault(
                DclMagicDeliveryOutcome.Delivered,
                Zero) +
            delivery.GetValueOrDefault(DclMagicDeliveryOutcome.CriticalDelivered, Zero);
        var ctCreditProbability = new Dictionary<DclRational, DclRational>
        {
            [Zero] = DclRational.FromInteger(1) - grantProbability,
            [ctGrant] = grantProbability,
        };
        DclCanonicalResourceEvaluation resources = DclCanonicalMagicEvaluationExecutor.EvaluateResources(
            baseOutcomes,
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment);
        return new DclCanonicalQuickEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            lockStateKind,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: true,
            Failures: [],
            input.Target.Unit,
            attempt.CostCommitment,
            attempt.Timing,
            ctGrant,
            delivery,
            ctCreditProbability,
            grantProbability * ctGrant,
            resources,
            ResourceFailed: false);
    }

    public static DclCanonicalQuickForecastProjection ProjectPlayer(
        DclCanonicalQuickEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalQuickForecastProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.LockStateKind,
            evaluation.ForecastBoundary,
            evaluation.Legal,
            evaluation.Target,
            checked((int)(evaluation.GrantProbability * DclRational.FromInteger(100)).RoundNearest()),
            evaluation.CtGrant,
            evaluation.CtCreditProbability,
            evaluation.CostCommitment,
            evaluation.Timing);
    }

    public static DclCanonicalQuickAiProjection ProjectAi(
        DclCanonicalQuickEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalQuickAiProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.LockStateKind,
            evaluation.AiBoundary,
            evaluation.Legal,
            evaluation.Target,
            evaluation.GrantProbability,
            evaluation.ExpectedCtCredit,
            evaluation.CtCreditProbability,
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

    private static DclCanonicalQuickEvaluationResult Empty(
        DclAbilityBinding binding,
        string lockStateKind,
        DclUnitKey target,
        DclCastDeclarationAttempt attempt,
        IReadOnlyList<string> failures,
        DclRational ctGrant,
        bool resourceFailed)
        => new(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            lockStateKind,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: false,
            failures,
            target,
            attempt.CostCommitment,
            attempt.Timing,
            ctGrant,
            new Dictionary<DclMagicDeliveryOutcome, DclRational>(),
            new Dictionary<DclRational, DclRational>(),
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
