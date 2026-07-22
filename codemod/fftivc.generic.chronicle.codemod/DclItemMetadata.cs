using System.Text.Json;
using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

internal enum DclItemSlot
{
    Unknown,
    Weapon,
    Shield,
    Body,
    Head,
    Accessory,
}

internal enum DclWeaponHands
{
    Unknown,
    OneHanded,
    TwoHanded,
}

internal sealed record DclWeaponMetadata(
    string SkillFamily,
    DclSkillDifficulty Difficulty,
    DclDamageBasis DamageBasis,
    string? FixedDamageExpression,
    int DamageModifier,
    int WholeDiceModifier,
    DclDamageType DamageType,
    DclRational ArmorDivisor,
    int Reach,
    int MaximumRange,
    DclWeaponHands Hands,
    DclRational? ParryLoadOverride,
    int ParryModifier,
    DclWeaponBalance Balance,
    DclWeaponReadinessProperty Readiness,
    int Accuracy,
    DclPhysicalRoute Trajectory,
    bool VisionRequired,
    DclRangedWeaponKind? RangedKind = null);

internal sealed record DclShieldMetadata(
    string SkillFamily,
    DclSkillDifficulty Difficulty,
    int BlockModifier,
    int DefenseBonus,
    IReadOnlySet<DclDelivery> CoveredDeliveries);

internal sealed record DclFocusMetadata(
    int SpellSkillModifier,
    int FocusDamageModifier,
    int FocusHealingModifier,
    DclRational? ElementBoostMultiplier,
    int ConcentrationModifier,
    int CastCtModifier,
    DclRational? MpCostMultiplier,
    IReadOnlyList<string> CompatibleTraditions);

internal sealed record DclElementItemProperty(
    string Element,
    bool Absorb,
    bool Nullify,
    int AffinityStep,
    DclRational? SourceBoostMultiplier);

internal sealed record DclItemMetadata(
    int ItemId,
    int ProfileRevision,
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
    DclWeaponMetadata? Weapon,
    DclShieldMetadata? Shield,
    DclFocusMetadata? Focus,
    IReadOnlyList<DclElementItemProperty> ElementProperties,
    IReadOnlyList<string> StatusImmunities,
    IReadOnlyList<string> SpecialProperties);

internal sealed record DclItemMetadataFinding(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}

internal sealed class DclItemMetadataValidation
{
    private readonly List<DclItemMetadataFinding> _findings = [];
    public IReadOnlyList<DclItemMetadataFinding> Findings => _findings;
    public bool IsValid => _findings.Count == 0;
    internal void Error(string path, string message) => _findings.Add(new(path, message));
}

internal static class DclItemMetadataContract
{
    public static DclItemMetadataValidation Validate(DclItemMetadata item, ItemCatalog nativeCatalog)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(nativeCatalog);
        var result = new DclItemMetadataValidation();
        if (item.ItemId is < 0 or > 255) result.Error("ItemId", "must identify an existing native item in 0..255");
        else if (!nativeCatalog.TryGet(item.ItemId, out _)) result.Error("ItemId", "does not exist in the native item catalog; the DCL adds no new SKU");
        if (item.ProfileRevision <= 0) result.Error("ProfileRevision", "must be positive");
        if (item.Slot == DclItemSlot.Unknown) result.Error("Slot", "must be explicit");
        if (item.Weight < DclRational.FromInteger(0)) result.Error("Weight", "cannot be negative");
        if (item.BodyDr < 0 || item.HeadDr < 0) result.Error("DR", "cannot be negative");
        if (item.Slot == DclItemSlot.Body && item.HeadDr != 0) result.Error("HeadDr", "body equipment cannot silently supply HeadDR");
        if (item.Slot == DclItemSlot.Head && item.BodyDr != 0) result.Error("BodyDr", "head equipment cannot silently supply BodyDR");
        if (item.Slot is not (DclItemSlot.Body or DclItemSlot.Accessory) && item.BodyDr != 0)
            result.Error("BodyDr", "is legal only for body equipment or an explicit accessory");
        if (item.Slot is not (DclItemSlot.Head or DclItemSlot.Accessory) && item.HeadDr != 0)
            result.Error("HeadDr", "is legal only for head equipment or an explicit accessory");

        if ((item.Slot == DclItemSlot.Weapon) != (item.Weapon is not null))
            result.Error("Weapon", "must exist exactly for Weapon slot metadata");
        if ((item.Slot == DclItemSlot.Shield) != (item.Shield is not null))
            result.Error("Shield", "must exist exactly for Shield slot metadata");
        if (item.Weapon is { } weapon) ValidateWeapon(weapon, result);
        if (item.Shield is { } shield) ValidateShield(shield, result);
        if (item.Focus is { } focus) ValidateFocus(focus, result);

