namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclPositionPresentationInput(
    bool Ko,
    bool Transformation,
    bool KnockedDown,
    bool Stun,
    bool Confusion,
    bool Critical,
    bool Charging,
    bool Casting,
    bool Performing,
    bool Defending);

internal readonly record struct DclPalettePresentationInput(
    bool Ko,
    bool Invisible,
    bool Stun,
    bool Berserk,
    bool Taunt,
    bool Fear,
    bool Vampire,
    bool Undead,
    bool Poison,
    bool Oil,
    string? ElementalExposure);

internal readonly record struct DclResolvedPresentation(
    string PositionAssetRef,
    string? PaletteAssetRef,
    bool ApplyInvisibilityTransparency);

internal readonly record struct DclNativePresentationInput(
    bool Ko,
    bool Transformation,
    bool Confusion,
    bool Critical,
    bool Charging,
    bool Casting,
    bool Performing,
    bool Defending,
    bool Invisible,
    bool Berserk,
    bool Vampire,
    bool Undead,
    bool Poison,
    bool Oil,
    string? ElementalExposure);

internal sealed record DclUnitPresentationSnapshot(
    DclResolvedPresentation Body,
    DclStatePresentationChannels Channels);

internal sealed record DclSelectedStateDetail(
    string DisplayName,
    string MechanicalEffect,
    DclUnitKey? Source,
    int? Magnitude,
    string Remaining,
    DclStateStackPolicy Stacking,
    IReadOnlyList<string> CureFamilies,
    string IconAssetRef);

internal sealed record DclStateIconPresentation(
    long InstanceId,
    string StateKind,
    string IconAssetRef);

internal sealed record DclStatePresentationChannels(
    IReadOnlyList<DclStateIconPresentation> UnitIcons,
    IReadOnlyList<DclStateIconPresentation> TimelineIcons,
    IReadOnlyList<DclSelectedStateDetail> SelectedDetails,
    IReadOnlyList<string> DetailOnlyStateKinds);

internal static class DclStatePresentation
{
    public static DclResolvedPresentation Resolve(
        DclPositionPresentationInput position,
        DclPalettePresentationInput palette)
        => new(
            ResolvePosition(position),
            ResolvePalette(palette),
            palette.Invisible);

    public static string ResolvePosition(DclPositionPresentationInput input)
    {
        if (input.Ko) return "NativePosition(KO)";
        if (input.Transformation) return "NativePosition(Transformation)";
        if (input.KnockedDown) return "NativePosition(KO)";
        if (input.Stun || input.Confusion) return input.Stun
            ? "NativePosition(Critical)"
            : "NativePosition(Confusion)";
        if (input.Critical) return "NativePosition(Critical)";
        if (input.Charging) return "NativePosition(Charging)";
        if (input.Casting) return "NativePosition(Casting)";
        if (input.Performing) return "NativePosition(Performing)";
        if (input.Defending) return "NativePosition(Defending)";
        return "NativePosition(Normal)";
    }

    public static string? ResolvePalette(DclPalettePresentationInput input)
    {
        if (input.Ko) return null;
        if (input.Stun) return "DclPalette(PaleYellow:#E9D96E)";
        if (input.Berserk) return "NativePalette(Berserk)";
        if (input.Taunt) return "DclPalette(IntenseRed:#D82020)";
        if (input.Fear) return "DclPalette(BlueGray:#7D8FA3)";
        if (input.Vampire) return "NativePalette(Vampire)";
        if (input.Undead) return "NativePalette(Undead)";
        if (input.Poison) return "NativePalette(Poison)";
        if (input.Oil) return "NativePalette(Oil)";
        return string.IsNullOrWhiteSpace(input.ElementalExposure)
            ? null
            : $"DclPalette(Element:{input.ElementalExposure.Trim().ToLowerInvariant()})";
    }

