using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalAreaTargetEvaluationInput(
    DclTargetCandidate Target,
    int TargetSpellScore,
    int? TargetGateScore,
    int MagnitudeAttribute,
    DclElementAffinity Affinity,
    DclRational FaithMagnitude,
    bool TargetHasShell,
    int TargetMaxHp,
    bool FireEffect,
    bool OilContributed,
    int ApplicableDr = 0,
    int AdditionalMagnitudeIntegerModifier = 0,
    IReadOnlyList<DclMagicStatusRiderForecast>? StatusRiders = null,
    int? EffectiveHt = null,
    DclCanonicalResourcePoolSnapshot? ResourceTargetPools = null,
    DclCanonicalNativeMovementVerdict? ForcedMovementVerdict = null,
    bool ForcedMovementImmune = false,
    DclCanonicalConcentrationTargetContext? ForcedMovementConcentrationContext = null,
    DclCanonicalInjuryTargetContext? InjuryTargetContext = null,
    int AuthoredForcedDisplacement = 0,
    IReadOnlyList<DclCanonicalInjuryMovementBranchSet?>? InjuryMovementBranchesByStrike = null,
    IReadOnlyList<DclCanonicalInjuryMovementBranchForest?>? InjuryMovementBranchForestsByStrike = null);

internal sealed record DclCanonicalAreaMagicEvaluationInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    IReadOnlyDictionary<DclUnitKey, DclTargetCandidate> CurrentUnits,
    IReadOnlyList<DclTargetCandidate> NativeGeometricMembers,
    int BaseSpellScore,
    IReadOnlyList<DclCanonicalAreaTargetEvaluationInput> Targets,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclCanonicalResourcePoolSnapshot? ResourceSourcePools = null);

internal sealed record DclCanonicalAreaTargetEvaluationResult(
    DclUnitKey Target,
    DclRational AnyCarrierDeliveryProbability,
    DclRational ExpectedLandedStrikes,
    IReadOnlyDictionary<int, DclRational> RiderApplicationProbability,
    DclDiceExpression NormalMagnitudeExpression,
    DclDiceExpression CriticalMagnitudeExpression,
    DclExactValueForecast TotalHpDebit,
    DclExactValueForecast TotalHpCredit,
    DclExactValueForecast TotalMpDebit,
    DclExactValueForecast TotalMpCredit,
    DclCanonicalForcedMovementResult? DeliveredMovement = null,
    DclRational? ExpectedMovedTiles = null,
    DclRational? FallProbability = null,
    DclRational? ConcentrationInterruptionProbability = null);

internal sealed record DclCanonicalAreaSourceResourceOutcome(
    DclCanonicalNativeNumericChannels Channels,
    bool SourceKo);

internal sealed record DclCanonicalAreaSourceResourceEvaluation(
    IReadOnlyDictionary<DclCanonicalAreaSourceResourceOutcome, DclRational> Outcomes)
{
    public DclRational ExpectedHpDebit => Expected(channels => channels.HpDebit);
    public DclRational ExpectedHpCredit => Expected(channels => channels.HpCredit);
    public DclRational ExpectedMpDebit => Expected(channels => channels.MpDebit);
    public DclRational ExpectedMpCredit => Expected(channels => channels.MpCredit);
    public DclRational SourceKoProbability => Outcomes.Where(pair => pair.Key.SourceKo)
        .Aggregate(DclRational.FromInteger(0), (sum, pair) => sum + pair.Value);

    private DclRational Expected(Func<DclCanonicalNativeNumericChannels, int> selector)
        => Outcomes.Aggregate(
            DclRational.FromInteger(0),
            (sum, pair) => sum + DclRational.FromInteger(selector(pair.Key.Channels)) * pair.Value);
}

internal sealed record DclCanonicalAreaMagicEvaluationResult(
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    string ForecastBoundary,
    string AiBoundary,
    bool Legal,
    IReadOnlyList<string> Failures,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    DclResolvedCastCenter? Center,
    DclTargetBatch? TargetBatch,
    DclMagicCorrelatedForecastResult CorrelatedDelivery,
    IReadOnlyList<DclCanonicalAreaTargetEvaluationResult> Targets,
    DclCanonicalResourceEvaluation Resources,
    bool ResourceFailed,
    DclCanonicalAreaSourceResourceEvaluation? SourceResourceChange = null);

internal sealed record DclCanonicalAreaMagicAiProjection(
    int AbilityId,
    string ActionId,
    string AiBoundary,
    bool Legal,
    DclRational ExpectedDeliveredTargets,
    DclRational ExpectedLandedStrikes,
    DclRational ExpectedStatusApplications,
    DclRational ExpectedTargetHpDebit,
    DclRational ExpectedTargetHpCredit,
    DclRational ExpectedTargetMpDebit,
    DclRational ExpectedTargetMpCredit,
    DclRational ExpectedSourceEffectHpDebit,
    DclRational ExpectedSourceEffectHpCredit,
    DclRational ExpectedSourceEffectMpDebit,
    DclRational ExpectedSourceEffectMpCredit,
    DclRational SourceEffectKoProbability,
    DclRational ExpectedMovedTiles,
    DclRational FallProbability,
    DclRational ExpectedCasterMpDebit,
    DclRational ExpectedCasterHpDebit,
    DclRational PayerKoProbability,
    DclRational ExpectedConcentrationInterruptions)
{
    public static DclCanonicalAreaMagicAiProjection From(DclCanonicalAreaMagicEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalAreaMagicAiProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.AiBoundary,
            evaluation.Legal,
            evaluation.Targets.Aggregate(Zero, (sum, target) => sum + target.AnyCarrierDeliveryProbability),
            evaluation.Targets.Aggregate(Zero, (sum, target) => sum + target.ExpectedLandedStrikes),
            evaluation.Targets.Aggregate(Zero, (sum, target) => sum +
                target.RiderApplicationProbability.Values.Aggregate(Zero, (riderSum, probability) => riderSum + probability)),
            evaluation.Targets.Aggregate(Zero, (sum, target) => sum + target.TotalHpDebit.ExpectedValue),
            evaluation.Targets.Aggregate(Zero, (sum, target) => sum + target.TotalHpCredit.ExpectedValue),
            evaluation.Targets.Aggregate(Zero, (sum, target) => sum + target.TotalMpDebit.ExpectedValue),
            evaluation.Targets.Aggregate(Zero, (sum, target) => sum + target.TotalMpCredit.ExpectedValue),
            evaluation.SourceResourceChange?.ExpectedHpDebit ?? Zero,
            evaluation.SourceResourceChange?.ExpectedHpCredit ?? Zero,
            evaluation.SourceResourceChange?.ExpectedMpDebit ?? Zero,
            evaluation.SourceResourceChange?.ExpectedMpCredit ?? Zero,
            evaluation.SourceResourceChange?.SourceKoProbability ?? Zero,
            evaluation.Targets.Aggregate(Zero, (sum, target) =>
                sum + (target.ExpectedMovedTiles ?? Zero)),
            evaluation.Targets.Aggregate(Zero, (sum, target) =>
                sum + (target.FallProbability ?? Zero)),
            evaluation.Resources.ExpectedMpDebit,
            evaluation.Resources.ExpectedHpDebit,
            evaluation.Resources.PayerKoProbability,
            evaluation.Targets.Aggregate(Zero, (sum, target) =>
                sum + (target.ConcentrationInterruptionProbability ?? Zero)));
    }

    private static DclRational Zero => DclRational.FromInteger(0);
}

