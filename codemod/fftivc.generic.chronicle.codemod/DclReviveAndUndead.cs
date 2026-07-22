namespace fftivc.generic.chronicle.codemod;

internal enum DclUndeadEffectFamily
{
    DirectHealing,
    Regeneration,
    HpDrain,
    MpDrain,
    RaiseOrArise,
    Reraise,
    Poison,
    InstantKo,
    RestorativeItem,
}

internal sealed record DclUndeadFamilyRule(
    DclUndeadInteraction NormalTarget,
    DclUndeadInteraction UndeadTarget,
    DclUndeadInteraction UndeadCaster);

internal sealed class DclUndeadInteractionTable
{
    private readonly Dictionary<DclUndeadEffectFamily, DclUndeadFamilyRule> _rules;

    public DclUndeadInteractionTable(IReadOnlyDictionary<DclUndeadEffectFamily, DclUndeadFamilyRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = new Dictionary<DclUndeadEffectFamily, DclUndeadFamilyRule>(rules);
        foreach (DclUndeadEffectFamily family in Enum.GetValues<DclUndeadEffectFamily>())
        {
            if (!_rules.TryGetValue(family, out DclUndeadFamilyRule? rule))
                throw new ArgumentException($"Undead interaction family {family} is missing.", nameof(rules));
            ValidateInteraction(rule.NormalTarget, family, nameof(rule.NormalTarget));
            ValidateInteraction(rule.UndeadTarget, family, nameof(rule.UndeadTarget));
            ValidateInteraction(rule.UndeadCaster, family, nameof(rule.UndeadCaster));
        }
    }

    public DclUndeadFamilyRule Rule(DclUndeadEffectFamily family) => _rules[family];

    public DclUndeadInteraction TargetInteraction(DclUndeadEffectFamily family, bool targetUndead)
        => targetUndead ? Rule(family).UndeadTarget : Rule(family).NormalTarget;

    public DclUndeadInteraction CasterInteraction(DclUndeadEffectFamily family, bool casterUndead)
        => casterUndead ? Rule(family).UndeadCaster : DclUndeadInteraction.Normal;

    private static void ValidateInteraction(
        DclUndeadInteraction interaction,
        DclUndeadEffectFamily family,
        string axis)
    {
        if (interaction == DclUndeadInteraction.Unknown)
            throw new ArgumentException($"Undead family {family} has an unknown {axis} interaction.");
    }
}

internal enum DclReviveMode
{
    Immediate,
    StoredReraise,
}

internal enum DclReviveFaithAxis
{
    None,
    Success,
    RestoredHp,
    BothExplicit,
}

internal sealed record DclReviveProfile(
    DclEligibleTargetStates EligibleTargetStates,
    DclReviveMode Mode,
    DclReviveFaithAxis FaithAxis,
    bool PaysForBothFaithAxes,
    DclUndeadEffectFamily UndeadFamily);

internal enum DclReviveRoute
{
    NotEligible,
    DeliveryFailed,
    RejectedByUndeadRule,
    ImmediateHpCredit,
    StoredReraise,
    UndeadEffectOwned,
}

internal readonly record struct DclReviveResult(
    DclReviveRoute Route,
    int RawRestoredHp,
    int FinalRestoredHp,
    int AppliedHpCredit,
    bool StoreReraise,
    bool ClearKoAfterPositiveCredit,
    string Reason);

internal static class DclReviveRules
{
    public static void Validate(DclReviveProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.EligibleTargetStates == DclEligibleTargetStates.None)
            throw new ArgumentException("Revive requires explicit eligible target states.", nameof(profile));
        if (profile.FaithAxis == DclReviveFaithAxis.BothExplicit && !profile.PaysForBothFaithAxes)
            throw new ArgumentException("Faith may improve both success and restored HP only when the profile explicitly pays for both axes.", nameof(profile));
        if (profile.FaithAxis != DclReviveFaithAxis.BothExplicit && profile.PaysForBothFaithAxes)
            throw new ArgumentException("PaysForBothFaithAxes is legal only with BothExplicit.", nameof(profile));
        DclUndeadEffectFamily expected = profile.Mode == DclReviveMode.StoredReraise
            ? DclUndeadEffectFamily.Reraise
            : DclUndeadEffectFamily.RaiseOrArise;
        if (profile.UndeadFamily != expected)
            throw new ArgumentException("Revive mode and undead effect family must agree.", nameof(profile));
    }

    public static DclReviveResult Resolve(
        DclReviveProfile profile,
        DclUndeadInteractionTable undeadTable,
        DclEligibleTargetStates currentTargetStates,
        bool targetUndead,
        bool deliverySucceeded,
        int rawRestoredHp,
        DclRational faithMultiplier,
        int currentHp,
        int maxHp)
    {
        Validate(profile);
        ArgumentNullException.ThrowIfNull(undeadTable);
        if (rawRestoredHp < 0 || currentHp < 0 || maxHp < 1 || currentHp > maxHp ||
            faithMultiplier < DclRational.FromInteger(0))
            throw new ArgumentOutOfRangeException(nameof(rawRestoredHp));
        if ((profile.EligibleTargetStates & currentTargetStates) == 0)
            return Empty(DclReviveRoute.NotEligible, "target-state-not-eligible");
        if (!deliverySucceeded)
            return Empty(DclReviveRoute.DeliveryFailed, "delivery-failed");

        DclUndeadInteraction interaction = undeadTable.TargetInteraction(profile.UndeadFamily, targetUndead);
        if (interaction == DclUndeadInteraction.Reject)
            return Empty(DclReviveRoute.RejectedByUndeadRule, "undead-rule-rejected");
        if (interaction is DclUndeadInteraction.Harm or DclUndeadInteraction.Heal or
            DclUndeadInteraction.Reverse or DclUndeadInteraction.EffectOwned)
            return Empty(DclReviveRoute.UndeadEffectOwned, "undead-rule-routes-to-explicit-effect");

        if (profile.Mode == DclReviveMode.StoredReraise)
            return new DclReviveResult(
                DclReviveRoute.StoredReraise,
                0,
                0,
                0,
                StoreReraise: true,
                ClearKoAfterPositiveCredit: false,
                "store-reraise-trigger");

        bool faithModifiesHp = profile.FaithAxis is DclReviveFaithAxis.RestoredHp or DclReviveFaithAxis.BothExplicit;
        int final = faithModifiesHp
            ? checked((int)(DclRational.FromInteger(rawRestoredHp) * faithMultiplier).Floor())
            : rawRestoredHp;
        int applied = Math.Min(final, maxHp - currentHp);
        return new DclReviveResult(
            DclReviveRoute.ImmediateHpCredit,
            rawRestoredHp,
            final,
            applied,
            StoreReraise: false,
            ClearKoAfterPositiveCredit: applied > 0,
            applied > 0 ? "native-hp-credit-then-clear-ko" : "zero-credit-does-not-clear-ko");
    }

    private static DclReviveResult Empty(DclReviveRoute route, string reason)
        => new(route, 0, 0, 0, false, false, reason);
}
