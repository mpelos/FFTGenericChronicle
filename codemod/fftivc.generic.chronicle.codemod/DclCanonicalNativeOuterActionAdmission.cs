namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeOuterSweepRequest(
    long SweepSerial,
    DclUnitKey Source,
    int ActionType,
    int AbilityId,
    int BattleState,
    int NativeRepeatCount,
    int NativeRepeatIndex,
    IReadOnlyList<DclUnitKey> Targets,
    DclUnitKey? SelectedUnit = null,
    DclBattleTile? SelectedTile = null,
    int? ActiveWeaponItemId = null,
    DclNativeWeaponHand? ActiveWeaponHand = null);

internal sealed record DclCanonicalNativeOuterSweepAdmission(
    long SweepSerial,
    long ActionInstanceId,
    DclUnitKey Source,
    int ActionType,
    int AbilityId,
    int StrikeIndex,
    bool StartsAction,
    bool CompletesNativeSweepSequence,
    IReadOnlyList<DclUnitKey> Targets,
    DclUnitKey? SelectedUnit,
    DclBattleTile SelectedTile,
    int? ActiveWeaponItemId,
    DclNativeWeaponHand? ActiveWeaponHand);

/// <summary>
/// Converts proven outer-sweep invocations into canonical ActionInstance ownership. One ordinary
/// sweep owns one ActionInstance. A native repeat sequence owns one ActionInstance across its
/// state-0x2A index-zero sweep and every following state-0x2F sweep. This class deliberately does
/// not infer identity from time, a reusable order-record pointer, or matching action fields.
/// </summary>
internal sealed class DclCanonicalNativeOuterActionAdmission
{
    private sealed record ActiveRepeat(
        long ActionInstanceId,
        DclUnitKey Source,
        int ActionType,
        int AbilityId,
        int RepeatCount,
        int NextIndex,
        DclUnitKey[] Targets,
        DclUnitKey? SelectedUnit,
        DclBattleTile SelectedTile);

    private readonly object _gate = new();
    private long _lastSweepSerial;
    private ActiveRepeat? _activeRepeat;

    public bool HasActiveRepeat
    {
        get { lock (_gate) return _activeRepeat is not null; }
    }

    public DclCanonicalNativeOuterSweepAdmission Admit(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        if (request.SweepSerial <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "A native outer sweep requires a positive serial.");
        if (!request.Source.IsValid || request.Source.BattleGeneration != battle.BattleGeneration ||
            request.ActionType is < 0 or > byte.MaxValue || request.AbilityId is < 0 or > ushort.MaxValue)
            throw new ArgumentException("Native outer-sweep identity does not belong to this battle.", nameof(request));
        if (request.Targets is null || request.Targets.Count == 0)
            throw new ArgumentException("Confirmed canonical execution requires the complete nonempty native target list.", nameof(request));
        if (request.SelectedTile is not { } selectedTile ||
            selectedTile.X < 0 || selectedTile.Y < 0 || selectedTile.Layer is < 0 or > 1)
            throw new ArgumentException("Confirmed canonical execution requires one valid native selected X/Y/map-level tuple.", nameof(request));

        DclUnitKey[] targets = request.Targets.ToArray();
        if (targets.Distinct().Count() != targets.Length || targets.Select(target => target.UnitSlot).Distinct().Count() != targets.Length)
            throw new ArgumentException("One native target batch cannot repeat a UnitKey or occupied unit slot.", nameof(request));
        RequireObserved(battle, request.Source, "source");
        if (request.SelectedUnit is { } selectedUnit)
            RequireObserved(battle, selectedUnit, "selected unit");
        foreach (DclUnitKey target in targets)
            RequireObserved(battle, target, "target");

        (DclAbilityBinding binding, DclActionProfile profile) = battle.Catalog.ResolveAbility(request.AbilityId);
        if (binding.CarrierKind == DclNativeCarrierKind.NativeSpecialPreserved)
            throw new InvalidOperationException("A preserved native special does not reserve canonical execution ownership.");
        if (binding.CarrierKind == DclNativeCarrierKind.RandomFireRepeat)
            throw new InvalidOperationException(
                "RandomFire cannot be admitted until one DCL-owned plan fixes the complete repeated target sequence before the first result.");

        lock (_gate)
        {
            if (request.SweepSerial <= _lastSweepSerial)
                throw new InvalidOperationException("Native outer-sweep serials must be strictly increasing within one battle generation.");

            bool nativeRepeatSequence = binding.CarrierKind == DclNativeCarrierKind.NativeRepeat &&
                request.NativeRepeatCount > 1;
            DclCanonicalNativeOuterSweepAdmission admitted = nativeRepeatSequence
                ? AdmitRepeat(battle, request, binding, profile, targets)
                : AdmitSingle(battle, request, binding, profile, targets);
            _lastSweepSerial = request.SweepSerial;
            return admitted;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _lastSweepSerial = 0;
            _activeRepeat = null;
        }
    }

