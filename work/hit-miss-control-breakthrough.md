# Hit/Miss/Block/Parry control — BREAKTHROUGH synthesis

Date: 2026-06-25 · Status: **wall broken + observe probe built (green); forecast display characterized; pending live validation**
Source: 4 parallel investigations (binary RE, data lever, FFT mechanics, corpus re-analysis).
**Updated by a round-2 deeper-RE pass — see correction box below.**

---

> ## ⚠️ ROUND-2 CORRECTION (deeper RE) — read this first
> A deeper disassembly pass **corrected the dispatcher interpretation in §1.** Net effect: the
> conclusion ("we can control miss/block/parry") is **strengthened**, but the mechanism differs.
>
> - The dispatcher categories `0x300/0x200/0x100` are **NOT hit/miss/status.** They are an **event
>   queue**: `0x300`=apply a staged HP/MP/stat change (→`0x30A51C`), `0x200`=status/effect-state
>   transition, `0x100`=action/turn completion, `0xFF00`=terminator, `0xE000`=init. A **miss simply
>   never stages an HP change**, so no `0x300` is emitted for it — this is why a miss bypasses the
>   apply hook (not because the hook is "skipped").
> - The **hit/miss roll is virtualized** (gate `0x30FA34`; forecast/evade helpers `0x269760`,
>   `0x2759F8`, `0x2B8F30`, `0x2740A0` — all VM thunks). We can't read it; we don't need to.
> - **The apply path `0x30A51C` is fully reversed:** `newHP = clamp(HP + (word[unit+0x1C6] −
>   word[unit+0x1C4]), 0, MaxHP)`. It reads staged damage from **`+0x1C4`**, heal from `+0x1C6`,
>   and consults **no** hit-flag. ⇒ our existing `0x30A66F` pre-clamp hook already controls
>   hit-vs-no-damage. (`+0x1BB→2` is written later in the aftermath at `0x30AAFC`.)
> - **The evade/miss ANIMATION lever is a separate byte:** the **evade-type at `actionObj+0x1C0`**
>   (actionObj = `*(unit+0x148)`), read by the selector `0x205210` when no damage is staged
>   (`+0x1BE==0`). Values (HIGH conf map, MEDIUM exact sprite): `0x00`=hit (no anim), `0x04`=guard/
>   block (anim `0x13`), `0x01/0x02/0x03`=guard variants (anim `0x12`/`0x13`), `0x06`=plain miss.
>   It is written together with result-code `+0x1BE` at `0x205B39` (`mov [rdi+0x1C0],ah`).
>   **This is normal, hookable memory** — the missing piece that lets us render a native
>   miss/dodge/parry/block on demand (the old MVP could not).
> - **Forecast hit% display:** virtualized math, **no stored % field**; only real-code lever is the
>   evade INPUT bytes (`+0x4B` class, `+0x4A/+0x4E` shield, `+0x46/+0x47` weapon). The engine
>   re-reads them into a scratch buffer (`0x7832E6`) each preview, so writing them moves the shown
>   number — but only to quantized cross-products. (A UI-render hook to substitute an arbitrary % is
>   under investigation.)
> - `+0x1E5` resultKind bits: `0x80`=damage, `0x40`/`0x10`=heal/MP, `0x08`=status, `0x01`=stat-change,
>   `0x20`=special/reaction.
>
> **Definitive recipe.** Let `unit = 0x1853CE0 + idx*0x200`; `actionObj = *(unit+0x148)`.
> - **force-HIT(D):** seed `word[unit+0x1C4]=D`, `word[unit+0x1C6]=0` (existing `0x30A66F` hook).
> - **force-MISS + native animation:** set `byte[result+0x1BE]=0` and evade-type `byte[actionObj+0x1C0]`
>   (`0x04`=block, `0x01/2/3`=guard, `0x06`=miss). New hook at selector `0x205210` (record=`[r8+0x148]`,
>   ExpectedBytes `48 89 5C 24 08 48 89 6C 24 10`) or at the staging write `0x205B39`.
> - **override-displayed-HIT%:** write the target evade input bytes pre-forecast (quantized), or hook
>   the UI render point (TBD). No arbitrary direct write exists.
>
> The strategy in §5/§6 still holds; only the dispatcher mechanism is corrected. Full round-2 detail:
> this box + the implementation plan in `work/` (selector observe probe).

