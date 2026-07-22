namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalReraiseTriggerRequest(
    long ActionInstanceId,
    DclTargetCandidate Target,
    int TargetMaxHp,
    long ReraiseStateInstanceId);

internal sealed record DclCanonicalReraiseTriggerResult(
    long ActionInstanceId,
    int AbilityId,
    DclUnitKey Target,
    DclReviveResult Revive,
    string ConsumedStateKind,
    IReadOnlyList<long> ConsumedStateInstanceIds,
    DclCanonicalNativeNumericChannels Channels,
    bool ClearKoAfterPositiveCredit,
    bool OpensReactionWindow);

/// <summary>
/// Resolves one persistent Reraise trigger after native KO entry. The target owns this lifecycle
/// ActionInstance; restored HP is sampled once, the exact trigger instance is consumed once, and
/// native integration must credit HP before clearing KO.
/// </summary>
internal static class DclCanonicalReraiseTriggerExecutor
{
    public static DclCanonicalReraiseTriggerResult Resolve(
        DclCanonicalBattleRuntime battle,
        DclCanonicalReraiseTriggerRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActionInstanceId <= 0 || request.Target.Unit.BattleGeneration != battle.BattleGeneration ||
            request.TargetMaxHp < 1 || request.Target.CurrentHp != 0 ||
            (request.Target.States & DclEligibleTargetStates.Ko) == 0)
            throw new ArgumentException("Reraise trigger requires one current KO target in this battle.", nameof(request));
        if (!battle.TryGetObservedUnit(request.Target.Unit.UnitSlot, out DclUnitKey observedTarget) ||
            observedTarget != request.Target.Unit)
            throw new ArgumentException("Reraise trigger requires a current observed UnitKey.", nameof(request));
        if (!battle.States.TryGet(request.ReraiseStateInstanceId, out DclStateInstance instance) ||
            instance.Target != request.Target.Unit || instance.Payload is not DclReraiseStatePayload payload)
            throw new ArgumentException("Reraise trigger references a stale, foreign, or untyped persistent instance.", nameof(request));
        if (payload.AbilityId is < 0 or > 511 || payload.FaithMultiplier < DclRational.FromInteger(0))
            throw new InvalidOperationException("Stored Reraise contains an invalid ability or Faith payload.");

        IReadOnlyList<int> restoredHpDice = battle.ExecutionRandom.RollD6Pool(
            battle.RollIdentity(
                request.ActionInstanceId,
                request.Target.Unit,
                request.Target.Unit,
                strikeIndex: 0,
                DclRollSite.HealingDie,
                drawIndex: 0),
            payload.RestoredHpExpression.Dice);
        int raw = Math.Max(0, DclInjury.RollDamage(payload.RestoredHpExpression, restoredHpDice));
        int final = payload.FaithModifiesRestoredHp
            ? checked((int)(DclRational.FromInteger(raw) * payload.FaithMultiplier).Floor())
            : raw;
        int applied = Math.Min(final, request.TargetMaxHp);
        IReadOnlyList<long> consumed = battle.States.RemoveInstances([instance.InstanceId]);
        if (!consumed.SequenceEqual([instance.InstanceId]))
            throw new InvalidOperationException("Reraise trigger did not consume its exact persistent instance.");
        var revive = new DclReviveResult(
            DclReviveRoute.ImmediateHpCredit,
            raw,
            final,
            applied,
            StoreReraise: false,
            ClearKoAfterPositiveCredit: applied > 0,
            applied > 0 ? "native-hp-credit-then-clear-ko" : "zero-credit-does-not-clear-ko");
        return new DclCanonicalReraiseTriggerResult(
            request.ActionInstanceId,
            payload.AbilityId,
            request.Target.Unit,
            revive,
            instance.Kind,
            consumed,
            new DclCanonicalNativeNumericChannels(
                hpDebit: 0,
                hpCredit: applied,
                mpDebit: 0,
                mpCredit: 0),
            revive.ClearKoAfterPositiveCredit,
            OpensReactionWindow: false);
    }
}
