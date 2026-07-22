using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

internal enum DclActionSource { Unknown, Physical, Spell, Ki, Divine, Spiritual, Equipment, MonsterPower, PeriodicEffect, Other }
internal enum DclOvercastPolicy { Unknown, Allowed, Forbidden }
internal enum DclTargetMode { Unknown, Unit, FixedTile, Caster }
internal enum DclAllegiancePolicy { Unknown, Everyone, AlliesOnly, EnemiesOnly, CasterSide, Explicit }
internal enum DclPhysicalRoute { Unknown, NativeDirect, NativeArc, None }
internal enum DclAreaCenterMode { Unknown, TrackedUnit, FixedTile, Caster }
internal enum DclAreaShape { Unknown, Diamond, Square, Line, Cone, Cross, Ring, Explicit }
internal enum DclAreaDeliveryGate { Unknown, None, Dodge, QuickContest }
internal enum DclDelivery { Unknown, PhysicalAttack, ExternalProjectile, InternalDirect, Touch, Area, Rider, Beneficial, Other }
internal enum DclResistanceCharacteristic { None, Ht, Will, SpiritualResistance, Authored }
internal enum DclArmorPolicy { Unknown, Manifestation, ArmorDividing, InternalSpiritual, IgnoreDr, None }
internal enum DclLocationPolicy { Unknown, NormalCombined, Body, Head, EffectOwned }
internal enum DclDamageBasis { Unknown, Thrust, Swing, Fixed }
internal enum DclFaithPolicy { Unknown, None, Caster, Target, Both }
internal enum DclResourceKind { Unknown, Hp, Mp, Ct, Other }
internal enum DclResourceChangeRoute { Unknown, TargetCredit, TargetDebit, DrainTargetToSource }
internal enum DclForcedMovementDirection { Unknown, AwayFromSource, TowardSource, SourceFacing, ExplicitVector }
internal enum DclEffectKind { Unknown, Damage, Healing, ResourceChange, StatusApplication, StatusRemoval, CtChange, Dispel, Revive, ForcedMovement, Reequip, MechanismOwned }
internal enum DclEffectRole { Unknown, Carrier, Rider, Independent }
internal enum DclUndeadInteraction { Unknown, Normal, Reverse, Harm, Heal, Reject, EffectOwned }
internal enum DclCastingRollCardinality { Unknown, PerAction, None }
internal enum DclTargetGateCardinality { Unknown, PerAction, PerTarget, PerStrike, Explicit }
internal enum DclMagnitudeRollCardinality { Unknown, PerTargetPerStrike, PerTarget, Shared, Explicit }
internal enum DclWithinActionApplication { Unknown, Deferred, Immediate }
internal enum DclCriticalSuccessEffect { Unknown, DeliveryDefault, MaximizeOneHealingDie, Explicit, None }
internal enum DclCriticalFailureEffect { Unknown, MissOnly, ExplicitDeterministic }
internal enum DclLegalUsePolicy { Unknown, Standard, Explicit }
internal enum DclExpectedValuePolicy { Unknown, Exact, Explicit }
internal enum DclFriendlyFirePolicy { Unknown, Forbidden, Allowed, Penalized, Explicit }
internal enum DclDispelScope { Unknown, OneInstance, AllEligible }
internal enum DclStateResistanceGate { Unknown, None, SuccessRoll, QuickContest, Explicit }
internal enum DclStateStackPolicy { Unknown, Replace, Refresh, StrongestWins, StackToCap, Independent, Explicit }
internal enum DclStateDurationClock { Unknown, GlobalCt, TargetTurn, SourceTurn, UsesOrTriggers, ExplicitCommand, Permanent, Explicit }
internal enum DclStateTickSource { Unknown, OriginalSource, Target }

[Flags]
internal enum DclEligibleTargetStates
{
    None = 0,
    Alive = 1,
    Ko = 2,
    Undead = 4,
    CrystalOrTreasure = 8,
    Explicit = 16,
}

internal sealed record DclSourceProfile(DclActionSource Source, bool Verbal);

internal sealed record DclSkillProfile(
    int? GoverningSkillId,
    int SkillModifier,
    int? SourceJobId,
    bool ZodiacSensitive);

internal sealed record DclResourceProfile(
    int BaseMpCost,
    DclRational MpCostMultiplier,
    DclOvercastPolicy OvercastPolicy);

internal sealed record DclTimingProfile(
    bool ConsumesAction,
    bool ConsumesMovement,
    int BaseCastCt,
    int CastCtModifier,
    bool ConcentrationRequired);

internal sealed record DclAreaProfile(
    DclAreaCenterMode CenterMode,
    DclAreaShape Shape,
    int Size,
    DclAreaDeliveryGate DeliveryGate);

internal sealed record DclTargetProfile(
    DclTargetMode TargetMode,
    DclAllegiancePolicy AllegiancePolicy,
    DclEligibleTargetStates EligibleTargetStates,
    int RangeMin,
    int RangeMax,
    int VerticalTolerance,
    bool VisionRequired,
    DclPhysicalRoute PhysicalRoute,
    DclAreaProfile? Area);

internal sealed record DclDeliveryProfile(
    DclDelivery Delivery,
    bool Dodgeable,
    bool Parryable,
    bool Blockable,
    bool UsesDefenseBonus,
    DclResistanceCharacteristic? ResistanceCharacteristic,
    DclArmorPolicy ArmorPolicy,
    DclRational? ArmorDivisor,
    DclLocationPolicy LocationPolicy,
    bool Reflectable,
    bool ShellSensitive);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "magnitudeKind")]
[JsonDerivedType(typeof(DclDamageMagnitude), "damage")]
[JsonDerivedType(typeof(DclHealingMagnitude), "healing")]
[JsonDerivedType(typeof(DclFixedResourceMagnitude), "fixedResource")]
internal abstract record DclMagnitudeProfile;

internal sealed record DclDamageMagnitude(
    DclDamageBasis Basis,
    string? FixedExpression,
    int IntegerModifier,
    int WholeDiceModifier,
    DclDamageType DamageType,
    string? Element,
    DclRational? ElementBoostMultiplier,
    DclFaithPolicy FaithPolicy) : DclMagnitudeProfile;

internal sealed record DclHealingMagnitude(
    DclDamageBasis Basis,
    string? FixedExpression,
    int IntegerModifier,
    int WholeDiceModifier,
    DclFaithPolicy FaithPolicy) : DclMagnitudeProfile;

