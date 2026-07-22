namespace fftivc.generic.chronicle.codemod;

internal enum DclZodiacCompatibility
{
    Best,
    Good,
    Neutral,
    Bad,
    Worst,
}

internal readonly record struct DclSpellGateResult(
    DclCastingOutcome BaseOutcome,
    bool BaseSucceeded,
    bool TargetSucceeded,
    bool TargetCritical,
    int BaseSpellScore,
    int TargetSpellScore,
    int SharedRoll);

internal enum DclMagicDeliveryOutcome
{
    BaseFailure,
    TargetFailure,
    Resisted,
    Defended,
    Delivered,
    CriticalDelivered,
}

internal readonly record struct DclMagicDeliveryResult(
    DclMagicDeliveryOutcome Outcome,
    DclSpellGateResult Gate,
    DclDefenseKind DefenseKind,
    int DefenseScore,
    int DefenseRoll,
    int? ResistanceRoll,
    int? WinningMargin)
{
    public bool Delivered => Outcome is DclMagicDeliveryOutcome.Delivered or DclMagicDeliveryOutcome.CriticalDelivered;
}

internal static class DclMagicDefensePolicy
{
    public static void Validate(
        DclDeliveryProfile delivery,
        DclDefenseOption defense,
        DclDefenseResourceSnapshot resources)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(resources);
        if (delivery.Delivery != DclDelivery.ExternalProjectile)
        {
            if (defense != new DclDefenseOption(DclDefenseKind.None, 0, Depletes: false))
                throw new ArgumentException("Only External Projectile delivery may select an active magic defense.", nameof(defense));
            return;
        }

        switch (defense.Kind)
        {
            case DclDefenseKind.None:
                if (defense.Target != 0 || defense.Depletes)
                    throw new ArgumentException("No defense must carry score zero and no finite resource.", nameof(defense));
                break;
            case DclDefenseKind.Dodge:
                if (!delivery.Dodgeable || defense.Depletes)
                    throw new ArgumentException("The selected magic Dodge must be authored and cannot deplete a finite resource.", nameof(defense));
                break;
            case DclDefenseKind.Block:
                if (!delivery.Blockable || !delivery.UsesDefenseBonus || !defense.Depletes)
                    throw new ArgumentException("The selected magic Block must be authored and deplete the one current Block resource.", nameof(defense));
                if (!resources.BlockAvailable)
                    throw new ArgumentException("The selected magic Block is unavailable before the defender's next granted turn.", nameof(resources));
                break;
            case DclDefenseKind.Parry:
                throw new ArgumentException("External Projectile does not permit ordinary Parry.", nameof(defense));
            default:
                throw new ArgumentOutOfRangeException(nameof(defense));
        }
        if (defense.Kind != DclDefenseKind.None && defense.Target < 0)
            throw new ArgumentOutOfRangeException(nameof(defense), "An active-defense score cannot be negative.");
    }

    public static DclDefenseResourceSnapshot ResolveFinalResources(
        DclMagicDeliveryResult delivery,
        DclDefenseResourceSnapshot before)
        => delivery.DefenseKind == DclDefenseKind.Block
            ? before with { BlockAvailable = false }
            : before;

    public static DclRational AttemptProbability(
        DclDeliveryProfile delivery,
        int baseSpellScore,
        int targetSpellScore,
        DclDefenseOption defense)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (delivery.Delivery != DclDelivery.ExternalProjectile || defense.Kind == DclDefenseKind.None)
            return DclRational.FromInteger(0);
        int weight = 0;
        for (int roll = DclSuccessRoll.MinRoll; roll <= DclSuccessRoll.MaxRoll; roll++)
        {
            DclSpellGateResult gate = DclSpellResolution.ClassifySharedRoll(
                roll,
                baseSpellScore,
                targetSpellScore);
            if (gate.BaseSucceeded && gate.TargetSucceeded && !gate.TargetCritical)
                weight += DclSuccessRoll.OutcomeMultiplicity(roll);
        }
        return new DclRational(weight, 216);
    }
}

