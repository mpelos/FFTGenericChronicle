namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Own-turn edge state for the DCL's small MP trickle. The native MP/MaxMP fields remain the
/// per-battle budget; this state only supplies the deliberately small regeneration tail.
/// </summary>
internal sealed class DclMpTrickleState
{
    public int TargetCharId { get; private set; } = -1;
    public bool WasActive { get; private set; }
    public bool Initialized { get; private set; }

    public bool Observe(int targetCharId, bool activeNow)
    {
        if (!Initialized || TargetCharId != targetCharId)
        {
            TargetCharId = targetCharId;
            WasActive = activeNow;
            Initialized = true;
            return false;
        }

        bool startedOwnTurn = !WasActive && activeNow;
        WasActive = activeNow;
        return startedOwnTurn;
    }
}

internal static class DclMpEconomy
{
    public static int ClampCredit(int requested, int currentMp, int maxMp)
    {
        currentMp = Math.Max(0, currentMp);
        maxMp = Math.Max(currentMp, maxMp);
        return Math.Clamp(requested, 0, maxMp - currentMp);
    }
}
