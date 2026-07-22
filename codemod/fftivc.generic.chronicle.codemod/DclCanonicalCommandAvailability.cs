namespace fftivc.generic.chronicle.codemod;

internal enum DclCanonicalCommandKind
{
    Ready,
    Reequip,
    StandUp,
}

internal sealed record DclCanonicalCommandAvailability(
    DclCanonicalCommandKind Kind,
    bool Available,
    string? WeaponResourceKey,
    string? Reason);

/// <summary>
/// Pure command-availability owner for DCL universal commands. It does not invent native menu rows:
/// native UI, equipment-policy, selected slot, and carrier binding remain explicit inputs. The
/// result is the canonical truth those later bridges must present before confirmed execution.
/// </summary>
internal static class DclCanonicalCommandAvailabilityResolver
{
    public const string TauntBlockedReason = "taunt-allows-only-universal-normal-attack";
    public const string ActionUnavailableReason = "action-resource-unavailable";
    public const string MovementUnavailableReason = "movement-resource-unavailable";
    public const string WeaponReadyReason = "weapon-not-unready";
    public const string EquipmentPolicyRejectedReason = "equipment-policy-rejected";
    public const string NotKnockedDownReason = "not-knocked-down";

    public static IReadOnlyList<DclCanonicalCommandAvailability> EvaluateAll(
        DclCanonicalBattleRuntime battle,
        DclUnitKey unit,
        string knockedDownStateKind,
        bool equipmentPolicyAllowsReequip)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (string.IsNullOrWhiteSpace(knockedDownStateKind))
            throw new ArgumentException("The Knocked Down state kind is required.", nameof(knockedDownStateKind));

        var result = new List<DclCanonicalCommandAvailability>();
        foreach (DclCanonicalWeaponRuntimeSnapshot weapon in battle.CaptureWeaponStates(unit))
            result.Add(EvaluateReady(battle, unit, weapon.ResourceKey));
        result.Add(EvaluateReequip(battle, unit, equipmentPolicyAllowsReequip));
        result.Add(EvaluateStandUp(battle, unit, knockedDownStateKind));
        return result;
    }

    public static DclCanonicalCommandAvailability EvaluateReady(
        DclCanonicalBattleRuntime battle,
        DclUnitKey unit,
        string weaponResourceKey)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (string.IsNullOrWhiteSpace(weaponResourceKey))
            throw new ArgumentException("The weapon resource key is required.", nameof(weaponResourceKey));

        string? sharedFailure = EvaluateSharedActionFailure(battle, unit);
        if (sharedFailure is not null)
            return Unavailable(DclCanonicalCommandKind.Ready, weaponResourceKey, sharedFailure);

        DclWeaponActionState weapon = battle.RegisteredWeaponState(unit, weaponResourceKey);
        return weapon.RequiresReadyAction
            ? Available(DclCanonicalCommandKind.Ready, weaponResourceKey)
            : Unavailable(DclCanonicalCommandKind.Ready, weaponResourceKey, WeaponReadyReason);
    }

    public static DclCanonicalCommandAvailability EvaluateReequip(
        DclCanonicalBattleRuntime battle,
        DclUnitKey unit,
        bool equipmentPolicyAllows)
    {
        ArgumentNullException.ThrowIfNull(battle);

        string? sharedFailure = EvaluateSharedActionFailure(battle, unit);
        if (sharedFailure is not null)
            return Unavailable(DclCanonicalCommandKind.Reequip, WeaponResourceKey: null, sharedFailure);
        return equipmentPolicyAllows
            ? Available(DclCanonicalCommandKind.Reequip, WeaponResourceKey: null)
            : Unavailable(DclCanonicalCommandKind.Reequip, WeaponResourceKey: null, EquipmentPolicyRejectedReason);
    }

    public static DclCanonicalCommandAvailability EvaluateStandUp(
        DclCanonicalBattleRuntime battle,
        DclUnitKey unit,
        string knockedDownStateKind)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (string.IsNullOrWhiteSpace(knockedDownStateKind))
            throw new ArgumentException("The Knocked Down state kind is required.", nameof(knockedDownStateKind));

        string? sharedFailure = EvaluateSharedActionFailure(battle, unit);
        if (sharedFailure is not null)
            return Unavailable(DclCanonicalCommandKind.StandUp, WeaponResourceKey: null, sharedFailure);
        if (!battle.TurnResources(unit).MovementAvailable)
            return Unavailable(DclCanonicalCommandKind.StandUp, WeaponResourceKey: null, MovementUnavailableReason);

        bool knockedDown = battle.States.Instances.Any(instance =>
            instance.Target == unit && StringComparer.Ordinal.Equals(instance.Kind, knockedDownStateKind));
        return knockedDown
            ? Available(DclCanonicalCommandKind.StandUp, WeaponResourceKey: null)
            : Unavailable(DclCanonicalCommandKind.StandUp, WeaponResourceKey: null, NotKnockedDownReason);
    }

    private static string? EvaluateSharedActionFailure(DclCanonicalBattleRuntime battle, DclUnitKey unit)
    {
        DclCanonicalTauntActionLegality taunt = DclCanonicalActionStateProjection.EvaluateTaunt(
            battle,
            unit,
            consumesAction: true,
            isUniversalNormalAttack: false,
            selectedTarget: unit,
            normalAttackTargetLegal: false);
        if (!taunt.Legal)
            return taunt.Reason ?? TauntBlockedReason;
        return battle.TurnResources(unit).ActionAvailable ? null : ActionUnavailableReason;
    }

    private static DclCanonicalCommandAvailability Available(
        DclCanonicalCommandKind kind,
        string? WeaponResourceKey)
        => new(kind, Available: true, WeaponResourceKey, Reason: null);

    private static DclCanonicalCommandAvailability Unavailable(
        DclCanonicalCommandKind kind,
        string? WeaponResourceKey,
        string reason)
        => new(kind, Available: false, WeaponResourceKey, reason);
}
