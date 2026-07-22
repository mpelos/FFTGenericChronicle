namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalAreaTargetInput(
    DclTargetCandidate Target,
    int TargetSpellScore,
    DclDefenseOption? Dodge,
    IReadOnlyList<int> DodgeRolls,
    int? ResistanceScore,
    int? ResistanceRoll,
    int MagnitudeAttribute,
    IReadOnlyList<IReadOnlyList<int>?> MagnitudeDiceByStrike,
    DclElementAffinity Affinity,
    DclRational FaithMagnitude,
    bool TargetHasShell,
    int TargetMaxHp,
    bool FireEffect,
    bool OilContributed,
    int ApplicableDr = 0,
    DclCanonicalInjuryTargetContext? InjuryTargetContext = null,
    IReadOnlyList<DclCanonicalInjuryConsequenceRolls?>? InjuryConsequenceRollsByStrike = null,
    int AdditionalMagnitudeIntegerModifier = 0,
    bool DirectConcentrationCancellation = false,
    int AuthoredForcedDisplacement = 0,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0,
    IReadOnlyList<DclCanonicalStatusRiderInput>? StatusRiders = null,
    DclCanonicalInjuryStateCommitContext? InjuryStateCommit = null,
    DclCanonicalResourcePoolSnapshot? ResourceTargetPools = null,
    DclCanonicalNativeMovementVerdict? ForcedMovementVerdict = null,
    bool ForcedMovementImmune = false,
    DclCanonicalConcentrationTargetContext? ForcedMovementConcentrationContext = null,
    int? ForcedMovementConcentrationRoll = null,
    IReadOnlyList<DclCanonicalInjuryMovementBranchSet?>? InjuryMovementBranchesByStrike = null,
    IReadOnlyList<DclCanonicalInjuryMovementBranchForest?>? InjuryMovementBranchForestsByStrike = null);

internal sealed record DclCanonicalAreaMagicExecutionInput(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    IReadOnlyDictionary<DclUnitKey, DclTargetCandidate> CurrentUnits,
    IReadOnlyList<DclTargetCandidate> NativeGeometricMembers,
    int BaseSpellScore,
    int? SharedCasterRoll,
    IReadOnlyList<DclCanonicalAreaTargetInput> Targets,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    DclCanonicalReactionWindowRequest? ReactionWindow = null,
    DclCanonicalResourcePoolSnapshot? ResourceSourcePools = null);

internal sealed record DclCanonicalAreaMagicStrikeResult(
    DclUnitKey Target,
    int StrikeIndex,
    DclMagicDeliveryOutcome? DeliveryOutcome,
    DclInjuryResult? BaseInjury,
    DclMagicalEffectResult? Effect,
    DclHealingResult? Healing,
    DclInjuryConsequenceResult? InjuryConsequences,
    bool TargetKoAfterStrike,
    bool KoShortCircuited,
    DclCanonicalAimLifecycleResult? AimRetention = null,
    IReadOnlyList<DclCanonicalStatusRiderResult>? StatusRiders = null,
    DclCanonicalInjuryStateCommitResult? InjuryStates = null,
    DclCanonicalResourceChangeResult? ResourceChange = null,
    DclCanonicalForcedMovementResult? ForcedMovement = null,
    DclConcentrationResult? Concentration = null,
    DclCanonicalChargedCancellation? ChargingCancellation = null);

internal sealed record DclCanonicalAreaMagicExecutionResult(
    DclActionDeclaration Declaration,
    DclCastingOutcome Outcome,
    DclResolvedCastCenter? Center,
    DclResourcePayment Payment,
    IReadOnlyList<DclCanonicalAreaMagicStrikeResult> Strikes,
    DclActionTransaction Transaction,
    DclCanonicalReactionWindowResult? Reactions = null);

