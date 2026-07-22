namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativePhysicalTargetInputs(
    DclUnitKey Target,
    int EffectiveHtModifier = 0,
    int ConcentrationStatePenaltyMagnitude = 0,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0);

internal sealed record DclCanonicalNativePhysicalRangedInputs(
    int HorizontalTiles,
    int LocationPenaltyMagnitude,
    bool NativeRangeLegal,
    bool NativeTrajectoryLegal,
    bool MovedBeforeShot);

internal sealed record DclCanonicalNativePhysicalStrikeInputs(
    DclUnitKey Target,
    int StrikeIndex,
    int BaseWeaponSkill,
    IReadOnlyList<DclDefenseCandidate> DefenseCandidates,
    DclPhysicalLocation? EffectOwnedLocation = null,
    bool DirectConcentrationCancellation = false,
    int AuthoredForcedDisplacement = 0,
    DclCanonicalNativePhysicalRangedInputs? Ranged = null,
    IReadOnlyList<DclCanonicalPhysicalStatusRiderInput>? StatusRiders = null,
    DclCanonicalInjuryMovementBranchSet? InjuryMovementBranches = null,
    DclCanonicalInjuryMovementBranchForest? InjuryMovementBranchForest = null,
    DclAptitudeTier? SkillAptitudeTier = null,
    int? SkillJobLevel = null,
    int ExplicitSkillModifier = 0);