internal sealed record DclFixedResourceMagnitude(
    DclResourceKind Resource,
    string Expression) : DclMagnitudeProfile;

internal sealed record DclResourceChangeProfile(
    DclResourceChangeRoute Route,
    DclUndeadInteraction SourceUndeadInteraction);

internal sealed record DclForcedMovementProfile(
    int DistanceTiles,
    DclForcedMovementDirection Direction,
    int? DeltaX = null,
    int? DeltaY = null);

internal sealed record DclEffectProfile(
    DclEffectKind Kind,
    DclEffectRole Role,
    DclEligibleTargetStates EligibleTargetStates,
    DclUndeadInteraction UndeadInteraction,
    string? ReferencedStateKind = null,
    string? MechanismId = null);

internal sealed record DclTransactionProfile(
    int StrikeCount,
    DclCastingRollCardinality CastingRollCardinality,
    DclTargetGateCardinality TargetGateCardinality,
    DclMagnitudeRollCardinality MagnitudeRollCardinality,
    DclWithinActionApplication WithinActionApplication);

internal sealed record DclCriticalProfile(
    DclCriticalSuccessEffect SuccessEffect,
    DclCriticalFailureEffect FailureEffect);

internal sealed record DclPresentationProfile(
    string ActionAnimation,
    string? ChargePresentation,
    string ResultText,
    IReadOnlyList<string> ForecastTerms,
    IReadOnlyList<string> StatePresentationIds);

internal sealed record DclAiProfile(
    DclLegalUsePolicy LegalUsePolicy,
    IReadOnlyList<string> OutcomeTags,
    DclExpectedValuePolicy ExpectedValuePolicy,
    DclFriendlyFirePolicy FriendlyFirePolicy);

internal sealed record DclActionProfile(
    string ActionId,
    int ProfileRevision,
    DclSourceProfile SourceProfile,
    DclSkillProfile SkillProfile,
    DclResourceProfile ResourceProfile,
    DclTimingProfile TimingProfile,
    DclTargetProfile TargetProfile,
    DclDeliveryProfile DeliveryProfile,
    DclMagnitudeProfile? MagnitudeProfile,
    IReadOnlyList<DclEffectProfile> Effects,
    DclTransactionProfile TransactionProfile,
    DclCriticalProfile CriticalProfile,
    DclPresentationProfile PresentationProfile,
    DclAiProfile AiProfile,
    DclReviveProfile? ReviveProfile = null,
    DclDispelProfile? DispelProfile = null,
    DclResourceChangeProfile? ResourceChangeProfile = null,
    DclForcedMovementProfile? ForcedMovementProfile = null);

internal sealed record DclDispelProfile(
    DclDispelScope Scope,
    IReadOnlyList<string> EligibleCureFamilies,
    bool SourceMatchedOnly);

internal sealed record DclStateDurationProfile(
    DclStateDurationClock Clock,
    string? Formula);

internal sealed record DclStateTickProfile(
    int Interval,
    string EffectActionId,
    int EffectAbilityId,
    DclStateTickSource ActionSource,
    bool ImmediatePayload);

internal sealed record DclStateDefinition(
    string Kind,
    DclNativeStatusBit? NativeStatusBit,
    bool SourceRequired,
    DclStateResistanceGate ResistanceGate,
    string ImmunityFamily,
    DclStateStackPolicy StackPolicy,
    string StackKey,
    int? StackCap,
    string? ContributionKey,
    DclStateStackPolicy? ContributionReapplicationPolicy,
    DclStateDurationProfile Duration,
    DclStateTickProfile? TickProfile,
    string? EffectStrengthFormula,
    bool RemoveOnTargetKo,
    bool RemoveOnSourceKo,
    bool RemoveOnSourceLoss,
    IReadOnlyList<string> CureFamilies,
    string PayloadSchema,
    string MechanicalRules,
    string PresentationProfile,
    string AiProfile);

internal sealed record DclAuthoringFinding(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}

internal sealed class DclAuthoringValidation
{
    private readonly List<DclAuthoringFinding> _findings = new();
    public IReadOnlyList<DclAuthoringFinding> Findings => _findings;
    public bool IsValid => _findings.Count == 0;
    internal void Error(string path, string message) => _findings.Add(new(path, message));
}

internal static class DclAuthoringContract
{
    private static readonly DclRational Zero = DclRational.FromInteger(0);

    public static DclAuthoringValidation Validate(DclActionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var result = new DclAuthoringValidation();
        RequireText(result, "ActionId", profile.ActionId);
        if (profile.ProfileRevision <= 0) result.Error("ProfileRevision", "must be positive");

        if (profile.SourceProfile.Source == DclActionSource.Unknown)
            result.Error("SourceProfile.Source", "must be explicit");
        if (profile.SkillProfile.GoverningSkillId is < 0)
            result.Error("SkillProfile.GoverningSkillId", "cannot be negative");
        if (profile.SkillProfile.SourceJobId is < 0)
            result.Error("SkillProfile.SourceJobId", "cannot be negative");

        ValidateResource(profile.ResourceProfile, result);
        ValidateTiming(profile.SourceProfile, profile.TimingProfile, result);
        ValidateTarget(profile.TargetProfile, result);
        ValidateDelivery(profile.DeliveryProfile, result);
        ValidateMagnitude(profile.MagnitudeProfile, result);
        ValidateEffects(profile.Effects, result);
        ValidateTransaction(profile.TransactionProfile, profile.TargetProfile, profile.DeliveryProfile, result);
        ValidateCritical(profile.CriticalProfile, result);
        ValidatePresentation(profile.PresentationProfile, result);
        ValidateAi(profile.AiProfile, result);
        ValidateCrossProfile(profile, result);
        return result;
    }