internal static class DclCanonicalAreaMagicExecutor
{
    public static DclCanonicalAreaMagicExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalAreaMagicExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        bool statusOnly = profile.MagnitudeProfile is null && profile.Effects.Count == 1 && profile.Effects[0] is
        {
            Role: DclEffectRole.Carrier,
            Kind: DclEffectKind.StatusApplication,
            ReferencedStateKind: not null,
        };
        bool forcedMovementOnly = profile.MagnitudeProfile is null && profile.ForcedMovementProfile is not null &&
            profile.Effects.Count == 1 && profile.Effects[0] is
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.ForcedMovement,
            };
        bool carrierCompatible = statusOnly
            ? binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket or
                DclNativeCarrierKind.ConditionalStatusProducer
            : forcedMovementOnly
                ? binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket
                : binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.NativeRepeat;
        if (!carrierCompatible)
            throw new InvalidOperationException("The canonical area executor received an incompatible carrier.");
        if (profile.DeliveryProfile.Delivery != DclDelivery.Area || profile.TargetProfile.Area is null ||
            profile.Effects.Count == 0 || profile.Effects[0].Role != DclEffectRole.Carrier ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is not (DclDamageMagnitude or DclHealingMagnitude or DclFixedResourceMagnitude) ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is DclDamageMagnitude && profile.Effects[0].Kind != DclEffectKind.Damage ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is DclHealingMagnitude && profile.Effects[0].Kind != DclEffectKind.Healing ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is DclFixedResourceMagnitude && profile.Effects[0].Kind != DclEffectKind.ResourceChange ||
            !statusOnly && !forcedMovementOnly && profile.Effects.Skip(1).Any(effect => effect is not
            {
                Role: DclEffectRole.Rider,
                Kind: DclEffectKind.StatusApplication,
                ReferencedStateKind: not null,
            }))
            throw new InvalidOperationException("The area vertical requires one numeric Carrier with status Riders, one pure status Carrier, or one pure ForcedMovement Carrier.");
        int[] riderEffectIndexes = statusOnly ? [0] : Enumerable.Range(1, profile.Effects.Count - 1).ToArray();
        RequireSupportedRiderTiming(profile);
        DclCanonicalInjuryConsequenceCommitter.RequireSupportedTiming(profile);
        var riderDefinitions = new Dictionary<int, DclStateDefinition>();
        foreach (int effectIndex in riderEffectIndexes)
        {
            string stateKind = profile.Effects[effectIndex].ReferencedStateKind!;
            if (!runtime.Authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition))
                throw new InvalidOperationException($"Area status Rider {effectIndex} lost state definition '{stateKind}'.");
            if (definition.ResistanceGate == DclStateResistanceGate.Explicit)
                throw new InvalidOperationException(
                    $"Area status Rider {effectIndex} uses Explicit resistance and requires its named mechanism owner.");
            if (statusOnly)
            {
                DclStateResistanceGate expected = profile.TargetProfile.Area.DeliveryGate == DclAreaDeliveryGate.QuickContest
                    ? DclStateResistanceGate.QuickContest
                    : DclStateResistanceGate.None;
                if (definition.ResistanceGate != expected)
                    throw new InvalidOperationException(
                        $"Area status Carrier delivery gate {profile.TargetProfile.Area.DeliveryGate} requires state gate {expected}.");
            }
            riderDefinitions.Add(effectIndex, definition);
        }
        DclDamageMagnitude? damageMagnitude = profile.MagnitudeProfile as DclDamageMagnitude;
        DclHealingMagnitude? healingMagnitude = profile.MagnitudeProfile as DclHealingMagnitude;
        DclFixedResourceMagnitude? resourceMagnitude = profile.MagnitudeProfile as DclFixedResourceMagnitude;
        if (resourceMagnitude is not null && (profile.Effects.Count != 1 || profile.ResourceChangeProfile is null))
            throw new InvalidOperationException("Area ResourceChange requires its single normalized Carrier and explicit route profile.");
        DclCanonicalBattleRuntime? executionBattle = input.ExecutionBattle;
        if (input.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            input.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Area declaration does not match the bound profile revision.", nameof(input));
        if (resourceMagnitude is not null)
        {
            DclCanonicalResourcePoolSnapshot sourcePools = input.ResourceSourcePools ??
                throw new ArgumentException("Area ResourceChange requires exact source pools.", nameof(input));
            if (sourcePools.CurrentHp != input.CurrentHpAtResolution ||
                sourcePools.CurrentMp != input.CurrentMpAtResolution)
                throw new ArgumentException("Area ResourceChange source pools must match the payer snapshot.", nameof(input));
        }
        else if (input.ResourceSourcePools is not null || input.Targets.Any(target => target.ResourceTargetPools is not null))
        {
            throw new ArgumentException("Area Damage/Healing/status/ForcedMovement cannot own ResourceChange pool snapshots.", nameof(input));
        }
        if (executionBattle is not null)
        {
            if (!ReferenceEquals(executionBattle.Catalog, runtime) ||
                executionBattle.BattleGeneration != input.DeclarationRequest.Caster.BattleGeneration)
                throw new ArgumentException("Area execution battle and catalog ownership are inconsistent.", nameof(input));
            if (!executionBattle.TryGetObservedUnit(input.DeclarationRequest.Caster.UnitSlot, out DclUnitKey observedSource) ||
                observedSource != input.DeclarationRequest.Caster || input.CurrentUnits.Any(pair =>
                    pair.Key != pair.Value.Unit ||
                    !executionBattle.TryGetObservedUnit(pair.Key.UnitSlot, out DclUnitKey observedUnit) ||
                    observedUnit != pair.Key) || input.NativeGeometricMembers.Any(member =>
                    !executionBattle.TryGetObservedUnit(member.Unit.UnitSlot, out DclUnitKey observedMember) ||
                    observedMember != member.Unit))
                throw new ArgumentException("Area execution requires current observed source, unit, and geometric-member UnitKeys.", nameof(input));
            if (input.ReactionWindow is not { } ownedReactionWindow ||
                !ReferenceEquals(ownedReactionWindow.Runtime, runtime) ||
                !ReferenceEquals(ownedReactionWindow.ExecutionBattle, executionBattle))
                throw new ArgumentException(
                    "Confirmed Area execution requires its exact battle-owned Reaction window, including when empty.",
                    nameof(input));
            RequireNoPreSuppliedExecutionDraws(input);
        }
        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(input.DeclarationRequest, input.ActionInstanceId);
        if (!attempt.Legal || attempt.Declaration is null)
            throw new InvalidOperationException($"Canonical area declaration failed: {string.Join(",", attempt.Failures)}");
        if (input.ReactionWindow is { } reactionWindow)
            DclCanonicalReactionWindow.Preflight(attempt.Declaration, reactionWindow);
        if (!DclMagicResources.CanPayFullCost(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment))
        {
            if (input.SharedCasterRoll is not null || input.Targets.Any(target =>
                    target.ResistanceRoll is not null || target.DodgeRolls.Count != 0 ||
                    target.MagnitudeDiceByStrike.Any(draws => draws is not null) ||
                    target.InjuryConsequenceRollsByStrike?.Any(draws => draws is not null) == true ||
                    (target.StatusRiders ?? []).Any(rider => rider.ResistanceRoll is not null)))
                throw new ArgumentException("ResourceFailure occurs before all Area casting, target-gate, and magnitude random sites.", nameof(input));
            DclResourcePayment failurePayment = DclMagicResources.Settle(
                input.CurrentMpAtResolution,
                input.CurrentHpAtResolution,
                attempt.CostCommitment,
                DclCastingOutcome.ResourceFailure);
            var failed = new DclActionTransaction(attempt.Declaration, profile);
            failed.FailResourceCommitment();
            return new DclCanonicalAreaMagicExecutionResult(
                attempt.Declaration,
                DclCastingOutcome.ResourceFailure,
                Center: null,
                failurePayment,
                [],
                failed);
        }

        int sharedCasterRoll = input.SharedCasterRoll ?? executionBattle?.ExecutionRandom.Roll3D6(
            executionBattle.RollIdentity(
                input.ActionInstanceId,
                input.DeclarationRequest.Caster,
                target: null,
                strikeIndex: 0,
                DclRollSite.Casting,
                drawIndex: 0)) ??
            throw new ArgumentNullException(nameof(input), "A payable Area cast requires its shared caster draw.");

        DclAreaProfile area = profile.TargetProfile.Area;
        DclResolvedCastCenter center = DclMagicTargeting.ResolveCenter(
            attempt.Declaration,
            area.CenterMode,
            input.CurrentUnits);
        if (!center.Available)
            throw new InvalidOperationException($"Canonical area center is unavailable: {center.Reason}");
        DclTargetBatch targetBatch = DclMagicTargeting.SnapshotAreaTargets(
            attempt.Declaration.Source.BattleGeneration,
            input.NativeGeometricMembers,
            profile.TargetProfile.AllegiancePolicy,
            profile.TargetProfile.EligibleTargetStates);
        Dictionary<DclUnitKey, DclCanonicalAreaTargetInput> targetInputs = input.Targets.ToDictionary(target => target.Target.Unit);
        if (targetInputs.Count != input.Targets.Count ||
            targetInputs.Keys.ToHashSet().SetEquals(targetBatch.Targets.Select(target => target.Target)) == false)
            throw new ArgumentException("Area target inputs must match the stable filtered TargetBatch exactly.", nameof(input));

        int baseTargetScore = input.Targets.Count == 0 ? input.BaseSpellScore : input.Targets[0].TargetSpellScore;
        DclSpellGateResult baseGate = DclSpellResolution.ClassifySharedRoll(
            sharedCasterRoll,
            input.BaseSpellScore,
            baseTargetScore);
        DclResourcePayment payment = DclMagicResources.Settle(
            input.CurrentMpAtResolution,
            input.CurrentHpAtResolution,
            attempt.CostCommitment,
            baseGate.BaseOutcome);
        if (!payment.Legal)
            throw new InvalidOperationException("A prechecked area cost became illegal during settlement.");

        var strikeResults = new List<DclCanonicalAreaMagicStrikeResult>();
        var resolved = new List<DclCanonicalResolvedStrike>();
        var riderResultsByTarget = new Dictionary<DclUnitKey, List<DclCanonicalStatusRiderResult>>();
        var riderCarrierStrikeByTarget = new Dictionary<DclUnitKey, int>();
        var plannedTargetAimRemovalIds = new HashSet<long>();
        DclCanonicalResourcePoolSnapshot? currentSourcePools = input.ResourceSourcePools;
        foreach (DclTargetResolutionSnapshot snapshot in targetBatch.Targets)
        {
            bool targetAimAvailable = executionBattle?.States.CaptureTarget(snapshot.Target).Instances.Any(instance =>
                StringComparer.Ordinal.Equals(instance.Kind, "aim")) == true;
            DclCanonicalAreaTargetInput targetInput = targetInputs[snapshot.Target];
            DclCanonicalStatusRiderInput[] targetRiders = (targetInput.StatusRiders ?? []).ToArray();
            Dictionary<int, DclCanonicalStatusRiderInput> targetRidersByEffect =
                targetRiders.ToDictionary(rider => rider.EffectIndex);
            if (targetRidersByEffect.Count != targetRiders.Length ||
                !targetRidersByEffect.Keys.Order().SequenceEqual(riderEffectIndexes))
                throw new ArgumentException(
                    "Each Area target must provide exactly one input for every normalized status Rider.",
                    nameof(input));
            foreach (int effectIndex in riderEffectIndexes)
            {
                DclCanonicalStatusRiderInput rider = targetRidersByEffect[effectIndex];
                DclStateDefinition definition = riderDefinitions[effectIndex];
                if (rider.StateRegistry.BattleGeneration != snapshot.Target.BattleGeneration ||
                    !StringComparer.Ordinal.Equals(rider.StateMaterialization.Payload.SchemaId, definition.PayloadSchema) ||
                    executionBattle is not null && !ReferenceEquals(rider.StateRegistry, executionBattle.States))
                    throw new ArgumentException(
                        "Area Rider registry/materialization must match its target battle and normalized state definition.",
                        nameof(input));
            }
            DclCanonicalStatusRiderInput? statusCarrierInput = statusOnly
                ? targetRidersByEffect[0]
                : null;
            if (statusOnly && profile.TargetProfile.Area.DeliveryGate == DclAreaDeliveryGate.QuickContest &&
                targetInput.ResistanceScore != statusCarrierInput!.ResistanceScore)
                throw new ArgumentException(
                    "Area status Carrier must use the same resistance snapshot for delivery and materialization.",
                    nameof(input));
            DclCanonicalForcedMovementResult? plannedForcedMovement = null;
            if (forcedMovementOnly)
            {
                targetInput.ForcedMovementConcentrationContext?.Validate();
                if (executionBattle is { } concentrationBattle &&
                    (targetInput.ForcedMovementConcentrationContext?.Charging ?? false) !=
                        concentrationBattle.IsCharging(snapshot.Target))
                    throw new ArgumentException(
                        "Area ForcedMovement concentration snapshot diverges from the canonical timeline before RNG.",
                        nameof(input));
                if (targetInput.ForcedMovementImmune &&
                    profile.TargetProfile.Area.DeliveryGate != DclAreaDeliveryGate.QuickContest)
                    throw new ArgumentException(
                        "Area ForcedMovement immunity is legal only under its QuickContest delivery gate.",
                        nameof(input));
                plannedForcedMovement = DclCanonicalForcedMovement.Resolve(
                    profile,
                    snapshot.Target,
                    targetInput.Target.Tile,
                    targetKo: snapshot.CurrentHp == 0,
                    targetInput.ForcedMovementVerdict);
            }
            else if (targetInput.ForcedMovementVerdict is not null || targetInput.ForcedMovementImmune)
            {
                throw new ArgumentException(
                    "Only an Area ForcedMovement Carrier can own a movement verdict or movement immunity.",
                    nameof(input));
            }
            else if (targetInput.ForcedMovementConcentrationContext is not null ||
                     targetInput.ForcedMovementConcentrationRoll is not null)
                throw new ArgumentException(
                    "Only an Area ForcedMovement Carrier can own concentration inputs.", nameof(input));
            if (targetInput.MagnitudeDiceByStrike.Count != profile.TransactionProfile.StrikeCount)
                throw new ArgumentException("Each area target requires one nullable magnitude-draw entry per Strike.", nameof(input));
            if (targetInput.TargetMaxHp < 1 || snapshot.CurrentHp > targetInput.TargetMaxHp)
                throw new ArgumentOutOfRangeException(nameof(input), "Area target MaxHP must contain the snapshotted HP.");
            if (damageMagnitude is not null)
            {
                if (targetInput.InjuryTargetContext is not { } injuryContext ||
                    injuryContext.MaxHp != targetInput.TargetMaxHp ||
                    targetInput.InjuryConsequenceRollsByStrike is null ||
                    targetInput.InjuryConsequenceRollsByStrike.Count != profile.TransactionProfile.StrikeCount)
                    throw new ArgumentException("Area damage requires one target Injury context and one nullable consequence-roll entry per Strike.", nameof(input));
                if (targetInput.InjuryMovementBranchesByStrike is { } movementBranches &&
                    movementBranches.Count != profile.TransactionProfile.StrikeCount)
                    throw new ArgumentException(
                        "Area Injury movement branches must match the exact Strike cardinality.", nameof(input));
                foreach (DclCanonicalInjuryMovementBranchSet branches in
                    targetInput.InjuryMovementBranchesByStrike?.OfType<DclCanonicalInjuryMovementBranchSet>() ?? [])
                    branches.Validate(snapshot.Target);
                if (targetInput.InjuryMovementBranchForestsByStrike is { } movementForests &&
                    movementForests.Count != profile.TransactionProfile.StrikeCount)
                    throw new ArgumentException(
                        "Area Injury movement forests must match the exact Strike cardinality.", nameof(input));
                if (targetInput.InjuryMovementBranchesByStrike is not null &&
                    targetInput.InjuryMovementBranchForestsByStrike is not null)
                    throw new ArgumentException(
                        "Area Damage cannot own both single-origin and conditional-origin Injury branches.",
                        nameof(input));
                foreach (DclCanonicalInjuryMovementBranchForest forest in
                    targetInput.InjuryMovementBranchForestsByStrike?
                        .OfType<DclCanonicalInjuryMovementBranchForest>() ?? [])
                    forest.Validate(snapshot.Target);
            }
            else if (targetInput.InjuryTargetContext is not null || targetInput.InjuryConsequenceRollsByStrike is not null ||
                     targetInput.InjuryMovementBranchesByStrike is not null ||
                     targetInput.InjuryMovementBranchForestsByStrike is not null)
            {
                throw new ArgumentException("Area healing/ResourceChange cannot own Injury-consequence inputs.", nameof(input));
            }
            DclCanonicalResourcePoolSnapshot? currentTargetPools = targetInput.ResourceTargetPools;
            if (resourceMagnitude is not null)
            {
                if (currentTargetPools is null)
                    throw new ArgumentException(
                        "Area ResourceChange requires exact target pools for every filtered target.", nameof(input));
                if (currentTargetPools.CurrentHp != snapshot.CurrentHp ||
                    currentTargetPools.MaxHp != targetInput.TargetMaxHp)
                    throw new ArgumentException(
                        "Area ResourceChange target pools must match the stable target snapshot.", nameof(input));
            }
            DclSpellGateResult targetGate = DclSpellResolution.ClassifySharedRoll(
                sharedCasterRoll,
                input.BaseSpellScore,
                targetInput.TargetSpellScore);
            DclMagicTargetGateResult? sharedTargetGate = null;
            int? carrierResistanceRoll = null;
            if (!targetGate.BaseSucceeded || !targetGate.TargetSucceeded)
            {
                sharedTargetGate = DclMagicActionGates.ResolveAreaTarget(
                    snapshot.Target,
                    sharedCasterRoll,
                    input.BaseSpellScore,
                    targetInput.TargetSpellScore,
                    area.DeliveryGate,
                    profile.TransactionProfile.StrikeCount,
                    targetInput.Dodge,
                    dodgeRolls: [],
                    resistanceScore: targetInput.ResistanceScore,
                    resistanceRoll: null);
                if (targetInput.DodgeRolls.Count != 0 || targetInput.ResistanceRoll is not null)
                    throw new ArgumentException("A failed area casting gate cannot own target-gate draws.", nameof(input));
            }
            else if (area.DeliveryGate is DclAreaDeliveryGate.None or DclAreaDeliveryGate.QuickContest)
            {
                int? resistanceRoll = targetInput.ResistanceRoll;
                bool carrierImmune = statusOnly && statusCarrierInput!.Immune ||
                    forcedMovementOnly && targetInput.ForcedMovementImmune;
                if (area.DeliveryGate == DclAreaDeliveryGate.QuickContest && carrierImmune)
                {
                    if (resistanceRoll is not null)
                        throw new ArgumentException(
                            "An immune Area status Carrier target cannot own a resistance draw.", nameof(input));
                    sharedTargetGate = ImmuneAreaCarrierTarget(snapshot.Target, targetGate);
                }
                else if (area.DeliveryGate == DclAreaDeliveryGate.QuickContest && executionBattle is not null)
                {
                    resistanceRoll = executionBattle.ExecutionRandom.Roll3D6(executionBattle.RollIdentity(
                        input.ActionInstanceId,
                        input.DeclarationRequest.Caster,
                        snapshot.Target,
                        strikeIndex: 0,
                        DclRollSite.Resistance,
                        drawIndex: 0));
                }
                carrierResistanceRoll = resistanceRoll;
                sharedTargetGate ??= DclMagicActionGates.ResolveAreaTarget(
                        snapshot.Target,
                        sharedCasterRoll,
                        input.BaseSpellScore,
                        targetInput.TargetSpellScore,
                        area.DeliveryGate,
                        profile.TransactionProfile.StrikeCount,
                        targetInput.Dodge,
                        dodgeRolls: [],
                        targetInput.ResistanceScore,
                        resistanceRoll);
                if (targetInput.DodgeRolls.Count != 0)
                    throw new ArgumentException("None and QuickContest area delivery cannot own Dodge draws.", nameof(input));
            }

            int remainingHp = snapshot.CurrentHp;
            DclBattleTile movementTile = targetInput.Target.Tile;
            int shockInjury = targetInput.InjuryTargetContext?.UnexpiredShockInjury ?? 0;
            bool charging = targetInput.InjuryTargetContext?.Charging ?? false;
            bool immediateStun = false;
            bool immediateKnockedDown = false;
            bool immediateApplication = profile.TransactionProfile.WithinActionApplication ==
                DclWithinActionApplication.Immediate;
            bool oilAvailable = targetInput.OilContributed;
            int dodgeDrawIndex = 0;
            DclDiceExpression diceExpression = profile.MagnitudeProfile switch
            {
                DclDamageMagnitude damage => DclCanonicalMagnitude.BuildDiceExpression(
                    targetInput.MagnitudeAttribute,
                    damage.Basis,
                    damage.FixedExpression,
                    checked(damage.IntegerModifier + targetInput.AdditionalMagnitudeIntegerModifier),
                    damage.WholeDiceModifier),
                DclHealingMagnitude healing => DclCanonicalMagnitude.BuildDiceExpression(
                    targetInput.MagnitudeAttribute,
                    healing.Basis,
                    healing.FixedExpression,
                    checked(healing.IntegerModifier + targetInput.AdditionalMagnitudeIntegerModifier),
                    healing.WholeDiceModifier),
                DclFixedResourceMagnitude resource when DclDiceExpression.TryParseAuthored(
                    resource.Expression,
                    out DclDiceExpression expression) => expression,
                DclFixedResourceMagnitude => throw new InvalidOperationException(
                    "Area ResourceChange magnitude does not use the exact Xd6+Y grammar."),
                _ when statusOnly => new DclDiceExpression(0, 0),
                _ when forcedMovementOnly => new DclDiceExpression(0, 0),
                _ => throw new InvalidOperationException(),
            };
            for (int strikeIndex = 0; strikeIndex < profile.TransactionProfile.StrikeCount; strikeIndex++)
            {
                if (remainingHp == 0)
                {
                    if (targetInput.MagnitudeDiceByStrike[strikeIndex] is not null)
                        throw new ArgumentException("A KO-short-circuited area Strike cannot own magnitude draws.", nameof(input));
                    if (targetInput.InjuryConsequenceRollsByStrike?[strikeIndex] is not null)
                        throw new ArgumentException("A KO-short-circuited area Strike cannot own Injury-consequence draws.", nameof(input));
                    strikeResults.Add(new DclCanonicalAreaMagicStrikeResult(
                        snapshot.Target, strikeIndex, null, null, null, null, null,
                        TargetKoAfterStrike: true,
                        KoShortCircuited: true,
                        StatusRiders: []));
                    resolved.Add(new DclCanonicalResolvedStrike(snapshot.Target, strikeIndex, [], TargetKoAfterStrike: true));
                    continue;
                }

                DclMagicStrikeGateResult strikeGate;
                if (area.DeliveryGate == DclAreaDeliveryGate.Dodge && targetGate.BaseSucceeded && targetGate.TargetSucceeded)
                {
                    bool critical = targetGate.TargetCritical;
                    IReadOnlyList<int> oneRoll = critical
                        ? []
                        : executionBattle is not null
                            ? [executionBattle.ExecutionRandom.Roll3D6(executionBattle.RollIdentity(
                                input.ActionInstanceId,
                                input.DeclarationRequest.Caster,
                                snapshot.Target,
                                strikeIndex,
                                DclRollSite.ActiveDefense,
                                drawIndex: 0))]
                            : dodgeDrawIndex < targetInput.DodgeRolls.Count
                                ? [targetInput.DodgeRolls[dodgeDrawIndex++]]
                                : throw new ArgumentException("Area Dodge is missing one per-Strike defense draw.", nameof(input));
                    int immediateDodgePenalty = immediateApplication
                        ? (immediateStun ? 4 : 0) + (immediateKnockedDown ? 3 : 0)
                        : 0;
                    DclDefenseOption? currentDodge = targetInput.Dodge is { } dodge
                        ? dodge with { Target = checked(dodge.Target - immediateDodgePenalty) }
                        : null;
                    strikeGate = DclMagicActionGates.ResolveAreaTarget(
                        snapshot.Target,
                        sharedCasterRoll,
                        input.BaseSpellScore,
                        targetInput.TargetSpellScore,
                        DclAreaDeliveryGate.Dodge,
                        strikeCount: 1,
                        currentDodge,
                        oneRoll,
                        resistanceScore: null,
                        resistanceRoll: null).Strikes[0] with { StrikeIndex = strikeIndex };
                }
                else
                {
                    strikeGate = sharedTargetGate!.Strikes[strikeIndex];
                }

                DclMagicalEffectResult? effect = null;
                DclInjuryResult? baseInjury = null;
                DclHealingResult? healing = null;
                DclCanonicalResourceChangeResult? resourceChange = null;
                DclCanonicalForcedMovementResult? forcedMovement = null;
                DclConcentrationResult? concentration = null;
                DclInjuryConsequenceResult? injuryConsequences = null;
                DclCanonicalAimLifecycleResult? aimRetention = null;
                IReadOnlyList<int>? magnitudeDice = targetInput.MagnitudeDiceByStrike[strikeIndex];
                bool targetKo = false;
                if (strikeGate.Landed && !statusOnly)
                {
                    if (forcedMovementOnly)
                    {
                        forcedMovement = plannedForcedMovement;
                        if (forcedMovement?.CancelAim == true && targetAimAvailable && executionBattle is not null)
                        {
                            aimRetention = DclCanonicalAimLifecycle.PlanCancelOwner(
                                executionBattle.States,
                                snapshot.Target,
                                "forced-movement-cancelled");
                            if (aimRetention is { HadAim: true, InstanceId: { } removedAimId })
                            {
                                targetAimAvailable = false;
                                plannedTargetAimRemovalIds.Add(removedAimId);
                            }
                        }
                        if (forcedMovement?.CreatesConcentrationIncident == true)
                        {
                            DclCanonicalConcentrationTargetContext context =
                                targetInput.ForcedMovementConcentrationContext ??
                                new DclCanonicalConcentrationTargetContext(
                                    Charging: false,
                                    Will: 1,
                                    ConcentrationModifier: 0,
                                    StatePenaltyMagnitude: 0);
                            int? concentrationRoll = targetInput.ForcedMovementConcentrationRoll;
                            if (context.Charging && executionBattle is not null)
                                concentrationRoll = executionBattle.ExecutionRandom.Roll3D6(
                                    executionBattle.RollIdentity(
                                        input.ActionInstanceId,
                                        input.DeclarationRequest.Caster,
                                        snapshot.Target,
                                        strikeIndex,
                                        DclRollSite.Concentration,
                                        drawIndex: 0));
                            concentration = DclConcentration.ResolveStrikeIncident(
                                context.Charging,
                                directCancellation: false,
                                injury: 0,
                                forcedDisplacement: forcedMovement.MovedTiles,
                                context.Will,
                                context.ConcentrationModifier,
                                context.StatePenaltyMagnitude,
                                concentrationRoll);
                        }
                    }
                    else
                    {
                    DclDiceExpression strikeDiceExpression = diceExpression;
                    if (healingMagnitude is not null &&
                        strikeGate.Outcome == DclMagicDeliveryOutcome.CriticalDelivered &&
                        profile.CriticalProfile.SuccessEffect == DclCriticalSuccessEffect.MaximizeOneHealingDie)
                    {
                        DclCriticalHealingExpression critical =
                            DclMagicMagnitude.CriticalHealingExpression(diceExpression);
                        strikeDiceExpression = new DclDiceExpression(critical.DiceToRoll, critical.Adds);
                    }
                    magnitudeDice ??= executionBattle?.ExecutionRandom.RollD6Pool(
                        executionBattle.RollIdentity(
                            input.ActionInstanceId,
                            input.DeclarationRequest.Caster,
                            snapshot.Target,
                            strikeIndex,
                            damageMagnitude is not null ? DclRollSite.DamageDie :
                                healingMagnitude is not null ? DclRollSite.HealingDie :
                                DclRollSite.ResourceMagnitudeDie,
                            drawIndex: 0),
                        strikeDiceExpression.Dice);
                    if (magnitudeDice is null)
                        throw new ArgumentNullException(nameof(input), "A landed area Strike requires its exact magnitude draws.");
                    int raw = Math.Max(0, DclInjury.RollDamage(strikeDiceExpression, magnitudeDice));
                    if (damageMagnitude is not null)
                    {
                        baseInjury = DclCanonicalMagnitude.ResolveMagicalBaseInjury(
                            raw,
                            damageMagnitude.DamageType,
                            targetInput.ApplicableDr,
                            profile.DeliveryProfile);
                        effect = DclMagicalEffect.ResolveInjuryOrAbsorb(
                            baseInjury.Value.Injury,
                            targetInput.Affinity,
                            targetInput.FaithMagnitude,
                            profile.DeliveryProfile.ShellSensitive,
                            targetInput.TargetHasShell,
                            remainingHp,
                            targetInput.TargetMaxHp,
                            targetInput.FireEffect,
                            oilAvailable);
                        if (effect.Value.Route == DclMagicalEffectRoute.Injury)
                        {
                            DclCanonicalInjuryTargetContext injuryContext = targetInput.InjuryTargetContext!.Value;
                            DclInjuryResult finalInjury = baseInjury.Value with { Injury = effect.Value.FinalInjury };
                            bool targetSurvivesInjury = remainingHp > finalInjury.Injury;
                            int requestedInjuryMovement = checked(
                                targetInput.AuthoredForcedDisplacement + DclInjury.CriticalKnockbackTiles(
                                    strikeGate.Outcome == DclMagicDeliveryOutcome.CriticalDelivered,
                                    targetSurvivesInjury,
                                    damageMagnitude.DamageType,
                                    finalInjury.RolledDamage,
                                    finalInjury.PenetratingDamage,
                                    injuryContext.TargetSt));
                            if (requestedInjuryMovement > 0)
                            {
                                forcedMovement = targetInput.InjuryMovementBranchForestsByStrike?[strikeIndex]?.Resolve(
                                    snapshot.Target,
                                    movementTile,
                                    targetKo: !targetSurvivesInjury,
                                    requestedInjuryMovement) ?? targetInput.InjuryMovementBranchesByStrike?[strikeIndex]?.Resolve(
                                        snapshot.Target,
                                        targetKo: !targetSurvivesInjury,
                                        requestedInjuryMovement) ?? throw new InvalidOperationException(
                                        "Area Injury displacement requires its frozen native map branch.");
                                if (forcedMovement.Origin != movementTile)
                                    throw new InvalidOperationException(
                                        "Area Injury movement diverges from its selected conditional origin timeline.");
                                if (forcedMovement.MovedTiles > 0)
                                    movementTile = forcedMovement.Destination;
                            }
                            int settledInjuryMovement = forcedMovement?.MovedTiles ?? 0;
                            DclCanonicalInjuryConsequenceRolls? suppliedRolls =
                                targetInput.InjuryConsequenceRollsByStrike![strikeIndex];
                            DclCanonicalInjuryConsequenceRolls rolls = suppliedRolls ?? (executionBattle is not null
                                ? DclCanonicalInjuryRandomPlanner.PlanRolls(
                                    executionBattle,
                                    input.ActionInstanceId,
                                    input.DeclarationRequest.Caster,
                                    snapshot.Target,
                                    strikeIndex,
                                    remainingHp,
                                    finalInjury,
                                    damageMagnitude.DamageType,
                                    strikeGate.Outcome == DclMagicDeliveryOutcome.CriticalDelivered,
                                    injuryContext with
                                    {
                                        UnexpiredShockInjury = shockInjury,
                                        Charging = charging,
                                    },
                                    targetInput.DirectConcentrationCancellation,
                                    targetInput.AuthoredForcedDisplacement,
                                    settledInjuryMovement)
                                : throw new ArgumentNullException(nameof(input), "A landed Area Injury requires its exact conditional consequence draws."));
                            injuryConsequences = DclInjury.ResolveConsequences(
                                remainingHp,
                                baseInjury.Value with { Injury = effect.Value.FinalInjury },
                                damageMagnitude.DamageType,
                                strikeGate.Outcome == DclMagicDeliveryOutcome.CriticalDelivered,
                                new DclInjuryConsequenceInput(
                                    injuryContext.MaxHp,
                                    injuryContext.TargetSt,
                                    injuryContext.EffectiveHt,
                                    shockInjury,
                                    charging,
                                    injuryContext.Will,
                                    injuryContext.ConcentrationModifier,
                                    injuryContext.ConcentrationStatePenaltyMagnitude,
                                    rolls.MajorWoundHtRoll,
                                    rolls.DirectConcentrationCancellation,
                                    rolls.AuthoredForcedDisplacement,
                                    rolls.ConcentrationRoll,
                                    rolls.SettledForcedDisplacement));
                            if (injuryConsequences.Value.TotalForcedDisplacement != settledInjuryMovement)
                                throw new InvalidOperationException(
                                    "Area Injury consequence and settled map verdict disagree on actual displacement.");
                            shockInjury = injuryConsequences.Value.UnexpiredShockInjury;
                            if (injuryConsequences.Value.Concentration.Outcome is
                                DclConcentrationOutcome.Interrupted or DclConcentrationOutcome.DirectCancellation)
                                charging = false;
                            if (immediateApplication && injuryConsequences.Value.ApplyStun)
                            {
                                immediateStun = true;
                                immediateKnockedDown = injuryConsequences.Value.ApplyKnockedDown;
                            }
                            remainingHp = Math.Max(0, remainingHp - effect.Value.FinalInjury);
                            targetKo = remainingHp == 0;
                            if (executionBattle is not null)
                            {
                                bool targetHasAim = targetAimAvailable;
                                bool directAimCancellation = targetKo || injuryConsequences.Value.ApplyStun ||
                                    injuryConsequences.Value.ApplyKnockedDown || forcedMovement?.CancelAim == true;
                                if (targetHasAim && effect.Value.FinalInjury > 0)
                                {
                                    if (directAimCancellation)
                                    {
                                        string reason = targetKo ? "ko-cancelled"
                                            : injuryConsequences.Value.ApplyStun ? "stun-cancelled"
                                            : injuryConsequences.Value.ApplyKnockedDown ? "knocked-down-cancelled"
                                            : "forced-movement-cancelled";
                                        aimRetention = DclCanonicalAimLifecycle.PlanCancelOwner(
                                            executionBattle.States,
                                            snapshot.Target,
                                            reason);
                                    }
                                    else
                                    {
                                        int retentionRoll = executionBattle.ExecutionRandom.Roll3D6(
                                            executionBattle.RollIdentity(
                                                input.ActionInstanceId,
                                                input.DeclarationRequest.Caster,
                                                snapshot.Target,
                                                strikeIndex,
                                                DclRollSite.AimRetention,
                                                drawIndex: 0));
                                        aimRetention = DclCanonicalAimLifecycle.PlanInjuryRetention(
                                            executionBattle.States,
                                            snapshot.Target,
                                            effect.Value.FinalInjury,
                                            forcedMovement: false,
                                            injuryContext.Will,
                                            targetInput.AimRetentionModifier,
                                            targetInput.AimRetentionStatePenaltyMagnitude,
                                            retentionRoll);
                                    }
                                    if (aimRetention is { HadAim: true, Retained: false, InstanceId: { } removedAimId })
                                    {
                                        targetAimAvailable = false;
                                        plannedTargetAimRemovalIds.Add(removedAimId);
                                    }
                                }
                            }
                        }
                        else if (effect.Value.Route == DclMagicalEffectRoute.AbsorbedHealing)
                        {
                            if (targetInput.InjuryConsequenceRollsByStrike![strikeIndex] is not null)
                                throw new ArgumentException("Absorbed Area magic cannot own Injury-consequence draws.", nameof(input));
                            remainingHp = Math.Min(
                                targetInput.TargetMaxHp,
                                remainingHp + effect.Value.AppliedHealing);
                        }
                        else if (targetInput.InjuryConsequenceRollsByStrike![strikeIndex] is not null)
                        {
                            throw new ArgumentException("Non-injuring Area magic cannot own Injury-consequence draws.", nameof(input));
                        }
                        if (effect.Value.ConsumeOil)
                            oilAvailable = false;
                    }
                    else if (healingMagnitude is not null)
                    {
                        healing = DclMagicalEffect.ResolveHealing(
                            raw,
                            targetInput.FaithMagnitude,
                            remainingHp,
                            targetInput.TargetMaxHp);
                        remainingHp = Math.Min(
                            targetInput.TargetMaxHp,
                            remainingHp + healing.Value.AppliedHealing);
                    }
                    else if (resourceMagnitude is not null)
                    {
                        resourceChange = DclCanonicalResourceChange.Resolve(
                            profile,
                            currentTargetPools!,
                            currentSourcePools!,
                            magnitudeDice);
                        currentTargetPools = ApplyChannels(currentTargetPools!, resourceChange.TargetChannels);
                        currentSourcePools = ApplyChannels(currentSourcePools!, resourceChange.SourceChannels);
                        remainingHp = currentTargetPools.CurrentHp;
                        targetKo = resourceChange.TargetKo;
                    }

                    if (riderEffectIndexes.Length > 0 && !riderResultsByTarget.ContainsKey(snapshot.Target))
                    {
                        var targetRiderResults = new List<DclCanonicalStatusRiderResult>(riderEffectIndexes.Length);
                        foreach (int effectIndex in riderEffectIndexes)
                        {
                            DclCanonicalStatusRiderInput rider = targetRidersByEffect[effectIndex];
                            int? resistanceRoll = rider.ResistanceRoll;
                            DclStateResistanceGate resistanceGate = riderDefinitions[effectIndex].ResistanceGate;
                            if (!rider.Immune && executionBattle is not null &&
                                resistanceGate is DclStateResistanceGate.SuccessRoll or DclStateResistanceGate.QuickContest)
                            {
                                resistanceRoll = executionBattle.ExecutionRandom.Roll3D6(
                                    executionBattle.RollIdentity(
                                        input.ActionInstanceId,
                                        input.DeclarationRequest.Caster,
                                        snapshot.Target,
                                        strikeIndex,
                                        DclRollSite.Resistance,
                                        drawIndex: effectIndex));
                            }
                            DclMagicRiderResult gate = DclMagicActionGates.ResolveStatusRider(
                                targetGate,
                                carrierLanded: true,
                                rider.ResistanceScore,
                                resistanceRoll,
                                rider.Immune,
                                resistanceGate);
                            targetRiderResults.Add(new DclCanonicalStatusRiderResult(
                                effectIndex,
                                gate,
                                StateApplication: null));
                        }
                        riderResultsByTarget.Add(snapshot.Target, targetRiderResults);
                        riderCarrierStrikeByTarget.Add(snapshot.Target, strikeIndex);
                    }
                    }
                }
                if (statusOnly && !riderResultsByTarget.ContainsKey(snapshot.Target))
                {
                    DclMagicRiderResult gate = ResolveAreaStatusCarrier(
                        targetGate,
                        strikeGate,
                        area.DeliveryGate,
                        carrierResistanceRoll,
                        statusCarrierInput!);
                    riderResultsByTarget.Add(snapshot.Target,
                    [
                        new DclCanonicalStatusRiderResult(0, gate, StateApplication: null),
                    ]);
                    riderCarrierStrikeByTarget.Add(snapshot.Target, strikeIndex);
                }
                if (!strikeGate.Landed && magnitudeDice is not null)
                {
                    throw new ArgumentException("A failed or defended area Strike cannot own magnitude draws.", nameof(input));
                }
                if (!strikeGate.Landed && targetInput.InjuryConsequenceRollsByStrike?[strikeIndex] is not null)
                    throw new ArgumentException("A failed or defended area Strike cannot own Injury-consequence draws.", nameof(input));
                if (!strikeGate.Landed && targetInput.ForcedMovementConcentrationRoll is not null)
                    throw new ArgumentException(
                        "A failed or defended Area ForcedMovement Strike cannot own concentration RNG.", nameof(input));
                strikeResults.Add(new DclCanonicalAreaMagicStrikeResult(
                    snapshot.Target,
                    strikeIndex,
                    strikeGate.Outcome,
                    baseInjury,
                    effect,
                    healing,
                    injuryConsequences,
                    TargetKoAfterStrike: targetKo,
                    KoShortCircuited: false,
                    aimRetention,
                    StatusRiders: [],
                    ResourceChange: resourceChange,
                    ForcedMovement: forcedMovement,
                    Concentration: concentration));
                var appliedEffectIndexes = new List<int>();
                if (!statusOnly && strikeGate.Landed && resourceChange?.RejectedByUndeadPolicy != true)
                    appliedEffectIndexes.Add(0);
                if (riderCarrierStrikeByTarget.TryGetValue(snapshot.Target, out int riderStrikeIndex) &&
                    riderStrikeIndex == strikeIndex)
                    appliedEffectIndexes.AddRange(riderResultsByTarget[snapshot.Target]
                        .Where(rider => rider.Gate.Applied)
                        .Select(rider => rider.EffectIndex));
                resolved.Add(new DclCanonicalResolvedStrike(
                    snapshot.Target,
                    strikeIndex,
                    appliedEffectIndexes,
                    TargetKoAfterStrike: targetKo));
            }
            if (dodgeDrawIndex != targetInput.DodgeRolls.Count)
                throw new ArgumentException("Area Dodge input contains draws for a target/Strike that never reached that random site.", nameof(input));
            if (!riderResultsByTarget.ContainsKey(snapshot.Target) &&
                targetRiders.Any(rider => rider.ResistanceRoll is not null))
                throw new ArgumentException(
                    "An Area target whose Carrier never landed cannot own Rider resistance draws.",
                    nameof(input));
        }
        DclCanonicalTransactionResult committed = DclCanonicalTransactionExecutor.Commit(
            attempt.Declaration,
            profile,
            targetBatch.Targets,
            resolved,
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                if (plannedTargetAimRemovalIds.Count > 0)
                {
                    IReadOnlyList<long> removedAimIds = executionBattle!.States.RemoveInstances(plannedTargetAimRemovalIds);
                    if (removedAimIds.Count != plannedTargetAimRemovalIds.Count)
                        throw new InvalidOperationException("Target Aim state changed before Area commit.");
                }
                for (int strikeResultIndex = 0; strikeResultIndex < strikeResults.Count; strikeResultIndex++)
                {
                    DclCanonicalAreaMagicStrikeResult strike = strikeResults[strikeResultIndex];
                    if (strike.Concentration is { } concentration)
                    {
                        DclCanonicalChargedCancellation? cancellation = executionBattle?.ResolveConcentrationIncident(
                            strike.Target,
                            concentration) ?? (concentration.Outcome == DclConcentrationOutcome.NoIncident
                                ? null
                                : throw new InvalidOperationException(
                                    "An executable Area ForcedMovement concentration incident lost its timeline owner."));
                        strike = strike with { ChargingCancellation = cancellation };
                        strikeResults[strikeResultIndex] = strike;
                    }
                    if (strike.Effect is not { Route: DclMagicalEffectRoute.Injury } injuryEffect ||
                        strike.BaseInjury is not { } baseInjury ||
                        strike.InjuryConsequences is not { } consequences)
                        continue;
                    DclCanonicalAreaTargetInput targetInput = targetInputs[strike.Target];
                    DclCanonicalChargedCancellation? injuryChargingCancellation =
                        executionBattle?.ResolveInjuryConcentration(strike.Target, consequences) ??
                        (consequences.Concentration.Outcome == DclConcentrationOutcome.NoIncident
                            ? null
                            : throw new InvalidOperationException(
                                "An executable Area Injury concentration incident lost its timeline owner."));
                    DclCanonicalInjuryStateCommitResult? injuryStates = targetInput.InjuryStateCommit is { } injuryCommit
                        ? DclCanonicalInjuryConsequenceCommitter.Commit(
                            runtime,
                            baseInjury with { Injury = injuryEffect.FinalInjury },
                            consequences,
                            strike.Target,
                            input.DeclarationRequest.Caster,
                            targetInput.TargetMaxHp,
                            injuryCommit)
                        : null;
                    strikeResults[strikeResultIndex] = strike with
                    {
                        InjuryStates = injuryStates,
                        ChargingCancellation = injuryChargingCancellation,
                    };
                }
                foreach (DclTargetResolutionSnapshot snapshot in targetBatch.Targets)
                {
                    if (!riderResultsByTarget.TryGetValue(snapshot.Target, out List<DclCanonicalStatusRiderResult>? riderResults))
                        continue;
                    Dictionary<int, DclCanonicalStatusRiderInput> targetRiders =
                        (targetInputs[snapshot.Target].StatusRiders ?? []).ToDictionary(rider => rider.EffectIndex);
                    for (int index = 0; index < riderResults.Count; index++)
                    {
                        DclCanonicalStatusRiderResult riderResult = riderResults[index];
                        if (!riderResult.Gate.Applied) continue;
                        DclCanonicalStatusRiderInput rider = targetRiders[riderResult.EffectIndex];
                        DclCanonicalStateMaterialization materialization = rider.StateMaterialization;
                        DclStateDefinition definition = riderDefinitions[riderResult.EffectIndex];
                        var stateApplication = new DclStateApplication(
                            definition,
                            snapshot.Target,
                            materialization.BindSource ? input.DeclarationRequest.Caster : null,
                            rider.StateRegistry.CurrentGlobalCt,
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
                        DclStateApplicationResult application = executionBattle is not null
                            ? executionBattle.ApplyState(stateApplication)
                            : rider.StateRegistry.Apply(stateApplication);
                        riderResults[index] = riderResult with { StateApplication = application };
                    }
                }
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: input.ExecutionBattle?.States);
        strikeResults = strikeResults.Select(strike =>
            riderCarrierStrikeByTarget.TryGetValue(strike.Target, out int carrierStrike) &&
            carrierStrike == strike.StrikeIndex
                ? strike with { StatusRiders = riderResultsByTarget[strike.Target].ToArray() }
                : strike).ToList();
        return new DclCanonicalAreaMagicExecutionResult(
            attempt.Declaration,
            baseGate.BaseOutcome,
            center,
            payment,
            strikeResults,
            committed.Transaction,
            committed.Reactions);
    }

    internal static void RequireSupportedRiderTiming(DclActionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        bool hasStatusRider = profile.Effects.Skip(1).Any(effect => effect is
        {
            Role: DclEffectRole.Rider,
            Kind: DclEffectKind.StatusApplication,
        });
        if (hasStatusRider && profile.TransactionProfile.StrikeCount > 1 &&
            profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate)
            throw new InvalidOperationException(
                "Multi-Strike Area Riders with Immediate application require between-Strike mechanical reprojection; use Deferred until that adapter is bound.");
    }

    private static DclMagicTargetGateResult ImmuneAreaCarrierTarget(
        DclUnitKey target,
        DclSpellGateResult castingGate)
    {
        if (!castingGate.BaseSucceeded || !castingGate.TargetSucceeded)
            throw new ArgumentException("Only a successful casting gate can reach status immunity.", nameof(castingGate));
        return new DclMagicTargetGateResult(
            target,
            castingGate,
            [new DclMagicStrikeGateResult(
                StrikeIndex: 0,
                DclMagicDeliveryOutcome.Resisted,
                Landed: false,
                TargetGateRolled: false)],
            TargetGateRollCount: 0);
    }

    private static DclMagicRiderResult ResolveAreaStatusCarrier(
        DclSpellGateResult castingGate,
        DclMagicStrikeGateResult delivery,
        DclAreaDeliveryGate deliveryGate,
        int? resistanceRoll,
        DclCanonicalStatusRiderInput carrier)
    {
        if (!castingGate.BaseSucceeded || !castingGate.TargetSucceeded)
            return new DclMagicRiderResult(false, false, 0, 0, "carrier-did-not-land");
        if (carrier.Immune)
        {
            if (resistanceRoll is not null)
                throw new ArgumentException("Status immunity is checked before the Area Carrier resistance roll.", nameof(resistanceRoll));
            return new DclMagicRiderResult(false, false, 0, 0, "immune");
        }
        if (deliveryGate == DclAreaDeliveryGate.QuickContest)
        {
            if (resistanceRoll is null)
                throw new ArgumentNullException(nameof(resistanceRoll), "A QuickContest Area status Carrier requires its delivery resistance roll.");
            DclQuickContestResult contest = DclQuickContest.Resolve(
                castingGate.TargetSpellScore,
                castingGate.SharedRoll,
                carrier.ResistanceScore,
                resistanceRoll.Value);
            if (delivery.Landed != contest.ActingSideWon)
                throw new InvalidOperationException("Area status Carrier delivery and its single Quick Contest disagree.");
            return new DclMagicRiderResult(
                Attempted: true,
                Applied: contest.ActingSideWon,
                contest.ActingMargin,
                contest.TargetMargin,
                contest.ActingSideWon
                    ? "area-status-carrier-applied"
                    : "area-status-carrier-resisted-tie-or-better");
        }
        if (!delivery.Landed)
            return new DclMagicRiderResult(false, false, 0, 0, "carrier-did-not-land");
        if (resistanceRoll is not null)
            throw new ArgumentException("A non-contested Area status Carrier cannot own a resistance roll.", nameof(resistanceRoll));
        return new DclMagicRiderResult(false, true, 0, 0, "area-status-carrier-applied-no-resistance");
    }

    private static void RequireNoPreSuppliedExecutionDraws(DclCanonicalAreaMagicExecutionInput input)
    {
        if (input.SharedCasterRoll is not null || input.Targets.Any(target =>
                target.ResistanceRoll is not null || target.DodgeRolls.Count != 0 ||
                target.MagnitudeDiceByStrike.Any(draws => draws is not null) ||
                target.InjuryConsequenceRollsByStrike?.Any(draws => draws is not null) == true ||
                (target.StatusRiders ?? []).Any(rider => rider.ResistanceRoll is not null)))
            throw new ArgumentException(
                "Confirmed Area execution accepts deterministic inputs only; the battle ledger owns every draw.",
                nameof(input));
    }

    private static DclCanonicalResourcePoolSnapshot ApplyChannels(
        DclCanonicalResourcePoolSnapshot pools,
        DclCanonicalNativeNumericChannels channels)
    {
        ArgumentNullException.ThrowIfNull(pools);
        int hp = checked(pools.CurrentHp - channels.HpDebit + channels.HpCredit);
        int mp = checked(pools.CurrentMp - channels.MpDebit + channels.MpCredit);
        if (hp < 0 || hp > pools.MaxHp || mp < 0 || mp > pools.MaxMp)
            throw new InvalidOperationException("Area ResourceChange channels escaped their snapshotted pool bounds.");
        return pools with { CurrentHp = hp, CurrentMp = mp };
    }
}
