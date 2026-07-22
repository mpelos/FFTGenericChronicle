namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeSingleTargetMagicPolicySource(
    string Tradition,
    int TraditionSkill,
    DclZodiacCompatibility ZodiacCompatibility,
    int TargetRelativePenaltyMagnitude,
    bool Learned,
    bool SourceUsable,
    bool PrerequisitesMet,
    bool OvercastConfirmed,
    int ExplicitCasterStatePenaltyMagnitude = 0);

/// <summary>
/// Materializes the shared declaration-policy inputs for admitted single-target magic-like actions.
/// This boundary deliberately supplies only the common casting declaration facts; family-specific
/// defense, resistance, materialization, timeline, revive, dispel, and movement policy stays with
/// the family policy provider.
/// </summary>
internal static class DclCanonicalNativeSingleTargetMagicPolicyProvider
{
    public static DclCanonicalNativeSingleTargetMagicInputs BuildForCapturedUnitAction(
        DclCanonicalNativeAdmittedAction action,
        DclCanonicalNativeSingleTargetMagicPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(source);
        if (action.Admissions.Count != 1)
            throw new ArgumentException(
                "Captured single-target magic policy requires exactly one admitted nonrepeat sweep.",
                nameof(action));
        return BuildForAdmission(action.Admissions[0], source);
    }

    public static DclCanonicalNativeSingleTargetMagicInputs BuildForAdmission(
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSingleTargetMagicPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(admission);
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(source.Tradition) ||
            source.TraditionSkill < 0 ||
            source.TargetRelativePenaltyMagnitude < 0 ||
            source.ExplicitCasterStatePenaltyMagnitude < 0)
            throw new ArgumentException(
                "Single-target magic policy source contains invalid skill or penalty inputs.",
                nameof(source));

        if (!admission.StartsAction ||
            !admission.CompletesNativeSweepSequence ||
            admission.StrikeIndex != 0 ||
            admission.Targets.Count != 1)
            throw new ArgumentException(
                "Captured single-target magic policy requires one complete unit-targeted nonrepeat admission.",
                nameof(admission));
        DclUnitKey declaredTarget = admission.Targets[0];

        return new DclCanonicalNativeSingleTargetMagicInputs(
            declaredTarget,
            source.Tradition,
            source.TraditionSkill,
            source.ZodiacCompatibility,
            source.TargetRelativePenaltyMagnitude,
            source.Learned,
            source.SourceUsable,
            source.PrerequisitesMet,
            source.OvercastConfirmed,
            source.ExplicitCasterStatePenaltyMagnitude);
    }
}
