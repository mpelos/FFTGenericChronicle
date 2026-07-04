# DCL — Miss-Consumption Signal & Counter/Reaction Result Path (offline static RE)

**Date:** 2026-07-04 (unix 1783184308) · **Target:** `FFT_enhanced.exe` (Steam v1.0, Denuvo, image base `0x140000000`, no ASLR, real code RVA `0x1000..0x610000`).
**Method:** offline static disassembly only — pefile + capstone 5.0.7, image on disk read-only. No live tests, no codemod edits, no deploys.
**Scripts (all in `work/`):** `disasm_q_common.py` (shared helpers), `disasm_q2_counter.py`, `disasm_q1_consume.py`, `disasm_q_refine.py`, `disasm_q_deep.py`, `disasm_q_table.py`, `disasm_q_counter2.py`, `disasm_q_final.py`.

## Confidence convention (house)
- **Proven** = live-observed in a prior session (cited).
- **Strong (static)** = direct disassembly evidence in this pass, register/context read off real instructions, no live confirmation yet.
- **Hypothesis** = inference beyond what the bytes alone show; must be gated by a live test.

Everything newly derived here is at best **Strong (static)** until a live probe confirms it. The `0x59F550` HID false-positive (documented in the RE manual) is the cautionary standard: I distinguish *bytes I disassembled* from *what I infer they mean*.

---

## Method notes / caveats about the evidence

- Head-finding is the int3-pad walkback used by every prior report (`find_head`). Caller enumeration is exact `E8 rel32` / `E9 rel32` target matching across executable sections with RVA < `0x610000`.
- A **linear-disasm store scan for `[reg+0x1C0]` is unreliable** (it desyncs on data and reports the same instruction at two overlapping starts, e.g. `89 81 …` and `48 89 81 …`). I only trust `+0x1C0` sites that I re-disassembled **aligned from a known function head**. The one real-code writer of `+0x1C0` confirmed this way is `0x205B38` (below); the raw byte-scan's dozens of other `+0x1C0` hits are unrelated structs (UI/matrix/HID) and are NOT evade-byte writers.
- VM-thunk detection = head is `E9` into `>= 0x610000`. All the "roll" and skillset-resolver calls in the reaction cluster are thunks, as the manual predicts.

---

## Q1 — a once-per-execution miss/evade **commit** signal

### Candidate 1 — the evade-type `+0x1C0` writer `0x205B38` inside fn `0x2055FC`  ★ RECOMMENDED

Aligned disassembly (fn head `0x2055FC`, `rdi` = the actor/result record throughout):

```
205B2F  F6 87 5C 01 00 00 04   test byte [rdi+0x15C], 4      ; GATE
205B36  74 07                  je  0x205B3F                  ; skip write if bit clear
205B38  44 88 A7 C0 01 00 00   mov  byte [rdi+0x1C0], r12b    ; <== WRITE evade-type byte
205B3F  8A 87 BE 01 00 00      mov  al, byte [rdi+0x1BE]      ; reads +1BE result flag
205B45  41 BF 3C 00 00 00      mov  r15d, 0x3C                ; a 60-frame timer value
205B4B  44 88 A7 60 03 00 00   mov  byte [rdi+0x360], r12b    ; mirror evade byte -> +0x360
205B52  66 44 89 3D 92 25 60 00 mov word [rip+0x602592], r15w ; arm 0x3C-frame anim timer
205B5A  3C 06                  cmp  al, 6                     ; branch on evade/hit kind
```

**What the bytes show (Strong-static):** this is the **result-teardown / animation-authoring** step. It is the sole real-code writer of the evade-type byte `+0x1C0` (the manual already names `0x205B38` as "the real-code copy; the authoring value is produced inside the VM roll `0x30FA34`"). Here it also **mirrors the same byte to `+0x360`** and **arms a `0x3C`-frame (≈1 s) animation timer** — behavior that only makes sense when a result is being *committed for display*, not during a passive AI/forecast evaluation.

