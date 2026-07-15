namespace fftivc.generic.chronicle.codemod;

internal static class DclStatusGroupedCarrier
{
    private static readonly DclNativeStatusBit[] MuramasaBits =
    [
        new(1, 0x10), // Confusion
        new(4, 0x01), // Death Sentence
    ];

    public static bool TryGetSuppressedDataBits(int abilityId, out IReadOnlyList<DclNativeStatusBit> bits)
    {
        bits = abilityId switch
        {
            82 => MuramasaBits,
            _ => Array.Empty<DclNativeStatusBit>(),
        };
        return bits.Count > 0;
    }

    public static string RequiredContestMode(int abilityId)
        => abilityId switch
        {
            82 => DclStatusGroups.RandomOne,
            _ => DclStatusGroups.Independent,
        };
}
