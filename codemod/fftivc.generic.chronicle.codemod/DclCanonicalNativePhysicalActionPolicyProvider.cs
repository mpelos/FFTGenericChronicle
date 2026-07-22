namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativePhysicalActionPolicySource(
    int WeaponItemId,
    string WeaponResourceKey,
    bool PassedRangeCheck,
    bool PassedVerticalCheck,
    IReadOnlyList<DclCanonicalNativePhysicalTargetInputs> Targets,
    IReadOnlyList<DclCanonicalNativePhysicalStrikeInputs> Strikes,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    bool IsUniversalNormalAttack = false,
    IReadOnlyList<DclCanonicalPhysicalStrikeWeapon>? StrikeWeapons = null,
    DclCanonicalProtectionRedirectCandidate? ProtectionRedirect = null,
    DclSkillTrainingPolicy? SkillTrainingPolicy = null);

/// <summary>
/// Materializes PhysicalDamage family policy inputs from a complete native admission sequence.
/// Native declaration identity comes from the selected unit or selected tile according to the
/// normalized target mode; weapon skill, defense candidates, route checks, protection redirects,
/// and Strike policy remain explicit.
/// </summary>
internal static class DclCanonicalNativePhysicalActionPolicyProvider
{
    public static DclCanonicalNativePhysicalActionInputs BuildForAdmissions(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        DclCanonicalNativePhysicalActionPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(admissions);
        ArgumentNullException.ThrowIfNull(source);
        if (admissions.Count == 0)
            throw new ArgumentException("Physical policy requires a complete admission sequence.", nameof(admissions));
        if (string.IsNullOrWhiteSpace(source.WeaponResourceKey) || source.WeaponItemId < 0 ||
            source.Targets is null || source.Strikes is null)
            throw new ArgumentException("Physical policy source contains an invalid explicit weapon or Strike policy.", nameof(source));

        DclCanonicalNativeOuterSweepAdmission first = admissions[0];
        if (battle.Catalog.ResolveAbilityFamily(first.AbilityId) != DclCanonicalActionFamily.PhysicalDamage)
            throw new ArgumentException("Physical policy requires a PhysicalDamage admitted action.", nameof(admissions));
        (_, DclActionProfile profile) = battle.Catalog.ResolveAbility(first.AbilityId);
        for (int index = 0; index < admissions.Count; index++)
        {
            DclCanonicalNativeOuterSweepAdmission admission = admissions[index];
            if (admission.ActionInstanceId != first.ActionInstanceId ||
                admission.Source != first.Source ||
                admission.ActionType != first.ActionType ||
                admission.AbilityId != first.AbilityId ||
                admission.StrikeIndex != index ||
                admission.StartsAction != (index == 0) ||
                admission.CompletesNativeSweepSequence != (index == admissions.Count - 1) ||
                admission.SelectedTile != first.SelectedTile ||
                admission.SelectedUnit != first.SelectedUnit ||
                !admission.Targets.SequenceEqual(first.Targets))
                throw new ArgumentException("Physical policy requires one complete contiguous admitted action.", nameof(admissions));
        }

        DclUnitKey? declaredTarget = null;
        DclBattleTile? fixedTile = null;
        switch (profile.TargetProfile.TargetMode)
        {
            case DclTargetMode.Unit:
                declaredTarget = first.SelectedUnit ??
                    throw new ArgumentException("Unit-targeted physical policy requires the admitted selected unit.", nameof(admissions));
                break;
            case DclTargetMode.FixedTile:
                fixedTile = first.SelectedTile;
                break;
        }

        if (source.Strikes.Select(strike => strike.StrikeIndex).Distinct().Count() != source.Strikes.Count)
            throw new ArgumentException("Physical policy source cannot repeat a StrikeIndex.", nameof(source));
        int[] admittedIndexes = admissions.Select(admission => admission.StrikeIndex).ToArray();
        if (!source.Strikes.Select(strike => strike.StrikeIndex).Order().SequenceEqual(admittedIndexes))
            throw new ArgumentException("Physical policy source must provide exactly one Strike policy per admitted Strike.", nameof(source));

        return new DclCanonicalNativePhysicalActionInputs(
            source.WeaponItemId,
            source.WeaponResourceKey,
            declaredTarget,
            fixedTile,
            source.PassedRangeCheck,
            source.PassedVerticalCheck,
            source.Targets,
            source.Strikes,
            source.ReactionCandidates,
            source.IsUniversalNormalAttack,
            source.StrikeWeapons,
            source.ProtectionRedirect,
            source.SkillTrainingPolicy);
    }
}