**Why this distinguishes execution from preview/AI (Strong-static + one Proven corroboration):**
- The `test [rdi+0x15C], 4` gate g024: the write only happens when bit 2 of the record's `+0x15C` state byte is set. Forecast/AI evaluation does not author an on-screen evade animation, so it should not pass this gate. (Bit-4-of-`0x15C` meaning is **Hypothesis** until a live read — see live test.)
- Arming a fixed 60-frame timer is a presentation act; previews don't animate a miss.
- Corroboration: the manual's SELECTOR-PROBE observed evade types **only at execution** (Blade Grasp `0x0B`, class-evade `0x04`), never during idle forecast polling — consistent with the authoring happening once, here, at commit.

**Context / who is identified:** `rdi` = the result record (same object family the selector calls "record", reachable as `[actor+0x148]`). At this site the **evade kind is in `r12b`** and the record is `rdi`. To recover (caster, target, ability) you read from the record: target index at `+0x1BC`; the order-record spine (`caster+0x1A0` packing casterIdx|type|abilityId) is *not* in a register here, so a hook would deref the record → its owning unit → `+0x1A0`. This is the same record the pre-clamp and selector use, so the correlation to the mod's decision cache key is available (Strong-static).

**Callers of `0x2055FC`:** `0x205AAF` (a self-loop within `0x2055FC` iterating the per-unit result records — i.e. it runs once per affected unit of an executed action) and `0x212B82` (in fn `0x210AF0`). The self-loop is the tell that this fires **per resolved target of an executed action**, exactly the granularity the decision cache wants (one fire per (record=target) at commit).

**Hook guard AOB (for a future `DclHitConsumptionProbe`):**
- Site RVA `0x205B38`, expected bytes `44 88 A7 C0 01 00 00` (`mov [rdi+0x1C0], r12b`).
- Guard window (unique, spans the gate): `F6 87 5C 01 00 00 04 74 07 44 88 A7 C0 01 00 00` (`test [rdi+0x15C],4 ; je ; mov [rdi+0x1C0],r12b`).
- **Recommendation:** hook `0x205B38` **ExecuteFirst**. Read `r12b` = committed evade/hit kind (0x00 hit … 0x04 class … 0x06 miss … 0x0B Blade Grasp), and `rdi` = record; derive target = `byte[rdi+0x1BC]`, caster/ability from the record's owning unit `+0x1A0`. Fire the cache-invalidation when `r12b != 0x00` (a miss/evade was committed) — or unconditionally, treating any fire as "this (caster,target,ability) executed." One fire per target per executed action; does not fire for preview.

### Candidate 2 — result/animation selector `0x205210`  ✗ NOT a clean signal

Head `0x204FF0`; `0x205210` is a mid-function entry. Exactly **3 callers, all in the combat-popup region**: `0x26A683` (fn `0x26A580`), `0x26A7B1` and `0x26A92F` (fn `0x26A704`). Each does `mov cl, byte [rcx+0x1C0]` immediately before the call — i.e. it **consumes** the already-authored evade byte; it does not author it.

Aligned body shows it is a pure **render classifier**: it reads `+0x1BE` (result flag), `+0x1E5` (effect-kind), `+0x1C4` (staged dmg), and the `+0x1D0..+0x1D5` staged-result bytes, and fills a small render descriptor (`rbx+0x0/+0x8/+0x30`). It returns `al` = "should draw" and is called from the popup/animation builders (`0x31BEC0` → `0x26A704` → selector).

**Verdict (Strong-static):** `0x205210` is render-side and is reachable on both preview and execution frames (the popup builders run whenever the panel/animation is drawn). It is **not** a once-per-execution commit and would double-fire / fire on preview. Do **not** use it for consumption. (It remains the right place to *read* the final evade kind for a probe, but not to *count* executions.)

### Candidate 3 — evade-record store sites `0x284BEC/…/0x3964A5`  ✗ REFUTED as commit markers

