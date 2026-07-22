namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Selects the exact family-policy provider for one retained native action from the already
/// classified canonical ability family. This boundary accepts only policy-source objects, never
/// already-composed request inputs, so a production callback cannot accidentally bypass the
/// admission-derived declaration checks.
/// </summary>
internal static class DclCanonicalNativeFamilyPolicyProvider
{
    public static object BuildForCapturedAction(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action,
        object familyPolicySource)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(familyPolicySource);
        DclCanonicalActionFamily family = battle.Catalog.ResolveAbilityFamily(action.AbilityId);
        return (family, familyPolicySource) switch
        {
            (DclCanonicalActionFamily.DirectNumeric, DclCanonicalNativeDirectActionPolicySource source) =>
                DclCanonicalNativeDirectActionPolicyProvider.BuildForCapturedUnitAction(action, source),
            (DclCanonicalActionFamily.PhysicalDamage, DclCanonicalNativePhysicalActionPolicySource source) =>
                DclCanonicalNativePhysicalActionPolicyProvider.BuildForAdmissions(battle, action.Admissions, source),
            (DclCanonicalActionFamily.AreaNumeric, DclCanonicalNativeAreaActionPolicySource source) =>
                DclCanonicalNativeAreaActionPolicyProvider.BuildForAdmissions(battle, action.Admissions, source),
            (DclCanonicalActionFamily.StatusApplication, DclCanonicalNativeStatusActionPolicySource source) =>
                DclCanonicalNativeAuxiliaryMagicPolicyProvider.BuildStatus(Single(action), source),
            (DclCanonicalActionFamily.StatusRemoval, DclCanonicalNativeSingleTargetMagicPolicySource source) =>
                DclCanonicalNativeAuxiliaryMagicPolicyProvider.BuildStatusRemoval(Single(action), source),
            (DclCanonicalActionFamily.Dispel, DclCanonicalNativeDispelActionPolicySource source) =>
                DclCanonicalNativeAuxiliaryMagicPolicyProvider.BuildDispel(Single(action), source),
            (DclCanonicalActionFamily.Quick, DclCanonicalNativeQuickActionPolicySource source) =>
                DclCanonicalNativeAuxiliaryMagicPolicyProvider.BuildQuick(Single(action), source),
            (DclCanonicalActionFamily.Revive, DclCanonicalNativeReviveActionPolicySource source) =>
                DclCanonicalNativeAuxiliaryMagicPolicyProvider.BuildRevive(Single(action), source),
            (DclCanonicalActionFamily.ForcedMovement, DclCanonicalNativeForcedMovementActionPolicySource source) =>
                DclCanonicalNativeAuxiliaryMagicPolicyProvider.BuildForcedMovement(Single(action), source),
            _ => throw new ArgumentException(
                $"Ability {action.AbilityId} family {family} requires its exact native policy-source type, not {familyPolicySource.GetType().Name}.",
                nameof(familyPolicySource)),
        };
    }

    private static DclCanonicalNativeOuterSweepAdmission Single(DclCanonicalNativeAdmittedAction action)
        => action.Admissions.Count == 1
            ? action.Admissions[0]
            : throw new ArgumentException("This canonical family requires exactly one admitted nonrepeat sweep.", nameof(action));
}