internal sealed record DclCanonicalAreaTargetForecastProjection(
    DclUnitKey Target,
    int AnyCarrierDeliveryPercent,
    string NormalMagnitude,
    string CriticalMagnitude,
    int MinimumTotalHpDebit,
    int MaximumTotalHpDebit,
    int MinimumTotalHpCredit,
    int MaximumTotalHpCredit,
    int MinimumTotalMpDebit,
    int MaximumTotalMpDebit,
    int MinimumTotalMpCredit,
    int MaximumTotalMpCredit,
    IReadOnlyDictionary<int, int> RiderApplicationPercent,
    int RequestedMovementTiles = 0,
    int MovedTilesOnDelivery = 0,
    bool FallsOnDelivery = false,
    DclRational ExpectedMovedTiles = default,
    int FallChancePercent = 0,
    int ConcentrationInterruptionPercent = 0);

internal sealed record DclCanonicalAreaMagicForecastProjection(
    int AbilityId,
    string ActionId,
    string ForecastBoundary,
    bool Legal,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    IReadOnlyList<DclCanonicalAreaTargetForecastProjection> Targets,
    DclCanonicalAreaSourceResourceEvaluation? SourceResourceChange)
{
    public static DclCanonicalAreaMagicForecastProjection From(DclCanonicalAreaMagicEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalAreaMagicForecastProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.ForecastBoundary,
            evaluation.Legal,
            evaluation.CostCommitment,
            evaluation.Timing,
            evaluation.Targets.Select(target => new DclCanonicalAreaTargetForecastProjection(
                target.Target,
                Percent(target.AnyCarrierDeliveryProbability),
                target.NormalMagnitudeExpression.ToString(),
                target.CriticalMagnitudeExpression.ToString(),
                target.TotalHpDebit.Minimum,
                target.TotalHpDebit.Maximum,
                target.TotalHpCredit.Minimum,
                target.TotalHpCredit.Maximum,
                target.TotalMpDebit.Minimum,
                target.TotalMpDebit.Maximum,
                target.TotalMpCredit.Minimum,
                target.TotalMpCredit.Maximum,
                target.RiderApplicationProbability.ToDictionary(pair => pair.Key, pair => Percent(pair.Value)),
                target.DeliveredMovement?.RequestedTiles ?? 0,
                target.DeliveredMovement?.MovedTiles ?? 0,
                target.DeliveredMovement?.Fell ?? false,
                target.ExpectedMovedTiles ?? DclRational.FromInteger(0),
                Percent(target.FallProbability ?? DclRational.FromInteger(0)),
                Percent(target.ConcentrationInterruptionProbability ?? DclRational.FromInteger(0))))
                .ToArray(),
            evaluation.SourceResourceChange);
    }

    private static int Percent(DclRational probability)
        => checked((int)(probability * DclRational.FromInteger(100)).RoundNearest());
}

