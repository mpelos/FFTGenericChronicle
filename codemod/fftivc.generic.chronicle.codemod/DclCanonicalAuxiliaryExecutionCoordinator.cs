namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalPublishedAuxiliaryExecution<TResult>(
    TResult Result,
    DclCanonicalNativeActionApplication Application);

/// <summary>
/// Atomic resolve-and-publish boundary for canonical non-damage action families. Inputs remain
/// deterministic; each executor receives the same battle owner whose immutable binding is used for
/// auxiliary native projection and publication.
/// </summary>
internal static class DclCanonicalAuxiliaryExecutionCoordinator
{
    public static DclCanonicalPublishedAuxiliaryExecution<DclCanonicalStatusExecutionResult> ResolveStatus(
        DclCanonicalBattleRuntime battle,
        DclCanonicalStatusExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactionCandidates = null)
    {
        RequireUnbound(battle, input.ExecutionBattle, input.ReactionWindow);
        RequireActionLegal(battle, input.AbilityId, input.DeclarationRequest.Caster, input.Target);
        DclCanonicalStatusExecutionResult result = DclCanonicalStatusExecutor.Resolve(
            battle.Catalog,
            input with
            {
                ExecutionBattle = battle,
                ReactionWindow = DclCanonicalReactionWindow.ConfirmedRequest(battle, reactionCandidates),
            });
        return new(result, DclCanonicalNativeExecutionPublisher.PublishStatus(battle, input.AbilityId, result));
    }

    public static DclCanonicalPublishedAuxiliaryExecution<DclCanonicalStatusRemovalExecutionResult> ResolveStatusRemoval(
        DclCanonicalBattleRuntime battle,
        DclCanonicalStatusRemovalExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactionCandidates = null)
    {
        RequireUnbound(battle, input.ExecutionBattle, input.ReactionWindow);
        RequireActionLegal(battle, input.AbilityId, input.DeclarationRequest.Caster, input.Target);
        DclCanonicalStatusRemovalExecutionResult result = DclCanonicalStatusRemovalExecutor.Resolve(
            battle.Catalog,
            input with
            {
                ExecutionBattle = battle,
                ReactionWindow = DclCanonicalReactionWindow.ConfirmedRequest(battle, reactionCandidates),
            });
        return new(result, DclCanonicalNativeExecutionPublisher.PublishStatusRemoval(battle, input.AbilityId, result));
    }

    public static DclCanonicalPublishedAuxiliaryExecution<DclCanonicalDispelExecutionResult> ResolveDispel(
        DclCanonicalBattleRuntime battle,
        DclCanonicalDispelExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactionCandidates = null)
    {
        RequireUnbound(battle, input.ExecutionBattle, input.ReactionWindow);
        RequireActionLegal(battle, input.AbilityId, input.DeclarationRequest.Caster, input.Target);
        DclCanonicalDispelExecutionResult result = DclCanonicalDispelExecutor.Resolve(
            battle.Catalog,
            input with
            {
                ExecutionBattle = battle,
                ReactionWindow = DclCanonicalReactionWindow.ConfirmedRequest(battle, reactionCandidates),
            });
        return new(result, DclCanonicalNativeExecutionPublisher.PublishDispel(battle, input.AbilityId, result));
    }

    public static DclCanonicalPublishedAuxiliaryExecution<DclCanonicalQuickExecutionResult> ResolveQuick(
        DclCanonicalBattleRuntime battle,
        DclCanonicalQuickExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactionCandidates = null)
    {
        RequireUnbound(battle, input.ExecutionBattle, input.ReactionWindow);
        RequireActionLegal(battle, input.AbilityId, input.DeclarationRequest.Caster, input.Target);
        DclCanonicalQuickExecutionResult result = DclCanonicalQuickExecutor.Resolve(
            battle.Catalog,
            input with
            {
                ExecutionBattle = battle,
                ReactionWindow = DclCanonicalReactionWindow.ConfirmedRequest(battle, reactionCandidates),
            });
        return new(result, DclCanonicalNativeExecutionPublisher.PublishQuick(battle, input.AbilityId, result));
    }

    public static DclCanonicalPublishedAuxiliaryExecution<DclCanonicalReviveExecutionResult> ResolveRevive(
        DclCanonicalBattleRuntime battle,
        DclCanonicalReviveExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactionCandidates = null)
    {
        RequireUnbound(battle, input.ExecutionBattle, input.ReactionWindow);
        RequireActionLegal(battle, input.AbilityId, input.DeclarationRequest.Caster, input.Target);
        DclCanonicalReviveExecutionResult result = DclCanonicalReviveExecutor.Resolve(
            battle.Catalog,
            input with
            {
                ExecutionBattle = battle,
                ReactionWindow = DclCanonicalReactionWindow.ConfirmedRequest(battle, reactionCandidates),
            });
        return new(result, DclCanonicalNativeExecutionPublisher.PublishRevive(battle, input.AbilityId, result));
    }

    private static void RequireUnbound(
        DclCanonicalBattleRuntime battle,
        DclCanonicalBattleRuntime? suppliedBattle,
        DclCanonicalReactionWindowRequest? suppliedReactionWindow)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (suppliedBattle is not null || suppliedReactionWindow is not null)
            throw new ArgumentException(
                "Auxiliary coordination accepts deterministic inputs only; the coordinator supplies the battle and Reaction-window owners.",
                nameof(suppliedBattle));
    }

    private static void RequireActionLegal(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclUnitKey source,
        DclTargetCandidate target)
    {
        (_, DclActionProfile profile) = battle.Catalog.ResolveAbility(abilityId);
        if (battle.States.CaptureTarget(target.Unit).Revision != target.CombatStateRevision)
            throw new ArgumentException(
                "Auxiliary execution received a stale target custom-state revision.",
                nameof(target));
        DclCanonicalActionStateProjection.RequireActionLegal(
            DclCanonicalActionStateProjection.EvaluateTaunt(
                battle,
                source,
                profile.TimingProfile.ConsumesAction,
                isUniversalNormalAttack: false,
                target.Unit,
                normalAttackTargetLegal: false));
    }
}
