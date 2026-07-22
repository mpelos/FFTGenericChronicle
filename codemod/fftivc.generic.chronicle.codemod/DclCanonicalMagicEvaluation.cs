using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalMagicEvaluationInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    DclDefenseOption Defense,
    int MagnitudeAttribute,
    DclElementAffinity Affinity,
    DclRational FaithMagnitude,
    bool TargetHasShell,
    int TargetMaxHp,
    bool FireEffect,
    bool OilContributed,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    bool DeclaredTargetHasReflect = false,
    bool ReflectionAlreadyConsumed = false,
    DclTargetCandidate? ReflectedTarget = null,
    int ApplicableDr = 0,
    int AdditionalMagnitudeIntegerModifier = 0,
    int? ResistanceScore = null,
    bool Immune = false,
    DclCanonicalResourcePoolSnapshot? ResourceTargetPools = null,
    DclCanonicalResourcePoolSnapshot? ResourceSourcePools = null,
    IReadOnlyList<DclDefenseCandidate>? TouchDefenseCandidates = null,
    DclTouchNativeRouteVerdict? TouchRouteVerdict = null,
    IReadOnlyList<DclMagicStatusRiderForecast>? StatusRiders = null,
    DclCanonicalInjuryTargetContext? InjuryTargetContext = null,
    int AuthoredForcedDisplacement = 0,
    DclCanonicalInjuryMovementBranchSet? InjuryMovementBranches = null);

internal sealed record DclCanonicalResourceEvaluation(
    IReadOnlyDictionary<DclCastingOutcome, DclRational> BaseOutcomeProbability,
    IReadOnlyDictionary<DclCastingOutcome, DclResourcePayment> PaymentByOutcome,
    DclRational ExpectedMpDebit,
    DclRational ExpectedHpDebit,
    DclRational PayerKoProbability);

internal sealed record DclCanonicalMagicEvaluationResult(
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    string ForecastBoundary,
    string AiBoundary,
    bool Legal,
    IReadOnlyList<string> Failures,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    DclReflectRoute ReflectRoute,
    IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> DeliveryOutcomeProbability,
    DclRational DefenseAttemptProbability,
    DclRational BlockSpendProbability,
    DclRational ParrySpendProbability,
    DclDiceExpression? NormalMagnitudeExpression,
    DclDiceExpression? CriticalMagnitudeExpression,
    DclExactValueForecast TargetHpDebit,
    DclExactValueForecast TargetHpCredit,
    DclCanonicalResourceEvaluation Resources,
    bool ResourceFailed,
    IReadOnlyDictionary<int, DclRational> RiderApplicationProbability,
    DclRational ExpectedMovedTiles,
    DclRational FallProbability,
    DclCanonicalResourceChangeEvaluation? ResourceChange = null)
{
    public DclRational DeliveryProbability
        => Probability(DclMagicDeliveryOutcome.Delivered) + Probability(DclMagicDeliveryOutcome.CriticalDelivered);

    public DclRational CriticalDeliveryProbability
        => Probability(DclMagicDeliveryOutcome.CriticalDelivered);

    public DclRational Probability(DclMagicDeliveryOutcome outcome)
        => DeliveryOutcomeProbability.GetValueOrDefault(outcome, DclRational.FromInteger(0));
}

internal sealed record DclCanonicalMagicAiProjection(
    int AbilityId,
    string ActionId,
    string AiBoundary,
    bool Legal,
    DclRational ExpectedTargetHpDebit,
    DclRational ExpectedTargetHpCredit,
    DclRational ExpectedCasterMpDebit,
    DclRational ExpectedCasterHpDebit,
    DclRational PayerKoProbability,
    DclRational DeliveryProbability,
    DclRational DefenseAttemptProbability,
    DclRational BlockSpendProbability,
    DclRational ParrySpendProbability,
    DclRational ExpectedMovedTiles,
    DclRational FallProbability,
    int NativeExpectedHpDebit,
    int NativeExpectedHpCredit,
    IReadOnlyDictionary<int, DclRational> RiderApplicationProbability,
    DclCanonicalResourceChangeEvaluation? ResourceChange)
{
    public static DclCanonicalMagicAiProjection From(DclCanonicalMagicEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalMagicAiProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.AiBoundary,
            evaluation.Legal,
            evaluation.TargetHpDebit.ExpectedValue,
            evaluation.TargetHpCredit.ExpectedValue,
            evaluation.Resources.ExpectedMpDebit,
            evaluation.Resources.ExpectedHpDebit,
            evaluation.Resources.PayerKoProbability,
            evaluation.DeliveryProbability,
            evaluation.DefenseAttemptProbability,
            evaluation.BlockSpendProbability,
            evaluation.ParrySpendProbability,
            evaluation.ExpectedMovedTiles,
            evaluation.FallProbability,
            checked((int)evaluation.TargetHpDebit.ExpectedValue.RoundNearest()),
            checked((int)evaluation.TargetHpCredit.ExpectedValue.RoundNearest()),
            evaluation.RiderApplicationProbability,
            evaluation.ResourceChange);
    }
}

