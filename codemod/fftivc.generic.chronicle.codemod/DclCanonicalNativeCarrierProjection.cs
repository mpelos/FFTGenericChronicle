namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclCanonicalNativeNumericChannels
{
    public int HpDebit { get; }
    public int HpCredit { get; }
    public int MpDebit { get; }
    public int MpCredit { get; }

    public bool IsEmpty => HpDebit == 0 && HpCredit == 0 && MpDebit == 0 && MpCredit == 0;

    public DclCanonicalNativeNumericChannels(int hpDebit, int hpCredit, int mpDebit, int mpCredit)
    {
        if (hpDebit < 0 || hpCredit < 0 || mpDebit < 0 || mpCredit < 0)
            throw new ArgumentOutOfRangeException(nameof(hpDebit), "Native numeric channels cannot be negative.");
        if (hpDebit > 0 && hpCredit > 0)
            throw new ArgumentException("One target-result record cannot debit and credit HP simultaneously.");
        if (mpDebit > 0 && mpCredit > 0)
            throw new ArgumentException("One target-result record cannot debit and credit MP simultaneously.");
        HpDebit = hpDebit;
        HpCredit = hpCredit;
        MpDebit = mpDebit;
        MpCredit = mpCredit;
    }
}

internal sealed record DclCanonicalNativeTargetProjection(
    DclUnitKey Target,
    DclMagicDeliveryOutcome DeliveryOutcome,
    bool Delivered,
    DclCanonicalNativeNumericChannels Channels,
    DclMagicalEffectRoute? MagicalRoute,
    DclInjuryConsequenceResult? InjuryConsequences,
    bool TargetKo,
    IReadOnlyList<int> AppliedRiderEffectIndexes,
    DclCanonicalNativeAuxiliaryEffects? AuxiliaryEffects = null,
    DclPhysicalOutcome? PhysicalOutcome = null);

internal sealed record DclCanonicalNativePaymentProjection(
    DclUnitKey Payer,
    DclCanonicalNativeNumericChannels Channels,
    bool PayerKo,
    bool IsResourcePayment,
    bool OpensDamageReaction);

internal sealed record DclCanonicalNativeSourceEffectProjection(
    DclUnitKey Source,
    DclCanonicalNativeNumericChannels Channels,
    bool SourceKo,
    bool IsResourceChange,
    bool OpensDamageReaction);

internal sealed record DclCanonicalNativeStateMutationProjection(
    long InstanceId,
    string StateKind);

internal sealed record DclCanonicalNativeAuxiliaryEffects(
    IReadOnlyList<DclCanonicalNativeStateMutationProjection> AddedStates,
    IReadOnlyList<DclCanonicalNativeStateMutationProjection> RemovedStates,
    DclRational? CtCredit,
    bool ClearKoAfterPositiveHpCredit,
    DclCanonicalForcedMovementResult? ForcedMovement = null)
{
    public bool IsEmpty => AddedStates.Count == 0 && RemovedStates.Count == 0 &&
        CtCredit is null && !ClearKoAfterPositiveHpCredit && ForcedMovement is null;
}

internal sealed record DclCanonicalNativeStrikeProjection(
    DclUnitKey Target,
    int StrikeIndex,
    DclMagicDeliveryOutcome? MagicDeliveryOutcome,
    DclPhysicalOutcome? PhysicalOutcome,
    bool Delivered,
    DclCanonicalNativeNumericChannels Channels,
    DclMagicalEffectRoute? MagicalRoute,
    DclInjuryConsequenceResult? InjuryConsequences,
    bool TargetKoAfterStrike,
    bool KoShortCircuited,
    DclCanonicalNativeAuxiliaryEffects? AuxiliaryEffects = null);

