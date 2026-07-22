using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalTimelineUnitCheckpoint(
    int UnitSlot,
    int CharacterId,
    int InitiativeRank,
    string CtNumerator,
    string CtDenominator,
    DclCtRate Rate,
    long TurnSerial);

internal sealed record DclCanonicalChargedActionCheckpoint(
    int AbilityId,
    long ActionInstanceId,
    int SourceUnitSlot,
    int SourceCharacterId,
    string ActionId,
    int ProfileRevision,
    DclTargetMode TargetMode,
    int? TargetUnitSlot,
    int? TargetCharacterId,
    DclBattleTile? FixedTile,
    DclBattleTile DeclarationTile,
    bool PassedRangeCheck,
    bool PassedVerticalCheck,
    int FinalMpCost,
    int ApprovedHpCap,
    int CastCt,
    long DeclaredAtGlobalCt,
    long ResolvesAtGlobalCt);

internal sealed record DclCanonicalWeaponRuntimeCheckpoint(
    int UnitSlot,
    int CharacterId,
    string ResourceKey,
    DclWeaponBalance Balance,
    DclWeaponReadinessProperty ReadinessProperty,
    bool Ready,
    bool ParrySuppressedAfterAttack);

internal sealed record DclCanonicalDefenseRuntimeCheckpoint(
    int UnitSlot,
    int CharacterId,
    long Revision,
    bool BlockAvailable,
    IReadOnlyDictionary<string, int> ParryAttemptCounts);

internal sealed record DclCanonicalTimelineCheckpoint(
    int SchemaRevision,
    int SavedBattleGeneration,
    long CurrentGlobalCt,
    long NextActionInstanceId,
    string? QuickLockStateKind,
    DclStateRegistryCheckpoint StateRegistry,
    IReadOnlyList<DclCanonicalTimelineUnitCheckpoint> Units,
    IReadOnlyList<DclCanonicalWeaponRuntimeCheckpoint> Weapons,
    IReadOnlyList<DclCanonicalDefenseRuntimeCheckpoint> DefenseResources,
    IReadOnlyList<DclCanonicalChargedActionCheckpoint> ChargedActions);

internal sealed record DclCanonicalTimelineRestoreResult(
    DclCanonicalBattleRuntime Battle,
    DclCanonicalTimelineScheduler Timeline,
    DclQuickLockController? QuickLocks);

internal static class DclCanonicalTimelineCheckpointCodec
{
    public const int CurrentSchemaRevision = 5;

    public static string Serialize(DclCanonicalTimelineScheduler timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        timeline.RequireCheckpointBoundary();
        if (timeline.QuickLockStateKind is { } quickLockKind)
        {
            HashSet<DclUnitKey> stateLocks = timeline.Battle.States.Instances
                .Where(instance => StringComparer.Ordinal.Equals(instance.Kind, quickLockKind))
                .Select(instance => instance.Target)
                .ToHashSet();
            if (timeline.QuickLocks is null || !stateLocks.SetEquals(timeline.QuickLocks.LockedUnits))
                throw new InvalidOperationException(
                    "Timeline checkpoint requires exact agreement between QuickLock controller and persistent states.");
        }
        DclCanonicalTimelineUnitCheckpoint[] units = timeline.TimelineUnits.Select(unit =>
            new DclCanonicalTimelineUnitCheckpoint(
                unit.Unit.UnitSlot,
                unit.Unit.CharacterId,
                unit.InitiativeRank,
                unit.Clock.CurrentCt.Numerator.ToString(),
                unit.Clock.CurrentCt.Denominator.ToString(),
                unit.Rate,
                timeline.Battle.CurrentTurnSerial(unit.Unit))).ToArray();
        DclCanonicalChargedActionCheckpoint[] charged = timeline.ChargedActions
            .Select(CaptureChargedAction)
            .ToArray();
        DclCanonicalWeaponRuntimeCheckpoint[] weapons = timeline.Battle.CaptureWeaponStates()
            .Select(snapshot => new DclCanonicalWeaponRuntimeCheckpoint(
                snapshot.Unit.UnitSlot,
                snapshot.Unit.CharacterId,
                snapshot.ResourceKey,
                snapshot.Balance,
                snapshot.ReadinessProperty,
                snapshot.Ready,
                snapshot.ParrySuppressedAfterAttack))
            .ToArray();
        DclCanonicalDefenseRuntimeCheckpoint[] defenseResources = timeline.Battle.CaptureDefenseStates()
            .Select(snapshot => new DclCanonicalDefenseRuntimeCheckpoint(
                snapshot.Unit.UnitSlot,
                snapshot.Unit.CharacterId,
                snapshot.Resources.Revision,
                snapshot.Resources.BlockAvailable,
                snapshot.Resources.ParryAttemptCounts))
            .ToArray();
        var checkpoint = new DclCanonicalTimelineCheckpoint(
            CurrentSchemaRevision,
            timeline.Battle.BattleGeneration,
            timeline.CurrentGlobalCt,
            timeline.Battle.ActionInstances.NextId,
            timeline.QuickLockStateKind,
            timeline.Battle.States.CaptureCheckpoint(),
            units,
            weapons,
            defenseResources,
            charged);
        return JsonSerializer.Serialize(checkpoint, CreateOptions());
    }

