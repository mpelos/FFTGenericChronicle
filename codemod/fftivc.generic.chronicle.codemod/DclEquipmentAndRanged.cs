namespace fftivc.generic.chronicle.codemod;

internal enum DclEncumbranceBand
{
    None,
    Light,
    Medium,
    Heavy,
    ExtraHeavy,
}

internal readonly record struct DclEncumbranceResult(
    DclRational Load,
    DclRational LoadRatio,
    DclEncumbranceBand Band,
    DclRational MoveMultiplier,
    int DodgePenalty,
    int EffectiveMove,
    int EffectiveJump,
    int EffectiveDodge);

internal static class DclEncumbrance
{
    public static DclEncumbranceResult Resolve(
        DclRational basicLift,
        IEnumerable<DclRational> equippedWeights,
        int basicMove,
        int baseJump,
        int dodgeBeforeEncumbrance)
    {
        if (basicLift <= DclRational.FromInteger(0)) throw new ArgumentOutOfRangeException(nameof(basicLift));
        ArgumentNullException.ThrowIfNull(equippedWeights);
        DclRational load = DclRational.FromInteger(0);
        foreach (DclRational weight in equippedWeights)
        {
            if (weight < DclRational.FromInteger(0)) throw new ArgumentOutOfRangeException(nameof(equippedWeights));
            load += weight;
        }
        DclRational ratio = load / basicLift;
        (DclEncumbranceBand band, DclRational multiplier, int dodgePenalty) = ratio switch
        {
            _ when ratio <= DclRational.FromInteger(1) => (DclEncumbranceBand.None, DclRational.FromInteger(1), 0),
            _ when ratio <= DclRational.FromInteger(2) => (DclEncumbranceBand.Light, new DclRational(4, 5), 1),
            _ when ratio <= DclRational.FromInteger(3) => (DclEncumbranceBand.Medium, new DclRational(3, 5), 2),
            _ when ratio <= DclRational.FromInteger(6) => (DclEncumbranceBand.Heavy, new DclRational(2, 5), 3),
            _ => (DclEncumbranceBand.ExtraHeavy, new DclRational(1, 5), 4),
        };
        int effectiveMove = Math.Max(1, checked((int)(DclRational.FromInteger(basicMove) * multiplier).Floor()));
        int effectiveJump = Math.Max(1, checked((int)(DclRational.FromInteger(baseJump) * multiplier).Floor()));
        int effectiveDodge = checked(dodgeBeforeEncumbrance - dodgePenalty);
        return new DclEncumbranceResult(load, ratio, band, multiplier, dodgePenalty, effectiveMove, effectiveJump, effectiveDodge);
    }
}

internal enum DclRangedWeaponKind
{
    Bow,
    Crossbow,
    Gun,
    ThrownWeapon,
}

internal readonly record struct DclRangedDefenseLegality(bool Dodge, bool Block, bool Parry);

internal static class DclRangedRules
{
    public static int DistancePenalty(int horizontalTiles)
        => horizontalTiles switch
        {
            < 1 => throw new ArgumentOutOfRangeException(nameof(horizontalTiles)),
            <= 2 => 0,
            3 => -1,
            <= 5 => -2,
            <= 7 => -3,
            <= 10 => -4,
            <= 15 => -5,
            <= 20 => -6,
            <= 30 => -7,
            <= 50 => -8,
            _ => throw new ArgumentOutOfRangeException(nameof(horizontalTiles), "The canonical ranged penalty table ends at 50 tiles."),
        };

    public static int AimBonus(int accuracy, int consecutiveSteps)
    {
        if (accuracy < 0 || consecutiveSteps < 0) throw new ArgumentOutOfRangeException(nameof(accuracy));
        return consecutiveSteps switch
        {
            0 => 0,
            1 => accuracy,
            2 => checked(accuracy + 1),
            _ => checked(accuracy + 2),
        };
    }

