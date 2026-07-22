namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Retains complete native-admitted outer actions until a production policy provider can compose
/// their confirmed-execution and forecast/AI requests. The ledger stores only complete captured
/// actions; partial NativeRepeat sweeps remain inside the capture state machine.
/// </summary>
internal enum DclCanonicalNativeAdmittedActionPublishStatus
{
    Published,
    DuplicateAdmittedAction,
    ActionAlreadyPublished,
}

internal sealed record DclCanonicalNativeAdmittedActionPublishResult(
    DclCanonicalNativeAdmittedActionPublishStatus Status,
    DclCanonicalNativeAdmittedAction Action);

internal sealed class DclCanonicalNativeAdmittedActionLedger
{
    private readonly object _gate = new();
    private readonly int _battleGeneration;
    private readonly Dictionary<long, DclCanonicalNativeAdmittedAction> _actions = [];

    public DclCanonicalNativeAdmittedActionLedger(int battleGeneration)
    {
        if (battleGeneration <= 0) throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        _battleGeneration = battleGeneration;
    }

    public int Count
    {
        get { lock (_gate) return _actions.Count; }
    }

    public void Publish(DclCanonicalBattleRuntime battle, DclCanonicalNativeAdmittedAction action)
    {
        DclCanonicalNativeAdmittedActionPublishResult result = PublishCore(
            battle,
            action,
            duplicateActionIsNoWrite: false);
        if (result.Status != DclCanonicalNativeAdmittedActionPublishStatus.Published)
            throw new InvalidOperationException($"Native admitted action was not published: {result.Status}.");
    }

    public DclCanonicalNativeAdmittedActionPublishResult TryPublishForPolicyTicket(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action)
        => PublishCore(
            battle,
            action,
            duplicateActionIsNoWrite: true);

    private DclCanonicalNativeAdmittedActionPublishResult PublishCore(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action,
        bool duplicateActionIsNoWrite)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(action);
        lock (_gate)
        {
            if (_actions.ContainsKey(action.ActionInstanceId) && duplicateActionIsNoWrite)
                return new DclCanonicalNativeAdmittedActionPublishResult(
                    DclCanonicalNativeAdmittedActionPublishStatus.DuplicateAdmittedAction,
                    _actions[action.ActionInstanceId]);
        }
        if (battle.BattleGeneration != _battleGeneration ||
            action.Source.BattleGeneration != _battleGeneration ||
            action.Admissions.Count == 0 ||
            action.Admissions.Any(admission =>
                admission.ActionInstanceId != action.ActionInstanceId ||
                admission.Source != action.Source ||
                admission.AbilityId != action.AbilityId ||
                admission.Source.BattleGeneration != _battleGeneration ||
                admission.Targets.Any(target => target.BattleGeneration != _battleGeneration)) ||
            action.NativeRows.Keys.Any(unit => unit.BattleGeneration != _battleGeneration))
            throw new ArgumentException("A retained native admission must belong wholly to this battle generation.", nameof(action));
        battle.RequireObserved(action.Source);
        foreach (DclUnitKey target in action.Targets)
            battle.RequireObserved(target);
        if (action.SelectedUnit is { } selectedUnit)
            battle.RequireObserved(selectedUnit);
        foreach ((DclUnitKey unit, UnitSnapshot row) in action.NativeRows)
        {
            battle.RequireObserved(unit);
            if (row.CharId != unit.CharacterId || row.Raw is null || row.Raw.Length != 0x200)
                throw new ArgumentException("A retained native admission row must be a complete frozen unit snapshot.", nameof(action));
        }

        lock (_gate)
        {
            if (_actions.ContainsKey(action.ActionInstanceId))
                throw new InvalidOperationException("A complete native admission cannot be published twice.");
            _actions.Add(action.ActionInstanceId, action);
        }
        return new DclCanonicalNativeAdmittedActionPublishResult(
            DclCanonicalNativeAdmittedActionPublishStatus.Published,
            action);
    }

    public bool TryGet(long actionInstanceId, out DclCanonicalNativeAdmittedAction action)
    {
        lock (_gate)
            return _actions.TryGetValue(actionInstanceId, out action!);
    }

    public DclCanonicalNativeAdmittedAction Get(long actionInstanceId)
    {
        lock (_gate)
            return _actions.TryGetValue(actionInstanceId, out DclCanonicalNativeAdmittedAction? action)
                ? action
                : throw new KeyNotFoundException("The native admitted action is not retained.");
    }

    public DclCanonicalNativeAdmittedAction Retire(long actionInstanceId)
    {
        lock (_gate)
        {
            DclCanonicalNativeAdmittedAction action = Get(actionInstanceId);
            _actions.Remove(actionInstanceId);
            return action;
        }
    }

    public void Clear()
    {
        lock (_gate)
            _actions.Clear();
    }
}
