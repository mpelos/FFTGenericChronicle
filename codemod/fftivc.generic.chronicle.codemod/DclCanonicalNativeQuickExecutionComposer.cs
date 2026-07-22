namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeQuickActionInputs(
    DclCanonicalNativeSingleTargetMagicInputs Magic,
    DclCtState TargetCt,
    DclQuickLockController QuickLocks,
    DclCanonicalStateMaterialization LockMaterialization);

/// <summary>
/// Joins an admitted Quick carrier to the common synchronized declaration while retaining the
/// timeline-owned target clock and QuickLock controller as explicit identity-bearing inputs.
/// </summary>
internal static class DclCanonicalNativeQuickExecutionComposer
{
    public static DclCanonicalQuickExecutionInput Compose(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeQuickActionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(inputs.TargetCt);
        ArgumentNullException.ThrowIfNull(inputs.QuickLocks);
        if (battle.Catalog.ResolveAbilityFamily(admission.AbilityId) != DclCanonicalActionFamily.Quick)
            throw new ArgumentException("Native Quick composition requires the Quick canonical family.", nameof(admission));
        DclCanonicalNativeSingleTargetMagicSnapshot common =
            DclCanonicalNativeSingleTargetMagicComposer.Compose(battle, admission, snapshots, inputs.Magic);
        if (common.Profile.Effects.Count != 2 || common.Profile.Effects[1] is not
            {
                Kind: DclEffectKind.StatusApplication,
                ReferencedStateKind: { } lockStateKind,
            } || !battle.Catalog.Authoring.States.TryGetValue(lockStateKind, out DclStateDefinition? lockDefinition) ||
            !StringComparer.Ordinal.Equals(inputs.LockMaterialization.Payload.SchemaId, lockDefinition.PayloadSchema))
            throw new ArgumentException("QuickLock materialization does not match the normalized Quick action.", nameof(inputs));
        return new DclCanonicalQuickExecutionInput(
            admission.AbilityId,
            common.DeclarationRequest,
            admission.ActionInstanceId,
            common.Target.Unit.Target,
            common.BaseSpellScore,
            common.TargetSpellScore,
            SharedCasterRoll: null,
            common.Source.Unit.CurrentMp,
            common.Source.Unit.Target.CurrentHp,
            inputs.TargetCt,
            inputs.QuickLocks,
            battle.States,
            inputs.LockMaterialization);
    }
}
