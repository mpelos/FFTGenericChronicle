using System.Text;
using System.Text.Json;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclGrowthPersistenceLoad(
    DclGrowthState? State,
    string SerializedRecord,
    bool GrowthEnabled,
    string? CompatibilityError)
{
    public string SerializeForSave()
        => State is null ? SerializedRecord : DclGrowthStateCodec.Serialize(State);
}

internal sealed class DclGrowthPersistenceException : Exception
{
    public DclGrowthPersistenceException(string message) : base(message) { }

    public DclGrowthPersistenceException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Strict per-character persistence for the DCL growth record. Current records use a deterministic
/// fixed-field JSON shape. Unknown newer records remain opaque and byte-for-byte reusable so an
/// older mod cannot erase progress it does not understand.
/// </summary>
internal static class DclGrowthStateCodec
{
    private const string RevisionProperty = "growthSchemaRevision";
    private const string LevelProperty = "highestAwardedCharacterLevel";
    private const string ProgressProperty = "growthProgressMicro";

    private static readonly IReadOnlyDictionary<DclGrowthChannel, string> ChannelProperties =
        new Dictionary<DclGrowthChannel, string>
        {
            [DclGrowthChannel.St] = "st",
            [DclGrowthChannel.Dx] = "dx",
            [DclGrowthChannel.Iq] = "iq",
            [DclGrowthChannel.Brave] = "brave",
            [DclGrowthChannel.HpModifier] = "hpModifier",
            [DclGrowthChannel.MpModifier] = "mpModifier",
        };

    public static string Serialize(DclGrowthState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.GrowthSchemaRevision != DclCharacterGrowth.CurrentSchemaRevision)
            throw new DclGrowthPersistenceException(
                "Only the current growth schema may be serialized from a decoded state.");
        DclCharacterGrowth.ValidatePersistentState(state);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber(RevisionProperty, state.GrowthSchemaRevision);
            writer.WriteNumber(LevelProperty, state.HighestAwardedCharacterLevel);
            writer.WritePropertyName(ProgressProperty);
            writer.WriteStartObject();
            foreach (DclGrowthChannel channel in Enum.GetValues<DclGrowthChannel>())
                writer.WriteNumber(ChannelProperties[channel], state.GrowthProgressMicro[channel]);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static DclGrowthPersistenceLoad Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new DclGrowthPersistenceException("A persisted growth record is required.");
        try
        {
            using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new DclGrowthPersistenceException("A persisted growth record must be one JSON object.");

            Dictionary<string, JsonElement> rootProperties = UniqueProperties(root, "growth record");
            if (!rootProperties.TryGetValue(RevisionProperty, out JsonElement revisionElement) ||
                !revisionElement.TryGetInt32(out int revision) || revision < 1)
                throw new DclGrowthPersistenceException("GrowthSchemaRevision must be a positive integer.");

            if (revision > DclCharacterGrowth.CurrentSchemaRevision)
            {
                return new DclGrowthPersistenceLoad(
                    State: null,
                    SerializedRecord: json,
                    GrowthEnabled: false,
                    CompatibilityError: "unknown-newer-schema-growth-disabled");
            }
            if (revision < DclCharacterGrowth.CurrentSchemaRevision)
                throw new DclGrowthPersistenceException(
                    "A known older growth schema requires an explicit migration before loading.");

            RequireExactProperties(rootProperties.Keys, [RevisionProperty, LevelProperty, ProgressProperty], "growth record");
            if (!rootProperties[LevelProperty].TryGetInt32(out int level))
                throw new DclGrowthPersistenceException("HighestAwardedCharacterLevel must be an integer.");
            JsonElement progressElement = rootProperties[ProgressProperty];
            if (progressElement.ValueKind != JsonValueKind.Object)
                throw new DclGrowthPersistenceException("GrowthProgressMicro must be one JSON object.");
            Dictionary<string, JsonElement> progressProperties = UniqueProperties(progressElement, "growth progress");
            RequireExactProperties(progressProperties.Keys, ChannelProperties.Values, "growth progress");

            var progress = new Dictionary<DclGrowthChannel, long>();
            foreach (DclGrowthChannel channel in Enum.GetValues<DclGrowthChannel>())
            {
                string property = ChannelProperties[channel];
                if (!progressProperties[property].TryGetInt64(out long value))
                    throw new DclGrowthPersistenceException($"Growth progress {property} must be a signed 64-bit integer.");
                progress.Add(channel, value);
            }
            var state = new DclGrowthState(revision, level, progress);
            DclCharacterGrowth.ValidatePersistentState(state);
            string canonical = Serialize(state);
            return new DclGrowthPersistenceLoad(state, canonical, GrowthEnabled: true, CompatibilityError: null);
        }
        catch (DclGrowthPersistenceException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or OverflowException or ArgumentException)
        {
            throw new DclGrowthPersistenceException("The persisted growth record is invalid.", exception);
        }
    }

    private static Dictionary<string, JsonElement> UniqueProperties(JsonElement element, string owner)
    {
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
            if (!properties.TryAdd(property.Name, property.Value))
                throw new DclGrowthPersistenceException($"The {owner} duplicates property '{property.Name}'.");
        return properties;
    }

    private static void RequireExactProperties(
        IEnumerable<string> actual,
        IEnumerable<string> expected,
        string owner)
    {
        string[] actualOrdered = actual.Order(StringComparer.Ordinal).ToArray();
        string[] expectedOrdered = expected.Order(StringComparer.Ordinal).ToArray();
        if (!actualOrdered.SequenceEqual(expectedOrdered, StringComparer.Ordinal))
            throw new DclGrowthPersistenceException(
                $"The {owner} fields must be exactly: {string.Join(", ", expectedOrdered)}.");
    }
}
