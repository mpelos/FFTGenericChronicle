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
    private const int MinRoll = 3;
    private const int MaxRoll = 18;

    public static int Roll3D6(Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return rng.Next(1, 7) + rng.Next(1, 7) + rng.Next(1, 7);
    }

    public static bool IsCritical(int roll, int skill)
        => roll is 3 or 4 || (roll == 5 && skill >= 15) || (roll == 6 && skill >= 16);

    public static bool IsFumble(int roll, int skill)
        => roll == 18 || (roll == 17 && skill <= 15);

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

        // Ties deliberately prefer Block, then Parry, then Dodge. This makes the authored guard
        // ladder deterministic and spends the finite shield before the finite weapon guard.
        var best = new DclDefenseOption(DclDefenseKind.Dodge, dodge + modifier, false);
        if (parryAvailable && parry + modifier >= best.Target)
            best = new DclDefenseOption(DclDefenseKind.Parry, parry + modifier, true);
        if (blockAvailable && block + modifier >= best.Target)
            best = new DclDefenseOption(DclDefenseKind.Block, block + modifier, true);
        return best;
    }

    public static DclPhysicalContestResult Resolve(
        int attackSkill,
        int attackRoll,
        DclDefenseOption defense,
        int defenseRoll)
    {
        if (attackRoll is < MinRoll or > MaxRoll)
            throw new ArgumentOutOfRangeException(nameof(attackRoll), "3d6 totals must be within 3..18.");
        if (defense.Kind != DclDefenseKind.None && (defenseRoll is < MinRoll or > MaxRoll))
            throw new ArgumentOutOfRangeException(nameof(defenseRoll), "3d6 totals must be within 3..18 when a defense roll exists.");

        if (IsCritical(attackRoll, attackSkill))
            return new DclPhysicalContestResult(DclPhysicalOutcome.CriticalHit, attackSkill, attackRoll,
                DclDefenseKind.None, 0, -1);
        if (IsFumble(attackRoll, attackSkill))
            return new DclPhysicalContestResult(DclPhysicalOutcome.AttackFumble, attackSkill, attackRoll,
                DclDefenseKind.None, 0, -1);
        if (attackRoll > attackSkill)
            return new DclPhysicalContestResult(DclPhysicalOutcome.AttackMiss, attackSkill, attackRoll,
                DclDefenseKind.None, 0, -1);
        if (defense.Kind != DclDefenseKind.None && defenseRoll <= defense.Target)
            return new DclPhysicalContestResult(DclPhysicalOutcome.Defended, attackSkill, attackRoll,
                defense.Kind, defense.Target, defenseRoll);

        return new DclPhysicalContestResult(DclPhysicalOutcome.Hit, attackSkill, attackRoll,
            defense.Kind, defense.Target, defense.Kind == DclDefenseKind.None ? -1 : defenseRoll);
    }

    public static int HitChancePercent(int attackSkill, DclDefenseOption defense)
    {
        int hits = 0;
        int outcomes = 0;
        for (int a = 1; a <= 6; a++)
        for (int b = 1; b <= 6; b++)
        for (int c = 1; c <= 6; c++)
        {
            int attackRoll = a + b + c;
            if (IsCritical(attackRoll, attackSkill))
            {
                int multiplicity = defense.Kind == DclDefenseKind.None ? 1 : 216;
                hits += multiplicity;
                outcomes += multiplicity;
                continue;
            }

            if (IsFumble(attackRoll, attackSkill) || attackRoll > attackSkill)
            {
                outcomes += defense.Kind == DclDefenseKind.None ? 1 : 216;
                continue;
            }

            if (defense.Kind == DclDefenseKind.None)
            {
                hits++;
                outcomes++;
                continue;
            }

            for (int d = 1; d <= 6; d++)
            for (int e = 1; e <= 6; e++)
            for (int f = 1; f <= 6; f++)
            {
                int defenseRoll = d + e + f;
                if (defenseRoll > defense.Target)
                    hits++;
                outcomes++;
            }
        }

        return outcomes == 0 ? 0 : (hits * 100 + outcomes / 2) / outcomes;
    }
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
