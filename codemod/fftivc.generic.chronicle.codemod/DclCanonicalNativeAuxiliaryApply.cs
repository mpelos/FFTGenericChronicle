namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeStatusPacketMutation(
    DclNativeStatusBit Bit,
    bool Add,
    string StateKind);

/// <summary>
/// Separates effects that must cross the native result packet from effects whose mechanical
/// authority already belongs to the battle-scoped DCL registry. Exact CT credit remains rational
/// here; a native adapter must not invent an integer rounding boundary while staging it.
/// </summary>
internal sealed record DclCanonicalNativeAuxiliaryApplyPlan(
    IReadOnlyList<DclCanonicalNativeStatusPacketMutation> StatusPacketMutations,
    IReadOnlyList<DclNativeStatusBit> NativeOwnedBits,
    IReadOnlyList<DclCanonicalNativeStateMutationProjection> CustomAddedStates,
    IReadOnlyList<DclCanonicalNativeStateMutationProjection> CustomRemovedStates,
    DclRational? CtCredit,
    bool ClearKoAfterPositiveHpCredit,
    DclCanonicalForcedMovementResult? ForcedMovement)
{
    public static DclCanonicalNativeAuxiliaryApplyPlan Empty { get; } = new(
        [], [], [], [], CtCredit: null, ClearKoAfterPositiveHpCredit: false, ForcedMovement: null);

    public bool HasStatusPacketMutation => StatusPacketMutations.Count > 0;
    public bool HasCustomStateMutation => CustomAddedStates.Count > 0 || CustomRemovedStates.Count > 0;
    public bool RequiresNativeAuxiliaryCarrier => HasStatusPacketMutation || CtCredit is not null || ForcedMovement is not null;
}

internal static class DclCanonicalNativeAuxiliaryApplyPlanner
{
    public static DclCanonicalNativeAuxiliaryApplyPlan Build(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalNativeStrikeProjection strike)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(strike);
        if (strike.AuxiliaryEffects is not { } auxiliary)
            return DclCanonicalNativeAuxiliaryApplyPlan.Empty;

        ValidateMutations(auxiliary);
        if (auxiliary.ClearKoAfterPositiveHpCredit && strike.Channels.HpCredit <= 0)
            throw new ArgumentException(
                "Canonical KO clear requires a positive HP credit in the same native target carrier.",
                nameof(strike));
        var nativeAdds = new Dictionary<DclNativeStatusBit, string>();
        var nativeRemoves = new Dictionary<DclNativeStatusBit, string>();
        var customAdds = new List<DclCanonicalNativeStateMutationProjection>();
        var customRemoves = new List<DclCanonicalNativeStateMutationProjection>();

        foreach (DclCanonicalNativeStateMutationProjection mutation in auxiliary.AddedStates)
            Classify(runtime, mutation, nativeAdds, customAdds);
        foreach (DclCanonicalNativeStateMutationProjection mutation in auxiliary.RemovedStates)
            Classify(runtime, mutation, nativeRemoves, customRemoves);

        // Replacement/refresh of one native overlay can remove an old registry instance and add
        // its successor in the same outer action. The final native truth is present, so Add wins.
        foreach (DclNativeStatusBit bit in nativeAdds.Keys)
            nativeRemoves.Remove(bit);

        DclCanonicalNativeStatusPacketMutation[] packetMutations = nativeRemoves
            .Select(pair => new DclCanonicalNativeStatusPacketMutation(pair.Key, Add: false, pair.Value))
            .Concat(nativeAdds.Select(pair =>
                new DclCanonicalNativeStatusPacketMutation(pair.Key, Add: true, pair.Value)))
            .OrderBy(mutation => mutation.Bit.ByteIndex)
            .ThenBy(mutation => mutation.Bit.Mask)
            .ToArray();
        DclNativeStatusBit[] ownedBits = packetMutations
            .Select(mutation => mutation.Bit)
            .Distinct()
            .OrderBy(bit => bit.ByteIndex)
            .ThenBy(bit => bit.Mask)
            .ToArray();

