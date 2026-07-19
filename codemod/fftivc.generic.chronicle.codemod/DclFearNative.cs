using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;

namespace fftivc.generic.chronicle.codemod;

internal enum DclFearNativeState
{
    Idle = 0,
    FleeTileSelected = 1,
    PlanningLegalAction = 2,
    PlanComposed = 3,
    NativeFallback = 4,
}

internal static class DclFearNativeLayout
{
    public const int State = 0x00;
    public const int Sequence = 0x04;
    public const int FailureStage = 0x08;
    public const int UnitPtr = 0x10;
    public const int PlannedTile = 0x18;
    public const int UnitTileBefore = 0x20;
    public const int SelectedTile = 0x24;
    public const int UnitTileRestored = 0x28;
    public const int PlannerResult = 0x2C;
    public const int EffectiveChickenBefore = 0x30;
    public const int EffectiveDontMoveBefore = 0x34;
    public const int StatusBytesRestored = 0x38;
    public const int FinalUnitTileRestored = 0x3C;
    public const int Size = 0x40;
}

internal static class DclFearConfirmActorLayout
{
    public const int ActionId = 0x142;
    public const int UnitPtr = 0x148;
    public const int PresentationId = 0x18C;
    public const int TargetCount = 0x1A9;
    public const int TargetList = 0x1AA;
    public const int PrimaryTarget = 0x1BC;
    public const int MaxTargets = 21;
}

internal static class DclFearNativeAsm
{
    public const int TargetListBuilderHookLength = 5;
    public const int TargetListCalcRecordOffset = 0x1A0;
    public const int PlayerConfirmHookLength = 5;
    public const int ChickenDispatchHookLength = 6;

    private static string Hex(nint address) => $"0{address:X}h";

    public static AsmHookOptions BuildTargetListHookOptions() => new()
    {
        Behaviour = AsmHookBehaviour.DoNotExecuteOriginal,
        PreferRelativeJump = true,
        MaxOpcodeSize = TargetListBuilderHookLength,
        hookLength = TargetListBuilderHookLength,
    };

    /// <summary>
    /// Replaces only the five-byte affected-target builder call at 0x281EC3. Hooking its return at
    /// 0x281EC8 is unsafe because the following instruction at 0x281ECC is an external branch
    /// target. The native builder runs first; its return state is then preserved around a managed
    /// callback. At this site r14 is the caster unit base, so unit+0x1A0 is the calculation/order
    /// record expected by the managed callback. The caller's stack is 16-byte aligned and eight
    /// saves preserve that alignment, therefore the wrapper frame is exactly 0x80 bytes.
    /// </summary>
    public static string[] BuildTargetListShim(nint callback, nint originalBuilder)
    {
        string cb = Hex(callback);
        string builder = Hex(originalBuilder);
        return
        [
            "use64",
            $"mov r11, {builder}",
            "call r11",
            "push rax", "push rcx", "push rdx", "push r8", "push r9", "push r10", "push r11", "pushfq",
            $"lea rcx, [r14+{TargetListCalcRecordOffset:X}h]",
            "lea rdx, [rbp-28h]",
            "sub rsp, 80h",
            "movdqu [rsp+20h], xmm0", "movdqu [rsp+30h], xmm1", "movdqu [rsp+40h], xmm2",
            "movdqu [rsp+50h], xmm3", "movdqu [rsp+60h], xmm4", "movdqu [rsp+70h], xmm5",
            $"mov r11, {cb}",
            "call r11",
            "movdqu xmm0, [rsp+20h]", "movdqu xmm1, [rsp+30h]", "movdqu xmm2, [rsp+40h]",
            "movdqu xmm3, [rsp+50h]", "movdqu xmm4, [rsp+60h]", "movdqu xmm5, [rsp+70h]",
            "add rsp, 80h",
            "popfq", "pop r11", "pop r10", "pop r9", "pop r8", "pop rdx", "pop rcx", "pop rax",
        ];
    }

