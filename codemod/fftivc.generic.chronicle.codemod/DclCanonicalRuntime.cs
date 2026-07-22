namespace fftivc.generic.chronicle.codemod;

internal sealed class DclCanonicalRuntimeCatalog
{
    private readonly Dictionary<int, DclCanonicalActionFamily> _abilityFamilies = [];

    public DclCanonicalRuntimeCatalog(
        DclAuthoringRegistry authoring,
        DclItemMetadataRegistry items,
        DclAbilityBindingRegistry abilities,
        DclNativeReactionBindingRegistry reactionBindings,
        DclCanonicalNativePolicyTicketTemplateRegistry? policyTicketTemplates = null,
        DclStatePresentationProfileRegistry? statePresentations = null)
    {
        ArgumentNullException.ThrowIfNull(authoring);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(reactionBindings);
        Authoring = authoring;
        Items = items;
        Abilities = abilities;
        ReactionBindings = reactionBindings;
        PolicyTicketTemplates = policyTicketTemplates ?? new DclCanonicalNativePolicyTicketTemplateRegistry([]);
        StatePresentations = statePresentations ?? new DclStatePresentationProfileRegistry();
        DclAuthoringValidation references = authoring.ValidateReferences();
        if (!references.IsValid)
            throw new ArgumentException($"Canonical authoring references are incomplete: {string.Join("; ", references.Findings)}", nameof(authoring));
        if (statePresentations is not null)
        {
            DclStatePresentationValidation presentationReferences =
                statePresentations.ValidateStateReferences(authoring.States);
            if (!presentationReferences.IsValid)
                throw new ArgumentException(
                    $"Canonical state presentation references are incomplete: {string.Join("; ", presentationReferences.Findings)}",
                    nameof(statePresentations));
        }
        foreach (DclAbilityBinding binding in abilities.Bindings.Values)
        {
            if (!authoring.Actions.TryGetValue(binding.ActionId, out DclActionProfile? profile) ||
                profile.ProfileRevision != binding.ProfileRevision)
                throw new ArgumentException($"Ability {binding.AbilityId} lost its normalized action/revision binding.", nameof(abilities));
            DclCanonicalActionCapability capability =
                DclCanonicalActionCapabilityResolver.Resolve(binding, profile, authoring);
            if (!capability.Supported)
                throw new ArgumentException(
                    $"Ability {binding.AbilityId} action '{binding.ActionId}' has no complete canonical family: " +
                    string.Join("; ", capability.Failures),
                    nameof(abilities));
            _abilityFamilies.Add(binding.AbilityId, capability.Family);
        }
        foreach (DclStateDefinition state in authoring.States.Values)
        {
            if (state.TickProfile is not { } tick) continue;
            if (!abilities.Bindings.TryGetValue(tick.EffectAbilityId, out DclAbilityBinding? binding) ||
                !StringComparer.Ordinal.Equals(binding.ActionId, tick.EffectActionId))
                throw new ArgumentException(
                    $"Periodic state '{state.Kind}' requires native ability {tick.EffectAbilityId} bound to action '{tick.EffectActionId}'.",
                    nameof(abilities));
        }
        DclNativeReactionBindingValidation reactionCoverage = reactionBindings.Audit(authoring);
        if (!reactionCoverage.IsValid)
            throw new ArgumentException(
                $"Canonical Reaction bindings are incomplete: {string.Join("; ", reactionCoverage.Findings)}",
                nameof(reactionBindings));
    }

    public DclAuthoringRegistry Authoring { get; }
    public DclItemMetadataRegistry Items { get; }
    public DclAbilityBindingRegistry Abilities { get; }
    public DclNativeReactionBindingRegistry ReactionBindings { get; }
    public DclCanonicalNativePolicyTicketTemplateRegistry PolicyTicketTemplates { get; }
    public DclStatePresentationProfileRegistry StatePresentations { get; }

    public (DclAbilityBinding Binding, DclActionProfile Profile) ResolveAbility(int abilityId)
    {
        if (!Abilities.Bindings.TryGetValue(abilityId, out DclAbilityBinding? binding))
            throw new KeyNotFoundException($"Native ability {abilityId} has no canonical DCL binding.");
        if (!Authoring.Actions.TryGetValue(binding.ActionId, out DclActionProfile? profile) ||
            profile.ProfileRevision != binding.ProfileRevision)
            throw new InvalidOperationException($"Native ability {abilityId} has a stale canonical action binding.");
        return (binding, profile);
    }

