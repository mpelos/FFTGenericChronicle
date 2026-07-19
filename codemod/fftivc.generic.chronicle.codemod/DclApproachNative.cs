namespace fftivc.generic.chronicle.codemod;

internal enum DclApproachNativeState
{
    Idle = 0,
    PendingDecision = 1,
    Release = 2,
    InvokeQueue = 3,
    QueueAccepted = 4,
    QueueRejected = 5,
    ResumeWritten = 6,
    ResumeReleased = 7,
    Aborted = 8,
}

/// <summary>
/// Shared unmanaged control block. The game-thread shim publishes State last; the managed poller
/// publishes a command by writing all command fields before State. All offsets are fixed and tested.
/// </summary>
internal static class DclApproachNativeLayout
{
    public const int State = 0x00;
    public const int Sequence = 0x04;
    public const int Outcome = 0x08;
    public const int ControlWrites = 0x0C;
    public const int ActorPtr = 0x10;
    public const int UnitPtr = 0x18;
    public const int MoverTableIndex = 0x20;
    public const int MoverCharId = 0x24;
    public const int RouteLength = 0x28;
    public const int RouteCursor = 0x2C;
    public const int TileX = 0x30;
    public const int TileY = 0x34;
    public const int TileLayer = 0x38;
    public const int DeliveryReactionId = 0x3C;
    public const int CandidateMask = 0x40;
    public const int CommitMask = 0x44;
    public const int BattleGeneration = 0x48;
    public const int DecisionDeadlineTick = 0x50;
    public const int RouteSignature = 0x58;
    public const int RouteRecord = 0x60;
    public const int RouteRecordSize = 0x80;
    public const int PauseCallCount = 0xE0;
    public const int MaximumPauseCalls = 0xE4;
    public const int SourceTargetMarkBefore = 0xE8;
    public const int SourceTargetMarkForced = 0xEC;
    public const int SourceTargetMarkRestored = 0xF0;
    public const int QueueValidationStage = 0xF4;
    public const int QueueUnitTile = 0xF8;
    public const int QueueMapDimensions = 0xFC;
    public const int SourceUnitTileBefore = 0x100;
    public const int SourceUnitTileForced = 0x104;
    public const int SourceUnitTileRestored = 0x108;
    public const int Size = 0x110;
}

internal static class DclApproachNativeAsm
{
    private static string Hex(nint address) => $"0{address:X}h";

