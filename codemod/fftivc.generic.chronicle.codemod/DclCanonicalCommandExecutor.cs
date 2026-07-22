namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalCommandExecutionResult(
    DclActionDeclaration Declaration,
    DclActionTransaction Transaction,
    DclReequipDefenseSnapshot? ReequipSnapshot,
    IReadOnlyList<long> RemovedStateInstanceIds,
    DclCanonicalReactionWindowResult? Reactions);

internal static class DclCanonicalCommandExecutor
{
    public const string ReadyMechanism = "universal.ready";
    public const string StandUpMechanism = "universal.stand-up";

    public static DclCanonicalCommandExecutionResult Ready(
        DclCanonicalRuntimeCatalog runtime,
        DclActionDeclaration declaration,
        DclTargetResolutionSnapshot sourceSnapshot,
        DclTurnResources resources,
        DclWeaponActionState weapon,
        DclCanonicalReactionWindowRequest? reactionWindow = null,
        DclStateRegistry? stateRegistry = null)
    {
        DclActionProfile profile = ResolveSystemProfile(
            runtime,
            declaration,
            DclEffectKind.MechanismOwned,
            ReadyMechanism);
        RequireSelfTarget(declaration, sourceSnapshot, profile);
        if (!weapon.RequiresReadyAction)
            throw new InvalidOperationException("Ready is illegal unless the selected weapon is currently Unready.");
        DclCanonicalTransactionResult committed = DclCanonicalTransactionExecutor.Commit(
            declaration,
            profile,
            [sourceSnapshot],
            [new DclCanonicalResolvedStrike(sourceSnapshot.Target, 0, [0], TargetKoAfterStrike: false)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
                DclUniversalActions.Ready(resources, weapon)),
            reactionWindow,
            stateRegistry);
        return new DclCanonicalCommandExecutionResult(
            declaration,
            committed.Transaction,
            ReequipSnapshot: null,
            RemovedStateInstanceIds: [],
            committed.Reactions);
    }

    public static DclCanonicalCommandExecutionResult Reequip(
        DclCanonicalRuntimeCatalog runtime,
        DclActionDeclaration declaration,
        DclTargetResolutionSnapshot sourceSnapshot,
        DclTurnResources resources,
        DclReequipDefenseSnapshot currentDefenseState,
        DclCanonicalReactionWindowRequest? reactionWindow = null,
        DclStateRegistry? stateRegistry = null)
    {
        DclActionProfile profile = ResolveSystemProfile(
            runtime,
            declaration,
            DclEffectKind.Reequip,
            mechanismId: null);
        RequireSelfTarget(declaration, sourceSnapshot, profile);
        DclReequipDefenseSnapshot? result = null;
        DclCanonicalTransactionResult committed = DclCanonicalTransactionExecutor.Commit(
            declaration,
            profile,
            [sourceSnapshot],
            [new DclCanonicalResolvedStrike(sourceSnapshot.Target, 0, [0], TargetKoAfterStrike: false)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
                result = DclUniversalActions.Reequip(resources, currentDefenseState)),
            reactionWindow,
            stateRegistry);
        return new DclCanonicalCommandExecutionResult(
            declaration,
            committed.Transaction,
            result ?? throw new InvalidOperationException("Reequip commit did not produce its preserved defense snapshot."),
            RemovedStateInstanceIds: [],
            committed.Reactions);
    }

    public static DclCanonicalCommandExecutionResult StandUp(
        DclCanonicalRuntimeCatalog runtime,
        DclActionDeclaration declaration,
        DclTargetResolutionSnapshot sourceSnapshot,
        DclTurnResources resources,
        DclStateRegistry stateRegistry,
        string knockedDownStateKind,
        DclCanonicalReactionWindowRequest? reactionWindow = null)
    {
        DclActionProfile profile = ResolveSystemProfile(
            runtime,
            declaration,
            DclEffectKind.MechanismOwned,
            StandUpMechanism);
        RequireSelfTarget(declaration, sourceSnapshot, profile);
        if (stateRegistry.BattleGeneration != sourceSnapshot.Target.BattleGeneration)
            throw new ArgumentException("Stand Up registry and source must share one battle generation.", nameof(stateRegistry));
        bool knockedDown = stateRegistry.Instances.Any(instance =>
            instance.Target == sourceSnapshot.Target && StringComparer.Ordinal.Equals(instance.Kind, knockedDownStateKind));
        if (!knockedDown)
            throw new InvalidOperationException("Stand Up is illegal while the source is not Knocked Down.");
        IReadOnlyList<long> removed = [];
        DclCanonicalTransactionResult committed = DclCanonicalTransactionExecutor.Commit(
            declaration,
            profile,
            [sourceSnapshot],
            [new DclCanonicalResolvedStrike(sourceSnapshot.Target, 0, [0], TargetKoAfterStrike: false)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                DclUniversalActions.StandUp(resources, knockedDown: true);
                removed = stateRegistry.RemoveKind(sourceSnapshot.Target, knockedDownStateKind);
                if (removed.Count == 0)
                    throw new InvalidOperationException("Knocked Down disappeared before Stand Up committed.");
            }),
            reactionWindow,
            stateRegistry);
        return new DclCanonicalCommandExecutionResult(
            declaration,
            committed.Transaction,
            ReequipSnapshot: null,
            removed,
            committed.Reactions);
    }

    private static DclActionProfile ResolveSystemProfile(
        DclCanonicalRuntimeCatalog runtime,
        DclActionDeclaration declaration,
        DclEffectKind expectedKind,
        string? mechanismId)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(declaration);
        DclActionProfile profile = runtime.ResolveAction(declaration.ActionId, declaration.ProfileRevision);
        if (profile.SourceProfile.Source != DclActionSource.Other ||
            profile.DeliveryProfile.Delivery != DclDelivery.Other ||
            profile.ResourceProfile.BaseMpCost != 0 ||
            profile.TransactionProfile.StrikeCount != 1 ||
            profile.TransactionProfile.CastingRollCardinality != DclCastingRollCardinality.None ||
            profile.Effects.Count != 1 || profile.Effects[0] is not
            {
                Role: DclEffectRole.Carrier,
            } effect || effect.Kind != expectedKind ||
            !StringComparer.Ordinal.Equals(effect.MechanismId, mechanismId))
            throw new InvalidOperationException("The declaration does not reference the expected normalized universal command profile.");
        return profile;
    }

    private static void RequireSelfTarget(
        DclActionDeclaration declaration,
        DclTargetResolutionSnapshot sourceSnapshot,
        DclActionProfile profile)
    {
        if (profile.TargetProfile.TargetMode != DclTargetMode.Caster ||
            declaration.Source != sourceSnapshot.Target || declaration.TargetMode != DclTargetMode.Caster)
            throw new ArgumentException("Universal command source, self target, and TargetBatch identity must agree.");
    }
}
