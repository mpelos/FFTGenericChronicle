namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeDirectActionInputs(
    DclUnitKey DeclaredTarget,
    string Tradition,
    int TraditionSkill,
    DclZodiacCompatibility ZodiacCompatibility,
    int TargetRelativePenaltyMagnitude,
    bool Learned,
    bool SourceUsable,
    bool PrerequisitesMet,
    bool OvercastConfirmed,
    DclDefenseOption Defense,
    int ExplicitCasterStatePenaltyMagnitude = 0,
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
/// Joins one admitted nonrepeat native sweep to its synchronized snapshot batch and the remaining
/// explicit non-job action inputs. Everything derivable from action, unit, equipment, state, pools,
/// Faith, position, and battle time is projected here exactly once.
/// </summary>
internal static class DclCanonicalNativeDirectExecutionComposer
{
    public static DclCanonicalMagicExecutionRequest Compose(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeDirectActionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(admission);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(inputs);
        if (battle.Catalog.ResolveAbilityFamily(admission.AbilityId) != DclCanonicalActionFamily.DirectNumeric)
            throw new ArgumentException("Native direct composition requires the DirectNumeric canonical family.", nameof(admission));
        if (!admission.StartsAction || !admission.CompletesNativeSweepSequence || admission.StrikeIndex != 0 ||
            admission.Source != snapshots.Source || admission.Targets.Count != 1)
            throw new ArgumentException("Direct composition requires one complete admitted nonrepeat outer sweep.", nameof(admission));

        (DclAbilityBinding _, DclActionProfile profile) = battle.Catalog.ResolveAbility(admission.AbilityId);
        if (profile.TargetProfile.TargetMode != DclTargetMode.Unit || profile.TargetProfile.Area is not null)
            throw new InvalidOperationException("The native direct composer requires one normalized unit-target action without Area.");
        if (string.IsNullOrWhiteSpace(inputs.Tradition) || inputs.TraditionSkill < 0 ||
            inputs.TargetRelativePenaltyMagnitude < 0 || inputs.ExplicitCasterStatePenaltyMagnitude < 0 ||
            inputs.ConcentrationStatePenaltyMagnitude < 0 || inputs.AimRetentionStatePenaltyMagnitude < 0 ||
            inputs.AuthoredForcedDisplacement < 0)
            throw new ArgumentException("Explicit direct-action skill, penalty, and displacement inputs are invalid.", nameof(inputs));

        DclUnitKey resolutionTargetKey = admission.Targets[0];
        DclCanonicalNativeUnitSnapshotResult source = RequireUnit(snapshots, admission.Source, "source");
        DclCanonicalNativeUnitSnapshotResult declared = RequireUnit(snapshots, inputs.DeclaredTarget, "declared target");
        DclCanonicalNativeUnitSnapshotResult resolution = RequireUnit(snapshots, resolutionTargetKey, "resolution target");
        if (source.Unit.Unit != admission.Source || declared.Unit.Unit != inputs.DeclaredTarget ||
            resolution.Unit.Unit != resolutionTargetKey)
            throw new InvalidOperationException("Native direct snapshots lost their stable UnitKey ownership.");
        inputs.InjuryMovementBranches?.Validate(resolutionTargetKey);
        if (inputs.InjuryMovementBranches is { } movementBranches &&
            movementBranches.Origin != resolution.Unit.Target.Tile)
            throw new ArgumentException(
                "Direct Injury movement origin diverges from the synchronized resolution target tile.", nameof(inputs));

        DclCanonicalMagicSnapshotMechanics mechanics = DclCanonicalMagicSnapshotProjector.Resolve(
            profile,
            inputs.Tradition,
            source.Unit,
            source.Equipment,
            declared.Unit,
            resolution.Unit,
            resolution.Equipment,
            inputs.EffectOwnedLocation);
        int baseSpellScore = checked(
            inputs.TraditionSkill + mechanics.SpellScoreModifier - inputs.ExplicitCasterStatePenaltyMagnitude);
        int targetSpellScore = DclSpellResolution.TargetSpellScore(
            baseSpellScore,
            profile.SkillProfile.ZodiacSensitive,
            inputs.ZodiacCompatibility,
            inputs.TargetRelativePenaltyMagnitude);
        bool sourceSilenced = source.Unit.State.HasNativeEffective(DclCanonicalNativeStatuses.Silence);

        var declaration = new DclCastDeclarationRequest(
            profile,
            admission.Source,
            source.Unit.Target.Tile,
            source.Unit.Target.Height,
            declared.Unit.Target,
            FixedTile: null,
            FixedTileHeight: null,
            inputs.Learned,
            inputs.SourceUsable,
            sourceSilenced,
            inputs.PrerequisitesMet,
            source.Unit.CurrentMp,
            source.Unit.Target.CurrentHp,
            inputs.OvercastConfirmed,
            battle.CurrentGlobalCt,
            mechanics.CastCtModifiers,
            mechanics.MpCostMultipliers);

        DclCanonicalInjuryTargetContext? injury = profile.MagnitudeProfile is DclDamageMagnitude
            ? DclCanonicalCombatSnapshotProjector.ResolveInjuryTarget(
                resolution.Unit,
                inputs.EffectiveTargetHtModifier,
                mechanics.ConcentrationModifier,
                inputs.ConcentrationStatePenaltyMagnitude)
            : null;
        bool isResourceChange = profile.MagnitudeProfile is DclFixedResourceMagnitude;
        DclCanonicalResourcePoolSnapshot? targetPools = isResourceChange ? Pools(resolution.Unit) : null;
        DclCanonicalResourcePoolSnapshot? sourcePools = isResourceChange ? Pools(source.Unit) : null;
        bool reflected = resolutionTargetKey != inputs.DeclaredTarget;

        return new DclCanonicalMagicExecutionRequest(
            admission.AbilityId,
            declaration,
            admission.ActionInstanceId,
            declared.Unit.Target,
            baseSpellScore,
            targetSpellScore,
            inputs.Defense,
            source.Unit.Primary.Iq,
            mechanics.Affinity,
            mechanics.FaithMagnitude,
            mechanics.TargetHasShell,
            resolution.Unit.Secondary.MaxHp,
            mechanics.FireEffect,
            mechanics.OilContributed,
            source.Unit.CurrentMp,
            source.Unit.Target.CurrentHp,
            inputs.StatusRiders,
            mechanics.DeclaredTargetHasReflect,
            inputs.ReflectionAlreadyConsumed,
            reflected ? resolution.Unit.Target : null,
            mechanics.ApplicableDr,
            injury,
            inputs.DirectConcentrationCancellation,
            inputs.AuthoredForcedDisplacement,
            checked(mechanics.MagnitudeIntegerModifier + inputs.AdditionalMagnitudeIntegerModifier),
            inputs.AimRetentionModifier,
            inputs.AimRetentionStatePenaltyMagnitude,
            targetPools,
            sourcePools,
            inputs.ResistanceScore,
            inputs.Immune,
            inputs.ReactionCandidates,
            inputs.TouchDefenseCandidates,
            inputs.TouchRouteVerdict,
            inputs.InjuryMovementBranches);
    }

    private static DclCanonicalNativeUnitSnapshotResult RequireUnit(
        DclCanonicalNativeSnapshotBatch snapshots,
        DclUnitKey unit,
        string role)
        => snapshots.Units.TryGetValue(unit, out DclCanonicalNativeUnitSnapshotResult? snapshot)
            ? snapshot
            : throw new ArgumentException($"The synchronized snapshot batch lacks the admitted {role}.", nameof(snapshots));

    private static DclCanonicalResourcePoolSnapshot Pools(DclCanonicalNativeUnitProjection unit)
        => new(
            unit.Target.CurrentHp,
            unit.Secondary.MaxHp,
            unit.CurrentMp,
            unit.Secondary.MaxMp,
            unit.Target.States.HasFlag(DclEligibleTargetStates.Undead));
}
