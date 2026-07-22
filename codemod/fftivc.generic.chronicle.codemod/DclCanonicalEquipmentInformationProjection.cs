namespace fftivc.generic.chronicle.codemod;

internal sealed record DclEquipmentWeaponInformation(
    string SkillFamily,
    DclSkillDifficulty Difficulty,
    DclDamageBasis DamageBasis,
    DclDiceExpression FinalDamageExpression,
    int DamageModifier,
    int WholeDiceModifier,
    DclDamageType DamageType,
    DclRational ArmorDivisor,
    int Reach,
    int MaximumRange,
    int Accuracy,
    DclWeaponHands Hands,
    DclRational ParryLoad,
    int ParryModifier,
    DclWeaponBalance Balance,
    DclWeaponReadinessProperty Readiness,
    DclPhysicalRoute Trajectory,
    bool VisionRequired,
    DclRangedWeaponKind? RangedKind);

internal sealed record DclEquipmentShieldInformation(
    string SkillFamily,
    DclSkillDifficulty Difficulty,
    int BlockModifier,
    int DefenseBonus,
    IReadOnlySet<DclDelivery> CoveredDeliveries);

internal sealed record DclEquipmentFocusInformation(
    int SpellSkillModifier,
    int FocusDamageModifier,
    int FocusHealingModifier,
    DclRational? ElementBoostMultiplier,
    int ConcentrationModifier,
    int CastCtModifier,
    DclRational? MpCostMultiplier,
    IReadOnlyList<string> CompatibleTraditions);

internal sealed record DclEquipmentItemInformationProjection(
    int ItemId,
    DclItemSlot Slot,
    DclRational Weight,
    int BodyDr,
    int HeadDr,
    int StModifier,
    int DxModifier,
    int IqModifier,
    int HpModifier,
    int MpModifier,
    DclRational BasicSpeedModifier,
    int MoveModifier,
    int JumpModifier,
    int DodgeModifier,
    int WillModifier,
    int MagicResistanceModifier,
    DclEquipmentWeaponInformation? Weapon,
    DclEquipmentShieldInformation? Shield,
    DclEquipmentFocusInformation? Focus,
    IReadOnlyList<DclElementItemProperty> ElementProperties,
    IReadOnlyList<string> StatusImmunities,
    IReadOnlyList<string> SpecialProperties,
    IReadOnlyList<string> VisiblePropertyKeys);

internal sealed record DclEquipmentChangePreview(
    DclRational BeforeLoad,
    DclRational AfterLoad,
    DclEncumbranceBand BeforeEncumbranceBand,
    DclEncumbranceBand AfterEncumbranceBand,
    DclRational? NextEncumbranceThreshold,
    DclRational? LoadUntilNextEncumbranceBand,
    int BeforeMove,
    int AfterMove,
    int BeforeJump,
    int AfterJump,
    int BeforeDodge,
    int AfterDodge,
    int BeforeBodyDr,
    int AfterBodyDr,
    int BeforeHeadDr,
    int AfterHeadDr,
    int BeforeMaxHp,
    int AfterMaxHp,
    int BeforeMaxMp,
    int AfterMaxMp,
    int BeforeMagicResistance,
    int AfterMagicResistance,
    DclCanonicalFocusModifiers? FocusModifiers);

internal static class DclCanonicalEquipmentInformationProjector
{
    public static DclEquipmentItemInformationProjection ProjectItem(DclItemMetadata item, int? sourceSt = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        DclEquipmentWeaponInformation? weapon = item.Weapon is null
            ? null
            : ProjectWeapon(item, item.Weapon, sourceSt);
        DclEquipmentShieldInformation? shield = item.Shield is null
            ? null
            : new DclEquipmentShieldInformation(
                item.Shield.SkillFamily,
                item.Shield.Difficulty,
                item.Shield.BlockModifier,
                item.Shield.DefenseBonus,
                item.Shield.CoveredDeliveries);
        DclEquipmentFocusInformation? focus = item.Focus is null
            ? null
            : new DclEquipmentFocusInformation(
                item.Focus.SpellSkillModifier,
                item.Focus.FocusDamageModifier,
                item.Focus.FocusHealingModifier,
                item.Focus.ElementBoostMultiplier,
                item.Focus.ConcentrationModifier,
                item.Focus.CastCtModifier,
                item.Focus.MpCostMultiplier,
                item.Focus.CompatibleTraditions);
        return new DclEquipmentItemInformationProjection(
            item.ItemId,
            item.Slot,
            item.Weight,
            item.BodyDr,
            item.HeadDr,
            item.StModifier,
            item.DxModifier,
            item.IqModifier,
            item.HpModifier,
            item.MpModifier,
            item.BasicSpeedModifier,
            item.MoveModifier,
            item.JumpModifier,
            item.DodgeModifier,
            item.WillModifier,
            item.MagicResistanceModifier,
            weapon,
            shield,
            focus,
            item.ElementProperties,
            item.StatusImmunities,
            item.SpecialProperties,
            VisiblePropertyKeys(item));
    }

