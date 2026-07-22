namespace fftivc.generic.chronicle.codemod;

internal enum DclCanonicalNativeActionStage
{
    Published,
    ApplyingStrikes,
    StrikesApplied,
    SourceEffectCommitted,
    PaymentCommitted,
    ReactionOpened,
    Settled,
}

internal sealed record DclCanonicalNativeActionPlan(
    long ActionInstanceId,
    DclUnitKey Source,
    int AbilityId,
    string ActionId,
    int ProfileRevision,
    DclNativeCarrierKind CarrierKind,
    DclCarrierRewritePolicy RewritePolicy,
    string ApplyBoundary,
    string PresentationBoundary,
    IReadOnlyList<DclCanonicalNativeStrikeProjection> Strikes,
    DclCanonicalNativePaymentProjection? ResourcePayment,
    bool ReactionWindowOpened,
    bool ResourceFailed,
    DclCanonicalNativeSourceEffectProjection? SourceEffect = null,
    DclCanonicalReactionWindowResult? Reactions = null);

internal sealed record DclCanonicalNativeReactionEffectAcknowledgement(
    DclCanonicalReactionResult Reaction,
    int EffectCompletionIndex,
    int ExpectedEffectCompletionCount,
    bool ReactionCompleted);

/// <summary>
/// Owns the native delivery cursor for one already-resolved outer ActionInstance. Canonical
/// resolution publishes exactly once. Native apply then consumes each target/Strike in stable
/// order, commits resource payment as a separate step, acknowledges the single Reaction window,
/// and only then settles presentation/cleanup.
/// </summary>
internal sealed class DclCanonicalNativeActionApplication
{
    private readonly HashSet<(DclUnitKey Target, int StrikeIndex)> _applied = [];
    private readonly HashSet<(int NativeOrder, DclUnitKey Reactor, string ReactionId, int EffectCompletionIndex)>
        _completedReactionEffects = [];
    private int _nextStrike;
    private int _nextReaction;
    private int _nextReactionEffectCompletion;
    private int _completedReactionEffectDeliveries;

    public DclCanonicalNativeActionApplication(DclCanonicalNativeActionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        foreach (IGrouping<DclUnitKey, DclCanonicalNativeStrikeProjection> targetStrikes in
            plan.Strikes.GroupBy(strike => strike.Target))
        {
            DclCanonicalForcedMovementResult? previousMovement = null;
            foreach (DclCanonicalForcedMovementResult movement in targetStrikes
                .Select(strike => strike.AuxiliaryEffects?.ForcedMovement)
                .OfType<DclCanonicalForcedMovementResult>())
            {
                if (previousMovement is not null && movement.Origin != previousMovement.Destination)
                    throw new InvalidOperationException(
                        "Multiple settled movements for one target must form one contiguous conditional origin timeline.");
                previousMovement = movement;
            }
        }
        Plan = plan;
        Stage = DclCanonicalNativeActionStage.Published;
    }

    public DclCanonicalNativeActionPlan Plan { get; }
    public DclCanonicalNativeActionStage Stage { get; private set; }
    public int AppliedStrikeCount => _nextStrike;
    public bool PaymentAcknowledged { get; private set; }
    public bool SourceEffectAcknowledged { get; private set; }
    public bool ReactionAcknowledged { get; private set; }
    public int CompletedReactionCount => _nextReaction;
    public int CompletedReactionEffectDeliveryCount => _completedReactionEffectDeliveries;

    public DclCanonicalNativeStrikeProjection PeekNextStrike()
    {
        if (Plan.ResourceFailed || _nextStrike >= Plan.Strikes.Count ||
            Stage is not (DclCanonicalNativeActionStage.Published or DclCanonicalNativeActionStage.ApplyingStrikes))
            throw new InvalidOperationException("This canonical action has no pending target/Strike carrier.");
        return Plan.Strikes[_nextStrike];
    }

