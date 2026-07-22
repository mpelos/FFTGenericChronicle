namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalStunRecoveryRequest(
    long TurnActionInstanceId,
    DclUnitKey Unit,
    bool TurnBeganStunned,
    int RecoveryScore,
    long? StunStateInstanceId);

internal sealed record DclCanonicalStunRecoveryExecutionResult(
    DclStunRecoveryResult Recovery,
    IReadOnlyList<long> RemovedStateInstanceIds);

/// <summary>
/// Owns the end-of-turn Stun recovery site. The turn owner supplies one stable ActionInstance id;
/// recovery removes only the exact Stun instance that existed for the turn snapshot.
/// </summary>
internal static class DclCanonicalRecoveryExecutor
{
    public static DclCanonicalStunRecoveryExecutionResult ResolveStun(
        DclCanonicalBattleRuntime battle,
        DclCanonicalStunRecoveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        if (request.TurnActionInstanceId <= 0 || request.Unit.BattleGeneration != battle.BattleGeneration)
            throw new ArgumentException("Stun recovery request does not belong to this battle turn.", nameof(request));
        if (!battle.TryGetObservedUnit(request.Unit.UnitSlot, out DclUnitKey observedUnit) ||
            observedUnit != request.Unit)
            throw new ArgumentException("Stun recovery requires a current observed UnitKey.", nameof(request));

        DclStateInstance? stun = null;
        if (request.StunStateInstanceId is { } instanceId)
        {
            if (!battle.States.TryGet(instanceId, out DclStateInstance candidate) ||
                candidate.Target != request.Unit || !StringComparer.Ordinal.Equals(candidate.Kind, "stun"))
                throw new ArgumentException("Stun recovery references a stale or foreign state instance.", nameof(request));
            stun = candidate;
        }
        if (request.TurnBeganStunned && stun is null)
            throw new ArgumentException("A turn that began Stunned requires its exact state instance.", nameof(request));

        int? roll = request.TurnBeganStunned
            ? battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                request.TurnActionInstanceId,
                request.Unit,
                request.Unit,
                strikeIndex: 0,
                DclRollSite.Recovery,
                drawIndex: 0))
            : null;
        DclStunRecoveryResult recovery = DclStunRecovery.Resolve(
            request.TurnBeganStunned,
            request.RecoveryScore,
            roll);
        IReadOnlyList<long> removed = recovery.RemoveStun
            ? battle.States.RemoveInstances([stun!.InstanceId])
            : [];
        if (recovery.RemoveStun && !removed.SequenceEqual([stun!.InstanceId]))
            throw new InvalidOperationException("Successful Stun recovery did not remove its exact snapshotted instance.");
        return new DclCanonicalStunRecoveryExecutionResult(recovery, removed);
    }
}
