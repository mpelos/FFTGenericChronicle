namespace fftivc.generic.chronicle.codemod;

internal enum DclPresentationNativeSurface
{
    UnitBodyPosition,
    UnitBodyPalette,
    UnitTransparency,
    AboveUnitStatusIcon,
    CtTimelineIcon,
    SelectedStateDetail,
    EquipmentDetail,
    DefensePanel,
    CommandDisablement,
    EntryFeedback,
}

internal enum DclPresentationNativeSurfaceStatus
{
    Unknown,
    PureSnapshotReady,
    NativeBindingLiveGated,
    NativeBindingProven,
}

internal sealed record DclPresentationNativeSurfaceBinding(
    DclPresentationNativeSurface Surface,
    DclPresentationNativeSurfaceStatus Status,
    string? ProofId,
    string Notes);

internal sealed record DclPresentationNativeSurfaceRequirement(
    DclPresentationNativeSurface Surface,
    string AssetOrChannel,
    string Reason,
    IReadOnlyList<long> StateInstanceIds);

internal sealed record DclPresentationNativeSurfaceFinding(
    DclPresentationNativeSurface Surface,
    string Message);

internal sealed class DclPresentationNativeSurfaceAuditResult
{
    internal DclPresentationNativeSurfaceAuditResult(
        IReadOnlyList<DclPresentationNativeSurfaceRequirement> requirements,
        IReadOnlyList<DclPresentationNativeSurfaceFinding> findings,
        IReadOnlyDictionary<DclPresentationNativeSurface, DclPresentationNativeSurfaceBinding> bindingsBySurface)
    {
        Requirements = requirements;
        Findings = findings;
        BindingsBySurface = bindingsBySurface;
    }

    public IReadOnlyList<DclPresentationNativeSurfaceRequirement> Requirements { get; }
    public IReadOnlyList<DclPresentationNativeSurfaceFinding> Findings { get; }
    public IReadOnlyDictionary<DclPresentationNativeSurface, DclPresentationNativeSurfaceBinding> BindingsBySurface { get; }
    public bool IsValid => Findings.Count == 0;

    public bool AllNativeBindingsProven
    {
        get
        {
            if (!IsValid) return false;
            foreach (DclPresentationNativeSurfaceRequirement requirement in Requirements)
            {
                if (!BindingsBySurface.TryGetValue(requirement.Surface, out DclPresentationNativeSurfaceBinding? binding))
                    return false;
                if (binding.Status != DclPresentationNativeSurfaceStatus.NativeBindingProven)
                    return false;
            }
            return true;
        }
    }
}

internal static class DclPresentationNativeSurfaceAudit
{
    public static DclPresentationNativeSurfaceAuditResult Audit(
        DclUnitPresentationSnapshot snapshot,
        IReadOnlyList<DclStateInstance> instances,
        DclStatePresentationProfileRegistry presentations,
        IReadOnlyDictionary<DclPresentationNativeSurface, DclPresentationNativeSurfaceBinding> bindingsBySurface)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentNullException.ThrowIfNull(presentations);
        ArgumentNullException.ThrowIfNull(bindingsBySurface);

        IReadOnlyList<DclPresentationNativeSurfaceRequirement> requirements =
            ResolveRequirements(snapshot, instances, presentations);
        var findings = new List<DclPresentationNativeSurfaceFinding>();
        foreach (DclPresentationNativeSurface surface in requirements
                     .Select(requirement => requirement.Surface)
                     .Distinct()
                     .OrderBy(surface => surface))
        {
            if (!bindingsBySurface.TryGetValue(surface, out DclPresentationNativeSurfaceBinding? binding))
            {
                findings.Add(new(surface, "native presentation surface is required by the snapshot but has no binding status"));
                continue;
            }
            if (binding.Surface != surface)
            {
                findings.Add(new(surface, $"binding row declares surface {binding.Surface}"));
                continue;
            }
            if (binding.Status is DclPresentationNativeSurfaceStatus.Unknown)
            {
                findings.Add(new(surface, "native presentation surface cannot remain Unknown"));
            }
            if (binding.Status is DclPresentationNativeSurfaceStatus.PureSnapshotReady)
            {
                findings.Add(new(surface, "pure snapshot readiness is not a native UI binding proof"));
            }
            if (binding.Status is DclPresentationNativeSurfaceStatus.NativeBindingProven &&
                string.IsNullOrWhiteSpace(binding.ProofId))
            {
                findings.Add(new(surface, "proven native presentation binding requires a stable proof id"));
            }
        }

