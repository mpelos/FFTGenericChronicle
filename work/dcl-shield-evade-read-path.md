# Shield-evade read path — DEFINITIVE static RE (2026-07-03)

**Question that forced this trace:** LT5-A2 live FAIL — Agrias→Ramza showed **preview 50 %
hit + Ramza shield-PARRIED** even though we stamp the target's evade bytes to 0 at
`computeActionResult 0x309A44` (µs before the VM roll) *and* zero them in the three
equip/refresh copier tails. Meanwhile the 2026-06-27 proof showed writing **class +0x4B**
UP to 0x64 live DID make Ramza class-dodge. So class is honored by a live struct write;
shield +0x4A is not. Why?

**Verdict up front:** Shield evade is honored *differently* from class because the combat
code does **not treat the shield leg as a single struct byte read live at roll time.** Two
mechanisms defeat a late `+0x4A := 0` stamp:

1. The **combat-input record builders** (`0x284BC0`, `0x3600DC`, `0x3962F0`) copy the
   evade block into a **separate forecast/AI record** and, crucially, compute the shield
   field as **`MAX(+0x4A shield, +0x49 accessory)`** — a *derived* value in a *different
   struct* — whereas the **class field is a plain copy of +0x4B**. These builders run at
   action/forecast SETUP, **before** `0x309A44`. The preview number and (for the shield
   leg) the roll read that pre-built record, not our late `+0x4A` write.
2. The unit-struct `+0x4A` byte itself is written by exactly one real-code site — the
   equipment-refresh **Writer A** (`0x59F550`, derived from equipment source `[rdi+0x12]`)
   — and there is **no per-action re-derivation of +0x4A in real code**. So if the roll's
   shield leg is fed from the equipment item or from a mirror copy, our struct write is
   simply not the source it reads.

Bottom line: **there is no clean single-byte runtime lever for shield evade equivalent to
+0x4B for class.** Shield must be neutralized either (a) at the record-builder /
`MAX(+0x4A,+0x49)` site (hook + zero the packed shield field in the forecast record), or
(b) in DATA (zero the shield item's evade column). Class +0x4B remains a valid live lever.

---

## 1. Reader / snapshot map (byte-scan of real code, RVA < 0x610000)

Scripts: `work/disasm_shield_evade_reads.py`, `_classify.py`, `_snapshot.py`,
`_copiers.py`, `_rollpath.py`, `_writerA_callers.py`.

### 1a. The roll-path anchors do NOT read the evade bytes
Dumped `PRODUCER 0x30F0C4`, `ROLL 0x30FA34`, `GATHER 0x30FC30`, `COMPUTE-ENTRY 0x309A44`,
`SELECTOR 0x205210`. **None reads +0x46/47/48/49/4A/4B/4E.**
- `0x30FA34` is a bare `jmp 0x150cfb562` straight into the Denuvo VM (beyond 0x610000).
- `0x30FC30` reads only status flags `[rcx+0x61]/[+0x64]/[+0x65]/[+0x1B4]` (confirms prior note).
- The evade values therefore enter the roll from a **record built earlier**, not from a
  live struct read inside these functions.

### 1b. Combat-input record builders — THE asymmetry site
Three near-identical functions pack a unit's evade block into a **destination record**
`[dst+0x44..0x52]` (dst ≠ unit). Confirmed encodings:

| builder fn head | evade packing (src = unit, dst = record) |
|---|---|
| `0x284A80` (site 0x284BE8) | `dst+0x44 = src+0x4B` (class, **plain copy**) |
| `0x3600DC` (site 0x3602D2) | `dst+0x46 = MAX(src+0x4A shield, src+0x49 accessory)` |
| `0x3962F0` (site 0x396464) | `dst+0x50 = MAX(src+0x4D, src+0x4E shield)` |

Exact bytes at `0x3602D2` (representative):
```
0F B6 47 4B   movzx eax,[rdi+0x4B]      ; class
66 89 43 44   mov   [rbx+0x44],ax       ; -> record+0x44   (CLASS = direct copy of +0x4B)
0F B6 4F 49   movzx ecx,[rdi+0x49]      ; accessory evade
0F B6 47 4A   movzx eax,[rdi+0x4A]      ; shield evade
3A C8         cmp   cl,al
0F 47 C1      cmova eax,ecx             ; eax = MAX(shield,accessory)
0F B6 C0      movzx eax,al
66 89 43 46   mov   [rbx+0x46],ax       ; -> record+0x46   (SHIELD = MAX(+0x4A,+0x49))
...
0F B6 4F 4D   movzx ecx,[rdi+0x4D]
0F B6 47 4E   movzx eax,[rdi+0x4E]      ; shield block secondary
3A C8/0F47C1  eax = MAX(+0x4D,+0x4E)
66 89 43 50   mov   [rbx+0x50],ax       ; -> record+0x50
```
**Class = 1:1 copy of the live byte. Shield = a computed MAX over two bytes, landed in a
different struct.** This is exactly why zeroing the live `+0x4A` alone does not force a hit:
the preview/roll consult the packed `record+0x46`, and even if we did zero `+0x4A` in time,
`+0x49` (accessory) can keep the packed shield field non-zero. Callers:
`0x284A80` ← 0x2850DE/0x285AB3/0x287102/0x28B8B6/0x2BDB2B;
`0x3962F0` ← **0x39675D (inside Writer-B-twin `0x3965B0`, i.e. equip-refresh)** + 0x33E1F8.

### 1c. AI-eval snapshot (both bytes copied, informational)
`0x258ED0` and `0x25A70C` copy `unit+0x4A → obj+0x9C` and `unit+0x4B → obj+0x9D` into a
~0xA0-byte AI-evaluation object (float-normalized stats at obj+0x74..0x80). Both copied
symmetrically here; this feeds AI scoring, not the miss/parry decision.

### 1d. Debug/telemetry dumps (not roll-relevant)
`0x226EBC` (all-7 bytes → rip globals 0x55c3xx) and `0x30302A` (bulk `movsd/movups` of the
whole struct → rip globals 0x749d3xx) are wholesale unit→global snapshots used by
UI/debug panels. Not the roll input.

### 1e. Live evade "presence" gate (reads BOTH live, symmetric)
`0x2B86A1`, `0x134EC8` implement the canonical predicate
`cmp [u+0x4A],0 / jne / cmp [u+0x4B],0 / je no-evade` — "does the unit have any evade at
all". Reads shield and class **live and symmetrically**; it's a gate, not the decision, so
it doesn't explain the split (and would honor a live zero — but the record in §1b is what
picks the outcome).