        ValidateNamedList(item.StatusImmunities, "StatusImmunities", result);
        ValidateNamedList(item.SpecialProperties, "SpecialProperties", result);
        if (item.ElementProperties is null)
        {
            result.Error("ElementProperties", "is required, including when empty");
        }
        else
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < item.ElementProperties.Count; index++)
            {
                DclElementItemProperty property = item.ElementProperties[index];
                string path = $"ElementProperties[{index}]";
                if (string.IsNullOrWhiteSpace(property.Element)) result.Error($"{path}.Element", "is required");
                else if (!seen.Add(property.Element)) result.Error($"{path}.Element", "duplicates an element property");
                if (property.Absorb && property.Nullify) result.Error(path, "Absorb and Nullify cannot both be authored on one item property");
                if (property.SourceBoostMultiplier is { } boost && boost <= DclRational.FromInteger(0))
                    result.Error($"{path}.SourceBoostMultiplier", "must be positive");
            }
        }
        return result;
    }

    private static void ValidateWeapon(DclWeaponMetadata weapon, DclItemMetadataValidation result)
    {
        if (string.IsNullOrWhiteSpace(weapon.SkillFamily)) result.Error("Weapon.SkillFamily", "is required");
        if (weapon.Difficulty == DclSkillDifficulty.Unknown) result.Error("Weapon.Difficulty", "must be explicit");
        if (weapon.DamageBasis == DclDamageBasis.Unknown) result.Error("Weapon.DamageBasis", "must be explicit");
        if (weapon.DamageBasis == DclDamageBasis.Fixed && string.IsNullOrWhiteSpace(weapon.FixedDamageExpression))
            result.Error("Weapon.FixedDamageExpression", "Fixed basis requires an expression");
        else if (weapon.DamageBasis == DclDamageBasis.Fixed &&
                 !DclDiceExpression.TryParseAuthored(weapon.FixedDamageExpression, out _))
            result.Error("Weapon.FixedDamageExpression", "must use the exact Xd6+Y grammar without whitespace");
        if (weapon.DamageBasis != DclDamageBasis.Fixed && !string.IsNullOrWhiteSpace(weapon.FixedDamageExpression))
            result.Error("Weapon.FixedDamageExpression", "is legal only for Fixed basis");
        if (weapon.DamageType == DclDamageType.Unknown)
            result.Error("Weapon.DamageType", "must be one canonical wound-multiplier type");
        if (weapon.ArmorDivisor <= DclRational.FromInteger(0)) result.Error("Weapon.ArmorDivisor", "must be positive");
        if (weapon.Reach is < 0 or > 2 || weapon.MaximumRange < 0 || (weapon.Reach == 0 && weapon.MaximumRange == 0))
            result.Error("Weapon.Range", "must declare Reach 1/2 or a positive projectile range");
        if (weapon.Reach > 0 && weapon.MaximumRange > 0) result.Error("Weapon.Range", "melee Reach and projectile range are distinct profiles");
        if (weapon.Hands == DclWeaponHands.Unknown) result.Error("Weapon.Hands", "must be explicit");
        if (weapon.ParryLoadOverride is { } load && load < DclRational.FromInteger(0))
            result.Error("Weapon.ParryLoadOverride", "cannot be negative");
        if (weapon.Accuracy < 0) result.Error("Weapon.Accuracy", "cannot be negative");
        if (weapon.MaximumRange > 0 && weapon.Trajectory is not (DclPhysicalRoute.NativeDirect or DclPhysicalRoute.NativeArc))
            result.Error("Weapon.Trajectory", "projectiles require the native Direct or Arc route");
        if (weapon.Reach > 0 && weapon.Trajectory != DclPhysicalRoute.None)
            result.Error("Weapon.Trajectory", "melee weapons normalize trajectory to None");
        if ((weapon.MaximumRange > 0) != (weapon.RangedKind is not null))
            result.Error("Weapon.RangedKind", "must be present exactly for a projectile weapon");
    }

    private static void ValidateShield(DclShieldMetadata shield, DclItemMetadataValidation result)
    {
        if (string.IsNullOrWhiteSpace(shield.SkillFamily)) result.Error("Shield.SkillFamily", "is required");
        if (shield.Difficulty == DclSkillDifficulty.Unknown) result.Error("Shield.Difficulty", "must be explicit");
        if (shield.CoveredDeliveries is null || shield.CoveredDeliveries.Count == 0)
            result.Error("Shield.CoveredDeliveries", "must explicitly name physical/magical coverage");
        else if (shield.CoveredDeliveries.Contains(DclDelivery.InternalDirect))
            result.Error("Shield.CoveredDeliveries", "InternalDirect can never become shield coverage");
    }

    private static void ValidateFocus(DclFocusMetadata focus, DclItemMetadataValidation result)
    {
        if (focus.ElementBoostMultiplier is { } boost && boost <= DclRational.FromInteger(0))
            result.Error("Focus.ElementBoostMultiplier", "must be positive");
        if (focus.MpCostMultiplier is { } cost && cost <= DclRational.FromInteger(0))
            result.Error("Focus.MpCostMultiplier", "must be positive");
        ValidateNamedList(focus.CompatibleTraditions, "Focus.CompatibleTraditions", result);
        bool empty = focus.SpellSkillModifier == 0 && focus.FocusDamageModifier == 0 &&
            focus.FocusHealingModifier == 0 && focus.ElementBoostMultiplier is null &&
            focus.ConcentrationModifier == 0 && focus.CastCtModifier == 0 &&
            focus.MpCostMultiplier is null && (focus.CompatibleTraditions?.Count ?? 0) == 0;
        if (empty) result.Error("Focus", "an empty focus profile must be omitted");
    }

    private static void ValidateNamedList(
        IReadOnlyList<string>? values,
        string path,
        DclItemMetadataValidation result)
    {
        if (values is null)
        {
            result.Error(path, "is required, including when empty");
            return;
        }
        for (int index = 0; index < values.Count; index++)
            if (string.IsNullOrWhiteSpace(values[index])) result.Error($"{path}[{index}]", "cannot be blank");
        if (values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
            values.Count(value => !string.IsNullOrWhiteSpace(value)))
            result.Error(path, "cannot contain duplicates");
    }
}

