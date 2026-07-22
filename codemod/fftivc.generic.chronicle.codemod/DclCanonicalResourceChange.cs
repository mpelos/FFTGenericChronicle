using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalResourcePoolSnapshot(
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp,
    bool IsUndead);

internal sealed record DclCanonicalResourceChangeResult(
    DclCanonicalNativeNumericChannels TargetChannels,
    DclCanonicalNativeNumericChannels SourceChannels,
    int RolledMagnitude,
    int AppliedTargetMagnitude,
    int AppliedSourceMagnitude,
    bool TargetKo,
    bool SourceKo,
    bool RejectedByUndeadPolicy,
    bool MagnitudeRolled = true);

internal sealed record DclCanonicalResourceChangeEvaluation(
    IReadOnlyDictionary<DclCanonicalResourceChangeResult, DclRational> Outcomes)
{
    public DclRational ExpectedTargetHpDebit => Expected(result => result.TargetChannels.HpDebit);
    public DclRational ExpectedTargetHpCredit => Expected(result => result.TargetChannels.HpCredit);
    public DclRational ExpectedTargetMpDebit => Expected(result => result.TargetChannels.MpDebit);
    public DclRational ExpectedTargetMpCredit => Expected(result => result.TargetChannels.MpCredit);
    public DclRational ExpectedSourceHpDebit => Expected(result => result.SourceChannels.HpDebit);
    public DclRational ExpectedSourceHpCredit => Expected(result => result.SourceChannels.HpCredit);
    public DclRational ExpectedSourceMpDebit => Expected(result => result.SourceChannels.MpDebit);
    public DclRational ExpectedSourceMpCredit => Expected(result => result.SourceChannels.MpCredit);
    public DclRational TargetKoProbability => Probability(result => result.TargetKo);
    public DclRational SourceKoProbability => Probability(result => result.SourceKo);
    public DclRational RejectionProbability => Probability(result => result.RejectedByUndeadPolicy);

    public DclCanonicalResourceChangeEvaluation WithApplicationProbability(DclRational probability)
    {
        DclRational zero = DclRational.FromInteger(0);
        DclRational one = DclRational.FromInteger(1);
        if (probability < zero || probability > one)
            throw new ArgumentOutOfRangeException(nameof(probability));
        var mixed = new Dictionary<DclCanonicalResourceChangeResult, DclRational>();
        if (probability < one)
            mixed[NoDelivery()] = one - probability;
        foreach ((DclCanonicalResourceChangeResult outcome, DclRational outcomeProbability) in Outcomes)
            mixed[outcome] = mixed.GetValueOrDefault(outcome, zero) + probability * outcomeProbability;
        return new DclCanonicalResourceChangeEvaluation(mixed);
    }

    private DclRational Expected(Func<DclCanonicalResourceChangeResult, int> selector)
        => Outcomes.Aggregate(
            DclRational.FromInteger(0),
            (sum, pair) => sum + DclRational.FromInteger(selector(pair.Key)) * pair.Value);

    private DclRational Probability(Func<DclCanonicalResourceChangeResult, bool> predicate)
        => Outcomes.Where(pair => predicate(pair.Key)).Aggregate(
            DclRational.FromInteger(0),
            (sum, pair) => sum + pair.Value);

    private static DclCanonicalResourceChangeResult NoDelivery()
        => new(
            new DclCanonicalNativeNumericChannels(0, 0, 0, 0),
            new DclCanonicalNativeNumericChannels(0, 0, 0, 0),
            0,
            0,
            0,
            false,
            false,
            false,
            false);
}

internal sealed record DclCanonicalResourceChangeForecastProjection(
    DclResourceKind Resource,
    DclResourceChangeRoute Route,
    DclUndeadInteraction TargetUndeadInteraction,
    DclUndeadInteraction SourceUndeadInteraction,
    int MinimumRolledMagnitude,
    int MaximumRolledMagnitude,
    int TargetCurrent,
    int TargetMaximum,
    int SourceCurrent,
    int SourceMaximum,
    DclRational ExpectedTargetDebit,
    DclRational ExpectedTargetCredit,
    DclRational ExpectedSourceDebit,
    DclRational ExpectedSourceCredit,
    DclRational NoDeliveryProbability,
    DclRational TargetKoProbability,
    DclRational SourceKoProbability,
    DclRational RejectionProbability,
    DclRational ExpectedTargetExcessLostToCap,
    DclRational ExpectedSourceExcessLostToCap)
{
    public bool IsDrain => Route == DclResourceChangeRoute.DrainTargetToSource;
}

