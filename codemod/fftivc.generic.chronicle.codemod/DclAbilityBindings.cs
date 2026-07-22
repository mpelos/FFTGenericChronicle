using System.Text.Json;
using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

internal enum DclNativeCarrierKind
{
    Unknown,
    SingleResult,
    NativeRepeat,
    RandomFireRepeat,
    StatusPacket,
    ConditionalStatusProducer,
    PairedTargetSourceResult,
    EquipmentTransaction,
    LifecycleTransaction,
    NativeSpecialPreserved,
    SyntheticReaction,
}

internal enum DclCarrierRewritePolicy
{
    Unknown,
    ReplaceNumericResult,
    ReplaceStatusPacket,
    ReplaceCompleteResult,
    PreserveNativeSpecial,
    ManagedProducer,
}

internal enum DclNativeStrikeCountPolicy
{
    ExactProfile,
    SingleOrProfileMaximum,
}

internal sealed record DclAbilityBinding(
    int AbilityId,
    int NativeFormula,
    string ActionId,
    int ProfileRevision,
    DclNativeCarrierKind CarrierKind,
    DclCarrierRewritePolicy RewritePolicy,
    bool RequiresDataNeutralization,
    string ForecastBoundary,
    string AiBoundary,
    string ExecutionBoundary,
    string ApplyBoundary,
    string PresentationBoundary,
    DclNativeStrikeCountPolicy NativeStrikeCountPolicy = DclNativeStrikeCountPolicy.ExactProfile);

internal sealed record DclAbilityBindingFinding(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}

internal sealed class DclAbilityBindingValidation
{
    private readonly List<DclAbilityBindingFinding> _findings = [];
    public IReadOnlyList<DclAbilityBindingFinding> Findings => _findings;
    public bool IsValid => _findings.Count == 0;
    internal void Error(string path, string message) => _findings.Add(new(path, message));
}

internal static class DclAbilityBindingContract
{
    public static DclAbilityBindingValidation Validate(
        DclAbilityBinding binding,
        AbilityCatalog nativeCatalog,
        DclAuthoringRegistry authoring)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(nativeCatalog);
        ArgumentNullException.ThrowIfNull(authoring);
        var result = new DclAbilityBindingValidation();
        if (binding.AbilityId is < 0 or > 511)
        {
            result.Error("AbilityId", "must identify one native ability record in 0..511");
        }
        else if (!nativeCatalog.TryGet(binding.AbilityId, out AbilityCatalogEntry? native))
        {
            result.Error("AbilityId", "does not exist in the native ability catalog");
        }
        else if (native.Formula != binding.NativeFormula)
        {
            result.Error("NativeFormula", $"does not match catalog formula 0x{native.Formula:X2}");
        }
        if (string.IsNullOrWhiteSpace(binding.ActionId))
        {
            result.Error("ActionId", "is required");
        }
        else if (!authoring.Actions.TryGetValue(binding.ActionId, out DclActionProfile? profile))
        {
            result.Error("ActionId", "does not resolve to a normalized action profile");
        }
        else
        {
            if (binding.ProfileRevision != profile.ProfileRevision)
                result.Error("ProfileRevision", $"does not match loaded action revision {profile.ProfileRevision}");
            ValidateCarrierCompatibility(binding, profile, result);
        }
        if (binding.CarrierKind == DclNativeCarrierKind.Unknown) result.Error("CarrierKind", "must be explicit");
        if (binding.RewritePolicy == DclCarrierRewritePolicy.Unknown) result.Error("RewritePolicy", "must be explicit");
        RequireText(result, "ForecastBoundary", binding.ForecastBoundary);
        RequireText(result, "AiBoundary", binding.AiBoundary);
        RequireText(result, "ExecutionBoundary", binding.ExecutionBoundary);
        RequireText(result, "ApplyBoundary", binding.ApplyBoundary);
        RequireText(result, "PresentationBoundary", binding.PresentationBoundary);
        if (binding.RewritePolicy == DclCarrierRewritePolicy.PreserveNativeSpecial && binding.RequiresDataNeutralization)
            result.Error("RequiresDataNeutralization", "a preserved native special cannot also require its data carrier neutralized");
        return result;
    }

    private static void ValidateCarrierCompatibility(
        DclAbilityBinding binding,
        DclActionProfile profile,
        DclAbilityBindingValidation result)
    {
        if (binding.CarrierKind == DclNativeCarrierKind.SingleResult && profile.TransactionProfile.StrikeCount != 1)
            result.Error("CarrierKind", "SingleResult requires exactly one normalized Strike");
        if (binding.CarrierKind is DclNativeCarrierKind.NativeRepeat or DclNativeCarrierKind.RandomFireRepeat &&
            profile.TransactionProfile.StrikeCount <= 1)
            result.Error("CarrierKind", "repeat carriers require more than one normalized Strike");
        bool hasStatus = profile.Effects.Any(effect => effect.Kind is
            DclEffectKind.StatusApplication or DclEffectKind.StatusRemoval or DclEffectKind.Dispel);
        if (binding.CarrierKind is DclNativeCarrierKind.StatusPacket or DclNativeCarrierKind.ConditionalStatusProducer && !hasStatus)
            result.Error("CarrierKind", "status carriers require at least one normalized status effect");
        bool lifecycle = profile.Effects.Any(effect => effect.Kind == DclEffectKind.Revive) ||
            profile.Effects.Any(effect => effect.EligibleTargetStates.HasFlag(DclEligibleTargetStates.Ko));
        if (binding.CarrierKind == DclNativeCarrierKind.LifecycleTransaction && !lifecycle)
            result.Error("CarrierKind", "LifecycleTransaction requires an explicit KO/revive lifecycle effect");
        if (binding.CarrierKind == DclNativeCarrierKind.NativeSpecialPreserved &&
            binding.RewritePolicy != DclCarrierRewritePolicy.PreserveNativeSpecial)
            result.Error("RewritePolicy", "NativeSpecialPreserved requires PreserveNativeSpecial");
        if (binding.NativeStrikeCountPolicy == DclNativeStrikeCountPolicy.SingleOrProfileMaximum &&
            (binding.CarrierKind != DclNativeCarrierKind.NativeRepeat ||
             profile.DeliveryProfile.Delivery != DclDelivery.PhysicalAttack ||
             profile.TransactionProfile.StrikeCount != 2))
            result.Error(
                "NativeStrikeCountPolicy",
                "SingleOrProfileMaximum is reserved for a physical two-Strike maximum whose native Attack carrier emits one or two sweeps");
    }

    public static bool SupportsEffectiveStrikeCount(
        DclAbilityBinding binding,
        DclActionProfile profile,
        int strikeCount)
        => strikeCount > 0 && (binding.NativeStrikeCountPolicy switch
        {
            DclNativeStrikeCountPolicy.ExactProfile => strikeCount == profile.TransactionProfile.StrikeCount,
            DclNativeStrikeCountPolicy.SingleOrProfileMaximum =>
                profile.DeliveryProfile.Delivery == DclDelivery.PhysicalAttack &&
                profile.TransactionProfile.StrikeCount == 2 &&
                strikeCount is 1 or 2,
            _ => false,
        });

    private static void RequireText(DclAbilityBindingValidation result, string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) result.Error(path, "is required and must name the proven carrier boundary");
    }
}

