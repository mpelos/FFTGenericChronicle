namespace fftivc.generic.chronicle.codemod;

internal readonly record struct ImmediateActionCandidateScoreInput(
    bool IsTarget,
    int UnitHp,
    int ActionId,
    bool HasPrimaryPendingFlag,
    bool HasSecondaryPendingFlag,
    int PendingTimer,
    int ActiveMarker2,
    int StateAgeMs,
    int SeenAgeMs,
    int CtDropAgeMs,
    int RawDamage,
    int ActionIdAgeMs,
    int ActiveActionAgeMs)
{
    public bool AllowZeroActionIdActiveSource { get; init; }
}

internal readonly record struct ImmediateActionCandidateScore(
    int Score,
    string Role,
    bool SourceLike,
    bool CurrentActiveAction,
    bool FreshActionId,
    bool FreshActiveAction,
    bool StaleActionId,
    bool StaleActiveAction);

internal static class ImmediateActionCandidateScoring
{
    public const int FreshActionWindowMs = 5000;
    public const int StaleActionWindowMs = 15000;

    public static ImmediateActionCandidateScore Evaluate(ImmediateActionCandidateScoreInput input)
    {
        bool hasActiveSourceMarker = input.ActiveMarker2 == 1;
        bool zeroActionActiveSource = input.AllowZeroActionIdActiveSource &&
                                      !input.IsTarget &&
                                      input.UnitHp > 0 &&
                                      input.ActionId == 0 &&
                                      hasActiveSourceMarker;
        bool sourceLike = !input.IsTarget && input.UnitHp > 0 && (input.ActionId > 0 || zeroActionActiveSource);
        bool currentActiveAction = sourceLike && hasActiveSourceMarker;
        bool freshActionId = sourceLike && input.ActionId > 0 && IsWithin(input.ActionIdAgeMs, FreshActionWindowMs);
        bool freshActiveAction = currentActiveAction && IsWithin(input.ActiveActionAgeMs, FreshActionWindowMs);
        bool staleActionId = sourceLike && input.ActionId > 0 && input.ActionIdAgeMs > StaleActionWindowMs;
        bool staleActiveAction = currentActiveAction && input.ActiveActionAgeMs > StaleActionWindowMs;

        int score = 0;
        if (sourceLike) score += 600;
        if (currentActiveAction) score += 1000;
        if (freshActionId) score += 600;
        if (freshActiveAction) score += 300;
        if (!input.IsTarget && input.ActionId > 0) score += 150;
        if (zeroActionActiveSource) score += 150;
        if (!input.IsTarget && input.HasPrimaryPendingFlag) score += 200;
        if (!input.IsTarget && input.HasSecondaryPendingFlag) score += 200;
        if (!input.IsTarget && input.PendingTimer != 0xFF) score += 100;
        if (input.StateAgeMs >= 0) score += Math.Max(0, 250 - Math.Min(input.StateAgeMs, 250));
        if (input.SeenAgeMs >= 0) score += Math.Max(0, 250 - Math.Min(input.SeenAgeMs, 250));
        if (input.CtDropAgeMs >= 0) score += Math.Max(0, 150 - Math.Min(input.CtDropAgeMs, 150));
        if (input.IsTarget && input.RawDamage > 0) score += 250;
        if (input.IsTarget) score -= 200;
        if (input.UnitHp <= 0 && !input.IsTarget) score -= 500;
        if (staleActionId) score -= 500;
        if (staleActiveAction && !zeroActionActiveSource) score -= 500;

        string role = input.IsTarget ? "target" : zeroActionActiveSource ? "active-source-like" : sourceLike ? "source-like" : "context";
        return new ImmediateActionCandidateScore(
            score,
            role,
            sourceLike,
            currentActiveAction,
            freshActionId,
            freshActiveAction,
            staleActionId,
            staleActiveAction);
    }

    private static bool IsWithin(int ageMs, int windowMs)
        => ageMs >= 0 && ageMs <= windowMs;
}