    public static DclAuthoringValidation Validate(DclStateDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var result = new DclAuthoringValidation();
        RequireText(result, "Kind", definition.Kind);
        if (definition.NativeStatusBit is { ByteIndex: < 0 or >= DclStatusPacket.Width })
            result.Error("NativeStatusBit.ByteIndex", "must address the fixed native five-byte vocabulary");
        if (definition.NativeStatusBit is { } nativeBit &&
            (nativeBit.Mask == 0 || (nativeBit.Mask & (nativeBit.Mask - 1)) != 0))
            result.Error("NativeStatusBit.Mask", "must identify exactly one native status bit");
        if (definition.ResistanceGate == DclStateResistanceGate.Unknown)
            result.Error("ResistanceGate", "must be explicit, including None");
        RequireText(result, "ImmunityFamily", definition.ImmunityFamily);
        if (definition.StackPolicy == DclStateStackPolicy.Unknown)
            result.Error("StackPolicy", "must be explicit");
        RequireText(result, "StackKey", definition.StackKey);

        if (definition.StackPolicy == DclStateStackPolicy.StackToCap)
        {
            if (definition.StackCap is null or <= 0)
                result.Error("StackCap", "StackToCap requires a positive cap");
            RequireText(result, "ContributionKey", definition.ContributionKey);
            if (definition.ContributionReapplicationPolicy is not (DclStateStackPolicy.Refresh or DclStateStackPolicy.Replace))
                result.Error("ContributionReapplicationPolicy", "StackToCap requires Refresh or Replace for the same contribution");
        }
        else
        {
            if (definition.StackCap is not null)
                result.Error("StackCap", "is legal only for StackToCap or a named extension");
            if (!string.IsNullOrWhiteSpace(definition.ContributionKey))
                result.Error("ContributionKey", "is legal only for StackToCap or a named extension");
            if (definition.ContributionReapplicationPolicy is not null)
                result.Error("ContributionReapplicationPolicy", "is legal only for StackToCap or a named extension");
        }

        ValidateDuration(definition.Duration, result);
        if (definition.TickProfile is { } tick)
        {
            if (tick.Interval <= 0) result.Error("TickProfile.Interval", "must be positive");
            RequireText(result, "TickProfile.EffectActionId", tick.EffectActionId);
            if (tick.EffectAbilityId is < 0 or > 511)
                result.Error("TickProfile.EffectAbilityId", "must identify one native carrier ability in 0..511");
            if (tick.ActionSource == DclStateTickSource.Unknown)
                result.Error("TickProfile.ActionSource", "must be explicit");
            if (tick.ActionSource == DclStateTickSource.OriginalSource &&
                (!definition.SourceRequired || !definition.RemoveOnSourceLoss))
                result.Error(
                    "TickProfile.ActionSource",
                    "OriginalSource requires SourceRequired and RemoveOnSourceLoss so every future tick retains a valid UnitKey");
            if (definition.Duration.Clock != DclStateDurationClock.GlobalCt)
                result.Error("TickProfile", "periodic ticks require the GlobalCt duration clock");
        }
        RequireText(result, "PayloadSchema", definition.PayloadSchema);
        RequireText(result, "MechanicalRules", definition.MechanicalRules);
        RequireText(result, "PresentationProfile", definition.PresentationProfile);
        RequireText(result, "AiProfile", definition.AiProfile);
        RequireList(result, "CureFamilies", definition.CureFamilies, allowEmpty: true);
        return result;
    }

    private static void ValidateResource(DclResourceProfile profile, DclAuthoringValidation result)
    {
        if (profile.BaseMpCost < 0) result.Error("ResourceProfile.BaseMpCost", "cannot be negative");
        if (profile.MpCostMultiplier <= Zero)
            result.Error("ResourceProfile.MpCostMultiplier", "must be an exact positive rational");
        if (profile.OvercastPolicy == DclOvercastPolicy.Unknown)
            result.Error("ResourceProfile.OvercastPolicy", "must be explicit");
        if (profile.BaseMpCost == 0 && profile.OvercastPolicy != DclOvercastPolicy.Forbidden)
            result.Error("ResourceProfile.OvercastPolicy", "zero-cost actions normalize to Forbidden");
    }

    private static void ValidateTiming(
        DclSourceProfile source,
        DclTimingProfile profile,
        DclAuthoringValidation result)
    {
        if (profile.BaseCastCt < 0) result.Error("TimingProfile.BaseCastCt", "cannot be negative");
        if (source.Source == DclActionSource.PeriodicEffect)
        {
            if (profile.ConsumesAction || profile.ConsumesMovement || profile.BaseCastCt != 0 ||
                profile.CastCtModifier != 0 || profile.ConcentrationRequired)
                result.Error("TimingProfile", "PeriodicEffect actions consume no turn resource, CastCT, or concentration");
        }
        else if (!profile.ConsumesAction && !profile.ConsumesMovement)
            result.Error("TimingProfile", "must consume an explicitly supported turn resource");
    }

    private static void ValidateTarget(DclTargetProfile profile, DclAuthoringValidation result)
    {
        if (profile.TargetMode == DclTargetMode.Unknown) result.Error("TargetProfile.TargetMode", "must be explicit");
        if (profile.AllegiancePolicy == DclAllegiancePolicy.Unknown) result.Error("TargetProfile.AllegiancePolicy", "must be explicit");
        if (profile.EligibleTargetStates == DclEligibleTargetStates.None) result.Error("TargetProfile.EligibleTargetStates", "cannot be empty");
        if (profile.RangeMin < 0 || profile.RangeMax < 0 || profile.VerticalTolerance < 0)
            result.Error("TargetProfile", "range and vertical tolerance cannot be negative");
        if (profile.RangeMin > profile.RangeMax) result.Error("TargetProfile.RangeMin", "cannot exceed RangeMax");
        if (profile.PhysicalRoute == DclPhysicalRoute.Unknown) result.Error("TargetProfile.PhysicalRoute", "must be explicit, including None");
        if (profile.Area is { } area)
        {
            if (area.CenterMode == DclAreaCenterMode.Unknown) result.Error("TargetProfile.Area.CenterMode", "must be explicit");
            if (area.Shape == DclAreaShape.Unknown) result.Error("TargetProfile.Area.Shape", "must be explicit");
            if (area.Size < 0) result.Error("TargetProfile.Area.Size", "cannot be negative");
            if (area.DeliveryGate == DclAreaDeliveryGate.Unknown) result.Error("TargetProfile.Area.DeliveryGate", "must be explicit");
            bool centerMatchesTarget = area.CenterMode switch
            {
                DclAreaCenterMode.TrackedUnit => profile.TargetMode == DclTargetMode.Unit,
                DclAreaCenterMode.FixedTile => profile.TargetMode == DclTargetMode.FixedTile,
                DclAreaCenterMode.Caster => profile.TargetMode == DclTargetMode.Caster,
                _ => false,
            };
            if (!centerMatchesTarget)
                result.Error("TargetProfile.Area.CenterMode", "must match the authored tracked-unit, fixed-tile, or caster target mode");
        }
    }

