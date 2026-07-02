# DCL Action-Identity Candidates — Universal Ability-Id Path (offline synthesis)

Dated offline work report (2026-07-02) for doc `08` §1 "True action id / ability id". Sources: the
existing live capture corpus in `work/` plus a fresh static disasm pass (pefile+capstone) over
`FFT_enhanced.exe` on disk. **No live game interaction was performed.** Confidence labels follow the
modding manual (**Proven** = live-proven, **Strong** = static-proven or multiply-corroborated,
**Hypothesis** = needs a live test).

TL;DR: the ability id lives in three places with different lifetimes — (1) a **static
current-action global block** `0x14186AF60..0x14186AFF4` (new, static-proven; includes
`word[0x14186AFF0]` = current ability id, live at forecast-compute AND apply), (2) the **caster
unit's order record** `unit+0x1A0..0x1B3` (`+0x1A2` u16 = id; live-proven from confirm until
overwritten), and (3) the **actor mirror** `actor+0x142` (live-proven at HP apply and selector).
Static disasm now proves (3) is a copy of (2), and (2) is a typed record whose payload is the id.

---

## 1. What the existing captures prove, per lifecycle phase

### 1.1 Baseline (no action selected)

- Caster/target action fields idle: `+0x18D = 0xFF`, `+0x61/+0x1EF = 0`, `+0x1A2` still holds the
  *last* resolved action id (historical). Evidence:
  `work/live_unit_struct_snapshot.round2.baseline-no-target.*`,
  `work/live_unit_struct_snapshot.cross-slash-baseline-no-target.extended.md`; rule already canonical
  in `docs/modding/04-engine-memory-model.md` §5.3 ("action id alone is not pending state").

### 1.2 Forecast (preview open, target highlighted, NOT confirmed)

- **The unit structs carry NO ability id at forecast time.** Round-2 Braver→Agrias forecast diff
  (`work/live_unit_struct_snapshot.round2.forecast-agrias.md`): caster Cloud = "No byte changes";
  target Agrias gained only `+0x1C4=76` (forecast damage), `+0x1D8=2`, `+0x1E5=0x80`. Same shape in
  `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-forecast.extended.md`.
- The forecast object (`target_unit+0x1BE`, aliased by globals `0x142FF3CF8` / `0x14186AF70` /
  `0x14186AF60`) exposes amounts and hit-% (`obj+0x6/+0x8`, `obj+0x2C`) but **no id field was ever
  observed in the unit-window snapshots**.
- Global value/pointer scans at forecast time found **no isolable forecast/action object** carrying
  the id: `work/live_forecast_values.forecast-agrias.md`, the `braverId` named-value scans
  (`work/live_named_value_records.round2.forecast-agrias.braver-damage-charge-cloud-agrias-id.md`)
  return thousands of coincidental hits and zero verified clusters — consistent with `04` §4.3
  ("scan leads are noise").
- **NEW (static-proven, §4):** the forecast finalizer code reads the current ability id and caster
  from a static global block while computing the preview — `word[0x14186AFF0]` (id),
  `dword[0x14186AFF4]` (caster unit index), `qword[0x14186AF78]` (caster unit ptr). This is the
  best forecast-time id path; needs one live confirmation (**Hypothesis** until then).

### 1.3 Confirmed (order committed; charging if delayed)

- **Live-proven, twice, byte-exact.** The caster's struct receives the full order record in one
  step. Braver (id 257 = `0x0101`) — `work/live_unit_struct_snapshot.round2.confirmed-agrias.md`:

  ```text
  Cloud+0x061: 00->08        pending/charging (mirror)
  Cloud+0x18D: FF->02        charge timer (Braver CT)
  Cloud+0x1A0: 00->13        order block byte
  Cloud+0x1A1: 00->29        order TYPE byte
  Cloud+0x1A2..1A3: 0000->0101  ABILITY ID u16 LE = 257 Braver
  Cloud+0x1AA: 00->05        constant marker (see §2)
  Cloud+0x1AC: 00->0A        target tile coord
  Cloud+0x1B0: 00->09        target tile coord
  Cloud+0x1BA: 00->01        active-source marker
  Cloud+0x1EF: 00->08        pending/charging (durable)
  ```

  Cross Slash (id 258 = `0x0102`) reproduces the identical record with `+0x1A2..3 = 02 01` and
  `+0x18D = 03` — `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-confirmed.extended.md`.
  Note `+0x1A0/+0x1A1/+0x1AA/+0x1AC/+0x1B0` are **identical across both actions** (same caster,
  same skillset, same epicenter unit) — consistent with type byte + target-tile params, not with
  action-specific data.