    public static string IconFor(string stateKind, int? magnitude = null)
    {
        if (string.IsNullOrWhiteSpace(stateKind)) throw new ArgumentException("A state kind is required.", nameof(stateKind));
        return stateKind.Trim().ToLowerInvariant() switch
        {
            "stun" or "guard-broken" or "weapon-bound" => "NativeStatusIcon(Disable)",
            "knocked-down" => "NativeStatusIcon(Immobilize)",
            "shock" when magnitude is >= 1 and <= 3 => $"NativeStatusIcon(Doom{magnitude})",
            "shock" => throw new ArgumentOutOfRangeException(nameof(magnitude), "Shock icon magnitude must be 1..3."),
            "taunt" => "NativeStatusIcon(Berserk)",
            "fear" => "NativeStatusIcon(Chicken)",
            "bulwark" or "cover" or "bodyguard" => "NativeStatusIcon(Protect)",
            "elemental-exposure" => "NativeStatusIcon(Oil)",
            _ => $"PresentationId({stateKind.Trim()})",
        };
    }

    public static string? UnitIconFor(string stateKind, int? magnitude = null)
    {
        if (string.IsNullOrWhiteSpace(stateKind)) throw new ArgumentException("A state kind is required.", nameof(stateKind));
        return stateKind.Trim().ToLowerInvariant() switch
        {
            "quicklock" or "ready" or "unready" or "block-spent" or "repeated-parry" => null,
            _ => IconFor(stateKind, magnitude),
        };
    }

    public static string? TimelineIconFor(string stateKind)
    {
        if (string.IsNullOrWhiteSpace(stateKind)) throw new ArgumentException("A state kind is required.", nameof(stateKind));
        return stateKind.Trim().ToLowerInvariant() switch
        {
            "quicklock" => "NativeStatusIcon(Haste)",
            _ => null,
        };
    }

