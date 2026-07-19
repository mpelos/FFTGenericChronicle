# DCL Fear voluntary-confirm crash: explicit hook length

## Live result

The ABI-adjusted player-confirm shim was installed and the Josephine/Arthur autosave fixture was
loaded through Reloaded-II. Josephine selected basic Attack and Arthur was selected as the opposing
target. Pressing the target-confirm key closed the visual game window again.

The surviving `FFT_enhanced` process object was only the known exited zombie: it had no foreground
window, approximately 594 KiB working set, and no CPU activity. The installed process module list
confirmed that `fftivc.generic.chronicle.codemod.dll` was injected.

## Decisive evidence

Windows Application Error event 1000 and .NET Runtime event 1026 report:

- exception `0xC0000005`;
- faulting module `FFT_enhanced.exe`;
- fault offset / address `0x20C565` / `0x14020C565`.

The native instructions at this boundary are:

```text
0020C55F: E8 94 AD FF FF               call 0x1402072F8
0020C564: 83 3D 61 EC A5 00 19         cmp dword ptr [rip + 0xA5EC61], 0x19
```

`0x20C565` is one byte into the seven-byte `cmp`, not a valid instruction boundary. The default
Reloaded assembly-hook relocation therefore crossed the five-byte call, stole the first byte of the
following instruction, and resumed at its second byte. This explains both the original crash and the
retest after the stack-frame correction: the managed callback can return successfully, but native
control flow resumes mid-instruction.

## Correction

The player-confirm hook now passes the explicit hook length `5` through
`DclFearNativeAsm.PlayerConfirmHookLength`. The smoke test fixes that constant to the exact native
call length. The `0x90` wrapper frame remains because the native call site itself is 16-byte aligned
and the callback-result slot needs eight bytes beyond the six XMM saves.

## Validation gate

Rebuild and reinstall the code mod, then repeat the same Josephine basic-Attack confirmation against
Arthur. PASS requires the process and visual window to survive the confirmation. The fresh
`battleprobe_log.txt` must then distinguish `reject` from `allow`; only after that result is stable
should the live mechanism be promoted beyond the hook-layout fact in `docs/modding`.

## First explicit-length retest

Passing `hookLength=5` through the four-argument overload was not sufficient. The next live attempt
faulted at `0x14020C568`, still inside the instruction beginning at `0x20C564`.

The installed Reloaded.Hooks 4.3.2 implementation explains the result. `AsmHookOptions` defaults
`PreferRelativeJump=false`; an x64 absolute entry jump is wider than five bytes. Supplying a shorter
`hookLength` controls the saved span and jump-back address but does not make that entry jump fit. The
absolute jump therefore overwrote bytes beyond the declared span, while the trampoline still
returned to `0x20C564`.

The corrected hook now uses a complete `AsmHookOptions` contract:

- `Behaviour=DoNotExecuteOriginal`;
- `PreferRelativeJump=true`;
- `MaxOpcodeSize=5`;
- `hookLength=5`.

Reloaded allocates the entry stub within rel32 range, so the five-byte `E9` fits the native `call`
boundary without touching `0x20C564`.
