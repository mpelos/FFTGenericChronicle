namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalReactionCandidateEvaluation(
    int NativeOrder,
    DclUnitKey Reactor,
    DclReactionDefinition Definition,
    bool ReachesActivation,
    DclExactProbability ActivationProbability,
    bool NaturalEffectGateRequired,
    string Reason,
    DclActionProfile EffectAction,
    DclCanonicalReactionEffectRoute EffectRoute);

internal sealed record DclCanonicalReactionEvaluationResult(
    long ActionInstanceId,
    IReadOnlyList<DclCanonicalReactionCandidateEvaluation> Candidates,
    DclRational ExpectedAcceptedActivations);

internal sealed record DclCanonicalReactionPlayerForecast(
    int NativeOrder,
    DclUnitKey Reactor,
    string ReactionId,
    DclReactionActivationMode ActivationMode,
    int ActivationChancePercent,
    bool NaturalEffectGateRequired,
    string Reason,
    string EffectActionId,
    DclUnitKey EffectSource,
    DclUnitKey EffectTarget);

internal sealed record DclCanonicalReactionAiProjection(
    DclRational ExpectedAcceptedActivations,
    IReadOnlyDictionary<(DclUnitKey Reactor, string ReactionId), DclRational> ActivationProbabilities);

internal static class DclCanonicalReactionEvaluation
{
    public static DclCanonicalReactionEvaluationResult Evaluate(
        DclCanonicalBattleRuntime battle,
        DclActionDeclaration declaration,
        IReadOnlyList<DclCanonicalReactionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(battle);
        return Evaluate(
            battle.Catalog,
            declaration,
            DclCanonicalReactionStateProjection.Project(battle, candidates));
    }

    public static DclCanonicalReactionEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclActionDeclaration declaration,
        IReadOnlyList<DclCanonicalReactionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(candidates);
        if (declaration.ActionInstanceId <= 0 || !declaration.Source.IsValid)
            throw new ArgumentException("Reaction evaluation requires a valid outer ActionInstance.", nameof(declaration));
        runtime.ResolveAction(declaration.ActionId, declaration.ProfileRevision);
        DclCanonicalReactionCandidate[] ordered = candidates
            .OrderBy(candidate => candidate.NativeOrder)
            .ThenBy(candidate => candidate.Reactor.UnitSlot)
            .ThenBy(candidate => candidate.ReactionId, StringComparer.Ordinal)
            .ToArray();
        if (ordered.Any(candidate => candidate.NativeOrder < 0 || candidate.ActivationRoll is not null ||
                candidate.WeaponEquipmentSlot is { } slot && string.IsNullOrWhiteSpace(slot)))
            throw new ArgumentException(
                "Reaction evaluation requires nonnegative native order, exact weapon slots, and no execution rolls.",
                nameof(candidates));
        if (ordered.Select(candidate => candidate.NativeOrder).Distinct().Count() != ordered.Length ||
            ordered.Select(candidate => (candidate.Reactor, candidate.ReactionId)).Distinct().Count() != ordered.Length)
            throw new ArgumentException("Reaction evaluation candidates require unique order and reactor/Reaction identities.", nameof(candidates));

