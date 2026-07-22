using System.Globalization;
using System.Text.RegularExpressions;

namespace fftivc.generic.chronicle.codemod;

internal enum DclStateDurationRuleKind
{
    None,
    MaterializedAtApplication,
    Fixed,
    WinningMargin,
}

internal readonly record struct DclStateDurationRule(
    DclStateDurationRuleKind Kind,
    int FixedUnits,
    int BaseDuration,
    int DurationBand,
    int MinimumDuration,
    int MaximumDuration);

internal static partial class DclStateDurationRules
{
    public const string MaterializedFormula = "resolved-at-application";

    [GeneratedRegex(
        @"^clamp\(\s*(?<min>\d+)\s*,\s*(?<base>\d+)\s*\+\s*floor\(\s*margin\s*/\s*(?<band>\d+)\s*\)\s*,\s*(?<max>\d+)\s*\)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex WinningMarginPattern();

    public static DclStateDurationRule Parse(DclStateDurationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        bool untimed = profile.Clock is DclStateDurationClock.Permanent or
            DclStateDurationClock.ExplicitCommand;
        if (untimed)
        {
            if (!string.IsNullOrWhiteSpace(profile.Formula))
                throw new FormatException("Permanent and ExplicitCommand durations cannot own a formula.");
            return new DclStateDurationRule(DclStateDurationRuleKind.None, 0, 0, 0, 0, 0);
        }
        if (profile.Clock == DclStateDurationClock.Unknown)
            throw new FormatException("A state duration clock must be explicit.");
        string formula = profile.Formula?.Trim() ?? "";
        if (formula.Length == 0)
            throw new FormatException("A timed state requires a duration formula.");
        if (StringComparer.OrdinalIgnoreCase.Equals(formula, MaterializedFormula))
            return new DclStateDurationRule(
                DclStateDurationRuleKind.MaterializedAtApplication,
                0,
                0,
                0,
                0,
                0);
        if (int.TryParse(formula, NumberStyles.None, CultureInfo.InvariantCulture, out int fixedUnits))
        {
            if (fixedUnits < 1)
                throw new FormatException("A fixed state duration must be positive.");
            return new DclStateDurationRule(DclStateDurationRuleKind.Fixed, fixedUnits, 0, 0, 0, 0);
        }
        Match margin = WinningMarginPattern().Match(formula);
        if (!margin.Success)
            throw new FormatException(
                "Duration formula must be a positive integer, 'resolved-at-application', or clamp(min, base + floor(margin / band), max).");
        int minimum = ParseGroup(margin, "min");
        int baseDuration = ParseGroup(margin, "base");
        int band = ParseGroup(margin, "band");
        int maximum = ParseGroup(margin, "max");
        if (band < 1 || minimum < 1 || minimum > baseDuration || baseDuration > maximum)
            throw new FormatException(
                "Winning-margin duration must satisfy band > 0 and 1 <= minimum <= base <= maximum.");
        return new DclStateDurationRule(
            DclStateDurationRuleKind.WinningMargin,
            0,
            baseDuration,
            band,
            minimum,
            maximum);
    }

    public static int? Resolve(
        DclStateDurationProfile profile,
        int? winningMargin,
        int? materializedUnits)
    {
        DclStateDurationRule rule = Parse(profile);
        return rule.Kind switch
        {
            DclStateDurationRuleKind.None when materializedUnits is null => null,
            DclStateDurationRuleKind.None => throw new ArgumentException(
                "An untimed state cannot receive materialized duration units.", nameof(materializedUnits)),
            DclStateDurationRuleKind.MaterializedAtApplication when materializedUnits is > 0 => materializedUnits,
            DclStateDurationRuleKind.MaterializedAtApplication => throw new ArgumentException(
                "This duration requires positive application-owned units.", nameof(materializedUnits)),
            DclStateDurationRuleKind.Fixed when materializedUnits is null || materializedUnits == rule.FixedUnits =>
                rule.FixedUnits,
            DclStateDurationRuleKind.Fixed => throw new ArgumentException(
                "Materialized duration disagrees with the fixed authored duration.", nameof(materializedUnits)),
            DclStateDurationRuleKind.WinningMargin when winningMargin is > 0 &&
                (materializedUnits is null || materializedUnits == DclStatusRules.DurationFromWinningMargin(
                    rule.BaseDuration,
                    rule.DurationBand,
                    rule.MinimumDuration,
                    rule.MaximumDuration,
                    winningMargin.Value)) => DclStatusRules.DurationFromWinningMargin(
                        rule.BaseDuration,
                        rule.DurationBand,
                        rule.MinimumDuration,
                        rule.MaximumDuration,
                        winningMargin.Value),
            DclStateDurationRuleKind.WinningMargin when winningMargin is null or <= 0 =>
                throw new ArgumentException("Winning-margin duration requires a positive margin.", nameof(winningMargin)),
            DclStateDurationRuleKind.WinningMargin => throw new ArgumentException(
                "Materialized duration disagrees with the authored winning-margin formula.", nameof(materializedUnits)),
            _ => throw new InvalidOperationException("Unknown state duration rule."),
        };
    }

    private static int ParseGroup(Match match, string name)
        => int.Parse(match.Groups[name].Value, NumberStyles.None, CultureInfo.InvariantCulture);
}
