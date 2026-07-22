namespace fftivc.generic.chronicle.codemod;

internal enum DclCanonicalActionFamily
{
    Unknown,
    PhysicalDamage,
    DirectNumeric,
    AreaNumeric,
    StatusApplication,
    StatusRemoval,
    Dispel,
    Quick,
    Revive,
    ForcedMovement,
    NativeSpecialPreserved,
}

internal sealed record DclCanonicalActionCapability(
    DclCanonicalActionFamily Family,
    IReadOnlyList<string> Failures)
{
    public bool Supported => Family != DclCanonicalActionFamily.Unknown && Failures.Count == 0;
}

/// <summary>
/// Proves that one bound normalized profile has one complete forecast/AI/execution owner before a
/// battle can observe it. Schema-valid effect combinations which have no canonical family remain
/// legal authoring vocabulary, but cannot enter the executable runtime until that family exists.
/// </summary>
internal static class DclCanonicalActionCapabilityResolver
{
    public static DclCanonicalActionCapability Resolve(
        DclAbilityBinding binding,
        DclActionProfile profile,
        DclAuthoringRegistry authoring)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(authoring);

        if (binding.CarrierKind == DclNativeCarrierKind.NativeSpecialPreserved &&
            binding.RewritePolicy == DclCarrierRewritePolicy.PreserveNativeSpecial)
            return Supported(DclCanonicalActionFamily.NativeSpecialPreserved);

        DclCanonicalActionFamily family = ClassifyShape(profile);
        if (family == DclCanonicalActionFamily.Unknown)
            return Unsupported("no canonical forecast/AI/execution family consumes this ordered delivery, magnitude, and effect shape");

