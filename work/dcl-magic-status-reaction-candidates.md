# DCL offline RE — Magic-Evade / Status-Infliction / Reaction-Dispatch candidates

Static analysis only (FFT_enhanced.exe on disk, image base `0x140000000`, no ASLR, `.xcode`
RVA `0x1000..0x610000`). Calibration sites verified byte-exact: `0x30A66F 0F BF 45 06`,
`0x30637E 66 41 89 50 06`, `0x227FFE 41 BA 02 00 00 00`. Tooling: pefile 2024.8.26 + capstone
5.0.7 (scripts in scratchpad, not committed).

## Shared infrastructure (used by all three goals)

- **The staged-result object** is reached through three aliased pointer globals that all hold the
  same value: `0x14186AF70`, `0x14186AF78` (and `0x142FF3CF8`, `0x14186AF60` per doc 04/05). The
  object **is `target_unit + 0x1BE`** (already proven in doc 11). Field map used by the code below:
  `obj+0 = +0x1BE` present-flag; `obj+2 = +0x1C0` evade/result-kind byte; `obj+6 = +0x1C4` HP-debit;
  `obj+0xA = +0x1C8` MP-debit; `obj+0x10/0x12 = +0x1CE/0x1D0` status masks; `obj+0x28 = +0x1E6`;
  `obj+0x2C = +0x1EA` hit-% source; `obj+0x27 = +0x1E5` resultKind bits.
- **The one native RNG primitive** is `roll @ 0x278EE0` (`jmp` into `.edata` → thin VM stub, but
  the *call sites* are all real code). Signature `roll(ecx = range, edx = chance) -> eax` (nonzero
  = pass). It has **11 call sites in `.xcode`**, and they partition cleanly by goal:
  `0x304E33` (magic/Faith), `0x306636 0x3083AB 0x30946B` (status/self-status), `0x271D88 0x27223D
  0x27CA8C` (targeting/AoE), `0x30BE86 0x30BEDC 0x30BF32 0x30BF72` (reaction Brave-gates).
- A parallel non-`roll` accuracy gate `0x304DF0` has 2 callers (`0x307B32`, `0x307C06`).
- Per-formula staged-calc "captured input" byte/word globals live at `0x7B0760..0x7B07BC`
  (e.g. `g_7B079D` Faith snapshot, `g_7B07AC` status-chance snapshot, `g_7B0770/72` XA/hit words,
  `g_7B0776` staged reaction/effect id). These are the scratch the finalizers read.

---

## GOAL 1 — MAGIC EVADE / Faith accuracy

### Site A (PRIMARY): magic hit-% Faith-scale + fizzle roll — `0x304DF0` fn, roll at `0x304E33`

```
0x304DF4  mov  r11, [rip+0x1566175]     ; r11 = *(0x14186AF70) = target_unit+0x1BE (result obj)
0x304DFB  mov  eax, 0x51EB851F          ; reciprocal-of-100 constant (÷100 fingerprint)
0x304E00  movzx r8d, byte [0x7B079D]    ; r8d = g_7B079D  (target Faith snapshot, 0..100)
0x304E08  movsx ecx, word [r11+0x2C]    ; ecx = staged hit%  (obj+0x2C = unit+0x1EA)
0x304E0D  imul ecx, r8d                 ; hit% * Faith
0x304E11  imul ecx ; sar edx,5 ; …      ; edx = (hit% * Faith) / 100
0x304E1D  mov  word [r11+0x2C], dx      ; write scaled hit% back  <-- Faith modifies magic hit%
0x304E22  cmp  dword [0x186AF80], 0     ; test/debug-mode gate
0x304E2B  mov  edx, r8d                 ; edx = Faith-scaled chance
0x304E2E  mov  ecx, 0x64                ; range = 100
0x304E33  call 0x278EE0                 ; roll(100, chance)
0x304E38  test eax,eax / je 0x304E4C
0x304E3C  mov  byte [r11], 0            ; obj+0 = 0  (no damage staged)
0x304E45  mov  byte [r11+2], 6          ; obj+2 = +0x1C0 = 0x06  = "plain miss" evade-type
```

