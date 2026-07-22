namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclComputedNumericResult(
    int CasterIdx,
    int ActionType,
    int AbilityId,
    int ActionPayload,
    long TimestampTicks,
    int NaturalHpDebit,
    int NaturalHpCredit,
    int NaturalMpDebit,
    int NaturalMpCredit,
    byte NaturalResultFlags,
    int HpDebit,
    int HpCredit,
    int MpDebit,
    int MpCredit,
    byte ResultFlags,
    DclCanonicalNativeApplyReservation? CanonicalReservation = null);

/// <summary>
/// Holds the one consumer-visible numeric result produced for a confirmed execution target at the
/// post-calc boundary. The later pre-clamp delivery must consume this exact result instead of
/// evaluating formulas a second time against already-rewritten staged fields.
/// </summary>
internal sealed class DclComputePointCache
{
    private const int SlotCount = 64;

    private readonly object _gate = new();
    private readonly DclComputedNumericResult[] _results = new DclComputedNumericResult[SlotCount];
    private readonly bool[] _hasResult = new bool[SlotCount];

    public void Record(int targetIdx, DclComputedNumericResult result)
    {
        if ((uint)targetIdx >= SlotCount)
            return;

        lock (_gate)
        {
            if (_hasResult[targetIdx] && result.TimestampTicks < _results[targetIdx].TimestampTicks)
                return;
            _results[targetIdx] = result;
            _hasResult[targetIdx] = true;
        }
    }

    public bool TryGet(
        int targetIdx,
        int casterIdx,
        int actionType,
        int abilityId,
        int actionPayload,
        long nowTicks,
        long maxAgeTicks,
        out DclComputedNumericResult result)
    {
        result = default;
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasResult[targetIdx])
                return false;

            DclComputedNumericResult candidate = _results[targetIdx];
            if (candidate.CasterIdx != casterIdx ||
                candidate.ActionType != actionType ||
                candidate.AbilityId != abilityId ||
                candidate.ActionPayload != actionPayload)
                return false;
            if (maxAgeTicks < 0 || nowTicks - candidate.TimestampTicks > maxAgeTicks)
                return false;

            result = candidate;
            return true;
        }
    }

    public void Invalidate(int targetIdx)
    {
        if ((uint)targetIdx >= SlotCount)
            return;
        lock (_gate)
            _hasResult[targetIdx] = false;
    }

    public void Clear()
    {
        lock (_gate)
            Array.Clear(_hasResult);
    }
}