    /// <summary>
    /// ExecuteFirst at movement boundary 0x1FE793. The original instruction stream is used for a
    /// release. A pending/accepted event returns through updater epilogue 0x1FE940 before consuming
    /// another route byte. Queue pass 2 is invoked directly so unrelated pass-0/1 work cannot win.
    /// The typed source-target helper reads the source battle-unit coordinates before requiring
    /// target-map bit 0x40 on that tile. Native movement defers those coordinates until route release,
    /// so the shim lends the actor's already-entered tile to the unit and lends exactly the target bit
    /// for the synchronous queue/actor-construction call, then restores both byte-exactly.
    /// </summary>
    public static string[] BuildBoundaryShim(
        nint buffer,
        nint movementUpdaterEpilogue,
        nint reactionQueue,
        nint battleStateGlobal,
        nint reactionQueuePassGlobal,
        nint reactionSourceIndexGlobal,
        nint unitTable,
        nint mapWidthGlobal,
        nint tileTable)
    {
        string buf = Hex(buffer);
        string epilogue = Hex(movementUpdaterEpilogue);
        string queue = Hex(reactionQueue);
        string battleState = Hex(battleStateGlobal);
        string queuePass = Hex(reactionQueuePassGlobal);
        string sourceIndex = Hex(reactionSourceIndexGlobal);
        string table = Hex(unitTable);
        string mapWidth = Hex(mapWidthGlobal);
        string tiles = Hex(tileTable);
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
            "push r10",
            "push r11",
            "pushfq",
            $"mov rsi, {buf}",
            $"mov eax, dword [rsi+{DclApproachNativeLayout.State}]",
            $"cmp eax, {(int)DclApproachNativeState.Idle}",
            "je .approach_publish",
            $"cmp eax, {(int)DclApproachNativeState.PendingDecision}",
            "je .approach_pending",
            $"cmp eax, {(int)DclApproachNativeState.Release}",
            "je .approach_release",
            $"cmp eax, {(int)DclApproachNativeState.InvokeQueue}",
            "je .approach_queue",
            $"cmp eax, {(int)DclApproachNativeState.QueueAccepted}",
            "je .approach_pause",
            $"cmp eax, {(int)DclApproachNativeState.ResumeWritten}",
            "je .approach_resume_release",
            $"cmp eax, {(int)DclApproachNativeState.QueueRejected}",
            "je .approach_republish",
            $"cmp eax, {(int)DclApproachNativeState.ResumeReleased}",
            "je .approach_republish",
            "jmp .approach_fail_open",

            ".approach_pending:",
            $"add dword [rsi+{DclApproachNativeLayout.PauseCallCount}], 1",
            $"mov ecx, dword [rsi+{DclApproachNativeLayout.MaximumPauseCalls}]",
            "test ecx, ecx",
            "jz .approach_pending_timeout",
            $"cmp dword [rsi+{DclApproachNativeLayout.PauseCallCount}], ecx",
            "jb .approach_pause",
            ".approach_pending_timeout:",
            $"mov dword [rsi+{DclApproachNativeLayout.Outcome}], -4",
            $"mov dword [rsi+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.QueueRejected}",
            "jmp .approach_original",

            ".approach_release:",
            $"mov dword [rsi+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.Idle}",
            "jmp .approach_original",

            ".approach_resume_release:",
            $"mov dword [rsi+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.ResumeReleased}",
            "jmp .approach_original",

            ".approach_republish:",
            $"mov dword [rsi+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.Idle}",
            "jmp .approach_publish",

            ".approach_fail_open:",
            $"mov dword [rsi+{DclApproachNativeLayout.Outcome}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.Idle}",
            "jmp .approach_original",

            ".approach_publish:",
            $"add dword [rsi+{DclApproachNativeLayout.Sequence}], 1",
            $"mov dword [rsi+{DclApproachNativeLayout.Outcome}], 0",
            $"mov dword [rsi+{DclApproachNativeLayout.DeliveryReactionId}], 0",
            $"mov dword [rsi+{DclApproachNativeLayout.CandidateMask}], 0",
            $"mov dword [rsi+{DclApproachNativeLayout.CommitMask}], 0",
            $"mov dword [rsi+{DclApproachNativeLayout.PauseCallCount}], 0",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceTargetMarkBefore}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceTargetMarkForced}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceTargetMarkRestored}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueUnitTile}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueMapDimensions}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceUnitTileBefore}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceUnitTileForced}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceUnitTileRestored}], -1",
            $"mov qword [rsi+{DclApproachNativeLayout.ActorPtr}], rbx",
            "mov rdi, qword [rbx+148h]",
            $"mov qword [rsi+{DclApproachNativeLayout.UnitPtr}], rdi",
            "test rdi, rdi",
            "jz .approach_publish_no_unit",
            // The movement actor's +0x148 points at the 0x200-byte battle-unit record. Byte +1 is
            // that record's physical 0..20 table slot; actor/unit +8 is unrelated runtime data.
            "movzx eax, byte [rdi+1]",
            $"mov dword [rsi+{DclApproachNativeLayout.MoverTableIndex}], eax",
            "movzx eax, byte [rdi]",
            $"mov dword [rsi+{DclApproachNativeLayout.MoverCharId}], eax",
            "jmp .approach_publish_route",
            ".approach_publish_no_unit:",
            $"mov dword [rsi+{DclApproachNativeLayout.MoverTableIndex}], -1",
            $"mov dword [rsi+{DclApproachNativeLayout.MoverCharId}], -1",
            ".approach_publish_route:",
            "movzx eax, byte [rbx+0A8h]",
            $"mov dword [rsi+{DclApproachNativeLayout.RouteLength}], eax",
            "mov eax, dword [rbx+0A4h]",
            $"mov dword [rsi+{DclApproachNativeLayout.RouteCursor}], eax",
            "movzx eax, byte [rbx+088h]",
            $"mov dword [rsi+{DclApproachNativeLayout.TileX}], eax",
            "movzx eax, byte [rbx+089h]",
            $"mov dword [rsi+{DclApproachNativeLayout.TileY}], eax",
            "movzx eax, byte [rbx+08Ah]",
            $"mov dword [rsi+{DclApproachNativeLayout.TileLayer}], eax",
        };
        for (int offset = 0; offset < DclApproachNativeLayout.RouteRecordSize; offset += 8)
        {
            asm.Add($"mov rax, qword [rbx+0{0xA8 + offset:X}h]");
            asm.Add($"mov qword [rsi+{DclApproachNativeLayout.RouteRecord + offset}], rax");
        }
        asm.AddRange(
        [
            $"mov dword [rsi+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.PendingDecision}",
            "jmp .approach_pause",

            ".approach_queue:",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 0",
            // Revalidate the exact paused boundary before any native call or global write.
            $"cmp qword [rsi+{DclApproachNativeLayout.ActorPtr}], rbx",
            "jne .approach_queue_abort",
            "mov rdi, qword [rbx+148h]",
            $"cmp qword [rsi+{DclApproachNativeLayout.UnitPtr}], rdi",
            "jne .approach_queue_abort",
            "test rdi, rdi",
            "jz .approach_queue_abort",
            "movzx eax, byte [rdi+1]",
            $"cmp eax, dword [rsi+{DclApproachNativeLayout.MoverTableIndex}]",
            "jne .approach_queue_abort",
            "movzx eax, byte [rdi]",
            $"cmp eax, dword [rsi+{DclApproachNativeLayout.MoverCharId}]",
            "jne .approach_queue_abort",
            "movzx eax, byte [rbx+0A8h]",
            $"cmp eax, dword [rsi+{DclApproachNativeLayout.RouteLength}]",
            "jne .approach_queue_abort",
            "mov eax, dword [rbx+0A4h]",
            $"cmp eax, dword [rsi+{DclApproachNativeLayout.RouteCursor}]",
            "jne .approach_queue_abort",
            "movzx eax, byte [rbx+088h]",
            $"cmp eax, dword [rsi+{DclApproachNativeLayout.TileX}]",
            "jne .approach_queue_abort",
            "movzx eax, byte [rbx+089h]",
            $"cmp eax, dword [rsi+{DclApproachNativeLayout.TileY}]",
            "jne .approach_queue_abort",
            "movzx eax, byte [rbx+08Ah]",
            $"cmp eax, dword [rsi+{DclApproachNativeLayout.TileLayer}]",
            "jne .approach_queue_abort",
            "movzx edx, byte [rdi+4Fh]",
            "movzx eax, byte [rdi+50h]",
            "shl eax, 8",
            "or edx, eax",
            "movzx eax, byte [rdi+51h]",
            "shl eax, 24",
            "or edx, eax",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueUnitTile}], edx",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceUnitTileBefore}], edx",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 1",
            $"mov rax, {battleState}",
            "cmp dword [rax], 11h",
            "jne .approach_queue_abort",
            $"mov r9d, dword [rsi+{DclApproachNativeLayout.CandidateMask}]",
            "test r9d, r9d",
            "jz .approach_queue_abort",
            $"mov r10d, dword [rsi+{DclApproachNativeLayout.DeliveryReactionId}]",
            "cmp r10d, 422",
            "jl .approach_queue_abort",
            "cmp r10d, 453",
            "jg .approach_queue_abort",
            $"mov rdi, {table}",
            "xor r8d, r8d",
            ".approach_mailbox_check:",
            // Pass 2 excludes the current source index before reading unit+0x1CE. Preserve that
            // source-owned word exactly; it cannot participate in this queue invocation.
            $"cmp r8d, dword [rsi+{DclApproachNativeLayout.MoverTableIndex}]",
            "jne .approach_mailbox_check_candidate",
            "mov eax, r9d",
            "mov ecx, r8d",
            "shr eax, cl",
            "and eax, 1",
            "test eax, eax",
            "jnz .approach_queue_abort",
            "jmp .approach_mailbox_next",
            ".approach_mailbox_check_candidate:",
            "mov eax, r9d",
            "mov ecx, r8d",
            "shr eax, cl",
            "and eax, 1",
            "movzx edx, word [rdi+1CEh]",
            "test eax, eax",
            "jz .approach_mailbox_expect_empty",
            "cmp edx, r10d",
            "jne .approach_queue_abort",
            "jmp .approach_mailbox_next",
            ".approach_mailbox_expect_empty:",
            "test edx, edx",
            "jne .approach_queue_abort",
            ".approach_mailbox_next:",
            "add rdi, 200h",
            "add r8d, 1",
            "cmp r8d, 21",
            "jl .approach_mailbox_check",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 2",
            // Force pass 2: pass 0/1 belong to unrelated queue families.
            $"mov rax, {queuePass}",
            "mov dword [rax], 2",
            $"mov rax, {sourceIndex}",
            $"mov ecx, dword [rsi+{DclApproachNativeLayout.MoverTableIndex}]",
            "mov dword [rax], ecx",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 3",
            // Counter's typed helper reads the source unit's coordinates for both its tile lookup and
            // output order. At this boundary the actor is already on the entered tile while the unit
            // record still holds the route origin. Validate both coordinate tuples, lend the entered
            // tuple plus target bit only through synchronous order/actor construction, then restore.
            $"mov rdi, qword [rsi+{DclApproachNativeLayout.UnitPtr}]",
            $"mov r10, {mapWidth}",
            "movzx r10d, byte [r10]",
            "mov r9d, r10d",
            "mov edx, r10d",
            $"mov r10, {mapWidth}",
            "movzx ecx, byte [r10+1]",
            "shl ecx, 8",
            "or edx, ecx",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueMapDimensions}], edx",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 4",
            $"mov r10, {mapWidth}",
            "movzx r10d, byte [r10]",
            "test r10d, r10d",
            "jz .approach_queue_abort",
            // The original deferred unit tuple must itself be a real map tile before it is loaned.
            "movzx ecx, byte [rdi+4Fh]",
            "cmp ecx, r10d",
            "jae .approach_queue_abort",
            $"mov r10, {mapWidth}",
            "movzx edx, byte [r10+1]",
            "test edx, edx",
            "jz .approach_queue_abort",
            "movzx eax, byte [rdi+50h]",
            "cmp eax, edx",
            "jae .approach_queue_abort",
            // The actor's entered tuple is authoritative for this Approach interruption.
            $"mov ecx, dword [rsi+{DclApproachNativeLayout.TileX}]",
            "cmp ecx, r9d",
            "jae .approach_queue_abort",
            $"mov eax, dword [rsi+{DclApproachNativeLayout.TileY}]",
            "cmp eax, edx",
            "jae .approach_queue_abort",
            $"mov edx, dword [rsi+{DclApproachNativeLayout.TileLayer}]",
            "cmp edx, 1",
            "ja .approach_queue_abort",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 5",
            // Lend actor X/Y and only the level bit; preserve the unit's facing/low bits exactly.
            "mov byte [rdi+4Fh], cl",
            $"mov ecx, dword [rsi+{DclApproachNativeLayout.TileY}]",
            "mov byte [rdi+50h], cl",
            "movzx ecx, byte [rdi+51h]",
            "and ecx, 7Fh",
            "shl edx, 7",
            "or ecx, edx",
            "mov byte [rdi+51h], cl",
            "movzx edx, byte [rdi+4Fh]",
            "movzx ecx, byte [rdi+50h]",
            "shl ecx, 8",
            "or edx, ecx",
            "movzx ecx, byte [rdi+51h]",
            "shl ecx, 24",
            "or edx, ecx",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceUnitTileForced}], edx",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 6",
            // Compute the dynamic-mark record from the same entered tuple now visible to the helper.
            $"mov eax, dword [rsi+{DclApproachNativeLayout.TileY}]",
            $"mov ecx, dword [rsi+{DclApproachNativeLayout.TileX}]",
            $"mov r10, {mapWidth}",
            "movzx r10d, byte [r10]",
            "imul eax, r10d",
            "add eax, ecx",
            $"mov ecx, dword [rsi+{DclApproachNativeLayout.TileLayer}]",
            "shl ecx, 8",
            "add eax, ecx",
            "shl rax, 3",
            $"mov r10, {tiles}",
            "add r10, rax",
            "movzx eax, byte [r10+5]",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceTargetMarkBefore}], eax",
            "or byte [r10+5], 40h",
            "movzx eax, byte [r10+5]",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceTargetMarkForced}], eax",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 7",
            "sub rsp, 40h",
            "mov qword [rsp+20h], r10",
            $"mov rax, {queue}",
            "call rax",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 8",
            "mov r10, qword [rsp+20h]",
            $"mov ecx, dword [rsi+{DclApproachNativeLayout.SourceTargetMarkBefore}]",
            "test ecx, 40h",
            "jnz .approach_target_mark_restore_set",
            "and byte [r10+5], 0BFh",
            "jmp .approach_target_mark_restored",
            ".approach_target_mark_restore_set:",
            "or byte [r10+5], 40h",
            ".approach_target_mark_restored:",
            "movzx ecx, byte [r10+5]",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceTargetMarkRestored}], ecx",
            // Restore the deferred route-origin tuple byte-exactly without clobbering queue eax.
            $"mov edx, dword [rsi+{DclApproachNativeLayout.SourceUnitTileBefore}]",
            "mov byte [rdi+4Fh], dl",
            "mov ecx, edx",
            "shr ecx, 8",
            "mov byte [rdi+50h], cl",
            "shr edx, 24",
            "mov byte [rdi+51h], dl",
            "movzx edx, byte [rdi+4Fh]",
            "movzx ecx, byte [rdi+50h]",
            "shl ecx, 8",
            "or edx, ecx",
            "movzx ecx, byte [rdi+51h]",
            "shl ecx, 24",
            "or edx, ecx",
            $"mov dword [rsi+{DclApproachNativeLayout.SourceUnitTileRestored}], edx",
            $"mov dword [rsi+{DclApproachNativeLayout.QueueValidationStage}], 9",
            "add rsp, 40h",
            "test eax, eax",
            "jz .approach_queue_rejected",
            $"mov dword [rsi+{DclApproachNativeLayout.Outcome}], 2",
            $"mov dword [rsi+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.QueueAccepted}",
            "jmp .approach_pause",

            ".approach_queue_abort:",
            $"mov dword [rsi+{DclApproachNativeLayout.Outcome}], -2",
            "jmp .approach_queue_clear",
            ".approach_queue_rejected:",
            $"mov dword [rsi+{DclApproachNativeLayout.Outcome}], 3",
            ".approach_queue_clear:",
            // Initial revalidation requires every unowned, non-source mailbox to be zero. The
            // selector-excluded source mailbox is never in the armed bitset, so clearing only that
            // bitset restores the exact pre-command mailbox state.
            $"mov r9d, dword [rsi+{DclApproachNativeLayout.CandidateMask}]",
            $"mov rdi, {table}",
            "xor r8d, r8d",
            ".approach_mailbox_clear:",
            "mov eax, r9d",
            "mov ecx, r8d",
            "shr eax, cl",
            "and eax, 1",
            "jz .approach_mailbox_clear_next",
            "mov word [rdi+1CEh], 0",
            ".approach_mailbox_clear_next:",
            "add rdi, 200h",
            "add r8d, 1",
            "cmp r8d, 21",
            "jl .approach_mailbox_clear",
            $"mov dword [rsi+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.QueueRejected}",
            "jmp .approach_original",

            ".approach_pause:",
            "popfq",
            "pop r11",
            "pop r10",
            "pop r9",
            "pop r8",
            "pop rdi",
            "pop rsi",
            "pop rdx",
            "pop rcx",
            "pop rax",
            // FASM cannot encode a rel32 jump when Reloaded allocates the hook stub farther than
            // 2 GiB from the game image. This push/xchg/ret trampoline preserves rax and flags.
            "push rax",
            $"mov rax, {epilogue}",
            "xchg qword [rsp], rax",
            "ret",

            ".approach_original:",
            "popfq",
            "pop r11",
            "pop r10",
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

    /// <summary>
    /// ExecuteFirst at 0x211E09. A final route boundary returns through the movement-updater
    /// epilogue before the managed poller can answer; retain state 0x11 while that exact actor is
    /// pending, while its command is being handed to the game thread, or while the accepted queue
    /// owns control. Release/rejection/resume states deliberately fall through so normal movement
    /// completion can advance to state 0x12.
    /// </summary>
    public static string[] BuildMovementCompletionGuard(nint buffer, nint skipTarget)
        =>
        [
            "use64",
            "push rax",
            "pushfq",
            $"mov rax, {Hex(buffer)}",
            $"cmp dword [rax+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.PendingDecision}",
            "je .approach_completion_owned",
            $"cmp dword [rax+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.InvokeQueue}",
            "je .approach_completion_owned",
            $"cmp dword [rax+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.QueueAccepted}",
            "jne .approach_completion_original",
            ".approach_completion_owned:",
            $"cmp qword [rax+{DclApproachNativeLayout.ActorPtr}], rbx",
            "jne .approach_completion_original",
            "popfq",
            "pop rax",
            "push rax",
            $"mov rax, {Hex(skipTarget)}",
            "xchg qword [rsp], rax",
            "ret",
            ".approach_completion_original:",
            "popfq",
            "pop rax",
        ];

    /// <summary>
    /// Inline snippet for the existing pass-2 commit hook. It stamps only an exact armed delivery,
    /// source index, reactor table index, and reactor unit pointer.
    /// </summary>
    public static string[] BuildPass2CommitStamp(
        nint buffer,
        nint sourceIndexGlobal,
        nint unitTable,
        string actorRegister)
    {
        if (actorRegister is not "rbx" and not "rdi")
            throw new ArgumentOutOfRangeException(nameof(actorRegister));
        return
        [
            $"mov rax, {Hex(buffer)}",
            $"mov ecx, dword [rax+{DclApproachNativeLayout.State}]",
            $"cmp ecx, {(int)DclApproachNativeState.InvokeQueue}",
            "je .approach_commit_state_ok",
            $"cmp ecx, {(int)DclApproachNativeState.QueueAccepted}",
            "jne .approach_commit_done",
            ".approach_commit_state_ok:",
            $"movzx ecx, word [{actorRegister}+142h]",
            $"cmp ecx, dword [rax+{DclApproachNativeLayout.DeliveryReactionId}]",
            "jne .approach_commit_done",
            $"mov rdx, {Hex(sourceIndexGlobal)}",
            "mov edx, dword [rdx]",
            $"cmp edx, dword [rax+{DclApproachNativeLayout.MoverTableIndex}]",
            "jne .approach_commit_done",
            $"mov rdx, qword [{actorRegister}+148h]",
            "test rdx, rdx",
            "jz .approach_commit_done",
            "movzx ecx, byte [rdx+1]",
            "cmp ecx, 20",
            "jg .approach_commit_done",
            $"cmp rdx, qword [rax+{DclApproachNativeLayout.UnitPtr}]",
            "je .approach_commit_done", // mover can never be its own Approach reactor.
            $"mov rdx, {Hex(unitTable)}",
            "shl rcx, 9",
            "add rdx, rcx",
            $"cmp qword [{actorRegister}+148h], rdx",
            "jne .approach_commit_done",
            "movzx ecx, byte [rdx+1]",
            "mov edx, 1",
            "shl edx, cl",
            $"test dword [rax+{DclApproachNativeLayout.CandidateMask}], edx",
            "jz .approach_commit_done",
            $"or dword [rax+{DclApproachNativeLayout.CommitMask}], edx",
            ".approach_commit_done:",
        ];
    }

    /// <summary>ExecuteAfter the ten-byte native state-0x28 write at 0xD7D0A81.</summary>
    public static string[] BuildResumeShim(
        nint buffer,
        nint battleStateGlobal,
        int maximumWrites)
        =>
        [
            "use64",
            "push rax",
            "push rcx",
            "push rdx",
            "pushfq",
            $"mov rax, {Hex(buffer)}",
            $"cmp dword [rax+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.QueueAccepted}",
            "jne .approach_resume_done",
            $"cmp dword [rax+{DclApproachNativeLayout.CommitMask}], 0",
            "je .approach_resume_abort",
            $"cmp dword [rax+{DclApproachNativeLayout.ControlWrites}], {Math.Clamp(maximumWrites, 1, 32)}",
            "jae .approach_resume_abort",
            $"mov rcx, qword [rax+{DclApproachNativeLayout.ActorPtr}]",
            "test rcx, rcx",
            "jz .approach_resume_abort",
            "movzx edx, byte [rcx+0A8h]",
            $"cmp edx, dword [rax+{DclApproachNativeLayout.RouteLength}]",
            "jne .approach_resume_abort",
            "mov edx, dword [rcx+0A4h]",
            $"cmp edx, dword [rax+{DclApproachNativeLayout.RouteCursor}]",
            "jne .approach_resume_abort",
            "movzx edx, byte [rcx+088h]",
            $"cmp edx, dword [rax+{DclApproachNativeLayout.TileX}]",
            "jne .approach_resume_abort",
            "movzx edx, byte [rcx+089h]",
            $"cmp edx, dword [rax+{DclApproachNativeLayout.TileY}]",
            "jne .approach_resume_abort",
            "movzx edx, byte [rcx+08Ah]",
            $"cmp edx, dword [rax+{DclApproachNativeLayout.TileLayer}]",
            "jne .approach_resume_abort",
            $"mov rcx, {Hex(battleStateGlobal)}",
            "cmp dword [rcx], 28h",
            "jne .approach_resume_abort",
            "mov dword [rcx], 11h",
            $"add dword [rax+{DclApproachNativeLayout.ControlWrites}], 1",
            $"mov dword [rax+{DclApproachNativeLayout.Outcome}], 4",
            $"mov dword [rax+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.ResumeWritten}",
            "jmp .approach_resume_done",
            ".approach_resume_abort:",
            $"mov dword [rax+{DclApproachNativeLayout.Outcome}], -3",
            $"mov dword [rax+{DclApproachNativeLayout.State}], {(int)DclApproachNativeState.Aborted}",
            ".approach_resume_done:",
            "popfq",
            "pop rdx",
            "pop rcx",
            "pop rax",
        ];
}
