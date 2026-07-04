# DCL Kind-Paint Analysis — authoring Miss / Parry / Block at the compute point

**Date:** 2026-07-03  ·  **Mode:** offline static disassembly only (no live tests)
**Binary:** `FFT_enhanced.exe`, image base 0x140000000, real code RVA 0x1000..0x610000
**Scripts:** `work/disasm_kind_paint.py` … `disasm_kind_paint6.py` (extend `disasm_evadetype.py`)

Field recap (TARGET unit, table 0x141853CE0, stride 0x200):
`+0x1BE` forecast-object base · `+0x1BF` companion byte · `+0x1C0` **result/evade kind**
(0=hit,1=cloak,2=weapon-parry,3=shield-block,4=class-evade "Miss",6=plain miss) ·
`+0x1C2` word · `+0x1C4` staged HP damage (word) · `+0x1E5` result flag (0x80=apply) ·
`+0x1D0` status/immunity word · `+0x15C` "has-result" gate flag.

---

## 1. Who reads +0x1C0, and which read drives MESSAGE / ANIMATION

Byte-accurate scan of the whole real-code region found **exactly 9 real readers** of
`[reg+0x1C0]` and **exactly 1 real writer**. All 9 readers verified by clean-window
disasm (none are mid-stream false positives). Classification:

| Reader RVA | Enclosing fn | Role | Message/anim? |
|---|---|---|---|
| **0x1FAB3F** | 0x1FA9E4 | multi-target loop (0x15=21 iters) → maps kind 0..6 into a **per-target display/anim code buffer** (`mov [r9],0..6`) | **YES — animation code builder** |
| **0x26A98B** | **0x26A704** | post-selector **kind switch** → sets message-id r14 (0x18/0x19/0x58/0x59/0x5A…) fed to emitter `call 0x268E7C` | **YES — message/anim dispatch** |
| 0x26A67D | 0x26A580 | loads kind into `cl`, immediately `call 0x205210` (selector) | feeds selector |
| 0x26A7AB | 0x26A580 | same pattern, 2nd selector call | feeds selector |
| 0x26A8F1 | 0x26A704 | same pattern, selector call at 0x26A92F | feeds selector |
| 0x26A4E5 | 0x26A3xx | switch on kind∈{2,3,7} → UI/target-validity gate | internal logic |
| 0x2061C3 | 0x206xxx | kind==9? else `neg/sbb/and 3` → small state byte | internal logic |
| 0x266DE1 | 0x266xxx | `kind-4` → forecast word `[rdi+0x342]` (preview/forecast UI) | forecast preview |
| 0x266E10 | 0x266xxx | kind==1 → id 0x21, else branch (forecast/preview) | forecast preview |

**The two rendering consumers are 0x1FAB3F (animation code builder) and 0x26A704
(message/animation dispatcher).** Both are keyed on **+0x1C0 essentially ALONE** as
the kind selector — with these companion conditions:

- **0x1FAB3F** switch is pure on +0x1C0 for kinds 0..6. Only the *hit* leg (fall-through)
  additionally consults `+0x1D0 & 4` and `+0x1BF` to decide "did it actually connect / crit".
  Kinds 2 (parry), 3 (block), 4/6 (miss) map straight to display codes with **no HP/flag test**.
- **0x26A704** dispatch (0x26A98B): switch purely on +0x1C0 (0..7). It does **not** gate on
  `+0x1C4==0` or `+0x1E5`. The message id it emits is chosen by kind alone; damage number
  (`+0x1C2`/`+0x4E`) is emitted separately only on the hit path.

### The selector 0x205210 is a **pure transform**, not a re-derivation

Critical finding from the selector body (section D dump): **0x205210 never writes +0x1C0.**
Its input is `cl` (the kind, already loaded from +0x1C0 by its callers) plus the unit ptr in
`r8`. It reads `+0x1BE` (must be non-zero → "has forecast"), `+0x1E5` (sign = special),
`+0x1C4` (staged damage, only for the negative/knock path), and the evade-source bytes
`+0x1D2..+0x1D5`, `+0x1D0`, `+0x1D8`. It then writes an **outparam struct** (`[rbx]` /
`[rbx+8]` / `[rbx+0x30]`) with message codes (0x12,0x13,0x14,0x16,0x17,0x09) — NOT back into
the unit. So the selector is a **kind→message-code lookup**, gated by whether the unit has a
staged forecast (`+0x1BE != 0`) and by the evade-source bytes.

