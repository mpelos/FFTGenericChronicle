namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalPhysicalEvaluationInput(
    int AbilityId,
    int WeaponItemId,
    int SourceSt,
    IReadOnlyList<DclTargetResolutionSnapshot> Targets,
    IReadOnlyList<DclCanonicalPhysicalStrikeInput> Strikes,
    IReadOnlyList<DclCanonicalPhysicalTargetContext>? TargetContexts = null,
    IReadOnlyList<DclCanonicalPhysicalRiderForecast>? StatusRiders = null,
    IReadOnlyList<DclCanonicalPhysicalStrikeWeapon>? StrikeWeapons = null);

internal sealed record DclCanonicalPhysicalRiderForecast(
    DclUnitKey Target,
    int StrikeIndex,
    int EffectIndex,
    int ResistanceScore,
    bool Immune);

internal sealed record DclCanonicalPhysicalTargetEvaluation(
    DclUnitKey Target,
    IReadOnlyDictionary<int, IReadOnlyDictionary<DclPhysicalOutcome, DclRational>> OutcomeProbabilityByStrike,
    DclExactValueForecast TotalRawInjury,
    DclExactValueForecast AppliedHpLoss,
    DclExactValueForecast FinalHp,
    DclExactProbability KoProbability,
    DclRational ExpectedExecutedStrikes,
    DclRational ExpectedHits,
    DclRational ExpectedMovedTiles,
    DclRational FallProbability,
    IReadOnlyDictionary<int, IReadOnlyDictionary<DclDefenseKind, DclRational>> DefenseSelectionProbabilityByStrike,
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, DclRational>> RiderApplicationProbabilityByStrike);

internal sealed record DclCanonicalPhysicalEvaluationResult(
    int AbilityId,
    IReadOnlyList<DclCanonicalPhysicalTargetEvaluation> Targets);

internal sealed record DclCanonicalPhysicalPlayerForecast(
    DclUnitKey Target,
    IReadOnlyDictionary<int, int> HitChancePercentByStrike,
    int MinimumRawInjury,
    int MaximumRawInjury,
    DclRational ExpectedRawInjury,
    DclRational ExpectedAppliedHpLoss,
    int KoChancePercent,
    DclRational ExpectedMovedTiles,
    int FallChancePercent,
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, int>> RiderApplicationChancePercentByStrike);

internal sealed record DclCanonicalPhysicalAiProjection(
    DclUnitKey Target,
    DclRational ExpectedAppliedHpLoss,
    DclRational KoProbability,
    DclRational ExpectedHits,
    DclRational ExpectedExecutedStrikes,
    DclRational ExpectedMovedTiles,
    DclRational FallProbability,
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, DclRational>> RiderApplicationProbabilityByStrike);

