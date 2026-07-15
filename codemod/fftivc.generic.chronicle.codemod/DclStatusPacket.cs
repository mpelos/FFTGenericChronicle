namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclStatusPacketPlan(byte[] Add, byte[] Remove, byte ResultFlags);

internal static class DclStatusPacket
{
    public const int Width = 5;
    public const byte ResultFlag = 0x08;

    public static DclStatusPacketPlan Compose(
        ReadOnlySpan<byte> oldAdd,
        ReadOnlySpan<byte> oldRemove,
        byte oldResultFlags,
        IReadOnlyList<DclStatusWrite> writes)
    {
        if (oldAdd.Length != Width)
            throw new ArgumentException($"status add packet must contain {Width} bytes", nameof(oldAdd));
        if (oldRemove.Length != Width)
            throw new ArgumentException($"status remove packet must contain {Width} bytes", nameof(oldRemove));
        ArgumentNullException.ThrowIfNull(writes);

        byte[] add = oldAdd.ToArray();
        byte[] remove = oldRemove.ToArray();
        foreach (var write in writes)
        {
            if (write.StatusByteIndex is < 0 or >= Width)
                throw new ArgumentOutOfRangeException(nameof(writes), $"status byte {write.StatusByteIndex} is outside 0..4");

            byte inverse = (byte)~write.StatusMask;
            add[write.StatusByteIndex] &= inverse;
            remove[write.StatusByteIndex] &= inverse;
            if (write.Immune || write.Resisted || write.FailClosed || write.NotSelected)
                continue;
            if (write.Add)
                add[write.StatusByteIndex] |= write.StatusMask;
            else
                remove[write.StatusByteIndex] |= write.StatusMask;
        }

        bool any = add.Any(value => value != 0) || remove.Any(value => value != 0);
        byte flags = any
            ? (byte)(oldResultFlags | ResultFlag)
            : (byte)(oldResultFlags & ~ResultFlag);
        return new DclStatusPacketPlan(add, remove, flags);
    }
}