Aligned, all nine sites are **combat-input record BUILDERS** in fns `0x284A80` / `0x3600DC` / `0x396208`. They pack the defender's evade block into a forecast/AI record at action SETUP:

```
284BEC  mov [rbx+0x44], ax   ; ax = [rsi+0x4B]  (class, 1:1 copy)
284C00  mov [rbx+0x46], ax   ; ax = MAX([rsi+0x49] acc, [rsi+0x4A] shield)  (shield DERIVED)
284C28  mov [rbx+0x50], ax   ; ax = MAX([rsi+0x4D],[rsi+0x4E])
```

These are the exact builders the manual already dissected for the shield-evade read path (`dst+0x44 = [unit+0x4B]`, `dst+0x46 = MAX(shield,accessory)`). They run at **setup, before the roll** (and before `0x309A44`), and the manual explicitly REFUTED them live as feeding the combat roll (LT5-A3: forcing their stores to 0 did not change the shield outcome). They are **inputs at setup**, not **outputs at commit** — the opposite polarity from what a consumption signal needs. Do not use them.

### Q1 recommendation (summary)
Hook **`0x205B38`** (fn `0x2055FC`), **ExecuteFirst**, expected bytes `44 88 A7 C0 01 00 00`, guarded by the preceding `test [rdi+0x15C],4 ; je`. Read `r12b` (committed evade/hit kind) and `rdi` (result record → `+0x1BC` target, owning-unit `+0x1A0` for caster/ability). Confidence: **Strong (static)** that this authors the committed evade byte once per executed target and skips preview; the *exact* preview-vs-execute discrimination via `+0x15C` bit-2 is **Hypothesis** pending a live read.

---

## Q2 — why counters bypass `computeActionResult 0x309A44`, and where a counter's result is computed

### `computeActionResult 0x309A44` has exactly two callers (Strong-static)

`0x309A44` is a **genuine real-code function head** (standard prologue, not a thunk, no tail-jumps into it). Exhaustive `E8`/`E9` xref scan over all real code:

- **`0x281F85`** — inside fn `0x281D60`. This is the **multi-target / AI candidate sweep**: a loop `for rbx in 0..0x14` (`cmp rbx, 0x15; jl`) over the 21-entry unit/result array (`lea r14,[rax+0x1853E80]`), reading each target index `dl = [rbp+rbx-0x28]`, skipping `0xFF`, then `call 0x309A44`. It stages a global target index (`[0x52E807]`) each iteration. → the preview/AI per-target evaluation spine.
- **`0x307F68`** — inside fn `0x307E90`. This is the **single-action execution** caller: it sets the pending-order fields on the caster (`byte[rcx+0x1A1]=1`, `word[rcx+0x1A2]=abilityId`), does `add rcx, 0x1A0` (the order-record spine), loads `dl` = target, then `call 0x309A44`, and restores `+0x1A1/+0x1A2` after. `0x307E90`'s pointer lives in an **action-handler dispatch table** (file offset `0x682CE0` region — a table of `0x327xxx/0x328xxx` handlers), so execution is entered by indexed dispatch on action type, not a direct call.

**Both callers are the *normal action pipeline* (menu/AI → execute or forecast).** Neither is a reaction path. Since `0x309A44` is hooked at its entry, a counter that does not reach it must be **computed by a different function entirely** — confirmed: no tail-jump and no other xref reaches this head.

### The reaction machinery does NOT compute damage via `0x309A44` (Strong-static)

The reaction dispatcher is fn `0x30B410` (contains the `0x30BE86..0x30BF72` Brave-gate cluster region as siblings). Its full call graph:

