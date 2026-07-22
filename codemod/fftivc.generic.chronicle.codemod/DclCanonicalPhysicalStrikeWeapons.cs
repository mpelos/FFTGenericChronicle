namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalPhysicalStrikeWeapon(
    int StrikeIndex,
    int WeaponItemId,
    string WeaponResourceKey);

internal sealed record DclCanonicalPhysicalStrikeWeaponState(
    DclCanonicalPhysicalStrikeWeapon Weapon,
    DclWeaponActionState State);

/// <summary>
/// Normalizes the weapon identity owned by each physical Strike. The legacy action-wide fields are
/// accepted only as the compact representation of an action whose every Strike uses the same hand.
/// </summary>
internal static class DclCanonicalPhysicalStrikeWeapons
{
    public const string PrimaryUnarmedResourceKey = "unarmed-primary";
    public const string OffHandUnarmedResourceKey = "unarmed-offhand";

    public static DclCanonicalPhysicalStrikeWeapon[] Normalize(
        int strikeCount,
        int legacyWeaponItemId,
        string legacyWeaponResourceKey,
        IReadOnlyList<DclCanonicalPhysicalStrikeWeapon>? explicitWeapons)
    {
        if (strikeCount < 1)
            throw new ArgumentOutOfRangeException(nameof(strikeCount));
        DclCanonicalPhysicalStrikeWeapon[] weapons = explicitWeapons is { Count: > 0 }
            ? explicitWeapons.ToArray()
            : Enumerable.Range(0, strikeCount)
                .Select(index => new DclCanonicalPhysicalStrikeWeapon(
                    index,
                    legacyWeaponItemId,
                    legacyWeaponResourceKey))
                .ToArray();
        if (weapons.Length != strikeCount ||
            weapons.Select(weapon => weapon.StrikeIndex).Distinct().Count() != weapons.Length ||
            !weapons.Select(weapon => weapon.StrikeIndex).Order().SequenceEqual(Enumerable.Range(0, strikeCount)) ||
            weapons.Any(weapon => weapon.WeaponItemId < 0 || string.IsNullOrWhiteSpace(weapon.WeaponResourceKey)) ||
            weapons.GroupBy(weapon => weapon.WeaponResourceKey, StringComparer.Ordinal)
                .Any(group => group.Select(weapon => weapon.WeaponItemId).Distinct().Count() != 1))
            throw new ArgumentException("Physical execution requires one valid weapon item/hand identity per Strike.", nameof(explicitWeapons));
        return weapons.OrderBy(weapon => weapon.StrikeIndex).ToArray();
    }

    public static string ResolveCapturedHandResourceKey(
        DclCanonicalEquipmentSnapshot equipment,
        DclNativeWeaponHand hand,
        int weaponItemId)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        if (weaponItemId < 0)
            throw new ArgumentOutOfRangeException(nameof(weaponItemId));
        if (weaponItemId == 0)
            return hand == DclNativeWeaponHand.Primary
                ? PrimaryUnarmedResourceKey
                : OffHandUnarmedResourceKey;

        int right = equipment.NativeSlots.RightWeaponItemId;
        int left = equipment.NativeSlots.LeftWeaponItemId;
        bool rightOccupied = right is not (0 or 255);
        return hand switch
        {
            DclNativeWeaponHand.Primary when right == weaponItemId => "right-weapon",
            DclNativeWeaponHand.Primary when !rightOccupied && left == weaponItemId => "left-weapon",
            DclNativeWeaponHand.OffHand when rightOccupied && left == weaponItemId => "left-weapon",
            _ => throw new InvalidOperationException(
                $"Captured {hand} item {weaponItemId} does not match the synchronized native equipment slots."),
        };
    }

    public static IReadOnlyDictionary<int, DclWeaponMetadata> ResolveMetadata(
        DclCanonicalRuntimeCatalog runtime,
        IReadOnlyList<DclCanonicalPhysicalStrikeWeapon> strikeWeapons,
        DclDamageType? requiredDamageType = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(strikeWeapons);
        var resolved = new Dictionary<int, DclWeaponMetadata>();
        foreach (DclCanonicalPhysicalStrikeWeapon strikeWeapon in strikeWeapons)
        {
            if (!runtime.Items.Items.TryGetValue(strikeWeapon.WeaponItemId, out DclItemMetadata? item) ||
                item.Weapon is not { } weapon)
                throw new KeyNotFoundException($"Weapon item {strikeWeapon.WeaponItemId} has no normalized DCL metadata.");
            if (requiredDamageType is { } required && weapon.DamageType != required)
                throw new InvalidOperationException(
                    $"Strike {strikeWeapon.StrikeIndex} weapon and physical action disagree on wound-multiplier damage type.");
            resolved.Add(strikeWeapon.StrikeIndex, weapon);
        }
        return resolved;
    }

    public static void RequireEquipped(
        DclCanonicalEquipmentSnapshot equipment,
        IReadOnlyList<DclCanonicalPhysicalStrikeWeapon> strikeWeapons)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        foreach (DclCanonicalPhysicalStrikeWeapon strikeWeapon in strikeWeapons)
        {
            if (strikeWeapon.WeaponItemId == 0)
            {
                if (strikeWeapon.WeaponResourceKey is "right-weapon" or "left-weapon")
                    throw new InvalidOperationException(
                        $"Unarmed Strike {strikeWeapon.StrikeIndex} cannot claim an occupied equipment slot.");
                continue;
            }
            DclItemMetadata item = equipment.ResolveActiveWeapon(strikeWeapon.WeaponItemId);
            bool slotMatches = strikeWeapon.WeaponResourceKey switch
            {
                "right-weapon" => equipment.NativeSlots.RightWeaponItemId == strikeWeapon.WeaponItemId,
                "left-weapon" => equipment.NativeSlots.LeftWeaponItemId == strikeWeapon.WeaponItemId,
                _ => true,
            };
            if (item.Weapon is null || !slotMatches)
                throw new InvalidOperationException(
                    $"Strike {strikeWeapon.StrikeIndex} weapon/hand does not match the synchronized equipment snapshot.");
        }
    }
}
