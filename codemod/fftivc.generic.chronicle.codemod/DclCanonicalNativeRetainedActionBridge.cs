namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeRetainedExecutionRequest(
    long ActionInstanceId,
    IReadOnlyList<DclCanonicalNativeUnitPolicySource> UnitPolicySources,
    object FamilyPolicyInputs);

internal sealed record DclCanonicalNativeRetainedPolicySourceExecutionRequest(
    long ActionInstanceId,
    IReadOnlyList<DclCanonicalNativeUnitPolicySource> UnitPolicySources,
    object FamilyPolicySource);

internal sealed record DclCanonicalNativeRetainedExecutionResult(
    DclCanonicalNativeAdmittedAction Action,
    DclCanonicalNativeComposedExecution Composed,
    DclCanonicalConfirmedExecutionDispatchResult Dispatch);

internal enum DclCanonicalNativePolicyTicketBridgeStatus
{
    Published,
    MissingAdmittedAction,
    MissingPolicyTicket,
    ActionAlreadyPublished,
}

internal sealed record DclCanonicalNativePolicyTicketBridgeResult(
    DclCanonicalNativePolicyTicketBridgeStatus Status,
    DclCanonicalNativeRetainedExecutionResult? Execution);

internal sealed record DclCanonicalNativePolicyTicketIntakeExecutionResult(
    DclCanonicalNativePolicyTicketPublishResult Intake,
    DclCanonicalNativePolicyTicketBridgeResult Bridge);

internal sealed record DclCanonicalNativeAdmissionIntakeExecutionResult(
    DclCanonicalNativeAdmittedActionPublishResult Intake,
    DclCanonicalNativePolicyTicketBridgeResult Bridge);

internal sealed record DclCanonicalNativeAdmissionTemplateExecutionResult(
    DclCanonicalNativeAdmissionIntakeExecutionResult Admission,
    DclCanonicalNativePolicyTicketTemplateBuildResult? Template,
    DclCanonicalNativePolicyTicketIntakeExecutionResult? Ticket)
{
    public DclCanonicalNativePolicyTicketBridgeResult FinalBridge => Ticket?.Bridge ?? Admission.Bridge;
}

/// <summary>
/// Bridges one complete retained native admission to canonical execution publication. The bridge is
/// intentionally policy-source driven: live/native callers must supply explicit unit and family
/// policy sources, and no job, target, defense, timeline, or movement fact is inferred here.
/// </summary>
internal static class DclCanonicalNativeRetainedActionBridge
{
    public static DclCanonicalNativeAdmissionTemplateExecutionResult TryPublishAdmissionBuildTemplateAndResolve(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(action);
        DclCanonicalNativeAdmissionIntakeExecutionResult admission =
            TryPublishAdmissionAndResolve(battle, action);
        if (admission.Bridge.Status != DclCanonicalNativePolicyTicketBridgeStatus.MissingPolicyTicket)
            return new DclCanonicalNativeAdmissionTemplateExecutionResult(
                admission,
                Template: null,
                Ticket: null);

        DclCanonicalNativePolicyTicketTemplateBuildResult template =
            battle.Catalog.PolicyTicketTemplates.TryBuildTicket(battle, action);
        if (template.Status != DclCanonicalNativePolicyTicketTemplateBuildStatus.Built ||
            template.Ticket is null)
            return new DclCanonicalNativeAdmissionTemplateExecutionResult(
                admission,
                template,
                Ticket: null);

        DclCanonicalNativePolicyTicketIntakeExecutionResult ticket =
            TryPublishTicketAndResolve(battle, template.Ticket);
        return new DclCanonicalNativeAdmissionTemplateExecutionResult(
            admission,
            template,
            ticket);
    }

    public static DclCanonicalNativeAdmissionIntakeExecutionResult TryPublishAdmissionAndResolve(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(action);
        if (battle.NativeActions.ContainsPublishedOrRetired(action.ActionInstanceId))
        {
            return new DclCanonicalNativeAdmissionIntakeExecutionResult(
                new DclCanonicalNativeAdmittedActionPublishResult(
                    DclCanonicalNativeAdmittedActionPublishStatus.ActionAlreadyPublished,
                    action),
                new DclCanonicalNativePolicyTicketBridgeResult(
                    DclCanonicalNativePolicyTicketBridgeStatus.ActionAlreadyPublished,
                    Execution: null));
        }
        DclCanonicalNativeAdmittedActionPublishResult intake =
            battle.NativeAdmittedActions.TryPublishForPolicyTicket(battle, action);
        return new DclCanonicalNativeAdmissionIntakeExecutionResult(
            intake,
            TryResolvePublishAndRetirePolicyTicket(battle, action.ActionInstanceId));
    }

