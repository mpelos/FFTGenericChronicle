namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclCriticalAdjustments(
    bool IsCritical,
    int FinalMove,
    int FinalDodge);

internal static class DclCriticalState
{
    public static bool IsCritical(int currentHp, int maxHp)
    {
        if (currentHp < 0 || maxHp < 1) throw new ArgumentOutOfRangeException(nameof(currentHp));
        return currentHp > 0 && checked(3L * currentHp) < maxHp;
    }

    public static DclCriticalAdjustments Apply(int currentHp, int maxHp, int moveBeforeCritical, int dodgeBeforeCritical)
    {
        bool critical = IsCritical(currentHp, maxHp);
        if (!critical) return new DclCriticalAdjustments(false, moveBeforeCritical, dodgeBeforeCritical);
        int move = Math.Max(1, checked((int)new DclRational(moveBeforeCritical, 2).Ceiling()));
        int dodge = checked((int)new DclRational(dodgeBeforeCritical, 2).Ceiling());
        return new DclCriticalAdjustments(true, move, dodge);
    }
}

internal readonly record struct DclKnockedDownAdjustments(
    int DodgeModifier,
    int ParryModifier,
    bool BlockAvailable,
    int MeleeAttackModifier,
    int CrawlMove,
    int EnemyMeleeAttackModifier,
    int EnemyRangedAttackModifier)
{
    public static DclKnockedDownAdjustments Canonical => new(
        DodgeModifier: -3,
        ParryModifier: -2,
        BlockAvailable: false,
        MeleeAttackModifier: -4,
        CrawlMove: 1,
        EnemyMeleeAttackModifier: 2,
        EnemyRangedAttackModifier: -2);
}

internal readonly record struct DclStunRules(
    bool MovementAvailable,
    bool ActionAvailable,
    int DodgeModifier,
    int ParryModifier,
    int BlockModifier)
{
    public static DclStunRules Canonical => new(
        MovementAvailable: true,
        ActionAvailable: false,
        DodgeModifier: -4,
        ParryModifier: -4,
        BlockModifier: -4);
}

internal readonly record struct DclStunRecoveryResult(bool Rolled, bool RemoveStun);

internal static class DclStunRecovery
{
    public static DclStunRecoveryResult Resolve(bool turnBeganStunned, int recoveryScore, int? roll)
    {
        if (!turnBeganStunned)
        {
            if (roll is not null) throw new ArgumentException("Stun applied during this turn cannot consume a recovery roll.", nameof(roll));
            return new DclStunRecoveryResult(Rolled: false, RemoveStun: false);
        }
        if (roll is null) throw new ArgumentNullException(nameof(roll));
        return new DclStunRecoveryResult(
            Rolled: true,
            RemoveStun: DclSuccessRoll.Succeeds(roll.Value, recoveryScore));
    }
}

internal sealed record DclShockStatePayload(
    string SchemaId,
    int UnexpiredInjury) : DclStatePayload(SchemaId)
{
    public DclShockStatePayload AddInjury(int injury)
    {
        if (injury < 0) throw new ArgumentOutOfRangeException(nameof(injury));
        return this with { UnexpiredInjury = checked(UnexpiredInjury + injury) };
    }

    public int PenaltyMagnitude(int maxHp)
    {
        if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp));
        int shockUnit = Math.Max(1, maxHp / 10);
        return Math.Min(3, UnexpiredInjury / shockUnit);
    }

    public int SkillModifier(int maxHp) => -PenaltyMagnitude(maxHp);
}

internal sealed record DclTauntStatePayload(
    string SchemaId,
    DclUnitKey Provocateur) : DclStatePayload(SchemaId);

internal sealed record DclFearStatePayload(
    string SchemaId,
    DclUnitKey FearSource,
    int WinningMargin) : DclStatePayload(SchemaId);

internal sealed record DclGuardBrokenStatePayload(
    string SchemaId,
    bool SuppressBlock,
    int ParryPenalty) : DclStatePayload(SchemaId)
{
    public int StrengthKey
        => checked((SuppressBlock ? 1_000_000 : 0) + Math.Abs(ParryPenalty));
}

internal sealed record DclWeaponBoundStatePayload(
    string SchemaId,
    string EquipmentSlot,
    bool SuppressAttack,
    bool SuppressParry,
    bool SuppressWeaponReactions,
    int WeaponSkillPenalty) : DclStatePayload(SchemaId);

internal sealed record DclElementalExposureStatePayload(
    string SchemaId,
    string Element,
    int AffinityStep) : DclStatePayload(SchemaId);

internal static class DclStatusRules
{
    public static int BraveTemperamentModifier(int brave)
        => checked((int)new DclRational(checked(brave - 50), 20).RoundNearest());

    public static int TauntResistance(int will, int brave)
        => checked(will - BraveTemperamentModifier(brave));

    public static int FearResistance(int will, int brave)
        => checked(will + BraveTemperamentModifier(brave));

    public static int DurationFromWinningMargin(
        int baseDuration,
        int durationBand,
        int minimumDuration,
        int maximumDuration,
        int winningMargin)
    {
        if (durationBand <= 0) throw new ArgumentOutOfRangeException(nameof(durationBand));
        if (minimumDuration < 1 || minimumDuration > baseDuration || baseDuration > maximumDuration)
            throw new ArgumentException("Duration bounds must satisfy 1 <= minimum <= base <= maximum.");
        if (winningMargin <= 0) throw new ArgumentOutOfRangeException(nameof(winningMargin));
        int duration = checked(baseDuration + winningMargin / durationBand);
        return Math.Clamp(duration, minimumDuration, maximumDuration);
    }

    public static int EffectStrength(int selectedBasisValue, int effectStrengthModifier)
        => Math.Max(1, checked(selectedBasisValue + effectStrengthModifier));

    public static DclQuickContestResult ResolveHostileStatus(
        int sourceScore,
        int sourceRoll,
        int resistanceScore,
        int resistanceRoll)
        => DclQuickContest.Resolve(sourceScore, sourceRoll, resistanceScore, resistanceRoll);

    public static DclQuickContestResult ResolveDispel(
        int dispelScore,
        int sharedDispelRoll,
        int storedEffectStrength,
        int effectResistanceRoll)
    {
        if (storedEffectStrength < 1) throw new ArgumentOutOfRangeException(nameof(storedEffectStrength));
        return DclQuickContest.Resolve(dispelScore, sharedDispelRoll, storedEffectStrength, effectResistanceRoll);
    }
}