    public static AsmHookOptions BuildPlayerConfirmHookOptions() => new()
    {
        Behaviour = AsmHookBehaviour.DoNotExecuteOriginal,
        PreferRelativeJump = true,
        MaxOpcodeSize = PlayerConfirmHookLength,
        hookLength = PlayerConfirmHookLength,
    };

    public static AsmHookOptions BuildChickenDispatchHookOptions() => new()
    {
        Behaviour = AsmHookBehaviour.DoNotExecuteOriginal,
        PreferRelativeJump = true,
        MaxOpcodeSize = ChickenDispatchHookLength,
        hookLength = ChickenDispatchHookLength,
    };

    /// <summary>
    /// Replaces the voluntary-confirm call at 0x20C55F. The native caller has a 16-byte-aligned
    /// stack immediately before that call. Eight saves preserve that alignment, so the managed
    /// wrapper frame must remain a multiple of 16. At this site rbx still owns the actor returned by
    /// 0x2607C0 at 0x20C341. The actor's own target list is not materialized yet, so the shim invokes
    /// the native affected-target builder into a private 21-byte buffer before passing both pointers
    /// to the managed observe-only callback. The 0xB0-byte frame provides shadow space, six volatile
    /// XMM saves, a private eight-byte callback-result slot at +0x80, and the target buffer at +0x90.
    /// </summary>
    public static string[] BuildPlayerConfirmShim(nint callback, nint original, nint targetBuilder)
    {
        string cb = Hex(callback);
        string originalAddress = Hex(original);
        string builder = Hex(targetBuilder);
        return
        [
            "use64",
            "push rax", "push rcx", "push rdx", "push r8", "push r9", "push r10", "push r11", "pushfq",
            "sub rsp, 0B0h",
            "movdqu [rsp+20h], xmm0", "movdqu [rsp+30h], xmm1", "movdqu [rsp+40h], xmm2",
            "movdqu [rsp+50h], xmm3", "movdqu [rsp+60h], xmm4", "movdqu [rsp+70h], xmm5",
            "lea rcx, [rsp+90h]",
            "mov rdx, rbx",
            $"mov r11, {builder}",
            "call r11",
            "mov rcx, rbx",
            "lea rdx, [rsp+90h]",
            $"mov r11, {cb}",
            "call r11",
            "mov qword [rsp+80h], rax",
            "movdqu xmm0, [rsp+20h]", "movdqu xmm1, [rsp+30h]", "movdqu xmm2, [rsp+40h]",
            "movdqu xmm3, [rsp+50h]", "movdqu xmm4, [rsp+60h]", "movdqu xmm5, [rsp+70h]",
            "mov rax, qword [rsp+80h]",
            "add rsp, 0B0h",
            "test rax, rax",
            "jz .fear_reject",
            "popfq", "pop r11", "pop r10", "pop r9", "pop r8", "pop rdx", "pop rcx", "pop rax",
            $"mov r11, {originalAddress}",
            "call r11",
            "jmp .fear_confirm_done",
            ".fear_reject:",
            "popfq", "pop r11", "pop r10", "pop r9", "pop r8", "pop rdx", "pop rcx", "pop rax",
            ".fear_confirm_done:",
        ];
    }

