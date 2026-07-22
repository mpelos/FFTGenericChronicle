using System.Globalization;

namespace fftivc.generic.chronicle.codemod;

internal enum DclStrengthDamageMode
{
    Thrust,
    Swing,
}

internal readonly record struct DclDiceExpression
{
    public int Dice { get; init; }
    public int Adds { get; init; }

    public DclDiceExpression(int dice, int adds)
    {
        if (dice < 0)
            throw new ArgumentOutOfRangeException(nameof(dice), "DCL dice expressions cannot contain negative dice.");
        Dice = dice;
        Adds = adds;
    }

    public static bool TryParseAuthored(string? text, out DclDiceExpression expression)
    {
        expression = default;
        if (string.IsNullOrEmpty(text) || !string.Equals(text, text.Trim(), StringComparison.Ordinal))
            return false;
        int marker = text.IndexOf("d6", StringComparison.Ordinal);
        if (marker <= 0 || text.IndexOf("d6", marker + 2, StringComparison.Ordinal) >= 0)
            return false;
        if (!int.TryParse(text.AsSpan(0, marker), NumberStyles.None, CultureInfo.InvariantCulture, out int dice))
            return false;
        ReadOnlySpan<char> suffix = text.AsSpan(marker + 2);
        int adds = 0;
        if (!suffix.IsEmpty)
        {
            if (suffix.Length < 2 || suffix[0] is not ('+' or '-') ||
                !int.TryParse(suffix, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out adds))
                return false;
        }
        try
        {
            expression = new DclDiceExpression(dice, adds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static DclDiceExpression ParseAuthored(string text)
        => TryParseAuthored(text, out DclDiceExpression expression)
            ? expression
            : throw new FormatException($"'{text}' is not a canonical Xd6+Y expression.");

    public DclDiceExpression AddAndNormalize(int modifier)
    {
        int dice = Dice;
        int adds = checked(Adds + modifier);
        while (adds >= 7)
        {
            dice = checked(dice + 2);
            adds -= 7;
        }
        while (adds >= 4)
        {
            dice = checked(dice + 1);
            adds -= 4;
        }
        return new DclDiceExpression(dice, adds);
    }

    public override string ToString() => Adds switch
    {
        > 0 => $"{Dice}d6+{Adds}",
        < 0 => $"{Dice}d6{Adds}",
        _ => $"{Dice}d6",
    };
}

/// <summary>
/// Literal GURPS 4e Basic Set ST damage table. Values above ST 100 add one die to both modes per
/// full 10 ST; unlisted ST 41..99 uses the greatest listed five-point row not exceeding ST.
/// </summary>
internal static class DclStrengthDamage
{
    private static readonly DclDiceExpression[] ThrustThrough40 =
    [
        default,
        new(1, -6), new(1, -6), new(1, -5), new(1, -5), new(1, -4),
        new(1, -4), new(1, -3), new(1, -3), new(1, -2), new(1, -2),
        new(1, -1), new(1, -1), new(1, 0), new(1, 0), new(1, 1),
        new(1, 1), new(1, 2), new(1, 2), new(2, -1), new(2, -1),
        new(2, 0), new(2, 0), new(2, 1), new(2, 1), new(2, 2),
        new(2, 2), new(3, -1), new(3, -1), new(3, 0), new(3, 0),
        new(3, 1), new(3, 1), new(3, 2), new(3, 2), new(4, -1),
        new(4, -1), new(4, 0), new(4, 0), new(4, 1), new(4, 1),
    ];

    private static readonly DclDiceExpression[] SwingThrough40 =
    [
        default,
        new(1, -5), new(1, -5), new(1, -4), new(1, -4), new(1, -3),
        new(1, -3), new(1, -2), new(1, -2), new(1, -1), new(1, 0),
        new(1, 1), new(1, 2), new(2, -1), new(2, 0), new(2, 1),
        new(2, 2), new(3, -1), new(3, 0), new(3, 1), new(3, 2),
        new(4, -1), new(4, 0), new(4, 1), new(4, 2), new(5, -1),
        new(5, 0), new(5, 1), new(5, 1), new(5, 2), new(5, 2),
        new(6, -1), new(6, -1), new(6, 0), new(6, 0), new(6, 1),
        new(6, 1), new(6, 2), new(6, 2), new(7, -1), new(7, -1),
    ];

    public static DclDiceExpression Lookup(int strength, DclStrengthDamageMode mode)
    {
        if (strength < 1)
            throw new ArgumentOutOfRangeException(nameof(strength), "ST damage lookup requires ST >= 1.");

        if (strength <= 40)
            return mode == DclStrengthDamageMode.Thrust
                ? ThrustThrough40[strength]
                : SwingThrough40[strength];

        int anchor = strength >= 100 ? 100 : strength / 5 * 5;
        DclDiceExpression value = Anchor(anchor, mode);
        if (strength >= 100)
            value = value with { Dice = checked(value.Dice + (strength - 100) / 10) };
        return value;
    }

    private static DclDiceExpression Anchor(int strength, DclStrengthDamageMode mode) =>
        (strength, mode) switch
        {
            (40, DclStrengthDamageMode.Thrust) => new(4, 1),
            (40, DclStrengthDamageMode.Swing) => new(7, -1),
            (45, DclStrengthDamageMode.Thrust) => new(5, 0),
            (45, DclStrengthDamageMode.Swing) => new(7, 1),
            (50, DclStrengthDamageMode.Thrust) => new(5, 2),
            (50, DclStrengthDamageMode.Swing) => new(8, -1),
            (55, DclStrengthDamageMode.Thrust) => new(6, 0),
            (55, DclStrengthDamageMode.Swing) => new(8, 1),
            (60, DclStrengthDamageMode.Thrust) => new(7, -1),
            (60, DclStrengthDamageMode.Swing) => new(9, 0),
            (65, DclStrengthDamageMode.Thrust) => new(7, 1),
            (65, DclStrengthDamageMode.Swing) => new(9, 2),
            (70, DclStrengthDamageMode.Thrust) => new(8, 0),
            (70, DclStrengthDamageMode.Swing) => new(10, 0),
            (75, DclStrengthDamageMode.Thrust) => new(8, 2),
            (75, DclStrengthDamageMode.Swing) => new(10, 2),
            (80, DclStrengthDamageMode.Thrust) => new(9, 0),
            (80, DclStrengthDamageMode.Swing) => new(11, 0),
            (85, DclStrengthDamageMode.Thrust) => new(9, 2),
            (85, DclStrengthDamageMode.Swing) => new(11, 2),
            (90, DclStrengthDamageMode.Thrust) => new(10, 0),
            (90, DclStrengthDamageMode.Swing) => new(12, 0),
            (95, DclStrengthDamageMode.Thrust) => new(10, 2),
            (95, DclStrengthDamageMode.Swing) => new(12, 2),
            (100, DclStrengthDamageMode.Thrust) => new(11, 0),
            (100, DclStrengthDamageMode.Swing) => new(13, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(strength)),
        };
}
