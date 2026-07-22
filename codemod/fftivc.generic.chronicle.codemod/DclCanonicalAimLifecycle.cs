namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalAimLifecycleResult(
    bool HadAim,
    bool Retained,
    long? InstanceId,
    string Reason);

internal static class DclCanonicalAimLifecycle
{
    public static DclAimStatePayload PlanGrantedStep(
        DclCanonicalStateSnapshot owner,
        DclUnitKey trackedTarget,
        string payloadSchema)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!trackedTarget.IsValid || trackedTarget.BattleGeneration != owner.Unit.BattleGeneration)
            throw new ArgumentException("Aim must track a stable target in the same battle.", nameof(trackedTarget));
        DclAimState aim = owner.TryGetAim(out DclAimStatePayload? existing)
            ? (existing ?? throw new InvalidOperationException("Aim payload was not materialized.")).Materialize()
            : new DclAimState(trackedTarget);
        aim.GrantStep(trackedTarget);
        return aim.ToPayload(payloadSchema);
    }

    public static DclCanonicalAimLifecycleResult ResolveInjuryRetention(
        DclStateRegistry registry,
        DclUnitKey owner,
        int injury,
        bool forcedMovement,
        int will,
        int aimRetentionModifier,
        int statePenaltyMagnitude,
        int? roll)
    {
        DclCanonicalAimLifecycleResult planned = PlanInjuryRetention(
            registry,
            owner,
            injury,
            forcedMovement,
            will,
            aimRetentionModifier,
            statePenaltyMagnitude,
            roll);
        if (planned is { HadAim: true, Retained: false, InstanceId: { } instanceId })
            registry.RemoveInstances([instanceId]);
        return planned;
    }

    public static DclCanonicalAimLifecycleResult PlanInjuryRetention(
        DclStateRegistry registry,
        DclUnitKey owner,
        int injury,
        bool forcedMovement,
        int will,
        int aimRetentionModifier,
        int statePenaltyMagnitude,
        int? roll)
    {
        ArgumentNullException.ThrowIfNull(registry);
        DclStateInstance? instance = UniqueAim(registry, owner);
        if (instance is null)
        {
            if (roll is not null) throw new ArgumentException("A unit without Aim cannot consume retention RNG.", nameof(roll));
            return new DclCanonicalAimLifecycleResult(false, false, null, "no-aim");
        }
        if (instance.Payload is not DclAimStatePayload payload)
            throw new InvalidOperationException("Aim instance lost its typed payload.");
        DclAimState aim = payload.Materialize();
        bool retained = aim.ResolveInjuryRetention(
            injury,
            forcedMovement,
            will,
            aimRetentionModifier,
            statePenaltyMagnitude,
            roll);
        string reason = retained
            ? injury == 0 ? "zero-injury-retained" : "retention-roll-succeeded"
            : forcedMovement ? "forced-movement-cancelled" : "retention-roll-failed";
        return new DclCanonicalAimLifecycleResult(true, retained, instance.InstanceId, reason);
    }

    public static DclCanonicalAimLifecycleResult CancelOwner(
        DclStateRegistry registry,
        DclUnitKey owner,
        string reason)
    {
        DclCanonicalAimLifecycleResult planned = PlanCancelOwner(registry, owner, reason);
        if (planned is { HadAim: true, InstanceId: { } instanceId })
            registry.RemoveInstances([instanceId]);
        return planned;
    }

    public static DclCanonicalAimLifecycleResult PlanCancelOwner(
        DclStateRegistry registry,
        DclUnitKey owner,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Aim cancellation reason is required.", nameof(reason));
        DclStateInstance? instance = UniqueAim(registry, owner);
        if (instance is null)
            return new DclCanonicalAimLifecycleResult(false, false, null, reason);
        return new DclCanonicalAimLifecycleResult(true, false, instance.InstanceId, reason);
    }

    public static IReadOnlyList<long> CancelTrackedTargetLoss(
        DclStateRegistry registry,
        DclUnitKey lostTarget)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (!lostTarget.IsValid || lostTarget.BattleGeneration != registry.BattleGeneration)
            throw new ArgumentException("Lost Aim target is invalid for this registry.", nameof(lostTarget));
        long[] ids = registry.Instances
            .Where(instance => StringComparer.Ordinal.Equals(instance.Kind, "aim") &&
                               instance.Payload is DclAimStatePayload payload &&
                               payload.Target == lostTarget)
            .Select(instance => instance.InstanceId)
            .Order()
            .ToArray();
        if (ids.Length > 0) registry.RemoveInstances(ids);
        return ids;
    }

    private static DclStateInstance? UniqueAim(DclStateRegistry registry, DclUnitKey owner)
    {
        if (!owner.IsValid || owner.BattleGeneration != registry.BattleGeneration)
            throw new ArgumentException("Aim owner is invalid for this registry.", nameof(owner));
        DclStateInstance[] matching = registry.CaptureTarget(owner).Instances
            .Where(instance => StringComparer.Ordinal.Equals(instance.Kind, "aim"))
            .ToArray();
        if (matching.Length > 1)
            throw new InvalidOperationException("An Aim owner cannot have multiple live Aim instances.");
        return matching.SingleOrDefault();
    }
}
