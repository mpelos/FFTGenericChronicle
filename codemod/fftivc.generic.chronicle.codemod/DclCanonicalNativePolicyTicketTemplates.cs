using System.Text.Json;
using System.Text.Json.Serialization;

namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativePolicyTicketTemplateBundle(
    int SchemaRevision,
    IReadOnlyList<DclCanonicalNativePolicyTicketTemplate> Templates);

internal sealed record DclCanonicalNativePolicyTicketTemplate(
    int AbilityId,
    DclCanonicalNativeUnitPolicyTemplate UnitPolicy,
    DclCanonicalNativeFamilyPolicyTicketTemplate FamilyPolicy);

internal sealed record DclCanonicalNativeUnitPolicyTemplate(
    int SourceTileHeight,
    int TargetTileHeight,
    bool SynchronizeNativePools = false);

internal sealed record DclCanonicalNativeFamilyPolicyTicketTemplate(
    DclCanonicalActionFamily Family,
    DclCanonicalNativeDirectActionPolicyTemplate? DirectNumeric = null,
    DclCanonicalNativeStatusApplicationPolicyTemplate? StatusApplication = null,
    DclCanonicalNativeSingleTargetMagicPolicySource? StatusRemoval = null,
    DclCanonicalNativeDispelActionPolicyTemplate? Dispel = null,
    DclCanonicalNativeQuickActionPolicyTemplate? Quick = null,
    DclCanonicalNativeReviveActionPolicyTemplate? Revive = null,
    DclCanonicalNativeForcedMovementActionPolicyTemplate? ForcedMovement = null,
    DclCanonicalNativeAreaActionPolicySource? AreaNumeric = null,
    DclCanonicalNativePhysicalActionPolicySource? PhysicalDamage = null);

internal sealed record DclCanonicalNativeDirectActionPolicyTemplate(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclDefenseOption Defense,
    int EffectiveTargetHtModifier = 0,
    int ConcentrationStatePenaltyMagnitude = 0,
    int AdditionalMagnitudeIntegerModifier = 0,
    int AimRetentionModifier = 0,
    int AimRetentionStatePenaltyMagnitude = 0,
    bool DirectConcentrationCancellation = false,
    int AuthoredForcedDisplacement = 0,
    bool ReflectionAlreadyConsumed = false,
    int? ResistanceScore = null,
    bool Immune = false);

internal sealed record DclCanonicalNativeDispelActionPolicyTemplate(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    int FinalDispelScore,
    long? SelectedInstanceId = null);

internal sealed record DclCanonicalNativeStatusApplicationPolicyTemplate(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclCanonicalNativePropertyStateMaterializationTemplate StateMaterialization,
    int JobMagicResistance = 0,
    int ExplicitStateResistanceModifier = 0,
    int? AuthoredResistanceScore = null);

internal sealed record DclCanonicalNativeQuickActionPolicyTemplate(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclRational TargetCt,
    DclCanonicalNativePropertyStateMaterializationTemplate LockMaterialization);

internal sealed record DclCanonicalNativeReviveActionPolicyTemplate(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclRational FaithMultiplier,
    DclCanonicalNativeUndeadInteractionTableTemplate UndeadInteractions,
    int? ResistanceScore = null,
    bool Immune = false,
    DclCanonicalNativePropertyStateMaterializationTemplate? StoredReraiseMaterialization = null);

internal sealed record DclCanonicalNativeForcedMovementActionPolicyTemplate(
    DclCanonicalNativeSingleTargetMagicPolicySource Magic,
    DclDefenseOption Defense,
    DclCanonicalNativeMovementVerdict NativeMovementVerdict,
    int? ResistanceScore = null,
    bool Immune = false);

internal sealed record DclCanonicalNativeUndeadInteractionTableTemplate(
    IReadOnlyDictionary<DclUndeadEffectFamily, DclUndeadFamilyRule> Rules);

