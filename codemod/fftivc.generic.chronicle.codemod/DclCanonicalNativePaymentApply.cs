namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclCanonicalNativePoolSnapshot
{
    public int CurrentHp { get; }
    public int MaxHp { get; }
    public int CurrentMp { get; }
    public int MaxMp { get; }

    public DclCanonicalNativePoolSnapshot(int currentHp, int maxHp, int currentMp, int maxMp)
    {
        if (maxHp < 1 || maxMp < 0 || currentHp < 0 || currentHp > maxHp ||
            currentMp < 0 || currentMp > maxMp)
            throw new ArgumentOutOfRangeException(nameof(currentHp),
                "Native payer pools must remain within MaxHP > 0 and MaxMP >= 0.");
        CurrentHp = currentHp;
        MaxHp = maxHp;
        CurrentMp = currentMp;
        MaxMp = maxMp;
    }
}

internal sealed record DclCanonicalNativePaymentApplyPlan(
    DclUnitKey Payer,
    DclCanonicalNativePoolSnapshot Before,
    DclCanonicalNativePoolSnapshot After,
    int MpDebit,
    int HpDebit,
    bool ApplyMpFirst,
    bool PayerKo,
    IReadOnlyList<DclStateInstance> RemovedOnPayerKo,
    DclCanonicalNativePaymentProjection? Projection)
{
    public bool IsNoOp => Projection is null;
}

internal sealed record DclCanonicalNativePaymentCommitResult(
    DclCanonicalNativePaymentProjection? Payment,
    IReadOnlyList<DclStateInstance> RemovedOnPayerKo);

internal static class DclCanonicalNativePaymentApplyPlanner
{
    public static DclCanonicalNativePaymentApplyPlan Build(
        DclUnitKey expectedPayer,
        DclCanonicalNativePoolSnapshot pools,
        DclCanonicalNativePaymentProjection? payment,
        DclStateRegistry? stateRegistry = null)
    {
        if (!expectedPayer.IsValid)
            throw new ArgumentException("A stable expected payer identity is required.", nameof(expectedPayer));
        if (payment is null)
            return new DclCanonicalNativePaymentApplyPlan(
                expectedPayer,
                pools,
                pools,
                MpDebit: 0,
                HpDebit: 0,
                ApplyMpFirst: true,
                PayerKo: false,
                RemovedOnPayerKo: [],
                Projection: null);
        if (payment.Payer != expectedPayer || !payment.IsResourcePayment || payment.OpensDamageReaction ||
            payment.Channels.HpCredit != 0 || payment.Channels.MpCredit != 0)
            throw new ArgumentException(
                "Canonical resource payment must debit only the declared payer and cannot open a damage Reaction.",
                nameof(payment));
        int mpDebit = payment.Channels.MpDebit;
        int hpDebit = payment.Channels.HpDebit;
        if (mpDebit > pools.CurrentMp || hpDebit > pools.CurrentHp)
            throw new InvalidOperationException(
                "Canonical resource payment exceeds the snapshotted payer pools.");
        int nextMp = checked(pools.CurrentMp - mpDebit);
        int nextHp = checked(pools.CurrentHp - hpDebit);
        bool payerKo = nextHp == 0;
        if (payment.PayerKo != payerKo)
            throw new InvalidOperationException(
                "Canonical payment KO metadata disagrees with the exact post-payment HP pool.");
        DclStateInstance[] removedOnPayerKo = [];
        if (payerKo)
        {
            if (stateRegistry is null || stateRegistry.BattleGeneration != expectedPayer.BattleGeneration)
                throw new InvalidOperationException(
                    "Lethal canonical payment requires the payer's exact battle state registry for KO cleanup.");
            removedOnPayerKo = stateRegistry.Instances
                .Where(instance =>
                    (instance.Target == expectedPayer && instance.Definition.RemoveOnTargetKo) ||
                    (instance.Source == expectedPayer && instance.Definition.RemoveOnSourceKo))
                .DistinctBy(instance => instance.InstanceId)
                .OrderBy(instance => instance.InstanceId)
                .ToArray();
        }
        return new DclCanonicalNativePaymentApplyPlan(
            expectedPayer,
            pools,
            new DclCanonicalNativePoolSnapshot(nextHp, pools.MaxHp, nextMp, pools.MaxMp),
            mpDebit,
            hpDebit,
            ApplyMpFirst: true,
            payerKo,
            removedOnPayerKo,
            payment);
    }

