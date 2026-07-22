namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalReactionCandidate(
    int NativeOrder,
    DclUnitKey Reactor,
    string ReactionId,
    bool TriggerSatisfied,
    bool Eligible,
    bool Aware,
    bool CostsAndUsesAvailable,
    int? ActivationReferenceScore,
    int? ActivationRoll,
    bool NativeCardinalityAllows,
    DclUnitKey? TriggerTarget = null,
    DclUnitKey? ExplicitEffectSource = null,
    DclUnitKey? ExplicitEffectTarget = null,
    string? WeaponEquipmentSlot = null);

internal sealed record DclCanonicalReactionWindowRequest(
    DclCanonicalRuntimeCatalog Runtime,
    IReadOnlyList<DclCanonicalReactionCandidate> Candidates,
    DclCanonicalBattleRuntime? ExecutionBattle = null);

internal sealed record DclCanonicalReactionResult(
    int NativeOrder,
    DclUnitKey Reactor,
    DclReactionDefinition Definition,
    DclNativeReactionBinding NativeBinding,
    DclReactionActivationResult Activation,
    bool CardinalityAccepted,
    DclActionProfile? EffectAction,
    DclCanonicalReactionEffectRoute? EffectRoute);

internal sealed record DclCanonicalReactionWindowResult(
    long ActionInstanceId,
    IReadOnlyList<DclCanonicalReactionResult> Candidates,
    IReadOnlyList<DclCanonicalReactionResult> Accepted);

internal static class DclCanonicalReactionWindow
{
    public static DclCanonicalReactionWindowRequest ConfirmedRequest(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalReactionCandidate>? candidates)
    {
        ArgumentNullException.ThrowIfNull(battle);
        return new DclCanonicalReactionWindowRequest(
            battle.Catalog,
            DclCanonicalReactionStateProjection.Project(battle, candidates ?? []),
            battle);
    }

