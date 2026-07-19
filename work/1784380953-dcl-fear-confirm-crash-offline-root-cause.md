# DCL Fear confirm crash — offline root cause and correction

Created: 2026-07-18

## Symptom

The clean minimal Fear profile closed `FFT_enhanced.exe` immediately after the player confirmed the
test action. The pre-confirm and post-confirm Reloaded logs were byte-identical and contained no
`[DCL-FEAR-TARGET]`, `[DCL-FEAR-CONFIRM]`, or status line, consistent with a failure on the game
thread before the queued managed log could be drained.

Evidence:

- `work/1784378645-dcl-fear-clean-retest-pre-confirm.log`
- `work/1784378698-dcl-fear-clean-retest-confirm-crash.log`

## Offline isolation

The voluntary-confirm boundary at RVA `0x20C55F` is a native `call 0x2072F8`. Its containing
function enters with the standard Windows x64 stack parity, then executes `push r14` and
`sub rsp,0x20`; every native call in the function therefore starts with RSP aligned to 16 bytes.

The Fear shim replaced that call, saved eight qwords, then used `sub rsp,0x88` before calling the
Reloaded reverse wrapper. Eight saves preserve the incoming alignment, but subtracting `0x88`
changes it by eight bytes. The wrapper call therefore violated the Windows x64 ABI. The same frame
stored its managed result at `rsp+0x80`, so a corrected `0x80` frame would not be large enough.

## Correction

The confirm shim now uses `sub/add rsp,0x90`. This preserves 16-byte alignment and contains:

- 32 bytes of shadow space;
- six 16-byte XMM save slots at `+0x20..+0x7F`;
- a private eight-byte callback result at `+0x80`.

The builder moved to `DclFearNativeAsm.BuildPlayerConfirmShim`, making the assembly contract
directly testable. The C# smoke suite asserts the aligned frame, result slot, native allow call,
reject branch, and two exact restore paths.

## Verification

```text
dotnet build ... Release
Build succeeded. 0 warnings, 0 errors.

dotnet run ... codemod.smoketests ... Release
formula runtime smoke tests passed
```

## Confidence

- **Proven (offline ABI):** `0x88` misaligns the wrapper call at this native call site.
- **Proven (offline implementation):** `0x90` preserves alignment and keeps the result slot inside
  the allocated frame.
- **Live gate:** deploy the corrected DLL under the same minimal profile and repeat the exact confirm
  action. A surviving process plus `[DCL-FEAR-CONFIRM]` distinguishes this correction from the
  still-independent target-list and Chicken-dispatch hooks.
