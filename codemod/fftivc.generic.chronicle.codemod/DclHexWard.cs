namespace fftivc.generic.chronicle.codemod;

internal enum DclHexWardReservationPhase
{
    Rejected,
    Requested,
    Committed,
}

internal readonly record struct DclHexWardReservation(
    nint DefenderPtr,
    int DefenderTableIndex,
    int DefenderCharId,
    DclReactionActionToken ActionToken,
    int Chance,
    int Roll,
    DclHexWardReservationPhase Phase);

internal readonly record struct DclHexWardGateDecision(
    bool Eligible,
    bool Accepted,
    bool ShouldRequestProducer,
    bool Replayed,
    DclHexWardReservation Reservation,
    string Reason);

/// <summary>
/// Idempotent bridge between Hex Ward's managed Caution roll and the later native pass-2 commit.
/// Evaluation reserves but never consumes reaction cadence. Only an exact actor/source commit may
/// take the reservation; duplicate gate callbacks and duplicate commits remain side-effect free.
/// </summary>
internal sealed class DclHexWardCoordinator
{
    private readonly object _gate = new();
    private readonly Dictionary<nint, DclHexWardReservation> _latestByDefender = new();

    public DclHexWardGateDecision Evaluate(
        nint defenderPtr,
        int defenderTableIndex,
        int defenderCharId,
        DclReactionActionToken actionToken,
        bool eligible,
        int chance,
        int roll,
        string ineligibleReason = "ineligible")
    {
        int boundedChance = Math.Clamp(chance, 0, 100);
        int boundedRoll = Math.Clamp(roll, 0, 99);
        lock (_gate)
        {
            if (_latestByDefender.TryGetValue(defenderPtr, out var previous) &&
                previous.DefenderCharId == defenderCharId &&
                previous.ActionToken == actionToken)
            {
                bool acceptedBefore = previous.Phase is DclHexWardReservationPhase.Requested or
                    DclHexWardReservationPhase.Committed;
                return new DclHexWardGateDecision(
                    eligible,
                    acceptedBefore,
                    ShouldRequestProducer: false,
                    Replayed: true,
                    previous,
                    previous.Phase == DclHexWardReservationPhase.Committed
                        ? "already-committed"
                        : acceptedBefore ? "already-requested" : "already-rejected");
            }

            bool accepted = eligible && boundedRoll < boundedChance;
            var reservation = new DclHexWardReservation(
                defenderPtr,
                defenderTableIndex,
                defenderCharId,
                actionToken,
                boundedChance,
                boundedRoll,
                accepted ? DclHexWardReservationPhase.Requested : DclHexWardReservationPhase.Rejected);
            _latestByDefender[defenderPtr] = reservation;
            return new DclHexWardGateDecision(
                eligible,
                accepted,
                ShouldRequestProducer: accepted,
                Replayed: false,
                reservation,
                eligible ? accepted ? "accepted" : "chance-failed" : ineligibleReason);
        }
    }

    public bool TryCommit(
        nint defenderPtr,
        int defenderCharId,
        int sourceTableIndex,
        out DclHexWardReservation committed)
    {
        committed = default;
        lock (_gate)
        {
            if (!_latestByDefender.TryGetValue(defenderPtr, out var pending) ||
                pending.DefenderCharId != defenderCharId ||
                pending.Phase != DclHexWardReservationPhase.Requested ||
                pending.ActionToken.SourceIdx != sourceTableIndex)
                return false;

            committed = pending with { Phase = DclHexWardReservationPhase.Committed };
            _latestByDefender[defenderPtr] = committed;
            return true;
        }
    }

    public void Forget(nint defenderPtr)
    {
        lock (_gate)
            _latestByDefender.Remove(defenderPtr);
    }
}