## TL;DR — definitive conclusions

1. **We are NOT stuck.** The hit/miss/block/parry **decision is NOT fully virtualized.** Only the
   *roll arithmetic* lives in the Denuvo VM. The **outcome dispatch and the result/animation
   selection run in normal, hookable `.text`** — the same class of stable code as our proven
   pre-clamp hook (`0x30A66F`).
2. There are **two independent control levers**, cross-validated by static RE + FFT mechanics + the
   data layer:
   - **Lever A (data inputs):** write the unit's evade stat bytes / toggle ability `Evadeable` → the
     engine natively computes hit%, rolls, and **plays the correct dodge/parry/block animation.**
   - **Lever B (outcome hooks):** hook the non-virtualized **result dispatcher** (`0x38A6F1`) or write
     the per-unit **result record** (`+0x1E5`/`+0x1C4`/`+0x1BB`) to force hit vs miss/evade and drive
     the native popup/animation.
3. **Best path = hybrid:** Lever A makes the engine render native dodge/parry/block and approximate
   our DCL hit%; Lever B gives exactness and lets us force a specific outcome (incl. a **native miss
   animation**, which the old MVP could not do).
4. **Correction to a prior belief:** we **never actually captured a real miss/block in memory.** The
   "block clears +0x1C4 without firing the hook" finding was **misattributed forecast-teardown
   noise** (it recurs on every connect). So the old "can only convert hit→0-dmg-hit" ceiling is
   **superseded** — it was based on bad data.
5. The **displayed forecast hit% is genuinely not in the unit struct** (confirmed two ways). It is a
   UI/forecast-path value, consistent with a community report that IVC's on-screen hit% is "just a
   visual," separate from the rolled value. The forecast UI is built at `0x205210` (called from the
   `0x26Axxx` region) — that is where to look to **override the displayed hit% for our custom
   forecast.**

---

## 1. The breakthrough (binary RE — byte-verified)

Tooling: capstone 5.0.7 + pefile over `FFT_enhanced.exe`. Real code is section **`.xcode`**
(VA 0x1000, vsize 0x610000); the ~340 MB `.edata` is the Denuvo VM region. All anchors below are
**module RVAs** in `.xcode` (image base `0x140000000`), all in non-virtualized, ASLR-stable code —
hookable with `CreateAsmHook`, same as our existing `0x30A66F`.

### 1a. The per-unit struct IS an array (unifies our map)
Derived from the apply routine (`0x30A549 lea rdi,[image_base]` → `lea rdx,[rdi+0x1853CE0]` +
`shl idx,9`):

- **Unit/result-record array base: RVA `0x1853CE0` → runtime `0x141853CE0`.**
- **Stride `0x200` (512 B); ≤ 21 entries (bound-checked `idx < 0x15`).**
- Ramza (known ptr `0x141855CE0`) = **index 16** (`0x141853CE0 + 16*0x200`).
- ⇒ The "battle unit struct" we mapped (HP +0x30, evade +0x4B, staged dmg +0x1C4, resultKind +0x1E5)
  and the "result record" are **the same 0x200 struct.** Stats and per-action result share one record.

### 1b. DECISION BRANCH — apply (hit) vs miss/evade/status  → **FOUND, hookable**
Result-dispatcher function `0x38A4FC` loops over units; per unit it calls a producer and branches on
a packed outcome code `edx = (category<<8) | unit_idx`:

```
0x38A6D3  call 0x30F0C4        ; producer -> eax = idx | (category<<8)
0x38A6DF  and  edx, r15d       ; r15d = 0xFF00
0x38A6E8  cmp  edx, r15d
0x38A6EB  je   0x38A788        ; 0xFF00 = terminator (no more results)
0x38A6F1  cmp  edx, 0x0300     ; <<< OUTCOME-CATEGORY TEST (the decision branch)
0x38A6F7  jne  0x38A700
0x38A6F9  call 0x30A51C        ; category 0x300 => APPLY HP/DAMAGE (a HIT)
0x38A700  cmp  edx, 0x0200     ; -> handler 0x38ABBC  (miss/evade family)
0x38A736  cmp  edx, 0x0100     ; -> handler 0x38BBFC  (status family)
```
- `0x300` = **apply / hit** (HIGH confidence). `0x200` / `0x100` = other outcomes (miss/evade vs
  status — MEDIUM; their handlers are `E9` thunks into the VM, but **the dispatch is outside the VM**).
