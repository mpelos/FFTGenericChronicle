using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalDispelEvaluationInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int FinalDispelScore,
    IReadOnlyList<long> SelectedInstanceIds,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclStateRegistry StateRegistry);

internal sealed record DclCanonicalDispelInstanceEvaluation(
    long InstanceId,
    string StateKind,
    int EffectStrength,
    DclRational RemovalProbability);

internal sealed record DclCanonicalDispelRemovalOutcome(
    IReadOnlyList<long> RemovedInstanceIds,
    DclRational Probability);

internal sealed record DclCanonicalDispelEvaluationResult(
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    string ForecastBoundary,
    string AiBoundary,
    bool Legal,
    IReadOnlyList<string> Failures,
    DclUnitKey Target,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    IReadOnlyList<DclCanonicalDispelInstanceEvaluation> Instances,
    IReadOnlyList<DclCanonicalDispelRemovalOutcome> RemovalOutcomes,
    IReadOnlyDictionary<int, DclRational> RemovedCountProbability,
    DclRational ExpectedRemovedCount,
    DclCanonicalResourceEvaluation Resources,
    bool ResourceFailed)
{
    public DclRational AnyRemovalProbability
        => Legal
            ? DclRational.FromInteger(1) - RemovedCountProbability.GetValueOrDefault(
                0,
                DclRational.FromInteger(0))
            : DclRational.FromInteger(0);
}

internal sealed record DclCanonicalDispelForecastProjection(
    int AbilityId,
    string ActionId,
    string ForecastBoundary,
    bool Legal,
    DclUnitKey Target,
    int AnyRemovalPercent,
    IReadOnlyList<DclCanonicalDispelInstanceEvaluation> Instances,
    IReadOnlyList<DclCanonicalDispelRemovalOutcome> RemovalOutcomes,
    IReadOnlyDictionary<int, DclRational> RemovedCountProbability,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing);

internal sealed record DclCanonicalDispelAiProjection(
    int AbilityId,
    string ActionId,
    string AiBoundary,
    bool Legal,
    DclUnitKey Target,
    DclRational AnyRemovalProbability,
    DclRational ExpectedRemovedCount,
    IReadOnlyList<DclCanonicalDispelInstanceEvaluation> Instances,
    IReadOnlyList<DclCanonicalDispelRemovalOutcome> RemovalOutcomes,
    IReadOnlyDictionary<int, DclRational> RemovedCountProbability,
    DclRational ExpectedCasterMpDebit,
    DclRational ExpectedCasterHpDebit,
    DclRational PayerKoProbability);