### 1.4 Pending (charge counting down, other units acting)

- Record persists on the caster; only the timer moves (`+0x18D` 3→1 for Cross Slash). Target-local
  forecast (`+0x1C4`) can persist or clear (clears if the pending target takes its own turn) — so
  forecast fields are hints, never target truth. Evidence: `04` §5.3;
  `work/live_unit_struct_snapshot.round2.pending-beowulf.extended.md`,
  `work/live_action_context_records.round2.pending-beowulf.cloud-agrias.md` (pointer-cluster scans
  again found no clean pending object beyond the unit record — the turn-order array at
  `0x15E417BC8`-style addresses was **Refuted** as an action object in
  `work/handoff-to-gpt-2026-06-22-forecast-context.md`).

### 1.5 Execution (HP/MP apply, pre-clamp frame)

- **Pending flags clear BEFORE the HP write** (`04` §5.3, proven): at the pre-clamp the caster
  already shows `+0x61=0, +0x18D=FF, +0x1EF=0` — but `+0x1A2` still holds the id (historical), and
  the **actor mirror `actor+0x142` holds it authoritatively**.
- `[PRECLAMP-ACTOR-CTX]` resolves caster+id memory-only at every positive-debit event for all four
  tested classes: basic (`0`), instant named (`159` Divine Ruination), charged single (`257`
  Braver), charged AoE (`258` Cross Slash, same caster/id across both target events) —
  `work/1782693058-action-identity-live-observe-report.md`,
  `work/1782680077-action-identity-goal-checkpoint.md`.
- Observed id vocabulary through this path (matches the classic FFT ability index; empirical table):
  `0` basic, `1` Cure, `16` Fire, `158` Hallowed Bolt, `159` Divine Ruination, `257` Braver,
  `258` Cross Slash, `265` Choco Beak —
  `work/1782758077-action-identity-coverage-with-selector-fallback.md` (102 logs aggregated).

### 1.6 Selector / render frame (incl. no-HP outcomes)

- The result/animation selector frame (`0x205210`) exposes the **source actor with its `+0x142` id**
  in `rdx`, `r15`, and a stack slot (`+0x90`/`+0xA0`), for normal hits AND for a native no-HP Blade
  Grasp row (`evadeType=0x0B`, source `act=0`) — `work/1782694729-selector-baseline-report.md`,
  `work/1782695389-reaction-nohp-selector-report.md`. Named ids seen at the selector: 158, 159, 257
  (`work/1782686404-…`, `work/1782698795-…`).

### 1.7 Reaction-cancelled incoming action (the one remaining id gap)

- First Strike cancellation: at target-cache time the defender's `+0x1C4` holds the interrupted
  debit and the hook frame's registers point at the incoming **source unit** — but **no action id**
  (unit refs only, no actor refs with `act=`):
  `work/1782729990-first-strike-targetcache-register-report.md`; aggregate: "target-cache source
  action ids = none seen" (`work/1782758077-…`). Named-incoming id is still **Missing**.

---

## 2. Best-candidate pending-action record layout (assembled)

The "pending action" is a **typed order record embedded in the caster's unit struct** at
`+0x1A0..0x1B3`, plus flags/timer outside it. Static disasm (§4) found its generic setter and its
consumer, which fixes the field semantics:

