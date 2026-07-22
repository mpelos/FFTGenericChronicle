namespace fftivc.generic.chronicle.codemod;

internal sealed record DclAttributeInputs(
    int RawPa,
    int RawSpeed,
    int RawMa,
    int CurrentBrave,
    int JobStAdjustment = 0,
    int EquipmentStAdjustment = 0,
    int StateStAdjustment = 0,
    int JobDxAdjustment = 0,
    int EquipmentDxAdjustment = 0,
    int StateDxAdjustment = 0,
    int JobIqAdjustment = 0,
    int EquipmentIqAdjustment = 0,
    int StateIqAdjustment = 0);

internal readonly record struct DclPrimaryCharacteristics(int St, int Dx, int Iq, int Ht);

internal sealed record DclSecondaryInputs(
    int CharacterHpModifier = 0,
    int JobHpModifier = 0,
    int EquipmentStatusHpModifier = 0,
    int CharacterMpModifier = 0,
    int JobMpModifier = 0,
    int EquipmentStatusMpModifier = 0,
    int WillModifier = 0,
    DclRational? JobBasicSpeedAdjustment = null,
    DclRational? BasicSpeedModifier = null,
    int JobMoveAdjustment = 0,
    int MoveModifier = 0,
    int JobJumpAdjustment = 0,
    int EquipmentStatusJumpModifier = 0);

internal readonly record struct DclSecondaryCharacteristics(
    int BaseMaxHp,
    int MaxHp,
    int BaseMaxMp,
    int MaxMp,
    int Will,
    DclRational BasicLift,
    DclRational BasicSpeed,
    int BasicMove,
    int BaseJump,
    int BaseDodge);

internal readonly record struct DclPoolReconciliation(int CurrentHp, int CurrentMp);

internal static class DclCharacteristics
{
    private static readonly DclRational OneQuarter = new(1, 4);
    private static readonly DclRational OneFifth = new(1, 5);

    public static DclPrimaryCharacteristics ResolvePrimary(DclAttributeInputs input)
    {
        ArgumentNullException.ThrowIfNull(input);
        int st = Math.Max(1, checked(input.RawPa + input.JobStAdjustment + input.EquipmentStAdjustment + input.StateStAdjustment));
        int dx = Math.Max(1, checked(input.RawSpeed + input.JobDxAdjustment + input.EquipmentDxAdjustment + input.StateDxAdjustment));
        int iq = Math.Max(1, checked(input.RawMa + input.JobIqAdjustment + input.EquipmentIqAdjustment + input.StateIqAdjustment));
        int ht = BraveToHt(input.CurrentBrave);
        return new DclPrimaryCharacteristics(st, dx, iq, ht);
    }

    public static int BraveToHt(int currentBrave)
    {
        DclRational offset = new(checked(currentBrave - 50), 8);
        int ht = checked(10 + (int)offset.RoundNearest());
        return Math.Max(4, ht);
    }

    public static DclSecondaryCharacteristics ResolveSecondary(
        DclPrimaryCharacteristics primary,
        DclSecondaryInputs input)
    {
        ArgumentNullException.ThrowIfNull(input);
        int baseMaxHp = checked(primary.St + input.CharacterHpModifier + input.JobHpModifier);
        int maxHp = Math.Max(1, checked(baseMaxHp + input.EquipmentStatusHpModifier));
        int baseMaxMp = checked(Math.Max(primary.Ht, primary.Iq) + input.CharacterMpModifier + input.JobMpModifier);
        int maxMp = Math.Max(1, checked(baseMaxMp + input.EquipmentStatusMpModifier));
        int will = checked(primary.Iq + input.WillModifier);
        DclRational basicLift = DclRational.FromInteger(primary.St) * DclRational.FromInteger(primary.St) * OneFifth;
        DclRational basicSpeed = DclRational.FromInteger(checked(primary.Dx + primary.Ht)) * OneQuarter +
            (input.JobBasicSpeedAdjustment ?? DclRational.FromInteger(0)) +
            (input.BasicSpeedModifier ?? DclRational.FromInteger(0));
        int basicMove = checked((int)basicSpeed.Floor() + input.JobMoveAdjustment + input.MoveModifier);
        int baseJump = Math.Max(1, checked(3 + input.JobJumpAdjustment + input.EquipmentStatusJumpModifier));
        int baseDodge = checked((int)basicSpeed.Floor() + 3);
        return new DclSecondaryCharacteristics(
            baseMaxHp,
            maxHp,
            baseMaxMp,
            maxMp,
            will,
            basicLift,
            basicSpeed,
            basicMove,
            baseJump,
            baseDodge);
    }

    public static DclPoolReconciliation ReconcilePools(int oldCurrentHp, int oldCurrentMp, int newMaxHp, int newMaxMp)
    {
        if (oldCurrentHp < 0 || oldCurrentMp < 0) throw new ArgumentOutOfRangeException(nameof(oldCurrentHp));
        if (newMaxHp < 1 || newMaxMp < 1) throw new ArgumentOutOfRangeException(nameof(newMaxHp));
        return new DclPoolReconciliation(Math.Min(oldCurrentHp, newMaxHp), Math.Min(oldCurrentMp, newMaxMp));
    }
}

