namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalResolvedStrike(
    DclUnitKey Target,
    int StrikeIndex,
    IReadOnlyList<int> AppliedEffectIndexes,
    bool TargetKoAfterStrike);

internal sealed record DclCanonicalTransactionResult(
    DclActionTransaction Transaction,
    IReadOnlyList<(DclUnitKey Target, int StrikeIndex)> ExecutedStrikes,
    IReadOnlyList<(DclUnitKey Target, int StrikeIndex)> KoShortCircuitedStrikes,
    DclCanonicalReactionWindowResult? Reactions);

internal sealed record DclCanonicalCommitCallbacks(
    Action<DclStrikeCommitDecision, DclCanonicalResolvedStrike>? BeforeCompleteStrike = null,
    Action<DclActionTransaction>? AfterCommitBeforeReaction = null,
    Action<DclActionTransaction>? AfterReactionWindowOpened = null);

internal static class DclCanonicalTransactionExecutor
{
    public static DclCanonicalTransactionResult Commit(
        DclActionDeclaration declaration,
        DclActionProfile profile,
        IEnumerable<DclTargetResolutionSnapshot> targetSnapshots,
        IEnumerable<DclCanonicalResolvedStrike> resolvedStrikes,
        DclCanonicalCommitCallbacks? callbacks = null,
        DclCanonicalReactionWindowRequest? reactionWindow = null,
        DclStateRegistry? stateRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(targetSnapshots);
        ArgumentNullException.ThrowIfNull(resolvedStrikes);
        DclTargetResolutionSnapshot[] targets = targetSnapshots
            .OrderBy(target => target.Target.UnitSlot)
            .ThenBy(target => target.Target.CharacterId)
            .ToArray();
        if (stateRegistry is not null)
        {
            if (stateRegistry.BattleGeneration != declaration.Source.BattleGeneration)
                throw new ArgumentException(
                    "Confirmed transaction registry and ActionInstance must share one battle generation.",
                    nameof(stateRegistry));
            foreach (DclTargetResolutionSnapshot target in targets)
            {
                DclStateRegistryTargetSnapshot current = stateRegistry.CaptureTarget(target.Target);
                if (current.Revision != target.CombatStateRevision)
                    throw new ArgumentException(
                        $"Confirmed transaction target {target.Target} has stale custom-state revision " +
                        $"{target.CombatStateRevision}; current revision is {current.Revision}.",
                        nameof(targetSnapshots));
            }
        }
        DclCanonicalResolvedStrike[] strikes = resolvedStrikes.ToArray();
        var byIdentity = new Dictionary<(DclUnitKey Target, int StrikeIndex), DclCanonicalResolvedStrike>();
        foreach (DclCanonicalResolvedStrike strike in strikes)
        {
            if (!byIdentity.TryAdd((strike.Target, strike.StrikeIndex), strike))
                throw new ArgumentException("A canonical plan cannot contain duplicate target/Strike identities.", nameof(resolvedStrikes));
            if (strike.AppliedEffectIndexes is null || strike.AppliedEffectIndexes.Distinct().Count() != strike.AppliedEffectIndexes.Count)
                throw new ArgumentException("Applied effect indexes must be present and unique inside one Strike.", nameof(resolvedStrikes));
        }
        foreach (DclTargetResolutionSnapshot target in targets)
        {
            for (int strikeIndex = 0; strikeIndex < profile.TransactionProfile.StrikeCount; strikeIndex++)
                if (!byIdentity.ContainsKey((target.Target, strikeIndex)))
                    throw new ArgumentException("Every TargetResult must provide every declared Strike identity.", nameof(resolvedStrikes));
        }
        if (byIdentity.Count != targets.Length * profile.TransactionProfile.StrikeCount)
            throw new ArgumentException("Resolved Strikes contain a target outside the canonical TargetBatch.", nameof(resolvedStrikes));
        if (reactionWindow is not null)
            DclCanonicalReactionWindow.Preflight(declaration, reactionWindow);

        var transaction = new DclActionTransaction(declaration, profile);
        transaction.SnapshotTargetBatch(new DclTargetBatch(declaration.Source.BattleGeneration, targets));
        transaction.BeginPlanning();
        foreach (DclTargetResolutionSnapshot target in targets)
        {
            for (int strikeIndex = 0; strikeIndex < profile.TransactionProfile.StrikeCount; strikeIndex++)
            {
                DclCanonicalResolvedStrike strike = byIdentity[(target.Target, strikeIndex)];
                DclPlannedEffect[] effects = strike.AppliedEffectIndexes.Select(effectIndex =>
                {
                    if (effectIndex < 0 || effectIndex >= profile.Effects.Count)
                        throw new ArgumentOutOfRangeException(nameof(resolvedStrikes), "An applied effect index is outside the normalized effect list.");
                    DclEffectProfile effect = profile.Effects[effectIndex];
                    bool immediate = effect.Kind is DclEffectKind.Damage or DclEffectKind.Healing or
                        DclEffectKind.ResourceChange or DclEffectKind.CtChange or DclEffectKind.Revive ||
                        profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate;
                    return new DclPlannedEffect(effectIndex, effect, immediate);
                }).ToArray();
                transaction.PlanStrike(target.Target, strikeIndex, effects);
            }
        }
        transaction.SealPlan();
        transaction.BeginCommit();
        var executed = new List<(DclUnitKey Target, int StrikeIndex)>();
        var skipped = new List<(DclUnitKey Target, int StrikeIndex)>();
        while (transaction.BeginNextStrike() is { } decision)
        {
            var identity = (decision.Strike.Target, decision.Strike.StrikeIndex);
            if (!decision.ExecuteMechanics)
            {
                skipped.Add(identity);
                continue;
            }
            executed.Add(identity);
            DclCanonicalResolvedStrike resolved = byIdentity[identity];
            callbacks?.BeforeCompleteStrike?.Invoke(decision, resolved);
            transaction.CompleteActiveStrike(resolved.TargetKoAfterStrike);
        }
        transaction.CompleteCommit();
        callbacks?.AfterCommitBeforeReaction?.Invoke(transaction);
        transaction.OpenReactionWindow();
        DclCanonicalReactionWindowResult? reactions = reactionWindow is null
            ? null
            : DclCanonicalReactionWindow.Resolve(transaction.Declaration, reactionWindow);
        callbacks?.AfterReactionWindowOpened?.Invoke(transaction);
        transaction.Settle();
        return new DclCanonicalTransactionResult(transaction, executed, skipped, reactions);
    }
}
