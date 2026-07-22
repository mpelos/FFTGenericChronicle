namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeMovementVerdict(
    DclUnitKey Target,
    DclBattleTile Origin,
    DclBattleTile Destination,
    int RequestedTiles,
    int MovedTiles,
    DclForcedMovementDirection Direction,
    bool Fell,
    bool NativePathResolved);

internal sealed record DclCanonicalForcedMovementResult(
    DclUnitKey Target,
    DclBattleTile Origin,
    DclBattleTile Destination,
    int RequestedTiles,
    int MovedTiles,
    DclForcedMovementDirection Direction,
    bool Fell,
    bool CancelAim,
    bool CreatesConcentrationIncident,
    bool SuppressedByTargetKo);

internal sealed record DclCanonicalInjuryMovementBranchSet(
    DclForcedMovementDirection Direction,
    IReadOnlyDictionary<int, DclCanonicalNativeMovementVerdict> VerdictByRequestedTiles)
{
    public DclBattleTile Origin => VerdictByRequestedTiles.Count == 0
        ? throw new InvalidOperationException("An Injury movement branch set cannot be empty.")
        : VerdictByRequestedTiles.First().Value.Origin;

    public void Validate(DclUnitKey target)
    {
        ArgumentNullException.ThrowIfNull(VerdictByRequestedTiles);
        DclBattleTile origin = Origin;
        if (VerdictByRequestedTiles.Count == 0 || VerdictByRequestedTiles.Any(pair =>
                pair.Key <= 0 || pair.Value is null || pair.Value.RequestedTiles != pair.Key ||
                pair.Value.Target != target || pair.Value.Origin != origin || pair.Value.Direction != Direction))
            throw new ArgumentException(
                "Every Injury movement branch must freeze one matching positive-distance native verdict.",
                nameof(VerdictByRequestedTiles));
    }

    public DclCanonicalForcedMovementResult? Resolve(
        DclUnitKey target,
        bool targetKo,
        int requestedTiles)
    {
        Validate(target);
        DclBattleTile origin = Origin;
        if (requestedTiles < 0) throw new ArgumentOutOfRangeException(nameof(requestedTiles));
        if (requestedTiles == 0) return null;
        DclCanonicalNativeMovementVerdict? verdict = targetKo
            ? null
            : VerdictByRequestedTiles.TryGetValue(requestedTiles, out DclCanonicalNativeMovementVerdict? frozen)
                ? frozen
                : throw new InvalidOperationException(
                    $"Injury displacement selected {requestedTiles} tiles without its frozen map verdict.");
        return DclCanonicalForcedMovement.Resolve(
            new DclForcedMovementProfile(requestedTiles, Direction),
            target,
            origin,
            targetKo,
            verdict);
    }
}

internal sealed record DclCanonicalInjuryMovementBranchForest(
    IReadOnlyDictionary<DclBattleTile, DclCanonicalInjuryMovementBranchSet> BranchesByOrigin)
{
    public void Validate(DclUnitKey target)
    {
        ArgumentNullException.ThrowIfNull(BranchesByOrigin);
        if (BranchesByOrigin.Count == 0)
            throw new ArgumentException("An Injury movement branch forest cannot be empty.", nameof(BranchesByOrigin));
        DclForcedMovementDirection? direction = null;
        foreach ((DclBattleTile origin, DclCanonicalInjuryMovementBranchSet branches) in BranchesByOrigin)
        {
            if (branches is null || branches.Origin != origin)
                throw new ArgumentException(
                    "Every Injury movement forest entry must be keyed by its exact origin tile.",
                    nameof(BranchesByOrigin));
            branches.Validate(target);
            direction ??= branches.Direction;
            if (branches.Direction != direction)
                throw new ArgumentException(
                    "One Injury movement branch forest cannot change direction between origins.",
                    nameof(BranchesByOrigin));
        }
    }

    public DclCanonicalForcedMovementResult? Resolve(
        DclUnitKey target,
        DclBattleTile origin,
        bool targetKo,
        int requestedTiles)
    {
        Validate(target);
        if (requestedTiles < 0) throw new ArgumentOutOfRangeException(nameof(requestedTiles));
        if (requestedTiles == 0) return null;
        DclCanonicalInjuryMovementBranchSet branches = BranchesByOrigin.TryGetValue(origin, out var selected)
            ? selected
            : throw new InvalidOperationException(
                $"Injury displacement selected origin {origin} without its frozen map branch forest entry.");
        return branches.Resolve(target, targetKo, requestedTiles);
    }

    public static DclCanonicalInjuryMovementBranchForest FromSingle(
        DclCanonicalInjuryMovementBranchSet branches)
    {
        ArgumentNullException.ThrowIfNull(branches);
        return new DclCanonicalInjuryMovementBranchForest(
            new Dictionary<DclBattleTile, DclCanonicalInjuryMovementBranchSet>
            {
                [branches.Origin] = branches,
            });
    }
}

