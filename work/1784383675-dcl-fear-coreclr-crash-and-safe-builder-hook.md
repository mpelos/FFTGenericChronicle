# DCL Fear: CoreCLR crash diagnosis and safe target-builder bridge

## Live evidence

The forced-flee probe crashed after player confirmation. The fresh battle log ended with:

```text
[CALC] n=0 rec=0x141856080 casterSlot=17 casterIdx=17 type=0x01 abilityId=0 (0x0000) payload=0 activeWeapon=255 repeat=0/1 nativeWeapons=255/255 targetIdx=16 casterTeam=0 turnOwner=17
[DCL-FEAR-CONFIRM] casterIdx=0 turnOwner=17 type=0x00 ability=0 state=0x19 decision=allow
```

There was no `[DCL-FEAR-TARGET]` row. Windows Error Reporting recorded `FFT_enhanced.exe` failing in
`coreclr.dll` with exception `0xC0000005`, fault offset `0x15731e`, and managed runtime exit code
`0x80131506`. The archived report is:

```text
C:\ProgramData\Microsoft\Windows\WER\ReportArchive\AppCrash_FFT_enhanced.exe_882f3b5bccd34fec7e0201ea5e386184d85f3bc_f1d49a97_dbff92da-aad8-4e79-8335-bcea8797550d\Report.wer
```

This differs from the earlier game-code faults at `0x20C564`, `0x20C565`, and `0x20C568`. The latest
relative five-byte confirmation hook ran its managed callback and no longer overwrote the instruction
after `0x20C55F`.

## Offline diagnosis

PE exception metadata places `0x281EC8` in function `0x281CE8..0x282231`. Its prologue performs seven
pushes followed by `sub rsp,0x50`, leaving `rsp` 16-byte aligned at the affected-target builder and
return sites. The old shim performed eight additional saves and `sub rsp,0x88`, so its managed call
was misaligned by eight bytes. This is an objective ABI violation consistent with the CoreCLR access
violation.

The old shim passed `r14` as an order/calculation-record pointer. At the site, disassembly establishes
that `r14` is the caster unit base; the required record is `r14+0x1A0`. Consequently
`CalcEntryProbeAddressing.TryGetCasterSlot` rejected every invocation before logging or caching the
player forecast. That explains the default `casterIdx=0` confirmation decision despite the real
caster being slot 17.

The old hook address itself is unsafe. The instruction at `0x281ECC` is the direct target of the near
jump at `0x281DF7`. An inline hook beginning at the four-byte instruction `0x281EC8` must steal the
next instruction to fit a jump, destroying that external entry.

## Corrected contract

The target bridge now replaces only the complete five-byte call at `0x281EC3` with an exact relative
hook. Its shim:

1. calls original target builder `0x282754` with the untouched native arguments;
2. preserves the builder's return state;
3. passes `r14+0x1A0` and `[rbp-0x28]` to the managed callback;
4. uses a `0x80`-byte aligned wrapper frame;
5. returns to the untouched continuation at `0x281EC8`.

The runtime defaults and guarded live fixture use RVA `0x281EC3` with bytes
`E8 8C 08 00 00`. Smoke tests enforce exact hook length/options, native-builder-before-callback order,
the `+0x1A0` argument, and the `0x80` frame. `tools/analyze_dcl_fear_boundaries.py` additionally guards
the external branch from `0x281DF7` to `0x281ECC`.

## Offline validation

- `tools/analyze_dcl_fear_boundaries.py --check-only`: PASS
- `tools/test_dcl_fear_mechanism.py`: PASS
- Release build: PASS, zero warnings/errors
- codemod smoke tests: PASS
- `codemod/run-offline-checks.ps1`: PASS
- built DLL SHA-256: `DEA9CBA433943DBE8F73F71CFCB56FEBC66EBBCCD49C9915C04062D04DCE0ECD`

## Next live falsifier

Use the safe-builder live profile and repeat the same Josephine-to-Arthur Basic Attack. Required
minimum evidence is: no crash through confirmation and result execution, a non-default
`[DCL-FEAR-TARGET]` row for caster slot 17 in state `0x19`, and `[DCL-FEAR-CONFIRM]` reusing that exact
caster/action decision. If Fear is already owned by the attacker, an opposing target must be rejected;
otherwise the first pass only proves the bridge and cache identity before the status/forced-route leg.