| Offset | Width | Meaning | Confidence | Source |
| --- | --- | --- | --- | --- |
| `+0x61` | b | pending/charging mirror flag `0x08` | Proven | live diffs; `04` §5.3 |
| `+0x18D` | b | charge timer; **initialized from the ability row's CT** (`abilityRow[0xC] & 0x7F`); `0xFF` idle | Proven (live) + Strong (init site `0x30F8C7`) | snapshots; static §4.5 |
| `+0x1A0` | b | order block byte (sub-state; `0x13` observed both tests) | Hypothesis | live diffs |
| `+0x1A1` | b | **order TYPE byte** (`0x29` = ability-use order, observed; `0x0B` = amount-payload order, static) | Strong | live diffs + setter `0x2832F8` |
| `+0x1A2` | w | **order payload = ABILITY ID** for ability orders; `0` basic; amount for type `0x0B` orders | Proven (ability orders) | live diffs; copies §4.6 |
| `+0x1AA` | w | constant `5` (written literally by the record writer) | Strong | live diffs + `0x2832E8` |
| `+0x1AC` | w | target tile coord (out-param 1 of position helper `0x30D484`, which reads unit `+0x4F/+0x50` X/Y and map dims) | Strong | live diffs + static §4.4 |
| `+0x1AE` | w | target tile coord (out-param 3; `0` in live captures) | Strong | static §4.4 |
| `+0x1B0` | w | target tile coord (out-param 2; `9` observed) | Strong | live diffs + static §4.4 |
| `+0x1BA` | b | active-source marker `1` | Proven | `04` §5.2 |
| `+0x1BD` | b | charge-CT copy (same write as `+0x18D`) | Strong | static `0x30F8DB` |
| `+0x1EF` | b | pending/charging durable flag `0x08` | Proven | `04` §5.3 |

Generic order-record setter (real code, **Strong**): function `0x1402832F8` —
`rcx`=unit, `dl`→`+0x1A1` (type), `r8w`→`+0x1A2` (payload), then per-type params into
`+0x1AA/+0x1AC/+0x1AE/+0x1B0`. A dedicated writer at `0x2832AD` builds a type-`0x0B` record with
payload from `unit+0x1E6` and tile coords from `0x30D484`.

**Actor mirror** (per-participant struct, stride `0x548`): at order pickup the engine copies the
whole record — `movups xmm0,[unit+0x1A0]` → `[actor+0x178]` and `[unit+0x1B0]` → `[actor+0x188]`,
plus `unit+0x1A2` → **`actor+0x142`** (word). Two independent copy sites: `0x2126F0` and `0x20C7D1`
(**Strong**, static). This explains every actor id alias seen live (`0x142`, `0x17A` = `0x178+2`,
`0x18C`). Other actor facts: `+0x148` unit ptr (Proven), `+0x1C2` = comparison/queued id (read at
`0x205814/0x26A584`), `+0x0` list link, `+0x8` slot index byte. **Current-actor getters**
`0x260814`/`0x260838` walk the actor list head at `qword[0x140D3A410]` matching the index byte
against `dword[0x140C6AD8C]` / `dword[0x140CF873C]` (two "current" selectors — likely turn-owner vs
executing) — a static, hookless way to reach "the current actor" from managed code.

**Important reinterpretation:** `unit+0x1A2` is not intrinsically "the ability id field"; it is the
**payload of a typed order record**. For ability orders (type `0x29` observed) the payload is the
ability id — which is what every live capture showed. Formula code should gate on the type byte
`+0x1A1` (and pending flags) before trusting `+0x1A2` as an id.

`unit+0x1E6` is **not** an id: the finalizer at `0x30983C..0x309860` stores `word[attacker+0x1E6]`
directly into the result record's damage/heal amount (`obj+6`/`obj+8`) — it is a staged
amount/X-param field.

---

## 3. Multi-hit / AoE batch identity

- **Forecast marks only the epicenter.** Cross Slash centered on Agrias: forecast `+0x1C4=115` on
  Agrias only; Ninja had no forecast fields, yet took 273 at resolution
  (`04` §5.3; `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-*.extended.md`).
- **Resolution = one pre-clamp HP event per victim, sequential, same caster actor + same
  `actionId` across the batch.** Live: Cross Slash produced separate `[PRECLAMP-ACTOR-CTX]` events
  for Beowulf (−230) and Ramza (−187), both resolving Cloud/`258`
  (`work/1782693058-action-identity-live-observe-report.md`). The caster actor is constant across
  the batch; the target varies per event (`04` §3).