internal sealed record DclCanonicalNativePropertyStateMaterializationTemplate(
    bool BindSource,
    long AppliedBeforeTurnSerial,
    long? FirstEligibleTargetTurnSerial,
    long? FirstEligibleSourceTurnSerial,
    int? DurationUnits,
    int? Strength,
    string StackDiscriminator,
    string? ContributionIdentity,
    string PayloadSchemaId,
    IReadOnlyDictionary<string, string>? PayloadValues = null);

internal enum DclCanonicalNativePolicyTicketTemplateBuildStatus
{
    Built,
    MissingTemplate,
    AbilityMismatch,
    FamilyMismatch,
    UnsupportedFamilyTemplate,
}

internal sealed record DclCanonicalNativePolicyTicketTemplateBuildResult(
    DclCanonicalNativePolicyTicketTemplateBuildStatus Status,
    DclCanonicalNativePolicySourceTicket? Ticket);

internal sealed class DclCanonicalNativePolicyTicketTemplateRegistry
{
    private readonly Dictionary<int, DclCanonicalNativePolicyTicketTemplate> _templates = [];

    public DclCanonicalNativePolicyTicketTemplateRegistry(
        IEnumerable<DclCanonicalNativePolicyTicketTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        foreach (DclCanonicalNativePolicyTicketTemplate template in templates)
        {
            if (!_templates.TryAdd(template.AbilityId, template))
                throw new DclAuthoringLoadException(
                    $"Duplicate native policy ticket template for ability {template.AbilityId}.");
        }
    }

    public int Count => _templates.Count;

    public bool TryGet(int abilityId, out DclCanonicalNativePolicyTicketTemplate template)
        => _templates.TryGetValue(abilityId, out template!);

    public DclCanonicalNativePolicyTicketTemplateBuildResult TryBuildTicket(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(action);
        if (!_templates.TryGetValue(action.AbilityId, out DclCanonicalNativePolicyTicketTemplate? template))
            return new DclCanonicalNativePolicyTicketTemplateBuildResult(
                DclCanonicalNativePolicyTicketTemplateBuildStatus.MissingTemplate,
                null);
        return DclCanonicalNativePolicyTicketTemplateBuilder.TryBuild(battle, action, template);
    }
}

internal static class DclCanonicalNativePolicyTicketTemplateBuilder
{
    public static DclCanonicalNativePolicyTicketTemplateBuildResult TryBuild(
        DclCanonicalBattleRuntime battle,
        DclCanonicalNativeAdmittedAction action,
        DclCanonicalNativePolicyTicketTemplate template)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(template);
        if (template.AbilityId != action.AbilityId)
            return new DclCanonicalNativePolicyTicketTemplateBuildResult(
                DclCanonicalNativePolicyTicketTemplateBuildStatus.AbilityMismatch,
                null);

        DclCanonicalActionFamily actualFamily = battle.Catalog.ResolveAbilityFamily(action.AbilityId);
        if (template.FamilyPolicy.Family != actualFamily)
            return new DclCanonicalNativePolicyTicketTemplateBuildResult(
                DclCanonicalNativePolicyTicketTemplateBuildStatus.FamilyMismatch,
                null);

        object familyPolicySource = actualFamily switch
        {
            DclCanonicalActionFamily.DirectNumeric when template.FamilyPolicy.DirectNumeric is { } direct =>
                BuildDirectPolicySource(direct),
            DclCanonicalActionFamily.StatusApplication when template.FamilyPolicy.StatusApplication is { } status =>
                BuildStatusApplicationPolicySource(status),
            DclCanonicalActionFamily.StatusRemoval when template.FamilyPolicy.StatusRemoval is { } statusRemoval =>
                statusRemoval,
            DclCanonicalActionFamily.Dispel when template.FamilyPolicy.Dispel is { } dispel =>
                BuildDispelPolicySource(dispel),
            DclCanonicalActionFamily.Quick when template.FamilyPolicy.Quick is { } quick =>
                BuildQuickPolicySource(quick),
            DclCanonicalActionFamily.Revive when template.FamilyPolicy.Revive is { } revive =>
                BuildRevivePolicySource(revive),
            DclCanonicalActionFamily.ForcedMovement when template.FamilyPolicy.ForcedMovement is { } movement =>
                BuildForcedMovementPolicySource(movement),
            DclCanonicalActionFamily.AreaNumeric when template.FamilyPolicy.AreaNumeric is { } area =>
                area,
            DclCanonicalActionFamily.PhysicalDamage when template.FamilyPolicy.PhysicalDamage is { } physical =>
                physical,
            _ => null!,
        };
        if (familyPolicySource is null)
            return new DclCanonicalNativePolicyTicketTemplateBuildResult(
                DclCanonicalNativePolicyTicketTemplateBuildStatus.UnsupportedFamilyTemplate,
                null);

