namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeAreaActionPolicySource(
    string Tradition,
    int TraditionSkill,
    bool Learned,
    bool SourceUsable,
    bool PrerequisitesMet,
    bool OvercastConfirmed,
    int ExplicitCasterStatePenaltyMagnitude,
    IReadOnlyList<DclCanonicalNativeAreaTargetInputs> Targets,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    int? FixedTileHeight = null);

/// <summary>
/// Materializes AreaNumeric family policy inputs from a complete native Area admission sequence.
/// Declaration identity comes from the normalized target mode and captured native selected unit or
/// tile; per-target defense, resistance, status, movement, and Injury policy remain explicit.
/// </summary>
internal static class DclCanonicalNativeAreaActionPolicyProvider
{
    public static DclCanonicalNativeAreaActionInputs BuildForAdmissions(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        DclCanonicalNativeAreaActionPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(admissions);
        ArgumentNullException.ThrowIfNull(source);
        if (admissions.Count == 0)
            throw new ArgumentException("Area policy requires a complete admission sequence.", nameof(admissions));
        if (string.IsNullOrWhiteSpace(source.Tradition) ||
            source.TraditionSkill < 0 ||
            source.ExplicitCasterStatePenaltyMagnitude < 0 ||
            source.Targets is null)
            throw new ArgumentException("Area policy source contains invalid skill, penalty, or target inputs.", nameof(source));

        DclCanonicalNativeOuterSweepAdmission first = admissions[0];
        if (battle.Catalog.ResolveAbilityFamily(first.AbilityId) != DclCanonicalActionFamily.AreaNumeric)
            throw new ArgumentException("Area policy requires an AreaNumeric admitted action.", nameof(admissions));
        (_, DclActionProfile profile) = battle.Catalog.ResolveAbility(first.AbilityId);
        if (profile.TargetProfile.Area is null)
            throw new ArgumentException("Area policy requires a normalized Area target profile.", nameof(admissions));
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
                throw new ArgumentException("Area policy requires one complete contiguous admitted action.", nameof(admissions));
        }

        DclUnitKey? declaredTarget = null;
        DclBattleTile? fixedTile = null;
        int? fixedTileHeight = null;
        switch (profile.TargetProfile.TargetMode)
        {
            case DclTargetMode.Unit:
                declaredTarget = first.SelectedUnit ??
                    throw new ArgumentException("Unit-targeted Area policy requires the admitted selected unit.", nameof(admissions));
                if (!first.Targets.Contains(declaredTarget.Value))
                    throw new ArgumentException("Unit-targeted Area policy selected unit must belong to the admitted geometry.", nameof(admissions));
                break;
            case DclTargetMode.FixedTile:
                fixedTile = first.SelectedTile;
                fixedTileHeight = source.FixedTileHeight ??
                    throw new ArgumentException("Fixed-tile Area policy requires an explicit selected-tile height.", nameof(source));
                if (fixedTileHeight < 0)
                    throw new ArgumentOutOfRangeException(nameof(source), "Fixed-tile Area policy height cannot be negative.");
                break;
            case DclTargetMode.Caster:
                break;
        }

        return new DclCanonicalNativeAreaActionInputs(
            declaredTarget,
            fixedTile,
            fixedTileHeight,
            source.Tradition,
            source.TraditionSkill,
            source.Learned,
            source.SourceUsable,
            source.PrerequisitesMet,
            source.OvercastConfirmed,
            source.ExplicitCasterStatePenaltyMagnitude,
            source.Targets,
            source.ReactionCandidates);
    }
}
