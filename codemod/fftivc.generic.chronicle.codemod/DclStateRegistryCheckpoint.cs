using System.Text.Json;
using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclStateRegistryRevisionCheckpoint(
    int UnitSlot,
    int CharacterId,
    long Revision);

internal sealed record DclStateInstanceCheckpoint(
    long InstanceId,
    string StateKind,
    string DefinitionFingerprint,
    int TargetUnitSlot,
    int TargetCharacterId,
    int? SourceUnitSlot,
    int? SourceCharacterId,
    long AppliedAtGlobalCt,
    long AppliedBeforeTurnSerial,
    long? ExpiresAtGlobalCt,
    long? ExpiresAfterTargetTurnSerial,
    long? ExpiresAfterSourceTurnSerial,
    int? RemainingUses,
    long? NextTickGlobalCt,
    int? Strength,
    int? WinningMargin,
    string StackDiscriminator,
    string? ContributionIdentity,
    string PayloadKind,
    string PayloadJson,
    string PresentationId);

internal sealed record DclStateRegistryCheckpoint(
    int SchemaRevision,
    int SavedBattleGeneration,
    long CurrentGlobalCt,
    long NextInstanceId,
    IReadOnlyList<DclStateRegistryRevisionCheckpoint> TargetRevisions,
    IReadOnlyList<DclStateInstanceCheckpoint> Instances);

internal static class DclStateRegistryCheckpointCodec
{
    public const int CurrentSchemaRevision = 1;

    public static string Serialize(DclStateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return JsonSerializer.Serialize(registry.CaptureCheckpoint(), CreateOptions());
    }

    public static DclStateRegistry Restore(
        string json,
        int battleGeneration,
        IReadOnlyDictionary<string, DclStateDefinition> definitions)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("A state-registry checkpoint is required.", nameof(json));
        if (battleGeneration <= 0)
            throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        ArgumentNullException.ThrowIfNull(definitions);
        DclStateRegistryCheckpoint checkpoint;
        try
        {
            checkpoint = JsonSerializer.Deserialize<DclStateRegistryCheckpoint>(json, CreateOptions())
                ?? throw new JsonException("State-registry checkpoint deserialized to null.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new InvalidOperationException("The state-registry checkpoint is not valid strict JSON.", exception);
        }
        if (checkpoint.SchemaRevision != CurrentSchemaRevision)
            throw new InvalidOperationException(
                $"Unsupported state-registry checkpoint schema {checkpoint.SchemaRevision}.");
        if (checkpoint.SavedBattleGeneration <= 0 || checkpoint.CurrentGlobalCt < 0 ||
            checkpoint.NextInstanceId <= 0 || checkpoint.TargetRevisions is null || checkpoint.Instances is null)
            throw new InvalidOperationException("State-registry checkpoint header is invalid.");

        var registry = new DclStateRegistry(battleGeneration, checkpoint.CurrentGlobalCt);
        registry.RestoreCheckpoint(checkpoint, definitions);
        return registry;
    }

    internal static DclStateRegistryCheckpoint Capture(DclStateRegistry registry)
    {
        DclStateInstanceCheckpoint[] instances = registry.Instances
            .OrderBy(instance => instance.InstanceId)
            .Select(CaptureInstance)
            .ToArray();
        DclStateRegistryRevisionCheckpoint[] revisions = registry.TargetRevisions
            .OrderBy(pair => pair.Key.UnitSlot)
            .ThenBy(pair => pair.Key.CharacterId)
            .Select(pair => new DclStateRegistryRevisionCheckpoint(
                pair.Key.UnitSlot,
                pair.Key.CharacterId,
                pair.Value))
            .ToArray();
        return new DclStateRegistryCheckpoint(
            CurrentSchemaRevision,
            registry.BattleGeneration,
            registry.CurrentGlobalCt,
            registry.NextInstanceId,
            revisions,
            instances);
    }

