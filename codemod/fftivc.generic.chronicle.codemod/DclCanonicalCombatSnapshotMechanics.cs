namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalRangedSnapshotMechanics(
    int EffectiveAttackSkill,
    DclCanonicalRangedStrikeContext Context,
    bool AimWillDischargeOrCancel);

internal sealed record DclCanonicalStatusSnapshotMechanics(
    bool Immune,
    int? ResistanceScore);

internal static class DclCanonicalCombatSnapshotProjector
{
    public static DclCanonicalInjuryTargetContext ResolveInjuryTarget(
        DclCanonicalNativeUnitProjection target,
        int effectiveHtModifier = 0,
        int concentrationModifier = 0,
        int concentrationStatePenaltyMagnitude = 0)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (concentrationStatePenaltyMagnitude < 0)
            throw new ArgumentOutOfRangeException(nameof(concentrationStatePenaltyMagnitude));
        DclCanonicalShockSnapshot? shock = target.State.Shock(target.Secondary.MaxHp);
        return new DclCanonicalInjuryTargetContext(
            target.Secondary.MaxHp,
            target.Primary.St,
            checked(target.Primary.Ht + effectiveHtModifier),
            shock?.UnexpiredInjury ?? 0,
            target.State.HasNativeEffective(DclCanonicalNativeStatuses.Charging),
            target.Secondary.Will,
            concentrationModifier,
            concentrationStatePenaltyMagnitude);
    }

    public static DclCanonicalPhysicalTargetContext ResolvePhysicalTarget(
        DclCanonicalNativeUnitProjection target,
        int effectiveHtModifier = 0,
        int concentrationModifier = 0,
        int concentrationStatePenaltyMagnitude = 0,
        int aimRetentionModifier = 0,
        int aimRetentionStatePenaltyMagnitude = 0)
    {
        DclCanonicalInjuryTargetContext injury = ResolveInjuryTarget(
            target,
            effectiveHtModifier,
            concentrationModifier,
            concentrationStatePenaltyMagnitude);
        return new DclCanonicalPhysicalTargetContext(
            target.Unit,
            injury.MaxHp,
            injury.TargetSt,
            injury.EffectiveHt,
            injury.UnexpiredShockInjury,
            injury.Charging,
            injury.Will,
            injury.ConcentrationModifier,
            injury.ConcentrationStatePenaltyMagnitude,
            aimRetentionModifier,
            aimRetentionStatePenaltyMagnitude);
    }

    public static DclSkillTrainingResult ResolveWeaponSkillTraining(
        DclCanonicalNativeUnitProjection source,
        DclWeaponMetadata weapon,
        DclSkillTrainingPolicy policy,
        int explicitSkillModifier = 0)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(weapon);
        ArgumentNullException.ThrowIfNull(policy);
        if (source.Progression.JobLevel is < 1 or > 8)
            throw new ArgumentException("Weapon Skill training requires a synchronized native Job Level.", nameof(source));
        DclAptitudeTier aptitudeTier = policy.Resolve(source.Progression.JobId, weapon.SkillFamily);
        return ResolveWeaponSkillTraining(
            source,
            weapon,
            aptitudeTier,
            source.Progression.JobLevel,
            explicitSkillModifier);
    }

    public static DclSkillTrainingResult ResolveWeaponSkillTraining(
        DclCanonicalNativeUnitProjection source,
        DclWeaponMetadata weapon,
        DclAptitudeTier aptitudeTier,
        int jobLevel,
        int explicitSkillModifier = 0)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(weapon);
        return DclSkillRules.ResolveTraining(new DclSkillTrainingInput(
            source.Primary.Dx,
            weapon.Difficulty,
            aptitudeTier,
            jobLevel,
            explicitSkillModifier));
    }

    public static int ResolveApplicableDr(
        DclCanonicalEquipmentSnapshot targetEquipment,
        DclLocationPolicy policy,
        DclPhysicalLocation? effectOwnedLocation = null,
        DclCanonicalStateSnapshot? targetState = null)
    {
        ArgumentNullException.ThrowIfNull(targetEquipment);
        DclPhysicalLocation location = policy switch
        {
            DclLocationPolicy.NormalCombined => DclPhysicalLocation.NormalCombined,
            DclLocationPolicy.Body => DclPhysicalLocation.Body,
            DclLocationPolicy.Head => DclPhysicalLocation.Head,
            DclLocationPolicy.EffectOwned when effectOwnedLocation is not null => effectOwnedLocation.Value,
            DclLocationPolicy.EffectOwned => throw new ArgumentException("Effect-owned DR requires an explicit resolved location.", nameof(effectOwnedLocation)),
            _ => throw new InvalidOperationException("The action has no usable physical location policy."),
        };
        int equipmentDr = targetEquipment.ApplicableDr(location);
        int stateModifier = targetState?.DefenseMechanics("body").DrModifier ?? 0;
        return Math.Max(0, checked(equipmentDr + stateModifier));
    }

    public static DclCanonicalRangedSnapshotMechanics ResolveRangedStrike(
        DclCanonicalNativeUnitProjection source,
        DclWeaponMetadata weapon,
        DclUnitKey target,
        int baseWeaponSkill,
        int horizontalTiles,
        int locationPenaltyMagnitude,
        bool nativeRangeLegal,
        bool nativeTrajectoryLegal,
        bool movedBeforeShot)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(weapon);
        if (weapon.MaximumRange <= 0 || weapon.RangedKind is null)
            throw new ArgumentException("Ranged snapshot projection requires ranged weapon metadata.", nameof(weapon));
        if (!target.IsValid || target.BattleGeneration != source.Unit.BattleGeneration)
            throw new ArgumentException("The ranged target must be a stable unit in the same battle.", nameof(target));
        if (horizontalTiles < 0 || locationPenaltyMagnitude < 0)
            throw new ArgumentOutOfRangeException(nameof(horizontalTiles));

        DclAimState? aim = null;
        long? aimInstanceId = null;
        if (source.State.TryGetAim(out DclStateInstance? aimInstance, out DclAimStatePayload? payload))
        {
            aim = (payload ?? throw new InvalidOperationException("Aim payload was not materialized.")).Materialize();
            aimInstanceId = (aimInstance ?? throw new InvalidOperationException("Aim instance was not captured.")).InstanceId;
        }
        int aimBonus = aim is { Active: true } && !movedBeforeShot && aim.Target == target
            ? aim.Bonus(weapon.Accuracy)
            : 0;
        int shockPenalty = source.State.Shock(source.Secondary.MaxHp)?.PenaltyMagnitude ?? 0;
        int effective = DclRangedRules.EffectiveSkill(
            baseWeaponSkill,
            aimBonus,
            horizontalTiles,
            locationPenaltyMagnitude,
            shockPenalty);
        var context = new DclCanonicalRangedStrikeContext(
            weapon.RangedKind.Value,
            baseWeaponSkill,
            horizontalTiles,
            locationPenaltyMagnitude,
            shockPenalty,
            nativeRangeLegal,
            nativeTrajectoryLegal,
            movedBeforeShot,
            aim,
            aimInstanceId);
        return new DclCanonicalRangedSnapshotMechanics(
            effective,
            context,
            AimWillDischargeOrCancel: aim is { Active: true });
    }

    public static DclCanonicalStatusSnapshotMechanics ResolveStatusTarget(
        DclStateDefinition definition,
        DclResistanceCharacteristic resistanceCharacteristic,
        DclCanonicalNativeUnitProjection target,
        DclCanonicalEquipmentSnapshot targetEquipment,
        int jobMagicResistance = 0,
        int explicitStateResistanceModifier = 0,
        int? authoredResistanceScore = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetEquipment);
        bool immune = definition.NativeStatusBit is { } nativeBit && target.State.HasNativeImmunity(nativeBit);
        if (!string.IsNullOrWhiteSpace(definition.ImmunityFamily) &&
            !StringComparer.OrdinalIgnoreCase.Equals(definition.ImmunityFamily, "none"))
            immune |= targetEquipment.HasStatusImmunity(definition.ImmunityFamily);

        int? resistance = definition.ResistanceGate switch
        {
            DclStateResistanceGate.None when resistanceCharacteristic is DclResistanceCharacteristic.None => null,
            DclStateResistanceGate.None => throw new ArgumentException("A state with no resistance gate cannot request a resistance characteristic.", nameof(resistanceCharacteristic)),
            DclStateResistanceGate.SuccessRoll or DclStateResistanceGate.QuickContest => resistanceCharacteristic switch
            {
                DclResistanceCharacteristic.Ht => target.Primary.Ht,
                DclResistanceCharacteristic.Will => target.Secondary.Will,
                DclResistanceCharacteristic.SpiritualResistance => checked(
                    target.Secondary.Will + jobMagicResistance + targetEquipment.MagicResistanceModifier +
                    explicitStateResistanceModifier),
                DclResistanceCharacteristic.Authored when authoredResistanceScore is not null => authoredResistanceScore.Value,
                _ => throw new ArgumentException("The resistance gate requires one complete named resistance score.", nameof(resistanceCharacteristic)),
            },
            DclStateResistanceGate.Explicit => throw new InvalidOperationException("An Explicit resistance gate requires its named mechanism owner."),
            _ => throw new InvalidOperationException("The state has no usable resistance gate."),
        };
        return new DclCanonicalStatusSnapshotMechanics(immune, resistance);
    }
}