- **Mechanism (high confidence):** this is the classic FFT magic-accuracy path. It takes the
  computed spell hit-% (`obj+0x2C`), multiplies by the target-Faith snapshot `g_7B079D`, truncating
  `/100` (the `MA·Q·Faith/100` fingerprint from doc 05 §5), writes it back, then rolls. On the
  miss branch it stamps the **known evade-type `0x06` ("plain miss")** at `+0x1C0` — matching the
  live-observed enum where failed-accuracy rolls (Steal/status) route through `0x06`. This is the
  reason zeroing the *physical* evade bytes (`+0x46/47/4A/4B/4E`) never makes Fire always-hit: magic
  never consults them; it consults Faith and this roll instead.
- **Struct offsets read:** target `unit+0x2D` Faith is the ultimate source of `g_7B079D` (captured
  upstream into the scratch global; see Site B for the live per-unit capture). No physical-evade
  byte is read here.
- **Writable always-hit / always-miss levers (candidates):**
  - Always-hit (data, upstream): raise **caster & target Faith** — but `g_7B079D` is a *snapshot*,
    so the honest lever is the snapshot global or the `obj+0x2C` write.
  - Always-hit (code hook): hook **`0x304E2B` `mov edx, r8d`** (`ExecuteFirst`) and force `edx = 100`
    before the roll → `roll(100,100)` always passes → never takes the fizzle branch. Clean 3-byte
    site, no internal jump target. Bytes at `0x304E2B`: `41 8B D0`.
  - Always-miss: force `edx = 0`.
  - Alternatively neutralize the Faith scale by hooking `0x304E1D` (`mov [r11+0x2C], dx`) to keep
    the pre-Faith hit-%.
- **Does it also handle status-only spells?** No — this site is HP/damage-spell accuracy. Status
  infliction is a *separate* roll (Goal 2, `0x3065F0`). A status-carrying attack spell would pass
  through here for the hit and through Goal-2 code for the ailment.
- **Confidence: HIGH** (the `÷100` constant, Faith read, hit-% obj field, and the `0x06` miss-stamp
  all corroborate).

### Site B: per-unit Faith/element snapshot into the scratch globals — `0x3062EC` fn

```
0x3062EC  mov r8, [0x14186AF78]         ; r8 = result obj (target)
0x3062F8  mov dl, byte [r8+0x65]        ; +0x65 effective-status/element byte
… builds element/half/absorb factors, then:
0x306365  movsx eax, word [r8+6]        ; obj+6 = +0x1C4 staged damage
0x30636E  imul … ÷ (0x68DB8BAD, sar 0xC = ÷1000-ish)   ; elemental scale
0x30637E  mov word [r8+6], dx           ; write scaled damage back  (== calibration site 0x30637E)
```

- Confirms `unit+0x65` is the effective status/element byte consulted during resolution; the magic
  path scales *damage* by element here (weak/half/absorb, doc 05 §5). Not the accuracy site, but the
  neighbor that made `0x30637E` a known damage-finalizer. Confidence: MEDIUM-HIGH.

### LIVE-TEST PLAN — Goal 1 (planned only)
1. Baseline: cast Fire on a high-Faith target, log `obj+0x2C` before/after `0x304E1D` and the roll
   result — confirm `obj+0x2C` is multiplied by target Faith /100.
2. Install a log-only hook at `0x304E33` capturing `ecx/edx` and `eax` for physical vs magic actions
   → verify only spells (and Faith-rolls) reach it, physical attacks do not.
3. Force-hit test: `ExecuteFirst` hook `0x304E2B`, set `edx=100`; cast Fire at a target with evade —
   expect 100% connect regardless of Faith. Force-miss with `edx=0`.
4. Cross-check the forecast %: because `obj+0x2C`(=`+0x1EA`) is the hit-% source (doc 10/11), confirm
   the displayed magic % tracks the Faith scaling.

---

## GOAL 2 — STATUS INFLICTION

### Site A (PRIMARY): status-proc roll — `0x3065F0` fn, roll at `0x306636`