internal sealed record DclCanonicalMagicForecastProjection(
    int AbilityId,
    string ActionId,
    string ForecastBoundary,
    bool Legal,
    int DeliveryPercent,
    int DefenseAttemptPercent,
    int BlockSpendPercent,
    int ParrySpendPercent,
    DclRational ExpectedMovedTiles,
    int FallChancePercent,
    string? NormalMagnitude,
    string? CriticalMagnitude,
    int MinimumHpDebit,
    int MaximumHpDebit,
    int MinimumHpCredit,
    int MaximumHpCredit,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    DclReflectRoute ReflectRoute,
    IReadOnlyDictionary<int, int> RiderApplicationPercent,
    DclCanonicalResourceChangeEvaluation? ResourceChange)
{
    public static DclCanonicalMagicForecastProjection From(DclCanonicalMagicEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclCanonicalMagicForecastProjection(
            evaluation.AbilityId,
            evaluation.ActionId,
            evaluation.ForecastBoundary,
            evaluation.Legal,
            checked((int)(evaluation.DeliveryProbability * DclRational.FromInteger(100)).RoundNearest()),
            checked((int)(evaluation.DefenseAttemptProbability * DclRational.FromInteger(100)).RoundNearest()),
            checked((int)(evaluation.BlockSpendProbability * DclRational.FromInteger(100)).RoundNearest()),
            checked((int)(evaluation.ParrySpendProbability * DclRational.FromInteger(100)).RoundNearest()),
            evaluation.ExpectedMovedTiles,
            checked((int)(evaluation.FallProbability * DclRational.FromInteger(100)).RoundNearest()),
            evaluation.NormalMagnitudeExpression?.ToString(),
            evaluation.CriticalMagnitudeExpression?.ToString(),
            evaluation.TargetHpDebit.Minimum,
            evaluation.TargetHpDebit.Maximum,
            evaluation.TargetHpCredit.Minimum,
            evaluation.TargetHpCredit.Maximum,
            evaluation.CostCommitment,
            evaluation.Timing,
            evaluation.ReflectRoute,
            evaluation.RiderApplicationProbability.ToDictionary(
                pair => pair.Key,
                pair => checked((int)(pair.Value * DclRational.FromInteger(100)).RoundNearest())),
            evaluation.ResourceChange);
    }
}

internal static class DclCanonicalMagicEvaluationExecutor
{
    private static readonly DclRational One = DclRational.FromInteger(1);
    private static readonly DclRational Zero = DclRational.FromInteger(0);

