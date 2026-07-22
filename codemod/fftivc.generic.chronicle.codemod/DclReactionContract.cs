namespace fftivc.generic.chronicle.codemod;

internal enum DclReactionActivationMode
{
    Unknown,
    AutomaticTrigger,
    SkillResponse,
    ActivationRoll,
}

internal enum DclReactionActivationReferenceKind
{
    Unknown,
    Dx,
    Ht,
    Iq,
    Will,
    NamedSkill,
}

internal enum DclReactionBinding
{
    Unknown,
    ReactorToSource,
    ReactorToTarget,
    SourceToReactor,
    Explicit,
}

internal enum DclReactionCardinality
{
    Unknown,
    OncePerWindow,
    Native,
}

internal sealed record DclReactionActivationReference(
    DclReactionActivationReferenceKind Kind,
    string? NamedSkillId);

internal sealed record DclReactionCost(
    int Mp,
    int Hp,
    int ItemUses,
    int CooldownUses)
{
    public bool IsFree => Mp == 0 && Hp == 0 && ItemUses == 0 && CooldownUses == 0;
}

internal sealed record DclReactionDefinition(
    string ReactionId,
    string Trigger,
    DclReactionActivationMode ActivationMode,
    DclReactionActivationReference? ActivationReference,
    int ActivationModifier,
    string EligibilityAndAwareness,
    DclReactionBinding Binding,
    string EffectActionId,
    string LegalDelivery,
    DclReactionCardinality Cardinality,
    DclReactionCost Cost,
    int? FiniteUses,
    string FailureBehavior,
    string Presentation);

internal readonly record struct DclCanonicalReactionEffectRoute(
    DclUnitKey Source,
    DclUnitKey Target);

internal sealed record DclReactionValidationFinding(string Path, string Message);

internal sealed class DclReactionValidation
{
    private readonly List<DclReactionValidationFinding> _findings = [];
    public IReadOnlyList<DclReactionValidationFinding> Findings => _findings;
    public bool IsValid => _findings.Count == 0;
    internal void Error(string path, string message) => _findings.Add(new DclReactionValidationFinding(path, message));
}

internal readonly record struct DclReactionActivationResult(
    bool Accepted,
    bool NaturalEffectGateRequired,
    bool ActivationRollConsumed,
    int EffectiveActivationScore,
    string Reason);

internal static class DclReactionContract
{
    public static DclReactionValidation Validate(DclReactionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var result = new DclReactionValidation();
        RequireText(result, "ReactionId", definition.ReactionId);
        RequireText(result, "Trigger", definition.Trigger);
        RequireText(result, "EligibilityAndAwareness", definition.EligibilityAndAwareness);
        RequireText(result, "EffectActionId", definition.EffectActionId);
        RequireText(result, "LegalDelivery", definition.LegalDelivery);
        RequireText(result, "FailureBehavior", definition.FailureBehavior);
        RequireText(result, "Presentation", definition.Presentation);
        if (definition.ActivationMode == DclReactionActivationMode.Unknown)
            result.Error("ActivationMode", "must be explicit");
        if (definition.Binding == DclReactionBinding.Unknown)
            result.Error("Binding", "must be explicit");
        if (definition.Cardinality == DclReactionCardinality.Unknown)
            result.Error("Cardinality", "must be explicit");
        if (definition.Cost.Mp < 0 || definition.Cost.Hp < 0 || definition.Cost.ItemUses < 0 || definition.Cost.CooldownUses < 0)
            result.Error("Cost", "cost channels cannot be negative");
        if (definition.FiniteUses is <= 0)
            result.Error("FiniteUses", "must be positive when present");

        switch (definition.ActivationMode)
        {
            case DclReactionActivationMode.ActivationRoll:
                if (definition.ActivationReference is null)
                {
                    result.Error("ActivationReference", "ActivationRoll requires exactly one reference");
                    break;
                }
                if (!IsCanonicalActivationReferenceKind(definition.ActivationReference.Kind))
                    result.Error("ActivationReference.Kind", "must be DX, HT, IQ, Will, or one named Skill");
                bool named = definition.ActivationReference.Kind == DclReactionActivationReferenceKind.NamedSkill;
                if (named && string.IsNullOrWhiteSpace(definition.ActivationReference.NamedSkillId))
                    result.Error("ActivationReference.NamedSkillId", "NamedSkill requires one identifier");
                if (!named && !string.IsNullOrWhiteSpace(definition.ActivationReference.NamedSkillId))
                    result.Error("ActivationReference.NamedSkillId", "is legal only for NamedSkill");
                break;
            case DclReactionActivationMode.AutomaticTrigger:
            case DclReactionActivationMode.SkillResponse:
                if (definition.ActivationReference is not null)
                    result.Error("ActivationReference", "only ActivationRoll owns an extra activation reference");
                if (definition.ActivationModifier != 0)
                    result.Error("ActivationModifier", "only ActivationRoll owns an extra activation modifier");
                break;
        }
        if (definition.ActivationMode == DclReactionActivationMode.AutomaticTrigger && !definition.Cost.IsFree)
            result.Error("Cost", "AutomaticTrigger may have no MP, HP, item, or cooldown cost");
        return result;
    }

