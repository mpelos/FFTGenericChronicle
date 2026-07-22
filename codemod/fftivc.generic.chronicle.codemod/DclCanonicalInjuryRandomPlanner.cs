namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Determines which conditional Injury-consequence random sites are reachable, samples them once
/// through the battle ledger, and returns the exact executor input. This prevents a native apply
/// re-entry from repeating Major-Wound or concentration checks.
/// </summary>
internal static class DclCanonicalInjuryRandomPlanner
{
    public static DclCanonicalInjuryConsequenceRolls PlanRolls(
        DclCanonicalBattleRuntime battle,
        long actionInstanceId,
        DclUnitKey source,
        DclUnitKey target,
        int strikeIndex,
        int hpBeforeInjury,
        DclInjuryResult injury,
        DclDamageType damageType,
        bool criticalSuccess,
        DclCanonicalInjuryTargetContext context,
        bool directConcentrationCancellation,
        int authoredForcedDisplacement,
        int? settledForcedDisplacement = null)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (hpBeforeInjury < 0 || authoredForcedDisplacement < 0 || context.MaxHp < 1 ||
            context.TargetSt < 1 || context.UnexpiredShockInjury < 0 ||
            context.ConcentrationStatePenaltyMagnitude < 0)
            throw new ArgumentOutOfRangeException(nameof(hpBeforeInjury));

        int remainingHp = Math.Max(0, checked(hpBeforeInjury - injury.Injury));
        bool requiresMajorWoundRoll = remainingHp > 0 &&
            DclRational.FromInteger(injury.Injury) > new DclRational(context.MaxHp, 2);
        int? majorWoundRoll = requiresMajorWoundRoll
            ? battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                actionInstanceId,
                source,
                target,
                strikeIndex,
                DclRollSite.MajorWound,
                drawIndex: 0))
            : null;
        DclMajorWoundResult majorWound = DclInjury.ResolveMajorWound(
            hpBeforeInjury,
            context.MaxHp,
            injury.Injury,
            context.EffectiveHt,
            majorWoundRoll);
        bool collapsed = majorWound.RequiresHtRoll && !majorWound.AvoidedCollapse;
        int criticalKnockback = DclInjury.CriticalKnockbackTiles(
            criticalSuccess,
            majorWound.RemainingHp > 0,
            damageType,
            injury.RolledDamage,
            injury.PenetratingDamage,
            context.TargetSt);
        int totalDisplacement = checked(authoredForcedDisplacement + criticalKnockback);
        int actualDisplacement = settledForcedDisplacement ?? totalDisplacement;
        if (actualDisplacement < 0 || actualDisplacement > totalDisplacement)
            throw new ArgumentOutOfRangeException(nameof(settledForcedDisplacement));
        bool directCancellation = context.Charging &&
            (directConcentrationCancellation || collapsed || majorWound.RemainingHp == 0);
        bool requiresConcentrationRoll = context.Charging && !directCancellation &&
            (injury.Injury > 0 || actualDisplacement > 0);
        int? concentrationRoll = requiresConcentrationRoll
            ? battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                actionInstanceId,
                source,
                target,
                strikeIndex,
                DclRollSite.Concentration,
                drawIndex: 0))
            : null;
        return new DclCanonicalInjuryConsequenceRolls(
            majorWoundRoll,
            directConcentrationCancellation,
            authoredForcedDisplacement,
            concentrationRoll,
            actualDisplacement);
    }

    public static DclInjuryConsequenceInput BuildInput(
        DclCanonicalInjuryTargetContext context,
        DclCanonicalInjuryConsequenceRolls rolls)
        => new(
            context.MaxHp,
            context.TargetSt,
            context.EffectiveHt,
            context.UnexpiredShockInjury,
            context.Charging,
            context.Will,
            context.ConcentrationModifier,
            context.ConcentrationStatePenaltyMagnitude,
            rolls.MajorWoundHtRoll,
            rolls.DirectConcentrationCancellation,
            rolls.AuthoredForcedDisplacement,
            rolls.ConcentrationRoll,
            rolls.SettledForcedDisplacement);
}
