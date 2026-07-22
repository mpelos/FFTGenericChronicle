namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNonPhysicalPlanningResult<TEvaluation>(
    DclCanonicalTauntActionLegality StateLegality,
    TEvaluation? Evaluation)
    where TEvaluation : class
{
    public bool Legal => StateLegality.Legal && Evaluation is not null;
}

/// <summary>
/// Shared state/identity gate for RNG-free nonphysical forecast and AI evaluation. The caller's
/// evaluator remains the family-specific exact owner; this boundary guarantees that player and AI
/// cannot evaluate an action which confirmed execution would reject because of Taunt or stale
/// battle snapshots.
/// </summary>
internal static class DclCanonicalNonPhysicalPlanningCoordinator
{
    public static DclCanonicalNonPhysicalPlanningResult<TEvaluation> Evaluate<TEvaluation>(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclUnitKey source,
        DclUnitKey? selectedTarget,
        IEnumerable<DclTargetCandidate> stateCandidates,
        Func<TEvaluation> evaluate)
        where TEvaluation : class
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(stateCandidates);
        ArgumentNullException.ThrowIfNull(evaluate);
        (_, DclActionProfile profile) = battle.Catalog.ResolveAbility(abilityId);
        if (!battle.TryGetObservedUnit(source.UnitSlot, out DclUnitKey observedSource) || observedSource != source)
            throw new ArgumentException("Nonphysical planning source must be a current observed UnitKey.", nameof(source));

        DclTargetCandidate[] candidates = stateCandidates.ToArray();
        if (candidates.Select(candidate => candidate.Unit).Distinct().Count() != candidates.Length)
            throw new ArgumentException("Nonphysical planning cannot contain duplicate target snapshots.", nameof(stateCandidates));
        foreach (DclTargetCandidate candidate in candidates)
        {
            if (!battle.TryGetObservedUnit(candidate.Unit.UnitSlot, out DclUnitKey observedTarget) ||
                observedTarget != candidate.Unit)
                throw new ArgumentException(
                    "Nonphysical planning target must be a current observed UnitKey.",
                    nameof(stateCandidates));
            if (battle.States.CaptureTarget(candidate.Unit).Revision != candidate.CombatStateRevision)
                throw new ArgumentException(
                    "Nonphysical planning received a stale target custom-state revision.",
                    nameof(stateCandidates));
        }

        DclCanonicalTauntActionLegality legality = DclCanonicalActionStateProjection.EvaluateTaunt(
            battle,
            source,
            profile.TimingProfile.ConsumesAction,
            isUniversalNormalAttack: false,
            selectedTarget,
            normalAttackTargetLegal: false);
        if (!legality.Legal)
            return new DclCanonicalNonPhysicalPlanningResult<TEvaluation>(legality, Evaluation: null);
        return new DclCanonicalNonPhysicalPlanningResult<TEvaluation>(legality, evaluate());
    }
}