    private static void ValidateDelivery(DclDeliveryProfile profile, DclAuthoringValidation result)
    {
        if (profile.Delivery == DclDelivery.Unknown) result.Error("DeliveryProfile.Delivery", "must be explicit");
        if (profile.ArmorPolicy == DclArmorPolicy.Unknown) result.Error("DeliveryProfile.ArmorPolicy", "must be explicit, including None");
        if (profile.LocationPolicy == DclLocationPolicy.Unknown) result.Error("DeliveryProfile.LocationPolicy", "must be explicit");
        if (profile.Delivery == DclDelivery.InternalDirect && (profile.Dodgeable || profile.Parryable || profile.Blockable))
            result.Error("DeliveryProfile", "InternalDirect rejects Dodge, Parry, and Block");
        if (profile.Delivery == DclDelivery.InternalDirect && profile.ResistanceCharacteristic is null or DclResistanceCharacteristic.None)
            result.Error("DeliveryProfile.ResistanceCharacteristic", "InternalDirect requires an authored resistance characteristic");
        if (profile.Delivery == DclDelivery.Touch && profile.Blockable)
            result.Error("DeliveryProfile.Blockable", "Touch permits only its explicitly authored Dodge and Parry defenses");
        if (profile.Delivery == DclDelivery.ExternalProjectile && profile.Parryable)
            result.Error("DeliveryProfile.Parryable", "ExternalProjectile permits only its explicitly authored Dodge and Block defenses");
        if (profile.Delivery == DclDelivery.Beneficial &&
            (profile.Dodgeable || profile.Parryable || profile.Blockable))
            result.Error("DeliveryProfile", "Beneficial delivery rejects hostile active defenses");
        if (profile.Delivery == DclDelivery.Rider &&
            (profile.Dodgeable || profile.Parryable || profile.Blockable))
            result.Error("DeliveryProfile", "Rider delivery reuses its carrier and rejects a second active defense");
        if (profile.Delivery is (DclDelivery.PhysicalAttack or DclDelivery.ExternalProjectile or
            DclDelivery.Touch or DclDelivery.Beneficial) &&
            profile.ResistanceCharacteristic is not (null or DclResistanceCharacteristic.None))
            result.Error("DeliveryProfile.ResistanceCharacteristic", "this delivery class does not own a resistance gate");
        if (profile.Blockable && !profile.UsesDefenseBonus)
            result.Error("DeliveryProfile.UsesDefenseBonus", "Block requires an explicit Defense Bonus policy");
        if (profile.ArmorPolicy == DclArmorPolicy.ArmorDividing)
        {
            if (profile.ArmorDivisor is null || profile.ArmorDivisor <= Zero)
                result.Error("DeliveryProfile.ArmorDivisor", "ArmorDividing requires a positive divisor");
        }
        else if (profile.ArmorDivisor is not null)
        {
            result.Error("DeliveryProfile.ArmorDivisor", "is legal only with ArmorDividing");
        }
    }

    private static void ValidateMagnitude(DclMagnitudeProfile? profile, DclAuthoringValidation result)
    {
        switch (profile)
        {
            case null:
                return;
            case DclDamageMagnitude damage:
                ValidateBasis("MagnitudeProfile", damage.Basis, damage.FixedExpression, result);
                if (damage.DamageType == DclDamageType.Unknown)
                    result.Error("MagnitudeProfile.DamageType", "must be one canonical wound-multiplier type");
                if (damage.FaithPolicy == DclFaithPolicy.Unknown) result.Error("MagnitudeProfile.FaithPolicy", "must be explicit");
                if (damage.ElementBoostMultiplier is { } boost && boost <= Zero)
                    result.Error("MagnitudeProfile.ElementBoostMultiplier", "must be positive");
                break;
            case DclHealingMagnitude healing:
                ValidateBasis("MagnitudeProfile", healing.Basis, healing.FixedExpression, result);
                if (healing.FaithPolicy == DclFaithPolicy.Unknown) result.Error("MagnitudeProfile.FaithPolicy", "must be explicit");
                break;
            case DclFixedResourceMagnitude resource:
                if (resource.Resource == DclResourceKind.Unknown) result.Error("MagnitudeProfile.Resource", "must be explicit");
                RequireText(result, "MagnitudeProfile.Expression", resource.Expression);
                break;
            default:
                result.Error("MagnitudeProfile", "has an unknown union member");
                break;
        }
    }

    private static void ValidateBasis(string path, DclDamageBasis basis, string? expression, DclAuthoringValidation result)
    {
        if (basis == DclDamageBasis.Unknown) result.Error($"{path}.Basis", "must be explicit");
        if (basis == DclDamageBasis.Fixed)
        {
            RequireText(result, $"{path}.FixedExpression", expression);
            if (!string.IsNullOrWhiteSpace(expression) && !DclDiceExpression.TryParseAuthored(expression, out _))
                result.Error($"{path}.FixedExpression", "must use the exact Xd6+Y grammar without whitespace");
        }
        else if (!string.IsNullOrWhiteSpace(expression)) result.Error($"{path}.FixedExpression", "is legal only for Fixed basis");
    }

    private static void ValidateEffects(IReadOnlyList<DclEffectProfile> effects, DclAuthoringValidation result)
    {
        if (effects is null || effects.Count == 0)
        {
            result.Error("Effects", "must contain at least one typed effect");
            return;
        }
        for (int index = 0; index < effects.Count; index++)
        {
            DclEffectProfile effect = effects[index];
            string path = $"Effects[{index}]";
            if (effect.Kind == DclEffectKind.Unknown) result.Error($"{path}.Kind", "must be explicit");
            if (effect.Role == DclEffectRole.Unknown) result.Error($"{path}.Role", "must be explicit");
            if (effect.EligibleTargetStates == DclEligibleTargetStates.None) result.Error($"{path}.EligibleTargetStates", "cannot be empty");
            if (effect.UndeadInteraction == DclUndeadInteraction.Unknown) result.Error($"{path}.UndeadInteraction", "must be explicit");
            if (effect.Kind is DclEffectKind.StatusApplication or DclEffectKind.StatusRemoval)
                RequireText(result, $"{path}.ReferencedStateKind", effect.ReferencedStateKind);
            else if (effect.Kind != DclEffectKind.Revive && !string.IsNullOrWhiteSpace(effect.ReferencedStateKind))
                result.Error($"{path}.ReferencedStateKind", "is legal only for a status or stored Revive effect");
            if (effect.Kind == DclEffectKind.MechanismOwned)
                RequireText(result, $"{path}.MechanismId", effect.MechanismId);
            else if (!string.IsNullOrWhiteSpace(effect.MechanismId))
                result.Error($"{path}.MechanismId", "is legal only for MechanismOwned");
        }
    }