internal static class DclCanonicalDispelEvaluation
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);
    private static readonly DclRational One = DclRational.FromInteger(1);

    public static DclCanonicalDispelEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalDispelEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.StatusPacket or
                DclNativeCarrierKind.ConditionalStatusProducer or DclNativeCarrierKind.SingleResult) ||
            profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            {
                Kind: DclEffectKind.Dispel,
                Role: DclEffectRole.Carrier,
            } || profile.DispelProfile is null)
            throw new InvalidOperationException(
                "Dispel evaluation requires one unit-targeted Dispel Carrier and its explicit selection profile.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision ||
            input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("Dispel evaluation lost its declaration target/profile revision.", nameof(input));
        if (input.StateRegistry.BattleGeneration != input.Target.Unit.BattleGeneration)
            throw new ArgumentException("Dispel evaluation registry and target must share one battle generation.", nameof(input));
        if (input.FinalDispelScore < 1)
            throw new ArgumentOutOfRangeException(nameof(input), "Final DispelScore must be positive.");

        DclDispelProfile dispelProfile = profile.DispelProfile;
        HashSet<string> eligibleFamilies = dispelProfile.EligibleCureFamilies.ToHashSet(StringComparer.Ordinal);
        DclStateInstance[] eligibleInstances = input.StateRegistry.CaptureTarget(input.Target.Unit).Instances
            .Where(instance => instance.Definition.CureFamilies.Any(eligibleFamilies.Contains) &&
                (!dispelProfile.SourceMatchedOnly || instance.Source == input.DeclarationRequest.Caster))
            .OrderBy(instance => instance.InstanceId)
            .ToArray();
        long[] selectedIds = input.SelectedInstanceIds.ToArray();
        HashSet<long> selectedSet = selectedIds.ToHashSet();
        if (selectedSet.Count != selectedIds.Length)
            throw new ArgumentException("A Dispel evaluation cannot select the same state instance twice.", nameof(input));
        HashSet<long> eligibleIds = eligibleInstances.Select(instance => instance.InstanceId).ToHashSet();
        if (selectedSet.Any(id => !eligibleIds.Contains(id)))
            throw new ArgumentException("Dispel evaluation selected an instance outside its target/family/source policy.", nameof(input));
        if (dispelProfile.Scope == DclDispelScope.AllEligible && !selectedSet.SetEquals(eligibleIds))
            throw new ArgumentException("AllEligible Dispel evaluation must select the complete eligible snapshot.", nameof(input));
        if (dispelProfile.Scope == DclDispelScope.OneInstance && selectedIds.Length > 1)
            throw new ArgumentException("OneInstance Dispel evaluation may select at most one eligible instance.", nameof(input));
        DclStateInstance[] selected = eligibleInstances.Where(instance => selectedSet.Contains(instance.InstanceId)).ToArray();
        if (selected.Any(instance => instance.Strength is null))
            throw new InvalidOperationException("Every selected dispellable state must have stored EffectStrength.");

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, actionInstanceId: 1);
        if (!attempt.Legal)
            return Empty(binding, input.Target.Unit, attempt, attempt.Failures, ResourceFailed: false);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
            return Empty(
                binding,
                input.Target.Unit,
                attempt,
                ["resource-failure-at-resolution"],
                ResourceFailed: true);

        (IReadOnlyList<DclCanonicalDispelRemovalOutcome> removalOutcomes,
            IReadOnlyDictionary<DclCastingOutcome, DclRational> baseOutcomes) =
            Enumerate(input, selected);
        DclCanonicalDispelInstanceEvaluation[] instances = selected.Select(instance =>
            new DclCanonicalDispelInstanceEvaluation(
                instance.InstanceId,
                instance.Kind,
                instance.Strength!.Value,
                removalOutcomes
                    .Where(outcome => outcome.RemovedInstanceIds.Contains(instance.InstanceId))
                    .Select(outcome => outcome.Probability)
                    .Aggregate(Zero, (total, probability) => total + probability)))
            .ToArray();
        IReadOnlyDictionary<int, DclRational> removedCounts = removalOutcomes
            .GroupBy(outcome => outcome.RemovedInstanceIds.Count)
            .ToDictionary(
                group => group.Key,
                group => group.Select(outcome => outcome.Probability)
                    .Aggregate(Zero, (total, probability) => total + probability));
        DclRational expectedRemovedCount = removedCounts
            .Select(pair => DclRational.FromInteger(pair.Key) * pair.Value)
            .Aggregate(Zero, (total, contribution) => total + contribution);
        DclCanonicalResourceEvaluation resources = DclCanonicalMagicEvaluationExecutor.EvaluateResources(
            baseOutcomes,
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment);
        return new DclCanonicalDispelEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: true,
            Failures: [],
            input.Target.Unit,
            attempt.CostCommitment,
            attempt.Timing,
            instances,
            removalOutcomes,
            removedCounts,
            expectedRemovedCount,
            resources,
            ResourceFailed: false);
    }

    public static DclCanonicalDispelForecastProjection ProjectPlayer(
        DclCanonicalDispelEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalDispelForecastProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.ForecastBoundary,
            evaluation.Legal,
            evaluation.Target,
            checked((int)(evaluation.AnyRemovalProbability * DclRational.FromInteger(100)).RoundNearest()),
            evaluation.Instances,
            evaluation.RemovalOutcomes,
            evaluation.RemovedCountProbability,
            evaluation.CostCommitment,
            evaluation.Timing);
    }

    public static DclCanonicalDispelAiProjection ProjectAi(
        DclCanonicalDispelEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalDispelAiProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.AiBoundary,
            evaluation.Legal,
            evaluation.Target,
            evaluation.AnyRemovalProbability,
            evaluation.ExpectedRemovedCount,
            evaluation.Instances,
            evaluation.RemovalOutcomes,
            evaluation.RemovedCountProbability,
            evaluation.Resources.ExpectedMpDebit,
            evaluation.Resources.ExpectedHpDebit,
            evaluation.Resources.PayerKoProbability);
    }

    private static (
        IReadOnlyList<DclCanonicalDispelRemovalOutcome> RemovalOutcomes,
        IReadOnlyDictionary<DclCastingOutcome, DclRational> BaseOutcomes) Enumerate(
        DclCanonicalDispelEvaluationInput input,
        IReadOnlyList<DclStateInstance> selected)
    {
        var removalWeights = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
        var baseWeights = new Dictionary<DclCastingOutcome, BigInteger>();
        BigInteger resistanceSpace = BigInteger.Pow(216, selected.Count);
        for (int casterRoll = DclSuccessRoll.MinRoll; casterRoll <= DclSuccessRoll.MaxRoll; casterRoll++)
        {
            BigInteger casterWeight = DclSuccessRoll.OutcomeMultiplicity(casterRoll);
            DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
                casterRoll,
                input.BaseSpellScore,
                input.TargetSpellScore);
            Add(baseWeights, gate.BaseOutcome, casterWeight);
            if (!gate.BaseSucceeded || !gate.TargetSucceeded)
            {
                Add(removalWeights, EmptySet(selected.Count), casterWeight * resistanceSpace);
                continue;
            }

            var conditional = new Dictionary<string, BigInteger>(StringComparer.Ordinal)
            {
                [EmptySet(selected.Count)] = casterWeight,
            };
            for (int index = 0; index < selected.Count; index++)
            {
                int winningResistanceOutcomes = 0;
                for (int resistanceRoll = DclSuccessRoll.MinRoll;
                     resistanceRoll <= DclSuccessRoll.MaxRoll;
                     resistanceRoll++)
                    if (DclStatusRules.ResolveDispel(
                            input.FinalDispelScore,
                            casterRoll,
                            selected[index].Strength!.Value,
                            resistanceRoll).ActingSideWon)
                        winningResistanceOutcomes += DclSuccessRoll.OutcomeMultiplicity(resistanceRoll);
                int losingResistanceOutcomes = 216 - winningResistanceOutcomes;
                var next = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
                foreach ((string set, BigInteger weight) in conditional)
                {
                    if (losingResistanceOutcomes > 0)
                        Add(next, set, weight * losingResistanceOutcomes);
                    if (winningResistanceOutcomes > 0)
                        Add(next, MarkRemoved(set, index), weight * winningResistanceOutcomes);
                }
                conditional = next;
            }
            foreach ((string set, BigInteger weight) in conditional)
                Add(removalWeights, set, weight);
        }

        BigInteger total = 216 * resistanceSpace;
        DclCanonicalDispelRemovalOutcome[] outcomes = removalWeights
            .Where(pair => pair.Value > 0)
            .OrderBy(pair => CountRemoved(pair.Key))
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new DclCanonicalDispelRemovalOutcome(
                selected.Where((_, index) => pair.Key[index] == '1').Select(instance => instance.InstanceId).ToArray(),
                new DclRational(pair.Value, total)))
            .ToArray();
        return (
            outcomes,
            baseWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, 216)));
    }

    private static DclCanonicalDispelEvaluationResult Empty(
        DclAbilityBinding binding,
        DclUnitKey target,
        DclCastDeclarationAttempt attempt,
        IReadOnlyList<string> failures,
        bool ResourceFailed)
        => new(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: false,
            failures,
            target,
            attempt.CostCommitment,
            attempt.Timing,
            Instances: [],
            RemovalOutcomes: [],
            new Dictionary<int, DclRational>(),
            Zero,
            new DclCanonicalResourceEvaluation(
                new Dictionary<DclCastingOutcome, DclRational>(),
                new Dictionary<DclCastingOutcome, DclResourcePayment>(),
                Zero,
                Zero,
                Zero),
            ResourceFailed);

    private static string EmptySet(int count) => new('0', count);

    private static string MarkRemoved(string value, int index)
    {
        char[] marked = value.ToCharArray();
        marked[index] = '1';
        return new string(marked);
    }

    private static int CountRemoved(string value) => value.Count(bit => bit == '1');

    private static void Add<TKey>(Dictionary<TKey, BigInteger> weights, TKey key, BigInteger value)
        where TKey : notnull
        => weights[key] = weights.GetValueOrDefault(key) + value;
}
