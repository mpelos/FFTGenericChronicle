namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclNativeRepeatSnapshot(
    int Count,
    int Index,
    int RightWeaponItemId,
    int LeftWeaponItemId,
    int ActiveWeaponItemId);

internal enum DclNativeWeaponHand
{
    Primary,
    OffHand,
}

/// <summary>
/// Current-build native repeat-carrier facts kept separate from policy math. RandomFire formulas
/// and Barrage share the same repeat count/index globals and result-producer continuation test.
/// </summary>
internal static class DclNativeRepeat
{
    public const int RepeatCountRva = 0x7B0762;
    public const int RepeatIndexRva = 0x7B0763;
    public const int RightWeaponRva = 0x7B0764;
    public const int LeftWeaponRva = 0x7B0766;
    public const int BarrageRepeatCount = 4;

    private static readonly int[] TruthWeights = [5, 5, 10, 10, 20, 20, 10, 10, 5, 5];

    // The native result producer increments RepeatIndex before downstream delivery. Index < count
    // therefore means that the current target's spell-level decision must survive another result.
    public static bool HasMoreRepeats(bool enabled, int repeatCount, int repeatIndex)
        => enabled && repeatCount > 0 && repeatIndex >= 0 && repeatIndex < repeatCount;

    // This is the current-build selector at computeActionResult 0x309AB5..0x309AE6. The protected
    // repeat initializer normalizes the equipped weapon ids into the globals above before any
    // calculation: repeat index zero selects the normalized right/primary weapon, index one selects
    // the left/off-hand weapon, and native sequences longer than two always select the primary.
    public static int SelectActiveWeaponItemId(
        int actionType,
        int repeatCount,
        int repeatIndex,
        int rightWeapon,
        int leftWeapon)
    {
        if (actionType != 1 || repeatCount < 0 || repeatIndex < 0)
            return -1;

        return repeatCount > 2 || repeatIndex == 0
            ? rightWeapon
            : leftWeapon;
    }

    public static DclNativeWeaponHand? SelectActiveWeaponHand(
        int actionType,
        int repeatCount,
        int repeatIndex)
    {
        if (actionType != 1 || repeatCount < 0 || repeatIndex < 0)
            return null;
        return repeatCount > 2 || repeatIndex == 0
            ? DclNativeWeaponHand.Primary
            : DclNativeWeaponHand.OffHand;
    }

    public static int TruthRepeatCountFromPercentile(int percentile)
    {
        if (percentile is < 0 or > 99)
            throw new ArgumentOutOfRangeException(nameof(percentile));

        int cumulative = 0;
        for (int index = 0; index < TruthWeights.Length; index++)
        {
            cumulative += TruthWeights[index];
            if (percentile < cumulative)
                return index + 1;
        }
        throw new InvalidOperationException("Native Truth/Untruth weights must cover all 100 percentiles.");
    }

    public static int Formula5ERepeatCount(int x)
    {
        if (x is < 0 or > 254)
            throw new ArgumentOutOfRangeException(nameof(x));
        return x + 1;
    }
}
