namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclComputePointNumericPlan(
    int HpDebit,
    int HpCredit,
    int MpDebit,
    int MpCredit,
    byte ResultFlags,
    bool WriteHpDebit,
    bool WriteHpCredit,
    bool WriteMpDebit,
    bool WriteMpCredit,
    bool WriteResultFlags)
{
    public static DclComputePointNumericPlan Build(
        int naturalHpDebit,
        int naturalHpCredit,
        int naturalMpDebit,
        int naturalMpCredit,
        byte naturalResultFlags,
        int? authoredHpDebit,
        int? authoredHpCredit,
        int? authoredMpDebit,
        int? authoredMpCredit,
        bool forcedMiss,
        bool controlResultFlags,
        int preserveResultFlagsMask)
    {
        if (forcedMiss)
        {
            // Keep the native connected-result flag until the downstream miss selector consumes it.
            // Zeroing every numeric channel is sufficient to make the miss invisible to AI utility.
            return new DclComputePointNumericPlan(
                0, 0, 0, 0, naturalResultFlags,
                WriteHpDebit: true,
                WriteHpCredit: true,
                WriteMpDebit: true,
                WriteMpCredit: true,
                WriteResultFlags: false);
        }

        int hpDebit = ClampChannel(authoredHpDebit ?? naturalHpDebit);
        int hpCredit = ClampChannel(authoredHpCredit ?? naturalHpCredit);
        int mpDebit = ClampChannel(authoredMpDebit ?? naturalMpDebit);
        int mpCredit = ClampChannel(authoredMpCredit ?? naturalMpCredit);
        byte resultFlags = controlResultFlags
            ? DclResultFlags.Compose(
                naturalResultFlags,
                preserveResultFlagsMask,
                hpDebit,
                hpCredit,
                mpDebit,
                mpCredit)
            : naturalResultFlags;

        return new DclComputePointNumericPlan(
            hpDebit,
            hpCredit,
            mpDebit,
            mpCredit,
            resultFlags,
            WriteHpDebit: authoredHpDebit.HasValue,
            WriteHpCredit: authoredHpCredit.HasValue,
            WriteMpDebit: authoredMpDebit.HasValue,
            WriteMpCredit: authoredMpCredit.HasValue,
            WriteResultFlags: controlResultFlags);
    }

    private static int ClampChannel(int value) => Math.Clamp(value, 0, short.MaxValue);
}
