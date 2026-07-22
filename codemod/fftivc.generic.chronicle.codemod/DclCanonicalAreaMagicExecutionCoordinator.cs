namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalAreaTargetExecutionRequest(
    DclTargetCandidate Target,
    int TargetSpellScore,
    DclDefenseOption? Dodge,
    int? ResistanceScore,
    int MagnitudeAttribute,
    DclElementAffinity Affinity,
    DclRational FaithMagnitude,
    bool TargetHasShell,
    int TargetMaxHp,
    bool FireEffect,
    bool OilContributed,
    int ApplicableDr = 0,
    DclCanonicalInjuryTargetContext? InjuryTargetContext = null,
    int AdditionalMagnitudeIntegerModifier = 0,
    bool DirectConcentrationCancellation = false,
    int AuthoredForcedDisplacement = 0,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0,
    IReadOnlyList<DclCanonicalStatusRiderExecutionRequest>? StatusRiders = null,
    DclCanonicalResourcePoolSnapshot? ResourceTargetPools = null,
    DclCanonicalNativeMovementVerdict? ForcedMovementVerdict = null,
    bool ForcedMovementImmune = false,
    DclCanonicalConcentrationTargetContext? ForcedMovementConcentrationContext = null,
    IReadOnlyList<DclCanonicalInjuryMovementBranchSet?>? InjuryMovementBranchesByStrike = null,
    IReadOnlyList<DclCanonicalInjuryMovementBranchForest?>? InjuryMovementBranchForestsByStrike = null);

internal sealed record DclCanonicalAreaMagicExecutionRequest(
    int AbilityId,
    DclCastDeclarationRequest DeclarationRequest,
    long ActionInstanceId,
    IReadOnlyDictionary<DclUnitKey, DclTargetCandidate> CurrentUnits,
    IReadOnlyList<DclTargetCandidate> NativeGeometricMembers,
    int BaseSpellScore,
    IReadOnlyList<DclCanonicalAreaTargetExecutionRequest> Targets,
    int CurrentMpAtResolution,
    int CurrentHpAtResolution,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    DclCanonicalResourcePoolSnapshot? ResourceSourcePools = null);

internal sealed record DclCanonicalPublishedAreaMagicExecution(
    DclCanonicalAreaMagicExecutionResult Result,
    DclCanonicalNativeActionApplication Application);

