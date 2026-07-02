# DCL AI-anchor candidates — the shared calc entry, statically mapped (2026-07-02)

Offline static pass only (pefile+capstone over `FFT_enhanced.exe` on disk, image base
`0x140000000`, real code RVA `0x1000..0x610000`; **no live game interaction**). Feeds doc 08 §7
"AI scoring view" (GOAL A of `work/dcl-ai-and-progression-notes.md`). Confidence labels:
**Proven** (live) / **Strong** (static, byte-verified) / **Hypothesis**.

Script archived as `work/disasm_ai_anchor.py` (+ `work/disasm_ai_anchor.out.txt` key output).

TL;DR:
1. The refuted "roll-verdict" site is the **battle phase/turn engine** (fn `0x30F0C4`), not AI
   scoring — its bursts are the CT-tick scheduler loop. Useful only as an enemy-turn-onset marker.
2. There is exactly **one real-code compute-action-result path**: sweep driver `0x281D60` →
   per-target calc `0x309A44` → **static formula dispatch table at VA `0x140682BC8`** (`.rodata`,
   formula id → 162 real-code handlers). Every known finalizer/status/accuracy site lives under it.
3. `0x281D60` has **zero real-code callers** — the forecast driver, the executor, and (if same-calc
   holds) the AI evaluator all enter from the Denuvo VM. Statically unresolvable further; ONE
   log-only hook at `0x309A44` answers the AI question in a single enemy turn.
4. No team/controller check exists anywhere in the calc path — the AI discriminator must be derived
   at hook time from the caster's team byte (`unit+0x04`) + the turn-owner global.

---

## 1. The roll-verdict site, recontextualized: fn `0x30F0C4` is the phase/turn engine (Strong)