        return new DclCanonicalNativeAuxiliaryApplyPlan(
            packetMutations,
            ownedBits,
            customAdds.OrderBy(mutation => mutation.InstanceId).ToArray(),
            customRemoves.OrderBy(mutation => mutation.InstanceId).ToArray(),
            auxiliary.CtCredit,
            auxiliary.ClearKoAfterPositiveHpCredit,
            auxiliary.ForcedMovement);
    }

    public static DclStatusPacketPlan ComposeStatusPacket(
        DclCanonicalNativeAuxiliaryApplyPlan plan,
        ReadOnlySpan<byte> oldAdd,
        ReadOnlySpan<byte> oldRemove,
        byte oldResultFlags)
    {
        ArgumentNullException.ThrowIfNull(plan);
        DclStatusWrite[] writes = plan.StatusPacketMutations.Select(mutation => new DclStatusWrite(
            $"canonical:{mutation.StateKind}",
            mutation.Bit.ByteIndex,
            mutation.Bit.Mask,
            mutation.Add,
            Resistance: 0,
            Roll: -1,
            Resisted: false,
            Immune: false,
            DurationTargetTurns: 0)).ToArray();
        return DclStatusPacket.Compose(oldAdd, oldRemove, oldResultFlags, writes, plan.NativeOwnedBits);
    }

    public static byte EncodePositiveCtDelta(DclRational credit)
    {
        if (!credit.Denominator.IsOne || credit.Numerator < 1 || credit.Numerator > 127)
            throw new InvalidOperationException(
                $"Canonical CT credit {credit} cannot be represented exactly by the native positive sign-magnitude carrier.");
        return (byte)(0x80 | (byte)credit.Numerator);
    }

    private static void Classify(
        DclCanonicalRuntimeCatalog runtime,
        DclCanonicalNativeStateMutationProjection mutation,
        IDictionary<DclNativeStatusBit, string> native,
        ICollection<DclCanonicalNativeStateMutationProjection> custom)
    {
        if (!runtime.Authoring.States.TryGetValue(mutation.StateKind, out DclStateDefinition? definition))
            throw new InvalidOperationException(
                $"Canonical native projection references unloaded state '{mutation.StateKind}'.");
        if (definition.NativeStatusBit is not { } bit)
        {
            custom.Add(mutation);
            return;
        }
        if (native.TryGetValue(bit, out string? existingKind) &&
            !StringComparer.Ordinal.Equals(existingKind, mutation.StateKind))
            throw new InvalidOperationException(
                $"Native status bit {bit.ByteIndex}:0x{bit.Mask:X2} has multiple semantic owners in one apply plan.");
        native[bit] = mutation.StateKind;
    }

    private static void ValidateMutations(DclCanonicalNativeAuxiliaryEffects auxiliary)
    {
        DclCanonicalNativeStateMutationProjection[] all = auxiliary.AddedStates
            .Concat(auxiliary.RemovedStates)
            .ToArray();
        if (all.Any(mutation => mutation.InstanceId <= 0 || string.IsNullOrWhiteSpace(mutation.StateKind)))
            throw new ArgumentException("Canonical auxiliary state mutations require stable identities.", nameof(auxiliary));
        if (auxiliary.AddedStates.GroupBy(mutation => mutation.InstanceId).Any(group => group.Count() > 1) ||
            auxiliary.RemovedStates.GroupBy(mutation => mutation.InstanceId).Any(group => group.Count() > 1))
            throw new ArgumentException("One auxiliary apply plan cannot duplicate a state-instance mutation.", nameof(auxiliary));
        foreach (IGrouping<long, DclCanonicalNativeStateMutationProjection> group in all.GroupBy(mutation => mutation.InstanceId))
            if (group.Select(mutation => mutation.StateKind).Distinct(StringComparer.Ordinal).Count() > 1)
                throw new ArgumentException("One state instance cannot change semantic kind during native apply.", nameof(auxiliary));
        if (auxiliary.CtCredit is { } ct && ct <= DclRational.FromInteger(0))
            throw new ArgumentException("Canonical CT credit must remain exact and positive.", nameof(auxiliary));
        if (auxiliary.ForcedMovement is { } movement &&
            (movement.SuppressedByTargetKo || movement.MovedTiles <= 0 || movement.Destination == movement.Origin ||
             !movement.CancelAim || !movement.CreatesConcentrationIncident))
            throw new ArgumentException(
                "A native forced-movement carrier requires one positive settled displacement with its lifecycle consequences.",
                nameof(auxiliary));
    }
}

internal static class DclCanonicalNativeForcedMovementWriter
{
    public static void Apply(
        DclCanonicalNativeAuxiliaryApplyPlan plan,
        Func<DclBattleTile> readTargetTile,
        Action<DclCanonicalForcedMovementResult> executeNativeMovement)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(readTargetTile);
        ArgumentNullException.ThrowIfNull(executeNativeMovement);
        if (plan.ForcedMovement is not { } movement) return;
        if (readTargetTile() != movement.Origin)
            throw new InvalidOperationException("Target tile changed before canonical forced movement applied.");
        executeNativeMovement(movement);
        if (readTargetTile() != movement.Destination)
            throw new InvalidOperationException("Native forced-movement readback disagrees with the shared map verdict.");
    }
}