This exactly explains the old "paint only re-skins a natural hit" observation: when we paint
+0x1C0 via the selector path, the selector re-reads the unit's evade-source bytes
(`+0x1D0/1D2..1D5`) and, if the engine's own verdict already produced an avoid, those bytes
drive it back to the natural code (branch at 0x205348..0x205363 → message 0x17) — our +0x1C0
`cl` is overridden by the evade-source state. On a natural hit those bytes are clear, so the
selector honours our `cl`. **The selector consults the raw evade-source bytes, not just +0x1C0.**

---

## 2. Call ordering — where +0x1C0 is finally consumed, and is it re-written after 0x281F8A

### Xrefs (E8/E9 rel32) to the key functions
```
compute 0x309A44 : 0x281F85 (sweep call), 0x307F68            (2 xrefs)
selector 0x205210: 0x26A683, 0x26A7B1, 0x26A92F               (3 xrefs — all inside
                                                               presentation fns 0x26A580/0x26A704)
apply   0x30A66F : (none — reached by fall-through/jump inside 0x30A5xx apply routine)
```
### The single +0x1C0 WRITER
Whole-region scan → **one** real writer:
```
0x205B38: mov byte ptr [rdi+0x1C0], r12b     gated by  test [rdi+0x15C],4 ; je (skip)
```
inside fn **0x2055FC** (a result-finalize routine; recurses via 0x205AAF, also entered from
0x212B82). This is the **compute/finalize stage** that stamps the kind onto the unit. There is
**no writer of +0x1C0 anywhere in the presentation functions** (0x1FA9E4, 0x26A580, 0x26A704)
nor in the selector 0x205210.

### Resulting frame order (static)
```
sweep driver fn (starts ~0x281D60)
  └─ loop rbx=0..0x14 (21 targets):
       0x281F85  call 0x309A44   ── COMPUTE: stages this target's bundle
       0x281F8A  ← OUR HOOK (post-call compute point; +0x1C4 write PROVEN to leak, LT4)
     … driver tail (0x281F93+) finalizes/among which the finalize fn 0x2055FC writes +0x1C0
  … later, apply (0x30A5xx/0x30A66F) commits HP
  … later still, PRESENTATION pass:
       0x31C830 → 0x26A704 → (0x26A8F1 reads +0x1C0 → 0x205210 selector) → 0x26A98B kind switch
                                                                          → 0x268E7C message/anim emit
       0x20C4A5 → 0x1FA9E4 → 0x1FAB3F reads +0x1C0 → per-target anim code buffer
```

**Answer to the ordering question:** the rendering reads of +0x1C0 happen **strictly AFTER**
the compute point 0x281F8A (they live in a separate presentation pass reached from
0x31C830 / 0x20C490, not from the compute call). The engine's **only** write to +0x1C0 is at
the finalize stage (0x205B38), which — per the driver tail — executes **after** 0x281F8A within
the same target iteration. So:

- A +0x1C0 value that is *already committed* to the unit before finalize can be **overwritten**
  by 0x205B38 (this is the finalize stamp deriving the kind from the VM verdict / evade-source).
- The presentation readers see whatever +0x1C0 holds **after** finalize.

There is **no second, later re-write** of +0x1C0 between finalize and presentation — presentation
is a pure reader. The "engine re-derives our paint" effect is **not** a late re-write of the byte;
it is (a) the finalize stamp 0x205B38 running after our hook, and (b) the selector 0x205210
re-consulting the evade-source bytes `+0x1D0/1D2..1D5` at render time.

---

## 3. VERDICT — is compute-point (0x281F8A) kind-paint a clean authored-outcome lever?

**Partially, with a hard constraint. Writing +0x1C0 alone at 0x281F8A is NOT reliable**, for two
independent reasons, both proven statically:

1. **The finalize writer 0x205B38 runs after our hook** and can re-stamp +0x1C0 from the VM
   verdict. Its write is *gated* by `test [rdi+0x15C],4` — so it only fires on units flagged with
   the 0x04 result bit. To make our +0x1C0 survive, that write must either not fire, or we must
   write **after** it. 0x281F8A is *before* finalize in the iteration, so a raw +0x1C0 write there
   is vulnerable.
2. **The selector 0x205210 re-consults evade-source bytes at render time.** Even if +0x1C0
   survives to presentation, on a *natural avoid* the bytes `+0x1D0` (bit tests), `+0x1D2..+0x1D5`
   (weapon/shield/accessory evade sources) still carry the natural verdict, and the selector's
   0x205348..0x205363 / 0x2053EF branches steer the message back to the natural code. So a paint
   that contradicts the evade-source bytes gets partly overridden in the message path.

