using System.Diagnostics;

namespace fftivc.generic.chronicle.codemod;

internal sealed record ActionProbeState(
    int PendingFlag,
    int PendingTimer,
    int ActionId,
    int ForecastDamage,
    int ForecastCharge,
    int ForecastFlag,
    int PendingFlag2,
    int ActiveMarker,
    int ActiveMarker2,
    int PhaseMarker)
{
    public static ActionProbeState From(UnitSnapshot unit)
        => new(
            unit.ReadByte(0x61),
            unit.ReadByte(0x18D),
            unit.ReadUInt16(0x1A2),
            unit.ReadUInt16(0x1C4),
            unit.ReadByte(0x1D8),
            unit.ReadByte(0x1E5),
            unit.ReadByte(0x1EF),
            unit.ReadByte(0x1B8),
            unit.ReadByte(0x1BA),
            unit.ReadByte(0x1BB));

    private const int PendingActionBit = 0x08;

    public bool HasPrimaryPendingFlag => (PendingFlag & PendingActionBit) != 0;
    public bool HasSecondaryPendingFlag => (PendingFlag2 & PendingActionBit) != 0;

    public bool IsLivePendingAction => HasPrimaryPendingFlag && HasSecondaryPendingFlag && ActionId > 0;

    public bool IsClearedPendingAction(int actionId)
        => actionId > 0 &&
           ActionId == actionId &&
           !HasPrimaryPendingFlag &&
           !HasSecondaryPendingFlag &&
           PendingTimer == 0xFF;

    public bool LooksRelevant =>
        PendingFlag != 0 ||
        PendingTimer != 0xFF ||
        ActionId != 0 ||
        ForecastDamage != 0 ||
        ForecastCharge != 0 ||
        ForecastFlag != 0 ||
        PendingFlag2 != 0 ||
        ActiveMarker != 0 ||
        ActiveMarker2 != 0 ||
        PhaseMarker != 0;

    public string Key => string.Join('/',
        PendingFlag,
        PendingTimer,
        ActionId,
        ForecastDamage,
        ForecastCharge,
        ForecastFlag,
        PendingFlag2,
        ActiveMarker,
        ActiveMarker2,
        PhaseMarker);

    public string PendingFields =>
        $"s61={PendingFlag}/t18D={PendingTimer}/act={ActionId}/f1EF={PendingFlag2}";

    public string TargetCacheFields =>
        $"dmg1C4={ForecastDamage}/chg1D8={ForecastCharge}/f1E5={ForecastFlag}/bb={PhaseMarker}";

    public string AllFields =>
        $"{PendingFields}/dmg1C4={ForecastDamage}/chg1D8={ForecastCharge}/f1E5={ForecastFlag}" +
        $"/b8={ActiveMarker}/ba={ActiveMarker2}/bb={PhaseMarker}";
}

internal sealed record PendingActionMatchResult(IReadOnlyList<string> Lines, PendingActionMatch? Match);

internal sealed record PendingActionMatch(
    UnitSnapshot Caster,
    string Source,
    long BatchId,
    int ActionId,
    int BatchAgeMs,
    int BatchEvent,
    int MaxBatchEvents,
    string Confidence,
    int Score,
    int ObservedHpLoss,
    bool CurrentDamageCacheMatches,
    bool RecentDamageCacheMatches,
    bool CurrentDamageCacheExactMatches,
    bool RecentDamageCacheExactMatches,
    bool CurrentDamageCacheLethalClampMatches,
    bool RecentDamageCacheLethalClampMatches,
    bool HasCurrentTargetMetadata,
    int CurrentTargetCacheDamage,
    int RecentTargetCacheDamage)
{
    public bool HasDamageCacheMatch => CurrentDamageCacheMatches || RecentDamageCacheMatches;
}

internal sealed class PendingActionTracker
{
    private readonly Dictionary<nint, PendingActionRecord> _pendingByCaster = new();
    private readonly Dictionary<nint, TargetCacheRecord> _targetCacheByUnit = new();
    private readonly List<ResolvingActionBatch> _resolvingBatches = new();
    private long _nextBatchId = 1;

