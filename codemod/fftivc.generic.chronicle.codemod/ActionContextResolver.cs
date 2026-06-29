using System.Diagnostics;
using System.Text;

namespace fftivc.generic.chronicle.codemod;

internal sealed record UnitObservation(UnitSnapshot Unit, long SeenTick, long CtDropTick = 0, int CtDropAmount = 0);

internal sealed record ResolvedAttacker(UnitSnapshot? Unit, string Source, string Summary);

internal sealed class BattleContextResolver
{
    private readonly RuntimeSettings _settings;
    private RecentHpDamageEvent? _lastHpDamageEvent;

    public BattleContextResolver(RuntimeSettings settings)
    {
        _settings = settings;
    }

    public ResolvedAttacker ResolveRecentAttacker(
        UnitSnapshot target,
        IReadOnlyDictionary<nint, UnitObservation> observations,
        long nowTick)
    {
        string ctSummary = "";

        // Legacy diagnostic path. CT is useful for comparing old captures, but it is not accepted
        // as DCL ownership. Shipping profiles should leave this disabled.
        if (_settings.ResolveAttackerByCt)
        {
            var byCt = ResolveByCt(target, observations, nowTick, out ctSummary);
            if (byCt is not null) return byCt;
        }

        // Legacy diagnostic fallback. Native actor/selector context is preferred for reactions;
        // this inversion only preserves old runtime traces for comparison profiles.
        if (_settings.ResolveCounterFromRecentDamage)
        {
            var byCounter = ResolveByCounterInversion(target, observations, nowTick);
            if (byCounter is not null) return byCounter;
        }

        // Fallback: legacy recency heuristic (hook-touch order). Off by default.
        var candidates = BuildCandidates(target, observations, nowTick);
        string summary = FormatSummaryParts(ctSummary, FormatCandidates(candidates));
        if (!_settings.InferAttackerFromRecentUnits || candidates.Count == 0)
            return new ResolvedAttacker(null, "none", summary);

        var best = candidates[0];
        return new ResolvedAttacker(best.Unit, "recent-unit", summary);
    }

    public void RememberHpDamageEvent(
        UnitSnapshot target,
        UnitSnapshot? attacker,
        string attackerSource,
        int signedDamage,
        long eventTick,
        long eventIndex)
    {
        if (signedDamage <= 0 || attacker is null) return;
        if (attacker.Ptr == target.Ptr) return;

        _lastHpDamageEvent = new RecentHpDamageEvent(target, attacker, attackerSource, signedDamage, eventTick, eventIndex);
    }

    // Diagnostic only: attacker = registered non-target unit whose CT (+0x41) most recently reset.
    // This is intentionally opt-in because Wait, delayed actions, and reactions make CT too fragile
    // for DCL ownership.
    private ResolvedAttacker? ResolveByCt(
        UnitSnapshot target,
        IReadOnlyDictionary<nint, UnitObservation> observations,
        long nowTick,
        out string missSummary)
    {
        missSummary = "";
        int dropWindowMs = Math.Clamp(_settings.CtDropWindowMs, 1, 60_000);
        var list = new List<CtCandidate>();
        foreach (var observation in observations.Values)
        {
            var unit = observation.Unit;
            if (unit.Ptr == target.Ptr) continue;
            if (unit.Hp <= 0) continue;
            if (observation.CtDropTick <= 0) continue;
            int dropAgeMs = AgeMs(nowTick, observation.CtDropTick);
            if (dropAgeMs < 0 || dropAgeMs > dropWindowMs) continue;
            list.Add(new CtCandidate(unit, unit.Ct, observation.CtDropTick, observation.CtDropAmount, dropAgeMs));
        }

        if (list.Count == 0)
        {
            var lowCtCandidates = BuildLowCtCandidates(target, observations, nowTick);
            if (_settings.ResolveAttackerByLowCtFallback && lowCtCandidates.Count > 0)
            {
                var bestLow = lowCtCandidates[0];
                return new ResolvedAttacker(bestLow.Unit, "ct-low", FormatSummaryParts("ctCandidates=none", FormatLowCtCandidates(lowCtCandidates)));
            }

            missSummary = FormatSummaryParts("ctCandidates=none", FormatLowCtCandidates(lowCtCandidates), FormatCtObserved(target, observations, nowTick));
            return null;
        }

        list.Sort((a, b) =>
        {
            int cmp = b.CtDropTick.CompareTo(a.CtDropTick);   // most recent CT drop first
            if (cmp != 0) return cmp;
            cmp = a.Ct.CompareTo(b.Ct);                       // tiebreak: lowest absolute CT
            if (cmp != 0) return cmp;
            cmp = b.CtDropAmount.CompareTo(a.CtDropAmount);    // tiebreak: largest drop
            if (cmp != 0) return cmp;
            return a.Unit.Ptr.ToInt64().CompareTo(b.Unit.Ptr.ToInt64());
        });

        var best = list[0];
        return new ResolvedAttacker(best.Unit, "ct-reset", FormatCtCandidates(list));
    }

