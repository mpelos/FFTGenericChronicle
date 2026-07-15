namespace fftivc.generic.chronicle.codemod;

internal sealed class DclInstantKoRule
{
    public string Name { get; set; } = "";
    public int AbilityId { get; set; } = -1;
    public int ActionType { get; set; } = -1;
    public string ConditionFormula { get; set; } = "";
    public string ResistanceFormula { get; set; } = "";
    public bool ZeroDamageOnFailure { get; set; } = false;
    public bool NativeKoSuppressedByData { get; set; } = false;

    public string DisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"ability-{AbilityId}-instant-ko"
        : Name.Trim();

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

internal readonly record struct DclInstantKoDecision(
    string RuleName,
    bool Matched,
    bool Immune,
    int Resistance,
    int Roll,
    bool Resisted,
    bool ZeroDamageOnFailure)
{
    public bool Kills => Matched && !Immune && !Resisted;
}

internal readonly record struct DclInstantKoAssessment(
    string RuleName,
    bool Matched,
    bool Immune,
    int Resistance,
    bool ZeroDamageOnFailure)
{
    public int SuccessPermille => Matched && !Immune
        ? 1000 - DclStatusContest.ResistChancePermille(Resistance)
        : 0;
}

internal static class DclLifecycle
{
    public static int ComputeLethalDebit(int currentHp, int stagedHpCredit)
        => Math.Clamp(Math.Max(0, currentHp) + Math.Max(0, stagedHpCredit), 0, short.MaxValue);

    public static bool WouldBeLethal(int currentHp, int stagedHpCredit, int stagedHpDebit)
        => Math.Max(0, currentHp) + Math.Max(0, stagedHpCredit) - Math.Max(0, stagedHpDebit) <= 0;

    public static int ComputeExpectedInstantKoDebit(
        int currentHp,
        int stagedHpCredit,
        int failureDebit,
        DclInstantKoAssessment assessment)
    {
        int safeFailureDebit = Math.Clamp(failureDebit, 0, short.MaxValue);
        if (!assessment.Matched)
            return safeFailureDebit;

        int successPermille = assessment.SuccessPermille;
        int failurePermille = 1000 - successPermille;
        int lethalDebit = ComputeLethalDebit(currentHp, stagedHpCredit);
        long weighted = (long)lethalDebit * successPermille + (long)safeFailureDebit * failurePermille;
        return Math.Clamp((int)((weighted + 500) / 1000), 0, short.MaxValue);
    }

    public static int ComputeResolvedInstantKoDebit(
        int currentHp,
        int stagedHpCredit,
        int failureDebit,
        DclInstantKoDecision decision)
    {
        int safeFailureDebit = Math.Clamp(failureDebit, 0, short.MaxValue);
        if (!decision.Matched)
            return safeFailureDebit;
        if (decision.Kills)
            return ComputeLethalDebit(currentHp, stagedHpCredit);
        return decision.ZeroDamageOnFailure ? 0 : safeFailureDebit;
    }
}
