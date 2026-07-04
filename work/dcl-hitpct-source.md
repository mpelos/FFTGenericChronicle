# DCL — Where is the final hit% stored before the Denuvo VM roll?

Static disassembly only (no live tests). Binary `FFT_enhanced.exe`, image base
0x140000000, real code RVA 0x1000..0x610000, `.xcode`/`.debug$P` above = Denuvo VM.
Shared RNG trampoline **0x278EE0**, called `roll(range=ecx=100, chance=edx)`.

Scripts (all in `work/`): `disasm_hitpct_source.py`, `disasm_hitpct_2b2c.py`,
`disasm_hitpct_staging.py`, `disasm_hitpct_lever.py`, `disasm_hitpct_writers.py`,
`disasm_hitpct_phys.py`.

---

## 1. unit+0x1EA is the DISPLAY field, not the roll input

Byte-scan of the whole real-code range for word/byte accesses to displacement
`+0x1EA`:

- **WRITES to +0x1EA: 0** (none in real code).
- **READS of +0x1EA: 1**, at `0x3B122F`:
  ```
  003B122F: 0F B6 83 EA 01 00 00  movzx eax, byte ptr [rbx + 0x1ea]
  003B1236: 89 87 F8 00 00 00     mov   dword ptr [rdi + 0xf8], eax
  ```
  This is a **struct-copy / UI-forecast builder** (it copies +0x1D0.. +0x1EA from the
  source unit `rbx` into a display object `rdi` at +0xF8, +0x1F8, +0x208, ...). It
  READS +0x1EA to paint the panel. Nothing computes into +0x1EA in real code.

**Reconciliation with prior RE ("obj+0x2C aliases +0x1BE"):** the forecast object base
is unit+0x1BE, so obj+0x2C == unit+0x1BE+0x2C == **unit+0x1EA**. So +0x1EA and
"obj+0x2C" are the *same field* — the displayed hit%. It is written only through the
staging-object path below (obj+0x2C stores), which then feeds the panel. It is **not**
read by any RNG roll.

Verdict for (1): **unit+0x1EA holds the displayed number only. The VM roll does not
read it.** Writing +0x1EA changes at most the panel (and only if the panel is
re-dirtied), never the actual hit chance.

---

## 2. The formula module and the real chance sources

Dispatch table **0x140682BC8**: 170 slots inspected, **134 point into real code**
(0x3071B4.. 0x31ABxx range — the formula-handler module), a handful of `0x103E0`
padding stubs, 4 trampolines (slots 150-153 → `.debug$P` 0x7B09xx), and non-pointer
data rows (masks/constants) interleaved. So the handlers themselves are REAL code; the
**roll** is the only part that vanishes into the VM.

There are **11 real-code call sites of RNG 0x278EE0**. Their `chance` (edx) operand:

| Site       | Path (formula class)        | chance = edx source                              |
|------------|-----------------------------|--------------------------------------------------|
| 0x304E33   | **Magic accuracy** (fn 0x304DF0) | `r8d = byte [0x1407B079D]` (global Faith snapshot) |
| 0x306636   | **Blind / status**          | `byte [0x1407B07AC]` (VM scratch global)         |
| 0x3083AB   | **Physical attack**         | `byte [src+0x2B]` (src = qword[0x14186AF68])     |
| 0x30946B   | physical variant            | `byte [r8+0x2B]`                                  |
| 0x30BE86/DC/F32/F72 | reaction / counter | `byte [unit+0x2B]`                               |
| 0x271D88, 0x27223D, 0x27CA8C | non-combat (movement/table) | local-computed / table lookup            |

So the "always-hit lever" hooks from LT3 (0x304E2B etc.) only ever sat on the **magic**
path (0x304DF0) — which is exactly why they never fired for Fire/Blind: **Fire and Blind
route through 0x306608, not 0x304DF0.** Confirmed by the table.

### fn 0x304DF0 (magic) — what it actually does
```
304E00  movzx r8d, byte [0x1407B079D]     ; r8d = Faith global (byte)
304E08  movsx ecx, word [r11+0x2c]        ; ecx = staged accuracy (staging obj +0x2C)
304E0D  imul  ecx, r8d                     ; acc * Faith
304E11..1B  ... (×0x51EB851F, sar5) => /100 ; ecx = acc*Faith/100
304E1D  mov   word [r11+0x2c], dx          ; store the reduced % back to staging+0x2C  (DISPLAY)
304E2B  mov   edx, r8d                      ; edx = Faith byte           <-- roll input
304E2E  mov   ecx, 0x64
304E33  call  0x278EE0                      ; roll(100, Faith)
```
Note the twist: it computes `acc*Faith/100` and writes that to the **staging/display**
(+0x2C), but the **roll input `edx` is the raw Faith byte**, not the computed accuracy.
The staging +0x2C write is a mirror for the panel; the VM roll consumes Faith.