**Therefore kind-paint is fundamentally a RE-SKIN of a naturally-connecting attack UNLESS we
also author the evade-source inputs.** The clean, robust lever is **not** +0x1C0 at the OUTPUT
alone — it is the pair *(force the attack to connect naturally, then author both +0x1C0 AND the
evade-source bytes so the finalize + selector agree with us)*. This matches the proven
INPUT-CONTROL result (writing defender evade bytes on the live struct makes the VM honour them).

### What is co-writable and where
- **To author a MISS/PARRY/BLOCK on an attack that naturally HIT:** at/after finalize, set
  `+0x1C0 = kind` **and** set the matching evade-source byte so the selector agrees:
  - class-evade "Miss" (kind 4): needs the class-evade path; `+0x1D8 & 3 == 1` branch (0x20541C)
  - weapon-parry (kind 2) / shield-block (kind 3): set the corresponding `+0x1D2..+0x1D5`
    evade-source byte negative (the selector's `js` tests at 0x205348+ pick these)
  - plain miss (kind 6): `+0x1C0=6`; forecast `+0x1BE` must be non-zero (else selector skips to
    0x2053FA "no-forecast" path)
  Also ensure `+0x1C4` (staged damage) = 0 for a true miss/avoid so no number is emitted, and
  leave `+0x1E5` apply-flag consistent (avoid = don't apply HP).
- **To flip a natural AVOID into a HIT (or a different avoid):** you cannot do it by +0x1C0 at
  0x281F8A alone — the evade-source bytes and finalize will re-derive it. You must clear the
  evade-source bytes (`+0x1D0/1D2..1D5`) so the VM/finalize produce a hit, i.e. the **input**
  lever, exactly as the miss→hit enabler already proven. This is the "INPUT where OUTPUT can't"
  case in the output-control-first rule.

**Bottom line:** 0x281F8A is an authoritative **damage** output lever (LT4). For **outcome KIND**,
the compute point is *not* a clean standalone paint: the kind is re-derived downstream by the
finalize stamp (0x205B38) + the selector's evade-source reads. Authoring a clean miss/block/parry
requires co-writing +0x1C0 **with** the evade-source bytes (`+0x1D0`, `+0x1D2..+0x1D5`, `+0x1D8`)
and zeroing `+0x1C4`, and is only "free" on an attack that the VM lets connect. Flipping the
verdict direction (avoid↔hit) is an INPUT-side operation on the evade-source bytes.

---

## Proposed LT5 test (proc-free weapon, natural HIT expected)

Use a plain weapon with no reaction/proc. Target one unit. At the compute hook 0x281F8A for that
target (and, to bracket the finalize race, also re-assert immediately after the driver tail), write
per-case and observe the on-screen message + animation + HP:

| Case | +0x1C0 | evade-source co-write | +0x1C4 | +0x1E5 | Expected on-screen |
|---|---|---|---|---|---|
| A. authored **weapon-Parry** | 2 | set `+0x1D2` (weapon-evade src) = 0x80 (neg) | 0 | clear apply | "Parry" msg + parry anim, no HP loss |
| B. authored **shield-Block** | 3 | set `+0x1D4`/`+0x1D5` (shield-evade src) = 0x80 | 0 | clear apply | "Block/Guard" msg + block anim, no HP loss |
| C. authored **class-Miss** | 4 | set `+0x1D8` low2 bits = 1 | 0 | clear apply | "Miss" (class-evade) + dodge anim, no HP loss |
| D. authored **plain Miss** | 6 | ensure `+0x1BE` != 0 (forecast present) | 0 | clear apply | "Miss" + miss anim, no HP loss |
| E. control (paint kind only) | 2 | **no** evade-source write | 0 | — | tests whether paint-alone survives finalize+selector (expected: re-skins hit only; may revert) |

Pass criterion: A–D each show the authored message **and** matching animation **and** zero HP
change, proving co-write of (+0x1C0 + evade-source + zeroed +0x1C4) authors the outcome. Case E
is the diagnostic for whether +0x1C0-alone at 0x281F8A ever survives (predict: no / re-skin-only).
If A–D need the write moved to *after* finalize (0x205B38) to stick, that pins the authoritative
kind lever to the post-finalize point rather than 0x281F8A.
