# Roll-verdict override — the clean neutralization lever (2026-06-27)

> ## ⛔ REFUTED LIVE (2026-06-27). `0x30F4A7` is NOT the accuracy roll.
> Built + tested (`work/battle-runtime-settings.roll-verdict-test.json`,
> log `battleprobe_log.txt` 221 lines). Three facts kill it:
> 1. **`native=0` in ALL 112 captured events** — never any other value, including for Agrias's
>    confirmed 184 HIT on Ramza. A real hit/miss verdict would vary. It doesn't ⇒ eax here is not the
>    verdict (and `eax=0` is clearly NOT "miss", since hits show 0).
> 2. **Timeline:** the 184 hit applied at the pre-clamp/selector (`now≈795177`); the FIRST roll-verdict
>    event is `now≈795259` — *after*. The attack that damaged Ramza never passed through `0x30F4A7`
>    before applying.
> 3. **The bursts track per-unit CT/turn ticks** (each `id=0x82` slot: `b8=1 ct=0` →
>    `+0x1B9:00→01` → `ct=20`, ~8–11 roll-verdict fires per unit per tick). So `0x30FA34` at this site
>    is a per-unit turn/CT/AI evaluation that returns 0 — NOT the per-attack avoidance roll. The
>    static spine (3 agents) mis-identified it. Forcing `eax=0` was a no-op on an already-0 value.
> ⇒ This is the SECOND refuted in-code neutralization point (after evade-input). Strong signal that
> the accuracy/evade verdict is computed entirely inside the VM with no real-code flip site. Pivot:
> the pre-clamp is the universal HP-apply point — settle whether it fires on a native MISS
> (`work/battle-runtime-settings.miss-capture.json`). If yes ⇒ author damage on misses (full control,
> zero data). If no ⇒ targeted evade-off in data + output-control. The `RollVerdict*` harness stays
> (disabled) as a negative-result artifact.

Status: ~~statically validated; built into the harness; 1 decisive live test away~~ **REFUTED (above).**
This was the attempt to neutralize native avoidance WITHOUT gutting data — it came out of the
evade-input dead end, and it too is a dead end. Reasoning preserved below for the record.

## The chain of reasoning

1. Output-control (pre-clamp `0x30A66F` + selector `0x205210`) is PROVEN: we can author hit→miss +
   zero damage. But it can only *downgrade* a native hit. A native **miss** skips the apply path
   entirely (`r12d` never gets `0x300`), so the pre-clamp never fires and we cannot force a dodge to
   connect. To handle the miss→hit direction we needed to **neutralize** native avoidance first.
2. Neutralize-in-DATA works (zero evade bytes, strip reactions) but the user dislikes data-gutting.
3. Neutralize-via-evade-input-control is **DEAD** (live-refuted 2026-06-27): the defender's evade is
   read inside the VM roll; `rbx` at `0x30F49C` is the ATTACKER, not the defender. See
   `input-control-hook-map.md`.
4. **The dead end pointed at the real lever:** the avoidance roll's *verdict* lands in `eax` in REAL
   code right after the VM returns. Override `eax` ⇒ override the verdict ⇒ neutralize ALL native
   avoidance (evade + reactions, both virtualized inside the one roll) with **zero data changes**.

## The hook (disassembly-confirmed, `work/disasm_roll_verdict.py`)

```
0030F49C: 48 8B D3              mov rdx, rbx          ; rbx = acting unit (attacker), live-confirmed
0030F49F: 88 4B 41              mov [rbx+0x41], cl
0030F4A2: E8 8D 05 00 00        call 0x30FA34         ; THE avoidance roll. 0x30FA34 = E9 jmp 0x150CFB562 (VM thunk)
0030F4A7: 44 8B D0              mov r10d, eax         ; <== HOOK HERE. eax = verdict, saved to r10d
0030F4AA: BD 01 00 00 00        mov ebp, 1
0030F4AF: 85 C0                 test eax, eax         ; THE verdict branch
0030F4B1: 74 13                 je 0x30F4C6           ;   eax==0 -> miss/alternate path
0030F4B3: 44 8B E7              mov r12d, edi
0030F4B6: 41 81 CC 00 03 00 00  or r12d, 0x300        ;   eax!=0 -> stage apply + damage (HIT)
0030F4BD: 44 84 D5              test bpl, r10b        ; (eax&1) routes to the apply call 0x30F900
0030F4C0: 0F 85 3A 04 00 00     jne 0x30F900
```

