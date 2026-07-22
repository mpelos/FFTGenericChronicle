namespace fftivc.generic.chronicle.codemod;

internal enum DclDamageType
{
    Unknown,
    Crushing,
    Cutting,
    Impaling,
    SmallPiercing,
    Piercing,
    LargePiercing,
    HugePiercing,
}

internal readonly record struct DclInjuryResult(
    int RolledDamage,
    int ApplicableDr,
    int EffectiveDr,
    int PenetratingDamage,
    int Injury);

internal readonly record struct DclMajorWoundResult(
    int RemainingHp,
    bool RequiresHtRoll,
    bool AvoidedCollapse);

internal readonly record struct DclInjuryConsequenceInput(
    int MaxHp,
    int TargetSt,
    int EffectiveHt,
    int UnexpiredShockInjury,
    bool StillCharging,
    int Will,
    int ConcentrationModifier,
    int ConcentrationStatePenaltyMagnitude,
    int? MajorWoundHtRoll,
    bool DirectConcentrationCancellation,
    int AuthoredForcedDisplacement,
    int? ConcentrationRoll,
    int? SettledForcedDisplacement = null);

internal readonly record struct DclCanonicalInjuryTargetContext(
    int MaxHp,
    int TargetSt,
    int EffectiveHt,
    int UnexpiredShockInjury,
    bool Charging,
    int Will,
    int ConcentrationModifier,
    int ConcentrationStatePenaltyMagnitude);

internal readonly record struct DclCanonicalInjuryConsequenceRolls(
    int? MajorWoundHtRoll,
    bool DirectConcentrationCancellation,
    int AuthoredForcedDisplacement,
    int? ConcentrationRoll,
    int? SettledForcedDisplacement = null);

internal readonly record struct DclInjuryConsequenceResult(
    DclMajorWoundResult MajorWound,
    int UnexpiredShockInjury,
    int ShockPenaltyMagnitude,
    bool ApplyStun,
    bool ApplyKnockedDown,
    int CriticalKnockbackTiles,
    int TotalForcedDisplacement,
    DclConcentrationResult Concentration,
    int RequestedForcedDisplacement = 0);

internal static class DclInjury
{
    private static readonly DclRational One = DclRational.FromInteger(1);
    private static readonly DclRational Zero = DclRational.FromInteger(0);

    public static int RollDamage(DclDiceExpression expression, IReadOnlyList<int> dice)
    {
        ArgumentNullException.ThrowIfNull(dice);
        if (dice.Count != expression.Dice)
            throw new ArgumentException("The damage random site must provide exactly one result per authored d6.", nameof(dice));
        int total = expression.Adds;
        foreach (int die in dice)
        {
            if (die is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(dice), "Damage dice must be within 1..6.");
            total = checked(total + die);
        }
        return total;
    }

    public static DclRational WoundMultiplier(DclDamageType damageType)
        => damageType switch
        {
            DclDamageType.Crushing => One,
            DclDamageType.Cutting => new DclRational(3, 2),
            DclDamageType.Impaling => DclRational.FromInteger(2),
            DclDamageType.SmallPiercing => new DclRational(1, 2),
            DclDamageType.Piercing => One,
            DclDamageType.LargePiercing => new DclRational(3, 2),
            DclDamageType.HugePiercing => DclRational.FromInteger(2),
            _ => throw new ArgumentOutOfRangeException(nameof(damageType)),
        };

    public static DclInjuryResult Resolve(
        int rawRolledDamage,
        DclDamageType damageType,
        int applicableDr,
        DclRational armorDivisor,
        bool ignoreDr = false)
    {
        if (applicableDr < 0) throw new ArgumentOutOfRangeException(nameof(applicableDr));
        if (armorDivisor <= Zero) throw new ArgumentOutOfRangeException(nameof(armorDivisor));
        int minimumBasicDamage = damageType == DclDamageType.Crushing ? 0 : 1;
        int rolledDamage = Math.Max(minimumBasicDamage, rawRolledDamage);
        int divisorDr = armorDivisor < One && applicableDr == 0 ? 1 : applicableDr;
        int effectiveDr = ignoreDr
            ? 0
            : checked((int)(DclRational.FromInteger(divisorDr) / armorDivisor).Floor());
        effectiveDr = Math.Max(0, effectiveDr);
        int penetratingDamage = Math.Max(0, checked(rolledDamage - effectiveDr));
        int injury = penetratingDamage == 0
            ? 0
            : Math.Max(1, checked((int)(DclRational.FromInteger(penetratingDamage) * WoundMultiplier(damageType)).Floor()));
        return new DclInjuryResult(rolledDamage, applicableDr, effectiveDr, penetratingDamage, injury);
    }