internal static class DclSpellResolution
{
    public static int TraditionSkill(
        int iq,
        DclSkillDifficulty traditionDifficulty,
        int rank,
        int explicitTraditionModifier)
        => checked(DclSkillRules.GurpsSkillScore(iq, traditionDifficulty, rank) + explicitTraditionModifier);

    public static int BaseSpellScore(
        int traditionSkill,
        int spellModifier,
        IEnumerable<int> equipmentSkillModifiers,
        int shockPenaltyMagnitude,
        int casterStatePenaltyMagnitude)
    {
        if (shockPenaltyMagnitude < 0 || casterStatePenaltyMagnitude < 0)
            throw new ArgumentOutOfRangeException(nameof(shockPenaltyMagnitude));
        ArgumentNullException.ThrowIfNull(equipmentSkillModifiers);
        int score = checked(traditionSkill + spellModifier - shockPenaltyMagnitude - casterStatePenaltyMagnitude);
        foreach (int modifier in equipmentSkillModifiers) score = checked(score + modifier);
        return score;
    }

    public static int ZodiacModifier(DclZodiacCompatibility compatibility)
        => compatibility switch
        {
            DclZodiacCompatibility.Best => 2,
            DclZodiacCompatibility.Good => 1,
            DclZodiacCompatibility.Neutral => 0,
            DclZodiacCompatibility.Bad => -1,
            DclZodiacCompatibility.Worst => -2,
            _ => throw new ArgumentOutOfRangeException(nameof(compatibility)),
        };

    public static int TargetSpellScore(
        int baseSpellScore,
        bool zodiacSensitive,
        DclZodiacCompatibility compatibility,
        int targetRelativePenaltyMagnitude)
    {
        if (targetRelativePenaltyMagnitude < 0) throw new ArgumentOutOfRangeException(nameof(targetRelativePenaltyMagnitude));
        return checked(baseSpellScore + (zodiacSensitive ? ZodiacModifier(compatibility) : 0) - targetRelativePenaltyMagnitude);
    }

    public static DclCastingOutcome ClassifyBaseOutcome(int sharedRoll, int baseSpellScore)
    {
        DclSuccessRoll.Validate(sharedRoll);
        if (DclSuccessRoll.IsCriticalSuccess(sharedRoll, baseSpellScore)) return DclCastingOutcome.CriticalSuccess;
        if (DclSuccessRoll.IsCriticalFailure(sharedRoll, baseSpellScore)) return DclCastingOutcome.CriticalFailure;
        return DclSuccessRoll.Succeeds(sharedRoll, baseSpellScore)
            ? DclCastingOutcome.Success
            : DclCastingOutcome.OrdinaryFailure;
    }

    public static DclSpellGateResult ClassifySharedRoll(
        int sharedRoll,
        int baseSpellScore,
        int targetSpellScore)
    {
        DclCastingOutcome baseOutcome = ClassifyBaseOutcome(sharedRoll, baseSpellScore);
        bool baseSucceeded = baseOutcome is DclCastingOutcome.CriticalSuccess or DclCastingOutcome.Success;
        bool targetSucceeded = baseSucceeded && DclSuccessRoll.Succeeds(sharedRoll, targetSpellScore);
        bool targetCritical = targetSucceeded && DclSuccessRoll.IsCriticalSuccess(sharedRoll, targetSpellScore);
        return new DclSpellGateResult(
            baseOutcome,
            baseSucceeded,
            targetSucceeded,
            targetCritical,
            baseSpellScore,
            targetSpellScore,
            sharedRoll);
    }

