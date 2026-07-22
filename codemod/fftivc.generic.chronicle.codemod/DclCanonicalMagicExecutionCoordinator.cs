namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalStatusRiderExecutionRequest(
    int EffectIndex,
    int ResistanceScore,
    bool Immune,
    DclStateRegistry StateRegistry,
    DclCanonicalStateMaterialization StateMaterialization);

internal sealed record DclCanonicalMagicExecutionRequest(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    DclDefenseOption Defense,
    int MagnitudeAttribute,
    DclElementAffinity Affinity,
    DclRational FaithMagnitude,
    bool TargetHasShell,
    int TargetMaxHp,
    bool FireEffect,
    bool OilContributed,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    IReadOnlyList<DclCanonicalStatusRiderExecutionRequest>? StatusRiders = null,
    bool DeclaredTargetHasReflect = false,
    bool ReflectionAlreadyConsumed = false,
    DclTargetCandidate? ReflectedTarget = null,
    int ApplicableDr = 0,
    DclCanonicalInjuryTargetContext? InjuryTargetContext = null,
    bool DirectConcentrationCancellation = false,
    int AuthoredForcedDisplacement = 0,
    int AdditionalMagnitudeIntegerModifier = 0,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0,
    DclCanonicalResourcePoolSnapshot? ResourceTargetPools = null,
    DclCanonicalResourcePoolSnapshot? ResourceSourcePools = null,
    int? ResistanceScore = null,
    bool Immune = false,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    IReadOnlyList<DclDefenseCandidate>? TouchDefenseCandidates = null,
    DclTouchNativeRouteVerdict? TouchRouteVerdict = null,
    DclCanonicalInjuryMovementBranchSet? InjuryMovementBranches = null);

internal sealed record DclCanonicalPublishedMagicExecution(
    DclCanonicalMagicExecutionResult Result,
    DclCanonicalNativeActionApplication Application,
    DclCanonicalAimLifecycleResult? AimRetention = null);

