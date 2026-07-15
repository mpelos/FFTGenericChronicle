namespace fftivc.generic.chronicle.codemod;

internal static class DclStatusConditionalCarrier
{
    public const int SelfDestructAbilityId = 277;
    public const string SelfDestructVictimCondition = "dcl.isSelf == 0";

    public static bool TryGetRequiredBit(int abilityId, out DclNativeStatusBit bit)
    {
        if (abilityId == SelfDestructAbilityId)
        {
            bit = new DclNativeStatusBit(2, 0x80); // Oil
            return true;
        }

        bit = default;
        return false;
    }

    public static bool IsRequiredCondition(int abilityId, string formula)
    {
        if (abilityId != SelfDestructAbilityId)
            return false;
        string normalized = string.Concat((formula ?? "").Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
        return normalized == "dcl.isself==0";
    }
}