```
0x30B41E -> 0x30DDB0  [VM thunk]   reaction-eval (per-unit predicate)
0x30B6E2 -> 0x276ED4  [VM thunk]
0x30B745 -> 0x2B8CE8  [VM thunk]   item/equip lookup
0x30B759 -> 0x2BB0D4  [VM thunk]   SKILLSET resolver (reactions live here, per manual)
0x30B7BE -> 0x2B8CB8  [real]       item id->row map
0x30B882 -> 0x30B234  [real]       reaction predicate (reads +0x65)
0x30BBD8 -> 0x30B30C  [real]       reaction predicate (reads +0x97/+6)
0x30BBDD -> 0x30B410  [real]       RECURSES (walks reaction chain)
0x30BC13 -> 0x41FB70  [real]       stack-cookie/util
```

- `0x30B410` is a **big switch on a global reaction-type id** (`byte[0x4A5348]`, values compared up to `0x56`) that, per branch, stages a reaction **outcome id** into the reaction record (`word[rbx+0x10]`, `word[rbx+0x28] = word[rbx+6]`) and recurses to walk the reaction chain. Values like `0x1B1/0x1B2/0x1B7/0x1B9` are staged into `+0x10` — these are reaction/animation ids, **not damage**.
- The reaction siblings `0x30B234` (`test [r11+0x65],4`) and `0x30B30C` (`test [r11+0x97],0x40`) are **detection predicates** returning a small code (6/7/…); they call the VM eval `0x30DDB0` and compute no damage.
- **None** of the reaction-dispatch calls reach `0x309A44`. The damage roll itself is VM-internal (via `0x2BB0D4` skillset → VM), matching the Proven LT6/LT7 observation that the counter's damage fired the pre-clamp with `reason=no-calc-entry` (no computeActionResult entry existed for it).

### Where the counter's (attacker, target, ability) context IS, and its result computed — fn `0x30C798`  ★ PROPOSED PROBE

The apply function `0x30A51C` (staged HP debit clamp+apply, hook `0x30A66F`) has **5 real-code callers**:

```
0x20452B (fn 0x20435C)  normal action-resolution driver
0x2047BF (fn 0x20435C)  "
0x20C06E (fn 0x20BFD4)  a second resolution path
0x30C7DC (fn 0x30C798)  <== distinct staging fn (reaction/counter candidate)
0x38A6F9 (fn 0x38A4FC)  the event-queue result DISPATCHER (edx==0x300 apply branch)
```

`0x38A4FC` is the queue dispatcher (`0x30F0C4` category producer → `cmp edx,0x300` → apply) — the shared sink **all** applied HP changes pass through, including counters. But the *staging* that a counter goes through before the queue is fn **`0x30C798`**:

```
30C798  head; rcx = a result record (checks byte[rcx+1]==0xFF), rdx = out buffer
30C7B6  movzx eax, byte [rcx+0x1BC]     ; target index
30C7C5  mov  [0x155E829], eax           ; stash target idx (global)
30C7CB  mov  dword [0x155E7C7], 1       ; a "1" flag
30C7D5  movzx ecx, byte [rcx+0x1BC]     ; target idx (arg)
30C7DC  call 0x30A51C                   ; APPLY staged HP change
30C7EB  mov  cl, byte [rbx+0x1E8] -> [rdi+0]   ; copy result bytes to out buffer
30C7F3  mov  cl, byte [rbx+0x1E9] -> [rdi+1]
30C809  mov  cl, byte [rbx+0x29]  -> [rdi+2]   ; (gated) level? -> out
```

`0x30C798` reads the **target index `+0x1BC`** and the **result bytes `+0x1E8/+0x1E9`** off the record `rbx`, applies the staged damage, and emits an outcome descriptor. Its **sole caller is `0x20460A`** in fn `0x20435C` — the same action-resolution driver that owns the normal `0x30A51C` calls. So a counter is resolved through `0x20435C` → `0x30C798` → apply, **staging its result from the record `rbx` without going through `0x309A44`**. This is the most likely place a counter's (attacker, target, ability) context is live in real code post-roll.