    private static void ValidateTransaction(
        DclTransactionProfile profile,
        DclTargetProfile target,
        DclDeliveryProfile delivery,
        DclAuthoringValidation result)
    {
        if (profile.StrikeCount <= 0) result.Error("TransactionProfile.StrikeCount", "must be positive");
        if (profile.CastingRollCardinality == DclCastingRollCardinality.Unknown) result.Error("TransactionProfile.CastingRollCardinality", "must be explicit");
        if (profile.TargetGateCardinality == DclTargetGateCardinality.Unknown) result.Error("TransactionProfile.TargetGateCardinality", "must be explicit");
        if (profile.MagnitudeRollCardinality == DclMagnitudeRollCardinality.Unknown) result.Error("TransactionProfile.MagnitudeRollCardinality", "must be explicit");
        if (profile.WithinActionApplication == DclWithinActionApplication.Unknown) result.Error("TransactionProfile.WithinActionApplication", "must be explicit");
        bool castingDelivery = delivery.Delivery is DclDelivery.ExternalProjectile or DclDelivery.InternalDirect or DclDelivery.Area or DclDelivery.Beneficial;
        if (castingDelivery && profile.CastingRollCardinality == DclCastingRollCardinality.None)
            result.Error("TransactionProfile.CastingRollCardinality", "the selected delivery requires its outer casting draw");
        if (target.Area is null && delivery.Delivery == DclDelivery.Area)
            result.Error("TargetProfile.Area", "Area delivery requires an authored area profile");
        if (target.Area is { } area)
        {
            if (delivery.Delivery != DclDelivery.Area)
                result.Error("DeliveryProfile.Delivery", "an authored area requires Area delivery");
            if (profile.CastingRollCardinality != DclCastingRollCardinality.PerAction)
                result.Error("TransactionProfile.CastingRollCardinality", "area magic shares one casting roll per outer action");
            switch (area.DeliveryGate)
            {
                case DclAreaDeliveryGate.None:
                    if (delivery.Dodgeable || delivery.Parryable || delivery.Blockable)
                        result.Error("DeliveryProfile", "AreaDeliveryGate None rejects active defenses");
                    if (profile.TargetGateCardinality != DclTargetGateCardinality.PerTarget)
                        result.Error("TransactionProfile.TargetGateCardinality", "AreaDeliveryGate None retains one target-relative casting classification per target");
                    break;
                case DclAreaDeliveryGate.Dodge:
                    if (!delivery.Dodgeable || delivery.Parryable || delivery.Blockable)
                        result.Error("DeliveryProfile", "AreaDeliveryGate Dodge permits Dodge only");
                    if (profile.TargetGateCardinality != DclTargetGateCardinality.PerStrike)
                        result.Error("TransactionProfile.TargetGateCardinality", "AreaDeliveryGate Dodge requires one gate per target per Strike");
                    break;
                case DclAreaDeliveryGate.QuickContest:
                    if (delivery.Dodgeable || delivery.Parryable || delivery.Blockable ||
                        delivery.ResistanceCharacteristic is null or DclResistanceCharacteristic.None)
                        result.Error("DeliveryProfile", "AreaDeliveryGate QuickContest requires resistance and rejects active defenses");
                    if (profile.TargetGateCardinality != DclTargetGateCardinality.PerTarget)
                        result.Error("TransactionProfile.TargetGateCardinality", "AreaDeliveryGate QuickContest is shared across all Strikes against one target");
                    break;
            }
        }
    }

    private static void ValidateCritical(DclCriticalProfile profile, DclAuthoringValidation result)
    {
        if (profile.SuccessEffect == DclCriticalSuccessEffect.Unknown) result.Error("CriticalProfile.SuccessEffect", "must be explicit");
        if (profile.FailureEffect == DclCriticalFailureEffect.Unknown) result.Error("CriticalProfile.FailureEffect", "must be explicit");
    }

    private static void ValidatePresentation(DclPresentationProfile profile, DclAuthoringValidation result)
    {
        RequireText(result, "PresentationProfile.ActionAnimation", profile.ActionAnimation);
        RequireText(result, "PresentationProfile.ResultText", profile.ResultText);
        RequireList(result, "PresentationProfile.ForecastTerms", profile.ForecastTerms, allowEmpty: true);
        RequireList(result, "PresentationProfile.StatePresentationIds", profile.StatePresentationIds, allowEmpty: true);
    }

    private static void ValidateAi(DclAiProfile profile, DclAuthoringValidation result)
    {
        if (profile.LegalUsePolicy == DclLegalUsePolicy.Unknown) result.Error("AiProfile.LegalUsePolicy", "must be explicit");
        if (profile.ExpectedValuePolicy == DclExpectedValuePolicy.Unknown) result.Error("AiProfile.ExpectedValuePolicy", "must be explicit");
        if (profile.FriendlyFirePolicy == DclFriendlyFirePolicy.Unknown) result.Error("AiProfile.FriendlyFirePolicy", "must be explicit");
        RequireList(result, "AiProfile.OutcomeTags", profile.OutcomeTags, allowEmpty: true);
    }

