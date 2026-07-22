namespace fftivc.generic.chronicle.codemod;

internal sealed record DclTouchResolutionResult(
    DclPhysicalContestResult Contest,
    DclDefenseCandidate SelectedDefense,
    DclDefenseResourceSnapshot FinalDefenseResources);

internal sealed record DclTouchEvaluationResult(
    IReadOnlyDictionary<DclPhysicalOutcome, DclRational> OutcomeProbability,
    DclDefenseCandidate SelectedDefense,
    DclRational HitProbability,
    DclRational DefenseAttemptProbability,
    DclRational ParrySpendProbability);

internal sealed record DclTouchForecastProjection(
    int HitPercent,
    int DefenseAttemptPercent,
    int ParrySpendPercent,
    DclDefenseKind SelectedDefense,
    int SelectedDefenseScore);

internal sealed record DclTouchAiProjection(
    DclRational HitProbability,
    DclRational DefenseAttemptProbability,
    DclRational ParrySpendProbability,
    DclDefenseKind SelectedDefense,
    int SelectedDefenseScore);

internal sealed record DclTouchNativeRouteVerdict(
    DclPhysicalRoute Route,
    bool RangeLegal,
    bool TrajectoryLegal);

internal static class DclTouchResolution
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);

    public static IReadOnlyList<string> RouteFailures(
        DclActionProfile profile,
        DclTouchNativeRouteVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(verdict);
        if (profile.DeliveryProfile.Delivery != DclDelivery.Touch ||
            profile.TargetProfile.PhysicalRoute is not (DclPhysicalRoute.NativeDirect or DclPhysicalRoute.NativeArc))
            throw new ArgumentException("A Touch route verdict requires normalized Touch NativeDirect/NativeArc metadata.", nameof(profile));
        if (verdict.Route != profile.TargetProfile.PhysicalRoute)
            throw new ArgumentException("The Touch native route verdict does not match the authored physical route.", nameof(verdict));
        var failures = new List<string>(2);
        if (!verdict.RangeLegal) failures.Add("native-touch-range-illegal");
        if (!verdict.TrajectoryLegal) failures.Add("native-touch-trajectory-illegal");
        return failures;
    }

    public static DclTouchResolutionResult Resolve(
        DclDeliveryProfile delivery,
        int spellScore,
        int attackRoll,
        IReadOnlyList<DclDefenseCandidate> defenseCandidates,
        DclDefenseResourceSnapshot defenseResources,
        int? defenseRoll)
    {
        Validate(delivery, defenseCandidates, defenseResources);
        DclSuccessRoll.Validate(attackRoll);
        var resources = new DclMutableDefenseResources(defenseResources);
        DclDefenseCandidate selected = SelectDefense(delivery, defenseCandidates, resources);
        bool attemptsDefense = OrdinaryAttackSucceeded(attackRoll, spellScore) &&
            selected.Kind != DclDefenseKind.None;
        if (!attemptsDefense)
        {
            if (defenseRoll is not null)
                throw new ArgumentException("Touch cannot consume defense RNG unless an ordinary successful attack reaches active defense.", nameof(defenseRoll));
            DclPhysicalContestResult uncontested = DclPhysicalContest.Resolve(
                spellScore,
                attackRoll,
                new DclDefenseOption(DclDefenseKind.None, 0, Depletes: false),
                defenseRoll: -1);
            return new DclTouchResolutionResult(uncontested, selected, resources.CaptureSnapshot());
        }

        if (defenseRoll is null)
            throw new ArgumentNullException(nameof(defenseRoll), "A reachable Touch active defense requires one defense roll.");
        if (selected.Kind == DclDefenseKind.Parry)
            resources.SpendParryAttempt(selected.ResourceKey!);
        DclPhysicalContestResult contest = DclPhysicalContest.Resolve(
            spellScore,
            attackRoll,
            new DclDefenseOption(
                selected.Kind,
                selected.Score,
                Depletes: selected.Kind == DclDefenseKind.Parry),
            defenseRoll.Value);
        return new DclTouchResolutionResult(contest, selected, resources.CaptureSnapshot());
    }

    public static DclTouchEvaluationResult Evaluate(
        DclDeliveryProfile delivery,
        int spellScore,
        IReadOnlyList<DclDefenseCandidate> defenseCandidates,
        DclDefenseResourceSnapshot defenseResources)
    {
        Validate(delivery, defenseCandidates, defenseResources);
        var resources = new DclMutableDefenseResources(defenseResources);
        DclDefenseCandidate selected = SelectDefense(delivery, defenseCandidates, resources);
        var weights = Enum.GetValues<DclPhysicalOutcome>()
            .Where(outcome => outcome != DclPhysicalOutcome.Legacy)
            .ToDictionary(outcome => outcome, _ => 0L);
        long totalWeight = 0;
        int defenseOutcomeCount = selected.Kind == DclDefenseKind.None ? 1 : 216;
        long defenseAttemptWeight = 0;
        for (int attackRoll = DclSuccessRoll.MinRoll; attackRoll <= DclSuccessRoll.MaxRoll; attackRoll++)
        {
            int attackMultiplicity = DclSuccessRoll.OutcomeMultiplicity(attackRoll);
            if (!OrdinaryAttackSucceeded(attackRoll, spellScore) || selected.Kind == DclDefenseKind.None)
            {
                DclPhysicalContestResult contest = DclPhysicalContest.Resolve(
                    spellScore,
                    attackRoll,
                    new DclDefenseOption(DclDefenseKind.None, 0, Depletes: false),
                    defenseRoll: -1);
                long weight = checked((long)attackMultiplicity * defenseOutcomeCount);
                weights[contest.Outcome] = checked(weights[contest.Outcome] + weight);
                totalWeight = checked(totalWeight + weight);
                continue;
            }

            defenseAttemptWeight = checked(defenseAttemptWeight + (long)attackMultiplicity * 216);
            for (int defenseRoll = DclSuccessRoll.MinRoll; defenseRoll <= DclSuccessRoll.MaxRoll; defenseRoll++)
            {
                DclPhysicalContestResult contest = DclPhysicalContest.Resolve(
                    spellScore,
                    attackRoll,
                    new DclDefenseOption(
                        selected.Kind,
                        selected.Score,
                        Depletes: selected.Kind == DclDefenseKind.Parry),
                    defenseRoll);
                long weight = checked((long)attackMultiplicity * DclSuccessRoll.OutcomeMultiplicity(defenseRoll));
                weights[contest.Outcome] = checked(weights[contest.Outcome] + weight);
                totalWeight = checked(totalWeight + weight);
            }
        }
        if (totalWeight != 216L * defenseOutcomeCount)
            throw new InvalidOperationException("Touch evaluation did not preserve the exact 3d6 probability space.");
        IReadOnlyDictionary<DclPhysicalOutcome, DclRational> probabilities = weights
            .Where(pair => pair.Value != 0)
            .ToDictionary(pair => pair.Key, pair => new DclRational(pair.Value, totalWeight));
        DclRational hitProbability = probabilities.GetValueOrDefault(DclPhysicalOutcome.Hit, Zero) +
            probabilities.GetValueOrDefault(DclPhysicalOutcome.CriticalHit, Zero);
        DclRational defenseAttemptProbability = new(defenseAttemptWeight, totalWeight);
        return new DclTouchEvaluationResult(
            probabilities,
            selected,
            hitProbability,
            defenseAttemptProbability,
            selected.Kind == DclDefenseKind.Parry ? defenseAttemptProbability : Zero);
    }

    public static bool RequiresDefenseAttempt(
        int spellScore,
        int attackRoll,
        DclTouchEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        DclSuccessRoll.Validate(attackRoll);
        return OrdinaryAttackSucceeded(attackRoll, spellScore) &&
            evaluation.SelectedDefense.Kind != DclDefenseKind.None;
    }

    public static DclMagicDeliveryResult ToMagicDelivery(DclTouchResolutionResult touch)
    {
        ArgumentNullException.ThrowIfNull(touch);
        DclCastingOutcome baseOutcome = touch.Contest.Outcome switch
        {
            DclPhysicalOutcome.CriticalHit => DclCastingOutcome.CriticalSuccess,
            DclPhysicalOutcome.Hit or DclPhysicalOutcome.Defended => DclCastingOutcome.Success,
            DclPhysicalOutcome.AttackMiss => DclCastingOutcome.OrdinaryFailure,
            DclPhysicalOutcome.AttackFumble => DclCastingOutcome.CriticalFailure,
            _ => throw new InvalidOperationException("Touch cannot translate a legacy physical outcome."),
        };
        bool baseSucceeded = baseOutcome is DclCastingOutcome.Success or DclCastingOutcome.CriticalSuccess;
        bool critical = touch.Contest.Outcome == DclPhysicalOutcome.CriticalHit;
        var gate = new DclSpellGateResult(
            baseOutcome,
            baseSucceeded,
            baseSucceeded,
            critical,
            touch.Contest.AttackSkill,
            touch.Contest.AttackSkill,
            touch.Contest.AttackRoll);
        DclMagicDeliveryOutcome outcome = touch.Contest.Outcome switch
        {
            DclPhysicalOutcome.CriticalHit => DclMagicDeliveryOutcome.CriticalDelivered,
            DclPhysicalOutcome.Hit => DclMagicDeliveryOutcome.Delivered,
            DclPhysicalOutcome.Defended => DclMagicDeliveryOutcome.Defended,
            DclPhysicalOutcome.AttackMiss or DclPhysicalOutcome.AttackFumble => DclMagicDeliveryOutcome.BaseFailure,
            _ => throw new InvalidOperationException("Touch cannot translate a legacy physical outcome."),
        };
        return new DclMagicDeliveryResult(
            outcome,
            gate,
            touch.Contest.DefenseKind,
            touch.Contest.DefenseTarget,
            touch.Contest.DefenseRoll,
            ResistanceRoll: null,
            WinningMargin: null);
    }

    public static DclTouchForecastProjection ProjectPlayer(DclTouchEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclTouchForecastProjection(
            checked((int)(evaluation.HitProbability * DclRational.FromInteger(100)).RoundNearest()),
            checked((int)(evaluation.DefenseAttemptProbability * DclRational.FromInteger(100)).RoundNearest()),
            checked((int)(evaluation.ParrySpendProbability * DclRational.FromInteger(100)).RoundNearest()),
            evaluation.SelectedDefense.Kind,
            evaluation.SelectedDefense.Score);
    }

    public static DclTouchAiProjection ProjectAi(DclTouchEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        return new DclTouchAiProjection(
            evaluation.HitProbability,
            evaluation.DefenseAttemptProbability,
            evaluation.ParrySpendProbability,
            evaluation.SelectedDefense.Kind,
            evaluation.SelectedDefense.Score);
    }

    public static void Validate(
        DclDeliveryProfile delivery,
        IReadOnlyList<DclDefenseCandidate> defenseCandidates,
        DclDefenseResourceSnapshot defenseResources)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(defenseCandidates);
        ArgumentNullException.ThrowIfNull(defenseResources);
        if (delivery.Delivery != DclDelivery.Touch)
            throw new ArgumentException("Touch resolution requires Touch delivery metadata.", nameof(delivery));
        if (delivery.Blockable)
            throw new ArgumentException("Touch never permits Block.", nameof(delivery));
        var identities = new HashSet<(DclDefenseKind Kind, string ResourceKey)>();
        foreach (DclDefenseCandidate candidate in defenseCandidates)
        {
            if (candidate.Kind is not (DclDefenseKind.Dodge or DclDefenseKind.Parry))
                throw new ArgumentException("Touch defense candidates are limited to Dodge and Parry.", nameof(defenseCandidates));
            string key = candidate.ResourceKey ?? string.Empty;
            if (!identities.Add((candidate.Kind, key)))
                throw new ArgumentException("Touch defense candidates must have unique defense/resource identities.", nameof(defenseCandidates));
            if (candidate.Kind == DclDefenseKind.Dodge)
            {
                if (candidate.ResourceKey is not null)
                    throw new ArgumentException("Reusable Touch Dodge cannot name a finite resource.", nameof(defenseCandidates));
                if (candidate.Legal && !delivery.Dodgeable)
                    throw new ArgumentException("Touch cannot expose legal Dodge when the action does not author Dodge.", nameof(defenseCandidates));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(candidate.ResourceKey))
                    throw new ArgumentException("Touch Parry requires its exact weapon/limb resource key.", nameof(defenseCandidates));
                if (candidate.Legal && !delivery.Parryable)
                    throw new ArgumentException("Touch cannot expose legal Parry when the action does not author Parry.", nameof(defenseCandidates));
            }
        }
    }

    private static DclDefenseCandidate SelectDefense(
        DclDeliveryProfile delivery,
        IReadOnlyList<DclDefenseCandidate> defenseCandidates,
        DclMutableDefenseResources resources)
        => DclActiveDefenseRules.SelectOne(defenseCandidates.Select(candidate => candidate.Kind switch
        {
            DclDefenseKind.Dodge => candidate with { Legal = candidate.Legal && delivery.Dodgeable },
            DclDefenseKind.Parry => candidate with
            {
                Score = checked(candidate.Score + resources.CurrentParryPenalty(candidate.ResourceKey!)),
                Legal = candidate.Legal && delivery.Parryable,
            },
            _ => throw new InvalidOperationException(),
        }));

    private static bool OrdinaryAttackSucceeded(int attackRoll, int spellScore)
        => !DclPhysicalContest.IsCritical(attackRoll, spellScore) &&
           !DclPhysicalContest.IsFumble(attackRoll, spellScore) &&
           DclSuccessRoll.Succeeds(attackRoll, spellScore);
}