- **Dual wield = two separate staged-debit events**, not one aggregate (`04` §5.2, Proven).
- Both AoE victims show `+0x1BB: →0x02` within the same phase window
  (`…cross-slash-aoe-agrias-ninja-post…`), i.e. the batch completes inside one resolution window;
  the snapshots are phase-sampled so intra-frame ordering comes only from probe-log event order
  (sequential lines). No counter-example of interleaved batches exists in the corpus; max
  simultaneous active batches ever observed = 1 (`work/1782758077-…`, "Multiple simultaneous
  pending actions: Missing").
- Batch boundary recipe stays as in `04` §5.3: caster pending→cleared transition opens the batch;
  each pre-clamp event joins by caster-actor identity. What is still missing is an explicit **hit
  index** — the pre-clamp sees N events but nothing numbers them; DCL must count per batch itself.

---

## 4. Static-disasm corroboration (fresh pass, this report)

Method: pefile+capstone byte-pattern scan of real code (RVA `0x1000..0x610000`) for disp32 accesses
of `0x142/0x148/0x18D/0x1A0/0x1A2/0x1BD/0x1C2/0x1D8/0x1E6` + windows around the known sites.
Script/outputs archived as `work/disasm_actionid.py`, `work/disasm_actionid.out.txt`,
`work/disasm_actionid_ctx.out.txt`; key excerpts below are re-derivable from the exe.

### 4.1 NEW — static current-action global block `0x14186AF60..0x14186AFF4` (**Strong**)

The known forecast-object aliases (`0x14186AF60/70`, `05` §11) are part of a larger, coherent
"current action context" block, all read by real (hookable/pollable) code:

| VA | Meaning | Read site (real code) |
| --- | --- | --- |
| `0x14186AF60` | result-record ptr alias (= target+0x1BE) | known (`05` §11) |
| `0x14186AF68` | **current apply-TARGET unit ptr** | pre-clamp itself: `0x30A668 mov rdi,[rip+0x15608F9]` feeds the `movsx eax,[rbp+6]` at `0x30A66F`; also `0x30A63B` |
| `0x14186AF70` | result-record ptr (= target+0x1BE) | `0x30A661`; finalizers `0x30982A/0x309846/0x309890/0x3098A0` |
| `0x14186AF78` | **current ATTACKER/CASTER unit ptr** | forecast-finalizer math reads caster PA/MA through it: `0x307ED5` (`+0x3F/+0x3E`), `0x307DED` (`+0x3F`), `0x307F2B`, `0x30983C`, `0x3072F0` |
| `0x14186AF84` | dword param (divisor in a percent formula) | `0x309868` |
| `0x14186AF88` | **0x14-byte current ability-action data copy** (Formula/X/Y-row-sized) | `0x2831DD` block copy |
| `0x14186AFF0` | **word = CURRENT ABILITY ID** | `0x309687 movsx rcx,[rip+0x1561961]` then `cmp cx,0x1B8` (a specific ability id) |
| `0x14186AFF4` | **dword = current actor's unit INDEX** into the unit array | `0x3096A3 movsxd rax,[rip+…]; shl rax,9; lea rdx,[rip+…]` → `rdx` = `0x141853CE0` — independently re-confirms the unit array base + stride `0x200` |

Because finalizers run at forecast-compute time (proven live for the `obj+6` stores, `05` §11) and
the pre-clamp reads the same block at apply, **this block is live in BOTH phases** — it is the best
single "universal ability id + caster + target" surface: three pointer/index reads, no hook
required. (Writers are likely inside VM staging helpers `0x30BC3C/0x30BCF8` — irrelevant; the data
is ordinary memory.)

### 4.2 Pre-clamp context is register-free reachable

`0x30A65C call 0x30BCF8; mov rbp,[0x14186AF70]; mov rdi,[0x14186AF68]; movsx eax,[rbp+6]` — the
target/record the pre-clamp hook receives in `rdi/rbp` are just these globals; a managed poller can
read the same identities without the hook (though the hook remains the write point).

### 4.3 Display format dispatch confirms the resultKind→field map

`0x22800B..0x228073`: `al = obj+0x27` (= `unit+0x1E5`); bit `0x80`→`obj+6` (HP dmg), `0x40`→`obj+8`
(HP heal), `0x20`→`obj+0xA` (= `unit+0x1C8` MP debit), `0x10`→`obj+0xC` (MP credit), else `obj+0xE`.
Independently re-proves the `04` §2.3 bit map from the binary.

### 4.4 Order-record writer + tile params

`0x283250..0x2832F1`: reads unit X/Y (`+0x4F/+0x50`), calls position helper `0x30D484` (verified: it
reads `+0x4F/+0x50`, map dims at `0x140C6AD6A/B`, writes 3 out-params), then writes the record:
`+0x1A1=0x0B, +0x1A2=word[unit+0x1E6], +0x1AC/+0x1B0/+0x1AE=outparams, +0x1AA=5`. Generic setter at
`0x2832F8` (type in `dl`, payload in `r8w`). Fixes §2's layout semantics.

### 4.5 Charge timer comes from the ability row (id → data in real code)

`0x30F8C7: movsx ecx,[rbx+0x1A2]; call 0x1402BB0D4; mov r14b,[rax+0xC]; …; mov [rbx+0x1BD],r14b;
mov [rbx+0x18D],r14b` — `0x2BB0D4(abilityId)` returns the ability-action row (**the resolver itself
is a VM thunk** `jmp 0x15020BC6D`, so never hook/call it; but its call sites pass the id in real
code) and `row[0xC]` is CT — matching the classic `Ability_Data` layout (`05` §7, FFHacktics:
`0x0C CT`). Also `0x271CD2 cmp [rcx+0x18D],0xFF` (idle check) and apply-side writes
`0x30AA2F/0x30D424`.

### 4.6 The unit→actor id copy (direction of truth)

`0x2126CF/0x20C7B0`: `rcx=[actor+0x148]; movups xmm0,[rcx+0x1A0]; movups [actor+0x178],xmm0;
mov eax,[rcx+0x1B0]; mov [actor+0x188],eax; movzx eax,word[rcx+0x1A2]; mov [actor+0x142],ax`.
**`unit+0x1A2` is the source; `actor+0x142` is the mirror.** Both copy sites are immediately after
`call 0x260838` (current-actor getter, §2). Also `0x205032` (function just before the selector):
current actor → `+0x142`, with real-code id-range classification (`0x18A..0x195`→class 4,
`0x196..0x19D`→class 6) — the engine itself treats these values as the classic ability index.

---

## 5. Minimal live-test plan (PLANNED ONLY — no live run now)

Goal: promote the static block `0x14186AFF0/AF F4/AF78/AF68` to **Proven** as the universal
ability-id path, and close the forecast-time gap. Observe-only, one battle, ~10 minutes.

1. **Probe build (offline first):** extend the poller to log, at ~20 ms cadence on change:
   `word[0x14186AFF0]` (id), `dword[0x14186AFF4]` (unit index), `qword[0x14186AF78]` (caster ptr),
   `qword[0x14186AF68]` (target ptr), `qword[0x14186AF70]` (record ptr), and the first 0x14 bytes
   of `0x14186AF88` — tag `[ACTION-GLOBALS]`. Also log them synchronously inside the existing
   pre-clamp callback (same-frame ground truth).
2. **Forecast sweep (the key question):** open previews WITHOUT confirming for: (a) basic attack,
   (b) Fire, (c) Braver, (d) Cure/heal, (e) an item. PASS = `0x14186AFF0` equals the selected
   ability id at each preview-open (0/16/257/1/item-id) and `0x14186AFF4*0x200+0x141853CE0` equals
   the acting unit. FAIL = block only valid during the compute frame → fallback: hook one finalizer
   (`0x308D8F` physical / `0x30637E` magic, both already knob-supported) and read the block there.
3. **Execution equality:** confirm one instant named + one charged action; assert
   `[ACTION-GLOBALS]` at the pre-clamp frame equals `[PRECLAMP-ACTOR-CTX]` actionId (159 / 257).
4. **AoE batch:** Cross Slash with 2+ victims; assert the globals stay constant across both HP
   events and record the event order.
5. **Multi-pending (the hard attribution case):** two overlapping charged casts; at each
   resolution, assert `0x14186AFF0/AF78` identify the RESOLVING action, not the other pending one.
   This would close the "multiple simultaneous pending" gap in `work/1782758077-…` with a single
   data read.
6. **Reaction bonus (optional):** repeat First Strike (Ninja setup); read `[ACTION-GLOBALS]` at the
   target-cache frame — if `0x14186AFF0` holds the incoming action's id before cancellation, the
   last Missing row (named-incoming id) closes too.

Success promotes to docs: `04` gains the global block + order-record layout (§2), `05` gains the
new anchors (`0x2832F8` setter, `0x2126F0/0x20C7D1` copies, `0x260814/0x260838` getters,
`0x30F8C7` CT init), `06` wires `action.id` from the block with actor-ctx as cross-check.

## Open items after this synthesis

- Forecast-time persistence of `0x14186AFF0` (test 2) — the only blocker for a truly universal
  forecast-phase id read.
- Named-incoming id at reaction-cancel (test 6) — still Missing in all captures.
- Hit index within a batch — engine provides none; DCL must count per batch (own state).
- Cross-battle stability of the actor array/list head `0x140D3A410` — one more battle/save check.
