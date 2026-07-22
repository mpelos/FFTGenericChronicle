namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// The single publication boundary between a fully resolved canonical execution and its native
/// battle ledger. Projection and publication remain one operation so a callback cannot publish a
/// result under a different binding revision or accidentally publish the same resolution twice.
/// </summary>
internal static class DclCanonicalNativeExecutionPublisher
{
    public static DclCanonicalNativeActionApplication PublishMagic(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclUnitKey payer,
        DclCanonicalMagicExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(result);
        (DclAbilityBinding binding, _) = battle.Catalog.ResolveAbility(abilityId);
        DclCanonicalNativeCarrierProjection projection =
            DclCanonicalNativeCarrierProjector.ProjectMagic(battle.Catalog, abilityId, payer, result);
        return battle.NativeActions.Publish(result.Declaration.Source, binding, projection);
    }

    public static DclCanonicalNativeActionApplication PublishAreaMagic(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclUnitKey payer,
        DclCanonicalAreaMagicExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(result);
        (DclAbilityBinding binding, _) = battle.Catalog.ResolveAbility(abilityId);
        DclCanonicalNativeMultiCarrierProjection projection =
            DclCanonicalNativeCarrierProjector.ProjectAreaMagic(battle.Catalog, abilityId, payer, result);
        return battle.NativeActions.Publish(result.Declaration.Source, binding, projection);
    }

    public static DclCanonicalNativeActionApplication PublishPhysical(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalPhysicalExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(result);
        (DclAbilityBinding binding, _) = battle.Catalog.ResolveAbility(abilityId);
        DclCanonicalNativeMultiCarrierProjection projection =
            DclCanonicalNativeCarrierProjector.ProjectPhysical(battle.Catalog, abilityId, result);
        return battle.NativeActions.Publish(result.Commit.Transaction.Declaration.Source, binding, projection);
    }

    public static DclCanonicalNativeActionApplication PublishStatus(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalStatusExecutionResult result)
        => PublishAuxiliary(
            battle,
            abilityId,
            result.Declaration.Source,
            DclCanonicalNativeAuxiliaryProjector.ProjectStatus(battle.Catalog, abilityId, result));

    public static DclCanonicalNativeActionApplication PublishStatusRemoval(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalStatusRemovalExecutionResult result)
        => PublishAuxiliary(
            battle,
            abilityId,
            result.Declaration.Source,
            DclCanonicalNativeAuxiliaryProjector.ProjectStatusRemoval(battle.Catalog, abilityId, result));

    public static DclCanonicalNativeActionApplication PublishDispel(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalDispelExecutionResult result)
        => PublishAuxiliary(
            battle,
            abilityId,
            result.Declaration.Source,
            DclCanonicalNativeAuxiliaryProjector.ProjectDispel(battle.Catalog, abilityId, result));

    public static DclCanonicalNativeActionApplication PublishQuick(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalQuickExecutionResult result)
        => PublishAuxiliary(
            battle,
            abilityId,
            result.Declaration.Source,
            DclCanonicalNativeAuxiliaryProjector.ProjectQuick(battle.Catalog, abilityId, result));

    public static DclCanonicalNativeActionApplication PublishForcedMovement(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalForcedMovementExecutionResult result)
        => PublishAuxiliary(
            battle,
            abilityId,
            result.Declaration.Source,
            DclCanonicalNativeAuxiliaryProjector.ProjectForcedMovement(battle.Catalog, abilityId, result));

    public static DclCanonicalNativeActionApplication PublishRevive(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalReviveExecutionResult result)
        => PublishAuxiliary(
            battle,
            abilityId,
            result.Declaration.Source,
            DclCanonicalNativeAuxiliaryProjector.ProjectRevive(battle.Catalog, abilityId, result));

    public static DclCanonicalNativeActionApplication PublishReraiseTrigger(
        DclCanonicalBattleRuntime battle,
        DclCanonicalReraiseTriggerResult result)
        => PublishAuxiliary(
            battle,
            result.AbilityId,
            result.Target,
            DclCanonicalNativeAuxiliaryProjector.ProjectReraiseTrigger(battle.Catalog, result));

    private static DclCanonicalNativeActionApplication PublishAuxiliary(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclUnitKey source,
        DclCanonicalNativeMultiCarrierProjection projection)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(projection);
        (DclAbilityBinding binding, _) = battle.Catalog.ResolveAbility(abilityId);
        return battle.NativeActions.Publish(source, binding, projection);
    }
}