    public static DclReactionActivationResult Resolve(
        DclReactionDefinition definition,
        bool triggerSatisfied,
        bool eligible,
        bool aware,
        bool costsAndUsesAvailable,
        int? activationReferenceScore,
        int? activationRoll)
    {
        DclReactionValidation validation = Validate(definition);
        if (!validation.IsValid)
            throw new ArgumentException($"The Reaction definition is not normalized: {string.Join("; ", validation.Findings.Select(f => $"{f.Path}: {f.Message}"))}", nameof(definition));
        if (!triggerSatisfied || !eligible || !aware || !costsAndUsesAvailable)
        {
            if (activationRoll is not null)
                throw new ArgumentException("An ineligible Reaction must not consume an activation roll.", nameof(activationRoll));
            string reason = !triggerSatisfied ? "trigger-not-satisfied"
                : !eligible ? "reactor-ineligible"
                : !aware ? "awareness-failed"
                : "cost-or-use-unavailable";
            return new DclReactionActivationResult(false, false, false, 0, reason);
        }

        return definition.ActivationMode switch
        {
            DclReactionActivationMode.AutomaticTrigger => WithoutExtraRoll(accepted: true, naturalGate: false, "automatic-trigger"),
            DclReactionActivationMode.SkillResponse => WithoutExtraRoll(accepted: true, naturalGate: true, "natural-effect-gate"),
            DclReactionActivationMode.ActivationRoll => ResolveActivationRoll(
                definition,
                activationReferenceScore,
                activationRoll),
            _ => throw new ArgumentOutOfRangeException(nameof(definition)),
        };
    }

    public static DclExactProbability ActivationProbability(
        DclReactionDefinition definition,
        int? activationReferenceScore)
    {
        DclReactionValidation validation = Validate(definition);
        if (!validation.IsValid) throw new ArgumentException("The Reaction definition is not normalized.", nameof(definition));
        if (definition.ActivationMode != DclReactionActivationMode.ActivationRoll)
            return new DclExactProbability(1, 1);
        if (activationReferenceScore is null)
            throw new ArgumentException("ActivationRoll forecast requires the one authored reference score.", nameof(activationReferenceScore));
        int score = checked(activationReferenceScore.Value + definition.ActivationModifier);
        return new DclExactProbability(DclSuccessRoll.SuccessOutcomeCount(score), 216);
    }