/// <summary>
/// Owns every reachable random site for one direct numeric magic execution, invokes the canonical
/// resolver once, and publishes the immutable native carrier under the same ActionInstance.
/// Deterministic action/unit/item/state inputs remain explicit and are never inferred from draft
/// job content.
/// </summary>
internal static class DclCanonicalMagicExecutionCoordinator
{
    public static DclCanonicalPublishedMagicExecution ResolveAndPublish(
        DclCanonicalBattleRuntime battle,
        DclCanonicalMagicExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        (DclAbilityBinding _, DclActionProfile profile) = battle.Catalog.ResolveAbility(request.AbilityId);
        if (request.ActionInstanceId <= 0 || request.DeclarationRequest.Caster.BattleGeneration != battle.BattleGeneration)
            throw new ArgumentException("The direct-magic request does not belong to this battle generation.", nameof(request));
        if (!battle.TryGetObservedUnit(request.DeclarationRequest.Caster.UnitSlot, out DclUnitKey observedSource) ||
            observedSource != request.DeclarationRequest.Caster ||
            !battle.TryGetObservedUnit(request.Target.Unit.UnitSlot, out DclUnitKey observedDeclaredTarget) ||
            observedDeclaredTarget != request.Target.Unit)
            throw new ArgumentException("The direct-magic source and declared target must be current observed UnitKeys.", nameof(request));
        if (profile.MagnitudeProfile is not (DclDamageMagnitude or DclHealingMagnitude or DclFixedResourceMagnitude) ||
            profile.Effects.Count == 0 || profile.Effects[0].Kind is not
            (DclEffectKind.Damage or DclEffectKind.Healing or DclEffectKind.ResourceChange))
            throw new InvalidOperationException("The direct-magic coordinator requires one numeric Carrier and supported optional status Riders.");
        if (profile.MagnitudeProfile is DclDamageMagnitude)
            DclCanonicalInjuryConsequenceCommitter.ValidateUniversal(battle.Catalog);
        if (profile.DeliveryProfile.Delivery == DclDelivery.ExternalProjectile && request.Defense.Kind == DclDefenseKind.Parry)
            throw new ArgumentException("External Projectile cannot request ordinary Parry.", nameof(request));
        if (profile.DeliveryProfile.Delivery == DclDelivery.Beneficial && request.Defense.Kind != DclDefenseKind.None)
            throw new ArgumentException("Beneficial direct magic cannot consume an active-defense site.", nameof(request));
        if (profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect &&
            (request.Defense.Kind != DclDefenseKind.None || request.ResistanceScore is null))
            throw new ArgumentException("Internal Direct requires no active defense and one exact resistance score.", nameof(request));
        if (profile.DeliveryProfile.Delivery == DclDelivery.Touch &&
            request.Defense != new DclDefenseOption(DclDefenseKind.None, 0, Depletes: false))
            throw new ArgumentException("Touch uses its authored defense-candidate set rather than one magic-defense option.", nameof(request));
        if (profile.DeliveryProfile.Delivery is not
            (DclDelivery.ExternalProjectile or DclDelivery.InternalDirect or DclDelivery.Beneficial or DclDelivery.Touch))
            throw new InvalidOperationException("The direct numeric coordinator supports ExternalProjectile, InternalDirect, Beneficial, or Touch delivery.");
        DclReflectRoute reflectRoute = DclReflectRouting.Resolve(
            profile.TargetProfile.TargetMode,
            profile.DeliveryProfile.Reflectable,
            request.DeclaredTargetHasReflect,
            request.ReflectionAlreadyConsumed,
            request.DeclarationRequest.Caster,
            request.Target.Unit);
        DclTargetCandidate resolutionTarget = reflectRoute.Reflected
            ? request.ReflectedTarget ?? throw new ArgumentException("A reflected execution requires its routed target snapshot.", nameof(request))
            : request.Target;
        if (resolutionTarget.Unit != reflectRoute.FinalTarget)
            throw new ArgumentException("The reflected target snapshot does not match the canonical route.", nameof(request));
        if (!battle.TryGetObservedUnit(resolutionTarget.Unit.UnitSlot, out DclUnitKey observedResolutionTarget) ||
            observedResolutionTarget != resolutionTarget.Unit)
            throw new ArgumentException("The routed resolution target must be a current observed UnitKey.", nameof(request));
        if (battle.States.CaptureTarget(resolutionTarget.Unit).Revision != resolutionTarget.CombatStateRevision)
            throw new ArgumentException(
                "The routed resolution target carries a stale custom-state revision.",
                nameof(request));
        DclDefenseCandidate[] touchDefenseCandidates = (request.TouchDefenseCandidates ?? []).ToArray();
        string[] touchParryKeys = profile.DeliveryProfile.Delivery == DclDelivery.Touch
            ? touchDefenseCandidates
                .Where(candidate => candidate.Kind == DclDefenseKind.Parry && !string.IsNullOrWhiteSpace(candidate.ResourceKey))
                .Select(candidate => candidate.ResourceKey!)
                .Distinct(StringComparer.Ordinal)
                .ToArray()
            : [];
        if (profile.DeliveryProfile.Delivery != DclDelivery.Touch && touchDefenseCandidates.Length != 0)
            throw new ArgumentException("Only Touch execution may supply Touch defense candidates.", nameof(request));
        DclDefenseResourceSnapshot defenseBefore = battle.CaptureDefenseResources(resolutionTarget.Unit, touchParryKeys);
        if (!DclCanonicalBattleRuntime.DefenseResourcesEqual(defenseBefore, resolutionTarget.DefenseResources))
            throw new ArgumentException(
                "The routed resolution target carries a stale or noncanonical defense-resource snapshot.",
                nameof(request));
        resolutionTarget = resolutionTarget with { DefenseResources = defenseBefore };
        if (profile.DeliveryProfile.Delivery == DclDelivery.Touch)
        {
            DclTouchResolution.Validate(profile.DeliveryProfile, touchDefenseCandidates, defenseBefore);
            DclTouchNativeRouteVerdict routeVerdict = request.TouchRouteVerdict ??
                throw new ArgumentException("Touch execution requires its current native range/trajectory verdict.", nameof(request));
            IReadOnlyList<string> routeFailures = DclTouchResolution.RouteFailures(profile, routeVerdict);
            if (routeFailures.Count != 0)
                throw new ArgumentException($"Touch native route is illegal: {string.Join(",", routeFailures)}", nameof(request));
        }
        else
        {
            if (request.TouchRouteVerdict is not null)
                throw new ArgumentException("Only Touch execution may supply a Touch native route verdict.", nameof(request));
            DclMagicDefensePolicy.Validate(profile.DeliveryProfile, request.Defense, defenseBefore);
        }
        if (request.TargetMaxHp < 1 || resolutionTarget.CurrentHp > request.TargetMaxHp)
            throw new ArgumentOutOfRangeException(nameof(request), "The routed target HP snapshot is outside MaxHP.");
        if (profile.MagnitudeProfile is DclDamageMagnitude &&
            request.InjuryTargetContext is { } injuryTimelineContext &&
            injuryTimelineContext.Charging != battle.IsCharging(resolutionTarget.Unit))
            throw new ArgumentException(
                "Direct-magic Injury Charging snapshot diverges from the canonical timeline before RNG.", nameof(request));
        request.InjuryMovementBranches?.Validate(resolutionTarget.Unit);
        if (profile.MagnitudeProfile is DclFixedResourceMagnitude)
        {
            DclCanonicalResourcePoolSnapshot targetPools = request.ResourceTargetPools ??
                throw new ArgumentException("ResourceChange coordination requires exact routed-target pools.", nameof(request));
            DclCanonicalResourcePoolSnapshot sourcePools = request.ResourceSourcePools ??
                throw new ArgumentException("ResourceChange coordination requires exact source pools.", nameof(request));
            if (targetPools.CurrentHp != resolutionTarget.CurrentHp || targetPools.MaxHp != request.TargetMaxHp ||
                sourcePools.CurrentHp != request.CurrentHpAtResolution || sourcePools.CurrentMp != request.CurrentMpAtResolution)
                throw new ArgumentException("ResourceChange pools must match the routed target and payer snapshots.", nameof(request));
        }

        DclCanonicalActionStateProjection.RequireActionLegal(
            DclCanonicalActionStateProjection.EvaluateTaunt(
                battle,
                request.DeclarationRequest.Caster,
                profile.TimingProfile.ConsumesAction,
                isUniversalNormalAttack: false,
                request.Target.Unit,
                normalAttackTargetLegal: false));

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(
            request.DeclarationRequest,
            request.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical direct-magic declaration failed: {string.Join(",", attempt.Failures)}");
        DclCanonicalReactionWindowRequest reactionWindow =
            DclCanonicalReactionWindow.ConfirmedRequest(battle, request.ReactionCandidates);
        DclCanonicalReactionWindow.Preflight(attempt.Declaration, reactionWindow);

        DclCanonicalStatusRiderExecutionRequest[] riderRequests = (request.StatusRiders ?? []).ToArray();
        int[] expectedRiderIndexes = Enumerable.Range(1, profile.Effects.Count - 1).ToArray();
        if (riderRequests.Select(rider => rider.EffectIndex).Distinct().Count() != riderRequests.Length ||
            !riderRequests.Select(rider => rider.EffectIndex).Order().SequenceEqual(expectedRiderIndexes))
            throw new ArgumentException("Status-rider requests must match every normalized Rider effect index exactly.", nameof(request));
        var riderDefinitions = new Dictionary<int, DclStateDefinition>();
        foreach (DclCanonicalStatusRiderExecutionRequest rider in riderRequests)
        {
            DclEffectProfile effect = profile.Effects[rider.EffectIndex];
            if (effect is not { Role: DclEffectRole.Rider, Kind: DclEffectKind.StatusApplication, ReferencedStateKind: { } stateKind } ||
                !battle.Catalog.Authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition) ||
                rider.StateRegistry.BattleGeneration != battle.BattleGeneration ||
                !StringComparer.Ordinal.Equals(rider.StateMaterialization.Payload.SchemaId, definition.PayloadSchema))
                throw new ArgumentException("A status-rider request does not match its bound state definition.", nameof(request));
            if (definition.ResistanceGate == DclStateResistanceGate.Explicit)
                throw new InvalidOperationException(
                    $"Status Rider {rider.EffectIndex} uses Explicit resistance and requires its named mechanism owner.");
            riderDefinitions.Add(rider.EffectIndex, definition);
        }
        bool payable = DclMagicResources.CanPayFullCost(
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            attempt.CostCommitment);
        int? casterRoll = null;
        int? touchAttackRoll = null;
        int? defenseRoll = null;
        int? targetResistanceRoll = null;
        IReadOnlyList<int>? magnitudeDice = null;
        DclInjuryConsequenceInput? injuryConsequenceInput = null;
        var riderInputs = new List<DclCanonicalStatusRiderInput>(riderRequests.Length);

        if (payable)
        {
            if (profile.DeliveryProfile.Delivery == DclDelivery.Touch)
            {
                touchAttackRoll = battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                    request.ActionInstanceId,
                    request.DeclarationRequest.Caster,
                    resolutionTarget.Unit,
                    strikeIndex: 0,
                    DclRollSite.Attack,
                    drawIndex: 0));
                DclTouchEvaluationResult touchPlan = DclTouchResolution.Evaluate(
                    profile.DeliveryProfile,
                    request.BaseSpellScore,
                    touchDefenseCandidates,
                    defenseBefore);
                if (DclTouchResolution.RequiresDefenseAttempt(
                        request.BaseSpellScore,
                        touchAttackRoll.Value,
                        touchPlan))
                    defenseRoll = battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                        request.ActionInstanceId,
                        request.DeclarationRequest.Caster,
                        resolutionTarget.Unit,
                        strikeIndex: 0,
                        DclRollSite.ActiveDefense,
                        drawIndex: 0));
            }
            else
            {
                casterRoll = battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                    request.ActionInstanceId,
                    request.DeclarationRequest.Caster,
                    target: null,
                    strikeIndex: 0,
                    DclRollSite.Casting,
                    drawIndex: 0));
                DclSpellGateResult castingGate = DclSpellResolution.ClassifySharedRoll(
                    casterRoll.Value,
                    request.BaseSpellScore,
                    request.TargetSpellScore);
                if (profile.DeliveryProfile.Delivery == DclDelivery.ExternalProjectile &&
                    castingGate.TargetSucceeded && !castingGate.TargetCritical &&
                    request.Defense.Kind != DclDefenseKind.None)
                    defenseRoll = battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                        request.ActionInstanceId,
                        request.DeclarationRequest.Caster,
                        resolutionTarget.Unit,
                        strikeIndex: 0,
                        DclRollSite.ActiveDefense,
                        drawIndex: 0));
                if (profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect &&
                    castingGate.TargetSucceeded && !request.Immune)
                    targetResistanceRoll = battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                        request.ActionInstanceId,
                        request.DeclarationRequest.Caster,
                        resolutionTarget.Unit,
                        strikeIndex: 0,
                        DclRollSite.Resistance,
                        drawIndex: 0));
            }
            DclMagicDeliveryResult delivery = profile.DeliveryProfile.Delivery switch
            {
                DclDelivery.ExternalProjectile => DclSpellResolution.ResolveExternal(
                    casterRoll ?? throw new InvalidOperationException("External Projectile lost its casting draw."),
                    request.BaseSpellScore,
                    request.TargetSpellScore,
                    request.Defense,
                    defenseRoll),
                DclDelivery.Beneficial => DclSpellResolution.ResolveBeneficial(
                    casterRoll ?? throw new InvalidOperationException("Beneficial delivery lost its casting draw."),
                    request.BaseSpellScore,
                    request.TargetSpellScore),
                DclDelivery.InternalDirect => DclSpellResolution.ResolveInternal(
                    casterRoll ?? throw new InvalidOperationException("Internal Direct lost its casting draw."),
                    request.BaseSpellScore,
                    request.TargetSpellScore,
                    request.ResistanceScore!.Value,
                    targetResistanceRoll,
                    request.Immune),
                DclDelivery.Touch => DclTouchResolution.ToMagicDelivery(DclTouchResolution.Resolve(
                    profile.DeliveryProfile,
                    request.BaseSpellScore,
                    touchAttackRoll!.Value,
                    touchDefenseCandidates,
                    defenseBefore,
                    defenseRoll)),
                _ => throw new InvalidOperationException("The direct numeric coordinator supports ExternalProjectile, InternalDirect, Beneficial, or Touch delivery."),
            };

            if (delivery.Delivered)
            {
                (DclDiceExpression expression, DclRollSite rollSite) = BuildRolledExpression(
                    profile,
                    request.MagnitudeAttribute,
                    request.AdditionalMagnitudeIntegerModifier,
                    delivery.Outcome);
                magnitudeDice = battle.ExecutionRandom.RollD6Pool(
                    battle.RollIdentity(
                        request.ActionInstanceId,
                        request.DeclarationRequest.Caster,
                        resolutionTarget.Unit,
                        strikeIndex: 0,
                        rollSite,
                        drawIndex: 0),
                    expression.Dice);

                if (profile.MagnitudeProfile is DclDamageMagnitude damageMagnitude)
                {
                    int rawMagnitude = Math.Max(0, DclInjury.RollDamage(expression, magnitudeDice));
                    DclInjuryResult baseInjury = DclCanonicalMagnitude.ResolveMagicalBaseInjury(
                        rawMagnitude,
                        damageMagnitude.DamageType,
                        request.ApplicableDr,
                        profile.DeliveryProfile);
                    DclMagicalEffectResult routed = DclMagicalEffect.ResolveInjuryOrAbsorb(
                        baseInjury.Injury,
                        request.Affinity,
                        request.FaithMagnitude,
                        profile.DeliveryProfile.ShellSensitive,
                        request.TargetHasShell,
                        resolutionTarget.CurrentHp,
                        request.TargetMaxHp,
                        request.FireEffect,
                        request.OilContributed);
                    if (routed.Route == DclMagicalEffectRoute.Injury)
                    {
                        DclCanonicalInjuryTargetContext consequenceContext = request.InjuryTargetContext ??
                            throw new ArgumentException("A routed magical Injury requires its deterministic target context.", nameof(request));
                        DclInjuryResult finalInjury = baseInjury with { Injury = routed.FinalInjury };
                        bool targetSurvivesInjury = resolutionTarget.CurrentHp > finalInjury.Injury;
                        int requestedInjuryMovement = checked(
                            request.AuthoredForcedDisplacement + DclInjury.CriticalKnockbackTiles(
                                delivery.Outcome == DclMagicDeliveryOutcome.CriticalDelivered,
                                targetSurvivesInjury,
                                damageMagnitude.DamageType,
                                finalInjury.RolledDamage,
                                finalInjury.PenetratingDamage,
                                consequenceContext.TargetSt));
                        DclCanonicalForcedMovementResult? injuryMovement = requestedInjuryMovement > 0
                            ? request.InjuryMovementBranches?.Resolve(
                                resolutionTarget.Unit,
                                targetKo: !targetSurvivesInjury,
                                requestedInjuryMovement) ?? throw new InvalidOperationException(
                                "Direct magical Injury displacement requires its frozen native map branch.")
                            : null;
                        DclCanonicalInjuryConsequenceRolls consequenceRolls =
                            DclCanonicalInjuryRandomPlanner.PlanRolls(
                                battle,
                                request.ActionInstanceId,
                                request.DeclarationRequest.Caster,
                                resolutionTarget.Unit,
                                strikeIndex: 0,
                                resolutionTarget.CurrentHp,
                                finalInjury,
                                damageMagnitude.DamageType,
                                delivery.Outcome == DclMagicDeliveryOutcome.CriticalDelivered,
                                consequenceContext,
                                request.DirectConcentrationCancellation,
                                request.AuthoredForcedDisplacement,
                                injuryMovement?.MovedTiles ?? 0);
                        injuryConsequenceInput = DclCanonicalInjuryRandomPlanner.BuildInput(
                            consequenceContext,
                            consequenceRolls);
                    }
                }
            }

            foreach (DclCanonicalStatusRiderExecutionRequest rider in riderRequests)
            {
                DclStateResistanceGate resistanceGate = riderDefinitions[rider.EffectIndex].ResistanceGate;
                int? resistanceRoll = delivery.Delivered && !rider.Immune &&
                    resistanceGate is DclStateResistanceGate.SuccessRoll or DclStateResistanceGate.QuickContest
                    ? battle.ExecutionRandom.Roll3D6(battle.RollIdentity(
                        request.ActionInstanceId,
                        request.DeclarationRequest.Caster,
                        resolutionTarget.Unit,
                        strikeIndex: 0,
                        DclRollSite.Resistance,
                        drawIndex: rider.EffectIndex))
                    : null;
                riderInputs.Add(new DclCanonicalStatusRiderInput(
                    rider.EffectIndex,
                    rider.ResistanceScore,
                    resistanceRoll,
                    rider.Immune,
                    rider.StateRegistry,
                    rider.StateMaterialization));
            }
        }
        else
        {
            riderInputs.AddRange(riderRequests.Select(rider => new DclCanonicalStatusRiderInput(
                rider.EffectIndex,
                rider.ResistanceScore,
                ResistanceRoll: null,
                rider.Immune,
                rider.StateRegistry,
                rider.StateMaterialization)));
        }

        var input = new DclCanonicalMagicExecutionInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.ActionInstanceId,
            reflectRoute.Reflected ? request.Target : resolutionTarget,
            request.BaseSpellScore,
            request.TargetSpellScore,
            casterRoll,
            request.Defense,
            defenseRoll,
            request.MagnitudeAttribute,
            magnitudeDice,
            request.Affinity,
            request.FaithMagnitude,
            request.TargetHasShell,
            request.TargetMaxHp,
            request.FireEffect,
            request.OilContributed,
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            riderInputs,
            request.DeclaredTargetHasReflect,
            request.ReflectionAlreadyConsumed,
            reflectRoute.Reflected ? resolutionTarget : null,
            request.ApplicableDr,
            injuryConsequenceInput,
            request.AdditionalMagnitudeIntegerModifier,
            DclCanonicalInjuryStateCommitContext.FromBattle(battle, resolutionTarget.Unit),
            request.ResourceTargetPools,
            request.ResourceSourcePools,
            request.ResistanceScore,
            targetResistanceRoll,
            request.Immune,
            reactionWindow,
            battle,
            request.AimRetentionModifier,
            request.AimRetentionStatePenaltyMagnitude,
            touchAttackRoll,
            touchDefenseCandidates,
            request.TouchRouteVerdict,
            InjuryMovementBranches: request.InjuryMovementBranches);
        DclCanonicalMagicExecutionResult result = DclCanonicalMagicExecutor.Resolve(battle.Catalog, input);
        if (result.Outcome != DclCastingOutcome.ResourceFailure)
        {
            DclDefenseResourceSnapshot finalDefense = result.FinalDefenseResources ??
                throw new InvalidOperationException("Confirmed direct magic lost its final defense-resource snapshot.");
            if (!DclCanonicalBattleRuntime.DefenseResourcesEqual(defenseBefore, finalDefense))
                battle.CommitDefenseResourcesBatch([
                    new DclCanonicalDefenseResourceCommit(resolutionTarget.Unit, defenseBefore, finalDefense),
                ]);
        }
        DclCanonicalNativeActionApplication application =
            DclCanonicalNativeExecutionPublisher.PublishMagic(
                battle,
                request.AbilityId,
                request.DeclarationRequest.Caster,
                result);
        return new DclCanonicalPublishedMagicExecution(result, application, result.AimRetention);
    }

    private static (DclDiceExpression Expression, DclRollSite RollSite) BuildRolledExpression(
        DclActionProfile profile,
        int magnitudeAttribute,
        int additionalMagnitudeIntegerModifier,
        DclMagicDeliveryOutcome deliveryOutcome)
    {
        return profile.MagnitudeProfile switch
        {
            DclDamageMagnitude damage => (
                DclCanonicalMagnitude.BuildDiceExpression(
                    magnitudeAttribute,
                    damage.Basis,
                    damage.FixedExpression,
                    checked(damage.IntegerModifier + additionalMagnitudeIntegerModifier),
                    damage.WholeDiceModifier),
                DclRollSite.DamageDie),
            DclHealingMagnitude healing => (
                CriticalHealingExpression(profile, magnitudeAttribute, additionalMagnitudeIntegerModifier, healing, deliveryOutcome),
                DclRollSite.HealingDie),
            DclFixedResourceMagnitude resource when DclDiceExpression.TryParseAuthored(
                resource.Expression,
                out DclDiceExpression expression) => (expression, DclRollSite.ResourceMagnitudeDie),
            DclFixedResourceMagnitude => throw new InvalidOperationException(
                "ResourceChange magnitude does not use the exact Xd6+Y grammar."),
            _ => throw new InvalidOperationException("Direct numeric magic requires Damage, Healing, or ResourceChange magnitude."),
        };
    }

    private static DclDiceExpression CriticalHealingExpression(
        DclActionProfile profile,
        int magnitudeAttribute,
        int additionalMagnitudeIntegerModifier,
        DclHealingMagnitude healing,
        DclMagicDeliveryOutcome deliveryOutcome)
    {
        DclDiceExpression normal = DclCanonicalMagnitude.BuildDiceExpression(
            magnitudeAttribute,
            healing.Basis,
            healing.FixedExpression,
            checked(healing.IntegerModifier + additionalMagnitudeIntegerModifier),
            healing.WholeDiceModifier);
        if (deliveryOutcome != DclMagicDeliveryOutcome.CriticalDelivered ||
            profile.CriticalProfile.SuccessEffect != DclCriticalSuccessEffect.MaximizeOneHealingDie)
            return normal;
        DclCriticalHealingExpression critical = DclMagicMagnitude.CriticalHealingExpression(normal);
        return new DclDiceExpression(critical.DiceToRoll, critical.Adds);
    }
}
