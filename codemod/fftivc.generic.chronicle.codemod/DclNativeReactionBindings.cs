using System.Text.Json;
using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Joins one normalized Reaction definition to the two independent native identities retained by
/// a queued Reaction actor. NativeReactionAbilityId owns presentation/dispatch at actor+0x18C;
/// NativeEffectAbilityId owns the executable effect at actor+0x142.
/// </summary>
internal sealed record DclNativeReactionBinding(
    string ReactionId,
    int NativeReactionAbilityId,
    int NativeEffectAbilityId,
    string EffectActionId,
    int EffectProfileRevision);

internal sealed record DclNativeReactionBindingFinding(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}

internal sealed class DclNativeReactionBindingValidation
{
    private readonly List<DclNativeReactionBindingFinding> _findings = [];
    public IReadOnlyList<DclNativeReactionBindingFinding> Findings => _findings;
    public bool IsValid => _findings.Count == 0;
    internal void Error(string path, string message) => _findings.Add(new(path, message));
}

internal static class DclNativeReactionBindingContract
{
    public static DclNativeReactionBindingValidation Validate(
        DclNativeReactionBinding binding,
        AbilityCatalog nativeCatalog,
        DclAuthoringRegistry authoring,
        DclAbilityBindingRegistry abilities)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(nativeCatalog);
        ArgumentNullException.ThrowIfNull(authoring);
        ArgumentNullException.ThrowIfNull(abilities);
        var result = new DclNativeReactionBindingValidation();

        DclReactionDefinition? definition = null;
        if (string.IsNullOrWhiteSpace(binding.ReactionId))
            result.Error("ReactionId", "is required");
        else if (!authoring.Reactions.TryGetValue(binding.ReactionId, out definition))
            result.Error("ReactionId", "does not resolve to a normalized Reaction definition");

        RequireNativeAbility(result, nativeCatalog, "NativeReactionAbilityId", binding.NativeReactionAbilityId);
        RequireNativeAbility(result, nativeCatalog, "NativeEffectAbilityId", binding.NativeEffectAbilityId);

        if (string.IsNullOrWhiteSpace(binding.EffectActionId))
        {
            result.Error("EffectActionId", "is required");
        }
        else if (!authoring.Actions.TryGetValue(binding.EffectActionId, out DclActionProfile? effectAction))
        {
            result.Error("EffectActionId", "does not resolve to a normalized effect Action");
        }
        else if (effectAction.ProfileRevision != binding.EffectProfileRevision)
        {
            result.Error("EffectProfileRevision", $"does not match loaded effect Action revision {effectAction.ProfileRevision}");
        }

        if (definition is not null &&
            !StringComparer.Ordinal.Equals(definition.EffectActionId, binding.EffectActionId))
            result.Error("EffectActionId", $"does not match Reaction effect Action '{definition.EffectActionId}'");

        if (!abilities.Bindings.TryGetValue(binding.NativeEffectAbilityId, out DclAbilityBinding? effectAbility))
        {
            result.Error("NativeEffectAbilityId", "has no canonical ability binding");
        }
        else
        {
            if (!StringComparer.Ordinal.Equals(effectAbility.ActionId, binding.EffectActionId))
                result.Error("NativeEffectAbilityId", $"is bound to Action '{effectAbility.ActionId}', not the Reaction effect Action");
            if (effectAbility.ProfileRevision != binding.EffectProfileRevision)
                result.Error("NativeEffectAbilityId", $"is bound at effect revision {effectAbility.ProfileRevision}, not {binding.EffectProfileRevision}");
        }

        return result;
    }

    private static void RequireNativeAbility(
        DclNativeReactionBindingValidation result,
        AbilityCatalog nativeCatalog,
        string path,
        int abilityId)
    {
        if (abilityId is < 0 or > 511)
            result.Error(path, "must identify one native ability record in 0..511");
        else if (!nativeCatalog.TryGet(abilityId, out _))
            result.Error(path, "does not exist in the native ability catalog");
    }
}