    internal static DclStateInstance RestoreInstance(
        DclStateInstanceCheckpoint saved,
        int battleGeneration,
        IReadOnlyDictionary<string, DclStateDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(saved);
        if (saved.InstanceId <= 0 || string.IsNullOrWhiteSpace(saved.StateKind) ||
            string.IsNullOrWhiteSpace(saved.DefinitionFingerprint) ||
            saved.TargetUnitSlot is < 0 or >= 64 || saved.TargetCharacterId < 0 ||
            saved.AppliedAtGlobalCt < 0 || saved.AppliedBeforeTurnSerial < 0 ||
            string.IsNullOrWhiteSpace(saved.PayloadKind) || string.IsNullOrWhiteSpace(saved.PayloadJson) ||
            string.IsNullOrWhiteSpace(saved.PresentationId))
            throw new InvalidOperationException($"State checkpoint instance {saved.InstanceId} is invalid.");
        bool hasSourceSlot = saved.SourceUnitSlot is not null;
        bool hasSourceCharacter = saved.SourceCharacterId is not null;
        if (hasSourceSlot != hasSourceCharacter || saved.SourceUnitSlot is < 0 or >= 64 || saved.SourceCharacterId < 0)
            throw new InvalidOperationException($"State checkpoint instance {saved.InstanceId} has a partial source identity.");
        if (!definitions.TryGetValue(saved.StateKind, out DclStateDefinition? definition) ||
            !StringComparer.Ordinal.Equals(saved.DefinitionFingerprint, Fingerprint(definition)))
            throw new InvalidOperationException(
                $"State checkpoint instance {saved.InstanceId} does not match loaded definition '{saved.StateKind}'.");
        var target = new DclUnitKey(battleGeneration, saved.TargetUnitSlot, saved.TargetCharacterId);
        DclUnitKey? source = hasSourceSlot
            ? new DclUnitKey(battleGeneration, saved.SourceUnitSlot!.Value, saved.SourceCharacterId!.Value)
            : null;
        DclStatePayload payload = RestorePayload(saved.PayloadKind, saved.PayloadJson, battleGeneration);
        if (!StringComparer.Ordinal.Equals(payload.SchemaId, definition.PayloadSchema) ||
            !StringComparer.Ordinal.Equals(saved.PresentationId, definition.PresentationProfile))
            throw new InvalidOperationException(
                $"State checkpoint instance {saved.InstanceId} lost its payload or presentation revision.");
        return new DclStateInstance(
            saved.InstanceId,
            definition,
            target,
            source,
            saved.AppliedAtGlobalCt,
            saved.AppliedBeforeTurnSerial,
            saved.ExpiresAtGlobalCt,
            saved.ExpiresAfterTargetTurnSerial,
            saved.ExpiresAfterSourceTurnSerial,
            saved.RemainingUses,
            saved.NextTickGlobalCt,
            saved.Strength,
            saved.WinningMargin,
            saved.StackDiscriminator ?? "",
            saved.ContributionIdentity,
            payload,
            saved.PresentationId);
    }

    private static DclStateInstanceCheckpoint CaptureInstance(DclStateInstance instance)
    {
        (string payloadKind, string payloadJson) = CapturePayload(instance.Payload);
        return new DclStateInstanceCheckpoint(
            instance.InstanceId,
            instance.Kind,
            Fingerprint(instance.Definition),
            instance.Target.UnitSlot,
            instance.Target.CharacterId,
            instance.Source?.UnitSlot,
            instance.Source?.CharacterId,
            instance.AppliedAtGlobalCt,
            instance.AppliedBeforeTurnSerial,
            instance.ExpiresAtGlobalCt,
            instance.ExpiresAfterTargetTurnSerial,
            instance.ExpiresAfterSourceTurnSerial,
            instance.RemainingUses,
            instance.NextTickGlobalCt,
            instance.Strength,
            instance.WinningMargin,
            instance.StackDiscriminator,
            instance.ContributionIdentity,
            payloadKind,
            payloadJson,
            instance.PresentationId);
    }

