namespace fftivc.generic.chronicle.codemod;

internal sealed class AbilityCatalog
{
    private readonly Dictionary<int, AbilityCatalogEntry> _abilities;

    private AbilityCatalog(
        string path,
        Dictionary<int, AbilityCatalogEntry> abilities,
        string error = "",
        string metadataPath = "",
        int metadataCount = 0,
        string metadataError = "")
    {
        Path = path;
        _abilities = abilities;
        Error = error;
        MetadataPath = metadataPath;
        MetadataCount = metadataCount;
        MetadataError = metadataError;
    }

    public string Path { get; }

    public string Error { get; }

    public string MetadataPath { get; }

    public int MetadataCount { get; }

    public string MetadataError { get; }

    public int Count => _abilities.Count;

    public bool Loaded => Count > 0 && string.IsNullOrWhiteSpace(Error);

    public static AbilityCatalog Empty(string path, string error = "") => new(path, new Dictionary<int, AbilityCatalogEntry>(), error);

    public static AbilityCatalog Load(string path, string metadataPath = "")
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
            var abilities = new Dictionary<int, AbilityCatalogEntry>();

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cells = ParseCsvLine(line);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count && i < cells.Count; i++)
                    row[headers[i]] = cells[i];

                int abilityId = ReadInt(row, "id_dec");
                if (abilityId < 0) continue;
                abilities[abilityId] = AbilityCatalogEntry.FromRow(abilityId, row);
            }

            int metadataCount = 0;
            string metadataError = "";
            if (!string.IsNullOrWhiteSpace(metadataPath))
            {
                var metadata = LoadApprovedMetadata(metadataPath, out metadataError);
                if (string.IsNullOrWhiteSpace(metadataError))
                {
                    foreach (var pair in metadata)
                    {
                        if (!abilities.TryGetValue(pair.Key, out var ability))
                        {
                            metadataError = $"approved metadata references missing ability {pair.Key}";
                            break;
                        }
                        abilities[pair.Key] = ability with { DclMetadata = pair.Value };
                        metadataCount++;
                    }
                }
            }

            return new AbilityCatalog(path, abilities, metadataPath: metadataPath,
                metadataCount: metadataCount, metadataError: metadataError);
        }
        catch (Exception ex)
        {
            return Empty(path, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public bool TryGet(int abilityId, out AbilityCatalogEntry ability) => _abilities.TryGetValue(abilityId, out ability!);

    public string Describe()
    {
        if (Loaded)
        {
            string metadata = string.IsNullOrWhiteSpace(MetadataPath)
                ? "DCL metadata disabled"
                : string.IsNullOrWhiteSpace(MetadataError)
                    ? $"{MetadataCount} approved DCL metadata rows from {MetadataPath}"
                    : $"DCL metadata disabled ({MetadataError})";
            return $"{Count} abilities from {Path}; {metadata}";
        }
        return string.IsNullOrWhiteSpace(Error) ? $"not loaded from {Path}" : $"not loaded from {Path} ({Error})";
    }

    private static Dictionary<int, DclAbilityMetadata> LoadApprovedMetadata(string path, out string error)
    {
        error = "";
        var result = new Dictionary<int, DclAbilityMetadata>();
        try
        {
            if (!File.Exists(path))
            {
                error = $"metadata file not found: {path}";
                return result;
            }

            using var reader = new StreamReader(path);
            string? headerLine = reader.ReadLine();
            if (headerLine is null)
            {
                error = "metadata file is empty";
                return result;
            }
            var headers = ParseCsvLine(headerLine);
            if (!headers.Any(name => name.Equals("ability_id", StringComparison.OrdinalIgnoreCase) ||
                                     name.Equals("id_dec", StringComparison.OrdinalIgnoreCase)) ||
                !headers.Any(name => name.Equals("approved", StringComparison.OrdinalIgnoreCase)))
            {
                error = "metadata requires ability_id (or id_dec) and approved columns";
                return result;
            }

            int lineNumber = 1;
            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cells = ParseCsvLine(line);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count && i < cells.Count; i++)
                    row[headers[i]] = cells[i];
                if (!ReadApproved(row)) continue;

                int abilityId = ReadInt(row, "ability_id");
                if (abilityId < 0) abilityId = ReadInt(row, "id_dec");
                if (abilityId is < 0 or > 511)
                    throw new InvalidDataException($"line {lineNumber}: approved ability id must be 0..511");
                if (result.ContainsKey(abilityId))
                    throw new InvalidDataException($"line {lineNumber}: duplicate approved ability id {abilityId}");
                result[abilityId] = DclAbilityMetadata.FromRow(row, lineNumber);
            }
        }
        catch (Exception ex)
        {
            result.Clear();
            error = $"{ex.GetType().Name}: {ex.Message}";
        }
        return result;
    }

    private static bool ReadApproved(Dictionary<string, string> row)
    {
        if (!row.TryGetValue("approved", out string? value)) return false;
        value = value.Trim();
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    internal static int ReadInt(Dictionary<string, string> row, string name)
    {
        return row.TryGetValue(name, out string? text) && int.TryParse(text, out int value) ? value : -1;
    }

    internal static int ReadIntOrZero(Dictionary<string, string> row, string name)
    {
        int value = ReadInt(row, name);
        return value < 0 ? 0 : value;
    }

    internal static int ReadHexOrZero(Dictionary<string, string> row, string name)
    {
        if (!row.TryGetValue(name, out string? text)) return 0;
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return 0;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(text[2..], System.Globalization.NumberStyles.HexNumber, null, out int hexValue) ? hexValue : 0;
        return int.TryParse(text, out int value) ? value : 0;
    }

    internal static int ReadBoolFlag(Dictionary<string, string> row, string name)
    {
        if (!row.TryGetValue(name, out string? text)) return 0;
        text = text.Trim();
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase)) return 1;
        if (text.Equals("false", StringComparison.OrdinalIgnoreCase)) return 0;
        return int.TryParse(text, out int value) && value != 0 ? 1 : 0;
    }

    internal static List<string> ParseCsvLine(string line)
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

