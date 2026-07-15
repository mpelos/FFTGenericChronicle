namespace fftivc.generic.chronicle.codemod;

internal enum DclMagicMultistrikeAvoidance
{
    PerTarget,
    PerStrike,
}

internal readonly record struct DclMagicStrikeDecision(
    int StrikeIndex,
    bool Hit,
    int HitPct,
    int Roll,
    int MagicEvade);

internal sealed record DclMagicMultistrikeResolution(
    IReadOnlyList<DclMagicStrikeDecision> Strikes,
    DclMultistrikeAggregate Aggregate,
    int AnyHitPct);

/// <summary>
/// Pure Magic-Evade decision generator for an authored multistrike. It deliberately owns no native
/// apply or status carrier: RandomFire result cardinality must be observed before an aggregate can
/// be bound to the pre-clamp pipeline without risking duplicate damage.
/// </summary>
internal static class DclMagicMultistrike
{
    public static DclMagicMultistrikeResolution Resolve(
        int strikeCount,
        DclMagicMultistrikeAvoidance avoidance,
        int magicEvadeCapPct,
        Func<int, int> rawMagicEvadeForStrike,
        Func<int, int> rollForStrike)
    {
        if (strikeCount is < 1 or > 99)
            throw new ArgumentOutOfRangeException(nameof(strikeCount));
        ArgumentNullException.ThrowIfNull(rawMagicEvadeForStrike);
        ArgumentNullException.ThrowIfNull(rollForStrike);

        var decisions = new DclMagicStrikeDecision[strikeCount];
        var hitPcts = new int[strikeCount];
        if (avoidance == DclMagicMultistrikeAvoidance.PerTarget)
        {
            int magicEvade = DclMagicEvade.EvadePercent(rawMagicEvadeForStrike(0), magicEvadeCapPct);
            int hitPct = 100 - magicEvade;
            int roll = CheckedRoll(rollForStrike(0));
            bool hit = roll < hitPct;
            for (int strikeIndex = 0; strikeIndex < strikeCount; strikeIndex++)
            {
                decisions[strikeIndex] = new DclMagicStrikeDecision(
                    strikeIndex, hit, hitPct, roll, magicEvade);
                hitPcts[strikeIndex] = hitPct;
            }
        }
        else
        {
            for (int strikeIndex = 0; strikeIndex < strikeCount; strikeIndex++)
            {
                int magicEvade = DclMagicEvade.EvadePercent(
                    rawMagicEvadeForStrike(strikeIndex), magicEvadeCapPct);
                int hitPct = 100 - magicEvade;
                int roll = CheckedRoll(rollForStrike(strikeIndex));
                decisions[strikeIndex] = new DclMagicStrikeDecision(
                    strikeIndex, roll < hitPct, hitPct, roll, magicEvade);
                hitPcts[strikeIndex] = hitPct;
            }
        }

        var aggregate = DclMultistrike.AggregateMagic(decisions.Select(decision => decision.Hit).ToArray());
        int anyHitPct = avoidance == DclMagicMultistrikeAvoidance.PerTarget
            ? hitPcts[0]
            : DclMultistrike.AnyHitChancePercent(hitPcts);
        return new DclMagicMultistrikeResolution(Array.AsReadOnly(decisions), aggregate, anyHitPct);
    }

    private static int CheckedRoll(int roll)
    {
        if (roll is < 0 or > 99)
            throw new ArgumentOutOfRangeException(nameof(roll), "Magic-Evade rolls must be within 0..99.");
        return roll;
    }
}
