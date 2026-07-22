namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeDirectActionPolicySource(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclDefenseOption Defense,
    int EffectiveTargetHtModifier = 0,
    int ConcentrationStatePenaltyMagnitude = 0,
    int AdditionalMagnitudeIntegerModifier = 0,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0,
    bool DirectConcentrationCancellation = false,
    int AuthoredForcedDisplacement = 0,
    bool ReflectionAlreadyConsumed = false,
    DclPhysicalLocation? EffectOwnedLocation = null,
    int? ResistanceScore = null,
    bool Immune = false,
    IReadOnlyList<DclCanonicalStatusRiderExecutionRequest>? StatusRiders = null,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    IReadOnlyList<DclDefenseCandidate>? TouchDefenseCandidates = null,
    DclTouchNativeRouteVerdict? TouchRouteVerdict = null,
    DclCanonicalInjuryMovementBranchSet? InjuryMovementBranches = null);

/// <summary>
/// Materializes the DirectNumeric policy-input shape from a retained unit-target native action.
/// The provider owns only explicit non-job policy facts that cannot be derived from synchronized
/// rows; it does not infer defense candidates, state penalties, riders, Reactions, or movement.
/// </summary>
internal static class DclCanonicalNativeDirectActionPolicyProvider
{
    public static DclCanonicalNativeDirectActionInputs BuildForCapturedUnitAction(
        DclCanonicalNativeAdmittedAction action,
        DclCanonicalNativeDirectActionPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(source);
        DclCanonicalNativeSingleTargetMagicInputs magic =
            DclCanonicalNativeSingleTargetMagicPolicyProvider.BuildForCapturedUnitAction(action, source.Magic);
        if (source.ConcentrationStatePenaltyMagnitude < 0 ||
            source.AimRetentionStatePenaltyMagnitude < 0 ||
            source.AuthoredForcedDisplacement < 0)
            throw new ArgumentException(
                "Direct policy source contains invalid state-penalty or displacement inputs.",
                nameof(source));
        source.InjuryMovementBranches?.Validate(action.Targets.Single());

        return new DclCanonicalNativeDirectActionInputs(
            magic.DeclaredTarget,
            magic.Tradition,
            magic.TraditionSkill,
            magic.ZodiacCompatibility,
            magic.TargetRelativePenaltyMagnitude,
            magic.Learned,
            magic.SourceUsable,
            magic.PrerequisitesMet,
            magic.OvercastConfirmed,
            source.Defense,
            magic.ExplicitCasterStatePenaltyMagnitude,
            source.EffectiveTargetHtModifier,
            source.ConcentrationStatePenaltyMagnitude,
            source.AdditionalMagnitudeIntegerModifier,
            source.AimRetentionModifier,
            source.AimRetentionStatePenaltyMagnitude,
            source.DirectConcentrationCancellation,
            source.AuthoredForcedDisplacement,
            source.ReflectionAlreadyConsumed,
            source.EffectOwnedLocation,
            source.ResistanceScore,
            source.Immune,
            source.StatusRiders,
            source.ReactionCandidates,
            source.TouchDefenseCandidates,
            source.TouchRouteVerdict,
            source.InjuryMovementBranches);
    }
}