internal sealed record AbilityCatalogEntry(
    int AbilityId,
    string Name,
    int Formula,
    int X,
    int Y,
    int Range,
    int Aoe,
    int Vertical,
    int Ct,
    int MpCost,
    string Elements,
    int InflictStatus,
    string InflictStatusMode,
    string InflictStatuses,
    int ForceSelfTarget,
    int Blank7,
    int WeaponRange,
    int VerticalFixed,
    int VerticalTolerance,
    int WeaponStrike,
    int Auto,
    int TargetSelf,
    int HitEnemies,
    int HitAllies,
    int TopDownTarget,
    int FollowTarget,
    int RandomFire,
    int LinearAttack,
    int ThreeDirections,
    int HitCaster,
    int Reflectable,
    int Arithmetickable,
    int Silenceable,
    int Mimicable,
    int NormalAttack,
    int Persevere,
    int ShowQuote,
    int AnimateOnMiss,
    int CounterFlood,
    int CounterMagic,
    int Direct,
    int Shirahadori,
    int RequiresSword,
    int RequiresMateriaBlade,
    int Evadeable,
    int JpCost,
    int UsedByEnemies,
    DclAbilityMetadata DclMetadata)
{
    private static readonly string[] KnownElements =
        ["dark", "earth", "fire", "holy", "ice", "lightning", "water", "wind"];

    private static readonly string[] KnownStatuses =
    [
        "berserk", "bloodsuck", "charm", "confusion", "crystal", "darkness", "dead",
        "deathsentence", "dontact", "dontmove", "faith", "float", "frog", "haste",
        "innocent", "invite", "oil", "petrify", "poison", "protect", "reflect",
        "regen", "reraise", "shell", "silence", "sleep", "slow", "stop",
        "transparent", "undead"
    ];

    private static readonly string[] KnownInflictModes = ["all", "random", "separate", "cancel"];

    private static readonly string[] FlagVariableNames =
    [
        "force_self_target", "blank7", "weapon_range", "vertical_fixed", "vertical_tolerance",
        "weapon_strike", "auto", "target_self", "hit_enemies", "hit_allies", "top_down_target",
        "follow_target", "random_fire", "linear_attack", "three_directions", "hit_caster",
        "reflectable", "arithmetickable", "silenceable", "mimicable", "normal_attack",
        "persevere", "show_quote", "animate_on_miss", "counter_flood", "counter_magic",
        "direct", "shirahadori", "requires_sword", "requires_materia_blade", "evadeable",
        "used_by_enemies"
    ];

    public static AbilityCatalogEntry FromRow(int abilityId, Dictionary<string, string> row)
        => new(
            abilityId,
            NameFromRow(row),
            AbilityCatalog.ReadHexOrZero(row, "formula_hex"),
            AbilityCatalog.ReadIntOrZero(row, "x"),
            AbilityCatalog.ReadIntOrZero(row, "y"),
            AbilityCatalog.ReadIntOrZero(row, "range"),
            AbilityCatalog.ReadIntOrZero(row, "aoe"),
            AbilityCatalog.ReadIntOrZero(row, "vertical"),
            AbilityCatalog.ReadIntOrZero(row, "ct"),
            AbilityCatalog.ReadIntOrZero(row, "mp_cost"),
            Text(row, "elements"),
            AbilityCatalog.ReadHexOrZero(row, "inflict_status_hex"),
            Text(row, "inflict_status_mode"),
            Text(row, "inflict_statuses"),
            AbilityCatalog.ReadBoolFlag(row, "ForceSelfTarget"),
            AbilityCatalog.ReadBoolFlag(row, "Blank7"),
            AbilityCatalog.ReadBoolFlag(row, "WeaponRange"),
            AbilityCatalog.ReadBoolFlag(row, "VerticalFixed"),
            AbilityCatalog.ReadBoolFlag(row, "VerticalTolerance"),
            AbilityCatalog.ReadBoolFlag(row, "WeaponStrike"),
            AbilityCatalog.ReadBoolFlag(row, "Auto"),
            AbilityCatalog.ReadBoolFlag(row, "TargetSelf"),
            AbilityCatalog.ReadBoolFlag(row, "HitEnemies"),
            AbilityCatalog.ReadBoolFlag(row, "HitAllies"),
            AbilityCatalog.ReadBoolFlag(row, "TopDownTarget"),
            AbilityCatalog.ReadBoolFlag(row, "FollowTarget"),
            AbilityCatalog.ReadBoolFlag(row, "RandomFire"),
            AbilityCatalog.ReadBoolFlag(row, "LinearAttack"),
            AbilityCatalog.ReadBoolFlag(row, "ThreeDirections"),
            AbilityCatalog.ReadBoolFlag(row, "HitCaster"),
            AbilityCatalog.ReadBoolFlag(row, "Reflectable"),
            AbilityCatalog.ReadBoolFlag(row, "Arithmetickable"),
            AbilityCatalog.ReadBoolFlag(row, "Silenceable"),
            AbilityCatalog.ReadBoolFlag(row, "Mimicable"),
            AbilityCatalog.ReadBoolFlag(row, "NormalAttack"),
            AbilityCatalog.ReadBoolFlag(row, "Persevere"),
            AbilityCatalog.ReadBoolFlag(row, "ShowQuote"),
            AbilityCatalog.ReadBoolFlag(row, "AnimateOnMiss"),
            AbilityCatalog.ReadBoolFlag(row, "CounterFlood"),
            AbilityCatalog.ReadBoolFlag(row, "CounterMagic"),
            AbilityCatalog.ReadBoolFlag(row, "Direct"),
            AbilityCatalog.ReadBoolFlag(row, "Shirahadori"),
            AbilityCatalog.ReadBoolFlag(row, "RequiresSword"),
            AbilityCatalog.ReadBoolFlag(row, "RequiresMateriaBlade"),
            AbilityCatalog.ReadBoolFlag(row, "Evadeable"),
            AbilityCatalog.ReadIntOrZero(row, "jp_cost"),
            AbilityCatalog.ReadBoolFlag(row, "used_by_enemies"),
            DclAbilityMetadata.Empty);

    public void AddVariables(FormulaContext context, string prefix)
    {
        Set(context, prefix, "id", AbilityId);
        Set(context, prefix, "abilityId", AbilityId);
        Set(context, prefix, "formula", Formula);
        Set(context, prefix, "x", X);
        Set(context, prefix, "y", Y);
        Set(context, prefix, "range", Range);
        Set(context, prefix, "aoe", Aoe);
        Set(context, prefix, "vertical", Vertical);
        Set(context, prefix, "ct", Ct);
        Set(context, prefix, "mp_cost", MpCost);
        Set(context, prefix, "jp_cost", JpCost);
        Set(context, prefix, "inflict_status", InflictStatus);

        foreach (var (name, value) in FlagVariables())
            Set(context, prefix, name, value);

        var elements = NormalizedList(Elements);
        foreach (string name in KnownElements)
            Set(context, prefix, $"element_{name}", elements.Contains(name) ? 1 : 0);

        var statuses = NormalizedList(InflictStatuses);
        foreach (string name in KnownStatuses)
            Set(context, prefix, $"inflict_{name}", statuses.Contains(name) ? 1 : 0);

        string mode = NormalizeInflictMode(InflictStatusMode);
        foreach (string name in KnownInflictModes)
            Set(context, prefix, $"inflict_mode_{name}", mode == name ? 1 : 0);

        DclMetadata.AddVariables(context, $"{prefix}.dcl");
    }

    public static void AddDefaultVariables(FormulaContext context, string prefix, int abilityId = 0)
    {
        Set(context, prefix, "id", abilityId);
        Set(context, prefix, "abilityId", abilityId);
        Set(context, prefix, "formula", 0);
        Set(context, prefix, "x", 0);
        Set(context, prefix, "y", 0);
        Set(context, prefix, "range", 0);
        Set(context, prefix, "aoe", 0);
        Set(context, prefix, "vertical", 0);
        Set(context, prefix, "ct", 0);
        Set(context, prefix, "mp_cost", 0);
        Set(context, prefix, "jp_cost", 0);
        Set(context, prefix, "inflict_status", 0);

        foreach (string name in FlagVariableNames)
            Set(context, prefix, name, 0);

        foreach (string name in KnownElements)
            Set(context, prefix, $"element_{name}", 0);
        foreach (string name in KnownStatuses)
            Set(context, prefix, $"inflict_{name}", 0);
        foreach (string name in KnownInflictModes)
            Set(context, prefix, $"inflict_mode_{name}", 0);

        DclAbilityMetadata.Empty.AddVariables(context, $"{prefix}.dcl");
    }

    private IEnumerable<(string Name, int Value)> FlagVariables()
    {
        yield return ("force_self_target", ForceSelfTarget);
        yield return ("blank7", Blank7);
        yield return ("weapon_range", WeaponRange);
        yield return ("vertical_fixed", VerticalFixed);
        yield return ("vertical_tolerance", VerticalTolerance);
        yield return ("weapon_strike", WeaponStrike);
        yield return ("auto", Auto);
        yield return ("target_self", TargetSelf);
        yield return ("hit_enemies", HitEnemies);
        yield return ("hit_allies", HitAllies);
        yield return ("top_down_target", TopDownTarget);
        yield return ("follow_target", FollowTarget);
        yield return ("random_fire", RandomFire);
        yield return ("linear_attack", LinearAttack);
        yield return ("three_directions", ThreeDirections);
        yield return ("hit_caster", HitCaster);
        yield return ("reflectable", Reflectable);
        yield return ("arithmetickable", Arithmetickable);
        yield return ("silenceable", Silenceable);
        yield return ("mimicable", Mimicable);
        yield return ("normal_attack", NormalAttack);
        yield return ("persevere", Persevere);
        yield return ("show_quote", ShowQuote);
        yield return ("animate_on_miss", AnimateOnMiss);
        yield return ("counter_flood", CounterFlood);
        yield return ("counter_magic", CounterMagic);
        yield return ("direct", Direct);
        yield return ("shirahadori", Shirahadori);
        yield return ("requires_sword", RequiresSword);
        yield return ("requires_materia_blade", RequiresMateriaBlade);
        yield return ("evadeable", Evadeable);
        yield return ("used_by_enemies", UsedByEnemies);
    }

    private static HashSet<string> NormalizedList(string source)
        => source
            .Replace('|', ',')
            .Split(',')
            .Select(part => FormulaExpression.NormalizeIdentifierPart(part))
            .Where(part => part != "unnamed" && part != "none")
            .ToHashSet();

    private static string NormalizeInflictMode(string mode)
    {
        string normalized = FormulaExpression.NormalizeIdentifierPart(mode);
        return normalized switch
        {
            "allornothing" or "all_or_nothing" => "all",
            "random" => "random",
            "separate" => "separate",
            "cancel" => "cancel",
            _ => "",
        };
    }

    private static string NameFromRow(Dictionary<string, string> row)
    {
        string name = Text(row, "name_ivc").Trim();
        return string.IsNullOrWhiteSpace(name) ? Text(row, "name_wotl").Trim() : name;
    }

    private static string Text(Dictionary<string, string> row, string name)
        => row.TryGetValue(name, out string? text) ? text : "";

    private static void Set(FormulaContext context, string prefix, string name, int value)
        => context.Set($"{prefix}.{name}", Math.Max(0, value));
}