    public static DclEquipmentChangePreview ProjectChange(
        DclCanonicalUnitInformationProjection before,
        DclCanonicalUnitInformationProjection after,
        string? focusTradition = null)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        DclRational? nextThreshold = NextEncumbranceThreshold(after.BasicLift, after.Mobility.Encumbrance.Band);
        DclRational? loadUntilNext = nextThreshold is null
            ? null
            : Max(DclRational.FromInteger(0), nextThreshold.Value - after.Mobility.Encumbrance.Load);
        DclCanonicalFocusModifiers? focus = string.IsNullOrWhiteSpace(focusTradition)
            ? null
            : after.Equipment.ResolveFocus(focusTradition);
        return new DclEquipmentChangePreview(
            before.Mobility.Encumbrance.Load,
            after.Mobility.Encumbrance.Load,
            before.Mobility.Encumbrance.Band,
            after.Mobility.Encumbrance.Band,
            nextThreshold,
            loadUntilNext,
            before.Mobility.FinalMove,
            after.Mobility.FinalMove,
            before.Mobility.FinalJump,
            after.Mobility.FinalJump,
            before.Mobility.FinalDodge,
            after.Mobility.FinalDodge,
            before.Equipment.BodyDr,
            after.Equipment.BodyDr,
            before.Equipment.HeadDr,
            after.Equipment.HeadDr,
            before.Hp.Maximum,
            after.Hp.Maximum,
            before.Mp.Maximum,
            after.Mp.Maximum,
            before.Equipment.MagicResistanceModifier,
            after.Equipment.MagicResistanceModifier,
            focus);
    }

    private static DclEquipmentWeaponInformation ProjectWeapon(
        DclItemMetadata item,
        DclWeaponMetadata weapon,
        int? sourceSt)
    {
        DclDiceExpression damage = weapon.DamageBasis switch
        {
            DclDamageBasis.Fixed => DclDiceExpression.ParseAuthored(weapon.FixedDamageExpression!),
            DclDamageBasis.Thrust when sourceSt is not null => DclStrengthDamage.Lookup(
                sourceSt.Value,
                DclStrengthDamageMode.Thrust),
            DclDamageBasis.Swing when sourceSt is not null => DclStrengthDamage.Lookup(
                sourceSt.Value,
                DclStrengthDamageMode.Swing),
            DclDamageBasis.Thrust or DclDamageBasis.Swing => throw new ArgumentException(
                "ST-based weapon information requires the source ST used by the preview.",
                nameof(sourceSt)),
            _ => throw new InvalidOperationException("Weapon information requires a normalized damage basis."),
        };
        damage = damage.AddAndNormalize(weapon.DamageModifier);
        if (weapon.WholeDiceModifier != 0)
            damage = damage with { Dice = Math.Max(0, checked(damage.Dice + weapon.WholeDiceModifier)) };
        return new DclEquipmentWeaponInformation(
            weapon.SkillFamily,
            weapon.Difficulty,
            weapon.DamageBasis,
            damage,
            weapon.DamageModifier,
            weapon.WholeDiceModifier,
            weapon.DamageType,
            weapon.ArmorDivisor,
            weapon.Reach,
            weapon.MaximumRange,
            weapon.Accuracy,
            weapon.Hands,
            weapon.ParryLoadOverride ?? item.Weight,
            weapon.ParryModifier,
            weapon.Balance,
            weapon.Readiness,
            weapon.Trajectory,
            weapon.VisionRequired,
            weapon.RangedKind);
    }

    private static IReadOnlyList<string> VisiblePropertyKeys(DclItemMetadata item)
    {
        var keys = new List<string> { "slot", "weight" };
        if (item.BodyDr != 0 || item.Slot == DclItemSlot.Body) keys.Add("body-dr");
        if (item.HeadDr != 0 || item.Slot == DclItemSlot.Head) keys.Add("head-dr");
        AddIf(keys, item.StModifier != 0, "st");
        AddIf(keys, item.DxModifier != 0, "dx");
        AddIf(keys, item.IqModifier != 0, "iq");
        AddIf(keys, item.HpModifier != 0, "hp");
        AddIf(keys, item.MpModifier != 0, "mp");
        AddIf(keys, item.BasicSpeedModifier != DclRational.FromInteger(0), "basic-speed");
        AddIf(keys, item.MoveModifier != 0, "move");
        AddIf(keys, item.JumpModifier != 0, "jump");
        AddIf(keys, item.DodgeModifier != 0, "dodge");
        AddIf(keys, item.WillModifier != 0, "will");
        AddIf(keys, item.MagicResistanceModifier != 0, "magic-resistance");
        AddIf(keys, item.Weapon is not null, "weapon");
        AddIf(keys, item.Shield is not null, "shield");
        AddIf(keys, item.Focus is not null, "focus");
        AddIf(keys, item.ElementProperties.Count > 0, "elements");
        AddIf(keys, item.StatusImmunities.Count > 0, "status-immunities");
        AddIf(keys, item.SpecialProperties.Count > 0, "special-properties");
        return keys;
    }

    private static DclRational? NextEncumbranceThreshold(DclRational basicLift, DclEncumbranceBand band)
        => band switch
        {
            DclEncumbranceBand.None => basicLift,
            DclEncumbranceBand.Light => basicLift * DclRational.FromInteger(2),
            DclEncumbranceBand.Medium => basicLift * DclRational.FromInteger(3),
            DclEncumbranceBand.Heavy => basicLift * DclRational.FromInteger(6),
            DclEncumbranceBand.ExtraHeavy => null,
            _ => throw new ArgumentOutOfRangeException(nameof(band)),
        };

    private static DclRational Max(DclRational left, DclRational right)
        => left >= right ? left : right;

    private static void AddIf(ICollection<string> keys, bool condition, string key)
    {
        if (condition) keys.Add(key);
    }
}