    public DclCanonicalActionFamily ResolveAbilityFamily(int abilityId)
    {
        if (!_abilityFamilies.TryGetValue(abilityId, out DclCanonicalActionFamily family))
            throw new KeyNotFoundException($"Native ability {abilityId} has no executable canonical family.");
        return family;
    }

    public DclActionProfile ResolveAction(string actionId, int profileRevision)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            throw new ArgumentException("A canonical ActionId is required.", nameof(actionId));
        if (!Authoring.Actions.TryGetValue(actionId, out DclActionProfile? profile))
            throw new KeyNotFoundException($"Canonical action '{actionId}' is not loaded.");
        if (profile.ProfileRevision != profileRevision)
            throw new InvalidOperationException($"Canonical action '{actionId}' is loaded at revision {profile.ProfileRevision}, not {profileRevision}.");
        return profile;
    }

    public DclNativeReactionBinding ResolveReactionBinding(string reactionId)
    {
        if (string.IsNullOrWhiteSpace(reactionId))
            throw new ArgumentException("A canonical ReactionId is required.", nameof(reactionId));
        if (!ReactionBindings.Bindings.TryGetValue(reactionId, out DclNativeReactionBinding? binding))
            throw new KeyNotFoundException($"Canonical Reaction '{reactionId}' has no native binding.");
        if (!Authoring.Reactions.TryGetValue(reactionId, out DclReactionDefinition? definition) ||
            !StringComparer.Ordinal.Equals(definition.EffectActionId, binding.EffectActionId))
            throw new InvalidOperationException($"Canonical Reaction '{reactionId}' has a stale native binding.");
        return binding;
    }

    public DclStateRegistry CreateBattleStateRegistry(int battleGeneration, long initialGlobalCt)
        => new(battleGeneration, initialGlobalCt);
}

internal sealed record DclCanonicalMagicExecutionInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    DclTargetCandidate Target,
    int BaseSpellScore,
    int TargetSpellScore,
    int? SharedCasterRoll,
    DclDefenseOption Defense,
    int? DefenseRoll,
    int MagnitudeAttribute,
    IReadOnlyList<int>? MagnitudeDice,
    DclElementAffinity Affinity,
    DclRational FaithMagnitude,
    bool TargetHasShell,
    int TargetMaxHp,
    bool FireEffect,
    bool OilContributed,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    IReadOnlyList<DclCanonicalStatusRiderInput>? StatusRiders = null,
    bool DeclaredTargetHasReflect = false,
    bool ReflectionAlreadyConsumed = false,
    DclTargetCandidate? ReflectedTarget = null,
    int ApplicableDr = 0,
    DclInjuryConsequenceInput? InjuryConsequenceInput = null,
    int AdditionalMagnitudeIntegerModifier = 0,
    DclCanonicalInjuryStateCommitContext? InjuryStateCommit = null,
    DclCanonicalResourcePoolSnapshot? ResourceTargetPools = null,
    DclCanonicalResourcePoolSnapshot? ResourceSourcePools = null,
    int? ResistanceScore = null,
    int? ResistanceRoll = null,
    bool Immune = false,
    DclCanonicalReactionWindowRequest? ReactionWindow = null,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0,
    int? TouchAttackRoll = null,
    IReadOnlyList<DclDefenseCandidate>? TouchDefenseCandidates = null,
    DclTouchNativeRouteVerdict? TouchRouteVerdict = null,
    DclCanonicalInjuryMovementBranchSet? InjuryMovementBranches = null);

internal sealed record DclCanonicalStatusRiderInput(
    int EffectIndex,
    int ResistanceScore,
    int? ResistanceRoll,
    bool Immune,
    DclStateRegistry StateRegistry,
    DclCanonicalStateMaterialization StateMaterialization);

internal sealed record DclCanonicalStatusRiderResult(
    int EffectIndex,
    DclMagicRiderResult Gate,
    DclStateApplicationResult? StateApplication);

