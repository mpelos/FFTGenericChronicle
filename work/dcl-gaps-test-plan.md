# DCL combat-layer — gaps test plan & answers ledger (2026-06-27)

Goal: close **every** open question ("furo") needed to build the DCL combat layer (preview + execution),
where our external formula owns hit/miss/block/parry/damage/status. Offline binary RE answers what it
can; the rest gets a live probe + exact in-game instructions. **At the end of this plan, every row in the
ledger is answered.**

Conventions: image base `0x140000000`; REAL code RVA < ~0x610000; Denuvo VM = `jmp`/`call` into >0x610000
(virtualizes CODE, not DATA — the engine reads struct bytes live, which is why input-control works).
Unit struct stride `0x200`; `unit = battleArrayBase + charId*0x200`.

## ⚠️ Architecture principle (corrected 2026-06-27, per user): OUTPUT-control FIRST

Control the mechanic by **rewriting the staged RESULT in memory before it is applied to the target**
(the pre-clamp `0x30A66F` / selector `0x205210` interception point). Use **INPUT-control (alter data
before the VM calc) ONLY where output cannot reach it.**

The one engine fact that forces a small amount of input: **a native MISS stages nothing** (no apply
event, `+0x1C4=0`, `+0x1E5=0`) and the hit/miss roll is VM-virtualized (no real-code force-hit switch)
[proven, `1782517714-...definitive.md`]. ⇒ **output cannot do miss→hit / cannot un-negate a reaction**
— there is no staged result to rewrite. So INPUT is used ONLY as the minimal *enabler* that guarantees a
paintable apply event exists: zero the defender's evade so the engine always hits (and, ONLY IF output
can't override them, suppress negating reactions). Everything else — damage, MP, hit↔miss-type, status —
is OUTPUT (rewrite the staged result / write the result onto the target). This is why the persistent
Brave=10 reaction test was off the critical path: it tested an input fallback before checking whether
output overrides the reaction. Reprioritized tests: **T-B (does output override Blade Grasp?)** and
**T-C (status via output)** come first; Brave-suppression (old T1) is plan-B, gated on T-B.

---

## Answers ledger

| # | Question | Offline verdict | Live test | State |
|---|---|---|---|---|
| G1 | Reactions (Blade Grasp/Hamedo/Counter) controllable by Brave `+0x2B`? | YES — roll = Brave% at `0x30BExx`; chicken floor Brave≥10 | **T1** | ✅ CONFIRMED LIVE (Brave 10 → 3/3 hits, Blade Grasp suppressed) — but this is the INPUT plan-B; T-B checks if OUTPUT covers it |
| G2 | Status infliction: force-apply / prevent via memory? | YES — write `+0x1EF` master + `+0x61` mirror (`+0x57` if innate) | **T-C** | ✅ **CONFIRMED LIVE 2026-06-27**: `+0x1EF/+0x61 |= 0x10` → Ramza got **Undead**. Status is OUTPUT-controllable. Bit map now empirical: `0x10`=Undead (offline guess "control-flip" was WRONG); remaining bits to map by testing |
| G3 | MP cost / MP damage / restore controllable? | YES — `+0x34`=MP; staged `+0x1C8`/`+0x1CA`; same apply as HP | **T4** (optional) | static-proven |
| G4 | Miss-TYPE coverage incl. cloak `0x01` & plain-miss `0x06`? | type is VM-made; `0x01`←`+0x49`(?); `0x06`=failed-accuracy (no input byte); both forceable via selector `0x205210` | **T3** (cloak input) | output-control HIGH; cloak-input needs live |
| G5 | Preview hit-%: show arbitrary DCL number? | NO single writable %; it's VM/heap-transient. Levers: quantized evade bytes (real) OR live-pinned VM-return slot | **T6** (capture) | offline-proven negative; arbitrary needs live capture |
| G6 | Input-control deterministic at extremes (evade 0=guaranteed hit, 100=guaranteed avoid)? | endpoints behave as 0%/100% in evade proof | **T5** (fold into T1/T3) | needs explicit multi-sample confirm |
| G7 | Per-action arming: plant the defender's inputs before THE roll for a specific confirmed action? | 20ms poll won race in evade/chicken/reaction; preview-phase pre-arm removes timing risk | **T7** (DCL-slice) | needs live during DCL build |
| G8 | Damage value (incl. element/crit/absorb→heal) ownable? | YES — pre-clamp `0x30A66F` debit `+0x1C4`/credit `+0x1C6` | already PROVEN | done |
| G9 | Hit/miss + miss-type (parry/block/class) ownable? | YES — defender evade bytes (input) or selector (output) | already PROVEN | done |
| G10 | Attacker/target identity at attack time? | YES — CT-drop + actor array `+0x148`; target = pre-clamp ptr / pending-action | already PROVEN | done |

