namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalPhysicalStrikeInput(
    DclUnitKey Target,
    int StrikeIndex,
    int EffectiveAttackSkill,
    int? AttackRoll,
    IReadOnlyList<DclDefenseCandidate> DefenseCandidates,
    int? DefenseRoll,
    IReadOnlyList<int>? DamageDice,
    int ApplicableDr,
    int? MajorWoundHtRoll = null,
    bool DirectConcentrationCancellation = false,
    int AuthoredForcedDisplacement = 0,
    int? ConcentrationRoll = null,
    DclCanonicalRangedStrikeContext? Ranged = null,
    int? AimRetentionRoll = null,
    IReadOnlyList<DclCanonicalPhysicalStatusRiderInput>? StatusRiders = null,
    DclCanonicalInjuryMovementBranchSet? InjuryMovementBranches = null,
    DclCanonicalInjuryMovementBranchForest? InjuryMovementBranchForest = null,
    DclSkillTrainingResult? WeaponSkillTraining = null);

internal sealed record DclCanonicalPhysicalStatusRiderInput(
    int EffectIndex,
    int ResistanceScore,
    int? ResistanceRoll,
    bool Immune,
    DclStateRegistry StateRegistry,
    DclCanonicalStateMaterialization StateMaterialization);

internal sealed record DclCanonicalPhysicalTargetContext(
    DclUnitKey Target,
    int MaxHp,
    int TargetSt,
    int EffectiveHt,
    int UnexpiredShockInjury,
    bool Charging,
    int Will,
    int ConcentrationModifier,
    int ConcentrationStatePenaltyMagnitude,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0,
    DclBattleTile? MovementOrigin = null);

internal sealed record DclCanonicalRangedStrikeContext(
    DclRangedWeaponKind WeaponKind,
    int BaseWeaponSkill,
    int HorizontalTiles,
    int LocationPenaltyMagnitude,
    int ShockStatePenaltyMagnitude,
    bool NativeRangeLegal,
    bool NativeTrajectoryLegal,
    bool MovedBeforeShot,
    DclAimState? Aim,
    long? AimStateInstanceId = null);

internal sealed record DclCanonicalPhysicalStrikeResult(
    DclUnitKey Target,
    int StrikeIndex,
    DclPhysicalContestResult? Contest,
    DclInjuryResult? Injury,
    DclInjuryConsequenceResult? Consequences,
    bool TargetKoAfterStrike,
    bool KoShortCircuited,
    DclCanonicalAimLifecycleResult? AimRetention = null,
    DclCanonicalInjuryStateCommitResult? InjuryStates = null,
    IReadOnlyList<DclCanonicalStatusRiderResult>? StatusRiders = null,
    DclCanonicalChargedCancellation? ChargingCancellation = null,
    DclCanonicalForcedMovementResult? ForcedMovement = null);

internal sealed record DclCanonicalPhysicalExecutionInput(
    int AbilityId,
    DclActionDeclaration Declaration,
    int WeaponItemId,
    int SourceSt,
    IReadOnlyList<DclTargetResolutionSnapshot> Targets,
    IReadOnlyList<DclCanonicalPhysicalTargetContext> TargetContexts,
    IReadOnlyList<DclCanonicalPhysicalStrikeInput> Strikes,
    DclTurnResources TurnResources,
    DclWeaponActionState WeaponActionState,
    DclStateRegistry? BattleStateRegistry = null,
    DclCanonicalBattleRuntime? ExecutionBattle = null,
    DclCanonicalReactionWindowRequest? ReactionWindow = null,
    IReadOnlyList<DclCanonicalPhysicalStrikeWeapon>? StrikeWeapons = null,
    IReadOnlyList<DclCanonicalPhysicalStrikeWeaponState>? StrikeWeaponStates = null);

internal sealed record DclCanonicalPhysicalExecutionResult(
    IReadOnlyList<DclCanonicalPhysicalStrikeResult> Strikes,
    DclCanonicalTransactionResult Commit,
    IReadOnlyDictionary<DclUnitKey, DclDefenseResourceSnapshot> FinalDefenseResources,
    DclCanonicalProtectionRedirectPlan? ProtectionRedirectPlan = null,
    DclCanonicalProtectionRedirectResolution? ProtectionRedirect = null);