internal sealed class DclNativeReactionBindingRegistry
{
    private readonly Dictionary<string, DclNativeReactionBinding> _bindings = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, DclNativeReactionBinding> Bindings => _bindings;

    public DclNativeReactionBindingValidation TryRegister(
        DclNativeReactionBinding binding,
        AbilityCatalog nativeCatalog,
        DclAuthoringRegistry authoring,
        DclAbilityBindingRegistry abilities)
    {
        DclNativeReactionBindingValidation validation =
            DclNativeReactionBindingContract.Validate(binding, nativeCatalog, authoring, abilities);
        if (!validation.IsValid) return validation;
        if (_bindings.ContainsKey(binding.ReactionId))
        {
            validation.Error("ReactionId", "already has one native binding");
            return validation;
        }
        _bindings.Add(binding.ReactionId, binding);
        return validation;
    }

    public DclNativeReactionBindingValidation Audit(DclAuthoringRegistry authoring)
    {
        ArgumentNullException.ThrowIfNull(authoring);
        var result = new DclNativeReactionBindingValidation();
        foreach (string reactionId in authoring.Reactions.Keys.Order(StringComparer.Ordinal))
            if (!_bindings.ContainsKey(reactionId))
                result.Error($"Reactions[{reactionId}]", "has no native presentation/effect binding");
        foreach (string reactionId in _bindings.Keys.Order(StringComparer.Ordinal))
            if (!authoring.Reactions.ContainsKey(reactionId))
                result.Error($"Bindings[{reactionId}]", "has no normalized Reaction definition");
        return result;
    }
}

internal sealed record DclNativeReactionBindingBundle(
    int SchemaRevision,
    IReadOnlyList<DclNativeReactionBinding> Bindings);

internal static class DclNativeReactionBindingJsonLoader
{
    public const int CurrentSchemaRevision = 1;

    public static DclNativeReactionBindingRegistry Load(
        string json,
        AbilityCatalog nativeCatalog,
        DclAuthoringRegistry authoring,
        DclAbilityBindingRegistry abilities)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new DclAuthoringLoadException("The DCL native Reaction binding bundle is empty.");
        DclNativeReactionBindingBundle bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<DclNativeReactionBindingBundle>(json, CreateOptions())
                ?? throw new JsonException("Native Reaction binding bundle deserialized to null.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new DclAuthoringLoadException("The DCL native Reaction binding bundle is not valid strict JSON.", exception);
        }
        if (bundle.SchemaRevision != CurrentSchemaRevision)
            throw new DclAuthoringLoadException($"Unsupported DCL native Reaction-binding schema revision {bundle.SchemaRevision}.");
        if (bundle.Bindings is null)
            throw new DclAuthoringLoadException("Bindings array is required, including when empty.");

        var registry = new DclNativeReactionBindingRegistry();
        var findings = new List<string>();
        for (int index = 0; index < bundle.Bindings.Count; index++)
        {
            DclNativeReactionBinding? binding = bundle.Bindings[index];
            if (binding is null)
            {
                findings.Add($"bindings[{index}]: entry is null");
                continue;
            }
            DclNativeReactionBindingValidation validation =
                registry.TryRegister(binding, nativeCatalog, authoring, abilities);
            findings.AddRange(validation.Findings.Select(finding => $"bindings[{index}].{finding}"));
        }
        DclNativeReactionBindingValidation coverage = registry.Audit(authoring);
        findings.AddRange(coverage.Findings.Select(finding => $"coverage.{finding}"));
        if (findings.Count > 0)
            throw new DclAuthoringLoadException(
                $"The DCL native Reaction binding bundle failed validation: {string.Join("; ", findings)}");
        return registry;
    }

    public static string Serialize(DclNativeReactionBindingBundle bundle)
        => JsonSerializer.Serialize(bundle, CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            WriteIndented = true,
        };
    }
}
