namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclReactionActionToken(
    int SourceIdx,
    int SourceCharId,
    long SourceTurnEpoch,
    int ActionType,
    int AbilityId);

/// <summary>
/// Runtime-owned cadence for one defender slot. The game's active-unit rising edge defines an own
/// turn epoch; a normal FFT action is then identified by the source slot/character, source turn
/// epoch, action type, and ability id. Reaction-triggered actions remain a separate eligibility
/// question and must not be silently folded into this normal-action token.
/// </summary>
internal sealed class DclReactionCadenceState
{
    private readonly Dictionary<int, DclReactionActionToken> _lastAttackerActionByReaction = new();
    private readonly HashSet<int> _consumedOwnCycle = new();

    public int TargetCharId { get; private set; } = -1;
    public bool Initialized { get; private set; }
    public bool WasActive { get; private set; }
    public long OwnTurnEpoch { get; private set; }

    public bool Observe(int targetCharId, bool activeNow)
    {
        if (!Initialized || TargetCharId != targetCharId)
        {
            TargetCharId = targetCharId;
            Initialized = true;
            WasActive = activeNow;
            OwnTurnEpoch = 0;
            _consumedOwnCycle.Clear();
            _lastAttackerActionByReaction.Clear();
            return false;
        }

        bool startedOwnTurn = !WasActive && activeNow;
        WasActive = activeNow;
        if (!startedOwnTurn)
            return false;

        OwnTurnEpoch++;
        _consumedOwnCycle.Clear();
        return true;
    }

    public bool TryConsumeOwnTurnCycle(int reactionAbilityId)
        => _consumedOwnCycle.Add(reactionAbilityId);

    public bool TryConsumeAttackerAction(int reactionAbilityId, DclReactionActionToken token)
    {
        if (_lastAttackerActionByReaction.TryGetValue(reactionAbilityId, out var previous) && previous == token)
            return false;

        _lastAttackerActionByReaction[reactionAbilityId] = token;
        return true;
    }

    public bool CanConsumeAttackerAction(int reactionAbilityId, DclReactionActionToken token)
        => !_lastAttackerActionByReaction.TryGetValue(reactionAbilityId, out var previous) || previous != token;
}
