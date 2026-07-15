namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclNativeStatusBit(int ByteIndex, byte Mask);

internal static class DclStatusNativeCarrier
{
    private static readonly DclNativeStatusBit[] KiyomoriBits =
    [
        new(3, 0x20), // Protect
        new(3, 0x10), // Shell
    ];

    private static readonly DclNativeStatusBit[] MasamuneBits =
    [
        new(3, 0x40), // Regen
        new(3, 0x08), // Haste
    ];

    private static readonly DclNativeStatusBit[] SalveBits =
    [
        new(1, 0x20), // Darkness
        new(1, 0x08), // Silence
        new(3, 0x80), // Poison
    ];

    private static readonly DclNativeStatusBit[] PetrifyBits = [new(1, 0x80)];
    private static readonly DclNativeStatusBit[] StopBits = [new(3, 0x02)];
    private static readonly DclNativeStatusBit[] DontActBits = [new(4, 0x04)];
    private static readonly DclNativeStatusBit[] DontMoveBits = [new(4, 0x08)];
    private static readonly DclNativeStatusBit[] DarknessBits = [new(1, 0x20)];
    private static readonly DclNativeStatusBit[] SilenceBits = [new(1, 0x08)];
    private static readonly DclNativeStatusBit[] ConfusionBits = [new(1, 0x10)];
    private static readonly DclNativeStatusBit[] SlowBits = [new(3, 0x04)];
    private static readonly DclNativeStatusBit[] ReflectBits = [new(4, 0x02)];

    private static readonly DclNativeStatusBit[] NightmareBits =
    [
        new(4, 0x10), // Sleep
        new(4, 0x01), // Death Sentence
    ];

    private static readonly DclNativeStatusBit[] TootBits =
    [
        new(1, 0x10), // Confusion
        new(4, 0x10), // Sleep
    ];

    private static readonly DclNativeStatusBit[] PoisonousFrogBits =
    [
        new(2, 0x02), // Frog
        new(3, 0x80), // Poison
    ];

    private static readonly DclNativeStatusBit[] DischordBits =
    [
        new(2, 0x40), // Float
        new(2, 0x20), // Reraise
        new(2, 0x10), // Transparent
        new(3, 0x40), // Regen
        new(3, 0x20), // Protect
        new(3, 0x10), // Shell
        new(3, 0x08), // Haste
        new(4, 0x80), // Faith
        new(4, 0x02), // Reflect
    ];

    private static readonly DclNativeStatusBit[] BadBreathBits =
    [
        new(1, 0x80), // Petrify
        new(1, 0x20), // Darkness
        new(1, 0x10), // Confusion
        new(1, 0x08), // Silence
        new(2, 0x80), // Oil
        new(2, 0x02), // Frog
        new(3, 0x80), // Poison
        new(4, 0x10), // Sleep
    ];

    private static readonly DclNativeStatusBit[] GrandCrossBits =
    [
        new(1, 0x80), // Petrify
        new(1, 0x20), // Darkness
        new(1, 0x10), // Confusion
        new(1, 0x08), // Silence
        new(2, 0x08), // Berserk
        new(2, 0x02), // Frog
        new(3, 0x80), // Poison
        new(3, 0x04), // Slow
        new(4, 0x10), // Sleep
    ];

    public static bool TryGetRequiredBits(int abilityId, out IReadOnlyList<DclNativeStatusBit> bits)
    {
        bits = abilityId switch
        {
            81 => KiyomoriBits,
            84 => MasamuneBits,
            149 => SalveBits,
            181 or 187 => PetrifyBits,
            182 or 193 => StopBits,
            188 => DontActBits,
            189 or 327 => DontMoveBits,
            190 => DarknessBits,
            191 => SilenceBits,
            192 => ConfusionBits,
            194 => NightmareBits,
            195 => SlowBits,
            287 => DischordBits,
            313 => TootBits,
            326 => ReflectBits,
            328 or 356 => BadBreathBits,
            346 => PoisonousFrogBits,
            350 => GrandCrossBits,
            _ => Array.Empty<DclNativeStatusBit>(),
        };
        return bits.Count > 0;
    }

    public static string RequiredContestMode(int abilityId)
        => abilityId switch
        {
            194 or 313 => DclStatusGroups.RandomOne,
            346 => DclStatusGroups.AllOrNothing,
            _ => DclStatusGroups.Independent,
        };
}
