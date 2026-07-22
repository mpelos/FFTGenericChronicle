namespace fftivc.generic.chronicle.codemod;

internal sealed record DclMagicForecastTarget(
    DclUnitKey Target,
    int TargetSpellScore,
    DclAreaDeliveryGate DeliveryGate,
    int? TargetGateScore,
    int StrikeCount = 1,
    IReadOnlyList<DclMagicStatusRiderForecast>? StatusRiders = null,
    bool DeliveryImmune = false);

internal sealed record DclMagicStatusRiderForecast(
    int EffectIndex,
    int ResistanceScore,
    bool Immune,
    DclStateResistanceGate ResistanceGate = DclStateResistanceGate.QuickContest);

internal sealed record DclMagicCorrelatedForecastResult(
    IReadOnlyDictionary<int, DclRational> DeliveredTargetCountProbability,
    IReadOnlyDictionary<DclUnitKey, DclRational> PerTargetDeliveryProbability,
    IReadOnlyDictionary<DclUnitKey, DclRational> PerTargetExpectedLandedStrikes,
    IReadOnlyDictionary<DclUnitKey, IReadOnlyDictionary<int, DclRational>> PerTargetRiderApplicationProbability)
{
    public DclRational AnyTargetDelivered
        => DeliveredTargetCountProbability
            .Where(pair => pair.Key > 0)
            .Aggregate(DclRational.FromInteger(0), (sum, pair) => sum + pair.Value);
}

internal static class DclMagicCorrelatedForecast
{
    private static readonly DclRational One = DclRational.FromInteger(1);
    private static readonly DclRational Zero = DclRational.FromInteger(0);

    public static DclMagicCorrelatedForecastResult Enumerate(
        int baseSpellScore,
        IReadOnlyList<DclMagicForecastTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Count == 0) throw new ArgumentException("Correlated magic forecast requires at least one affected target.", nameof(targets));
        if (targets.Any(target => !target.Target.IsValid) || targets.Select(target => target.Target).Distinct().Count() != targets.Count)
            throw new ArgumentException("Forecast targets require distinct stable UnitKeys.", nameof(targets));
        foreach (DclMagicForecastTarget target in targets)
            ValidateTarget(target);

