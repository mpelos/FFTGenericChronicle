namespace fftivc.generic.chronicle.codemod;

internal enum DclHitModel
{
    Percent,
    PhysicalContest,
    MagicEvade,
}

internal readonly record struct DclHitDecision(
    bool Hit,
    int Pct,
    int Roll,
    DclPhysicalOutcome PhysicalOutcome = DclPhysicalOutcome.Legacy,
    int AttackSkill = 0,
    DclDefenseKind DefenseKind = DclDefenseKind.None,
    int DefenseTarget = 0,
    int DefenseRoll = -1,
    DclHitModel Model = DclHitModel.Percent,
    int MagicEvade = 0,
    DclMultistrikeAggregate Multistrike = default);

// Per-target decision cache for DCL hit control. Calc-entry (0x3099AC) refires for the same
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
        int ActionPayload,
        DclHitDecision Decision,
        long TimestampTicks,
        bool DamageConsumed = false,
        bool OutcomeConsumed = false,
        bool DefenseCommitted = false);

    public bool TryGet(int targetIdx, int casterIdx, int abilityId, int actionType, int actionPayload, long nowTicks, long ttlTicks, out DclHitDecision decision)
    {
        decision = default;
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return false;

            var entry = _entries[targetIdx];
            if (entry.CasterIdx != casterIdx || entry.AbilityId != abilityId || entry.ActionType != actionType ||
                entry.ActionPayload != actionPayload)
                return false;
            if (ttlTicks <= 0 || nowTicks - entry.TimestampTicks > ttlTicks)
                return false;

            decision = entry.Decision;
            return true;
        }
    }

    public void Record(int targetIdx, int casterIdx, int abilityId, int actionType, int actionPayload, DclHitDecision decision, long nowTicks)
    {
        if ((uint)targetIdx >= SlotCount)
            return;

        lock (_gate)
        {
            _entries[targetIdx] = new Entry(casterIdx, abilityId, actionType, actionPayload, decision, nowTicks);
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

    // Selector/reaction consumers run after calc-entry and only have the defender/target pointer.
    // The cache is already one slot per target, so expose the current live entry together with its
    // key without requiring those late consumers to reconstruct caster/action state from VM frames.
    public bool TryGetLatest(
        int targetIdx,
        long nowTicks,
        long ttlTicks,
        out DclHitDecision decision,
        out int casterIdx,
        out int abilityId,
        out int actionType,
        out int actionPayload)
    {
        decision = default;
        casterIdx = -1;
        abilityId = -1;
        actionType = -1;
        actionPayload = -1;
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return false;

            var entry = _entries[targetIdx];
            if (ttlTicks <= 0 || nowTicks - entry.TimestampTicks > ttlTicks)
            {
                _hasEntry[targetIdx] = false;
                return false;
            }

            decision = entry.Decision;
            casterIdx = entry.CasterIdx;
            abilityId = entry.AbilityId;
            actionType = entry.ActionType;
            actionPayload = entry.ActionPayload;
            return true;
        }
    }

    public void Invalidate(int targetIdx, int casterIdx, int abilityId, int actionType, int actionPayload)
    {
        if ((uint)targetIdx >= SlotCount)
            return;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return;

            var entry = _entries[targetIdx];
            if (entry.CasterIdx == casterIdx && entry.AbilityId == abilityId && entry.ActionType == actionType &&
                entry.ActionPayload == actionPayload)
                _hasEntry[targetIdx] = false;
        }
    }

    // Two-consumer consumption handshake for armed output-control delivery. A forced outcome is
    // consumed by TWO independent hooks whose fire order is NOT proven (the pre-clamp staged-damage
    // apply 0x30A5D7 and a downstream outcome-delivery hook): neither may retire the entry alone or
    // the other would find nothing and fall back to the wrong behavior (full formula damage on a
    // "missed" swing, a kept hit-kind on a zeroed swing, or a decision=none log that breaks the
    // per-swing 1:1 verification). This applies to BOTH outcomes — a hit is consumed by the same
    // two sides as a miss — so neither the pre-clamp path nor the kind callback retires alone.
    // The outcome side can be the proven result selector 0x205210 or the older result-kind commit
    // 0x205B38. Each consumer marks its side; whoever completes the pair retires the entry. If one side never
    // fires (hook disabled / no staged damage), the entry lingers until TTL — exactly the
    // pre-existing V1 behavior, never worse. Returns true when this call completed the pair and
    // retired the entry.
    public bool MarkConsumed(int targetIdx, int casterIdx, int abilityId, int actionType, int actionPayload, bool byOutcomeDelivery)
    {
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return false;

            var entry = _entries[targetIdx];
            if (entry.CasterIdx != casterIdx || entry.AbilityId != abilityId || entry.ActionType != actionType ||
                entry.ActionPayload != actionPayload)
                return false;

            bool damageConsumed = entry.DamageConsumed || !byOutcomeDelivery;
            bool outcomeConsumed = entry.OutcomeConsumed || byOutcomeDelivery;
            if (damageConsumed && outcomeConsumed)
            {
                _hasEntry[targetIdx] = false;
                return true;
            }

            _entries[targetIdx] = entry with { DamageConsumed = damageConsumed, OutcomeConsumed = outcomeConsumed };
            return false;
        }
    }

    public bool TryMarkDefenseCommitted(
        int targetIdx,
        int casterIdx,
        int abilityId,
        int actionType,
        int actionPayload,
        out DclHitDecision decision)
    {
        decision = default;
        if ((uint)targetIdx >= SlotCount)
            return false;

        lock (_gate)
        {
            if (!_hasEntry[targetIdx])
                return false;

            var entry = _entries[targetIdx];
            if (entry.CasterIdx != casterIdx || entry.AbilityId != abilityId || entry.ActionType != actionType ||
                entry.ActionPayload != actionPayload ||
                entry.DefenseCommitted)
                return false;

            decision = entry.Decision;
            _entries[targetIdx] = entry with { DefenseCommitted = true };
            return true;
        }
    }
}