internal sealed class DclAbilityBindingRegistry
{
    private readonly Dictionary<int, DclAbilityBinding> _bindings = [];
    public IReadOnlyDictionary<int, DclAbilityBinding> Bindings => _bindings;

    public DclAbilityBindingValidation TryRegister(
        DclAbilityBinding binding,
        AbilityCatalog nativeCatalog,
        DclAuthoringRegistry authoring)
    {
        DclAbilityBindingValidation validation = DclAbilityBindingContract.Validate(binding, nativeCatalog, authoring);
        if (!validation.IsValid) return validation;
        if (_bindings.ContainsKey(binding.AbilityId))
        {
            validation.Error("AbilityId", "already has one canonical binding");
            return validation;
        }
        _bindings.Add(binding.AbilityId, binding);
        return validation;
    }

    public DclAbilityBindingCoverage Audit(AbilityCatalog nativeCatalog, IReadOnlySet<int>? explicitExclusions = null)
    {
        explicitExclusions ??= new HashSet<int>();
        var missing = new List<int>();
        var unknownExclusions = explicitExclusions.Where(id => !nativeCatalog.TryGet(id, out _)).Order().ToArray();
        for (int abilityId = 0; abilityId <= 511; abilityId++)
        {
            if (!nativeCatalog.TryGet(abilityId, out _)) continue;
            if (_bindings.ContainsKey(abilityId) || explicitExclusions.Contains(abilityId)) continue;
            missing.Add(abilityId);
        }
        return new DclAbilityBindingCoverage(
            _bindings.Count,
            explicitExclusions.Count,
            missing,
            unknownExclusions);
    }
}

internal sealed record DclAbilityBindingCoverage(
    int BoundCount,
    int ExcludedCount,
    IReadOnlyList<int> MissingAbilityIds,
    IReadOnlyList<int> UnknownExcludedIds)
{
    public bool Complete => MissingAbilityIds.Count == 0 && UnknownExcludedIds.Count == 0;
}

internal sealed record DclAbilityBindingBundle(int SchemaRevision, IReadOnlyList<DclAbilityBinding> Bindings);

internal static class DclAbilityBindingJsonLoader
{
    public const int CurrentSchemaRevision = 1;

    public static DclAbilityBindingRegistry Load(
        string json,
        AbilityCatalog nativeCatalog,
        DclAuthoringRegistry authoring)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new DclAuthoringLoadException("The DCL ability binding bundle is empty.");
        DclAbilityBindingBundle bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<DclAbilityBindingBundle>(json, CreateOptions())
                ?? throw new JsonException("Ability binding bundle deserialized to null.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new DclAuthoringLoadException("The DCL ability binding bundle is not valid strict JSON.", exception);
        }
        if (bundle.SchemaRevision != CurrentSchemaRevision)
            throw new DclAuthoringLoadException($"Unsupported DCL ability-binding schema revision {bundle.SchemaRevision}.");
        if (bundle.Bindings is null) throw new DclAuthoringLoadException("Bindings array is required, including when empty.");
        var registry = new DclAbilityBindingRegistry();
        var findings = new List<string>();
        for (int index = 0; index < bundle.Bindings.Count; index++)
        {
            DclAbilityBinding? binding = bundle.Bindings[index];
            if (binding is null)
            {
                findings.Add($"bindings[{index}]: entry is null");
                continue;
            }
            DclAbilityBindingValidation validation = registry.TryRegister(binding, nativeCatalog, authoring);
            findings.AddRange(validation.Findings.Select(finding => $"bindings[{index}].{finding}"));
        }
        if (findings.Count > 0)
            throw new DclAuthoringLoadException($"The DCL ability binding bundle failed validation: {string.Join("; ", findings)}");
        return registry;
    }

    public static string Serialize(DclAbilityBindingBundle bundle)
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
        return options;
    }
}
