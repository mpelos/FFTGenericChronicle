namespace fftivc.generic.chronicle.codemod;

internal sealed record DclStatePresentationProfileBundle(
    int SchemaRevision,
    IReadOnlyList<DclStatePresentationProfile> Profiles);

internal sealed record DclStatePresentationProfile(
    string PresentationId,
    string DisplayName,
    string MechanicalEffect,
    string? UnitIconAssetRef,
    string? TimelineIconAssetRef,
    string? PositionAssetRef,
    string? PaletteAssetRef,
    string? EntryFeedbackRef,
    IReadOnlyList<string> DetailTerms,
    bool ShowsSource,
    bool ShowsMagnitude,
    bool ShowsExpiry,
    bool ShowsStacking,
    IReadOnlyList<string> CureFamilies);

internal sealed record DclStatePresentationFinding(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}

internal sealed class DclStatePresentationValidation
{
    private readonly List<DclStatePresentationFinding> _findings = new();
    public IReadOnlyList<DclStatePresentationFinding> Findings => _findings;
    public bool IsValid => _findings.Count == 0;
    internal void Error(string path, string message) => _findings.Add(new(path, message));
}

internal sealed class DclStatePresentationProfileRegistry
{
    private readonly Dictionary<string, DclStatePresentationProfile> _profiles = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, DclStatePresentationProfile> Profiles => _profiles;

    public DclStatePresentationValidation TryRegister(DclStatePresentationProfile profile)
    {
        DclStatePresentationValidation validation = DclStatePresentationProfileContract.Validate(profile);
        if (!validation.IsValid) return validation;
        if (_profiles.ContainsKey(profile.PresentationId))
        {
            validation.Error("PresentationId", $"state presentation '{profile.PresentationId}' is already loaded");
            return validation;
        }
        _profiles.Add(profile.PresentationId, profile);
        return validation;
    }

    public DclStatePresentationValidation ValidateStateReferences(
        IReadOnlyDictionary<string, DclStateDefinition> states)
    {
        ArgumentNullException.ThrowIfNull(states);
        var validation = new DclStatePresentationValidation();
        foreach ((string stateKind, DclStateDefinition definition) in states)
        {
            if (!_profiles.ContainsKey(definition.PresentationProfile))
            {
                validation.Error(
                    $"States[{stateKind}].PresentationProfile",
                    $"presentation '{definition.PresentationProfile}' is not loaded in the state presentation catalog");
            }
        }
        return validation;
    }

    public IReadOnlyDictionary<string, string> BuildDisplayNamesByStateKind(
        IReadOnlyDictionary<string, DclStateDefinition> states)
    {
        ArgumentNullException.ThrowIfNull(states);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        DclStatePresentationValidation validation = ValidateStateReferences(states);
        if (!validation.IsValid)
        {
            throw new ArgumentException(
                $"State presentation references are incomplete: {string.Join("; ", validation.Findings)}",
                nameof(states));
        }
        foreach ((string stateKind, DclStateDefinition definition) in states)
        {
            result[stateKind] = _profiles[definition.PresentationProfile].DisplayName;
        }
        return result;
    }
}

internal static class DclStatePresentationProfileContract
{
    public static DclStatePresentationValidation Validate(DclStatePresentationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var validation = new DclStatePresentationValidation();
        RequireText(validation, "PresentationId", profile.PresentationId);
        RequireText(validation, "DisplayName", profile.DisplayName);
        RequireText(validation, "MechanicalEffect", profile.MechanicalEffect);
        RequireSymbolicAsset(validation, "UnitIconAssetRef", profile.UnitIconAssetRef, "NativeStatusIcon(", "PresentationId(");
        RequireSymbolicAsset(validation, "TimelineIconAssetRef", profile.TimelineIconAssetRef, "NativeStatusIcon(", "PresentationId(");
        RequireSymbolicAsset(validation, "PositionAssetRef", profile.PositionAssetRef, "NativePosition(");
        RequireSymbolicAsset(validation, "PaletteAssetRef", profile.PaletteAssetRef, "NativePalette(", "DclPalette(");
        RequireSymbolicAsset(validation, "EntryFeedbackRef", profile.EntryFeedbackRef, "NativeFeedback(", "DclFeedback(");
        RequireList(validation, "DetailTerms", profile.DetailTerms, allowEmpty: false);
        RequireList(validation, "CureFamilies", profile.CureFamilies, allowEmpty: true);
        if (profile.DetailTerms is not null && profile.DetailTerms.Count > 0)
        {
            bool declaresSelectedUnitDetail =
                profile.ShowsSource ||
                profile.ShowsMagnitude ||
                profile.ShowsExpiry ||
                profile.ShowsStacking ||
                profile.CureFamilies is { Count: > 0 };
            if (!declaresSelectedUnitDetail)
                validation.Error(
                    "DetailTerms",
                    "must expose at least one selected-unit detail channel: source, magnitude, expiry, stacking, or cure family");
        }
        return validation;
    }

