namespace fftivc.generic.chronicle.codemod;

internal sealed record DclUnitAttributeBreakdown(
    int BaseValue,
    int JobAdjustment,
    int EquipmentAdjustment,
    int StateAdjustment,
    int FinalValue);

internal sealed record DclUnitPoolBreakdown(
    int Current,
    int BaseMaximum,
    int Maximum,
    int CharacterModifier,
    int JobModifier,
    int EquipmentStatusModifier);

internal sealed record DclUnitMobilityBreakdown(
    DclRational BasicSpeed,
    int BasicMove,
    int BaseJump,
    int BaseDodge,
    DclEncumbranceResult Encumbrance,
    DclCriticalAdjustments Critical,
    int FinalMove,
    int FinalJump,
    int FinalDodge);

internal sealed record DclCanonicalUnitInformationProjection(
    DclUnitKey Unit,
    DclUnitAttributeBreakdown St,
    DclUnitAttributeBreakdown Dx,
    DclUnitAttributeBreakdown Iq,
    int CurrentBrave,
    int Ht,
    int Will,
    int CurrentFaith,
    DclUnitPoolBreakdown Hp,
    DclUnitPoolBreakdown Mp,
    DclRational BasicLift,
    DclUnitMobilityBreakdown Mobility,
    DclCanonicalEquipmentSnapshot Equipment);

internal static class DclCanonicalUnitInformationProjector
{
    public static DclCanonicalUnitInformationProjection Project(
        DclCanonicalNativeUnitProjection unit,
        DclCanonicalEquipmentSnapshot equipment,
        DclCanonicalAttributeAdjustments nonEquipmentAttributes,
        DclSecondaryInputs nonEquipmentSecondary,
        int rawPa,
        int rawSpeed,
        int rawMa,
        int currentBrave)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(nonEquipmentAttributes);
        ArgumentNullException.ThrowIfNull(nonEquipmentSecondary);
        if (rawPa <= 0 || rawSpeed <= 0 || rawMa <= 0)
            throw new ArgumentOutOfRangeException(nameof(rawPa));
        if (currentBrave < 0)
            throw new ArgumentOutOfRangeException(nameof(currentBrave));

        int expectedHt = DclCharacteristics.BraveToHt(currentBrave);
        if (unit.Primary.Ht != expectedHt)
            throw new InvalidOperationException("Unit information projection requires the current Brave/HT snapshot to be synchronized.");
        DclCanonicalAttributeAdjustments actualEquipmentAttributes = equipment.AttributeAdjustments;
        var st = new DclUnitAttributeBreakdown(
            rawPa,
            nonEquipmentAttributes.JobSt,
            actualEquipmentAttributes.EquipmentSt,
            nonEquipmentAttributes.StateSt,
            unit.Primary.St);
        var dx = new DclUnitAttributeBreakdown(
            rawSpeed,
            nonEquipmentAttributes.JobDx,
            actualEquipmentAttributes.EquipmentDx,
            nonEquipmentAttributes.StateDx,
            unit.Primary.Dx);
        var iq = new DclUnitAttributeBreakdown(
            rawMa,
            nonEquipmentAttributes.JobIq,
            actualEquipmentAttributes.EquipmentIq,
            nonEquipmentAttributes.StateIq,
            unit.Primary.Iq);
        DclEncumbranceResult encumbrance = DclEncumbrance.Resolve(
            unit.Secondary.BasicLift,
            equipment.Equipped.Select(slot => slot.Item.Weight),
            unit.Secondary.BasicMove,
            unit.Secondary.BaseJump,
            unit.Secondary.BaseDodge + equipment.DodgeModifier);
        DclCriticalAdjustments critical = DclCriticalState.Apply(
            unit.Target.CurrentHp,
            unit.Secondary.MaxHp,
            encumbrance.EffectiveMove,
            encumbrance.EffectiveDodge);
        return new DclCanonicalUnitInformationProjection(
            unit.Unit,
            st,
            dx,
            iq,
            currentBrave,
            unit.Primary.Ht,
            unit.Secondary.Will,
            unit.CurrentFaith,
            new DclUnitPoolBreakdown(
                unit.Target.CurrentHp,
                unit.Secondary.BaseMaxHp,
                unit.Secondary.MaxHp,
                nonEquipmentSecondary.CharacterHpModifier,
                nonEquipmentSecondary.JobHpModifier,
                equipment.HpModifier),
            new DclUnitPoolBreakdown(
                unit.CurrentMp,
                unit.Secondary.BaseMaxMp,
                unit.Secondary.MaxMp,
                nonEquipmentSecondary.CharacterMpModifier,
                nonEquipmentSecondary.JobMpModifier,
                equipment.MpModifier),
            unit.Secondary.BasicLift,
            new DclUnitMobilityBreakdown(
                unit.Secondary.BasicSpeed,
                unit.Secondary.BasicMove,
                unit.Secondary.BaseJump,
                unit.Secondary.BaseDodge,
                encumbrance,
                critical,
                critical.FinalMove,
                encumbrance.EffectiveJump,
                critical.FinalDodge),
            equipment);
    }
}