    public static DclCanonicalNativePolicyTicketIntakeExecutionResult TryPublishTicketAndResolve(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativePolicySourceTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(ticket);
        DclCanonicalNativePolicyTicketPublishResult intake =
            battle.NativePolicySources.TryPublishForRetainedAdmission(battle, ticket);
        if (intake.Status == DclCanonicalNativePolicyTicketPublishStatus.MissingAdmittedAction)
        {
            return new DclCanonicalNativePolicyTicketIntakeExecutionResult(
                intake,
                new DclCanonicalNativePolicyTicketBridgeResult(
                    DclCanonicalNativePolicyTicketBridgeStatus.MissingAdmittedAction,
                    Execution: null));
        }

        return new DclCanonicalNativePolicyTicketIntakeExecutionResult(
            intake,
            TryResolvePublishAndRetirePolicyTicket(battle, ticket.ActionInstanceId));
    }

    public static DclCanonicalNativePolicyTicketBridgeResult TryResolvePublishAndRetirePolicyTicket(
        DclCanonicalBattleRuntime battle,
        long actionInstanceId)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (!battle.NativeAdmittedActions.TryGet(actionInstanceId, out _))
            return new DclCanonicalNativePolicyTicketBridgeResult(
                DclCanonicalNativePolicyTicketBridgeStatus.MissingAdmittedAction,
                Execution: null);
        if (!battle.NativePolicySources.TryGet(actionInstanceId, out _))
            return new DclCanonicalNativePolicyTicketBridgeResult(
                DclCanonicalNativePolicyTicketBridgeStatus.MissingPolicyTicket,
                Execution: null);

        return new DclCanonicalNativePolicyTicketBridgeResult(
            DclCanonicalNativePolicyTicketBridgeStatus.Published,
            ResolvePublishAndRetirePolicyTicket(battle, actionInstanceId));
    }

    public static DclCanonicalNativeRetainedExecutionResult ResolvePublishAndRetirePolicyTicket(
        DclCanonicalBattleRuntime battle,
        long actionInstanceId)
    {
        ArgumentNullException.ThrowIfNull(battle);
        DclCanonicalNativePolicySourceTicket ticket = battle.NativePolicySources.Get(actionInstanceId);
        DclCanonicalNativeRetainedExecutionResult result = ResolvePublishAndRetire(
            battle,
            new DclCanonicalNativeRetainedPolicySourceExecutionRequest(
                ticket.ActionInstanceId,
                ticket.UnitPolicySources,
                ticket.FamilyPolicySource));
        DclCanonicalNativePolicySourceTicket retired = battle.NativePolicySources.Retire(actionInstanceId);
        if (!ReferenceEquals(retired, ticket))
            throw new InvalidOperationException("Retained native execution retired a different policy-source ticket.");
        return result;
    }

    public static DclCanonicalNativeRetainedExecutionResult ResolvePublishAndRetire(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeRetainedPolicySourceExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.UnitPolicySources);
        ArgumentNullException.ThrowIfNull(request.FamilyPolicySource);

        DclCanonicalNativeAdmittedAction action = battle.NativeAdmittedActions.Get(request.ActionInstanceId);
        object familyInputs = DclCanonicalNativeFamilyPolicyProvider.BuildForCapturedAction(
            battle,
            action,
            request.FamilyPolicySource);
        return ResolvePublishAndRetire(
            battle,
            new DclCanonicalNativeRetainedExecutionRequest(
                request.ActionInstanceId,
                request.UnitPolicySources,
                familyInputs));
    }

    public static DclCanonicalNativeRetainedExecutionResult ResolvePublishAndRetire(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeRetainedExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.UnitPolicySources);
        ArgumentNullException.ThrowIfNull(request.FamilyPolicyInputs);

        DclCanonicalNativeAdmittedAction action = battle.NativeAdmittedActions.Get(request.ActionInstanceId);
        IReadOnlyList<DclCanonicalNativeUnitPolicyInput> unitPolicies =
            DclCanonicalNativeUnitPolicyProvider.BuildForCapturedAction(action, request.UnitPolicySources);
        DclCanonicalNativeComposedExecution composed = DclCanonicalNativeConfirmedRequestComposer.ComposeCaptured(
            battle,
            action,
            unitPolicies,
            request.FamilyPolicyInputs);
        DclCanonicalConfirmedExecutionDispatchResult dispatch =
            DclCanonicalConfirmedExecutionDispatcher.ResolveAndPublish(battle, composed);
        if (dispatch.Application?.Plan.ActionInstanceId != action.ActionInstanceId)
            throw new InvalidOperationException("Retained native execution published under a different ActionInstance.");
        DclCanonicalNativeAdmittedAction retired = battle.NativeAdmittedActions.Retire(action.ActionInstanceId);
        if (!ReferenceEquals(retired, action))
            throw new InvalidOperationException("Retained native execution retired a different admitted action.");
        return new DclCanonicalNativeRetainedExecutionResult(action, composed, dispatch);
    }
}
