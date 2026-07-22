namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalAttributeAdjustments(
    int JobSt = 0,
    int EquipmentSt = 0,
    int StateSt = 0,
    int JobDx = 0,
    int EquipmentDx = 0,
    int StateDx = 0,
    int JobIq = 0,
    int EquipmentIq = 0,
    int StateIq = 0);

internal sealed record DclCanonicalNativeUnitProjectionInput(
    DclCanonicalAttributeAdjustments AttributeAdjustments,
    DclSecondaryInputs SecondaryInputs,
    int TileHeight,
    DclStateRegistry StateRegistry,
    DclDefenseResourceSnapshot DefenseResources,
    bool ExplicitlyEligible = false)
{
    public static DclCanonicalNativeUnitProjectionInput Compose(
        DclCanonicalAttributeAdjustments nonEquipmentAttributes,
        DclSecondaryInputs nonEquipmentSecondary,
        DclCanonicalEquipmentSnapshot equipment,
        int tileHeight,
        DclStateRegistry stateRegistry,
        DclDefenseResourceSnapshot defenseResources,
        bool explicitlyEligible = false)
    {
        ArgumentNullException.ThrowIfNull(nonEquipmentAttributes);
        ArgumentNullException.ThrowIfNull(nonEquipmentSecondary);
        ArgumentNullException.ThrowIfNull(equipment);
        if (nonEquipmentAttributes.EquipmentSt != 0 || nonEquipmentAttributes.EquipmentDx != 0 ||
            nonEquipmentAttributes.EquipmentIq != 0)
            throw new ArgumentException("Non-equipment attribute input cannot prepopulate the equipment channel.", nameof(nonEquipmentAttributes));
        DclCanonicalAttributeAdjustments attributes = nonEquipmentAttributes with
        {
            EquipmentSt = equipment.StModifier,
            EquipmentDx = equipment.DxModifier,
            EquipmentIq = equipment.IqModifier,
        };
        DclSecondaryInputs secondary = nonEquipmentSecondary with
        {
            EquipmentStatusHpModifier = checked(
                nonEquipmentSecondary.EquipmentStatusHpModifier + equipment.HpModifier),
            EquipmentStatusMpModifier = checked(
                nonEquipmentSecondary.EquipmentStatusMpModifier + equipment.MpModifier),
            WillModifier = checked(nonEquipmentSecondary.WillModifier + equipment.WillModifier),
            BasicSpeedModifier = (nonEquipmentSecondary.BasicSpeedModifier ?? DclRational.FromInteger(0)) +
                equipment.BasicSpeedModifier,
            MoveModifier = checked(nonEquipmentSecondary.MoveModifier + equipment.MoveModifier),
            EquipmentStatusJumpModifier = checked(
                nonEquipmentSecondary.EquipmentStatusJumpModifier + equipment.JumpModifier),
        };
        return new DclCanonicalNativeUnitProjectionInput(
            attributes,
            secondary,
            tileHeight,
            stateRegistry,
            defenseResources,
            explicitlyEligible);
    }
}

internal sealed record DclCanonicalNativeUnitProjection(
    DclUnitKey Unit,
    DclPrimaryCharacteristics Primary,
    DclSecondaryCharacteristics Secondary,
    DclCanonicalProgressionSnapshot Progression,
    DclTargetCandidate Target,
    DclCanonicalStateSnapshot State,
    int CurrentMp,
    int CurrentFaith);

internal readonly record struct DclCanonicalProgressionSnapshot(
    int JobId,
    int JobIndex,
    int SpendableJp,
    int TotalJp,
    int JobLevel);

internal static class DclCanonicalNativeSnapshotAdapter
{
    private const int JobIdOffset = 0x03;
    private const int JobIdBase = 0x4A;
    private const int JobCount = 23;
    private const int SpendableJpBaseOffset = 0xF0;
    private const int TotalJpBaseOffset = 0x11E;
    private const int RawPaOffset = 0x38;
    private const int RawMaOffset = 0x39;
    private const int RawSpeedOffset = 0x3A;
    private const int XOffset = 0x4F;
    private const int YOffset = 0x50;
    private const int FacingAndLayerOffset = 0x51;
    private const int EffectiveStatusOffset = 0x61;
    private const int UndeadMask = 0x10;
    private const int MapLayerMask = 0x80;