/// <summary>
/// Owns delivered HP/MP credit, debit, and drain magnitude. These are pool mutations rather than
/// Injury: they create no DR, wound multiplier, Shock, Major Wound, or damage Reaction. Delivery,
/// casting, payment, and the one outer Reaction window remain owned by the surrounding action.
/// </summary>
internal static class DclCanonicalResourceChange
{
    private enum TargetOperation { Credit, Debit }
    private enum SourceOperation { None, Credit, Debit }

    public static DclCanonicalResourceChangeResult Resolve(
        DclActionProfile profile,
        DclCanonicalResourcePoolSnapshot target,
        DclCanonicalResourcePoolSnapshot source,
        IReadOnlyList<int> magnitudeDice)
    {
        ArgumentNullException.ThrowIfNull(magnitudeDice);
        (DclFixedResourceMagnitude magnitude, _, _) = RequireProfile(profile);
        DclDiceExpression expression = ParseExpression(magnitude.Expression);
        int rolled = Math.Max(0, DclInjury.RollDamage(expression, magnitudeDice));
        return ResolveMagnitude(profile, target, source, rolled);
    }

    public static DclCanonicalResourceChangeResult ResolveMagnitude(
        DclActionProfile profile,
        DclCanonicalResourcePoolSnapshot target,
        DclCanonicalResourcePoolSnapshot source,
        int rolledMagnitude)
    {
        if (rolledMagnitude < 0) throw new ArgumentOutOfRangeException(nameof(rolledMagnitude));
        ValidateSnapshot(target, nameof(target));
        ValidateSnapshot(source, nameof(source));
        (DclFixedResourceMagnitude magnitude, DclEffectProfile effect, DclResourceChangeProfile change) =
            RequireProfile(profile);

        TargetOperation targetOperation = change.Route == DclResourceChangeRoute.TargetCredit
            ? TargetOperation.Credit
            : TargetOperation.Debit;
        bool drain = change.Route == DclResourceChangeRoute.DrainTargetToSource;
        if (target.IsUndead)
        {
            switch (effect.UndeadInteraction)
            {
                case DclUndeadInteraction.Normal:
                    break;
                case DclUndeadInteraction.Reverse:
                    targetOperation = targetOperation == TargetOperation.Credit
                        ? TargetOperation.Debit
                        : TargetOperation.Credit;
                    drain = false;
                    break;
                case DclUndeadInteraction.Harm:
                    targetOperation = TargetOperation.Debit;
                    break;
                case DclUndeadInteraction.Heal:
                    targetOperation = TargetOperation.Credit;
                    drain = false;
                    break;
                case DclUndeadInteraction.Reject:
                    return Rejected(rolledMagnitude);
                case DclUndeadInteraction.EffectOwned:
                    throw new InvalidOperationException("EffectOwned Undead routing requires a mechanism-specific ResourceChange executor.");
                default:
                    throw new InvalidOperationException("ResourceChange target Undead interaction is not explicit.");
            }
        }

        SourceOperation sourceOperation = SourceOperation.None;
        if (drain)
        {
            sourceOperation = SourceOperation.Credit;
            if (source.IsUndead)
            {
                sourceOperation = change.SourceUndeadInteraction switch
                {
                    DclUndeadInteraction.Normal or DclUndeadInteraction.Heal => SourceOperation.Credit,
                    DclUndeadInteraction.Reverse or DclUndeadInteraction.Harm => SourceOperation.Debit,
                    DclUndeadInteraction.Reject => SourceOperation.None,
                    DclUndeadInteraction.EffectOwned => throw new InvalidOperationException(
                        "EffectOwned source-Undead routing requires a mechanism-specific ResourceChange executor."),
                    _ => throw new InvalidOperationException("Drain source Undead interaction is not explicit."),
                };
                if (change.SourceUndeadInteraction == DclUndeadInteraction.Reject)
                    return Rejected(rolledMagnitude);
            }
        }

        (DclCanonicalNativeNumericChannels targetChannels, int appliedTarget, bool targetKo) =
            Apply(magnitude.Resource, targetOperation == TargetOperation.Credit, target, rolledMagnitude);
        int sourceMagnitude = drain && targetOperation == TargetOperation.Debit ? appliedTarget : 0;
        (DclCanonicalNativeNumericChannels sourceChannels, int appliedSource, bool sourceKo) =
            sourceOperation switch
            {
                SourceOperation.Credit => Apply(magnitude.Resource, credit: true, source, sourceMagnitude),
                SourceOperation.Debit => Apply(magnitude.Resource, credit: false, source, sourceMagnitude),
                _ => (EmptyChannels(), 0, false),
            };
        return new DclCanonicalResourceChangeResult(
            targetChannels,
            sourceChannels,
            rolledMagnitude,
            appliedTarget,
            appliedSource,
            targetKo,
            sourceKo,
            RejectedByUndeadPolicy: false,
            MagnitudeRolled: true);
    }

