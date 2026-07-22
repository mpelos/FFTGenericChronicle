namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeAdmittedAction(
    long ActionInstanceId,
    DclUnitKey Source,
    int ActionType,
    int AbilityId,
    IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> Admissions,
    IReadOnlyDictionary<DclUnitKey, UnitSnapshot> NativeRows)
{
    public IReadOnlyList<DclUnitKey> Targets => Admissions[0].Targets;
    public DclUnitKey? SelectedUnit => Admissions[0].SelectedUnit;
    public DclBattleTile SelectedTile => Admissions[0].SelectedTile;
    public IReadOnlyList<int?> ActiveWeaponItemIds => Admissions.Select(admission => admission.ActiveWeaponItemId).ToArray();
    public IReadOnlyList<DclNativeWeaponHand?> ActiveWeaponHands => Admissions.Select(admission => admission.ActiveWeaponHand).ToArray();
}

/// <summary>
/// Converts the synchronous native sweep stream into complete outer actions. Admission remains in
/// the battle-owned ledger; this collector only retains an exact NativeRepeat sequence until its
/// terminal sweep so family request composition can never see a partial multi-Strike action.
/// </summary>
internal sealed class DclCanonicalNativeOuterActionCapture
{
    private sealed record PendingAction(
        List<DclCanonicalNativeOuterSweepAdmission> Admissions,
        IReadOnlyDictionary<DclUnitKey, UnitSnapshot> NativeRows);

    private readonly object _gate = new();
    private PendingAction? _pending;

    public bool HasPendingAction
    {
        get { lock (_gate) return _pending is not null; }
    }

    public DclCanonicalNativeAdmittedAction? Capture(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepRequest request,
        IReadOnlyDictionary<DclUnitKey, UnitSnapshot> nativeRows)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(nativeRows);
        lock (_gate)
        {
            ValidateRows(battle, request, nativeRows);
            DclCanonicalNativeOuterSweepAdmission admission = battle.NativeAdmissions.Admit(battle, request);
            if (admission.StartsAction)
            {
                if (_pending is not null)
                    throw new InvalidOperationException("A captured native action cannot replace an incomplete sequence.");
                IReadOnlyDictionary<DclUnitKey, UnitSnapshot> frozenRows = FreezeRows(nativeRows);
                if (admission.CompletesNativeSweepSequence)
                    return Complete([admission], frozenRows);
                _pending = new PendingAction([admission], frozenRows);
                return null;
            }

            PendingAction pending = _pending ??
                throw new InvalidOperationException("A native continuation has no captured index-zero sweep.");
            DclCanonicalNativeOuterSweepAdmission first = pending.Admissions[0];
            if (admission.ActionInstanceId != first.ActionInstanceId ||
                admission.Source != first.Source || admission.ActionType != first.ActionType ||
                admission.AbilityId != first.AbilityId || admission.StrikeIndex != pending.Admissions.Count ||
                admission.SelectedUnit != first.SelectedUnit ||
                admission.SelectedTile != first.SelectedTile ||
                !OrderKeys(nativeRows.Keys).SequenceEqual(OrderKeys(pending.NativeRows.Keys)))
                throw new InvalidOperationException("The captured native continuation diverged from its outer action.");
            pending.Admissions.Add(admission);
            if (!admission.CompletesNativeSweepSequence)
                return null;

            DclCanonicalNativeAdmittedAction completed = Complete(pending.Admissions, pending.NativeRows);
            _pending = null;
            return completed;
        }
    }

    public void Clear()
    {
        lock (_gate) _pending = null;
    }

    private static DclCanonicalNativeAdmittedAction Complete(
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        IReadOnlyDictionary<DclUnitKey, UnitSnapshot> nativeRows)
    {
        DclCanonicalNativeOuterSweepAdmission first = admissions[0];
        if (!first.StartsAction || !admissions[^1].CompletesNativeSweepSequence ||
            admissions.Select(admission => admission.ActionInstanceId).Distinct().Count() != 1 ||
            admissions.Select(admission => admission.StrikeIndex).SequenceEqual(Enumerable.Range(0, admissions.Count)) is false)
            throw new InvalidOperationException("Only one complete contiguous admitted outer action can be published.");
        return new DclCanonicalNativeAdmittedAction(
            first.ActionInstanceId,
            first.Source,
            first.ActionType,
            first.AbilityId,
            admissions.ToArray(),
            nativeRows);
    }

    private static void ValidateRows(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepRequest request,
        IReadOnlyDictionary<DclUnitKey, UnitSnapshot> nativeRows)
    {
        IEnumerable<DclUnitKey> requiredUnits = request.Targets.Append(request.Source);
        if (request.SelectedUnit is { } selectedUnit)
            requiredUnits = requiredUnits.Append(selectedUnit);
        DclUnitKey[] required = OrderKeys(requiredUnits.Distinct()).ToArray();
        DclUnitKey[] actual = OrderKeys(nativeRows.Keys).ToArray();
        if (required.Any(unit => !actual.Contains(unit)))
            throw new ArgumentException(
                "Native row capture must contain at least the source, admitted target, and selected-unit identities.",
                nameof(nativeRows));
        foreach ((DclUnitKey unit, UnitSnapshot row) in nativeRows)
        {
            if (!battle.TryGetObservedUnit(unit.UnitSlot, out DclUnitKey observed) || observed != unit ||
                row.CharId != unit.CharacterId || row.Raw is null || row.Raw.Length != 0x200)
                throw new ArgumentException("Every captured native row must match one current observed UnitKey and complete unit record.", nameof(nativeRows));
        }
    }

    private static IReadOnlyDictionary<DclUnitKey, UnitSnapshot> FreezeRows(
        IReadOnlyDictionary<DclUnitKey, UnitSnapshot> nativeRows)
        => nativeRows.ToDictionary(
            pair => pair.Key,
            pair => pair.Value with { Raw = (byte[])pair.Value.Raw.Clone() });

    private static IOrderedEnumerable<DclUnitKey> OrderKeys(IEnumerable<DclUnitKey> units)
        => units.OrderBy(unit => unit.BattleGeneration)
            .ThenBy(unit => unit.UnitSlot)
            .ThenBy(unit => unit.CharacterId);
}
