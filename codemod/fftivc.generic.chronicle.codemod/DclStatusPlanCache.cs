namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclPreparedStatusPlan(
    int CasterIdx,
    int ActionType,
    int AbilityId,
    long TimestampTicks,
    IReadOnlyList<DclStatusWrite> Writes);

internal sealed class DclStatusPlanCache
{
    private const int SlotCount = 64;
    private readonly object _gate = new();
    private readonly DclPreparedStatusPlan[] _plans = new DclPreparedStatusPlan[SlotCount];
    private readonly bool[] _hasPlan = new bool[SlotCount];

    public void Record(int targetIdx, DclPreparedStatusPlan plan)
    {
        if ((uint)targetIdx >= SlotCount)
            return;
        lock (_gate)
        {
            _plans[targetIdx] = plan;
            _hasPlan[targetIdx] = true;
        }
    }

    public bool TryGet(
        int targetIdx,
        int casterIdx,
        int actionType,
        int abilityId,
        long nowTicks,
        long maxAgeTicks,
        out DclPreparedStatusPlan plan)
    {
        plan = default;
        if ((uint)targetIdx >= SlotCount)
            return false;
        lock (_gate)
        {
            if (!_hasPlan[targetIdx])
                return false;
            var candidate = _plans[targetIdx];
            if (candidate.CasterIdx != casterIdx || candidate.ActionType != actionType ||
                candidate.AbilityId != abilityId || maxAgeTicks >= 0 && nowTicks - candidate.TimestampTicks > maxAgeTicks)
                return false;
            plan = candidate;
            return true;
        }
    }

    public void Invalidate(int targetIdx, int casterIdx, int actionType, int abilityId)
    {
        if ((uint)targetIdx >= SlotCount)
            return;
        lock (_gate)
        {
            if (!_hasPlan[targetIdx])
                return;
            var candidate = _plans[targetIdx];
            if (candidate.CasterIdx == casterIdx && candidate.ActionType == actionType && candidate.AbilityId == abilityId)
                _hasPlan[targetIdx] = false;
        }
    }
}