    public static DclCanonicalResourceChangeEvaluation Evaluate(
        DclActionProfile profile,
        DclCanonicalResourcePoolSnapshot target,
        DclCanonicalResourcePoolSnapshot source)
    {
        ValidateSnapshot(target, nameof(target));
        ValidateSnapshot(source, nameof(source));
        (DclFixedResourceMagnitude magnitude, _, _) = RequireProfile(profile);
        DclExactIntegerDistribution distribution = DclExactIntegerDistribution.Roll(ParseExpression(magnitude.Expression));
        var outcomes = new Dictionary<DclCanonicalResourceChangeResult, DclRational>();
        foreach ((int rolled, BigInteger weight) in distribution.Weights)
        {
            DclCanonicalResourceChangeResult result = ResolveMagnitude(profile, target, source, Math.Max(0, rolled));
            DclRational probability = new(weight, distribution.TotalOutcomes);
            outcomes[result] = outcomes.GetValueOrDefault(result, DclRational.FromInteger(0)) + probability;
        }
        DclRational total = outcomes.Values.Aggregate(
            DclRational.FromInteger(0),
            (sum, probability) => sum + probability);
        if (total != DclRational.FromInteger(1))
            throw new InvalidOperationException($"ResourceChange probability mass is {total}, expected one.");
        return new DclCanonicalResourceChangeEvaluation(outcomes);
    }

    public static DclCanonicalResourceChangeForecastProjection ProjectForecast(
        DclActionProfile profile,
        DclCanonicalResourcePoolSnapshot target,
        DclCanonicalResourcePoolSnapshot source,
        DclCanonicalResourceChangeEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ValidateSnapshot(target, nameof(target));
        ValidateSnapshot(source, nameof(source));
        (DclFixedResourceMagnitude magnitude, DclEffectProfile effect, DclResourceChangeProfile change) =
            RequireProfile(profile);
        DclDiceExpression expression = ParseExpression(magnitude.Expression);
        DclResourceKind resource = magnitude.Resource;
        int targetCurrent = resource == DclResourceKind.Hp ? target.CurrentHp : target.CurrentMp;
        int targetMaximum = resource == DclResourceKind.Hp ? target.MaxHp : target.MaxMp;
        int sourceCurrent = resource == DclResourceKind.Hp ? source.CurrentHp : source.CurrentMp;
        int sourceMaximum = resource == DclResourceKind.Hp ? source.MaxHp : source.MaxMp;
        DclRational zero = DclRational.FromInteger(0);
        DclRational noDelivery = zero;
        DclRational targetExcess = zero;
        DclRational sourceExcess = zero;
        foreach ((DclCanonicalResourceChangeResult outcome, DclRational probability) in evaluation.Outcomes)
        {
            if (!outcome.MagnitudeRolled)
            {
                noDelivery += probability;
                continue;
            }
            if (outcome.RejectedByUndeadPolicy) continue;
            targetExcess += DclRational.FromInteger(
                Math.Max(0, outcome.RolledMagnitude - outcome.AppliedTargetMagnitude)) * probability;
            if (change.Route == DclResourceChangeRoute.DrainTargetToSource)
            {
                sourceExcess += DclRational.FromInteger(
                    Math.Max(0, outcome.AppliedTargetMagnitude - outcome.AppliedSourceMagnitude)) * probability;
            }
        }

        return new DclCanonicalResourceChangeForecastProjection(
            resource,
            change.Route,
            effect.UndeadInteraction,
            change.SourceUndeadInteraction,
            Math.Max(0, expression.Dice + expression.Adds),
            Math.Max(0, checked(expression.Dice * 6 + expression.Adds)),
            targetCurrent,
            targetMaximum,
            sourceCurrent,
            sourceMaximum,
            ExpectedDebit(resource, target: true, evaluation),
            ExpectedCredit(resource, target: true, evaluation),
            ExpectedDebit(resource, target: false, evaluation),
            ExpectedCredit(resource, target: false, evaluation),
            noDelivery,
            evaluation.TargetKoProbability,
            evaluation.SourceKoProbability,
            evaluation.RejectionProbability,
            targetExcess,
            sourceExcess);
    }