    public static DclCanonicalNativeUnitProjection ProjectUnit(
        UnitSnapshot unit,
        int battleGeneration,
        int unitSlot,
        DclUnitKey source,
        int sourceTeam,
        DclCanonicalNativeUnitProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(input);
        DclPrimaryCharacteristics primary = ProjectPrimary(unit, input.AttributeAdjustments);
        DclSecondaryCharacteristics secondary = DclCharacteristics.ResolveSecondary(primary, input.SecondaryInputs);
        if (unit.MaxHp != secondary.MaxHp || unit.MaxMp != secondary.MaxMp ||
            unit.Hp < 0 || unit.Hp > secondary.MaxHp || unit.Mp < 0 || unit.Mp > secondary.MaxMp)
            throw new InvalidOperationException(
                "The native HP/MP pools are not synchronized with the canonical DCL characteristic snapshot.");
        if (unit.Faith is < 0 or > 100)
            throw new InvalidOperationException("The native current Faith is outside the canonical 0..100 magnitude domain.");
        var key = new DclUnitKey(battleGeneration, unitSlot, unit.CharId);
        DclCanonicalProgressionSnapshot progression = ProjectProgression(unit);
        DclCanonicalStateSnapshot state = DclCanonicalStateSnapshot.Capture(unit, key, input.StateRegistry);
        DclTargetCandidate target = ProjectTarget(
            unit,
            battleGeneration,
            unitSlot,
            source,
            sourceTeam,
            input.TileHeight,
            state.Revision,
            input.DefenseResources,
            input.ExplicitlyEligible);
        return new DclCanonicalNativeUnitProjection(
            target.Unit,
            primary,
            secondary,
            progression,
            target,
            state,
            unit.Mp,
            unit.Faith);
    }

    public static DclCanonicalProgressionSnapshot ProjectProgression(UnitSnapshot unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        int jobId = unit.ReadByte(JobIdOffset);
        int jobIndex = jobId - JobIdBase;
        if (jobIndex is < 0 or >= JobCount)
            return new DclCanonicalProgressionSnapshot(jobId, Math.Max(0, jobIndex), 0, 0, 0);
        int spendableJp = unit.ReadUInt16(SpendableJpBaseOffset + jobIndex * 2);
        int totalJp = unit.ReadUInt16(TotalJpBaseOffset + jobIndex * 2);
        return new DclCanonicalProgressionSnapshot(
            jobId,
            jobIndex,
            spendableJp,
            totalJp,
            DclSkillRules.NativeJobLevelFromTotalJp(totalJp));
    }

    public static DclPrimaryCharacteristics ProjectPrimary(
        UnitSnapshot unit,
        DclCanonicalAttributeAdjustments adjustments)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(adjustments);
        int rawPa = unit.ReadByte(RawPaOffset);
        int rawMa = unit.ReadByte(RawMaOffset);
        int rawSpeed = unit.ReadByte(RawSpeedOffset);
        if (rawPa <= 0 || rawMa <= 0 || rawSpeed <= 0)
            throw new ArgumentException("The native unit snapshot lacks positive proven raw PA/MA/Speed fields.", nameof(unit));
        return DclCharacteristics.ResolvePrimary(new DclAttributeInputs(
            rawPa,
            rawSpeed,
            rawMa,
            unit.Brave,
            adjustments.JobSt,
            adjustments.EquipmentSt,
            adjustments.StateSt,
            adjustments.JobDx,
            adjustments.EquipmentDx,
            adjustments.StateDx,
            adjustments.JobIq,
            adjustments.EquipmentIq,
            adjustments.StateIq));
    }

    public static DclTargetCandidate ProjectTarget(
        UnitSnapshot unit,
        int battleGeneration,
        int unitSlot,
        DclUnitKey source,
        int sourceTeam,
        int tileHeight,
        long combatStateRevision,
        DclDefenseResourceSnapshot defenseResources,
        bool explicitlyEligible = false)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(defenseResources);
        if (battleGeneration <= 0 || unitSlot is < 0 or >= 64 || tileHeight < 0 || combatStateRevision < 0)
            throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        var key = new DclUnitKey(battleGeneration, unitSlot, unit.CharId);
        if (!source.IsValid || source.BattleGeneration != battleGeneration)
            throw new ArgumentException("The projection source must belong to the same native battle generation.", nameof(source));
        int x = unit.ReadByte(XOffset);
        int y = unit.ReadByte(YOffset);
        int facingAndLayer = unit.ReadByte(FacingAndLayerOffset);
        int effectiveStatus = unit.ReadByte(EffectiveStatusOffset);
        if (x < 0 || y < 0 || facingAndLayer < 0 || effectiveStatus < 0)
            throw new ArgumentException("The native unit snapshot does not contain the proven coordinate/status fields.", nameof(unit));
        if (unit.Hp < 0 || unit.MaxHp < 1 || unit.Hp > unit.MaxHp)
            throw new ArgumentException("The native HP snapshot is outside the canonical target domain.", nameof(unit));

        DclEligibleTargetStates states = unit.Hp == 0
            ? DclEligibleTargetStates.Ko
            : DclEligibleTargetStates.Alive;
        if ((effectiveStatus & UndeadMask) != 0)
            states |= DclEligibleTargetStates.Undead;
        DclAllegianceRelation relation = key == source
            ? DclAllegianceRelation.Self
            : unit.Team == sourceTeam ? DclAllegianceRelation.Ally : DclAllegianceRelation.Enemy;
        return new DclTargetCandidate(
            key,
            new DclBattleTile(x, y, (facingAndLayer & MapLayerMask) == 0 ? 0 : 1),
            tileHeight,
            relation,
            states,
            unit.Hp,
            combatStateRevision,
            new DclDefenseResourceSnapshot(
                new Dictionary<string, int>(defenseResources.ParryAttemptCounts, StringComparer.Ordinal),
                defenseResources.BlockAvailable),
            explicitlyEligible);
    }
}