        var result = new List<DclCanonicalReactionCandidateEvaluation>(ordered.Length);
        DclRational expected = DclRational.FromInteger(0);
        foreach (DclCanonicalReactionCandidate candidate in ordered)
        {
            if (!candidate.Reactor.IsValid ||
                candidate.Reactor.BattleGeneration != declaration.Source.BattleGeneration)
                throw new ArgumentException("Reaction evaluation requires reactor identities from the outer battle generation.", nameof(candidates));
            if (!runtime.Authoring.Reactions.TryGetValue(candidate.ReactionId, out DclReactionDefinition? definition))
                throw new KeyNotFoundException($"Canonical Reaction '{candidate.ReactionId}' is not loaded.");
            DclReactionValidation validation = DclReactionContract.Validate(definition);
            if (!validation.IsValid)
                throw new ArgumentException("Reaction evaluation references a non-normalized definition.", nameof(candidates));
            bool nativePermitted = definition.Cardinality != DclReactionCardinality.Native ||
                candidate.NativeCardinalityAllows;
            bool reachesActivation = candidate.TriggerSatisfied && candidate.Eligible && candidate.Aware &&
                candidate.CostsAndUsesAvailable && nativePermitted;
            if (reachesActivation && definition.ActivationMode == DclReactionActivationMode.ActivationRoll &&
                candidate.ActivationReferenceScore is null)
                throw new ArgumentException("A reachable ActivationRoll evaluation requires its one reference score.", nameof(candidates));
            if (definition.ActivationMode != DclReactionActivationMode.ActivationRoll &&
                candidate.ActivationReferenceScore is not null)
                throw new ArgumentException(
                    "AutomaticTrigger and SkillResponse evaluation cannot own an activation reference score.",
                    nameof(candidates));

            DclExactProbability probability = reachesActivation
                ? DclReactionContract.ActivationProbability(definition, candidate.ActivationReferenceScore)
                : new DclExactProbability(0, 1);
            string reason = reachesActivation
                ? definition.ActivationMode switch
                {
                    DclReactionActivationMode.AutomaticTrigger => "automatic-trigger",
                    DclReactionActivationMode.SkillResponse => "natural-effect-gate",
                    DclReactionActivationMode.ActivationRoll => "activation-roll",
                    _ => throw new InvalidOperationException(),
                }
                : !candidate.TriggerSatisfied ? "trigger-not-satisfied"
                : !candidate.Eligible ? "reactor-ineligible"
                : !nativePermitted ? "native-cardinality-rejected"
                : !candidate.Aware ? "awareness-failed"
                : "cost-or-use-unavailable";
            DclActionProfile effectAction = runtime.ResolveAction(
                definition.EffectActionId,
                runtime.Authoring.Actions[definition.EffectActionId].ProfileRevision);
            DclCanonicalReactionEffectRoute effectRoute = DclReactionContract.ResolveEffectRoute(
                definition,
                declaration.Source,
                candidate.Reactor,
                candidate.TriggerTarget,
                candidate.ExplicitEffectSource,
                candidate.ExplicitEffectTarget);
            result.Add(new DclCanonicalReactionCandidateEvaluation(
                candidate.NativeOrder,
                candidate.Reactor,
                definition,
                reachesActivation,
                probability,
                definition.ActivationMode == DclReactionActivationMode.SkillResponse && reachesActivation,
                reason,
                effectAction,
                effectRoute));
            expected += probability.Fraction;
        }
        return new DclCanonicalReactionEvaluationResult(declaration.ActionInstanceId, result, expected);
    }

    public static IReadOnlyList<DclCanonicalReactionPlayerForecast> ProjectPlayer(
        DclCanonicalReactionEvaluationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Candidates.Select(candidate => new DclCanonicalReactionPlayerForecast(
            candidate.NativeOrder,
            candidate.Reactor,
            candidate.Definition.ReactionId,
            candidate.Definition.ActivationMode,
            candidate.ActivationProbability.RoundWholePercent(),
            candidate.NaturalEffectGateRequired,
            candidate.Reason,
            candidate.EffectAction.ActionId,
            candidate.EffectRoute.Source,
            candidate.EffectRoute.Target)).ToArray();
    }

    public static DclCanonicalReactionAiProjection ProjectAi(DclCanonicalReactionEvaluationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new DclCanonicalReactionAiProjection(
            result.ExpectedAcceptedActivations,
            result.Candidates.ToDictionary(
                candidate => (candidate.Reactor, candidate.Definition.ReactionId),
                candidate => candidate.ActivationProbability.Fraction));
    }

    public static int? ResolveBaseActivationReference(
        DclReactionDefinition definition,
        DclCanonicalNativeUnitProjection reactor,
        IReadOnlyDictionary<string, int>? namedSkillScores = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(reactor);
        DclReactionValidation validation = DclReactionContract.Validate(definition);
        if (!validation.IsValid)
            throw new ArgumentException("The Reaction definition is not normalized.", nameof(definition));
        if (definition.ActivationMode != DclReactionActivationMode.ActivationRoll) return null;
        DclReactionActivationReference reference = definition.ActivationReference ??
            throw new InvalidOperationException("ActivationRoll lost its normalized reference.");
        return reference.Kind switch
        {
            DclReactionActivationReferenceKind.Dx => reactor.Primary.Dx,
            DclReactionActivationReferenceKind.Ht => reactor.Primary.Ht,
            DclReactionActivationReferenceKind.Iq => reactor.Primary.Iq,
            DclReactionActivationReferenceKind.Will => reactor.Secondary.Will,
            DclReactionActivationReferenceKind.NamedSkill when namedSkillScores is not null &&
                namedSkillScores.TryGetValue(reference.NamedSkillId!, out int score) => score,
            DclReactionActivationReferenceKind.NamedSkill => throw new KeyNotFoundException(
                $"Reaction activation Skill '{reference.NamedSkillId}' has no projected score."),
            _ => throw new InvalidOperationException("Reaction activation reference is not executable."),
        };
    }
}