    private static (DclFixedResourceMagnitude Magnitude, DclEffectProfile Effect, DclResourceChangeProfile Change)
        RequireProfile(DclActionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        DclEffectProfile[] effects = profile.Effects.Where(effect => effect.Kind == DclEffectKind.ResourceChange).ToArray();
        if (profile.MagnitudeProfile is not DclFixedResourceMagnitude
            {
                Resource: DclResourceKind.Hp or DclResourceKind.Mp,
            } magnitude || effects.Length != 1 || effects[0].Role != DclEffectRole.Carrier ||
            profile.ResourceChangeProfile is not { Route: not DclResourceChangeRoute.Unknown } change)
            throw new InvalidOperationException("ResourceChange requires one HP/MP magnitude, one Carrier, and one explicit route profile.");
        if (change.SourceUndeadInteraction == DclUndeadInteraction.Unknown)
            throw new InvalidOperationException("ResourceChange source Undead interaction must be explicit.");
        return (magnitude, effects[0], change);
    }

    private static DclDiceExpression ParseExpression(string authored)
        => DclDiceExpression.TryParseAuthored(authored, out DclDiceExpression expression)
            ? expression
            : throw new InvalidOperationException("ResourceChange magnitude does not use the exact Xd6+Y grammar.");

    private static void ValidateSnapshot(DclCanonicalResourcePoolSnapshot snapshot, string parameter)
    {
        ArgumentNullException.ThrowIfNull(snapshot, parameter);
        if (snapshot.MaxHp < 1 || snapshot.MaxMp < 0 || snapshot.CurrentHp < 0 ||
            snapshot.CurrentHp > snapshot.MaxHp || snapshot.CurrentMp < 0 || snapshot.CurrentMp > snapshot.MaxMp)
            throw new ArgumentOutOfRangeException(parameter, "Resource pools must lie inside their exact maxima.");
    }

    private static (DclCanonicalNativeNumericChannels Channels, int Applied, bool Ko) Apply(
        DclResourceKind resource,
        bool credit,
        DclCanonicalResourcePoolSnapshot pool,
        int magnitude)
    {
        int applied = resource switch
        {
            DclResourceKind.Hp when credit => Math.Min(magnitude, pool.MaxHp - pool.CurrentHp),
            DclResourceKind.Hp => Math.Min(magnitude, pool.CurrentHp),
            DclResourceKind.Mp when credit => Math.Min(magnitude, pool.MaxMp - pool.CurrentMp),
            DclResourceKind.Mp => Math.Min(magnitude, pool.CurrentMp),
            _ => throw new InvalidOperationException("Generic ResourceChange owns only HP or MP."),
        };
        DclCanonicalNativeNumericChannels channels = resource switch
        {
            DclResourceKind.Hp when credit => new(0, applied, 0, 0),
            DclResourceKind.Hp => new(applied, 0, 0, 0),
            DclResourceKind.Mp when credit => new(0, 0, 0, applied),
            DclResourceKind.Mp => new(0, 0, applied, 0),
            _ => throw new InvalidOperationException(),
        };
        bool ko = resource == DclResourceKind.Hp && !credit && pool.CurrentHp - applied == 0;
        return (channels, applied, ko);
    }

    private static DclRational ExpectedDebit(
        DclResourceKind resource,
        bool target,
        DclCanonicalResourceChangeEvaluation evaluation)
        => resource == DclResourceKind.Hp
            ? target ? evaluation.ExpectedTargetHpDebit : evaluation.ExpectedSourceHpDebit
            : target ? evaluation.ExpectedTargetMpDebit : evaluation.ExpectedSourceMpDebit;

    private static DclRational ExpectedCredit(
        DclResourceKind resource,
        bool target,
        DclCanonicalResourceChangeEvaluation evaluation)
        => resource == DclResourceKind.Hp
            ? target ? evaluation.ExpectedTargetHpCredit : evaluation.ExpectedSourceHpCredit
            : target ? evaluation.ExpectedTargetMpCredit : evaluation.ExpectedSourceMpCredit;

    private static DclCanonicalResourceChangeResult Rejected(int rolledMagnitude)
        => new(EmptyChannels(), EmptyChannels(), rolledMagnitude, 0, 0, false, false, true, true);

    private static DclCanonicalNativeNumericChannels EmptyChannels() => new(0, 0, 0, 0);
}