internal sealed record DclCanonicalMagicExecutionResult(
    DclActionDeclaration Declaration,
    DclCastingOutcome Outcome,
    DclSpellGateResult? CastingGate,
    DclMagicDeliveryResult? Delivery,
    DclInjuryResult? BaseInjury,
    DclMagicalEffectResult? Effect,
    DclHealingResult? Healing,
    DclInjuryConsequenceResult? InjuryConsequences,
    DclResourcePayment Payment,
    DclActionTransaction? Transaction,
    bool TargetKoAfterStrike,
    IReadOnlyList<DclCanonicalStatusRiderResult> StatusRiders,
    DclReflectRoute ReflectRoute,
    DclCanonicalInjuryStateCommitResult? InjuryStates = null,
    DclCanonicalAimLifecycleResult? AimRetention = null,
    DclCanonicalResourceChangeResult? ResourceChange = null,
    DclCanonicalReactionWindowResult? Reactions = null,
    DclDefenseResourceSnapshot? FinalDefenseResources = null,
    DclPhysicalContestResult? TouchContest = null,
    DclCanonicalChargedCancellation? ChargingCancellation = null,
    DclCanonicalForcedMovementResult? ForcedMovement = null);

internal static class DclCanonicalMagicExecutor
{
    public static DclCanonicalMagicExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalMagicExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind != DclNativeCarrierKind.SingleResult ||
            binding.RewritePolicy is not (DclCarrierRewritePolicy.ReplaceCompleteResult or DclCarrierRewritePolicy.ReplaceNumericResult))
            throw new InvalidOperationException("The canonical single-result magic executor received an incompatible carrier binding.");
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Declaration request does not use the ability binding's exact normalized action revision.", nameof(input));
        if (profile.TransactionProfile.StrikeCount != 1 || profile.TargetProfile.TargetMode != DclTargetMode.Unit)
            throw new InvalidOperationException("This vertical executor owns one unit-targeted Strike only.");
        if (profile.Effects.Count == 0 || profile.Effects[0] is not
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.Damage or DclEffectKind.Healing or DclEffectKind.ResourceChange,
            } || profile.Effects.Skip(1).Any(effect => effect is not
            {
                Role: DclEffectRole.Rider,
                Kind: DclEffectKind.StatusApplication,
            }))
            throw new InvalidOperationException("This vertical requires one Damage/Healing Carrier followed only by normalized status Riders.");
        if (input.Target.Unit != input.DeclarationRequest.UnitTarget?.Unit)
            throw new ArgumentException("Input Target must be the stable original unit target declared by the action.", nameof(input));
        DclReflectRoute reflectRoute = DclReflectRouting.Resolve(
            profile.TargetProfile.TargetMode,
            profile.DeliveryProfile.Reflectable,
            input.DeclaredTargetHasReflect,
            input.ReflectionAlreadyConsumed,
            input.DeclarationRequest.Caster,
            input.Target.Unit);
        DclTargetCandidate resolutionTarget;
        if (reflectRoute.Reflected)
        {
            if (input.ReflectedTarget is null || input.ReflectedTarget.Unit != reflectRoute.FinalTarget)
                throw new ArgumentException("A reflected cast requires the current original-caster target snapshot.", nameof(input));
            resolutionTarget = input.ReflectedTarget;
        }
        else
        {
            if (input.ReflectedTarget is not null)
                throw new ArgumentException("An unreflected cast cannot own a reflected-target snapshot.", nameof(input));
            resolutionTarget = input.Target;
        }
        if (profile.MagnitudeProfile is not DclDamageMagnitude && input.InjuryMovementBranches is not null)
            throw new ArgumentException("Only direct Damage can own Injury movement branches.", nameof(input));
        input.InjuryMovementBranches?.Validate(resolutionTarget.Unit);
        DclDefenseCandidate[] touchDefenseCandidates = (input.TouchDefenseCandidates ?? []).ToArray();
        if (profile.DeliveryProfile.Delivery == DclDelivery.Touch)
        {
            if (input.Defense != new DclDefenseOption(DclDefenseKind.None, 0, Depletes: false))
                throw new ArgumentException("Touch owns defense candidates rather than one caller-selected magic defense.", nameof(input));
            DclTouchResolution.Validate(profile.DeliveryProfile, touchDefenseCandidates, resolutionTarget.DefenseResources);
            DclTouchNativeRouteVerdict routeVerdict = input.TouchRouteVerdict ??
                throw new ArgumentException("Touch execution requires one immutable native range/trajectory verdict.", nameof(input));
            IReadOnlyList<string> routeFailures = DclTouchResolution.RouteFailures(profile, routeVerdict);
            if (routeFailures.Count != 0)
                throw new InvalidOperationException($"Touch native route failed: {string.Join(",", routeFailures)}");
        }
        else
        {
            if (touchDefenseCandidates.Length != 0 || input.TouchAttackRoll is not null || input.TouchRouteVerdict is not null)
                throw new ArgumentException("Only Touch delivery may own Touch attack/defense inputs.", nameof(input));
            DclMagicDefensePolicy.Validate(profile.DeliveryProfile, input.Defense, resolutionTarget.DefenseResources);
        }
        if (input.TargetMaxHp < 1 || resolutionTarget.CurrentHp > input.TargetMaxHp)
            throw new ArgumentOutOfRangeException(nameof(input), "Target MaxHP must contain current HP.");
        if (profile.MagnitudeProfile is DclFixedResourceMagnitude)
        {
            DclCanonicalResourcePoolSnapshot targetPools = input.ResourceTargetPools ??
                throw new ArgumentException("ResourceChange execution requires exact routed-target pools.", nameof(input));
            DclCanonicalResourcePoolSnapshot sourcePools = input.ResourceSourcePools ??
                throw new ArgumentException("ResourceChange execution requires exact source pools.", nameof(input));
            if (targetPools.CurrentHp != resolutionTarget.CurrentHp || targetPools.MaxHp != input.TargetMaxHp ||
                sourcePools.CurrentHp != input.CurrentHpAtResolution ||
                sourcePools.CurrentMp != input.CurrentMpAtResolution)
                throw new ArgumentException("ResourceChange pools must match the routed target and payer snapshots.", nameof(input));
        }
        else if (input.ResourceTargetPools is not null || input.ResourceSourcePools is not null)
        {
            throw new ArgumentException("Damage/healing execution cannot own ResourceChange pool snapshots.", nameof(input));
        }

        DclCanonicalStatusRiderInput[] riderInputs = (input.StatusRiders ?? []).ToArray();
        Dictionary<int, DclCanonicalStatusRiderInput> ridersByEffect = riderInputs.ToDictionary(rider => rider.EffectIndex);
        int[] expectedRiderIndexes = Enumerable.Range(1, profile.Effects.Count - 1).ToArray();
        if (ridersByEffect.Count != riderInputs.Length || !ridersByEffect.Keys.Order().SequenceEqual(expectedRiderIndexes))
            throw new ArgumentException("Status-rider inputs must match every normalized Rider effect index exactly.", nameof(input));
        var riderDefinitions = new Dictionary<int, DclStateDefinition>();
        foreach (int effectIndex in expectedRiderIndexes)
        {
            DclEffectProfile riderEffect = profile.Effects[effectIndex];
            if (riderEffect.ReferencedStateKind is null ||
                !runtime.Authoring.States.TryGetValue(riderEffect.ReferencedStateKind, out DclStateDefinition? definition))
                throw new InvalidOperationException($"Status Rider {effectIndex} lost its normalized state definition.");
            DclCanonicalStatusRiderInput riderInput = ridersByEffect[effectIndex];
            if (riderInput.StateRegistry.BattleGeneration != resolutionTarget.Unit.BattleGeneration)
                throw new ArgumentException("A Rider state registry and target must belong to the same battle generation.", nameof(input));
            if (!StringComparer.Ordinal.Equals(riderInput.StateMaterialization.Payload.SchemaId, definition.PayloadSchema))
                throw new ArgumentException("A Rider materialization payload does not match its state definition.", nameof(input));
            riderDefinitions.Add(effectIndex, definition);
        }

        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, input.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical cast declaration failed: {string.Join(",", attempt.Failures)}");
        if (input.ReactionWindow is { } reactionWindow)
            DclCanonicalReactionWindow.Preflight(attempt.Declaration, reactionWindow);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
        {
            if (input.SharedCasterRoll is not null || input.TouchAttackRoll is not null || input.DefenseRoll is not null || input.ResistanceRoll is not null ||
                input.MagnitudeDice is not null ||
                input.InjuryConsequenceInput is not null ||
                riderInputs.Any(rider => rider.ResistanceRoll is not null))
                throw new ArgumentException("ResourceFailure occurs before every casting, defense, resistance, and magnitude random site.", nameof(input));
            DclResourcePayment resourceFailure = DclMagicResources.Settle(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment,
                DclCastingOutcome.ResourceFailure);
            var failedTransaction = new DclActionTransaction(attempt.Declaration, profile);
            failedTransaction.FailResourceCommitment();
            return new DclCanonicalMagicExecutionResult(
                attempt.Declaration,
                DclCastingOutcome.ResourceFailure,
                CastingGate: null,
                Delivery: null,
                BaseInjury: null,
                Effect: null,
                Healing: null,
                InjuryConsequences: null,
                resourceFailure,
                failedTransaction,
                TargetKoAfterStrike: false,
                StatusRiders: [],
                reflectRoute);
        }

        DclTouchResolutionResult? touchResolution = null;
        DclMagicDeliveryResult delivery;
        if (profile.DeliveryProfile.Delivery == DclDelivery.Touch)
        {
            if (input.SharedCasterRoll is not null)
                throw new ArgumentException("Touch uses its per-Strike attack draw, not a shared casting draw.", nameof(input));
            touchResolution = DclTouchResolution.Resolve(
                profile.DeliveryProfile,
                input.BaseSpellScore,
                input.TouchAttackRoll ?? throw new ArgumentNullException(nameof(input), "Payable Touch requires one attack draw."),
                touchDefenseCandidates,
                resolutionTarget.DefenseResources,
                input.DefenseRoll);
            delivery = DclTouchResolution.ToMagicDelivery(touchResolution);
        }
        else
        {
            int sharedCasterRoll = input.SharedCasterRoll ??
                throw new ArgumentNullException(nameof(input), "A payable cast requires its one shared caster draw.");
            delivery = profile.DeliveryProfile.Delivery switch
            {
                DclDelivery.ExternalProjectile => DclSpellResolution.ResolveExternal(
                    sharedCasterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore,
                    input.Defense,
                    input.DefenseRoll),
                DclDelivery.Beneficial => DclSpellResolution.ResolveBeneficial(
                    sharedCasterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore),
                DclDelivery.InternalDirect => DclSpellResolution.ResolveInternal(
                    sharedCasterRoll,
                    input.BaseSpellScore,
                    input.TargetSpellScore,
                    input.ResistanceScore ?? throw new ArgumentException(
                        "Internal Direct execution requires its exact resistance score.", nameof(input)),
                    input.ResistanceRoll,
                    input.Immune),
                _ => throw new InvalidOperationException("The canonical direct executor supports ExternalProjectile, InternalDirect, Beneficial, or Touch delivery."),
            };
        }
        DclDefenseResourceSnapshot finalDefenseResources = touchResolution?.FinalDefenseResources ??
            DclMagicDefensePolicy.ResolveFinalResources(delivery, resolutionTarget.DefenseResources);

        DclMagicalEffectResult? effect = null;
        DclInjuryResult? baseInjury = null;
        DclHealingResult? healing = null;
        DclInjuryConsequenceResult? injuryConsequences = null;
        DclCanonicalResourceChangeResult? resourceChange = null;
        if (delivery.Delivered)
        {
            if (input.MagnitudeDice is null)
                throw new ArgumentNullException(nameof(input), "A delivered magnitude must own its exact d6 draws.");
            switch (profile.MagnitudeProfile)
            {
                case DclDamageMagnitude damageMagnitude:
                {
                    DclDiceExpression diceExpression = DclCanonicalMagnitude.BuildDiceExpression(
                        input.MagnitudeAttribute,
                        damageMagnitude.Basis,
                        damageMagnitude.FixedExpression,
                        checked(damageMagnitude.IntegerModifier + input.AdditionalMagnitudeIntegerModifier),
                        damageMagnitude.WholeDiceModifier);
                    int rolledBaseMagnitude = Math.Max(0, DclInjury.RollDamage(diceExpression, input.MagnitudeDice));
                    baseInjury = DclCanonicalMagnitude.ResolveMagicalBaseInjury(
                        rolledBaseMagnitude,
                        damageMagnitude.DamageType,
                        input.ApplicableDr,
                        profile.DeliveryProfile);
                    effect = DclMagicalEffect.ResolveInjuryOrAbsorb(
                        baseInjury.Value.Injury,
                        input.Affinity,
                        input.FaithMagnitude,
                        profile.DeliveryProfile.ShellSensitive,
                        input.TargetHasShell,
                        resolutionTarget.CurrentHp,
                        input.TargetMaxHp,
                        input.FireEffect,
                        input.OilContributed);
                    if (effect.Value.Route == DclMagicalEffectRoute.Injury)
                    {
                        DclInjuryConsequenceInput consequenceInput = input.InjuryConsequenceInput ??
                            throw new ArgumentNullException(nameof(input), "Magical Injury requires its complete per-Strike consequence context and exact conditional draws.");
                        if (consequenceInput.MaxHp != input.TargetMaxHp)
                            throw new ArgumentException("Magical Injury consequence MaxHP must match the routed target snapshot.", nameof(input));
                        injuryConsequences = DclInjury.ResolveConsequences(
                            resolutionTarget.CurrentHp,
                            baseInjury.Value with { Injury = effect.Value.FinalInjury },
                            damageMagnitude.DamageType,
                            delivery.Outcome == DclMagicDeliveryOutcome.CriticalDelivered,
                            consequenceInput);
                    }
                    else if (input.InjuryConsequenceInput is not null)
                    {
                        throw new ArgumentException("Nullified or absorbed magic cannot own Injury-consequence inputs.", nameof(input));
                    }
                    break;
                }
                case DclHealingMagnitude healingMagnitude:
                {
                    if (input.InjuryConsequenceInput is not null)
                        throw new ArgumentException("Healing cannot own Injury-consequence inputs.", nameof(input));
                    DclDiceExpression normal = DclCanonicalMagnitude.BuildDiceExpression(
                        input.MagnitudeAttribute,
                        healingMagnitude.Basis,
                        healingMagnitude.FixedExpression,
                        checked(healingMagnitude.IntegerModifier + input.AdditionalMagnitudeIntegerModifier),
                        healingMagnitude.WholeDiceModifier);
                    DclDiceExpression rolledExpression = normal;
                    if (delivery.Outcome == DclMagicDeliveryOutcome.CriticalDelivered &&
                        profile.CriticalProfile.SuccessEffect == DclCriticalSuccessEffect.MaximizeOneHealingDie)
                    {
                        DclCriticalHealingExpression critical = DclMagicMagnitude.CriticalHealingExpression(normal);
                        rolledExpression = new DclDiceExpression(critical.DiceToRoll, critical.Adds);
                    }
                    int rawHealing = Math.Max(0, DclInjury.RollDamage(rolledExpression, input.MagnitudeDice));
                    healing = DclMagicalEffect.ResolveHealing(
                        rawHealing,
                        input.FaithMagnitude,
                        resolutionTarget.CurrentHp,
                        input.TargetMaxHp);
                    break;
                }
                case DclFixedResourceMagnitude:
                {
                    if (input.InjuryConsequenceInput is not null)
                        throw new ArgumentException("ResourceChange cannot own Injury-consequence inputs.", nameof(input));
                    resourceChange = DclCanonicalResourceChange.Resolve(
                        profile,
                        input.ResourceTargetPools!,
                        input.ResourceSourcePools!,
                        input.MagnitudeDice);
                    break;
                }
                default:
                    throw new InvalidOperationException("The canonical numeric magic vertical requires Damage, Healing, or ResourceChange magnitude.");
            }
        }
        else if (input.MagnitudeDice is not null)
        {
            throw new ArgumentException("A failed or defended delivery cannot consume magnitude RNG.", nameof(input));
        }
        else if (input.InjuryConsequenceInput is not null)
        {
            throw new ArgumentException("A failed or defended delivery cannot snapshot or roll Injury consequences.", nameof(input));
        }

        var riderResults = new List<DclCanonicalStatusRiderResult>();
        bool carrierDelivered = delivery.Delivered && resourceChange?.RejectedByUndeadPolicy != true;
        foreach (int effectIndex in expectedRiderIndexes)
        {
            DclCanonicalStatusRiderInput riderInput = ridersByEffect[effectIndex];
            DclMagicRiderResult riderGate = DclMagicActionGates.ResolveStatusRider(
                delivery.Gate,
                carrierDelivered,
                riderInput.ResistanceScore,
                riderInput.ResistanceRoll,
                riderInput.Immune,
                riderDefinitions[effectIndex].ResistanceGate);
            riderResults.Add(new DclCanonicalStatusRiderResult(effectIndex, riderGate, StateApplication: null));
        }

        DclResourcePayment payment = DclMagicResources.Settle(
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment,
            delivery.Gate.BaseOutcome);
        if (!payment.Legal)
            throw new InvalidOperationException("A prechecked cost commitment became illegal during atomic settlement.");

        int appliedInjury = effect is { Route: DclMagicalEffectRoute.Injury } value ? value.FinalInjury : 0;
        bool targetKo = (appliedInjury >= resolutionTarget.CurrentHp && resolutionTarget.CurrentHp > 0) ||
            resourceChange?.TargetKo == true;
        DclCanonicalForcedMovementResult? injuryMovement = null;
        if (injuryConsequences is { RequestedForcedDisplacement: > 0 } displacementConsequences)
        {
            injuryMovement = input.InjuryMovementBranches?.Resolve(
                resolutionTarget.Unit,
                targetKo,
                displacementConsequences.RequestedForcedDisplacement) ??
                throw new InvalidOperationException(
                    "Direct magical Injury displacement requires its frozen native map branch.");
            if (injuryMovement.MovedTiles != displacementConsequences.TotalForcedDisplacement)
                throw new InvalidOperationException(
                    "Direct magical Injury consequence and settled map verdict disagree on actual displacement.");
        }
        DclCanonicalAimLifecycleResult? aimRetention = null;
        long? plannedAimRemovalId = null;
        if (effect is { Route: DclMagicalEffectRoute.Injury, FinalInjury: > 0 } injuryForAim &&
            injuryConsequences is { } consequencesForAim &&
            input.ExecutionBattle is { } executionBattle)
        {
            bool targetHasAim = executionBattle.States.CaptureTarget(resolutionTarget.Unit).Instances.Any(instance =>
                StringComparer.Ordinal.Equals(instance.Kind, "aim"));
            if (targetHasAim)
            {
                bool directCancellation = targetKo || consequencesForAim.ApplyStun ||
                    consequencesForAim.ApplyKnockedDown || injuryMovement?.CancelAim == true;
                if (directCancellation)
                {
                    string reason = targetKo ? "ko-cancelled"
                        : consequencesForAim.ApplyStun ? "stun-cancelled"
                        : consequencesForAim.ApplyKnockedDown ? "knocked-down-cancelled"
                        : "forced-movement-cancelled";
                    aimRetention = DclCanonicalAimLifecycle.PlanCancelOwner(
                        executionBattle.States,
                        resolutionTarget.Unit,
                        reason);
                }
                else
                {
                    int retentionRoll = executionBattle.ExecutionRandom.Roll3D6(
                        executionBattle.RollIdentity(
                            input.ActionInstanceId,
                            input.DeclarationRequest.Caster,
                            resolutionTarget.Unit,
                            strikeIndex: 0,
                            DclRollSite.AimRetention,
                            drawIndex: 0));
                    aimRetention = DclCanonicalAimLifecycle.PlanInjuryRetention(
                        executionBattle.States,
                        resolutionTarget.Unit,
                        injuryForAim.FinalInjury,
                        forcedMovement: false,
                        input.InjuryConsequenceInput?.Will ?? throw new ArgumentException(
                            "Magical Aim retention requires the routed Injury target context.", nameof(input)),
                        input.AimRetentionModifier,
                        input.AimRetentionStatePenaltyMagnitude,
                        retentionRoll);
                }
                if (aimRetention is { HadAim: true, Retained: false, InstanceId: { } removedAimId })
                    plannedAimRemovalId = removedAimId;
            }
        }
        var appliedEffectIndexes = new List<int>();
        if (carrierDelivered) appliedEffectIndexes.Add(0);
        appliedEffectIndexes.AddRange(riderResults
            .Where(rider => rider.Gate.Applied)
            .Select(rider => rider.EffectIndex));
        DclCanonicalInjuryStateCommitResult? injuryStates = null;
        DclCanonicalChargedCancellation? chargingCancellation = null;
        DclCanonicalTransactionResult committed = DclCanonicalTransactionExecutor.Commit(
            attempt.Declaration,
            profile,
            [new DclTargetResolutionSnapshot(
                resolutionTarget.Unit,
                resolutionTarget.CurrentHp,
                resolutionTarget.CombatStateRevision,
                resolutionTarget.DefenseResources)],
            [new DclCanonicalResolvedStrike(
                resolutionTarget.Unit,
                StrikeIndex: 0,
                appliedEffectIndexes,
                TargetKoAfterStrike: targetKo)],
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                _.ApplyFinalDefenseResources(resolutionTarget.Unit, finalDefenseResources);
                if (plannedAimRemovalId is { } removedAimId)
                {
                    IReadOnlyList<long> removedAimIds = input.ExecutionBattle!.States.RemoveInstances([removedAimId]);
                    if (removedAimIds.Count != 1)
                        throw new InvalidOperationException("Target Aim state changed before direct-magic commit.");
                }
                if (effect is { Route: DclMagicalEffectRoute.Injury } injuryEffect &&
                    injuryConsequences is { } consequences)
                {
                    if (input.InjuryStateCommit is { } injuryCommit)
                    {
                        injuryStates = DclCanonicalInjuryConsequenceCommitter.Commit(
                            runtime,
                            baseInjury!.Value with { Injury = injuryEffect.FinalInjury },
                            consequences,
                            resolutionTarget.Unit,
                            input.DeclarationRequest.Caster,
                            input.TargetMaxHp,
                            injuryCommit);
                    }
                    chargingCancellation = input.ExecutionBattle?.ResolveInjuryConcentration(
                        resolutionTarget.Unit,
                        consequences) ?? (consequences.Concentration.Outcome == DclConcentrationOutcome.NoIncident
                            ? null
                            : throw new InvalidOperationException(
                                "An executable direct-magic Injury concentration incident lost its timeline owner."));
                }
                for (int index = 0; index < riderResults.Count; index++)
                {
                    DclCanonicalStatusRiderResult riderResult = riderResults[index];
                    if (!riderResult.Gate.Applied) continue;
                    DclCanonicalStatusRiderInput riderInput = ridersByEffect[riderResult.EffectIndex];
                    DclCanonicalStateMaterialization materialization = riderInput.StateMaterialization;
                    DclStateDefinition definition = riderDefinitions[riderResult.EffectIndex];
                    var stateApplication = new DclStateApplication(
                        definition,
                        resolutionTarget.Unit,
                        materialization.BindSource ? input.DeclarationRequest.Caster : null,
                        riderInput.StateRegistry.CurrentGlobalCt,
                        materialization.AppliedBeforeTurnSerial,
                        materialization.FirstEligibleTargetTurnSerial,
                        materialization.FirstEligibleSourceTurnSerial,
                        materialization.DurationUnits,
                        materialization.Strength,
                        riderResult.Gate.CasterMargin,
                        materialization.StackDiscriminator,
                        materialization.ContributionIdentity,
                        materialization.Payload,
                        definition.PresentationProfile);
                    DclStateApplicationResult application = input.ExecutionBattle is not null
                        ? input.ExecutionBattle.ApplyState(stateApplication)
                        : riderInput.StateRegistry.Apply(stateApplication);
                    riderResults[index] = riderResult with { StateApplication = application };
                }
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: input.ExecutionBattle?.States);
        return new DclCanonicalMagicExecutionResult(
            attempt.Declaration,
            delivery.Gate.BaseOutcome,
            delivery.Gate,
            delivery,
            baseInjury,
            effect,
            healing,
            injuryConsequences,
            payment,
            committed.Transaction,
            targetKo,
            riderResults,
            reflectRoute,
            injuryStates,
            aimRetention,
            ResourceChange: resourceChange,
            Reactions: committed.Reactions,
            FinalDefenseResources: finalDefenseResources,
            TouchContest: touchResolution?.Contest,
            ChargingCancellation: chargingCancellation,
            ForcedMovement: injuryMovement);
    }

}