    public static DclMagicDeliveryResult ResolveExternal(
        int sharedRoll,
        int baseSpellScore,
        int targetSpellScore,
        DclDefenseOption defense,
        int? defenseRoll)
    {
        DclSpellGateResult gate = ClassifySharedRoll(sharedRoll, baseSpellScore, targetSpellScore);
        if (!gate.BaseSucceeded)
            return Delivery(DclMagicDeliveryOutcome.BaseFailure, gate);
        if (!gate.TargetSucceeded)
            return Delivery(DclMagicDeliveryOutcome.TargetFailure, gate);
        if (gate.TargetCritical)
            return Delivery(DclMagicDeliveryOutcome.CriticalDelivered, gate);
        if (defense.Kind == DclDefenseKind.None)
        {
            if (defenseRoll is not null) throw new ArgumentException("No-defense delivery cannot consume defense RNG.", nameof(defenseRoll));
            return Delivery(DclMagicDeliveryOutcome.Delivered, gate);
        }
        if (defense.Kind == DclDefenseKind.Parry)
            throw new ArgumentException("External Projectile does not permit ordinary Parry.", nameof(defense));
        if (defenseRoll is null) throw new ArgumentNullException(nameof(defenseRoll));
        bool defended = DclSuccessRoll.Succeeds(defenseRoll.Value, defense.Target);
        return new DclMagicDeliveryResult(
            defended ? DclMagicDeliveryOutcome.Defended : DclMagicDeliveryOutcome.Delivered,
            gate,
            defense.Kind,
            defense.Target,
            defenseRoll.Value,
            null,
            null);
    }

    public static DclMagicDeliveryResult ResolveBeneficial(
        int sharedRoll,
        int baseSpellScore,
        int targetSpellScore)
    {
        DclSpellGateResult gate = ClassifySharedRoll(sharedRoll, baseSpellScore, targetSpellScore);
        return Delivery(!gate.BaseSucceeded
            ? DclMagicDeliveryOutcome.BaseFailure
            : !gate.TargetSucceeded
                ? DclMagicDeliveryOutcome.TargetFailure
                : gate.TargetCritical
                    ? DclMagicDeliveryOutcome.CriticalDelivered
                    : DclMagicDeliveryOutcome.Delivered, gate);
    }

    public static DclMagicDeliveryResult ResolveInternal(
        int sharedRoll,
        int baseSpellScore,
        int targetSpellScore,
        int resistanceScore,
        int? resistanceRoll,
        bool immune = false)
    {
        DclSpellGateResult gate = ClassifySharedRoll(sharedRoll, baseSpellScore, targetSpellScore);
        if (!gate.BaseSucceeded)
        {
            if (resistanceRoll is not null) throw new ArgumentException("Base failure cannot consume resistance RNG.", nameof(resistanceRoll));
            return Delivery(DclMagicDeliveryOutcome.BaseFailure, gate);
        }
        if (!gate.TargetSucceeded)
        {
            if (resistanceRoll is not null) throw new ArgumentException("TargetSpellScore failure cannot consume resistance RNG.", nameof(resistanceRoll));
            return Delivery(DclMagicDeliveryOutcome.TargetFailure, gate);
        }
        if (immune)
        {
            if (resistanceRoll is not null) throw new ArgumentException("Immunity is checked before resistance RNG.", nameof(resistanceRoll));
            return Delivery(DclMagicDeliveryOutcome.Resisted, gate);
        }
        if (resistanceRoll is null)
            throw new ArgumentNullException(nameof(resistanceRoll), "A delivered nonimmune Internal Direct effect requires one target resistance roll.");
        DclQuickContestResult contest = DclQuickContest.Resolve(
            targetSpellScore,
            sharedRoll,
            resistanceScore,
            resistanceRoll.Value);
        if (!contest.ActingSideWon)
            return new DclMagicDeliveryResult(
                DclMagicDeliveryOutcome.Resisted,
                gate,
                DclDefenseKind.None,
                0,
                -1,
                resistanceRoll.Value,
                contest.ActingMargin - contest.TargetMargin);
        return new DclMagicDeliveryResult(
            gate.TargetCritical ? DclMagicDeliveryOutcome.CriticalDelivered : DclMagicDeliveryOutcome.Delivered,
            gate,
            DclDefenseKind.None,
            0,
            -1,
            resistanceRoll.Value,
            contest.ActingMargin - contest.TargetMargin);
    }

