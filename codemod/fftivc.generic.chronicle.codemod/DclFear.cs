namespace fftivc.generic.chronicle.codemod;

internal static class DclFearActionRecordLayout
{
    public const int CasterIndexOffset = 0x00;
    public const int ActionTypeOffset = 0x01;
    public const int AbilityIdOffset = 0x02;
}

internal readonly record struct DclFearUnit(int Index, int Team, bool IsFoe, bool Present = true);

internal readonly record struct DclFearTargetAssessment(
    int AffectedCount,
    int OpposingCount)
{
    public bool HasOpposingTarget => OpposingCount > 0;
}

internal static class DclFearPolicy
{
    public const int BattleUnitCapacity = 64;
    public const int PlayerForecastBattleState = 0x19;
    public const int AiEvaluationBattleState = 0x05;
    public const int ConfirmedExecutionBattleState = 0x2A;
    public const int NativeRepeatExecutionBattleState = 0x2F;
    public const int ReactionDeliveryBattleState = 0x2C;
    public const byte EmptyTarget = 0xFF;

    public static bool IsOpposing(DclFearUnit caster, DclFearUnit target)
        => caster.IsFoe != target.IsFoe || caster.Team != target.Team;

    public static DclFearTargetAssessment Assess(
        DclFearUnit caster,
        ReadOnlySpan<byte> targetIndices,
        IReadOnlyDictionary<int, DclFearUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);

        int affected = 0;
        int opposing = 0;
        foreach (byte rawIndex in targetIndices)
        {
            if (rawIndex == EmptyTarget ||
                !units.TryGetValue(rawIndex, out DclFearUnit target) ||
                !target.Present)
                continue;

            affected++;
            if (IsOpposing(caster, target))
                opposing++;
        }

        return new DclFearTargetAssessment(affected, opposing);
    }

    public static bool MutatesCandidate(int battleState)
        => battleState is AiEvaluationBattleState or
            ConfirmedExecutionBattleState or
            NativeRepeatExecutionBattleState;

    public static bool RejectsPlayerConfirmation(
        bool fearOwned,
        DclFearTargetAssessment assessment)
        => fearOwned && assessment.HasOpposingTarget;

    public static bool TryResolveVoluntaryCasterIndex(
        int battleState,
        int turnOwner,
        out int casterIndex)
    {
        casterIndex = -1;
        if (battleState != PlayerForecastBattleState ||
            (uint)turnOwner >= BattleUnitCapacity)
            return false;

        casterIndex = turnOwner;
        return true;
    }

    public static bool TryInvalidateCandidate(
        bool fearOwned,
        int battleState,
        DclFearTargetAssessment assessment,
        Span<byte> targetIndices)
    {
        if (!fearOwned || !assessment.HasOpposingTarget || !MutatesCandidate(battleState))
            return false;

        targetIndices.Fill(EmptyTarget);
        return true;
    }
}
