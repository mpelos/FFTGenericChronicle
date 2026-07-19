namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclBaseHpResolution(
    bool Resolved,
    int BaseHp,
    int EquipmentHpBonus,
    int HeadItemId,
    int BodyItemId,
    string Error);

internal static class DclBaseHp
{
    // Proven battle-unit equipment words. Head and body are the only item families whose
    // ItemArmorData rows contribute MaxHP; both bonuses must be removed for DCL intrinsic HP.
    public const int HeadItemOffset = 0x1A;
    public const int BodyItemOffset = 0x1C;

    public static DclBaseHpResolution Resolve(UnitSnapshot? unit, ItemCatalog catalog)
    {
        if (unit is null)
            return Unresolved("unit is absent");
        if (!catalog.Loaded)
            return Unresolved("item catalog is not loaded");
        if (unit.MaxHp <= 0)
            return Unresolved("unit MaxHP is not positive");

        int headItemId = unit.ReadUInt16(HeadItemOffset);
        int bodyItemId = unit.ReadUInt16(BodyItemOffset);
        if (headItemId < 0 || bodyItemId < 0)
            return Unresolved("battle-unit equipment words are unavailable", headItemId, bodyItemId);
        if (!catalog.TryGet(headItemId, out var head))
            return Unresolved($"head item {headItemId} is absent from the item catalog", headItemId, bodyItemId);
        if (!catalog.TryGet(bodyItemId, out var body))
            return Unresolved($"body item {bodyItemId} is absent from the item catalog", headItemId, bodyItemId);

        int equipmentHpBonus = Math.Max(0, head.ArmorHpBonus) + Math.Max(0, body.ArmorHpBonus);
        int baseHp = unit.MaxHp - equipmentHpBonus;
        if (baseHp <= 0)
        {
            return new DclBaseHpResolution(
                Resolved: false,
                BaseHp: 0,
                EquipmentHpBonus: equipmentHpBonus,
                HeadItemId: headItemId,
                BodyItemId: bodyItemId,
                Error: $"MaxHP {unit.MaxHp} is not greater than equipment HP bonus {equipmentHpBonus}");
        }

        return new DclBaseHpResolution(
            Resolved: true,
            BaseHp: baseHp,
            EquipmentHpBonus: equipmentHpBonus,
            HeadItemId: headItemId,
            BodyItemId: bodyItemId,
            Error: "");
    }

    private static DclBaseHpResolution Unresolved(
        string error,
        int headItemId = -1,
        int bodyItemId = -1)
        => new(
            Resolved: false,
            BaseHp: 0,
            EquipmentHpBonus: 0,
            HeadItemId: headItemId,
            BodyItemId: bodyItemId,
            Error: error);
}