internal sealed record DclCanonicalNativeMultiCarrierProjection(
    long ActionInstanceId,
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    DclNativeCarrierKind CarrierKind,
    DclCarrierRewritePolicy RewritePolicy,
    IReadOnlyList<DclCanonicalNativeStrikeProjection> Strikes,
    DclCanonicalNativePaymentProjection? ResourcePayment,
    bool ReactionWindowOpened,
    bool ResourceFailed,
    DclCanonicalNativeSourceEffectProjection? SourceEffect = null,
    DclCanonicalReactionWindowResult? Reactions = null);

internal sealed record DclCanonicalNativeCarrierProjection(
    long ActionInstanceId,
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    DclNativeCarrierKind CarrierKind,
    DclCarrierRewritePolicy RewritePolicy,
    string ForecastBoundary,
    string AiBoundary,
    string ExecutionBoundary,
    string ApplyBoundary,
    string PresentationBoundary,
    DclCanonicalNativeTargetProjection? TargetResult,
    DclCanonicalNativePaymentProjection? ResourcePayment,
    bool ReactionWindowOpened,
    bool ResourceFailed,
    DclCanonicalNativeSourceEffectProjection? SourceEffect = null,
    DclCanonicalReactionWindowResult? Reactions = null);

/// <summary>
/// Converts a fully resolved canonical action into semantic native-carrier records. This layer does
/// not write game memory. Target effect and caster payment deliberately remain separate even when
/// Reflect makes them refer to the same unit: an HP payment is not Injury and cannot manufacture a
/// damage-triggered Reaction or damage presentation.
/// </summary>
internal static class DclCanonicalNativeCarrierProjector
{
    public static DclCanonicalNativeMultiCarrierProjection ProjectPhysical(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclCanonicalPhysicalExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(result);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(abilityId);
        if (binding.CarrierKind is not (DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.NativeRepeat) ||
            binding.RewritePolicy is not (DclCarrierRewritePolicy.ReplaceNumericResult or DclCarrierRewritePolicy.ReplaceCompleteResult))
            throw new InvalidOperationException("Physical execution requires a replaceable result/repeat carrier.");
        if (profile.DeliveryProfile.Delivery != DclDelivery.PhysicalAttack ||
            profile.Effects.Count == 0 || profile.Effects[0] is not
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.Damage,
            } || profile.Effects.Skip(1).Any(effect => effect is not
            {
                Role: DclEffectRole.Rider,
                Kind: DclEffectKind.StatusApplication,
            }))
            throw new InvalidOperationException("Physical projection requires one Damage Carrier followed only by normalized status Riders.");
        DclActionTransaction transaction = result.Commit.Transaction;
        if (transaction.Declaration.ActionId != profile.ActionId ||
            transaction.Declaration.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("The physical result does not match the native ability binding.", nameof(result));
        if (transaction.Stage != DclActionTransactionStage.Settled)
            throw new ArgumentException("A physical carrier requires one settled outer transaction.", nameof(result));
        if ((result.ProtectionRedirectPlan is null) != (result.ProtectionRedirect is null))
            throw new ArgumentException("Physical protection routing must publish both its plan and committed resolution.", nameof(result));
        DclCanonicalNativeAuxiliaryEffects? protectionAuxiliary = ProjectProtectionRedirect(result);

        DclCanonicalNativeStrikeProjection[] strikes = result.Strikes.Select((strike, projectionIndex) =>
        {
            bool delivered = strike.Contest?.Hit == true;
            int hpDebit = strike.Injury?.Injury ?? 0;
            if (!delivered && strike.Injury is not null)
                throw new ArgumentException("A missed or defended physical Strike cannot project Injury.", nameof(result));
            if (delivered && strike.Injury is null)
                throw new ArgumentException("A landed physical Strike must project its resolved Injury, including zero.", nameof(result));
            if (delivered != (strike.Consequences is not null))
                throw new ArgumentException("Physical Injury and its universal per-Strike consequences must cross native projection together.", nameof(result));
            if (strike.KoShortCircuited && (strike.Contest is not null || strike.Injury is not null || strike.Consequences is not null))
                throw new ArgumentException("A KO-short-circuited physical Strike must remain an empty visual continuation.", nameof(result));
            if (strike.TargetKoAfterStrike && hpDebit == 0 && !strike.KoShortCircuited)
                throw new ArgumentException("Only physical Injury can create a new target KO transition.", nameof(result));
            DclCanonicalStatusRiderResult[] riderResults = (strike.StatusRiders ?? []).ToArray();
            if (riderResults.Any(rider => rider.Gate.Applied != (rider.StateApplication is not null)))
                throw new ArgumentException("Applied physical Rider gates and durable state applications must agree before native projection.", nameof(result));
            if (!delivered && riderResults.Length != 0)
                throw new ArgumentException("A missed, defended, or skipped physical Strike cannot project status Riders.", nameof(result));
            return new DclCanonicalNativeStrikeProjection(
                strike.Target,
                strike.StrikeIndex,
                MagicDeliveryOutcome: null,
                strike.Contest?.Outcome,
                delivered,
                new DclCanonicalNativeNumericChannels(hpDebit, 0, 0, 0),
                MagicalRoute: null,
                strike.Consequences,
                strike.TargetKoAfterStrike,
                strike.KoShortCircuited,
                DclCanonicalNativeAuxiliaryProjector.Merge(
                    projectionIndex == 0 ? protectionAuxiliary : null,
                    DclCanonicalNativeAuxiliaryProjector.ProjectInjuryStates(strike.InjuryStates),
                    DclCanonicalNativeAuxiliaryProjector.ProjectAimLifecycle(strike.AimRetention),
                    DclCanonicalNativeAuxiliaryProjector.ProjectForcedMovement(strike.ForcedMovement),
                    DclCanonicalNativeAuxiliaryProjector.ProjectStateApplications(
                        riderResults.Select(rider => rider.StateApplication))));
        }).ToArray();
        if (strikes.Length != result.Commit.ExecutedStrikes.Count + result.Commit.KoShortCircuitedStrikes.Count)
            throw new ArgumentException("Physical projection lost an executed or KO-short-circuited Strike.", nameof(result));

        return new DclCanonicalNativeMultiCarrierProjection(
            transaction.Declaration.ActionInstanceId,
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.CarrierKind,
            binding.RewritePolicy,
            strikes,
            ResourcePayment: null,
            transaction.ReactionWindowOpened,
            ResourceFailed: false,
            Reactions: result.Commit.Reactions);
    }