internal sealed record DclAbilityMetadata(
    string ActionKind,
    string DamageType,
    string AvoidancePolicy,
    string StatusCategory,
    string SideEffectPolicy,
    int Power,
    int StrikeCount)
{
    public static DclAbilityMetadata Empty { get; } = new("", "", "", "", "", 0, 0);

    private static readonly string[] ActionKinds =
    [
        "physical_damage", "magic_damage", "hybrid_damage", "healing", "status_control",
        "resource_damage", "stat_or_trait_change", "passive", "item_dispatch", "command_dispatch",
        "special_control", "special_or_hybrid"
    ];
    private static readonly string[] DamageTypes =
    [
        "none", "weapon_defined", "swing", "thrust", "crush", "missile", "elemental",
        "spiritual", "magic_untyped", "hybrid_untyped", "special"
    ];
    private static readonly string[] AvoidancePolicies =
    [
        "none", "physical_contest", "physical_contest_then_status_contest",
        "magic_evade_per_target", "magic_evade_per_target_then_status_contest",
        "magic_evade_per_strike", "magic_evade_per_strike_then_status_contest",
        "status_contest", "auto_hit", "native"
    ];
    private static readonly string[] StatusCategories =
    ["none", "mental", "mental_inverted", "physical", "magical", "lifecycle_special", "mixed"];
    private static readonly string[] SideEffectPolicies =
    [
        "none_or_catalog_visuals", "managed_status_rider", "managed_multistrike",
        "managed_multistrike_status_rider", "native_multistrike", "native_multistrike_status_rider",
        "managed_status_commit", "equipment_transaction", "inventory_transaction",
        "multi_unit_transaction", "stat_or_trait_commit", "preserve_native_special",
        "external_or_data_dispatch", "managed_resource_commit", "special_handler",
        "one_hit_guard", "status_and_ct_transaction"
    ];

    public static DclAbilityMetadata FromRow(Dictionary<string, string> row, int lineNumber)
    {
        string actionKind = RequiredEnum(row, "action_kind", ActionKinds, lineNumber);
        string damageType = RequiredEnum(row, "damage_type", DamageTypes, lineNumber);
        string avoidance = RequiredEnum(row, "avoidance_policy", AvoidancePolicies, lineNumber);
        string status = RequiredEnum(row, "status_category", StatusCategories, lineNumber);
        string sideEffect = RequiredEnum(row, "side_effect_policy", SideEffectPolicies, lineNumber);
        int power = 0;
        if (row.TryGetValue("power", out string? powerText) && !string.IsNullOrWhiteSpace(powerText) &&
            (!int.TryParse(powerText, out power) || power < 0))
            throw new InvalidDataException($"line {lineNumber}: power must be a non-negative integer");
        int strikeCount = 0;
        if (row.TryGetValue("strike_count", out string? strikeCountText) && !string.IsNullOrWhiteSpace(strikeCountText) &&
            (!int.TryParse(strikeCountText, out strikeCount) || strikeCount < 0 || strikeCount > 99))
            throw new InvalidDataException($"line {lineNumber}: strike_count must be an integer within 0..99");
        bool managedMultistrike = sideEffect.Equals("managed_multistrike", StringComparison.OrdinalIgnoreCase) ||
                                  sideEffect.Equals("managed_multistrike_status_rider", StringComparison.OrdinalIgnoreCase);
        if (managedMultistrike && strikeCount < 2)
            throw new InvalidDataException(
                $"line {lineNumber}: managed_multistrike requires strike_count >= 2");
        if (!managedMultistrike && strikeCount != 0)
            throw new InvalidDataException(
                $"line {lineNumber}: strike_count is only valid with a managed multistrike side-effect policy");
        return new(actionKind, damageType, avoidance, status, sideEffect, power, strikeCount);
    }

    public bool IsManagedMultistrike
        => (SideEffectPolicy.Equals("managed_multistrike", StringComparison.OrdinalIgnoreCase) ||
            SideEffectPolicy.Equals("managed_multistrike_status_rider", StringComparison.OrdinalIgnoreCase)) &&
           StrikeCount >= 2;

    public bool IsNativeMultistrike
        => SideEffectPolicy.Equals("native_multistrike", StringComparison.OrdinalIgnoreCase) ||
           SideEffectPolicy.Equals("native_multistrike_status_rider", StringComparison.OrdinalIgnoreCase);

    public bool HasManagedStatusRider
        => SideEffectPolicy.Equals("managed_status_rider", StringComparison.OrdinalIgnoreCase) ||
           SideEffectPolicy.Equals("managed_multistrike_status_rider", StringComparison.OrdinalIgnoreCase) ||
           SideEffectPolicy.Equals("native_multistrike_status_rider", StringComparison.OrdinalIgnoreCase);

    public void AddVariables(FormulaContext context, string prefix)
    {
        Set(context, prefix, "approved", ReferenceEquals(this, Empty) ? 0 : 1);
        Set(context, prefix, "power", Power);
        Set(context, prefix, "strike_count", StrikeCount);
        AddEnum(context, prefix, "action_kind", ActionKind, ActionKinds);
        AddEnum(context, prefix, "damage_type", DamageType, DamageTypes);
        AddEnum(context, prefix, "avoidance", AvoidancePolicy, AvoidancePolicies);
        AddEnum(context, prefix, "status_category", StatusCategory, StatusCategories);
        AddEnum(context, prefix, "side_effect", SideEffectPolicy, SideEffectPolicies);
    }

    private static void AddEnum(FormulaContext context, string prefix, string field, string selected, string[] values)
    {
        foreach (string value in values)
            Set(context, prefix, $"{field}_{value}", selected == value ? 1 : 0);
    }

    private static string RequiredEnum(
        Dictionary<string, string> row, string name, string[] allowed, int lineNumber)
    {
        string raw = row.TryGetValue(name, out string? value) ? value : "";
        string normalized = FormulaExpression.NormalizeIdentifierPart(raw);
        if (!allowed.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"line {lineNumber}: {name} '{raw}' is not one of {string.Join('|', allowed)}");
        return normalized;
    }

    private static void Set(FormulaContext context, string prefix, string name, int value)
        => context.Set($"{prefix}.{name}", Math.Max(0, value));
}
