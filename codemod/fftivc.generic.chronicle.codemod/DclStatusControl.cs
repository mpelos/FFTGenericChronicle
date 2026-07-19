namespace fftivc.generic.chronicle.codemod;

internal sealed class DclStatusRule
{
    public string Name { get; set; } = "";
    public int AbilityId { get; set; } = -1;
    public int ActionType { get; set; } = -1;
    public int StatusByteIndex { get; set; } = -1;
    public int StatusMask { get; set; } = 0;
    public string Operation { get; set; } = "add";
    public string NativeRiderPolicy { get; set; } = "";
    public string ConditionFormula { get; set; } = "";
    public string ResistanceFormula { get; set; } = "";
    public int DurationTargetTurns { get; set; } = 0;
    public string ContestGroup { get; set; } = "";
    public string ContestMode { get; set; } = DclStatusGroups.Independent;

    public string DisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"ability-{AbilityId}-status-{StatusByteIndex}:{StatusMask:X2}"
        : Name.Trim();

    public bool IsAdd => Operation.Equals("add", StringComparison.OrdinalIgnoreCase);
    public bool IsRemove => Operation.Equals("remove", StringComparison.OrdinalIgnoreCase);

    public bool NativeRiderAbsent => NativeRiderPolicy.Equals("absent", StringComparison.OrdinalIgnoreCase);

    public bool NativeRiderSuppressedByData =>
        NativeRiderPolicy.Equals("suppressed-by-data", StringComparison.OrdinalIgnoreCase);

    public bool NativeRiderRetainedAsCarrier =>
        NativeRiderPolicy.Equals("retained-as-carrier", StringComparison.OrdinalIgnoreCase);

    public bool NativeRiderReplacedPostCalc =>
        NativeRiderPolicy.Equals("replaced-post-calc", StringComparison.OrdinalIgnoreCase);

    public string NormalizedContestMode => DclStatusGroups.NormalizeMode(ContestMode);
    public string NormalizedContestGroup => (ContestGroup ?? "").Trim().ToLowerInvariant();
    public bool UsesSharedContest => NormalizedContestMode != DclStatusGroups.Independent;

    public bool TryMatches(int actionType, int abilityId, FormulaContext context, out bool matches, out string error)
    {
        matches = false;
        error = "";
        if (AbilityId != abilityId)
            return true;
        if (ActionType >= 0 && ActionType != actionType)
            return true;

        if (!string.IsNullOrWhiteSpace(ConditionFormula))
        {
            if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
                return false;
            matches = value != 0;
            return true;
        }

        matches = true;
        return true;
    }
}

internal readonly record struct DclStatusWrite(
    string RuleName,
    int StatusByteIndex,
    byte StatusMask,
    bool Add,
    int Resistance,
    int Roll,
    bool Resisted,
    bool Immune,
    int DurationTargetTurns,
    bool FailClosed = false,
    bool NotSelected = false);

internal sealed class DclStatusDurationState
{
    public required nint TargetPtr { get; init; }
    public required int TargetCharId { get; init; }
    public required int StatusByteIndex { get; init; }
    public required byte StatusMask { get; init; }
    public required string RuleName { get; init; }
    public int RemainingTargetTurns { get; set; }
    public bool WasActive { get; set; }
    public bool SkipFirstFallingEdge { get; set; }
}

internal enum DclStatusDurationTransition
{
    None,
    SkippedApplicationTurn,
    CountedTargetTurn,
    Expired,
}

internal static class DclStatusDurationTracker
{
    public static DclStatusDurationTransition Advance(DclStatusDurationState state, bool activeNow)
    {
        ArgumentNullException.ThrowIfNull(state);

        bool completedTargetTurn = state.WasActive && !activeNow;
        state.WasActive = activeNow;
        if (!completedTargetTurn)
            return DclStatusDurationTransition.None;

        if (state.SkipFirstFallingEdge)
        {
            state.SkipFirstFallingEdge = false;
            return DclStatusDurationTransition.SkippedApplicationTurn;
        }

        state.RemainingTargetTurns--;
        return state.RemainingTargetTurns <= 0
            ? DclStatusDurationTransition.Expired
            : DclStatusDurationTransition.CountedTargetTurn;
    }
}

internal static class DclStatusContest
{
    public static int Roll3D6(Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return rng.Next(1, 7) + rng.Next(1, 7) + rng.Next(1, 7);
    }

    public static bool Resists(int roll, int resistance)
        => DclSuccessRoll.Succeeds(roll, resistance);

    public static int ResistChancePermille(int resistance)
    {
        int passing = 0;
        for (int a = 1; a <= 6; a++)
        for (int b = 1; b <= 6; b++)
        for (int c = 1; c <= 6; c++)
            if (Resists(a + b + c, resistance))
                passing++;
        return passing * 1000 / 216;
    }
}