    private static DclCanonicalNativeAuxiliaryEffects? ProjectProtectionRedirect(
        DclCanonicalPhysicalExecutionResult result)
    {
        if (result.ProtectionRedirectPlan is not { } plan || result.ProtectionRedirect is not { } redirect)
            return null;
        if (plan.Candidate != redirect.Candidate ||
            plan.ExpectedState.InstanceId != redirect.Candidate.StateInstanceId ||
            plan.Resolution.FinalTarget != redirect.FinalTarget)
            throw new ArgumentException("Physical protection routing changed between plan and committed projection.", nameof(result));
        DclCanonicalNativeStateMutationProjection[] removed = redirect.Trigger.RemovedStateInstanceIds
            .Select(instanceId => new DclCanonicalNativeStateMutationProjection(instanceId, plan.ExpectedState.Kind))
            .ToArray();
        if (removed.Length == 0)
            return null;
        return new DclCanonicalNativeAuxiliaryEffects(
            AddedStates: [],
            RemovedStates: removed,
            CtCredit: null,
            ClearKoAfterPositiveHpCredit: false);
    }

    public static DclCanonicalNativeMultiCarrierProjection ProjectAreaMagic(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclUnitKey payer,
        DclCanonicalAreaMagicExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(result);
        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(abilityId);
        bool statusOnly = profile.MagnitudeProfile is null && profile.Effects.Count == 1 && profile.Effects[0] is
        {
            Role: DclEffectRole.Carrier,
            Kind: DclEffectKind.StatusApplication,
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
        bool rewriteCompatible = statusOnly
            ? binding.RewritePolicy is DclCarrierRewritePolicy.ReplaceCompleteResult or
                DclCarrierRewritePolicy.ReplaceStatusPacket or DclCarrierRewritePolicy.ManagedProducer
            : forcedMovementOnly
                ? binding.RewritePolicy is DclCarrierRewritePolicy.ReplaceCompleteResult or
                    DclCarrierRewritePolicy.ReplaceStatusPacket
                : binding.RewritePolicy is DclCarrierRewritePolicy.ReplaceNumericResult or
                    DclCarrierRewritePolicy.ReplaceCompleteResult;
        if (!carrierCompatible || !rewriteCompatible)
            throw new InvalidOperationException("Area magic requires a replaceable result/repeat carrier.");
        if (profile.ActionId != result.Declaration.ActionId ||
            profile.ProfileRevision != result.Declaration.ProfileRevision)
            throw new ArgumentException("The Area result does not match the native ability binding.", nameof(result));
        if (!payer.IsValid || payer != result.Declaration.Source)
            throw new ArgumentException("The Area payment owner must equal the declared source.", nameof(payer));
        if (!result.Payment.Legal)
            throw new ArgumentException("An illegal Area resource settlement cannot reach native projection.", nameof(result));

        bool resourceFailed = result.Outcome == DclCastingOutcome.ResourceFailure;
        if (resourceFailed)
        {
            if (result.Transaction.Stage != DclActionTransactionStage.ResourceFailed ||
                result.Strikes.Count != 0 || result.Payment is not { CostDue: 0, MpPaid: 0, HpPaid: 0 })
                throw new ArgumentException("Area ResourceFailure must reach native projection empty.", nameof(result));
            return new DclCanonicalNativeMultiCarrierProjection(
                result.Declaration.ActionInstanceId,
                binding.AbilityId,
                binding.ActionId,
                binding.ProfileRevision,
                binding.CarrierKind,
                binding.RewritePolicy,
                [],
                ResourcePayment: null,
                ReactionWindowOpened: false,
                ResourceFailed: true);
        }

        IEnumerable<DclCanonicalAreaMagicStrikeResult> nativeOrderedResults = binding.CarrierKind == DclNativeCarrierKind.NativeRepeat
            ? result.Strikes.OrderBy(strike => strike.StrikeIndex)
            : result.Strikes;
        int sourceHpDebit = 0;
        int sourceHpCredit = 0;
        int sourceMpDebit = 0;
        int sourceMpCredit = 0;
        bool sourceKo = false;
        DclCanonicalNativeStrikeProjection[] strikes = nativeOrderedResults.Select(strike =>
        {
            int hpDebit = strike.Effect is { Route: DclMagicalEffectRoute.Injury } injury
                ? injury.FinalInjury
                : strike.ResourceChange?.TargetChannels.HpDebit ?? 0;
            int hpCredit = strike.Effect is { Route: DclMagicalEffectRoute.AbsorbedHealing } absorbed
                ? absorbed.AppliedHealing
                : strike.Healing?.AppliedHealing ?? strike.ResourceChange?.TargetChannels.HpCredit ?? 0;
            int mpDebit = strike.ResourceChange?.TargetChannels.MpDebit ?? 0;
            int mpCredit = strike.ResourceChange?.TargetChannels.MpCredit ?? 0;
            bool delivered = strike.DeliveryOutcome is
                DclMagicDeliveryOutcome.Delivered or DclMagicDeliveryOutcome.CriticalDelivered &&
                strike.ResourceChange?.RejectedByUndeadPolicy != true;
            if (!delivered && strike.ResourceChange?.RejectedByUndeadPolicy != true &&
                (strike.Effect is not null || strike.Healing is not null || strike.ResourceChange is not null))
                throw new ArgumentException("A failed/defended Area Strike cannot project numeric effects.", nameof(result));
            if ((strike.Effect?.Route == DclMagicalEffectRoute.Injury) != (strike.InjuryConsequences is not null))
                throw new ArgumentException("Area Injury and its universal per-Strike consequences must cross native projection together.", nameof(result));
            if (strike.KoShortCircuited &&
                (strike.DeliveryOutcome is not null || strike.Effect is not null || strike.Healing is not null ||
                 strike.ResourceChange is not null || strike.InjuryConsequences is not null))
                throw new ArgumentException("A KO-short-circuited Area Strike must remain an empty visual continuation.", nameof(result));
            if (strike.TargetKoAfterStrike && hpDebit == 0 && !strike.KoShortCircuited)
                throw new ArgumentException("Only Area Injury or HP ResourceChange can create a new target KO transition.", nameof(result));
            if (strike.ResourceChange is { } resourceChange)
            {
                sourceHpDebit = checked(sourceHpDebit + resourceChange.SourceChannels.HpDebit);
                sourceHpCredit = checked(sourceHpCredit + resourceChange.SourceChannels.HpCredit);
                sourceMpDebit = checked(sourceMpDebit + resourceChange.SourceChannels.MpDebit);
                sourceMpCredit = checked(sourceMpCredit + resourceChange.SourceChannels.MpCredit);
                sourceKo |= resourceChange.SourceKo;
            }
            DclCanonicalNativeAuxiliaryEffects? riderAuxiliary =
                DclCanonicalNativeAuxiliaryProjector.Merge(
                DclCanonicalNativeAuxiliaryProjector.ProjectInjuryStates(strike.InjuryStates),
                  DclCanonicalNativeAuxiliaryProjector.ProjectAimLifecycle(strike.AimRetention),
                  DclCanonicalNativeAuxiliaryProjector.ProjectForcedMovement(strike.ForcedMovement),
                  DclCanonicalNativeAuxiliaryProjector.ProjectStateApplications(
                        (strike.StatusRiders ?? []).Select(rider => rider.StateApplication)));
            return new DclCanonicalNativeStrikeProjection(
                strike.Target,
                strike.StrikeIndex,
                strike.DeliveryOutcome,
                PhysicalOutcome: null,
                delivered,
                new DclCanonicalNativeNumericChannels(hpDebit, hpCredit, mpDebit, mpCredit),
                strike.Effect?.Route,
                strike.InjuryConsequences,
                strike.TargetKoAfterStrike,
                strike.KoShortCircuited,
                riderAuxiliary);
        }).ToArray();
        if (result.Transaction is not { Stage: DclActionTransactionStage.Settled } transaction)
            throw new ArgumentException("A resolved Area carrier requires one settled outer transaction.", nameof(result));
        DclCanonicalNativeSourceEffectProjection? sourceEffect =
            sourceHpDebit != 0 || sourceHpCredit != 0 || sourceMpDebit != 0 || sourceMpCredit != 0 || sourceKo
                ? new DclCanonicalNativeSourceEffectProjection(
                    payer,
                    new DclCanonicalNativeNumericChannels(
                        sourceHpDebit,
                        sourceHpCredit,
                        sourceMpDebit,
                        sourceMpCredit),
                    sourceKo,
                    IsResourceChange: true,
                    OpensDamageReaction: false)
                : null;
        return new DclCanonicalNativeMultiCarrierProjection(
            result.Declaration.ActionInstanceId,
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.CarrierKind,
            binding.RewritePolicy,
            strikes,
            BuildPayment(payer, result.Payment),
            transaction.ReactionWindowOpened,
            ResourceFailed: false,
            SourceEffect: sourceEffect,
            Reactions: result.Reactions);
    }

    public static DclComputePointNumericPlan BuildTargetNumericPlan(
        DclCanonicalNativeCarrierProjection projection,
        int naturalHpDebit,
        int naturalHpCredit,
        int naturalMpDebit,
        int naturalMpCredit,
        byte naturalResultFlags,
        bool controlResultFlags,
        int preserveResultFlagsMask)
    {
        ArgumentNullException.ThrowIfNull(projection);
        DclCanonicalNativeTargetProjection target = projection.TargetResult ??
            throw new InvalidOperationException("A ResourceFailure projection has no native target-result staging plan.");
        return BuildNumericPlan(
            target.Delivered,
            target.Channels,
            naturalHpDebit,
            naturalHpCredit,
            naturalMpDebit,
            naturalMpCredit,
            naturalResultFlags,
            controlResultFlags,
            preserveResultFlagsMask);
    }

    public static DclComputePointNumericPlan BuildStrikeNumericPlan(
        DclCanonicalNativeStrikeProjection strike,
        int naturalHpDebit,
        int naturalHpCredit,
        int naturalMpDebit,
        int naturalMpCredit,
        byte naturalResultFlags,
        bool controlResultFlags,
        int preserveResultFlagsMask)
    {
        ArgumentNullException.ThrowIfNull(strike);
        return BuildNumericPlan(
            strike.Delivered,
            strike.Channels,
            naturalHpDebit,
            naturalHpCredit,
            naturalMpDebit,
            naturalMpCredit,
            naturalResultFlags,
            controlResultFlags,
            preserveResultFlagsMask);
    }

    private static DclComputePointNumericPlan BuildNumericPlan(
        bool delivered,
        DclCanonicalNativeNumericChannels channels,
        int naturalHpDebit,
        int naturalHpCredit,
        int naturalMpDebit,
        int naturalMpCredit,
        byte naturalResultFlags,
        bool controlResultFlags,
        int preserveResultFlagsMask)
    {
        return DclComputePointNumericPlan.Build(
            naturalHpDebit,
            naturalHpCredit,
            naturalMpDebit,
            naturalMpCredit,
            naturalResultFlags,
            authoredHpDebit: channels.HpDebit,
            authoredHpCredit: channels.HpCredit,
            authoredMpDebit: channels.MpDebit,
            authoredMpCredit: channels.MpCredit,
            forcedMiss: !delivered,
            controlResultFlags,
            preserveResultFlagsMask);
    }

    public static DclCanonicalNativeCarrierProjection ProjectMagic(
        DclCanonicalRuntimeCatalog runtime,
        int abilityId,
        DclUnitKey payer,
        DclCanonicalMagicExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(result);
        if (!payer.IsValid || payer.BattleGeneration != result.Declaration.Source.BattleGeneration)
            throw new ArgumentException("The resource payer must be a stable unit in the action battle generation.", nameof(payer));
        if (payer != result.Declaration.Source)
            throw new ArgumentException("A canonical magic resource payment belongs to the declared source.", nameof(payer));

        (DclAbilityBinding binding, DclActionProfile profile) = runtime.ResolveAbility(abilityId);
        if (binding.CarrierKind != DclNativeCarrierKind.SingleResult ||
            binding.RewritePolicy is not (DclCarrierRewritePolicy.ReplaceNumericResult or DclCarrierRewritePolicy.ReplaceCompleteResult))
            throw new InvalidOperationException("The direct magic projection requires a replaceable SingleResult carrier.");
        if (profile.ActionId != result.Declaration.ActionId ||
            profile.ProfileRevision != result.Declaration.ProfileRevision)
            throw new ArgumentException("The resolved declaration does not match the native ability binding.", nameof(result));
        if (!result.Payment.Legal)
            throw new ArgumentException("An illegal resource settlement cannot be projected to a native carrier.", nameof(result));

        bool resourceFailed = result.Outcome == DclCastingOutcome.ResourceFailure;
        if (resourceFailed)
        {
            if (result.Transaction is not { Stage: DclActionTransactionStage.ResourceFailed, ReactionWindowOpened: false } ||
                result.Delivery is not null || result.Effect is not null || result.Healing is not null ||
                result.ResourceChange is not null || result.TouchContest is not null ||
                result.Payment is not { CostDue: 0, MpPaid: 0, HpPaid: 0 })
                throw new ArgumentException("ResourceFailure must remain empty through the native projection boundary.", nameof(result));
            return Build(binding, result, TargetResult: null, ResourcePayment: null, ResourceFailed: true);
        }

        DclMagicDeliveryResult delivery = result.Delivery ??
            throw new ArgumentException("A non-resource-failed direct magic result requires its target delivery result.", nameof(result));
        if (profile.DeliveryProfile.Delivery == DclDelivery.Touch)
        {
            DclPhysicalContestResult touch = result.TouchContest ??
                throw new ArgumentException("A non-resource-failed Touch result requires its exact physical contest.", nameof(result));
            DclMagicDeliveryOutcome expectedTouchDelivery = touch.Outcome switch
            {
                DclPhysicalOutcome.CriticalHit => DclMagicDeliveryOutcome.CriticalDelivered,
                DclPhysicalOutcome.Hit => DclMagicDeliveryOutcome.Delivered,
                DclPhysicalOutcome.Defended => DclMagicDeliveryOutcome.Defended,
                DclPhysicalOutcome.AttackMiss or DclPhysicalOutcome.AttackFumble => DclMagicDeliveryOutcome.BaseFailure,
                _ => throw new ArgumentException("Touch cannot project a legacy physical outcome.", nameof(result)),
            };
            if (delivery.Outcome != expectedTouchDelivery)
                throw new ArgumentException("Touch physical and magical delivery outcomes diverged before native projection.", nameof(result));
        }
        else if (result.TouchContest is not null)
        {
            throw new ArgumentException("Only Touch delivery may project a physical contest through the magic carrier.", nameof(result));
        }
        DclUnitKey finalTarget = result.ReflectRoute.FinalTarget;
        if (!finalTarget.IsValid || finalTarget.BattleGeneration != payer.BattleGeneration)
            throw new ArgumentException("The final routed target is not a stable unit in the action battle generation.", nameof(result));

        int hpDebit = 0;
        int hpCredit = 0;
        int mpDebit = 0;
        int mpCredit = 0;
        DclMagicalEffectRoute? magicalRoute = null;
        if (result.Effect is { } magical)
        {
            magicalRoute = magical.Route;
            hpDebit = magical.Route == DclMagicalEffectRoute.Injury ? magical.FinalInjury : 0;
            hpCredit = magical.Route == DclMagicalEffectRoute.AbsorbedHealing ? magical.AppliedHealing : 0;
        }
        if (result.Healing is { } healing)
        {
            if (result.Effect is not null)
                throw new ArgumentException("One direct target result cannot be both damage/absorb and direct healing.", nameof(result));
            hpCredit = healing.AppliedHealing;
        }
        DclCanonicalNativeSourceEffectProjection? sourceEffect = null;
        if (result.ResourceChange is { } resourceChange)
        {
            if (result.Effect is not null || result.Healing is not null)
                throw new ArgumentException("One direct target result cannot combine ResourceChange with damage/healing.", nameof(result));
            hpDebit = resourceChange.TargetChannels.HpDebit;
            hpCredit = resourceChange.TargetChannels.HpCredit;
            mpDebit = resourceChange.TargetChannels.MpDebit;
            mpCredit = resourceChange.TargetChannels.MpCredit;
            if (!resourceChange.SourceChannels.IsEmpty || resourceChange.SourceKo)
            {
                sourceEffect = new DclCanonicalNativeSourceEffectProjection(
                    payer,
                    resourceChange.SourceChannels,
                    resourceChange.SourceKo,
                    IsResourceChange: true,
                    OpensDamageReaction: false);
            }
        }
        bool carrierDelivered = delivery.Delivered && result.ResourceChange?.RejectedByUndeadPolicy != true;
        if (!delivery.Delivered && (result.Effect is not null || result.Healing is not null ||
            result.ResourceChange is not null || hpDebit != 0 || hpCredit != 0 || mpDebit != 0 || mpCredit != 0))
            throw new ArgumentException("A failed or defended delivery cannot project target numeric channels.", nameof(result));
        if ((magicalRoute == DclMagicalEffectRoute.Injury) != (result.InjuryConsequences is not null))
            throw new ArgumentException("Magical Injury and its universal per-Strike consequences must cross native projection together.", nameof(result));
        if (result.TargetKoAfterStrike && hpDebit == 0)
            throw new ArgumentException("Only an HP-debit target channel can own target KO in the direct magic projection.", nameof(result));

        int[] appliedRiders = result.StatusRiders
            .Where(rider => rider.Gate.Applied)
            .Select(rider => rider.EffectIndex)
            .Order()
            .ToArray();
        if (result.StatusRiders.Any(rider => rider.Gate.Applied != (rider.StateApplication is not null)))
            throw new ArgumentException("Applied Rider gates and durable state applications must agree before native projection.", nameof(result));
        DclCanonicalNativeAuxiliaryEffects? riderAuxiliary =
            DclCanonicalNativeAuxiliaryProjector.Merge(
                DclCanonicalNativeAuxiliaryProjector.ProjectInjuryStates(result.InjuryStates),
                DclCanonicalNativeAuxiliaryProjector.ProjectAimLifecycle(result.AimRetention),
                DclCanonicalNativeAuxiliaryProjector.ProjectForcedMovement(result.ForcedMovement),
                DclCanonicalNativeAuxiliaryProjector.ProjectStateApplications(
                    result.StatusRiders.Select(rider => rider.StateApplication)));

        var targetProjection = new DclCanonicalNativeTargetProjection(
            finalTarget,
            delivery.Outcome,
            carrierDelivered,
            new DclCanonicalNativeNumericChannels(hpDebit, hpCredit, mpDebit, mpCredit),
            magicalRoute,
            result.InjuryConsequences,
            result.TargetKoAfterStrike,
            appliedRiders,
            riderAuxiliary,
            PhysicalOutcome: result.TouchContest?.Outcome);
        DclCanonicalNativePaymentProjection paymentProjection = BuildPayment(payer, result.Payment);

        if (result.Transaction is not { Stage: DclActionTransactionStage.Settled } transaction)
            throw new ArgumentException("A non-resource-failed native projection requires one settled outer transaction.", nameof(result));
        return Build(binding, result, targetProjection, paymentProjection, ResourceFailed: false) with
        {
            ReactionWindowOpened = transaction.ReactionWindowOpened,
            SourceEffect = sourceEffect,
            Reactions = result.Reactions,
        };
    }

    private static DclCanonicalNativeCarrierProjection Build(
        DclAbilityBinding binding,
        DclCanonicalMagicExecutionResult result,
        DclCanonicalNativeTargetProjection? TargetResult,
        DclCanonicalNativePaymentProjection? ResourcePayment,
        bool ResourceFailed)
        => new(
            result.Declaration.ActionInstanceId,
            binding.AbilityId,
            binding.ActionId,
            binding.ProfileRevision,
            binding.CarrierKind,
            binding.RewritePolicy,
            binding.ForecastBoundary,
            binding.AiBoundary,
            binding.ExecutionBoundary,
            binding.ApplyBoundary,
            binding.PresentationBoundary,
            TargetResult,
            ResourcePayment,
            result.Transaction?.ReactionWindowOpened ?? false,
            ResourceFailed);

    internal static DclCanonicalNativePaymentProjection BuildPayment(
        DclUnitKey payer,
        DclResourcePayment payment)
        => new(
            payer,
            new DclCanonicalNativeNumericChannels(payment.HpPaid, 0, payment.MpPaid, 0),
            payment.PayerKo,
            IsResourcePayment: true,
            OpensDamageReaction: false);
}
