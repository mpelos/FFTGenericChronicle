namespace fftivc.generic.chronicle.codemod;

internal enum DclSyntheticReactionReservationPhase
{
    Rejected,
    Requested,
    Committed,
}

internal readonly record struct DclSyntheticReactionReservation(
    nint DefenderPtr,
    int DefenderTableIndex,
    int DefenderCharId,
    DclReactionActionToken ActionToken,
    int Chance,
    int Roll,
    DclSyntheticReactionReservationPhase Phase);

internal readonly record struct DclSyntheticReactionGateDecision(
    bool Eligible,
    bool Accepted,
    bool ShouldRequestProducer,
    bool Replayed,
    DclSyntheticReactionReservation Reservation,
    string Reason);

/// <summary>
/// Idempotent bridge between a managed synthetic-Reaction roll and the later native pass-2 commit.
/// Evaluation reserves but never consumes reaction cadence. Only an exact actor/source commit may
/// take the reservation; duplicate gate callbacks and duplicate commits remain side-effect free.
/// The coordinator is carrier-, trigger-, and effect-agnostic.
/// </summary>
internal sealed class DclSyntheticReactionCoordinator
{
    private readonly object _gate = new();
    private readonly Dictionary<nint, DclSyntheticReactionReservation> _latestByDefender = new();

    public DclSyntheticReactionGateDecision Evaluate(
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
                bool acceptedBefore = previous.Phase is DclSyntheticReactionReservationPhase.Requested or
                    DclSyntheticReactionReservationPhase.Committed;
                return new DclSyntheticReactionGateDecision(
                    eligible,
                    acceptedBefore,
                    ShouldRequestProducer: false,
                    Replayed: true,
                    previous,
                    previous.Phase == DclSyntheticReactionReservationPhase.Committed
                        ? "already-committed"
                        : acceptedBefore ? "already-requested" : "already-rejected");
            }

            bool accepted = eligible && boundedRoll < boundedChance;
            var reservation = new DclSyntheticReactionReservation(
                defenderPtr,
                defenderTableIndex,
                defenderCharId,
                actionToken,
                boundedChance,
                boundedRoll,
                accepted
                    ? DclSyntheticReactionReservationPhase.Requested
                    : DclSyntheticReactionReservationPhase.Rejected);
            _latestByDefender[defenderPtr] = reservation;
            return new DclSyntheticReactionGateDecision(
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
        out DclSyntheticReactionReservation committed)
    {
        committed = default;
        lock (_gate)
        {
            if (!_latestByDefender.TryGetValue(defenderPtr, out var pending) ||
                pending.DefenderCharId != defenderCharId ||
                pending.Phase != DclSyntheticReactionReservationPhase.Requested ||
                pending.ActionToken.SourceIdx != sourceTableIndex)
                return false;

            committed = pending with { Phase = DclSyntheticReactionReservationPhase.Committed };
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
