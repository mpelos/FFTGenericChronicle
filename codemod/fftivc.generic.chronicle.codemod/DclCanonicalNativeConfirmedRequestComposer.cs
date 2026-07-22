namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeComposedExecution(
    int AbilityId,
    DclCanonicalActionFamily Family,
    object FamilyInput);

/// <summary>
/// Single classified native request-composition entry. The atomic ability family selects the only
/// accepted explicit-policy input and exact deterministic coordinator request; the hook never
/// reclassifies from formula, animation, result kind, or input object shape.
/// </summary>
internal static class DclCanonicalNativeConfirmedRequestComposer
{
    public static DclCanonicalNativeComposedExecution ComposeCaptured(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action,
        IEnumerable<DclCanonicalNativeUnitPolicyInput> unitPolicyInputs,
        object familyPolicyInputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(unitPolicyInputs);
        if (action.Admissions.Count == 0 ||
            action.Admissions.Any(admission =>
                admission.ActionInstanceId != action.ActionInstanceId ||
                admission.Source != action.Source ||
                admission.ActionType != action.ActionType ||
                admission.AbilityId != action.AbilityId))
            throw new ArgumentException("A captured native action must retain one exact admission identity.", nameof(action));
        if (battle.Catalog.ResolveAbilityFamily(action.AbilityId) == DclCanonicalActionFamily.DirectNumeric &&
            familyPolicyInputs is not DclCanonicalNativeDirectActionInputs)
            throw new ArgumentException(
                "Captured DirectNumeric composition requires exact DirectNumeric policy inputs.",
                nameof(familyPolicyInputs));
        DclCanonicalNativeSnapshotBatch snapshots = DclCanonicalNativeSnapshotBatchProjector.ProjectCaptured(
            battle,
            action,
            unitPolicyInputs);
        ValidateNativeSelection(battle, action, snapshots, familyPolicyInputs);
        return Compose(battle, action.Admissions, snapshots, familyPolicyInputs);
    }

    public static DclCanonicalNativeComposedExecution Compose(
        DclCanonicalBattleRuntime battle,
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions,
        DclCanonicalNativeSnapshotBatch snapshots,
        object familyPolicyInputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(admissions);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(familyPolicyInputs);
        if (admissions.Count == 0)
            throw new ArgumentException("Native confirmed composition requires at least one admitted sweep.", nameof(admissions));
        int abilityId = admissions[0].AbilityId;
        if (admissions.Any(admission => admission.AbilityId != abilityId))
            throw new ArgumentException("One native composition cannot combine admissions from different abilities.", nameof(admissions));
        DclCanonicalActionFamily family = battle.Catalog.ResolveAbilityFamily(abilityId);
        object request = (family, familyPolicyInputs) switch
        {
            (DclCanonicalActionFamily.DirectNumeric, DclCanonicalNativeDirectActionInputs inputs) =>
                DclCanonicalNativeDirectExecutionComposer.Compose(battle, Single(admissions), snapshots, inputs),
            (DclCanonicalActionFamily.PhysicalDamage, DclCanonicalNativePhysicalActionInputs inputs) =>
                DclCanonicalNativePhysicalExecutionComposer.Compose(battle, admissions, snapshots, inputs),
            (DclCanonicalActionFamily.AreaNumeric, DclCanonicalNativeAreaActionInputs inputs) =>
                DclCanonicalNativeAreaExecutionComposer.Compose(battle, admissions, snapshots, inputs),
            (DclCanonicalActionFamily.StatusApplication, DclCanonicalNativeStatusActionInputs inputs) =>
                DclCanonicalNativeStatusExecutionComposer.Compose(battle, Single(admissions), snapshots, inputs),
            (DclCanonicalActionFamily.StatusRemoval, DclCanonicalNativeSingleTargetMagicInputs inputs) =>
                DclCanonicalNativeStatusRemovalExecutionComposer.Compose(battle, Single(admissions), snapshots, inputs),
            (DclCanonicalActionFamily.Dispel, DclCanonicalNativeDispelActionInputs inputs) =>
                DclCanonicalNativeDispelExecutionComposer.Compose(battle, Single(admissions), snapshots, inputs),
            (DclCanonicalActionFamily.Quick, DclCanonicalNativeQuickActionInputs inputs) =>
                DclCanonicalNativeQuickExecutionComposer.Compose(battle, Single(admissions), snapshots, inputs),
            (DclCanonicalActionFamily.Revive, DclCanonicalNativeReviveActionInputs inputs) =>
                DclCanonicalNativeReviveExecutionComposer.Compose(battle, Single(admissions), snapshots, inputs),
            (DclCanonicalActionFamily.ForcedMovement, DclCanonicalNativeForcedMovementActionInputs inputs) =>
                DclCanonicalNativeForcedMovementExecutionComposer.Compose(battle, Single(admissions), snapshots, inputs),
            _ => throw new ArgumentException(
                $"Ability {abilityId} family {family} requires its exact native policy-input type, not {familyPolicyInputs.GetType().Name}.",
                nameof(familyPolicyInputs)),
        };
        return new DclCanonicalNativeComposedExecution(abilityId, family, request);
    }