    private static (string Kind, string Json) CapturePayload(DclStatePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        object normalized = payload switch
        {
            DclPropertyStatePayload value => value with
            {
                Values = value.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            },
            DclProtectionStatePayload value => value with
            {
                Prepared = value.Prepared with
                {
                    EligibleDeliveryClasses = new SortedSet<DclDelivery>(value.Prepared.EligibleDeliveryClasses),
                },
            },
            DclReraiseStatePayload or DclOverwatchStatePayload or DclBulwarkStatePayload or
                DclAimStatePayload or DclShockStatePayload or DclTauntStatePayload or
                DclFearStatePayload or DclGuardBrokenStatePayload or DclWeaponBoundStatePayload or
                DclElementalExposureStatePayload => payload,
            _ => throw new InvalidOperationException(
                $"State payload type '{payload.GetType().Name}' has no checkpoint discriminator."),
        };
        string kind = normalized.GetType().Name;
        return (kind, JsonSerializer.Serialize(normalized, normalized.GetType(), CreateOptions()));
    }

    private static DclStatePayload RestorePayload(string kind, string json, int battleGeneration)
    {
        DclStatePayload payload = kind switch
        {
            nameof(DclPropertyStatePayload) => Deserialize<DclPropertyStatePayload>(json),
            nameof(DclReraiseStatePayload) => Deserialize<DclReraiseStatePayload>(json),
            nameof(DclOverwatchStatePayload) => Deserialize<DclOverwatchStatePayload>(json),
            nameof(DclProtectionStatePayload) => Deserialize<DclProtectionStatePayload>(json),
            nameof(DclBulwarkStatePayload) => Deserialize<DclBulwarkStatePayload>(json),
            nameof(DclAimStatePayload) => Deserialize<DclAimStatePayload>(json),
            nameof(DclShockStatePayload) => Deserialize<DclShockStatePayload>(json),
            nameof(DclTauntStatePayload) => Deserialize<DclTauntStatePayload>(json),
            nameof(DclFearStatePayload) => Deserialize<DclFearStatePayload>(json),
            nameof(DclGuardBrokenStatePayload) => Deserialize<DclGuardBrokenStatePayload>(json),
            nameof(DclWeaponBoundStatePayload) => Deserialize<DclWeaponBoundStatePayload>(json),
            nameof(DclElementalExposureStatePayload) => Deserialize<DclElementalExposureStatePayload>(json),
            _ => throw new InvalidOperationException($"Unknown state checkpoint payload discriminator '{kind}'."),
        };
        return RebasePayload(payload, battleGeneration);
    }

    private static T Deserialize<T>(string json) where T : DclStatePayload
        => JsonSerializer.Deserialize<T>(json, CreateOptions())
            ?? throw new InvalidOperationException($"Checkpoint payload '{typeof(T).Name}' deserialized to null.");

    private static DclStatePayload RebasePayload(DclStatePayload payload, int battleGeneration)
        => payload switch
        {
            DclAimStatePayload value => value with { Target = Rebase(value.Target, battleGeneration) },
            DclTauntStatePayload value => value with { Provocateur = Rebase(value.Provocateur, battleGeneration) },
            DclFearStatePayload value => value with { FearSource = Rebase(value.FearSource, battleGeneration) },
            DclProtectionStatePayload value => value with
            {
                Prepared = value.Prepared with
                {
                    ProtectedUnit = Rebase(value.Prepared.ProtectedUnit, battleGeneration),
                },
            },
            _ => payload,
        };

    private static DclUnitKey Rebase(DclUnitKey key, int battleGeneration)
    {
        if (!key.IsValid) throw new InvalidOperationException("Checkpoint payload contains an invalid UnitKey.");
        return new DclUnitKey(battleGeneration, key.UnitSlot, key.CharacterId);
    }

    private static string Fingerprint(DclStateDefinition definition)
    {
        DclStateDefinition normalized = definition with
        {
            CureFamilies = definition.CureFamilies.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        };
        return JsonSerializer.Serialize(normalized, CreateOptions());
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
            WriteIndented = false,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