    private static void ValidateCrossProfile(DclActionProfile profile, DclAuthoringValidation result)
    {
        DclDeliveryProfile delivery = profile.DeliveryProfile;
        DclTransactionProfile transaction = profile.TransactionProfile;
        DclTargetProfile target = profile.TargetProfile;

        if (profile.SkillProfile.GoverningSkillId is null)
        {
            bool deterministicUngated = delivery.Delivery == DclDelivery.Other &&
                transaction.CastingRollCardinality == DclCastingRollCardinality.None &&
                !delivery.Dodgeable && !delivery.Parryable && !delivery.Blockable &&
                delivery.ResistanceCharacteristic is null or DclResistanceCharacteristic.None;
            if (!deterministicUngated)
                result.Error("SkillProfile.GoverningSkillId", "None is legal only for an explicit deterministic ungated system command/effect");
        }

        switch (delivery.Delivery)
        {
            case DclDelivery.PhysicalAttack:
            case DclDelivery.Touch:
                if (target.PhysicalRoute == DclPhysicalRoute.None)
                    result.Error("TargetProfile.PhysicalRoute", "physical and Touch delivery require NativeDirect or NativeArc routing");
                if (transaction.CastingRollCardinality != DclCastingRollCardinality.None)
                    result.Error("TransactionProfile.CastingRollCardinality", "physical and Touch delivery use their per-Strike attack gate, not a casting draw");
                if (transaction.TargetGateCardinality != DclTargetGateCardinality.PerStrike)
                    result.Error("TransactionProfile.TargetGateCardinality", "physical and Touch delivery resolve their attack/defense gate per Strike");
                break;
            case DclDelivery.ExternalProjectile:
                RequireMagicRouteNone(target, result);
                if (transaction.CastingRollCardinality != DclCastingRollCardinality.PerAction)
                    result.Error("TransactionProfile.CastingRollCardinality", "External Projectile shares one outer casting draw");
                DclTargetGateCardinality externalGate = delivery.Dodgeable || delivery.Blockable
                    ? DclTargetGateCardinality.PerStrike
                    : DclTargetGateCardinality.PerTarget;
                if (transaction.TargetGateCardinality != externalGate)
                    result.Error("TransactionProfile.TargetGateCardinality", $"External Projectile requires {externalGate} for its explicit defense policy");
                break;
            case DclDelivery.InternalDirect:
                RequireMagicRouteNone(target, result);
                if (transaction.CastingRollCardinality != DclCastingRollCardinality.PerAction)
                    result.Error("TransactionProfile.CastingRollCardinality", "Internal Direct shares one outer casting draw");
                DclTargetGateCardinality internalGate = profile.DispelProfile is null
                    ? DclTargetGateCardinality.PerTarget
                    : DclTargetGateCardinality.Explicit;
                if (transaction.TargetGateCardinality != internalGate)
                    result.Error("TransactionProfile.TargetGateCardinality", profile.DispelProfile is null
                        ? "Internal Direct rolls one Quick Contest per target for the outer action"
                        : "Dispel owns one explicit stored-effect contest per selected instance");
                break;
            case DclDelivery.Area:
                RequireMagicRouteNone(target, result);
                if (transaction.CastingRollCardinality != DclCastingRollCardinality.PerAction)
                    result.Error("TransactionProfile.CastingRollCardinality", "magical Area delivery shares one outer casting draw");
                break;
            case DclDelivery.Beneficial:
                RequireMagicRouteNone(target, result);
                if (transaction.CastingRollCardinality != DclCastingRollCardinality.PerAction)
                    result.Error("TransactionProfile.CastingRollCardinality", "Beneficial delivery shares one outer casting draw");
                if (transaction.TargetGateCardinality != DclTargetGateCardinality.PerTarget)
                    result.Error("TransactionProfile.TargetGateCardinality", "Beneficial delivery classifies the shared casting draw once per willing target without active defense");
                break;
            case DclDelivery.Rider:
                RequireMagicRouteNone(target, result);
                if (delivery.ResistanceCharacteristic is null or DclResistanceCharacteristic.None)
                    result.Error("DeliveryProfile.ResistanceCharacteristic", "Rider delivery requires its one authored resistance gate");
                break;
        }

        bool hasDamage = profile.Effects.Any(effect => effect.Kind == DclEffectKind.Damage);
        bool hasHealing = profile.Effects.Any(effect => effect.Kind == DclEffectKind.Healing);
        bool hasResource = profile.Effects.Any(effect => effect.Kind is DclEffectKind.ResourceChange or DclEffectKind.CtChange);
        bool hasResourceChange = profile.Effects.Any(effect => effect.Kind == DclEffectKind.ResourceChange);
        bool hasForcedMovement = profile.Effects.Any(effect => effect.Kind == DclEffectKind.ForcedMovement);
        bool hasRevive = profile.Effects.Any(effect => effect.Kind == DclEffectKind.Revive);
        bool hasDispel = profile.Effects.Any(effect => effect.Kind == DclEffectKind.Dispel);
        switch (profile.MagnitudeProfile)
        {
            case null when hasDamage || hasHealing || hasResource:
                result.Error("MagnitudeProfile", "numeric Damage, Healing, ResourceChange, and CTChange effects require one primary magnitude");
                break;
            case DclDamageMagnitude when !hasDamage:
                result.Error("Effects", "DamageMagnitude requires an ordered Damage effect");
                break;
            case DclHealingMagnitude when !hasHealing:
                result.Error("Effects", "HealingMagnitude requires an ordered Healing effect");
                break;
            case DclFixedResourceMagnitude resource:
                bool matching = resource.Resource == DclResourceKind.Ct
                    ? profile.Effects.Any(effect => effect.Kind == DclEffectKind.CtChange)
                    : profile.Effects.Any(effect => effect.Kind == DclEffectKind.ResourceChange) ||
                      resource.Resource == DclResourceKind.Hp && hasRevive;
                if (!matching) result.Error("Effects", "FixedResourceMagnitude requires its matching ResourceChange, CTChange, or HP Revive effect");
                break;
        }
        if (profile.MagnitudeProfile is not DclDamageMagnitude && hasDamage)
            result.Error("MagnitudeProfile", "a Damage effect requires DamageMagnitude");
        if (profile.MagnitudeProfile is not DclHealingMagnitude && hasHealing)
            result.Error("MagnitudeProfile", "a Healing effect requires HealingMagnitude");
        if (hasRevive != (profile.ReviveProfile is not null))
            result.Error("ReviveProfile", "must be present exactly when the ordered effects contain Revive");
        if (profile.ReviveProfile is { } reviveProfile)
        {
            try
            {
                DclReviveRules.Validate(reviveProfile);
            }
            catch (ArgumentException exception)
            {
                result.Error("ReviveProfile", exception.Message);
            }
            DclEffectProfile[] reviveEffects = profile.Effects.Where(effect => effect.Kind == DclEffectKind.Revive).ToArray();
            if (reviveEffects.Length != 1)
                result.Error("Effects", "a normalized revive action requires exactly one Revive effect");
            else
            {
                DclEffectProfile reviveEffect = reviveEffects[0];
                if (reviveEffect.EligibleTargetStates != reviveProfile.EligibleTargetStates)
                    result.Error("ReviveProfile.EligibleTargetStates", "must equal the Revive effect target-state policy");
                if (reviveProfile.Mode == DclReviveMode.StoredReraise && string.IsNullOrWhiteSpace(reviveEffect.ReferencedStateKind))
                    result.Error("Effects", "StoredReraise requires its referenced persistent trigger state");
                if (reviveProfile.Mode == DclReviveMode.Immediate && !string.IsNullOrWhiteSpace(reviveEffect.ReferencedStateKind))
                    result.Error("Effects", "Immediate revive cannot reference a stored trigger state");
            }
            if (profile.MagnitudeProfile is not DclFixedResourceMagnitude
                {
                    Resource: DclResourceKind.Hp,
                } restoredHp || !DclDiceExpression.TryParseAuthored(restoredHp.Expression, out _))
                result.Error("MagnitudeProfile", "Revive requires an HP FixedResourceMagnitude using the exact Xd6+Y restored-HP grammar");
            if (delivery.Delivery is not (DclDelivery.Beneficial or DclDelivery.InternalDirect))
                result.Error("DeliveryProfile.Delivery", "Revive requires Beneficial or InternalDirect casting policy");
        }
        if (hasDispel != (profile.DispelProfile is not null))
            result.Error("DispelProfile", "must be present exactly when the ordered effects contain Dispel");
        if (profile.DispelProfile is { } dispelProfile)
        {
            DclEffectProfile[] dispelEffects = profile.Effects.Where(effect => effect.Kind == DclEffectKind.Dispel).ToArray();
            if (dispelEffects.Length != 1 || dispelEffects[0].Role != DclEffectRole.Carrier)
                result.Error("Effects", "a normalized Dispel action requires exactly one Carrier effect");
            if (dispelProfile.Scope == DclDispelScope.Unknown)
                result.Error("DispelProfile.Scope", "must be explicit");
            RequireList(result, "DispelProfile.EligibleCureFamilies", dispelProfile.EligibleCureFamilies, allowEmpty: false);
            if (profile.MagnitudeProfile is not null)
                result.Error("MagnitudeProfile", "Dispel has no numeric magnitude");
            if (delivery.Delivery != DclDelivery.InternalDirect ||
                delivery.ResistanceCharacteristic is null or DclResistanceCharacteristic.None)
                result.Error("DeliveryProfile", "Dispel uses InternalDirect with the stored effect as its authored resistance owner");
        }
        if (hasResourceChange != (profile.ResourceChangeProfile is not null))
            result.Error("ResourceChangeProfile", "must be present exactly when the ordered effects contain ResourceChange");
        if (profile.ResourceChangeProfile is { } resourceChange)
        {
            DclEffectProfile[] resourceEffects = profile.Effects
                .Where(effect => effect.Kind == DclEffectKind.ResourceChange)
                .ToArray();
            if (resourceEffects.Length != 1 || resourceEffects[0].Role != DclEffectRole.Carrier ||
                profile.Effects.Count != 1)
                result.Error("Effects", "a normalized generic ResourceChange action requires exactly one Carrier and no implicit Riders");
            if (profile.MagnitudeProfile is not DclFixedResourceMagnitude
                {
                    Resource: DclResourceKind.Hp or DclResourceKind.Mp,
                } fixedResource || !DclDiceExpression.TryParseAuthored(fixedResource.Expression, out _))
                result.Error("MagnitudeProfile", "ResourceChange requires an HP or MP FixedResourceMagnitude using the exact Xd6+Y grammar");
            if (resourceChange.Route == DclResourceChangeRoute.Unknown)
                result.Error("ResourceChangeProfile.Route", "must be explicit");
            if (resourceChange.SourceUndeadInteraction == DclUndeadInteraction.Unknown)
                result.Error("ResourceChangeProfile.SourceUndeadInteraction", "must be explicit");
            if (resourceChange.Route != DclResourceChangeRoute.DrainTargetToSource &&
                resourceChange.SourceUndeadInteraction != DclUndeadInteraction.Normal)
                result.Error("ResourceChangeProfile.SourceUndeadInteraction", "non-Drain resource changes must use Normal because they do not affect the source pool");
        }
        if (hasForcedMovement != (profile.ForcedMovementProfile is not null))
            result.Error("ForcedMovementProfile", "must be present exactly when the ordered effects contain ForcedMovement");
        if (profile.ForcedMovementProfile is { } forcedMovement)
        {
            if (forcedMovement.DistanceTiles <= 0)
                result.Error("ForcedMovementProfile.DistanceTiles", "must be positive");
            if (forcedMovement.Direction == DclForcedMovementDirection.Unknown)
                result.Error("ForcedMovementProfile.Direction", "must be explicit");
            bool explicitVector = forcedMovement.Direction == DclForcedMovementDirection.ExplicitVector;
            if (explicitVector)
            {
                if (forcedMovement.DeltaX is null || forcedMovement.DeltaY is null ||
                    forcedMovement.DeltaX is < -1 or > 1 || forcedMovement.DeltaY is < -1 or > 1 ||
                    forcedMovement.DeltaX == 0 && forcedMovement.DeltaY == 0)
                    result.Error("ForcedMovementProfile", "ExplicitVector requires one nonzero normalized -1..1 tile vector");
            }
            else if (forcedMovement.DeltaX is not null || forcedMovement.DeltaY is not null)
            {
                result.Error("ForcedMovementProfile", "only ExplicitVector may author DeltaX/DeltaY");
            }
            if (profile.TransactionProfile.StrikeCount > 1 &&
                profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate)
                result.Error("ForcedMovementProfile", "multi-Strike Immediate forced movement requires an explicit between-Strike map reprojection owner");
        }
        if (profile.MagnitudeProfile is DclHealingMagnitude &&
            delivery.Delivery == DclDelivery.Beneficial &&
            profile.CriticalProfile.SuccessEffect != DclCriticalSuccessEffect.MaximizeOneHealingDie)
            result.Error("CriticalProfile.SuccessEffect", "direct Beneficial healing requires MaximizeOneHealingDie");
        if (profile.MagnitudeProfile is not DclHealingMagnitude &&
            profile.CriticalProfile.SuccessEffect == DclCriticalSuccessEffect.MaximizeOneHealingDie)
            result.Error("CriticalProfile.SuccessEffect", "MaximizeOneHealingDie is legal only for a healing magnitude");

        int carrierCount = 0;
        for (int index = 0; index < profile.Effects.Count; index++)
        {
            DclEffectProfile effect = profile.Effects[index];
            if (effect.Role == DclEffectRole.Carrier) carrierCount++;
            if (effect.Role == DclEffectRole.Rider && carrierCount == 0)
                result.Error($"Effects[{index}].Role", "a Rider must follow its successful Carrier in the ordered effect list");
        }
        if (carrierCount > 1)
            result.Error("Effects", "one action may name only one Carrier; additional unconditional results are Independent");
    }

