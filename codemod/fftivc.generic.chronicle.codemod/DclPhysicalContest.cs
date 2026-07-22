namespace fftivc.generic.chronicle.codemod;

internal enum DclPhysicalOutcome
{
    Legacy,
    AttackMiss,
    AttackFumble,
    Defended,
    Hit,
    CriticalHit,
}

internal enum DclDefenseKind
{
    None,
    Dodge,
    Parry,
    Block,
}

internal readonly record struct DclDefenseOption(DclDefenseKind Kind, int Target, bool Depletes);

internal readonly record struct DclPhysicalContestResult(
    DclPhysicalOutcome Outcome,
    int AttackSkill,
    int AttackRoll,
    DclDefenseKind DefenseKind,
    int DefenseTarget,
    int DefenseRoll)
{
    public bool Hit => Outcome is DclPhysicalOutcome.Hit or DclPhysicalOutcome.CriticalHit;
    public bool SpendsDefense => DefenseKind is DclDefenseKind.Parry or DclDefenseKind.Block;
}

internal static class DclPhysicalContest
{
    public static int Roll3D6(Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return rng.Next(1, 7) + rng.Next(1, 7) + rng.Next(1, 7);
    }

    public static bool IsCritical(int roll, int skill)
        => DclSuccessRoll.IsCriticalSuccess(roll, skill);

    public static bool IsFumble(int roll, int skill)
        => DclSuccessRoll.IsCriticalFailure(roll, skill);

    public static DclDefenseOption ChooseBestDefense(
        int dodge,
        int parry,
        bool parryAvailable,
        int block,
        bool blockAvailable,
        int modifier,
        bool defenseAllowed)
    {
        if (!defenseAllowed)
            return new DclDefenseOption(DclDefenseKind.None, 0, false);

        // Ties deliberately prefer Dodge, then Parry, then Block. This preserves finite defense
        // resources when a reusable defense is equally effective.
        var best = new DclDefenseOption(DclDefenseKind.Dodge, dodge + modifier, false);
        if (parryAvailable && parry + modifier > best.Target)
            best = new DclDefenseOption(DclDefenseKind.Parry, parry + modifier, true);
        if (blockAvailable && block + modifier > best.Target)
            best = new DclDefenseOption(DclDefenseKind.Block, block + modifier, true);
        return best;
    }

    public static DclPhysicalContestResult Resolve(
        int attackSkill,
        int attackRoll,
        DclDefenseOption defense,
        int defenseRoll)
    {
        DclSuccessRoll.Validate(attackRoll);
        if (defense.Kind != DclDefenseKind.None)
            DclSuccessRoll.Validate(defenseRoll);

        if (IsCritical(attackRoll, attackSkill))
            return new DclPhysicalContestResult(DclPhysicalOutcome.CriticalHit, attackSkill, attackRoll,
                DclDefenseKind.None, 0, -1);
        if (IsFumble(attackRoll, attackSkill))
            return new DclPhysicalContestResult(DclPhysicalOutcome.AttackFumble, attackSkill, attackRoll,
                DclDefenseKind.None, 0, -1);
        if (!DclSuccessRoll.Succeeds(attackRoll, attackSkill))
            return new DclPhysicalContestResult(DclPhysicalOutcome.AttackMiss, attackSkill, attackRoll,
                DclDefenseKind.None, 0, -1);
        if (defense.Kind != DclDefenseKind.None && DclSuccessRoll.Succeeds(defenseRoll, defense.Target))
            return new DclPhysicalContestResult(DclPhysicalOutcome.Defended, attackSkill, attackRoll,
                defense.Kind, defense.Target, defenseRoll);

        return new DclPhysicalContestResult(DclPhysicalOutcome.Hit, attackSkill, attackRoll,
            defense.Kind, defense.Target, defense.Kind == DclDefenseKind.None ? -1 : defenseRoll);
    }

    public static DclExactProbability HitProbability(int attackSkill, DclDefenseOption defense)
    {
        int hits = 0;
        int defenseOutcomes = defense.Kind == DclDefenseKind.None ? 1 : 216;
        int defenseSuccesses = defense.Kind == DclDefenseKind.None
            ? 0
            : DclSuccessRoll.SuccessOutcomeCount(defense.Target);
        for (int attackRoll = DclSuccessRoll.MinRoll;
             attackRoll <= DclSuccessRoll.MaxRoll;
             attackRoll++)
        {
            int attackMultiplicity = DclSuccessRoll.OutcomeMultiplicity(attackRoll);
            if (IsCritical(attackRoll, attackSkill))
            {
                hits += attackMultiplicity * defenseOutcomes;
                continue;
            }

            if (!DclSuccessRoll.Succeeds(attackRoll, attackSkill))
                continue;

            if (defense.Kind == DclDefenseKind.None)
            {
                hits += attackMultiplicity;
                continue;
            }

            hits += attackMultiplicity * (216 - defenseSuccesses);
        }

        return new DclExactProbability(hits, 216 * defenseOutcomes);
    }

    public static int HitChancePercent(int attackSkill, DclDefenseOption defense)
        => HitProbability(attackSkill, defense).RoundWholePercent();
}

internal sealed class DclGuardPool
{
    public int TargetCharId { get; private set; } = -1;
    public int MaxParryUses { get; private set; }
    public int ParryUses { get; private set; }
    public int MaxBlockUses { get; private set; }
    public int BlockUses { get; private set; }
    public bool WasActive { get; private set; }

    public void InitializeOrUpdate(int targetCharId, int maxParryUses, int maxBlockUses, bool activeNow)
    {
        maxParryUses = Math.Max(0, maxParryUses);
        maxBlockUses = Math.Max(0, maxBlockUses);
        if (TargetCharId != targetCharId)
        {
            TargetCharId = targetCharId;
            MaxParryUses = maxParryUses;
            ParryUses = maxParryUses;
            MaxBlockUses = maxBlockUses;
            BlockUses = maxBlockUses;
            WasActive = activeNow;
            return;
        }

        MaxParryUses = maxParryUses;
        MaxBlockUses = maxBlockUses;
        ParryUses = Math.Min(ParryUses, MaxParryUses);
        BlockUses = Math.Min(BlockUses, MaxBlockUses);
    }

    public bool ObserveActive(bool activeNow)
    {
        bool startedOwnTurn = !WasActive && activeNow;
        WasActive = activeNow;
        if (!startedOwnTurn)
            return false;

        ParryUses = MaxParryUses;
        BlockUses = MaxBlockUses;
        return true;
    }

    public bool IsAvailable(DclDefenseKind kind)
        => kind switch
        {
            DclDefenseKind.Dodge => true,
            DclDefenseKind.Parry => ParryUses > 0,
            DclDefenseKind.Block => BlockUses > 0,
            _ => false,
        };

    public bool Spend(DclDefenseKind kind)
    {
        switch (kind)
        {
            case DclDefenseKind.Parry when ParryUses > 0:
                ParryUses--;
                return true;
            case DclDefenseKind.Block when BlockUses > 0:
                BlockUses--;
                return true;
            default:
                return false;
        }
    }
}
