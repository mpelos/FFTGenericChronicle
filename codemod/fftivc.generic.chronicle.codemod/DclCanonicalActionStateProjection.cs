namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalPhysicalStatePlan(
    bool Legal,
    string Reason,
    long SourceStateRevision,
    IReadOnlyDictionary<DclUnitKey, long> TargetStateRevisions,
    IReadOnlyList<DclCanonicalPhysicalStrikeInput> Strikes);

internal sealed record DclCanonicalPhysicalPlanningResult(
    DclCanonicalPhysicalStatePlan StatePlan,
    DclCanonicalPhysicalEvaluationResult? Evaluation,
    IReadOnlyList<DclCanonicalPhysicalPlayerForecast> PlayerForecast,
    IReadOnlyList<DclCanonicalPhysicalAiProjection> AiProjection,
    DclCanonicalProtectionRedirectPlan? ProtectionRedirect = null);

/// <summary>
/// Applies battle-owned custom-state mechanics at the shared deterministic boundary used before
/// physical forecast/AI or confirmed execution. Native status arrays remain owned by the full
/// synchronized unit snapshot; this projector consumes only revisioned DCL registry instances.
/// </summary>
internal static class DclCanonicalActionStateProjection
{
    public static DclCanonicalPhysicalStatePlan PlanPhysical(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        DclTimingProfile timing,
        string sourceWeaponSlot,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> strikes,
        bool isUniversalNormalAttack,
        DclUnitKey? selectedTarget,
        bool normalAttackTargetLegal)
        => PlanPhysical(
            battle,
            source,
            timing,
            strikes.Select(strike => strike.StrikeIndex).Distinct().ToDictionary(index => index, _ => sourceWeaponSlot),
            strikes,
            isUniversalNormalAttack,
            selectedTarget,
            normalAttackTargetLegal);

    public static DclCanonicalPhysicalStatePlan PlanPhysical(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        DclTimingProfile timing,
        IReadOnlyDictionary<int, string> sourceWeaponSlots,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> strikes,
        bool isUniversalNormalAttack,
        DclUnitKey? selectedTarget,
        bool normalAttackTargetLegal)
    {
        ArgumentNullException.ThrowIfNull(timing);
        ValidateStrikeSlots(sourceWeaponSlots, strikes);
        DclStateRegistryTargetSnapshot sourceState = battle.States.CaptureTarget(source);
        Dictionary<DclUnitKey, DclStateRegistryTargetSnapshot> targetStates =
            strikes.Select(strike => strike.Target).Distinct().ToDictionary(
            target => target,
            target => battle.States.CaptureTarget(target));
        var targetRevisions = targetStates.ToDictionary(pair => pair.Key, pair => pair.Value.Revision);
        DclCanonicalTauntActionLegality taunt = EvaluateTaunt(
            battle,
            source,
            sourceState,
            timing.ConsumesAction,
            isUniversalNormalAttack,
            selectedTarget,
            normalAttackTargetLegal);
        if (taunt.StateShouldEnd)
            throw new InvalidOperationException(
                "A stale Taunt reached action planning after its provocateur lifecycle should have removed it.");
        if (!taunt.Legal)
            return new(false, taunt.Reason, sourceState.Revision, targetRevisions, []);
        foreach (string sourceWeaponSlot in sourceWeaponSlots.Values.Distinct(StringComparer.Ordinal))
        {
            DclCanonicalDefenseStateMechanics sourceMechanics =
                DclCanonicalCustomStateMechanics.DefenseMechanics(source, sourceState.Instances, sourceWeaponSlot);
            if (sourceMechanics.WeaponAttackSuppressed)
                return new(false, $"weapon-bound-suppresses-attack:{sourceWeaponSlot}",
                    sourceState.Revision, targetRevisions, []);
        }
        return new(
            true,
            "legal",
            sourceState.Revision,
            targetRevisions,
            ProjectPhysicalStrikes(source, sourceState, targetStates, sourceWeaponSlots, strikes));
    }

