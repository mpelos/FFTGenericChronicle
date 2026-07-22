namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeReviveActionInputs(
    DclCanonicalNativeSingleTargetMagicInputs Magic,
    DclRational FaithMultiplier,
    DclUndeadInteractionTable UndeadInteractions,
    int? ResistanceScore = null,
    bool Immune = false,
    DclCanonicalStateMaterialization? StoredReraiseMaterialization = null);

/// <summary>
/// Builds the deterministic immediate-Revive or Stored-Reraise request from one admitted target.
/// The native KO/Undead snapshot supplies lifecycle state; the authored revive faith factor and
/// complete Undead interaction table remain explicit normalized policy inputs.
/// </summary>
internal static class DclCanonicalNativeReviveExecutionComposer
{
    public static DclCanonicalReviveExecutionInput Compose(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeReviveActionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(inputs.UndeadInteractions);
        if (inputs.FaithMultiplier < DclRational.FromInteger(0))
            throw new ArgumentOutOfRangeException(nameof(inputs), "Revive Faith multiplier cannot be negative.");
        if (battle.Catalog.ResolveAbilityFamily(admission.AbilityId) != DclCanonicalActionFamily.Revive)
            throw new ArgumentException("Native Revive composition requires the Revive canonical family.", nameof(admission));
        DclCanonicalNativeSingleTargetMagicSnapshot common =
            DclCanonicalNativeSingleTargetMagicComposer.Compose(battle, admission, snapshots, inputs.Magic);
        DclReviveProfile revive = common.Profile.ReviveProfile ??
            throw new InvalidOperationException("The Revive carrier lost its normalized lifecycle profile.");
        bool stored = revive.Mode == DclReviveMode.StoredReraise;
        if (stored != (inputs.StoredReraiseMaterialization is not null))
            throw new ArgumentException(
                "Stored Reraise requires exactly one state materialization; immediate Revive permits none.",
                nameof(inputs));
        if (common.Profile.DeliveryProfile.Delivery == DclDelivery.Beneficial &&
            (inputs.ResistanceScore is not null || inputs.Immune))
            throw new ArgumentException("Beneficial Revive cannot own hostile resistance or immunity.", nameof(inputs));
        bool undead = common.Target.Unit.Target.States.HasFlag(DclEligibleTargetStates.Undead);
        return new DclCanonicalReviveExecutionInput(
            admission.AbilityId,
            common.DeclarationRequest,
            admission.ActionInstanceId,
            common.Target.Unit.Target,
            common.BaseSpellScore,
            common.TargetSpellScore,
            SharedCasterRoll: null,
            inputs.ResistanceScore,
            ResistanceRoll: null,
            inputs.Immune,
            RestoredHpDice: null,
            inputs.FaithMultiplier,
            undead,
            common.Target.Unit.Secondary.MaxHp,
            common.Source.Unit.CurrentMp,
            common.Source.Unit.Target.CurrentHp,
            inputs.UndeadInteractions,
            stored ? battle.States : null,
            inputs.StoredReraiseMaterialization);
    }
}
