namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeSourceEffectApplyPlan(
    DclUnitKey Source,
    DclCanonicalNativePoolSnapshot Before,
    DclCanonicalNativePoolSnapshot After,
    DclCanonicalNativeSourceEffectProjection Projection,
    IReadOnlyList<DclStateInstance> RemovedOnSourceKo);

internal sealed record DclCanonicalNativeSourceEffectCommitResult(
    DclCanonicalNativeSourceEffectProjection SourceEffect,
    IReadOnlyList<DclStateInstance> RemovedOnSourceKo);

internal static class DclCanonicalNativeSourceEffectApplyPlanner
{
    public static DclCanonicalNativeSourceEffectApplyPlan Build(
        DclUnitKey expectedSource,
        DclCanonicalNativePoolSnapshot pools,
        DclCanonicalNativeSourceEffectProjection projection,
        DclStateRegistry? stateRegistry = null)
    {
        if (!expectedSource.IsValid || projection.Source != expectedSource ||
            !projection.IsResourceChange || projection.OpensDamageReaction)
            throw new ArgumentException(
                "A source-side carrier must be a non-damage ResourceChange owned by the exact action source.",
                nameof(projection));
        DclCanonicalNativeNumericChannels channels = projection.Channels;
        int nextHp = checked(pools.CurrentHp - channels.HpDebit + channels.HpCredit);
        int nextMp = checked(pools.CurrentMp - channels.MpDebit + channels.MpCredit);
        if (nextHp < 0 || nextHp > pools.MaxHp || nextMp < 0 || nextMp > pools.MaxMp)
            throw new InvalidOperationException("The source ResourceChange exceeds its snapshotted HP/MP pools.");
        bool sourceKo = nextHp == 0;
        if (projection.SourceKo != sourceKo)
            throw new InvalidOperationException("Source ResourceChange KO metadata disagrees with the exact post-effect HP pool.");
        DclStateInstance[] removed = [];
        if (sourceKo)
        {
            if (stateRegistry is null || stateRegistry.BattleGeneration != expectedSource.BattleGeneration)
                throw new InvalidOperationException(
                    "A lethal source ResourceChange requires the exact battle registry for KO cleanup.");
            removed = stateRegistry.Instances
                .Where(instance =>
                    (instance.Target == expectedSource && instance.Definition.RemoveOnTargetKo) ||
                    (instance.Source == expectedSource && instance.Definition.RemoveOnSourceKo))
                .DistinctBy(instance => instance.InstanceId)
                .OrderBy(instance => instance.InstanceId)
                .ToArray();
        }
        return new DclCanonicalNativeSourceEffectApplyPlan(
            expectedSource,
            pools,
            new DclCanonicalNativePoolSnapshot(nextHp, pools.MaxHp, nextMp, pools.MaxMp),
            projection,
            removed);
    }

    public static void ValidateKoCleanup(
        DclCanonicalNativeSourceEffectApplyPlan plan,
        DclStateRegistry stateRegistry)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(stateRegistry);
        if (stateRegistry.BattleGeneration != plan.Source.BattleGeneration)
            throw new ArgumentException("Source-effect cleanup registry belongs to another battle.", nameof(stateRegistry));
        long[] current = plan.Projection.SourceKo
            ? stateRegistry.Instances
                .Where(instance =>
                    (instance.Target == plan.Source && instance.Definition.RemoveOnTargetKo) ||
                    (instance.Source == plan.Source && instance.Definition.RemoveOnSourceKo))
                .Select(instance => instance.InstanceId)
                .Distinct()
                .Order()
                .ToArray()
            : [];
        long[] expected = plan.RemovedOnSourceKo.Select(instance => instance.InstanceId).Order().ToArray();
        if (!current.SequenceEqual(expected))
            throw new InvalidOperationException("Source ResourceChange KO cleanup became stale before native commit.");
    }

    public static IReadOnlyList<DclStateInstance> CommitKoCleanup(
        DclCanonicalNativeSourceEffectApplyPlan plan,
        DclStateRegistry stateRegistry)
    {
        ValidateKoCleanup(plan, stateRegistry);
        if (!plan.Projection.SourceKo) return [];
        long[] removed = stateRegistry.OnTargetKo(plan.Source)
            .Concat(stateRegistry.OnSourceKo(plan.Source))
            .Distinct()
            .Order()
            .ToArray();
        long[] expected = plan.RemovedOnSourceKo.Select(instance => instance.InstanceId).Order().ToArray();
        if (!removed.SequenceEqual(expected))
            throw new InvalidOperationException("Source ResourceChange KO cleanup committed a different state set than planned.");
        return plan.RemovedOnSourceKo;
    }
}

internal static class DclCanonicalNativeSourceEffectWriter
{
    public static void Apply(
        DclCanonicalNativeSourceEffectApplyPlan plan,
        Func<DclCanonicalNativePoolSnapshot> readPools,
        Action<int> writeCurrentMp,
        Action<int> writeCurrentHp)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(readPools);
        ArgumentNullException.ThrowIfNull(writeCurrentMp);
        ArgumentNullException.ThrowIfNull(writeCurrentHp);
        if (readPools() != plan.Before)
            throw new InvalidOperationException("Native source pools changed before ResourceChange write.");
        bool wroteMp = false;
        bool wroteHp = false;
        try
        {
            writeCurrentMp(plan.After.CurrentMp);
            wroteMp = true;
            writeCurrentHp(plan.After.CurrentHp);
            wroteHp = true;
            if (readPools() != plan.After)
                throw new InvalidOperationException("Native source-pool readback disagrees with the ResourceChange plan.");
        }
        catch (Exception writeFailure)
        {
            var rollbackFailures = new List<Exception>();
            if (wroteHp)
                try { writeCurrentHp(plan.Before.CurrentHp); }
                catch (Exception rollbackFailure) { rollbackFailures.Add(rollbackFailure); }
            if (wroteMp)
                try { writeCurrentMp(plan.Before.CurrentMp); }
                catch (Exception rollbackFailure) { rollbackFailures.Add(rollbackFailure); }
            if (rollbackFailures.Count > 0)
                throw new AggregateException(
                    "Source ResourceChange write failed and native pool rollback was incomplete.",
                    [writeFailure, .. rollbackFailures]);
            throw;
        }
    }
}
