namespace fftivc.generic.chronicle.codemod;

internal interface IDclUniformRandomSource
{
    int Next(int inclusiveMinimum, int exclusiveMaximum);
}

internal sealed class DclSystemRandomSource : IDclUniformRandomSource
{
    public int Next(int inclusiveMinimum, int exclusiveMaximum)
        => Random.Shared.Next(inclusiveMinimum, exclusiveMaximum);
}

internal enum DclExecutionDrawKind
{
    ThreeD6,
    D6,
    Percentage,
    RandomSelection,
}

internal sealed record DclExecutionRandomDraw(
    DclRollIdentity Identity,
    DclExecutionDrawKind Kind,
    int InclusiveMinimum,
    int ExclusiveMaximum,
    IReadOnlyList<int> Components,
    int Result);

/// <summary>
/// Battle-scoped owner of confirmed-execution randomness. One semantic RollIdentity is sampled at
/// most once; re-entry at compute, apply, or presentation receives the immutable cached draw.
/// Forecast and AI have no API that implicitly samples this owner.
/// </summary>
internal sealed class DclExecutionRandomLedger
{
    private readonly object _gate = new();
    private readonly IDclUniformRandomSource _source;
    private readonly Dictionary<DclRollIdentity, DclExecutionRandomDraw> _draws = [];

    public DclExecutionRandomLedger(int battleGeneration, IDclUniformRandomSource? source = null)
    {
        if (battleGeneration <= 0) throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        BattleGeneration = battleGeneration;
        _source = source ?? new DclSystemRandomSource();
    }

    public int BattleGeneration { get; }
    public int Count { get { lock (_gate) return _draws.Count; } }

    public int Roll3D6(DclRollIdentity identity)
        => GetOrDraw(identity, DclExecutionDrawKind.ThreeD6, 1, 7, componentCount: 3).Result;

    public int RollD6(DclRollIdentity identity)
        => GetOrDraw(identity, DclExecutionDrawKind.D6, 1, 7, componentCount: 1).Result;

    public int RollPercentage(DclRollIdentity identity)
        => GetOrDraw(identity, DclExecutionDrawKind.Percentage, 0, 100, componentCount: 1).Result;

    public int SelectIndex(DclRollIdentity identity, int candidateCount)
    {
        if (candidateCount <= 0) throw new ArgumentOutOfRangeException(nameof(candidateCount));
        return GetOrDraw(
            identity,
            DclExecutionDrawKind.RandomSelection,
            0,
            candidateCount,
            componentCount: 1).Result;
    }

    public IReadOnlyList<int> RollD6Pool(DclRollIdentity firstDieIdentity, int diceCount)
    {
        if (diceCount < 0) throw new ArgumentOutOfRangeException(nameof(diceCount));
        var result = new int[diceCount];
        for (int index = 0; index < diceCount; index++)
        {
            DclRollIdentity identity = firstDieIdentity with
            {
                DrawIndex = checked(firstDieIdentity.DrawIndex + index),
            };
            result[index] = RollD6(identity);
        }
        return result;
    }

    public DclExecutionRandomDraw Get(DclRollIdentity identity)
    {
        ValidateIdentity(identity);
        lock (_gate)
            return _draws.TryGetValue(identity, out DclExecutionRandomDraw? draw)
                ? draw
                : throw new KeyNotFoundException("The execution random site has not been sampled.");
    }

    private DclExecutionRandomDraw GetOrDraw(
        DclRollIdentity identity,
        DclExecutionDrawKind kind,
        int inclusiveMinimum,
        int exclusiveMaximum,
        int componentCount)
    {
        ValidateIdentity(identity);
        if (inclusiveMinimum >= exclusiveMaximum || componentCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        lock (_gate)
        {
            if (_draws.TryGetValue(identity, out DclExecutionRandomDraw? existing))
            {
                if (existing.Kind != kind || existing.InclusiveMinimum != inclusiveMinimum ||
                    existing.ExclusiveMaximum != exclusiveMaximum || existing.Components.Count != componentCount)
                    throw new InvalidOperationException(
                        "One execution RollIdentity cannot be reused for a different random domain.");
                return existing;
            }

            var components = new int[componentCount];
            int sum = 0;
            for (int index = 0; index < componentCount; index++)
            {
                int value = _source.Next(inclusiveMinimum, exclusiveMaximum);
                if (value < inclusiveMinimum || value >= exclusiveMaximum)
                    throw new InvalidOperationException("The configured random source returned a value outside its requested domain.");
                components[index] = value;
                sum = checked(sum + value);
            }
            int result = componentCount == 1 ? components[0] : sum;
            var draw = new DclExecutionRandomDraw(
                identity,
                kind,
                inclusiveMinimum,
                exclusiveMaximum,
                components,
                result);
            _draws.Add(identity, draw);
            return draw;
        }
    }

    private void ValidateIdentity(DclRollIdentity identity)
    {
        if (identity.BattleGeneration != BattleGeneration || identity.ActionInstanceId <= 0 ||
            identity.SourceUnitSlot is < 0 or >= 64 || identity.SourceCharacterId is < 0 or > byte.MaxValue ||
            identity.StrikeIndex < 0 || identity.DrawIndex < 0)
            throw new ArgumentException("The execution RollIdentity is outside this battle/action domain.", nameof(identity));
        bool noTarget = identity.TargetUnitSlot == -1 && identity.TargetCharacterId == -1;
        bool validTarget = identity.TargetUnitSlot is >= 0 and < 64 &&
                           identity.TargetCharacterId is >= 0 and <= byte.MaxValue;
        if (!noTarget && !validTarget)
            throw new ArgumentException("Targetless random sites use -1/-1; target-owned sites require a complete UnitKey.", nameof(identity));
    }
}
