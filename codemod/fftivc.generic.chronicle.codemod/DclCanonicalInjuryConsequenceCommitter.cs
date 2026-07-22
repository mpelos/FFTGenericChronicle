namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalInjuryStateCommitInput(
    DclStateRegistry StateRegistry,
    DclUnitKey Target,
    DclUnitKey? Source,
    int MaxHp,
    long AppliedBeforeTurnSerial,
    long FirstEligibleTargetTurnSerial,
    DclStateDefinition ShockDefinition,
    DclStateDefinition StunDefinition,
    DclStateDefinition KnockedDownDefinition);

internal sealed record DclCanonicalInjuryStateCommitResult(
    DclStateApplicationResult? Shock,
    DclStateApplicationResult? Stun,
    DclStateApplicationResult? KnockedDown,
    IReadOnlyList<DclStateInstance>? RemovedOnTargetKo = null)
{
    public bool CancelConcentrationPreparations => Stun is not null || KnockedDown is not null;
}

internal sealed record DclCanonicalInjuryStateCommitContext(
    DclStateRegistry StateRegistry,
    long AppliedBeforeTurnSerial,
    long FirstEligibleTargetTurnSerial)
{
    public static DclCanonicalInjuryStateCommitContext FromBattle(
        DclCanonicalBattleRuntime battle,
        DclUnitKey target)
    {
        ArgumentNullException.ThrowIfNull(battle);
        long currentTurn = battle.CurrentTurnSerial(target);
        return new DclCanonicalInjuryStateCommitContext(
            battle.States,
            currentTurn,
            checked(currentTurn + 1));
    }
}

internal static class DclCanonicalInjuryConsequenceCommitter
{
    public static void ValidateUniversal(DclCanonicalRuntimeCatalog runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        DclStateDefinition shock = ResolveUniversal(runtime, "shock");
        DclStateDefinition stun = ResolveUniversal(runtime, "stun");
        DclStateDefinition knockedDown = ResolveUniversal(runtime, "knocked-down");
        if (shock.StackPolicy != DclStateStackPolicy.Explicit ||
            shock.Duration.Clock != DclStateDurationClock.TargetTurn ||
            !shock.RemoveOnTargetKo)
            throw new InvalidOperationException(
                "Universal Shock must use the named Explicit stack handler and TargetTurn duration.");
        if (stun.Duration.Clock != DclStateDurationClock.ExplicitCommand ||
            knockedDown.Duration.Clock != DclStateDurationClock.ExplicitCommand ||
            !stun.RemoveOnTargetKo || !knockedDown.RemoveOnTargetKo)
            throw new InvalidOperationException(
                "Universal Stun and Knocked Down must use ExplicitCommand removal.");
    }

