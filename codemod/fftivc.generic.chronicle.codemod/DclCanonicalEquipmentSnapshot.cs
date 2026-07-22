namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclNativeEquipmentSlots(
    int HeadItemId,
    int BodyItemId,
    int AccessoryItemId,
    int RightWeaponItemId,
    int RightShieldItemId,
    int LeftWeaponItemId,
    int LeftShieldItemId);

internal sealed record DclCanonicalEquippedItem(
    string NativeSlot,
    DclItemMetadata Item);

internal sealed record DclCanonicalFocusModifiers(
    int SpellSkillModifier,
    int DamageModifier,
    int HealingModifier,
    DclRational ElementBoostMultiplier,
    int ConcentrationModifier,
    int CastCtModifier,
    DclRational MpCostMultiplier,
    int ApplicableFocusCount);

internal sealed record DclCanonicalEquipmentSnapshot(
    DclNativeEquipmentSlots NativeSlots,
    IReadOnlyList<DclCanonicalEquippedItem> Equipped,
    DclRational TotalWeight,
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
    int BodyDr,
    int HeadDr)
{
    public DclCanonicalAttributeAdjustments AttributeAdjustments
        => new(EquipmentSt: StModifier, EquipmentDx: DxModifier, EquipmentIq: IqModifier);

    public bool HasStatusImmunity(string family)
    {
        if (string.IsNullOrWhiteSpace(family)) throw new ArgumentException("An immunity family is required.", nameof(family));
        return Equipped.Any(slot => slot.Item.StatusImmunities.Contains(family, StringComparer.OrdinalIgnoreCase));
    }

    public bool HasSpecialProperty(string property)
    {
        if (string.IsNullOrWhiteSpace(property)) throw new ArgumentException("A special property is required.", nameof(property));
        return Equipped.Any(slot => slot.Item.SpecialProperties.Contains(property, StringComparer.OrdinalIgnoreCase));
    }

    public int ApplicableDr(DclPhysicalLocation location)
        => DclFacingAndTargeting.ApplicableDr(BodyDr, HeadDr, location);

    public DclElementAffinity ResolveIncomingAffinity(
        string element,
        DclCanonicalEquipmentSnapshot source)
        => ResolveIncomingAffinity(element, source, [], []);

    public DclElementAffinity ResolveIncomingAffinity(
        string element,
        DclCanonicalEquipmentSnapshot source,
        IEnumerable<int> additionalNumericSteps,
        IEnumerable<DclRational> additionalSourceBoosts)
    {
        if (string.IsNullOrWhiteSpace(element)) throw new ArgumentException("An element is required.", nameof(element));
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(additionalNumericSteps);
        ArgumentNullException.ThrowIfNull(additionalSourceBoosts);
        DclElementItemProperty[] targetProperties = Equipped
            .SelectMany(slot => slot.Item.ElementProperties)
            .Where(property => StringComparer.OrdinalIgnoreCase.Equals(property.Element, element))
            .ToArray();
        DclRational[] sourceBoosts = source.Equipped
            .SelectMany(slot => slot.Item.ElementProperties)
            .Where(property => StringComparer.OrdinalIgnoreCase.Equals(property.Element, element) &&
                               property.SourceBoostMultiplier is not null)
            .Select(property => property.SourceBoostMultiplier!.Value)
            .ToArray();
        return DclMagicMagnitude.ResolveAffinity(
            absorb: targetProperties.Any(property => property.Absorb),
            nullify: targetProperties.Any(property => property.Nullify),
            numericSteps: targetProperties.Select(property => property.AffinityStep).Concat(additionalNumericSteps),
            sourceBoosts.Concat(additionalSourceBoosts));
    }

    public DclCanonicalFocusModifiers ResolveFocus(string tradition)
    {
        if (string.IsNullOrWhiteSpace(tradition)) throw new ArgumentException("A tradition identity is required.", nameof(tradition));
        DclFocusMetadata[] applicable = Equipped
            .Select(slot => slot.Item.Focus)
            .Where(focus => focus is not null && focus.CompatibleTraditions.Contains(tradition, StringComparer.OrdinalIgnoreCase))
            .Cast<DclFocusMetadata>()
            .ToArray();
        DclRational boost = DclRational.FromInteger(1);
        DclRational mpCost = DclRational.FromInteger(1);
        int spell = 0, damage = 0, healing = 0, concentration = 0, castCt = 0;
        foreach (DclFocusMetadata focus in applicable)
        {
            spell = checked(spell + focus.SpellSkillModifier);
            damage = checked(damage + focus.FocusDamageModifier);
            healing = checked(healing + focus.FocusHealingModifier);
            concentration = checked(concentration + focus.ConcentrationModifier);
            castCt = checked(castCt + focus.CastCtModifier);
            if (focus.ElementBoostMultiplier is { } candidate && candidate > boost) boost = candidate;
            if (focus.MpCostMultiplier is { } multiplier) mpCost *= multiplier;
        }
        return new DclCanonicalFocusModifiers(
            spell,
            damage,
            healing,
            boost,
            concentration,
            castCt,
            mpCost,
            applicable.Length);
    }

    public DclItemMetadata ResolveActiveWeapon(int itemId)
    {
        DclCanonicalEquippedItem[] matches = Equipped
            .Where(slot => slot.Item.ItemId == itemId && slot.Item.Slot == DclItemSlot.Weapon)
            .ToArray();
        if (matches.Length == 0)
            throw new InvalidOperationException($"Active weapon {itemId} is not equipped in the synchronized snapshot.");
        return matches[0].Item;
    }
}