    /// <summary>
    /// Replaces exactly the six-byte Chicken test-and-branch pair at 0x38BC37. Callback result zero
    /// routes explicitly to the native Chicken or non-Chicken successor. Result two mirrors the
    /// native Chicken selector-to-planner prefix and captures its winning flee tile. It then lends
    /// that tile to the ordinary action planner while temporarily clearing effective Chicken and
    /// setting effective Immobilize. The movement enumerator therefore exposes only the flee tile,
    /// while managed Fear authorization rejects every opposing-target candidate. Coordinates and
    /// both effective-status bytes are restored byte-exactly before returning the composed native
    /// move-plus-action plan through the forced-control resolver's handled epilogue. Any failure
    /// restores every loan and falls back to the untouched native Chicken branch.
    /// </summary>
    public static string[] BuildChickenDispatchShim(
        nint callback,
        nint buffer,
        nint selector,
        nint activeUnitPtrGlobal,
        nint planningBuffer,
        nint memorySet,
        nint planner,
        nint winningTile,
        nint ordinaryPlanner,
        nint composedPlanTile,
        nint nativeChickenTarget,
        nint nativeNonChickenTarget,
        nint handledEpilogue)
    {
        string cb = Hex(callback);
        string buf = Hex(buffer);
        string selectorAddress = Hex(selector);
        string activeUnit = Hex(activeUnitPtrGlobal);
        string planning = Hex(planningBuffer);
        string memset = Hex(memorySet);
        string plannerAddress = Hex(planner);
        string winner = Hex(winningTile);
        string actionPlanner = Hex(ordinaryPlanner);
        string planTile = Hex(composedPlanTile);
        string nativeChicken = Hex(nativeChickenTarget);
        string nativeNonChicken = Hex(nativeNonChickenTarget);
        string handled = Hex(handledEpilogue);

        var asm = new List<string>
        {
            "use64",
            // Managed ownership query. This is identical to the existing observe-only save set.
            "push rax", "push rcx", "push rdx", "push r8", "push r9", "push r10", "push r11", "pushfq",
            "mov rcx, rdi",
            "sub rsp, 90h",
            "movdqu [rsp+20h], xmm0", "movdqu [rsp+30h], xmm1", "movdqu [rsp+40h], xmm2",
            "movdqu [rsp+50h], xmm3", "movdqu [rsp+60h], xmm4", "movdqu [rsp+70h], xmm5",
            $"mov r11, {cb}",
            "call r11",
            "mov qword [rsp+80h], rax",
            "movdqu xmm0, [rsp+20h]", "movdqu xmm1, [rsp+30h]", "movdqu xmm2, [rsp+40h]",
            "movdqu xmm3, [rsp+50h]", "movdqu xmm4, [rsp+60h]", "movdqu xmm5, [rsp+70h]",
            "mov rax, qword [rsp+80h]",
            "add rsp, 90h",
            "cmp rax, 2",
            "je .fear_forced_cleanup",
            ".fear_native_cleanup:",
            "popfq", "pop r11", "pop r10", "pop r9", "pop r8", "pop rdx", "pop rcx", "pop rax",
            "test byte [rdi+63h], 04h",
            "jz .fear_non_chicken",
            "jmp .fear_native_chicken",
            ".fear_forced_cleanup:",
            "popfq", "pop r11", "pop r10", "pop r9", "pop r8", "pop rdx", "pop rcx", "pop rax",

            // Publish identity and the byte-exact pre-selector coordinate tuple.
            $"mov r10, {buf}",
            $"add dword [r10+{DclFearNativeLayout.Sequence}], 1",
            $"mov dword [r10+{DclFearNativeLayout.State}], {(int)DclFearNativeState.Idle}",
            $"mov dword [r10+{DclFearNativeLayout.FailureStage}], 1",
            $"mov qword [r10+{DclFearNativeLayout.UnitPtr}], rdi",
            $"mov qword [r10+{DclFearNativeLayout.PlannedTile}], 0",
            $"mov dword [r10+{DclFearNativeLayout.SelectedTile}], 0",
            $"mov dword [r10+{DclFearNativeLayout.UnitTileRestored}], 0",
            $"mov dword [r10+{DclFearNativeLayout.PlannerResult}], -1",
            $"mov dword [r10+{DclFearNativeLayout.StatusBytesRestored}], 0",
            $"mov dword [r10+{DclFearNativeLayout.FinalUnitTileRestored}], 0",
            "movzx eax, byte [rdi+63h]",
            $"mov dword [r10+{DclFearNativeLayout.EffectiveChickenBefore}], eax",
            "movzx eax, byte [rdi+65h]",
            $"mov dword [r10+{DclFearNativeLayout.EffectiveDontMoveBefore}], eax",
            "movzx eax, byte [rdi+4Fh]",
            "movzx ecx, byte [rdi+50h]",
            "shl ecx, 8",
            "or eax, ecx",
            "movzx ecx, byte [rdi+51h]",
            "shl ecx, 16",
            "or eax, ecx",
            $"mov dword [r10+{DclFearNativeLayout.UnitTileBefore}], eax",
            $"mov r11, {activeUnit}",
            "cmp qword [r11], rdi",
            "jne .fear_native_fallback",

            // The selector scores every candidate, but its scratch tuple is only the final
            // enumerated candidate. Mirror the native Chicken prefix through planner 0x321390;
            // the planner publishes the actual winning X/Y/layer record.
            $"mov r11, {selectorAddress}",
            "call r11",
            "test eax, eax",
            "jnz .fear_restore_and_fallback",
            $"mov rcx, {planning}",
            "xor edx, edx",
            "mov r8d, 240h",
            $"mov r11, {memset}",
            "call r11",
            "mov ecx, 0FFh",
            "mov edx, 1",
            $"mov r11, {plannerAddress}",
            "call r11",
            "test eax, eax",
            "jz .fear_restore_and_fallback",
            $"mov r10, {buf}",
            $"mov r11, {winner}",
            "mov eax, dword [r11]",
            "and eax, 00FFFFFFh",
            $"mov dword [r10+{DclFearNativeLayout.SelectedTile}], eax",

            // Restore the selector's coordinate loan, then lend the selected flee tile to the
            // ordinary planner. Preserve the low seven bits of +0x51 and replace only its layer bit.
            ".fear_restore_after_selector:",
            $"mov r10, {buf}",
            $"mov eax, dword [r10+{DclFearNativeLayout.UnitTileBefore}]",
            "mov byte [rdi+4Fh], al",
            "mov ecx, eax",
            "shr ecx, 8",
            "mov byte [rdi+50h], cl",
            "shr eax, 16",
            "mov byte [rdi+51h], al",
            "movzx eax, byte [rdi+4Fh]",
            "movzx ecx, byte [rdi+50h]",
            "shl ecx, 8",
            "or eax, ecx",
            "movzx ecx, byte [rdi+51h]",
            "shl ecx, 16",
            "or eax, ecx",
            $"mov dword [r10+{DclFearNativeLayout.UnitTileRestored}], eax",
            $"cmp eax, dword [r10+{DclFearNativeLayout.UnitTileBefore}]",
            "jne .fear_native_fallback",
            $"mov dword [r10+{DclFearNativeLayout.State}], {(int)DclFearNativeState.FleeTileSelected}",
            $"mov dword [r10+{DclFearNativeLayout.FailureStage}], 2",
            $"mov eax, dword [r10+{DclFearNativeLayout.SelectedTile}]",
            "mov byte [rdi+4Fh], al",
            "mov ecx, eax",
            "shr ecx, 8",
            "mov byte [rdi+50h], cl",
            "shr eax, 16",
            "and eax, 1",
            "shl eax, 7",
            "movzx ecx, byte [rdi+51h]",
            "and ecx, 7Fh",
            "or ecx, eax",
            "mov byte [rdi+51h], cl",

            // Chicken is only the durable visual/status carrier. During this synchronous planning
            // loan, clear it from the effective mirror and add effective Immobilize so the native
            // selector can plan an allowed action but cannot choose a second movement tile.
            "and byte [rdi+63h], 0FBh",
            "or byte [rdi+65h], 08h",
            $"mov r10, {buf}",
            $"mov dword [r10+{DclFearNativeLayout.State}], {(int)DclFearNativeState.PlanningLegalAction}",
            $"mov dword [r10+{DclFearNativeLayout.FailureStage}], 3",
            $"mov r11, {actionPlanner}",
            "call r11",
            $"mov r10, {buf}",
            $"mov dword [r10+{DclFearNativeLayout.PlannerResult}], eax",
            $"mov r11, {planTile}",
            "mov ecx, dword [r11]",
            "and ecx, 00FFFFFFh",
            $"mov qword [r10+{DclFearNativeLayout.PlannedTile}], rcx",
            "cmp eax, -1",
            "je .fear_restore_all_and_fallback",

            // The temporary Immobilize contract requires the ordinary plan to retain exactly the
            // flee destination. Treat any mismatch as a fail-closed native Chicken fallback.
            $"mov edx, dword [r10+{DclFearNativeLayout.SelectedTile}]",
            "and edx, 00FFFFFFh",
            "cmp ecx, edx",
            "jne .fear_restore_all_and_fallback",
            $"mov dword [r10+{DclFearNativeLayout.FailureStage}], 4",
            "call .fear_restore_all",
            $"mov r10, {buf}",
            $"mov dword [r10+{DclFearNativeLayout.FailureStage}], 0",
            $"mov dword [r10+{DclFearNativeLayout.State}], {(int)DclFearNativeState.PlanComposed}",
            "jmp .fear_handled",

            // Every failure path restores both coordinate and effective-status loans before native
            // Chicken retries. The local call is balanced and uses no shadow space or external ABI.
            ".fear_restore_and_fallback:",
            "jmp .fear_restore_all_and_fallback",
            ".fear_restore_all_and_fallback:",
            "call .fear_restore_all",
            "jmp .fear_native_fallback",
            ".fear_restore_all:",
            $"mov r10, {buf}",
            $"mov eax, dword [r10+{DclFearNativeLayout.UnitTileBefore}]",
            "mov byte [rdi+4Fh], al",
            "mov ecx, eax",
            "shr ecx, 8",
            "mov byte [rdi+50h], cl",
            "shr eax, 16",
            "mov byte [rdi+51h], al",
            "movzx edx, byte [rdi+4Fh]",
            "movzx ecx, byte [rdi+50h]",
            "shl ecx, 8",
            "or edx, ecx",
            "movzx ecx, byte [rdi+51h]",
            "shl ecx, 16",
            "or edx, ecx",
            $"mov dword [r10+{DclFearNativeLayout.FinalUnitTileRestored}], edx",
            $"mov eax, dword [r10+{DclFearNativeLayout.EffectiveChickenBefore}]",
            "mov byte [rdi+63h], al",
            $"mov eax, dword [r10+{DclFearNativeLayout.EffectiveDontMoveBefore}]",
            "mov byte [rdi+65h], al",
            "movzx eax, byte [rdi+63h]",
            $"cmp edx, dword [r10+{DclFearNativeLayout.UnitTileBefore}]",
            "jne .fear_restore_done",
            $"cmp eax, dword [r10+{DclFearNativeLayout.EffectiveChickenBefore}]",
            "jne .fear_restore_done",
            "movzx eax, byte [rdi+65h]",
            $"cmp eax, dword [r10+{DclFearNativeLayout.EffectiveDontMoveBefore}]",
            "jne .fear_restore_done",
            $"mov dword [r10+{DclFearNativeLayout.StatusBytesRestored}], 1",
            ".fear_restore_done:",
            "ret",
            ".fear_native_fallback:",
            $"mov r10, {buf}",
            $"mov dword [r10+{DclFearNativeLayout.State}], {(int)DclFearNativeState.NativeFallback}",
            ".fear_native_chicken:",
            "push rax",
            $"mov rax, {nativeChicken}",
            "xchg qword [rsp], rax",
            "ret",

            ".fear_non_chicken:",
            "push rax",
            $"mov rax, {nativeNonChicken}",
            "xchg qword [rsp], rax",
            "ret",

            ".fear_handled:",
            "push rax",
            $"mov rax, {handled}",
            "xchg qword [rsp], rax",
            "ret",
        };
        return asm.ToArray();
    }
}
