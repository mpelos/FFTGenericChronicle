using System.Diagnostics;
using System.Text;

namespace fftivc.generic.chronicle.codemod;

internal sealed record UnitObservation(UnitSnapshot Unit, long SeenTick);

internal sealed record ResolvedAttacker(UnitSnapshot? Unit, string Source, string Summary);

internal sealed class BattleContextResolver
{
    private readonly RuntimeSettings _settings;

    public BattleContextResolver(RuntimeSettings settings)
    {
        _settings = settings;
    }

    public ResolvedAttacker ResolveRecentAttacker(
        UnitSnapshot target,
        IReadOnlyDictionary<nint, UnitObservation> observations,
        long nowTick)
    {
        var candidates = BuildCandidates(target, observations, nowTick);
        string summary = FormatCandidates(candidates);
        if (!_settings.InferAttackerFromRecentUnits || candidates.Count == 0)
            return new ResolvedAttacker(null, "none", summary);

        var best = candidates[0];
        return new ResolvedAttacker(best.Unit, "recent-unit", summary);
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

    private static int AgeMs(long nowTick, long seenTick)
        => (int)Math.Round((nowTick - seenTick) * 1000.0 / Stopwatch.Frequency);

    private sealed record AttackerCandidate(UnitSnapshot Unit, int AgeMs, bool Opposing, int Score);
}