`work/roll-verdict-override.md` refuted `0x30F4A7` live ("per-unit turn/CT/AI evaluation,
~8–11 fires per unit per CT tick"). Full disasm of the enclosing function settles what it is.

**Fn `0x30F0C4(ecx = mode)`** is a state machine over the battle **phase dword
`[0x14186B044]`** (read at `0x30F1BA`; same global doc 05 §3 already names for the category
producer). Nearly every phase case is a loop over the **unit array `0x141853CE0`, stride
`0x200`, 21 slots** (`r15 = 0x200` throughout):

| Phase case | Block | What the loop does (unit fields touched) |
| --- | --- | --- |
| init (`ecx==1`) | `0x30F0FD` | zero phase block, set every unit `+0x41 = 0`, `+0x18D = 0xFF`, emit `0xE000` |
| 6 | `0x30F218` | scan a 21-byte side table `0x1437A5BE0` for a nonzero slot |
| 5 | `0x30F242` | refresh that table from unit `+0x184` (gated on `+0x0` != 0xFF and `+0x0` bit 4) |
| 4 | `0x30F298` | per-unit status-duration tick: walks bit rows `+0x1F2`/`+0x5A`, decrements counters `+0x66..+0x74`, sets expiry bit `+0x25`; calls `0x305370`; stages `record+0x27=8`. Emits phase 5 |
| 3 | `0x30F396` | decrement charge timer `+0x18D` (if not 0/0xFF) — the CT countdown |
| 2 | `0x30F35E` | find unit with `+0x18D == 0` (charge complete) → `0x30F8C1`: re-init `+0x1BD`/`+0x18D` from ability row CT (`movsx ecx,[rbx+0x1A2]; call 0x2BB0D4; mov r14b,[rax+0xC]` at `0x30F8C7`) and emit `idx|0x200` |
| **1** | `0x30F3DD` | **the roll-verdict block** (below) |
| 0 | `0x30F500` | CT accumulate: `+0x41 += Speed[+0x40]` (halved/1.5× by status bits of `0x30FC30(unit)`), clamp 0xFE |
| 9/0xF/... | `0x30F55C`+ | turn-owner bookkeeping, apply-event emission via `0x30C2D4(unit)` → `idx|0x300` |

**The roll-verdict block (`0x30F3DD..0x30F4E6`), instruction-accurate:**

```text
0x30F3F0  loop over 21 units: skip if 0x30FC30(unit) & 1 (dead/ineligible);
          prefer units with flag dword [unit+0x1F8] & 0x400; track MAX byte [unit+0x41] (CT)
0x30F44C  selected unit: if +0x1F8 bit10 set -> clear it, write +0x41 = saved value
0x30F466  if byte [unit+0x61] & 4     -> +0x41 = 0x63 (99), bail to turn-grant
0x30F48A  cl = maxCT % 100            (0x51EB851F reciprocal-÷100 at 0x30F48D)
0x30F49C  mov rdx, rbx (unit) ; mov [rbx+0x41], cl   ; write wrapped CT back
0x30F4A2  call 0x30FA34 (VM)          ; decide what this unit's readiness produces
0x30F4A7  mov r10d, eax               ; the refuted "verdict" — actually an event-kind code
0x30F4B1  eax==0 -> 0x30F4C6: if 0x30FC30(unit)&7 ==0 -> 0x30F968 TURN GRANT
          (writes unit+0x1B8=1, +0x1BA, +0x2E=1; phase:=9, next:=0xA)
          else eax&1 -> 0x30F900: half-HP/quarter-MP style adjust via 0x233350/0x2333E0,
          contagion walk +0x1F5/+0x1F4 ; returns idx|0x300 (apply event)
0x30F9BD  (alt select path) turn grant: unit+0x1B8=1, +0x2E=1, +0x41=CT, +0x1BA=1;
          dword [0x1407B0708] = unit index  <- CURRENT-TURN-OWNER GLOBAL (written 0x30F9D0)
```

**Verdict:** it reads/writes ONLY scheduler state (`+0x41` CT, `+0x18D` charge timer, `+0x61`
status, `+0x1F8` flags, `+0x1B8/+0x1BA/+0x2E` turn markers). No staged result fields, no ability
loop, no candidate-target loop. The 8–11 fires/unit/tick = the phase-1 select loop re-entered per
event-pump pass. **It is the engine's turn scheduler — an AI anchor only in the weak sense that
the turn grant marks think-time onset.** Its 3 callers: battle-flow state machines `0x304580`
(called from `0x20E0D0`, `0x212FF4`) and `0x304800` (from `0x20EBEA`), plus the result dispatcher
fn (callsite `0x38A6D3`, same fn whose `0x38A6F9` calls the apply path `0x30A51C`).

Bonus (Strong): `dword [0x1407B0708]` = current turn-owner unit index (written at turn grant) —
a hookless "whose turn is it" read the DCL can poll.

## 2. The shared "compute action result" entry (Strong)

### 2.1 Per-target calc: fn `0x309A44(rcx = action blob*, dl = target unit index)`

Head (byte-verified): copies `0x14` bytes from `rcx` to stack, then **derives and writes every
known staging global itself**:

```text
0x309A81  movzx eax, byte [rsp+0x20]      ; blob[0] = CASTER unit index
0x309A8D  mov [0x1407B0760], al           ; caster idx byte global
0x309A96  shl rax,9 ; add rax, 0x141853CE0
0x309AA3  mov [0x14186AF78], rax          ; caster unit ptr        (the known global)
0x309AAA  lea rax,[r8+0x1BE] ; add rax,rcx(=tgt<<9)
0x309AB4  mov [0x1407B0761], bl           ; target idx byte global
0x309AC1  mov [0x14186AF70], rax          ; target result record = target+0x1BE
0x309AE1  mov [0x14186AF68], rdx          ; target unit ptr
```

then: terrain lookup from target `+0x4F/+0x50/+0x51` (map cell table `0x140D8DCB0`), ability row
fetch `0x2BB0D4(id)` with the **id = blob word `[rcx+2]`**, `0x14`-byte row copy to `0x14186AF88`,
caster/target **Faith `+0x2D` snapshots** into the `0x7B07xx` scratch block (`0x309C78/0x309C88`),
per-formula X/Y copy from static table `0x14080FBA0 + abilityId*6` → `0x1407B07B0`, and finally:

```text
0x309E31  movzx edi, cl                   ; cl = abilityRow[8] = FORMULA id (1..0x6A used, else 1)
0x309FE3  movzx eax, dil
0x309FE7  call qword [r12 + rax*8 + 0x682BC8]   ; r12 = image base — FORMULA DISPATCH
0x309FF5  call 0x306864                   ; post-pass (formula < 7)
0x30A00C  call 0x30A1B0                   ; finalize/copy pass
```

**The formula dispatch table at VA `0x140682BC8` is STATIC (`.rodata`)**: 255 entries, 162
distinct real-code handlers. Sanity anchors: formula `0x05` → `0x30738C`, `0x2D` → `0x308558`,
`0x5A..0x5D` (the Dragon-check family) → `0x30926C/0x3092A8/0x30931C/0x30936C`; `0x6D/0x74/...`
and all `0xD3+` → a common stub `0x10FA4`. Full dump in `work/disasm_ai_anchor.out.txt`. Every
previously mapped site — magic accuracy `0x304DF0`, status proc `0x3065F0`, the finalizers
`0x30637E/0x308D8F/0x307DC4/0x309664`, the `roll` callers — is inside these handlers.
**This is the classic FFT formula table living in real, hookable code.**

### 2.2 Target sweep: fn `0x281D60(ecx = caster unit index, rdx = out summary*)`

```text
0x281DC2  reject ecx >= 0x15 (return -1)
0x281DEA  unit  = 0x141853CE0 + idx*0x200 ; record = unit+0x1BE (cleared via 0x30A4A4)
0x281E92  call 0x281C9C(unit+0x1A0, ...)  ; validate the ORDER RECORD at caster+0x1A0
0x281F3B  call 0x2827CC(&list21, unit)    ; VM thunk: enumerate AFFECTED TARGET unit indices
0x281F6A  call 0x309960(unit+0x1A0)       ; VM thunk: stage current-action globals (0x14186AFF0 id?)
0x281F72  loop rbx=0..0x14: dl = list21[rbx]; if dl != 0xFF:
0x281F7B      rcx = caster+0x1A0          ; the order record IS the action blob
0x281F85      call 0x309A44               ; compute result for this target
0x281F93+ builds the out summary: [rdi]=casterIdx, +1=count, +2..0x11=target list,
          +0x14=id/0x200 special, +0x1C/+0x1D/+0x1E=tile coords, flags...
```

Two corollaries (Strong): **`unit+0x1A0` (order-record byte 0) is the caster's own unit slot
index** (matches live capture: Cloud `+0x1A0 = 0x13` = slot 19) — refines the `+0x1A0` "order
block byte" row in `work/dcl-action-id-candidates.md` §2 — and the 0x14-byte order record
`+0x1A0..+0x1B3` doubles as the calc's input blob (`blob[2]` word = ability id `+0x1A2`).

### 2.3 Who calls the sweep? NOBODY in real code (Strong — the pivotal negative)

Exhaustive scans (all E8 rel32, all E9 rel32, all absolute-qword pointers in every section):
**`0x281D60` has zero real-code callers**; `0x309A44`'s only callers are the sweep loop
(`0x281F85`) and one formula-internal re-entry (`0x307F68`). `0x309960`, `0x2827CC` are VM
thunks. Therefore the drivers that invoke the shared calc — the player forecast builder, the
execution pipeline, and (per the same-calc hypothesis) the AI evaluator — all live inside the
Denuvo VM and enter real code at this one pair. Matches the live-proven fact that the same
finalizers fire at forecast-compute AND at apply (doc 05 §11). **The AI question is therefore
statically unresolvable beyond this point — but it is one log-only hook away.**

## 3. AI/player discriminator (Strong for the signals; Hypothesis for the AI pattern)

There is **no team/controller check inside the calc path**: a scan of `0x281900..0x2833C0`,
`0x304580..0x304C00`, `0x307060..0x30A050`, `0x30A51C..0x30AC10`, `0x30F0C4..0x30FA30` and the
forecast/display region found zero `[reg+4]` byte reads (unit `+0x04` = team, doc 04 §2). The
calc is side-blind; the hook must derive context:

1. **Caster team**: `casterIdx = byte[rcx]` at the `0x309A44` hook → `unitPtr = 0x141853CE0 +
   idx*0x200` → `byte[unitPtr+4]` (team/group id, doc 04). Enemy-team caster ⇒ not a player
   forecast.
2. **Turn owner**: `dword[0x1407B0708]` (§1) — evaluation during an enemy-owned turn.
3. **Commit state**: caster pending flags `+0x61/+0x1EF` (=8 once confirmed) and charge timer
   `+0x18D` — calc fires for a NOT-yet-committed enemy action = think-time evaluation.
4. Expected AI signature (Hypothesis): after the turn grant to an enemy, BURSTS of
   `0x309A44` fires with that enemy as caster, sweeping multiple `dl` target indices and/or
   ability ids, all BEFORE its order record `+0x1A0..+0x1B3`/`+0x1EF=8` commit. A player forecast
   instead fires with the player-team caster while the targeting UI is open, one target per hover.

## 4. LT3 instrumentation — the one cheap hook

**Hook `0x309A44` head, log-only** (ExecuteFirst, no register writes). Head signature is unique
in the whole exe (verified 1 match, 32 bytes):
`48 89 5C 24 18 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 40 48 8B 05 D1 25 47 00 48 33 C4 48 89`.

Per fire log: `casterIdx = byte[rcx]`, `type = byte[rcx+1]`, `abilityId = word[rcx+2]`,
`targetIdx = dl`, `casterTeam = byte[0x141853CE0 + casterIdx*0x200 + 4]`, caster `+0x61/+0x18D`,
`turnOwner = dword[0x1407B0708]`, `phase = dword[0x14186B044]`, timestamp. (All operands are
available in the hook frame's `rcx`/`dl` — no staging-global race.)

One enemy turn answers GOAL A:
- **Fires with enemy caster over multiple targets before commit** ⇒ AI same-calc **Proven** — and
  `0x309A44` (or the dispatch-table entry) is the steering point: a DCL formula rewrite here is
  seen by forecast, execution, AND the AI for free (PSX-style formula-hack parity).
- **No fires during think, yet the enemy acts** ⇒ AI evaluates elsewhere (VM-internal or
  heuristic scoring off ability metadata) ⇒ escalate to plan-B anchors.

Plan-B anchors (also log-only, if needed): the sweep loop `0x281F72` (unique 19-byte sig
`8A 54 1D D8 80 FA FF 74 0F 49 8B CE 44 88 2D 07 E8 52 00`) proves per-action sweep boundaries;
the turn-grant write `0x30F9D0` timestamps think-time onset. Avoid hooking `0x281D60` head — its
24-byte prologue has 4 matches (needs a longer sig with the `movdqa` rip operand).

**Steering preview (post-LT3, if same-calc proves):** replacing/patching a dispatch-table entry at
`0x140682BC8 + formula*8` (data write, table is `.rodata` — needs VirtualProtect) reroutes that
formula for player AND AI alike — a cleaner "custom formula" surface than the pre-clamp for the
compute side, with the pre-clamp remaining the apply-side authority.

## Cross-refs

- Refines: `work/roll-verdict-override.md` (site identified), `work/dcl-action-id-candidates.md`
  §2 (`+0x1A0` = caster slot index; order record = calc input blob), doc 05 §3 (phase engine),
  doc 08 §7 (AI scoring view row).
- New statics for doc 05 when promoted: formula dispatch table `0x140682BC8`; calc entry
  `0x309A44`; sweep `0x281D60`; turn-owner `0x1407B0708`; caster/target idx bytes
  `0x1407B0760/61`; X/Y table `0x14080FBA0` (stride 6, indexed by ability id).