    public DclCanonicalNativeStrikeProjection ApplyNextStrike(DclUnitKey target, int strikeIndex)
    {
        if (Plan.ResourceFailed)
            throw new InvalidOperationException("ResourceFailure has no target carrier to apply.");
        if (Stage is not (DclCanonicalNativeActionStage.Published or DclCanonicalNativeActionStage.ApplyingStrikes))
            throw new InvalidOperationException("Target carriers can be applied only before resource payment.");
        if (_nextStrike >= Plan.Strikes.Count)
            throw new InvalidOperationException("Every target/Strike carrier has already been applied.");
        DclCanonicalNativeStrikeProjection next = Plan.Strikes[_nextStrike];
        if (next.Target != target || next.StrikeIndex != strikeIndex)
            throw new InvalidOperationException(
                $"Native carrier order diverged: expected {next.Target}/Strike {next.StrikeIndex}, received {target}/Strike {strikeIndex}.");
        if (!_applied.Add((target, strikeIndex)))
            throw new InvalidOperationException("One target/Strike carrier cannot be applied twice.");
        _nextStrike++;
        Stage = _nextStrike == Plan.Strikes.Count
            ? DclCanonicalNativeActionStage.StrikesApplied
            : DclCanonicalNativeActionStage.ApplyingStrikes;
        return next;
    }

    public DclCanonicalNativeStrikeProjection ReadAppliedForPresentation(DclUnitKey target, int strikeIndex)
    {
        if (!_applied.Contains((target, strikeIndex)))
            throw new InvalidOperationException("Presentation cannot observe a target/Strike before its native apply boundary.");
        return Plan.Strikes.Single(strike => strike.Target == target && strike.StrikeIndex == strikeIndex);
    }

    public DclCanonicalNativePaymentProjection? CommitResourcePayment()
    {
        if (Plan.ResourceFailed)
            throw new InvalidOperationException("ResourceFailure commits no resource payment.");
        DclCanonicalNativeActionStage required = Plan.SourceEffect is null
            ? DclCanonicalNativeActionStage.StrikesApplied
            : DclCanonicalNativeActionStage.SourceEffectCommitted;
        if (Stage != required)
            throw new InvalidOperationException("Resource payment follows every target/Strike and source-effect carrier.");
        if (PaymentAcknowledged)
            throw new InvalidOperationException("Resource payment cannot be committed twice.");
        PaymentAcknowledged = true;
        Stage = DclCanonicalNativeActionStage.PaymentCommitted;
        return Plan.ResourcePayment;
    }

    public DclCanonicalNativeSourceEffectProjection? CommitSourceEffect()
    {
        if (Plan.ResourceFailed)
            throw new InvalidOperationException("ResourceFailure commits no source effect.");
        if (Plan.SourceEffect is null)
            throw new InvalidOperationException("This canonical action has no source effect carrier.");
        if (Stage != DclCanonicalNativeActionStage.StrikesApplied)
            throw new InvalidOperationException("A source effect follows every target/Strike carrier.");
        if (SourceEffectAcknowledged)
            throw new InvalidOperationException("A source effect cannot be committed twice.");
        SourceEffectAcknowledged = true;
        Stage = DclCanonicalNativeActionStage.SourceEffectCommitted;
        return Plan.SourceEffect;
    }

    public void AcknowledgeReactionWindow()
    {
        if (!Plan.ReactionWindowOpened)
            throw new InvalidOperationException("The canonical action did not open a Reaction window.");
        if (Stage is not (DclCanonicalNativeActionStage.PaymentCommitted or DclCanonicalNativeActionStage.ReactionOpened))
            throw new InvalidOperationException("The Reaction window follows target effects and resource payment.");
        if (ReactionAcknowledged)
            throw new InvalidOperationException("One outer ActionInstance cannot open a second Reaction window.");
        int acceptedCount = Plan.Reactions?.Accepted.Count ?? 0;
        if (_nextReaction != acceptedCount)
            throw new InvalidOperationException(
                $"The native Reaction window completed after {_nextReaction} of {acceptedCount} accepted canonical effects.");
        ReactionAcknowledged = true;
        Stage = DclCanonicalNativeActionStage.ReactionOpened;
    }