internal static class DclCanonicalEquipmentProjector
{
    private const int HeadOffset = 0x1A;
    private const int BodyOffset = 0x1C;
    private const int AccessoryOffset = 0x1E;
    private const int RightWeaponOffset = 0x20;
    private const int RightShieldOffset = 0x22;
    private const int LeftWeaponOffset = 0x24;
    private const int LeftShieldOffset = 0x26;

    public static DclCanonicalEquipmentSnapshot Project(
        UnitSnapshot unit,
        DclItemMetadataRegistry metadata)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(metadata);
        var native = new DclNativeEquipmentSlots(
            unit.ReadUInt16(HeadOffset),
            unit.ReadUInt16(BodyOffset),
            unit.ReadUInt16(AccessoryOffset),
            unit.ReadUInt16(RightWeaponOffset),
            unit.ReadUInt16(RightShieldOffset),
            unit.ReadUInt16(LeftWeaponOffset),
            unit.ReadUInt16(LeftShieldOffset));
        var equipped = new List<DclCanonicalEquippedItem>();
        Add(equipped, metadata, "head", native.HeadItemId, DclItemSlot.Head);
        Add(equipped, metadata, "body", native.BodyItemId, DclItemSlot.Body);
        Add(equipped, metadata, "accessory", native.AccessoryItemId, DclItemSlot.Accessory);
        Add(equipped, metadata, "right-weapon", native.RightWeaponItemId, DclItemSlot.Weapon);
        Add(equipped, metadata, "right-shield", native.RightShieldItemId, DclItemSlot.Shield);
        Add(equipped, metadata, "left-weapon", native.LeftWeaponItemId, DclItemSlot.Weapon);
        Add(equipped, metadata, "left-shield", native.LeftShieldItemId, DclItemSlot.Shield);

        DclRational totalWeight = DclRational.FromInteger(0);
        DclRational basicSpeedModifier = DclRational.FromInteger(0);
        int st = 0, dx = 0, iq = 0, hp = 0, mp = 0, move = 0, jump = 0, dodge = 0, will = 0;
        int magicResistance = 0, bodyDr = 0, headDr = 0;
        foreach (DclCanonicalEquippedItem slot in equipped)
        {
            DclItemMetadata item = slot.Item;
            totalWeight += item.Weight;
            st = checked(st + item.StModifier);
            dx = checked(dx + item.DxModifier);
            iq = checked(iq + item.IqModifier);
            hp = checked(hp + item.HpModifier);
            mp = checked(mp + item.MpModifier);
            basicSpeedModifier += item.BasicSpeedModifier;
            move = checked(move + item.MoveModifier);
            jump = checked(jump + item.JumpModifier);
            dodge = checked(dodge + item.DodgeModifier);
            will = checked(will + item.WillModifier);
            magicResistance = checked(magicResistance + item.MagicResistanceModifier);
            bodyDr = checked(bodyDr + item.BodyDr);
            headDr = checked(headDr + item.HeadDr);
        }
        return new DclCanonicalEquipmentSnapshot(
            native,
            equipped,
            totalWeight,
            st,
            dx,
            iq,
            hp,
            mp,
            basicSpeedModifier,
            move,
            jump,
            dodge,
            will,
            magicResistance,
            bodyDr,
            headDr);
    }

    private static void Add(
        ICollection<DclCanonicalEquippedItem> equipped,
        DclItemMetadataRegistry metadata,
        string nativeSlot,
        int itemId,
        DclItemSlot expectedSlot)
    {
        if (itemId is 0 or 255) return;
        if (itemId < 0 || itemId > byte.MaxValue)
            throw new InvalidOperationException($"Native equipment slot {nativeSlot} is unreadable or outside the item-id domain.");
        if (!metadata.Items.TryGetValue(itemId, out DclItemMetadata? item))
            throw new InvalidOperationException($"Native equipment slot {nativeSlot} item {itemId} has no canonical metadata.");
        if (item.Slot != expectedSlot)
            throw new InvalidOperationException(
                $"Native equipment slot {nativeSlot} item {itemId} is authored as {item.Slot}, not {expectedSlot}.");
        equipped.Add(new DclCanonicalEquippedItem(nativeSlot, item));
    }
}