internal static class DclCanonicalPhysicalExecutor
{
    public static DclCanonicalPhysicalExecutionResult Resolve(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalPhysicalExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.NativeRepeat))
            throw new InvalidOperationException("The canonical physical executor received an incompatible carrier.");
        if (profile.DeliveryProfile.Delivery != DclDelivery.PhysicalAttack ||
            profile.MagnitudeProfile is not DclDamageMagnitude damageMagnitude ||
            profile.Effects.Count == 0 || profile.Effects[0] is not
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.Damage,
            } || profile.Effects.Skip(1).Any(effect => effect is not
            {
                Role: DclEffectRole.Rider,
                Kind: DclEffectKind.StatusApplication,
            }))
            throw new InvalidOperationException("The physical vertical requires one Damage Carrier followed only by normalized status Riders.");
        profile = DclCanonicalPhysicalStrikeCardinality.ResolveEffectiveProfile(
            binding,
            profile,
            input.Targets,
            input.Strikes);
        DclCanonicalInjuryConsequenceCommitter.RequireSupportedTiming(profile);
        if (profile.Effects.Count > 1 && profile.TransactionProfile.StrikeCount > 1 &&
            profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate)
            throw new InvalidOperationException("Immediate multi-Strike physical status Riders require an explicit between-Strike state reprojection owner.");
        if (input.Declaration.ActionId != profile.ActionId || input.Declaration.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Physical declaration does not match the bound profile revision.", nameof(input));
        DclCanonicalPhysicalStrikeWeapon[] strikeWeapons = DclCanonicalPhysicalStrikeWeapons.Normalize(
            profile.TransactionProfile.StrikeCount,
            input.WeaponItemId,
            input.WeaponActionState.ResourceKey,
            input.StrikeWeapons);
        IReadOnlyDictionary<int, DclWeaponMetadata> weapons =
            DclCanonicalPhysicalStrikeWeapons.ResolveMetadata(runtime, strikeWeapons);
        DclCanonicalPhysicalStrikeWeaponState[] strikeWeaponStates = ResolveStrikeWeaponStates(
            strikeWeapons,
            input.WeaponActionState,
            input.StrikeWeaponStates);
        DclCanonicalBattleRuntime? executionBattle = input.ExecutionBattle;
        DclStateRegistry? battleStates = executionBattle?.States ?? input.BattleStateRegistry;
        if (executionBattle is not null)
        {
            if (!ReferenceEquals(executionBattle.Catalog, runtime) ||
                executionBattle.BattleGeneration != input.Declaration.Source.BattleGeneration ||
                input.BattleStateRegistry is not null && !ReferenceEquals(input.BattleStateRegistry, executionBattle.States))
                throw new ArgumentException("Physical execution battle/catalog/state ownership is inconsistent.", nameof(input));
            if (!executionBattle.TryGetObservedUnit(input.Declaration.Source.UnitSlot, out DclUnitKey observedSource) ||
                observedSource != input.Declaration.Source || input.Targets.Any(target =>
                    !executionBattle.TryGetObservedUnit(target.Target.UnitSlot, out DclUnitKey observedTarget) ||
                    observedTarget != target.Target))
                throw new ArgumentException("Physical execution requires current observed source and target UnitKeys.", nameof(input));
            if (!ReferenceEquals(input.TurnResources, executionBattle.TurnResources(input.Declaration.Source)) ||
                strikeWeaponStates.Any(entry => !ReferenceEquals(entry.State, executionBattle.WeaponState(
                    input.Declaration.Source,
                    entry.Weapon.WeaponResourceKey,
                    weapons[entry.Weapon.StrikeIndex].Balance,
                    weapons[entry.Weapon.StrikeIndex].Readiness))))
                throw new ArgumentException("Physical execution must use battle-owned turn and weapon resources.", nameof(input));
            if (input.ReactionWindow is not { } ownedReactionWindow ||
                !ReferenceEquals(ownedReactionWindow.Runtime, runtime) ||
                !ReferenceEquals(ownedReactionWindow.ExecutionBattle, executionBattle))
                throw new ArgumentException(
                    "Confirmed physical execution requires its exact battle-owned Reaction window, including when empty.",
                    nameof(input));
            foreach (DclCanonicalPhysicalStrikeInput strike in input.Strikes)
                RequireNoPreSuppliedExecutionDraws(strike);
        }
        if (input.SourceSt < 1)
            throw new ArgumentOutOfRangeException(nameof(input), "Weapon damage requires current ST >= 1.");
        if (input.Targets.Count == 0)
            throw new ArgumentException("Physical execution requires at least one target snapshot.", nameof(input));
        if (!input.TurnResources.CanPay(profile.TimingProfile))
            throw new InvalidOperationException("The physical action cannot pay its normalized turn resources.");
        if (strikeWeaponStates.Any(entry => !entry.State.CanAttack))
            throw new InvalidOperationException("One selected Strike weapon is Unready and cannot begin the physical action.");
        foreach (DclWeaponMetadata weapon in weapons.Values)
        {
            if (weapon.MaximumRange > 0 && profile.TargetProfile.PhysicalRoute != weapon.Trajectory)
                throw new InvalidOperationException("Ranged action routing and one selected Strike weapon's native trajectory disagree.");
            if (weapon.Reach > 0 && profile.TargetProfile.PhysicalRoute == DclPhysicalRoute.None)
                throw new InvalidOperationException("Melee action lost its required native direct/arc target route.");
        }

        long[] aimStateInstanceIds = input.Strikes
            .Where(strike => strike.Ranged?.Aim is { Active: true })
            .Select(strike => strike.Ranged!.AimStateInstanceId ?? 0)
            .Distinct()
            .ToArray();
        if (aimStateInstanceIds.Length > 0)
        {
            if (battleStates is null || aimStateInstanceIds.Contains(0) ||
                battleStates.BattleGeneration != input.Declaration.Source.BattleGeneration)
                throw new ArgumentException("Persistent Aim discharge requires its exact source registry instance.", nameof(input));
            foreach (long instanceId in aimStateInstanceIds)
            {
                if (!battleStates.TryGet(instanceId, out DclStateInstance instance) ||
                    instance.Target != input.Declaration.Source ||
                    !StringComparer.Ordinal.Equals(instance.Kind, "aim") ||
                    instance.Payload is not DclAimStatePayload)
                    throw new ArgumentException("A ranged Strike references a stale or foreign Aim instance.", nameof(input));
            }
        }
        else if (battleStates is not null && input.Strikes.Any(strike => strike.Ranged?.AimStateInstanceId is not null))
        {
            throw new ArgumentException("Inactive Aim cannot retain a registry discharge identity.", nameof(input));
        }

        if (input.ReactionWindow is { } reactionWindow)
            DclCanonicalReactionWindow.Preflight(input.Declaration, reactionWindow);

        IReadOnlyDictionary<int, DclDiceExpression> damageExpressions = weapons.ToDictionary(
            pair => pair.Key,
            pair => DclCanonicalMagnitude.BuildDiceExpression(
                input.SourceSt,
                pair.Value.DamageBasis,
                pair.Value.FixedDamageExpression,
                checked(pair.Value.DamageModifier + damageMagnitude.IntegerModifier),
                checked(pair.Value.WholeDiceModifier + damageMagnitude.WholeDiceModifier)));
        IReadOnlyDictionary<int, DclRational> armorDivisors = weapons.ToDictionary(
            pair => pair.Key,
            pair => profile.DeliveryProfile.ArmorPolicy == DclArmorPolicy.ArmorDividing
                ? profile.DeliveryProfile.ArmorDivisor!.Value
                : pair.Value.ArmorDivisor);
        bool ignoreDr = profile.DeliveryProfile.ArmorPolicy == DclArmorPolicy.IgnoreDr;

        DclTargetResolutionSnapshot[] targets = input.Targets
            .OrderBy(target => target.Target.UnitSlot)
            .ThenBy(target => target.Target.CharacterId)
            .ToArray();
        var remainingHp = targets.ToDictionary(target => target.Target, target => target.CurrentHp);
        var targetContexts = input.TargetContexts.ToDictionary(context => context.Target, context => context);
        if (targetContexts.Count != input.TargetContexts.Count || targetContexts.Count != targets.Length ||
            targets.Any(target => !targetContexts.ContainsKey(target.Target)))
            throw new ArgumentException("Physical execution requires exactly one consequence context per TargetBatch member.", nameof(input));
        foreach ((DclUnitKey target, DclCanonicalPhysicalTargetContext context) in targetContexts)
        {
            if (context.MaxHp < 1 || remainingHp[target] > context.MaxHp || context.TargetSt < 1 ||
                context.UnexpiredShockInjury < 0 || context.ConcentrationStatePenaltyMagnitude < 0 ||
                context.AimRetentionStatePenaltyMagnitude < 0)
                throw new ArgumentException("Physical target consequence context contains an invalid pool, characteristic, or penalty.", nameof(input));
            if (executionBattle is not null && context.Charging != executionBattle.IsCharging(target))
                throw new ArgumentException(
                    "Physical Injury Charging snapshot diverges from the canonical timeline before RNG.", nameof(input));
        }
        var shockInjury = targetContexts.ToDictionary(pair => pair.Key, pair => pair.Value.UnexpiredShockInjury);
        var charging = targetContexts.ToDictionary(pair => pair.Key, pair => pair.Value.Charging);
        var immediateStun = targets.ToDictionary(target => target.Target, _ => false);
        var immediateKnockedDown = targets.ToDictionary(target => target.Target, _ => false);
        var movementTiles = targetContexts.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.MovementOrigin);
        bool immediateApplication = profile.TransactionProfile.WithinActionApplication ==
            DclWithinActionApplication.Immediate;
        var defenseResources = targets.ToDictionary(
            target => target.Target,
            target => new DclMutableDefenseResources(target.DefenseResources));
        var strikeInputs = input.Strikes.ToDictionary(
            strike => (strike.Target, strike.StrikeIndex),
            strike => strike);
        if (strikeInputs.Count != input.Strikes.Count)
            throw new ArgumentException("Physical execution contains duplicate target/Strike identities.", nameof(input));
        foreach (DclCanonicalPhysicalStrikeInput strike in strikeInputs.Values)
        {
            if (strike.InjuryMovementBranches is not null && strike.InjuryMovementBranchForest is not null)
                throw new ArgumentException(
                    "A physical Strike cannot supply both legacy single-origin and conditional-origin Injury branches.",
                    nameof(input));
            strike.InjuryMovementBranches?.Validate(strike.Target);
            strike.InjuryMovementBranchForest?.Validate(strike.Target);
            if (strike.InjuryMovementBranchForest is not null && movementTiles[strike.Target] is null)
                throw new ArgumentException(
                    "Conditional physical Injury movement requires the target's synchronized initial tile.",
                    nameof(input));
            if (movementTiles[strike.Target] is null && strike.InjuryMovementBranches is not null)
                movementTiles[strike.Target] = strike.InjuryMovementBranches.Origin;
        }
        int[] expectedRiderIndexes = Enumerable.Range(1, profile.Effects.Count - 1).ToArray();
        var riderDefinitions = new Dictionary<int, DclStateDefinition>();
        foreach (int effectIndex in expectedRiderIndexes)
        {
            DclEffectProfile effect = profile.Effects[effectIndex];
            if (effect.ReferencedStateKind is null ||
                !runtime.Authoring.States.TryGetValue(effect.ReferencedStateKind, out DclStateDefinition? definition))
                throw new InvalidOperationException($"Physical status Rider {effectIndex} lost its normalized state definition.");
            if (definition.ResistanceGate == DclStateResistanceGate.Explicit)
                throw new InvalidOperationException(
                    $"Physical status Rider {effectIndex} uses Explicit resistance and requires its named mechanism owner.");
            riderDefinitions.Add(effectIndex, definition);
        }
        foreach (DclCanonicalPhysicalStrikeInput strike in input.Strikes)
        {
            DclCanonicalPhysicalStatusRiderInput[] riders = (strike.StatusRiders ?? []).ToArray();
            if (riders.Select(rider => rider.EffectIndex).Distinct().Count() != riders.Length ||
                !riders.Select(rider => rider.EffectIndex).Order().SequenceEqual(expectedRiderIndexes))
                throw new ArgumentException(
                    "Every physical Strike must supply every normalized status Rider effect exactly once.",
                    nameof(input));
            foreach (DclCanonicalPhysicalStatusRiderInput rider in riders)
            {
                DclStateDefinition definition = riderDefinitions[rider.EffectIndex];
                if (rider.StateRegistry.BattleGeneration != strike.Target.BattleGeneration ||
                    executionBattle is not null && !ReferenceEquals(rider.StateRegistry, executionBattle.States) ||
                    !StringComparer.Ordinal.Equals(rider.StateMaterialization.Payload.SchemaId, definition.PayloadSchema))
                    throw new ArgumentException(
                        "A physical status Rider does not match its target battle or normalized state definition.",
                        nameof(input));
            }
        }
        var results = new List<DclCanonicalPhysicalStrikeResult>();
        var resolved = new List<DclCanonicalResolvedStrike>();
        var plannedTargetAimRemovalIds = new HashSet<long>();
        foreach (DclTargetResolutionSnapshot target in targets)
        {
            bool targetAimAvailable = battleStates?.CaptureTarget(target.Target).Instances.Any(instance =>
                StringComparer.Ordinal.Equals(instance.Kind, "aim")) == true;
            for (int strikeIndex = 0; strikeIndex < profile.TransactionProfile.StrikeCount; strikeIndex++)
            {
                DclWeaponMetadata weapon = weapons[strikeIndex];
                DclDiceExpression damageExpression = damageExpressions[strikeIndex];
                DclRational armorDivisor = armorDivisors[strikeIndex];
                if (!strikeInputs.TryGetValue((target.Target, strikeIndex), out DclCanonicalPhysicalStrikeInput? strike))
                    throw new ArgumentException("Physical execution is missing one declared target/Strike input.", nameof(input));
                if (remainingHp[target.Target] == 0)
                {
                    RequireNoSkippedDraws(strike);
                    results.Add(new DclCanonicalPhysicalStrikeResult(
                        target.Target,
                        strikeIndex,
                        null,
                        null,
                        null,
                        TargetKoAfterStrike: true,
                        KoShortCircuited: true));
                    resolved.Add(new DclCanonicalResolvedStrike(target.Target, strikeIndex, [], TargetKoAfterStrike: true));
                    continue;
                }
                (int effectiveAttackSkill, DclRangedDefenseLegality? rangedDefense) = ResolveAttackSkillAndRangedDefense(
                    weapon,
                    target.Target,
                    strike);
                int? attackRoll = strike.AttackRoll ?? executionBattle?.ExecutionRandom.Roll3D6(
                    executionBattle.RollIdentity(
                        input.Declaration.ActionInstanceId,
                        input.Declaration.Source,
                        target.Target,
                        strikeIndex,
                        DclRollSite.Attack,
                        drawIndex: 0));
                if (attackRoll is null)
                    throw new ArgumentNullException(nameof(input), "An executable physical Strike requires one attack roll.");
                bool critical = DclPhysicalContest.IsCritical(attackRoll.Value, effectiveAttackSkill);
                bool ordinaryAttackSucceeded = !critical &&
                    !DclPhysicalContest.IsFumble(attackRoll.Value, effectiveAttackSkill) &&
                    DclSuccessRoll.Succeeds(attackRoll.Value, effectiveAttackSkill);
                DclDefenseOption defense = new(DclDefenseKind.None, 0, Depletes: false);
                int defenseRoll = -1;
                if (ordinaryAttackSucceeded)
                {
                    DclMutableDefenseResources resources = defenseResources[target.Target];
                    DclDefenseCandidate selected = DclActiveDefenseRules.SelectOne(strike.DefenseCandidates.Select(candidate =>
                    {
                        bool rangedLegal = rangedDefense is null || candidate.Kind switch
                        {
                            DclDefenseKind.Dodge => rangedDefense.Value.Dodge,
                            DclDefenseKind.Block => rangedDefense.Value.Block,
                            DclDefenseKind.Parry => rangedDefense.Value.Parry,
                            _ => true,
                        };
                        candidate = candidate with { Legal = candidate.Legal && rangedLegal };
                        int immediatePenalty = immediateApplication
                            ? (immediateStun[target.Target] ? 4 : 0) +
                              (immediateKnockedDown[target.Target] ? candidate.Kind switch
                              {
                                  DclDefenseKind.Dodge => 3,
                                  DclDefenseKind.Parry or DclDefenseKind.Block => 2,
                                  _ => 0,
                              } : 0)
                            : 0;
                        candidate = candidate with { Score = checked(candidate.Score - immediatePenalty) };
                        if (candidate.Kind == DclDefenseKind.Parry)
                        {
                            bool named = !string.IsNullOrWhiteSpace(candidate.ResourceKey);
                            int score = named
                                ? checked(candidate.Score + resources.CurrentParryPenalty(candidate.ResourceKey!))
                                : candidate.Score;
                            return candidate with { Score = score, Legal = candidate.Legal && named };
                        }
                        if (candidate.Kind == DclDefenseKind.Block)
                            return candidate with { Legal = candidate.Legal && resources.BlockAvailable };
                        return candidate;
                    }));
                    if (selected.Kind != DclDefenseKind.None)
                    {
                        int? selectedDefenseRoll = strike.DefenseRoll ?? executionBattle?.ExecutionRandom.Roll3D6(
                            executionBattle.RollIdentity(
                                input.Declaration.ActionInstanceId,
                                input.Declaration.Source,
                                target.Target,
                                strikeIndex,
                                DclRollSite.ActiveDefense,
                                drawIndex: 0));
                        if (selectedDefenseRoll is null)
                            throw new ArgumentNullException(nameof(input), "An ordinary successful attack with a legal defense requires one defense roll.");
                        defense = new DclDefenseOption(selected.Kind, selected.Score, selected.Kind is DclDefenseKind.Parry or DclDefenseKind.Block);
                        defenseRoll = selectedDefenseRoll.Value;
                        if (selected.Kind == DclDefenseKind.Parry)
                            resources.SpendParryAttempt(selected.ResourceKey!);
                        else if (selected.Kind == DclDefenseKind.Block && !resources.TrySpendBlock())
                            throw new InvalidOperationException("Selected Block was unavailable at its finite-resource commit.");
                    }
                    else if (strike.DefenseRoll is not null)
                    {
                        throw new ArgumentException("No legal active defense may consume defense RNG.", nameof(input));
                    }
                }
                else if (strike.DefenseRoll is not null)
                {
                    throw new ArgumentException("A miss, fumble, or critical hit cannot consume active-defense RNG.", nameof(input));
                }

                DclPhysicalContestResult contest = DclPhysicalContest.Resolve(
                    effectiveAttackSkill,
                    attackRoll.Value,
                    defense,
                    defenseRoll);
                DclInjuryResult? injury = null;
                DclInjuryConsequenceResult? consequences = null;
                DclCanonicalAimLifecycleResult? aimRetention = null;
                DclCanonicalForcedMovementResult? injuryMovement = null;
                var statusRiderResults = new List<DclCanonicalStatusRiderResult>(expectedRiderIndexes.Length);
                bool targetKo = false;
                if (contest.Hit)
                {
                    IReadOnlyList<int>? damageDice = strike.DamageDice ?? executionBattle?.ExecutionRandom.RollD6Pool(
                        executionBattle.RollIdentity(
                            input.Declaration.ActionInstanceId,
                            input.Declaration.Source,
                            target.Target,
                            strikeIndex,
                            DclRollSite.DamageDie,
                            drawIndex: 0),
                        damageExpression.Dice);
                    if (damageDice is null)
                        throw new ArgumentNullException(nameof(input), "A landed physical Strike requires its exact weapon-damage draws.");
                    int rawDamage = DclInjury.RollDamage(damageExpression, damageDice);
                    injury = DclInjury.Resolve(
                        rawDamage,
                        weapon.DamageType,
                        strike.ApplicableDr,
                        armorDivisor,
                        ignoreDr);
                    int hpBeforeInjury = remainingHp[target.Target];
                    DclCanonicalPhysicalTargetContext targetContext = targetContexts[target.Target];
                    bool targetSurvivesInjury = hpBeforeInjury > injury.Value.Injury;
                    int requestedInjuryMovement = checked(
                        strike.AuthoredForcedDisplacement + DclInjury.CriticalKnockbackTiles(
                            contest.Outcome == DclPhysicalOutcome.CriticalHit,
                            targetSurvivesInjury,
                            weapon.DamageType,
                            injury.Value.RolledDamage,
                            injury.Value.PenetratingDamage,
                            targetContext.TargetSt));
                    targetKo = !targetSurvivesInjury;
                    if (requestedInjuryMovement > 0)
                    {
                        DclBattleTile movementOrigin = movementTiles[target.Target] ??
                            throw new InvalidOperationException(
                                "Physical Injury displacement lost its synchronized movement origin.");
                        injuryMovement = strike.InjuryMovementBranchForest?.Resolve(
                            target.Target,
                            movementOrigin,
                            targetKo,
                            requestedInjuryMovement) ?? strike.InjuryMovementBranches?.Resolve(
                                target.Target,
                                targetKo,
                                requestedInjuryMovement) ?? throw new InvalidOperationException(
                                "Physical Injury displacement requires its frozen native map branch.");
                        if (injuryMovement.Origin != movementOrigin)
                            throw new InvalidOperationException(
                                "Physical Injury movement branch diverges from the selected conditional origin.");
                        if (injuryMovement.MovedTiles > 0)
                            movementTiles[target.Target] = injuryMovement.Destination;
                    }
                    int settledInjuryMovement = injuryMovement?.MovedTiles ?? 0;
                    DclInjuryConsequenceInput consequenceInput;
                    if (executionBattle is not null)
                    {
                        var injuryTarget = new DclCanonicalInjuryTargetContext(
                            targetContext.MaxHp,
                            targetContext.TargetSt,
                            targetContext.EffectiveHt,
                            shockInjury[target.Target],
                            charging[target.Target],
                            targetContext.Will,
                            targetContext.ConcentrationModifier,
                            targetContext.ConcentrationStatePenaltyMagnitude);
                        DclCanonicalInjuryConsequenceRolls consequenceRolls =
                            DclCanonicalInjuryRandomPlanner.PlanRolls(
                                executionBattle,
                                input.Declaration.ActionInstanceId,
                                input.Declaration.Source,
                                target.Target,
                                strikeIndex,
                                hpBeforeInjury,
                                injury.Value,
                                weapon.DamageType,
                                contest.Outcome == DclPhysicalOutcome.CriticalHit,
                                injuryTarget,
                                strike.DirectConcentrationCancellation,
                                strike.AuthoredForcedDisplacement,
                                settledInjuryMovement);
                        consequenceInput = DclCanonicalInjuryRandomPlanner.BuildInput(injuryTarget, consequenceRolls);
                    }
                    else
                    {
                        consequenceInput = new DclInjuryConsequenceInput(
                            targetContext.MaxHp,
                            targetContext.TargetSt,
                            targetContext.EffectiveHt,
                            shockInjury[target.Target],
                            charging[target.Target],
                            targetContext.Will,
                            targetContext.ConcentrationModifier,
                            targetContext.ConcentrationStatePenaltyMagnitude,
                            strike.MajorWoundHtRoll,
                            strike.DirectConcentrationCancellation,
                            strike.AuthoredForcedDisplacement,
                            strike.ConcentrationRoll,
                            settledInjuryMovement);
                    }
                    consequences = DclInjury.ResolveConsequences(
                        hpBeforeInjury,
                        injury.Value,
                        weapon.DamageType,
                        contest.Outcome == DclPhysicalOutcome.CriticalHit,
                        consequenceInput);
                    if (consequences.Value.TotalForcedDisplacement != settledInjuryMovement)
                        throw new InvalidOperationException(
                            "Physical Injury consequence and settled map verdict disagree on actual displacement.");
                    shockInjury[target.Target] = consequences.Value.UnexpiredShockInjury;
                    if (consequences.Value.Concentration.Outcome is
                        DclConcentrationOutcome.Interrupted or DclConcentrationOutcome.DirectCancellation)
                        charging[target.Target] = false;
                    if (immediateApplication && consequences.Value.ApplyStun)
                    {
                        immediateStun[target.Target] = true;
                        immediateKnockedDown[target.Target] = consequences.Value.ApplyKnockedDown;
                    }
                    remainingHp[target.Target] = Math.Max(0, remainingHp[target.Target] - injury.Value.Injury);
                    targetKo = remainingHp[target.Target] == 0;
                    if (battleStates is not null)
                    {
                        int? aimRetentionRoll = strike.AimRetentionRoll;
                        bool targetHasAim = targetAimAvailable;
                        bool directAimCancellation = targetKo || consequences.Value.ApplyStun ||
                            consequences.Value.ApplyKnockedDown || injuryMovement?.CancelAim == true;
                        if (executionBattle is not null && targetHasAim && injury.Value.Injury > 0 && !directAimCancellation)
                        {
                            aimRetentionRoll = executionBattle.ExecutionRandom.Roll3D6(
                                executionBattle.RollIdentity(
                                    input.Declaration.ActionInstanceId,
                                    input.Declaration.Source,
                                    target.Target,
                                    strikeIndex,
                                    DclRollSite.AimRetention,
                                    drawIndex: 0));
                        }
                        if (targetHasAim && directAimCancellation)
                        {
                            if (aimRetentionRoll is not null)
                                throw new ArgumentException("KO, Stun, Knocked Down, or forced movement cancels Aim without a retention roll.", nameof(input));
                            string cancellation = targetKo ? "ko-cancelled"
                                : consequences.Value.ApplyStun ? "stun-cancelled"
                                : consequences.Value.ApplyKnockedDown ? "knocked-down-cancelled"
                                : "forced-movement-cancelled";
                            aimRetention = DclCanonicalAimLifecycle.PlanCancelOwner(
                                battleStates,
                                target.Target,
                                cancellation);
                        }
                        else
                        {
                            DclCanonicalAimLifecycleResult resolvedAim = DclCanonicalAimLifecycle.PlanInjuryRetention(
                                battleStates,
                                target.Target,
                                injury.Value.Injury,
                                forcedMovement: false,
                                targetContext.Will,
                                targetContext.AimRetentionModifier,
                                targetContext.AimRetentionStatePenaltyMagnitude,
                                aimRetentionRoll);
                            aimRetention = resolvedAim.HadAim ? resolvedAim : null;
                        }
                        if (aimRetention is { HadAim: true, Retained: false, InstanceId: { } removedAimId })
                        {
                            targetAimAvailable = false;
                            plannedTargetAimRemovalIds.Add(removedAimId);
                        }
                    }
                    else if (strike.AimRetentionRoll is not null)
                    {
                        throw new ArgumentException("Aim-retention RNG requires the battle state registry.", nameof(input));
                    }
                    foreach (DclCanonicalPhysicalStatusRiderInput rider in strike.StatusRiders ?? [])
                    {
                        int? resistanceRoll = rider.ResistanceRoll;
                        DclStateResistanceGate resistanceGate = riderDefinitions[rider.EffectIndex].ResistanceGate;
                        if (executionBattle is not null && !rider.Immune &&
                            resistanceGate is DclStateResistanceGate.SuccessRoll or DclStateResistanceGate.QuickContest)
                        {
                            resistanceRoll = executionBattle.ExecutionRandom.Roll3D6(
                                executionBattle.RollIdentity(
                                    input.Declaration.ActionInstanceId,
                                    input.Declaration.Source,
                                    target.Target,
                                    strikeIndex,
                                    DclRollSite.Resistance,
                                    drawIndex: rider.EffectIndex));
                        }
                        DclMagicRiderResult gate = DclMagicActionGates.ResolveRider(
                            effectiveAttackSkill,
                            attackRoll.Value,
                            carrierLanded: true,
                            rider.ResistanceScore,
                            resistanceRoll,
                            rider.Immune,
                            resistanceGate);
                        statusRiderResults.Add(new DclCanonicalStatusRiderResult(
                            rider.EffectIndex,
                            gate,
                            StateApplication: null));
                    }
                }
                else if (strike.DamageDice is not null)
                {
                    throw new ArgumentException("A missed or defended Strike cannot consume magnitude RNG.", nameof(input));
                }
                else if (strike.MajorWoundHtRoll is not null || strike.ConcentrationRoll is not null)
                {
                    throw new ArgumentException("A missed or defended Strike cannot consume Injury-consequence RNG.", nameof(input));
                }
                else if (strike.AimRetentionRoll is not null)
                {
                    throw new ArgumentException("A missed or defended Strike cannot consume Aim-retention RNG.", nameof(input));
                }
                else if ((strike.StatusRiders ?? []).Any(rider => rider.ResistanceRoll is not null))
                {
                    throw new ArgumentException("A missed or defended Strike cannot consume status-Rider resistance RNG.", nameof(input));
                }
                results.Add(new DclCanonicalPhysicalStrikeResult(
                    target.Target,
                    strikeIndex,
                    contest,
                    injury,
                    consequences,
                    TargetKoAfterStrike: targetKo,
                    KoShortCircuited: false,
                    AimRetention: aimRetention,
                    StatusRiders: statusRiderResults,
                    ForcedMovement: injuryMovement));
                int[] appliedEffectIndexes = contest.Hit
                    ? new[] { 0 }.Concat(statusRiderResults
                        .Where(rider => rider.Gate.Applied)
                        .Select(rider => rider.EffectIndex))
                        .ToArray()
                    : [];
                resolved.Add(new DclCanonicalResolvedStrike(
                    target.Target,
                    strikeIndex,
                    AppliedEffectIndexes: appliedEffectIndexes,
                    TargetKoAfterStrike: targetKo));
            }
        }
        if (strikeInputs.Count != targets.Length * profile.TransactionProfile.StrikeCount)
            throw new ArgumentException("Physical execution contains a Strike outside the TargetBatch/profile cardinality.", nameof(input));
        IReadOnlyDictionary<DclUnitKey, DclDefenseResourceSnapshot> finalDefenseResources =
            defenseResources.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.CaptureSnapshot());
        DclCanonicalTransactionResult commit = DclCanonicalTransactionExecutor.Commit(
            input.Declaration,
            profile,
            targets,
            resolved,
            new DclCanonicalCommitCallbacks(AfterCommitBeforeReaction: _ =>
            {
                foreach ((DclUnitKey target, DclDefenseResourceSnapshot resources) in finalDefenseResources)
                    _.ApplyFinalDefenseResources(target, resources);
                if (executionBattle is not null)
                {
                    for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
                    {
                        DclCanonicalPhysicalStrikeResult strike = results[resultIndex];
                        if (strike.Injury is not { } injury || strike.Consequences is not { } consequences)
                            continue;
                        DclCanonicalPhysicalTargetContext targetContext = targetContexts[strike.Target];
                        DclCanonicalInjuryStateCommitResult injuryStates =
                            DclCanonicalInjuryConsequenceCommitter.Commit(
                                runtime,
                                injury,
                                consequences,
                                strike.Target,
                                input.Declaration.Source,
                                targetContext.MaxHp,
                                DclCanonicalInjuryStateCommitContext.FromBattle(executionBattle, strike.Target));
                        DclCanonicalChargedCancellation? chargingCancellation =
                            executionBattle.ResolveInjuryConcentration(strike.Target, consequences);
                        results[resultIndex] = strike with
                        {
                            InjuryStates = injuryStates,
                            ChargingCancellation = chargingCancellation,
                        };
                    }
                }
                for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
                {
                    DclCanonicalPhysicalStrikeResult strikeResult = results[resultIndex];
                    DclCanonicalStatusRiderResult[] riderResults = (strikeResult.StatusRiders ?? []).ToArray();
                    if (riderResults.Length == 0) continue;
                    DclCanonicalPhysicalStrikeInput strikeInput = strikeInputs[(strikeResult.Target, strikeResult.StrikeIndex)];
                    Dictionary<int, DclCanonicalPhysicalStatusRiderInput> riderInputs =
                        (strikeInput.StatusRiders ?? []).ToDictionary(rider => rider.EffectIndex);
                    for (int riderIndex = 0; riderIndex < riderResults.Length; riderIndex++)
                    {
                        DclCanonicalStatusRiderResult riderResult = riderResults[riderIndex];
                        if (!riderResult.Gate.Applied) continue;
                        DclCanonicalPhysicalStatusRiderInput riderInput = riderInputs[riderResult.EffectIndex];
                        DclCanonicalStateMaterialization materialization = riderInput.StateMaterialization;
                        DclStateDefinition definition = riderDefinitions[riderResult.EffectIndex];
                        var stateApplication = new DclStateApplication(
                            definition,
                            strikeResult.Target,
                            materialization.BindSource ? input.Declaration.Source : null,
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
                        DclStateApplicationResult application = executionBattle is not null
                            ? executionBattle.ApplyState(stateApplication)
                            : riderInput.StateRegistry.Apply(stateApplication);
                        riderResults[riderIndex] = riderResult with { StateApplication = application };
                    }
                    results[resultIndex] = strikeResult with { StatusRiders = riderResults };
                }
                if (aimStateInstanceIds.Length > 0)
                    battleStates!.RemoveInstances(aimStateInstanceIds);
                if (plannedTargetAimRemovalIds.Count > 0)
                {
                    IReadOnlyList<long> removedAimIds = battleStates!.RemoveInstances(plannedTargetAimRemovalIds);
                    if (removedAimIds.Count != plannedTargetAimRemovalIds.Count)
                        throw new InvalidOperationException("Target Aim state changed before physical commit.");
                }
                input.TurnResources.Pay(profile.TimingProfile);
                foreach (DclWeaponActionState state in strikeWeaponStates.Select(entry => entry.State).Distinct())
                    state.CommitAttack();
            }),
            reactionWindow: input.ReactionWindow,
            stateRegistry: expectedRiderIndexes.Length > 0 ? battleStates : executionBattle?.States);
        return new DclCanonicalPhysicalExecutionResult(results, commit, finalDefenseResources);
    }

    private static DclCanonicalPhysicalStrikeWeaponState[] ResolveStrikeWeaponStates(
        IReadOnlyList<DclCanonicalPhysicalStrikeWeapon> weapons,
        DclWeaponActionState legacyState,
        IReadOnlyList<DclCanonicalPhysicalStrikeWeaponState>? explicitStates)
    {
        DclCanonicalPhysicalStrikeWeaponState[] states = explicitStates is { Count: > 0 }
            ? explicitStates.ToArray()
            : weapons.Select(weapon => new DclCanonicalPhysicalStrikeWeaponState(weapon, legacyState)).ToArray();
        if (states.Length != weapons.Count ||
            states.Select(entry => entry.Weapon.StrikeIndex).Distinct().Count() != states.Length ||
            states.Any(entry => entry.State is null ||
                entry.Weapon != weapons.Single(weapon => weapon.StrikeIndex == entry.Weapon.StrikeIndex) ||
                !StringComparer.Ordinal.Equals(entry.State.ResourceKey, entry.Weapon.WeaponResourceKey)))
            throw new ArgumentException("Physical execution requires one exact battle-owned weapon state per Strike identity.", nameof(explicitStates));
        return states.OrderBy(entry => entry.Weapon.StrikeIndex).ToArray();
    }

    private static void RequireNoSkippedDraws(DclCanonicalPhysicalStrikeInput strike)
    {
        if (strike.AttackRoll is not null || strike.DefenseRoll is not null || strike.DamageDice is not null ||
            strike.MajorWoundHtRoll is not null || strike.ConcentrationRoll is not null || strike.AimRetentionRoll is not null ||
            (strike.StatusRiders ?? []).Any(rider => rider.ResistanceRoll is not null))
            throw new ArgumentException("A KO-short-circuited Strike cannot own attack, defense, magnitude, consequence, Aim-retention, or Rider draws.", nameof(strike));
    }

    private static void RequireNoPreSuppliedExecutionDraws(DclCanonicalPhysicalStrikeInput strike)
    {
        if (strike.AttackRoll is not null || strike.DefenseRoll is not null || strike.DamageDice is not null ||
            strike.MajorWoundHtRoll is not null || strike.ConcentrationRoll is not null || strike.AimRetentionRoll is not null ||
            (strike.StatusRiders ?? []).Any(rider => rider.ResistanceRoll is not null))
            throw new ArgumentException("Battle-owned physical execution cannot mix pre-supplied and ledger-owned random draws.", nameof(strike));
    }

    internal static (int EffectiveSkill, DclRangedDefenseLegality? Defense) ResolveAttackSkillAndRangedDefense(
        DclWeaponMetadata weapon,
        DclUnitKey target,
        DclCanonicalPhysicalStrikeInput strike)
    {
        if (weapon.MaximumRange == 0)
        {
            if (strike.Ranged is not null)
                throw new ArgumentException("A melee Strike cannot own ranged computation inputs.", nameof(strike));
            RequireWeaponSkillTraining(strike, strike.EffectiveAttackSkill);
            return (strike.EffectiveAttackSkill, null);
        }
        DclCanonicalRangedStrikeContext ranged = strike.Ranged ??
            throw new ArgumentException("A projectile Strike requires canonical range/Aim/trajectory inputs.", nameof(strike));
        RequireWeaponSkillTraining(strike, ranged.BaseWeaponSkill);
        if (weapon.RangedKind != ranged.WeaponKind)
            throw new InvalidOperationException("Ranged Strike kind disagrees with selected item metadata.");
        if (!ranged.NativeRangeLegal || !ranged.NativeTrajectoryLegal ||
            ranged.HorizontalTiles > weapon.MaximumRange)
            throw new InvalidOperationException("Native range/trajectory or authored maximum range rejected the projectile Strike.");
        int aimBonus = 0;
        if (ranged.Aim is { Active: true } aim)
        {
            if (!ranged.MovedBeforeShot && aim.Target == target)
                aimBonus = aim.Bonus(weapon.Accuracy);
        }
        int effective = DclRangedRules.EffectiveSkill(
            ranged.BaseWeaponSkill,
            aimBonus,
            ranged.HorizontalTiles,
            ranged.LocationPenaltyMagnitude,
            ranged.ShockStatePenaltyMagnitude);
        if (strike.EffectiveAttackSkill != effective)
            throw new ArgumentException("Supplied ranged EffectiveSkill does not match canonical distance/Aim/location/state computation.", nameof(strike));
        return (effective, DclRangedRules.DefenseLegality(ranged.WeaponKind));
    }

    private static void RequireWeaponSkillTraining(DclCanonicalPhysicalStrikeInput strike, int baseWeaponSkill)
    {
        if (strike.WeaponSkillTraining is not { } training)
            return;
        if (training.FinalScore != baseWeaponSkill)
            throw new ArgumentException(
                "Supplied physical Weapon Skill training result does not match the Strike's canonical base weapon skill.",
                nameof(strike));
    }
}