Verdict enum (from the branch logic): **0 = miss**, **1 = clean hit** (stage 0x300 + apply), 2 =
engine "special" (nonzero so stages, but `eax&1`==0 falls through; `cmp r10d,2` handled separately).

## Why hook 0x30F4A7 (not 0x30F4AF)

ExecuteFirst at `0x30F4A7` runs our asm, then the stolen `mov r10d,eax` propagates our `eax` to the
SECOND consumer (`r10b` at 0x30F4BD) for free. We set ONE register; both `test eax,eax` and
`test bpl,r10b` then follow our verdict. Hooking at the `test` would force us to set both eax and r10d.

## Register facts

- `rbx` = the acting unit (ATTACKER). charId at `[rbx+0]`. The defender is NOT in a register here
  (it's resolved inside the VM) — but we DON'T need it: the verdict override is defender-agnostic.
- The damage forecast (`+0x1C4`) is irrelevant to a forced hit: the **pre-clamp authors the debit**
  regardless (proven: 184→0; equally writes 0→N). So forcing eax=1 on a would-miss + writing damage
  at the pre-clamp = a complete forced hit, no dependence on the native forecast.

## The complete CODE-ONLY architecture (no data changes)

| Hook | RVA | Role | Status |
|---|---|---|---|
| **Roll-verdict** | `0x30F4A7` | force `eax=1` ⇒ every attack takes the hit/apply path (neutralize native evade + reactions) | **statically validated, this test** |
| Pre-clamp | `0x30A66F` (rdi=defender) | write debit = formula damage | **PROVEN** |
| Selector | `0x205210` (defender via record) | write `+0x1C0` = evade-type (hit/miss/block/parry animation) | **PROVEN** |

All per-(attacker,defender) formula logic lives at the pre-clamp/selector, where the defender IS
available (rdi / the record). The roll-verdict hook just flips the master switch to "always hit."

## The harness (Mod.cs, compiles clean)

`RollVerdictProbe*` / `RollVerdictControl*` settings. Probe logs `native` vs `final` verdict per roll
(`[ROLL-VERDICT event=.. actor=.. id=.. native=1(hit) final=0(miss) [VERDICT FORCED]]`). Control:
`ForceVerdict` (-1 observe / 0 miss / 1 hit / 2 special), `TargetCharId` (gate on acting unit),
`MaxWrites`, `LogOnly`. ExecuteFirst at 0x30F4A7, expected bytes `44 8B D0 BD 01 00 00 00 85 C0`.

## Decisive live test #1 (deployed: `work/battle-runtime-settings.roll-verdict-test.json`)

Force `eax=0` (miss) on a guaranteed hit. **PASS** = (1) `[ROLL-VERDICT .. native=1 final=0 [VERDICT
FORCED]]`, (2) Ramza takes 0 damage, (3) the pre-clamp (observe-only) does **NOT** fire for that
attack — proving eax gates the *entire* apply path (output-control could only zero the debit while the
path still ran; this skips it). By the symmetry of `test eax,eax; je`, this proves eax=1 forces the
hit path. Setup: relaunch, make Agrias→Ramza the FIRST attack.

## Follow-up test #2 (after #1 passes)

Force `eax=1` broadly (TargetCharId=-1, MaxWrites high), play a battle with sub-100% attacks; watch for
`native=0(miss) final=1(hit)` events WITH damage applied = end-to-end neutralization proof (the
miss→hit direction output-control can't do). Then wire the real formula at pre-clamp/selector.

See `1782517714-miss-block-parry-control-definitive.md`, `input-control-hook-map.md`,
`docs/modding/05-reverse-engineering.md`.
