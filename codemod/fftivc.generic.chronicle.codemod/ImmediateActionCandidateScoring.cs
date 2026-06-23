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
    int ActiveActionAgeMs);

internal readonly record struct ImmediateActionCandidateScore(
    int Score,
    string Role,
    bool SourceLike,
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
        bool sourceLike = !input.IsTarget && input.UnitHp > 0 && input.ActionId > 0;
        bool freshActionId = sourceLike && IsWithin(input.ActionIdAgeMs, FreshActionWindowMs);
        bool freshActiveAction = sourceLike &&
                                 input.ActiveMarker2 != 0 &&
                                 IsWithin(input.ActiveActionAgeMs, FreshActionWindowMs);
        bool staleActionId = sourceLike && input.ActionIdAgeMs > StaleActionWindowMs;
        bool staleActiveAction = sourceLike &&
                                 input.ActiveMarker2 != 0 &&
                                 input.ActiveActionAgeMs > StaleActionWindowMs;

        int score = 0;
        if (sourceLike) score += 600;
        if (freshActionId) score += 600;
        if (freshActiveAction) score += 800;
        if (!input.IsTarget && input.ActionId > 0) score += 150;
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
        if (staleActiveAction) score -= 500;

        string role = input.IsTarget ? "target" : sourceLike ? "source-like" : "context";
        return new ImmediateActionCandidateScore(
            score,
            role,
            sourceLike,
            freshActionId,
            freshActiveAction,
            staleActionId,
            staleActiveAction);
    }

    private static bool IsWithin(int ageMs, int windowMs)
        => ageMs >= 0 && ageMs <= windowMs;
}