    private static void RequireText(DclStatePresentationValidation validation, string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) validation.Error(path, "is required");
    }

    private static void RequireList(
        DclStatePresentationValidation validation,
        string path,
        IReadOnlyList<string>? values,
        bool allowEmpty)
    {
        if (values is null)
        {
            validation.Error(path, "is required");
            return;
        }
        if (!allowEmpty && values.Count == 0) validation.Error(path, "cannot be empty");
        for (int index = 0; index < values.Count; index++)
            if (string.IsNullOrWhiteSpace(values[index])) validation.Error($"{path}[{index}]", "cannot be blank");
    }

    private static void RequireSymbolicAsset(
        DclStatePresentationValidation validation,
        string path,
        string? value,
        params string[] acceptedPrefixes)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!acceptedPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal)) ||
            !value.EndsWith(")", StringComparison.Ordinal) ||
            value[..^1].Contains(')', StringComparison.Ordinal))
        {
            validation.Error(path, $"must be a symbolic asset reference: {string.Join(" or ", acceptedPrefixes)}...");
        }
    }
}

internal sealed class DclStatePresentationProfileLoadException : Exception
{
    public DclStatePresentationProfileLoadException(string message) : base(message) { }
    public DclStatePresentationProfileLoadException(string message, Exception innerException) : base(message, innerException) { }
}

internal static class DclStatePresentationProfileJsonLoader
{
    public const int CurrentSchemaRevision = 1;

    private static readonly System.Text.Json.JsonSerializerOptions Options = CreateOptions();

    public static DclStatePresentationProfileRegistry Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new DclStatePresentationProfileLoadException("The DCL state presentation profile bundle is empty.");
        DclStatePresentationProfileBundle bundle;
        try
        {
            bundle = System.Text.Json.JsonSerializer.Deserialize<DclStatePresentationProfileBundle>(json, Options)
                ?? throw new DclStatePresentationProfileLoadException(
                    "The DCL state presentation profile bundle deserialized to null.");
        }
        catch (DclStatePresentationProfileLoadException)
        {
            throw;
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or NotSupportedException)
        {
            throw new DclStatePresentationProfileLoadException(
                "The DCL state presentation profile bundle is not valid strict JSON for the normalized schema.",
                exception);
        }

        if (bundle.SchemaRevision != CurrentSchemaRevision)
            throw new DclStatePresentationProfileLoadException(
                $"Unsupported DCL state presentation profile schema revision {bundle.SchemaRevision}; expected {CurrentSchemaRevision}.");
        if (bundle.Profiles is null)
            throw new DclStatePresentationProfileLoadException("Profiles array is required, including when empty.");

        var registry = new DclStatePresentationProfileRegistry();
        var findings = new List<string>();
        for (int index = 0; index < bundle.Profiles.Count; index++)
        {
            DclStatePresentationProfile? profile = bundle.Profiles[index];
            if (profile is null)
            {
                findings.Add($"profiles[{index}]: entry is null");
                continue;
            }
            DclStatePresentationValidation validation = registry.TryRegister(profile);
            findings.AddRange(validation.Findings.Select(finding => $"profiles[{index}].{finding}"));
        }
        if (findings.Count > 0)
            throw new DclStatePresentationProfileLoadException(
                $"The DCL state presentation profile bundle failed normalized validation: {string.Join("; ", findings)}");
        return registry;
    }

    public static string Serialize(DclStatePresentationProfileBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        return System.Text.Json.JsonSerializer.Serialize(bundle, Options);
    }

    private static System.Text.Json.JsonSerializerOptions CreateOptions()
        => new()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            WriteIndented = true,
        };
}