---

## Part 1 — Offline-resolved (RE evidence)

### G2 — Status effects ✅ (script `work/disasm_status.py`)
- **`+0x61` = effective status byte** the engine tests everywhere (KO `0x20`, control-flip `0x10`,
  petrify/removed `0x40`, can't-act/sleep-stop `0x08`, charging `0x04`). It is **derived in REAL code**:
  `0x30D42A..0x30D43C` computes `+0x61 = (+0x1EF & 0xF2) | +0x57`.
- **`+0x1EF` = master/volatile status** (durable source); **`+0x57` = innate/equipment status** OR'd in.
- The infliction **roll** and the death transition are VM (`0x30C114`, `0x30C910`, `0x30FA34`) — not
  patchable — but the status **bytes are data the VM reads back**, so we control by writing them.
- **Recipe — FORCE a status:** `byte[U+0x1EF] |= MASK; byte[U+0x61] |= MASK` (writing `+0x61` alone is
  reverted by the next recompute). **PREVENT/CURE:** clear the same bits on `+0x1EF`, `+0x61`, and
  `+0x57` (innate). Duration-counter array (`+0x5C/+0x7C/+0x9C/+0xBC/+0x13C` triples) may need a nonzero
  count for ticking ailments (poison) — **pin live** (T2).
- Confidence: recompute = static-proven; control = strong (same data-not-code principle as evade/HP/Brave).

### G3 — MP ✅ static-proven (script `work/disasm_mp.py`)
- Struct words: `+0x30` HP, `+0x32` MaxHP, `+0x34` MP, `+0x36` MaxMP.
- The pre-clamp `0x30A66F` is a **combined HP+MP apply** (fn `0x30A51C`): `newMP = clamp(MP + (+0x1CA) -
  (+0x1C8), 0, MaxMP)`, written `mov [rdi+0x34], ax` at `0x30A6CC`.
- Staged result words: `+0x1C4` HP-damage, `+0x1C6` HP-heal, **`+0x1C8` MP-debit**, **`+0x1CA` MP-credit**.
- **Recipe:** at the pre-clamp hook (rdi=defender) write `word[D+0x1C8]=cost` / `word[D+0x1CA]=restore`
  before apply; or write `word[D+0x34]` directly (clamp yourself to `word[D+0x36]`).

### G4 — Evade/miss-type production (script `work/disasm_evadetype.py`)
- The `+0x1C0` evade-type **value is produced inside the VM roll `0x30FA34`**; the only real-code `+0x1C0`
  write is a teardown copy at `0x205B38` (not the authoring site). So there is **no real-code switch** to
  name the type directly.
- The enum is real & each value renders distinctly (consumers `0x1FAB3F`, `0x266E10` [`0x01` has its own
  anim id 0x21], selector `0x2053FA`). Values: `0x00` hit, `0x01` cloak, `0x02` weapon parry, `0x03`
  shield block, `0x04` class evade, `0x06` plain miss.
- **Cloak `0x01`**: strong-inferred driven by **`+0x49`** (accessory physical evade; aggregators take
  `max(+0x49, shield +0x4A)` and the struct map's unmapped "cloak evasion" field). Input-control: set
  `+0x49` high, all other evade bytes 0 → expect `0x01` — **needs live (T3)**. Output-control: selector
  `0x205210` set `+0x1C0=0x01` — HIGH (proven mechanism).
- **Plain-miss `0x06`**: the failed-accuracy path; **no defender input byte**. Force only via output-
  control selector `+0x1C0=0x06`. HIGH.

### G5 — Preview hit-% ❌ no writable location (script `work/disasm_preview.py`)
- The hit-% is **VM-computed** (no real-code formula; first-touch helper `0x269760` and gate `0x30FA34`
  are VM thunks; no `1e8` divisor in real code). It is **not stored** in any real struct/global — only
  transiently in heap UI buffers / a VM-return register.
- The status-panel exporter `0x226EBC`→scratch `0x7832E0` copies **raw stats only** (no computed %).
- **Two levers:** (a) **quantized** — write the target's evade bytes; the VM re-reads at preview and the
  native % moves (dense but not arbitrary; this is the only real-code lever and doubles as the outcome
  lever); (b) **arbitrary** — trampoline a forecast caller (`0x26A618/0x26A746/0x26A89A`) and overwrite
  the VM-return %-slot, which must be **pinned live (T6)**.

---

## Part 2 — Live probes (design + in-game instructions)

> Division of labor: I build/deploy the harness; you do the in-game GUI step and report. Each test below
> is a `battle-runtime-settings.<name>.json` profile. Run only `fftivc.utility.modloader` +
> `fftivc.generic.chronicle.codemod`; data mod stays disabled. Relaunch the game after each deploy.

### T1 — Reaction suppress (G1, G6-partial) — **READY, deployed**
- Profile: `battle-runtime-settings.brave-reaction-test.json` (Brave→10 broadcast; chicken-safe).
- **You:** with Ramza holding Blade Grasp (Shirahadori, 97 Brave), attack him with a physical melee
  **5–6×**. Report: how many times Blade Grasp caught vs let the hit through.
- PASS: catches ≈0–1/6 (vs ~all at 97). Optional crisp follow-up: I flip to Brave=100 → catches every hit.

### T2 — Status control (G2) — **build `StatusOverride` knob**
- Probe: poller writes `+0x1EF` and `+0x61` (and optional `+0x57`) on the target, like BraveOverride.
- Profile A (most visual): set `+0x1EF |= 0x10` and `+0x61 |= 0x10` on one enemy → **control-flip**: that
  unit should switch to your side / act under AI as an ally. Profile B: `|= 0x08` → unit can't act.
- **You:** load a battle, observe the targeted unit. Report: did it switch sides (A) / lose its turn (B)?
- PASS: the status takes effect and persists → data-write controls status. (Then we map remaining bits.)

### T3 — Cloak `0x01` via `+0x49` (G4) — **extend `EvadeOverride` with accessory bytes**
- Probe: add `+0x48/+0x49/+0x4C/+0x4D` to the evade override.
- Profile: defender `+0x49=100`, all of `+0x46/+0x47/+0x4A/+0x4B/+0x4E=0`. Attack from the front.
- **You:** report the dodge animation + the `[SELECTOR-PROBE]` `evadeType`.
- PASS: `evadeType=0x01` (cloak) → `+0x49` is the cloak source, input-selectable. FAIL → use output-control.

### T4 — MP control (G3) — **optional (static-proven); quick confirm**
- Probe: poller writes `+0x34` (MP) to a sentinel on a tracked unit (e.g. 5), or pre-clamp writes
  `+0x1C8=5` during an attack.
- **You:** watch the unit's MP gauge. PASS: MP shows the sentinel / drops by 5 on the next hit.

### T5 — Determinism at extremes (G6) — **fold into T1/T3 runs**
- During T1/T3, also do: defender all evade bytes `=0`, attack 6× → expect **6/6 hits** (guaranteed hit);
  then `+0x4B=100`, attack 6× → expect **6/6 class-evades** (guaranteed avoid).
- **You:** report the 6/6 counts. PASS: endpoints are deterministic (lets the DCL force binary outcomes).

### T6 — Preview arbitrary-% (G5) — **build a forecast-return capture probe** (do last)
- Probe: hook a forecast caller (`0x26A618/0x26A746/0x26A89A`), log the VM-return register/stack slot
  holding the % for a target whose on-screen preview shows a known value.
- **You:** open the attack preview on a unit, note the on-screen %, report it; I match it to the captured
  slot. PASS: the % appears at a stable slot we can overwrite → arbitrary display %. (If not, we use the
  quantized evade-byte lever for the preview.)

### T7 — Per-action arming (G7) — **validated during the first DCL slice**
- Probe: the DCL slice itself — on preview/confirm of a specific (attacker→target), compute the formula,
  plant the defender's evade/Brave for that one action, write damage at pre-clamp.
- **You:** execute a few attacks; report whether the planted outcome lands every time (no races).
- PASS: outcome matches the planted value each action → arming is reliable.

---

## Part 3 — Harness changes to build
1. `StatusOverride*` (write `+0x1EF/+0x61/+0x57`) — mirror `BraveOverride`. [T2]
2. Extend `EvadeOverride` with `+0x48/+0x49/+0x4C/+0x4D`. [T3]
3. `MpOverride` (write `+0x34`) and/or pre-clamp staged-MP (`+0x1C8/+0x1CA`). [T4]
4. Forecast-return capture hook at `0x26A618/...`. [T6]
5. (DCL slice) per-action arming via the pending-action tracker. [T7]

## Part 4 — Recommended execution order
T1 (ready) → T2 (status, most decisive new result) → T3 (cloak) + T5 (determinism, same runs) → T4 (MP) →
T6 (preview capture) → T7 (DCL slice). After T1–T6 every ledger row is answered; T7 validates the
assembled architecture.

## Net architecture once answered
- **Preview:** quantized evade-byte lever (or T6 arbitrary override) shows the DCL %.
- **Execution:** plant defender evade (hit/miss/type) + Brave (reactions) + status bytes (`+0x1EF/+0x61`)
  before the roll; write damage/MP at the combined pre-clamp `0x30A66F`. Engine renders everything.
  All driven by our formula with full attacker+target context. No data-gutting, no result-forging.
