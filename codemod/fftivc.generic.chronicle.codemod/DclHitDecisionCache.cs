namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclHitDecision(bool Hit, int Pct, int Roll);

// Per-target decision cache for DCL hit control. Calc-entry (0x309A44) refires for the same
// (caster, target, ability, type) during preview, charge and AI evaluation; within the TTL those
// refires must reuse ONE rolled decision so the forced outcome stays stable across the whole
// action (preview, charge and execution all see the same hit/miss). A different key (new action)
// or an expired entry recomputes and re-rolls.
internal sealed class DclHitDecisionCache
{
    private const int SlotCount = 64;

    private readonly object _gate = new();
    private readonly Entry[] _entries = new Entry[SlotCount];
    private readonly bool[] _hasEntry = new bool[SlotCount];

    private readonly record struct Entry(
        int CasterIdx,
        int AbilityId,
        int ActionType,
        DclHitDecision Decision,
        long TimestampTicks,
        bool DamageConsumed = false,
        bool KindConsumed = false);

    public bool TryGet(int targetIdx, int casterIdx, int abilityId, int actionType, long nowTicks, long ttlTicks, out DclHitDecision decision)
    {
        decision = default;
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return false;

            var entry = _entries[targetIdx];
            if (entry.CasterIdx != casterIdx || entry.AbilityId != abilityId || entry.ActionType != actionType)
                return false;
            if (ttlTicks <= 0 || nowTicks - entry.TimestampTicks > ttlTicks)
                return false;

            decision = entry.Decision;
            return true;
        }
    }

    public void Record(int targetIdx, int casterIdx, int abilityId, int actionType, DclHitDecision decision, long nowTicks)
    {
        if ((uint)targetIdx >= SlotCount)
            return;

        lock (_gate)
        {
            _entries[targetIdx] = new Entry(casterIdx, abilityId, actionType, decision, nowTicks);
            _hasEntry[targetIdx] = true;
        }
    }

    public bool HasLiveDecision(int targetIdx, long nowTicks, long ttlTicks)
    {
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return false;

            if (ttlTicks <= 0 || nowTicks - _entries[targetIdx].TimestampTicks > ttlTicks)
            {
                _hasEntry[targetIdx] = false;
                return false;
            }

            return true;
        }
    }

    public void Invalidate(int targetIdx, int casterIdx, int abilityId, int actionType)
    {
        if ((uint)targetIdx >= SlotCount)
            return;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return;

            var entry = _entries[targetIdx];
            if (entry.CasterIdx == casterIdx && entry.AbilityId == abilityId && entry.ActionType == actionType)
                _hasEntry[targetIdx] = false;
        }
    }

    // Two-consumer consumption handshake for armed output-control delivery. A forced outcome is
    // consumed by TWO independent hooks whose fire order is NOT proven (the pre-clamp staged-damage
    // apply 0x30A66F and the result-kind commit 0x205B38): neither may retire the entry alone or
    // the other would find nothing and fall back to the wrong behavior (full formula damage on a
    // "missed" swing, a kept hit-kind on a zeroed swing, or a decision=none log that breaks the
    // per-swing 1:1 verification). This applies to BOTH outcomes — a hit is consumed by the same
    // two sides as a miss — so neither the pre-clamp path nor the kind callback retires alone.
    // Each consumer marks its side; whoever completes the pair retires the entry. If one side never
    // fires (hook disabled / no staged damage), the entry lingers until TTL — exactly the
    // pre-existing V1 behavior, never worse. Returns true when this call completed the pair and
    // retired the entry.
    public bool MarkConsumed(int targetIdx, int casterIdx, int abilityId, int actionType, bool byKindCommit)
    {
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return false;

            var entry = _entries[targetIdx];
            if (entry.CasterIdx != casterIdx || entry.AbilityId != abilityId || entry.ActionType != actionType)
                return false;

            bool damageConsumed = entry.DamageConsumed || !byKindCommit;
            bool kindConsumed = entry.KindConsumed || byKindCommit;
            if (damageConsumed && kindConsumed)
            {
                _hasEntry[targetIdx] = false;
                return true;
            }

            _entries[targetIdx] = entry with { DamageConsumed = damageConsumed, KindConsumed = kindConsumed };
            return false;
        }
    }
}
