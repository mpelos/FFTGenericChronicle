namespace fftivc.generic.chronicle.codemod;

internal enum DclBattleLifecycleSignal
{
    None = 0,
    BattleStarted,
    UnitIdentityReused,
    BattleEnded,
}

/// <summary>
/// Tracks the identity occupying each observed battle-unit pointer. Battle-table pointers are
/// reused, so pointer identity alone cannot own persistent DCL state across battles.
/// </summary>
internal sealed class DclBattleLifecycle
{
    private readonly object _gate = new();
    private readonly Dictionary<nint, int> _identities = new();

    public int Generation { get; private set; }

    public int TrackedUnitCount
    {
        get
        {
            lock (_gate)
                return _identities.Count;
        }
    }

    public DclBattleLifecycleSignal Observe(nint unitPtr, int charId, out int previousCharId)
    {
        previousCharId = -1;
        if (unitPtr == 0 || charId < 0)
            return DclBattleLifecycleSignal.None;

        lock (_gate)
        {
            if (_identities.TryGetValue(unitPtr, out previousCharId))
            {
                if (previousCharId == charId)
                    return DclBattleLifecycleSignal.None;

                // A table slot changing identity without an invalid observation is a hard generation
                // boundary. Drop every old identity so later observations join the new generation.
                _identities.Clear();
                _identities[unitPtr] = charId;
                Generation++;
                return DclBattleLifecycleSignal.UnitIdentityReused;
            }

            previousCharId = -1;
            bool startsBattle = _identities.Count == 0;
            _identities[unitPtr] = charId;
            if (!startsBattle)
                return DclBattleLifecycleSignal.None;

            Generation++;
            return DclBattleLifecycleSignal.BattleStarted;
        }
    }

    public DclBattleLifecycleSignal Forget(nint unitPtr)
    {
        if (unitPtr == 0)
            return DclBattleLifecycleSignal.None;

        lock (_gate)
        {
            if (!_identities.Remove(unitPtr) || _identities.Count != 0)
                return DclBattleLifecycleSignal.None;
            return DclBattleLifecycleSignal.BattleEnded;
        }
    }
}
