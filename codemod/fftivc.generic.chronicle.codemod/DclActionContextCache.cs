namespace fftivc.generic.chronicle.codemod;

internal enum DclCalcOrigin
{
    Unknown,
    OuterSweep,
    NestedRendAttack,
    ForecastTrace,
}

internal readonly record struct DclActionContext(
    int CasterIdx,
    int ActionType,
    int AbilityId,
    long TimestampTicks,
    DclCalcOrigin Origin = DclCalcOrigin.Unknown,
    long ReturnRva = 0,
    int BattleState = -1,
    int SourceIdx = -1,
    long ForecastPtr = 0,
    int ActionPayload = -1,
    int ActiveWeaponItemId = -1,
    int NativeRepeatCount = -1,
    int NativeRepeatIndex = -1,
    int NativeRightWeaponItemId = -1,
    int NativeLeftWeaponItemId = -1)
{
    public bool IsConfirmedExecution =>
        Origin == DclCalcOrigin.OuterSweep && DclCalcProvenance.IsExecutionBattleState(BattleState);
}

internal static class DclCalcProvenance
{
    public const long OuterSweepReturnRva = 0x281F12;
    public const long NestedRendAttackReturnRva = 0x307ED5;
    public const long ForecastTraceReturnRva = 0xEF53F14;
    public const int AiEvaluationBattleState = 0x05;
    public const int ConfirmedExecutionBattleState = 0x2A;
    public const int NativeRepeatExecutionBattleState = 0x2F;

    public static bool IsExecutionBattleState(int battleState)
        => battleState is ConfirmedExecutionBattleState or NativeRepeatExecutionBattleState;

    public static DclCalcOrigin Classify(long returnRva)
        => returnRva switch
        {
            OuterSweepReturnRva => DclCalcOrigin.OuterSweep,
            NestedRendAttackReturnRva => DclCalcOrigin.NestedRendAttack,
            ForecastTraceReturnRva => DclCalcOrigin.ForecastTrace,
            _ => DclCalcOrigin.Unknown,
        };

    public static string Name(DclCalcOrigin origin)
        => origin switch
        {
            DclCalcOrigin.OuterSweep => "outer-sweep",
            DclCalcOrigin.NestedRendAttack => "nested-rend-attack",
            DclCalcOrigin.ForecastTrace => "forecast-trace",
            _ => "unknown",
        };
}

internal static class CalcEntryProbeAddressing
{
    public const int UnitSlotCount = 64;
    public const int UnitStride = 0x200;
    public const int CalcRecordOffset = 0x1A0;

    public static bool TryGetCasterSlot(long calcRecord, long unitTable, out int casterSlot)
    {
        casterSlot = -1;

        try
        {
            long relative = checked(calcRecord - unitTable);
            if (relative < CalcRecordOffset)
                return false;

            long slotRelative = relative - CalcRecordOffset;
            if (slotRelative % UnitStride != 0)
                return false;

            long candidate = slotRelative / UnitStride;
            if ((ulong)candidate >= UnitSlotCount)
                return false;

            casterSlot = (int)candidate;
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}

internal sealed class DclActionContextCache
{
    private const int SlotCount = 64;

    private readonly object _gate = new();
    private readonly DclActionContext[] _contexts = new DclActionContext[SlotCount];
    private readonly bool[] _hasContext = new bool[SlotCount];

    public void Record(
        int targetIdx,
        int casterIdx,
        int actionType,
        int abilityId,
        long timestampTicks,
        long returnRva = 0,
        int battleState = -1,
        int sourceIdx = -1,
        long forecastPtr = 0,
        int actionPayload = -1,
        int activeWeaponItemId = -1,
        int nativeRepeatCount = -1,
        int nativeRepeatIndex = -1,
        int nativeRightWeaponItemId = -1,
        int nativeLeftWeaponItemId = -1)
    {
        if ((uint)targetIdx >= SlotCount)
            return;

        var origin = DclCalcProvenance.Classify(returnRva);

        lock (_gate)
        {
            // Formula 0x25 temporarily rewrites the outer Rend order to synthetic Attack and re-enters
            // computeActionResult. That inner identity is an implementation detail, not the action that
            // the later apply window must attribute. The outer row was published first, so refusing the
            // nested overwrite preserves Rend without inventing a second result owner.
            if (origin == DclCalcOrigin.NestedRendAttack)
                return;

            if (_hasContext[targetIdx] && timestampTicks < _contexts[targetIdx].TimestampTicks)
                return;

            _contexts[targetIdx] = new DclActionContext(
                casterIdx,
                actionType,
                abilityId,
                timestampTicks,
                origin,
                returnRva,
                battleState,
                sourceIdx,
                forecastPtr,
                actionPayload,
                activeWeaponItemId,
                nativeRepeatCount,
                nativeRepeatIndex,
                nativeRightWeaponItemId,
                nativeLeftWeaponItemId);
            _hasContext[targetIdx] = true;
        }
    }

    public bool TryGetLatest(int targetIdx, long nowTicks, long maxAgeTicks, out DclActionContext ctx)
    {
        ctx = default;
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasContext[targetIdx])
                return false;

            var candidate = _contexts[targetIdx];
            if (maxAgeTicks < 0 || nowTicks - candidate.TimestampTicks > maxAgeTicks)
                return false;

            ctx = candidate;
            return true;
        }
    }

    public bool TryGetLatestConfirmedExecution(
        int targetIdx,
        long nowTicks,
        long maxAgeTicks,
        out DclActionContext ctx)
    {
        if (!TryGetLatest(targetIdx, nowTicks, maxAgeTicks, out ctx))
            return false;
        if (ctx.IsConfirmedExecution)
            return true;
        ctx = default;
        return false;
    }

    public void Clear()
    {
        lock (_gate)
            Array.Clear(_hasContext);
    }
}