internal static class DclCanonicalMagnitude
{
    public static DclDiceExpression BuildDiceExpression(
        int magnitudeAttribute,
        DclDamageBasis basis,
        string? fixedExpressionText,
        int integerModifier,
        int wholeDiceModifier)
    {
        DclDiceExpression? fixedExpression = fixedExpressionText is null
            ? null
            : DclDiceExpression.ParseAuthored(fixedExpressionText);
        return DclMagicMagnitude.BuildDiceExpression(
            magnitudeAttribute,
            basis,
            fixedExpression,
            integerModifier,
            wholeDiceModifier);
    }

    public static DclInjuryResult ResolveMagicalBaseInjury(
        int rawRolledDamage,
        DclDamageType damageType,
        int applicableDr,
        DclDeliveryProfile delivery)
    {
        if (applicableDr < 0) throw new ArgumentOutOfRangeException(nameof(applicableDr));
        (int dr, DclRational divisor, bool ignoreDr) = delivery.ArmorPolicy switch
        {
            DclArmorPolicy.Manifestation => (applicableDr, DclRational.FromInteger(1), false),
            DclArmorPolicy.ArmorDividing => (applicableDr, delivery.ArmorDivisor!.Value, false),
            DclArmorPolicy.IgnoreDr or DclArmorPolicy.InternalSpiritual or DclArmorPolicy.None =>
                (0, DclRational.FromInteger(1), true),
            _ => throw new InvalidOperationException("Magical damage has no normalized armor policy."),
        };
        return DclInjury.Resolve(rawRolledDamage, damageType, dr, divisor, ignoreDr);
    }
}
