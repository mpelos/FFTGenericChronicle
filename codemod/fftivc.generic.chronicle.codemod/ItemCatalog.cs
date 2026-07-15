namespace fftivc.generic.chronicle.codemod;

internal sealed class ItemCatalog
{
    private readonly Dictionary<int, ItemCatalogEntry> _items;

    private ItemCatalog(string path, Dictionary<int, ItemCatalogEntry> items, string error = "")
    {
        Path = path;
        _items = items;
        Error = error;
    }

    public string Path { get; }

    public string Error { get; }

    public int Count => _items.Count;

    public bool Loaded => Count > 0 && string.IsNullOrWhiteSpace(Error);

    public static ItemCatalog Empty(string path, string error = "") => new(path, new Dictionary<int, ItemCatalogEntry>(), error);

    public static ItemCatalog Load(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return Empty(path, "no catalog path");
            if (!File.Exists(path))
                return Empty(path, "file not found");

            using var reader = new StreamReader(path);
            string? headerLine = reader.ReadLine();
            if (headerLine is null)
                return Empty(path, "empty file");

            var headers = ParseCsvLine(headerLine);
            var items = new Dictionary<int, ItemCatalogEntry>();

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cells = ParseCsvLine(line);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count && i < cells.Count; i++)
                    row[headers[i]] = cells[i];

