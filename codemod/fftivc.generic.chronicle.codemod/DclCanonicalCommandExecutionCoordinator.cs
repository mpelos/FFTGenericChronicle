namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Confirmed semantic boundary for normalized system commands. These commands are not invented
/// native abilities: native menu/result carriers remain an integration concern, while this owner
/// guarantees current battle state, turn resources, Taunt legality, deterministic mutation, and
/// the one post-action Reaction window.
/// </summary>
internal static class DclCanonicalCommandExecutionCoordinator
{
    public static DclCanonicalCommandExecutionResult Ready(
        DclCanonicalBattleRuntime battle,
        DclActionDeclaration declaration,
        DclTargetResolutionSnapshot sourceSnapshot,
        string weaponResourceKey,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactionCandidates = null)
    {
        RequireCurrentLegalCommand(battle, declaration, sourceSnapshot);
        DclWeaponActionState weapon = battle.RegisteredWeaponState(declaration.Source, weaponResourceKey);
        return DclCanonicalCommandExecutor.Ready(
            battle.Catalog,
            declaration,
            sourceSnapshot,
            battle.TurnResources(declaration.Source),
            weapon,
            DclCanonicalReactionWindow.ConfirmedRequest(battle, reactionCandidates),
            battle.States);
    }

    public static DclCanonicalCommandExecutionResult Reequip(
        DclCanonicalBattleRuntime battle,
        DclActionDeclaration declaration,
        DclTargetResolutionSnapshot sourceSnapshot,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactionCandidates = null)
    {
        RequireCurrentLegalCommand(battle, declaration, sourceSnapshot);
        return DclCanonicalCommandExecutor.Reequip(
            battle.Catalog,
            declaration,
            sourceSnapshot,
            battle.TurnResources(declaration.Source),
            battle.CaptureReequipDefenseSnapshot(declaration.Source),
            DclCanonicalReactionWindow.ConfirmedRequest(battle, reactionCandidates),
            battle.States);
    }

    public static DclCanonicalCommandExecutionResult StandUp(
        DclCanonicalBattleRuntime battle,
        DclActionDeclaration declaration,
        DclTargetResolutionSnapshot sourceSnapshot,
        string knockedDownStateKind,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactionCandidates = null)
    {
        RequireCurrentLegalCommand(battle, declaration, sourceSnapshot);
        return DclCanonicalCommandExecutor.StandUp(
            battle.Catalog,
            declaration,
            sourceSnapshot,
            battle.TurnResources(declaration.Source),
            battle.States,
            knockedDownStateKind,
            DclCanonicalReactionWindow.ConfirmedRequest(battle, reactionCandidates));
    }

    private static DclActionProfile RequireCurrentLegalCommand(
        DclCanonicalBattleRuntime battle,
        DclActionDeclaration declaration,
        DclTargetResolutionSnapshot sourceSnapshot)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(sourceSnapshot);
        if (declaration.Source != sourceSnapshot.Target)
            throw new ArgumentException("Confirmed command source and self snapshot must agree.", nameof(sourceSnapshot));

        DclStateRegistryTargetSnapshot currentState = battle.States.CaptureTarget(declaration.Source);
        if (currentState.Revision != sourceSnapshot.CombatStateRevision)
            throw new ArgumentException(
                "Confirmed command received a stale source custom-state revision.",
                nameof(sourceSnapshot));
        DclDefenseResourceSnapshot currentDefense = battle.CaptureDefenseResources(
            declaration.Source,
            sourceSnapshot.DefenseResources.ParryAttemptCounts.Keys);
        if (!DclCanonicalBattleRuntime.DefenseResourcesEqual(currentDefense, sourceSnapshot.DefenseResources))
            throw new ArgumentException(
                "Confirmed command received a stale source defense-resource revision.",
                nameof(sourceSnapshot));

        DclActionProfile profile = battle.Catalog.ResolveAction(declaration.ActionId, declaration.ProfileRevision);
        DclCanonicalActionStateProjection.RequireActionLegal(
            DclCanonicalActionStateProjection.EvaluateTaunt(
                battle,
                declaration.Source,
                profile.TimingProfile.ConsumesAction,
                isUniversalNormalAttack: false,
                declaration.Source,
                normalAttackTargetLegal: false));
        return profile;
    }
}