    public static DclCanonicalTimelineRestoreResult Restore(
        string json,
        DclCanonicalRuntimeCatalog catalog,
        int battleGeneration,
        IDclUniformRandomSource? executionRandomSource = null)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("A timeline checkpoint is required.", nameof(json));
        ArgumentNullException.ThrowIfNull(catalog);
        if (battleGeneration <= 0) throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        DclCanonicalTimelineCheckpoint checkpoint;
        try
        {
            checkpoint = JsonSerializer.Deserialize<DclCanonicalTimelineCheckpoint>(json, CreateOptions()) ??
                throw new JsonException("Timeline checkpoint deserialized to null.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new InvalidOperationException("The timeline checkpoint is not valid strict JSON.", exception);
        }
        ValidateHeader(checkpoint);
        var battle = new DclCanonicalBattleRuntime(
            catalog,
            battleGeneration,
            checkpoint.CurrentGlobalCt,
            executionRandomSource);
        var units = new List<DclCanonicalTimelineUnit>(checkpoint.Units.Count);
        var serials = new Dictionary<DclUnitKey, long>();
        foreach (DclCanonicalTimelineUnitCheckpoint saved in checkpoint.Units)
        {
            DclUnitKey unit = RestoreUnitKey(saved.UnitSlot, saved.CharacterId, battleGeneration, "timeline unit");
            if (!BigInteger.TryParse(saved.CtNumerator, out BigInteger numerator) ||
                !BigInteger.TryParse(saved.CtDenominator, out BigInteger denominator) || denominator <= 0)
                throw new InvalidOperationException("Timeline checkpoint contains an invalid exact CT value.");
            var clock = DclCtState.Restore(new DclRational(numerator, denominator));
            battle.ObserveUnit(unit);
            units.Add(new DclCanonicalTimelineUnit(unit, saved.InitiativeRank, clock, saved.Rate));
            if (!serials.TryAdd(unit, saved.TurnSerial))
                throw new InvalidOperationException("Timeline checkpoint duplicates a unit identity.");
        }
        battle.States.RestoreCheckpoint(checkpoint.StateRegistry, catalog.Authoring.States);
        foreach (DclStateInstance prepared in battle.States.Instances.Where(instance =>
                     instance.Payload is DclOverwatchStatePayload or DclProtectionStatePayload or DclBulwarkStatePayload))
            battle.PreparedStates.Attach(prepared.InstanceId);
        long[] preparedActionReservations = battle.States.Instances
            .SelectMany(instance => instance.Payload is DclOverwatchStatePayload payload
                ? payload.ReservedActionInstanceIds ?? []
                : [])
            .ToArray();
        if (preparedActionReservations.Distinct().Count() != preparedActionReservations.Length ||
            preparedActionReservations.Any(id => id <= 0 || id >= checkpoint.NextActionInstanceId) ||
            checkpoint.ChargedActions.Any(charged => preparedActionReservations.Contains(charged.ActionInstanceId)))
            throw new InvalidOperationException(
                "Timeline checkpoint prepared Action reservations are duplicated or outside the saved ActionInstance cursor.");
        battle.RestoreTurnSerials(serials);
        battle.RestoreWeaponStates(checkpoint.Weapons.Select(saved =>
            new DclCanonicalWeaponRuntimeSnapshot(
                RestoreUnitKey(saved.UnitSlot, saved.CharacterId, battleGeneration, "weapon owner"),
                saved.ResourceKey,
                saved.Balance,
                saved.ReadinessProperty,
                saved.Ready,
                saved.ParrySuppressedAfterAttack)));
        battle.RestoreDefenseStates(checkpoint.DefenseResources.Select(saved =>
            new DclCanonicalDefenseRuntimeSnapshot(
                RestoreUnitKey(saved.UnitSlot, saved.CharacterId, battleGeneration, "defense-resource owner"),
                new DclDefenseResourceSnapshot(
                    saved.ParryAttemptCounts,
                    saved.BlockAvailable,
                    saved.Revision))));
        battle.ActionInstances.RestoreNext(checkpoint.NextActionInstanceId);

        DclQuickLockController? quickLocks = null;
        if (checkpoint.QuickLockStateKind is { } quickLockKind)
        {
            if (!catalog.Authoring.States.ContainsKey(quickLockKind))
                throw new InvalidOperationException("Timeline checkpoint QuickLock definition is absent from loaded authoring.");
            quickLocks = new DclQuickLockController();
            IGrouping<DclUnitKey, DclStateInstance>[] savedLocks = battle.States.Instances
                .Where(instance => StringComparer.Ordinal.Equals(instance.Kind, quickLockKind))
                .GroupBy(instance => instance.Target)
                .ToArray();
            if (savedLocks.Any(group => group.Count() != 1 || units.All(unit => unit.Unit != group.Key)))
                throw new InvalidOperationException(
                    "Timeline checkpoint QuickLock must have exactly one persistent state on a registered timeline unit.");
            foreach (DclUnitKey locked in savedLocks.Select(group => group.Key))
                quickLocks.RestoreLocked(locked);
        }
        var timeline = new DclCanonicalTimelineScheduler(
            battle,
            units,
            quickLocks,
            checkpoint.QuickLockStateKind);
        foreach (DclCanonicalChargedActionCheckpoint saved in checkpoint.ChargedActions
                     .OrderBy(entry => entry.ActionInstanceId))
            timeline.RestoreChargedAction(RestoreChargedAction(saved, battleGeneration));
        if (checkpoint.ChargedActions.Any(entry => entry.ActionInstanceId >= checkpoint.NextActionInstanceId))
            throw new InvalidOperationException("Timeline checkpoint ActionInstance sequence does not follow every pending charged action.");
        return new DclCanonicalTimelineRestoreResult(battle, timeline, quickLocks);
    }