    public DclCanonicalNativeReactionEffectAcknowledgement AcknowledgeReactionEffect(
        DclUnitKey reactor,
        int nativeReactionAbilityId,
        int nativeEffectAbilityId)
    {
        if (!Plan.ReactionWindowOpened || Plan.Reactions is not { } reactions)
            throw new InvalidOperationException("This canonical action has no resolved Reaction effects to acknowledge.");
        if (Stage is not (DclCanonicalNativeActionStage.PaymentCommitted or DclCanonicalNativeActionStage.ReactionOpened) ||
            ReactionAcknowledged)
            throw new InvalidOperationException("Reaction effects must complete after payment and before terminal window acknowledgement.");
        if (_nextReaction >= reactions.Accepted.Count)
            throw new InvalidOperationException("Every accepted canonical Reaction effect has already completed.");
        DclCanonicalReactionResult expected = reactions.Accepted[_nextReaction];
        DclNativeReactionBinding binding = expected.NativeBinding;
        if (expected.Reactor != reactor ||
            binding.NativeReactionAbilityId != nativeReactionAbilityId ||
            binding.NativeEffectAbilityId != nativeEffectAbilityId)
            throw new InvalidOperationException(
                $"Native Reaction effect diverged: expected order {expected.NativeOrder}, reactor {expected.Reactor}, " +
                $"presentation {binding.NativeReactionAbilityId}, effect {binding.NativeEffectAbilityId}.");
        int expectedCompletions = expected.EffectAction?.TransactionProfile.StrikeCount ??
            throw new InvalidOperationException("An accepted canonical Reaction lost its exact effect Action.");
        int completionIndex = _nextReactionEffectCompletion;
        var identity = (expected.NativeOrder, reactor, expected.Definition.ReactionId, completionIndex);
        if (!_completedReactionEffects.Add(identity))
            throw new InvalidOperationException("One accepted Reaction effect delivery cannot complete twice.");
        _nextReactionEffectCompletion++;
        _completedReactionEffectDeliveries++;
        bool reactionCompleted = _nextReactionEffectCompletion == expectedCompletions;
        if (_nextReactionEffectCompletion > expectedCompletions)
            throw new InvalidOperationException("Native Reaction effect produced more completions than its normalized Strike count.");
        if (reactionCompleted)
        {
            _nextReaction++;
            _nextReactionEffectCompletion = 0;
        }
        Stage = DclCanonicalNativeActionStage.ReactionOpened;
        return new DclCanonicalNativeReactionEffectAcknowledgement(
            expected,
            completionIndex,
            expectedCompletions,
            reactionCompleted);
    }

    public void Settle()
    {
        if (Plan.ResourceFailed)
        {
            if (Stage != DclCanonicalNativeActionStage.Published)
                throw new InvalidOperationException("ResourceFailure settlement cannot own partial native delivery.");
            Stage = DclCanonicalNativeActionStage.Settled;
            return;
        }
        DclCanonicalNativeActionStage required = Plan.ReactionWindowOpened
            ? DclCanonicalNativeActionStage.ReactionOpened
            : DclCanonicalNativeActionStage.PaymentCommitted;
        if (Stage != required)
            throw new InvalidOperationException("Native cleanup cannot settle before effects, payment, and the declared Reaction boundary.");
        Stage = DclCanonicalNativeActionStage.Settled;
    }
}

internal sealed class DclCanonicalNativeActionLedger
{
    private readonly object _gate = new();
    private readonly Dictionary<long, DclCanonicalNativeActionApplication> _actions = [];
    private readonly HashSet<long> _retiredActionInstanceIds = [];

    public DclCanonicalNativeActionLedger(int battleGeneration)
    {
        if (battleGeneration <= 0) throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        BattleGeneration = battleGeneration;
    }

    public int BattleGeneration { get; }
    public int Count { get { lock (_gate) return _actions.Count; } }