        IReadOnlyList<DclCanonicalNativeUnitPolicySource> units =
            BuildUnitPolicySources(action, template.UnitPolicy);
        var ticket = new DclCanonicalNativePolicySourceTicket(
            action.ActionInstanceId,
            units,
            familyPolicySource);

        return new DclCanonicalNativePolicyTicketTemplateBuildResult(
            DclCanonicalNativePolicyTicketTemplateBuildStatus.Built,
            ticket);
    }

    private static IReadOnlyList<DclCanonicalNativeUnitPolicySource> BuildUnitPolicySources(
        DclCanonicalNativeAdmittedAction action,
        DclCanonicalNativeUnitPolicyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (template.SourceTileHeight < 0 || template.TargetTileHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(template), "Native policy ticket template tile heights cannot be negative.");
        return action.NativeRows.Keys
            .OrderBy(unit => unit.BattleGeneration)
            .ThenBy(unit => unit.UnitSlot)
            .ThenBy(unit => unit.CharacterId)
            .Select(unit =>
            {
                DclCanonicalAttributeAdjustments attributes = new();
                DclSecondaryInputs? secondary = null;
                if (template.SynchronizeNativePools)
                {
                    UnitSnapshot native = action.NativeRows[unit];
                    DclPrimaryCharacteristics primary =
                        DclCanonicalNativeSnapshotAdapter.ProjectPrimary(native, attributes);
                    secondary = new DclSecondaryInputs(
                        CharacterHpModifier: checked(native.MaxHp - primary.St),
                        CharacterMpModifier: checked(native.MaxMp - Math.Max(primary.Ht, primary.Iq)));
                }
                return new DclCanonicalNativeUnitPolicySource(
                    unit,
                    unit == action.Source ? template.SourceTileHeight : template.TargetTileHeight,
                    attributes,
                    secondary);
            })
            .ToArray();
    }

    private static DclCanonicalNativeDirectActionPolicySource BuildDirectPolicySource(
        DclCanonicalNativeDirectActionPolicyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(template.Magic);
        return new DclCanonicalNativeDirectActionPolicySource(
            template.Magic,
            template.Defense,
            template.EffectiveTargetHtModifier,
            template.ConcentrationStatePenaltyMagnitude,
            template.AdditionalMagnitudeIntegerModifier,
            template.AimRetentionModifier,
            template.AimRetentionStatePenaltyMagnitude,
            template.DirectConcentrationCancellation,
            template.AuthoredForcedDisplacement,
            template.ReflectionAlreadyConsumed,
            EffectOwnedLocation: null,
            template.ResistanceScore,
            template.Immune);
    }

    private static DclCanonicalNativeStatusActionPolicySource BuildStatusApplicationPolicySource(
        DclCanonicalNativeStatusApplicationPolicyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(template.Magic);
        return new DclCanonicalNativeStatusActionPolicySource(
            template.Magic,
            BuildPropertyStateMaterialization(template.StateMaterialization),
            template.JobMagicResistance,
            template.ExplicitStateResistanceModifier,
            template.AuthoredResistanceScore);
    }

    private static DclCanonicalStateMaterialization BuildPropertyStateMaterialization(
        DclCanonicalNativePropertyStateMaterializationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return new DclCanonicalStateMaterialization(
            template.BindSource,
            template.AppliedBeforeTurnSerial,
            template.FirstEligibleTargetTurnSerial,
            template.FirstEligibleSourceTurnSerial,
            template.DurationUnits,
            template.Strength,
            template.StackDiscriminator,
            template.ContributionIdentity,
            new DclPropertyStatePayload(
                template.PayloadSchemaId,
                template.PayloadValues ?? new Dictionary<string, string>()));
    }

    private static DclCanonicalNativeDispelActionPolicySource BuildDispelPolicySource(
        DclCanonicalNativeDispelActionPolicyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(template.Magic);
        return new DclCanonicalNativeDispelActionPolicySource(
            template.Magic,
            template.FinalDispelScore,
            template.SelectedInstanceId);
    }

    private static DclCanonicalNativeQuickActionPolicySource BuildQuickPolicySource(
        DclCanonicalNativeQuickActionPolicyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(template.Magic);
        return new DclCanonicalNativeQuickActionPolicySource(
            template.Magic,
            DclCtState.Restore(template.TargetCt),
            new DclQuickLockController(),
            BuildPropertyStateMaterialization(template.LockMaterialization));
    }

    private static DclCanonicalNativeReviveActionPolicySource BuildRevivePolicySource(
        DclCanonicalNativeReviveActionPolicyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(template.Magic);
        ArgumentNullException.ThrowIfNull(template.UndeadInteractions);
        if (template.UndeadInteractions.Rules is null)
            throw new ArgumentException("Revive policy template requires complete Undead interaction rules.", nameof(template));
        return new DclCanonicalNativeReviveActionPolicySource(
            template.Magic,
            template.FaithMultiplier,
            new DclUndeadInteractionTable(template.UndeadInteractions.Rules),
            template.ResistanceScore,
            template.Immune,
            template.StoredReraiseMaterialization is null
                ? null
                : BuildPropertyStateMaterialization(template.StoredReraiseMaterialization));
    }

    private static DclCanonicalNativeForcedMovementActionPolicySource BuildForcedMovementPolicySource(
        DclCanonicalNativeForcedMovementActionPolicyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(template.Magic);
        ArgumentNullException.ThrowIfNull(template.NativeMovementVerdict);
        return new DclCanonicalNativeForcedMovementActionPolicySource(
            template.Magic,
            template.Defense,
            template.NativeMovementVerdict,
            template.ResistanceScore,
            template.Immune);
    }
}

