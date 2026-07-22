namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeApplyReservation(
    long ActionInstanceId,
    DclUnitKey Source,
    DclUnitKey Target,
    int StrikeIndex,
    string ApplyBoundary,
    string PresentationBoundary,
    DclCanonicalNativeStrikeProjection Strike,
    DclComputePointNumericPlan NumericPlan,
    DclCanonicalNativeAuxiliaryApplyPlan AuxiliaryPlan,
    DclCanonicalNativeActionApplication Application);

internal sealed record DclCanonicalNativePaymentReservation(
    long ActionInstanceId,
    DclCanonicalNativePaymentProjection? Payment,
    bool ReactionWindowOpened,
    DclCanonicalNativeActionApplication Application);

internal sealed record DclCanonicalNativeSourceEffectReservation(
    long ActionInstanceId,
    DclCanonicalNativeSourceEffectProjection SourceEffect,
    DclCanonicalNativeActionApplication Application);

internal static class DclCanonicalNativeApplyRouter
{
    public static bool TryPrepare(
        DclCanonicalBattleRuntime battle,
        DclUnitKey source,
        int abilityId,
        DclUnitKey target,
        int strikeIndex,
        int naturalHpDebit,
        int naturalHpCredit,
        int naturalMpDebit,
        int naturalMpCredit,
        byte naturalResultFlags,
        bool controlResultFlags,
        int preserveResultFlagsMask,
        out DclCanonicalNativeApplyReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(battle);
        reservation = null!;
        if (!battle.NativeActions.TryFindPendingApply(source, abilityId, target, strikeIndex, out var application))
            return false;
        DclCanonicalNativeStrikeProjection strike = application.PeekNextStrike();
        DclComputePointNumericPlan numericPlan = DclCanonicalNativeCarrierProjector.BuildStrikeNumericPlan(
            strike,
            naturalHpDebit,
            naturalHpCredit,
            naturalMpDebit,
            naturalMpCredit,
            naturalResultFlags,
            controlResultFlags,
            preserveResultFlagsMask);
        DclCanonicalNativeAuxiliaryApplyPlan auxiliaryPlan =
            DclCanonicalNativeAuxiliaryApplyPlanner.Build(battle.Catalog, strike);
        reservation = new DclCanonicalNativeApplyReservation(
            application.Plan.ActionInstanceId,
            source,
            target,
            strikeIndex,
            application.Plan.ApplyBoundary,
            application.Plan.PresentationBoundary,
            strike,
            numericPlan,
            auxiliaryPlan,
            application);
        return true;
    }

    public static DclCanonicalNativeStrikeProjection Commit(DclCanonicalNativeApplyReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        DclCanonicalNativeStrikeProjection pending = reservation.Application.PeekNextStrike();
        if (pending != reservation.Strike)
            throw new InvalidOperationException("The canonical apply reservation became stale before native commit.");
        return reservation.Application.ApplyNextStrike(reservation.Target, reservation.StrikeIndex);
    }

    public static DclCanonicalNativeStrikeProjection ReadPresentation(
        DclCanonicalNativeApplyReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        return reservation.Application.ReadAppliedForPresentation(reservation.Target, reservation.StrikeIndex);
    }

    public static bool TryPreparePayment(
        DclCanonicalBattleRuntime battle,
        long actionInstanceId,
        out DclCanonicalNativePaymentReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(battle);
        reservation = null!;
        DclCanonicalNativeActionApplication application;
        try
        {
            application = battle.NativeActions.Get(actionInstanceId);
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        DclCanonicalNativeActionStage required = application.Plan.SourceEffect is null
            ? DclCanonicalNativeActionStage.StrikesApplied
            : DclCanonicalNativeActionStage.SourceEffectCommitted;
        if (application.Stage != required ||
            application.Plan.ResourceFailed)
            return false;
        reservation = new DclCanonicalNativePaymentReservation(
            actionInstanceId,
            application.Plan.ResourcePayment,
            application.Plan.ReactionWindowOpened,
            application);
        return true;
    }

    public static bool TryPrepareSourceEffect(
        DclCanonicalBattleRuntime battle,
        long actionInstanceId,
        out DclCanonicalNativeSourceEffectReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(battle);
        reservation = null!;
        DclCanonicalNativeActionApplication application;
        try
        {
            application = battle.NativeActions.Get(actionInstanceId);
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        if (application.Stage != DclCanonicalNativeActionStage.StrikesApplied ||
            application.Plan.ResourceFailed || application.Plan.SourceEffect is not { } sourceEffect)
            return false;
        reservation = new DclCanonicalNativeSourceEffectReservation(
            actionInstanceId,
            sourceEffect,
            application);
        return true;
    }

    public static DclCanonicalNativeSourceEffectApplyPlan PlanSourceEffect(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeSourceEffectReservation reservation,
        DclCanonicalNativePoolSnapshot pools)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(reservation);
        return DclCanonicalNativeSourceEffectApplyPlanner.Build(
            reservation.Application.Plan.Source,
            pools,
            reservation.SourceEffect,
            battle.States);
    }

    public static DclCanonicalNativeSourceEffectCommitResult CommitSourceEffect(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeSourceEffectReservation reservation,
        DclCanonicalNativeSourceEffectApplyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(plan);
        if (reservation.Application.Plan.ActionInstanceId != reservation.ActionInstanceId ||
            reservation.Application.Plan.SourceEffect != reservation.SourceEffect ||
            plan.Source != reservation.Application.Plan.Source || plan.Projection != reservation.SourceEffect ||
            reservation.Application.Stage != DclCanonicalNativeActionStage.StrikesApplied)
            throw new InvalidOperationException("Canonical source-effect plan became stale before managed commit.");
        DclCanonicalNativeSourceEffectApplyPlanner.ValidateKoCleanup(plan, battle.States);
        DclCanonicalNativeSourceEffectProjection effect = reservation.Application.CommitSourceEffect()!;
        IReadOnlyList<DclStateInstance> removed =
            DclCanonicalNativeSourceEffectApplyPlanner.CommitKoCleanup(plan, battle.States);
        return new DclCanonicalNativeSourceEffectCommitResult(effect, removed);
    }

    public static DclCanonicalNativePaymentProjection? CommitPayment(
        DclCanonicalNativePaymentReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        if (reservation.Application.Plan.ActionInstanceId != reservation.ActionInstanceId ||
            reservation.Application.Plan.ResourcePayment != reservation.Payment)
            throw new InvalidOperationException("The canonical resource-payment reservation became stale before native commit.");
        return reservation.Application.CommitResourcePayment();
    }

    public static DclCanonicalNativePaymentApplyPlan PlanPayment(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativePaymentReservation reservation,
        DclUnitKey expectedPayer,
        DclCanonicalNativePoolSnapshot pools)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(reservation);
        if (battle.BattleGeneration != expectedPayer.BattleGeneration ||
            reservation.Application.Plan.Source != expectedPayer)
            throw new InvalidOperationException("Payment reservation source and native payer identity disagree.");
        return DclCanonicalNativePaymentApplyPlanner.Build(
            expectedPayer,
            pools,
            reservation.Payment,
            battle.States);
    }

