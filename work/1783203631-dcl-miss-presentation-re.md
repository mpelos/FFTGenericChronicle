# DCL — Forcing a MISS *presentation* on a committed hit (offline static RE)

**Date:** 2026-07-04 (unix 1783203631) · **Target:** `FFT_enhanced.exe` (Steam v1.0, Denuvo, image base `0x140000000`, no ASLR, real code RVA `0x1000..0x610000`).
**Method:** offline static disassembly only (pefile + capstone, image read-only). No live tests, no codemod edits, no deploys. Two independent tracks (an opus RE agent driving the `disasm_q_*`/`disasm_p_*` capstone toolchain, and gpt‑5.5 via codex as an independent cross-check) — they **converge** on every load-bearing finding below.
**Scratch scripts (in `work/`):** `disasm_p_present.py`, `disasm_p_refs.py`, `disasm_p_ctx.py`, `disasm_p_final.py` (+ the reused `disasm_q_common.py` helpers).

## Confidence convention (house)
- **Proven** = live-observed in a prior session (cited).
- **Strong (static)** = direct disassembly evidence produced this pass, registers/offsets read off aligned real instructions.
- **Hypothesis** = inference beyond what the bytes alone show; must be gated by a live test.

**Methodology guard (the project's standing burn):** an unaligned linear disp32 scan for `[reg+0x1C0]` / `[reg+0x15C]` / `[reg+0x1D8]` is **pure noise** (desyncs on data, hits unrelated UI/matrix/HID structs, reports overlapping duplicate decodes). Every site below was byte-candidate-scanned and then **re-disassembled ALIGNED from its `find_head` function head** before being trusted. Sites are flagged alignment-verified; raw-scan candidates that failed alignment were rejected and are listed under dead ends.

---

## TL;DR (the decision)

The renderer decides "draw a damage number" vs "draw a Miss/evade glyph" **live at draw time** off two record fields, in this priority:

1. `record+0x1D8` — a **draw-routing bitfield**. `mov r8d,[rdi+0x1d8]` at `0x266B7C`, then `test r8w,r8w`: **any low bit `0..0xF` set ⇒ the DAMAGE-NUMBER path** (draws `+0x344` from staged `+0x1C4`). Only if the low word is clear does it fall through to the high-bit branches `bt r8d, 0x10..0x18`.
2. `record+0x1C0` — the outcome-kind byte — is read **only inside** the high-bit evade branches `bt r8d, 0x16` (`0x266DDA`) and `bt r8d, 0x17` (`0x266E05`), to pick the evade/miss glyph into `+0x342`.

So on a committed **hit**, a low `+0x1D8` bit routes to the number branch and `+0x1C0` is **never consulted**. Writing `+0x1C0=0x06` alone does **not** flip the presentation. **Both independent tracks confirm this.**

**Minimal write-set to convert a hit's presentation to a plain Miss, all writable plain record memory:**

```c
record[0x1C0]            = 0x06;   // plain-miss kind (read by the evade glyph branch)
*(uint16_t*)(record+0x1C4) = 0;    // no damage debit -> no number value, no HP loss
*(uint32_t*)(record+0x1D8) =       // force draw-route: clear low+mid bits, set only the miss-glyph bit
    (*(uint32_t*)(record+0x1D8) & ~0x01FFFFFFu) | 0x00800000u;  // clear bits 0..0x18, set bit 0x17
record[0x360]           = 0x06;    // optional: keep the +0x1C0 mirror consistent for the anim orchestrator
```

`0x00800000 == bit 0x17`. Clearing bits `0..0x18` guarantees no low number-route bit and no competing high glyph bit survives; setting bit 0x17 routes into the `+0x1C0`-reading evade-glyph branch, where kind `0x06` maps to the plain-miss glyph.

**The #1 unknown (blocks a confident ship, cheap to settle live):** whether `+0x1D8` (and `+0x1C0`) for that hit are populated **before** the pre-clamp moment `0x30A66F` (so our write lands on a ready value the later draw frame reads) or **after** it by a VM step that clobbers our write. Neither track could resolve this statically. This is the single A/B live test below.

---

## Q1 — Who CONSUMES `+0x1C0` / `+0x360` and the staged numbers to drive PRESENTATION?

### The consumer is the popup glyph/digit builder fn `0x2667E0`, and it reads `+0x1C0` LIVE at draw time — Strong (static)

Presentation is **not latched at commit**; it is rebuilt each draw frame from the linked battle-record. The reader is fn `0x2667E0` (entry `0x266AE0` is a sub-routine of it). Register map at the reads: `rdi` = presentation/draw record; `rdx = [presentation+0x148]` = the linked 0x200-stride battle-record; `r8d = [rdi+0x1D8]` = the draw bitfield.

```
00266B18  mov  rdx, [rcx+0x148]            ; rdx = linked BATTLE-RECORD
00266B7C  mov  r8d, [rdi+0x1d8]            ; r8d = draw-routing bitfield
00266B93  test r8w, r8w / je 0x266D01      ; low word set -> NUMBER path; else fall to high-bit branches
00266B9D  movzx eax, word [rdx+0x1c4]      ; staged DAMAGE  (number source)
00266BAE  movzx eax, word [rdx+0x1c6]      ; staged HEAL
00266CD7  mov  word [rdi+0x344], ax        ; +0x344 = the NUMBER drawn
00266CE7  mov  word [rdi+0x342], ax        ; +0x342 = glyph-id field
...
00266DDA  bt   r8d, 0x16 / jae ...         ; evade branch A -> reads +0x1C0
00266DE1  mov  al, byte [rdx+0x1c0]        ; <== READS KIND BYTE, LIVE
00266E05  bt   r8d, 0x17 / jae ...         ; evade branch B -> reads +0x1C0
00266E10  mov  al, byte [rdx+0x1c0]        ; <== READS KIND BYTE AGAIN
00266E16  cmp  al, 1 -> glyph 0x21 (kind 01 accessory-evade)
00266E24  sub  al, 2 ; jbe ... -> kind 02/03 parry/block glyph 0x22/0x23
          (kind 0x06 falls through to r10w init 0x20 -> plain-miss glyph 0x20)
00266EC0  btr  dword [rdi+0x1d8], 0x17     ; self-clear the route bit after drawing
```

**This directly satisfies the task's core hypothesis:** because `+0x1C0` and the staged `+0x1C4` are re-read live at draw time (a frame after apply-staging), a same-hit write of `+0x1C0=0x06` + `+0x1C4=0` done inside the pre-clamp callback (`0x30A66F`, apply-staging) *will* be observed by the renderer — **provided `+0x1D8` routes into the evade branch** (see the crux in Q's follow-up below).