internal sealed record DclItemMetadataBundle(int SchemaRevision, IReadOnlyList<DclItemMetadata> Items);

internal sealed class DclItemMetadataRegistry
{
    private readonly Dictionary<int, DclItemMetadata> _items = [];
    public IReadOnlyDictionary<int, DclItemMetadata> Items => _items;

    public DclItemMetadataValidation TryRegister(DclItemMetadata item, ItemCatalog nativeCatalog)
    {
        DclItemMetadataValidation validation = DclItemMetadataContract.Validate(item, nativeCatalog);
        if (!validation.IsValid) return validation;
        if (_items.TryGetValue(item.ItemId, out DclItemMetadata? existing))
        {
            validation.Error("ItemId", $"item is already loaded at revision {existing.ProfileRevision}");
            return validation;
        }
        _items.Add(item.ItemId, item);
        return validation;
    }
}

internal static class DclItemMetadataJsonLoader
{
    public const int CurrentSchemaRevision = 1;

    public static DclItemMetadataRegistry Load(string json, ItemCatalog nativeCatalog)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new DclAuthoringLoadException("The DCL item metadata bundle is empty.");
        DclItemMetadataBundle bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<DclItemMetadataBundle>(json, CreateOptions())
                ?? throw new JsonException("Item metadata bundle deserialized to null.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or FormatException or OverflowException)
        {
            throw new DclAuthoringLoadException("The DCL item metadata bundle is not valid strict JSON.", exception);
        }
        if (bundle.SchemaRevision != CurrentSchemaRevision)
            throw new DclAuthoringLoadException($"Unsupported DCL item schema revision {bundle.SchemaRevision}.");
        if (bundle.Items is null) throw new DclAuthoringLoadException("Items array is required, including when empty.");
        var registry = new DclItemMetadataRegistry();
        var findings = new List<string>();
        for (int index = 0; index < bundle.Items.Count; index++)
        {
            DclItemMetadata? item = bundle.Items[index];
            if (item is null)
            {
                findings.Add($"items[{index}]: entry is null");
                continue;
            }
            DclItemMetadataValidation validation = registry.TryRegister(item, nativeCatalog);
            findings.AddRange(validation.Findings.Select(finding => $"items[{index}].{finding}"));
        }
        if (findings.Count > 0)
            throw new DclAuthoringLoadException($"The DCL item metadata bundle failed validation: {string.Join("; ", findings)}");
        return registry;
    }

    public static string Serialize(DclItemMetadataBundle bundle)
        => JsonSerializer.Serialize(bundle, CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.Converters.Add(new DclRationalJsonConverter());
        return options;
    }
}
