namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclStatusGroupDecision(
    int Resistance,
    int Roll,
    bool Resisted,
    int SelectedIndex);

internal static class DclStatusGroups
{
    public const string Independent = "independent";
    public const string AllOrNothing = "all-or-nothing";
    public const string RandomOne = "random-one";

    public static string NormalizeMode(string? mode)
        => string.IsNullOrWhiteSpace(mode) ? Independent : mode.Trim().ToLowerInvariant();

    public static bool IsSupportedMode(string? mode)
        => NormalizeMode(mode) is Independent or AllOrNothing or RandomOne;

    public static DclStatusGroupDecision Resolve(
        string mode,
        int memberCount,
        int resistance,
        int roll,
        int selectedIndex = -1)
    {
        string normalizedMode = NormalizeMode(mode);
        if (normalizedMode is not (AllOrNothing or RandomOne))
            throw new ArgumentException("a shared status contest must use all-or-nothing or random-one", nameof(mode));
        if (memberCount < 2)
            throw new ArgumentOutOfRangeException(nameof(memberCount), "a shared status contest requires at least two members");
        if (roll is < 3 or > 18)
            throw new ArgumentOutOfRangeException(nameof(roll), "a status contest roll must be a 3d6 total within 3..18");
        if (normalizedMode == RandomOne && (selectedIndex < 0 || selectedIndex >= memberCount))
            throw new ArgumentOutOfRangeException(nameof(selectedIndex), "random-one requires one valid selected member");
        if (normalizedMode == AllOrNothing && selectedIndex != -1)
            throw new ArgumentOutOfRangeException(nameof(selectedIndex), "all-or-nothing cannot select one member");

        return new DclStatusGroupDecision(
            resistance,
            roll,
            DclStatusContest.Resists(roll, resistance),
            selectedIndex);
    }
}
