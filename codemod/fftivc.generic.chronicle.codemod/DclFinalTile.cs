namespace fftivc.generic.chronicle.codemod;

/// <summary>
/// Immutable copy of one completed native movement. The native hook publishes this snapshot only
/// after the route finalizer has committed the battle unit's final coordinates. No field in the
/// movement actor, route, battle unit, or battle globals is writable through this contract.
/// </summary>
internal readonly record struct DclFinalTileSnapshot(
    long BattleGeneration,
    long Sequence,
    nint ActorPtr,
    nint UnitPtr,
    int MoverTableIndex,
    int MoverCharId,
    int RouteLength,
    int RouteCursor,
    int ActorState,
    DclFinalTilePosition ActorTile,
    DclFinalTilePosition TargetTile,
    DclFinalTilePosition UnitTile,
    int BattleState,
    ulong RouteSignature);

internal readonly record struct DclFinalTilePosition(int X, int Y, int Layer)
{
    public bool IsNativeTile => X is >= 0 and <= 255 && Y is >= 0 and <= 255 && Layer is 0 or 1;
}

internal readonly record struct DclFinalTileValidation(bool Accepted, string Reason);

internal static class DclFinalTileEvent
{
    public const int MovementBattleState = 0x11;

    public static DclFinalTileValidation Validate(DclFinalTileSnapshot snapshot)
    {
        if (snapshot.BattleGeneration <= 0)
            return new(false, "missing-battle-generation");
        if (snapshot.Sequence <= 0)
            return new(false, "invalid-sequence");
        if (snapshot.ActorPtr == 0)
            return new(false, "missing-actor");
        if (snapshot.UnitPtr == 0)
            return new(false, "missing-unit");
        if (snapshot.MoverTableIndex is < 0 or >= 21)
            return new(false, "invalid-unit-index");
        if (snapshot.MoverCharId is < 0 or > 255)
            return new(false, "invalid-char-id");
        if (snapshot.RouteLength is < 0 or > 127)
            return new(false, "invalid-route-length");
        if (snapshot.RouteCursor is < 0 or > 127)
            return new(false, "invalid-route-cursor");
        // The terminal finalizer clears +0xA8 before its safe convergence point but preserves the
        // consumed cursor at +0xA4. Consequently 0/0 is settlement noise, while cursor>0/length=0
        // is the normal post-finalizer representation of a completed nonempty route.
        if (snapshot.RouteCursor == 0 && snapshot.RouteLength == 0)
            return new(false, "no-movement");
        if (snapshot.RouteLength > 0 && snapshot.RouteCursor < snapshot.RouteLength)
            return new(false, "route-not-finished");
        if (snapshot.ActorState != 0)
            return new(false, "actor-not-idle");
        if (!snapshot.ActorTile.IsNativeTile || !snapshot.TargetTile.IsNativeTile || !snapshot.UnitTile.IsNativeTile)
            return new(false, "invalid-tile");
        if (snapshot.ActorTile != snapshot.TargetTile)
            return new(false, "actor-target-mismatch");
        if (snapshot.ActorTile != snapshot.UnitTile)
            return new(false, "actor-unit-mismatch");
        if (snapshot.BattleState != MovementBattleState)
            return new(false, "not-movement-state");
        return new(true, "completed-movement");
    }

    public static ulong ComputeRouteSignature(ReadOnlySpan<byte> routeRecord)
    {
        // FNV-1a is deterministic and allocation-free. This is an event identity aid, not game math.
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        foreach (byte value in routeRecord)
        {
            hash ^= value;
            hash *= prime;
        }
        return hash;
    }

}

/// <summary>Fixed unmanaged ring layout shared by the native writer and managed drain.</summary>
internal static class DclFinalTileNativeLayout
{
    public const int Count = 0x00;
    public const int Events = 0x20;
    public const int RingSize = 64;
    public const int RingMask = RingSize - 1;

    public const int PublishedSequence = 0x00;
    public const int ActorPtr = 0x08;
    public const int UnitPtr = 0x10;
    public const int MoverTableIndex = 0x18;
    public const int MoverCharId = 0x1C;
    public const int RouteLength = 0x20;
    public const int RouteCursor = 0x24;
    public const int ActorState = 0x28;
    public const int ActorX = 0x2C;
    public const int ActorY = 0x30;
    public const int ActorLayer = 0x34;
    public const int TargetX = 0x38;
    public const int TargetY = 0x3C;
    public const int TargetLayer = 0x40;
    public const int UnitX = 0x44;
    public const int UnitY = 0x48;
    public const int UnitLayer = 0x4C;
    public const int RouteRecord = 0x50;
    public const int RouteRecordSize = 0x80;
    public const int BattleState = 0xD0;
    public const int SlotSize = 0xE0;
    public const int BufferSize = Events + (RingSize * SlotSize);
}

