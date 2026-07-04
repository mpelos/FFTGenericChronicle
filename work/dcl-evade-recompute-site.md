# DCL — Defender evade-byte WRITE sites (equipment→struct recompute)

Offline static RE (no live tests). Target: `FFT_enhanced.exe`, image base 0x140000000,
no ASLR, real code RVA 0x1000..0x610000. Unit struct table 0x141853CE0, stride 0x200.
Defender evade bytes: +0x46/+0x47 weapon parry, +0x4A/+0x4E shield block, +0x4B class evade
(+0x48/+0x49/+0x4C/+0x4D accessory-evade candidates).

Scripts: `work/disasm_evade_writers.py`, `_writers2.py`, `_ctx.py`, `_callers.py`,
`_chain.py`, `_roots.py`, `_combatregion.py`, `_gatherer.py`.

## Method
Byte-scanned every `mov [base+disp8], r8` (88 /r mod=01), `mov byte [base+disp8], imm8`
(C6 /0 mod=01) and disp32 forms, with optional REX, for disp in {0x46,0x47,0x4A,0x4B,0x4E}
(and the whole 0x44..0x4F cluster). Dropped rsp-relative (stack locals) and scaled-SIB
(array) forms — the real struct writes use a plain base reg. Clustered adjacent writes
(a real equip→struct copy writes several evade bytes in one run), found function heads
(int3-pad boundary), enumerated E8 callers, and BFS'd the call graph toward the avoidance
cluster (producer 0x30F0C4, roll 0x30FA34, selector 0x205210, apply 0x30A66F,
compute-entry 0x309A44).

## The two genuine equipment→struct evade copiers (real code)

### WRITER A — shield / class / accessory evade  (fn 0x59F550)
Writes at **0x59F8F6..0x59F927**, destination base = **RBX** (the unit struct),
source = **RDI** (equipment/base-stat block; `rdi = rdx + r9b`, args to the fn):

```
0059F8F2: movzx eax,[rdi+0x10] ; 0059F8F6: mov [rbx+0x48],al   ; accessory
0059F8F9: movzx eax,[rdi+0x11] ; 0059F8FD: mov [rbx+0x49],al
0059F900: movzx eax,[rdi+0x12] ; 0059F904: mov [rbx+0x4A],al   ; <-- SHIELD BLOCK
0059F907: movzx eax,[rdi+0x13] ; 0059F90B: mov [rbx+0x4B],al   ; <-- CLASS EVADE
0059F90E: movzx eax,[rdi+0x14] ; 0059F912: mov [rbx+0x4C],al
0059F915: movzx eax,[rdi+0x15] ; 0059F919: mov [rbx+0x4D],al
0059F91C: movzx eax,[rdi+0x16] ; 0059F920: mov [rbx+0x4E],al   ; <-- SHIELD BLOCK
0059F923: movzx eax,[rdi+0x17] ; 0059F927: mov [rbx+0x4F],al
```
Preceded by a shield-type branch (`cmp cl,3 / je 0x59F92C`) that instead force-writes
0x80 to 0x48/0x4A/0x4C/0x4E. So this fn is a straight **copy of a stat block into the
unit struct** — clearly an equip/refresh recompute, NOT a per-attack step.
- Real code: YES (0x59F550 < 0x610000).
- Source: copied from another struct field block `[rdi+0x10..0x17]` (equipment/derived stats).
- Value written to 0x4A/0x4B/0x4E: `[rdi+0x12], [rdi+0x13], [rdi+0x16]`.
- Only caller: **0x59CACD**, inside fn **0x59C0B0** — a big state/menu dispatcher full of
  indirect `call rax` (UI/equip/refresh handler). Does NOT loop the unit table, is NOT on
  the combat roll path.

### WRITER B — weapon parry  (fns 0x285394 and 0x3965B0)
Two near-identical copies write 0x44..0x47 from a stack scratch buffer that a
weapon/item lookup call just filled:

```
00285532: call 0x287410            ; item/weapon lookup -> fills [rsp+0x20..]
00285537: mov al,[rsp+0x26] ; 0028553B: mov [rbx+0x44],al
0028553E: mov al,[rsp+0x28] ; 00285542: mov [rbx+0x45],al
00285545: mov al,[rsp+0x2A] ; 00285549: mov [rbx+0x46],al   ; <-- WEAPON PARRY
0028554C: mov al,[rsp+0x2C] ; 00285550: mov [rbx+0x47],al   ; <-- WEAPON PARRY
```
(0x3965B0 is the same shape around 0x39672D, calling lookup 0x396C8C.)
- Real code: YES.
- Source: **weapon/item table lookup** (call 0x287410 / 0x396C8C) staged on the stack, then copied.
- Callers (BFS): 0x28556C, 0x33E428, 0x210214, 0x3932E4/0x393A50/0x393D90, 0x396D9C.
  None loop the unit table; none reach the avoidance cluster within 4 levels. These are
  per-unit "rebuild derived stats from equipment" routines (equip / battle-setup / status).