    public static DclCanonicalReactionEffectRoute ResolveEffectRoute(
        DclReactionDefinition definition,
        DclUnitKey outerSource,
        DclUnitKey reactor,
        DclUnitKey? triggerTarget,
        DclUnitKey? explicitSource = null,
        DclUnitKey? explicitTarget = null)
    {
        DclReactionValidation validation = Validate(definition);
        if (!validation.IsValid)
            throw new ArgumentException("The Reaction definition is not normalized.", nameof(definition));
        if (!outerSource.IsValid || !reactor.IsValid || reactor.BattleGeneration != outerSource.BattleGeneration)
            throw new ArgumentException("Reaction route identities must share one valid battle generation.");
        foreach (DclUnitKey unit in new DclUnitKey?[] { triggerTarget, explicitSource, explicitTarget }
                     .Where(unit => unit is not null)
                     .Select(unit => unit!.Value))
            if (!unit.IsValid || unit.BattleGeneration != outerSource.BattleGeneration)
                throw new ArgumentException("Optional Reaction route identities must share the outer battle generation.");

        return definition.Binding switch
        {
            DclReactionBinding.ReactorToSource when explicitSource is null && explicitTarget is null =>
                new DclCanonicalReactionEffectRoute(reactor, outerSource),
            DclReactionBinding.ReactorToTarget when triggerTarget is { } target &&
                explicitSource is null && explicitTarget is null =>
                new DclCanonicalReactionEffectRoute(reactor, target),
            DclReactionBinding.SourceToReactor when explicitSource is null && explicitTarget is null =>
                new DclCanonicalReactionEffectRoute(outerSource, reactor),
            DclReactionBinding.Explicit when explicitSource is { } source && explicitTarget is { } target =>
                new DclCanonicalReactionEffectRoute(source, target),
            DclReactionBinding.ReactorToTarget => throw new ArgumentException(
                "ReactorToTarget requires the exact target whose result triggered this candidate."),
            DclReactionBinding.Explicit => throw new ArgumentException(
                "Explicit Reaction binding requires both exact effect source and target."),
            _ => throw new ArgumentException(
                "Only Explicit binding may supply explicit effect identities."),
        };
    }

    private static DclReactionActivationResult ResolveActivationRoll(
        DclReactionDefinition definition,
        int? referenceScore,
        int? roll)
    {
        if (referenceScore is null || roll is null)
            throw new ArgumentException("Execution of ActivationRoll requires one authored reference score and one 3d6 roll.");
        int score = checked(referenceScore.Value + definition.ActivationModifier);
        bool accepted = DclSuccessRoll.Succeeds(roll.Value, score);
        return new DclReactionActivationResult(
            accepted,
            NaturalEffectGateRequired: false,
            ActivationRollConsumed: true,
            score,
            accepted ? "activation-roll-succeeded" : "activation-roll-failed");
    }

    private static DclReactionActivationResult WithoutExtraRoll(bool accepted, bool naturalGate, string reason)
        => new(accepted, naturalGate, ActivationRollConsumed: false, EffectiveActivationScore: 0, reason);

    private static bool IsCanonicalActivationReferenceKind(DclReactionActivationReferenceKind kind)
        => kind is DclReactionActivationReferenceKind.Dx
            or DclReactionActivationReferenceKind.Ht
            or DclReactionActivationReferenceKind.Iq
            or DclReactionActivationReferenceKind.Will
            or DclReactionActivationReferenceKind.NamedSkill;

    private static void RequireText(DclReactionValidation result, string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) result.Error(path, "is required");
    }
}

internal sealed class DclReactionWindowCardinality
{
    private readonly long _actionInstanceId;
    private readonly HashSet<(DclUnitKey Reactor, string ReactionId)> _accepted = [];

    public DclReactionWindowCardinality(long actionInstanceId)
    {
        if (actionInstanceId <= 0) throw new ArgumentOutOfRangeException(nameof(actionInstanceId));
        _actionInstanceId = actionInstanceId;
    }

    public long ActionInstanceId => _actionInstanceId;

    public bool TryAccept(
        DclUnitKey reactor,
        DclReactionDefinition definition,
        bool nativeCardinalityAllows)
    {
        if (!reactor.IsValid) throw new ArgumentException("A stable reactor identity is required.", nameof(reactor));
        DclReactionValidation validation = DclReactionContract.Validate(definition);
        if (!validation.IsValid) throw new ArgumentException("The Reaction definition is not normalized.", nameof(definition));
        var key = (reactor, definition.ReactionId);
        if (_accepted.Contains(key)) return false;
        bool permitted = definition.Cardinality switch
        {
            DclReactionCardinality.OncePerWindow => true,
            DclReactionCardinality.Native => nativeCardinalityAllows,
            _ => false,
        };
        if (!permitted) return false;
        _accepted.Add(key);
        return true;
    }
}