    public static DclSelectedStateDetail Detail(
        DclStateInstance instance,
        string displayName,
        long currentGlobalCt,
        int? remainingTurnsOrUses = null)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A display name is required.", nameof(displayName));
        if (currentGlobalCt < 0 || remainingTurnsOrUses < 0) throw new ArgumentOutOfRangeException(nameof(currentGlobalCt));
        string remaining = instance.Definition.Duration.Clock switch
        {
            DclStateDurationClock.GlobalCt when instance.ExpiresAtGlobalCt is { } expiry =>
                $"{Math.Max(0, expiry - currentGlobalCt)} CT",
            DclStateDurationClock.TargetTurn => $"{remainingTurnsOrUses ?? throw MissingRemaining()} target turns",
            DclStateDurationClock.SourceTurn => $"{remainingTurnsOrUses ?? throw MissingRemaining()} source turns",
            DclStateDurationClock.UsesOrTriggers => $"{instance.RemainingUses ?? remainingTurnsOrUses ?? throw MissingRemaining()} uses/triggers",
            DclStateDurationClock.ExplicitCommand => "until removal command",
            DclStateDurationClock.Permanent => "permanent",
            DclStateDurationClock.Explicit => "state-specific expiry",
            _ => "unknown expiry",
        };
        int? magnitude = instance.Strength;
        return new DclSelectedStateDetail(
            displayName,
            instance.Definition.MechanicalRules,
            instance.Source,
            magnitude,
            remaining,
            instance.Definition.StackPolicy,
            instance.Definition.CureFamilies,
            IconFor(instance.Kind, instance.Kind.Equals("shock", StringComparison.OrdinalIgnoreCase) ? magnitude : null));
    }

    public static DclStatePresentationChannels ResolveChannels(
        IReadOnlyList<DclStateInstance> instances,
        IReadOnlyDictionary<string, string> displayNamesByKind,
        long currentGlobalCt,
        IReadOnlyDictionary<long, int>? remainingTurnsOrUsesByInstance = null)
    {
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentNullException.ThrowIfNull(displayNamesByKind);
        if (currentGlobalCt < 0) throw new ArgumentOutOfRangeException(nameof(currentGlobalCt));

        var unitIcons = new List<DclStateIconPresentation>();
        var timelineIcons = new List<DclStateIconPresentation>();
        var details = new List<DclSelectedStateDetail>();
        var detailOnly = new List<string>();
        foreach (DclStateInstance instance in instances.OrderBy(instance => instance.InstanceId))
        {
            if (!displayNamesByKind.TryGetValue(instance.Kind, out string? displayName) ||
                string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    $"Missing display name for DCL state kind '{instance.Kind}'.",
                    nameof(displayNamesByKind));
            }

            int? iconMagnitude = instance.Kind.Equals("shock", StringComparison.OrdinalIgnoreCase)
                ? instance.Strength
                : null;
            string? unitIcon = UnitIconFor(instance.Kind, iconMagnitude);
            if (unitIcon is not null)
                unitIcons.Add(new DclStateIconPresentation(instance.InstanceId, instance.Kind, unitIcon));

            string? timelineIcon = TimelineIconFor(instance.Kind);
            if (timelineIcon is not null)
                timelineIcons.Add(new DclStateIconPresentation(instance.InstanceId, instance.Kind, timelineIcon));

            int? remaining = remainingTurnsOrUsesByInstance?.GetValueOrDefault(instance.InstanceId);
            details.Add(Detail(instance, displayName, currentGlobalCt, remaining));
            if (unitIcon is null && timelineIcon is null)
                detailOnly.Add(instance.Kind);
        }

        return new DclStatePresentationChannels(unitIcons, timelineIcons, details, detailOnly);
    }

    public static DclStatePresentationChannels ResolveChannels(
        IReadOnlyList<DclStateInstance> instances,
        DclStatePresentationProfileRegistry presentations,
        long currentGlobalCt,
        IReadOnlyDictionary<long, int>? remainingTurnsOrUsesByInstance = null)
    {
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentNullException.ThrowIfNull(presentations);
        var displayNamesByKind = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DclStateInstance instance in instances.OrderBy(instance => instance.InstanceId))
        {
            if (!StringComparer.Ordinal.Equals(instance.PresentationId, instance.Definition.PresentationProfile))
            {
                throw new ArgumentException(
                    $"State instance {instance.InstanceId} kind '{instance.Kind}' carries presentation '{instance.PresentationId}' but definition requires '{instance.Definition.PresentationProfile}'.",
                    nameof(instances));
            }
            if (!presentations.Profiles.TryGetValue(
                    instance.Definition.PresentationProfile,
                    out DclStatePresentationProfile? profile))
            {
                throw new ArgumentException(
                    $"Missing presentation profile '{instance.Definition.PresentationProfile}' for DCL state kind '{instance.Kind}'.",
                    nameof(presentations));
            }
            displayNamesByKind[instance.Kind] = profile.DisplayName;
        }
        return ResolveChannels(instances, displayNamesByKind, currentGlobalCt, remainingTurnsOrUsesByInstance);
    }

    public static DclUnitPresentationSnapshot ResolveUnitPresentation(
        IReadOnlyList<DclStateInstance> instances,
        DclStatePresentationProfileRegistry presentations,
        DclNativePresentationInput native,
        long currentGlobalCt,
        IReadOnlyDictionary<long, int>? remainingTurnsOrUsesByInstance = null)
    {
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentNullException.ThrowIfNull(presentations);
        DclStatePresentationChannels channels = ResolveChannels(
            instances,
            presentations,
            currentGlobalCt,
            remainingTurnsOrUsesByInstance);
        DclResolvedPresentation body = new(
            ResolveProfilePosition(instances, presentations, native),
            ResolveProfilePalette(instances, presentations, native),
            native.Invisible);
        return new DclUnitPresentationSnapshot(body, channels);
    }

    private static string ResolveProfilePosition(
        IReadOnlyList<DclStateInstance> instances,
        DclStatePresentationProfileRegistry presentations,
        DclNativePresentationInput native)
    {
        if (native.Ko) return "NativePosition(KO)";
        if (native.Transformation) return "NativePosition(Transformation)";

        string? selected = null;
        int selectedPriority = int.MaxValue;
        long selectedInstanceId = long.MaxValue;
        foreach (DclStateInstance instance in instances)
        {
            DclStatePresentationProfile profile = RequireProfile(instance, presentations);
            if (string.IsNullOrWhiteSpace(profile.PositionAssetRef)) continue;
            int priority = PositionPriority(profile.PositionAssetRef);
            if (priority < selectedPriority ||
                priority == selectedPriority && instance.InstanceId < selectedInstanceId)
            {
                selected = profile.PositionAssetRef;
                selectedPriority = priority;
                selectedInstanceId = instance.InstanceId;
            }
        }

        if (selected is not null && selectedPriority <= PositionPriority("NativePosition(Critical)"))
            return selected;
        if (native.Confusion) return "NativePosition(Confusion)";
        if (native.Critical) return "NativePosition(Critical)";
        if (selected is not null) return selected;
        if (native.Charging) return "NativePosition(Charging)";
        if (native.Casting) return "NativePosition(Casting)";
        if (native.Performing) return "NativePosition(Performing)";
        if (native.Defending) return "NativePosition(Defending)";
        return "NativePosition(Normal)";
    }

    private static string? ResolveProfilePalette(
        IReadOnlyList<DclStateInstance> instances,
        DclStatePresentationProfileRegistry presentations,
        DclNativePresentationInput native)
    {
        if (native.Ko) return null;

        string? selected = null;
        int selectedPriority = int.MaxValue;
        foreach (DclStateInstance instance in instances)
        {
            DclStatePresentationProfile profile = RequireProfile(instance, presentations);
            if (string.IsNullOrWhiteSpace(profile.PaletteAssetRef)) continue;
            int priority = PalettePriority(profile.PaletteAssetRef);
            if (priority < selectedPriority ||
                priority == selectedPriority && string.CompareOrdinal(profile.PaletteAssetRef, selected) < 0)
            {
                selected = profile.PaletteAssetRef;
                selectedPriority = priority;
            }
        }

        string? nativePalette = ResolvePalette(new DclPalettePresentationInput(
            Ko: false,
            native.Invisible,
            Stun: false,
            native.Berserk,
            Taunt: false,
            Fear: false,
            native.Vampire,
            native.Undead,
            native.Poison,
            native.Oil,
            native.ElementalExposure));
        if (nativePalette is null) return selected;
        if (selected is null) return nativePalette;
        return PalettePriority(selected) <= PalettePriority(nativePalette)
            ? selected
            : nativePalette;
    }

    private static DclStatePresentationProfile RequireProfile(
        DclStateInstance instance,
        DclStatePresentationProfileRegistry presentations)
    {
        if (!StringComparer.Ordinal.Equals(instance.PresentationId, instance.Definition.PresentationProfile))
        {
            throw new ArgumentException(
                $"State instance {instance.InstanceId} kind '{instance.Kind}' carries presentation '{instance.PresentationId}' but definition requires '{instance.Definition.PresentationProfile}'.",
                nameof(instance));
        }
        if (!presentations.Profiles.TryGetValue(
                instance.Definition.PresentationProfile,
                out DclStatePresentationProfile? profile))
        {
            throw new ArgumentException(
                $"Missing presentation profile '{instance.Definition.PresentationProfile}' for DCL state kind '{instance.Kind}'.",
                nameof(presentations));
        }
        return profile;
    }

    private static int PositionPriority(string positionAssetRef)
        => positionAssetRef switch
        {
            "NativePosition(Transformation)" => 10,
            "NativePosition(KO)" => 20,
            "NativePosition(Critical)" => 30,
            "NativePosition(Charging)" => 60,
            "NativePosition(Casting)" => 61,
            "NativePosition(Performing)" => 62,
            "NativePosition(Defending)" => 63,
            "NativePosition(Normal)" => 100,
            _ => 50,
        };

    private static int PalettePriority(string paletteAssetRef)
        => paletteAssetRef switch
        {
            "DclPalette(PaleYellow:#E9D96E)" => 10,
            "NativePalette(Berserk)" => 20,
            "DclPalette(IntenseRed:#D82020)" => 30,
            "DclPalette(BlueGray:#7D8FA3)" => 40,
            "NativePalette(Vampire)" => 50,
            "NativePalette(Undead)" => 60,
            "NativePalette(Poison)" => 70,
            "NativePalette(Oil)" => 80,
            _ when paletteAssetRef.StartsWith("DclPalette(Element:", StringComparison.Ordinal) => 90,
            _ => 100,
        };

    private static InvalidOperationException MissingRemaining()
        => new("Turn-clock state detail requires the current remaining-turn count.");
}
