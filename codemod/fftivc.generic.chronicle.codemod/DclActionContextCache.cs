namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclActionContext(int CasterIdx, int ActionType, int AbilityId, long TimestampTicks);

internal sealed class DclActionContextCache
{
    private const int SlotCount = 64;

    private readonly object _gate = new();
    private readonly DclActionContext[] _contexts = new DclActionContext[SlotCount];
    private readonly bool[] _hasContext = new bool[SlotCount];

    public void Record(int targetIdx, int casterIdx, int actionType, int abilityId, long timestampTicks)
    {
        if ((uint)targetIdx >= SlotCount)
            return;

        lock (_gate)
        {
            if (_hasContext[targetIdx] && timestampTicks < _contexts[targetIdx].TimestampTicks)
                return;

            _contexts[targetIdx] = new DclActionContext(casterIdx, actionType, abilityId, timestampTicks);
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
}