    public static void RequireSupportedTiming(DclActionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.MagnitudeProfile is DclDamageMagnitude &&
            profile.TransactionProfile.StrikeCount > 1 &&
            profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate &&
            profile.DeliveryProfile.Delivery is not (DclDelivery.PhysicalAttack or DclDelivery.Area))
            throw new InvalidOperationException(
                "This multi-Strike Immediate Injury delivery lacks between-Strike mechanical reprojection.");
    }

    public static DclCanonicalInjuryStateCommitResult Commit(
        DclCanonicalRuntimeCatalog runtime,
        DclInjuryResult injury,
        DclInjuryConsequenceResult consequences,
        DclUnitKey target,
        DclUnitKey? source,
        int maxHp,
        DclCanonicalInjuryStateCommitContext context)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(context);
        ValidateUniversal(runtime);
        return Commit(
            injury,
            consequences,
            new DclCanonicalInjuryStateCommitInput(
                context.StateRegistry,
                target,
                source,
                maxHp,
                context.AppliedBeforeTurnSerial,
                context.FirstEligibleTargetTurnSerial,
                ResolveUniversal(runtime, "shock"),
                ResolveUniversal(runtime, "stun"),
                ResolveUniversal(runtime, "knocked-down")));
    }

    public static DclCanonicalInjuryStateCommitResult Commit(
        DclInjuryResult injury,
        DclInjuryConsequenceResult consequences,
        DclCanonicalInjuryStateCommitInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.StateRegistry);
        if (!input.Target.IsValid || input.Target.BattleGeneration != input.StateRegistry.BattleGeneration)
            throw new ArgumentException("Injury consequence target and state registry must share one battle generation.", nameof(input));
        if (input.Source is { } source && (!source.IsValid || source.BattleGeneration != input.Target.BattleGeneration))
            throw new ArgumentException("Injury consequence source must belong to the target battle generation.", nameof(input));
        if (input.MaxHp < 1 || input.AppliedBeforeTurnSerial < 0 || input.FirstEligibleTargetTurnSerial < 0)
            throw new ArgumentOutOfRangeException(nameof(input));
        if (consequences.MajorWound.RemainingHp == 0)
        {
            if (consequences.ApplyStun || consequences.ApplyKnockedDown)
                throw new ArgumentException("Native KO cannot also materialize Major-Wound posture states.", nameof(consequences));
            DclStateInstance[] removed = input.StateRegistry.CaptureTarget(input.Target).Instances
                .Where(instance => instance.Definition.RemoveOnTargetKo)
                .OrderBy(instance => instance.InstanceId)
                .ToArray();
            long[] removedIds = input.StateRegistry.OnTargetKo(input.Target).Order().ToArray();
            if (!removed.Select(instance => instance.InstanceId).SequenceEqual(removedIds))
                throw new InvalidOperationException("Target-KO cleanup removed a different persistent-state set than its snapshot.");
            return new DclCanonicalInjuryStateCommitResult(null, null, null, removed);
        }
        if (consequences.ApplyStun != consequences.ApplyKnockedDown)
            throw new ArgumentException("A failed physical Major Wound must apply Stun and Knocked Down together.", nameof(consequences));

        DclStateApplicationResult? shock = null;
        if (injury.Injury > 0)
        {
            DclStateDefinition definition = input.ShockDefinition;
            shock = input.StateRegistry.ApplyShock(new DclStateApplication(
                definition,
                input.Target,
                definition.SourceRequired ? input.Source : null,
                input.StateRegistry.CurrentGlobalCt,
                input.AppliedBeforeTurnSerial,
                input.FirstEligibleTargetTurnSerial,
                FirstEligibleSourceTurnSerial: null,
                DurationUnits: 1,
                Strength: null,
                WinningMargin: null,
                StackDiscriminator: "injury-window",
                ContributionIdentity: null,
                new DclShockStatePayload(definition.PayloadSchema, injury.Injury),
                definition.PresentationProfile), input.MaxHp);
        }

        DclStateApplicationResult? stun = null;
        DclStateApplicationResult? knockedDown = null;
        if (consequences.ApplyStun)
        {
            stun = ApplyPosture(input.StunDefinition, input, "major-wound");
            knockedDown = ApplyPosture(input.KnockedDownDefinition, input, "major-wound");
        }
        return new DclCanonicalInjuryStateCommitResult(shock, stun, knockedDown, []);
    }

    private static DclStateApplicationResult ApplyPosture(
        DclStateDefinition definition,
        DclCanonicalInjuryStateCommitInput input,
        string discriminator)
    {
        if (definition.Duration.Clock != DclStateDurationClock.ExplicitCommand)
            throw new ArgumentException($"Universal posture state '{definition.Kind}' must use ExplicitCommand removal.", nameof(input));
        return input.StateRegistry.Apply(new DclStateApplication(
            definition,
            input.Target,
            definition.SourceRequired ? input.Source : null,
            input.StateRegistry.CurrentGlobalCt,
            input.AppliedBeforeTurnSerial,
            FirstEligibleTargetTurnSerial: null,
            FirstEligibleSourceTurnSerial: null,
            DurationUnits: null,
            Strength: null,
            WinningMargin: null,
            StackDiscriminator: discriminator,
            ContributionIdentity: null,
            new DclPropertyStatePayload(definition.PayloadSchema, new Dictionary<string, string>()),
            definition.PresentationProfile));
    }

    private static DclStateDefinition ResolveUniversal(
        DclCanonicalRuntimeCatalog runtime,
        string kind)
        => runtime.Authoring.States.TryGetValue(kind, out DclStateDefinition? definition)
            ? definition
            : throw new InvalidOperationException(
                $"Canonical Injury execution requires the universal state definition '{kind}'.");
}