internal static class DclCanonicalNativePolicyTicketTemplateJsonLoader
{
    public const int CurrentSchemaRevision = 1;

    public static DclCanonicalNativePolicyTicketTemplateRegistry Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new DclAuthoringLoadException("The DCL native policy ticket template bundle is empty.");
        DclCanonicalNativePolicyTicketTemplateBundle bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<DclCanonicalNativePolicyTicketTemplateBundle>(json, CreateOptions())
                ?? throw new JsonException("Native policy ticket template bundle deserialized to null.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new DclAuthoringLoadException(
                "The DCL native policy ticket template bundle is not valid strict JSON.",
                exception);
        }

        if (bundle.SchemaRevision != CurrentSchemaRevision)
            throw new DclAuthoringLoadException(
                $"Unsupported DCL native policy ticket template schema revision {bundle.SchemaRevision}.");
        if (bundle.Templates is null)
            throw new DclAuthoringLoadException("Templates array is required, including when empty.");

        foreach (DclCanonicalNativePolicyTicketTemplate? template in bundle.Templates)
        {
            if (template is null)
                throw new DclAuthoringLoadException("Native policy ticket template entries cannot be null.");
            Validate(template);
        }

        return new DclCanonicalNativePolicyTicketTemplateRegistry(bundle.Templates);
    }

    public static string Serialize(DclCanonicalNativePolicyTicketTemplateBundle bundle)
        => JsonSerializer.Serialize(bundle, CreateOptions());

    private static void Validate(DclCanonicalNativePolicyTicketTemplate template)
    {
        if (template.AbilityId < 0)
            throw new DclAuthoringLoadException("Native policy ticket template AbilityId cannot be negative.");
        if (template.UnitPolicy is null)
            throw new DclAuthoringLoadException("Native policy ticket template UnitPolicy is required.");
        if (template.UnitPolicy.SourceTileHeight < 0 || template.UnitPolicy.TargetTileHeight < 0)
            throw new DclAuthoringLoadException("Native policy ticket template tile heights cannot be negative.");
        if (template.FamilyPolicy is null)
            throw new DclAuthoringLoadException("Native policy ticket template FamilyPolicy is required.");
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.DirectNumeric)
        {
            if (template.FamilyPolicy.DirectNumeric is null)
                throw new DclAuthoringLoadException("DirectNumeric native policy ticket templates require directNumeric policy.");
            if (template.FamilyPolicy.DirectNumeric.Magic is null)
                throw new DclAuthoringLoadException("DirectNumeric native policy ticket templates require magic policy.");
            return;
        }
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.StatusApplication)
        {
            if (template.FamilyPolicy.StatusApplication is null)
                throw new DclAuthoringLoadException("StatusApplication native policy ticket templates require statusApplication policy.");
            if (template.FamilyPolicy.StatusApplication.Magic is null)
                throw new DclAuthoringLoadException("StatusApplication native policy ticket templates require magic policy.");
            ValidatePropertyStateMaterialization(template.FamilyPolicy.StatusApplication.StateMaterialization);
            return;
        }
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.StatusRemoval)
        {
            if (template.FamilyPolicy.StatusRemoval is null)
                throw new DclAuthoringLoadException("StatusRemoval native policy ticket templates require statusRemoval magic policy.");
            return;
        }
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.Dispel)
        {
            if (template.FamilyPolicy.Dispel is null)
                throw new DclAuthoringLoadException("Dispel native policy ticket templates require dispel policy.");
            if (template.FamilyPolicy.Dispel.Magic is null)
                throw new DclAuthoringLoadException("Dispel native policy ticket templates require magic policy.");
            if (template.FamilyPolicy.Dispel.FinalDispelScore < 1)
                throw new DclAuthoringLoadException("Dispel native policy ticket templates require positive FinalDispelScore.");
            return;
        }
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.Quick)
        {
            if (template.FamilyPolicy.Quick is null)
                throw new DclAuthoringLoadException("Quick native policy ticket templates require quick policy.");
            if (template.FamilyPolicy.Quick.Magic is null)
                throw new DclAuthoringLoadException("Quick native policy ticket templates require magic policy.");
            if (template.FamilyPolicy.Quick.TargetCt < DclRational.FromInteger(0) ||
                template.FamilyPolicy.Quick.TargetCt >= DclRational.FromInteger(DclCtState.TurnThreshold))
                throw new DclAuthoringLoadException("Quick native policy ticket templates require TargetCt in the pre-turn range.");
            ValidatePropertyStateMaterialization(template.FamilyPolicy.Quick.LockMaterialization);
            return;
        }
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.Revive)
        {
            if (template.FamilyPolicy.Revive is null)
                throw new DclAuthoringLoadException("Revive native policy ticket templates require revive policy.");
            if (template.FamilyPolicy.Revive.Magic is null)
                throw new DclAuthoringLoadException("Revive native policy ticket templates require magic policy.");
            if (template.FamilyPolicy.Revive.FaithMultiplier < DclRational.FromInteger(0))
                throw new DclAuthoringLoadException("Revive native policy ticket templates cannot use a negative FaithMultiplier.");
            ValidateUndeadInteractions(template.FamilyPolicy.Revive.UndeadInteractions);
            if (template.FamilyPolicy.Revive.StoredReraiseMaterialization is not null)
                ValidatePropertyStateMaterialization(template.FamilyPolicy.Revive.StoredReraiseMaterialization);
            return;
        }
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.ForcedMovement)
        {
            if (template.FamilyPolicy.ForcedMovement is null)
                throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates require forcedMovement policy.");
            if (template.FamilyPolicy.ForcedMovement.Magic is null)
                throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates require magic policy.");
            ValidateMovementVerdict(template.FamilyPolicy.ForcedMovement.NativeMovementVerdict);
            return;
        }
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.AreaNumeric)
        {
            ValidateAreaPolicySource(template.FamilyPolicy.AreaNumeric);
            return;
        }
        if (template.FamilyPolicy.Family == DclCanonicalActionFamily.PhysicalDamage)
        {
            ValidatePhysicalPolicySource(template.FamilyPolicy.PhysicalDamage);
            return;
        }

        throw new DclAuthoringLoadException(
            $"Native policy ticket template family {template.FamilyPolicy.Family} is not implemented by the template loader.");
    }

    private static void ValidatePropertyStateMaterialization(
        DclCanonicalNativePropertyStateMaterializationTemplate? template)
    {
        if (template is null)
            throw new DclAuthoringLoadException("StatusApplication native policy ticket templates require state materialization.");
        if (template.DurationUnits < 0)
            throw new DclAuthoringLoadException("StatusApplication native policy ticket templates cannot use negative DurationUnits.");
        if (template.Strength < 0)
            throw new DclAuthoringLoadException("StatusApplication native policy ticket templates cannot use negative Strength.");
        if (string.IsNullOrWhiteSpace(template.StackDiscriminator))
            throw new DclAuthoringLoadException("StatusApplication native policy ticket templates require StackDiscriminator.");
        if (string.IsNullOrWhiteSpace(template.PayloadSchemaId))
            throw new DclAuthoringLoadException("StatusApplication native policy ticket templates require PayloadSchemaId.");
        if (template.PayloadValues is null) return;
        foreach ((string key, string? value) in template.PayloadValues)
        {
            if (string.IsNullOrWhiteSpace(key) || value is null)
                throw new DclAuthoringLoadException("StatusApplication native policy ticket template payload values require nonempty string keys and non-null string values.");
        }
    }

    private static void ValidateUndeadInteractions(DclCanonicalNativeUndeadInteractionTableTemplate? template)
    {
        if (template?.Rules is null)
            throw new DclAuthoringLoadException("Revive native policy ticket templates require Undead interaction rules.");
        try
        {
            _ = new DclUndeadInteractionTable(template.Rules);
        }
        catch (ArgumentException exception)
        {
            throw new DclAuthoringLoadException(
                $"Revive native policy ticket template Undead interactions are invalid: {exception.Message}",
                exception);
        }
    }

    private static void ValidateMovementVerdict(DclCanonicalNativeMovementVerdict? verdict)
    {
        if (verdict is null)
            throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates require a native movement verdict.");
        if (!verdict.Target.IsValid)
            throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates require a valid verdict target.");
        if (!verdict.NativePathResolved)
            throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates require a resolved native path verdict.");
        if (verdict.RequestedTiles < 0 || verdict.MovedTiles < 0)
            throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates cannot use negative movement distances.");
        if (verdict.MovedTiles > verdict.RequestedTiles)
            throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates cannot move farther than requested.");
        if (verdict.RequestedTiles == 0 && verdict.MovedTiles != 0)
            throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates cannot move when zero tiles were requested.");
        if (verdict.MovedTiles == 0 && verdict.Destination != verdict.Origin)
            throw new DclAuthoringLoadException("ForcedMovement native policy ticket templates require blocked zero movement to end at the origin.");
    }

    private static void ValidateAreaPolicySource(DclCanonicalNativeAreaActionPolicySource? source)
    {
        if (source is null)
            throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates require areaNumeric policy.");
        if (string.IsNullOrWhiteSpace(source.Tradition))
            throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates require Tradition.");
        if (source.TraditionSkill < 0)
            throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates cannot use negative TraditionSkill.");
        if (source.ExplicitCasterStatePenaltyMagnitude < 0)
            throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates cannot use negative caster state penalty magnitude.");
        if (source.FixedTileHeight < 0)
            throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates cannot use negative FixedTileHeight.");
        if (source.Targets is null || source.Targets.Count == 0)
            throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates require at least one target policy.");
        foreach (DclCanonicalNativeAreaTargetInputs? target in source.Targets)
        {
            if (target is null)
                throw new DclAuthoringLoadException("AreaNumeric native policy ticket template target entries cannot be null.");
            if (!target.Target.IsValid)
                throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates require valid target identities.");
            if (target.TargetRelativePenaltyMagnitude < 0 ||
                target.ConcentrationStatePenaltyMagnitude < 0 ||
                target.AimRetentionStatePenaltyMagnitude < 0)
                throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates cannot use negative penalty magnitudes.");
            if (target.AuthoredForcedDisplacement < 0)
                throw new DclAuthoringLoadException("AreaNumeric native policy ticket templates cannot use negative authored displacement.");
            if (target.ForcedMovementVerdict is not null)
                ValidateMovementVerdict(target.ForcedMovementVerdict);
        }
    }

    private static void ValidatePhysicalPolicySource(DclCanonicalNativePhysicalActionPolicySource? source)
    {
        if (source is null)
            throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates require physicalDamage policy.");
        if (source.WeaponItemId < 0 || string.IsNullOrWhiteSpace(source.WeaponResourceKey))
            throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates require valid weapon identity.");
        if (source.Targets is null || source.Targets.Count == 0)
            throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates require at least one target policy.");
        if (source.Strikes is null || source.Strikes.Count == 0)
            throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates require at least one Strike policy.");
        foreach (DclCanonicalNativePhysicalTargetInputs? target in source.Targets)
        {
            if (target is null)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket template target entries cannot be null.");
            if (!target.Target.IsValid)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates require valid target identities.");
            if (target.ConcentrationStatePenaltyMagnitude < 0 || target.AimRetentionStatePenaltyMagnitude < 0)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates cannot use negative target penalty magnitudes.");
        }

        foreach (DclCanonicalNativePhysicalStrikeInputs? strike in source.Strikes)
        {
            if (strike is null)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket template Strike entries cannot be null.");
            if (!strike.Target.IsValid)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates require valid Strike target identities.");
            if (strike.StrikeIndex < 0 || strike.BaseWeaponSkill < 0 || strike.AuthoredForcedDisplacement < 0)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates cannot use negative Strike index, skill, or displacement.");
            if (strike.DefenseCandidates is null)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates require explicit defense candidate lists, including when empty.");
            if (strike.Ranged is { } ranged &&
                (ranged.HorizontalTiles < 0 || ranged.LocationPenaltyMagnitude < 0))
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates cannot use negative ranged route distances or penalties.");
            if (strike.InjuryMovementBranches is not null && strike.InjuryMovementBranchForest is not null)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates cannot include both single-origin and conditional-origin Injury movement branches.");
            try
            {
                strike.InjuryMovementBranches?.Validate(strike.Target);
                strike.InjuryMovementBranchForest?.Validate(strike.Target);
            }
            catch (ArgumentException exception)
            {
                throw new DclAuthoringLoadException(
                    $"PhysicalDamage native policy ticket template Injury movement policy is invalid: {exception.Message}",
                    exception);
            }
            if (strike.ExplicitSkillModifier < 0)
                throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates cannot use negative explicit Skill modifiers.");
        }

        if (source.StrikeWeapons is not null)
        {
            foreach (DclCanonicalPhysicalStrikeWeapon? weapon in source.StrikeWeapons)
            {
                if (weapon is null)
                    throw new DclAuthoringLoadException("PhysicalDamage native policy ticket template StrikeWeapon entries cannot be null.");
                if (weapon.StrikeIndex < 0 || weapon.WeaponItemId < 0 || string.IsNullOrWhiteSpace(weapon.WeaponResourceKey))
                    throw new DclAuthoringLoadException("PhysicalDamage native policy ticket templates require valid StrikeWeapon identities.");
            }
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.Converters.Add(new DclRationalJsonConverter());
        return options;
    }
}