    public static void RequireConfirmedRequest(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalBattleRuntime battle,
        DclCanonicalReactionWindowRequest? request)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(battle);
        if (request is null || !ReferenceEquals(request.Runtime, runtime) ||
            !ReferenceEquals(request.ExecutionBattle, battle) || request.Candidates is null)
            throw new ArgumentException(
                "Confirmed execution requires the exact battle-owned Reaction window, including an explicit empty candidate set.",
                nameof(request));
    }

    public static void Preflight(
        DclActionDeclaration declaration,
        DclCanonicalReactionWindowRequest request)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Runtime);
        if (request.Candidates is null)
            throw new ArgumentException("Reaction candidates are required, including when empty.", nameof(request));
        DclCanonicalBattleRuntime? executionBattle = request.ExecutionBattle;
        if (executionBattle is not null &&
            (!ReferenceEquals(executionBattle.Catalog, request.Runtime) ||
             executionBattle.BattleGeneration != declaration.Source.BattleGeneration ||
             !executionBattle.TryGetObservedUnit(declaration.Source.UnitSlot, out DclUnitKey observedSource) ||
             observedSource != declaration.Source))
            throw new ArgumentException("Reaction execution battle, catalog, or outer source is inconsistent.", nameof(request));
        DclCanonicalReactionCandidate[] candidates = request.Candidates
            .OrderBy(candidate => candidate.NativeOrder)
            .ThenBy(candidate => candidate.Reactor.UnitSlot)
            .ThenBy(candidate => candidate.ReactionId, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Any(candidate => candidate.NativeOrder < 0))
            throw new ArgumentException("Native Reaction order cannot be negative.", nameof(request));
        if (candidates.Select(candidate => candidate.NativeOrder).Distinct().Count() != candidates.Length)
            throw new ArgumentException("Each Reaction candidate requires one stable native order ordinal.", nameof(request));
        if (candidates.Select(candidate => (candidate.Reactor, candidate.ReactionId)).Distinct().Count() != candidates.Length)
            throw new ArgumentException("One Reaction window cannot offer the same reactor/Reaction definition twice.", nameof(request));

        foreach (DclCanonicalReactionCandidate candidate in candidates)
        {
            if (candidate.WeaponEquipmentSlot is { } equipmentSlot && string.IsNullOrWhiteSpace(equipmentSlot))
                throw new ArgumentException("A weapon Reaction candidate requires one exact nonblank equipment slot.", nameof(request));
            if (!candidate.Reactor.IsValid || candidate.Reactor.BattleGeneration != declaration.Source.BattleGeneration)
                throw new ArgumentException("Reaction reactor identity is invalid for the outer ActionInstance.", nameof(request));
            if (executionBattle is not null &&
                (!executionBattle.TryGetObservedUnit(candidate.Reactor.UnitSlot, out DclUnitKey observedReactor) ||
                 observedReactor != candidate.Reactor || candidate.ActivationRoll is not null))
                throw new ArgumentException(
                    "Confirmed Reaction execution requires an observed reactor and accepts no pre-supplied activation roll.",
                    nameof(request));
            if (!request.Runtime.Authoring.Reactions.TryGetValue(candidate.ReactionId, out DclReactionDefinition? definition))
                throw new KeyNotFoundException($"Canonical Reaction '{candidate.ReactionId}' is not loaded.");
            _ = request.Runtime.ResolveReactionBinding(candidate.ReactionId);
            DclReactionValidation validation = DclReactionContract.Validate(definition);
            if (!validation.IsValid)
                throw new ArgumentException("The Reaction window references a non-normalized definition.", nameof(request));
            _ = DclReactionContract.ResolveEffectRoute(
                definition,
                declaration.Source,
                candidate.Reactor,
                candidate.TriggerTarget,
                candidate.ExplicitEffectSource,
                candidate.ExplicitEffectTarget);
            bool nativePermitted = definition.Cardinality != DclReactionCardinality.Native ||
                candidate.NativeCardinalityAllows;
            bool reachesActivationRoll = definition.ActivationMode == DclReactionActivationMode.ActivationRoll &&
                candidate.TriggerSatisfied && candidate.Eligible && nativePermitted && candidate.Aware &&
                candidate.CostsAndUsesAvailable;
            if (reachesActivationRoll && candidate.ActivationReferenceScore is null)
                throw new ArgumentException("A reachable ActivationRoll requires its one deterministic reference score.", nameof(request));
            if (definition.ActivationMode != DclReactionActivationMode.ActivationRoll && candidate.ActivationRoll is not null)
                throw new ArgumentException("AutomaticTrigger and SkillResponse cannot own an activation roll.", nameof(request));
        }
    }

    public static DclCanonicalReactionWindowResult Resolve(
        DclActionDeclaration declaration,
        DclCanonicalReactionWindowRequest request)
    {
        Preflight(declaration, request);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Runtime);
        if (request.Candidates is null)
            throw new ArgumentException("Reaction candidates are required, including when empty.", nameof(request));
        DclCanonicalBattleRuntime? executionBattle = request.ExecutionBattle;
        if (executionBattle is not null &&
            (!ReferenceEquals(executionBattle.Catalog, request.Runtime) ||
             executionBattle.BattleGeneration != declaration.Source.BattleGeneration ||
             !executionBattle.TryGetObservedUnit(declaration.Source.UnitSlot, out DclUnitKey observedSource) ||
             observedSource != declaration.Source))
            throw new ArgumentException("Reaction execution battle, catalog, or outer source is inconsistent.", nameof(request));
        DclCanonicalReactionCandidate[] candidates = request.Candidates
            .OrderBy(candidate => candidate.NativeOrder)
            .ThenBy(candidate => candidate.Reactor.UnitSlot)
            .ThenBy(candidate => candidate.ReactionId, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Any(candidate => candidate.NativeOrder < 0))
            throw new ArgumentException("Native Reaction order cannot be negative.", nameof(request));
        if (candidates.Select(candidate => candidate.NativeOrder).Distinct().Count() != candidates.Length)
            throw new ArgumentException("Each Reaction candidate requires one stable native order ordinal.", nameof(request));
        if (candidates.Select(candidate => (candidate.Reactor, candidate.ReactionId)).Distinct().Count() != candidates.Length)
            throw new ArgumentException("One Reaction window cannot offer the same reactor/Reaction definition twice.", nameof(request));

        foreach (DclCanonicalReactionCandidate candidate in candidates)
        {
            if (!candidate.Reactor.IsValid || candidate.Reactor.BattleGeneration != declaration.Source.BattleGeneration)
                throw new ArgumentException("Reaction reactor identity is invalid for the outer ActionInstance.", nameof(request));
            if (executionBattle is not null &&
                (!executionBattle.TryGetObservedUnit(candidate.Reactor.UnitSlot, out DclUnitKey observedReactor) ||
                 observedReactor != candidate.Reactor || candidate.ActivationRoll is not null))
                throw new ArgumentException(
                    "Confirmed Reaction execution requires an observed reactor and accepts no pre-supplied activation roll.",
                    nameof(request));
            if (!request.Runtime.Authoring.Reactions.TryGetValue(candidate.ReactionId, out DclReactionDefinition? definition))
                throw new KeyNotFoundException($"Canonical Reaction '{candidate.ReactionId}' is not loaded.");
            _ = request.Runtime.ResolveReactionBinding(candidate.ReactionId);
            DclReactionValidation validation = DclReactionContract.Validate(definition);
            if (!validation.IsValid)
                throw new ArgumentException("The Reaction window references a non-normalized definition.", nameof(request));
            _ = DclReactionContract.ResolveEffectRoute(
                definition,
                declaration.Source,
                candidate.Reactor,
                candidate.TriggerTarget,
                candidate.ExplicitEffectSource,
                candidate.ExplicitEffectTarget);
            bool nativePermitted = definition.Cardinality != DclReactionCardinality.Native ||
                candidate.NativeCardinalityAllows;
            bool reachesActivationRoll = definition.ActivationMode == DclReactionActivationMode.ActivationRoll &&
                candidate.TriggerSatisfied && candidate.Eligible && nativePermitted && candidate.Aware &&
                candidate.CostsAndUsesAvailable;
            if (reachesActivationRoll && candidate.ActivationReferenceScore is null)
                throw new ArgumentException("A reachable ActivationRoll requires its one deterministic reference score.", nameof(request));
            if (definition.ActivationMode != DclReactionActivationMode.ActivationRoll && candidate.ActivationRoll is not null)
                throw new ArgumentException("AutomaticTrigger and SkillResponse cannot own an activation roll.", nameof(request));
        }

        var cardinality = new DclReactionWindowCardinality(declaration.ActionInstanceId);
        var results = new List<DclCanonicalReactionResult>();
        foreach (DclCanonicalReactionCandidate candidate in candidates)
        {
            if (!candidate.Reactor.IsValid || candidate.Reactor.BattleGeneration != declaration.Source.BattleGeneration)
                throw new ArgumentException("Reaction reactor identity is invalid for the outer ActionInstance.", nameof(request));
            if (executionBattle is not null &&
                (!executionBattle.TryGetObservedUnit(candidate.Reactor.UnitSlot, out DclUnitKey observedReactor) ||
                 observedReactor != candidate.Reactor || candidate.ActivationRoll is not null))
                throw new ArgumentException(
                    "Confirmed Reaction execution requires an observed reactor and accepts no pre-supplied activation roll.",
                    nameof(request));
            if (!request.Runtime.Authoring.Reactions.TryGetValue(candidate.ReactionId, out DclReactionDefinition? definition))
                throw new KeyNotFoundException($"Canonical Reaction '{candidate.ReactionId}' is not loaded.");
            DclNativeReactionBinding nativeBinding = request.Runtime.ResolveReactionBinding(candidate.ReactionId);
            bool nativePermitted = definition.Cardinality != DclReactionCardinality.Native ||
                candidate.NativeCardinalityAllows;
            int? activationRoll = candidate.ActivationRoll;
            bool reachesActivationRoll = definition.ActivationMode == DclReactionActivationMode.ActivationRoll &&
                candidate.TriggerSatisfied && candidate.Eligible && nativePermitted && candidate.Aware &&
                candidate.CostsAndUsesAvailable;
            if (executionBattle is not null && reachesActivationRoll)
            {
                activationRoll = executionBattle.ExecutionRandom.Roll3D6(executionBattle.RollIdentity(
                    declaration.ActionInstanceId,
                    declaration.Source,
                    candidate.Reactor,
                    strikeIndex: 0,
                    DclRollSite.ReactionActivation,
                    drawIndex: candidate.NativeOrder));
            }
            else if (definition.ActivationMode != DclReactionActivationMode.ActivationRoll && activationRoll is not null)
            {
                throw new ArgumentException("AutomaticTrigger and SkillResponse cannot own an activation roll.", nameof(request));
            }
            DclReactionActivationResult activation = DclReactionContract.Resolve(
                definition,
                candidate.TriggerSatisfied,
                candidate.Eligible && nativePermitted,
                candidate.Aware,
                candidate.CostsAndUsesAvailable,
                candidate.ActivationReferenceScore,
                activationRoll);
            bool accepted = activation.Accepted && cardinality.TryAccept(
                candidate.Reactor,
                definition,
                candidate.NativeCardinalityAllows);
            DclActionProfile? effectAction = accepted
                ? request.Runtime.ResolveAction(
                    definition.EffectActionId,
                    request.Runtime.Authoring.Actions[definition.EffectActionId].ProfileRevision)
                : null;
            DclCanonicalReactionEffectRoute? effectRoute = accepted
                ? DclReactionContract.ResolveEffectRoute(
                    definition,
                    declaration.Source,
                    candidate.Reactor,
                    candidate.TriggerTarget,
                    candidate.ExplicitEffectSource,
                    candidate.ExplicitEffectTarget)
                : null;
            results.Add(new DclCanonicalReactionResult(
                candidate.NativeOrder,
                candidate.Reactor,
                definition,
                nativeBinding,
                activation,
                accepted,
                effectAction,
                effectRoute));
        }
        return new DclCanonicalReactionWindowResult(
            declaration.ActionInstanceId,
            results,
            results.Where(result => result.CardinalityAccepted).ToArray());
    }
}

internal static class DclCanonicalReactionStateProjection
{
    public static IReadOnlyList<DclCanonicalReactionCandidate> Project(
        DclCanonicalBattleRuntime battle,
        IEnumerable<DclCanonicalReactionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(candidates);
        return candidates.Select(candidate =>
        {
            if (candidate.WeaponEquipmentSlot is not { } equipmentSlot)
                return candidate;
            if (string.IsNullOrWhiteSpace(equipmentSlot))
                throw new ArgumentException("A weapon Reaction candidate requires one exact nonblank equipment slot.", nameof(candidates));
            if (!battle.TryGetObservedUnit(candidate.Reactor.UnitSlot, out DclUnitKey observed) ||
                observed != candidate.Reactor)
                throw new ArgumentException("Reaction-state projection requires a current observed reactor.", nameof(candidates));
            DclStateRegistryTargetSnapshot state = battle.States.CaptureTarget(candidate.Reactor);
            DclCanonicalDefenseStateMechanics mechanics = DclCanonicalCustomStateMechanics.DefenseMechanics(
                candidate.Reactor,
                state.Instances,
                equipmentSlot);
            return mechanics.WeaponReactionsSuppressed
                ? candidate with { Eligible = false }
                : candidate;
        }).ToArray();
    }
}
