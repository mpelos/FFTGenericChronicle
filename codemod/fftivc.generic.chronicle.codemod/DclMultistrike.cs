namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclMultistrikeAggregate(
    int StrikeCount,
    int HitCount,
    int CriticalCount,
    int AttackMissCount,
    int FumbleCount,
    int EvadedCount,
    int DefendedCount,
    int ParryAttempts,
    int BlockAttempts,
    int TotalDebit)
{
    public bool AnyHit => HitCount > 0;
}

internal readonly record struct DclPhysicalStrikeProfile(
    int AttackSkill,
    DclDefenseOption Defense);

/// <summary>
/// Pure aggregation for a managed single-target multistrike. Each supplied contest remains an
/// independent strike; successful and failed finite-defense attempts are counted separately so the
/// runtime can spend the corresponding Guard charges exactly once when the aggregate result commits.
/// Reactions are intentionally outside this helper because their cadence is once per outer action.
/// </summary>
internal static class DclMultistrike
{
    private static readonly int[] ThreeD6Counts =
        [1, 3, 6, 10, 15, 21, 25, 27, 27, 25, 21, 15, 10, 6, 3, 1];

    public static DclMultistrikeAggregate AggregatePhysical(
        IReadOnlyList<DclPhysicalContestResult> strikes,
        int normalDebit,
        int criticalDebit)
    {
        ArgumentNullException.ThrowIfNull(strikes);
        if (normalDebit < 0)
            throw new ArgumentOutOfRangeException(nameof(normalDebit));
        if (criticalDebit < 0)
            throw new ArgumentOutOfRangeException(nameof(criticalDebit));

        int hitCount = 0;
        int criticalCount = 0;
        int attackMissCount = 0;
        int fumbleCount = 0;
        int defendedCount = 0;
        int parryAttempts = 0;
        int blockAttempts = 0;
        long totalDebit = 0;

        foreach (var strike in strikes)
        {
            if (strike.DefenseKind == DclDefenseKind.Parry)
                parryAttempts++;
            else if (strike.DefenseKind == DclDefenseKind.Block)
                blockAttempts++;

            switch (strike.Outcome)
            {
                case DclPhysicalOutcome.CriticalHit:
                    hitCount++;
                    criticalCount++;
                    totalDebit += criticalDebit;
                    break;
                case DclPhysicalOutcome.Hit:
                    hitCount++;
                    totalDebit += normalDebit;
                    break;
                case DclPhysicalOutcome.Defended:
                    defendedCount++;
                    break;
                case DclPhysicalOutcome.AttackMiss:
                    attackMissCount++;
                    break;
                case DclPhysicalOutcome.AttackFumble:
                    fumbleCount++;
                    break;
            }
        }

        return new DclMultistrikeAggregate(
            strikes.Count,
            hitCount,
            criticalCount,
            attackMissCount,
            fumbleCount,
            0,
            defendedCount,
            parryAttempts,
            blockAttempts,
            (int)Math.Min(totalDebit, int.MaxValue));
    }

    public static int AnyHitChancePercent(IReadOnlyList<int> strikeHitChancePct)
    {
        ArgumentNullException.ThrowIfNull(strikeHitChancePct);
        if (strikeHitChancePct.Count == 0)
            return 0;

        double allMissProbability = 1.0;
        foreach (int pct in strikeHitChancePct)
            allMissProbability *= 1.0 - Math.Clamp(pct, 0, 100) / 100.0;

        return Math.Clamp((int)Math.Round((1.0 - allMissProbability) * 100.0), 0, 100);
    }

    public static DclMultistrikeAggregate AggregateMagic(IReadOnlyList<bool> strikeHits)
    {
        ArgumentNullException.ThrowIfNull(strikeHits);
        int hitCount = strikeHits.Count(hit => hit);
        return new DclMultistrikeAggregate(
            StrikeCount: strikeHits.Count,
            HitCount: hitCount,
            CriticalCount: 0,
            AttackMissCount: 0,
            FumbleCount: 0,
            EvadedCount: strikeHits.Count - hitCount,
            DefendedCount: 0,
            ParryAttempts: 0,
            BlockAttempts: 0,
            TotalDebit: 0);
    }

    /// <summary>
    /// Exact nominal chance that at least one strike lands while finite Guard changes between
    /// strikes. Only no-hit branches need to remain in the state table: attack misses/fumbles keep
    /// Guard, while a successful finite defense consumes its selected charge. Branches where a
    /// strike lands are terminal for the any-hit question.
    /// </summary>
    public static int ExactPhysicalAnyHitChancePercent(
        int strikeCount,
        int initialParryUses,
        int initialBlockUses,
        Func<int, int, int, DclPhysicalStrikeProfile> profileForState)
    {
        if (strikeCount < 1)
            return 0;
        if (initialParryUses < 0)
            throw new ArgumentOutOfRangeException(nameof(initialParryUses));
        if (initialBlockUses < 0)
            throw new ArgumentOutOfRangeException(nameof(initialBlockUses));
        ArgumentNullException.ThrowIfNull(profileForState);

        var states = new Dictionary<(int Parry, int Block), double>
        {
            [(initialParryUses, initialBlockUses)] = 1.0,
        };

        for (int strikeIndex = 0; strikeIndex < strikeCount && states.Count > 0; strikeIndex++)
        {
            var next = new Dictionary<(int Parry, int Block), double>();
            foreach (var (state, stateProbability) in states)
            {
                var profile = profileForState(strikeIndex, state.Parry, state.Block);
                int defenseSuccesses = profile.Defense.Kind == DclDefenseKind.None
                    ? 0
                    : ThreeD6SuccessCount(profile.Defense.Target);

                for (int attackRoll = 3; attackRoll <= 18; attackRoll++)
                {
                    int attackMultiplicity = ThreeD6Counts[attackRoll - 3];
                    if (DclPhysicalContest.IsCritical(attackRoll, profile.AttackSkill))
                        continue;

                    double attackProbability = stateProbability * attackMultiplicity / 216.0;
                    if (DclPhysicalContest.IsFumble(attackRoll, profile.AttackSkill) ||
                        attackRoll > profile.AttackSkill)
                    {
                        AddProbability(next, state, attackProbability);
                        continue;
                    }

                    if (profile.Defense.Kind == DclDefenseKind.None || defenseSuccesses == 0)
                        continue;

                    var defendedState = profile.Defense.Kind switch
                    {
                        DclDefenseKind.Parry when state.Parry > 0 => (state.Parry - 1, state.Block),
                        DclDefenseKind.Block when state.Block > 0 => (state.Parry, state.Block - 1),
                        _ => state,
                    };
                    AddProbability(
                        next,
                        defendedState,
                        attackProbability * defenseSuccesses / 216.0);
                }
            }
            states = next;
        }

        double allMissProbability = states.Values.Sum();
        return Math.Clamp((int)Math.Round((1.0 - allMissProbability) * 100.0), 0, 100);
    }

    private static int ThreeD6SuccessCount(int target)
    {
        if (target < 3)
            return 0;
        if (target >= 18)
            return 216;

        int count = 0;
        for (int roll = 3; roll <= target; roll++)
            count += ThreeD6Counts[roll - 3];
        return count;
    }

    private static void AddProbability(
        Dictionary<(int Parry, int Block), double> states,
        (int Parry, int Block) state,
        double probability)
    {
        states.TryGetValue(state, out double existing);
        states[state] = existing + probability;
    }
}
