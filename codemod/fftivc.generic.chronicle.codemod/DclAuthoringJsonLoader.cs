using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclAuthoringBundle(
    int SchemaRevision,
    IReadOnlyList<DclActionProfile> Actions,
    IReadOnlyList<DclStateDefinition> States,
    IReadOnlyList<DclReactionDefinition> Reactions);

internal sealed class DclAuthoringLoadException : Exception
{
    public DclAuthoringLoadException(string message) : base(message) { }
    public DclAuthoringLoadException(string message, Exception innerException) : base(message, innerException) { }
}

internal static class DclAuthoringJsonLoader
{
    public const int CurrentSchemaRevision = 4;

    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static DclAuthoringRegistry Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new DclAuthoringLoadException("The DCL authoring bundle is empty.");
        DclAuthoringBundle bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<DclAuthoringBundle>(json, Options)
                ?? throw new DclAuthoringLoadException("The DCL authoring bundle deserialized to null.");
        }
        catch (DclAuthoringLoadException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or FormatException or OverflowException)
        {
            throw new DclAuthoringLoadException("The DCL authoring bundle is not valid strict JSON for the normalized schema.", exception);
        }

        if (bundle.SchemaRevision != CurrentSchemaRevision)
            throw new DclAuthoringLoadException(
                $"Unsupported DCL authoring schema revision {bundle.SchemaRevision}; expected {CurrentSchemaRevision}.");
        if (bundle.Actions is null || bundle.States is null || bundle.Reactions is null)
            throw new DclAuthoringLoadException("Actions, States, and Reactions arrays are required, including when empty.");

        var registry = new DclAuthoringRegistry();
        var findings = new List<string>();
        for (int index = 0; index < bundle.Actions.Count; index++)
        {
            DclActionProfile? profile = bundle.Actions[index];
            if (profile is null)
            {
                findings.Add($"actions[{index}]: entry is null");
                continue;
            }
            DclAuthoringValidation validation = registry.TryRegister(profile);
            findings.AddRange(validation.Findings.Select(finding => $"actions[{index}].{finding}"));
        }
        for (int index = 0; index < bundle.States.Count; index++)
        {
            DclStateDefinition? definition = bundle.States[index];
            if (definition is null)
            {
                findings.Add($"states[{index}]: entry is null");
                continue;
            }
            DclAuthoringValidation validation = registry.TryRegister(definition);
            findings.AddRange(validation.Findings.Select(finding => $"states[{index}].{finding}"));
        }
        for (int index = 0; index < bundle.Reactions.Count; index++)
        {
            DclReactionDefinition? definition = bundle.Reactions[index];
            if (definition is null)
            {
                findings.Add($"reactions[{index}]: entry is null");
                continue;
            }
            DclReactionValidation validation = registry.TryRegister(definition);
            findings.AddRange(validation.Findings.Select(finding => $"reactions[{index}].{finding.Path}: {finding.Message}"));
        }
        findings.AddRange(registry.ValidateReferences().Findings.Select(finding => finding.ToString()));
        if (findings.Count > 0)
            throw new DclAuthoringLoadException(
                $"The DCL authoring bundle failed normalized validation: {string.Join("; ", findings)}");
        return registry;
    }

    public static string Serialize(DclAuthoringBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        return JsonSerializer.Serialize(bundle, Options);
    }

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

internal sealed class DclRationalJsonConverter : JsonConverter<DclRational>
{
    public override DclRational Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Exact DCL rationals must be authored as quoted decimal or numerator/denominator strings.");
        string text = reader.GetString() ?? throw new JsonException("Exact DCL rational cannot be null.");
        int slash = text.IndexOf('/');
        if (slash < 0) return DclRational.ParseExactDecimal(text);
        if (slash == 0 || slash == text.Length - 1 || text.IndexOf('/', slash + 1) >= 0)
            throw new JsonException("A rational fraction must contain exactly one numerator/denominator separator.");
        if (!BigInteger.TryParse(text[..slash], out BigInteger numerator) ||
            !BigInteger.TryParse(text[(slash + 1)..], out BigInteger denominator))
            throw new JsonException("A rational fraction contains a non-integral numerator or denominator.");
        return new DclRational(numerator, denominator);
    }

    public override void Write(Utf8JsonWriter writer, DclRational value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