```
0x3065F0  push rbx / sub rsp,0x20
0x3065F6  mov  rax, [0x14186AF78]        ; rax = result obj (target)
0x3065FD  cmp  byte [rax+3], 0x5D        ; +0x1C1 guard (0x5D = the Dragon/no-formula marker)
0x306601  je   done
0x306603  mov  r11, [0x14186AF68]        ; r11 = a second staging obj
0x30660A  mov  ebx, 8
0x30660F  mov  byte [r11], 1             ; obj present
0x306613  movzx eax, word [rax+0x1A8]    ; +0x1A8 = status id/mask field on the target
0x30661A  mov  word [r11+4], ax          ; stage the status
0x30661F  mov  byte [r11+2], bl          ; obj+2 = +0x1C0 result-kind = 8 (STATUS)
0x306623  cmp  dword [0x186AF80], 0      ; test-mode gate
0x30662C  movzx edx, byte [0x7B07AC]     ; edx = g_7B07AC = staged STATUS-chance %
0x306633  lea  ecx, [rbx+0x5C]           ; ecx = 8+0x5C = 0x64 = 100  (range)
0x306636  call 0x278EE0                  ; roll(100, statusChance)
0x30663B  test eax,eax / je fail
0x30663F  mov  word [r11+0x12], bx       ; PASS: +0x1D0 status-apply mask |= 8
0x306646  mov  word [r11+0x12], 0x1000   ; FAIL: mask = 0x1000 (no-apply sentinel)
```

- **Mechanism (high confidence):** this is the status-infliction proc. `roll(100, g_7B07AC)` where
  `g_7B07AC` is the staged **status hit-%**; on pass it sets the status-apply word (`obj+0x12` =
  `unit+0x1D0`), on fail a `0x1000` sentinel. Result-kind byte `+0x1C0 = 8` matches doc 04's
  `+0x1E5 bit 0x08 = status`. The `+0x1A8` field carries the status id/mask being applied.
- **Callers of `0x3065F0` (4):** `0x307DE8 / 0x307E1C / 0x307E80 / 0x307E94` — the per-formula
  finalizers (e.g. the block that also writes `[rip+…]` MA/PA snapshots), i.e. status-carrying
  formulas call this after computing damage. Two sibling status paths that ALSO roll directly:
  `0x3083AB` (reads target `+0x2B` Brave; a Brave-scaled status) and `0x30946B` (self/`0x1C2`-tag
  status). This matches the FFT `statusHit% = X + …` family.