internal sealed record DclCanonicalNativePhysicalActionInputs(
    int WeaponItemId,
    string WeaponResourceKey,
    DclUnitKey? DeclaredTarget,
    DclBattleTile? FixedTile,
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
/// Converts one complete admitted physical sweep sequence and one synchronized native snapshot
/// batch into the deterministic physical coordinator request. Weapon skill, defense candidates,
/// native route verdicts, and Rider materialization remain explicit because they belong to
/// command/job/equipment-slot mechanisms outside the unit-row projection.
/// </summary>
internal static class DclCanonicalNativePhysicalExecutionComposer
{
    public static DclCanonicalPhysicalExecutionRequest Compose(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativePhysicalActionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(admissions);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(inputs);
        if (admissions.Count == 0)
            throw new ArgumentException("Physical composition requires the complete admitted sweep sequence.", nameof(admissions));

        DclCanonicalNativeOuterSweepAdmission first = admissions[0];
        if (battle.Catalog.ResolveAbilityFamily(first.AbilityId) != DclCanonicalActionFamily.PhysicalDamage)
            throw new ArgumentException("Native physical composition requires the PhysicalDamage canonical family.", nameof(admissions));
        (DclAbilityBinding binding, DclActionProfile profile) = battle.Catalog.ResolveAbility(first.AbilityId);
        int strikeCount = admissions.Count;
        if (!DclAbilityBindingContract.SupportsEffectiveStrikeCount(binding, profile, strikeCount) ||
            !first.StartsAction || first.StrikeIndex != 0 ||
            !admissions[^1].CompletesNativeSweepSequence || first.Source != snapshots.Source)
            throw new ArgumentException("Physical composition requires one complete admitted sequence under the snapshot source.", nameof(admissions));

        DclUnitKey[] admittedTargets = first.Targets.ToArray();
        for (int index = 0; index < admissions.Count; index++)
        {
            DclCanonicalNativeOuterSweepAdmission admission = admissions[index];
            if (admission.ActionInstanceId != first.ActionInstanceId || admission.Source != first.Source ||
                admission.ActionType != first.ActionType || admission.AbilityId != first.AbilityId ||
                admission.StrikeIndex != index || admission.StartsAction != (index == 0) ||
                admission.CompletesNativeSweepSequence != (index == admissions.Count - 1) ||
                !admission.Targets.SequenceEqual(admittedTargets))
                throw new ArgumentException("Physical admitted sweeps do not form one exact contiguous outer action.", nameof(admissions));
        }

        DclCanonicalProtectionRedirectPlan? protectionRedirect = null;
        DclUnitKey[] resolutionTargets = admittedTargets;
        if (inputs.ProtectionRedirect is { } protectionCandidate)
        {
            if (profile.DeliveryProfile.Delivery != DclDelivery.PhysicalAttack ||
                profile.TargetProfile.TargetMode != DclTargetMode.Unit || admittedTargets.Length != 1 ||
                protectionCandidate.Attacker != first.Source ||
                protectionCandidate.DeclaredTarget != admittedTargets[0] ||
                protectionCandidate.Delivery != DclDelivery.PhysicalAttack)
                throw new ArgumentException(
                    "Physical protection routing requires one exact unit-target PhysicalAttack admission.",
                    nameof(inputs));
            if (!snapshots.Units.ContainsKey(protectionCandidate.Protector))
                throw new ArgumentException("Protection routing requires the protector in the synchronized snapshot batch.", nameof(inputs));
            protectionRedirect = battle.PlanProtectionRedirect(protectionCandidate);
            resolutionTargets = [protectionRedirect.Resolution.FinalTarget];
        }

        if (profile.ResourceProfile.BaseMpCost != 0 ||
            profile.ResourceProfile.MpCostMultiplier != DclRational.FromInteger(1) ||
            profile.ResourceProfile.OvercastPolicy != DclOvercastPolicy.Forbidden ||
            profile.TimingProfile.BaseCastCt != 0 || profile.TimingProfile.CastCtModifier != 0)
            throw new InvalidOperationException("The physical native composer cannot bypass MP, overcast, or charged declaration ownership.");
        if (string.IsNullOrWhiteSpace(inputs.WeaponResourceKey) || inputs.WeaponItemId < 0 ||
            inputs.Targets is null || inputs.Strikes is null)
            throw new ArgumentException("Physical action inputs contain an invalid weapon identity or named magnitude.", nameof(inputs));

        DclCanonicalNativeUnitSnapshotResult source = RequireUnit(snapshots, first.Source, "source");
        DclCanonicalPhysicalStrikeWeapon[] strikeWeapons = ResolveStrikeWeapons(
            battle,
            admissions,
            inputs,
            source.Equipment,
            strikeCount);
        IReadOnlyDictionary<int, DclWeaponMetadata> weapons = DclCanonicalPhysicalStrikeWeapons.ResolveMetadata(
            battle.Catalog,
            strikeWeapons);

        ValidateDeclaredTarget(profile.TargetProfile.TargetMode, inputs, snapshots, first.Source);
        DclCanonicalNativePhysicalTargetInputs[] targetInputs = inputs.Targets.ToArray();
        if (targetInputs.Select(target => target.Target).Distinct().Count() != targetInputs.Length ||
            targetInputs.Length != resolutionTargets.Length ||
            resolutionTargets.Any(target => targetInputs.All(candidate => candidate.Target != target)))
            throw new ArgumentException("Physical composition requires exactly one context per final routed target.", nameof(inputs));

        var targets = new List<DclTargetResolutionSnapshot>(targetInputs.Length);
        var contexts = new List<DclCanonicalPhysicalTargetContext>(targetInputs.Length);
        foreach (DclCanonicalNativePhysicalTargetInputs targetInput in targetInputs)
        {
            if (targetInput.ConcentrationStatePenaltyMagnitude < 0 ||
                targetInput.AimRetentionStatePenaltyMagnitude < 0)
                throw new ArgumentException("Physical target modifiers contain an invalid penalty magnitude.", nameof(inputs));
            DclCanonicalNativeUnitSnapshotResult target = RequireUnit(snapshots, targetInput.Target, "target");
            targets.Add(new DclTargetResolutionSnapshot(
                targetInput.Target,
                target.Unit.Target.CurrentHp,
                target.Unit.State.Revision,
                target.Unit.Target.DefenseResources));
            contexts.Add(DclCanonicalCombatSnapshotProjector.ResolvePhysicalTarget(
                target.Unit,
                targetInput.EffectiveHtModifier,
                concentrationModifier: 0,
                targetInput.ConcentrationStatePenaltyMagnitude,
                targetInput.AimRetentionModifier,
                targetInput.AimRetentionStatePenaltyMagnitude) with
            {
                MovementOrigin = target.Unit.Target.Tile,
            });
        }

        DclCanonicalNativePhysicalStrikeInputs[] strikeInputs = inputs.Strikes.ToArray();
        if (strikeInputs.Select(strike => (strike.Target, strike.StrikeIndex)).Distinct().Count() != strikeInputs.Length ||
            strikeInputs.Length != checked(resolutionTargets.Length * strikeCount))
            throw new ArgumentException("Physical composition requires exactly one input per final routed target/Strike.", nameof(inputs));

        var strikes = new List<DclCanonicalPhysicalStrikeInput>(strikeInputs.Length);
        foreach (DclUnitKey targetKey in resolutionTargets.OrderBy(target => target.UnitSlot).ThenBy(target => target.CharacterId))
        {
            DclCanonicalNativeUnitSnapshotResult target = RequireUnit(snapshots, targetKey, "target");
            for (int strikeIndex = 0; strikeIndex < strikeCount; strikeIndex++)
            {
                DclWeaponMetadata weapon = weapons[strikeIndex];
                DclCanonicalNativePhysicalStrikeInputs authored = strikeInputs.SingleOrDefault(strike =>
                    strike.Target == targetKey && strike.StrikeIndex == strikeIndex) ??
                    throw new ArgumentException("Physical composition is missing one admitted target/Strike input.", nameof(inputs));
                if (authored.BaseWeaponSkill < 0 || authored.AuthoredForcedDisplacement < 0 ||
                    authored.DefenseCandidates is null)
                    throw new ArgumentException("Physical Strike inputs contain an invalid skill, displacement, or defense set.", nameof(inputs));
                if (authored.InjuryMovementBranches is not null && authored.InjuryMovementBranchForest is not null)
                    throw new ArgumentException(
                        "Physical composition cannot own both single-origin and conditional-origin Injury branches.",
                        nameof(inputs));
                authored.InjuryMovementBranches?.Validate(targetKey);
                authored.InjuryMovementBranchForest?.Validate(targetKey);
                if (authored.InjuryMovementBranches is { } movementBranches &&
                    movementBranches.Origin != target.Unit.Target.Tile)
                    throw new ArgumentException(
                        "Physical Injury movement origin diverges from the synchronized target tile.", nameof(inputs));

                DclCanonicalRangedStrikeContext? ranged = null;
                DclSkillTrainingResult? weaponSkillTraining = null;
                if (inputs.SkillTrainingPolicy is not null &&
                    (authored.SkillAptitudeTier is not null || authored.SkillJobLevel is not null))
                    throw new ArgumentException(
                        "Physical Skill training projection must use either action policy or per-Strike explicit tier/level, not both.",
                        nameof(inputs));
                if (inputs.SkillTrainingPolicy is { } trainingPolicy)
                {
                    weaponSkillTraining = DclCanonicalCombatSnapshotProjector.ResolveWeaponSkillTraining(
                        source.Unit,
                        weapon,
                        trainingPolicy,
                        authored.ExplicitSkillModifier);
                    if (weaponSkillTraining.FinalScore != authored.BaseWeaponSkill)
                        throw new ArgumentException(
                            "Physical Skill training policy projection disagrees with the authored base weapon skill.",
                            nameof(inputs));
                }
                else if (authored.SkillAptitudeTier is not null ||
                    authored.SkillJobLevel is not null ||
                    authored.ExplicitSkillModifier != 0)
                {
                    if (authored.SkillAptitudeTier is not { } aptitudeTier ||
                        authored.SkillJobLevel is not { } skillJobLevel)
                        throw new ArgumentException(
                            "Physical Skill training projection requires both Aptitude Tier and native Job Level.",
                            nameof(inputs));
                    weaponSkillTraining = DclCanonicalCombatSnapshotProjector.ResolveWeaponSkillTraining(
                        source.Unit,
                        weapon,
                        aptitudeTier,
                        skillJobLevel,
                        authored.ExplicitSkillModifier);
                    if (weaponSkillTraining.FinalScore != authored.BaseWeaponSkill)
                        throw new ArgumentException(
                            "Physical Skill training projection disagrees with the authored base weapon skill.",
                            nameof(inputs));
                }
                int effectiveSkill = authored.BaseWeaponSkill;
                if (weapon.MaximumRange > 0 || weapon.RangedKind is not null)
                {
                    DclCanonicalNativePhysicalRangedInputs range = authored.Ranged ??
                        throw new ArgumentException("A ranged weapon Strike requires one exact native route snapshot.", nameof(inputs));
                    DclCanonicalRangedSnapshotMechanics projected = DclCanonicalCombatSnapshotProjector.ResolveRangedStrike(
                        source.Unit,
                        weapon,
                        targetKey,
                        authored.BaseWeaponSkill,
                        range.HorizontalTiles,
                        range.LocationPenaltyMagnitude,
                        range.NativeRangeLegal,
                        range.NativeTrajectoryLegal,
                        range.MovedBeforeShot);
                    effectiveSkill = projected.EffectiveAttackSkill;
                    ranged = projected.Context;
                }
                else if (authored.Ranged is not null)
                {
                    throw new ArgumentException("A melee weapon Strike cannot carry ranged route inputs.", nameof(inputs));
                }

                int applicableDr = DclCanonicalCombatSnapshotProjector.ResolveApplicableDr(
                    target.Equipment,
                    profile.DeliveryProfile.LocationPolicy,
                    authored.EffectOwnedLocation,
                    target.Unit.State);
                strikes.Add(new DclCanonicalPhysicalStrikeInput(
                    targetKey,
                    strikeIndex,
                    effectiveSkill,
                    AttackRoll: null,
                    authored.DefenseCandidates,
                    DefenseRoll: null,
                    DamageDice: null,
                    applicableDr,
                    MajorWoundHtRoll: null,
                    authored.DirectConcentrationCancellation,
                    authored.AuthoredForcedDisplacement,
                    ConcentrationRoll: null,
                    ranged,
                    AimRetentionRoll: null,
                    authored.StatusRiders,
                    authored.InjuryMovementBranches,
                    authored.InjuryMovementBranchForest,
                    weaponSkillTraining));
            }
        }

        var declaration = new DclActionDeclaration(
            first.ActionInstanceId,
            first.Source,
            profile.ActionId,
            profile.ProfileRevision,
            profile.TargetProfile.TargetMode,
            profile.TargetProfile.TargetMode == DclTargetMode.Caster ? null : inputs.DeclaredTarget,
            inputs.FixedTile,
            source.Unit.Target.Tile,
            inputs.PassedRangeCheck,
            inputs.PassedVerticalCheck,
            FinalMpCost: 0,
            ApprovedHpCap: 0,
            CastCt: 0,
            battle.CurrentGlobalCt,
            battle.CurrentGlobalCt);
        return new DclCanonicalPhysicalExecutionRequest(
            first.AbilityId,
            declaration,
            source.Equipment,
            strikeWeapons[0].WeaponItemId,
            strikeWeapons[0].WeaponResourceKey,
            source.Unit.Primary.St,
            targets,
            contexts,
            strikes,
            inputs.ReactionCandidates,
            inputs.IsUniversalNormalAttack,
            strikeWeapons,
            protectionRedirect);
    }

    private static DclCanonicalPhysicalStrikeWeapon[] ResolveStrikeWeapons(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        DclCanonicalNativePhysicalActionInputs inputs,
        DclCanonicalEquipmentSnapshot equipment,
        int strikeCount)
    {
        bool anyNative = admissions.Any(admission =>
            admission.ActiveWeaponItemId is not null || admission.ActiveWeaponHand is not null);
        bool completeNative = admissions.All(admission =>
            admission.ActiveWeaponItemId is >= 0 && admission.ActiveWeaponHand is not null);
        if (anyNative && !completeNative)
            throw new ArgumentException("Native physical admission exposed an incomplete per-Strike weapon/hand identity.", nameof(admissions));

        DclCanonicalPhysicalStrikeWeapon[] weapons;
        if (completeNative)
        {
            weapons = admissions.Select(admission => new DclCanonicalPhysicalStrikeWeapon(
                admission.StrikeIndex,
                admission.ActiveWeaponItemId!.Value,
                DclCanonicalPhysicalStrikeWeapons.ResolveCapturedHandResourceKey(
                    equipment,
                    admission.ActiveWeaponHand!.Value,
                    admission.ActiveWeaponItemId.Value)))
                .OrderBy(weapon => weapon.StrikeIndex)
                .ToArray();
            if (inputs.StrikeWeapons is { Count: > 0 })
            {
                DclCanonicalPhysicalStrikeWeapon[] explicitWeapons = DclCanonicalPhysicalStrikeWeapons.Normalize(
                    strikeCount,
                    inputs.WeaponItemId,
                    inputs.WeaponResourceKey,
                    inputs.StrikeWeapons);
                if (!explicitWeapons.SequenceEqual(weapons))
                    throw new ArgumentException("Explicit physical hand policy differs from the captured native per-Strike weapon identity.", nameof(inputs));
            }
        }
        else
        {
            weapons = DclCanonicalPhysicalStrikeWeapons.Normalize(
                strikeCount,
                inputs.WeaponItemId,
                inputs.WeaponResourceKey,
                inputs.StrikeWeapons);
        }
        DclCanonicalPhysicalStrikeWeapons.RequireEquipped(equipment, weapons);
        return weapons;
    }

    private static void ValidateDeclaredTarget(
        DclTargetMode mode,
        DclCanonicalNativePhysicalActionInputs inputs,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclUnitKey source)
    {
        switch (mode)
        {
            case DclTargetMode.Unit when inputs.DeclaredTarget is { } target && inputs.FixedTile is null:
                RequireUnit(snapshots, target, "declared target");
                return;
            case DclTargetMode.FixedTile when inputs.DeclaredTarget is null && inputs.FixedTile is not null:
                return;
            case DclTargetMode.Caster when inputs.DeclaredTarget is null && inputs.FixedTile is null &&
                                           snapshots.Units.ContainsKey(source):
                return;
            default:
                throw new ArgumentException("Physical declared target does not match the normalized target mode.", nameof(inputs));
        }
    }

    private static DclCanonicalNativeUnitSnapshotResult RequireUnit(
        DclCanonicalNativeSnapshotBatch snapshots,
        DclUnitKey unit,
        string role)
        => snapshots.Units.TryGetValue(unit, out DclCanonicalNativeUnitSnapshotResult? snapshot)
            ? snapshot
            : throw new ArgumentException($"The synchronized snapshot batch lacks the admitted {role}.", nameof(snapshots));
}