### The selector `0x205210` + its 3 popup-region callers are a RED HERRING for the number-vs-Miss decision — Strong (static)

`0x205210` (mid-fn of `0x204FF0`) has exactly 3 callers, all in the popup region (`0x26A683` in fn `0x26A580`; `0x26A7B1`, `0x26A92F` in fn `0x26A704`). Each does `mov cl, byte[rcx+0x1c0]` then `call 0x205210`, but the returned `al` is used **only as a gate to enqueue a popup descriptor at all** (`test al,al / je skip` then `call qword[rax+0x20]` = vtable enqueue). It never branches number-vs-Miss; that decision is downstream in `0x2667E0`. So the selector is the right place to *read* the kind but the wrong place to *decide* presentation. (Consistent with the predecessor report's classification of `0x205210` as render-side / dual-fires on preview.)

### Classified real-code consumers of `record+0x1C0` (alignment-verified)

| RVA | fn head | insn | role |
|---|---|---|---|
| `0x205B38` | `0x2055FC` | `mov [rdi+0x1c0], r12b` | **sole WRITER** (gated `+0x15C` bit2); Proven |
| `0x266DE1` | `0x2667E0` | `mov al,[rdx+0x1c0]` | **render-consume** (kind→glyph, live draw-time), evade branch A |
| `0x266E10` | `0x2667E0` | `mov al,[rdx+0x1c0]` | **render-consume** (kind 01/02/03→evade glyph), evade branch B |
| `0x26A4E5` | `0x26A4B8` | `movzx edx,[rcx+0x1c0]` | **gameplay-gate**: routes kinds {2,3,10} (parry/block/catch) into a sound/anim sub-select via `word[r8+0x1fc]` |
| `0x2061C3` | `0x2060F4` | `mov al,[rcx+0x1c0]` | **gameplay-gate**: `cmp al,9` (reflect) → per-hit result-slot code; this fn is also a `+0x1D8` producer (see Q's writers) |
| `0x1FAB3F` | `0x1FA9E4` | `movzx ecx,[rdx+0x1c0]` | dead end — inside a record-init/clear loop, not a presentation reader |

### The mirror `+0x360` (alignment-verified)

| RVA | fn head | insn | classification |
|---|---|---|---|
| `0x205B4B` | `0x2055FC` | `mov [rdi+0x360], r12b` | **mirror WRITE** — same value/fn as the `+0x1C0` store; immediately followed by the 0x3C anim-timer arm (`mov word[rip+0x602592], r15w`). Proven |
| `0x20C281` | `0x20BFD4` | `mov [rdi+0x360], sil` | gameplay writer (different resolution branch) |
| `0x26B4E0/0x26B513` | `0x26B388` | `mov [rdi+0x360], r11b/bpl` | render/anim-consume region |
| `0x26D38A` | `0x26D278` | `cmp [rdi+0x360], r8b` | **render-gate**: compared before `call 0x266988` in the popup orchestrator (which calls glyph builder `0x2667E0`) |
| `0xC5FA0` | `0xC5F8C` | `mov [rcx+0x360], 1` | **unrelated struct** (false-positive class), excluded |

**`+0x360` role:** the mirror is read/compared in the **animation/orchestration** region (`0x26D38A`), *not* in the number-vs-Miss glyph decision (which keys only off `+0x1C0` and `+0x1D8`). A raw `+0x1C0`-only write that skips `+0x360` would leave the mirror stale → `cmp byte[rdi+0x360]` at `0x26D38A` may mis-gate a redraw/SFX `call 0x266988`. Hence keeping `+0x360=0x06` in the write-set is the cheap consistency insurance, even though it isn't strictly on the glyph-decision path.

### What a raw byte-write SKIPS vs the engine's own `0x2055FC` commit (the honest ledger)
1. **`+0x360` mirror** — read at `0x26D38A`; stale value can mis-gate the redraw/SFX call. (Mitigation: write it, cheap.)
2. **0x3C (60-frame) animation timer** armed at `0x205B52` — a raw write never arms it, so no evade *animation* plays. For a **plain miss (kind 06)** that is fine — 06 carries no special evade animation (it is the "just didn't connect" miss). For 01–04/0B (which do animate) this would matter, but the mod wants a plain Miss, so this is acceptable.
3. The **`+0x15C` bit-2 gate** and any sibling stores inside the `0x2055FC` block do not run — but those are the very writes we are replacing, and on a hit the block is skipped anyway (below).
4. **`+0x1D8` draw-routing** — the actual gate on whether `+0x1C0` is even consulted. Necessary and NOT skippable — it must be part of the write-set (this is the Q-follow-up crux).

---

## Q2 — What SETS `+0x15C` bit 2 (the gate that makes `0x205B38` fire)? — no clean real-code writer

**Strong (static), both tracks:** there is **no aligned real-code writer that sets `+0x15C` bit 2 (mask `0x04`)**. The combat-region writers only touch **bit 1 (value 2)**:

```
0x1FB2EC  or  dword [rcx+0x15c], 2        (fn 0x1FB254)     set bit1
0x1FB35B  and dword [r8+0x15c], 0xFFFFFFFD (fn 0x1FB254)    clear bit1
0x20B7E5  or  dword [rbx+0x15c], 2        (fn 0x20B6DC)
0x20B772  and dword [rbx+0x15c], 0xFFFFFFFD (fn 0x20B6DC)
0x20B750  mov dword [rdi+0x15c], r8d      (fn 0x20B6DC; folds bit1 via cmovne dl==4)
0x26EF94  and dword [rbx+0x15c], 0        (fn 0x26EBEC)     record reset
```

The three sites that **test** mask 4 are `0x205B2F` (the known gate), `0x217142` (fn `0x216E98`), `0x273729` (fn `0x273668`) — three readers, **zero writers** of that bit. `0x204D76` reads a composite mask `test dword[rbx+0x15c], 0x80074` (bits 2,4,5,6,16,19) confirming bit 2 is a real semantic flag, but sets nothing.

**Conclusion (Strong):** `+0x15C` bit 2 is **VM-set on evade-class resolutions only** (consistent with the Proven LT9 fact that a committed hit leaves the gate false). There is **no real-code writer to imitate**, so "set bit2 + kind and let the engine self-commit" is **not viable**.

**Codex adds the killer detail:** even if you *could* set bit2, the finalizer block zeroes the store register first (`xor r12d, r12d` at `0x2059DD`), so `0x205B38` writes `r12b = 0x00` — it would overwrite your `0x06` with a *hit*. So the engine's own commit path is doubly useless for forcing a miss. **Verdict: do NOT rely on the engine self-commit; author the record fields directly.**

Note (ordering, Strong-static): within the state driver `0x210AF0`, **apply-staging runs before the finalizer writer**. Apply is state `0x15` → `0x20BFD4` → `0x30A51C` (`0x20C06E`); the finalizer block is state `0x2D` → `0x20CC50` → `0x2059AC` (calls `0x205AAF`/family reaching `0x205B38`). Because on a hit the finalizer's `+0x1C0` store is gated-off, a pre-clamp write of `+0x1C0` placed at apply-staging is **not** overwritten by the finalizer on a hit. (This is the reassuring half; the un-reassuring half is `+0x1D8`, next.)

---

## Q3 — Where the damage-NUMBER value comes from, and the separate "draw Miss instead" lever

**Number value chain (Strong-static):** `record+0x1C4` (staged debit) → snapshotted at `0x266B9D` → written to presentation `+0x344` at `0x266CD7`/`0x266CAA` → digit-split at `0x2671xx` (`movzx ecx, word[rdi+0x344]`). So zeroing `+0x1C4` (the mod's current lever) legitimately yields the "0" popup. The secondary format-dispatch you traced (`0x22802F` → store `0x228488` → display buffer `0x7832BE`) is the generic number-string formatter *downstream* of the value already chosen (`[rbp+6]` there is a caller arg frame, not the battle-record) — **not** the primary lever.

**The separate Miss lever (Strong-static) — this is `+0x1D8`, not `+0x1C0`:** the "draw a number at all vs draw the Miss/evade glyph" decision is keyed off the `record+0x1D8` draw-routing bitfield, decoded fully below. `presentation+0x342` = the **glyph-id** field; `presentation+0x344` = the **numeric value**. Routing `+0x1D8` into the evade branch makes the engine's own glyph code emit the Miss glyph (from `+0x1C0`) into `+0x342` and skip the number — **no gated writer `0x205B38` needed**.

### Full `+0x1D8` draw-routing decode (fn `0x2667E0`, alignment-verified — codex, corroborated)

```
00266B7C  mov r8d,[rdi+0x1d8]
00266B93  test r8w,r8w / je 0x266D01     ; ANY low bit 0..0xF set -> NUMBER path (first-set wins);
                                          ;   the number path self-clears only its selected low bit
                                          ;   (0x266CF0 and dword[rdi+0x1d8], edx)
; else fall through to high-bit branches (each btr self-clears after drawing):
00266D01  bt r8d,0x10  -> glyph 0x80
00266D1C  bt r8d,0x11  -> glyph 0x90
00266D3C  bt r8d,0x12  -> glyph 0xA0
00266D5C  bt r8d,0x13  -> glyph 0xB0 / 0x35
00266D9A  bt r8d,0x14  -> glyph 0xC0
00266DBA  bt r8d,0x15  -> glyph 0xD0
00266DDA  bt r8d,0x16  -> READS +0x1C0, glyph 0x10/0x11 (evade branch A)
00266E05  bt r8d,0x17  -> READS +0x1C0, MISS/EVADE glyph (branch B): kind01->0x21, kind02/03->0x22/0x23,
                          kind06 falls through to r10w=0x20 (plain-miss glyph); then btr 0x17 at 0x266EC0
00266ED4  bt r8d,0x18  -> glyph 0x30
```

**Number vs evade are effectively mutually exclusive by priority:** the low-word test wins first; if any low bit is set the evade branches are never reached. So to force Miss you must (a) ensure **no low bit 0..0xF is set** and (b) **set bit 0x17** (the plain-miss glyph branch that reads `+0x1C0`). Codex's robust formulation — clear bits `0..0x18` (`& ~0x01FFFFFF`) and set only `0x17` (`| 0x00800000`) — subsumes both requirements and also clears the competing high glyph bits.

> Reconciliation note: the opus track initially summarized "bit 2 = number route, bit 0x16 = evade"; codex's fuller decode shows the number route is *the whole low word 0..0xF (first-set-wins)* and the plain-miss glyph is *bit 0x17* (with 0x16 a second evade branch). The codex decode is the more complete and is adopted; the safe write-set (clear low+mid, set 0x17) is correct under either reading.

### `+0x1D8` writers — real-code vs VM (Strong-static + Hypothesis)

Aligned real-code producers found set only **high scratch bits** (`0x1B–0x1D`, `0x19–0x1A`), not the hit number-route low bits or bit `0x17`:

```
0x2064F3/FD/07  bts dword[rdi+0x1d8], 0x1D/0x1C/0x1B   (fn 0x2060F4; no direct E8 callers)
0x2688C4/D4     bts dword[rcx+0x1d8], 0x19/0x1A         (fn 0x268584; no direct E8 callers)
; word-granular writes, per-bit semantics unresolved:
0x2244A0        mov word[rdi+0x1d8], r11w               (fn 0x2239 3C)
0x2DBF54        mov word[rdi+0x1d8], r10w               (fn 0x2DB130)
0x32D73F        mov word[rdi+0x1d8], r12w               (fn 0x32D3B0)
```

**No aligned hookable `bts/or` produces the committed-hit number-route low bit or bit `0x17`.** Best inference (Hypothesis): the hit's low draw bit is populated either through one of those `mov word[..+0x1D8], reg` flows or by VM-side code — i.e. `+0x1D8`'s *semantics* are partly VM-owned like `+0x15C` bit2. **But** — the decisive practical point — `+0x1D8` is **plain writable record memory**, so the mod does not need a real-code writer to imitate: it can just author the dword directly from the pre-clamp callback. Real-code ownership only matters for the timing question (Q-crux #3), not for our ability to write it.

### Ordering crux (#1 live-test unknown) — Hypothesis, both tracks

The pre-clamp `0x30A66F` fires at apply-staging. Neither track could statically prove whether `+0x1D8` (and `+0x1C0`) for that hit are populated **before** apply-staging (our write lands on a ready value that the later draw frame reads) or **after** it by a subsequent VM step that clobbers our write. This is the one thing the bytes don't settle, and it decides whether the whole approach works from the pre-clamp hook or needs a later write point.

---

## Recommended implementation design

**Goal:** turn a VM-committed hit into a clean **plain-Miss presentation** (Miss glyph, no number, no HP change), reusing the already-proven pre-clamp hook.

### Primary design — author the record from the pre-clamp callback (`0x30A66F`)
The mod already hooks `0x30A66F` (`movsx eax, word[rbp+6]`, `rbp` = target unit/record; `rbp+6 == unit+0x1C4`). From that same callback, when the DCL roll says "miss", write the full record set (all offsets relative to the record base `rbp - 0x6`, i.e. the 0x200-stride battle-record for the target):

```c
uint8_t*  rec = (uint8_t*)rbp - 0x6;          // record base (rbp points at +0x1C4)
rec[0x1C0]              = 0x06;                // plain-miss kind
*(uint16_t*)(rec+0x1C4) = 0;                  // no debit (already the mod's current lever) -> HP kept, no number value
rec[0x360]              = 0x06;                // keep +0x1C0 mirror consistent (anim orchestrator gate 0x26D38A)
*(uint32_t*)(rec+0x1D8) = (*(uint32_t*)(rec+0x1D8) & ~0x01FFFFFFu) | 0x00800000u; // route to miss-glyph bit 0x17
```

Keep `word[rbp+8] (=+0x1C6) = 0` as well (no heal), exactly as the existing force-EVADE recipe does. This is a superset of the current "zero the debit" behavior plus the three presentation fields, all in one callback, same hit, before the draw frame.

**Why this and not the writer/gate route:** Q2 proves there is no clean real-code path to make the engine self-commit a miss (no `+0x15C` bit2 writer; the finalizer would write `0x00` anyway). Authoring the plain record memory directly is the only viable lever, and `+0x1D8`/`+0x1C0`/`+0x360` are all ordinary writable memory (Denuvo virtualizes code, not data — the same principle proven for the evade input bytes).

### Contingency (if the pre-clamp write is too early — see live test)
If the A/B test shows the VM repopulates `+0x1D8`/`+0x1C0` **after** apply-staging, move the presentation write to a **later** point that still runs before the draw:
- **Option B1 — a managed poll write** on the target record between resolution and draw (the same poll mechanism already used for `EvadeOverride`/forecast poke), gated to fire once per resolved miss. Racy against retained-mode redraw, same caveat as the forecast poke, but proven-workable there.
- **Option B2 — hook the finalizer tail `0x2059AC`/`0x205B38` region** (`ExecuteFirst`) and author `+0x1C0`/`+0x360`/`+0x1D8` there for our forced-miss targets; this runs at state `0x2D`, strictly after apply-staging (state `0x15`), so it is downstream of any VM population that races the pre-clamp. Read the record from `rdi`. This trades one extra hook for guaranteed post-population ordering.
- **Option B3 — hook the popup orchestrator `0x26D278` or the glyph builder `0x2667E0` entry** and force `+0x1D8`/`+0x342` at draw time. Cleanest visually (last possible moment) but requires resolving our forced-miss target at that site (walk `[rcx+0x148]` → record → `+0x1BC` target idx, match against the DCL decision cache). Use only if B1/B2 also lose the race.

### What NOT to do (proven dead ends, do not retry)
- Do **not** hook `0x205B38` to flip a hit→miss: on a hit its `test [rdi+0x15C],4` gate is false so it never fires (Proven, LT9); and even when it fires it writes the zeroed `r12b`.
- Do **not** try to set `+0x15C` bit2 to trigger the engine commit: no real-code writer, and the commit writes `0x00`.
- Do **not** write `+0x1C0=0x06` alone: the low `+0x1D8` number-route bit wins and `+0x1C0` is never read.

---

## Live-test plan

**Setup:** a forced-miss attack (mod's DCL roll returns miss for a guaranteed-hit action, e.g. Agrias→Ramza 100%), read-only instrumentation first, then the write.

### Test 1 — the decisive ordering probe (#1 unknown): does the pre-clamp write survive to the draw?
1. **Read-only pass:** at the pre-clamp callback, log `rec[0x1C0]`, `word[rec+0x1C4]`, `dword[rec+0x1D8]` **as they are when the callback fires** (before any write). Then install a one-shot read-only probe at the glyph builder read sites `0x266B7C` (log `[rdi+0x1d8]`) and `0x266DE1`/`0x266E10` (log `[rdx+0x1c0]`) for the same target, to capture the values **at draw time**.
   - **If `+0x1D8` at draw time already has a low bit set and `+0x1C0` = hit(0x00):** the VM populates these at/after commit → confirms we must overwrite them, and tells us whether the pre-clamp value survives.
2. **Write pass:** apply the primary write-set at the pre-clamp callback for the forced-miss target. Re-read at the draw-time probe.
   - **PASS (approach works from pre-clamp):** draw-time probe shows `[rdi+0x1d8]` bit 0x17 set / low bits clear, `[rdx+0x1c0] = 0x06`; on screen a **"Miss"** with no number and HP unchanged.
   - **REFUTE (write too early):** draw-time probe shows `+0x1D8`/`+0x1C0` back to hit values (VM clobbered our write) and the screen shows "0" damage → move to contingency B2 (finalizer-tail hook) and repeat.

### Test 2 — glyph identity confirmation
With the write applied, confirm the on-screen glyph is the **plain-miss** presentation (kind 0x06 → `r10w` init `0x20` branch), not a class-evade/parry glyph. If it renders as a parry/block, adjust the kind byte (the enum→glyph map at `0x266E16..0x266E37`) — 0x06 is the intended "just missed" glyph; 0x04 would render "class Miss" if that reads better in-game (both are acceptable Miss presentations, this is a taste call to make live).

### Test 3 — mirror/anim consistency
Toggle the optional `+0x360=0x06` write on/off and watch for any spurious redraw/SFX artifact gated by `0x26D38A` (`cmp [rdi+0x360]`). If no artifact appears with it **off**, `+0x360` can be dropped from the write-set; keep it if any flicker/sound glitch appears.

**What refutes the whole design:** if even the finalizer-tail (B2) write is clobbered before draw, or if `+0x1D8` bit 0x17 does not route to the miss glyph as decoded (i.e. the static decode is wrong), the presentation-forge approach fails and the fallback is the current "0-damage hit-animation" behavior (functionally a miss, cosmetically wrong) — no worse than today.

---

## Dead ends recorded
- Selector `0x205210` + its 3 callers: a "should a popup exist" gate only; **not** the number-vs-Miss decision (both tracks). Real callers of the popup fns: `0x26A5E1`/`0x26A72D` (fn `0x26A580`), `0x31C830`/`0x31C892` (fn `0x26A704`).
- No aligned real-code writer sets `+0x15C` bit2 → it is VM-set; "engine self-commit via bit2" is not buildable, and the finalizer zeroes the stored kind anyway.
- No aligned hookable `bts/or` produces the committed-hit number-route low bit or `+0x1D8` bit 0x17; but `+0x1D8` is plain writable memory so a real-code writer is not required — only the timing matters.
- `+0x360` at `0xC5FA0` is an unrelated struct (false-positive class), excluded.
- Setup record-builders (`0x284BEC/…`) and the pre-clamp `[rbp+6]` secondary formatter `0x228488`/`0x7832BE`: wrong polarity / downstream of the value choice — not levers here.
- Raw disp32 scans for `+0x1C0`/`+0x15C`/`+0x1D8` across whole sections: pure noise; every accepted site was re-aligned from a `find_head` function head.