    public static DclRangedDefenseLegality DefenseLegality(DclRangedWeaponKind kind)
        => kind switch
        {
            DclRangedWeaponKind.Bow => new(Dodge: true, Block: true, Parry: false),
            DclRangedWeaponKind.Crossbow => new(Dodge: true, Block: true, Parry: false),
            DclRangedWeaponKind.Gun => new(Dodge: true, Block: false, Parry: false),
            DclRangedWeaponKind.ThrownWeapon => new(Dodge: true, Block: true, Parry: false),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    public static int EffectiveSkill(
        int rangedWeaponSkill,
        int aimBonus,
        int horizontalTiles,
        int locationPenaltyMagnitude,
        int shockStatePenaltyMagnitude)
    {
        if (aimBonus < 0 || locationPenaltyMagnitude < 0 || shockStatePenaltyMagnitude < 0)
            throw new ArgumentOutOfRangeException(nameof(aimBonus));
        return checked(rangedWeaponSkill + aimBonus + DistancePenalty(horizontalTiles) -
            locationPenaltyMagnitude - shockStatePenaltyMagnitude);
    }
}

internal sealed record DclAimStatePayload(
    string SchemaId,
    DclUnitKey Target,
    int ConsecutiveSteps) : DclStatePayload(SchemaId)
{
    public DclAimState Materialize() => new(Target, ConsecutiveSteps);
}

internal sealed class DclAimState
{
    public DclAimState(DclUnitKey target, int consecutiveSteps = 0)
    {
        if (!target.IsValid) throw new ArgumentException("Aim requires a stable tracked target.", nameof(target));
        if (consecutiveSteps < 0) throw new ArgumentOutOfRangeException(nameof(consecutiveSteps));
        Target = target;
        ConsecutiveSteps = consecutiveSteps;
    }

    public DclUnitKey Target { get; private set; }
    public int ConsecutiveSteps { get; private set; }
    public bool Active => ConsecutiveSteps > 0;

    public void GrantStep(DclUnitKey target)
    {
        if (!target.IsValid || target.BattleGeneration != Target.BattleGeneration)
            throw new ArgumentException("Aim target identity must remain in the same battle generation.", nameof(target));
        if (target != Target)
        {
            Target = target;
            ConsecutiveSteps = 1;
            return;
        }
        ConsecutiveSteps = checked(ConsecutiveSteps + 1);
    }

    public int Bonus(int accuracy) => DclRangedRules.AimBonus(accuracy, ConsecutiveSteps);

    public DclAimStatePayload ToPayload(string schemaId)
    {
        if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentException("Aim payload schema is required.", nameof(schemaId));
        if (!Active) throw new InvalidOperationException("Inactive Aim cannot be persisted as a combat state.");
        return new DclAimStatePayload(schemaId, Target, ConsecutiveSteps);
    }

    public void Cancel() => ConsecutiveSteps = 0;

    public bool ResolveInjuryRetention(
        int injury,
        bool forcedMovement,
        int will,
        int aimRetentionModifier,
        int statePenaltyMagnitude,
        int? roll)
    {
        if (injury < 0 || statePenaltyMagnitude < 0) throw new ArgumentOutOfRangeException(nameof(injury));
        if (!Active)
        {
            if (roll is not null) throw new ArgumentException("Inactive Aim cannot consume a retention roll.", nameof(roll));
            return false;
        }
        if (forcedMovement)
        {
            if (roll is not null) throw new ArgumentException("Forced movement cancels Aim without a retention roll.", nameof(roll));
            Cancel();
            return false;
        }
        if (injury == 0)
        {
            if (roll is not null) throw new ArgumentException("Zero Injury cannot consume an Aim-retention roll.", nameof(roll));
            return true;
        }
        if (roll is null) throw new ArgumentNullException(nameof(roll));
        int score = checked(will + aimRetentionModifier - statePenaltyMagnitude);
        bool retained = DclSuccessRoll.Succeeds(roll.Value, score);
        if (!retained) Cancel();
        return retained;
    }
}