    public static DclExactProbability ExternalSuccessProbability(
        int baseSpellScore,
        int targetSpellScore,
        DclDefenseOption defense)
    {
        if (defense.Kind == DclDefenseKind.Parry)
            throw new ArgumentException("External Projectile does not permit ordinary Parry.", nameof(defense));
        int successes = 0;
        int defenseOutcomeCount = defense.Kind == DclDefenseKind.None ? 1 : 216;
        int defenseSuccessCount = defense.Kind == DclDefenseKind.None ? 0 : DclSuccessRoll.SuccessOutcomeCount(defense.Target);
        for (int roll = DclSuccessRoll.MinRoll; roll <= DclSuccessRoll.MaxRoll; roll++)
        {
            int multiplicity = DclSuccessRoll.OutcomeMultiplicity(roll);
            DclSpellGateResult gate = ClassifySharedRoll(roll, baseSpellScore, targetSpellScore);
            if (!gate.TargetSucceeded) continue;
            successes += gate.TargetCritical
                ? multiplicity * defenseOutcomeCount
                : multiplicity * (defenseOutcomeCount - defenseSuccessCount);
        }
        return new DclExactProbability(successes, 216 * defenseOutcomeCount);
    }

    public static DclExactProbability BeneficialSuccessProbability(int baseSpellScore, int targetSpellScore)
    {
        int successes = 0;
        for (int roll = DclSuccessRoll.MinRoll; roll <= DclSuccessRoll.MaxRoll; roll++)
            if (ClassifySharedRoll(roll, baseSpellScore, targetSpellScore).TargetSucceeded)
                successes += DclSuccessRoll.OutcomeMultiplicity(roll);
        return new DclExactProbability(successes, 216);
    }

    public static DclExactProbability InternalSuccessProbability(
        int baseSpellScore,
        int targetSpellScore,
        int resistanceScore)
    {
        int successes = 0;
        for (int castingRoll = DclSuccessRoll.MinRoll; castingRoll <= DclSuccessRoll.MaxRoll; castingRoll++)
        {
            DclSpellGateResult gate = ClassifySharedRoll(castingRoll, baseSpellScore, targetSpellScore);
            if (!gate.BaseSucceeded) continue;
            for (int resistanceRoll = DclSuccessRoll.MinRoll; resistanceRoll <= DclSuccessRoll.MaxRoll; resistanceRoll++)
            {
                if (!DclQuickContest.Resolve(targetSpellScore, castingRoll, resistanceScore, resistanceRoll).ActingSideWon)
                    continue;
                successes += DclSuccessRoll.OutcomeMultiplicity(castingRoll) *
                    DclSuccessRoll.OutcomeMultiplicity(resistanceRoll);
            }
        }
        return new DclExactProbability(successes, 216 * 216);
    }

    private static DclMagicDeliveryResult Delivery(DclMagicDeliveryOutcome outcome, DclSpellGateResult gate)
        => new(outcome, gate, DclDefenseKind.None, 0, -1, null, null);
}

internal enum DclElementAffinityKind
{
    Numeric,
    Null,
    Absorb,
}

internal readonly record struct DclElementAffinity(
    DclElementAffinityKind Kind,
    int Step,
    DclRational TargetMultiplier,
    DclRational SourceBoost);

internal static class DclMagicMagnitude
{
    private static readonly DclRational One = DclRational.FromInteger(1);

    public static int ClampFaith(int recruitmentFaith, int permanentChanges, int temporaryChanges = 0)
    {
        int permanent = Math.Clamp(checked(recruitmentFaith + permanentChanges), 0, 100);
        return Math.Clamp(checked(permanent + temporaryChanges), 0, 100);
    }

    public static DclRational FaithFactor(int currentFaith)
    {
        if (currentFaith is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(currentFaith));
        return One + new DclRational(3L * (currentFaith - 50), 500);
    }

