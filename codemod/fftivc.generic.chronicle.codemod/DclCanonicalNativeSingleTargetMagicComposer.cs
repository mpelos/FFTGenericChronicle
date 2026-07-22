namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeSingleTargetMagicInputs(
    DclUnitKey DeclaredTarget,
    string Tradition,
    int TraditionSkill,
    DclZodiacCompatibility ZodiacCompatibility,
    int TargetRelativePenaltyMagnitude,
    bool Learned,
    bool SourceUsable,
    bool PrerequisitesMet,
    bool OvercastConfirmed,
    int ExplicitCasterStatePenaltyMagnitude = 0);

internal sealed record DclCanonicalNativeSingleTargetMagicSnapshot(
    DclCanonicalNativeOuterSweepAdmission Admission,
    DclActionProfile Profile,
    DclCanonicalNativeUnitSnapshotResult Source,
    DclCanonicalNativeUnitSnapshotResult Target,
    DclCanonicalMagicSourceSnapshotMechanics SourceMechanics,
    DclCastDeclarationRequest DeclarationRequest,
    int BaseSpellScore,
    int TargetSpellScore);

/// <summary>
/// Shared deterministic declaration boundary for one admitted, nonrepeat, unit-targeted magic or
/// magic-like action. It deliberately stops before delivery-specific defense, resistance, effect,
/// and materialization policy.
/// </summary>
internal static class DclCanonicalNativeSingleTargetMagicComposer
{
    public static DclCanonicalNativeSingleTargetMagicSnapshot Compose(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeSingleTargetMagicInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(admission);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(inputs);
        if (!admission.StartsAction || !admission.CompletesNativeSweepSequence || admission.StrikeIndex != 0 ||
            admission.Source != snapshots.Source || admission.Targets.Count != 1 ||
            admission.Targets[0] != inputs.DeclaredTarget)
            throw new ArgumentException("Single-target composition requires one complete admitted nonrepeat unit sweep.", nameof(admission));
        (_, DclActionProfile profile) = battle.Catalog.ResolveAbility(admission.AbilityId);
        if (profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit ||
            profile.TargetProfile.Area is not null)
            throw new InvalidOperationException("The shared single-target composer requires one non-Area unit-targeted Strike.");
        if (string.IsNullOrWhiteSpace(inputs.Tradition) || inputs.TraditionSkill < 0 ||
            inputs.TargetRelativePenaltyMagnitude < 0 || inputs.ExplicitCasterStatePenaltyMagnitude < 0)
            throw new ArgumentException("Explicit single-target skill and penalty inputs are invalid.", nameof(inputs));

        DclCanonicalNativeUnitSnapshotResult source = RequireUnit(snapshots, admission.Source, "source");
        DclCanonicalNativeUnitSnapshotResult target = RequireUnit(snapshots, inputs.DeclaredTarget, "target");
        DclCanonicalMagicSourceSnapshotMechanics sourceMechanics = DclCanonicalMagicSnapshotProjector.ResolveSource(
            profile,
            inputs.Tradition,
            source.Unit,
            source.Equipment);
        int baseSpellScore = checked(
            inputs.TraditionSkill + sourceMechanics.SpellScoreModifier - inputs.ExplicitCasterStatePenaltyMagnitude);
        int targetSpellScore = DclSpellResolution.TargetSpellScore(
            baseSpellScore,
            profile.SkillProfile.ZodiacSensitive,
            inputs.ZodiacCompatibility,
            inputs.TargetRelativePenaltyMagnitude);
        var declaration = new DclCastDeclarationRequest(
            profile,
            admission.Source,
            source.Unit.Target.Tile,
            source.Unit.Target.Height,
            target.Unit.Target,
            FixedTile: null,
            FixedTileHeight: null,
            inputs.Learned,
            inputs.SourceUsable,
            source.Unit.State.HasNativeEffective(DclCanonicalNativeStatuses.Silence),
            inputs.PrerequisitesMet,
            source.Unit.CurrentMp,
            source.Unit.Target.CurrentHp,
            inputs.OvercastConfirmed,
            battle.CurrentGlobalCt,
            sourceMechanics.CastCtModifiers,
            sourceMechanics.MpCostMultipliers);
        return new DclCanonicalNativeSingleTargetMagicSnapshot(
            admission,
            profile,
            source,
            target,
            sourceMechanics,
            declaration,
            baseSpellScore,
            targetSpellScore);
    }

    private static DclCanonicalNativeUnitSnapshotResult RequireUnit(
        DclCanonicalNativeSnapshotBatch snapshots,
        DclUnitKey unit,
        string role)
        => snapshots.Units.TryGetValue(unit, out DclCanonicalNativeUnitSnapshotResult? snapshot)
            ? snapshot
            : throw new ArgumentException($"The synchronized snapshot batch lacks the admitted {role}.", nameof(snapshots));
}
