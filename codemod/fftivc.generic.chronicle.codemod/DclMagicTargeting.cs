namespace fftivc.generic.chronicle.codemod;

internal enum DclAllegianceRelation
{
    Self,
    Ally,
    Enemy,
}

internal sealed record DclTargetCandidate(
    DclUnitKey Unit,
    DclBattleTile Tile,
    int Height,
    DclAllegianceRelation Relation,
    DclEligibleTargetStates States,
    int CurrentHp,
    long CombatStateRevision,
    DclDefenseResourceSnapshot DefenseResources,
    bool ExplicitlyEligible = false);

internal sealed record DclCastDeclarationRequest(
    DclActionProfile Profile,
    DclUnitKey Caster,
    DclBattleTile CasterTile,
    int CasterHeight,
    DclTargetCandidate? UnitTarget,
    DclBattleTile? FixedTile,
    int? FixedTileHeight,
    bool Learned,
    bool SourceUsable,
    bool SourceSilenced,
    bool PrerequisitesMet,
    int CurrentMp,
    int CurrentHp,
    bool OvercastConfirmed,
    long CurrentGlobalCt,
    IReadOnlyList<int> CastCtModifiers,
    IReadOnlyList<DclRational>? MpCostMultipliers = null);

internal sealed record DclCastDeclarationAttempt(
    DclActionDeclaration? Declaration,
    DclCostCommitment CostCommitment,
    DclCastTiming Timing,
    IReadOnlyList<string> Failures)
{
    public bool Legal => Declaration is not null && CostCommitment.Legal && Failures.Count == 0;
}

internal readonly record struct DclResolvedCastCenter(
    DclBattleTile Tile,
    DclUnitKey? TrackedUnit,
    bool Available,
    string Reason);