    public static DclRational FaithMagnitude(DclFaithPolicy policy, int casterFaith, int targetFaith)
        => policy switch
        {
            DclFaithPolicy.None => One,
            DclFaithPolicy.Caster => FaithFactor(casterFaith),
            DclFaithPolicy.Target => FaithFactor(targetFaith),
            DclFaithPolicy.Both => FaithFactor(casterFaith) * FaithFactor(targetFaith),
            _ => throw new ArgumentOutOfRangeException(nameof(policy)),
        };

    public static DclElementAffinity ResolveAffinity(
        bool absorb,
        bool nullify,
        IEnumerable<int> numericSteps,
        IEnumerable<DclRational> sourceBoosts)
    {
        ArgumentNullException.ThrowIfNull(numericSteps);
        ArgumentNullException.ThrowIfNull(sourceBoosts);
        DclRational boost = One;
        foreach (DclRational candidate in sourceBoosts)
        {
            if (candidate < One) throw new ArgumentOutOfRangeException(nameof(sourceBoosts));
            if (candidate > boost) boost = candidate;
        }
        if (absorb) return new DclElementAffinity(DclElementAffinityKind.Absorb, 0, One, boost);
        if (nullify) return new DclElementAffinity(DclElementAffinityKind.Null, 0, DclRational.FromInteger(0), boost);
        int step = Math.Clamp(numericSteps.Sum(), -1, 2);
        DclRational multiplier = step switch
        {
            -1 => new DclRational(1, 2),
            0 => One,
            1 => new DclRational(3, 2),
            2 => DclRational.FromInteger(2),
            _ => throw new InvalidOperationException(),
        };
        return new DclElementAffinity(DclElementAffinityKind.Numeric, step, multiplier, boost);
    }

    public static DclDiceExpression BuildDiceExpression(
        int iq,
        DclDamageBasis basis,
        DclDiceExpression? fixedExpression,
        int integerModifier,
        int wholeDiceModifier)
    {
        DclDiceExpression basic = basis switch
        {
            DclDamageBasis.Thrust => DclStrengthDamage.Lookup(iq, DclStrengthDamageMode.Thrust),
            DclDamageBasis.Swing => DclStrengthDamage.Lookup(iq, DclStrengthDamageMode.Swing),
            DclDamageBasis.Fixed when fixedExpression is not null => fixedExpression.Value,
            DclDamageBasis.Fixed => throw new ArgumentNullException(nameof(fixedExpression)),
            _ => throw new ArgumentOutOfRangeException(nameof(basis)),
        };
        DclDiceExpression normalized = basic.AddAndNormalize(integerModifier);
        int dice = checked(normalized.Dice + wholeDiceModifier);
        if (dice < 0) throw new ArgumentOutOfRangeException(nameof(wholeDiceModifier));
        return new DclDiceExpression(dice, normalized.Adds);
    }

    public static DclCriticalHealingExpression CriticalHealingExpression(DclDiceExpression normal)
        => normal.Dice == 0
            ? new DclCriticalHealingExpression(0, normal.Adds)
            : new DclCriticalHealingExpression(normal.Dice - 1, checked(normal.Adds + 6));
}

internal readonly record struct DclCriticalHealingExpression
{
    public int DiceToRoll { get; }
    public int Adds { get; }

    public DclCriticalHealingExpression(int diceToRoll, int adds)
    {
        if (diceToRoll < 0) throw new ArgumentOutOfRangeException(nameof(diceToRoll));
        DiceToRoll = diceToRoll;
        Adds = adds;
    }
}

internal enum DclMagicalEffectRoute
{
    NoPenetration,
    Nullified,
    Injury,
    AbsorbedHealing,
}

internal readonly record struct DclMagicalEffectResult(
    DclMagicalEffectRoute Route,
    int BaseInjury,
    DclRational CombinedMultiplier,
    int FinalInjury,
    int UncappedHealing,
    int AppliedHealing,
    bool ConsumeOil);