    private DclCanonicalNativeOuterSweepAdmission AdmitSingle(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepRequest request,
        DclAbilityBinding binding,
        DclActionProfile profile,
        DclUnitKey[] targets)
    {
        if (_activeRepeat is not null)
            throw new InvalidOperationException("A new outer action cannot replace an unfinished native repeat sequence.");
        if (!DclAbilityBindingContract.SupportsEffectiveStrikeCount(binding, profile, 1) ||
            request.NativeRepeatIndex != 0 || request.NativeRepeatCount is < 0 or > 1 ||
            request.BattleState != DclCalcProvenance.ConfirmedExecutionBattleState)
            throw new InvalidOperationException(
                "A nonrepeat canonical action requires one Strike at native repeat index zero in execution state 0x2A.");
        long actionInstanceId = battle.NextActionInstanceId();
        return new DclCanonicalNativeOuterSweepAdmission(
            request.SweepSerial,
            actionInstanceId,
            request.Source,
            request.ActionType,
            request.AbilityId,
            StrikeIndex: 0,
            StartsAction: true,
            CompletesNativeSweepSequence: true,
            targets,
            request.SelectedUnit,
            request.SelectedTile!.Value,
            request.ActiveWeaponItemId,
            request.ActiveWeaponHand);
    }

    private DclCanonicalNativeOuterSweepAdmission AdmitRepeat(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepRequest request,
        DclAbilityBinding binding,
        DclActionProfile profile,
        DclUnitKey[] targets)
    {
        int strikeCount = request.NativeRepeatCount;
        if (strikeCount <= 1 ||
            !DclAbilityBindingContract.SupportsEffectiveStrikeCount(binding, profile, strikeCount) ||
            request.NativeRepeatIndex < 0 || request.NativeRepeatIndex >= strikeCount)
            throw new InvalidOperationException(
                "A native repeat must expose exactly the normalized StrikeCount and one in-range repeat index.");

        bool starts = request.NativeRepeatIndex == 0;
        if (starts)
        {
            if (_activeRepeat is not null)
                throw new InvalidOperationException("A second native repeat cannot start before the current sequence finishes.");
            if (request.BattleState != DclCalcProvenance.ConfirmedExecutionBattleState)
                throw new InvalidOperationException("Native repeat index zero must enter through confirmed execution state 0x2A.");
            long actionInstanceId = battle.NextActionInstanceId();
            _activeRepeat = new ActiveRepeat(
                actionInstanceId,
                request.Source,
                request.ActionType,
                request.AbilityId,
                strikeCount,
                NextIndex: 1,
                targets,
                request.SelectedUnit,
                request.SelectedTile!.Value);
            return new DclCanonicalNativeOuterSweepAdmission(
                request.SweepSerial,
                actionInstanceId,
                request.Source,
                request.ActionType,
                request.AbilityId,
                StrikeIndex: 0,
                StartsAction: true,
                CompletesNativeSweepSequence: false,
                targets,
                request.SelectedUnit,
                request.SelectedTile.Value,
                request.ActiveWeaponItemId,
                request.ActiveWeaponHand);
        }

        ActiveRepeat active = _activeRepeat ??
            throw new InvalidOperationException("A continuation sweep has no admitted index-zero outer action.");
        if (request.BattleState != DclCalcProvenance.NativeRepeatExecutionBattleState ||
            request.NativeRepeatIndex != active.NextIndex || request.NativeRepeatCount != active.RepeatCount ||
            request.Source != active.Source || request.ActionType != active.ActionType || request.AbilityId != active.AbilityId ||
            request.SelectedUnit != active.SelectedUnit ||
            request.SelectedTile != active.SelectedTile ||
            !targets.SequenceEqual(active.Targets))
            throw new InvalidOperationException(
                "Native repeat continuation diverged from the admitted source, action, target batch, count, or next index.");

        bool completes = request.NativeRepeatIndex == active.RepeatCount - 1;
        _activeRepeat = completes ? null : active with { NextIndex = active.NextIndex + 1 };
        return new DclCanonicalNativeOuterSweepAdmission(
            request.SweepSerial,
            active.ActionInstanceId,
            request.Source,
            request.ActionType,
            request.AbilityId,
            request.NativeRepeatIndex,
            StartsAction: false,
            CompletesNativeSweepSequence: completes,
            targets,
            request.SelectedUnit,
            request.SelectedTile!.Value,
            request.ActiveWeaponItemId,
            request.ActiveWeaponHand);
    }

    private static void RequireObserved(DclCanonicalBattleRuntime battle, DclUnitKey unit, string role)
    {
        if (!unit.IsValid || unit.BattleGeneration != battle.BattleGeneration ||
            !battle.TryGetObservedUnit(unit.UnitSlot, out DclUnitKey observed) || observed != unit)
            throw new ArgumentException($"Native outer-sweep {role} must be a current observed UnitKey.");
    }
}