    private static void RequireMagicRouteNone(DclTargetProfile target, DclAuthoringValidation result)
    {
        if (target.PhysicalRoute != DclPhysicalRoute.None)
            result.Error("TargetProfile.PhysicalRoute", "magical delivery uses explicit None rather than native physical trajectory routing");
    }

    private static void ValidateDuration(DclStateDurationProfile profile, DclAuthoringValidation result)
    {
        try
        {
            _ = DclStateDurationRules.Parse(profile);
        }
        catch (FormatException ex)
        {
            result.Error(profile.Clock == DclStateDurationClock.Unknown ? "Duration.Clock" : "Duration.Formula", ex.Message);
        }
    }

    private static void RequireText(DclAuthoringValidation result, string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) result.Error(path, "is required");
    }

    private static void RequireList(DclAuthoringValidation result, string path, IReadOnlyList<string>? values, bool allowEmpty)
    {
        if (values is null)
        {
            result.Error(path, "is required");
            return;
        }
        if (!allowEmpty && values.Count == 0) result.Error(path, "cannot be empty");
        for (int index = 0; index < values.Count; index++)
            if (string.IsNullOrWhiteSpace(values[index])) result.Error($"{path}[{index}]", "cannot be blank");
    }
}

internal sealed class DclAuthoringRegistry
{
    private readonly Dictionary<string, DclActionProfile> _actions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DclStateDefinition> _states = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DclReactionDefinition> _reactions = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, DclActionProfile> Actions => _actions;
    public IReadOnlyDictionary<string, DclStateDefinition> States => _states;
    public IReadOnlyDictionary<string, DclReactionDefinition> Reactions => _reactions;