internal static class DclCanonicalAreaMagicEvaluationExecutor
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);
    private static readonly DclRational One = DclRational.FromInteger(1);

    public static DclCanonicalAreaMagicEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalAreaMagicEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        bool statusOnly = profile.MagnitudeProfile is null && profile.Effects.Count == 1 && profile.Effects[0] is
        {
            Role: DclEffectRole.Carrier,
            Kind: DclEffectKind.StatusApplication,
            ReferencedStateKind: not null,
        };
        bool forcedMovementOnly = profile.MagnitudeProfile is null && profile.ForcedMovementProfile is not null &&
            profile.Effects.Count == 1 && profile.Effects[0] is
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.ForcedMovement,
            };
        bool carrierCompatible = statusOnly
            ? binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket or
                DclNativeCarrierKind.ConditionalStatusProducer
            : forcedMovementOnly
                ? binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket
                : binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.NativeRepeat;
        if (!carrierCompatible ||
            profile.DeliveryProfile.Delivery != DclDelivery.Area || profile.TargetProfile.Area is null ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is not (DclDamageMagnitude or DclHealingMagnitude or DclFixedResourceMagnitude) ||
            profile.Effects.Count == 0 || profile.Effects[0].Role != DclEffectRole.Carrier ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is DclDamageMagnitude && profile.Effects[0].Kind != DclEffectKind.Damage ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is DclHealingMagnitude && profile.Effects[0].Kind != DclEffectKind.Healing ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is DclFixedResourceMagnitude && profile.Effects[0].Kind != DclEffectKind.ResourceChange ||
            !statusOnly && !forcedMovementOnly && profile.Effects.Skip(1).Any(effect => effect is not
            {
                Role: DclEffectRole.Rider,
                Kind: DclEffectKind.StatusApplication,
                ReferencedStateKind: not null,
            }))
            throw new InvalidOperationException(
                "The canonical Area evaluation vertical requires one numeric Carrier with status Riders, one pure status Carrier, or one pure ForcedMovement Carrier.");
        bool resourceChange = profile.MagnitudeProfile is DclFixedResourceMagnitude;
        if (resourceChange && (profile.Effects.Count != 1 || profile.ResourceChangeProfile is null))
            throw new InvalidOperationException("Area ResourceChange evaluation requires its single Carrier and explicit route profile.");
        if (resourceChange)
        {
            DclCanonicalResourcePoolSnapshot sourcePools = input.ResourceSourcePools ??
                throw new ArgumentException("Area ResourceChange evaluation requires exact source pools.", nameof(input));
            if (sourcePools.CurrentHp != input.CurrentHpAtResolution ||
                sourcePools.CurrentMp != input.CurrentMpAtResolution)
                throw new ArgumentException("Area ResourceChange source pools do not match the payer snapshot.", nameof(input));
        }
        else if (input.ResourceSourcePools is not null || input.Targets.Any(target => target.ResourceTargetPools is not null))
            throw new ArgumentException("Area Damage/Healing/status/ForcedMovement evaluation cannot own ResourceChange pools.", nameof(input));
        DclCanonicalAreaMagicExecutor.RequireSupportedRiderTiming(profile);
        DclCanonicalInjuryConsequenceCommitter.RequireSupportedTiming(profile);
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Area evaluation does not use the bound normalized action revision.", nameof(input));

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, actionInstanceId: 1);
        if (!attempt.Legal || attempt.Declaration is null)
            return Empty(binding, attempt, attempt.Failures, ResourceFailed: false);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
            return Empty(binding, attempt, ["resource-failure-at-resolution"], ResourceFailed: true);

        DclResolvedCastCenter center = DclMagicTargeting.ResolveCenter(
            attempt.Declaration,
            profile.TargetProfile.Area.CenterMode,
            input.CurrentUnits);
        if (!center.Available)
            return Empty(binding, attempt, [$"area-center-unavailable:{center.Reason}"], ResourceFailed: false);
        DclTargetBatch targetBatch = DclMagicTargeting.SnapshotAreaTargets(
            attempt.Declaration.Source.BattleGeneration,
            input.NativeGeometricMembers,
            profile.TargetProfile.AllegiancePolicy,
            profile.TargetProfile.EligibleTargetStates);
        Dictionary<DclUnitKey, DclCanonicalAreaTargetEvaluationInput> targetInputs =
            input.Targets.ToDictionary(target => target.Target.Unit);
        if (targetInputs.Count != input.Targets.Count ||
            !targetInputs.Keys.ToHashSet().SetEquals(targetBatch.Targets.Select(target => target.Target)))
            throw new ArgumentException(
                "Area evaluation targets must match the stable filtered TargetBatch exactly.",
                nameof(input));

        int[] expectedRiderIndexes = statusOnly ? [0] : Enumerable.Range(1, profile.Effects.Count - 1).ToArray();
        var riderDefinitions = new Dictionary<int, DclStateDefinition>();
        foreach (int effectIndex in expectedRiderIndexes)
        {
            string stateKind = profile.Effects[effectIndex].ReferencedStateKind!;
            if (!runtime.Authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition))
                throw new InvalidOperationException($"Area status Rider {effectIndex} lost state definition '{stateKind}'.");
            if (definition.ResistanceGate == DclStateResistanceGate.Explicit)
                throw new InvalidOperationException(
                    $"Area status Rider {effectIndex} uses Explicit resistance and requires its named evaluator.");
            if (statusOnly)
            {
                DclStateResistanceGate expected = profile.TargetProfile.Area.DeliveryGate == DclAreaDeliveryGate.QuickContest
                    ? DclStateResistanceGate.QuickContest
                    : DclStateResistanceGate.None;
                if (definition.ResistanceGate != expected)
                    throw new InvalidOperationException(
                        $"Area status Carrier delivery gate {profile.TargetProfile.Area.DeliveryGate} requires state gate {expected}.");
            }
            riderDefinitions.Add(effectIndex, definition);
        }
        var forecastTargets = new List<DclMagicForecastTarget>(targetBatch.Targets.Count);
        var forcedMovements = new Dictionary<DclUnitKey, DclCanonicalForcedMovementResult>();
        foreach (DclTargetResolutionSnapshot snapshot in targetBatch.Targets)
        {
            DclCanonicalAreaTargetEvaluationInput target = targetInputs[snapshot.Target];
            if (target.TargetMaxHp < 1 || snapshot.CurrentHp > target.TargetMaxHp)
                throw new ArgumentOutOfRangeException(nameof(input), "Area evaluation MaxHP must contain current HP.");
            if (target.AuthoredForcedDisplacement < 0)
                throw new ArgumentOutOfRangeException(nameof(input), "Area authored Injury displacement cannot be negative.");
            if (resourceChange)
            {
                DclCanonicalResourcePoolSnapshot targetPools = target.ResourceTargetPools ??
                    throw new ArgumentException("Area ResourceChange evaluation requires exact target pools.", nameof(input));
                if (targetPools.CurrentHp != snapshot.CurrentHp || targetPools.MaxHp != target.TargetMaxHp)
                    throw new ArgumentException("Area ResourceChange target pools do not match the stable target snapshot.", nameof(input));
            }
            if (profile.MagnitudeProfile is DclDamageMagnitude)
            {
                if (target.InjuryTargetContext is not { } injuryContext ||
                    injuryContext.MaxHp != target.TargetMaxHp || injuryContext.TargetSt < 1 ||
                    injuryContext.UnexpiredShockInjury < 0 ||
                    injuryContext.ConcentrationStatePenaltyMagnitude < 0)
                    throw new ArgumentException(
                        "Area Damage evaluation requires one valid exact Injury target context.",
                        nameof(input));
                if (profile.TransactionProfile.StrikeCount > 1 &&
                    profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate &&
                    target.EffectiveHt is null)
                    throw new ArgumentException(
                        "Immediate multi-Strike Area Injury evaluation requires the target's exact effective HT.",
                        nameof(input));
                if (target.InjuryMovementBranchesByStrike is { } injuryMovementBranches &&
                    injuryMovementBranches.Count != profile.TransactionProfile.StrikeCount)
                    throw new ArgumentException(
                        "Area Injury movement branches must match the exact Strike cardinality.",
                        nameof(input));
                foreach (DclCanonicalInjuryMovementBranchSet branches in
                    target.InjuryMovementBranchesByStrike?.OfType<DclCanonicalInjuryMovementBranchSet>() ?? [])
                    branches.Validate(snapshot.Target);
                if (target.InjuryMovementBranchForestsByStrike is { } injuryMovementForests &&
                    injuryMovementForests.Count != profile.TransactionProfile.StrikeCount)
                    throw new ArgumentException(
                        "Area Injury movement forests must match the exact Strike cardinality.",
                        nameof(input));
                if (target.InjuryMovementBranchesByStrike is not null &&
                    target.InjuryMovementBranchForestsByStrike is not null)
                    throw new ArgumentException(
                        "Area Damage evaluation cannot own both single-origin and conditional-origin Injury branches.",
                        nameof(input));
                foreach (DclCanonicalInjuryMovementBranchForest forest in
                    target.InjuryMovementBranchForestsByStrike?
                        .OfType<DclCanonicalInjuryMovementBranchForest>() ?? [])
                    forest.Validate(snapshot.Target);
            }
            else if (target.InjuryTargetContext is not null || target.AuthoredForcedDisplacement != 0 ||
                target.InjuryMovementBranchesByStrike is not null ||
                target.InjuryMovementBranchForestsByStrike is not null)
            {
                throw new ArgumentException(
                    "Only Area Damage evaluation can own Injury displacement inputs.",
                    nameof(input));
            }
            if (forcedMovementOnly)
            {
                target.ForcedMovementConcentrationContext?.Validate();
                if (target.ForcedMovementImmune &&
                    profile.TargetProfile.Area.DeliveryGate != DclAreaDeliveryGate.QuickContest)
                    throw new ArgumentException(
                        "Area ForcedMovement immunity is legal only under QuickContest delivery.", nameof(input));
                forcedMovements.Add(snapshot.Target, DclCanonicalForcedMovement.Resolve(
                    profile,
                    snapshot.Target,
                    target.Target.Tile,
                    snapshot.CurrentHp == 0,
                    target.ForcedMovementVerdict));
            }
            else if (target.ForcedMovementVerdict is not null || target.ForcedMovementImmune)
                throw new ArgumentException(
                    "Only Area ForcedMovement evaluation can own a movement verdict or movement immunity.", nameof(input));
            else if (target.ForcedMovementConcentrationContext is not null)
                throw new ArgumentException(
                    "Only Area ForcedMovement evaluation can own a concentration snapshot.", nameof(input));
            DclMagicStatusRiderForecast[] riders = (target.StatusRiders ?? []).ToArray();
            if (!riders.Select(rider => rider.EffectIndex).Order().SequenceEqual(expectedRiderIndexes))
                throw new ArgumentException(
                    "Every Area evaluation target must provide every normalized status Rider exactly once.",
                    nameof(input));
            riders = riders.Select(rider => rider with
            {
                ResistanceGate = riderDefinitions[rider.EffectIndex].ResistanceGate,
            }).ToArray();
            if (statusOnly && profile.TargetProfile.Area.DeliveryGate == DclAreaDeliveryGate.QuickContest &&
                target.TargetGateScore != riders[0].ResistanceScore)
                throw new ArgumentException(
                    "Area status Carrier forecast must use the same target score for delivery and state application.",
                    nameof(input));
            ValidateTargetGate(profile.TargetProfile.Area.DeliveryGate, target.TargetGateScore);
            forecastTargets.Add(new DclMagicForecastTarget(
                snapshot.Target,
                target.TargetSpellScore,
                profile.TargetProfile.Area.DeliveryGate,
                target.TargetGateScore,
                profile.TransactionProfile.StrikeCount,
                statusOnly ? [] : riders,
                DeliveryImmune: statusOnly && riders[0].Immune ||
                    forcedMovementOnly && target.ForcedMovementImmune));
        }

        DclMagicCorrelatedForecastResult correlated = forecastTargets.Count == 0
            ? EmptyCorrelated()
            : DclMagicCorrelatedForecast.Enumerate(input.BaseSpellScore, forecastTargets);
        IReadOnlyDictionary<DclCastingOutcome, DclRational> baseOutcomes =
            EnumerateBaseOutcomes(input.BaseSpellScore);
        DclCanonicalResourceEvaluation resources = DclCanonicalMagicEvaluationExecutor.EvaluateResources(
            baseOutcomes,
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment);
        DclCanonicalAreaTargetEvaluationResult[] targetResults = targetBatch.Targets.Select(snapshot =>
        {
            DclCanonicalAreaTargetEvaluationInput target = targetInputs[snapshot.Target];
            (DclDiceExpression normal, DclDiceExpression critical, DclExactValueForecast debit,
                DclExactValueForecast credit, DclExactValueForecast mpDebit,
                DclExactValueForecast mpCredit, DclRational expectedLandedStrikes,
                DclRational injuryExpectedMovedTiles, DclRational injuryFallProbability) = statusOnly || forcedMovementOnly
                    ? EmptyMagnitude(correlated.PerTargetExpectedLandedStrikes[snapshot.Target])
                    : EnumerateTargetMagnitude(
                        profile,
                        input.BaseSpellScore,
                        target,
                        input.ResourceSourcePools);
            IReadOnlyDictionary<int, DclRational> statusApplications = statusOnly
                ? new Dictionary<int, DclRational>
                {
                    [0] = (target.StatusRiders ?? [])[0].Immune
                        ? Zero
                        : correlated.PerTargetDeliveryProbability[snapshot.Target],
                }
                : correlated.PerTargetRiderApplicationProbability[snapshot.Target];
            DclCanonicalForcedMovementResult? movement = forcedMovementOnly
                ? forcedMovements[snapshot.Target]
                : null;
            DclRational? expectedMovedTiles = movement is null
                ? profile.MagnitudeProfile is DclDamageMagnitude ? injuryExpectedMovedTiles : null
                : correlated.PerTargetDeliveryProbability[snapshot.Target] *
                    DclRational.FromInteger(movement.MovedTiles);
            DclRational? fallProbability = movement is null
                ? profile.MagnitudeProfile is DclDamageMagnitude ? injuryFallProbability : null
                : movement.Fell ? correlated.PerTargetDeliveryProbability[snapshot.Target] : Zero;
            DclRational? concentrationInterruption = movement is null
                ? null
                : target.ForcedMovementConcentrationContext is { Charging: true } concentration &&
                  movement.CreatesConcentrationIncident
                    ? correlated.PerTargetDeliveryProbability[snapshot.Target] * new DclRational(
                        216 - DclSuccessRoll.SuccessOutcomeCount(concentration.Score),
                        216)
                    : Zero;
            return new DclCanonicalAreaTargetEvaluationResult(
                snapshot.Target,
                correlated.PerTargetDeliveryProbability[snapshot.Target],
                expectedLandedStrikes,
                statusApplications,
                normal,
                critical,
                debit,
                credit,
                mpDebit,
                mpCredit,
                movement,
                expectedMovedTiles,
                fallProbability,
                concentrationInterruption);
        }).ToArray();
        DclCanonicalAreaSourceResourceEvaluation? sourceResourceChange = resourceChange
            ? EnumerateSourceResourceChange(
                profile,
                input.BaseSpellScore,
                targetBatch,
                targetInputs,
                input.ResourceSourcePools!)
            : null;

        return new DclCanonicalAreaMagicEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: true,
            Failures: [],
            attempt.CostCommitment,
            attempt.Timing,
            center,
            targetBatch,
            correlated,
            targetResults,
            resources,
            ResourceFailed: false,
            sourceResourceChange);
    }

    private static (DclDiceExpression Normal, DclDiceExpression Critical,
        DclExactValueForecast Debit, DclExactValueForecast Credit,
        DclExactValueForecast MpDebit, DclExactValueForecast MpCredit,
        DclRational ExpectedLandedStrikes, DclRational ExpectedMovedTiles,
        DclRational FallProbability) EmptyMagnitude(DclRational expectedLandedStrikes)
    {
        var zeroExpression = new DclDiceExpression(0, 0);
        var zeroForecast = new DclExactValueForecast(
            0,
            0,
            Zero,
            new Dictionary<int, DclRational> { [0] = One });
        return (zeroExpression, zeroExpression, zeroForecast, zeroForecast, zeroForecast, zeroForecast,
            expectedLandedStrikes, Zero, Zero);
    }

    private static (DclDiceExpression Normal, DclDiceExpression Critical,
        DclExactValueForecast Debit, DclExactValueForecast Credit,
        DclExactValueForecast MpDebit, DclExactValueForecast MpCredit,
        DclRational ExpectedLandedStrikes, DclRational ExpectedMovedTiles,
        DclRational FallProbability) EnumerateTargetMagnitude(
        DclActionProfile profile,
        int baseSpellScore,
        DclCanonicalAreaTargetEvaluationInput target,
        DclCanonicalResourcePoolSnapshot? sourcePools)
    {
        DclDiceExpression normal = profile.MagnitudeProfile switch
        {
            DclDamageMagnitude damage => DclCanonicalMagnitude.BuildDiceExpression(
                target.MagnitudeAttribute,
                damage.Basis,
                damage.FixedExpression,
                checked(damage.IntegerModifier + target.AdditionalMagnitudeIntegerModifier),
                damage.WholeDiceModifier),
            DclHealingMagnitude healing => DclCanonicalMagnitude.BuildDiceExpression(
                target.MagnitudeAttribute,
                healing.Basis,
                healing.FixedExpression,
                checked(healing.IntegerModifier + target.AdditionalMagnitudeIntegerModifier),
                healing.WholeDiceModifier),
            DclFixedResourceMagnitude resource when DclDiceExpression.TryParseAuthored(
                resource.Expression,
                out DclDiceExpression expression) => expression,
            DclFixedResourceMagnitude => throw new InvalidOperationException(
                "Area ResourceChange magnitude does not use the exact Xd6+Y grammar."),
            _ => throw new InvalidOperationException(),
        };
        DclDiceExpression critical = normal;
        if (profile.MagnitudeProfile is DclHealingMagnitude &&
            profile.CriticalProfile.SuccessEffect == DclCriticalSuccessEffect.MaximizeOneHealingDie)
        {
            DclCriticalHealingExpression criticalHealing = DclMagicMagnitude.CriticalHealingExpression(normal);
            critical = new DclDiceExpression(criticalHealing.DiceToRoll, criticalHealing.Adds);
        }

        var finalStates = new Dictionary<AreaValueState, DclRational>();
        for (int casterRoll = DclSuccessRoll.MinRoll; casterRoll <= DclSuccessRoll.MaxRoll; casterRoll++)
        {
            DclRational casterProbability = new(DclSuccessRoll.OutcomeMultiplicity(casterRoll), 216);
            DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
                casterRoll,
                baseSpellScore,
                target.TargetSpellScore);
            if (!gate.BaseSucceeded || !gate.TargetSucceeded)
            {
                Add(finalStates, InitialValueState(target), casterProbability);
                continue;
            }
            DclDiceExpression expression = gate.TargetCritical ? critical : normal;
            switch (profile.TargetProfile.Area!.DeliveryGate)
            {
                case DclAreaDeliveryGate.None:
                    Merge(finalStates, RunStrikes(
                        profile, target, sourcePools, expression, One, DynamicDodge: false,
                        targetCritical: gate.TargetCritical), casterProbability);
                    break;
                case DclAreaDeliveryGate.Dodge:
                {
                    DclRational landed = gate.TargetCritical
                        ? One
                        : One - new DclRational(
                            DclSuccessRoll.SuccessOutcomeCount(target.TargetGateScore!.Value), 216);
                    Merge(finalStates, RunStrikes(
                        profile,
                        target,
                        sourcePools,
                        expression,
                        landed,
                        DynamicDodge: !gate.TargetCritical,
                        targetCritical: gate.TargetCritical), casterProbability);
                    break;
                }
                case DclAreaDeliveryGate.QuickContest:
                {
                    DclRational delivered = ConditionalContestProbability(
                        target.TargetSpellScore,
                        casterRoll,
                        target.TargetGateScore!.Value);
                    Add(finalStates, InitialValueState(target),
                        casterProbability * (One - delivered));
                    Merge(finalStates, RunStrikes(
                        profile, target, sourcePools, expression, One, DynamicDodge: false,
                        targetCritical: gate.TargetCritical),
                        casterProbability * delivered);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        DclRational total = finalStates.Values.Aggregate(Zero, (sum, probability) => sum + probability);
        if (total != One)
            throw new InvalidOperationException($"Area magnitude forecast probability mass is {total}, not one.");
        Dictionary<int, DclRational> debit = Marginal(finalStates, state => state.TotalHpDebit);
        Dictionary<int, DclRational> credit = Marginal(finalStates, state => state.TotalHpCredit);
        Dictionary<int, DclRational> mpDebit = Marginal(finalStates, state => state.TotalMpDebit);
        Dictionary<int, DclRational> mpCredit = Marginal(finalStates, state => state.TotalMpCredit);
        DclRational expectedLandedStrikes = finalStates.Aggregate(
            Zero,
            (sum, pair) => sum + DclRational.FromInteger(pair.Key.LandedStrikes) * pair.Value);
        DclRational expectedMovedTiles = finalStates.Aggregate(
            Zero,
            (sum, pair) => sum + DclRational.FromInteger(pair.Key.TotalMovedTiles) * pair.Value);
        DclRational fallProbability = finalStates.Where(pair => pair.Key.Fell)
            .Aggregate(Zero, (sum, pair) => sum + pair.Value);
        return (normal, critical, Forecast(debit), Forecast(credit), Forecast(mpDebit), Forecast(mpCredit),
            expectedLandedStrikes, expectedMovedTiles, fallProbability);
    }

    private static Dictionary<AreaValueState, DclRational> RunStrikes(
        DclActionProfile profile,
        DclCanonicalAreaTargetEvaluationInput target,
        DclCanonicalResourcePoolSnapshot? sourcePools,
        DclDiceExpression expression,
        DclRational landedProbability,
        bool DynamicDodge,
        bool targetCritical)
    {
        var states = new Dictionary<AreaValueState, DclRational>
        {
            [InitialValueState(target)] = One,
        };
        DclExactIntegerDistribution magnitude = DclExactIntegerDistribution.Roll(expression);
        for (int strikeIndex = 0; strikeIndex < profile.TransactionProfile.StrikeCount; strikeIndex++)
        {
            var next = new Dictionary<AreaValueState, DclRational>();
            foreach ((AreaValueState state, DclRational stateProbability) in states)
            {
                if (state.CurrentHp == 0)
                {
                    Add(next, state, stateProbability);
                    continue;
                }
                DclRational currentLandedProbability = landedProbability;
                if (DynamicDodge && profile.TransactionProfile.WithinActionApplication ==
                    DclWithinActionApplication.Immediate)
                {
                    int penalty = (state.ImmediateStun ? 4 : 0) +
                        (state.ImmediateKnockedDown ? 3 : 0);
                    int dodgeScore = checked(target.TargetGateScore!.Value - penalty);
                    currentLandedProbability = One - new DclRational(
                        DclSuccessRoll.SuccessOutcomeCount(dodgeScore), 216);
                }
                if (currentLandedProbability < One)
                    Add(next, state, stateProbability * (One - currentLandedProbability));
                if (currentLandedProbability == Zero) continue;
                foreach ((int rawRoll, BigInteger weight) in magnitude.Weights)
                {
                    DclRational rollProbability = new(weight, magnitude.TotalOutcomes);
                    IReadOnlyDictionary<AreaValueState, DclRational> resolved = ResolveLandedBranches(
                        profile, target, sourcePools, state, Math.Max(0, rawRoll), strikeIndex, targetCritical);
                    foreach ((AreaValueState branchState, DclRational branchProbability) in resolved)
                        Add(next, branchState,
                            stateProbability * currentLandedProbability * rollProbability * branchProbability);
                }
            }
            states = next;
        }
        return states;
    }

    private static IReadOnlyDictionary<AreaValueState, DclRational> ResolveLandedBranches(
        DclActionProfile profile,
        DclCanonicalAreaTargetEvaluationInput target,
        DclCanonicalResourcePoolSnapshot? sourcePools,
        AreaValueState state,
        int raw,
        int strikeIndex,
        bool targetCritical)
    {
        if (profile.MagnitudeProfile is DclFixedResourceMagnitude)
        {
            DclCanonicalResourcePoolSnapshot original = target.ResourceTargetPools ??
                throw new ArgumentException("Area ResourceChange evaluation lost its target pools.", nameof(target));
            DclCanonicalResourceChangeResult changed = DclCanonicalResourceChange.ResolveMagnitude(
                profile,
                original with { CurrentHp = state.CurrentHp, CurrentMp = state.CurrentMp },
                sourcePools ?? throw new ArgumentException(
                    "Area ResourceChange evaluation lost its source pools.", nameof(sourcePools)),
                raw);
            return OneState(state with
            {
                CurrentHp = checked(state.CurrentHp - changed.TargetChannels.HpDebit + changed.TargetChannels.HpCredit),
                CurrentMp = checked(state.CurrentMp - changed.TargetChannels.MpDebit + changed.TargetChannels.MpCredit),
                TotalHpDebit = checked(state.TotalHpDebit + changed.TargetChannels.HpDebit),
                TotalHpCredit = checked(state.TotalHpCredit + changed.TargetChannels.HpCredit),
                TotalMpDebit = checked(state.TotalMpDebit + changed.TargetChannels.MpDebit),
                TotalMpCredit = checked(state.TotalMpCredit + changed.TargetChannels.MpCredit),
                LandedStrikes = checked(state.LandedStrikes + (changed.RejectedByUndeadPolicy ? 0 : 1)),
            });
        }
        if (profile.MagnitudeProfile is DclHealingMagnitude)
        {
            DclHealingResult healing = DclMagicalEffect.ResolveHealing(
                raw,
                target.FaithMagnitude,
                state.CurrentHp,
                target.TargetMaxHp);
            return OneState(state with
            {
                CurrentHp = Math.Min(target.TargetMaxHp, state.CurrentHp + healing.AppliedHealing),
                TotalHpCredit = checked(state.TotalHpCredit + healing.AppliedHealing),
                LandedStrikes = checked(state.LandedStrikes + 1),
            });
        }
        DclDamageMagnitude damage = (DclDamageMagnitude)profile.MagnitudeProfile!;
        DclInjuryResult baseInjury = DclCanonicalMagnitude.ResolveMagicalBaseInjury(
            raw,
            damage.DamageType,
            target.ApplicableDr,
            profile.DeliveryProfile);
        DclMagicalEffectResult effect = DclMagicalEffect.ResolveInjuryOrAbsorb(
            baseInjury.Injury,
            target.Affinity,
            target.FaithMagnitude,
            profile.DeliveryProfile.ShellSensitive,
            target.TargetHasShell,
            state.CurrentHp,
            target.TargetMaxHp,
            target.FireEffect,
            state.OilAvailable);
        AreaValueState resolved = effect.Route switch
        {
            DclMagicalEffectRoute.Injury => state with
            {
                CurrentHp = Math.Max(0, state.CurrentHp - effect.FinalInjury),
                OilAvailable = state.OilAvailable && !effect.ConsumeOil,
                TotalHpDebit = checked(state.TotalHpDebit + effect.FinalInjury),
                LandedStrikes = checked(state.LandedStrikes + 1),
            },
            DclMagicalEffectRoute.AbsorbedHealing => state with
            {
                CurrentHp = Math.Min(target.TargetMaxHp, state.CurrentHp + effect.AppliedHealing),
                OilAvailable = state.OilAvailable && !effect.ConsumeOil,
                TotalHpCredit = checked(state.TotalHpCredit + effect.AppliedHealing),
                LandedStrikes = checked(state.LandedStrikes + 1),
            },
            _ => state with
            {
                OilAvailable = state.OilAvailable && !effect.ConsumeOil,
                LandedStrikes = checked(state.LandedStrikes + 1),
            },
        };
        if (effect.Route == DclMagicalEffectRoute.Injury)
        {
            DclCanonicalInjuryTargetContext injuryContext = target.InjuryTargetContext ??
                throw new InvalidOperationException("Area Damage forecast lost its Injury target context.");
            bool targetSurvives = resolved.CurrentHp > 0;
            int requestedTiles = checked(target.AuthoredForcedDisplacement +
                DclInjury.CriticalKnockbackTiles(
                    targetCritical,
                    targetSurvives,
                    damage.DamageType,
                    baseInjury.RolledDamage,
                    baseInjury.PenetratingDamage,
                    injuryContext.TargetSt));
            if (requestedTiles > 0)
            {
                DclCanonicalInjuryMovementBranchSet? branchSet =
                    target.InjuryMovementBranchesByStrike?[strikeIndex];
                DclCanonicalForcedMovementResult movement =
                    target.InjuryMovementBranchForestsByStrike?[strikeIndex]?.Resolve(
                        target.Target.Unit,
                        resolved.MovementTile,
                        targetKo: !targetSurvives,
                        requestedTiles) ?? branchSet?.Resolve(
                            target.Target.Unit,
                            targetKo: !targetSurvives,
                            requestedTiles) ?? throw new InvalidOperationException(
                            "Area Damage forecast selected Injury displacement without its frozen per-Strike map branch.");
                if (movement.Origin != resolved.MovementTile)
                    throw new InvalidOperationException(
                        "Area Damage forecast movement diverges from its conditional origin timeline.");
                resolved = resolved with
                {
                    TotalMovedTiles = checked(resolved.TotalMovedTiles + movement.MovedTiles),
                    Fell = resolved.Fell || movement.Fell,
                    MovementTile = movement.MovedTiles > 0 ? movement.Destination : resolved.MovementTile,
                };
            }
        }
        bool majorWoundCheck = effect.Route == DclMagicalEffectRoute.Injury &&
            profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate &&
            resolved.CurrentHp > 0 && effect.FinalInjury > target.TargetMaxHp / 2;
        if (!majorWoundCheck) return OneState(resolved);
        var branches = new Dictionary<AreaValueState, DclRational>();
        for (int htRoll = DclSuccessRoll.MinRoll; htRoll <= DclSuccessRoll.MaxRoll; htRoll++)
        {
            DclRational probability = new(DclSuccessRoll.OutcomeMultiplicity(htRoll), 216);
            bool collapsed = !DclSuccessRoll.Succeeds(htRoll, target.EffectiveHt!.Value);
            Add(branches, collapsed
                ? resolved with { ImmediateStun = true, ImmediateKnockedDown = true }
                : resolved, probability);
        }
        return branches;
    }

    private static IReadOnlyDictionary<AreaValueState, DclRational> OneState(AreaValueState state)
        => new Dictionary<AreaValueState, DclRational> { [state] = One };

    private static AreaValueState InitialValueState(DclCanonicalAreaTargetEvaluationInput target)
        => new(
            target.Target.CurrentHp,
            target.ResourceTargetPools?.CurrentMp ?? 0,
            target.OilContributed,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            target.Target.Tile,
            false,
            false);

    private static IReadOnlyDictionary<DclCastingOutcome, DclRational> EnumerateBaseOutcomes(int baseSpellScore)
    {
        var weights = Enum.GetValues<DclCastingOutcome>().ToDictionary(outcome => outcome, _ => 0);
        for (int roll = DclSuccessRoll.MinRoll; roll <= DclSuccessRoll.MaxRoll; roll++)
        {
            DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
                roll,
                baseSpellScore,
                baseSpellScore);
            weights[gate.BaseOutcome] += DclSuccessRoll.OutcomeMultiplicity(roll);
        }
        return weights.Where(pair => pair.Value > 0)
            .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, 216));
    }

    private static DclRational ConditionalContestProbability(int actingScore, int actingRoll, int targetScore)
    {
        int successes = 0;
        for (int targetRoll = DclSuccessRoll.MinRoll; targetRoll <= DclSuccessRoll.MaxRoll; targetRoll++)
        {
            if (DclQuickContest.Resolve(actingScore, actingRoll, targetScore, targetRoll).ActingSideWon)
                successes += DclSuccessRoll.OutcomeMultiplicity(targetRoll);
        }
        return new DclRational(successes, 216);
    }

    private static void ValidateTargetGate(DclAreaDeliveryGate gate, int? score)
    {
        if (gate == DclAreaDeliveryGate.None && score is not null)
            throw new ArgumentException("Area gate None cannot own a target gate score.");
        if (gate is DclAreaDeliveryGate.Dodge or DclAreaDeliveryGate.QuickContest && score is null)
            throw new ArgumentException("Area Dodge/QuickContest evaluation requires the current target gate score.");
    }

    private static DclCanonicalAreaSourceResourceEvaluation EnumerateSourceResourceChange(
        DclActionProfile profile,
        int baseSpellScore,
        DclTargetBatch targetBatch,
        IReadOnlyDictionary<DclUnitKey, DclCanonicalAreaTargetEvaluationInput> targetInputs,
        DclCanonicalResourcePoolSnapshot sourcePools)
    {
        var outcomes = new Dictionary<DclCanonicalAreaSourceResourceOutcome, DclRational>();
        for (int casterRoll = DclSuccessRoll.MinRoll; casterRoll <= DclSuccessRoll.MaxRoll; casterRoll++)
        {
            DclRational casterProbability = new(DclSuccessRoll.OutcomeMultiplicity(casterRoll), 216);
            var states = new Dictionary<SourceAggregateState, DclRational>
            {
                [SourceAggregateState.Initial(sourcePools)] = One,
            };
            foreach (DclTargetResolutionSnapshot snapshot in targetBatch.Targets)
            {
                DclCanonicalAreaTargetEvaluationInput target = targetInputs[snapshot.Target];
                var next = new Dictionary<SourceAggregateState, DclRational>();
                foreach ((SourceAggregateState state, DclRational stateProbability) in states)
                {
                    IReadOnlyDictionary<SourceAggregateState, DclRational> branches =
                        EnumerateOneResourceTarget(profile, baseSpellScore, casterRoll, target, sourcePools, state);
                    foreach ((SourceAggregateState branch, DclRational probability) in branches)
                        AddProbability(next, branch, stateProbability * probability);
                }
                states = next;
            }
            foreach ((SourceAggregateState state, DclRational probability) in states)
            {
                var outcome = new DclCanonicalAreaSourceResourceOutcome(
                    new DclCanonicalNativeNumericChannels(
                        state.TotalHpDebit,
                        state.TotalHpCredit,
                        state.TotalMpDebit,
                        state.TotalMpCredit),
                    state.SourceKo);
                AddProbability(outcomes, outcome, casterProbability * probability);
            }
        }
        DclRational total = outcomes.Values.Aggregate(Zero, (sum, probability) => sum + probability);
        if (total != One)
            throw new InvalidOperationException($"Area ResourceChange source probability mass is {total}, not one.");
        return new DclCanonicalAreaSourceResourceEvaluation(outcomes);
    }

    private static IReadOnlyDictionary<SourceAggregateState, DclRational> EnumerateOneResourceTarget(
        DclActionProfile profile,
        int baseSpellScore,
        int casterRoll,
        DclCanonicalAreaTargetEvaluationInput target,
        DclCanonicalResourcePoolSnapshot sourceTemplate,
        SourceAggregateState source)
    {
        DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
            casterRoll,
            baseSpellScore,
            target.TargetSpellScore);
        if (!gate.BaseSucceeded || !gate.TargetSucceeded)
            return new Dictionary<SourceAggregateState, DclRational> { [source] = One };
        DclRational landed = profile.TargetProfile.Area!.DeliveryGate switch
        {
            DclAreaDeliveryGate.None => One,
            DclAreaDeliveryGate.Dodge when gate.TargetCritical => One,
            DclAreaDeliveryGate.Dodge => One - new DclRational(
                DclSuccessRoll.SuccessOutcomeCount(target.TargetGateScore!.Value), 216),
            DclAreaDeliveryGate.QuickContest => ConditionalContestProbability(
                target.TargetSpellScore,
                casterRoll,
                target.TargetGateScore!.Value),
            _ => throw new ArgumentOutOfRangeException(),
        };
        DclCanonicalResourcePoolSnapshot targetPools = target.ResourceTargetPools ??
            throw new ArgumentException("Area ResourceChange source evaluation lost target pools.", nameof(target));
        return RunResourceStrikes(profile, targetPools, sourceTemplate, source, landed);
    }

    private static IReadOnlyDictionary<SourceAggregateState, DclRational> RunResourceStrikes(
        DclActionProfile profile,
        DclCanonicalResourcePoolSnapshot targetPools,
        DclCanonicalResourcePoolSnapshot sourceTemplate,
        SourceAggregateState source,
        DclRational landedProbability)
    {
        DclFixedResourceMagnitude magnitude = (DclFixedResourceMagnitude)profile.MagnitudeProfile!;
        if (!DclDiceExpression.TryParseAuthored(magnitude.Expression, out DclDiceExpression expression))
            throw new InvalidOperationException("Area ResourceChange magnitude does not use the exact Xd6+Y grammar.");
        DclExactIntegerDistribution distribution = DclExactIntegerDistribution.Roll(expression);
        var states = new Dictionary<ResourceTargetSourceState, DclRational>
        {
            [new ResourceTargetSourceState(targetPools.CurrentHp, targetPools.CurrentMp, source)] = One,
        };
        for (int strikeIndex = 0; strikeIndex < profile.TransactionProfile.StrikeCount; strikeIndex++)
        {
            var next = new Dictionary<ResourceTargetSourceState, DclRational>();
            foreach ((ResourceTargetSourceState state, DclRational stateProbability) in states)
            {
                if (state.TargetHp == 0)
                {
                    AddProbability(next, state, stateProbability);
                    continue;
                }
                if (landedProbability < One)
                    AddProbability(next, state, stateProbability * (One - landedProbability));
                if (landedProbability == Zero) continue;
                foreach ((int rolled, BigInteger weight) in distribution.Weights)
                {
                    DclCanonicalResourceChangeResult changed = DclCanonicalResourceChange.ResolveMagnitude(
                        profile,
                        targetPools with { CurrentHp = state.TargetHp, CurrentMp = state.TargetMp },
                        sourceTemplate with
                        {
                            CurrentHp = state.Source.CurrentHp,
                            CurrentMp = state.Source.CurrentMp,
                        },
                        Math.Max(0, rolled));
                    SourceAggregateState updatedSource = state.Source.Apply(changed.SourceChannels, changed.SourceKo);
                    var updated = new ResourceTargetSourceState(
                        checked(state.TargetHp - changed.TargetChannels.HpDebit + changed.TargetChannels.HpCredit),
                        checked(state.TargetMp - changed.TargetChannels.MpDebit + changed.TargetChannels.MpCredit),
                        updatedSource);
                    AddProbability(
                        next,
                        updated,
                        stateProbability * landedProbability * new DclRational(weight, distribution.TotalOutcomes));
                }
            }
            states = next;
        }
        var collapsed = new Dictionary<SourceAggregateState, DclRational>();
        foreach ((ResourceTargetSourceState state, DclRational probability) in states)
            AddProbability(collapsed, state.Source, probability);
        return collapsed;
    }

    private static void AddProbability<TKey>(
        Dictionary<TKey, DclRational> probabilities,
        TKey key,
        DclRational probability)
        where TKey : notnull
        => probabilities[key] = probabilities.GetValueOrDefault(key, Zero) + probability;

    private static DclMagicCorrelatedForecastResult EmptyCorrelated()
        => new(
            new Dictionary<int, DclRational> { [0] = One },
            new Dictionary<DclUnitKey, DclRational>(),
            new Dictionary<DclUnitKey, DclRational>(),
            new Dictionary<DclUnitKey, IReadOnlyDictionary<int, DclRational>>());

    private static DclCanonicalAreaMagicEvaluationResult Empty(
        DclAbilityBinding binding,
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
            attempt.CostCommitment,
            attempt.Timing,
            Center: null,
            TargetBatch: null,
            EmptyCorrelated(),
            Targets: [],
            new DclCanonicalResourceEvaluation(
                new Dictionary<DclCastingOutcome, DclRational>(),
                new Dictionary<DclCastingOutcome, DclResourcePayment>(),
                Zero,
                Zero,
                Zero),
            ResourceFailed);

    private static void Merge(
        Dictionary<AreaValueState, DclRational> destination,
        IReadOnlyDictionary<AreaValueState, DclRational> source,
        DclRational factor)
    {
        if (factor == Zero) return;
        foreach ((AreaValueState state, DclRational probability) in source)
            Add(destination, state, probability * factor);
    }

    private static void Add(
        Dictionary<AreaValueState, DclRational> probabilities,
        AreaValueState state,
        DclRational probability)
    {
        if (probability == Zero) return;
        probabilities[state] = probabilities.GetValueOrDefault(state, Zero) + probability;
    }

    private static Dictionary<int, DclRational> Marginal(
        IReadOnlyDictionary<AreaValueState, DclRational> states,
        Func<AreaValueState, int> selector)
    {
        var result = new Dictionary<int, DclRational>();
        foreach ((AreaValueState state, DclRational probability) in states)
        {
            int value = selector(state);
            result[value] = result.GetValueOrDefault(value, Zero) + probability;
        }
        return result;
    }

    private static DclExactValueForecast Forecast(IReadOnlyDictionary<int, DclRational> probabilities)
    {
        DclRational total = probabilities.Values.Aggregate(Zero, (sum, probability) => sum + probability);
        if (total != One) throw new ArgumentException("Forecast probabilities must sum to one.", nameof(probabilities));
        return new DclExactValueForecast(
            probabilities.Keys.Min(),
            probabilities.Keys.Max(),
            probabilities.Aggregate(Zero, (sum, pair) =>
                sum + DclRational.FromInteger(pair.Key) * pair.Value),
            probabilities.OrderBy(pair => pair.Key).ToDictionary());
    }

    private readonly record struct AreaValueState(
        int CurrentHp,
        int CurrentMp,
        bool OilAvailable,
        int TotalHpDebit,
        int TotalHpCredit,
        int TotalMpDebit,
        int TotalMpCredit,
        int LandedStrikes,
        int TotalMovedTiles,
        bool Fell,
        DclBattleTile MovementTile,
        bool ImmediateStun,
        bool ImmediateKnockedDown);

    private readonly record struct SourceAggregateState(
        int CurrentHp,
        int CurrentMp,
        int TotalHpDebit,
        int TotalHpCredit,
        int TotalMpDebit,
        int TotalMpCredit,
        bool SourceKo)
    {
        public static SourceAggregateState Initial(DclCanonicalResourcePoolSnapshot source)
            => new(source.CurrentHp, source.CurrentMp, 0, 0, 0, 0, false);

        public SourceAggregateState Apply(DclCanonicalNativeNumericChannels channels, bool sourceKo)
            => new(
                checked(CurrentHp - channels.HpDebit + channels.HpCredit),
                checked(CurrentMp - channels.MpDebit + channels.MpCredit),
                checked(TotalHpDebit + channels.HpDebit),
                checked(TotalHpCredit + channels.HpCredit),
                checked(TotalMpDebit + channels.MpDebit),
                checked(TotalMpCredit + channels.MpCredit),
                SourceKo || sourceKo);
    }

    private readonly record struct ResourceTargetSourceState(
        int TargetHp,
        int TargetMp,
        SourceAggregateState Source);
}
