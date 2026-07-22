namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeAreaTargetInputs(
    DclUnitKey Target,
    DclZodiacCompatibility ZodiacCompatibility,
    int TargetRelativePenaltyMagnitude,
    DclDefenseOption? Dodge,
    int? ResistanceScore,
    DclPhysicalLocation? EffectOwnedLocation = null,
    int EffectiveTargetHtModifier = 0,
    int ConcentrationStatePenaltyMagnitude = 0,
    int AdditionalMagnitudeIntegerModifier = 0,
    bool DirectConcentrationCancellation = false,
    int AuthoredForcedDisplacement = 0,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0,
    IReadOnlyList<DclCanonicalStatusRiderExecutionRequest>? StatusRiders = null,
    DclCanonicalNativeMovementVerdict? ForcedMovementVerdict = null,
    bool ForcedMovementImmune = false,
    DclCanonicalConcentrationTargetContext? ForcedMovementConcentrationContext = null,
    IReadOnlyList<DclCanonicalInjuryMovementBranchSet?>? InjuryMovementBranchesByStrike = null,
    IReadOnlyList<DclCanonicalInjuryMovementBranchForest?>? InjuryMovementBranchForestsByStrike = null);

internal sealed record DclCanonicalNativeAreaActionInputs(
    DclUnitKey? DeclaredTarget,
    DclBattleTile? FixedTile,
    int? FixedTileHeight,
    string Tradition,
    int TraditionSkill,
    bool Learned,
    bool SourceUsable,
    bool PrerequisitesMet,
    bool OvercastConfirmed,
    int ExplicitCasterStatePenaltyMagnitude,
    IReadOnlyList<DclCanonicalNativeAreaTargetInputs> Targets,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null);