internal static class DclFinalTileNativeAsm
{
    private static string Hex(nint address) => $"0{address:X}h";

    /// <summary>
    /// ExecuteFirst at the finalizer's single convergence point, RVA 0xD45A2A2. The finalizer has
    /// already copied actor +0x88/+0x89/+0x8A to battle-unit +0x4F/+0x50/+0x51, and rbx still owns
    /// the movement actor before the security-cookie check and epilogue.
    /// This shim performs only reads from game memory and writes exclusively to the private ring.
    /// </summary>
    public static string[] BuildCaptureShim(nint buffer, nint battleStateGlobal)
    {
        string buf = Hex(buffer);
        string battleState = Hex(battleStateGlobal);
        var asm = new List<string>
        {
            "use64",
            "push rax",
            "push rcx",
            "push rdx",
            "push rsi",
            "push rdi",
            "push r8",
            "push r9",
            "pushfq",
            $"mov rsi, {buf}",
            $"mov ecx, dword [rsi+{DclFinalTileNativeLayout.Count}]",
            "add ecx, 1",
            $"mov dword [rsi+{DclFinalTileNativeLayout.Count}], ecx",
            "mov edx, ecx",
            $"and edx, {DclFinalTileNativeLayout.RingMask}",
            $"imul rdx, rdx, {DclFinalTileNativeLayout.SlotSize}",
            $"lea rdi, [rsi+rdx+{DclFinalTileNativeLayout.Events}]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.PublishedSequence}], 0",
            $"mov qword [rdi+{DclFinalTileNativeLayout.ActorPtr}], rbx",
            $"mov qword [rdi+{DclFinalTileNativeLayout.UnitPtr}], 0",
            $"mov dword [rdi+{DclFinalTileNativeLayout.MoverTableIndex}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.MoverCharId}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.RouteLength}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.RouteCursor}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.ActorState}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.ActorX}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.ActorY}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.ActorLayer}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.TargetX}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.TargetY}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.TargetLayer}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.UnitX}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.UnitY}], -1",
            $"mov dword [rdi+{DclFinalTileNativeLayout.UnitLayer}], -1",
            "test rbx, rbx",
            "jz .final_tile_battle_state",
            "mov r8, qword [rbx+148h]",
            $"mov qword [rdi+{DclFinalTileNativeLayout.UnitPtr}], r8",
            "test r8, r8",
            "jz .final_tile_actor",
            "movzx eax, byte [r8+1]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.MoverTableIndex}], eax",
            "movzx eax, byte [r8]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.MoverCharId}], eax",
            "movzx eax, byte [r8+4Fh]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.UnitX}], eax",
            "movzx eax, byte [r8+50h]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.UnitY}], eax",
            "movzx eax, byte [r8+51h]",
            "shr eax, 7",
            $"mov dword [rdi+{DclFinalTileNativeLayout.UnitLayer}], eax",
            ".final_tile_actor:",
            "movzx eax, byte [rbx+0A8h]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.RouteLength}], eax",
            "mov eax, dword [rbx+0A4h]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.RouteCursor}], eax",
            "movzx eax, byte [rbx+08Bh]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.ActorState}], eax",
            "movzx eax, byte [rbx+088h]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.ActorX}], eax",
            "movzx eax, byte [rbx+089h]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.ActorY}], eax",
            "movzx eax, byte [rbx+08Ah]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.ActorLayer}], eax",
            "movzx eax, byte [rbx+08Ch]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.TargetX}], eax",
            "movzx eax, byte [rbx+08Dh]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.TargetY}], eax",
            "movzx eax, byte [rbx+08Eh]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.TargetLayer}], eax",
        };

        for (int offset = 0; offset < DclFinalTileNativeLayout.RouteRecordSize; offset += 8)
        {
            asm.Add($"mov rax, qword [rbx+0{0xA8 + offset:X}h]");
            asm.Add($"mov qword [rdi+{DclFinalTileNativeLayout.RouteRecord + offset}], rax");
        }

        asm.AddRange(
        [
            ".final_tile_battle_state:",
            $"mov rax, {battleState}",
            "mov eax, dword [rax]",
            $"mov dword [rdi+{DclFinalTileNativeLayout.BattleState}], eax",
            $"mov dword [rdi+{DclFinalTileNativeLayout.PublishedSequence}], ecx",
            "popfq",
            "pop r9",
            "pop r8",
            "pop rdi",
            "pop rsi",
            "pop rdx",
            "pop rcx",
            "pop rax",
        ]);
        return asm.ToArray();
    }
}