- Producer `0x30F0C4` (also non-virtualized) emits the category, e.g. `0x30F4B6 or r12d,0x300`.
- **Hook point:** `0x38A6F1`. The decision is in register **`edx`** (high byte = category). Force a
  hit by making `(edx&0xFF00)==0x300`; force a miss by routing to `0x200`.

### 1c. RESULT / ANIMATION SELECTOR  → **FOUND, hookable**
Function `0x205210`, called from the forecast/UI region (`0x26A683`, `0x26A7B1`, `0x26A92F`). Builds
the on-screen result/popup from the record:

```
0x205279  cmp byte [rdi+0x1BE], 0          ; is there a staged result?
0x205286  mov al,  byte [rdi+0x1E5]         ; <<< READ resultKind
0x20528C  test al,al / jns ...              ; bit7 (0x80) = DAMAGE vs not
0x2052BA  movsx eax, word [rdi+0x1C4]       ; <<< the DAMAGE NUMBER shown
          ; non-damage path tests resultKind bits 0x20/0x01/0x08/0x10 + per-sub-hit bytes +0x1D0..+0x1D5
```
- Keyed on **`+0x1E5` (resultKind, bit7=damage)** and reads **`+0x1C4` (word damage)**.
- **Hook/lever:** write the record's `+0x1E5` / `+0x1C4` before this reads them → controls both the
  popup type and the number shown. This is also the entry to investigate for **overriding the
  displayed hit% in the forecast** (the `0x26Axxx` callers build the preview).

### 1d. What stays virtualized (NOT hookable, and we don't need it)
Only the roll itself: gate `0x30FA34`, staging helpers `0x30BC3C` / `0x30BCF8` (all `E9` thunks into
`.edata`). We compute our own rolls anyway, so this is irrelevant.

---

## 2. The data lever (Lever A)

Evade inputs are ordinary struct bytes we already read/write (offsets within the 0x200 unit struct;
all CONFIRMED 5/5 in `battle-unit-struct-attribute-map.md`):

| Offset | Field |
|---|---|
| `+0x4B` | Physical/Class Evasion % (C.Ev) |
| `+0x4A` | Shield Physical Parry % (S.Ev) |
| `+0x4E` | Shield Magick Parry % |
| `+0x46/+0x47` | Weapon Parry R/L % (W.Ev) |
| `+0x44/+0x45` | Weapon Attack R/L |
| `+0x40 / +0x2B / +0x2D` | Speed / Brave / Faith |