**Important honesty caveat (evidence vs inference):**
- **Strong (static):** `0x30C798` reads target `+0x1BC`, applies damage via `0x30A51C`, and is one of only two apply-staging functions distinct from `computeActionResult`. It is the best static candidate for "a counter's result being computed/committed outside the `0x309A44` hook."
- **Hypothesis (must be live-gated):** that `0x30C798` is *specifically* the counterattack path (vs. a shared post-roll commit used by ordinary actions too). The record `rcx`/`rbx` here is checked as `[rcx+1]==0xFF`, i.e. a *staged result record*, not obviously the reaction order-record. Whether the **attacker** identity is recoverable at this site is unconfirmed — the disassembly shows target (`+0x1BC`) and result bytes clearly, but no attacker register/field is read in the window. Do **not** assert this is "the counter attacker context" from the bytes alone.

### Q2 proposed probe (to confirm live later)

Install a `DclCounterPathProbe` that hooks **`0x30C798`** (ExecuteFirst; head prologue `48 89 5C 24 08 57 48 83 EC 20 80 79 01 FF`) and logs, per fire: `rcx` (record ptr), `byte[rcx+0x1BC]` (target idx), `byte[rcx+0x1E8]/[rcx+0x1E9]` (result), and — for attacker discovery — walk `rcx` back to its owning unit (`(rcx - table_base)/0x200` if it lies in the unit array `0x141853CE0`, stride `0x200`) and read `+0x1A0` (order-record spine) if present. Cross-correlate its fire timing against the pre-clamp `0x30A66F` and `computeActionResult 0x309A44` during a battle where a counter triggers:
- **Expected if the hypothesis holds:** `0x30C798` fires for the counter's damage; `0x309A44` does NOT fire for that same counter; the pre-clamp DOES (matching LT6/LT7).
- If `0x30C798` also fires for ordinary attacks, it is a shared commit, not counter-specific — then the counter's *unique* real-code surface is only the pre-clamp `0x30A66F` with `reason=no-calc-entry`, and the counter's attacker must be resolved from the **frame-side global** the exec caller uses (`0x307E90` reads a current-actor global `[rip+0x15630D8]`; note LT7 warned the frame-side caster can disagree with the calc-entry cache — the same disagreement class applies here).

---

## Combined "next live test" sketch

1. **Q1 consumption signal (one battle):** install a read-only probe at `0x205B38` (ExecuteFirst, no writes). Log `[fireCount, r12b, rcx/rdi record, byte[rdi+0x1BC] target, byte[rdi+0x15C]]`. Do a forced-miss attack (mod forces class-evade) and confirm: exactly **one** fire per executed target with `r12b == 0x04` (or `0x06`), and **zero** fires while merely hovering the target in the forecast panel. If clean → promote `0x205B38` to the `DclHitConsumptionProbe` invalidation hook. Also read `+0x15C` on a hovered-but-not-executed target to nail the bit-2 gate meaning.

2. **Q2 counter path (one battle with a Counter/reaction unit):** install the `0x30C798` read-only probe + keep the existing `0x309A44` and `0x30A66F` probes. Provoke a counter. Confirm the fire pattern above. If `0x30C798` is counter-specific and exposes the attacker, it becomes the counter's (attacker,target,ability) ownership site; otherwise fall back to pre-clamp + frame-global with the LT7 disagreement caveat.

## Dead ends worth recording
- Byte-scanning `[reg+0x1C0]` across the whole image is pure noise (dozens of unrelated structs, plus overlapping-decode duplicates) — only the head-aligned `0x205B38` is a real evade-byte writer. Anyone re-deriving this must align from a function head, not trust a raw disp32 scan.
- Selector `0x205210` and the setup record-builders (`0x284BEC/…`) are the wrong polarity for a consumption signal (render-consume and setup-input respectively); both already partially refuted live in the manual.
- The reaction dispatcher `0x30B410` stages animation/outcome **ids** (`0x1B1`…), not damage; the counter's damage math is VM-internal (via `0x2BB0D4`), so there is no real-code "counter damage formula" to hook — only its staged **apply** (`0x30C798`/`0x30A51C`/pre-clamp `0x30A66F`).