    public static IReadOnlyList<DclStateInstance> CommitPayerKoCleanup(
        DclCanonicalNativePaymentApplyPlan plan,
        DclStateRegistry stateRegistry)
    {
        ValidatePayerKoCleanup(plan, stateRegistry);
        if (!plan.PayerKo) return [];
        long[] expected = plan.RemovedOnPayerKo.Select(instance => instance.InstanceId).Order().ToArray();
        long[] removed = stateRegistry.OnTargetKo(plan.Payer)
            .Concat(stateRegistry.OnSourceKo(plan.Payer))
            .Distinct()
            .Order()
            .ToArray();
        if (!removed.SequenceEqual(expected))
            throw new InvalidOperationException("Payer KO cleanup committed a different state set than planned.");
        return plan.RemovedOnPayerKo;
    }

    public static void ValidatePayerKoCleanup(
        DclCanonicalNativePaymentApplyPlan plan,
        DclStateRegistry stateRegistry)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(stateRegistry);
        if (stateRegistry.BattleGeneration != plan.Payer.BattleGeneration)
            throw new ArgumentException("Payment cleanup registry belongs to another battle.", nameof(stateRegistry));
        if (!plan.PayerKo)
        {
            if (plan.RemovedOnPayerKo.Count != 0)
                throw new InvalidOperationException("A surviving payer cannot own KO cleanup mutations.");
            return;
        }
        long[] expected = plan.RemovedOnPayerKo.Select(instance => instance.InstanceId).Order().ToArray();
        long[] current = stateRegistry.Instances
            .Where(instance =>
                (instance.Target == plan.Payer && instance.Definition.RemoveOnTargetKo) ||
                (instance.Source == plan.Payer && instance.Definition.RemoveOnSourceKo))
            .Select(instance => instance.InstanceId)
            .Distinct()
            .Order()
            .ToArray();
        if (!expected.SequenceEqual(current))
            throw new InvalidOperationException("Payer KO cleanup plan became stale before native payment commit.");
    }
}

internal static class DclCanonicalNativePaymentWriter
{
    public static void Apply(
        DclCanonicalNativePaymentApplyPlan plan,
        Func<DclCanonicalNativePoolSnapshot> readPools,
        Action<int> writeCurrentMp,
        Action<int> writeCurrentHp)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(readPools);
        ArgumentNullException.ThrowIfNull(writeCurrentMp);
        ArgumentNullException.ThrowIfNull(writeCurrentHp);
        DclCanonicalNativePoolSnapshot observedBefore = readPools();
        if (observedBefore != plan.Before)
            throw new InvalidOperationException("Native payer pools changed before canonical payment write.");
        if (!plan.ApplyMpFirst)
            throw new InvalidOperationException("Canonical source payment must remain MP-first.");
        if (plan.IsNoOp)
        {
            if (plan.Before != plan.After || plan.MpDebit != 0 || plan.HpDebit != 0 || plan.PayerKo)
                throw new InvalidOperationException("A no-payment native plan cannot mutate payer pools or KO state.");
            return;
        }

        bool wroteMp = false;
        bool wroteHp = false;
        try
        {
            writeCurrentMp(plan.After.CurrentMp);
            wroteMp = true;
            writeCurrentHp(plan.After.CurrentHp);
            wroteHp = true;
            DclCanonicalNativePoolSnapshot observedAfter = readPools();
            if (observedAfter != plan.After)
                throw new InvalidOperationException("Native payer pool readback disagrees with canonical payment plan.");
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
                    "Canonical payment write failed and native payer-pool rollback was incomplete.",
                    [writeFailure, .. rollbackFailures]);
            throw;
        }
    }
}
