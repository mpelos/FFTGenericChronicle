namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeForcedMovementActionInputs(
    DclCanonicalNativeSingleTargetMagicInputs Magic,
    DclDefenseOption Defense,
    DclCanonicalNativeMovementVerdict NativeMovementVerdict,
    int? ResistanceScore = null,
    bool Immune = false,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    DclCanonicalConcentrationTargetContext? ConcentrationContext = null);

/// <summary>
/// Joins an admitted standalone ForcedMovement action to its synchronized target and the one
/// immutable native map verdict. Ordinary movement remains uninterrupted; only the final resolved
/// destination enters the canonical action.
/// </summary>
internal static class DclCanonicalNativeForcedMovementExecutionComposer
{
    public static DclCanonicalForcedMovementExecutionRequest Compose(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeForcedMovementActionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(inputs.NativeMovementVerdict);
        if (battle.Catalog.ResolveAbilityFamily(admission.AbilityId) != DclCanonicalActionFamily.ForcedMovement)
            throw new ArgumentException("Native ForcedMovement composition requires its canonical family.", nameof(admission));
        DclCanonicalNativeSingleTargetMagicSnapshot common =
            DclCanonicalNativeSingleTargetMagicComposer.Compose(battle, admission, snapshots, inputs.Magic);
        if (inputs.NativeMovementVerdict.Target != common.Target.Unit.Unit ||
            inputs.NativeMovementVerdict.Origin != common.Target.Unit.Target.Tile)
            throw new ArgumentException("ForcedMovement native verdict diverges from the synchronized target/origin.", nameof(inputs));
        return new DclCanonicalForcedMovementExecutionRequest(
            admission.AbilityId,
            common.DeclarationRequest,
            admission.ActionInstanceId,
            common.Target.Unit.Target,
            common.BaseSpellScore,
            common.TargetSpellScore,
            inputs.Defense,
            common.Source.Unit.CurrentMp,
            common.Source.Unit.Target.CurrentHp,
            inputs.NativeMovementVerdict,
            inputs.ResistanceScore,
            inputs.Immune,
            inputs.ReactionCandidates,
            inputs.ConcentrationContext);
    }
}
