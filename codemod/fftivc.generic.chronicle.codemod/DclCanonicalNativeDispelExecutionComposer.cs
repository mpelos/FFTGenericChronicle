namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeDispelActionInputs(
    DclCanonicalNativeSingleTargetMagicInputs Magic,
    int FinalDispelScore,
    long? SelectedInstanceId = null);

/// <summary>
/// Captures the exact eligible Dispel instance set from the same target-state revision as the
/// admitted native snapshot. AllEligible selects the complete ordered set; OneInstance requires
/// one explicit eligible stable InstanceId or an explicitly empty selection.
/// </summary>
internal static class DclCanonicalNativeDispelExecutionComposer
{
    public static DclCanonicalDispelExecutionInput Compose(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeOuterSweepAdmission admission,
        DclCanonicalNativeSnapshotBatch snapshots,
        DclCanonicalNativeDispelActionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(inputs);
        if (battle.Catalog.ResolveAbilityFamily(admission.AbilityId) != DclCanonicalActionFamily.Dispel)
            throw new ArgumentException("Native Dispel composition requires the Dispel canonical family.", nameof(admission));
        if (inputs.FinalDispelScore < 1)
            throw new ArgumentOutOfRangeException(nameof(inputs), "Final DispelScore must be positive.");
        DclCanonicalNativeSingleTargetMagicSnapshot common =
            DclCanonicalNativeSingleTargetMagicComposer.Compose(battle, admission, snapshots, inputs.Magic);
        DclDispelProfile dispel = common.Profile.DispelProfile ??
            throw new InvalidOperationException("The Dispel carrier lost its normalized selection profile.");
        HashSet<string> eligibleFamilies = dispel.EligibleCureFamilies.ToHashSet(StringComparer.Ordinal);
        DclStateInstance[] eligible = common.Target.Unit.State.Instances
            .Where(instance => instance.Definition.CureFamilies.Any(eligibleFamilies.Contains) &&
                (!dispel.SourceMatchedOnly || instance.Source == admission.Source))
            .OrderBy(instance => instance.InstanceId)
            .ToArray();
        DclStateInstance[] selected = dispel.Scope switch
        {
            DclDispelScope.AllEligible when inputs.SelectedInstanceId is null => eligible,
            DclDispelScope.AllEligible => throw new ArgumentException(
                "AllEligible Dispel owns the complete snapshot and cannot accept one selected id.", nameof(inputs)),
            DclDispelScope.OneInstance when inputs.SelectedInstanceId is null => [],
            DclDispelScope.OneInstance =>
                [eligible.SingleOrDefault(instance => instance.InstanceId == inputs.SelectedInstanceId.Value) ??
                 throw new ArgumentException("The selected Dispel instance is absent or ineligible in this snapshot.", nameof(inputs))],
            _ => throw new InvalidOperationException("The Dispel scope has no canonical selection owner."),
        };
        return new DclCanonicalDispelExecutionInput(
            admission.AbilityId,
            common.DeclarationRequest,
            admission.ActionInstanceId,
            common.Target.Unit.Target,
            common.BaseSpellScore,
            common.TargetSpellScore,
            inputs.FinalDispelScore,
            SharedCasterRoll: null,
            selected.Select(instance => new DclCanonicalDispelInstanceInput(
                instance.InstanceId,
                EffectResistanceRoll: null)).ToArray(),
            common.Source.Unit.CurrentMp,
            common.Source.Unit.Target.CurrentHp,
            battle.States);
    }
}