    private static DclCanonicalNativeOuterSweepAdmission Single(
        IReadOnlyList<DclCanonicalNativeOuterSweepAdmission> admissions)
        => admissions.Count == 1
            ? admissions[0]
            : throw new ArgumentException("This canonical family requires exactly one admitted nonrepeat sweep.", nameof(admissions));

    private static void ValidateNativeSelection(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action,
        DclCanonicalNativeSnapshotBatch snapshots,
        object familyPolicyInputs)
    {
        (DclAbilityBinding _, DclActionProfile profile) = battle.Catalog.ResolveAbility(action.AbilityId);
        switch (profile.TargetProfile.TargetMode)
        {
            case DclTargetMode.Unit:
            {
                DclUnitKey declaredUnit = DeclaredUnit(familyPolicyInputs) ??
                    throw new ArgumentException("The canonical family input lacks its declared unit target.", nameof(familyPolicyInputs));
                if (!snapshots.Units.TryGetValue(declaredUnit, out DclCanonicalNativeUnitSnapshotResult? selected) ||
                    selected.Unit.Target.Tile != action.SelectedTile)
                    throw new ArgumentException(
                        "The declared unit target, selected X/Y/map-level tuple, and frozen unit tile must agree.",
                        nameof(action));
                break;
            }
            case DclTargetMode.FixedTile:
            {
                DclBattleTile? declaredTile = FixedTile(familyPolicyInputs);
                if (declaredTile is null || declaredTile.Value != action.SelectedTile)
                    throw new ArgumentException(
                        "The canonical fixed-tile declaration must equal the captured native selected X/Y/map-level tuple.",
                        nameof(familyPolicyInputs));
                break;
            }
        }
    }

    private static DclUnitKey? DeclaredUnit(object inputs) => inputs switch
    {
        DclCanonicalNativeDirectActionInputs value => value.DeclaredTarget,
        DclCanonicalNativePhysicalActionInputs value => value.DeclaredTarget,
        DclCanonicalNativeAreaActionInputs value => value.DeclaredTarget,
        DclCanonicalNativeSingleTargetMagicInputs value => value.DeclaredTarget,
        DclCanonicalNativeStatusActionInputs value => value.Magic.DeclaredTarget,
        DclCanonicalNativeDispelActionInputs value => value.Magic.DeclaredTarget,
        DclCanonicalNativeQuickActionInputs value => value.Magic.DeclaredTarget,
        DclCanonicalNativeReviveActionInputs value => value.Magic.DeclaredTarget,
        DclCanonicalNativeForcedMovementActionInputs value => value.Magic.DeclaredTarget,
        _ => null,
    };

    private static DclBattleTile? FixedTile(object inputs) => inputs switch
    {
        DclCanonicalNativePhysicalActionInputs value => value.FixedTile,
        DclCanonicalNativeAreaActionInputs value => value.FixedTile,
        _ => null,
    };
}
