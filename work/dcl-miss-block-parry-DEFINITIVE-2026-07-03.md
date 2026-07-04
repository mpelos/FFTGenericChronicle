# Miss / Block / Parry control — DEFINITIVE conclusion (2026-07-03)

Synthesis of five parallel investigations (3 offline disasm, 1 repo/data, 1 web/mechanics).
Source docs: `dcl-evade-recompute-site.md`, `dcl-hitpct-source.md`, `dcl-kind-paint-analysis.md`,
`dcl-data-neutralization-levers.md`, `dcl-evade-mechanics-research.md`.

**We are NOT stuck. The old wall ("poll is racy, verdict is VM-internal, paint only re-skins a
hit") was a MISDIAGNOSIS. There is no per-attack writer to race, and there are three airtight
levers. Full miss/block/parry control is achievable.**

---

## 1. How FFT actually resolves avoidance (web-confirmed, matches our live enum)

Two phases, in order:

**Phase A — Reactions** (pre-empt the whole attack; trigger% = defender **Brave**):
Blade Grasp / Hamedo / Arrow Guard / Catch. Fire BEFORE the evade rolls; on success they end
the attack with their own outcome tag. Weapon Guard is NOT a reaction — it just enables the
weapon-parry evade byte in Phase B.

**Phase B — four evade bytes, base-hit 100, rolled SEPARATELY in slot order, first success
wins and stops the chain** (multiplicative survival, not additive):

| Slot | Source | Input byte (unit struct) | Direction nullified |
| --- | --- | --- | --- |
| 1 | Accessory evade | **+0x48 / +0x49** | — (survives even from rear) |
| 2 | RH (weapon parry) | **+0x46 / +0x47** | rear |
| 3 | LH (shield block) | **+0x4A / +0x4E** | rear |
| 4 | Class / physical evade | **+0x4B** | side + rear |

Direction: front = all 4 apply; side = class nullified; rear = class+RH+LH nullified (only
accessory survives). Concentrate/transparent zero all four. Blind/confuse double effective evade.

Hit% (WotL): front `Wp·(100−CEv)(100−SEv)(100−AEv)(100−WEv)/10⁸`; magic
`Wp·(100−M.SEv)(100−M.AEv)/10⁴` (no class evade; Faith enters magic only). Guns ignore evasion.

**Evade-TYPE = a single byte** (PSX `0x018e` ↔ IVC **`+0x1C0`**), set to whichever source won
first. IVC live enum (6/6): `00` hit · `01` accessory/cloak · `02` weapon-parry · `03`
shield-block · `04` class-evade "Miss" · `06` plain miss (also `09` reflect, `0b` Blade Grasp,
`0d` Catch). **To render a specific animation, make the corresponding source win first** (or
author the byte + its evade-source directly). Shield vs weapon is decided by which hand holds
the item, not a distinct type.

**Evadeable flag**: bit `0x02` of "AI Behavior Flags 2", ability-flag byte offset `0x05`.
Cleared ⇒ the attack skips the ENTIRE evade system AND reactions (fully unblockable).

---

## 2. The two byte-families (do not confuse them)

**Family 1 — pre-roll INPUT evade bytes** (what the VM rolls against, Phase B):
`+0x46/47` weapon · `+0x48/49` accessory · `+0x4A/4E` shield · `+0x4B` class.
Written ONLY by equip/refresh copiers (see §3). The VM reads these live from the struct at
roll time (Denuvo virtualizes code, not data — proven).

**Family 2 — staged-result evade-SOURCE bytes** (what the renderer reads to pick the message):
`+0x1D0` (bit tests) · `+0x1D2..+0x1D5` (weapon/shield/accessory result) · `+0x1D8` (class) ·
`+0x1C0` final kind. The VM writes these from the roll outcome; `+0x1C0` is stamped by the
finalize routine `0x205B38` (fn `0x2055FC`, gated `test [rdi+0x15C],4`, unit=RDI) — the SOLE
`+0x1C0` writer in the whole binary. Presentation (`0x1FAB3F`, `0x26A704`→selector
`0x205210`→emitter `0x268E7C`) is a pure reader downstream.

Pipeline: **Family 1 (input) → VM Phase B roll → Family 2 + `+0x1C0` (staged result) →
presentation.**

---

## 3. Why the poll was "racy" — and the airtight fix

**There is NO per-attack real-code writer of the Family-1 evade bytes.** BFS from every writer
never reaches the avoidance cluster (0x30F0C4 / 0x30FA34 / 0x309A44 / 0x205210 / 0x30A66F); the
combat region has zero genuine evade writers; the pre-roll gather `0x30FC30` reads `+0x61/0x64/
0x65/0x1B4`, not the evade bytes (they're consumed inside the VM).

The only real-code writers are equip/refresh **copiers** (run at battle-init / equip / status
edge, NOT per-attack):

- **Writer A — fn `0x59F550`** (unit ptr = **RBX**): copies a stat block `[rdi+0x10..0x17]` →
  `[rbx+0x48..0x4F]`, i.e. writes accessory `0x48/49`, shield `0x4A/4E`, class `0x4B`. Restamp
  after `0x59F927`.
- **Writer B — fns `0x285394` (unit=RBX) & `0x3965B0` (unit=RSI)**: weapon lookup → stack →
  `[rbx+0x44..0x47]`, writes weapon parry `0x46/47`. Restamp after `0x285550` / `0x39674B`.

Between A and B, **all Family-1 evade bytes are owned by exactly three real-code sites.** The
"~50% loss" was one of these copiers re-stamping on a state transition between the poll and the
roll — asynchronous to the poll, not a per-attack writer beating it.

**AIRTIGHT FIX: detour the three copiers.** Hook the tail of Writer A + Writer B (+ twin) and
over-stamp our formula-computed evade bytes after their equipment copy. Because these own every
legitimate path that changes the bytes, the value persists to the VM roll with **no race**.
This retires the poll entirely.

---

## 4. Arbitrary hit% — the engine's roll is NOT the lever; the MOD rolls

- `unit+0x1EA` is **display-only** (0 writers, 1 UI reader `0x3B122F`). Not read by any roll.
- The VM roll `roll(100, chance)` at `0x278EE0` gets `chance` from VM-written memory:
  magic ← Faith global `0x1407B079D`; status ← scratch `0x1407B07AC`; physical ← `staging+0x2B`
  (`qword[0x14186AF68]`). **None has a real-code writer** — the VM recomputes them at compute
  time (writing `0x1407B07AC` was already REFUTED live: engine overwrote 0→3854). So there is
  **no airtight input for an arbitrary engine-side %**, and no offline patch route.
- **Conclusion — don't fight the VM roll.** The mod computes its own DCL hit% from full context
  and rolls its **own** RNG, then forces the BINARY outcome via Family-1 evade bytes:
  - force **HIT**: all evade bytes = 0 (Concentrate-equivalent) ⇒ no source can win ⇒ connects.
  - force **AVOID(type)**: set the chosen source's byte = 100 (others 0) ⇒ that source wins ⇒
    that animation. (Rear-nullification etc. still apply; account for facing in the formula.)
  This yields EXACT arbitrary % (we roll) with zero dependence on the VM's opaque roll.

---

## 5. Output-paint (`+0x1C0`) — secondary lever, with a constraint

Writing `+0x1C0` alone is fundamentally a **re-skin of a naturally-connecting attack**:
1. finalize `0x205B38` re-stamps `+0x1C0` from the VM verdict AFTER our compute-point hook
   `0x281F8A` (so a raw write there is vulnerable); and
2. selector `0x205210` re-consults Family-2 evade-source bytes at render time — a paint that
   contradicts them gets steered back to the natural message.

So to author a type cleanly you must co-write `+0x1C0` **and** the matching Family-2 evade-source
byte, zero `+0x1C4`, keep `+0x1BE`≠0 — and it is only "free" on an attack the VM let connect.
Flipping avoid→hit is an INPUT operation (Family 1), not a paint. The authoritative kind-write
point is the finalize stamp `0x2055FC`/`0x205B38` (unit=RDI), not `0x281F8A`.

Damage stays fully controlled: pre-clamp `0x30A66F` (apply) and compute-point `0x281F8A`
(`+0x1C4`, LT4-proven) — both leak to the applied result.

---

## 6. Data-neutralization (the race-free "sledgehammer", physical only)

All three avoidance layers can be zeroed in DATA so every PHYSICAL attack connects naturally
(then the mod only ADDS the avoids its formula wants):
- **Evadeable bit** `0x02` in `OverrideAbilityActionData.nxd` (per-ability) — clear ⇒ skips
  evade + reactions entirely.
- **Equipment evade columns** in `item_catalog.csv` (weapon/shield/accessory evasion) — zero them.
- **Reaction R/S/M slots** in `JobCommandData.xml` (176 rows) or per-encounter ENTD — strip them.

Gap: **magic** avoidance (Faith roll) is VM-internal — data cannot gate it; needs a code hook
or the Family-1 M.evade route. Not a blocker (magic uses the same "mod rolls + forces outcome").

---

## 7. THE ARCHITECTURE (definitive)

Context spine = `computeActionResult 0x309A44` (per-action, per-target, player+AI; already
proven). Per (action, target):

1. **Mod computes** DCL outcome from full context (attacker/target/equipment/direction/status):
   hit% → own RNG → decision {HIT | AVOID(accessory/weapon/shield/class)} + damage.
2. **Force the decision via Family-1 INPUT**, injected airtight by hooking the 3 equip/refresh
   copiers (`0x59F550`, `0x285394`, `0x3965B0`): HIT ⇒ zero all evade bytes; AVOID ⇒ chosen
   source = 100. The VM then produces a fully coherent outcome (message + animation + apply-gate)
   — no race, no downstream fight.
3. **Damage** via pre-clamp `0x30A66F` and/or compute-point `0x281F8A` `+0x1C4`.
4. **Reactions** (Phase A) via the real-code Brave-gate `0x30BE86/8B` (force/suppress by Brave
   input) or data-strip.
5. **Output-paint** finalize `0x2055FC`/`0x205B38` (+ Family-2 co-write) only when we want a type
   independent of equipment, or as belt-and-suspenders.

Optional simplification: **data-neutralize physical avoidance** (§6) so step 2 only ever ADDS an
avoid (never has to force a hit), removing edge cases.

---

## 8. What still needs a LIVE test (LT5) — plan, do not run (needs user + build)

The base mechanism (VM honors Family-1 evade bytes; all-zero forces hit; byte selects type) is
ALREADY proven live (2026-06-27). NEW and strictly-necessary to validate before committing the
architecture:

- **LT5-A — copier-hook airtightness** (profile `work/battle-runtime-settings.lt5a-forcehit.json`).
  PASS = a normally-evasive enemy hit 6/6 (old poll leaked ~50%). This retires the poll.
- **LT5-B — force an AVOID airtight** (profile `work/battle-runtime-settings.lt5b-classmiss.json`,
  `4B=100` others 0). PASS = every hit shows class-evade "Miss" 6/6. (Byte→type mapping itself was
  already proven live 2026-06-27; LT5 only adds airtightness.)
- **LT5-C (optional) — finalize output-paint.** Cases A–E in `dcl-kind-paint-analysis.md` §"Proposed
  LT5": co-write `+0x1C0` + Family-2 evade-source at `0x2055FC`/`0x205B38` on a proc-free weapon
  (avoid the Ramza petrify confound). Confirms the type-author-independent-of-equipment path.

### AS-BUILT harness (`EvadeCopierOverride*`, compiled + settings-validated 2026-07-03)

Three `ExecuteFirst` asm hooks at the copier **tails** (unit ptr still live, all evade bytes freshly
stamped, no `call` stolen). Over-stamp each configured Family-1 byte (`-1` = leave), optional charId
filter. Fixed sites (RE discoveries, not user-tunable):

```
A  0x59F93C  unit=RBX  steal 0F B6 47 18 88 43 50        (branch convergence after evade stores)
B  0x285553  unit=RBX  steal 4C 8D 5C 24 60 49 8B 5B 10  (after [rbx+0x47], before epilogue restores rbx)
C  0x396757  unit=RSI  steal 48 8B D7 48 8B CE           (twin; rsi callee-saved across the intervening call)
```

Settings: `EvadeCopierOverrideEnabled`, `EvadeCopierOverrideTargetCharId` (-1=all), `EvadeCopierOverride46..4E`.
Everything above is offline-built; only the in-game confirmation needs the user.

### LT5-A RESULT (live 2026-07-03, profile lt5a-forcehit, all 3 hooks installed & byte-validated)

**Near-PASS, contaminated by Shirahadori (Blade Grasp).** All defenders had Shirahadori equipped —
a Phase-A reaction (Brave-gated), by design NOT affected by evade-byte zeroing. Decode:
- Preview showed **2–3%** = `100 − defender Brave` — i.e. evade collapsed to 0 (else Ramza's ~50%
  shield evade would push it to ~1.5%) and only BG survival remained. **Confirms the zeroing worked**
  AND that the IVC preview folds reaction-negate chance into the displayed hit%.
- **Zero class-evades / weapon-parries in ~14 swings; every swing BG didn't catch, HIT** (Ninja's
  2nd dual-wield swings landed consistently).
- **Mechanic find:** BG caught only the FIRST dual-wield swing, consistently → in IVC the reaction
  appears to fire once per action.
- **ONE residual leak:** a single shield-parry on a 2nd swing → some writer the static RE can't see
  (VM-side engine code or battle-init bulk copy) re-stamped shield evade once. Copier hooks alone are
  ~93% airtight, not 100%.

### FIX (built 2026-07-03): CalcEntryEvadeStamp — per-attack, zero-width race window

The per-attack injection point the engine "doesn't have" is OUR calc-entry hook: `computeActionResult
0x309A44` receives `dl = target unit index` and the VM avoidance roll happens INSIDE that call. New
`CalcEntryEvadeStampEnabled` stamps the EvadeCopierOverride* byte profile onto the target struct at
ExecuteFirst — microseconds before the roll; no state edge can intervene. Dual delivery (copier tails
+ calc entry) = the definitive airtight architecture. Validation: **LT5-A2** (profile
`lt5a2-dual.json`), defenders WITHOUT Shirahadori: PASS = 6/6 hits incl. both dual-wield swings,
preview ~100%.

### LT5-A2 RESULT (live 2026-07-03) — FAIL for SHIELD evade; reframes the model

All 4 hooks installed (log confirms `evadeStamp=ON`, all evade bytes forced 0). Yet Agrias→Ramza
showed **preview 50% and Ramza shield-PARRIED** — Ramza's shield evade (+0x4A = 0x32 = 50%) survived
to BOTH the preview number and the roll, despite a zero write at 0x309A44 *inside the same call as the
roll*. `dl` is confirmed (LT3a) to be the target index for a single-target Attack preview, so the write
landed on Ramza's struct. Conclusion: **the preview/roll do NOT read shield evade from the live +0x4A
byte** — it is snapshotted or recomputed from the equipped shield item before/at the roll.

**Reframe — the 2026-06-27 proof only covers +0x4B (class).** That test forced class-evade UP and Ramza
dodged (proving the VM reads +0x4B live). It never proved zeroing shield +0x4A forces a hit. The two
legs behave differently:
- **Class evade (+0x4B)** comes from the JOB → stored in the byte, read live → runtime-controllable. ✅
- **Shield evade (+0x4A/+0x4E)** is recomputed from the equipped shield ITEM at roll time (matches the
  old "engine RESTORES +0x4A from equipment right before the roll" note) → a live byte write is ignored.

So the "airtight input" architecture holds for job-derived evade (class) but NOT for equipment-derived
evade. RE `work/dcl-shield-evade-read-path.md` PINNED it (2026-07-03):

### The record-builder asymmetry (definitive) + the surgical lever

The preview/roll do NOT read the unit's evade bytes live. Three combat-input record builders
(`0x284A80`, `0x3600DC`, `0x3962F0`) pack them into a separate record at action SETUP (before
0x309A44): **class = 1:1 copy** (`record+0x44 = unit+0x4B` — why the live +0x4B write worked) but
**shield/accessory = derived MAX** (`record+0x46 = MAX(+0x4A,+0x49)`, `record+0x50 = MAX(+0x4D,+0x4E)`)
— a late unit-byte stamp neither reaches the already-built record nor beats the +0x49 term. Weapon
parry packs at `record+0x3C` (1:1 of +0x47). Roll anchors read NO evade bytes.

**Runtime lever built (`EvadeRecordOverride*`):** ExecuteFirst at the 9 packed-store sites
(`0x284BEC/0x284C00/0x284C28`, `0x3602D6/0x3602EA/0x360313`, `0x396468/0x39647C/0x3964A5`, all
`mov [rec+44/46/50], ax`) injecting `mov eax, VALUE` so the engine's own store writes our value
(eax is dead after each store). Settings 44/46/50, -1=leave. Global (both teams).
Also built: **`ReactionChanceControl*`** — the 4 real-code Brave-gate roll sites
(`0x30BE86/0x30BEDC/0x30BF32/0x30BF72`, `call 0x278EE0` with edx=Brave; eax==0 arms) — forced 0 =
NO reaction ever arms (kills the Shirahadori confound), 100 = always.

**LT5-A3** (profile `lt5a3-surgical.json`): record zeroed (44/46/50=0) + reactions suppressed +
copier/calc-entry unit-byte zeroing kept. PASS = preview 100%, every swing connects.

### LT5-A3 RESULT (live 2026-07-03) — FAIL; two more refutations, and the VM boundary is now sharp

All 16 hooks installed (log-confirmed). Results: Ninja→Ramza 3% preview, Shirahadori parried 1st
swing / 2nd hit (3×); Agrias→Ramza **1%** preview (= BG 3% × shield 50%), Shirahadori parried.
- ❌ **REFUTED: ReactionChanceControl** — `REACTROLL fires = 0` across a battle with 4+ Shirahadori
  triggers. **Shirahadori's roll is VM-internal**; the real-code Brave-gate cluster 0x30BE86-0x30BF72
  serves OTHER reactions (LT3b's 2 fires = Counter/Mana Shield class). The proven Shirahadori lever
  remains the **Brave input write** (2026-06-27: Brave 10 → suppressed; VM reads Brave live).
- ❌ **REFUTED: EvadeRecordOverride** (as the roll lever) — shield 50% still in preview and still
  parrying. The 3 real-code record builders either don't run for combat or don't feed the roll; the
  VM packs its own record. (No fire-counters on these hooks — observability gap, moot now.)

**Sharp VM boundary (consolidated):** the entire pre-hit avoidance pipeline — accuracy %, equipment
evade derivation, negate-reactions (Shirahadori) — is VM-internal and consumes VM-derived data. The
live unit-struct inputs the VM honors: **class evade +0x4B, Brave +0x2B, status/immunity bytes**.
Every runtime write to shield/weapon evade (unit bytes, copier tails, calc-entry, record stores) is
refuted. **One data surface remains untested — the loaded ITEM TABLE in memory** (the source the VM
derives equipment evade from; Writer B's lookup 0x287410 reads it). Item tables are plain data →
zero the per-item evade fields at runtime (or via the data mod) and the VM's own derivation yields 0.
RE in progress: `work/dcl-item-table-runtime-poke.md`.

## ✅✅ LT5-A4 PASS (live 2026-07-03) — EQUIPMENT EVADE KILLED AT THE SOURCE. CAMPAIGN CLOSED.

RE found the item stat tables baked at fixed VAs in a writable section (`work/dcl-item-table-runtime-
poke.md`, fn 0x2B8CB8 chain): weapon `0x80F690` (stride 8, W-Ev at +5), shield `0x80FA90` (16×2
phys/magic), accessory `0x80FB30` (32×2); sanity anchor `0x80FAAC` = `32 19` (Venetian Shield 50/25).
Built `ItemTableEvadeZero` (managed poll write, sanity-gated). Bonus RE correction: "Writer A"
0x59F550 was **HID gamepad enumeration** (byte-scan false positive) — removed from the copier hooks.

**LIVE RESULT (profile `lt5a4-itemtable.json`):**
- Ninja→Ramza (50% weapon-parry gear ON): **preview 100%, 8/8 dual-wield swings hit** (4 tests)
- Agrias→Ramza (50% shield ON): **preview 100%, 4/4 hit**

Zeroing the loaded item tables makes the VM's own derivation produce 0 evade — preview AND roll,
every unit, both teams. **PROVEN: source-data control beats the VM everywhere its inputs are data.**

## FINAL LEVER MAP (all live-proven)

| Layer | Lever | Status |
|---|---|---|
| Equipment evade (weapon/shield/accessory) | `ItemTableEvadeZero` — poke the loaded item tables | ✅ PROVEN LT5-A4 |
| Class evade (job) | `+0x4B` live byte write (poll / stamp) | ✅ proven 2026-06-27 |
| Reactions (Shirahadori etc.) | **Brave `+0x2B` write** (10=suppress, chicken floor) or data-strip R/S/M | ✅ proven 2026-06-27 (hook path REFUTED — VM rolls it) |
| Damage / HP / MP | pre-clamp `0x30A66F` + compute-point `0x281F8A` | ✅ proven (LT4) |
| Status | immunity `+0x5C` + master `+0x1EF`/`+0x61` | ✅ proven (LT1/LT2) |
| Hit% (arbitrary) | mod computes % + own RNG → forces binary outcome via the levers above | design consequence, unblocked |
| Authored miss/parry/block on a connecting hit | paint kind `+0x1C0` + Family-2 co-write at finalize `0x2055FC` (LT5-C, optional refinement) | designed, not yet live-tested |

**Refuted (never retry):** evade unit-byte writes for equipment legs (all 4 delivery mechanisms),
ReactionChanceControl @0x30BE86-0x30BF72 for Shirahadori (VM-internal; cluster serves other
reactions), EvadeRecordOverride as roll lever, VM chance globals, +0x1EA (display only).
