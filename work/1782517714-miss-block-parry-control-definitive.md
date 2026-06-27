# Miss / Block / Parry control — DEFINITIVE conclusions

Date: 2026-06-26 · Status: **PROVEN LIVE — quadrant 3 (hit→miss + zero damage) closed in-game.** Investigation CLOSED.
Method: 4 parallel static-RE / mechanics investigations over `FFT_enhanced.exe` (capstone) + FFT/IVC
canon (FFHacktics disassembly + IVC community datamining), each evaluating one candidate control
architecture. Supersedes the optimistic "COMPLETE" claim at the bottom of
`work/hit-miss-control-breakthrough.md` (which only covered the *easy* quadrants — see below).

---

## ✅ PROVEN LIVE (2026-06-26) — the decisive test PASSED

Quadrant 3 ("natural hit → MISS + zero damage") is now demonstrated **in-game**, not just on paper.
Test `work/battle-runtime-settings.hit-to-miss-test-v2.json` (Agrias id 0x1E basic attack → Ramza
id 0x01, a guaranteed 100%-to-hit). Proof log: `work/battleprobe_log.hit-to-miss-v2-PASS.*.txt`.

Both real-code hooks fired on the SAME hit and produced the authored outcome:
- **Pre-clamp `0x30A66F`** — `oldDebit=184 forcedDebit=0`, `rdi=Ramza(0x141855CE0,id 0x01)`,
  `rbp=Ramza+0x1BE` ⇒ debit written at `[rbp+6]=Ramza+0x1C4`. Result: `hp=567/567`, forecast field
  cleared (`+0x1C4: B8→00`). **Ramza lost 0 HP.**