## Trigger classification — NONE are per-attack

- BFS from all three copiers up 4 call levels never reaches
  0x30F0C4 / 0x30FA34 / 0x309A44 / 0x30A66F / 0x205210.
- The avoidance functions do NOT call any evade-copier (reverse check: none).
- **Combat region 0x309000..0x310000 contains ZERO genuine writers** to 0x44..0x4F
  (the 3 raw hits are a stack local `[rsp+0x4f]`, an unrelated `[r12+0x4f]`, and a scaled
  `[rdi+rcx+0x47]=imm 0xC9`). So nothing in real code restores the evade bytes on the
  path into the roll.
- The pre-roll gather fn **0x30FC30** (called twice from producer 0x30F0C4 right before the
  roll) reads `[rcx+0x61]/[rcx+0x64]/[rcx+0x65]/[rcx+0x1B4]` — NOT the evade bytes. The
  actual +0x46/+0x4A/+0x4B/+0x4E consumption happens deeper inside the Denuvo VM
  (0x30A4A4 tail-jumps to 0x150B013E5 = VM space).

Verdict on trigger: **battle-init / equip-refresh / status-change (once, or on equipment
change), never per-attack, never a per-table sweep on the roll path.**

## What the "restore race" actually is
The ~50% poll loss is NOT a per-attack real-code writer beating the poll. There is no such
writer. It is one of the equip/refresh copiers (Writer A/B) firing on some **state
transition** (turn start, status/CT tick, action-menu open, buff apply/expire) that
recomputes the unit's derived block from equipment and stamps 0x44..0x4F back to their
equipment-derived values. It is asynchronous to the poll, so a 20 ms external poll that
wrote the bytes earlier gets clobbered whenever such a refresh lands between the poll and
the VM roll.

## DEFINITIVE VERDICT
**No airtight per-attack real-code input-injection point for defender evade exists.**
The only real-code writers of +0x46/+0x47/+0x4A/+0x4B/+0x4E are the equipment→struct
recompute copiers (Writer A fn 0x59F550, Writer B fns 0x285394 / 0x3965B0). They run at
equip/refresh/state-transition time, not per-attack, and are absent from the roll path.
The VM reads the evade bytes from the live struct with no real-code re-stamp immediately
before the roll.

### Consequence for the mod — change strategy to a PERSISTENT DATA WRITE
Because the writers are init/refresh-time (data-stable between refreshes) and the VM reads
live data, the winning move is not to race a poll but to make the value **stick**:

1. **Hook the recompute copiers (best).** Detour Writer A at **0x59F550** and Writer B at
   **0x285394** and **0x3965B0** (all real code). At each, after the equipment copy, over-
   write the destination evade bytes with our formula value. The defender/unit pointer is
   in a known register at the store sites:
     - Writer A: destination unit = **RBX** (bytes written as `mov [rbx+0x4A/0x4B/0x4E],al`).
       Trampoline right after 0x59F927 and restamp `[rbx+0x46..0x4F]`.
     - Writer B: destination unit = **RBX** (`mov [rbx+0x44..0x47],al`), restamp after 0x285550
       (and RSI at 0x39672D..0x39674B for the 0x3965B0 twin).
   Because these are the *only* things that ever change the bytes, restamping inside them
   makes the value persist until the next equip change — no race with the roll.

2. **Alternatively (no code detour): persistent external re-write.** Since no real-code
   writer touches the bytes on the roll path, an external agent only needs to (a) write the
   evade bytes once, and (b) re-write on each refresh event. Subscribe to the refresh by
   watching the copiers (breakpoint/patch at 0x59F550 / 0x285394 / 0x3965B0) or simply
   re-assert the bytes on every turn/action-setup edge rather than a blind 20 ms poll.
   The "beat the poll" framing is wrong — you cannot lose to a per-attack writer because
   there is none; you only lose to refresh events, which are enumerable and hookable.

RECOMMENDED HOOK: patch the tail of **Writer A (0x59F550, after 0x59F927, unit ptr = RBX)**
and **Writer B (0x285394, after 0x285550, unit ptr = RBX; twin 0x3965B0 after 0x39674B,
unit ptr = RSI)** to stamp our formula-computed evade bytes. That is the airtight,
race-free injection: it owns every code path that can legitimately change the bytes.
```
```
