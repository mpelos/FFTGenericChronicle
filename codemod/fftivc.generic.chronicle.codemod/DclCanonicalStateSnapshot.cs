namespace fftivc.generic.chronicle.codemod;

internal static class DclCanonicalNativeStatuses
{
    public static DclNativeStatusBit Charging => new(0, 0x08);
    public static DclNativeStatusBit Ko => new(0, 0x20);
    public static DclNativeStatusBit Darkness => new(1, 0x20);
    public static DclNativeStatusBit Silence => new(1, 0x08);
    public static DclNativeStatusBit Oil => new(2, 0x80);
    public static DclNativeStatusBit Shell => new(3, 0x10);
    public static DclNativeStatusBit Reflect => new(4, 0x02);
}

internal sealed record DclCanonicalShockSnapshot(
    int UnexpiredInjury,
    int PenaltyMagnitude,
    int SkillModifier);

internal sealed record DclCanonicalTauntActionLegality(
    bool Restricted,
    bool Legal,
    bool StateShouldEnd,
    DclUnitKey? Provocateur,
    string Reason);

internal sealed record DclCanonicalDefenseStateMechanics(
    bool BlockSuppressed,
    bool ParrySuppressed,
    bool WeaponAttackSuppressed,
    bool WeaponReactionsSuppressed,
    int ParryPenaltyMagnitude,
    int WeaponSkillModifier,
    int BlockModifier,
    int DrModifier,
    int DisplacementResistance,
    string? PassabilityPolicy);

internal sealed class DclCanonicalStateSnapshot
{
    private const int StatusByteCount = 5;
    private const int SourceStatusOffset = 0x57;
    private const int ImmunityStatusOffset = 0x5C;
    private const int EffectiveStatusOffset = 0x61;
    private const int MasterStatusOffset = 0x1EF;

    private readonly byte[] _sourceStatuses;
    private readonly byte[] _immunityStatuses;
    private readonly byte[] _effectiveStatuses;
    private readonly byte[] _masterStatuses;
    private readonly DclStateInstance[] _instances;

    private DclCanonicalStateSnapshot(
        DclUnitKey unit,
        long revision,
        byte[] sourceStatuses,
        byte[] immunityStatuses,
        byte[] effectiveStatuses,
        byte[] masterStatuses,
        DclStateInstance[] instances)
    {
        Unit = unit;
        Revision = revision;
        _sourceStatuses = sourceStatuses;
        _immunityStatuses = immunityStatuses;
        _effectiveStatuses = effectiveStatuses;
        _masterStatuses = masterStatuses;
        _instances = instances;
    }

    public DclUnitKey Unit { get; }
    public long Revision { get; }
    public IReadOnlyList<byte> SourceStatuses => Array.AsReadOnly(_sourceStatuses);
    public IReadOnlyList<byte> ImmunityStatuses => Array.AsReadOnly(_immunityStatuses);
    public IReadOnlyList<byte> EffectiveStatuses => Array.AsReadOnly(_effectiveStatuses);
    public IReadOnlyList<byte> MasterStatuses => Array.AsReadOnly(_masterStatuses);
    public IReadOnlyList<DclStateInstance> Instances => Array.AsReadOnly(_instances);

    public static DclCanonicalStateSnapshot Capture(
        UnitSnapshot native,
        DclUnitKey unit,
        DclStateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(registry);
        if (!unit.IsValid || unit.BattleGeneration != registry.BattleGeneration || unit.CharacterId != native.CharId)
            throw new ArgumentException("The native unit, stable UnitKey, and state registry must identify the same battle unit.", nameof(unit));

        DclStateRegistryTargetSnapshot custom = registry.CaptureTarget(unit);
        return new DclCanonicalStateSnapshot(
            unit,
            custom.Revision,
            ReadStatusArray(native, SourceStatusOffset),
            ReadStatusArray(native, ImmunityStatusOffset),
            ReadStatusArray(native, EffectiveStatusOffset),
            ReadStatusArray(native, MasterStatusOffset),
            custom.Instances.ToArray());
    }

    public bool HasNativeSource(DclNativeStatusBit bit) => HasBit(_sourceStatuses, bit);
    public bool HasNativeImmunity(DclNativeStatusBit bit) => HasBit(_immunityStatuses, bit);
    public bool HasNativeEffective(DclNativeStatusBit bit) => HasBit(_effectiveStatuses, bit);
    public bool HasNativeMaster(DclNativeStatusBit bit) => HasBit(_masterStatuses, bit);

    public bool HasKind(string kind) => InstancesOf(kind).Count > 0;

    public IReadOnlyList<DclStateInstance> InstancesOf(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("State kind is required.", nameof(kind));
        return _instances.Where(instance => StringComparer.Ordinal.Equals(instance.Kind, kind)).ToArray();
    }