        return new DclPresentationNativeSurfaceAuditResult(requirements, findings, bindingsBySurface);
    }

    private static IReadOnlyList<DclPresentationNativeSurfaceRequirement> ResolveRequirements(
        DclUnitPresentationSnapshot snapshot,
        IReadOnlyList<DclStateInstance> instances,
        DclStatePresentationProfileRegistry presentations)
    {
        var requirements = new List<DclPresentationNativeSurfaceRequirement>();
        IReadOnlyDictionary<string, DclStatePresentationProfile> profilesById = presentations.Profiles;

        if (!StringComparer.Ordinal.Equals(snapshot.Body.PositionAssetRef, "NativePosition(Normal)"))
        {
            requirements.Add(new(
                DclPresentationNativeSurface.UnitBodyPosition,
                snapshot.Body.PositionAssetRef,
                "resolved unit body position differs from the neutral standing pose",
                InstanceIdsWithProfileAsset(instances, profilesById, profile => profile.PositionAssetRef)));
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Body.PaletteAssetRef))
        {
            requirements.Add(new(
                DclPresentationNativeSurface.UnitBodyPalette,
                snapshot.Body.PaletteAssetRef,
                "resolved unit body palette requires native palette or custom palette presentation",
                InstanceIdsWithProfileAsset(instances, profilesById, profile => profile.PaletteAssetRef)));
        }

        if (snapshot.Body.ApplyInvisibilityTransparency)
        {
            requirements.Add(new(
                DclPresentationNativeSurface.UnitTransparency,
                "NativeTransparency(Invisible)",
                "invisibility transparency is layered after the winning body palette",
                []));
        }

        if (snapshot.Channels.UnitIcons.Count > 0)
        {
            requirements.Add(new(
                DclPresentationNativeSurface.AboveUnitStatusIcon,
                string.Join(",", snapshot.Channels.UnitIcons.Select(icon => icon.IconAssetRef).Distinct(StringComparer.Ordinal)),
                "above-unit DCL status icon channel contains at least one icon",
                snapshot.Channels.UnitIcons.Select(icon => icon.InstanceId).Distinct().Order().ToList()));
        }

        if (snapshot.Channels.TimelineIcons.Count > 0)
        {
            requirements.Add(new(
                DclPresentationNativeSurface.CtTimelineIcon,
                string.Join(",", snapshot.Channels.TimelineIcons.Select(icon => icon.IconAssetRef).Distinct(StringComparer.Ordinal)),
                "CT timeline DCL status icon channel contains at least one icon",
                snapshot.Channels.TimelineIcons.Select(icon => icon.InstanceId).Distinct().Order().ToList()));
        }

        if (snapshot.Channels.SelectedDetails.Count > 0)
        {
            requirements.Add(new(
                DclPresentationNativeSurface.SelectedStateDetail,
                "SelectedUnitStateDetail",
                "selected-unit panel must expose persistent DCL state details",
                snapshot.Channels.SelectedDetails
                    .Select((_, index) => snapshot.Channels.SelectedDetails.Count == instances.Count
                        ? instances.OrderBy(instance => instance.InstanceId).ElementAt(index).InstanceId
                        : 0)
                    .Where(instanceId => instanceId != 0)
                    .ToList()));
        }

        IReadOnlyList<long> equipmentDetailInstances = InstancesMatchingKind(
            instances,
            "ready",
            "unready");
        if (equipmentDetailInstances.Count > 0)
        {
            requirements.Add(new(
                DclPresentationNativeSurface.EquipmentDetail,
                "SelectedEquipmentDetail",
                "ready/unready technical states are intentionally detail-only equipment facts",
                equipmentDetailInstances));
        }

        IReadOnlyList<long> defensePanelInstances = InstancesMatchingKind(
            instances,
            "block-spent",
            "repeated-parry");
        if (defensePanelInstances.Count > 0)
        {
            requirements.Add(new(
                DclPresentationNativeSurface.DefensePanel,
                "DefensiveResourcePanel",
                "defensive-resource technical states are intentionally detail-only defense-panel facts",
                defensePanelInstances));
        }

        IReadOnlyList<long> commandDisablementInstances = InstancesMatchingKind(
            instances,
            "stun",
            "knocked-down",
            "taunt");
        if (commandDisablementInstances.Count > 0)
        {
            requirements.Add(new(
                DclPresentationNativeSurface.CommandDisablement,
                "CommandAvailabilityDisabledRows",
                "current state set can suppress command rows and must not rely on hidden gameplay state",
                commandDisablementInstances));
        }

        IReadOnlyList<long> entryFeedbackInstances = InstanceIdsWithProfileAsset(
            instances,
            profilesById,
            profile => profile.EntryFeedbackRef);
        if (entryFeedbackInstances.Count > 0)
        {
            requirements.Add(new(
                DclPresentationNativeSurface.EntryFeedback,
                "StateEntryFeedback",
                "at least one active state profile declares entry feedback",
                entryFeedbackInstances));
        }

        return requirements;
    }

    private static IReadOnlyList<long> InstanceIdsWithProfileAsset(
        IReadOnlyList<DclStateInstance> instances,
        IReadOnlyDictionary<string, DclStatePresentationProfile> profilesById,
        Func<DclStatePresentationProfile, string?> selector)
        => instances
            .Where(instance =>
                profilesById.TryGetValue(instance.PresentationId, out DclStatePresentationProfile? profile) &&
                !string.IsNullOrWhiteSpace(selector(profile)))
            .Select(instance => instance.InstanceId)
            .Order()
            .ToList();

    private static IReadOnlyList<long> InstancesMatchingKind(
        IReadOnlyList<DclStateInstance> instances,
        params string[] stateKinds)
    {
        var accepted = new HashSet<string>(stateKinds, StringComparer.OrdinalIgnoreCase);
        return instances
            .Where(instance => accepted.Contains(instance.Kind))
            .Select(instance => instance.InstanceId)
            .Order()
            .ToList();
    }
}