    public static DclCanonicalTauntActionLegality EvaluateTaunt(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        bool consumesAction,
        bool isUniversalNormalAttack,
        DclUnitKey? selectedTarget,
        bool normalAttackTargetLegal)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (!source.IsValid || source.BattleGeneration != battle.BattleGeneration ||
            !battle.TryGetObservedUnit(source.UnitSlot, out DclUnitKey observedSource) || observedSource != source)
            throw new ArgumentException("Action-state projection requires the current observed source.", nameof(source));
        DclStateRegistryTargetSnapshot sourceState = battle.States.CaptureTarget(source);
        return EvaluateTaunt(
            battle,
            source,
            sourceState,
            consumesAction,
            isUniversalNormalAttack,
            selectedTarget,
            normalAttackTargetLegal);
    }

    private static DclCanonicalTauntActionLegality EvaluateTaunt(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        DclStateRegistryTargetSnapshot sourceState,
        bool consumesAction,
        bool isUniversalNormalAttack,
        DclUnitKey? selectedTarget,
        bool normalAttackTargetLegal)
    {
        DclTauntStatePayload? taunt = sourceState.Instances
            .Where(instance => StringComparer.Ordinal.Equals(instance.Kind, "taunt"))
            .Select(instance => instance.Payload as DclTauntStatePayload ??
                throw new InvalidOperationException("Taunt requires its typed mechanical payload."))
            .SingleOrDefault();
        bool provocateurActive = taunt is null ||
            battle.TryGetObservedUnit(taunt.Provocateur.UnitSlot, out DclUnitKey observedProvocateur) &&
            observedProvocateur == taunt.Provocateur;
        return DclCanonicalCustomStateMechanics.EvaluateTauntAction(
            source,
            sourceState.Instances,
            consumesAction,
            isUniversalNormalAttack,
            selectedTarget,
            provocateurActive,
            normalAttackTargetLegal);
    }

    public static void RequireActionLegal(DclCanonicalTauntActionLegality legality)
    {
        ArgumentNullException.ThrowIfNull(legality);
        if (legality.StateShouldEnd)
            throw new InvalidOperationException(
                "A stale Taunt reached action resolution after its provocateur lifecycle should have removed it.");
        if (!legality.Legal)
            throw new InvalidOperationException($"Action is illegal under Taunt: {legality.Reason}.");
    }

    public static IReadOnlyList<DclCanonicalPhysicalStrikeInput> ProjectPhysicalStrikes(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        string sourceWeaponSlot,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> strikes)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(strikes);
        DclStateRegistryTargetSnapshot sourceState = battle.States.CaptureTarget(source);
        Dictionary<DclUnitKey, DclStateRegistryTargetSnapshot> targetStates =
            strikes.Select(strike => strike.Target).Distinct().ToDictionary(
                target => target,
                target => battle.States.CaptureTarget(target));
        return ProjectPhysicalStrikes(
            source,
            sourceState,
            targetStates,
            strikes.Select(strike => strike.StrikeIndex).Distinct().ToDictionary(index => index, _ => sourceWeaponSlot),
            strikes);
    }

    public static IReadOnlyList<DclCanonicalPhysicalStrikeInput> ProjectPhysicalStrikes(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        IReadOnlyDictionary<int, string> sourceWeaponSlots,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> strikes)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(strikes);
        ValidateStrikeSlots(sourceWeaponSlots, strikes);
        DclStateRegistryTargetSnapshot sourceState = battle.States.CaptureTarget(source);
        Dictionary<DclUnitKey, DclStateRegistryTargetSnapshot> targetStates =
            strikes.Select(strike => strike.Target).Distinct().ToDictionary(
                target => target,
                target => battle.States.CaptureTarget(target));
        return ProjectPhysicalStrikes(source, sourceState, targetStates, sourceWeaponSlots, strikes);
    }

    private static IReadOnlyList<DclCanonicalPhysicalStrikeInput> ProjectPhysicalStrikes(
        DclUnitKey source,
        DclStateRegistryTargetSnapshot sourceState,
        IReadOnlyDictionary<DclUnitKey, DclStateRegistryTargetSnapshot> targetStates,
        IReadOnlyDictionary<int, string> sourceWeaponSlots,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> strikes)
    {
        ValidateStrikeSlots(sourceWeaponSlots, strikes);
        IReadOnlyDictionary<int, DclCanonicalDefenseStateMechanics> sourceMechanics = sourceWeaponSlots.ToDictionary(
            pair => pair.Key,
            pair => DclCanonicalCustomStateMechanics.DefenseMechanics(source, sourceState.Instances, pair.Value));
        DclCanonicalDefenseStateMechanics? suppressed = sourceMechanics.Values.FirstOrDefault(mechanics => mechanics.WeaponAttackSuppressed);
        if (suppressed is not null)
        {
            string slot = sourceWeaponSlots[sourceMechanics.Single(pair => ReferenceEquals(pair.Value, suppressed)).Key];
            throw new InvalidOperationException($"Weapon Bound suppresses attacks from slot '{slot}'.");
        }

        var targetMechanics = new Dictionary<(DclUnitKey Target, string Slot), DclCanonicalDefenseStateMechanics>();
        DclCanonicalDefenseStateMechanics Mechanics(DclUnitKey target, string slot)
        {
            var key = (target, slot);
            if (targetMechanics.TryGetValue(key, out DclCanonicalDefenseStateMechanics? cached))
                return cached;
            if (!targetStates.TryGetValue(target, out DclStateRegistryTargetSnapshot? state))
                throw new InvalidOperationException("Physical state plan lost a target state snapshot.");
            DclCanonicalDefenseStateMechanics projected =
                DclCanonicalCustomStateMechanics.DefenseMechanics(target, state.Instances, slot);
            targetMechanics.Add(key, projected);
            return projected;
        }

        return strikes.Select(strike =>
        {
            DclCanonicalDefenseStateMechanics body = Mechanics(strike.Target, "body");
            DclDefenseCandidate[] defenses = strike.DefenseCandidates.Select(candidate => candidate.Kind switch
            {
                DclDefenseKind.Parry when string.IsNullOrWhiteSpace(candidate.ResourceKey) =>
                    throw new InvalidOperationException("A Parry candidate requires its exact equipment/limb slot."),
                DclDefenseKind.Parry => ProjectParry(candidate, Mechanics(strike.Target, candidate.ResourceKey!)),
                DclDefenseKind.Block => candidate with
                {
                    Legal = candidate.Legal && !body.BlockSuppressed,
                    Score = checked(candidate.Score + body.BlockModifier),
                    Reason = body.BlockSuppressed ? "guard-broken-suppresses-block" : candidate.Reason,
                },
                _ => candidate,
            }).ToArray();
            return strike with
            {
                EffectiveAttackSkill = checked(strike.EffectiveAttackSkill + sourceMechanics[strike.StrikeIndex].WeaponSkillModifier),
                ApplicableDr = Math.Max(0, checked(strike.ApplicableDr + body.DrModifier)),
                DefenseCandidates = defenses,
            };
        }).ToArray();
    }

    private static void ValidateStrikeSlots(
        IReadOnlyDictionary<int, string> sourceWeaponSlots,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> strikes)
    {
        ArgumentNullException.ThrowIfNull(sourceWeaponSlots);
        ArgumentNullException.ThrowIfNull(strikes);
        int[] indexes = strikes.Select(strike => strike.StrikeIndex).Distinct().Order().ToArray();
        if (indexes.Length == 0 || sourceWeaponSlots.Count != indexes.Length ||
            !sourceWeaponSlots.Keys.Order().SequenceEqual(indexes) ||
            sourceWeaponSlots.Values.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Physical state projection requires one source weapon/hand slot per Strike index.", nameof(sourceWeaponSlots));
    }

    private static DclDefenseCandidate ProjectParry(
        DclDefenseCandidate candidate,
        DclCanonicalDefenseStateMechanics mechanics)
    {
        bool suppressed = mechanics.ParrySuppressed;
        return candidate with
        {
            Legal = candidate.Legal && !suppressed,
            Score = checked(candidate.Score - mechanics.ParryPenaltyMagnitude),
            Reason = suppressed ? "weapon-bound-suppresses-parry" : candidate.Reason,
        };
    }
}