    public DclAuthoringValidation ValidateReferences()
    {
        var validation = new DclAuthoringValidation();
        foreach ((string actionId, DclActionProfile profile) in _actions)
        {
            for (int index = 0; index < profile.Effects.Count; index++)
            {
                DclEffectProfile effect = profile.Effects[index];
                if (effect.Kind is DclEffectKind.StatusApplication or DclEffectKind.StatusRemoval or DclEffectKind.Revive &&
                    effect.ReferencedStateKind is { } stateKind && !_states.ContainsKey(stateKind))
                {
                    validation.Error($"Actions[{actionId}].Effects[{index}].ReferencedStateKind", $"state '{stateKind}' is not loaded in the same normalized bundle");
                    continue;
                }
                if (!RequiresStatePresentation(profile, effect) ||
                    string.IsNullOrWhiteSpace(effect.ReferencedStateKind))
                {
                    continue;
                }
                DclStateDefinition state = _states[effect.ReferencedStateKind];
                if (!profile.PresentationProfile.StatePresentationIds.Contains(
                        state.PresentationProfile,
                        StringComparer.Ordinal))
                {
                    validation.Error(
                        $"Actions[{actionId}].PresentationProfile.StatePresentationIds",
                        $"must include presentation '{state.PresentationProfile}' for applied state '{state.Kind}'");
                }
            }
        }
        foreach ((string reactionId, DclReactionDefinition reaction) in _reactions)
            if (!_actions.ContainsKey(reaction.EffectActionId))
                validation.Error($"Reactions[{reactionId}].EffectActionId", $"action '{reaction.EffectActionId}' is not loaded in the same normalized bundle");
        foreach ((string stateKind, DclStateDefinition state) in _states)
        {
            if (state.TickProfile is not { } tick) continue;
            if (!_actions.TryGetValue(tick.EffectActionId, out DclActionProfile? tickAction))
            {
                validation.Error(
                    $"States[{stateKind}].TickProfile.EffectActionId",
                    $"action '{tick.EffectActionId}' is not loaded in the same normalized bundle");
                continue;
            }
            if (tickAction.SourceProfile.Source != DclActionSource.PeriodicEffect)
                validation.Error(
                    $"States[{stateKind}].TickProfile.EffectActionId",
                    $"action '{tick.EffectActionId}' is not a PeriodicEffect action");
            if (tickAction.ResourceProfile.BaseMpCost != 0 ||
                tickAction.TargetProfile.TargetMode != DclTargetMode.Unit)
                validation.Error(
                    $"States[{stateKind}].TickProfile.EffectActionId",
                    $"action '{tick.EffectActionId}' must be a zero-cost unit-targeted periodic action");
        }
        foreach (IGrouping<DclNativeStatusBit, KeyValuePair<string, DclStateDefinition>> group in
                 _states.Where(pair => pair.Value.NativeStatusBit is not null)
                     .GroupBy(pair => pair.Value.NativeStatusBit!.Value))
        {
            string[] owners = group.Select(pair => pair.Key).Order(StringComparer.Ordinal).ToArray();
            if (owners.Length > 1)
                validation.Error("States.NativeStatusBit",
                    $"native bit {group.Key.ByteIndex}:0x{group.Key.Mask:X2} has multiple semantic owners: {string.Join(", ", owners)}");
        }
        return validation;
    }

    private static bool RequiresStatePresentation(DclActionProfile profile, DclEffectProfile effect)
        => effect.Kind == DclEffectKind.StatusApplication ||
           effect.Kind == DclEffectKind.Revive &&
           profile.ReviveProfile?.Mode == DclReviveMode.StoredReraise;

    public DclReactionValidation TryRegister(DclReactionDefinition definition)
    {
        DclReactionValidation validation = DclReactionContract.Validate(definition);
        if (!validation.IsValid) return validation;
        if (_reactions.ContainsKey(definition.ReactionId))
            validation.Error("ReactionId", $"reaction '{definition.ReactionId}' is already loaded");
        else
            _reactions.Add(definition.ReactionId, definition);
        return validation;
    }

    public DclAuthoringValidation TryRegister(DclActionProfile profile)
    {
        DclAuthoringValidation validation = DclAuthoringContract.Validate(profile);
        if (!validation.IsValid) return validation;
        if (_actions.TryGetValue(profile.ActionId, out DclActionProfile? existing))
        {
            validation.Error("ActionId", $"action '{profile.ActionId}' is already loaded at revision {existing.ProfileRevision}");
            return validation;
        }
        _actions[profile.ActionId] = profile;
        return validation;
    }

    public DclAuthoringValidation TryRegister(DclStateDefinition definition)
    {
        DclAuthoringValidation validation = DclAuthoringContract.Validate(definition);
        if (!validation.IsValid) return validation;
        if (_states.ContainsKey(definition.Kind))
        {
            validation.Error("Kind", $"state '{definition.Kind}' is already loaded");
            return validation;
        }
        _states.Add(definition.Kind, definition);
        return validation;
    }
}