        var failures = new List<string>();
        ValidateCarrier(family, binding.CarrierKind, failures);
        ValidateRewrite(family, binding.RewritePolicy, failures);
        ValidateFamilyTiming(family, profile, failures);
        ValidateFamilyCardinality(family, profile, failures);
        ValidateAreaBinding(family, binding, profile, failures);
        ValidateAreaStatusCarrier(family, profile, authoring, failures);
        ValidateStatusCarrier(family, profile, authoring, failures);
        ValidateReferencedRiders(family, profile, authoring, failures);
        return failures.Count == 0
            ? Supported(family)
            : new DclCanonicalActionCapability(DclCanonicalActionFamily.Unknown, failures);
    }

    private static DclCanonicalActionFamily ClassifyShape(DclActionProfile profile)
    {
        IReadOnlyList<DclEffectProfile> effects = profile.Effects;
        bool oneUnitStrike = profile.TransactionProfile.StrikeCount == 1 &&
            profile.TargetProfile.TargetMode == DclTargetMode.Unit;

        if (profile.DeliveryProfile.Delivery == DclDelivery.PhysicalAttack &&
            profile.MagnitudeProfile is DclDamageMagnitude &&
            IsCarrierWithStatusRiders(effects, DclEffectKind.Damage))
            return DclCanonicalActionFamily.PhysicalDamage;

        if (profile.DeliveryProfile.Delivery == DclDelivery.Area &&
            profile.TargetProfile.Area is not null &&
            profile.MagnitudeProfile is DclDamageMagnitude or DclHealingMagnitude or DclFixedResourceMagnitude &&
            IsCarrierWithStatusRiders(
                effects,
                profile.MagnitudeProfile switch
                {
                    DclDamageMagnitude => DclEffectKind.Damage,
                    DclHealingMagnitude => DclEffectKind.Healing,
                    DclFixedResourceMagnitude => DclEffectKind.ResourceChange,
                    _ => throw new InvalidOperationException(),
                }))
            return DclCanonicalActionFamily.AreaNumeric;

        if (profile.DeliveryProfile.Delivery == DclDelivery.Area &&
            profile.TargetProfile.Area is not null &&
            profile.TransactionProfile.StrikeCount == 1 &&
            profile.MagnitudeProfile is null && effects.Count == 1 && effects[0] is
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.StatusApplication,
                ReferencedStateKind: not null,
            })
            return DclCanonicalActionFamily.AreaNumeric;

        if (profile.DeliveryProfile.Delivery == DclDelivery.Area &&
            profile.TargetProfile.Area is not null &&
            profile.TransactionProfile.StrikeCount == 1 &&
            profile.MagnitudeProfile is null && profile.ForcedMovementProfile is not null &&
            effects.Count == 1 && effects[0] is
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.ForcedMovement,
            })
            return DclCanonicalActionFamily.AreaNumeric;

        if (oneUnitStrike &&
            profile.DeliveryProfile.Delivery is DclDelivery.ExternalProjectile or DclDelivery.InternalDirect or
                DclDelivery.Touch or DclDelivery.Beneficial &&
            profile.MagnitudeProfile is DclDamageMagnitude or DclHealingMagnitude or DclFixedResourceMagnitude &&
            effects.Count > 0 && effects[0] is
            {
                Role: DclEffectRole.Carrier,
                Kind: DclEffectKind.Damage or DclEffectKind.Healing or DclEffectKind.ResourceChange,
            } && effects.Skip(1).All(IsStatusRider))
            return DclCanonicalActionFamily.DirectNumeric;

        if (oneUnitStrike &&
            profile.DeliveryProfile.Delivery is DclDelivery.InternalDirect or DclDelivery.Beneficial &&
            profile.MagnitudeProfile is null && effects.Count == 1 && effects[0] is
            {
                Kind: DclEffectKind.StatusApplication,
                Role: DclEffectRole.Carrier,
                ReferencedStateKind: not null,
            })
            return DclCanonicalActionFamily.StatusApplication;

        if (oneUnitStrike && profile.DeliveryProfile.Delivery == DclDelivery.Beneficial &&
            profile.MagnitudeProfile is null && effects.Count == 1 && effects[0] is
            {
                Kind: DclEffectKind.StatusRemoval,
                Role: DclEffectRole.Carrier,
                ReferencedStateKind: not null,
            })
            return DclCanonicalActionFamily.StatusRemoval;

        if (oneUnitStrike && profile.MagnitudeProfile is null && profile.DispelProfile is not null &&
            effects.Count == 1 && effects[0] is
            {
                Kind: DclEffectKind.Dispel,
                Role: DclEffectRole.Carrier,
            })
            return DclCanonicalActionFamily.Dispel;

        if (oneUnitStrike && profile.DeliveryProfile.Delivery == DclDelivery.Beneficial &&
            profile.MagnitudeProfile is DclFixedResourceMagnitude { Resource: DclResourceKind.Ct } &&
            effects.Count == 2 && effects[0] is
            {
                Kind: DclEffectKind.CtChange,
                Role: DclEffectRole.Carrier,
            } && effects[1] is
            {
                Kind: DclEffectKind.StatusApplication,
                Role: DclEffectRole.Independent,
                ReferencedStateKind: not null,
            })
            return DclCanonicalActionFamily.Quick;

        if (oneUnitStrike && profile.ReviveProfile is not null &&
            profile.MagnitudeProfile is DclFixedResourceMagnitude { Resource: DclResourceKind.Hp } &&
            effects.Count == 1 && effects[0] is
            {
                Kind: DclEffectKind.Revive,
                Role: DclEffectRole.Carrier,
            })
            return DclCanonicalActionFamily.Revive;

        if (oneUnitStrike &&
            profile.DeliveryProfile.Delivery is DclDelivery.ExternalProjectile or DclDelivery.InternalDirect or
                DclDelivery.Beneficial &&
            profile.MagnitudeProfile is null && profile.ForcedMovementProfile is not null &&
            effects.Count == 1 && effects[0] is
            {
                Kind: DclEffectKind.ForcedMovement,
                Role: DclEffectRole.Carrier,
            })
            return DclCanonicalActionFamily.ForcedMovement;

        return DclCanonicalActionFamily.Unknown;
    }

    private static bool IsCarrierWithStatusRiders(
        IReadOnlyList<DclEffectProfile> effects,
        DclEffectKind carrierKind)
        => effects.Count > 0 && effects[0].Kind == carrierKind && effects[0].Role == DclEffectRole.Carrier &&
           effects.Skip(1).All(IsStatusRider);

    private static bool IsStatusRider(DclEffectProfile effect)
        => effect is
        {
            Kind: DclEffectKind.StatusApplication,
            Role: DclEffectRole.Rider,
            ReferencedStateKind: not null,
        };

    private static void ValidateCarrier(
        DclCanonicalActionFamily family,
        DclNativeCarrierKind carrier,
        List<string> failures)
    {
        bool legal = family switch
        {
            DclCanonicalActionFamily.PhysicalDamage => carrier is
                DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.NativeRepeat,
            DclCanonicalActionFamily.DirectNumeric => carrier == DclNativeCarrierKind.SingleResult,
            DclCanonicalActionFamily.AreaNumeric => carrier is
                DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.NativeRepeat or
                DclNativeCarrierKind.StatusPacket or DclNativeCarrierKind.ConditionalStatusProducer,
            DclCanonicalActionFamily.StatusApplication or DclCanonicalActionFamily.StatusRemoval or
                DclCanonicalActionFamily.Dispel => carrier is
                DclNativeCarrierKind.StatusPacket or DclNativeCarrierKind.ConditionalStatusProducer or
                DclNativeCarrierKind.SingleResult,
            DclCanonicalActionFamily.Quick => carrier is
                DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket,
            DclCanonicalActionFamily.Revive => carrier is
                DclNativeCarrierKind.LifecycleTransaction or DclNativeCarrierKind.SingleResult or
                DclNativeCarrierKind.StatusPacket,
            DclCanonicalActionFamily.ForcedMovement => carrier is
                DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket,
            _ => false,
        };
        if (!legal)
            failures.Add($"native carrier {carrier} cannot preserve the {family} family identity/cardinality");
    }

    private static void ValidateFamilyTiming(
        DclCanonicalActionFamily family,
        DclActionProfile profile,
        List<string> failures)
    {
        if (family is DclCanonicalActionFamily.PhysicalDamage or DclCanonicalActionFamily.AreaNumeric &&
            profile.Effects.Count > 1 && profile.TransactionProfile.StrikeCount > 1 &&
            profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate)
            failures.Add("multi-Strike Immediate status Riders have no canonical between-Strike state reprojection owner");
    }

    private static void ValidateFamilyCardinality(
        DclCanonicalActionFamily family,
        DclActionProfile profile,
        List<string> failures)
    {
        bool targetStrikeMagnitude =
            profile.TransactionProfile.MagnitudeRollCardinality == DclMagnitudeRollCardinality.PerTargetPerStrike ||
            profile.TransactionProfile.StrikeCount == 1 &&
            profile.TransactionProfile.MagnitudeRollCardinality == DclMagnitudeRollCardinality.PerTarget;
        if (family is DclCanonicalActionFamily.PhysicalDamage or DclCanonicalActionFamily.AreaNumeric &&
            profile.MagnitudeProfile is not null &&
            !targetStrikeMagnitude)
            failures.Add(
                $"generic {family} owns PerTargetPerStrike, or equivalent one-Strike PerTarget, magnitude only; " +
                $"{profile.TransactionProfile.MagnitudeRollCardinality} requires a named cardinality executor");
        if (family == DclCanonicalActionFamily.DirectNumeric &&
            profile.TransactionProfile.MagnitudeRollCardinality == DclMagnitudeRollCardinality.Explicit)
            failures.Add("generic DirectNumeric cannot execute an Explicit magnitude cardinality without its named owner");
    }

    private static void ValidateAreaBinding(
        DclCanonicalActionFamily family,
        DclAbilityBinding binding,
        DclActionProfile profile,
        List<string> failures)
    {
        if (family != DclCanonicalActionFamily.AreaNumeric) return;
        bool statusOnly = profile.MagnitudeProfile is null && profile.Effects.Count == 1 &&
            profile.Effects[0].Kind == DclEffectKind.StatusApplication;
        bool forcedMovementOnly = profile.MagnitudeProfile is null && profile.Effects.Count == 1 &&
            profile.Effects[0].Kind == DclEffectKind.ForcedMovement;
        bool carrierLegal = statusOnly
            ? binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket or
                DclNativeCarrierKind.ConditionalStatusProducer
            : forcedMovementOnly
                ? binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.StatusPacket
                : binding.CarrierKind is DclNativeCarrierKind.SingleResult or DclNativeCarrierKind.NativeRepeat;
        bool rewriteLegal = statusOnly
            ? binding.RewritePolicy is DclCarrierRewritePolicy.ReplaceCompleteResult or
                DclCarrierRewritePolicy.ReplaceStatusPacket or DclCarrierRewritePolicy.ManagedProducer
            : forcedMovementOnly
                ? binding.RewritePolicy is DclCarrierRewritePolicy.ReplaceCompleteResult or
                    DclCarrierRewritePolicy.ReplaceStatusPacket
                : binding.RewritePolicy is DclCarrierRewritePolicy.ReplaceCompleteResult or
                    DclCarrierRewritePolicy.ReplaceNumericResult;
        if (!carrierLegal)
            failures.Add($"native carrier {binding.CarrierKind} cannot preserve this Area Carrier shape");
        if (!rewriteLegal)
            failures.Add($"rewrite policy {binding.RewritePolicy} cannot preserve this Area Carrier shape");
    }

    private static void ValidateRewrite(
        DclCanonicalActionFamily family,
        DclCarrierRewritePolicy rewrite,
        List<string> failures)
    {
        bool legal = family switch
        {
            DclCanonicalActionFamily.PhysicalDamage or DclCanonicalActionFamily.DirectNumeric => rewrite is
                DclCarrierRewritePolicy.ReplaceCompleteResult or DclCarrierRewritePolicy.ReplaceNumericResult,
            DclCanonicalActionFamily.AreaNumeric => rewrite is
                DclCarrierRewritePolicy.ReplaceCompleteResult or DclCarrierRewritePolicy.ReplaceNumericResult or
                DclCarrierRewritePolicy.ReplaceStatusPacket or DclCarrierRewritePolicy.ManagedProducer,
            DclCanonicalActionFamily.StatusApplication or DclCanonicalActionFamily.StatusRemoval or
                DclCanonicalActionFamily.Dispel => rewrite is
                DclCarrierRewritePolicy.ReplaceCompleteResult or DclCarrierRewritePolicy.ReplaceStatusPacket or
                DclCarrierRewritePolicy.ManagedProducer,
            DclCanonicalActionFamily.Quick or DclCanonicalActionFamily.Revive or
                DclCanonicalActionFamily.ForcedMovement => rewrite is
                DclCarrierRewritePolicy.ReplaceCompleteResult or DclCarrierRewritePolicy.ReplaceStatusPacket,
            _ => false,
        };
        if (!legal)
            failures.Add($"rewrite policy {rewrite} cannot publish the {family} family result");
    }

    private static void ValidateReferencedRiders(
        DclCanonicalActionFamily family,
        DclActionProfile profile,
        DclAuthoringRegistry authoring,
        List<string> failures)
    {
        if (family is not (DclCanonicalActionFamily.PhysicalDamage or DclCanonicalActionFamily.DirectNumeric or
            DclCanonicalActionFamily.AreaNumeric))
            return;
        for (int index = 1; index < profile.Effects.Count; index++)
        {
            string stateKind = profile.Effects[index].ReferencedStateKind!;
            if (!authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition))
            {
                failures.Add($"Rider effect {index} lost referenced state '{stateKind}'");
                continue;
            }
            if (definition.ResistanceGate == DclStateResistanceGate.Explicit)
                failures.Add($"Rider effect {index} uses Explicit resistance without a named family owner");
        }
    }

    private static void ValidateStatusCarrier(
        DclCanonicalActionFamily family,
        DclActionProfile profile,
        DclAuthoringRegistry authoring,
        List<string> failures)
    {
        if (family != DclCanonicalActionFamily.StatusApplication)
            return;
        string stateKind = profile.Effects[0].ReferencedStateKind!;
        if (!authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition))
        {
            failures.Add($"status Carrier lost referenced state '{stateKind}'");
            return;
        }
        DclStateResistanceGate expected = profile.DeliveryProfile.Delivery == DclDelivery.InternalDirect
            ? DclStateResistanceGate.QuickContest
            : DclStateResistanceGate.None;
        if (definition.ResistanceGate != expected)
            failures.Add(
                $"status Carrier delivery {profile.DeliveryProfile.Delivery} requires state gate {expected}, " +
                $"not {definition.ResistanceGate}");
    }

    private static void ValidateAreaStatusCarrier(
        DclCanonicalActionFamily family,
        DclActionProfile profile,
        DclAuthoringRegistry authoring,
        List<string> failures)
    {
        if (family != DclCanonicalActionFamily.AreaNumeric || profile.MagnitudeProfile is not null ||
            profile.Effects.Count != 1 || profile.Effects[0].Kind != DclEffectKind.StatusApplication)
            return;
        string stateKind = profile.Effects[0].ReferencedStateKind!;
        if (!authoring.States.TryGetValue(stateKind, out DclStateDefinition? definition))
        {
            failures.Add($"Area status Carrier lost referenced state '{stateKind}'");
            return;
        }
        DclStateResistanceGate expected = profile.TargetProfile.Area!.DeliveryGate == DclAreaDeliveryGate.QuickContest
            ? DclStateResistanceGate.QuickContest
            : DclStateResistanceGate.None;
        if (definition.ResistanceGate != expected)
            failures.Add(
                $"Area status Carrier delivery gate {profile.TargetProfile.Area.DeliveryGate} requires state gate {expected}, " +
                $"not {definition.ResistanceGate}");
    }

    private static DclCanonicalActionCapability Supported(DclCanonicalActionFamily family)
        => new(family, []);

    private static DclCanonicalActionCapability Unsupported(string failure)
        => new(DclCanonicalActionFamily.Unknown, [failure]);
}