    public static DclCanonicalNativePaymentProjection? CommitPayment(
        DclCanonicalNativePaymentReservation reservation,
        DclCanonicalNativePaymentApplyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Payer != reservation.Application.Plan.Source || plan.Projection != reservation.Payment)
            throw new InvalidOperationException("Canonical native payment plan became stale before commit.");
        return CommitPayment(reservation);
    }

    public static DclCanonicalNativePaymentCommitResult CommitPayment(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativePaymentReservation reservation,
        DclCanonicalNativePaymentApplyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(plan);
        if (battle.BattleGeneration != plan.Payer.BattleGeneration ||
            plan.Payer != reservation.Application.Plan.Source ||
            plan.Projection != reservation.Payment ||
            reservation.Application.Stage != (reservation.Application.Plan.SourceEffect is null
                ? DclCanonicalNativeActionStage.StrikesApplied
                : DclCanonicalNativeActionStage.SourceEffectCommitted))
            throw new InvalidOperationException("Canonical native payment plan became stale before managed commit.");

        // The native HP/MP writer validates and applies plan.Before -> plan.After before entering
        // this managed commit. Prevalidate the exact KO cleanup set first so the following two
        // deterministic mutations cannot accept a hybrid payment/state revision.
        DclCanonicalNativePaymentApplyPlanner.ValidatePayerKoCleanup(plan, battle.States);
        DclCanonicalNativePaymentProjection? payment = reservation.Application.CommitResourcePayment();
        IReadOnlyList<DclStateInstance> removed =
            DclCanonicalNativePaymentApplyPlanner.CommitPayerKoCleanup(plan, battle.States);
        return new DclCanonicalNativePaymentCommitResult(payment, removed);
    }

    public static void AcknowledgeReactionWindow(DclCanonicalNativePaymentReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        if (!reservation.ReactionWindowOpened)
            throw new InvalidOperationException("The canonical ActionInstance did not declare a Reaction window.");
        reservation.Application.AcknowledgeReactionWindow();
    }

    public static DclCanonicalNativeReactionEffectAcknowledgement AcknowledgeReactionEffect(
        DclCanonicalNativePaymentReservation reservation,
        DclUnitKey reactor,
        int nativeReactionAbilityId,
        int nativeEffectAbilityId)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        if (!reservation.ReactionWindowOpened)
            throw new InvalidOperationException("The canonical ActionInstance did not declare a Reaction window.");
        return reservation.Application.AcknowledgeReactionEffect(
            reactor,
            nativeReactionAbilityId,
            nativeEffectAbilityId);
    }

    public static void SettleAndRetire(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativePaymentReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(reservation);
        if (battle.BattleGeneration != reservation.Application.Plan.Source.BattleGeneration)
            throw new InvalidOperationException("A native settlement reservation cannot cross battle generations.");
        reservation.Application.Settle();
        battle.NativeActions.Retire(reservation.ActionInstanceId);
    }

    public static void SettleResourceFailure(
        DclCanonicalBattleRuntime battle,
        long actionInstanceId)
    {
        ArgumentNullException.ThrowIfNull(battle);
        DclCanonicalNativeActionApplication application = battle.NativeActions.Get(actionInstanceId);
        if (!application.Plan.ResourceFailed)
            throw new InvalidOperationException("Only a canonical ResourceFailure can settle without target or payment carriers.");
        application.Settle();
        battle.NativeActions.Retire(actionInstanceId);
    }
}