/// <summary>
/// Joins one complete admitted Area sweep sequence to a single source-owned snapshot batch. Native
/// geometric membership is retained in admitted order; canonical allegiance/state filtering and
/// every target-local deterministic mechanic are then projected before battle-owned resolution.
/// </summary>
internal static class DclCanonicalNativeAreaExecutionComposer
{
    public static DclCanonicalAreaMagicExecutionRequest Compose(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeAreaActionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(admissions);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(inputs);
        if (admissions.Count == 0)
            throw new ArgumentException("Area composition requires the complete admitted sweep sequence.", nameof(admissions));

        DclCanonicalNativeOuterSweepAdmission first = admissions[0];
        if (battle.Catalog.ResolveAbilityFamily(first.AbilityId) != DclCanonicalActionFamily.AreaNumeric)
            throw new ArgumentException("Native Area composition requires the AreaNumeric canonical family.", nameof(admissions));
        (DclAbilityBinding _, DclActionProfile profile) = battle.Catalog.ResolveAbility(first.AbilityId);
        int strikeCount = profile.TransactionProfile.StrikeCount;
        if (profile.TargetProfile.Area is null || admissions.Count != strikeCount ||
            !first.StartsAction || first.StrikeIndex != 0 || !admissions[^1].CompletesNativeSweepSequence ||
            first.Source != snapshots.Source)
            throw new ArgumentException("Area composition requires one complete admitted sequence under the snapshot source.", nameof(admissions));

        DclUnitKey[] admittedTargets = first.Targets.ToArray();
        for (int index = 0; index < admissions.Count; index++)
        {
            DclCanonicalNativeOuterSweepAdmission admission = admissions[index];
            if (admission.ActionInstanceId != first.ActionInstanceId || admission.Source != first.Source ||
                admission.ActionType != first.ActionType || admission.AbilityId != first.AbilityId ||
                admission.StrikeIndex != index || admission.StartsAction != (index == 0) ||
                admission.CompletesNativeSweepSequence != (index == admissions.Count - 1) ||
                !admission.Targets.SequenceEqual(admittedTargets))
                throw new ArgumentException("Area admitted sweeps do not form one exact contiguous outer action.", nameof(admissions));
        }
        if (string.IsNullOrWhiteSpace(inputs.Tradition) || inputs.TraditionSkill < 0 ||
            inputs.ExplicitCasterStatePenaltyMagnitude < 0 || inputs.Targets is null)
            throw new ArgumentException("Explicit Area skill, state penalty, and target inputs are invalid.", nameof(inputs));

        DclCanonicalNativeUnitSnapshotResult source = RequireUnit(snapshots, first.Source, "source");
        DclCanonicalNativeUnitSnapshotResult declared = ResolveDeclared(profile, snapshots, source, inputs);
        DclCanonicalMagicSourceSnapshotMechanics sourceMechanics = DclCanonicalMagicSnapshotProjector.ResolveSource(
            profile,
            inputs.Tradition,
            source.Unit,
            source.Equipment);
        int baseSpellScore = checked(
            inputs.TraditionSkill + sourceMechanics.SpellScoreModifier - inputs.ExplicitCasterStatePenaltyMagnitude);
        bool sourceSilenced = source.Unit.State.HasNativeEffective(DclCanonicalNativeStatuses.Silence);

        DclTargetCandidate? declaredTarget = profile.TargetProfile.TargetMode == DclTargetMode.Unit
            ? declared.Unit.Target
            : null;
        var declaration = new DclCastDeclarationRequest(
            profile,
            first.Source,
            source.Unit.Target.Tile,
            source.Unit.Target.Height,
            declaredTarget,
            inputs.FixedTile,
            inputs.FixedTileHeight,
            inputs.Learned,
            inputs.SourceUsable,
            sourceSilenced,
            inputs.PrerequisitesMet,
            source.Unit.CurrentMp,
            source.Unit.Target.CurrentHp,
            inputs.OvercastConfirmed,
            battle.CurrentGlobalCt,
            sourceMechanics.CastCtModifiers,
            sourceMechanics.MpCostMultipliers);

        IReadOnlyDictionary<DclUnitKey, DclTargetCandidate> currentUnits = snapshots.Units
            .ToDictionary(pair => pair.Key, pair => pair.Value.Unit.Target);
        DclTargetCandidate[] nativeMembers = admittedTargets
            .Select(target => RequireUnit(snapshots, target, "geometric member").Unit.Target)
            .ToArray();
        DclTargetBatch filtered = DclMagicTargeting.SnapshotAreaTargets(
            battle.BattleGeneration,
            nativeMembers,
            profile.TargetProfile.AllegiancePolicy,
            profile.TargetProfile.EligibleTargetStates);
        DclCanonicalNativeAreaTargetInputs[] targetInputs = inputs.Targets.ToArray();
        if (targetInputs.Select(target => target.Target).Distinct().Count() != targetInputs.Length ||
            !targetInputs.Select(target => target.Target).ToHashSet()
                .SetEquals(filtered.Targets.Select(target => target.Target)))
            throw new ArgumentException("Area target inputs must match the stable filtered admitted TargetBatch.", nameof(inputs));

        var targets = new List<DclCanonicalAreaTargetExecutionRequest>(targetInputs.Length);
        foreach (DclTargetResolutionSnapshot filteredTarget in filtered.Targets)
        {
            DclCanonicalNativeAreaTargetInputs targetInput = targetInputs.Single(target =>
                target.Target == filteredTarget.Target);
            if (targetInput.TargetRelativePenaltyMagnitude < 0 || targetInput.ConcentrationStatePenaltyMagnitude < 0 ||
                targetInput.AuthoredForcedDisplacement < 0 || targetInput.AimRetentionStatePenaltyMagnitude < 0)
                throw new ArgumentException("Area target contains an invalid named penalty or displacement.", nameof(inputs));
            DclCanonicalNativeUnitSnapshotResult target = RequireUnit(snapshots, targetInput.Target, "target");
            foreach (DclCanonicalInjuryMovementBranchSet branches in
                targetInput.InjuryMovementBranchesByStrike?.OfType<DclCanonicalInjuryMovementBranchSet>() ?? [])
            {
                branches.Validate(targetInput.Target);
                if (branches.Origin != target.Unit.Target.Tile)
                    throw new ArgumentException(
                        "Area Injury movement origin diverges from the synchronized target tile.", nameof(inputs));
            }
            if (targetInput.InjuryMovementBranchesByStrike is not null &&
                targetInput.InjuryMovementBranchForestsByStrike is not null)
                throw new ArgumentException(
                    "Area native composition cannot own both single-origin and conditional-origin Injury branches.",
                    nameof(inputs));
            foreach (DclCanonicalInjuryMovementBranchForest forest in
                targetInput.InjuryMovementBranchForestsByStrike?
                    .OfType<DclCanonicalInjuryMovementBranchForest>() ?? [])
                forest.Validate(targetInput.Target);
            DclCanonicalMagicSnapshotMechanics mechanics = DclCanonicalMagicSnapshotProjector.Resolve(
                profile,
                inputs.Tradition,
                source.Unit,
                source.Equipment,
                declared.Unit,
                target.Unit,
                target.Equipment,
                targetInput.EffectOwnedLocation);
            int targetSpellScore = DclSpellResolution.TargetSpellScore(
                baseSpellScore,
                profile.SkillProfile.ZodiacSensitive,
                targetInput.ZodiacCompatibility,
                targetInput.TargetRelativePenaltyMagnitude);
            DclCanonicalInjuryTargetContext? injury = profile.MagnitudeProfile is DclDamageMagnitude
                ? DclCanonicalCombatSnapshotProjector.ResolveInjuryTarget(
                    target.Unit,
                    targetInput.EffectiveTargetHtModifier,
                    mechanics.ConcentrationModifier,
                    targetInput.ConcentrationStatePenaltyMagnitude)
                : null;
            targets.Add(new DclCanonicalAreaTargetExecutionRequest(
                target.Unit.Target,
                targetSpellScore,
                targetInput.Dodge,
                targetInput.ResistanceScore,
                source.Unit.Primary.Iq,
                mechanics.Affinity,
                mechanics.FaithMagnitude,
                mechanics.TargetHasShell,
                target.Unit.Secondary.MaxHp,
                mechanics.FireEffect,
                mechanics.OilContributed,
                mechanics.ApplicableDr,
                injury,
                checked(mechanics.MagnitudeIntegerModifier + targetInput.AdditionalMagnitudeIntegerModifier),
                targetInput.DirectConcentrationCancellation,
                targetInput.AuthoredForcedDisplacement,
                targetInput.AimRetentionModifier,
                targetInput.AimRetentionStatePenaltyMagnitude,
                targetInput.StatusRiders,
                profile.MagnitudeProfile is DclFixedResourceMagnitude
                    ? ResourcePools(target.Unit)
                    : null,
                targetInput.ForcedMovementVerdict,
                targetInput.ForcedMovementImmune,
                targetInput.ForcedMovementConcentrationContext,
                targetInput.InjuryMovementBranchesByStrike,
                targetInput.InjuryMovementBranchForestsByStrike));
        }

        return new DclCanonicalAreaMagicExecutionRequest(
            first.AbilityId,
            declaration,
            first.ActionInstanceId,
            currentUnits,
            nativeMembers,
            baseSpellScore,
            targets,
            source.Unit.CurrentMp,
            source.Unit.Target.CurrentHp,
            inputs.ReactionCandidates,
            profile.MagnitudeProfile is DclFixedResourceMagnitude
                ? ResourcePools(source.Unit)
                : null);
    }