    public IReadOnlyList<string> ObserveUnit(
        UnitSnapshot unit,
        ActionProbeState state,
        RuntimeSettings settings,
        long nowTick,
        bool touchForContext)
    {
        var lines = new List<string>();
        Prune(settings, nowTick, lines);
        ObserveTargetCache(unit, state, nowTick, touchForContext, lines);

        if (unit.Hp <= 0)
        {
            ForgetPendingActionOwner(unit.Ptr, lines, "unit-dead");
            return lines;
        }

        if (state.IsLivePendingAction)
        {
            if (!_pendingByCaster.TryGetValue(unit.Ptr, out var pending) ||
                pending.ActionId != state.ActionId)
            {
                _pendingByCaster[unit.Ptr] = new PendingActionRecord(unit, state, nowTick, nowTick);
                lines.Add(
                    $"[PENDING-ACTION-TRACK enter caster=0x{unit.Ptr:X}/id=0x{unit.CharId:X2} " +
                    $"act={state.ActionId} now={nowTick} touch={(touchForContext ? 1 : 0)} {state.AllFields}]");
                return lines;
            }

            if (pending.LastState.PendingTimer != state.PendingTimer ||
                pending.LastState.PendingFlag != state.PendingFlag ||
                pending.LastState.PendingFlag2 != state.PendingFlag2)
            {
                lines.Add(
                    $"[PENDING-ACTION-TRACK update caster=0x{unit.Ptr:X}/id=0x{unit.CharId:X2} " +
                    $"act={state.ActionId} age={AgeMs(nowTick, pending.StartTick)}ms " +
                    $"prev={pending.LastState.PendingFields} next={state.PendingFields} " +
                    $"touch={(touchForContext ? 1 : 0)}]");
            }

            pending.Caster = unit;
            pending.LastState = state;
            pending.LastSeenTick = nowTick;
            return lines;
        }

        if (!_pendingByCaster.TryGetValue(unit.Ptr, out var existing))
            return lines;

        if (state.IsClearedPendingAction(existing.ActionId))
        {
            _pendingByCaster.Remove(unit.Ptr);
            var batch = new ResolvingActionBatch(
                _nextBatchId++,
                unit,
                existing.ActionId,
                existing.StartTick,
                existing.LastSeenTick,
                nowTick,
                existing.LastState.PendingTimer,
                existing.LastState);
            _resolvingBatches.Add(batch);
            lines.Add(
                $"[PENDING-ACTION-TRACK resolve-open batch={batch.BatchId} " +
                $"caster=0x{unit.Ptr:X}/id=0x{unit.CharId:X2} act={batch.ActionId} " +
                $"pendingAge={AgeMs(nowTick, existing.StartTick)}ms " +
                $"lastSeen={AgeMs(nowTick, existing.LastSeenTick)}ms lastTimer={batch.LastPendingTimer} " +
                $"now={nowTick} clear={state.AllFields}]");
            return lines;
        }

        if (state.ActionId != existing.ActionId && state.ActionId > 0)
        {
            _pendingByCaster.Remove(unit.Ptr);
            lines.Add(
                $"[PENDING-ACTION-TRACK abandon caster=0x{unit.Ptr:X}/id=0x{unit.CharId:X2} " +
                $"oldAct={existing.ActionId} newAct={state.ActionId} reason=action-changed]");
        }

        return lines;
    }

    public PendingActionMatchResult MatchHpEvent(
        string kind,
        long eventIndex,
        UnitSnapshot target,
        ActionProbeState? targetState,
        int observedHpLoss,
        RuntimeSettings settings,
        long nowTick)
    {
        var lines = new List<string>();
        Prune(settings, nowTick, lines);

        int windowMs = Math.Clamp(settings.PendingActionResolveWindowMs, 1, 60_000);
        int maxEvents = Math.Clamp(settings.PendingActionMaxBatchEvents, 1, 64);
        var cacheEvidence = BuildTargetCacheEvidence(target, targetState, observedHpLoss, settings, nowTick);
        var active = _resolvingBatches
            .Where(batch => !batch.Closed &&
                            batch.Events < maxEvents &&
                            AgeMs(nowTick, batch.OpenTick) <= windowMs)
            .ToList();

        if (active.Count == 0)
        {
            lines.Add(
                $"[PENDING-ACTION-MATCH kind={kind} event={eventIndex} target=0x{target.Ptr:X}/id=0x{target.CharId:X2} " +
                $"resolved=none activeBatches=0 trackedPending={_pendingByCaster.Count} " +
                $"trackedResolving={_resolvingBatches.Count} observed={observedHpLoss} {cacheEvidence.Details}]");
            return new PendingActionMatchResult(lines, null);
        }

        var best = active
            .Select(batch => new { Batch = batch, Score = Score(batch, cacheEvidence, nowTick) })
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Batch.OpenTick)
            .First();