    private List<LowCtCandidate> BuildLowCtCandidates(
        UnitSnapshot target,
        IReadOnlyDictionary<nint, UnitObservation> observations,
        long nowTick)
    {
        int maxCt = Math.Clamp(_settings.CtLowFallbackMaxCt, 0, 100);
        int windowMs = Math.Clamp(_settings.CtLowFallbackWindowMs, 1, 60_000);
        var list = new List<LowCtCandidate>();

        foreach (var observation in observations.Values)
        {
            var unit = observation.Unit;
            if (unit.Ptr == target.Ptr) continue;
            if (unit.Hp <= 0) continue;
            if (unit.Ct > maxCt) continue;
            if (observation.SeenTick <= 0) continue;

            int seenAgeMs = AgeMs(nowTick, observation.SeenTick);
            if (seenAgeMs < 0 || seenAgeMs > windowMs) continue;

            list.Add(new LowCtCandidate(unit, unit.Ct, seenAgeMs));
        }

        list.Sort((a, b) =>
        {
            int cmp = a.SeenAgeMs.CompareTo(b.SeenAgeMs); // most recently hook-touched first
            if (cmp != 0) return cmp;
            cmp = a.Ct.CompareTo(b.Ct);                   // tiebreak: lowest absolute CT
            if (cmp != 0) return cmp;
            return a.Unit.Ptr.ToInt64().CompareTo(b.Unit.Ptr.ToInt64());
        });

        return list;
    }

    private ResolvedAttacker? ResolveByCounterInversion(
        UnitSnapshot target,
        IReadOnlyDictionary<nint, UnitObservation> observations,
        long nowTick)
    {
        var previous = _lastHpDamageEvent;
        if (previous is null) return null;

        int windowMs = Math.Clamp(_settings.CounterEventWindowMs, 1, 10_000);
        int ageMs = AgeMs(nowTick, previous.EventTick);
        if (ageMs < 0 || ageMs > windowMs) return null;
        if (previous.Attacker.Ptr != target.Ptr) return null;
        if (previous.Target.Ptr == target.Ptr) return null;

        UnitSnapshot counterAttacker = previous.Target;
        if (observations.TryGetValue(counterAttacker.Ptr, out var observation))
            counterAttacker = observation.Unit;
        if (counterAttacker.Hp <= 0) return null;

        string summary =
            $"counterPrevious=event#{previous.EventIndex}/age={ageMs}ms/" +
            $"prevTarget=0x{previous.Target.Ptr:X}/id=0x{previous.Target.CharId:X2}/" +
            $"prevAttacker=0x{previous.Attacker.Ptr:X}/id=0x{previous.Attacker.CharId:X2}/" +
            $"prevSource={previous.AttackerSource}/damage={previous.SignedDamage}";
        return new ResolvedAttacker(counterAttacker, "counter-inversion", summary);
    }

    private List<AttackerCandidate> BuildCandidates(
        UnitSnapshot target,
        IReadOnlyDictionary<nint, UnitObservation> observations,
        long nowTick)
    {
        int windowMs = Math.Clamp(_settings.RecentAttackerWindowMs, 1, 10_000);
        var candidates = new List<AttackerCandidate>();

        foreach (var observation in observations.Values)
        {
            var unit = observation.Unit;
            if (unit.Ptr == target.Ptr) continue;
            if (unit.Hp <= 0) continue;

            int ageMs = AgeMs(nowTick, observation.SeenTick);
            if (ageMs < 0 || ageMs > windowMs) continue;

            bool opposing = unit.IsFoe != target.IsFoe || unit.Team != target.Team;
            int score = ageMs + (_settings.PreferOpposingTeamAttacker && !opposing ? 500 : 0);
            candidates.Add(new AttackerCandidate(unit, ageMs, opposing, score));
        }

        candidates.Sort((a, b) =>
        {
            int cmp = a.Score.CompareTo(b.Score);
            if (cmp != 0) return cmp;
            cmp = a.AgeMs.CompareTo(b.AgeMs);
            if (cmp != 0) return cmp;
            return a.Unit.Ptr.ToInt64().CompareTo(b.Unit.Ptr.ToInt64());
        });
        return candidates;
    }

    private string FormatCandidates(List<AttackerCandidate> candidates)
    {
        if (candidates.Count == 0) return "attackerCandidates=none";

        int max = Math.Clamp(_settings.MaxAttackerCandidatesToLog, 1, 12);
        var sb = new StringBuilder("attackerCandidates=");
        for (int i = 0; i < candidates.Count && i < max; i++)
        {
            if (i > 0) sb.Append(" ");
            var c = candidates[i];
            sb.Append($"ptr=0x{c.Unit.Ptr:X}/id=0x{c.Unit.CharId:X2}/{c.Unit.FactionLabel.Trim()}/t{c.Unit.Team}/age={c.AgeMs}ms/PA={c.Unit.Pa}");
            if (c.Opposing) sb.Append("/opposing");
        }

        if (candidates.Count > max)
            sb.Append($" ... +{candidates.Count - max} more");
        return sb.ToString();
    }