internal sealed record DclInitiativeCandidate(DclUnitKey Unit, DclRational BasicSpeed, int Dx);

internal readonly record struct DclInitiativeEntry(DclUnitKey Unit, int Rank, int InitialCt);

internal static class DclInitiative
{
    public static IReadOnlyList<DclInitiativeEntry> Build(IEnumerable<DclInitiativeCandidate> eligibleUnits)
    {
        ArgumentNullException.ThrowIfNull(eligibleUnits);
        DclInitiativeCandidate[] ordered = eligibleUnits
            .OrderByDescending(candidate => candidate.BasicSpeed)
            .ThenByDescending(candidate => candidate.Dx)
            .ThenBy(candidate => candidate.Unit.UnitSlot)
            .ToArray();
        if (ordered.Any(candidate => !candidate.Unit.IsValid))
            throw new ArgumentException("Every initiative candidate must have a valid UnitKey.", nameof(eligibleUnits));
        if (ordered.Select(candidate => candidate.Unit).Distinct().Count() != ordered.Length)
            throw new ArgumentException("Initiative candidates must have unique UnitKeys.", nameof(eligibleUnits));
        if (ordered.Select(candidate => candidate.Unit.UnitSlot).Distinct().Count() != ordered.Length)
            throw new ArgumentException("One battle-generation initiative list cannot contain two identities for one unit slot.", nameof(eligibleUnits));
        int count = ordered.Length;
        var result = new DclInitiativeEntry[count];
        for (int rank = 0; rank < count; rank++)
        {
            int initialCt = checked((int)new DclRational((count - rank) * 100, count + 1).Floor());
            result[rank] = new DclInitiativeEntry(ordered[rank].Unit, rank, initialCt);
        }
        return result;
    }
}

internal enum DclCtRate
{
    Stopped,
    Slow,
    Normal,
    Haste,
}

internal sealed class DclCtState
{
    public const int TurnThreshold = 100;
    public const int GlobalCtGain = 10;

    public DclCtState(int initialCt)
    {
        if (initialCt is < 0 or >= TurnThreshold)
            throw new ArgumentOutOfRangeException(nameof(initialCt));
        CurrentCt = DclRational.FromInteger(initialCt);
    }

    private DclCtState(DclRational currentCt)
    {
        if (currentCt < DclRational.FromInteger(0)) throw new ArgumentOutOfRangeException(nameof(currentCt));
        CurrentCt = currentCt;
    }

    internal static DclCtState Restore(DclRational currentCt) => new(currentCt);

    public DclRational CurrentCt { get; private set; }
    public bool IsTurnEligible => CurrentCt >= DclRational.FromInteger(TurnThreshold);

    public void Tick(DclCtRate rate)
        => AdvanceTicks(rate, 1);

    public void AdvanceTicks(DclCtRate rate, long tickCount)
    {
        if (tickCount < 0) throw new ArgumentOutOfRangeException(nameof(tickCount));
        DclRational multiplier = rate switch
        {
            DclCtRate.Stopped => DclRational.FromInteger(0),
            DclCtRate.Slow => new DclRational(3, 4),
            DclCtRate.Normal => DclRational.FromInteger(1),
            DclCtRate.Haste => new DclRational(3, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(rate)),
        };
        CurrentCt += DclRational.FromInteger(GlobalCtGain) * multiplier * DclRational.FromInteger(tickCount);
    }

    public void GrantTurnAndReset()
    {
        if (!IsTurnEligible) throw new InvalidOperationException("CT has not reached the turn threshold.");
        CurrentCt = DclRational.FromInteger(0);
    }

    public void ApplyExplicitCtChange(DclRational delta)
    {
        CurrentCt += delta;
        if (CurrentCt < DclRational.FromInteger(0)) CurrentCt = DclRational.FromInteger(0);
    }
}

internal sealed class DclTurnResources
{
    public bool MovementAvailable { get; private set; } = true;
    public bool ActionAvailable { get; private set; } = true;

    public bool CanPay(DclTimingProfile timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        return (!timing.ConsumesMovement || MovementAvailable) && (!timing.ConsumesAction || ActionAvailable);
    }

    public void Pay(DclTimingProfile timing)
    {
        if (!CanPay(timing)) throw new InvalidOperationException("The action's declared turn resources are not available.");
        if (timing.ConsumesMovement) MovementAvailable = false;
        if (timing.ConsumesAction) ActionAvailable = false;
    }

    public void ResetForGrantedTurn(bool movementAvailable = true, bool actionAvailable = true)
    {
        MovementAvailable = movementAvailable;
        ActionAvailable = actionAvailable;
    }

    public void RemoveMovement() => MovementAvailable = false;
    public void RemoveAction() => ActionAvailable = false;
}
