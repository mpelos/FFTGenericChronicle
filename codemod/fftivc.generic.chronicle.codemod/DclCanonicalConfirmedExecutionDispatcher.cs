namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalConfirmedExecutionDispatchResult(
    int AbilityId,
    DclCanonicalActionFamily Family,
    object? Resolution,
    DclCanonicalNativeActionApplication? Application,
    bool NativePassthrough);

/// <summary>
/// One confirmed resolve-and-publish entry point. The atomic ability family selects the only legal
/// coordinator; family inputs contain deterministic snapshots only, and the battle owner retains
/// every random site, state commit, publication, payment, and Reaction identity.
/// </summary>
internal static class DclCanonicalConfirmedExecutionDispatcher
{
    public static void ValidatePreparedActionInput(
        DclCanonicalPreparedActionTicket ticket,
        object familyInput)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(familyInput);
        if (ticket.Stage != DclCanonicalPreparedActionStage.Reserved || ticket.Application is not null)
            throw new InvalidOperationException("Only the exact reserved prepared Action can enter confirmed execution.");

        switch (familyInput)
        {
            case DclCanonicalPhysicalExecutionRequest physical:
                RequirePreparedAbility(ticket, physical.AbilityId);
                if (physical.Declaration != ticket.Declaration)
                    throw new ArgumentException(
                        "Prepared physical execution must carry the exact reserved declaration.",
                        nameof(familyInput));
                return;
            case DclCanonicalMagicExecutionRequest direct:
                RequirePreparedCast(ticket, direct.AbilityId, direct.ActionInstanceId, direct.DeclarationRequest);
                return;
            case DclCanonicalAreaMagicExecutionRequest area:
                RequirePreparedCast(ticket, area.AbilityId, area.ActionInstanceId, area.DeclarationRequest);
                return;
            case DclCanonicalStatusExecutionInput status:
                RequirePreparedCast(ticket, status.AbilityId, status.ActionInstanceId, status.DeclarationRequest);
                return;
            case DclCanonicalStatusRemovalExecutionInput removal:
                RequirePreparedCast(ticket, removal.AbilityId, removal.ActionInstanceId, removal.DeclarationRequest);
                return;
            case DclCanonicalDispelExecutionInput dispel:
                RequirePreparedCast(ticket, dispel.AbilityId, dispel.ActionInstanceId, dispel.DeclarationRequest);
                return;
            case DclCanonicalQuickExecutionInput quick:
                RequirePreparedCast(ticket, quick.AbilityId, quick.ActionInstanceId, quick.DeclarationRequest);
                return;
            case DclCanonicalReviveExecutionInput revive:
                RequirePreparedCast(ticket, revive.AbilityId, revive.ActionInstanceId, revive.DeclarationRequest);
                return;
            case DclCanonicalForcedMovementExecutionRequest movement:
                RequirePreparedCast(ticket, movement.AbilityId, movement.ActionInstanceId, movement.DeclarationRequest);
                return;
            default:
                throw new ArgumentException(
                    $"Prepared Action execution does not accept {familyInput.GetType().Name}.",
                    nameof(familyInput));
        }
    }

    public static DclCanonicalConfirmedExecutionDispatchResult ResolveAndPublish(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        object familyInput,
        IReadOnlyList<DclCanonicalReactionCandidate>? auxiliaryReactionCandidates = null)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(familyInput);
        DclCanonicalActionFamily family = battle.Catalog.ResolveAbilityFamily(abilityId);
        return family switch
        {
            DclCanonicalActionFamily.PhysicalDamage when familyInput is DclCanonicalPhysicalExecutionRequest input =>
                Physical(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.DirectNumeric when familyInput is DclCanonicalMagicExecutionRequest input =>
                Direct(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.AreaNumeric when familyInput is DclCanonicalAreaMagicExecutionRequest input =>
                Area(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.StatusApplication when familyInput is DclCanonicalStatusExecutionInput input =>
                Status(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.StatusRemoval when familyInput is DclCanonicalStatusRemovalExecutionInput input =>
                StatusRemoval(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.Dispel when familyInput is DclCanonicalDispelExecutionInput input =>
                Dispel(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.Quick when familyInput is DclCanonicalQuickExecutionInput input =>
                Quick(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.Revive when familyInput is DclCanonicalReviveExecutionInput input =>
                Revive(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.ForcedMovement when familyInput is DclCanonicalForcedMovementExecutionRequest input =>
                ForcedMovement(battle, abilityId, input, auxiliaryReactionCandidates),
            DclCanonicalActionFamily.NativeSpecialPreserved => NativePassthrough(abilityId, family,
                auxiliaryReactionCandidates),
            _ => throw new ArgumentException(
                $"Ability {abilityId} family {family} requires its exact deterministic execution input, not {familyInput.GetType().Name}.",
                nameof(familyInput)),
        };
    }

    public static DclCanonicalConfirmedExecutionDispatchResult ResolveAndPublish(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeComposedExecution composed)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(composed);
        DclCanonicalActionFamily runtimeFamily = battle.Catalog.ResolveAbilityFamily(composed.AbilityId);
        if (runtimeFamily != composed.Family)
            throw new ArgumentException(
                "A composed native request must retain the same atomic ability family as the runtime catalog.",
                nameof(composed));
        return ResolveAndPublish(
            battle,
            composed.AbilityId,
            composed.FamilyInput,
            auxiliaryReactionCandidates: null);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult Physical(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalPhysicalExecutionRequest input,
        IReadOnlyList<DclCanonicalReactionCandidate>? auxiliaryReactions)
    {
        RequireAbilityAndEmbeddedReactions(abilityId, input.AbilityId, auxiliaryReactions);
        DclCanonicalPublishedPhysicalExecution published =
            DclCanonicalPhysicalExecutionCoordinator.ResolveAndPublish(battle, input);
        return Result(abilityId, DclCanonicalActionFamily.PhysicalDamage, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult Direct(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalMagicExecutionRequest input,
        IReadOnlyList<DclCanonicalReactionCandidate>? auxiliaryReactions)
    {
        RequireAbilityAndEmbeddedReactions(abilityId, input.AbilityId, auxiliaryReactions);
        DclCanonicalPublishedMagicExecution published =
            DclCanonicalMagicExecutionCoordinator.ResolveAndPublish(battle, input);
        return Result(abilityId, DclCanonicalActionFamily.DirectNumeric, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult Area(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalAreaMagicExecutionRequest input,
        IReadOnlyList<DclCanonicalReactionCandidate>? auxiliaryReactions)
    {
        RequireAbilityAndEmbeddedReactions(abilityId, input.AbilityId, auxiliaryReactions);
        DclCanonicalPublishedAreaMagicExecution published =
            DclCanonicalAreaMagicExecutionCoordinator.ResolveAndPublish(battle, input);
        return Result(abilityId, DclCanonicalActionFamily.AreaNumeric, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult Status(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalStatusExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactions)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalPublishedAuxiliaryExecution<DclCanonicalStatusExecutionResult> published =
            DclCanonicalAuxiliaryExecutionCoordinator.ResolveStatus(battle, input, reactions);
        return Result(abilityId, DclCanonicalActionFamily.StatusApplication, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult StatusRemoval(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalStatusRemovalExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactions)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalPublishedAuxiliaryExecution<DclCanonicalStatusRemovalExecutionResult> published =
            DclCanonicalAuxiliaryExecutionCoordinator.ResolveStatusRemoval(battle, input, reactions);
        return Result(abilityId, DclCanonicalActionFamily.StatusRemoval, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult Dispel(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalDispelExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactions)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalPublishedAuxiliaryExecution<DclCanonicalDispelExecutionResult> published =
            DclCanonicalAuxiliaryExecutionCoordinator.ResolveDispel(battle, input, reactions);
        return Result(abilityId, DclCanonicalActionFamily.Dispel, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult Quick(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalQuickExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactions)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalPublishedAuxiliaryExecution<DclCanonicalQuickExecutionResult> published =
            DclCanonicalAuxiliaryExecutionCoordinator.ResolveQuick(battle, input, reactions);
        return Result(abilityId, DclCanonicalActionFamily.Quick, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult Revive(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalReviveExecutionInput input,
        IReadOnlyList<DclCanonicalReactionCandidate>? reactions)
    {
        RequireAbility(abilityId, input.AbilityId);
        DclCanonicalPublishedAuxiliaryExecution<DclCanonicalReviveExecutionResult> published =
            DclCanonicalAuxiliaryExecutionCoordinator.ResolveRevive(battle, input, reactions);
        return Result(abilityId, DclCanonicalActionFamily.Revive, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult ForcedMovement(
        DclCanonicalBattleRuntime battle,
        int abilityId,
        DclCanonicalForcedMovementExecutionRequest input,
        IReadOnlyList<DclCanonicalReactionCandidate>? auxiliaryReactions)
    {
        RequireAbilityAndEmbeddedReactions(abilityId, input.AbilityId, auxiliaryReactions);
        DclCanonicalPublishedForcedMovementExecution published =
            DclCanonicalForcedMovementExecutionCoordinator.ResolveAndPublish(battle, input);
        return Result(abilityId, DclCanonicalActionFamily.ForcedMovement, published.Result, published.Application);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult NativePassthrough(
        int abilityId,
        DclCanonicalActionFamily family,
        IReadOnlyList<DclCanonicalReactionCandidate>? auxiliaryReactions)
    {
        if (auxiliaryReactions is not null)
            throw new ArgumentException("A preserved native special owns its native Reaction path.", nameof(auxiliaryReactions));
        return new DclCanonicalConfirmedExecutionDispatchResult(
            abilityId,
            family,
            Resolution: null,
            Application: null,
            NativePassthrough: true);
    }

    private static DclCanonicalConfirmedExecutionDispatchResult Result(
        int abilityId,
        DclCanonicalActionFamily family,
        object resolution,
        DclCanonicalNativeActionApplication application)
        => new(abilityId, family, resolution, application, NativePassthrough: false);

    private static void RequireAbilityAndEmbeddedReactions(
        int dispatchedAbilityId,
        int inputAbilityId,
        IReadOnlyList<DclCanonicalReactionCandidate>? auxiliaryReactions)
    {
        RequireAbility(dispatchedAbilityId, inputAbilityId);
        if (auxiliaryReactions is not null)
            throw new ArgumentException(
                "This family carries its Reaction candidates inside the deterministic execution request.",
                nameof(auxiliaryReactions));
    }

    private static void RequireAbility(int dispatchedAbilityId, int inputAbilityId)
    {
        if (dispatchedAbilityId != inputAbilityId)
            throw new ArgumentException(
                $"Dispatched ability {dispatchedAbilityId} does not match family input ability {inputAbilityId}.");
    }

    private static void RequirePreparedCast(
        DclCanonicalPreparedActionTicket ticket,
        int abilityId,
        long actionInstanceId,
        DclCastDeclarationRequest request)
    {
        RequirePreparedAbility(ticket, abilityId);
        if (actionInstanceId != ticket.Declaration.ActionInstanceId)
            throw new ArgumentException("Prepared cast execution changed its reserved ActionInstance identity.");
        DclCastDeclarationAttempt attempt = DclMagicTargeting.Declare(request, actionInstanceId);
        if (!attempt.Legal || attempt.Declaration != ticket.Declaration)
            throw new ArgumentException("Prepared cast execution must recreate the exact reserved declaration.");
    }

    private static void RequirePreparedAbility(DclCanonicalPreparedActionTicket ticket, int abilityId)
    {
        if (abilityId != ticket.EffectBinding.AbilityId)
            throw new ArgumentException(
                $"Prepared Action reserved ability {ticket.EffectBinding.AbilityId}, not {abilityId}.");
    }
}