    public DclCanonicalNativeActionApplication Publish(
        DclUnitKey source,
        DclAbilityBinding binding,
        DclCanonicalNativeMultiCarrierProjection projection)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(projection);
        RequireIdentity(source, binding, projection.ActionInstanceId, projection.AbilityId,
            projection.ActionId, projection.ProfileRevision, projection.CarrierKind, projection.RewritePolicy);
        return Publish(new DclCanonicalNativeActionPlan(
            projection.ActionInstanceId,
            source,
            projection.AbilityId,
            projection.ActionId,
            projection.ProfileRevision,
            projection.CarrierKind,
            projection.RewritePolicy,
            binding.ApplyBoundary,
            binding.PresentationBoundary,
            projection.Strikes.ToArray(),
            projection.ResourcePayment,
            projection.ReactionWindowOpened,
            projection.ResourceFailed,
            projection.SourceEffect,
            projection.Reactions));
    }

    public DclCanonicalNativeActionApplication Publish(
        DclUnitKey source,
        DclAbilityBinding binding,
        DclCanonicalNativeCarrierProjection projection)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(projection);
        RequireIdentity(source, binding, projection.ActionInstanceId, projection.AbilityId,
            projection.ActionId, projection.ProfileRevision, projection.CarrierKind, projection.RewritePolicy);
        DclCanonicalNativeStrikeProjection[] strikes = projection.TargetResult is { } target
            ?
            [
                new DclCanonicalNativeStrikeProjection(
                    target.Target,
                    StrikeIndex: 0,
                    target.DeliveryOutcome,
                    target.PhysicalOutcome,
                    target.Delivered,
                    target.Channels,
                    target.MagicalRoute,
                    target.InjuryConsequences,
                    target.TargetKo,
                    KoShortCircuited: false,
                    target.AuxiliaryEffects),
            ]
            : [];
        return Publish(new DclCanonicalNativeActionPlan(
            projection.ActionInstanceId,
            source,
            projection.AbilityId,
            projection.ActionId,
            projection.ProfileRevision,
            projection.CarrierKind,
            projection.RewritePolicy,
            projection.ApplyBoundary,
            projection.PresentationBoundary,
            strikes,
            projection.ResourcePayment,
            projection.ReactionWindowOpened,
            projection.ResourceFailed,
            projection.SourceEffect,
            projection.Reactions));
    }

    public DclCanonicalNativeActionApplication Get(long actionInstanceId)
    {
        lock (_gate)
            return _actions.TryGetValue(actionInstanceId, out DclCanonicalNativeActionApplication? action)
                ? action
                : throw new KeyNotFoundException($"Canonical ActionInstance {actionInstanceId} is not published.");
    }

    public bool TryGet(long actionInstanceId, out DclCanonicalNativeActionApplication action)
    {
        lock (_gate)
            return _actions.TryGetValue(actionInstanceId, out action!);
    }

    public bool ContainsPublishedOrRetired(long actionInstanceId)
    {
        lock (_gate)
            return _actions.ContainsKey(actionInstanceId) || _retiredActionInstanceIds.Contains(actionInstanceId);
    }

    public bool TryFindPendingApply(
        DclUnitKey source,
        int abilityId,
        DclUnitKey target,
        int strikeIndex,
        out DclCanonicalNativeActionApplication application)
    {
        application = null!;
        if (!source.IsValid || source.BattleGeneration != BattleGeneration ||
            !target.IsValid || target.BattleGeneration != BattleGeneration || strikeIndex < 0)
            return false;
        lock (_gate)
        {
            DclCanonicalNativeActionApplication[] matches = _actions.Values.Where(candidate =>
            {
                if (candidate.Plan.Source != source || candidate.Plan.AbilityId != abilityId ||
                    candidate.Plan.ResourceFailed || candidate.Plan.Strikes.Count == 0 ||
                    candidate.Stage is not (DclCanonicalNativeActionStage.Published or DclCanonicalNativeActionStage.ApplyingStrikes))
                    return false;
                DclCanonicalNativeStrikeProjection next = candidate.PeekNextStrike();
                return next.Target == target && next.StrikeIndex == strikeIndex;
            }).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException("Native apply identity matched multiple unresolved canonical ActionInstances.");
            if (matches.Length == 0) return false;
            application = matches[0];
            return true;
        }
    }

    public void Retire(long actionInstanceId)
    {
        lock (_gate)
        {
            DclCanonicalNativeActionApplication action = Get(actionInstanceId);
            if (action.Stage != DclCanonicalNativeActionStage.Settled)
                throw new InvalidOperationException("An unsettled canonical ActionInstance cannot be retired.");
            _actions.Remove(actionInstanceId);
            _retiredActionInstanceIds.Add(actionInstanceId);
        }
    }

    private DclCanonicalNativeActionApplication Publish(DclCanonicalNativeActionPlan plan)
    {
        ValidatePlan(plan);
        lock (_gate)
        {
            if (_actions.ContainsKey(plan.ActionInstanceId) || _retiredActionInstanceIds.Contains(plan.ActionInstanceId))
                throw new InvalidOperationException($"Canonical ActionInstance {plan.ActionInstanceId} is already published.");
            var action = new DclCanonicalNativeActionApplication(plan);
            _actions.Add(plan.ActionInstanceId, action);
            return action;
        }
    }

    private void RequireIdentity(
        DclUnitKey source,
        DclAbilityBinding binding,
        long actionInstanceId,
        int abilityId,
        string actionId,
        int profileRevision,
        DclNativeCarrierKind carrierKind,
        DclCarrierRewritePolicy rewritePolicy)
    {
        if (!source.IsValid || source.BattleGeneration != BattleGeneration)
            throw new ArgumentException("Action source does not belong to this native ledger battle generation.", nameof(source));
        if (actionInstanceId <= 0 || abilityId != binding.AbilityId || actionId != binding.ActionId ||
            profileRevision != binding.ProfileRevision || carrierKind != binding.CarrierKind ||
            rewritePolicy != binding.RewritePolicy)
            throw new ArgumentException("Native projection identity does not match its normalized ability binding.", nameof(binding));
    }

    private void ValidatePlan(DclCanonicalNativeActionPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.ApplyBoundary) || string.IsNullOrWhiteSpace(plan.PresentationBoundary))
            throw new ArgumentException("Native apply and presentation boundaries are required.", nameof(plan));
        if (plan.Strikes.Select(strike => (strike.Target, strike.StrikeIndex)).Distinct().Count() != plan.Strikes.Count)
            throw new ArgumentException("One native action plan cannot duplicate target/Strike identity.", nameof(plan));
        if (plan.Strikes.Any(strike => !strike.Target.IsValid || strike.Target.BattleGeneration != BattleGeneration))
            throw new ArgumentException("Every target carrier must belong to this ledger battle generation.", nameof(plan));
        if (plan.ResourcePayment is { } payment &&
            (!payment.Payer.IsValid || payment.Payer.BattleGeneration != BattleGeneration || payment.Payer != plan.Source))
            throw new ArgumentException("Resource payment must remain owned by the action source in this battle generation.", nameof(plan));
        if (plan.SourceEffect is { } sourceEffect &&
            (!sourceEffect.Source.IsValid || sourceEffect.Source.BattleGeneration != BattleGeneration ||
             sourceEffect.Source != plan.Source || !sourceEffect.IsResourceChange || sourceEffect.OpensDamageReaction))
            throw new ArgumentException("A source effect must be a non-damage ResourceChange owned by the action source.", nameof(plan));
        if (plan.ResourceFailed && (plan.Strikes.Count != 0 || plan.ResourcePayment is not null ||
            plan.SourceEffect is not null || plan.ReactionWindowOpened || plan.Reactions is not null))
            throw new ArgumentException("ResourceFailure must publish no target, source effect, payment, or Reaction carrier.", nameof(plan));
        if (!plan.ResourceFailed && plan.Strikes.Count == 0)
            throw new ArgumentException("A resolved native action must publish at least one target/Strike carrier.", nameof(plan));
        if (plan.ReactionWindowOpened != (plan.Reactions is not null))
            throw new ArgumentException(
                "A declared native Reaction window requires its exact resolved result, while a no-window plan cannot carry one.",
                nameof(plan));
        if (plan.Reactions is { } reactions)
        {
            if (!plan.ReactionWindowOpened || reactions.ActionInstanceId != plan.ActionInstanceId)
                throw new ArgumentException(
                    "A projected Reaction window must belong to the same outer ActionInstance and declared window.",
                    nameof(plan));
            if (reactions.Candidates.Any(candidate =>
                    candidate.Reactor.BattleGeneration != BattleGeneration ||
                    !StringComparer.Ordinal.Equals(candidate.NativeBinding.ReactionId, candidate.Definition.ReactionId) ||
                    candidate.NativeBinding.NativeReactionAbilityId is < 0 or > 511 ||
                    candidate.NativeBinding.NativeEffectAbilityId is < 0 or > 511 ||
                    candidate.EffectRoute is { } route &&
                    (route.Source.BattleGeneration != BattleGeneration || route.Target.BattleGeneration != BattleGeneration)))
                throw new ArgumentException(
                    "Every projected Reaction candidate and effect route must remain in the native action battle generation.",
                    nameof(plan));
            if (reactions.Candidates.Any(candidate => candidate.CardinalityAccepted
                    ? candidate.EffectAction is null || candidate.EffectRoute is null ||
                      !StringComparer.Ordinal.Equals(candidate.EffectAction.ActionId, candidate.NativeBinding.EffectActionId) ||
                      candidate.EffectAction.ProfileRevision != candidate.NativeBinding.EffectProfileRevision
                    : candidate.EffectAction is not null || candidate.EffectRoute is not null))
                throw new ArgumentException(
                    "Projected Reaction acceptance, effect Action revision, route, and native binding must agree exactly.",
                    nameof(plan));
        }
    }
}