                int itemId = ReadInt(row, "item_id");
                if (itemId < 0) continue;
                items[itemId] = ItemCatalogEntry.FromRow(itemId, row);
            }

            return new ItemCatalog(path, items);
        }
        catch (Exception ex)
        {
            return Empty(path, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public bool TryGet(int itemId, out ItemCatalogEntry item) => _items.TryGetValue(itemId, out item!);

    public string Describe()
    {
        if (Loaded) return $"{Count} items from {Path}";
        return string.IsNullOrWhiteSpace(Error) ? $"not loaded from {Path}" : $"not loaded from {Path} ({Error})";
    }

    internal static int ReadInt(Dictionary<string, string> row, string name)
    {
        return row.TryGetValue(name, out string? text) && int.TryParse(text, out int value) ? value : -1;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        bool quoted = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        cells.Add(current.ToString());
        return cells;
    }
}

internal sealed record ItemCatalogEntry(
    int ItemId,
    string Name,
    string TypeFlags,
    string ItemCategory,
    int RequiredLevel,
    int AdditionalDataId,
    int EquipBonusId,
    string SecondaryKind,
    int WeaponPower,
    int WeaponFormula,
    int WeaponEvasion,
    int ArmorHpBonus,
    int ArmorMpBonus,
    int ShieldPhysicalEvasion,
    int ShieldMagicalEvasion,
    int AccessoryPhysicalEvasion,
    int AccessoryMagicalEvasion,
    int BonusPa,
    int BonusMa,
    int BonusSpeed,
    int BonusMove,
    int BonusJump,
    int WeaponRange,
    string WeaponAttackFlags,
    string WeaponElements,
    int WeaponOptionsAbilityId,
    string BonusInnateStatus,
    string BonusImmuneStatus,
    string BonusStartingStatus,
    string BonusAbsorbElements,
    string BonusNullifyElements,
    string BonusHalveElements,
    string BonusWeakElements,
    string BonusStrongElements,
    int BonusBoostJp)
{
    private static readonly string[] KnownCategories =
    [
        "none", "knife", "ninjablade", "sword", "knightsword", "fellsword", "katana", "axe",
        "flail", "rod", "staff", "pole", "polearm", "crossbow", "bow", "gun", "book",
        "instrument", "bag", "cloth", "bomb", "throwing", "shield", "helmet", "hat", "hairadornment", "armor",
        "clothing", "robe", "shoes", "armguard", "armlet", "cloak", "ring", "perfume",
        "liprouge"
    ];

    private static readonly string[] KnownTypeFlags = ["weapon", "shield", "armor", "headgear", "accessory", "rare"];

    // Canonical name sets extracted from work/item_catalog.csv (2026-07-03); every list column is
    // exploded into per-name 0/1 formula vars so DCL formulas never parse strings.
    private static readonly string[] KnownAttackFlags =
        ["arc", "direct", "forcedtwohands", "lunging", "striking", "throwable", "twohands", "twoswords"];

    private static readonly string[] KnownElements =
        ["dark", "earth", "fire", "holy", "ice", "lightning", "water", "wind"];

    private static readonly string[] KnownStatuses =
    [
        "berserk", "blind", "charm", "confuse", "disable", "doom", "faith", "float", "haste",
        "immobilize", "invisible", "ko", "poison", "protect", "reflect", "regen", "reraise",
        "shell", "silence", "sleep", "slow", "stone", "stop", "toad", "traitor", "undead", "vampire"
    ];

    // (listPrefix, canonical set) pairs for the exploded 0/1 vars; value source resolved in AddVariables.
    private static readonly (string Prefix, string[] Names)[] ListVarGroups =
    [
        ("atkflag", KnownAttackFlags),
        ("element", KnownElements),
        ("innate", KnownStatuses),
        ("immune", KnownStatuses),
        ("start", KnownStatuses),
        ("absorb", KnownElements),
        ("nullify", KnownElements),
        ("halve", KnownElements),
        ("weak", KnownElements),
        ("strong", KnownElements),
    ];

    public static ItemCatalogEntry FromRow(int itemId, Dictionary<string, string> row)
        => new(
            itemId,
            Text(row, "name"),
            Text(row, "type_flags"),
            Text(row, "item_category"),
            ItemCatalog.ReadInt(row, "required_level"),
            ItemCatalog.ReadInt(row, "additional_data_id"),
            ItemCatalog.ReadInt(row, "equip_bonus_id"),
            Text(row, "secondary_kind"),
            ItemCatalog.ReadInt(row, "weapon_power"),
            ItemCatalog.ReadInt(row, "weapon_formula"),
            ItemCatalog.ReadInt(row, "weapon_evasion"),
            ItemCatalog.ReadInt(row, "armor_hp_bonus"),
            ItemCatalog.ReadInt(row, "armor_mp_bonus"),
            ItemCatalog.ReadInt(row, "shield_physical_evasion"),
            ItemCatalog.ReadInt(row, "shield_magical_evasion"),
            ItemCatalog.ReadInt(row, "accessory_physical_evasion"),
            ItemCatalog.ReadInt(row, "accessory_magical_evasion"),
            ItemCatalog.ReadInt(row, "bonus_pa"),
            ItemCatalog.ReadInt(row, "bonus_ma"),
            ItemCatalog.ReadInt(row, "bonus_speed"),
            ItemCatalog.ReadInt(row, "bonus_move"),
            ItemCatalog.ReadInt(row, "bonus_jump"),
            ItemCatalog.ReadInt(row, "weapon_range"),
            Text(row, "weapon_attack_flags"),
            Text(row, "weapon_elements"),
            ItemCatalog.ReadInt(row, "weapon_options_ability_id"),
            Text(row, "bonus_innate_status"),
            Text(row, "bonus_immune_status"),
            Text(row, "bonus_starting_status"),
            Text(row, "bonus_absorb_elements"),
            Text(row, "bonus_nullify_elements"),
            Text(row, "bonus_halve_elements"),
            Text(row, "bonus_weak_elements"),
            Text(row, "bonus_strong_elements"),
            Text(row, "bonus_boost_jp").Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

    public bool HasTypeFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return true;
        return TypeFlags.Split(',').Any(part => part.Trim().Equals(flag, StringComparison.OrdinalIgnoreCase));
    }

    public int IsSecondaryKind(string kind)
        => SecondaryKind.Equals(kind, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

    public int IsCategory(string category)
        => ItemCategory.Equals(category, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

    public void AddVariables(FormulaContext context, string prefix)
    {
        Set(context, prefix, "id", ItemId);
        Set(context, prefix, "itemId", ItemId);
        Set(context, prefix, "requiredLevel", RequiredLevel);
        Set(context, prefix, "additionalDataId", AdditionalDataId);
        Set(context, prefix, "equipBonusId", EquipBonusId);
        Set(context, prefix, "weaponPower", WeaponPower);
        Set(context, prefix, "weaponFormula", WeaponFormula);
        Set(context, prefix, "weaponEvasion", WeaponEvasion);
        Set(context, prefix, "armorHpBonus", ArmorHpBonus);
        Set(context, prefix, "armorMpBonus", ArmorMpBonus);
        Set(context, prefix, "shieldPhysicalEvasion", ShieldPhysicalEvasion);
        Set(context, prefix, "shieldMagicalEvasion", ShieldMagicalEvasion);
        Set(context, prefix, "accessoryPhysicalEvasion", AccessoryPhysicalEvasion);
        Set(context, prefix, "accessoryMagicalEvasion", AccessoryMagicalEvasion);
        Set(context, prefix, "bonusPa", BonusPa);
        Set(context, prefix, "bonusMa", BonusMa);
        Set(context, prefix, "bonusSpeed", BonusSpeed);
        Set(context, prefix, "bonusMove", BonusMove);
        Set(context, prefix, "bonusJump", BonusJump);
        Set(context, prefix, "isWeapon", IsSecondaryKind("weapon"));
        Set(context, prefix, "isArmor", IsSecondaryKind("armor"));
        Set(context, prefix, "isShield", IsSecondaryKind("shield"));
        Set(context, prefix, "isAccessory", IsSecondaryKind("accessory"));
        Set(context, prefix, "weaponRange", WeaponRange);
        Set(context, prefix, "weaponOptionsAbilityId", WeaponOptionsAbilityId);
        Set(context, prefix, "boostJp", BonusBoostJp);

        foreach (string knownCategory in KnownCategories)
            Set(context, prefix, $"category_{knownCategory}", 0);
        foreach (string knownFlag in KnownTypeFlags)
            Set(context, prefix, $"type_{knownFlag}", 0);

        string category = FormulaExpression.NormalizeIdentifierPart(ItemCategory);
        if (category != "unnamed")
            Set(context, prefix, $"category_{category}", 1);

        foreach (string flag in TypeFlags.Split(',').Select(part => FormulaExpression.NormalizeIdentifierPart(part)))
        {
            if (flag != "unnamed")
                Set(context, prefix, $"type_{flag}", 1);
        }

        foreach (var (groupPrefix, names) in ListVarGroups)
        {
            string source = groupPrefix switch
            {
                "atkflag" => WeaponAttackFlags,
                "element" => WeaponElements,
                "innate" => BonusInnateStatus,
                "immune" => BonusImmuneStatus,
                "start" => BonusStartingStatus,
                "absorb" => BonusAbsorbElements,
                "nullify" => BonusNullifyElements,
                "halve" => BonusHalveElements,
                "weak" => BonusWeakElements,
                "strong" => BonusStrongElements,
                _ => "",
            };
            var present = source
                .Split(',')
                .Select(part => FormulaExpression.NormalizeIdentifierPart(part))
                .Where(part => part != "unnamed" && part != "none")
                .ToHashSet();
            foreach (string name in names)
                Set(context, prefix, $"{groupPrefix}_{name}", present.Contains(name) ? 1 : 0);
        }
    }

    public static void AddDefaultVariables(FormulaContext context, string prefix, int itemId = 0)
    {
        Set(context, prefix, "id", itemId);
        Set(context, prefix, "itemId", itemId);
        Set(context, prefix, "requiredLevel", 0);
        Set(context, prefix, "additionalDataId", 0);
        Set(context, prefix, "equipBonusId", 0);
        Set(context, prefix, "weaponPower", 0);
        Set(context, prefix, "weaponFormula", 0);
        Set(context, prefix, "weaponEvasion", 0);
        Set(context, prefix, "armorHpBonus", 0);
        Set(context, prefix, "armorMpBonus", 0);
        Set(context, prefix, "shieldPhysicalEvasion", 0);
        Set(context, prefix, "shieldMagicalEvasion", 0);
        Set(context, prefix, "accessoryPhysicalEvasion", 0);
        Set(context, prefix, "accessoryMagicalEvasion", 0);
        Set(context, prefix, "bonusPa", 0);
        Set(context, prefix, "bonusMa", 0);
        Set(context, prefix, "bonusSpeed", 0);
        Set(context, prefix, "bonusMove", 0);
        Set(context, prefix, "bonusJump", 0);
        Set(context, prefix, "isWeapon", 0);
        Set(context, prefix, "isArmor", 0);
        Set(context, prefix, "isShield", 0);
        Set(context, prefix, "isAccessory", 0);
        Set(context, prefix, "weaponRange", 0);
        Set(context, prefix, "weaponOptionsAbilityId", 0);
        Set(context, prefix, "boostJp", 0);

        foreach (string knownCategory in KnownCategories)
            Set(context, prefix, $"category_{knownCategory}", 0);
        foreach (string knownFlag in KnownTypeFlags)
            Set(context, prefix, $"type_{knownFlag}", 0);

        foreach (var (groupPrefix, names) in ListVarGroups)
            foreach (string name in names)
                Set(context, prefix, $"{groupPrefix}_{name}", 0);
    }

    private static string Text(Dictionary<string, string> row, string name)
        => row.TryGetValue(name, out string? text) ? text : "";

    private static void Set(FormulaContext context, string prefix, string name, int value)
        => context.Set($"{prefix}.{name}", Math.Max(0, value));
}