internal static class DclCanonicalPhysicalPlanningCoordinator
{
    public static DclCanonicalPhysicalPlanningResult Evaluate(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        string sourceWeaponSlot,
        DclCanonicalPhysicalEvaluationInput input,
        bool isUniversalNormalAttack = false,
        DclUnitKey? selectedTarget = null,
        bool normalAttackTargetLegal = true,
        DclCanonicalProtectionRedirectPlan? protectionRedirect = null)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = battle.Catalog.ResolveAbility(input.AbilityId);
        if (!battle.TryGetObservedUnit(source.UnitSlot, out DclUnitKey observedSource) || observedSource != source ||
            input.Targets.Any(target =>
                !battle.TryGetObservedUnit(target.Target.UnitSlot, out DclUnitKey observedTarget) ||
                observedTarget != target.Target))
            throw new ArgumentException("Physical planning requires current observed source and target UnitKeys.", nameof(input));
        profile = DclCanonicalPhysicalStrikeCardinality.ResolveEffectiveProfile(
            binding,
            profile,
            input.Targets,
            input.Strikes);
        DclCanonicalPhysicalStrikeWeapon[] strikeWeapons = DclCanonicalPhysicalStrikeWeapons.Normalize(
            profile.TransactionProfile.StrikeCount,
            input.WeaponItemId,
            sourceWeaponSlot,
            input.StrikeWeapons);
        IReadOnlyDictionary<int, DclWeaponMetadata> weapons = DclCanonicalPhysicalStrikeWeapons.ResolveMetadata(
            battle.Catalog,
            strikeWeapons);
        DclCanonicalPhysicalStatePlan statePlan = DclCanonicalActionStateProjection.PlanPhysical(
            battle,
            source,
            profile.TimingProfile,
            strikeWeapons.ToDictionary(weapon => weapon.StrikeIndex, weapon => weapon.WeaponResourceKey),
            input.Strikes,
            isUniversalNormalAttack,
            selectedTarget,
            normalAttackTargetLegal);
        if (!statePlan.Legal)
            return new(statePlan, null, [], []);
        ValidateProtectionRedirect(source, selectedTarget, profile, input, statePlan.Strikes, protectionRedirect);
        foreach (DclCanonicalPhysicalStrikeWeapon strikeWeapon in strikeWeapons)
        {
            DclWeaponMetadata weapon = weapons[strikeWeapon.StrikeIndex];
            if (!battle.WeaponState(
                    source,
                    strikeWeapon.WeaponResourceKey,
                    weapon.Balance,
                    weapon.Readiness).CanAttack)
            {
                DclCanonicalPhysicalStatePlan unavailable = statePlan with
                {
                    Legal = false,
                    Reason = $"weapon-unready:{strikeWeapon.WeaponResourceKey}",
                    Strikes = [],
                };
                return new(unavailable, null, [], []);
            }
        }
        DclCanonicalPhysicalEvaluationResult evaluation = DclCanonicalPhysicalEvaluation.Evaluate(
            battle.Catalog,
            input with { Strikes = statePlan.Strikes });
        return new(
            statePlan,
            evaluation,
            DclCanonicalPhysicalEvaluation.ProjectPlayer(evaluation),
            DclCanonicalPhysicalEvaluation.ProjectAi(evaluation),
            protectionRedirect);
    }

    private static void ValidateProtectionRedirect(
        DclUnitKey source,
        DclUnitKey? selectedTarget,
        DclActionProfile profile,
        DclCanonicalPhysicalEvaluationInput input,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> stateProjectedStrikes,
        DclCanonicalProtectionRedirectPlan? protectionRedirect)
    {
        if (protectionRedirect is null)
            return;
        DclCanonicalProtectionRedirectCandidate candidate = protectionRedirect.Candidate;
        DclUnitKey finalTarget = protectionRedirect.Resolution.FinalTarget;
        if (profile.DeliveryProfile.Delivery != DclDelivery.PhysicalAttack ||
            candidate.Delivery != DclDelivery.PhysicalAttack ||
            candidate.Attacker != source ||
            selectedTarget is not { } declaredTarget ||
            candidate.DeclaredTarget != declaredTarget)
            throw new ArgumentException(
                "Physical protection planning must preserve the declared attacker/target and PhysicalAttack delivery.",
                nameof(protectionRedirect));
        if (input.Targets.Count != 1 ||
            input.TargetContexts?.Count != 1 ||
            input.Targets[0].Target != finalTarget ||
            input.TargetContexts[0].Target != finalTarget ||
            stateProjectedStrikes.Count == 0 ||
            stateProjectedStrikes.Any(strike => strike.Target != finalTarget))
            throw new ArgumentException(
                "Physical protection planning must evaluate every target context and Strike against the planned final target.",
                nameof(protectionRedirect));
    }
}