    private static DclCanonicalNativeUnitSnapshotResult ResolveDeclared(
        DclActionProfile profile,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeUnitSnapshotResult source,
        DclCanonicalNativeAreaActionInputs inputs)
        => profile.TargetProfile.TargetMode switch
        {
            DclTargetMode.Unit when inputs.DeclaredTarget is { } target &&
                                    inputs.FixedTile is null && inputs.FixedTileHeight is null =>
                RequireUnit(snapshots, target, "declared target"),
            DclTargetMode.FixedTile when inputs.DeclaredTarget is null &&
                                         inputs.FixedTile is not null && inputs.FixedTileHeight is not null => source,
            DclTargetMode.Caster when inputs.DeclaredTarget is null &&
                                      inputs.FixedTile is null && inputs.FixedTileHeight is null => source,
            _ => throw new ArgumentException("Area declared target does not match the normalized target mode.", nameof(inputs)),
        };

    private static DclCanonicalNativeUnitSnapshotResult RequireUnit(
        DclCanonicalNativeSnapshotBatch snapshots,
        DclUnitKey unit,
        string role)
        => snapshots.Units.TryGetValue(unit, out DclCanonicalNativeUnitSnapshotResult? snapshot)
            ? snapshot
            : throw new ArgumentException($"The synchronized snapshot batch lacks the admitted {role}.", nameof(snapshots));

    private static DclCanonicalResourcePoolSnapshot ResourcePools(DclCanonicalNativeUnitProjection unit)
        => new(
            unit.Target.CurrentHp,
            unit.Secondary.MaxHp,
            unit.CurrentMp,
            unit.Secondary.MaxMp,
            unit.Target.States.HasFlag(DclEligibleTargetStates.Undead));
}