    public static DclCanonicalMagicEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalMagicEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind != DclNativeCarrierKind.SingleResult ||
            profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.TransactionProfile.StrikeCount != 1 ||
            profile.MagnitudeProfile is not (DclDamageMagnitude or DclHealingMagnitude or DclFixedResourceMagnitude))
            throw new InvalidOperationException("This canonical evaluation vertical requires one numeric unit-targeted SingleResult action.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Evaluation request does not use the bound normalized action revision.", nameof(input));
        if (input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("Evaluation target must equal the original tracked declaration target.", nameof(input));
        if (profile.Effects.Count == 0 || profile.Effects[0] is not
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.Damage or DclEffectKind.Healing or DclEffectKind.ResourceChange,
            } || profile.Effects.Skip(1).Any(effect => effect is not
            {
                Role: DclEffectRole.Rider,
                Kind: DclEffectKind.StatusApplication,
                ReferencedStateKind: not null,
            }))
            throw new InvalidOperationException(
                "Direct numeric evaluation requires one numeric Carrier followed only by normalized status Riders.");
        int[] expectedRiderIndexes = Enumerable.Range(1, profile.Effects.Count - 1).ToArray();
        DclMagicStatusRiderForecast[] riderForecasts = (input.StatusRiders ?? []).ToArray();
        if (riderForecasts.Select(rider => rider.EffectIndex).Distinct().Count() != riderForecasts.Length ||
            !riderForecasts.Select(rider => rider.EffectIndex).Order().SequenceEqual(expectedRiderIndexes))
            throw new ArgumentException(
                "Direct numeric evaluation must provide every normalized status Rider exactly once.",
                nameof(input));
        for (int index = 0; index < riderForecasts.Length; index++)
        {
            int effectIndex = riderForecasts[index].EffectIndex;
            string stateKind = profile.Effects[effectIndex].ReferencedStateKind!;
            if (!runtime.Authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition))
                throw new InvalidOperationException($"Direct status Rider {effectIndex} lost state definition '{stateKind}'.");
            if (definition.ResistanceGate == DclStateResistanceGate.Explicit)
                throw new InvalidOperationException(
                    $"Direct status Rider {effectIndex} uses Explicit resistance and requires its named evaluator.");
            riderForecasts[index] = riderForecasts[index] with { ResistanceGate = definition.ResistanceGate };
        }

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, actionInstanceId: 1);
        DclReflectRoute reflectRoute = DclReflectRouting.Resolve(
            profile.TargetProfile.TargetMode,
            profile.DeliveryProfile.Reflectable,
            input.DeclaredTargetHasReflect,
            input.ReflectionAlreadyConsumed,
            input.DeclarationRequest.Caster,
            input.Target.Unit);
        DclTargetCandidate resolutionTarget = ResolveTarget(input, reflectRoute);
        DclDefenseCandidate[] touchDefenseCandidates = (input.TouchDefenseCandidates ?? []).ToArray();
        DclTouchEvaluationResult? touchEvaluation = null;
        if (profile.DeliveryProfile.Delivery == DclDelivery.Touch)
        {
            if (input.Defense != new DclDefenseOption(DclDefenseKind.None, 0, Depletes: false))
                throw new ArgumentException("Touch evaluation owns defense candidates rather than one magic-defense option.", nameof(input));
            touchEvaluation = DclTouchResolution.Evaluate(
                profile.DeliveryProfile,
                input.BaseSpellScore,
                touchDefenseCandidates,
                resolutionTarget.DefenseResources);
            DclTouchNativeRouteVerdict routeVerdict = input.TouchRouteVerdict ??
                throw new ArgumentException("Touch evaluation requires one immutable native range/trajectory verdict.", nameof(input));
            IReadOnlyList<string> routeFailures = DclTouchResolution.RouteFailures(profile, routeVerdict);
            if (routeFailures.Count != 0)
                return Empty(binding, attempt, reflectRoute, routeFailures, ResourceFailed: false);
        }
        else
        {
            if (touchDefenseCandidates.Length != 0 || input.TouchRouteVerdict is not null)
                throw new ArgumentException("Only Touch evaluation may supply Touch defense candidates.", nameof(input));
            DclMagicDefensePolicy.Validate(profile.DeliveryProfile, input.Defense, resolutionTarget.DefenseResources);
        }
        if (input.TargetMaxHp < 1 || resolutionTarget.CurrentHp > input.TargetMaxHp)
            throw new ArgumentOutOfRangeException(nameof(input), "Evaluation MaxHP must contain the routed target's current HP.");
        if (input.AuthoredForcedDisplacement < 0)
            throw new ArgumentOutOfRangeException(nameof(input), "Authored Injury displacement cannot be negative.");
        if (profile.MagnitudeProfile is DclDamageMagnitude)
        {
            if (input.InjuryTargetContext is not { } injuryContext ||
                injuryContext.MaxHp != input.TargetMaxHp || injuryContext.TargetSt < 1 ||
                injuryContext.UnexpiredShockInjury < 0 ||
                injuryContext.ConcentrationStatePenaltyMagnitude < 0)
                throw new ArgumentException(
                    "Direct damage evaluation requires one valid exact Injury target context.",
                    nameof(input));
            input.InjuryMovementBranches?.Validate(resolutionTarget.Unit);
        }
        else if (input.InjuryTargetContext is not null || input.AuthoredForcedDisplacement != 0 ||
            input.InjuryMovementBranches is not null)
        {
            throw new ArgumentException(
                "Only direct damage evaluation can own Injury displacement inputs.",
                nameof(input));
        }

        if (!attempt.Legal)
            return Empty(binding, attempt, reflectRoute, attempt.Failures, ResourceFailed: false);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
            return Empty(binding, attempt, reflectRoute, ["resource-failure-at-resolution"], ResourceFailed: true);

        (IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> delivery,
            IReadOnlyDictionary<DclCastingOutcome, DclRational> baseOutcomes) = touchEvaluation is null
            ? EnumerateDelivery(
                profile,
                input.BaseSpellScore,
                input.TargetSpellScore,
                input.Defense,
                input.ResistanceScore,
                input.Immune)
            : EnumerateTouchDelivery(touchEvaluation);
        DclRational defenseAttemptProbability = touchEvaluation?.DefenseAttemptProbability ??
            DclMagicDefensePolicy.AttemptProbability(
                profile.DeliveryProfile,
                input.BaseSpellScore,
                input.TargetSpellScore,
                input.Defense);
        DclRational blockSpendProbability = input.Defense.Kind == DclDefenseKind.Block
            ? defenseAttemptProbability
            : Zero;
        DclRational parrySpendProbability = touchEvaluation?.ParrySpendProbability ?? Zero;
        IReadOnlyDictionary<int, DclRational> riderApplicationProbability = EnumerateRiderApplications(
            profile,
            input.BaseSpellScore,
            input.TargetSpellScore,
            input.Defense,
            input.ResistanceScore,
            input.Immune,
            touchDefenseCandidates,
            resolutionTarget.DefenseResources,
            riderForecasts);
        DclCanonicalResourceEvaluation resources = EvaluateResources(
            baseOutcomes,
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment);

        DclDiceExpression normalExpression;
        DclDiceExpression criticalExpression;
        DclExactIntegerDistribution normalDebit;
        DclExactIntegerDistribution criticalDebit;
        DclExactIntegerDistribution normalCredit;
        DclExactIntegerDistribution criticalCredit;
        DclExactIntegerDistribution? injuryMagnitudeDistribution = null;
        DclCanonicalResourceChangeEvaluation? conditionalResourceChange = null;
        switch (profile.MagnitudeProfile)
        {
            case DclDamageMagnitude damage:
            {
                normalExpression = DclCanonicalMagnitude.BuildDiceExpression(
                    input.MagnitudeAttribute,
                    damage.Basis,
                    damage.FixedExpression,
                    checked(damage.IntegerModifier + input.AdditionalMagnitudeIntegerModifier),
                    damage.WholeDiceModifier);
                criticalExpression = normalExpression;
                DclExactIntegerDistribution rolled = DclExactIntegerDistribution.Roll(normalExpression);
                injuryMagnitudeDistribution = rolled;
                normalDebit = rolled.Transform(raw => ResolveDamageChannels(
                    raw, damage, profile.DeliveryProfile, input, resolutionTarget).HpDebit);
                normalCredit = rolled.Transform(raw => ResolveDamageChannels(
                    raw, damage, profile.DeliveryProfile, input, resolutionTarget).HpCredit);
                criticalDebit = normalDebit;
                criticalCredit = normalCredit;
                break;
            }
            case DclHealingMagnitude healing:
            {
                normalExpression = DclCanonicalMagnitude.BuildDiceExpression(
                    input.MagnitudeAttribute,
                    healing.Basis,
                    healing.FixedExpression,
                    checked(healing.IntegerModifier + input.AdditionalMagnitudeIntegerModifier),
                    healing.WholeDiceModifier);
                DclCriticalHealingExpression critical = profile.CriticalProfile.SuccessEffect ==
                    DclCriticalSuccessEffect.MaximizeOneHealingDie
                    ? DclMagicMagnitude.CriticalHealingExpression(normalExpression)
                    : new DclCriticalHealingExpression(normalExpression.Dice, normalExpression.Adds);
                criticalExpression = new DclDiceExpression(critical.DiceToRoll, critical.Adds);
                normalCredit = DclExactIntegerDistribution.Roll(normalExpression).Transform(raw =>
                    DclMagicalEffect.ResolveHealing(
                        Math.Max(0, raw),
                        input.FaithMagnitude,
                        resolutionTarget.CurrentHp,
                        input.TargetMaxHp).AppliedHealing);
                criticalCredit = DclExactIntegerDistribution.Roll(criticalExpression).Transform(raw =>
                    DclMagicalEffect.ResolveHealing(
                        Math.Max(0, raw),
                        input.FaithMagnitude,
                        resolutionTarget.CurrentHp,
                        input.TargetMaxHp).AppliedHealing);
                normalDebit = ZeroDistribution();
                criticalDebit = ZeroDistribution();
                break;
            }
            case DclFixedResourceMagnitude fixedResource:
            {
                if (profile.ResourceChangeProfile is null ||
                    !profile.Effects.Any(effect => effect.Kind == DclEffectKind.ResourceChange))
                    throw new InvalidOperationException("The generic fixed-resource evaluation vertical requires a normalized ResourceChange profile.");
                DclCanonicalResourcePoolSnapshot targetPools = input.ResourceTargetPools ??
                    throw new ArgumentException("ResourceChange evaluation requires exact routed-target pools.", nameof(input));
                DclCanonicalResourcePoolSnapshot sourcePools = input.ResourceSourcePools ??
                    throw new ArgumentException("ResourceChange evaluation requires exact source pools.", nameof(input));
                if (targetPools.CurrentHp != resolutionTarget.CurrentHp ||
                    sourcePools.CurrentHp != input.CurrentHpAtResolution ||
                    sourcePools.CurrentMp != input.CurrentMpAtResolution)
                    throw new ArgumentException("ResourceChange pools must match the current routed target and payer snapshots.", nameof(input));
                if (!DclDiceExpression.TryParseAuthored(fixedResource.Expression, out normalExpression))
                    throw new InvalidOperationException("ResourceChange magnitude does not use the exact Xd6+Y grammar.");
                criticalExpression = normalExpression;
                DclExactIntegerDistribution rolled = DclExactIntegerDistribution.Roll(normalExpression);
                normalDebit = rolled.Transform(raw => DclCanonicalResourceChange.ResolveMagnitude(
                    profile, targetPools, sourcePools, Math.Max(0, raw)).TargetChannels.HpDebit);
                normalCredit = rolled.Transform(raw => DclCanonicalResourceChange.ResolveMagnitude(
                    profile, targetPools, sourcePools, Math.Max(0, raw)).TargetChannels.HpCredit);
                criticalDebit = normalDebit;
                criticalCredit = normalCredit;
                conditionalResourceChange = DclCanonicalResourceChange.Evaluate(profile, targetPools, sourcePools);
                break;
            }
            default:
                throw new InvalidOperationException();
        }
        if (conditionalResourceChange is not null &&
            conditionalResourceChange.RejectionProbability > Zero)
        {
            DclRational acceptedCarrier = One - conditionalResourceChange.RejectionProbability;
            riderApplicationProbability = riderApplicationProbability.ToDictionary(
                pair => pair.Key,
                pair => pair.Value * acceptedCarrier);
        }

        DclRational normalProbability = delivery.GetValueOrDefault(DclMagicDeliveryOutcome.Delivered, Zero);
        DclRational criticalProbability = delivery.GetValueOrDefault(DclMagicDeliveryOutcome.CriticalDelivered, Zero);
        DclRational noDeliveryProbability = One - normalProbability - criticalProbability;
        DclExactValueForecast hpDebit = DclExactValueForecast.Mixture(
            noDeliveryProbability,
            [(normalProbability, normalDebit), (criticalProbability, criticalDebit)]);
        DclExactValueForecast hpCredit = DclExactValueForecast.Mixture(
            noDeliveryProbability,
            [(normalProbability, normalCredit), (criticalProbability, criticalCredit)]);
        (DclRational expectedMovedTiles, DclRational fallProbability) =
            profile.MagnitudeProfile is DclDamageMagnitude movementDamage && injuryMagnitudeDistribution is not null
                ? EnumerateInjuryMovement(
                    movementDamage,
                    profile.DeliveryProfile,
                    input,
                    resolutionTarget,
                    injuryMagnitudeDistribution,
                    normalProbability,
                    criticalProbability)
                : (Zero, Zero);
        DclCanonicalResourceChangeEvaluation? resourceChange = conditionalResourceChange?
            .WithApplicationProbability(normalProbability + criticalProbability);

        return new DclCanonicalMagicEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: true,
            Failures: [],
            attempt.CostCommitment,
            attempt.Timing,
            reflectRoute,
            delivery,
            defenseAttemptProbability,
            blockSpendProbability,
            parrySpendProbability,
            normalExpression,
            criticalExpression,
            hpDebit,
            hpCredit,
            resources,
            ResourceFailed: false,
            riderApplicationProbability,
            expectedMovedTiles,
            fallProbability,
            resourceChange);
    }

    private static (DclRational ExpectedMovedTiles, DclRational FallProbability) EnumerateInjuryMovement(
        DclDamageMagnitude damage,
        DclDeliveryProfile delivery,
        DclCanonicalMagicEvaluationInput input,
        DclTargetCandidate resolutionTarget,
        DclExactIntegerDistribution magnitudeDistribution,
        DclRational normalDeliveryProbability,
        DclRational criticalDeliveryProbability)
    {
        DclCanonicalInjuryTargetContext context = input.InjuryTargetContext ??
            throw new InvalidOperationException("Direct damage movement evaluation lost its Injury target context.");
        DclRational expectedMovedTiles = Zero;
        DclRational fallProbability = Zero;
        foreach ((bool critical, DclRational deliveryProbability) in new[]
                 {
                     (false, normalDeliveryProbability),
                     (true, criticalDeliveryProbability),
                 })
        {
            if (deliveryProbability == Zero) continue;
            foreach ((int raw, BigInteger weight) in magnitudeDistribution.Weights)
            {
                DclRational branchProbability = deliveryProbability *
                    new DclRational(weight, magnitudeDistribution.TotalOutcomes);
                DclInjuryResult baseInjury = DclCanonicalMagnitude.ResolveMagicalBaseInjury(
                    Math.Max(0, raw),
                    damage.DamageType,
                    input.ApplicableDr,
                    delivery);
                DclMagicalEffectResult routed = DclMagicalEffect.ResolveInjuryOrAbsorb(
                    baseInjury.Injury,
                    input.Affinity,
                    input.FaithMagnitude,
                    delivery.ShellSensitive,
                    input.TargetHasShell,
                    resolutionTarget.CurrentHp,
                    input.TargetMaxHp,
                    input.FireEffect,
                    input.OilContributed);
                if (routed.Route != DclMagicalEffectRoute.Injury) continue;
                bool targetSurvives = resolutionTarget.CurrentHp > routed.FinalInjury;
                int requestedTiles = checked(input.AuthoredForcedDisplacement +
                    DclInjury.CriticalKnockbackTiles(
                        critical,
                        targetSurvives,
                        damage.DamageType,
                        baseInjury.RolledDamage,
                        baseInjury.PenetratingDamage,
                        context.TargetSt));
                if (requestedTiles == 0) continue;
                DclCanonicalForcedMovementResult movement = input.InjuryMovementBranches?.Resolve(
                    resolutionTarget.Unit,
                    targetKo: !targetSurvives,
                    requestedTiles) ?? throw new InvalidOperationException(
                    "Direct damage forecast selected Injury displacement without its frozen native map branch.");
                expectedMovedTiles += DclRational.FromInteger(movement.MovedTiles) * branchProbability;
                if (movement.Fell) fallProbability += branchProbability;
            }
        }
        return (expectedMovedTiles, fallProbability);
    }

    private static (int HpDebit, int HpCredit) ResolveDamageChannels(
        int raw,
        DclDamageMagnitude damage,
        DclDeliveryProfile delivery,
        DclCanonicalMagicEvaluationInput input,
        DclTargetCandidate resolutionTarget)
    {
        DclInjuryResult baseInjury = DclCanonicalMagnitude.ResolveMagicalBaseInjury(
            Math.Max(0, raw),
            damage.DamageType,
            input.ApplicableDr,
            delivery);
        DclMagicalEffectResult effect = DclMagicalEffect.ResolveInjuryOrAbsorb(
            baseInjury.Injury,
            input.Affinity,
            input.FaithMagnitude,
            delivery.ShellSensitive,
            input.TargetHasShell,
            resolutionTarget.CurrentHp,
            input.TargetMaxHp,
            input.FireEffect,
            input.OilContributed);
        return effect.Route switch
        {
            DclMagicalEffectRoute.Injury => (effect.FinalInjury, 0),
            DclMagicalEffectRoute.AbsorbedHealing => (0, effect.AppliedHealing),
            _ => (0, 0),
        };
    }

    internal static (
        IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> Delivery,
        IReadOnlyDictionary<DclCastingOutcome, DclRational> BaseOutcomes)
        EnumerateTouchDelivery(DclTouchEvaluationResult touch)
    {
        ArgumentNullException.ThrowIfNull(touch);
        var delivery = new Dictionary<DclMagicDeliveryOutcome, DclRational>();
        var baseOutcomes = new Dictionary<DclCastingOutcome, DclRational>();
        foreach ((DclPhysicalOutcome outcome, DclRational probability) in touch.OutcomeProbability)
        {
            DclMagicDeliveryOutcome delivered = outcome switch
            {
                DclPhysicalOutcome.CriticalHit => DclMagicDeliveryOutcome.CriticalDelivered,
                DclPhysicalOutcome.Hit => DclMagicDeliveryOutcome.Delivered,
                DclPhysicalOutcome.Defended => DclMagicDeliveryOutcome.Defended,
                DclPhysicalOutcome.AttackMiss or DclPhysicalOutcome.AttackFumble => DclMagicDeliveryOutcome.BaseFailure,
                _ => throw new InvalidOperationException("Touch evaluation cannot translate a legacy physical outcome."),
            };
            DclCastingOutcome baseOutcome = outcome switch
            {
                DclPhysicalOutcome.CriticalHit => DclCastingOutcome.CriticalSuccess,
                DclPhysicalOutcome.Hit or DclPhysicalOutcome.Defended => DclCastingOutcome.Success,
                DclPhysicalOutcome.AttackMiss => DclCastingOutcome.OrdinaryFailure,
                DclPhysicalOutcome.AttackFumble => DclCastingOutcome.CriticalFailure,
                _ => throw new InvalidOperationException("Touch evaluation cannot translate a legacy physical outcome."),
            };
            delivery[delivered] = delivery.GetValueOrDefault(delivered, Zero) + probability;
            baseOutcomes[baseOutcome] = baseOutcomes.GetValueOrDefault(baseOutcome, Zero) + probability;
        }
        return (delivery, baseOutcomes);
    }

    internal static (
        IReadOnlyDictionary<DclMagicDeliveryOutcome, DclRational> Delivery,
        IReadOnlyDictionary<DclCastingOutcome, DclRational> BaseOutcomes)
        EnumerateDelivery(
            DclActionProfile profile,
            int baseSpellScore,
            int targetSpellScore,
            DclDefenseOption defense,
            int? resistanceScore,
            bool immune)
    {
        if (profile.DeliveryProfile.Delivery is not
            (DclDelivery.ExternalProjectile or DclDelivery.Beneficial or DclDelivery.InternalDirect))
            throw new InvalidOperationException("The direct numeric evaluation vertical supports ExternalProjectile, InternalDirect, or Beneficial delivery.");
        if (profile.DeliveryProfile.Delivery == DclDelivery.ExternalProjectile && defense.Kind == DclDefenseKind.Parry)
            throw new ArgumentException("External Projectile cannot forecast ordinary Parry.", nameof(defense));
        if (profile.DeliveryProfile.Delivery is DclDelivery.Beneficial or DclDelivery.InternalDirect &&
            defense.Kind != DclDefenseKind.None)
            throw new ArgumentException("Beneficial/Internal Direct delivery cannot own active-defense forecast input.", nameof(defense));
        if (profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect && resistanceScore is null)
            throw new ArgumentException("Internal Direct evaluation requires its exact target resistance score.", nameof(resistanceScore));

        int defenseOutcomes = profile.DeliveryProfile.Delivery == DclDelivery.ExternalProjectile &&
            defense.Kind != DclDefenseKind.None ? 216 : 1;
        int resistanceOutcomes = profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect && !immune ? 216 : 1;
        int defenseSuccesses = defenseOutcomes == 1 ? 0 : DclSuccessRoll.SuccessOutcomeCount(defense.Target);
        var deliveryWeights = Enum.GetValues<DclMagicDeliveryOutcome>()
            .ToDictionary(outcome => outcome, _ => BigInteger.Zero);
        var baseWeights = Enum.GetValues<DclCastingOutcome>()
            .ToDictionary(outcome => outcome, _ => BigInteger.Zero);
        for (int roll = DclSuccessRoll.MinRoll; roll <= DclSuccessRoll.MaxRoll; roll++)
        {
            int casterWeight = DclSuccessRoll.OutcomeMultiplicity(roll);
            DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(roll, baseSpellScore, targetSpellScore);
            baseWeights[gate.BaseOutcome] += casterWeight;
            BigInteger expandedWeight = casterWeight * defenseOutcomes * resistanceOutcomes;
            if (!gate.BaseSucceeded)
                deliveryWeights[DclMagicDeliveryOutcome.BaseFailure] += expandedWeight;
            else if (!gate.TargetSucceeded)
                deliveryWeights[DclMagicDeliveryOutcome.TargetFailure] += expandedWeight;
            else if (profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect && immune)
                deliveryWeights[DclMagicDeliveryOutcome.Resisted] += expandedWeight;
            else if (profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect)
            {
                for (int resistanceRoll = DclSuccessRoll.MinRoll;
                     resistanceRoll <= DclSuccessRoll.MaxRoll;
                     resistanceRoll++)
                {
                    int branchWeight = casterWeight * DclSuccessRoll.OutcomeMultiplicity(resistanceRoll);
                    DclMagicDeliveryResult result = DclSpellResolution.ResolveInternal(
                        roll,
                        baseSpellScore,
                        targetSpellScore,
                        resistanceScore!.Value,
                        resistanceRoll,
                        immune: false);
                    deliveryWeights[result.Outcome] += branchWeight;
                }
            }
            else if (gate.TargetCritical)
                deliveryWeights[DclMagicDeliveryOutcome.CriticalDelivered] += expandedWeight;
            else if (defenseOutcomes == 1)
                deliveryWeights[DclMagicDeliveryOutcome.Delivered] += expandedWeight;
            else
            {
                deliveryWeights[DclMagicDeliveryOutcome.Defended] += casterWeight * defenseSuccesses;
                deliveryWeights[DclMagicDeliveryOutcome.Delivered] += casterWeight * (defenseOutcomes - defenseSuccesses);
            }
        }
        BigInteger deliveryTotal = 216 * defenseOutcomes * resistanceOutcomes;
        return (
            deliveryWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, deliveryTotal)),
            baseWeights.Where(pair => pair.Value > 0)
                .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, 216)));
    }

    private static IReadOnlyDictionary<int, DclRational> EnumerateRiderApplications(
        DclActionProfile profile,
        int baseSpellScore,
        int targetSpellScore,
        DclDefenseOption defense,
        int? deliveryResistanceScore,
        bool deliveryImmune,
        IReadOnlyList<DclDefenseCandidate> touchDefenseCandidates,
        DclDefenseResourceSnapshot defenseResources,
        IReadOnlyList<DclMagicStatusRiderForecast> riders)
    {
        var result = riders.ToDictionary(rider => rider.EffectIndex, _ => Zero);
        if (riders.Count == 0) return result;
        DclDefenseCandidate? touchDefense = profile.DeliveryProfile.Delivery == DclDelivery.Touch
            ? DclTouchResolution.Evaluate(
                profile.DeliveryProfile,
                baseSpellScore,
                touchDefenseCandidates,
                defenseResources).SelectedDefense
            : null;
        for (int carrierRoll = DclSuccessRoll.MinRoll; carrierRoll <= DclSuccessRoll.MaxRoll; carrierRoll++)
        {
            DclRational carrierRollProbability = new(
                DclSuccessRoll.OutcomeMultiplicity(carrierRoll),
                216);
            DclRational landedProbability = profile.DeliveryProfile.Delivery switch
            {
                DclDelivery.ExternalProjectile => DirectExternalLandingProbability(
                    carrierRoll,
                    baseSpellScore,
                    targetSpellScore,
                    defense),
                DclDelivery.Beneficial => DclSpellResolution.ClassifySharedRoll(
                    carrierRoll,
                    baseSpellScore,
                    targetSpellScore).TargetSucceeded ? One : Zero,
                DclDelivery.InternalDirect => DirectInternalLandingProbability(
                    carrierRoll,
                    baseSpellScore,
                    targetSpellScore,
                    deliveryResistanceScore ?? throw new ArgumentException(
                        "Internal Direct Rider evaluation requires its delivery-resistance score."),
                    deliveryImmune),
                DclDelivery.Touch => TouchLandingProbability(
                    carrierRoll,
                    baseSpellScore,
                    touchDefense!.Value),
                _ => throw new InvalidOperationException(
                    "Direct status-Rider evaluation requires ExternalProjectile, InternalDirect, Beneficial, or Touch delivery."),
            };
            if (landedProbability == Zero) continue;
            foreach (DclMagicStatusRiderForecast rider in riders)
            {
                if (rider.Immune) continue;
                result[rider.EffectIndex] += carrierRollProbability * landedProbability *
                    RiderConditionalApplicationProbability(
                        targetSpellScore: profile.DeliveryProfile.Delivery == DclDelivery.Touch
                            ? baseSpellScore
                            : targetSpellScore,
                        carrierRoll,
                        rider);
            }
        }
        return result;
    }

    private static DclRational DirectExternalLandingProbability(
        int casterRoll,
        int baseSpellScore,
        int targetSpellScore,
        DclDefenseOption defense)
    {
        DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
            casterRoll,
            baseSpellScore,
            targetSpellScore);
        if (!gate.TargetSucceeded) return Zero;
        if (gate.TargetCritical || defense.Kind == DclDefenseKind.None) return One;
        return One - new DclRational(DclSuccessRoll.SuccessOutcomeCount(defense.Target), 216);
    }

    private static DclRational DirectInternalLandingProbability(
        int casterRoll,
        int baseSpellScore,
        int targetSpellScore,
        int resistanceScore,
        bool immune)
    {
        DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
            casterRoll,
            baseSpellScore,
            targetSpellScore);
        if (!gate.TargetSucceeded || immune) return Zero;
        int wins = 0;
        for (int resistanceRoll = DclSuccessRoll.MinRoll; resistanceRoll <= DclSuccessRoll.MaxRoll; resistanceRoll++)
        {
            if (DclQuickContest.Resolve(
                    targetSpellScore,
                    casterRoll,
                    resistanceScore,
                    resistanceRoll).ActingSideWon)
                wins += DclSuccessRoll.OutcomeMultiplicity(resistanceRoll);
        }
        return new DclRational(wins, 216);
    }

    private static DclRational TouchLandingProbability(
        int attackRoll,
        int spellScore,
        DclDefenseCandidate defense)
    {
        if (DclPhysicalContest.IsCritical(attackRoll, spellScore)) return One;
        if (DclPhysicalContest.IsFumble(attackRoll, spellScore) ||
            !DclSuccessRoll.Succeeds(attackRoll, spellScore)) return Zero;
        return defense.Kind == DclDefenseKind.None
            ? One
            : One - new DclRational(DclSuccessRoll.SuccessOutcomeCount(defense.Score), 216);
    }

    private static DclRational RiderConditionalApplicationProbability(
        int targetSpellScore,
        int carrierRoll,
        DclMagicStatusRiderForecast rider)
        => rider.ResistanceGate switch
        {
            DclStateResistanceGate.None => One,
            DclStateResistanceGate.SuccessRoll => new DclRational(
                216 - DclSuccessRoll.SuccessOutcomeCount(rider.ResistanceScore),
                216),
            DclStateResistanceGate.QuickContest => DirectRiderQuickContestProbability(
                targetSpellScore,
                carrierRoll,
                rider.ResistanceScore),
            _ => throw new InvalidOperationException("Direct Rider gate is not owned by the generic evaluator."),
        };

    private static DclRational DirectRiderQuickContestProbability(
        int carrierScore,
        int carrierRoll,
        int resistanceScore)
    {
        int wins = 0;
        for (int resistanceRoll = DclSuccessRoll.MinRoll; resistanceRoll <= DclSuccessRoll.MaxRoll; resistanceRoll++)
        {
            if (DclQuickContest.Resolve(
                    carrierScore,
                    carrierRoll,
                    resistanceScore,
                    resistanceRoll).ActingSideWon)
                wins += DclSuccessRoll.OutcomeMultiplicity(resistanceRoll);
        }
        return new DclRational(wins, 216);
    }

    internal static DclCanonicalResourceEvaluation EvaluateResources(
        IReadOnlyDictionary<DclCastingOutcome, DclRational> baseOutcomes,
        int currentMp,
        int currentHp,
        DclCostCommitment commitment)
    {
        var payments = new Dictionary<DclCastingOutcome, DclResourcePayment>();
        DclRational expectedMp = Zero;
        DclRational expectedHp = Zero;
        DclRational payerKo = Zero;
        foreach ((DclCastingOutcome outcome, DclRational probability) in baseOutcomes)
        {
            DclResourcePayment payment = DclMagicResources.Settle(currentMp, currentHp, commitment, outcome);
            if (!payment.Legal)
                throw new InvalidOperationException("A payable evaluation outcome exceeded its declaration commitment.");
            payments.Add(outcome, payment);
            expectedMp += probability * DclRational.FromInteger(payment.MpPaid);
            expectedHp += probability * DclRational.FromInteger(payment.HpPaid);
            if (payment.PayerKo) payerKo += probability;
        }
        return new DclCanonicalResourceEvaluation(baseOutcomes, payments, expectedMp, expectedHp, payerKo);
    }

    private static DclTargetCandidate ResolveTarget(
        DclCanonicalMagicEvaluationInput input,
        DclReflectRoute route)
    {
        if (route.Reflected)
        {
            if (input.ReflectedTarget is null || input.ReflectedTarget.Unit != route.FinalTarget)
                throw new ArgumentException("Reflected evaluation requires the current original-caster target snapshot.", nameof(input));
            return input.ReflectedTarget;
        }
        if (input.ReflectedTarget is not null)
            throw new ArgumentException("Unreflected evaluation cannot own a reflected-target snapshot.", nameof(input));
        return input.Target;
    }

    private static DclCanonicalMagicEvaluationResult Empty(
        DclAbilityBinding binding,
        DclCastDeclarationAttempt attempt,
        DclReflectRoute route,
        IReadOnlyList<string> failures,
        bool ResourceFailed)
    {
        DclExactValueForecast zero = DclExactValueForecast.Mixture(One, []);
        var resources = new DclCanonicalResourceEvaluation(
            new Dictionary<DclCastingOutcome, DclRational>(),
            new Dictionary<DclCastingOutcome, DclResourcePayment>(),
            Zero,
            Zero,
            Zero);
        return new DclCanonicalMagicEvaluationResult(
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.ForecastBoundary,
            binding.AiBoundary,
            Legal: false,
            failures,
            attempt.CostCommitment,
            attempt.Timing,
            route,
            new Dictionary<DclMagicDeliveryOutcome, DclRational>(),
            DefenseAttemptProbability: Zero,
            BlockSpendProbability: Zero,
            ParrySpendProbability: Zero,
            NormalMagnitudeExpression: null,
            CriticalMagnitudeExpression: null,
            zero,
            zero,
            resources,
            ResourceFailed,
            new Dictionary<int, DclRational>(),
            ExpectedMovedTiles: Zero,
            FallProbability: Zero,
            ResourceChange: null);
    }

    private static DclExactIntegerDistribution ZeroDistribution()
        => new(new Dictionary<int, BigInteger> { [0] = BigInteger.One });
}