    private static DclCanonicalChargedActionCheckpoint CaptureChargedAction(DclCanonicalScheduledCast charged)
    {
        DclActionDeclaration declaration = charged.Declaration;
        return new DclCanonicalChargedActionCheckpoint(
            charged.AbilityId,
            declaration.ActionInstanceId,
            declaration.Source.UnitSlot,
            declaration.Source.CharacterId,
            declaration.ActionId,
            declaration.ProfileRevision,
            declaration.TargetMode,
            declaration.TrackedTarget?.UnitSlot,
            declaration.TrackedTarget?.CharacterId,
            declaration.FixedTile,
            declaration.DeclarationTile,
            declaration.PassedRangeCheck,
            declaration.PassedVerticalCheck,
            declaration.FinalMpCost,
            declaration.ApprovedHpCap,
            declaration.CastCt,
            declaration.DeclaredAtGlobalCt,
            declaration.ResolvesAtGlobalCt);
    }

    private static DclCanonicalScheduledCast RestoreChargedAction(
        DclCanonicalChargedActionCheckpoint saved,
        int battleGeneration)
    {
        DclUnitKey source = RestoreUnitKey(
            saved.SourceUnitSlot, saved.SourceCharacterId, battleGeneration, "charged source");
        bool hasTargetSlot = saved.TargetUnitSlot is not null;
        bool hasTargetCharacter = saved.TargetCharacterId is not null;
        if (hasTargetSlot != hasTargetCharacter)
            throw new InvalidOperationException("Timeline checkpoint charged target identity is partial.");
        DclUnitKey? target = hasTargetSlot
            ? RestoreUnitKey(saved.TargetUnitSlot!.Value, saved.TargetCharacterId!.Value, battleGeneration, "charged target")
            : null;
        var declaration = new DclActionDeclaration(
            saved.ActionInstanceId,
            source,
            saved.ActionId,
            saved.ProfileRevision,
            saved.TargetMode,
            target,
            saved.FixedTile,
            saved.DeclarationTile,
            saved.PassedRangeCheck,
            saved.PassedVerticalCheck,
            saved.FinalMpCost,
            saved.ApprovedHpCap,
            saved.CastCt,
            saved.DeclaredAtGlobalCt,
            saved.ResolvesAtGlobalCt);
        return new DclCanonicalScheduledCast(saved.AbilityId, declaration);
    }

    private static DclUnitKey RestoreUnitKey(int slot, int characterId, int battleGeneration, string owner)
    {
        var unit = new DclUnitKey(battleGeneration, slot, characterId);
        if (!unit.IsValid) throw new InvalidOperationException($"Timeline checkpoint {owner} identity is invalid.");
        return unit;
    }

    private static void ValidateHeader(DclCanonicalTimelineCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (checkpoint.SchemaRevision != CurrentSchemaRevision)
            throw new InvalidOperationException($"Unsupported timeline checkpoint schema {checkpoint.SchemaRevision}.");
        if (checkpoint.SavedBattleGeneration <= 0 || checkpoint.CurrentGlobalCt < 0 ||
            checkpoint.NextActionInstanceId <= 0 || checkpoint.StateRegistry is null ||
            checkpoint.Units is null || checkpoint.Weapons is null || checkpoint.DefenseResources is null ||
            checkpoint.ChargedActions is null ||
            checkpoint.StateRegistry.CurrentGlobalCt != checkpoint.CurrentGlobalCt ||
            checkpoint.StateRegistry.SavedBattleGeneration != checkpoint.SavedBattleGeneration)
            throw new InvalidOperationException("Timeline checkpoint header is invalid or its clocks disagree.");
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
