namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeStatusActionPolicySource(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclCanonicalStateMaterialization StateMaterialization,
    int JobMagicResistance = 0,
    int ExplicitStateResistanceModifier = 0,
    int? AuthoredResistanceScore = null);

internal sealed record DclCanonicalNativeQuickActionPolicySource(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclCtState TargetCt,
    DclQuickLockController QuickLocks,
    DclCanonicalStateMaterialization LockMaterialization);

internal sealed record DclCanonicalNativeReviveActionPolicySource(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclRational FaithMultiplier,
    DclUndeadInteractionTable UndeadInteractions,
    int? ResistanceScore = null,
    bool Immune = false,
    DclCanonicalStateMaterialization? StoredReraiseMaterialization = null);

internal sealed record DclCanonicalNativeDispelActionPolicySource(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    int FinalDispelScore,
    long? SelectedInstanceId = null);

internal sealed record DclCanonicalNativeForcedMovementActionPolicySource(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclDefenseOption Defense,
    DclCanonicalNativeMovementVerdict NativeMovementVerdict,
    int? ResistanceScore = null,
    bool Immune = false,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    DclCanonicalConcentrationTargetContext? ConcentrationContext = null);

/// <summary>
/// Materializes auxiliary single-target magic family policy inputs from one admitted unit-target
/// native action. The shared casting declaration is normalized once; family-specific state,
/// timeline, lifecycle, dispel-selection, and map verdict inputs remain explicit.
/// </summary>
internal static class DclCanonicalNativeAuxiliaryMagicPolicyProvider
{
    public static DclCanonicalNativeStatusActionInputs BuildStatus(
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeStatusActionPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new DclCanonicalNativeStatusActionInputs(
            Magic(admission, source.Magic),
            source.StateMaterialization,
            source.JobMagicResistance,
            source.ExplicitStateResistanceModifier,
            source.AuthoredResistanceScore);
    }

    public static DclCanonicalNativeSingleTargetMagicInputs BuildStatusRemoval(
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSingleTargetMagicPolicySource source)
        => Magic(admission, source);

    public static DclCanonicalNativeQuickActionInputs BuildQuick(
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeQuickActionPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new DclCanonicalNativeQuickActionInputs(
            Magic(admission, source.Magic),
            source.TargetCt,
            source.QuickLocks,
            source.LockMaterialization);
    }

    public static DclCanonicalNativeReviveActionInputs BuildRevive(
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeReviveActionPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new DclCanonicalNativeReviveActionInputs(
            Magic(admission, source.Magic),
            source.FaithMultiplier,
            source.UndeadInteractions,
            source.ResistanceScore,
            source.Immune,
            source.StoredReraiseMaterialization);
    }

    public static DclCanonicalNativeDispelActionInputs BuildDispel(
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeDispelActionPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new DclCanonicalNativeDispelActionInputs(
            Magic(admission, source.Magic),
            source.FinalDispelScore,
            source.SelectedInstanceId);
    }

    public static DclCanonicalNativeForcedMovementActionInputs BuildForcedMovement(
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeForcedMovementActionPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new DclCanonicalNativeForcedMovementActionInputs(
            Magic(admission, source.Magic),
            source.Defense,
            source.NativeMovementVerdict,
            source.ResistanceScore,
            source.Immune,
            source.ReactionCandidates,
            source.ConcentrationContext);
    }

    private static DclCanonicalNativeSingleTargetMagicInputs Magic(
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSingleTargetMagicPolicySource source)
        => DclCanonicalNativeSingleTargetMagicPolicyProvider.BuildForAdmission(admission, source);
}
