namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclMagicStrikeGateResult(
    int StrikeIndex,
    DclMagicDeliveryOutcome Outcome,
    bool Landed,
    bool TargetGateRolled);

internal sealed record DclMagicTargetGateResult(
    DclUnitKey Target,
    DclSpellGateResult SharedCastingGate,
    IReadOnlyList<DclMagicStrikeGateResult> Strikes,
    int TargetGateRollCount)
{
    public bool AnyStrikeLanded => Strikes.Any(strike => strike.Landed);
}

internal readonly record struct DclMagicRiderResult(
    bool Attempted,
    bool Applied,
    int CasterMargin,
    int TargetMargin,
    string Reason);

internal static class DclMagicActionGates
{
    public static DclMagicTargetGateResult ResolveAreaTarget(
        DclUnitKey target,
        int sharedCasterRoll,
        int baseSpellScore,
        int targetSpellScore,
        DclAreaDeliveryGate deliveryGate,
        int strikeCount,
        DclDefenseOption? dodge,
        IReadOnlyList<int>? dodgeRolls,
        int? resistanceScore,
        int? resistanceRoll)
    {
        if (!target.IsValid) throw new ArgumentException("A stable target identity is required.", nameof(target));
        if (strikeCount <= 0) throw new ArgumentOutOfRangeException(nameof(strikeCount));
        DclSpellGateResult castingGate = DclSpellResolution.ClassifySharedRoll(
            sharedCasterRoll,
            baseSpellScore,
            targetSpellScore);

        if (!castingGate.BaseSucceeded || !castingGate.TargetSucceeded)
        {
            if (dodgeRolls is { Count: > 0 } || resistanceRoll is not null)
                throw new ArgumentException("A failed casting gate must not consume target-gate rolls.");
            return new DclMagicTargetGateResult(
                target,
                castingGate,
                Enumerable.Range(0, strikeCount)
                    .Select(index => new DclMagicStrikeGateResult(
                        index,
                        castingGate.BaseSucceeded
                            ? DclMagicDeliveryOutcome.TargetFailure
                            : DclMagicDeliveryOutcome.BaseFailure,
                        Landed: false,
                        TargetGateRolled: false))
                    .ToArray(),
                0);
        }

        return deliveryGate switch
        {
            DclAreaDeliveryGate.None => NoAvoidance(target, castingGate, strikeCount),
            DclAreaDeliveryGate.Dodge => DodgePerStrike(
                target,
                castingGate,
                sharedCasterRoll,
                baseSpellScore,
                targetSpellScore,
                strikeCount,
                dodge,
                dodgeRolls,
                resistanceRoll),
            DclAreaDeliveryGate.QuickContest => QuickContestPerTarget(
                target,
                castingGate,
                sharedCasterRoll,
                baseSpellScore,
                targetSpellScore,
                strikeCount,
                resistanceScore,
                resistanceRoll,
                dodgeRolls),
            _ => throw new ArgumentOutOfRangeException(nameof(deliveryGate), "Area delivery requires None, Dodge, or QuickContest."),
        };
    }

    public static DclMagicRiderResult ResolveStatusRider(
        DclSpellGateResult carrierCastingGate,
        bool carrierLanded,
        int resistanceScore,
        int? resistanceRoll,
        bool immune,
        DclStateResistanceGate resistanceGate = DclStateResistanceGate.QuickContest)
    {
        if (!carrierLanded || !carrierCastingGate.BaseSucceeded || !carrierCastingGate.TargetSucceeded)
        {
            if (resistanceRoll is not null)
                throw new ArgumentException("A rider whose carrier did not land must not consume a resistance roll.", nameof(resistanceRoll));
            return new DclMagicRiderResult(false, false, 0, 0, "carrier-did-not-land");
        }
        return ResolveRider(
            carrierCastingGate.TargetSpellScore,
            carrierCastingGate.SharedRoll,
            carrierLanded: true,
            resistanceScore,
            resistanceRoll,
            immune,
            resistanceGate);
    }

