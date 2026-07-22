namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalMagicSnapshotMechanics(
    int SpellScoreModifier,
    int MagnitudeIntegerModifier,
    DclElementAffinity Affinity,
    DclRational FaithMagnitude,
    bool TargetHasShell,
    bool DeclaredTargetHasReflect,
    bool FireEffect,
    bool OilContributed,
    int ApplicableDr,
    int ConcentrationModifier,
    IReadOnlyList<int> CastCtModifiers,
    IReadOnlyList<DclRational> MpCostMultipliers);

internal sealed record DclCanonicalMagicSourceSnapshotMechanics(
    int SpellScoreModifier,
    int DamageMagnitudeModifier,
    int HealingMagnitudeModifier,
    DclRational FocusElementBoostMultiplier,
    int ConcentrationModifier,
    IReadOnlyList<int> CastCtModifiers,
    IReadOnlyList<DclRational> MpCostMultipliers);

internal static class DclCanonicalMagicSnapshotProjector
{
    public static DclCanonicalMagicSourceSnapshotMechanics ResolveSource(
        DclActionProfile profile,
        string tradition,
        DclCanonicalNativeUnitProjection caster,
        DclCanonicalEquipmentSnapshot casterEquipment)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(casterEquipment);
        if (string.IsNullOrWhiteSpace(tradition))
            throw new ArgumentException("A magic tradition identity is required.", nameof(tradition));
        DclCanonicalFocusModifiers focus = casterEquipment.ResolveFocus(tradition);
        DclCanonicalShockSnapshot? casterShock = caster.State.Shock(caster.Secondary.MaxHp);
        return new DclCanonicalMagicSourceSnapshotMechanics(
            checked(profile.SkillProfile.SkillModifier + focus.SpellSkillModifier +
                (casterShock?.SkillModifier ?? 0)),
            focus.DamageModifier,
            focus.HealingModifier,
            focus.ElementBoostMultiplier,
            focus.ConcentrationModifier,
            focus.CastCtModifier == 0 ? [] : [focus.CastCtModifier],
            focus.MpCostMultiplier == DclRational.FromInteger(1) ? [] : [focus.MpCostMultiplier]);
    }

    public static DclCanonicalMagicSnapshotMechanics Resolve(
        DclActionProfile profile,
        string tradition,
        DclCanonicalNativeUnitProjection caster,
        DclCanonicalEquipmentSnapshot casterEquipment,
        DclCanonicalNativeUnitProjection declaredTarget,
        DclCanonicalNativeUnitProjection resolutionTarget,
        DclCanonicalEquipmentSnapshot resolutionTargetEquipment,
        DclPhysicalLocation? effectOwnedLocation = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(casterEquipment);
        ArgumentNullException.ThrowIfNull(declaredTarget);
        ArgumentNullException.ThrowIfNull(resolutionTarget);
        ArgumentNullException.ThrowIfNull(resolutionTargetEquipment);
        if (string.IsNullOrWhiteSpace(tradition)) throw new ArgumentException("A magic tradition identity is required.", nameof(tradition));
        if (caster.Target.Unit != caster.Unit || declaredTarget.Target.Unit != declaredTarget.Unit ||
            resolutionTarget.Target.Unit != resolutionTarget.Unit ||
            declaredTarget.Unit.BattleGeneration != caster.Unit.BattleGeneration ||
            resolutionTarget.Unit.BattleGeneration != caster.Unit.BattleGeneration)
            throw new ArgumentException("Magic snapshot mechanics require stable units from one battle generation.");

        DclCanonicalMagicSourceSnapshotMechanics source = ResolveSource(
            profile,
            tradition,
            caster,
            casterEquipment);

        DclFaithPolicy faithPolicy;
        int magnitudeModifier;
        string? element;
        DclRational? authoredElementBoost;
        switch (profile.MagnitudeProfile)
        {
            case DclDamageMagnitude damage:
                faithPolicy = damage.FaithPolicy;
                magnitudeModifier = source.DamageMagnitudeModifier;
                element = damage.Element;
                authoredElementBoost = damage.ElementBoostMultiplier;
                break;
            case DclHealingMagnitude healing:
                faithPolicy = healing.FaithPolicy;
                magnitudeModifier = source.HealingMagnitudeModifier;
                element = null;
                authoredElementBoost = null;
                break;
            case DclFixedResourceMagnitude:
                faithPolicy = DclFaithPolicy.None;
                magnitudeModifier = 0;
                element = null;
                authoredElementBoost = null;
                break;
            default:
                throw new InvalidOperationException("Magic snapshot mechanics require a normalized damage or healing magnitude profile.");
        }

        bool fire = !string.IsNullOrWhiteSpace(element) && StringComparer.OrdinalIgnoreCase.Equals(element.Trim(), "fire");
        bool oil = fire && resolutionTarget.State.HasNativeEffective(DclCanonicalNativeStatuses.Oil);
        int exposure = string.IsNullOrWhiteSpace(element)
            ? 0
            : resolutionTarget.State.ElementalExposureStep(element);
        var sourceBoosts = new List<DclRational> { source.FocusElementBoostMultiplier };
        if (authoredElementBoost is { } authoredBoost) sourceBoosts.Add(authoredBoost);
        DclElementAffinity affinity = string.IsNullOrWhiteSpace(element)
            ? DclMagicMagnitude.ResolveAffinity(false, false, [], sourceBoosts)
            : resolutionTargetEquipment.ResolveIncomingAffinity(
                element,
                casterEquipment,
                [exposure, oil ? 1 : 0],
                sourceBoosts);

        int applicableDr = profile.DeliveryProfile.ArmorPolicy switch
        {
            DclArmorPolicy.Manifestation or DclArmorPolicy.ArmorDividing =>
                DclCanonicalCombatSnapshotProjector.ResolveApplicableDr(
                    resolutionTargetEquipment,
                    profile.DeliveryProfile.LocationPolicy,
                    effectOwnedLocation,
                    resolutionTarget.State),
            DclArmorPolicy.InternalSpiritual or DclArmorPolicy.IgnoreDr or DclArmorPolicy.None => 0,
            _ => throw new InvalidOperationException("The magic action has no usable armor policy."),
        };
        return new DclCanonicalMagicSnapshotMechanics(
            source.SpellScoreModifier,
            magnitudeModifier,
            affinity,
            DclMagicMagnitude.FaithMagnitude(faithPolicy, caster.CurrentFaith, resolutionTarget.CurrentFaith),
            resolutionTarget.State.HasNativeEffective(DclCanonicalNativeStatuses.Shell),
            declaredTarget.State.HasNativeEffective(DclCanonicalNativeStatuses.Reflect),
            fire,
            oil,
            applicableDr,
            source.ConcentrationModifier,
            source.CastCtModifiers,
            source.MpCostMultipliers);
    }

    private static DclPhysicalLocation ResolveLocation(
        DclLocationPolicy policy,
        DclPhysicalLocation? effectOwnedLocation)
        => policy switch
        {
            DclLocationPolicy.NormalCombined => DclPhysicalLocation.NormalCombined,
            DclLocationPolicy.Body => DclPhysicalLocation.Body,
            DclLocationPolicy.Head => DclPhysicalLocation.Head,
            DclLocationPolicy.EffectOwned when effectOwnedLocation is not null => effectOwnedLocation.Value,
            DclLocationPolicy.EffectOwned => throw new ArgumentException("Effect-owned location requires the resolved physical location.", nameof(effectOwnedLocation)),
            _ => throw new InvalidOperationException("The action has no usable location policy."),
        };
}
