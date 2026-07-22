namespace fftivc.generic.chronicle.codemod;

internal enum DclWeaponBalance
{
    Balanced,
    Unbalanced,
}

internal enum DclWeaponReadinessProperty
{
    AlwaysReady,
    UnreadyAfterAttack,
}

internal sealed class DclWeaponActionState
{
    public DclWeaponActionState(
        string resourceKey,
        DclWeaponBalance balance,
        DclWeaponReadinessProperty readinessProperty,
        bool initiallyReady = true)
    {
        if (string.IsNullOrWhiteSpace(resourceKey)) throw new ArgumentException("A weapon/limb resource key is required.", nameof(resourceKey));
        ResourceKey = resourceKey;
        Balance = balance;
        ReadinessProperty = readinessProperty;
        Ready = readinessProperty == DclWeaponReadinessProperty.AlwaysReady || initiallyReady;
    }

    public string ResourceKey { get; }
    public DclWeaponBalance Balance { get; }
    public DclWeaponReadinessProperty ReadinessProperty { get; }
    public bool Ready { get; private set; }
    public bool ParrySuppressedAfterAttack { get; private set; }
    public bool CanAttack => Ready;
    public bool CanParry => Ready && !ParrySuppressedAfterAttack;
    public bool RequiresReadyAction => ReadinessProperty == DclWeaponReadinessProperty.UnreadyAfterAttack && !Ready;

    public void CommitAttack()
    {
        if (!CanAttack) throw new InvalidOperationException("An Unready weapon cannot attack.");
        if (ReadinessProperty == DclWeaponReadinessProperty.UnreadyAfterAttack) Ready = false;
        if (Balance == DclWeaponBalance.Unbalanced) ParrySuppressedAfterAttack = true;
    }

    public void ReadyWeapon()
    {
        if (ReadinessProperty == DclWeaponReadinessProperty.AlwaysReady)
            throw new InvalidOperationException("An AlwaysReady weapon does not expose the Ready command.");
        Ready = true;
    }

    public void BeginOwnerTurn()
    {
        ParrySuppressedAfterAttack = false;
    }

    internal void Restore(bool ready, bool parrySuppressedAfterAttack)
    {
        if (ReadinessProperty == DclWeaponReadinessProperty.AlwaysReady && !ready)
            throw new InvalidOperationException("An AlwaysReady weapon cannot restore as Unready.");
        if (Balance == DclWeaponBalance.Balanced && parrySuppressedAfterAttack)
            throw new InvalidOperationException("A Balanced weapon cannot restore an Unbalanced Parry suppression.");
        Ready = ready;
        ParrySuppressedAfterAttack = parrySuppressedAfterAttack;
    }
}

internal readonly record struct DclReequipDefenseSnapshot(
    IReadOnlyDictionary<string, int> ParryAttemptCounts,
    bool BlockAvailable,
    IReadOnlyDictionary<string, bool> WeaponReady,
    IReadOnlyDictionary<string, bool> ParrySuppressedAfterAttack);

internal static class DclUniversalActions
{
    private static readonly DclTimingProfile ActionOnly = new(
        ConsumesAction: true,
        ConsumesMovement: false,
        BaseCastCt: 0,
        CastCtModifier: 0,
        ConcentrationRequired: false);

    private static readonly DclTimingProfile ActionAndMovement = new(
        ConsumesAction: true,
        ConsumesMovement: true,
        BaseCastCt: 0,
        CastCtModifier: 0,
        ConcentrationRequired: false);

    public static void Attack(DclTurnResources resources, DclWeaponActionState weapon)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(weapon);
        if (!weapon.CanAttack) throw new InvalidOperationException("The selected weapon is Unready.");
        resources.Pay(ActionOnly);
        weapon.CommitAttack();
    }

    public static void Ready(DclTurnResources resources, DclWeaponActionState weapon)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(weapon);
        if (!weapon.RequiresReadyAction) throw new InvalidOperationException("Ready is legal only for an Unready weapon with that property.");
        resources.Pay(ActionOnly);
        weapon.ReadyWeapon();
    }

    public static DclReequipDefenseSnapshot Reequip(
        DclTurnResources resources,
        DclReequipDefenseSnapshot defenseState)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(defenseState.ParryAttemptCounts);
        ArgumentNullException.ThrowIfNull(defenseState.WeaponReady);
        ArgumentNullException.ThrowIfNull(defenseState.ParrySuppressedAfterAttack);
        resources.Pay(ActionOnly);
        return new DclReequipDefenseSnapshot(
            new Dictionary<string, int>(defenseState.ParryAttemptCounts, StringComparer.Ordinal),
            defenseState.BlockAvailable,
            new Dictionary<string, bool>(defenseState.WeaponReady, StringComparer.Ordinal),
            new Dictionary<string, bool>(defenseState.ParrySuppressedAfterAttack, StringComparer.Ordinal));
    }

    public static void StandUp(DclTurnResources resources, bool knockedDown)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (!knockedDown) throw new InvalidOperationException("Stand Up is legal only while Knocked Down.");
        resources.Pay(ActionAndMovement);
    }
}