internal static class DclMagicTargeting
{
    public static DclCastDeclarationAttempt Declare(
        DclCastDeclarationRequest request,
        long actionInstanceId)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actionInstanceId <= 0) throw new ArgumentOutOfRangeException(nameof(actionInstanceId));
        DclAuthoringValidation validation = DclAuthoringContract.Validate(request.Profile);
        if (!validation.IsValid)
            throw new ArgumentException($"The cast profile is not normalized: {string.Join("; ", validation.Findings)}", nameof(request));
        if (!request.Caster.IsValid) throw new ArgumentException("The caster requires a stable UnitKey.", nameof(request));
        if (request.CasterHeight < 0 || request.FixedTileHeight is < 0)
            throw new ArgumentOutOfRangeException(nameof(request));
        ArgumentNullException.ThrowIfNull(request.CastCtModifiers);
        DclRational[] additionalMpCostMultipliers = (request.MpCostMultipliers ?? []).ToArray();

        int finalMpCost = DclMagicResources.FinalMpCost(
            request.Profile.ResourceProfile.BaseMpCost,
            [request.Profile.ResourceProfile.MpCostMultiplier, .. additionalMpCostMultipliers]);
        DclCostCommitment commitment = DclMagicResources.DeclareCost(
            request.CurrentMp,
            request.CurrentHp,
            finalMpCost,
            request.Profile.ResourceProfile.OvercastPolicy,
            request.OvercastConfirmed);
        DclCastTiming timing = DclCastingTiming.Declare(
            request.Profile.TimingProfile.BaseCastCt,
            request.CastCtModifiers.Prepend(request.Profile.TimingProfile.CastCtModifier),
            request.CurrentGlobalCt);

        var failures = new List<string>();
        if (!request.Learned) failures.Add("action-not-learned");
        if (!request.SourceUsable) failures.Add("source-unusable");
        if (request.SourceSilenced && request.Profile.SourceProfile.Verbal) failures.Add("silence-blocks-verbal-action");
        if (!request.PrerequisitesMet) failures.Add("prerequisite-failed");
        if (!request.Profile.TimingProfile.ConsumesAction) failures.Add("cast-must-consume-action");
        if (!commitment.Legal) failures.Add(commitment.Reason);

        bool castingAction = request.Profile.TransactionProfile.CastingRollCardinality == DclCastingRollCardinality.PerAction;
        if (castingAction && request.Profile.TargetProfile.VisionRequired)
            failures.Add("magic-does-not-use-vision-or-line-of-sight");
        if (castingAction && request.Profile.TargetProfile.PhysicalRoute != DclPhysicalRoute.None)
            failures.Add("magic-does-not-use-a-physical-trajectory");

        DclUnitKey? trackedTarget = null;
        DclBattleTile? fixedTile = null;
        bool passedRange = false;
        bool passedVertical = false;
        switch (request.Profile.TargetProfile.TargetMode)
        {
            case DclTargetMode.Unit:
                if (request.UnitTarget is null)
                {
                    failures.Add("unit-target-required");
                    break;
                }
                ValidateUnitTarget(request, request.UnitTarget, failures, out passedRange, out passedVertical);
                trackedTarget = request.UnitTarget.Unit;
                break;
            case DclTargetMode.FixedTile:
                if (request.FixedTile is null || request.FixedTileHeight is null)
                {
                    failures.Add("fixed-tile-required");
                    break;
                }
                passedRange = IsInsideRange(
                    HorizontalDistance(request.CasterTile, request.FixedTile.Value),
                    request.Profile.TargetProfile.RangeMin,
                    request.Profile.TargetProfile.RangeMax);
                passedVertical = Math.Abs(request.CasterHeight - request.FixedTileHeight.Value) <=
                    request.Profile.TargetProfile.VerticalTolerance;
                if (!passedRange) failures.Add("outside-authored-range");
                if (!passedVertical) failures.Add("outside-vertical-tolerance");
                fixedTile = request.FixedTile;
                break;
            case DclTargetMode.Caster:
                passedRange = IsInsideRange(0, request.Profile.TargetProfile.RangeMin, request.Profile.TargetProfile.RangeMax);
                passedVertical = true;
                if (!passedRange) failures.Add("caster-outside-authored-range");
                break;
            default:
                failures.Add("unsupported-target-mode");
                break;
        }

        DclActionDeclaration? declaration = failures.Count == 0
            ? new DclActionDeclaration(
                actionInstanceId,
                request.Caster,
                request.Profile.ActionId,
                request.Profile.ProfileRevision,
                request.Profile.TargetProfile.TargetMode,
                trackedTarget,
                fixedTile,
                request.CasterTile,
                passedRange,
                passedVertical,
                finalMpCost,
                commitment.ApprovedHpCap,
                timing.CastCt,
                timing.DeclaredAtGlobalCt,
                timing.ResolvesAtGlobalCt)
            : null;
        return new DclCastDeclarationAttempt(declaration, commitment, timing, failures);
    }

    public static DclResolvedCastCenter ResolveCenter(
        DclActionDeclaration declaration,
        DclAreaCenterMode centerMode,
        IReadOnlyDictionary<DclUnitKey, DclTargetCandidate> currentUnits)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(currentUnits);
        return centerMode switch
        {
            DclAreaCenterMode.FixedTile when declaration.FixedTile is { } tile =>
                new DclResolvedCastCenter(tile, null, true, "fixed-tile"),
            DclAreaCenterMode.TrackedUnit when declaration.TrackedTarget is { } target && currentUnits.TryGetValue(target, out DclTargetCandidate? unit) =>
                new DclResolvedCastCenter(unit.Tile, target, true, "tracked-unit-current-tile"),
            DclAreaCenterMode.Caster when currentUnits.TryGetValue(declaration.Source, out DclTargetCandidate? caster) =>
                new DclResolvedCastCenter(caster.Tile, declaration.Source, true, "caster-current-tile"),
            DclAreaCenterMode.TrackedUnit => new DclResolvedCastCenter(default, declaration.TrackedTarget, false, "tracked-unit-unavailable"),
            DclAreaCenterMode.Caster => new DclResolvedCastCenter(default, declaration.Source, false, "caster-unavailable"),
            _ => new DclResolvedCastCenter(default, null, false, "center-mode-does-not-match-declaration"),
        };
    }

    public static DclTargetBatch SnapshotAreaTargets(
        int battleGeneration,
        IEnumerable<DclTargetCandidate> geometricMembers,
        DclAllegiancePolicy allegiancePolicy,
        DclEligibleTargetStates eligibleStates)
    {
        ArgumentNullException.ThrowIfNull(geometricMembers);
        if (allegiancePolicy == DclAllegiancePolicy.Unknown || eligibleStates == DclEligibleTargetStates.None)
            throw new ArgumentException("Area targeting requires explicit allegiance and eligible-state policies.");
        DclTargetResolutionSnapshot[] stableMembers = geometricMembers
            .Where(candidate => candidate.Unit.IsValid && candidate.Unit.BattleGeneration == battleGeneration)
            .Where(candidate => IsAllegianceEligible(allegiancePolicy, candidate.Relation, candidate.ExplicitlyEligible))
            .Where(candidate => (candidate.States & eligibleStates) != 0)
            .OrderBy(candidate => candidate.Unit.UnitSlot)
            .ThenBy(candidate => candidate.Unit.CharacterId)
            .Select(candidate => new DclTargetResolutionSnapshot(
                candidate.Unit,
                candidate.CurrentHp,
                candidate.CombatStateRevision,
                candidate.DefenseResources))
            .ToArray();
        return new DclTargetBatch(battleGeneration, stableMembers);
    }

    public static bool IsAllegianceEligible(
        DclAllegiancePolicy policy,
        DclAllegianceRelation relation,
        bool explicitlyEligible = false)
        => policy switch
        {
            DclAllegiancePolicy.Everyone => true,
            DclAllegiancePolicy.AlliesOnly => relation == DclAllegianceRelation.Ally,
            DclAllegiancePolicy.EnemiesOnly => relation == DclAllegianceRelation.Enemy,
            DclAllegiancePolicy.CasterSide => relation is DclAllegianceRelation.Self or DclAllegianceRelation.Ally,
            DclAllegiancePolicy.Explicit => explicitlyEligible,
            _ => false,
        };

    private static void ValidateUnitTarget(
        DclCastDeclarationRequest request,
        DclTargetCandidate target,
        List<string> failures,
        out bool passedRange,
        out bool passedVertical)
    {
        if (!target.Unit.IsValid || target.Unit.BattleGeneration != request.Caster.BattleGeneration)
            failures.Add("target-identity-invalid");
        if (!IsAllegianceEligible(request.Profile.TargetProfile.AllegiancePolicy, target.Relation, target.ExplicitlyEligible))
            failures.Add("target-allegiance-illegal");
        if ((target.States & request.Profile.TargetProfile.EligibleTargetStates) == 0)
            failures.Add("target-state-illegal");
        int distance = HorizontalDistance(request.CasterTile, target.Tile);
        passedRange = IsInsideRange(distance, request.Profile.TargetProfile.RangeMin, request.Profile.TargetProfile.RangeMax);
        passedVertical = Math.Abs(request.CasterHeight - target.Height) <= request.Profile.TargetProfile.VerticalTolerance;
        if (!passedRange) failures.Add("outside-authored-range");
        if (!passedVertical) failures.Add("outside-vertical-tolerance");
    }

    private static int HorizontalDistance(DclBattleTile first, DclBattleTile second)
        => checked(Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y));

    private static bool IsInsideRange(int distance, int minimum, int maximum)
        => distance >= minimum && distance <= maximum;
}
