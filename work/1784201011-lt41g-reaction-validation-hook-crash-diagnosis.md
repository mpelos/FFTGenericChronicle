# LT41G Reaction-validation hook crash diagnosis

## Scope

This checkpoint diagnoses the repeated process termination after Wenyld's distant synthetic
Counter delivery rejection in LT41F/LT41G. It does not classify Dual Wield yet and does not add any
job mechanic.

## Reproduced boundary

The correct owner fixture is
`work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`. LT41G confirms owner `443`,
delivery `442`, a committed Wenyld basic Attack, one accepted synthetic reservation, one staged
delivery, and native typed-family rejection:

```text
[DCL-REACTION-DELIVERY-VALIDATION] event=1 stage=typed-family reactorIdx=16 sourceIdx=6
reactionId=442 result=-2 accepted=0 ... syntheticState=2->6
```

The process terminates immediately afterward. The raw capture is
`work/1784174569-lt41g-dcl-dual-wield-provenance-instrumentation-crash-live.log`, SHA-256
`B59343D4D7F2D83DFFEF4420F6D2CACC95258E4EFF4ED08154664428DE363197`.

## Native crash evidence

Windows Application Error and Windows Error Reporting record both repeated failures with exception
`0xC0000005` at executable offset `0x283160`. The LT41G report is archived at:

```text
C:\ProgramData\Microsoft\Windows\WER\ReportArchive\
AppCrash_FFT_enhanced.exe_ff44325fdce59d241e4ba4094a615b5e09e1eb0_f1d49a97_b6af9c26-e116-4d64-a153-7c54c30c4477\Report.wer
```

The exact native sequence is:

```text
0x283157  call final_validator
0x28315C  test eax,eax
0x28315E  je   0x2831BD
0x283160  mov  edx,0x14       ; external restore entry
```

The typed-family rejection at `0x283019` jumps directly to `0x283160`; it does not pass through the
final validator.

## Cause

The final-result probe incorrectly began an `AsmHook` at `0x28315C`. Reloaded.Hooks defines the
original span as the minimum complete instruction sequence occupying at least seven bytes. Starting
at `0x28315C` therefore steals `test` (2), `je` (2), and `mov edx,0x14` (5), placing the external
branch target `0x283160` inside the detour span. A distant typed rejection can enter the middle of
that detour and fault at the exact WER offset. LT40's successful traversal with the unsafe hook was
accidental and is not a safety proof.

The earlier hypothesis that provenance logging crashed in an unbounded caster-team read is refuted
as the cause by the native fault address. That read was still unsafe and now rejects every caster
record outside exact unit slots `0..63`.

Primary hook-library behavior reference:
[Reloaded.Hooks assembly hooks](https://reloaded-project.github.io/Reloaded.Hooks/AssemblyHooks/).

## Correction

The final probe now:

- starts at RVA `0x283157`;
- validates bytes `E8 88 F7 FF FF 85 C0 74 5D`;
- uses `AsmHookBehaviour.ExecuteAfter`;
- sets `hookLength = 7`, relocating exactly `call + test`;
- resumes at `0x28315E`, leaving `0x283160` untouched.

Smoke tests require:

```text
DCL_REACTION_FINAL_VALIDATION_HOOK_RVA == 0x283157
DCL_REACTION_FINAL_VALIDATION_HOOK_LENGTH == 7
hook start + hook length <= 0x283160
```

The static executable/runtime analysis is
`work/1784200966-dcl-reaction-delivery-validation-analysis.md` and passes. The complete
`codemod/run-offline-checks.ps1` gate passes after the correction.

## Next live gate

Repeat the LT41G proven Auto-battle route with the exact owner-`443` fixture. Startup must show the
final hook at `rva=0x283157 behavior=ExecuteAfter hookLength=7`. Wenyld's `442 result=-2` must leave
the game running and continue the battle. Only after that negative-path safety gate should the run
collect the later adjacent accepted Counter and its two Dual Wield effect rows for second-strike
provenance.