    public static DclMajorWoundResult ResolveMajorWound(
        int hpBeforeInjury,
        int maxHp,
        int injury,
        int effectiveHt,
        int? htRoll)
    {
        if (hpBeforeInjury < 0 || maxHp < 1 || injury < 0)
            throw new ArgumentOutOfRangeException(nameof(hpBeforeInjury));
        int remainingHp = Math.Max(0, checked(hpBeforeInjury - injury));
        bool requiresRoll = remainingHp > 0 && DclRational.FromInteger(injury) > new DclRational(maxHp, 2);
        if (!requiresRoll)
        {
            if (htRoll is not null) throw new ArgumentException("A non-major wound cannot consume an HT roll.", nameof(htRoll));
            return new DclMajorWoundResult(remainingHp, RequiresHtRoll: false, AvoidedCollapse: true);
        }
        if (htRoll is null) throw new ArgumentNullException(nameof(htRoll), "A surviving Major Wound requires exactly one HT roll.");
        return new DclMajorWoundResult(
            remainingHp,
            RequiresHtRoll: true,
            AvoidedCollapse: DclSuccessRoll.Succeeds(htRoll.Value, effectiveHt));
    }

    public static int CriticalKnockbackTiles(
        bool criticalSuccess,
        bool targetSurvived,
        DclDamageType damageType,
        int rolledDamage,
        int penetratingDamage,
        int targetSt)
    {
        if (rolledDamage < 0 || penetratingDamage < 0 || targetSt < 1)
            throw new ArgumentOutOfRangeException(nameof(rolledDamage));
        if (!criticalSuccess || !targetSurvived) return 0;
        bool qualifyingType = damageType == DclDamageType.Crushing ||
            (damageType == DclDamageType.Cutting && penetratingDamage == 0);
        if (!qualifyingType) return 0;
        int knockbackUnit = targetSt <= 3 ? 1 : targetSt - 2;
        int gurpsTiles = rolledDamage / knockbackUnit;
        return Math.Min(1, gurpsTiles);
    }

    public static DclInjuryConsequenceResult ResolveConsequences(
        int hpBeforeInjury,
        DclInjuryResult injury,
        DclDamageType damageType,
        bool criticalSuccess,
        DclInjuryConsequenceInput input)
    {
        if (hpBeforeInjury < 0 || input.MaxHp < 1 || input.TargetSt < 1 ||
            input.UnexpiredShockInjury < 0 || input.ConcentrationStatePenaltyMagnitude < 0 ||
            input.AuthoredForcedDisplacement < 0)
            throw new ArgumentOutOfRangeException(nameof(input));
        DclMajorWoundResult majorWound = ResolveMajorWound(
            hpBeforeInjury,
            input.MaxHp,
            injury.Injury,
            input.EffectiveHt,
            input.MajorWoundHtRoll);
        int shockInjury = injury.Injury == 0
            ? input.UnexpiredShockInjury
            : checked(input.UnexpiredShockInjury + injury.Injury);
        int shockPenalty = new DclShockStatePayload("dcl.shock", shockInjury)
            .PenaltyMagnitude(input.MaxHp);
        bool collapsed = majorWound.RequiresHtRoll && !majorWound.AvoidedCollapse;
        int criticalKnockback = CriticalKnockbackTiles(
            criticalSuccess,
            majorWound.RemainingHp > 0,
            damageType,
            injury.RolledDamage,
            injury.PenetratingDamage,
            input.TargetSt);
        int requestedForcedDisplacement = checked(input.AuthoredForcedDisplacement + criticalKnockback);
        int forcedDisplacement = input.SettledForcedDisplacement ?? requestedForcedDisplacement;
        if (forcedDisplacement < 0 || forcedDisplacement > requestedForcedDisplacement)
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "Settled Injury displacement must be within the Strike's requested distance.");
        bool directCancellation = input.StillCharging &&
            (input.DirectConcentrationCancellation || collapsed || majorWound.RemainingHp == 0);
        DclConcentrationResult concentration = DclConcentration.ResolveStrikeIncident(
            input.StillCharging,
            directCancellation,
            injury.Injury,
            forcedDisplacement,
            input.Will,
            input.ConcentrationModifier,
            input.ConcentrationStatePenaltyMagnitude,
            input.ConcentrationRoll);
        return new DclInjuryConsequenceResult(
            majorWound,
            shockInjury,
            shockPenalty,
            ApplyStun: collapsed,
            ApplyKnockedDown: collapsed,
            criticalKnockback,
            forcedDisplacement,
            concentration,
            requestedForcedDisplacement);
    }
}