        best.Batch.Events++;
        string activeSummary = SummarizeActiveBatches(active, nowTick);
        int ageMs = AgeMs(nowTick, best.Batch.OpenTick);
        var match = new PendingActionMatch(
            best.Batch.Caster,
            "pending-clear",
            best.Batch.BatchId,
            best.Batch.ActionId,
            ageMs,
            best.Batch.Events,
            maxEvents,
            cacheEvidence.Confidence,
            best.Score,
            observedHpLoss,
            cacheEvidence.CurrentMatch,
            cacheEvidence.RecentMatch,
            cacheEvidence.CurrentExactMatch,
            cacheEvidence.RecentExactMatch,
            cacheEvidence.CurrentLethalClampMatch,
            cacheEvidence.RecentLethalClampMatch,
            cacheEvidence.HasCurrentTargetMetadata,
            cacheEvidence.CurrentDamage,
            cacheEvidence.RecentDamage);
        lines.Add(
            $"[PENDING-ACTION-MATCH kind={kind} event={eventIndex} target=0x{target.Ptr:X}/id=0x{target.CharId:X2} " +
            $"resolved=0x{best.Batch.Caster.Ptr:X}/id=0x{best.Batch.Caster.CharId:X2} " +
            $"source=pending-clear batch={best.Batch.BatchId} act={best.Batch.ActionId} " +
            $"batchAge={ageMs}ms batchEvent={best.Batch.Events}/{maxEvents} " +
            $"confidence={cacheEvidence.Confidence} score={best.Score} observed={observedHpLoss} " +
            $"activeBatches={active.Count} trackedPending={_pendingByCaster.Count} " +
            $"batches={activeSummary} {cacheEvidence.Details}]");

        if (best.Batch.Events >= maxEvents)
        {
            best.Batch.Closed = true;
            lines.Add(
                $"[PENDING-ACTION-TRACK resolve-close batch={best.Batch.BatchId} " +
                $"act={best.Batch.ActionId} reason=max-events events={best.Batch.Events}]");
        }