/// <summary>
/// Converts deterministic native/map/state snapshots into one battle-owned Area execution. Every
/// reachable casting, target-gate, magnitude, Injury, and Aim-retention site is sampled lazily by
/// the battle ledger; the resolved multi-carrier ActionInstance is then published exactly once.
/// </summary>
internal static class DclCanonicalAreaMagicExecutionCoordinator
{
    public static DclCanonicalPublishedAreaMagicExecution ResolveAndPublish(
        DclCanonicalBattleRuntime battle,
        DclCanonicalAreaMagicExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        (DclAbilityBinding binding, DclActionProfile profile) = battle.Catalog.ResolveAbility(request.AbilityId);
        if (request.ActionInstanceId <= 0 ||
            request.DeclarationRequest.Caster.BattleGeneration != battle.BattleGeneration)
            throw new ArgumentException("Area execution request does not belong to this battle.", nameof(request));
        if (request.DeclarationRequest.Profile.ActionId != profile.ActionId ||
            request.DeclarationRequest.Profile.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Area execution request lost its normalized action revision.", nameof(request));
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
        if (!carrierCompatible ||
            profile.DeliveryProfile.Delivery != DclDelivery.Area || profile.TargetProfile.Area is null ||
            !statusOnly && !forcedMovementOnly && profile.MagnitudeProfile is not (DclDamageMagnitude or DclHealingMagnitude or DclFixedResourceMagnitude))
            throw new InvalidOperationException("The Area coordinator requires one normalized numeric, pure-status, or pure-ForcedMovement Area carrier.");
        if (profile.MagnitudeProfile is DclDamageMagnitude)
            DclCanonicalInjuryConsequenceCommitter.ValidateUniversal(battle.Catalog);
        if (!battle.TryGetObservedUnit(request.DeclarationRequest.Caster.UnitSlot, out DclUnitKey observedSource) ||
            observedSource != request.DeclarationRequest.Caster)
            throw new ArgumentException("Area execution source must be a current observed UnitKey.", nameof(request));
        if (profile.MagnitudeProfile is DclFixedResourceMagnitude)
        {
            DclCanonicalResourcePoolSnapshot sourcePools = request.ResourceSourcePools ??
                throw new ArgumentException("Area ResourceChange requires exact source pools.", nameof(request));
            if (sourcePools.CurrentHp != request.CurrentHpAtResolution ||
                sourcePools.CurrentMp != request.CurrentMpAtResolution)
                throw new ArgumentException("Area ResourceChange source pools do not match the payer snapshot.", nameof(request));
        }
        else if (request.ResourceSourcePools is not null)
            throw new ArgumentException("Area Damage/Healing cannot own source ResourceChange pools.", nameof(request));
        if (request.Targets.Select(target => target.Target.Unit).Distinct().Count() != request.Targets.Count)
            throw new ArgumentException("Area execution contains duplicate deterministic target snapshots.", nameof(request));
        DclTargetCandidate[] suppliedCandidates = request.CurrentUnits.Values
            .Concat(request.NativeGeometricMembers)
            .Concat(request.Targets.Select(target => target.Target))
            .ToArray();
        foreach (DclTargetCandidate candidate in suppliedCandidates)
        {
            if (!battle.TryGetObservedUnit(candidate.Unit.UnitSlot, out DclUnitKey observed) ||
                observed != candidate.Unit)
                throw new ArgumentException(
                    "Every supplied Area candidate must be a current observed UnitKey.",
                    nameof(request));
            if (battle.States.CaptureTarget(candidate.Unit).Revision != candidate.CombatStateRevision)
                throw new ArgumentException(
                    "Area execution received a stale custom-state candidate before RNG.",
                    nameof(request));
        }
        foreach (DclCanonicalAreaTargetExecutionRequest target in request.Targets)
        {
            if (target.TargetMaxHp < 1 || target.Target.CurrentHp > target.TargetMaxHp ||
                target.AuthoredForcedDisplacement < 0 || target.AimRetentionStatePenaltyMagnitude < 0)
                throw new ArgumentException("Area target contains an invalid pool, displacement, or Aim penalty.", nameof(request));
            if (profile.MagnitudeProfile is DclDamageMagnitude)
            {
                if (target.InjuryTargetContext is not { } injuryContext)
                    throw new ArgumentException("Damaging Area execution requires one deterministic Injury context per target.", nameof(request));
                if (injuryContext.MaxHp != target.TargetMaxHp || injuryContext.TargetSt < 1 ||
                    injuryContext.UnexpiredShockInjury < 0 || injuryContext.ConcentrationStatePenaltyMagnitude < 0)
                    throw new ArgumentException("Area target Injury context is inconsistent with its target snapshot.", nameof(request));
                if (injuryContext.Charging != battle.IsCharging(target.Target.Unit))
                    throw new ArgumentException(
                        "Area Injury Charging snapshot diverges from the canonical timeline before RNG.", nameof(request));
                if (target.InjuryMovementBranchesByStrike is { } injuryMovementBranches &&
                    injuryMovementBranches.Count != profile.TransactionProfile.StrikeCount)
                    throw new ArgumentException(
                        "Area Injury movement branches must match the exact Strike cardinality before RNG.", nameof(request));
                foreach (DclCanonicalInjuryMovementBranchSet branches in
                    target.InjuryMovementBranchesByStrike?.OfType<DclCanonicalInjuryMovementBranchSet>() ?? [])
                    branches.Validate(target.Target.Unit);
                if (target.InjuryMovementBranchForestsByStrike is { } injuryMovementForests &&
                    injuryMovementForests.Count != profile.TransactionProfile.StrikeCount)
                    throw new ArgumentException(
                        "Area Injury movement forests must match the exact Strike cardinality before RNG.",
                        nameof(request));
                if (target.InjuryMovementBranchesByStrike is not null &&
                    target.InjuryMovementBranchForestsByStrike is not null)
                    throw new ArgumentException(
                        "Area execution cannot own both single-origin and conditional-origin Injury branches.",
                        nameof(request));
                foreach (DclCanonicalInjuryMovementBranchForest forest in
                    target.InjuryMovementBranchForestsByStrike?.OfType<DclCanonicalInjuryMovementBranchForest>() ?? [])
                    forest.Validate(target.Target.Unit);
            }
            else if (target.InjuryTargetContext is not null)
                throw new ArgumentException("Healing/ResourceChange Area execution cannot own Injury inputs.", nameof(request));
            if (profile.MagnitudeProfile is DclFixedResourceMagnitude)
            {
                DclCanonicalResourcePoolSnapshot targetPools = target.ResourceTargetPools ??
                    throw new ArgumentException("Area ResourceChange requires exact target pools.", nameof(request));
                if (targetPools.CurrentHp != target.Target.CurrentHp || targetPools.MaxHp != target.TargetMaxHp)
                    throw new ArgumentException("Area ResourceChange target pools do not match the target snapshot.", nameof(request));
            }
            else if (target.ResourceTargetPools is not null)
                throw new ArgumentException("Area Damage/Healing cannot own target ResourceChange pools.", nameof(request));
            if (forcedMovementOnly)
            {
                target.ForcedMovementConcentrationContext?.Validate();
                if ((target.ForcedMovementConcentrationContext?.Charging ?? false) !=
                    battle.IsCharging(target.Target.Unit))
                    throw new ArgumentException(
                        "Area ForcedMovement concentration snapshot diverges from the canonical timeline before RNG.",
                        nameof(request));
                if (target.ForcedMovementImmune &&
                    profile.TargetProfile.Area.DeliveryGate != DclAreaDeliveryGate.QuickContest)
                    throw new ArgumentException(
                        "Area ForcedMovement immunity is legal only under QuickContest delivery.", nameof(request));
                _ = DclCanonicalForcedMovement.Resolve(
                    profile,
                    target.Target.Unit,
                    target.Target.Tile,
                    target.Target.CurrentHp == 0,
                    target.ForcedMovementVerdict);
            }
            else if (target.ForcedMovementVerdict is not null || target.ForcedMovementImmune)
                throw new ArgumentException(
                    "Only Area ForcedMovement can own a native movement verdict or movement immunity.", nameof(request));
            else if (target.ForcedMovementConcentrationContext is not null)
                throw new ArgumentException(
                    "Only Area ForcedMovement can own a concentration snapshot.", nameof(request));
            DclCanonicalStatusRiderExecutionRequest[] riders = (target.StatusRiders ?? []).ToArray();
            int[] expectedRiderIndexes = statusOnly ? [0] : Enumerable.Range(1, profile.Effects.Count - 1).ToArray();
            if (riders.Select(rider => rider.EffectIndex).Distinct().Count() != riders.Length ||
                !riders.Select(rider => rider.EffectIndex).Order().SequenceEqual(expectedRiderIndexes))
                throw new ArgumentException(
                    "Every deterministic Area target must provide every normalized status Rider exactly once.",
                    nameof(request));
            foreach (DclCanonicalStatusRiderExecutionRequest rider in riders)
            {
                DclEffectProfile effect = profile.Effects[rider.EffectIndex];
                bool correctEffect = effect is
                {
                    Kind: DclEffectKind.StatusApplication,
                    ReferencedStateKind: not null,
                } && effect.Role == (statusOnly ? DclEffectRole.Carrier : DclEffectRole.Rider);
                string? stateKind = effect.ReferencedStateKind;
                if (!correctEffect || stateKind is null ||
                    !battle.Catalog.Authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition) ||
                    !ReferenceEquals(rider.StateRegistry, battle.States) ||
                    !StringComparer.Ordinal.Equals(rider.StateMaterialization.Payload.SchemaId, definition.PayloadSchema))
                    throw new ArgumentException(
                        "Area status Rider request does not match the battle registry or normalized state definition.",
                        nameof(request));
            }
            if (statusOnly && profile.TargetProfile.Area.DeliveryGate == DclAreaDeliveryGate.QuickContest &&
                target.ResistanceScore != riders[0].ResistanceScore)
                throw new ArgumentException(
                    "Area status Carrier delivery and materialization require the same resistance snapshot.",
                    nameof(request));
        }

        DclCanonicalActionStateProjection.RequireActionLegal(
            DclCanonicalActionStateProjection.EvaluateTaunt(
                battle,
                request.DeclarationRequest.Caster,
                profile.TimingProfile.ConsumesAction,
                isUniversalNormalAttack: false,
                request.DeclarationRequest.UnitTarget?.Unit,
                normalAttackTargetLegal: false));

        int strikeCount = profile.TransactionProfile.StrikeCount;
        DclCanonicalAreaTargetInput[] targets = request.Targets.Select(target =>
            new DclCanonicalAreaTargetInput(
                target.Target,
                target.TargetSpellScore,
                target.Dodge,
                DodgeRolls: [],
                target.ResistanceScore,
                ResistanceRoll: null,
                target.MagnitudeAttribute,
                MagnitudeDiceByStrike: Enumerable.Repeat<IReadOnlyList<int>?>(null, strikeCount).ToArray(),
                target.Affinity,
                target.FaithMagnitude,
                target.TargetHasShell,
                target.TargetMaxHp,
                target.FireEffect,
                target.OilContributed,
                target.ApplicableDr,
                target.InjuryTargetContext,
                target.InjuryTargetContext is null
                    ? null
                    : Enumerable.Repeat<DclCanonicalInjuryConsequenceRolls?>(null, strikeCount).ToArray(),
                target.AdditionalMagnitudeIntegerModifier,
                target.DirectConcentrationCancellation,
                target.AuthoredForcedDisplacement,
                target.AimRetentionModifier,
                target.AimRetentionStatePenaltyMagnitude,
                (target.StatusRiders ?? []).Select(rider => new DclCanonicalStatusRiderInput(
                    rider.EffectIndex,
                    rider.ResistanceScore,
                    ResistanceRoll: null,
                    rider.Immune,
                    rider.StateRegistry,
                    rider.StateMaterialization)).ToArray(),
                DclCanonicalInjuryStateCommitContext.FromBattle(battle, target.Target.Unit),
                target.ResourceTargetPools,
                target.ForcedMovementVerdict,
                target.ForcedMovementImmune,
                target.ForcedMovementConcentrationContext,
                InjuryMovementBranchesByStrike: target.InjuryMovementBranchesByStrike,
                InjuryMovementBranchForestsByStrike: target.InjuryMovementBranchForestsByStrike)).ToArray();
        var input = new DclCanonicalAreaMagicExecutionInput(
            request.AbilityId,
            request.DeclarationRequest,
            request.ActionInstanceId,
            request.CurrentUnits,
            request.NativeGeometricMembers,
            request.BaseSpellScore,
            SharedCasterRoll: null,
            targets,
            request.CurrentMpAtResolution,
            request.CurrentHpAtResolution,
            battle,
            DclCanonicalReactionWindow.ConfirmedRequest(battle, request.ReactionCandidates),
            request.ResourceSourcePools);
        DclCanonicalAreaMagicExecutionResult result = DclCanonicalAreaMagicExecutor.Resolve(
            battle.Catalog,
            input);
        DclCanonicalNativeActionApplication application =
            DclCanonicalNativeExecutionPublisher.PublishAreaMagic(
                battle,
                request.AbilityId,
                request.DeclarationRequest.Caster,
                result);
        return new DclCanonicalPublishedAreaMagicExecution(result, application);
    }
}
