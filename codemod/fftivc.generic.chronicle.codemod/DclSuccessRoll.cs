namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Shared GURPS-style 3d6 success-roll classification. Effective scores remain open-ended, while
/// natural 3-4 and 17-18 preserve the automatic outcome bands.
/// </summary>
internal static class DclSuccessRoll
{
    private static readonly int[] ThreeD6Counts =
        [1, 3, 6, 10, 15, 21, 25, 27, 27, 25, 21, 15, 10, 6, 3, 1];

    public const int MinRoll = 3;
    public const int MaxRoll = 18;

    public static bool IsAutomaticSuccess(int roll) => roll is 3 or 4;

    public static bool IsAutomaticFailure(int roll) => roll is 17 or 18;

    public static bool Succeeds(int roll, int effectiveScore)
    {
        Validate(roll);
        if (IsAutomaticSuccess(roll))
            return true;
        if (IsAutomaticFailure(roll))
            return false;
        return roll <= effectiveScore;
    }

    public static bool IsCriticalSuccess(int roll, int effectiveScore)
    {
        Validate(roll);
        return roll is 3 or 4 || (roll == 5 && effectiveScore >= 15) ||
               (roll == 6 && effectiveScore >= 16);
    }

    public static bool IsCriticalFailure(int roll, int effectiveScore)
    {
        Validate(roll);
        if (IsCriticalSuccess(roll, effectiveScore))
            return false;
        return roll == 18 || (roll == 17 && effectiveScore <= 15) ||
               (long)roll - effectiveScore >= 10;
    }

    public static int SuccessOutcomeCount(int effectiveScore)
    {
        int count = 0;
        for (int roll = MinRoll; roll <= MaxRoll; roll++)
        {
            if (Succeeds(roll, effectiveScore))
                count += ThreeD6Counts[roll - MinRoll];
        }
        return count;
    }

    public static void Validate(int roll)
    {
        if (roll is < MinRoll or > MaxRoll)
            throw new ArgumentOutOfRangeException(nameof(roll), "3d6 totals must be within 3..18.");
    }
}