internal static class DclMagicalEffect
{
    public static DclMagicalEffectResult ResolveInjuryOrAbsorb(
        int baseInjury,
        DclElementAffinity affinity,
        DclRational faithMagnitude,
        bool shellSensitive,
        bool targetHasShell,
        int targetCurrentHp,
        int targetMaxHp,
        bool fireEffect,
        bool oilContributed)
    {
        if (baseInjury < 0 || targetCurrentHp < 0 || targetMaxHp < 1 || targetCurrentHp > targetMaxHp)
            throw new ArgumentOutOfRangeException(nameof(baseInjury));
        if (faithMagnitude < DclRational.FromInteger(0)) throw new ArgumentOutOfRangeException(nameof(faithMagnitude));
        DclRational shell = shellSensitive && targetHasShell ? new DclRational(7, 10) : DclRational.FromInteger(1);
        DclRational common = affinity.SourceBoost * faithMagnitude * shell;
        if (baseInjury == 0)
            return new DclMagicalEffectResult(DclMagicalEffectRoute.NoPenetration, 0, common, 0, 0, 0, false);
        if (affinity.Kind == DclElementAffinityKind.Null)
            return new DclMagicalEffectResult(DclMagicalEffectRoute.Nullified, baseInjury, DclRational.FromInteger(0), 0, 0, 0, false);
        if (affinity.Kind == DclElementAffinityKind.Absorb)
        {
            int healing = Math.Max(1, checked((int)(DclRational.FromInteger(baseInjury) * common).Floor()));
            int applied = Math.Min(healing, targetMaxHp - targetCurrentHp);
            return new DclMagicalEffectResult(
                DclMagicalEffectRoute.AbsorbedHealing, baseInjury, common, 0, healing, applied, false);
        }
        DclRational combined = affinity.TargetMultiplier * common;
        if (combined == DclRational.FromInteger(0))
            return new DclMagicalEffectResult(DclMagicalEffectRoute.Nullified, baseInjury, combined, 0, 0, 0, false);
        int injury = Math.Max(1, checked((int)(DclRational.FromInteger(baseInjury) * combined).Floor()));
        bool consumeOil = fireEffect && oilContributed && injury > 0;
        return new DclMagicalEffectResult(
            DclMagicalEffectRoute.Injury, baseInjury, combined, injury, 0, 0, consumeOil);
    }

    public static DclHealingResult ResolveHealing(
        int rawHealing,
        DclRational faithMultiplier,
        int currentHp,
        int maxHp)
    {
        if (currentHp < 0 || maxHp < 1 || currentHp > maxHp || faithMultiplier < DclRational.FromInteger(0))
            throw new ArgumentOutOfRangeException(nameof(currentHp));
        int nonnegativeRaw = Math.Max(0, rawHealing);
        int final = checked((int)(DclRational.FromInteger(nonnegativeRaw) * faithMultiplier).Floor());
        int applied = Math.Min(final, maxHp - currentHp);
        return new DclHealingResult(nonnegativeRaw, final, applied);
    }
}

internal readonly record struct DclHealingResult(int RawHealing, int FinalHealing, int AppliedHealing);

internal readonly record struct DclReflectRoute(
    DclUnitKey OriginalTarget,
    DclUnitKey FinalTarget,
    bool Reflected,
    bool ReflectionConsumed);

internal static class DclReflectRouting
{
    public static DclReflectRoute Resolve(
        DclTargetMode targetMode,
        bool reflectable,
        bool targetHasReflect,
        bool reflectionAlreadyConsumed,
        DclUnitKey originalCaster,
        DclUnitKey originalTarget)
    {
        if (!originalCaster.IsValid || !originalTarget.IsValid ||
            originalCaster.BattleGeneration != originalTarget.BattleGeneration)
            throw new ArgumentException("Reflect routing requires stable unit identities in one battle generation.");
        bool route = targetMode == DclTargetMode.Unit && reflectable && targetHasReflect && !reflectionAlreadyConsumed;
        return new DclReflectRoute(
            originalTarget,
            route ? originalCaster : originalTarget,
            route,
            reflectionAlreadyConsumed || route);
    }
}
