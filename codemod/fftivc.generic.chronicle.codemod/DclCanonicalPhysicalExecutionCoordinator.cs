namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalPhysicalExecutionRequest(
    int AbilityId,
    DclActionDeclaration Declaration,
    DclCanonicalEquipmentSnapshot SourceEquipment,
    int WeaponItemId,
    string WeaponResourceKey,
    int SourceSt,
    IReadOnlyList<DclTargetResolutionSnapshot> Targets,
    IReadOnlyList<DclCanonicalPhysicalTargetContext> TargetContexts,
    IReadOnlyList<DclCanonicalPhysicalStrikeInput> Strikes,
    IReadOnlyList<DclCanonicalReactionCandidate>? ReactionCandidates = null,
    bool IsUniversalNormalAttack = false,
    IReadOnlyList<DclCanonicalPhysicalStrikeWeapon>? StrikeWeapons = null,
    DclCanonicalProtectionRedirectPlan? ProtectionRedirect = null);

internal sealed record DclCanonicalPublishedPhysicalExecution(
    DclCanonicalPhysicalExecutionResult Result,
    DclCanonicalNativeActionApplication Application);

internal static class DclCanonicalPhysicalExecutionCoordinator
{
    public static DclCanonicalPublishedPhysicalExecution ResolveAndPublish(
        DclCanonicalBattleRuntime battle,
        DclCanonicalPhysicalExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SourceEquipment);
        if (request.Declaration.Source.BattleGeneration != battle.BattleGeneration ||
            request.Declaration.ActionInstanceId <= 0 || string.IsNullOrWhiteSpace(request.WeaponResourceKey))
            throw new ArgumentException("Physical execution request does not belong to this battle.", nameof(request));
        (DclAbilityBinding binding, DclActionProfile profile) = battle.Catalog.ResolveAbility(request.AbilityId);
        if (request.Declaration.ActionId != profile.ActionId ||
            request.Declaration.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Physical execution request lost its normalized action revision.", nameof(request));
        profile = DclCanonicalPhysicalStrikeCardinality.ResolveEffectiveProfile(
            binding,
            profile,
            request.Targets,
            request.Strikes);
        DclCanonicalInjuryConsequenceCommitter.ValidateUniversal(battle.Catalog);
        DclCanonicalPhysicalStrikeWeapon[] strikeWeapons = DclCanonicalPhysicalStrikeWeapons.Normalize(
            profile.TransactionProfile.StrikeCount,
            request.WeaponItemId,
            request.WeaponResourceKey,
            request.StrikeWeapons);
        IReadOnlyDictionary<int, DclWeaponMetadata> weapons = DclCanonicalPhysicalStrikeWeapons.ResolveMetadata(
            battle.Catalog,
            strikeWeapons);
        DclCanonicalPhysicalStrikeWeapons.RequireEquipped(request.SourceEquipment, strikeWeapons);
        foreach (DclCanonicalPhysicalStrikeInput strike in request.Strikes)
        {
            if (strike.AttackRoll is not null || strike.DefenseRoll is not null || strike.DamageDice is not null ||
                strike.MajorWoundHtRoll is not null || strike.ConcentrationRoll is not null ||
                strike.AimRetentionRoll is not null ||
                (strike.StatusRiders ?? []).Any(rider => rider.ResistanceRoll is not null))
                throw new ArgumentException("Confirmed physical execution accepts deterministic inputs only; the battle ledger owns every draw.", nameof(request));
        }

        bool normalAttackTargetLegal = request.Declaration.PassedRangeCheck &&
            request.Declaration.PassedVerticalCheck && request.Strikes.All(strike =>
                strike.Ranged is null || strike.Ranged.NativeRangeLegal && strike.Ranged.NativeTrajectoryLegal);
        DclCanonicalPhysicalStatePlan statePlan = DclCanonicalActionStateProjection.PlanPhysical(
            battle,
            request.Declaration.Source,
            profile.TimingProfile,
            strikeWeapons.ToDictionary(weapon => weapon.StrikeIndex, weapon => weapon.WeaponResourceKey),
            request.Strikes,
            request.IsUniversalNormalAttack,
            request.Declaration.TrackedTarget,
            normalAttackTargetLegal);
        if (!statePlan.Legal)
            throw new InvalidOperationException($"Physical action is illegal under current DCL state: {statePlan.Reason}.");
        if (request.Targets.Any(target =>
                !statePlan.TargetStateRevisions.TryGetValue(target.Target, out long revision) ||
                revision != target.CombatStateRevision))
            throw new ArgumentException(
                "Physical execution received a stale target custom-state revision.",
                nameof(request));
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> stateProjectedStrikes = statePlan.Strikes;
        ValidateProtectionRedirect(request, profile, stateProjectedStrikes);

        var defenseBefore = new Dictionary<DclUnitKey, DclDefenseResourceSnapshot>();
        var canonicalTargets = new List<DclTargetResolutionSnapshot>(request.Targets.Count);
        foreach (DclTargetResolutionSnapshot target in request.Targets)
        {
            string[] parryKeys = target.DefenseResources.ParryAttemptCounts.Keys
                .Concat(stateProjectedStrikes
                    .Where(strike => strike.Target == target.Target)
                    .SelectMany(strike => strike.DefenseCandidates)
                    .Where(candidate => candidate.Kind == DclDefenseKind.Parry &&
                        !string.IsNullOrWhiteSpace(candidate.ResourceKey))
                    .Select(candidate => candidate.ResourceKey!))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            DclDefenseResourceSnapshot canonical = battle.CaptureDefenseResources(target.Target, parryKeys);
            if (!DclCanonicalBattleRuntime.DefenseResourcesEqual(canonical, target.DefenseResources))
                throw new ArgumentException("Physical execution received a stale or noncanonical defense-resource snapshot.", nameof(request));
            if (!defenseBefore.TryAdd(target.Target, canonical))
                throw new ArgumentException("Physical execution cannot contain the same target identity twice.", nameof(request));
            canonicalTargets.Add(target with { DefenseResources = canonical });
        }

        DclTurnResources turnResources = battle.TurnResources(request.Declaration.Source);
        DclCanonicalPhysicalStrikeWeaponState[] weaponStates = strikeWeapons.Select(strikeWeapon =>
            new DclCanonicalPhysicalStrikeWeaponState(
                strikeWeapon,
                battle.WeaponState(
                    request.Declaration.Source,
                    strikeWeapon.WeaponResourceKey,
                    weapons[strikeWeapon.StrikeIndex].Balance,
                    weapons[strikeWeapon.StrikeIndex].Readiness)))
            .ToArray();
        DclWeaponActionState weaponState = weaponStates[0].State;
        DclCanonicalProtectionRedirectResolution? committedProtectionRedirect = request.ProtectionRedirect is null
            ? null
            : battle.CommitProtectionRedirect(request.ProtectionRedirect);
        if (committedProtectionRedirect is not null)
        {
            for (int index = 0; index < canonicalTargets.Count; index++)
            {
                DclTargetResolutionSnapshot target = canonicalTargets[index];
                canonicalTargets[index] = target with
                {
                    CombatStateRevision = battle.States.CaptureTarget(target.Target).Revision,
                };
            }
        }
        var input = new DclCanonicalPhysicalExecutionInput(
            request.AbilityId,
            request.Declaration,
            request.WeaponItemId,
            request.SourceSt,
            canonicalTargets,
            request.TargetContexts,
            stateProjectedStrikes,
            turnResources,
            weaponState,
            battle.States,
            battle,
            DclCanonicalReactionWindow.ConfirmedRequest(battle, request.ReactionCandidates),
            strikeWeapons,
            weaponStates);
        DclCanonicalPhysicalExecutionResult result = DclCanonicalPhysicalExecutor.Resolve(battle.Catalog, input) with
        {
            ProtectionRedirectPlan = request.ProtectionRedirect,
            ProtectionRedirect = committedProtectionRedirect,
        };
        battle.CommitDefenseResourcesBatch(result.FinalDefenseResources.Select(pair =>
            new DclCanonicalDefenseResourceCommit(pair.Key, defenseBefore[pair.Key], pair.Value)));
        DclCanonicalNativeActionApplication application = DclCanonicalNativeExecutionPublisher.PublishPhysical(
            battle,
            request.AbilityId,
            result);
        return new DclCanonicalPublishedPhysicalExecution(result, application);
    }

