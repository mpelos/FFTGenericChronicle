namespace fftivc.generic.chronicle.codemod;

internal static class DclMagicEvade
{
    public static int EvadePercent(int rawEvade, int capPercent)
        => Math.Clamp(rawEvade, 0, Math.Clamp(capPercent, 0, 100));

    public static int HitChancePercent(int rawEvade, int capPercent)
        => 100 - EvadePercent(rawEvade, capPercent);
}
