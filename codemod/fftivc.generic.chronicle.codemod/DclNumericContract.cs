using System.Numerics;

namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Exact rational used at DCL rounding boundaries. It deliberately avoids binary floating-point
/// so authored decimals, multipliers, and forecast probabilities share one representation.
/// </summary>
internal readonly record struct DclRational : IComparable<DclRational>
{
    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }

    public DclRational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
            throw new DivideByZeroException("A DCL rational denominator cannot be zero.");
        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        BigInteger divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        Numerator = numerator / divisor;
        Denominator = denominator / divisor;
    }

    public static DclRational FromInteger(long value) => new(value, BigInteger.One);

    public static DclRational ParseExactDecimal(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string value = text.Trim();
        if (value.Length == 0)
            throw new FormatException("An exact DCL decimal cannot be empty.");

        int sign = 1;
        if (value[0] is '+' or '-')
        {
            sign = value[0] == '-' ? -1 : 1;
            value = value[1..];
        }

        string[] parts = value.Split('.');
        if (parts.Length > 2 || parts.All(part => part.Length == 0) ||
            parts.Any(part => part.Any(c => !char.IsAsciiDigit(c))))
            throw new FormatException($"'{text}' is not an exact base-10 decimal.");

        string whole = parts[0].Length == 0 ? "0" : parts[0];
        string fraction = parts.Length == 2 ? parts[1] : "";
        if (parts.Length == 2 && fraction.Length == 0)
            throw new FormatException($"'{text}' has no digits after its decimal point.");

        BigInteger denominator = BigInteger.Pow(10, fraction.Length);
        BigInteger numerator = BigInteger.Parse(whole) * denominator;
        if (fraction.Length > 0)
            numerator += BigInteger.Parse(fraction);
        return new DclRational(sign * numerator, denominator);
    }

    public BigInteger Floor()
    {
        BigInteger quotient = BigInteger.DivRem(Numerator, Denominator, out BigInteger remainder);
        return Numerator.Sign < 0 && !remainder.IsZero ? quotient - 1 : quotient;
    }

    public BigInteger Ceiling()
    {
        BigInteger quotient = BigInteger.DivRem(Numerator, Denominator, out BigInteger remainder);
        return Numerator.Sign > 0 && !remainder.IsZero ? quotient + 1 : quotient;
    }

    public BigInteger RoundNearest()
    {
        BigInteger absolute = BigInteger.Abs(Numerator);
        BigInteger quotient = BigInteger.DivRem(absolute, Denominator, out BigInteger remainder);
        if (remainder * 2 >= Denominator)
            quotient += 1;
        return Numerator.Sign < 0 ? -quotient : quotient;
    }

    public int CompareTo(DclRational other)
        => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);

    public static DclRational operator +(DclRational left, DclRational right)
        => new(left.Numerator * right.Denominator + right.Numerator * left.Denominator,
            left.Denominator * right.Denominator);

    public static DclRational operator -(DclRational left, DclRational right)
        => new(left.Numerator * right.Denominator - right.Numerator * left.Denominator,
            left.Denominator * right.Denominator);

    public static DclRational operator *(DclRational left, DclRational right)
        => new(left.Numerator * right.Numerator, left.Denominator * right.Denominator);

    public static DclRational operator /(DclRational left, DclRational right)
        => right.Numerator.IsZero
            ? throw new DivideByZeroException("A DCL rational cannot be divided by zero.")
            : new DclRational(left.Numerator * right.Denominator,
                left.Denominator * right.Numerator);

    public static bool operator <(DclRational left, DclRational right) => left.CompareTo(right) < 0;
    public static bool operator >(DclRational left, DclRational right) => left.CompareTo(right) > 0;
    public static bool operator <=(DclRational left, DclRational right) => left.CompareTo(right) <= 0;
    public static bool operator >=(DclRational left, DclRational right) => left.CompareTo(right) >= 0;

    public override string ToString()
        => Denominator.IsOne ? Numerator.ToString() : $"{Numerator}/{Denominator}";
}