Offline data tables (all editable via the mod loader): `JobData.xml <CharacterEvasion>` (→ +0x4B),
`ItemShieldData.xml`, `ItemAccessoryData.xml`, `ItemWeaponData.xml <Evasion>`, and
`AbilityData.xml AIBehaviorFlags` with the **`Evadeable`** master switch ("can this be dodged at
all"). Per-ability base hit% is exe-hardcoded (no data column) — but `Evadeable` off ⇒ engine hits
~100%.

**Granularity:** there is **no single "final hit%/evade" byte.** Evade is computed multiplicatively
from the separate %-bytes and truncated ⇒ the reachable set is **dense but quantized**, not arbitrary.
So Lever A approximates a target %; Lever B gives exactness.

---

## 3. Mechanics grounding (FFT canon, for prediction)

- **Physical hit%** (base = 100), truncated:
  - Front: `base·(100−Cev)(100−Sev)(100−Aev)(100−Wev) / 1e8`
  - Side: drops Class evade (`/1e6`); Rear: only Accessory evade (`/1e2`).
  - Facing **gates which evade sources apply** (NOT ×1.5/×3 multipliers — that framing was wrong).
- **Speed, PA, level, Brave, Faith do NOT affect physical hit%.** (Faith = magic only.)
- **Evade-type = several sequential rolls**, order **Accessory → RH → LH → Class**; the first success
  negates the hit and **determines which animation plays** (dodge/parry/block). Stored as one
  evade-type byte: `0x00=Hit`, nonzero = a specific avoidance method (PSX map: 0x01 accessory,
  0x02/0x03 hand-guard, 0x04 class/arrow, 0x06 miss, 0x0b Blade Grasp, …).
- **Animation is downstream of that byte** (jump table `evadeType<<2`). **Forcing evade-type/outcome
  = "hit" suppresses the evade animation.** This PSX model maps cleanly onto the IVC dispatcher in
  §1b (`0x300` hit vs `0x200`/`0x100`), strongly cross-validating it.
- ⚠️ Caveat: zeroing the 4 evade bytes (the "Concentrate" trick) does **not** stop *reaction* blocks
  (Blade Grasp/Catch) — those are a separate layer. To suppress ALL avoidance, force the final
  outcome, not just the input bytes.
- IVC engine = FFXVI/"Faith" engine (NOT UE5). Denuvo present but coexists with Reloaded modding.
  IVC's evade-type code map + animation dispatch are **not publicly documented** — our §1 RE is ahead
  of public knowledge and the PSX map is a strong-but-unverified prior for IVC.

---

## 4. Corpus correction (what our captures really show)

Full re-analysis of all logs (67 HP-events: 41 dmg + 26 heal — **all connects**):
- **No real miss/block was ever captured.** The "BLOCK signature" in `hitmiss_snapshots.md`
  (forecast set→cleared, HP unchanged, +0x1BB phase toggle) **recurs on ordinary connects** (events
  95/97, 122, 131/135 …) — it is forecast-slot teardown, **not** a block discriminator. Prior
  conclusion retracted.
- The recurring actor-dump floats are a **fixed coefficient block** at dump +0x3C0:
  `[1.0f | team-flag 0x10000 | 0.375f | 0.45f]`. `0.375`/`0.45` are **immutable engine constants**
  (identical across all Faith/Brave values ⇒ NOT faith/accuracy). `1.0f` varies by unit (1.0 ally;
  1.0/1.25 foe) ⇒ likely a zodiac/element multiplier. **None are hit%.**
- **Displayed hit% (50/100) is persisted nowhere** in the captures — not as int (0x32/0x64) nor
  float (0.5/1.0), in neither struct.

---

## 5. The plan (strategies + what's proven vs pending)

### Strategy A — let the engine roll & render (native visuals)
Write evade bytes / edit evade tables so the engine's hit% ≈ our DCL number; engine natively rolls
and plays dodge/parry/block. **Pros:** correct native visuals incl. evade-type. **Cons:** quantized;
accessory/magic-evade fields not yet located.

### Strategy B — decide ourselves, force the engine (exactness + forced outcomes)
Force vanilla always-hit (`Evadeable` off / zero evade), compute our DCL roll, then **either**
(a) hook `0x38A6F1` / write the record (`+0x1E5`,`+0x1C4`,`+0x1BB`) to route a DCL-miss to the
engine's native miss path (0x200) → **native miss animation**, **or** (b) fall back to 0-dmg hit.
**Pros:** arbitrary granularity; native miss animation now feasible. **Cons:** new hooks need live
validation; selecting *which* evade animation from the dispatcher is less certain than Lever A.

### Hybrid (recommended)
Lever A for native dodge/parry/block + approximate %, Lever B for exact % and forced outcomes.
Forecast display: override the preview hit% via the `0x205210`/`0x26Axxx` UI path (separate sub-task).

### Confidence ledger
- **CONFIRMED (static, byte-verified):** unit array `0x1853CE0`/0x200; dispatcher `0x38A4FC` +
  branch `0x38A6F1`; selector `0x205210` reads `+0x1E5`/`+0x1C4`; `0x300`=apply/hit; all in hookable
  `.xcode`.
- **HIGH (pending live):** writing the record / forcing `edx` controls outcome+animation; evade-byte
  writes move the shown hit%.
- **MEDIUM:** exact `0x200` vs `0x100` semantics; whether the producer leaves valid staged data when
  we force a category against the real roll (a forced "hit" on a real miss may have +0x1C4=0).
- **OPEN:** forecast hit% storage/override; accessory/magic evade offsets; IVC evade-type byte values.

---

## 6. Next steps

**Autonomous (no game needed) — finish the RE to "definitive":**
1. Disassemble deeper: nail `0x200`/`0x100` semantics; trace the producer `0x30F0C4` state machine to
   learn how it decides category from the (virtualized) roll result it reads, and **whether staged
   damage is present on the miss path** (determines the exact force-hit recipe).
2. Trace the `0x26Axxx` forecast callers of `0x205210` to find **where the displayed hit% is computed
   /stored** — the lever for the custom-forecast requirement.
3. Map the record's resultKind (`+0x1E5`) / outcome bytes to evade-types (cross-ref §3) so Lever B can
   select dodge vs parry vs block.
4. Draft the control probe/hook profile (observe-first, then a guarded single-unit force) for both
   levers, ready for the user to run.

**With the user (live validation):**
- **A/B evade-input test:** set target `+0x4B`=0 & parries=0 → expect shown hit ≈ base (~100);
  `+0x4B`=0x32 → ~half; shielded `+0x4A`=0x50 → block animation on evade. (Reads forecast only; no
  RNG consumed at preview.)
- **Dispatcher/record test:** with a guarded hook, force one attack to the miss path and confirm a
  native miss/evade animation fires; and force a hit and confirm damage applies.
- **The clean miss capture we still lack:** dump the actor/record on a *non-connect* boundary (the
  pre-clamp hook is bypassed on a miss, so add a dump at the dispatcher or on `0x200`).
  → **DONE (round 3):** implemented the observe-only **selector probe** at `0x205210` (captures the
  evade-type on every result incl. misses). Builds green; profile
  `work/battle-runtime-settings.result-selector-probe.json`. Ready to deploy + run.

---

## Round-3 — Forecast hit% display: DEFINITIVE verdict (virtualized)

The displayed forecast hit% is **NOT substitutable via a real-code integer hook**:
- **No stored hit% field** in the unit/result struct (confirmed a 3rd time).
- The two real-code number renderers — result-line selector `0x205210`, and the **combat-popup digit
  renderer `0x266AE0`** (a genuine real int→digit→glyph: value `[rdi+0x344]`, 3-digit split
  `0x2671BE`, glyph map `0x267350`) — source their numbers ONLY from staged combat fields
  (damage/heal/MP/status, via the popup mirror `0x3740200` fed solely from `+0x1C4..+0x1CA` /
  `+0x1D2..+0x1D7`). **Never a hit%.**
- The hit% computation is virtualized (`0x269760`/`0x2759F8`/`0x2B8F30`/`0x2740A0`), AND the UI
  primitives that would draw a % are themselves VM thunks (value-draw `0x2433A8`, text-setter
  `0x21F898`, layout `0x260134`). A sweep of the whole UI region for "clamp-to-100 then format"
  found only a clock formatter — no hit%-specific formatter exists in real code.
- Live scans: the % exists only as a transient float in heap UI buffers, never in a static struct.

**Three ways to satisfy "show a custom hit%":**
1. **(Recommended) Drive the evade inputs (Lever A):** write target `+0x4B/+0x4A/+0x4E/+0x46/+0x47`
   (or toggle `Evadeable`); the engine re-reads them into scratch `0x7832E6` every preview (no RNG
   consumed) → the native shown % moves. Quantized (dense cross-products), not arbitrary. This is the
   only real-code lever, and it doubles as the live-outcome lever (engine rolls + renders natively).
2. **(Arbitrary %, higher risk) Wrap a VM-thunk return:** trampoline a forecast call site
   (`0x26A618`/`0x26A746`/`0x26A89A` → `0x269760`) and overwrite the slot/register the VM leaves the
   % in — but the exact slot is NOT statically determinable; needs one live observe capture (dump
   `0x7832E6..0x783340` + `eax` around `0x26A618` while the preview shows a known number) to pin it.
3. **(Cosmetic) Overlay** our own glyphs via the real digit renderer instead of intercepting the
   native value.

---

## FINAL DEFINITIVE CONCLUSIONS (3 rounds, 8 agents)

**Q: Can we control miss / blocks / parries? — YES, definitively.**
- **Outcome (hit vs no-damage):** apply path fully reversed (`newHP = clamp(HP + heal − dmg)`, reading
  `+0x1C4`/`+0x1C6`); our existing `0x30A66F` hook already controls it. The roll is virtualized but
  irrelevant — we roll our own (DCL).
- **Native animation (dodge/parry/block/miss):** controlled by the evade-type byte `actionObj+0x1C0`
  (`actionObj=*(unit+0x148)`), read by the real-code selector `0x205210` (staged at `0x205B39`).
  Hookable & writable — **this was the missing piece.** An observe-only probe is **built (green)** to
  confirm the exact values live.
- **What stays virtualized (and we don't need):** the hit/evade roll + the displayed-% math.

**Q: Custom forecast hit%? — the display is virtualized**; achievable approximately via evade inputs
(real-code, quantized) or exactly via a live-pinned VM-return wrap / overlay.

**Net:** the project's long-standing "hit/miss outcome flag" blocker is **RESOLVED.** We have the exact
memory levers, the hook points, verified bytes, a guarded implementation plan, and a built observe
probe. Remaining work is **live validation + control-hook implementation — not investigation.**

## Live-validation handoff (needs in-game)
1. Deploy & run `work/battle-runtime-settings.result-selector-probe.json`; attack an evasive target
   front-on until a **miss/block** occurs; report outcome. → confirms the evade-type values (`+0x1C0`)
   for hit/miss/dodge/parry/block — the first clean miss capture.
2. A/B evade-input test (Lever A): set target evade bytes, read the forecast % → confirms inputs drive
   the native shown hit% and which evade animation renders.
3. (Optional, arbitrary forecast %) observe capture around `0x26A618` to pin the VM-return % slot.
Then: implement the guarded control hooks (force `+0x1C4` damage + `+0x1C0` evade-type) per the ready
plan (codemod-infra agent's spec).

---

## LIVE VALIDATION (2026-06-26) — observe probe at 0x205210

The observe-only selector hook (`[SELECTOR-PROBE]`) was deployed and fired. Captured events:

| # | Reported outcome | evadeType (cl / +0x1C0) | record | unit id | +1BB | +1BE | +1C4 (dmg) | +1E5 |
|---|---|---|---|---|---|---|---|---|
| 1 | Agrias→Ramza **hit** | **0x00** | 0x141855CE0 | 0x01 | 02 | 01 | 132 | 0x80 |
| 2 | Agrias→Ramza **hit** | **0x00** | 0x141855CE0 | 0x01 | 02 | 01 | (dmg) | 0x80 |
| 3 | Cloud→Beowulf **cloak evade** | **0x01** | 0x1418562E0 | 0x1F | 01 | 00 | 0 | 00 |
| 4 | Agrias→Ramza **shield parry** | **0x03** | 0x141855CE0 | 0x01 | 01 | 00 | 0 | 00 |

**Confirmed live:**
- The selector fires **exactly once per attack, at resolution** — no per-frame/preview spam. One
  attack = one event.
- `record = [r8+0x148]` IS the 0x200-stride unit struct (Beowulf 0x1418562E0 = Ramza 0x141855CE0 +
  3×0x200). The actor→unit link `actor+0x148` is real.
- **`+0x1C0` (== `cl`) is the animation lever.** Every evade shares `+1BE=00 / +1C4=0 / +1E5=00`;
  ONLY `+0x1C0` changes which evade animation renders (0x01 cloak vs 0x03 shield).
- A HIT is `+1BB=02 / +1BE=01 / +1C4=dmg / +1E5=0x80`.

**Validated enum:** 0x00 hit · 0x01 cloak/accessory evade · 0x03 shield parry/block.
**Still uncaptured (predicted):** 0x02 weapon/RH guard · 0x04 class evade ("Miss") · 0x06 plain miss.

**Status:** the control recipe is now LIVE-validated for hit + two evade types. Remaining: capture the
missing evade-type values (one front-on attack each on a weapon-parry / class-evade / pure-miss
target), then enable the guarded `+0x1C0`/`+0x1C4` control writes per the implementation plan.

**Update 2026-06-26 (event 5):** Agrias→Ramza **weapon parry** captured live →
`evadeType=0x02(guard-variant)`, record=0x141855CE0 (id 0x01), `+1BB=01 +1BE=00 +1C0=02 +1C4=0
+1E5=00`. Confirms predicted 0x02 = weapon parry. **Validated enum now: 0x00 hit · 0x01 cloak ·
0x02 weapon parry · 0x03 shield parry/block.** Still uncaptured: 0x04 class evade ("Miss"), 0x06
plain miss.

**Update 2026-06-26 (event 6):** Agrias→Ninja **class evade** captured live →
`evadeType=0x04`, unit id=0x80, `+1BB=01 +1BE=00 +1C0=04 +1C4=0 +1E5=00` (+1C2=FF). Confirms
0x04 = class evade ("Miss"). **Validated enum now: 0x00 hit · 0x01 cloak · 0x02 weapon parry ·
0x03 shield parry/block · 0x04 class evade. Only 0x06 (plain miss / failed ability accuracy) left.**

**Update 2026-06-26 (event 7):** Ramza→Ninja **Rend Helm evaded** → `evadeType=0x04` (class evade),
but `+1E5=01` (carried a stat/break effect) and `+1BA/+1BB=01`. NOT a new enum value: Rend Helm is an
"evadable" physical ability, so it rolls against the same class evasion as a basic Attack → 0x04. KEY:
`+0x1E5` (resultKind) reflects the action's *intended* effect kind (here 0x01 = stat/equip-break) even
when fully evaded with `+1C4=0` — i.e. evade is signalled ONLY by `+0x1C0` + `+1C4=0`, orthogonal to
`+0x1E5`. The hunt for 0x06 (plain miss / failed *accuracy* roll, e.g. Steal) is still open.

**Update 2026-06-26 (event 8) — ENUM COMPLETE:** Ramza→Blue Goblin **Steal Heart Miss** →
`evadeType=0x06`, unit id=0x82 team=3, `+1BB=01 +1BE=00 +1C0=06 +1C4=0 +1E5=00`. The failed-accuracy
miss DOES route through the selector (logged), using 0x06. **The evade-type enum is now 100%
LIVE-VALIDATED (6/6):**

| +0x1C0 | outcome | how to reproduce |
|---|---|---|
| 0x00 | HIT (damage applies) | any landing attack |
| 0x01 | cloak/accessory evade | target with evade accessory, front |
| 0x02 | weapon parry | target with parry weapon, front |
| 0x03 | shield parry/block | target with shield, front |
| 0x04 | class evade ("Miss") | bare high-C-EV job (Ninja), front; also evadable abilities (Rend Helm) |
| 0x06 | plain miss | failed ability accuracy roll (Steal Heart / status hit%) |

0x05 unobserved (gap, likely unused). Investigation phase CLOSED — remaining work is the guarded
control hook (write `+0x1C0` for the animation + `+0x1C4` for damage), per
`work/hit-miss-control-implementation-plan.md`.

---

## LIVE CONTROL PROOF (2026-06-26) — force-evade works end-to-end

The guarded control hook (extended selector hook at 0x205210) ran LIVE with
`Match=0x03(shield-block) -> Force=0x04(class-evade)`, `MaxWrites=1`. Captured event:

```
event=12 evadeType=0x03(shield-block) record=0x141855CE0 unit:id=0x01/team=0/hp=409
         rec+1BB=01 rec+1BE=00 rec+1C0=04 rec+1C4(dmg)=0 rec+1E5=00
         [CONTROL WROTE evadeType=0x04(class-evade) resultCode=--]
```

The engine naturally rolled a shield-block (`evadeType=0x03`, captured at hook entry from `cl`); our
hook matched it and overwrote `+0x1C0` (and the saved `cl` argument) to `0x04`. The dumped record
shows `rec+1C0=04` (post-write); HP stayed 409 (no damage, it was an evade). On screen, Ramza's
shield-block rendered as a class-evade "Miss" — confirmed by the user.

**This proves the evade-type byte `+0x1C0` (+ the `cl` argument) drives the on-screen animation:
writing it forces any of the 6 outcomes, and the engine renders the native animation.** Combined with
the pre-clamp damage hook (`+0x1C4` at 0x30A66F), the custom formula now owns hit/miss/block/parry AND
damage, while the engine renders natively and owns HP/KO. The hit/miss/block/parry control mandate is
COMPLETE and live-proven. Investigation AND first-control-implementation are both done; remaining work
is wiring the force decision to the DCL formula output (not RE).