- **Selector `0x205210`** — caught `evadeType=0x00(hit)` and `[CONTROL WROTE evadeType=0x04(class-evade)]`.
  **Ramza played the "Miss" sidestep** (user-confirmed on screen: "Ramza fez evade mesmo com 100% de
  chance de acertar ele").
- **Ordering:** the pre-clamp (debit→0) fired BEFORE the selector render; the two paths did not race.

**Refinements learned (for the real-mod implementation — engineering, not blockers):**
- The debit lever for this hit was `[rbp+6]` with `rbp = target+0x1BE`, i.e. it zeroed `target+0x1C4`
  directly (the same forecast field we track) — cleaner than the older "global `[0x186AF70]`" note (that
  may be a different call tree). Either way `0x30A66F` is the correct site.
- `rdi` WAS the target (id 0x01), so the `TargetCharId` guard is valid. The earlier **v1 failure was a
  SCENARIO artifact**, not a path/guard problem: a single global `MaxWrites=1` budget on each hook was
  consumed by an earlier qualifying pass, leaving the test hit unprotected. The real mod needs
  **per-action arming** (not a global counter) on both hooks, plus selector `+1BE!=0` gating to skip
  resting/teardown passes. Concept proven; robustness is an implementation detail.

---

## Why it felt like we were stuck (the honest framing)

The earlier "LIVE CONTROL PROOF" only demonstrated the two **easy** quadrants:
1. **natural hit → keep hit** (write damage), and
2. **natural evade → swap evade animation** (shield-block 0x03 → class-evade 0x04). A natural evade
   already has `+1C4=0`, so no damage coordination was needed — it isolated the animation byte.

What was never proven — and what "control miss/block/parry" actually requires — is **imposing our
decision against the engine's roll**:
3. **natural hit → force MISS/block/parry** (suppress the staged damage AND render an evade animation), and
4. **natural miss → force HIT** (inject damage — but on a miss the engine emits NO apply event, so the
   pre-clamp damage hook never even fires).

This document resolves 3 and 4 definitively.

---

## The decisive engine facts (newly byte-verified this pass)

- **A miss never stages damage.** Producer `0x30F0C4` calls the accuracy/evade gate `0x30FA34` and only
  `or r12d,0x300` (emit "apply") + stages damage **when the gate says hit** (`0x30F4AF test eax,eax; je`;
  `0x30F4B6 or r12d,0x300`). On a miss: no `0x300`, `+0x1C4=0`, `+0x1E5=0`. CONFIRMED-static + matches
  every live miss capture. ⇒ **You cannot "promote" a miss to a hit at the dispatcher — there is no
  staged damage to apply.** (Forcing category `0x300` over a miss applies `HP + 0 − 0` = nothing.)
- **The accuracy/evade gate is virtualized.** `0x30FA34` is an `E9` thunk into the Denuvo VM. There is
  **no real-code always-hit switch to nop/force.** The roll, the evade-source combine, and the write of
  the evade-type `+0x1C0` all live inside VM helper `0x2B8F30` (also `0x2759F8`, `0x30FA34` — all thunks).
- **Damage debit and animation are DIFFERENT memory on DIFFERENT call trees.**
  - HP debit: apply routine `0x30A51C` (dispatcher tree) reads a **global staging record**
    `rbp=[0x186AF70]`: `newHP = clamp(u16[unit+0x30] + s16[rbp+8] − s16[rbp+6])`. Our pre-clamp hook
    `0x30A66F` forces `word[rbp+6]`(=debit/+0x1C4) and `word[rbp+8]`(=credit/+0x1C6).
    *(Correction vs older notes: the lever is the global `[0x186AF70]+6/+8`, not a direct `unit+0x1C4`
    write; `unit+0x1C4` is the displayed copy.)*
  - Animation/result render: selector `0x205210` (forecast/UI tree) reads the **result record**
    `rdi=[r8+0x148]`. Master branch `0x205279 cmp byte[rdi+0x1BE],0`: **`+1BE!=0` → damage render**
    (then `+1E5` bit7 → reads `+1C4`); **`+1BE==0` → evade-animation switch** on `cl`(=`+0x1C0`) at
    `0x2053FA`. The two trees do **not** share a frame, so zeroing the debit does not race the render.

---

## Option space — every approach, evaluated

| # | Approach | Verdict | Why |
|---|---|---|---|
| 1 | **Force always-hit upstream, then AUTHOR the outcome at our 2 proven hooks** (`0x30A66F` debit + `0x205210` evade-type) | **PRIMARY — VIABLE (HIGH)** | Engine always reaches apply+selector with a hit-shaped record; we then keep it a hit or downgrade to any evade. Never needs the impossible "miss→hit". |
| 2 | **Deterministic evade-byte extremes** — write target evade bytes to 0/100 per-hit at **`0x30F49C`** (last real instr before the VM roll; target in `rdx=rbx`), let the engine roll our pre-decided outcome natively | ALTERNATIVE — CONDITIONAL (MED) | all-zero→hit is HIGH; single-source=100→that specific animation is MEDIUM (the source-combine + `+0x1C0` write are in the VM). Most elegant *if* it holds; settle with 1 live test. Hook mapped in `input-control-hook-map.md`; **corrects the earlier `0x226F39`** (that is a UI status-panel exporter, not the evade read). |
| 3 | **Dispatcher category rewrite** (flip `0x300`↔`0x200` at `0x38A6F1`) | **DEAD** | miss→hit applies nothing (no staged damage); hit→miss routes into a VM thunk untested with a damaging record. Adds nothing over the existing hooks. |
| 4 | Staging-write hook at `0x205B39` (`mov [rdi+0x1C0],ah`) | SUBSUMED | selector-entry hook already lands the value the renderer reads (live-proven). Keep only as fallback if entry proves too early. |
| 5 | HP-write reconciler + custom overlay (no native animation) | FALLBACK | last resort; loses native dodge/parry/block visuals. Already exists for HP. |

---

## THE DEFINITIVE ARCHITECTURE (how we will control miss/block/parry)

**"Engine neutralized, mod is the sole authority."** Two layers:

### A. Neutralize the engine's avoidance (data layer) so EVERY attack lands as a hit
The engine resolves avoidance in **three layers**; all must be neutralized, or a residual engine
outcome will pre-empt ours:
- **Layer A — Hamedo / First Strike** (Monk reaction): pre-empts the *entire* attack (strikes first,
  cancels it). Brave%-triggered. If it fires, our hooks never see the attack.
- **Layer B — reaction avoidance**: Blade Grasp, Arrow Guard, Catch, Reflect/Return Magic. Rolls
  **before** the 4 evade bytes. Brave%-triggered. **Zeroing evade bytes does NOT stop these.**
- **Layer C — the 4-byte equipment/class evade roll** (class `+0x4B`, shield `+0x4A`/`+0x4E`, weapon
  `+0x46/+0x47`) — the layer we already drive.

**Neutralization levers (data, cleanest):**
- **Strip the reaction slot / blank the reaction-ability table** → removes Hamedo + Blade Grasp + Catch
  (kills Layers A & B). REQUIRED for full control.
- **`Evadeable` flag OFF** (ability data, Flags4 bit 0x02) and/or **zero the target evade bytes** (Lever
  A) and/or **Concentrate** (which literally zeroes the 4 bytes) → kills Layer C.
- Passive evade-boost reactions (Parry, Archer's Bane, Reflexes, Abandon) just raise the evade bytes, so
  they collapse into Layer C and are killed by zeroing evade.

### B. Author the final outcome at the two proven real-code hooks (code layer)
Per attack, from the DCL formula:
- **DCL = HIT for D:** pre-clamp `0x30A66F` → `word[rbp+6]=D, word[rbp+8]=0`; selector leave
  `+1BE=01 / +1C0=00`. → native damage popup, HP −D.
- **DCL = MISS / dodge / weapon-parry / shield-block:**
  - pre-clamp `0x30A66F` → `word[rbp+6]=0, word[rbp+8]=0` (no HP change), **AND**
  - selector `0x205210` → `+1BE=0` and `+1C0`=evade-type (`0x06` miss / `0x04` class-evade / `0x02`
    weapon-parry / `0x03` shield-block) + force the saved `cl` to the same value.
  → native evade animation, HP unchanged.

**Minimal-write fact (CONFIRMED-static):** to render an evade you only need `{+1BE=0, +1C0=type}` — the
selector never reads `+1E5` or record-`+1C4` on the evade path (so no need to clear bit7 or the displayed
copy). But to make it *deal no damage* you MUST also zero the staged debit at the pre-clamp, because the
HP debit is a separate path. The old plan's `{+1C0,+1BE}`-only is render-complete but **damage-incomplete**.

This architecture makes quadrant 4 ("miss→hit") **unnecessary**: the engine never misses (Layer A neuter),
so we only ever *downgrade* a guaranteed hit — which is quadrants 1 (keep) and 3 (downgrade), both within
the two proven hooks.

---

## Exact hook recipe (shovel-ready)

| Capability | Site | Prologue / ExpectedBytes | Write |
|---|---|---|---|
| Force/zero HP debit | `0x30A66F` (RVA, existing PreClampDamageRewrite) | `0F BF 45 06 0F B7 57 30 44 0F B7 7F 32 0F BF 4D 08` | `word[rbp+6]`=debit (D or 0), `word[rbp+8]`=credit (0) |
| Force evade-type + result-code (animation) | `0x205210` (RVA 2118160, existing ResultSelectorProbe) | `48 89 5C 24 08 48 89 6C 24 10` | record=`[r8+0x148]`: `+0x1C0`=type, `+0x1BE`=0 (evade) / leave 1 (hit); force saved `cl` |

Both real code (`< 0x610000`), both already installed and individually live-proven. Guards exist:
pre-clamp has `TargetCharId` (`cmp edx,id`) + `MaxWrites` 1..32; selector has `MatchEvadeType` +
`MaxWrites` + `TargetCharId`.

---

## What still needs the game (validation only — NOT investigation)

1. ✅ **DONE — hit → miss + zero damage PROVEN LIVE (2026-06-26).** See the PROVEN banner at the top.
   Ramza took a 100%-hit Agrias attack as a class-evade "Miss" with 0 HP lost. Profile:
   `work/battle-runtime-settings.hit-to-miss-test-v2.json`; proof `work/battleprobe_log.hit-to-miss-v2-PASS.*.txt`.
2. **Reaction test** — force a hit (`+1C0=0/debit=D`) into a high-Brave Blade Grasp unit and a Hamedo
   Monk. Confirms whether disabling reactions in data is mandatory (predicted: yes — Hamedo pre-empts).
3. **(Optional) Option-2 test** — hook `0x30F49C` (corrected from `0x226F39`; see
   `input-control-hook-map.md`), set target `+0x4B=100`/others 0 → does it render class-evade 0x04?
   Confirms the deterministic-evade-byte path (the more elegant alternative).
4. **Reaction input-control test** — hook `0x271D20` (defender Brave% roll), force the defender's Brave
   `+0x2B` → Blade Grasp/Hamedo should not trigger. (No struct reaction-slot exists; Brave is the
   real-code lever. Otherwise strip reactions in data.)

## Confidence ledger
- Architecture (force-always-hit + author at the 2 hooks): **PROVEN LIVE** (2026-06-26 — debit-zero +
  evade-reshape pairing demonstrated on a guaranteed hit; see PROVEN banner).
- `+1BE` is the sole damage-vs-evade switch; `{+1BE=0,+1C0}` renders any evade: **CONFIRMED-static.**
- Debit & animation are independent paths; downgrade needs both writes: **HIGH.**
- Miss never stages damage; no real-code always-hit gate; dispatcher rewrite is dead: **CONFIRMED-static.**
- Reactions (Hamedo/Blade Grasp) pre-empt and survive zeroed evade; data-disable is the lever: **HIGH** (FFHacktics canon; IVC nerf to Brave-rate noted) — the reaction test confirms live.
- IVC evade-type enum (0x00 hit/0x01 cloak/0x02 weapon/0x03 shield/0x04 class/0x06 miss; 0x05 unused) is
  IVC-accurate (live 6/6) and diverges from the PSX table — trust live captures.

## Bottom line
We know **exactly** how we will control miss/block/parry: neutralize the engine's three avoidance layers
in data (reactions stripped + evade zeroed / `Evadeable` off), then author every outcome — damage number
and hit/miss/dodge/parry/block animation — at the two already-proven real-code hooks (`0x30A66F` +
`0x205210`). The dispatcher fight is unnecessary and was the dead end. One live test confirms the last
piece (hit→miss+zero-damage); the rest is implementation + calibration, not investigation.