internal readonly record struct DclExactProbability
{
    public BigInteger SuccessfulOutcomes { get; }
    public BigInteger TotalOutcomes { get; }

    public DclExactProbability(BigInteger successfulOutcomes, BigInteger totalOutcomes)
    {
        if (totalOutcomes <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalOutcomes), "Outcome count must be positive.");
        if (successfulOutcomes < 0 || successfulOutcomes > totalOutcomes)
            throw new ArgumentOutOfRangeException(nameof(successfulOutcomes),
                "Successful outcomes must be within 0..total outcomes.");
        SuccessfulOutcomes = successfulOutcomes;
        TotalOutcomes = totalOutcomes;
    }

    public DclRational Fraction => new(SuccessfulOutcomes, TotalOutcomes);

    public int RoundWholePercent()
        => checked((int)(Fraction * DclRational.FromInteger(100)).RoundNearest());

    public int FloorPermille()
        => checked((int)(Fraction * DclRational.FromInteger(1000)).Floor());
}

internal enum DclQuickContestOutcome
{
    ActingRollFailed,
    TargetResisted,
    ActingSideWon,
}

internal readonly record struct DclQuickContestResult(
    DclQuickContestOutcome Outcome,
    int ActingMargin,
    int TargetMargin)
{
    public bool ActingSideWon => Outcome == DclQuickContestOutcome.ActingSideWon;
}

internal static class DclQuickContest
{
    public const int OutcomeCount = 216 * 216;

    public static DclQuickContestResult Resolve(
        int actingScore,
        int actingRoll,
        int targetScore,
        int targetRoll)
    {
        DclSuccessRoll.Validate(actingRoll);
        DclSuccessRoll.Validate(targetRoll);
        int actingMargin = checked(actingScore - actingRoll);
        int targetMargin = checked(targetScore - targetRoll);
        if (!DclSuccessRoll.Succeeds(actingRoll, actingScore))
            return new DclQuickContestResult(
                DclQuickContestOutcome.ActingRollFailed, actingMargin, targetMargin);

        bool targetSucceeded = DclSuccessRoll.Succeeds(targetRoll, targetScore);
        return !targetSucceeded || actingMargin > targetMargin
            ? new DclQuickContestResult(DclQuickContestOutcome.ActingSideWon, actingMargin, targetMargin)
            : new DclQuickContestResult(DclQuickContestOutcome.TargetResisted, actingMargin, targetMargin);
    }

    public static DclExactProbability SuccessProbability(int actingScore, int targetScore)
    {
        int successes = 0;
        for (int actingRoll = DclSuccessRoll.MinRoll; actingRoll <= DclSuccessRoll.MaxRoll; actingRoll++)
        for (int targetRoll = DclSuccessRoll.MinRoll; targetRoll <= DclSuccessRoll.MaxRoll; targetRoll++)
        {
            if (!Resolve(actingScore, actingRoll, targetScore, targetRoll).ActingSideWon)
                continue;
            successes += DclSuccessRoll.OutcomeMultiplicity(actingRoll) *
                         DclSuccessRoll.OutcomeMultiplicity(targetRoll);
        }
        return new DclExactProbability(successes, OutcomeCount);
    }
}

internal static class DclPercentageRoll
{
    public static bool Succeeds(int roll, int chancePercent)
    {
        if (roll is < 0 or > 99)
            throw new ArgumentOutOfRangeException(nameof(roll), "Percentage rolls must be within 0..99.");
        return roll < Math.Clamp(chancePercent, 0, 100);
    }
}

internal enum DclRollSite
{
    Casting,
    Attack,
    ActiveDefense,
    Resistance,
    MajorWound,
    Concentration,
    AimRetention,
    Recovery,
    ReactionActivation,
    DamageDie,
    HealingDie,
    ResourceMagnitudeDie,
    Percentage,
    RandomSelection,
    Explicit,
}

/// <summary>
/// Semantic identity for one execution-only random draw. It contains no pointer or callback identity,
/// so forecast polling and nested native carriers cannot manufacture another random site.
/// </summary>
internal readonly record struct DclRollIdentity(
    int BattleGeneration,
    long ActionInstanceId,
    int SourceUnitSlot,
    int SourceCharacterId,
    int TargetUnitSlot,
    int TargetCharacterId,
    int StrikeIndex,
    DclRollSite RollSite,
    int DrawIndex);
