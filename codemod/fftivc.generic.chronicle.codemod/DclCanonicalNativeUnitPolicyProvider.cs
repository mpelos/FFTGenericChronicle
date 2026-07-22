namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalNativeUnitPolicySource(
    DclUnitKey Unit,
    int TileHeight,
    DclCanonicalAttributeAdjustments? NonEquipmentAttributes = null,
    DclSecondaryInputs? NonEquipmentSecondary = null,
    IReadOnlyList<string>? ParryResourceKeys = null,
    bool ExplicitlyEligible = false);

/// <summary>
/// Materializes the explicit per-unit policy inputs required by native snapshot projection. This
/// provider deliberately does not infer job modifiers, tile height, eligibility, or defense
/// resource keys from native effective stats; production callers must supply those policy facts
/// from their owning providers.
/// </summary>
internal static class DclCanonicalNativeUnitPolicyProvider
{
    public static IReadOnlyList<DclCanonicalNativeUnitPolicyInput> BuildForCapturedAction(
        DclCanonicalNativeAdmittedAction action,
        IEnumerable<DclCanonicalNativeUnitPolicySource> sources)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(sources);
        DclCanonicalNativeUnitPolicySource[] supplied = sources.ToArray();
        if (supplied.Select(source => source.Unit).Distinct().Count() != supplied.Length)
            throw new ArgumentException("Unit policy sources cannot contain duplicate UnitKeys.", nameof(sources));

        DclUnitKey[] required = OrderKeys(action.NativeRows.Keys).ToArray();
        DclUnitKey[] suppliedUnits = OrderKeys(supplied.Select(source => source.Unit)).ToArray();
        if (!required.SequenceEqual(suppliedUnits))
            throw new ArgumentException(
                "Unit policy provider requires exactly one explicit policy source per frozen native row.",
                nameof(sources));

        return required.Select(unit =>
        {
            DclCanonicalNativeUnitPolicySource source = supplied.Single(candidate => candidate.Unit == unit);
            if (source.TileHeight < 0)
                throw new ArgumentOutOfRangeException(nameof(sources), "Unit policy tile height cannot be negative.");
            DclCanonicalAttributeAdjustments attributes = source.NonEquipmentAttributes ?? new DclCanonicalAttributeAdjustments();
            if (attributes.EquipmentSt != 0 || attributes.EquipmentDx != 0 || attributes.EquipmentIq != 0)
                throw new ArgumentException(
                    "Unit policy source cannot prepopulate equipment attribute channels.",
                    nameof(sources));
            return new DclCanonicalNativeUnitPolicyInput(
                unit,
                attributes,
                source.NonEquipmentSecondary ?? new DclSecondaryInputs(),
                source.TileHeight,
                source.ParryResourceKeys,
                source.ExplicitlyEligible);
        }).ToArray();
    }

    private static IOrderedEnumerable<DclUnitKey> OrderKeys(IEnumerable<DclUnitKey> units)
        => units.OrderBy(unit => unit.BattleGeneration)
            .ThenBy(unit => unit.UnitSlot)
            .ThenBy(unit => unit.CharacterId);
}