    private string FormatCtCandidates(List<CtCandidate> candidates)
    {
        int max = Math.Clamp(_settings.MaxAttackerCandidatesToLog, 1, 12);
        var sb = new StringBuilder("ctCandidates=");
        for (int i = 0; i < candidates.Count && i < max; i++)
        {
            if (i > 0) sb.Append(' ');
            var c = candidates[i];
            sb.Append($"ptr=0x{c.Unit.Ptr:X}/id=0x{c.Unit.CharId:X2}/{c.Unit.FactionLabel.Trim()}/t{c.Unit.Team}/CT={c.Ct}/dropped{c.CtDropAmount}@{c.DropAgeMs}ms/PA={c.Unit.Pa}");
        }

        if (candidates.Count > max)
            sb.Append($" ... +{candidates.Count - max} more");
        return sb.ToString();
    }

    private string FormatLowCtCandidates(List<LowCtCandidate> candidates)
    {
        if (candidates.Count == 0) return "ctLowCandidates=none";

        int max = Math.Clamp(_settings.MaxAttackerCandidatesToLog, 1, 12);
        var sb = new StringBuilder("ctLowCandidates=");
        for (int i = 0; i < candidates.Count && i < max; i++)
        {
            if (i > 0) sb.Append(' ');
            var c = candidates[i];
            sb.Append($"ptr=0x{c.Unit.Ptr:X}/id=0x{c.Unit.CharId:X2}/{c.Unit.FactionLabel.Trim()}/t{c.Unit.Team}/CT={c.Ct}/seen={c.SeenAgeMs}ms/PA={c.Unit.Pa}");
        }

        if (candidates.Count > max)
            sb.Append($" ... +{candidates.Count - max} more");
        return sb.ToString();
    }

    private string FormatCtObserved(
        UnitSnapshot target,
        IReadOnlyDictionary<nint, UnitObservation> observations,
        long nowTick)
    {
        var list = new List<CtObserved>();
        foreach (var observation in observations.Values)
        {
            var unit = observation.Unit;
            if (unit.Ptr == target.Ptr) continue;
            if (unit.Hp <= 0) continue;
            int seenAgeMs = observation.SeenTick > 0 ? AgeMs(nowTick, observation.SeenTick) : -1;
            int dropAgeMs = observation.CtDropTick > 0 ? AgeMs(nowTick, observation.CtDropTick) : -1;
            list.Add(new CtObserved(unit, unit.Ct, seenAgeMs, dropAgeMs, observation.CtDropAmount));
        }

        if (list.Count == 0) return "ctObserved=none";

        list.Sort((a, b) =>
        {
            int aRank = a.DropAgeMs >= 0 ? a.DropAgeMs : int.MaxValue;
            int bRank = b.DropAgeMs >= 0 ? b.DropAgeMs : int.MaxValue;
            int cmp = aRank.CompareTo(bRank);
            if (cmp != 0) return cmp;
            cmp = a.Ct.CompareTo(b.Ct);
            if (cmp != 0) return cmp;
            return a.Unit.Ptr.ToInt64().CompareTo(b.Unit.Ptr.ToInt64());
        });

        int max = Math.Clamp(_settings.MaxAttackerCandidatesToLog, 1, 12);
        var sb = new StringBuilder("ctObserved=");
        for (int i = 0; i < list.Count && i < max; i++)
        {
            if (i > 0) sb.Append(' ');
            var c = list[i];
            sb.Append($"ptr=0x{c.Unit.Ptr:X}/id=0x{c.Unit.CharId:X2}/{c.Unit.FactionLabel.Trim()}/t{c.Unit.Team}/CT={c.Ct}");
            sb.Append(c.DropAgeMs >= 0 ? $"/drop={c.DropAgeMs}ms:{c.CtDropAmount}" : "/drop=none");
            sb.Append(c.SeenAgeMs >= 0 ? $"/seen={c.SeenAgeMs}ms" : "/seen=none");
            sb.Append($"/PA={c.Unit.Pa}");
        }

        if (list.Count > max)
            sb.Append($" ... +{list.Count - max} more");
        return sb.ToString();
    }

    private static string FormatSummaryParts(params string[] parts)
        => string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static int AgeMs(long nowTick, long seenTick)
        => (int)Math.Round((nowTick - seenTick) * 1000.0 / Stopwatch.Frequency);

    private sealed record AttackerCandidate(UnitSnapshot Unit, int AgeMs, bool Opposing, int Score);

    private sealed record CtCandidate(UnitSnapshot Unit, int Ct, long CtDropTick, int CtDropAmount, int DropAgeMs);

    private sealed record LowCtCandidate(UnitSnapshot Unit, int Ct, int SeenAgeMs);

    private sealed record CtObserved(UnitSnapshot Unit, int Ct, int SeenAgeMs, int DropAgeMs, int CtDropAmount);

    private sealed record RecentHpDamageEvent(
        UnitSnapshot Target,
        UnitSnapshot Attacker,
        string AttackerSource,
        int SignedDamage,
        long EventTick,
        long EventIndex);
}