/// <summary>
/// Validates one already-resolved native map displacement against normalized DCL authoring. The
/// native map remains the owner of tile/height/edge/fall legality; forecast, AI, and execution must
/// consume the same immutable verdict. No intermediate tile opens a Reaction or stop-hit window.
/// </summary>
internal static class DclCanonicalForcedMovement
{
    public static DclCanonicalForcedMovementResult Resolve(
        DclActionProfile profile,
        DclUnitKey target,
        DclBattleTile currentTile,
        bool targetKo,
        DclCanonicalNativeMovementVerdict? nativeVerdict)
    {
        ArgumentNullException.ThrowIfNull(profile);
        DclEffectProfile[] effects = profile.Effects
            .Where(effect => effect.Kind == DclEffectKind.ForcedMovement)
            .ToArray();
        if (profile.ForcedMovementProfile is not { } movement || effects.Length != 1)
            throw new InvalidOperationException("ForcedMovement requires one normalized effect and one movement profile.");
        return Resolve(movement, target, currentTile, targetKo, nativeVerdict);
    }

    public static DclCanonicalForcedMovementResult Resolve(
        DclForcedMovementProfile movement,
        DclUnitKey target,
        DclBattleTile currentTile,
        bool targetKo,
        DclCanonicalNativeMovementVerdict? nativeVerdict)
    {
        if (!target.IsValid)
            throw new ArgumentException("ForcedMovement requires a stable target identity.", nameof(target));
        if (targetKo)
        {
            if (nativeVerdict is not null)
                throw new ArgumentException("A KO-short-circuited target cannot request native displacement.", nameof(nativeVerdict));
            return new DclCanonicalForcedMovementResult(
                target,
                currentTile,
                currentTile,
                movement.DistanceTiles,
                MovedTiles: 0,
                movement.Direction,
                Fell: false,
                CancelAim: false,
                CreatesConcentrationIncident: false,
                SuppressedByTargetKo: true);
        }
        DclCanonicalNativeMovementVerdict verdict = nativeVerdict ??
            throw new ArgumentNullException(nameof(nativeVerdict), "A live target requires the shared native map verdict.");
        if (!verdict.NativePathResolved || verdict.Target != target || verdict.Origin != currentTile ||
            verdict.RequestedTiles != movement.DistanceTiles || verdict.Direction != movement.Direction ||
            verdict.MovedTiles < 0 || verdict.MovedTiles > verdict.RequestedTiles ||
            (verdict.MovedTiles == 0) != (verdict.Destination == verdict.Origin) ||
            verdict.MovedTiles > 0 && verdict.Destination == verdict.Origin)
            throw new ArgumentException("Native forced-movement verdict does not match the normalized action/target snapshot.", nameof(nativeVerdict));
        bool moved = verdict.MovedTiles > 0;
        return new DclCanonicalForcedMovementResult(
            target,
            verdict.Origin,
            verdict.Destination,
            verdict.RequestedTiles,
            verdict.MovedTiles,
            verdict.Direction,
            verdict.Fell,
            CancelAim: moved,
            CreatesConcentrationIncident: moved,
            SuppressedByTargetKo: false);
    }
}