    public bool TryGetUniquePayload<TPayload>(string kind, out TPayload? payload)
        where TPayload : DclStatePayload
    {
        IReadOnlyList<DclStateInstance> matching = InstancesOf(kind);
        if (matching.Count == 0)
        {
            payload = null;
            return false;
        }
        if (matching.Count != 1)
            throw new InvalidOperationException($"Singular state '{kind}' has {matching.Count} live instances.");
        if (matching[0].Payload is not TPayload typed)
            throw new InvalidOperationException($"State '{kind}' does not carry payload type {typeof(TPayload).Name}.");
        payload = typed;
        return true;
    }

    public DclCanonicalShockSnapshot? Shock(int maxHp)
    {
        if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp));
        if (!TryGetUniquePayload<DclShockStatePayload>("shock", out DclShockStatePayload? payload))
            return null;
        DclShockStatePayload resolved = payload ?? throw new InvalidOperationException("Shock payload was not materialized.");
        if (resolved.UnexpiredInjury < 0)
            throw new InvalidOperationException("Shock cannot retain negative Injury.");
        int penalty = resolved.PenaltyMagnitude(maxHp);
        DclStateInstance instance = InstancesOf("shock")[0];
        if (instance.Strength != penalty)
            throw new InvalidOperationException("Shock payload, Strength, and current MaxHP do not describe one canonical penalty.");
        return new DclCanonicalShockSnapshot(resolved.UnexpiredInjury, penalty, -penalty);
    }

    public bool TryGetAim(out DclAimStatePayload? aim)
        => TryGetAim(out _, out aim);

    public bool TryGetAim(out DclStateInstance? instance, out DclAimStatePayload? aim)
    {
        IReadOnlyList<DclStateInstance> matching = InstancesOf("aim");
        if (matching.Count == 0)
        {
            instance = null;
            aim = null;
            return false;
        }
        if (matching.Count != 1 || matching[0].Payload is not DclAimStatePayload typed)
            throw new InvalidOperationException("Singular Aim requires exactly one typed live instance.");
        instance = matching[0];
        aim = typed;
        DclAimStatePayload resolved = aim ?? throw new InvalidOperationException("Aim payload was not materialized.");
        if (!resolved.Target.IsValid || resolved.Target.BattleGeneration != Unit.BattleGeneration || resolved.ConsecutiveSteps <= 0)
            throw new InvalidOperationException("Persistent Aim requires an active step count and a stable target in the same battle.");
        return true;
    }

    public int ElementalExposureStep(string element)
    {
        if (string.IsNullOrWhiteSpace(element)) throw new ArgumentException("Element is required.", nameof(element));
        int total = 0;
        foreach (DclStateInstance instance in InstancesOf("elemental-exposure"))
        {
            if (instance.Payload is not DclElementalExposureStatePayload exposure)
                throw new InvalidOperationException("Elemental Exposure requires its typed mechanical payload.");
            if (string.IsNullOrWhiteSpace(exposure.Element))
                throw new InvalidOperationException("Elemental Exposure has no element identity.");
            if (StringComparer.OrdinalIgnoreCase.Equals(exposure.Element.Trim(), element.Trim()))
                total = checked(total + exposure.AffinityStep);
        }
        return total;
    }

    public DclCanonicalTauntActionLegality EvaluateTauntAction(
        bool consumesAction,
        bool isUniversalNormalAttack,
        DclUnitKey? selectedTarget,
        bool provocateurActive,
        bool normalAttackTargetLegal)
        => DclCanonicalCustomStateMechanics.EvaluateTauntAction(
            Unit,
            _instances,
            consumesAction,
            isUniversalNormalAttack,
            selectedTarget,
            provocateurActive,
            normalAttackTargetLegal);

    public DclCanonicalDefenseStateMechanics DefenseMechanics(string equipmentSlot)
        => DclCanonicalCustomStateMechanics.DefenseMechanics(Unit, _instances, equipmentSlot);

    private static byte[] ReadStatusArray(UnitSnapshot native, int offset)
    {
        var result = new byte[StatusByteCount];
        for (int index = 0; index < StatusByteCount; index++)
        {
            int value = native.ReadByte(offset + index);
            if (value < 0)
                throw new ArgumentException($"The native snapshot does not contain status array 0x{offset:X}..0x{offset + StatusByteCount - 1:X}.", nameof(native));
            result[index] = (byte)value;
        }
        return result;
    }

    private static bool HasBit(byte[] statuses, DclNativeStatusBit bit)
    {
        if (bit.ByteIndex is < 0 or >= StatusByteCount || bit.Mask == 0 || (bit.Mask & (bit.Mask - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(bit), "A native status identity must select one bit in the five-byte status vocabulary.");
        return (statuses[bit.ByteIndex] & bit.Mask) != 0;
    }
}

internal static class DclCanonicalCustomStateMechanics
{
    public static DclCanonicalTauntActionLegality EvaluateTauntAction(
        DclUnitKey unit,
        IReadOnlyList<DclStateInstance> instances,
        bool consumesAction,
        bool isUniversalNormalAttack,
        DclUnitKey? selectedTarget,
        bool provocateurActive,
        bool normalAttackTargetLegal)
    {
        ArgumentNullException.ThrowIfNull(instances);
        DclStateInstance[] matching = instances.Where(instance =>
            StringComparer.Ordinal.Equals(instance.Kind, "taunt")).ToArray();
        if (matching.Length == 0)
            return new(false, true, false, null, "not-taunted");
        if (matching.Length != 1 || matching[0].Payload is not DclTauntStatePayload payload)
            throw new InvalidOperationException("Singular Taunt requires exactly one typed live instance.");
        DclUnitKey provocateur = payload.Provocateur;
        if (!provocateur.IsValid || provocateur.BattleGeneration != unit.BattleGeneration || provocateur == unit)
            throw new InvalidOperationException("Taunt requires a distinct stable provocateur in the same battle generation.");
        if (matching[0].Source is { } source && source != provocateur)
            throw new InvalidOperationException("Taunt source identity and typed provocateur payload disagree.");
        if (!provocateurActive)
            return new(true, true, true, provocateur, "provocateur-invalid-taunt-ends");
        if (!consumesAction)
            return new(true, true, false, provocateur, "movement-remains-legal");
        bool legal = isUniversalNormalAttack && selectedTarget == provocateur && normalAttackTargetLegal;
        string reason = !isUniversalNormalAttack ? "taunt-allows-only-universal-normal-attack"
            : selectedTarget != provocateur ? "taunt-requires-provocateur-target"
            : !normalAttackTargetLegal ? "provocateur-not-a-legal-normal-attack-target"
            : "taunt-normal-attack-legal";
        return new(true, legal, false, provocateur, reason);
    }

    public static DclCanonicalDefenseStateMechanics DefenseMechanics(
        DclUnitKey unit,
        IReadOnlyList<DclStateInstance> instances,
        string equipmentSlot)
    {
        if (!unit.IsValid) throw new ArgumentException("Defense-state projection requires a valid unit.", nameof(unit));
        ArgumentNullException.ThrowIfNull(instances);
        if (string.IsNullOrWhiteSpace(equipmentSlot))
            throw new ArgumentException("Defense-state projection requires one exact equipment slot.", nameof(equipmentSlot));

        DclStateInstance[] guards = instances.Where(instance =>
            StringComparer.Ordinal.Equals(instance.Kind, "guard-broken")).ToArray();
        if (guards.Length > 1 || guards.SingleOrDefault()?.Payload is not (null or DclGuardBrokenStatePayload))
            throw new InvalidOperationException("Singular Guard Broken requires at most one typed live instance.");
        DclGuardBrokenStatePayload? guard = guards.SingleOrDefault()?.Payload as DclGuardBrokenStatePayload;
        if (guard is { ParryPenalty: < 0 })
            throw new InvalidOperationException("Guard Broken ParryPenalty is a nonnegative penalty magnitude.");

        DclWeaponBoundStatePayload[] bound = instances
            .Where(instance => StringComparer.Ordinal.Equals(instance.Kind, "weapon-bound"))
            .Select(instance => instance.Payload as DclWeaponBoundStatePayload ??
                throw new InvalidOperationException("Weapon Bound requires its typed mechanical payload."))
            .Where(payload => StringComparer.Ordinal.Equals(payload.EquipmentSlot, equipmentSlot))
            .ToArray();
        if (bound.Length > 1)
            throw new InvalidOperationException($"Equipment slot '{equipmentSlot}' has multiple live Weapon Bound owners.");
        DclWeaponBoundStatePayload? weaponBound = bound.SingleOrDefault();

        DclStateInstance[] bulwarks = instances.Where(instance =>
            StringComparer.Ordinal.Equals(instance.Kind, "bulwark")).ToArray();
        if (bulwarks.Length > 1 || bulwarks.SingleOrDefault()?.Payload is not (null or DclBulwarkStatePayload))
            throw new InvalidOperationException("Singular Bulwark requires at most one typed live instance.");
        DclBulwarkPayload? bulwark = (bulwarks.SingleOrDefault()?.Payload as DclBulwarkStatePayload)?.Prepared;
        if (bulwark is { } prepared &&
            (prepared.DisplacementResistance < 0 || string.IsNullOrWhiteSpace(prepared.PassabilityPolicy)))
            throw new InvalidOperationException("Bulwark requires nonnegative displacement resistance and a named passability policy.");

        return new DclCanonicalDefenseStateMechanics(
            guard?.SuppressBlock == true,
            weaponBound?.SuppressParry == true,
            weaponBound?.SuppressAttack == true,
            weaponBound?.SuppressWeaponReactions == true,
            guard?.ParryPenalty ?? 0,
            weaponBound?.WeaponSkillPenalty ?? 0,
            bulwark?.BlockModifier ?? 0,
            bulwark?.DrModifier ?? 0,
            bulwark?.DisplacementResistance ?? 0,
            bulwark?.PassabilityPolicy);
    }
}