    private static void ValidateProtectionRedirect(
        DclCanonicalPhysicalExecutionRequest request,
        DclActionProfile profile,
        IReadOnlyList<DclCanonicalPhysicalStrikeInput> stateProjectedStrikes)
    {
        if (request.ProtectionRedirect is null)
            return;
        DclCanonicalProtectionRedirectPlan plan = request.ProtectionRedirect;
        DclCanonicalProtectionRedirectCandidate candidate = plan.Candidate;
        DclUnitKey finalTarget = plan.Resolution.FinalTarget;
        if (profile.DeliveryProfile.Delivery != DclDelivery.PhysicalAttack ||
            candidate.Delivery != DclDelivery.PhysicalAttack ||
            candidate.Attacker != request.Declaration.Source ||
            candidate.DeclaredTarget != request.Declaration.TrackedTarget)
            throw new ArgumentException(
                "Physical protection routing must preserve the declared attacker/target and PhysicalAttack delivery.",
                nameof(request));
        if (request.Targets.Count != 1 || request.TargetContexts.Count != 1 ||
            request.Targets[0].Target != finalTarget ||
            request.TargetContexts[0].Target != finalTarget ||
            stateProjectedStrikes.Count == 0 ||
            stateProjectedStrikes.Any(strike => strike.Target != finalTarget))
            throw new ArgumentException(
                "Physical protection routing must resolve every target context and Strike against the planned final target.",
                nameof(request));
    }
}
