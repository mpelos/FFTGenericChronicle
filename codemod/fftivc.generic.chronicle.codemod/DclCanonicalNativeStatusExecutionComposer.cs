namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeStatusActionInputs(
    DclCanonicalNativeSingleTargetMagicInputs Magic,
    DclCanonicalStateMaterialization StateMaterialization,
    int JobMagicResistance = 0,
    int ExplicitStateResistanceModifier = 0,
    int? AuthoredResistanceScore = null);

/// <summary>
/// Projects one admitted standalone StatusApplication carrier from the common declaration snapshot
/// plus the exact referenced state definition. Immunity and resistance are derived from the same
/// target state/equipment revision used by confirmed execution.
/// </summary>
internal static class DclCanonicalNativeStatusExecutionComposer
{
    public static DclCanonicalStatusExecutionInput Compose(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeStatusActionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (battle.Catalog.ResolveAbilityFamily(admission.AbilityId) != DclCanonicalActionFamily.StatusApplication)
            throw new ArgumentException("Native status composition requires the StatusApplication canonical family.", nameof(admission));
        DclCanonicalNativeSingleTargetMagicSnapshot common =
            DclCanonicalNativeSingleTargetMagicComposer.Compose(battle, admission, snapshots, inputs.Magic);
        if (common.Profile.Effects.Count != 1 || common.Profile.Effects[0] is not
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.StatusApplication,
                ReferencedStateKind: { } stateKind,
            } || !battle.Catalog.Authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition))
            throw new InvalidOperationException("The standalone status carrier lost its exact normalized state definition.");
        if (!StringComparer.Ordinal.Equals(inputs.StateMaterialization.Payload.SchemaId, definition.PayloadSchema))
            throw new ArgumentException("Status materialization payload does not match the referenced state schema.", nameof(inputs));

        DclCanonicalStatusSnapshotMechanics target = DclCanonicalCombatSnapshotProjector.ResolveStatusTarget(
            definition,
            common.Profile.DeliveryProfile.ResistanceCharacteristic ?? DclResistanceCharacteristic.None,
            common.Target.Unit,
            common.Target.Equipment,
            inputs.JobMagicResistance,
            inputs.ExplicitStateResistanceModifier,
            inputs.AuthoredResistanceScore);
        if (common.Profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect && target.ResistanceScore is null)
            throw new InvalidOperationException("Internal Direct status composition requires one exact target resistance score.");

        return new DclCanonicalStatusExecutionInput(
            admission.AbilityId,
            common.DeclarationRequest,
            admission.ActionInstanceId,
            common.Target.Unit.Target,
            common.BaseSpellScore,
            common.TargetSpellScore,
            SharedCasterRoll: null,
            target.ResistanceScore ?? 0,
            ResistanceRoll: null,
            target.Immune,
            common.Source.Unit.CurrentMp,
            common.Source.Unit.Target.CurrentHp,
            battle.States,
            inputs.StateMaterialization);
    }
}