    public static DclMagicRiderResult ResolveRider(
        int carrierScore,
        int carrierRoll,
        bool carrierLanded,
        int resistanceScore,
        int? resistanceRoll,
        bool immune,
        DclStateResistanceGate resistanceGate = DclStateResistanceGate.QuickContest)
    {
        if (!carrierLanded)
        {
            if (resistanceRoll is not null)
                throw new ArgumentException("A rider whose carrier did not land must not consume a resistance roll.", nameof(resistanceRoll));
            return new DclMagicRiderResult(false, false, 0, 0, "carrier-did-not-land");
        }
        if (!DclSuccessRoll.Succeeds(carrierRoll, carrierScore))
            throw new ArgumentException("A landed rider carrier requires a successful carrier roll.", nameof(carrierRoll));
        if (immune)
        {
            if (resistanceRoll is not null)
                throw new ArgumentException("Immunity is checked before the rider resistance roll.", nameof(resistanceRoll));
            return new DclMagicRiderResult(false, false, 0, 0, "immune");
        }
        switch (resistanceGate)
        {
            case DclStateResistanceGate.None:
                if (resistanceRoll is not null)
                    throw new ArgumentException("A Rider with ResistanceGate None consumes no resistance roll.", nameof(resistanceRoll));
                return new DclMagicRiderResult(false, true, 0, 0, "rider-applied-no-resistance");
            case DclStateResistanceGate.SuccessRoll:
            {
                if (resistanceRoll is null)
                    throw new ArgumentException("A landed nonimmune SuccessRoll Rider requires one target resistance roll.", nameof(resistanceRoll));
                bool resisted = DclSuccessRoll.Succeeds(resistanceRoll.Value, resistanceScore);
                return new DclMagicRiderResult(
                    true,
                    Applied: !resisted,
                    CasterMargin: 0,
                    TargetMargin: checked(resistanceScore - resistanceRoll.Value),
                    Reason: resisted ? "target-resisted-success-roll" : "rider-applied-failed-resistance");
            }
            case DclStateResistanceGate.QuickContest:
            {
                if (resistanceRoll is null)
                    throw new ArgumentException("A landed nonimmune QuickContest Rider requires one target resistance roll.", nameof(resistanceRoll));
                DclQuickContestResult contest = DclQuickContest.Resolve(
                    carrierScore,
                    carrierRoll,
                    resistanceScore,
                    resistanceRoll.Value);
                return new DclMagicRiderResult(
                    true,
                    contest.ActingSideWon,
                    contest.ActingMargin,
                    contest.TargetMargin,
                    contest.ActingSideWon ? "rider-applied" : "target-resisted-tie-or-better");
            }
            case DclStateResistanceGate.Explicit:
                throw new InvalidOperationException("An Explicit Rider resistance gate requires its named mechanism owner.");
            case DclStateResistanceGate.Unknown:
            default:
                throw new ArgumentOutOfRangeException(nameof(resistanceGate), "Rider resistance gate must be explicit.");
        }
    }

    private static DclMagicTargetGateResult NoAvoidance(
        DclUnitKey target,
        DclSpellGateResult castingGate,
        int strikeCount)
        => new(
            target,
            castingGate,
            Enumerable.Range(0, strikeCount)
                .Select(index => new DclMagicStrikeGateResult(
                    index,
                    castingGate.TargetCritical
                        ? DclMagicDeliveryOutcome.CriticalDelivered
                        : DclMagicDeliveryOutcome.Delivered,
                    Landed: true,
                    TargetGateRolled: false))
                .ToArray(),
            0);

    private static DclMagicTargetGateResult DodgePerStrike(
        DclUnitKey target,
        DclSpellGateResult castingGate,
        int sharedCasterRoll,
        int baseSpellScore,
        int targetSpellScore,
        int strikeCount,
        DclDefenseOption? dodge,
        IReadOnlyList<int>? dodgeRolls,
        int? resistanceRoll)
    {
        if (resistanceRoll is not null)
            throw new ArgumentException("Dodge delivery must not consume a Quick Contest roll.", nameof(resistanceRoll));
        if (dodge is null || dodge.Value.Kind != DclDefenseKind.Dodge)
            throw new ArgumentException("Dodge delivery requires one current Dodge option.", nameof(dodge));
        bool critical = castingGate.TargetCritical;
        int requiredRolls = critical ? 0 : strikeCount;
        if ((dodgeRolls?.Count ?? 0) != requiredRolls)
            throw new ArgumentException("Ordinary Dodge delivery requires exactly one defense roll per Strike; a critical requires none.", nameof(dodgeRolls));

        var strikes = new DclMagicStrikeGateResult[strikeCount];
        for (int index = 0; index < strikeCount; index++)
        {
            DclMagicDeliveryResult delivery = DclSpellResolution.ResolveExternal(
                sharedCasterRoll,
                baseSpellScore,
                targetSpellScore,
                dodge.Value,
                critical ? null : dodgeRolls![index]);
            strikes[index] = new DclMagicStrikeGateResult(
                index,
                delivery.Outcome,
                delivery.Delivered,
                TargetGateRolled: !critical);
        }
        return new DclMagicTargetGateResult(target, castingGate, strikes, requiredRolls);
    }

    private static DclMagicTargetGateResult QuickContestPerTarget(
        DclUnitKey target,
        DclSpellGateResult castingGate,
        int sharedCasterRoll,
        int baseSpellScore,
        int targetSpellScore,
        int strikeCount,
        int? resistanceScore,
        int? resistanceRoll,
        IReadOnlyList<int>? dodgeRolls)
    {
        if (dodgeRolls is { Count: > 0 })
            throw new ArgumentException("Quick Contest delivery must not consume Dodge rolls.", nameof(dodgeRolls));
        if (resistanceScore is null || resistanceRoll is null)
            throw new ArgumentException("Quick Contest delivery requires exactly one target score and roll.");
        DclMagicDeliveryResult delivery = DclSpellResolution.ResolveInternal(
            sharedCasterRoll,
            baseSpellScore,
            targetSpellScore,
            resistanceScore.Value,
            resistanceRoll.Value);
        DclMagicStrikeGateResult[] strikes = Enumerable.Range(0, strikeCount)
            .Select(index => new DclMagicStrikeGateResult(
                index,
                delivery.Outcome,
                delivery.Delivered,
                TargetGateRolled: index == 0))
            .ToArray();
        return new DclMagicTargetGateResult(target, castingGate, strikes, TargetGateRollCount: 1);
    }
}
