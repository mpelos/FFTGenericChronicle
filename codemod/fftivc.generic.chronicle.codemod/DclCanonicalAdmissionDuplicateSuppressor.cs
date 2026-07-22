namespace fftivc.generic.chronicle.codemod;

internal sealed class DclCanonicalAdmissionDuplicateSuppressor
{
    private readonly object _gate = new();
    private string? _lastKey;
    private long _lastTick;

    public bool ShouldSuppress(string key, long nowTick, long duplicateWindowTicks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (duplicateWindowTicks < 0)
            throw new ArgumentOutOfRangeException(nameof(duplicateWindowTicks));

        lock (_gate)
        {
            if (_lastKey == key && nowTick - _lastTick <= duplicateWindowTicks)
                return true;
            _lastKey = key;
            _lastTick = nowTick;
            return false;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _lastKey = null;
            _lastTick = 0;
        }
    }
}