### Physical 0x3083AB
```
308388  movzx eax, byte [rbx+0x2b]          ; rbx = qword[0x14186AF68] staging/context
30838C  mov ecx,100 ; 308393 sub dx,ax      ; dx = 100 - [rbx+2B]
308396  mov word [r11+0x2c], dx             ; staged/display %  (DISPLAY)
3083A7  movzx edx, byte [rbx+0x2b]          ; edx = RAW [rbx+2B]           <-- roll input
3083AB  call 0x278EE0                        ; roll(100, [rbx+2B])
```
Same architecture: compute `100-x` for the panel, roll against the raw byte.

---

## 3. Are the chance sources writable pre-roll from real code?

Definitive writer scan (`disasm_hitpct_writers.py`, store opcodes 88/C6/89/C7/… with
rip-relative target resolution):

- **0x1407B079D (Faith / magic chance): 0 real-code writers**, 47 real-code readers.
- **0x1407B07AC (Blind/status chance): 0 real-code writers**, 25 real-code readers.

Both globals live in the **`.debug$P` VM-data region**. Real code only ever *reads*
them; **the value is deposited by the Denuvo VM before the roll.** For the physical/
reaction paths the chance is `byte [staging+0x2B]`, where `staging` is a VM-owned pointer
(`qword [0x14186AF6x]`, also `.debug$P`), and no real-code path computes an accuracy into
that +0x2B byte — the surrounding real code only *reads* +0x2B and writes the *display*
+0x2C.

The staging objects are at fixed globals **0x14186AF60 / AF68 / AF70 / AF78** (the same
LT3 "pending action" cluster near 0x14186AFF0). Their +0x2C word is the display hit%
(== unit+0x1EA mirror); their +0x2B byte is the physical roll input. All four are plain
`.debug$P` pointer globals loaded as `qword [rip+disp]`; the magic path uses AF70, the
physical path uses AF68.

**Important nuance — staging+0x2C IS read as an accuracy input (magic path):** at
`0x304E08` the magic handler does `movsx ecx, word [r11+0x2c]` and multiplies it by
Faith. So `staging+0x2C` (word) is *both* the input accuracy for the magic reduction
*and* the displayed hit% (it is overwritten with the reduced value at 0x304E1D). That
makes **staging+0x2C a genuine pre-roll input** for magic — but the roll itself still
consumes the raw Faith byte, so writing +0x2C alone changes the multiplicand, not the
final pass/fail threshold. The clean single-byte % lever remains the per-path chance
byte in section 3.

---

## VERDICT

**Arbitrary custom hit% CANNOT be achieved by writing the displayed field
(unit+0x1EA / obj+0x2C).** That field is display-only: 0 real-code writers, its sole
reader is the UI-forecast copy at 0x3B122F, and no RNG site reads it.

**The value the VM actually rolls against is a per-formula "chance" byte that real code
does NOT write:**
- magic → Faith byte `0x1407B079D`
- blind/status → scratch byte `0x1407B07AC`
- physical / reaction → `byte [staging+0x2B]`, staging = `qword[0x14186AF68/…]`

All of these are **written by the Denuvo VM**, immediately before `call 0x278EE0`, with
**no real-code store instruction** anywhere in 0x1000..0x610000. There is therefore **no
static real-code lever** that holds the final computed hit% in a way we can overwrite by
patching a mov, and no clean "compute-accuracy-and-store" function whose output we can
redirect (the one candidate, fn 0x304DF0, stores its computed number only to the
*display* +0x2C and rolls against raw Faith instead).

**Consequence — arbitrary custom hit% must be done by INPUT-CONTROL on the VM's chance
byte, or by OUTPUT-CONTROL of the roll result:**

1. **INPUT lever (viable, live-only):** the chance bytes ARE concrete writable memory —
   `0x1407B07AC` (status/blind), `0x1407B079D` (magic/Faith), and `[staging+0x2B]`
   (physical). They are populated by the VM each roll, so a static patch is impossible,
   but a **live write timed just before the roll** (the same technique that proved
   evade-byte input control) can set them to our formula's value. This matches the LT3
   note that poking `g_7B07AC` affected the status path. This is the analogue of the
   proven evade input-control: *the VM virtualizes code, not data*, so overwriting the
   data byte it is about to read forces our chance. **Field to write: the per-path
   chance byte above, selected by formula class, in the narrow window after the handler
   runs and before 0x278EE0.**

2. **OUTPUT lever (also viable):** hook/patch is impossible inside the VM, but the roll's
   *result* is staged into the staging object right after return (`[r11]` = hit/miss
   byte, `[r11+2]` = evade-type, `[r11+0x12]` = flags). Rewriting that staged result is
   the output-control path already used for damage.

**Recommended:** for arbitrary %, use **INPUT-CONTROL on the chance byte** (write
`0x1407B07AC` for status, `0x1407B079D` for magic, `[qword 0x14186AF68 + 0x2B]` for
physical) at compute time — it is the direct % lever and needs no post-hoc pass/fail
inversion. Fall back to **output-control** only where the chance byte can't be timed.
There is **no purely-static (offline patch) route** to arbitrary hit%, because the final
number is never stored by real code — only read from VM-written memory.
