namespace fftivc.generic.chronicle.codemod;

internal enum DclPrimaryResultPresentation
{
    None = 0,
    HpDamage,
    MpDebit,
    Credit,
    Other,
}

internal static class DclResultFlags
{
    internal const byte HpDamage = 0x80;
    internal const byte HpCredit = 0x40;
    internal const byte MpDebit = 0x20;
    internal const byte MpCredit = 0x10;
    internal const byte NumericMask = HpDamage | HpCredit | MpDebit | MpCredit;
    internal const byte DefaultPreserveMask = 0x0F;

    internal static byte Compose(
        int oldFlags,
        int preserveMask,
        int hpDebit,
        int hpCredit,
        int mpDebit,
        int mpCredit)
    {
        int numeric = 0;
        if (hpDebit > 0) numeric |= HpDamage;
        if (hpCredit > 0) numeric |= HpCredit;
        if (mpDebit > 0) numeric |= MpDebit;
        if (mpCredit > 0) numeric |= MpCredit;
        return (byte)(((oldFlags & 0xFF) & (preserveMask & DefaultPreserveMask)) | numeric);
    }

    // Mirrors the real-code selector at RVA 0x205286..0x2053EF: signed/0x80 first, then 0x20,
    // then the shared 0x40/0x10 credit route. Other low-bit effects sit between those branches.
    internal static DclPrimaryResultPresentation PrimaryPresentation(byte flags)
    {
        if ((flags & HpDamage) != 0) return DclPrimaryResultPresentation.HpDamage;
        if ((flags & MpDebit) != 0) return DclPrimaryResultPresentation.MpDebit;
        if ((flags & 0x0F) != 0) return DclPrimaryResultPresentation.Other;
        if ((flags & (HpCredit | MpCredit)) != 0) return DclPrimaryResultPresentation.Credit;
        return DclPrimaryResultPresentation.None;
    }
}