        return new PendingActionMatchResult(lines, match);
    }

    public IReadOnlyList<string> ForgetUnit(nint unitPtr)
    {
        var lines = new List<string>();
        ForgetUnit(unitPtr, lines, "unit-lost");
        return lines;
    }

    public void Reset()
    {
        _pendingByCaster.Clear();
        _targetCacheByUnit.Clear();
        _resolvingBatches.Clear();
        _nextBatchId = 1;
    }

    private void ObserveTargetCache(
        UnitSnapshot unit,
        ActionProbeState state,
        long nowTick,
        bool touchForContext,
        List<string> lines)
    {
        bool hasDamageCache = state.ForecastDamage > 0;
        if (hasDamageCache)
        {
            string key = state.TargetCacheFields;
            if (!_targetCacheByUnit.TryGetValue(unit.Ptr, out var existing))
            {
                _targetCacheByUnit[unit.Ptr] = new TargetCacheRecord(unit, state, nowTick, nowTick, key);
                lines.Add(
                    $"[PENDING-ACTION-TARGET enter target=0x{unit.Ptr:X}/id=0x{unit.CharId:X2} " +
                    $"now={nowTick} touch={(touchForContext ? 1 : 0)} {state.TargetCacheFields}]");
                return;
            }

            if (existing.Cleared)
            {
                int clearAge = existing.ClearTick > 0 ? AgeMs(nowTick, existing.ClearTick) : -1;
                lines.Add(
                    $"[PENDING-ACTION-TARGET reenter target=0x{unit.Ptr:X}/id=0x{unit.CharId:X2} " +
                    $"age={AgeMs(nowTick, existing.StartTick)}ms clearAge={clearAge}ms " +
                    $"prev={existing.LastState.TargetCacheFields} next={state.TargetCacheFields} " +
                    $"touch={(touchForContext ? 1 : 0)}]");
            }
            else if (existing.Key != key)
            {
                lines.Add(
                    $"[PENDING-ACTION-TARGET update target=0x{unit.Ptr:X}/id=0x{unit.CharId:X2} " +
                    $"age={AgeMs(nowTick, existing.StartTick)}ms prev={existing.LastState.TargetCacheFields} " +
                    $"next={state.TargetCacheFields} touch={(touchForContext ? 1 : 0)}]");
            }

            existing.Target = unit;
            existing.LastState = state;
            existing.LastSeenTick = nowTick;
            existing.Key = key;
            existing.Cleared = false;
            existing.ClearTick = 0;
            return;
        }

        if (_targetCacheByUnit.TryGetValue(unit.Ptr, out var cache) && !cache.Cleared)
        {
            cache.Cleared = true;
            cache.ClearTick = nowTick;
            lines.Add(
                $"[PENDING-ACTION-TARGET clear target=0x{unit.Ptr:X}/id=0x{unit.CharId:X2} " +
                $"age={AgeMs(nowTick, cache.StartTick)}ms lastSeen={AgeMs(nowTick, cache.LastSeenTick)}ms " +
                $"prev={cache.LastState.TargetCacheFields} touch={(touchForContext ? 1 : 0)}]");
        }
    }

    private void ForgetUnit(nint unitPtr, List<string> lines, string reason)
    {
        ForgetPendingActionOwner(unitPtr, lines, reason);
        _targetCacheByUnit.Remove(unitPtr);
    }

    private void ForgetPendingActionOwner(nint unitPtr, List<string> lines, string reason)
    {
        if (_pendingByCaster.Remove(unitPtr, out var pending))
        {
            lines.Add(
                $"[PENDING-ACTION-TRACK abandon caster=0x{unitPtr:X}/id=0x{pending.Caster.CharId:X2} " +
                $"act={pending.ActionId} reason={reason}]");
        }

        foreach (var batch in _resolvingBatches.Where(batch => batch.Caster.Ptr == unitPtr && !batch.Closed))
        {
            batch.Closed = true;
            lines.Add(
                $"[PENDING-ACTION-TRACK resolve-close batch={batch.BatchId} " +
                $"act={batch.ActionId} reason={reason} events={batch.Events}]");
        }
    }

    private void Prune(RuntimeSettings settings, long nowTick, List<string> lines)
    {
        int staleMs = Math.Clamp(settings.PendingActionStaleMs, 1, 300_000);
        foreach (var (ptr, pending) in _pendingByCaster.ToArray())
        {
            if (AgeMs(nowTick, pending.LastSeenTick) <= staleMs) continue;
            _pendingByCaster.Remove(ptr);
            lines.Add(
                $"[PENDING-ACTION-TRACK abandon caster=0x{ptr:X}/id=0x{pending.Caster.CharId:X2} " +
                $"act={pending.ActionId} reason=stale age={AgeMs(nowTick, pending.LastSeenTick)}ms]");
        }

        foreach (var (ptr, cache) in _targetCacheByUnit.ToArray())
        {
            long comparisonTick = cache.Cleared && cache.ClearTick > 0 ? cache.ClearTick : cache.LastSeenTick;
            if (AgeMs(nowTick, comparisonTick) <= staleMs) continue;
            _targetCacheByUnit.Remove(ptr);
            lines.Add(
                $"[PENDING-ACTION-TARGET drop target=0x{ptr:X}/id=0x{cache.Target.CharId:X2} " +
                $"reason=stale age={AgeMs(nowTick, comparisonTick)}ms last={cache.LastState.TargetCacheFields}]");
        }

        int windowMs = Math.Clamp(settings.PendingActionResolveWindowMs, 1, 60_000);
        foreach (var batch in _resolvingBatches.Where(batch => !batch.Closed).ToArray())
        {
            int ageMs = AgeMs(nowTick, batch.OpenTick);
            if (ageMs <= windowMs) continue;
            batch.Closed = true;
            lines.Add(
                $"[PENDING-ACTION-TRACK resolve-close batch={batch.BatchId} " +
                $"act={batch.ActionId} reason=expired age={ageMs}ms events={batch.Events}]");
        }

        _resolvingBatches.RemoveAll(batch => batch.Closed && AgeMs(nowTick, batch.OpenTick) > windowMs * 2);
    }

    private static int Score(ResolvingActionBatch batch, TargetCacheEvidence cacheEvidence, long nowTick)
    {
        int score = 100_000 - Math.Clamp(AgeMs(nowTick, batch.OpenTick), 0, 100_000);
        if (cacheEvidence.CurrentMatch)
            score += 1_000_000;
        else if (cacheEvidence.RecentMatch)
            score += 500_000;
        else if (cacheEvidence.HasCurrentTargetMetadata)
            score += 10_000;
        return score;
    }

    private TargetCacheEvidence BuildTargetCacheEvidence(
        UnitSnapshot target,
        ActionProbeState? targetState,
        int observedHpLoss,
        RuntimeSettings settings,
        long nowTick)
    {
        string current = targetState?.TargetCacheFields ?? "targetCache=unavailable";
        int currentDamage = targetState?.ForecastDamage ?? 0;
        bool currentExactMatch = targetState is not null &&
                                 targetState.ForecastDamage > 0 &&
                                 targetState.ForecastDamage == observedHpLoss;
        bool currentLethalClampMatch = targetState is not null &&
                                       IsLethalClampDamageCacheMatch(target.Hp, observedHpLoss, targetState.ForecastDamage);
        bool currentMatch = currentExactMatch || currentLethalClampMatch;
        bool hasCurrentMetadata = targetState is not null &&
                                  (targetState.ForecastCharge != 0 ||
                                   targetState.ForecastFlag != 0 ||
                                   targetState.PhaseMarker != 0);

        int staleMs = Math.Clamp(settings.PendingActionStaleMs, 1, 300_000);
        bool recentMatch = false;
        bool recentExactMatch = false;
        bool recentLethalClampMatch = false;
        int recentDamage = 0;
        string recent = "recentCache=none";
        if (_targetCacheByUnit.TryGetValue(target.Ptr, out var cache))
        {
            int lastSeenAge = AgeMs(nowTick, cache.LastSeenTick);
            int clearAge = cache.Cleared && cache.ClearTick > 0 ? AgeMs(nowTick, cache.ClearTick) : -1;
            bool recentEnough = lastSeenAge <= staleMs || (clearAge >= 0 && clearAge <= staleMs);
            recentExactMatch = recentEnough &&
                               cache.LastState.ForecastDamage > 0 &&
                               cache.LastState.ForecastDamage == observedHpLoss;
            recentLethalClampMatch = recentEnough &&
                                     IsLethalClampDamageCacheMatch(target.Hp, observedHpLoss, cache.LastState.ForecastDamage);
            recentMatch = recentExactMatch || recentLethalClampMatch;
            recentDamage = cache.LastState.ForecastDamage;
            recent = $"recentCache={cache.LastState.TargetCacheFields}/lastSeenAge={lastSeenAge}ms" +
                     (clearAge >= 0 ? $"/clearAge={clearAge}ms" : "/clearAge=live") +
                     $"/match={(recentMatch ? 1 : 0)}/exact={(recentExactMatch ? 1 : 0)}/lethalClamp={(recentLethalClampMatch ? 1 : 0)}";
        }

        string confidence = currentExactMatch
            ? "damage-cache"
            : recentExactMatch
                ? "recent-damage-cache"
                : currentLethalClampMatch
                    ? "damage-cache-lethal-clamp"
                    : recentLethalClampMatch
                        ? "recent-damage-cache-lethal-clamp"
                        : "recent-resolve";
        string details = $"currentCache={current}/match={(currentMatch ? 1 : 0)}/exact={(currentExactMatch ? 1 : 0)}/lethalClamp={(currentLethalClampMatch ? 1 : 0)} {recent}";
        return new TargetCacheEvidence(
            currentMatch,
            recentMatch,
            currentExactMatch,
            recentExactMatch,
            currentLethalClampMatch,
            recentLethalClampMatch,
            hasCurrentMetadata,
            confidence,
            details,
            currentDamage,
            recentDamage);
    }

    private static bool IsLethalClampDamageCacheMatch(int currentHp, int observedHpLoss, int cachedDamage)
        => currentHp == 0 &&
           observedHpLoss > 0 &&
           cachedDamage >= observedHpLoss;

    private static string SummarizeActiveBatches(IReadOnlyList<ResolvingActionBatch> active, long nowTick)
    {
        if (active.Count == 0) return "none";
        var parts = active
            .Take(6)
            .Select(batch =>
                $"#{batch.BatchId}:0x{batch.Caster.Ptr:X}/id=0x{batch.Caster.CharId:X2}/act={batch.ActionId}/age={AgeMs(nowTick, batch.OpenTick)}ms/events={batch.Events}");
        string summary = string.Join(";", parts);
        if (active.Count > 6)
            summary += $";+{active.Count - 6}";
        return summary;
    }

    private static int AgeMs(long nowTick, long previousTick)
        => (int)Math.Round((nowTick - previousTick) * 1000.0 / Stopwatch.Frequency);

    private sealed class PendingActionRecord
    {
        public PendingActionRecord(UnitSnapshot caster, ActionProbeState state, long startTick, long lastSeenTick)
        {
            Caster = caster;
            ActionId = state.ActionId;
            StartTick = startTick;
            LastSeenTick = lastSeenTick;
            LastState = state;
        }

        public UnitSnapshot Caster { get; set; }
        public int ActionId { get; }
        public long StartTick { get; }
        public long LastSeenTick { get; set; }
        public ActionProbeState LastState { get; set; }
    }

    private sealed class TargetCacheRecord
    {
        public TargetCacheRecord(UnitSnapshot target, ActionProbeState state, long startTick, long lastSeenTick, string key)
        {
            Target = target;
            LastState = state;
            StartTick = startTick;
            LastSeenTick = lastSeenTick;
            Key = key;
        }

        public UnitSnapshot Target { get; set; }
        public ActionProbeState LastState { get; set; }
        public long StartTick { get; }
        public long LastSeenTick { get; set; }
        public string Key { get; set; }
        public bool Cleared { get; set; }
        public long ClearTick { get; set; }
    }

    private sealed record TargetCacheEvidence(
        bool CurrentMatch,
        bool RecentMatch,
        bool CurrentExactMatch,
        bool RecentExactMatch,
        bool CurrentLethalClampMatch,
        bool RecentLethalClampMatch,
        bool HasCurrentTargetMetadata,
        string Confidence,
        string Details,
        int CurrentDamage,
        int RecentDamage);

    private sealed class ResolvingActionBatch
    {
        public ResolvingActionBatch(
            long batchId,
            UnitSnapshot caster,
            int actionId,
            long pendingStartTick,
            long pendingLastSeenTick,
            long openTick,
            int lastPendingTimer,
            ActionProbeState lastPendingState)
        {
            BatchId = batchId;
            Caster = caster;
            ActionId = actionId;
            PendingStartTick = pendingStartTick;
            PendingLastSeenTick = pendingLastSeenTick;
            OpenTick = openTick;
            LastPendingTimer = lastPendingTimer;
            LastPendingState = lastPendingState;
        }

        public long BatchId { get; }
        public UnitSnapshot Caster { get; }
        public int ActionId { get; }
        public long PendingStartTick { get; }
        public long PendingLastSeenTick { get; }
        public long OpenTick { get; }
        public int LastPendingTimer { get; }
        public ActionProbeState LastPendingState { get; }
        public int Events { get; set; }
        public bool Closed { get; set; }
    }
}
