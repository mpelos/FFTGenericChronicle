namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeUnitSnapshotRequest(
    int UnitSlot,
    UnitSnapshot Native,
    DclCanonicalAttributeAdjustments NonEquipmentAttributes,
    DclSecondaryInputs NonEquipmentSecondary,
    int TileHeight,
    IReadOnlyList<string>? ParryResourceKeys = null,
    bool ExplicitlyEligible = false);

internal sealed record DclCanonicalNativeUnitSnapshotResult(
    DclCanonicalNativeUnitProjection Unit,
    DclCanonicalEquipmentSnapshot Equipment);

internal sealed record DclCanonicalNativeSnapshotBatch(
    DclUnitKey Source,
    IReadOnlyDictionary<DclUnitKey, DclCanonicalNativeUnitSnapshotResult> Units);

internal sealed record DclCanonicalNativeUnitPolicyInput(
    DclUnitKey Unit,
    DclCanonicalAttributeAdjustments NonEquipmentAttributes,
    DclSecondaryInputs NonEquipmentSecondary,
    int TileHeight,
    IReadOnlyList<string>? ParryResourceKeys = null,
    bool ExplicitlyEligible = false);

/// <summary>
/// Builds one immutable family-input snapshot batch from current native rows without inferring job
/// or state modifiers. Equipment, custom state, observed identity, and finite-defense resources are
/// always taken from the same battle/catalog revision.
/// </summary>
internal static class DclCanonicalNativeSnapshotBatchProjector
{
    public static DclCanonicalNativeSnapshotBatch ProjectCaptured(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action,
        IEnumerable<DclCanonicalNativeUnitPolicyInput> policyInputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(policyInputs);
        DclCanonicalNativeUnitPolicyInput[] policies = policyInputs.ToArray();
        if (policies.Select(policy => policy.Unit).Distinct().Count() != policies.Length ||
            !OrderKeys(policies.Select(policy => policy.Unit)).SequenceEqual(OrderKeys(action.NativeRows.Keys)))
            throw new ArgumentException(
                "Captured snapshot projection requires exactly one explicit policy input per frozen native row.",
                nameof(policyInputs));
        return Project(
            battle,
            action.Source,
            policies.Select(policy => new DclCanonicalNativeUnitSnapshotRequest(
                policy.Unit.UnitSlot,
                action.NativeRows[policy.Unit],
                policy.NonEquipmentAttributes,
                policy.NonEquipmentSecondary,
                policy.TileHeight,
                policy.ParryResourceKeys,
                policy.ExplicitlyEligible)));
    }

    private static IOrderedEnumerable<DclUnitKey> OrderKeys(IEnumerable<DclUnitKey> units)
        => units.OrderBy(unit => unit.BattleGeneration)
            .ThenBy(unit => unit.UnitSlot)
            .ThenBy(unit => unit.CharacterId);

    public static DclCanonicalNativeSnapshotBatch Project(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        IEnumerable<DclCanonicalNativeUnitSnapshotRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(requests);
        if (!battle.TryGetObservedUnit(source.UnitSlot, out DclUnitKey observedSource) || observedSource != source)
            throw new ArgumentException("Snapshot-batch source must be a current observed UnitKey.", nameof(source));

        DclCanonicalNativeUnitSnapshotRequest[] rows = requests.ToArray();
        if (rows.Length == 0 || rows.Select(row => row.UnitSlot).Distinct().Count() != rows.Length)
            throw new ArgumentException("A native snapshot batch requires unique unit slots.", nameof(requests));
        DclCanonicalNativeUnitSnapshotRequest sourceRow = rows.SingleOrDefault(row => row.UnitSlot == source.UnitSlot)
            ?? throw new ArgumentException("A native snapshot batch must contain its source row.", nameof(requests));
        if (sourceRow.Native.CharId != source.CharacterId)
            throw new ArgumentException("Snapshot-batch source row does not match the observed source identity.", nameof(requests));

        var projected = new Dictionary<DclUnitKey, DclCanonicalNativeUnitSnapshotResult>();
        foreach (DclCanonicalNativeUnitSnapshotRequest row in rows.OrderBy(row => row.UnitSlot))
        {
            if (!battle.TryGetObservedUnit(row.UnitSlot, out DclUnitKey unit) ||
                row.Native.CharId != unit.CharacterId)
                throw new ArgumentException(
                    $"Native snapshot slot {row.UnitSlot} does not match the current observed identity.",
                    nameof(requests));
            if (row.TileHeight < 0)
                throw new ArgumentOutOfRangeException(nameof(requests), "Native tile height cannot be negative.");

            DclCanonicalEquipmentSnapshot equipment =
                DclCanonicalEquipmentProjector.Project(row.Native, battle.Catalog.Items);
            DclDefenseResourceSnapshot defense = battle.CaptureDefenseResources(
                unit,
                row.ParryResourceKeys ?? []);
            DclCanonicalNativeUnitProjectionInput input = DclCanonicalNativeUnitProjectionInput.Compose(
                row.NonEquipmentAttributes,
                row.NonEquipmentSecondary,
                equipment,
                row.TileHeight,
                battle.States,
                defense,
                row.ExplicitlyEligible);
            DclCanonicalNativeUnitProjection projection = DclCanonicalNativeSnapshotAdapter.ProjectUnit(
                row.Native,
                battle.BattleGeneration,
                row.UnitSlot,
                source,
                sourceRow.Native.Team,
                input);
            if (!projected.TryAdd(unit, new DclCanonicalNativeUnitSnapshotResult(projection, equipment)))
                throw new InvalidOperationException("A snapshot batch produced duplicate UnitKeys.");
        }
        return new DclCanonicalNativeSnapshotBatch(source, projected);
    }
}