- **Struct offsets read/written:** reads target `+0x1A8` (status id), `+0x1C1` guard (`0x5D`); writes
  staged `+0x1C0 = 8` (kind), `+0x1D0` (apply mask). Chance source is the scratch global `g_7B07AC`
  (captured from the formula's Y/hit%, not a raw struct byte at this instruction).
- **Force-proc / force-fail levers (candidates):**
  - Always-proc (code): `ExecuteFirst` hook **`0x30662C`** (`movzx edx, byte [0x7B07AC]`, bytes
    `0F B6 15 79 A1 4A 00`) and set `edx = 100` after the load; or hook `0x306633`/`0x306636` region.
    Simpler: overwrite the snapshot global `0x1407B07AC` to 100 each poll (data-only, no hook).
  - Never-proc: force `edx = 0` (or global = 0).
  - Direct result-forge: force the PASS store `0x30663F` (`word[r11+0x12] |= 8`).
- **Cross-ref PSX decomp:** structurally matches `BATTLE_*` status routines (compute a chance, one
  `Random`/`roll`, branch to an apply-mask write) — the IVC shape is a single `roll(100, chance)`
  with the ailment id staged at `+0x1A8` and the apply mask at `+0x1D0`.
- **Confidence: HIGH.**

### Site B: status master recompute (already known) — `0x30D42A`
Confirms the durable→effective status recompute `+0x61 = (+0x1EF & 0xF2) | +0x57` at `0x30D42A/33/3C`
(doc 04 §2.3), and the full status-clear block `0x30A8F9..0x30A918` (`+0x57,+0x5B,+0x5C,+0x60,+0x61,
+0x65,+0x66-0x75,+0x1EF,+0x1F3` all zeroed) — the "cure everything" footprint. Confidence: HIGH.

### LIVE-TEST PLAN — Goal 2 (planned only)
1. Log-only hook `0x306636`: capture `edx` (chance) and `eax` (result) plus target `+0x1A8` while
   using a status move (e.g. an add-Poison attack) vs a plain attack → confirm only status actions
   reach it and `+0x1A8` names the ailment.
2. Data poke test: write `0x1407B07AC = 100` on the poll and use a low-odds status move → expect
   100% infliction (proves the chance snapshot is the lever, no code hook).
3. Result-forge test: `ExecuteFirst` `0x30663F` to always set the apply mask; verify the ailment
   applies and renders.
4. Map the id: vary the status move and read `+0x1A8`/`+0x1D0` to build the ailment-id table.

---

## GOAL 3 — REACTION DISPATCH

### Site A (PRIMARY): reaction dispatcher — big fn at `0x30B584` (called from `0x229CFA`, `0x2E0920`)

The dispatcher reads a **reaction/support/movement flag bitfield at `unit+0x94..0x97`** and maps each
set bit to a reaction message/effect id (the `0x1A6..0x1BE` family), then gates it on a Brave roll.

```
0x30B895  mov  dl, byte [rcx+0x94]       ; rcx = defender; +0x94 reaction bitfield #1
… tests dl bits, each -> a distinct id in eax (0x1A9 Blade-Grasp-ish, 0x1AA, 0x1AB …):
0x30B8A0  cmp  byte [g_7B0776], 0x15      ; staged action-effect id vs 0x15 (gate)
0x30B8AD  test byte [g_7B079A], r13b      ; capability gate
0x30B94C  mov  al, byte [rcx+0x95]       ; +0x95 bitfield #2  -> ids 0x1AE,0x1B4,0x1B5,0x1B6…
0x30B9CD  mov  r8b, byte [rcx+0x96]      ; +0x96 bitfield #3  -> ids 0x1B3,0x1BA…
0x30BA19  test al, 4                      ; +0x94 bit tests continue (Arrow-Guard/Catch class)
0x30BA46  … word [rbx+6]/[rbx+0xA]…       ; some reactions edit staged dmg/heal (rbx = result obj)
0x30BAB1  cmp  byte [rcx+0x97], dil       ; +0x97 bitfield #4 (dl-signed reactions)
```

Then the trigger roll:

```
0x30BBD8  call 0x30B30C                   ; "is-reactable" #1 (reads +0x97 bit 0x40, +0x63, +0x94..)
0x30BBDD  call 0x30B410                   ; "is-reactable" #2 (reads +0x97 bit 0x10, table walk)
```

### Site B (PRIMARY): the Brave-gate cluster — 4 fns, all `roll(100, defender Brave +0x2B)`

```
0x30BE54 fn  (caller 0x30ABC0-ish path)   \
0x30BEAC fn  called from 0x30ABC0  ecx=id  |  each does:
0x30BEFC fn  called from 0x30AB97  ecx=id  |    movzx edx, byte [rax+0x2B]   ; defender Brave
0x30BF48 fn                                /     mov ecx, 0x64 ; call 0x278EE0 (roll 100,Brave)
```
e.g. `0x30BE7D mov ecx,0x64 / 0x30BE82 movzx edx,[rax+0x2B] / 0x30BE86 call 0x278EE0`. On pass the
fn writes the reaction id to `word[obj+0x10]` and effect to `word[obj+0x28]`. This is the
**canonical FFT Brave%-gate** and confirms doc 05's "write defender Brave `+0x2B` before the roll to
suppress reactions" — the Brave read is right here at `+0x2B`, four times.

### Reaction author (post-hit) — `0x30AA80..0x30AC01`
The block that, after a hit resolves, walks the defender's `+0x94/0x95/0x96/0x97` bits and calls the
Brave gates `0x30BEAC`/`0x30BEFC` with the matched id in `ecx`. `+0x94` bits → ids `0x1A6,0x1A7,
0x1A8,0x1AC`; `+0x95` → `0x1AF,0x1B0,0x1B1,0x1B2`; `+0x96` → `0x1B7,0x1B9`. Also note the
**Chicken/Brave floor** at `0x30A9BD cmp byte [rdi+0x2B],0x0A` (doc 05 warning: never write Brave<10).

### Skillset → reaction resolution is virtualized
The id→skill lookups go through `0x2BB0D4` (and `0x2B8CB8`, `0x2B8CE8`) which `jmp` into `.edata`
(VM). So the reaction *table* resolve is Denuvo-virtualized — but the **bitfield read (`+0x94..0x97`),
the id mapping, and the Brave-gate roll are all real code** and hookable. There is no single
"reaction id/type" struct byte; the reaction *set* is the `+0x94..0x97` bitfield, and the *trigger*
is the Brave gate.

- **Struct offsets:** `+0x94,+0x95,+0x96,+0x97` = reaction/support/move ability bitfields (4 bytes);
  `+0x2B` = Brave (gate); `+0x63` bit tests = a status/condition gate; result obj `+0x10/+0x28`
  receive the fired reaction id/effect.
- **Levers (candidates):**
  - Suppress a reaction: zero the matching bit in `+0x94..0x97` on the defender (data), OR write
    Brave `+0x2B` to 10 (floor) so the gate roll fails (already proven live).
  - Force a reaction: set the bit AND raise Brave (gate passes at high odds).
  - Log/redirect at the gate: `ExecuteFirst` hook `0x30BE86` (`E8 55 D0 F6 FF`) etc., capture/replace
    `edx`.
- **Confidence: HIGH** for the bitfield location and Brave-gate identity; MEDIUM for the exact
  bit→reaction-name mapping (needs the live id table).

### LIVE-TEST PLAN — Goal 3 (planned only)
1. Dump `defender+0x94..0x97` for units with known reactions (Blade Grasp, Arrow Guard, Auto-Potion,
   Counter) → build the bit→reaction map.
2. Log-only hook `0x30BE86`/`0x30BEDC`/`0x30BF32`/`0x30BF72`: capture `edx`(Brave), `eax`(result),
   and the id in the enclosing fn → confirm which gate fires for which reaction.
3. Suppress test: clear the Blade-Grasp bit in `+0x94..0x97` before the hit → reaction should not
   fire even at high Brave (isolates bitfield from Brave lever).
4. Force test: set the bit + Brave 90 → reaction fires; cross-check the fired id at result
   `obj+0x10`.

---

## Hook-candidate byte signatures (for re-location after a patch)

| Purpose | RVA | bytes |
| --- | --- | --- |
| Magic Faith roll fn head | `0x304DF0` | `48 83 EC 28 4C 8B 1D 75 61 56 01 B8 1F 85 EB 51` (unique) |
| Magic always-hit hook | `0x304E2B` | `41 8B D0` (set edx=100 ExecuteFirst) |
| Status roll fn head | `0x3065F0` | `40 53 48 83 EC 20 48 8B 05 7B 49 56 01 80 78 03 5D` (unique) |
| Status chance snapshot load | `0x30662C` | `0F B6 15 79 A1 4A 00` |
| Element/status snapshot fn | `0x3062EC` | `4C 8B 05 85 4C 56 01 45 33 C9 B0 64` (unique) |
| Reaction dispatcher head | `0x30B584` | `48 8B C4 48 89 58 08 48 89 70 10 48` (prologue; not unique — verify by +0x94 reads downstream) |
| Reaction Brave-gate fn (dmg) | `0x30BEAC` | `40 53 48 83 EC 20 4C 8B 1D B7 F0 55 01 0F B7 D9 41 80 7B 27` |
| Brave-gate roll | `0x30BE86` | `E8 55 D0 F6 FF` |

Sig-uniqueness of the fn heads was verified by full-`.xcode` scan (1 match each). The shared `roll`
target `0x278EE0` and staging pointers `0x14186AF70/78/68` are the reusable anchors.