internal static class DclCanonicalPhysicalEvaluation
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);
    private static readonly DclRational One = DclRational.FromInteger(1);
    private static readonly DclRational OneRollOutcome = new(1, 216);

    private readonly record struct State(
        int Hp,
        bool BlockAvailable,
        string ParryAttempts,
        int TotalRawInjury,
        int AppliedHpLoss,
        bool ImmediateStun,
        bool ImmediateKnockedDown,
        int TotalMovedTiles,
        bool Fell,
        DclBattleTile MovementTile);

    public static DclCanonicalPhysicalEvaluationResult Evaluate(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalPhysicalEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(input.AbilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.NativeRepeat) ||
            profile.DeliveryProfile.Delivery != DclDelivery.PhysicalAttack ||
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
            throw new InvalidOperationException("Physical evaluation requires one Damage Carrier followed only by normalized status Riders.");
        profile = DclCanonicalPhysicalStrikeCardinality.ResolveEffectiveProfile(
            binding,
            profile,
            input.Targets,
            input.Strikes);
        DclCanonicalInjuryConsequenceCommitter.RequireSupportedTiming(profile);
        if (profile.Effects.Count > 1 && profile.TransactionProfile.StrikeCount > 1 &&
            profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate)
            throw new InvalidOperationException("Immediate multi-Strike physical status Riders require an explicit between-Strike state reprojection owner.");
        DclCanonicalPhysicalStrikeWeapon[] strikeWeapons = DclCanonicalPhysicalStrikeWeapons.Normalize(
            profile.TransactionProfile.StrikeCount,
            input.WeaponItemId,
            legacyWeaponResourceKey: "evaluation-weapon",
            input.StrikeWeapons);
        IReadOnlyDictionary<int, DclWeaponMetadata> weapons =
            DclCanonicalPhysicalStrikeWeapons.ResolveMetadata(runtime, strikeWeapons);
        if (input.SourceSt < 1)
            throw new ArgumentException("Physical evaluation ST input is inconsistent.", nameof(input));
        if (input.Targets.Count == 0)
            throw new ArgumentException("Physical evaluation requires a nonempty TargetBatch.", nameof(input));
        bool immediateApplication = profile.TransactionProfile.WithinActionApplication ==
            DclWithinActionApplication.Immediate;
        Dictionary<DclUnitKey, DclCanonicalPhysicalTargetContext>? targetContexts =
            input.TargetContexts?.ToDictionary(context => context.Target);
        if (input.TargetContexts is not null && targetContexts!.Count != input.TargetContexts.Count)
            throw new ArgumentException("Physical evaluation target contexts cannot duplicate a target.", nameof(input));
        if (immediateApplication && profile.TransactionProfile.StrikeCount > 1 &&
            (targetContexts is null || targetContexts.Count != input.Targets.Count ||
            input.Targets.Any(target => !targetContexts.ContainsKey(target.Target))))
            throw new ArgumentException(
                "Multi-Strike Immediate physical evaluation requires one exact consequence context per target.",
                nameof(input));
        bool requiresInjuryMovementContext = input.Strikes.Any(strike =>
            strike.InjuryMovementBranches is not null || strike.InjuryMovementBranchForest is not null);
        if (requiresInjuryMovementContext &&
            (targetContexts is null || targetContexts.Count != input.Targets.Count ||
            input.Targets.Any(target => !targetContexts.ContainsKey(target.Target))))
            throw new ArgumentException(
                "Physical Injury displacement evaluation requires one exact consequence context per target.",
                nameof(input));
        if (targetContexts is not null)
        {
            foreach (DclTargetResolutionSnapshot target in input.Targets)
            {
                if (!targetContexts.TryGetValue(target.Target, out DclCanonicalPhysicalTargetContext? context) ||
                    context.MaxHp < 1 || target.CurrentHp > context.MaxHp || context.TargetSt < 1 ||
                    context.UnexpiredShockInjury < 0 || context.ConcentrationStatePenaltyMagnitude < 0 ||
                    context.AimRetentionStatePenaltyMagnitude < 0)
                    throw new ArgumentException(
                        "Physical evaluation consequence contexts must match the TargetBatch and contain valid exact inputs.",
                        nameof(input));
            }
        }
        foreach (DclCanonicalPhysicalStrikeInput strike in input.Strikes)
        {
            if (strike.InjuryMovementBranches is not null && strike.InjuryMovementBranchForest is not null)
                throw new ArgumentException(
                    "A physical forecast Strike cannot supply both single-origin and conditional-origin Injury branches.",
                    nameof(input));
            strike.InjuryMovementBranches?.Validate(strike.Target);
            strike.InjuryMovementBranchForest?.Validate(strike.Target);
            if (strike.InjuryMovementBranchForest is not null &&
                targetContexts![strike.Target].MovementOrigin is null)
                throw new ArgumentException(
                    "Conditional physical Injury movement forecast requires the synchronized initial target tile.",
                    nameof(input));
            if (strike.AttackRoll is not null || strike.DefenseRoll is not null || strike.DamageDice is not null ||
                strike.MajorWoundHtRoll is not null || strike.ConcentrationRoll is not null || strike.AimRetentionRoll is not null ||
                (strike.StatusRiders?.Count ?? 0) != 0)
                throw new ArgumentException("Forecast/AI physical evaluation cannot contain execution draws.", nameof(input));
        }

        int[] expectedRiderIndexes = Enumerable.Range(1, profile.Effects.Count - 1).ToArray();
        var riderResistanceGates = new Dictionary<int, DclStateResistanceGate>();
        foreach (int effectIndex in expectedRiderIndexes)
        {
            DclEffectProfile effect = profile.Effects[effectIndex];
            if (effect.ReferencedStateKind is null ||
                !runtime.Authoring.States.TryGetValue(effect.ReferencedStateKind, out DclStateDefinition? definition))
                throw new InvalidOperationException(
                    $"Physical status Rider {effectIndex} lost its normalized state definition.");
            if (definition.ResistanceGate == DclStateResistanceGate.Explicit)
                throw new InvalidOperationException(
                    $"Physical status Rider {effectIndex} uses Explicit resistance and requires its named mechanism owner.");
            riderResistanceGates.Add(effectIndex, definition.ResistanceGate);
        }
        DclCanonicalPhysicalRiderForecast[] riderForecasts = (input.StatusRiders ?? []).ToArray();
        if (riderForecasts.Any(rider => rider.StrikeIndex < 0 ||
                rider.StrikeIndex >= profile.TransactionProfile.StrikeCount ||
                !input.Targets.Any(target => target.Target == rider.Target)) ||
            riderForecasts.Select(rider => (rider.Target, rider.StrikeIndex, rider.EffectIndex)).Distinct().Count() != riderForecasts.Length)
            throw new ArgumentException("Physical Rider forecasts contain an invalid or duplicate target/Strike/effect identity.", nameof(input));
        foreach (DclTargetResolutionSnapshot target in input.Targets)
        {
            for (int strikeIndex = 0; strikeIndex < profile.TransactionProfile.StrikeCount; strikeIndex++)
            {
                int[] actual = riderForecasts
                    .Where(rider => rider.Target == target.Target && rider.StrikeIndex == strikeIndex)
                    .Select(rider => rider.EffectIndex)
                    .Order()
                    .ToArray();
                if (!actual.SequenceEqual(expectedRiderIndexes))
                    throw new ArgumentException(
                        "Every forecast target/Strike must supply every normalized physical status Rider exactly once.",
                        nameof(input));
            }
        }

        IReadOnlyDictionary<int, DclExactIntegerDistribution> damageDistributions = weapons.ToDictionary(
            pair => pair.Key,
            pair => DclExactIntegerDistribution.Roll(DclCanonicalMagnitude.BuildDiceExpression(
                input.SourceSt,
                pair.Value.DamageBasis,
                pair.Value.FixedDamageExpression,
                checked(pair.Value.DamageModifier + damageMagnitude.IntegerModifier),
                checked(pair.Value.WholeDiceModifier + damageMagnitude.WholeDiceModifier))));
        IReadOnlyDictionary<int, DclRational> armorDivisors = weapons.ToDictionary(
            pair => pair.Key,
            pair => profile.DeliveryProfile.ArmorPolicy == DclArmorPolicy.ArmorDividing
                ? profile.DeliveryProfile.ArmorDivisor!.Value
                : pair.Value.ArmorDivisor);
        bool ignoreDr = profile.DeliveryProfile.ArmorPolicy == DclArmorPolicy.IgnoreDr;
        Dictionary<(DclUnitKey Target, int Strike), DclCanonicalPhysicalStrikeInput> strikes = input.Strikes
            .ToDictionary(strike => (strike.Target, strike.StrikeIndex));
        if (strikes.Count != input.Strikes.Count ||
            input.Strikes.Count != input.Targets.Count * profile.TransactionProfile.StrikeCount)
            throw new ArgumentException("Physical evaluation Strike identities do not match TargetBatch cardinality.", nameof(input));

        DclCanonicalPhysicalTargetEvaluation[] targets = input.Targets
            .OrderBy(target => target.Target.UnitSlot)
            .ThenBy(target => target.Target.CharacterId)
            .Select(target => EvaluateTarget(
                profile,
                weapons,
                armorDivisors,
                ignoreDr,
                damageDistributions,
                target,
                strikes,
                targetContexts?.GetValueOrDefault(target.Target),
                riderForecasts.Where(rider => rider.Target == target.Target).ToArray(),
                riderResistanceGates))
            .ToArray();
        return new DclCanonicalPhysicalEvaluationResult(input.AbilityId, targets);
    }

    public static IReadOnlyList<DclCanonicalPhysicalPlayerForecast> ProjectPlayer(
        DclCanonicalPhysicalEvaluationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Targets.Select(target => new DclCanonicalPhysicalPlayerForecast(
            target.Target,
            target.OutcomeProbabilityByStrike.ToDictionary(
                pair => pair.Key,
                pair => Probability(Get(pair.Value, DclPhysicalOutcome.Hit) +
                    Get(pair.Value, DclPhysicalOutcome.CriticalHit)).RoundWholePercent()),
            target.TotalRawInjury.Minimum,
            target.TotalRawInjury.Maximum,
            target.TotalRawInjury.ExpectedValue,
            target.AppliedHpLoss.ExpectedValue,
            target.KoProbability.RoundWholePercent(),
            target.ExpectedMovedTiles,
            Probability(target.FallProbability).RoundWholePercent(),
            target.RiderApplicationProbabilityByStrike.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyDictionary<int, int>)pair.Value.ToDictionary(
                    rider => rider.Key,
                    rider => Probability(rider.Value).RoundWholePercent())))).ToArray();
    }

    public static IReadOnlyList<DclCanonicalPhysicalAiProjection> ProjectAi(
        DclCanonicalPhysicalEvaluationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Targets.Select(target => new DclCanonicalPhysicalAiProjection(
            target.Target,
            target.AppliedHpLoss.ExpectedValue,
            target.KoProbability.Fraction,
            target.ExpectedHits,
            target.ExpectedExecutedStrikes,
            target.ExpectedMovedTiles,
            target.FallProbability,
            target.RiderApplicationProbabilityByStrike)).ToArray();
    }

    private static DclCanonicalPhysicalTargetEvaluation EvaluateTarget(
        DclActionProfile profile,
        IReadOnlyDictionary<int, DclWeaponMetadata> weapons,
        IReadOnlyDictionary<int, DclRational> armorDivisors,
        bool ignoreDr,
        IReadOnlyDictionary<int, DclExactIntegerDistribution> damageDistributions,
        DclTargetResolutionSnapshot target,
        IReadOnlyDictionary<(DclUnitKey Target, int Strike), DclCanonicalPhysicalStrikeInput> strikes,
        DclCanonicalPhysicalTargetContext? targetContext,
        IReadOnlyList<DclCanonicalPhysicalRiderForecast> riderForecasts,
        IReadOnlyDictionary<int, DclStateResistanceGate> riderResistanceGates)
    {
        string[] parryKeys = target.DefenseResources.ParryAttemptCounts.Keys
            .Concat(strikes.Where(pair => pair.Key.Target == target.Target)
                .SelectMany(pair => pair.Value.DefenseCandidates)
                .Where(candidate => candidate.Kind == DclDefenseKind.Parry && !string.IsNullOrWhiteSpace(candidate.ResourceKey))
                .Select(candidate => candidate.ResourceKey!))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        int[] initialAttempts = parryKeys
            .Select(key => target.DefenseResources.ParryAttemptCounts.GetValueOrDefault(key))
            .ToArray();
        DclBattleTile initialMovementTile = targetContext?.MovementOrigin ?? strikes
            .Where(pair => pair.Key.Target == target.Target && pair.Value.InjuryMovementBranches is not null)
            .OrderBy(pair => pair.Key.Strike)
            .Select(pair => pair.Value.InjuryMovementBranches!.Origin)
            .FirstOrDefault();
        var states = new Dictionary<State, DclRational>
        {
            [new State(
                target.CurrentHp,
                target.DefenseResources.BlockAvailable,
                Encode(initialAttempts),
                TotalRawInjury: 0,
                AppliedHpLoss: 0,
                ImmediateStun: false,
                ImmediateKnockedDown: false,
                TotalMovedTiles: 0,
                Fell: false,
                initialMovementTile)] = One,
        };
        var outcomes = new Dictionary<int, Dictionary<DclPhysicalOutcome, DclRational>>();
        var defenseSelections = new Dictionary<int, Dictionary<DclDefenseKind, DclRational>>();
        var riderApplications = new Dictionary<int, Dictionary<int, DclRational>>();
        DclRational expectedExecuted = Zero;
        DclRational expectedHits = Zero;

        for (int strikeIndex = 0; strikeIndex < profile.TransactionProfile.StrikeCount; strikeIndex++)
        {
            DclWeaponMetadata weapon = weapons[strikeIndex];
            DclDamageType damageType = weapon.DamageType;
            DclRational armorDivisor = armorDivisors[strikeIndex];
            DclExactIntegerDistribution damageDistribution = damageDistributions[strikeIndex];
            DclCanonicalPhysicalStrikeInput strike = strikes.TryGetValue((target.Target, strikeIndex), out var found)
                ? found
                : throw new ArgumentException("Physical evaluation is missing one target/Strike input.", nameof(strikes));
            (int effectiveAttackSkill, DclRangedDefenseLegality? rangedLegality) =
                DclCanonicalPhysicalExecutor.ResolveAttackSkillAndRangedDefense(weapon, target.Target, strike);
            var next = new Dictionary<State, DclRational>();
            outcomes[strikeIndex] = [];
            defenseSelections[strikeIndex] = [];
            riderApplications[strikeIndex] = riderForecasts
                .Where(rider => rider.StrikeIndex == strikeIndex)
                .ToDictionary(rider => rider.EffectIndex, _ => Zero);
            foreach ((State state, DclRational stateProbability) in states)
            {
                if (state.Hp == 0)
                {
                    Add(next, state, stateProbability);
                    continue;
                }
                expectedExecuted += stateProbability;
                for (int attackRoll = DclSuccessRoll.MinRoll; attackRoll <= DclSuccessRoll.MaxRoll; attackRoll++)
                {
                    DclRational attackProbability = stateProbability *
                        DclRational.FromInteger(DclSuccessRoll.OutcomeMultiplicity(attackRoll)) * OneRollOutcome;
                    if (DclPhysicalContest.IsCritical(attackRoll, effectiveAttackSkill))
                    {
                        AddOutcome(outcomes[strikeIndex], DclPhysicalOutcome.CriticalHit, attackProbability);
                        expectedHits += attackProbability;
                        AddRiderApplications(
                            riderApplications[strikeIndex],
                            riderForecasts.Where(rider => rider.StrikeIndex == strikeIndex),
                            effectiveAttackSkill,
                            attackRoll,
                            attackProbability,
                            riderResistanceGates);
                        AddHitBranches(next, state, attackProbability, strike, damageType, armorDivisor, ignoreDr,
                            damageDistribution, profile, targetContext, criticalSuccess: true);
                        continue;
                    }
                    if (DclPhysicalContest.IsFumble(attackRoll, effectiveAttackSkill))
                    {
                        AddOutcome(outcomes[strikeIndex], DclPhysicalOutcome.AttackFumble, attackProbability);
                        Add(next, state, attackProbability);
                        continue;
                    }
                    if (!DclSuccessRoll.Succeeds(attackRoll, effectiveAttackSkill))
                    {
                        AddOutcome(outcomes[strikeIndex], DclPhysicalOutcome.AttackMiss, attackProbability);
                        Add(next, state, attackProbability);
                        continue;
                    }

                    (DclDefenseCandidate selected, State spentState) = SelectDefense(
                        state,
                        strike.DefenseCandidates,
                        rangedLegality,
                        parryKeys);
                    AddDefense(defenseSelections[strikeIndex], selected.Kind, attackProbability);
                    if (selected.Kind == DclDefenseKind.None)
                    {
                        AddOutcome(outcomes[strikeIndex], DclPhysicalOutcome.Hit, attackProbability);
                        expectedHits += attackProbability;
                        AddRiderApplications(
                            riderApplications[strikeIndex],
                            riderForecasts.Where(rider => rider.StrikeIndex == strikeIndex),
                            effectiveAttackSkill,
                            attackRoll,
                            attackProbability,
                            riderResistanceGates);
                        AddHitBranches(next, spentState, attackProbability, strike, damageType, armorDivisor, ignoreDr,
                            damageDistribution, profile, targetContext, criticalSuccess: false);
                        continue;
                    }
                    for (int defenseRoll = DclSuccessRoll.MinRoll; defenseRoll <= DclSuccessRoll.MaxRoll; defenseRoll++)
                    {
                        DclRational branch = attackProbability *
                            DclRational.FromInteger(DclSuccessRoll.OutcomeMultiplicity(defenseRoll)) * OneRollOutcome;
                        if (DclSuccessRoll.Succeeds(defenseRoll, selected.Score))
                        {
                            AddOutcome(outcomes[strikeIndex], DclPhysicalOutcome.Defended, branch);
                            Add(next, spentState, branch);
                        }
                        else
                        {
                            AddOutcome(outcomes[strikeIndex], DclPhysicalOutcome.Hit, branch);
                            expectedHits += branch;
                            AddRiderApplications(
                                riderApplications[strikeIndex],
                                riderForecasts.Where(rider => rider.StrikeIndex == strikeIndex),
                                effectiveAttackSkill,
                                attackRoll,
                                branch,
                                riderResistanceGates);
                            AddHitBranches(next, spentState, branch, strike, damageType, armorDivisor, ignoreDr,
                                damageDistribution, profile, targetContext, criticalSuccess: false);
                        }
                    }
                }
            }
            states = next;
        }

        Dictionary<int, DclRational> rawInjury = Collapse(states, state => state.TotalRawInjury);
        Dictionary<int, DclRational> appliedLoss = Collapse(states, state => state.AppliedHpLoss);
        Dictionary<int, DclRational> finalHp = Collapse(states, state => state.Hp);
        DclRational expectedMovedTiles = states.Aggregate(
            Zero,
            (sum, pair) => sum + DclRational.FromInteger(pair.Key.TotalMovedTiles) * pair.Value);
        DclRational fallProbability = states.Where(pair => pair.Key.Fell)
            .Aggregate(Zero, (sum, pair) => sum + pair.Value);
        DclRational ko = Get(finalHp, 0);
        return new DclCanonicalPhysicalTargetEvaluation(
            target.Target,
            outcomes.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyDictionary<DclPhysicalOutcome, DclRational>)pair.Value),
            Forecast(rawInjury),
            Forecast(appliedLoss),
            Forecast(finalHp),
            Probability(ko),
            expectedExecuted,
            expectedHits,
            expectedMovedTiles,
            fallProbability,
            defenseSelections.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyDictionary<DclDefenseKind, DclRational>)pair.Value),
            riderApplications.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyDictionary<int, DclRational>)pair.Value));
    }

    private static void AddRiderApplications(
        Dictionary<int, DclRational> destination,
        IEnumerable<DclCanonicalPhysicalRiderForecast> riders,
        int carrierScore,
        int carrierRoll,
        DclRational carrierLandedProbability,
        IReadOnlyDictionary<int, DclStateResistanceGate> resistanceGates)
    {
        foreach (DclCanonicalPhysicalRiderForecast rider in riders)
        {
            if (rider.Immune) continue;
            DclRational application = resistanceGates[rider.EffectIndex] switch
            {
                DclStateResistanceGate.None => One,
                DclStateResistanceGate.SuccessRoll => new DclRational(
                    216 - DclSuccessRoll.SuccessOutcomeCount(rider.ResistanceScore),
                    216),
                DclStateResistanceGate.QuickContest => QuickContestApplicationProbability(
                    carrierScore,
                    carrierRoll,
                    rider.ResistanceScore),
                DclStateResistanceGate.Explicit => throw new InvalidOperationException(
                    "Explicit physical Rider resistance requires its named evaluator."),
                _ => throw new InvalidOperationException("Physical Rider resistance gate is not normalized."),
            };
            destination[rider.EffectIndex] += carrierLandedProbability * application;
        }
    }

    private static DclRational QuickContestApplicationProbability(
        int carrierScore,
        int carrierRoll,
        int resistanceScore)
    {
        int appliedOutcomes = 0;
        for (int resistanceRoll = DclSuccessRoll.MinRoll; resistanceRoll <= DclSuccessRoll.MaxRoll; resistanceRoll++)
        {
            if (DclQuickContest.Resolve(
                    carrierScore,
                    carrierRoll,
                    resistanceScore,
                    resistanceRoll).ActingSideWon)
                appliedOutcomes += DclSuccessRoll.OutcomeMultiplicity(resistanceRoll);
        }
        return new DclRational(appliedOutcomes, 216);
    }

    private static (DclDefenseCandidate Selected, State SpentState) SelectDefense(
        State state,
        IReadOnlyList<DclDefenseCandidate> candidates,
        DclRangedDefenseLegality? ranged,
        IReadOnlyList<string> parryKeys)
    {
        int[] attempts = Decode(state.ParryAttempts, parryKeys.Count);
        DclDefenseCandidate selected = DclActiveDefenseRules.SelectOne(candidates.Select(candidate =>
        {
            bool rangedLegal = ranged is null || candidate.Kind switch
            {
                DclDefenseKind.Dodge => ranged.Value.Dodge,
                DclDefenseKind.Block => ranged.Value.Block,
                DclDefenseKind.Parry => ranged.Value.Parry,
                _ => true,
            };
            int immediatePenalty = (state.ImmediateStun ? 4 : 0) +
                (state.ImmediateKnockedDown ? candidate.Kind switch
                {
                    DclDefenseKind.Dodge => 3,
                    DclDefenseKind.Parry or DclDefenseKind.Block => 2,
                    _ => 0,
                } : 0);
            candidate = candidate with { Score = checked(candidate.Score - immediatePenalty) };
            if (candidate.Kind == DclDefenseKind.Parry)
            {
                int index = string.IsNullOrWhiteSpace(candidate.ResourceKey)
                    ? -1
                    : IndexOf(parryKeys, candidate.ResourceKey!);
                return candidate with
                {
                    Score = index < 0 ? candidate.Score : checked(candidate.Score - attempts[index] * 4),
                    Legal = candidate.Legal && rangedLegal && index >= 0,
                };
            }
            if (candidate.Kind == DclDefenseKind.Block)
                return candidate with { Legal = candidate.Legal && rangedLegal && state.BlockAvailable };
            return candidate with { Legal = candidate.Legal && rangedLegal };
        }));
        State spent = state;
        if (selected.Kind == DclDefenseKind.Parry)
        {
            int index = IndexOf(parryKeys, selected.ResourceKey!);
            attempts[index] = checked(attempts[index] + 1);
            spent = state with { ParryAttempts = Encode(attempts) };
        }
        else if (selected.Kind == DclDefenseKind.Block)
        {
            spent = state with { BlockAvailable = false };
        }
        return (selected, spent);
    }

    private static void AddHitBranches(
        Dictionary<State, DclRational> destination,
        State state,
        DclRational hitProbability,
        DclCanonicalPhysicalStrikeInput strike,
        DclDamageType damageType,
        DclRational armorDivisor,
        bool ignoreDr,
        DclExactIntegerDistribution damageDistribution,
        DclActionProfile profile,
        DclCanonicalPhysicalTargetContext? targetContext,
        bool criticalSuccess)
    {
        foreach ((int rawDamage, System.Numerics.BigInteger weight) in damageDistribution.Weights)
        {
            DclRational probability = hitProbability * new DclRational(weight, damageDistribution.TotalOutcomes);
            DclInjuryResult injury = DclInjury.Resolve(
                rawDamage,
                damageType,
                strike.ApplicableDr,
                armorDivisor,
                ignoreDr);
            int applied = Math.Min(state.Hp, injury.Injury);
            State injured = state with
            {
                Hp = state.Hp - applied,
                TotalRawInjury = checked(state.TotalRawInjury + injury.Injury),
                AppliedHpLoss = checked(state.AppliedHpLoss + applied),
            };
            int criticalKnockback = targetContext is { } movementContext
                ? DclInjury.CriticalKnockbackTiles(
                    criticalSuccess,
                    injured.Hp > 0,
                    damageType,
                    injury.RolledDamage,
                    injury.PenetratingDamage,
                    movementContext.TargetSt)
                : 0;
            int requestedMovement = checked(strike.AuthoredForcedDisplacement + criticalKnockback);
            if (requestedMovement > 0 && injured.Hp > 0)
            {
                DclCanonicalForcedMovementResult movement = strike.InjuryMovementBranchForest?.Resolve(
                    strike.Target,
                    injured.MovementTile,
                    targetKo: false,
                    requestedMovement) ?? strike.InjuryMovementBranches?.Resolve(
                        strike.Target,
                        targetKo: false,
                        requestedMovement) ?? throw new InvalidOperationException(
                        "Physical forecast selected Injury displacement without its frozen native map branch.");
                if (movement.Origin != injured.MovementTile)
                    throw new InvalidOperationException(
                        "Physical forecast Injury movement diverges from its conditional origin timeline.");
                injured = injured with
                {
                    TotalMovedTiles = checked(injured.TotalMovedTiles + movement.MovedTiles),
                    Fell = injured.Fell || movement.Fell,
                    MovementTile = movement.MovedTiles > 0 ? movement.Destination : injured.MovementTile,
                };
            }
            bool immediate = profile.TransactionProfile.WithinActionApplication ==
                DclWithinActionApplication.Immediate;
            bool majorWoundCheck = immediate && targetContext is { } context && injured.Hp > 0 &&
                injury.Injury > context.MaxHp / 2;
            if (!majorWoundCheck)
            {
                Add(destination, injured, probability);
                continue;
            }
            for (int htRoll = DclSuccessRoll.MinRoll; htRoll <= DclSuccessRoll.MaxRoll; htRoll++)
            {
                DclRational branch = probability *
                    DclRational.FromInteger(DclSuccessRoll.OutcomeMultiplicity(htRoll)) * OneRollOutcome;
                bool collapsed = !DclSuccessRoll.Succeeds(htRoll, targetContext!.EffectiveHt);
                Add(destination, collapsed
                    ? injured with { ImmediateStun = true, ImmediateKnockedDown = true }
                    : injured, branch);
            }
        }
    }

    private static DclExactValueForecast Forecast(IReadOnlyDictionary<int, DclRational> probabilities)
    {
        DclRational total = probabilities.Values.Aggregate(Zero, (sum, probability) => sum + probability);
        if (total != One) throw new InvalidOperationException($"Physical evaluation probability mass is {total}, expected one.");
        return new DclExactValueForecast(
            probabilities.Keys.Min(),
            probabilities.Keys.Max(),
            probabilities.Aggregate(Zero, (sum, pair) => sum + DclRational.FromInteger(pair.Key) * pair.Value),
            probabilities.OrderBy(pair => pair.Key).ToDictionary());
    }

    private static Dictionary<int, DclRational> Collapse(
        IReadOnlyDictionary<State, DclRational> states,
        Func<State, int> selector)
    {
        var result = new Dictionary<int, DclRational>();
        foreach ((State state, DclRational probability) in states)
        {
            int value = selector(state);
            result[value] = Get(result, value) + probability;
        }
        return result;
    }

    private static DclExactProbability Probability(DclRational probability)
        => new(probability.Numerator, probability.Denominator);

    private static void Add(Dictionary<State, DclRational> states, State state, DclRational probability)
        => states[state] = Get(states, state) + probability;

    private static void AddOutcome(
        Dictionary<DclPhysicalOutcome, DclRational> outcomes,
        DclPhysicalOutcome outcome,
        DclRational probability)
        => outcomes[outcome] = Get(outcomes, outcome) + probability;

    private static void AddDefense(
        Dictionary<DclDefenseKind, DclRational> defenses,
        DclDefenseKind defense,
        DclRational probability)
        => defenses[defense] = Get(defenses, defense) + probability;

    private static DclRational Get<TKey>(IReadOnlyDictionary<TKey, DclRational> values, TKey key)
        where TKey : notnull
        => values.TryGetValue(key, out DclRational value) ? value : Zero;

    private static string Encode(IReadOnlyList<int> attempts) => string.Join(",", attempts);

    private static int[] Decode(string encoded, int count)
    {
        if (count == 0) return [];
        string[] parts = encoded.Split(',');
        if (parts.Length != count) throw new InvalidOperationException("Physical evaluation parry-state vector is corrupt.");
        return parts.Select(int.Parse).ToArray();
    }

    private static int IndexOf(IReadOnlyList<string> values, string value)
    {
        for (int index = 0; index < values.Count; index++)
            if (StringComparer.Ordinal.Equals(values[index], value)) return index;
        return -1;
    }
}