---

## 2. Class vs shield — the structural difference (reconciled)

| | Class evade `+0x4B` | Shield evade `+0x4A/+0x4E` |
|---|---|---|
| Struct source | JOB → written once by Writer A `[rdi+0x13]→+0x4B` | EQUIPMENT → Writer A `[rdi+0x12]→+0x4A`, `[rdi+0x16]→+0x4E` |
| In the forecast record | **direct 1:1 copy** `record+0x44 = +0x4B` | **derived** `record+0x46 = MAX(+0x4A,+0x49)`, `record+0x50 = MAX(+0x4D,+0x4E)` |
| Honored by a LIVE struct write? | **YES** (proven 2026-06-27: +0x4B↑ → class dodge) | **NO** (LT5-A2: +0x4A=0x32 survived to preview + parry) |
| Why | nothing between the byte and the leg but a copy; the VM leg reads the byte/record 1:1 | the shield leg reads a *computed, pre-snapshotted* value in a *separate record*; a late +0x4A write neither reaches the record (built earlier) nor overrides the +0x49 term |

Writer A (`0x59F550`, sole `[equip+0x12]→[+0x4A]` copier in the binary; one caller
`0x59CACD` = equip-refresh) writes BOTH class and shield from the same equipment block, so
the split is **not** in the struct-write path — it is in the **read/pack path** (§1b): class
is copied verbatim, shield is fused with accessory into a derived record field.

---

## 3. DEFINITIVE verdict — is there a runtime lever for shield?

**No single-byte runtime lever equivalent to +0x4B exists for shield.** A late `+0x4A := 0`
stamp is ignored because the shield leg is fed by a pre-built, *derived* record field, not
by the live `+0x4A` byte. To make a shield-evade zero (or force) honored at runtime you must
neutralize the **derived record field**, not the struct byte:

**Runtime lever (if a hook is wanted):** hook the record builders and force the packed shield
field to 0 (or to your value) AFTER the `MAX`:
- `0x284BFD` — after `movzx eax,al` / before `mov [rbx+0x46],ax` (accessory record path)
- `0x3602E7` — after `movzx eax,al` / before `mov [rbx+0x46],ax` (main combat record)
- `0x396479` — after `movzx eax,al` / before `mov [rdi+0x46],ax` (equip-refresh record)
  plus the `+0x50` twin stores at `0x360310/0x3964A2/0x284C25`.
Writing 0 to `record+0x46` and `record+0x50` there kills the shield leg race-free. (Also
zero the source **accessory** byte `+0x49` if you stamp `+0x4A`, since the field is a MAX.)

**Simpler, race-free, recommended: handle shield in DATA.** Because the entire chain traces
back to the equipment item's evade column (Writer A `[equip+0x12]/[equip+0x16]`), zeroing the
**shield's evade columns in `item_catalog.csv`** removes the shield leg at the source — no
hook, no record math, no VM-timing race. This matches the doc-08 fallback and is the clean
answer for a "no ultra-hard, custom-formula" mod that doesn't want shield auto-block anyway.

**Class stays a runtime lever:** `+0x4B` is copied 1:1 and read live — keep using it for
class-evade control (force/zero) at the struct.

---

## 4. Artifacts
- `work/disasm_shield_evade_reads.py` — all evade-byte reads (857 raw; noise-filtered by fn).
- `work/disasm_shield_evade_classify.py` — reads grouped by enclosing fn (shield vs class).
- `work/disasm_shield_evade_snapshot.py` — evade-read→foreign-store copy sites (53).
- `work/disasm_shield_evade_copiers.py` / `_rollpath.py` / `_writerA_callers.py` — windows.
- Key sites: builders `0x284BC0/0x3600DC/0x396440` (class=copy, shield=MAX), Writer A
  `0x59F550` (equip→+0x4A/+0x4B), roll anchors read no evade bytes.
