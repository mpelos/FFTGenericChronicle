using System.Runtime.InteropServices;

namespace fftivc.generic.chronicle.codemod;

internal sealed class DclInterruptRule
{
    public string Name { get; set; } = "";
    public int AbilityId { get; set; } = -1;
    public int ActionType { get; set; } = -1;
    public string ConditionFormula { get; set; } = "";
    public string ResistanceFormula { get; set; } = "";

    public string DisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"ability-{AbilityId}-interrupt"
        : Name.Trim();

    public bool TryMatches(int actionType, int abilityId, FormulaContext context, out bool matches, out string error)
    {
        matches = false;
        error = "";
        if (AbilityId != abilityId)
            return true;
        if (ActionType >= 0 && ActionType != actionType)
            return true;

        if (!string.IsNullOrWhiteSpace(ConditionFormula))
        {
            if (!FormulaExpression.TryEvaluate(ConditionFormula, context, out int value, out error))
                return false;
            matches = value != 0;
            return true;
        }

        matches = true;
        return true;
    }
}

internal readonly record struct DclPendingCancellationState(
    byte EffectiveFlags,
    byte SourceFlags,
    byte Timer,
    byte ActionType,
    ushort ActionId,
    byte MasterFlags)
{
    public const byte ChargingBit = 0x08;
    public bool EffectiveCharging => (EffectiveFlags & ChargingBit) != 0;
    public bool MasterCharging => (MasterFlags & ChargingBit) != 0;
    public bool SourceCharging => (SourceFlags & ChargingBit) != 0;
    public bool IsLivePendingAction =>
        EffectiveCharging &&
        MasterCharging &&
        !SourceCharging &&
        Timer != 0xFF &&
        ActionId > 0;

    public string Describe()
        => $"effective=0x{EffectiveFlags:X2}/source=0x{SourceFlags:X2}/timer={Timer}/" +
           $"type=0x{ActionType:X2}/action={ActionId}/master=0x{MasterFlags:X2}";
}

internal readonly record struct DclPendingCancellationResult(
    bool Eligible,
    bool Applied,
    bool LogOnly,
    string Reason,
    DclPendingCancellationState Before,
    DclPendingCancellationState After);

internal static class DclPendingCancellation
{
    public const int EffectiveFlagsOffset = 0x61;
    public const int SourceFlagsOffset = 0x57;
    public const int TimerOffset = 0x18D;
    public const int ActionTypeOffset = 0x1A1;
    public const int ActionIdOffset = 0x1A2;
    public const int MasterFlagsOffset = 0x1EF;
    public const byte CancelledTimer = 0xFF;

    public static DclPendingCancellationState Read(nint unitPtr)
    {
        if (unitPtr == 0)
            throw new ArgumentOutOfRangeException(nameof(unitPtr));
        return new DclPendingCancellationState(
            Marshal.ReadByte(unitPtr, EffectiveFlagsOffset),
            Marshal.ReadByte(unitPtr, SourceFlagsOffset),
            Marshal.ReadByte(unitPtr, TimerOffset),
            Marshal.ReadByte(unitPtr, ActionTypeOffset),
            unchecked((ushort)Marshal.ReadInt16(unitPtr, ActionIdOffset)),
            Marshal.ReadByte(unitPtr, MasterFlagsOffset));
    }

    public static bool TryCancel(
        nint unitPtr,
        bool logOnly,
        out DclPendingCancellationResult result,
        out string error)
    {
        result = default;
        error = "";
        try
        {
            var before = Read(unitPtr);
            if (!before.IsLivePendingAction)
            {
                string reason = before.SourceCharging
                    ? "charging-bit-is-source-owned"
                    : before.ActionId <= 0
                        ? "no-action-id"
                        : before.Timer == CancelledTimer
                            ? "timer-already-cancelled"
                            : !before.EffectiveCharging || !before.MasterCharging
                                ? "charging-mirrors-disagree-or-clear"
                                : "not-pending";
                result = new DclPendingCancellationResult(false, false, logOnly, reason, before, before);
                return true;
            }

            if (logOnly)
            {
                result = new DclPendingCancellationResult(true, false, true, "eligible-log-only", before, before);
                return true;
            }

            // This callback runs synchronously on the game's outer result thread. Write the native
            // non-runnable sentinel first, then clear only Charging from durable/effective mirrors.
            // The broader native 0xF2/0xF6 cleanup masks also remove neighboring state families and
            // are intentionally not reproduced by an ability-specific Interrupt.
            Marshal.WriteByte(unitPtr, TimerOffset, CancelledTimer);
            Marshal.WriteByte(unitPtr, MasterFlagsOffset,
                unchecked((byte)(before.MasterFlags & ~DclPendingCancellationState.ChargingBit)));
            Marshal.WriteByte(unitPtr, EffectiveFlagsOffset,
                unchecked((byte)(before.EffectiveFlags & ~DclPendingCancellationState.ChargingBit)));

            var after = Read(unitPtr);
            byte preservedEffective = unchecked((byte)(before.EffectiveFlags & ~DclPendingCancellationState.ChargingBit));
            byte preservedMaster = unchecked((byte)(before.MasterFlags & ~DclPendingCancellationState.ChargingBit));
            bool verified =
                after.Timer == CancelledTimer &&
                after.EffectiveFlags == preservedEffective &&
                after.MasterFlags == preservedMaster &&
                after.ActionType == before.ActionType &&
                after.ActionId == before.ActionId;
            result = new DclPendingCancellationResult(
                Eligible: true,
                Applied: verified,
                LogOnly: false,
                Reason: verified ? "cancelled-and-verified" : "write-verification-failed",
                Before: before,
                After: after);
            if (!verified)
                error = "pending cancellation read-back did not preserve the exact three-field contract";
            return verified;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
