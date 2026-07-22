namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Builds one deterministic named StatusRemoval request from the shared admitted single-target
/// declaration. Exact removable instance selection remains inside the revisioned state registry.
/// </summary>
internal static class DclCanonicalNativeStatusRemovalExecutionComposer
{
    public static DclCanonicalStatusRemovalExecutionInput Compose(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeSingleTargetMagicInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (battle.Catalog.ResolveAbilityFamily(admission.AbilityId) != DclCanonicalActionFamily.StatusRemoval)
            throw new ArgumentException("Native status-removal composition requires the StatusRemoval canonical family.", nameof(admission));
        DclCanonicalNativeSingleTargetMagicSnapshot common =
            DclCanonicalNativeSingleTargetMagicComposer.Compose(battle, admission, snapshots, inputs);
        if (common.Profile.DeliveryProfile.Delivery != DclDelivery.Beneficial ||
            common.Profile.Effects.Count != 1 || common.Profile.Effects[0] is not
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.StatusRemoval,
                ReferencedStateKind: not null,
            })
            throw new InvalidOperationException("The named StatusRemoval carrier lost its normalized beneficial shape.");
        return new DclCanonicalStatusRemovalExecutionInput(
            admission.AbilityId,
            common.DeclarationRequest,
            admission.ActionInstanceId,
            common.Target.Unit.Target,
            common.BaseSpellScore,
            common.TargetSpellScore,
            SharedCasterRoll: null,
            common.Source.Unit.CurrentMp,
            common.Source.Unit.Target.CurrentHp,
            battle.States);
    }
}