        var countDistribution = Enumerable.Range(0, targets.Count + 1)
            .ToDictionary(count => count, _ => Zero);
        var perTarget = targets.ToDictionary(target => target.Target, _ => Zero);
        var expectedStrikes = targets.ToDictionary(target => target.Target, _ => Zero);
        var riderApplications = targets.ToDictionary(
            target => target.Target,
            target => (IReadOnlyDictionary<int, DclRational>)(target.StatusRiders ?? [])
                .ToDictionary(rider => rider.EffectIndex, _ => Zero));
        for (int casterRoll = DclSuccessRoll.MinRoll; casterRoll <= DclSuccessRoll.MaxRoll; casterRoll++)
        {
            DclRational casterWeight = new(DclSuccessRoll.OutcomeMultiplicity(casterRoll), 216);
            DclRational[] conditional = targets
                .Select(target => ConditionalDeliveryProbability(baseSpellScore, casterRoll, target))
                .ToArray();
            for (int index = 0; index < targets.Count; index++)
            {
                perTarget[targets[index].Target] += casterWeight * conditional[index];
                expectedStrikes[targets[index].Target] += casterWeight *
                    ConditionalExpectedLandedStrikes(baseSpellScore, casterRoll, targets[index]);
                if (conditional[index] == Zero) continue;
                Dictionary<int, DclRational> targetRiders =
                    (Dictionary<int, DclRational>)riderApplications[targets[index].Target];
                foreach (DclMagicStatusRiderForecast rider in targets[index].StatusRiders ?? [])
                {
                    if (rider.Immune) continue;
                    DclRational resistance = RiderApplicationProbability(
                        targets[index].TargetSpellScore,
                        casterRoll,
                        rider);
                    targetRiders[rider.EffectIndex] += casterWeight * conditional[index] * resistance;
                }
            }

            DclRational[] polynomial = Enumerable.Repeat(Zero, targets.Count + 1).ToArray();
            polynomial[0] = One;
            int processed = 0;
            foreach (DclRational delivered in conditional)
            {
                DclRational[] next = Enumerable.Repeat(Zero, targets.Count + 1).ToArray();
                for (int count = 0; count <= processed; count++)
                {
                    next[count] += polynomial[count] * (One - delivered);
                    next[count + 1] += polynomial[count] * delivered;
                }
                polynomial = next;
                processed++;
            }
            for (int count = 0; count <= targets.Count; count++)
                countDistribution[count] += casterWeight * polynomial[count];
        }
        return new DclMagicCorrelatedForecastResult(
            countDistribution,
            perTarget,
            expectedStrikes,
            riderApplications);
    }

    private static DclRational ConditionalDeliveryProbability(
        int baseSpellScore,
        int casterRoll,
        DclMagicForecastTarget target)
    {
        DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
            casterRoll,
            baseSpellScore,
            target.TargetSpellScore);
        if (!gate.BaseSucceeded || !gate.TargetSucceeded || target.DeliveryImmune) return Zero;
        return target.DeliveryGate switch
        {
            DclAreaDeliveryGate.None => One,
            DclAreaDeliveryGate.Dodge when gate.TargetCritical => One,
            DclAreaDeliveryGate.Dodge => One - Pow(
                new DclRational(DclSuccessRoll.SuccessOutcomeCount(target.TargetGateScore!.Value), 216),
                target.StrikeCount),
            DclAreaDeliveryGate.QuickContest => ConditionalContestProbability(
                target.TargetSpellScore,
                casterRoll,
                target.TargetGateScore!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };
    }

    private static DclRational ConditionalExpectedLandedStrikes(
        int baseSpellScore,
        int casterRoll,
        DclMagicForecastTarget target)
    {
        DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
            casterRoll,
            baseSpellScore,
            target.TargetSpellScore);
        if (!gate.BaseSucceeded || !gate.TargetSucceeded || target.DeliveryImmune) return Zero;
        DclRational strikeCount = DclRational.FromInteger(target.StrikeCount);
        return target.DeliveryGate switch
        {
            DclAreaDeliveryGate.None => strikeCount,
            DclAreaDeliveryGate.Dodge when gate.TargetCritical => strikeCount,
            DclAreaDeliveryGate.Dodge => strikeCount * (One - new DclRational(
                DclSuccessRoll.SuccessOutcomeCount(target.TargetGateScore!.Value), 216)),
            DclAreaDeliveryGate.QuickContest => strikeCount * ConditionalContestProbability(
                target.TargetSpellScore,
                casterRoll,
                target.TargetGateScore!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };
    }

    private static DclRational ConditionalContestProbability(
        int actingScore,
        int actingRoll,
        int targetScore)
    {
        int successes = 0;
        for (int targetRoll = DclSuccessRoll.MinRoll; targetRoll <= DclSuccessRoll.MaxRoll; targetRoll++)
        {
            if (DclQuickContest.Resolve(actingScore, actingRoll, targetScore, targetRoll).ActingSideWon)
                successes += DclSuccessRoll.OutcomeMultiplicity(targetRoll);
        }
        return new DclRational(successes, 216);
    }

    private static void ValidateTarget(DclMagicForecastTarget target)
    {
        if (target.StrikeCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(target), "Forecast StrikeCount must be positive.");
        DclMagicStatusRiderForecast[] riders = (target.StatusRiders ?? []).ToArray();
        if (riders.Any(rider => rider.EffectIndex <= 0) ||
            riders.Select(rider => rider.EffectIndex).Distinct().Count() != riders.Length)
            throw new ArgumentException("Forecast status Riders require distinct positive effect indexes.");
        if (riders.Any(rider => rider.ResistanceGate is DclStateResistanceGate.Unknown or DclStateResistanceGate.Explicit))
            throw new ArgumentException("Forecast status Riders require a generic None, SuccessRoll, or QuickContest gate.");
        switch (target.DeliveryGate)
        {
            case DclAreaDeliveryGate.None when target.TargetGateScore is not null:
                throw new ArgumentException("AreaDeliveryGate None must not own a target-gate score.");
            case DclAreaDeliveryGate.Dodge or DclAreaDeliveryGate.QuickContest when target.TargetGateScore is null:
                throw new ArgumentException("Dodge and QuickContest forecast require the current target gate score.");
            case DclAreaDeliveryGate.None or DclAreaDeliveryGate.Dodge or DclAreaDeliveryGate.QuickContest:
                return;
            default:
                throw new ArgumentException("Forecast delivery gate must be None, Dodge, or QuickContest.");
        }
    }

    private static DclRational RiderApplicationProbability(
        int carrierScore,
        int carrierRoll,
        DclMagicStatusRiderForecast rider)
        => rider.ResistanceGate switch
        {
            DclStateResistanceGate.None => One,
            DclStateResistanceGate.SuccessRoll => new DclRational(
                216 - DclSuccessRoll.SuccessOutcomeCount(rider.ResistanceScore),
                216),
            DclStateResistanceGate.QuickContest => ConditionalContestProbability(
                carrierScore,
                carrierRoll,
                rider.ResistanceScore),
            _ => throw new InvalidOperationException("Forecast Rider gate is not owned by the generic resolver."),
        };

    private static DclRational Pow(DclRational value, int exponent)
    {
        DclRational result = One;
        for (int index = 0; index < exponent; index++) result *= value;
        return result;
    }
}
