# DCL Mechanism Barrier Inventory — the definitive roadmap to implementation

**Date:** 2026-07-04 (unix 1783202375) · **Directive:** number calibration is easy and deferred;
what matters is that every engine capability the DCL design needs is **proven and reachable**.
This document classifies every combat mechanism the DCL requires and orders the work to close
every gap.

**Classification key**
- **PROVEN** — mechanism exists and is live-proven (LT cited).
- **BUILT-UNPROVEN** — code exists in the codemod, awaiting its live test.
- **NEEDS-RE** — the engine surface is not located/proven; states what must be found, best
  candidates with confidence, and the cheapest experiment.
- **CONFIG-ONLY** — no engine work; pure authoring (formulas, catalogs, data).

Sources: `docs/deep-combat-layer/*` (design demands), `docs/modding/06-code-mod-runtime-dsl.md`
§§DCL pipeline / hit control / miss output-control (LT6–LT9 truths),
`docs/modding/08-dcl-information-requirements.md`, `04-engine-memory-model.md`,
`05-reverse-engineering.md`, `work/1783184308-dcl-miss-consumption-and-counter-path.md`,
handoffs 2026-07-03-2207 §6 and 2026-07-04-1035.

---

## 0. The five proven pillars everything rides on

1. **Action context** — calc-entry `0x309A44` fires per (action, target) at preview, charge,
   execution, and AI think; `(casterIdx, actionType, abilityId)` cached per target. PROVEN (LT1/LT3/LT6).
2. **Damage authority** — pre-clamp managed callback `0x30A66F` rewrites the staged HP debit
   same-hit from the full (attacker, target, equipment, ability) formula context. PROVEN (LT6/LT7).
3. **Hit authority** — the mod computes its own hit%, rolls its own RNG at calc-entry, and forces
   the binary outcome: HIT via all-evade-zero (+ item-table evade kill, LT5-A4), MISS via
   output-control (force-connect + staged debit→0 + MP debit→0). PROVEN from any angle incl.
   monsters and AI attackers (LT8/LT9/LT9b).
4. **Unit/world state** — stats, raw/effective PA/MA/Speed, Brave/Faith, zodiac, gender, equipment
   slots, status arrays, immunities, position/facing (+0x4F/50/51, enum calibrated LT9), turn owner
   +0x1B8, tile table 0x140D8DCB0 (range/AoE membership readable). PROVEN (LT1/LT2).
5. **Preview paint** — forecast hit-% (`0x227FFE`) and forecast damage/heal + ghost bar
   (`obj+0x6/+0x8`) are deterministically writable. PROVEN (2026-06-27/28). *Not yet wired to DCL
   formulas* (LT6 observed vanilla numbers in the panel).

The engine rule that governs everything: **Denuvo virtualizes code, not data.** VM-internal
*rolls* are unhookable (refuted for Fire accuracy, Blind proc, Shirahadori); **inputs the VM reads
and outputs it stages are ours**. Output-control first (project rule).

---

## 1. Classification by mechanism

### 1.1 Physical damage (type-split base tables, subtractive DR, wound mults, pen floor, G)
**PROVEN.** LT7 (2026-07-04) ran the full GURPS-shaped model live: `thr/sw` off raw PA via
`DclDerivedVariables`, weapon Power, subtractive armor DR by damage type (DR tracked the target:
rod 19 vs Thief / 18 vs armored Ramza), wound multipliers, Brave trait scaling — 6 weapon hits
across 4 damage types, every UI HP drop == `[DCL]` debit. Over-cap skill→damage/penetration,
pen_floor, armor divisor, crossbow/gun `base(skill)`, fist_pen/MA_wmod are all expressible in the
same derived-variable chain.
**Remaining: CONFIG-ONLY** — author the DCL catalog (damage type per weapon, armor class + DR
matrix, Weight, wmod values, monster/fist synthetic rows — all MUST-AUTHOR per the 2026-07-02
coverage audit) and calibrate.

### 1.2 Physical hit/miss — delivery
**PROVEN.** LT8: authored 50% on basic attacks, 13/14 swings 1:1 with `[DCL-HIT]`; decision cache
with TTL; dual-wield swing-2 handling; fail-open toward HIT. LT9/LT9b: output-control misses from
side/rear and vs monsters (debit=0 on screen), CT-guard fixed, Mana-Shield MP leak fixed
(`+0x1C8` zeroing 5/5 — promotes the staged MP-debit word to Proven), AI attackers pass through
the authored hit%.
Facing math for front/side/back modifiers: attacker+target position/facing are already in the hit
context (`[DCL-HIT] ax/ay/af/tx/ty/tf`) — **CONFIG-ONLY** (write the arc math into
`DclHitChanceFormula`).

### 1.3 Physical miss — presentation
**NEEDS-RE.** A forced miss currently renders as a **"0" damage popup with the hit (even crit)
animation** (LT9 finding 1). The result-kind commit `0x205B38` fires **only for real evade
outcomes** (`+0x15C` bit-4 gate skips plain hits) — refuted as a per-execution commit, so under
force-connect there is nothing to intercept.
- **What must be found:** how to author the full "Miss" presentation on a VM-connected swing —
  co-write kind `+0x1C0`, the Family-2 bytes (`+0x1D2..+0x1D5`/`+0x1D8`), result flag `+0x1BE`,
  and zero `+0x1C4` at the finalize fn `0x2055FC` (LT5-C territory; cases A–E in
  `work/dcl-kind-paint-analysis.md`).
- **Confidence:** finalize site Strong (static); render classifier `0x205210` reads `+0x1BE`,
  `+0x1E5`, `+0x1C4` (Strong-static).
- **Cheapest experiment:** one battle with an ExecuteFirst co-write at `0x2055FC`-window on forced
  misses; observe which field set flips the popup/animation to "Miss".
- **Note:** cosmetic only — functionally the miss is already correct. Ship-blocking for
  "legibility" (design principle), not for mechanics.

### 1.4 Miss-consumption signal (per-swing independence after a MISS)
**NEEDS-RE.** V1 limitation: only a HIT is consumption-invalidated (pre-clamp rewrite); a forced
MISS stages no damage, and the `0x205B38` kind hook never fires under force-connect (LT9) — so a
same-key repeat inside the TTL (dual-wield swing 2 after swing-1 miss) reuses the MISS instead of
rolling independently. The two-consumer handshake (`DclHitDecisionCache.MarkConsumed`) is BUILT
and inert.
- **What must be found:** any once-per-executed-swing signal on a forced miss. Best candidate:
  make the presentation slice (1.3) fire the commit (the design doc says the kept hook becomes the
  signal once the commit fires); alternates: the result dispatcher `0x38A4FC` `edx==0x300` branch,
  or the apply-staging fn `0x30C798` (fires per applied record).
- **Cheapest experiment:** rides for free on the 1.3 experiment (watch `[DCL-KIND]` fire counts).
- **Interim mitigation (CONFIG):** short `DclHitDecisionTtlMs` bounds the window; per-swing
  UI-vs-log consistency is unaffected.

### 1.5 Crits and fumbles
- **DCL crit/fumble decision** (3d6-style windows keyed to skill, crit bypasses defense):
  **CONFIG-ONLY** — the mod owns the roll; crit/fumble are branches of the mod's own decision +
  a damage-formula multiplier. Event-seeded determinism = mod-side state (build, no RE).
- **Crit damage control:** **PROVEN** — the native crit multiplies the staged debit ×1.2 *before*
  the pre-clamp rewrite (LT8), so the DCL formula overwrites it; damage is always ours.
- **Native crit presentation:** **NEEDS-RE (cosmetic, deferrable)** — the engine still rolls its
  own crit animation independently of the DCL decision (LT9 saw a forced miss eat a native crit
  roll). To align the *label/animation* with DCL crits, the native crit trigger/flag must be found
  (likely near the same staged bundle; candidate fields in the `+0x1D0..` family). Cheapest
  experiment: log staged fields on natural crits vs normal hits at pre-clamp time, diff.

### 1.6 Magic damage (multiplicative spine, Faith², element_mult, G_m)
**PROVEN mechanism.** LT7 proved spells traverse the same pipeline with correct ability identity
(Fire id 16 resolved live via `AbilityCatalog`; charged-spell refires are idempotent; the
calc-entry cache — not the frame pointer — is the correct caster side, `[DCL-MISMATCH]`
documented). The LT7 profile deliberately passed spells through as vanilla (`actionType != 1`);
authoring the `11-magic.md` formula (base(MA) × spell_power × faith_c × faith_t × element ×
zodiac × G_m) is **CONFIG-ONLY** on proven inputs (MA, Faith both sides, ability element vars,
equipment affinity vars from the widened `ItemCatalog`).
**One gap:** healing spells route through the staged **credit** (`+0x1C6`, Proven for forecast +
apply); the DCL callback currently rewrites only the debit. Rewriting heals = small code
extension on the same hook (same pattern as the `+0x1C8` MP write) — **BUILT-UNPROVEN-adjacent**
(build + one live test; no RE).

### 1.7 Magic hit/evade (per-target Magic Evade, Faith, AoE-independent rolls)
**BUILT-UNPROVEN.** The hit-control layer is action-type-agnostic: decisions are keyed
per (caster, target, ability, type) and calc-entry fires per AoE victim (Proven), so per-target
Magic Evade is exactly `DclHitChanceFormula` branching on `action.type`/ability vars — but no
spell-gated hit profile has run live (LT8 forced `pct=100` for non-basic actions). Forced-miss
damage side works via output-control (action-independent). The VM's own magic-accuracy roll stays
force-connected (input stamps zero evade; magic accuracy roll for Fire is VM-internal — hooking
REFUTED, irrelevant under output-control).
**Cheapest test:** an LT8-style profile with `if(action.type != 1, 50, 100)` — one battle of
spells; PASS = per-victim independent outcomes inside one AoE cast.
**Open observable:** does a forced-missed *spell* stage other effects (status procs) — covered by
the status slice (1.9).

### 1.8 MP costs / MP damage / MP restoration
- **MP damage/redirect control:** **PROVEN** — staged MP-debit `unit+0x1C8` zeroed live 5/5
  (LT9b, Mana Shield incl. a 912 hit). MP-credit `+0x1CA` Strong (same apply clamp).
- **MP costs (per-ability):** **CONFIG-ONLY** — MP is in the WotL baseline CSV and
  `OverrideAbilityActionData` is a real sparse data patch layer (29 stock CT/MP overrides verified
  coherent); the per-battle-budget redesign is authoring on top.
- **Full MP formula authoring at runtime** (e.g. DCL-computed MP damage): small extension of the
  pre-clamp callback to write `+0x1C8/+0x1CA` from a formula — build, no RE.

### 1.9 Status infliction — ADD (the 3d6 contest) and native-proc suppression
**NEEDS-RE — the single largest uncovered outcome-authority group.** The native status roll is
VM-internal (Blind roll hook `0x30662C` refuted-in-practice; `g_7B07AC` poke refuted live). The
design (13-statuses-and-reactions) demands: replace % infliction with the mod's own 3d6 contest,
category-resolved resist (Brave / base-HP / inverse-Faith), and suppress native procs the DCL
didn't authorize (incl. on forced misses — the VM "believes it hit", LT9 open observable).
- **Proven pieces:** immunity-bit INPUT lever (`+0x5C..0x60` → 0% + miss, LT2); the staged-effect
  surface exists on the target — apply-mask `+0x1D0` (bit 8 = status), effect-kind `+0x1C0`,
  staged ailment id `+0x1A8`, all readable/writable in the compute→apply window (Strong static,
  the LT4 plan that was never run for status).
- **What must be found:** that rewriting `+0x1D0`/`+0x1A8` before apply suppresses/forces the
  status cleanly (with correct balloon/animation).
- **Cheapest experiment (the LT4-status test):** one battle with a status weapon/spell; pre-clamp
  callback logs then zeroes bit 8 of `+0x1D0` (suppress) on half the hits and leaves the rest —
  verify on-screen. A second pass forces a staged ailment on a plain hit.
- **Fallback if output fails:** per-action immunity-bit stamping at calc-entry (input-control,
  same pattern as evade stamps) — Strong, since immunity bits are already a proven input lever.

### 1.10 Status REMOVE (cure/dispel, timed durations for the reskins)
**NEEDS-RE (cheap).** Status force/cure by direct writes — OR/clear on master `+0x1EF` + effective
`+0x61` + source `+0x57` — is Strong (the arrays and the `0x30D42A` recompute rule are proven as
*observation*; Denuvo honors data writes) but has never been live-tested as a *write*.
- **Cheapest experiment:** live-poke Blind onto a clean unit via `+0x1EF|=0x20`+`+0x61|=0x20`,
  then clear it; verify balloon + behavior both ways. One battle, piggybacks on any other LT.
- Unlocks: DCL-owned durations (poller clears after N turns via turn-owner tracking — Proven
  input), the ~1-turn Stun/Knockdown reskins, dispel effects.
- **KO:** engine-owned death is REFUTED as a memory write; formula-owned KO through pre-clamp
  lethal damage is **PROVEN** (04 §6.2) — the DCL's only death path, and it is sufficient.

### 1.11 New statuses (Stun/Knockdown/Fear/Taunt/Interrupt)
- **Stun / Knockdown** (DA/DM reskins, ~1 turn): **CONFIG-ONLY + 1.10** (native flags exist; DCL
  sets category/duration at inflict time as mod state).
- **Taunt fallback** (1-turn Berserk): **CONFIG-ONLY + 1.10**.
- **Taunt ideal** (directed compulsion — retarget the AI at the taunter): **NEEDS-RE, EXPENSIVE**
  — AI target selection is VM-side scoring; no located steering surface. Design already carries
  the fallback; keep it.
- **Fear** (auto-flee + "no enemy-targeting actions" filter): flee = native Chicken-style behavior
  (CONFIG); the action-legality filter **NEEDS-RE, EXPENSIVE** (menu/AI legality surface not
  located). Nearest cheap approximation: Chicken reskin.
- **Interrupt** (cancel a charged action): **NEEDS-RE (cheap probe).** The pending record is owned
  (`+0x1A1` type, `+0x1A2` ability id, `+0x18D` charge timer, epicenter `+0x1AC/+0x1B0` — Strong,
  LT1-corroborated). Cheapest experiment: live-zero a charging unit's pending record fields and
  observe whether the charge cancels cleanly. If data-cancel fails, the lever is forcing the
  resolution to fizzle (hit-control MISS on the charged action's execution — already PROVEN).

### 1.12 Reactions / counters
The one combat damage path still outside DCL control. Three sub-problems:
- **(a) Trigger control:** **PROVEN.** The Brave-gate roll `0x30BE8B` is the one real-code combat
  roll observed live (`chance=61` = defender Brave); hookable/forceable (suppress=0 / always=100),
  plus the Brave `+0x2B` write lever (floor 10 — chicken guard `0x30A9BD`). Courage (∝Brave),
  Caution (∝inverse), Neutral (flat) triggers = force the gate from a mod formula —
  **mechanism proven, code not yet built.** Reaction-eval ability id readable at
  `word[0x14186AFF0]` (Proven). Reaction SET bitfield `+0x94..0x97` (Strong static).
- **(b) Damage control:** **NEEDS-RE (one probe, candidate in hand).** Counters bypass calc-entry
  `0x309A44` entirely (Strong-static: exactly two callers, both normal-pipeline; corroborated
  live LT6/LT7 `[DCL-MISS] no-calc-entry`) but their damage DOES hit the pre-clamp — today it
  falls through to vanilla safely. Missing piece: the counter's (attacker, ability) context at
  pre-clamp time. **Best candidate:** apply-staging fn `0x30C798` (sole extra apply caller;
  reads target `+0x1BC`, result bytes `+0x1E8/9`) — Strong (static); whether it is
  counter-specific and exposes the attacker is Hypothesis.
  **Cheapest experiment:** the already-specced read-only `DclCounterPathProbe` at `0x30C798`
  (head AOB `48 89 5C 24 08 57 48 83 EC 20 80 79 01 FF`) + existing probes, one battle with a
  Counter unit (`work/1783184308-...md` §Q2). Fallback if shared: frame-global attacker
  (`0x307E90` reads `[rip+0x15630D8]`) with the LT7 frame-vs-cache caveat.
- **(c) Outcome control (make a counter itself miss/hit):** downstream of (b) — once the reaction
  context is owned, the same output-control (staged debit→0 + presentation) applies; the
  *trigger* lever (a) can already suppress the counter outright, which covers most design needs
  (reach-2 escape-counter = suppress Counter when attacker distance > 1 — attacker/target
  positions are proven context). Shirahadori-style per-roll forcing beyond the Brave gate:
  REFUTED (VM-internal) — design within the Brave-gate + suppress/allow vocabulary.

### 1.13 Preview/forecast — "preview = result"
**PROVEN levers, wiring is a build task.** Hit-% paint `0x227FFE` (proven, deterministic,
same-redraw), forecast damage/heal `obj+0x6/+0x8` + ghost bar (proven — same fields the pre-clamp
rewrites), forecast object global + target ptr (proven). Calc-entry fires at preview-open with
the full (action, target) key (proven), so the preview can evaluate the *same* `DclHitChanceFormula`
+ damage formula and paint both. **Build:** wire the two paints to the DCL decision/damage caches
(replace `PreviewHitPctForcedValue`/`PreviewForecastPokeValue` constants with formula-fed
per-target values). Determinism note: hit preview shows a %, not the roll — the "preview = result"
principle binds *damage exactly* (proven: same formula, same staged field) and *odds honestly*.

### 1.14 Dual-wield / multi-hit independence
**PROVEN with one gap.** Engine resolves dual-wield as two separate pre-clamp events (Proven,
04 §5.2); LT6 rewrote both swings (45+45); after a HIT, invalidation-on-rewrite gives swing 2 a
fresh roll (LT8). **Gap:** after a MISS the second swing reuses the cached MISS within the TTL —
see 1.4 (miss-consumption NEEDS-RE). Per-strike damage penalty, per-strike crit windows:
CONFIG-ONLY once 1.4 closes. Guard-shredder depletion: see 1.16.

### 1.15 AoE / multi-target
**PROVEN.** AoE resolves as one pre-clamp event per victim under a constant (caster, ability)
(Proven); calc-entry fires per (action, target) incl. AI sweeps (Proven); tile epicenter on the
pending record + tile-table membership read (Proven, LT1). The engine exposes no hit index —
batch identity is DCL-owned state (group by caster+ability within a resolution window) —
**build, no RE**. Per-victim independent Magic Evade = 1.7.

### 1.16 Defense depletion ladder (Parry/Block deplete, best-defense auto-select, reset on turn)
**CONFIG + build on proven inputs — no RE.** Everything needed is proven: per-incoming-hit events
(calc-entry + pre-clamp), defender identity, weapon/shield metadata (W-EV/S-EV in `ItemCatalog`),
turn-owner transitions `+0x1B8` for the reset, battle lifecycle in the runtime registry. The
ladder is mod state feeding `DclHitChanceFormula` (defense value = best of non-depleted). The
native evade system is force-neutralized under output-control, so the DCL ladder fully replaces
it. Design's "Block covers ranged, Parry melee-only" = formula branching on weapon range vars.

### 1.17 Facing / positioning / reach
**CONFIG-ONLY.** Position + facing proven and already in the hit context (LT9 calibrated the enum:
0=−y 1=−x 2=+y 3=+x); front/side/back arithmetic, reach-1/2 rules, point-blank penalty are all
hit-formula math on `ax/ay/af/tx/ty/tf` + weapon range vars. The engine's own frontal-arc evade
quirk is irrelevant under output-control (LT9). **Exception — stop-hit** (free strike on
approach): **no engine surface exists** for injecting an out-of-turn attack on movement —
see §4 (impossible/expensive).

### 1.18 Knockback / positional effects
**Design demands no literal knockback** (design extraction: no forced-displacement damage
mechanic anywhere in the DCL docs; the modding docs likewise never map one). Fear-flee = native
behavior reuse (1.11). Position *writes* (`+0x4F/+0x50`) would likely be honored as data but are
untested and unneeded. **CONFIG-ONLY / N-A.** (If a future ability wants a push: NEEDS-RE, one
cheap poke test of position bytes + tile occupancy consistency.)

### 1.19 Equipment effects (elements, affinities, innate statuses, Weight, AC/DR, parry/block)
- **Catalog surface:** item id → full row incl. weapon range, elements,
  absorb/null/halve/weak/strong sets, status innate/immune/starting, procs — EXISTS in
  `item_catalog.csv`; the `ItemCatalogEntry` widening was completed and committed (handoff
  2026-07-03 §6.1). Formula exposure PROVEN (LT6/LT7 used `aslot.*`/`tslot.*` live).
- **Elemental interactions in DCL damage** (weak/resist/halve as multipliers): **CONFIG-ONLY**
  (the DCL formula reads affinity vars; native elemental math is irrelevant because the debit is
  overwritten).
- **Absorb-as-heal:** needs the debit→credit flip at pre-clamp (write `+0x1C6`, zero `+0x1C4`) —
  same in-place pattern as the proven MP write; **build + one live test, no RE**.
- **Innate/equipment statuses & immunities:** data columns exist; immunity bits proven; granting
  innate statuses at battle start = 1.10's write lever. **CONFIG + 1.10.**
- **Damage type / armor class / DR / Weight / wmod / synthetic monster-fist rows:** MUST-AUTHOR
  data — **CONFIG-ONLY**.
- **Weight → Dodge:** hit-formula math — **CONFIG-ONLY**. **Weight → Move:** needs a live write
  of Move `+0x42` (proven as a field, unproven as a write) — **cheap live poke test**
  (Denuvo-honors-data precedent makes this Strong).
- **Shield trap (recorded):** shield evade has NO single-byte struct lever (derived
  `MAX(+0x4A,+0x49)` at the record builders — LT5-A2 refuted); the item-table kill
  (`0x80FA90`) is the lever and is already part of the proven baseline stack.

### 1.20 Brave / Faith / Zodiac trait curves
**CONFIG-ONLY.** Brave/Faith raw+effective proven; zodiac nibble proven; gender flags proven.
Curves/bands/compat matrix live in `FormulaTables`/`FormulaMatrices` (proven mechanism, LT7 used
the Brave trait table live). Brave floor rule: never write Brave < 10 (chicken guard) — only
relevant to the *write* lever, not to formula reads.

### 1.21 Weapon skill growth (job grades, job level, char level)
**CONFIG-ONLY on proven inputs.** JP arrays proven (JP1 `+0xF0` spendable, JP2 `+0x11E` total);
job level derivable from JP2 vs `GeneralJob.RequiredJobExp` thresholds (Proven LT1); level `+0x29`
proven; job id proven. The `skill(grade, jobLevel, charLevel)` formula + job×family grade matrix
are settings tables. (Job-level nibbles `+0xE4..0xEE` remain Hypothesis — not needed given the
JP2 derivation.)

### 1.22 Turn / CT interactions
**PROVEN inputs, CONFIG for the rest.** Turn owner `+0x1B8` proven (exactly-one invariant);
CT `+0x41` proven as a field (CT-as-action-source REFUTED — never used as one); Speed→turn
frequency stays native (design keeps it); charge CT per ability = data (baseline CSV + override
layer). Depletion reset on own turn = 1.16 state machine on the proven turn-owner signal.

### 1.23 AI parity (enemies must evaluate DCL numbers, not vanilla)
**Half PROVEN, half NEEDS-RE (verify-first).**
- Hit side: **PROVEN** — AI actions traverse calc-entry, get DCL decisions, and pass through the
  authored hit% (LT3, LT9b). Input-side writes are AI-visible by construction.
- Damage side: the AI scores through the same per-(action,target) calc (Proven), but the DCL
  damage rewrite fires at the *apply* pre-clamp — 08 §7 flags display-paint and apply-time
  rewrites as an **AI blind spot** (AI may score vanilla damage while DCL applies different
  damage). LT7 observed pre-clamp refires during charged-spell *evaluation* loops, which hints
  the staging path may also run at think time — **unverified**.
- **Cheapest experiment:** log-only — correlate `[DCL]` pre-clamp fires against enemy think-time
  calc-entry sweeps in one battle (probes already exist). If the blind spot is real, the fix is a
  staged-value rewrite at the compute-point (the `0x281F8A` staged-bundle write is already
  LT4-proven as a lever) or accepting mis-scored AI (playable, not correct).

### 1.24 Monster actions
**CONFIG-ONLY.** Monster basic attacks use their own action types (`0xB0` Choco Beak, `0xB3`
Tackle, `0xB9` Claw et al. — LT8); formulas must branch on the full type set, and synthetic
"monster weapon" catalog rows are MUST-AUTHOR. Monsters proven to respect output-control misses
(LT9 Chocobo case).

### 1.25 Healing (runtime + forecast)
**PROVEN** for direct and delayed explicit heals (credit path + forecast `obj+0x8`). DCL-authored
heal *formulas* (Faith-scaled, undead inversion as authored content) = CONFIG + the 1.6 credit
extension for rewriting native heals.

---

## 2. Dependency-ordered build plan

Phase legend: (LT) = needs a user-gated live test; (RE) = offline RE first; (B) = pure build;
(C) = pure authoring. Items within a phase are parallel.

**Phase 0 — already standing** (LT6–LT9b): context, damage, hit, miss-damage, MP, facing.

**Phase 1 — close the outcome-authority gaps (all independent, batchable into 1–2 battles):**
1. **LT10-A (LT):** counter-path probe `0x30C798` (read-only, spec ready in
   `work/1783184308-...md`) → owns reaction (attacker, ability) context. *Unblocks 1.12b→c.*
2. **LT10-B (LT):** status output-control — log+rewrite staged `+0x1D0`/`+0x1A8` in the
   compute→apply window; piggyback the direct status force/cure poke (1.10) and the Move-write
   poke (1.19) in the same battle. *Unblocks the 3d6 contest, reskins, proc suppression, durations.*
3. **LT10-C (LT):** presentation finalize-slice at `0x2055FC` (kind + Family-2 + `+0x1C4`
   co-write on forced misses) → clean "Miss" render **and** revives the consumption signal.
   *Unblocks 1.3, 1.4, then full dual-wield miss independence (1.14).*

**Phase 2 — build on proven mechanisms (offline, no user gate until slice-ready):**
4. **Preview wiring (B):** feed `DclHitChanceFormula` → hit-% paint and the DCL damage formula →
   forecast poke, per hovered (attacker, target). *Delivers "preview = result".* (One confirm LT.)
5. **Defense-depletion state + facing/reach hit formulas (B+C):** the 1.16 ladder + 1.17 math.
6. **Credit-side rewrite (B):** heal + absorb-as-heal + formula-driven MP writes (1.6/1.8/1.19).
7. **Reaction trigger authoring (B):** Brave-gate forcing from a formula (courage/caution/neutral,
   escape-counter suppression by distance) — mechanism already proven, needs the callback.

**Phase 3 — verify the two systemic properties:**
8. **Magic slice (LT):** spell-gated hit% (per-AoE-victim Magic Evade) + the authored magic
   damage formula (1.7 + 1.6).
9. **AI-parity check (LT, log-only):** 1.23; remediate via compute-point staging only if refuted.

**Phase 4 — the long tail:**
10. **Interrupt probe (LT, cheap):** pending-record cancel (1.11).
11. **Native-crit presentation RE (RE+LT, cosmetic):** 1.5.
12. **DCL catalog authoring (C, parallel with everything):** damage types, DR matrix, Weight,
    wmod, spell powers, status categories, zodiac matrix, skill tables — plus calibration with
    Marcelo (explicitly deferred).

Rule carried from LT5: validate each delivery slice live before stacking the next layer.

---

## 3. The 3 highest-leverage next actions

1. **Run the status output-control live test (LT10-B, with the cure-write and Move-write pokes
   piggybacked).** Status add/remove is the largest outcome-authority group with zero live proof,
   and it gates the most design systems at once: the 3d6 contest, Stun/Knockdown/Fear/Taunt,
   equipment innate statuses, proc suppression on forced misses, durations. Candidates and the
   window are already Strong (static) — one battle decides.
2. **Run the counter-path probe (LT10-A, `0x30C798`).** Reactions are the only combat damage that
   the DCL cannot currently attribute or author; the probe is specced, read-only, and zero-risk,
   and its answer (counter-specific vs shared) picks between two already-designed ownership paths.
3. **Run the presentation finalize-slice (LT10-C at `0x2055FC`).** One experiment closes two gaps:
   the cosmetically-wrong forced miss (legibility is a design pillar) and the dead miss-consumption
   signal (the last hole in per-swing independence). All three actions are live tests that can be
   batched into a single user session (2 battles).

---

## 4. Refuted / impossible / very expensive — where the design must adapt

**Hard refutations (do not retry):**
- **Death by memory write** (HP=0 and/or KO bit) — REFUTED; death is engine-owned. The DCL's
  death path is lethal staged damage through the proven pre-clamp — sufficient; no design change.
- **Hooking VM-internal rolls** — magic accuracy (Fire), status proc (Blind), Shirahadori's
  reaction roll: all refuted-in-practice. The DCL never fights a VM roll; it writes inputs or
  rewrites outputs. Already the project rule; design costs nothing.
- **Shield evade as a struct byte** (`+0x4A`) — refuted (derived MAX at record build). Lever =
  item-table kill (in the proven baseline). No design impact.
- **Writable accuracy-% globals / `g_7B07AC` poke / evade-record overrides** — refuted;
  superseded by own-RNG + binary forcing.
- **`0x205B38` as a universal per-execution commit** — refuted (evade-outcomes only). Consumption
  signal must come via the presentation slice or another site (1.4).
- **CT as an action/source signal** — refuted for DCL use; action context comes from calc-entry.

**No engine surface located — expensive; design should adapt or keep its fallback:**
- **Stop-hit (attack-of-opportunity on approach).** FFT has no movement-triggered attack window
  and no located injection point for an out-of-turn action. Adapt: re-express as a
  reaction-family ability within the proven Brave-gate vocabulary (e.g. a Counter variant that
  the DCL allows only when the attacker *entered* reach this turn — position history is mod
  state), or cut. Design doc (`06-reach.md`) already frames it as an optional lancer ability.
- **Taunt ideal (directed AI retargeting).** AI target selection is VM-side; no steering surface.
  The design's own fallback (1-turn Berserk reskin) stands — ship it.
- **Fear's "no enemy-targeting actions" menu filter.** Action-legality/menu surface not located.
  Fallback: Chicken-style flee reskin delivers the fantasy minus the filter.
- **Injecting brand-new abilities/SKUs.** Consistent with the design's own "no new equipment"
  rule; ability behavior is authored by re-tuning existing ids + the override layer + DCL
  formulas keyed to ability id.
- **Native crit *animation* alignment** (1.5) — unknown surface; damage is already correct, so
  this is acceptable cosmetic debt until someone funds the RE.

**Watch-item (not refuted, unverified):** the **AI damage-scoring blind spot** (1.23). If real
and unfixable at the compute point, the design consequence is enemies occasionally
mis-evaluating (over/under-valuing attacks whose DCL damage differs greatly from vanilla) — a
quality cost, not a blocker; mitigate by keeping DCL damage within vanilla's order of magnitude
where AI competence matters.

---

## 5. Compact classification table

| # | Mechanism | Status | Key evidence / next step |
|---|-----------|--------|--------------------------|
| 1.1 | Physical damage pipeline (types, DR, mults, floor) | **PROVEN** (LT7) | author catalog + calibrate (C) |
| 1.2 | Physical hit/miss delivery (own RNG, any angle, AI) | **PROVEN** (LT8/LT9/LT9b) | facing math = C |
| 1.3 | Miss *presentation* (rendered "Miss") | **NEEDS-RE** | finalize-slice `0x2055FC` (LT10-C) |
| 1.4 | Miss-consumption signal (post-miss swing independence) | **NEEDS-RE** | rides LT10-C; TTL mitigates |
| 1.5 | Crits: decision C · damage PROVEN · native animation | **CONFIG / PROVEN / NEEDS-RE(cosmetic)** | staged-field diff on natural crits |
| 1.6 | Magic damage (incl. heal rewrite) | **PROVEN mech** (LT7) | heal=credit-write build+LT |
| 1.7 | Magic hit/evade (per-AoE-target, Faith) | **BUILT-UNPROVEN** | spell-gated LT8-style profile |
| 1.8 | MP damage / costs | **PROVEN** (LT9b) / **CONFIG** (override layer) | formula-MP = small build |
| 1.9 | Status ADD (3d6 contest, proc suppression) | **NEEDS-RE** | LT4-style staged `+0x1D0/+0x1A8` test (LT10-B) |
| 1.10 | Status REMOVE / durations / KO | **NEEDS-RE (cheap)** / KO **PROVEN** | poke force+cure in LT10-B |
| 1.11 | New statuses: Stun/KD/Taunt-fb **C**; Fear-filter, Taunt-ideal **expensive**; Interrupt probe | mixed | pending-record cancel poke |
| 1.12 | Reactions: trigger **PROVEN**; damage ctx **NEEDS-RE**; outcome follows | mixed | `0x30C798` probe (LT10-A) |
| 1.13 | Preview = result (hit% + damage paint) | **PROVEN levers**, wiring = build | Phase-2 #4 |
| 1.14 | Dual-wield/multi-hit independence | **PROVEN** except post-miss (→1.4) | — |
| 1.15 | AoE/multi-target (per-victim events, tiles) | **PROVEN** | batch state = build |
| 1.16 | Defense depletion ladder + reset-on-turn | **CONFIG+build** (all inputs proven) | Phase-2 #5 |
| 1.17 | Facing/reach/point-blank | **CONFIG-ONLY** | stop-hit → §4 |
| 1.18 | Knockback | **N/A** (design demands none) | — |
| 1.19 | Equipment effects (affinities, innates, Weight, DR) | **CONFIG** + absorb-heal build + Move-write poke | — |
| 1.20 | Brave/Faith/Zodiac curves | **CONFIG-ONLY** | — |
| 1.21 | Weapon-skill growth (grades, job level) | **CONFIG-ONLY** (JP2 derivation proven) | — |
| 1.22 | Turn/CT interactions | **PROVEN inputs** + C | — |
| 1.23 | AI parity (hit **PROVEN**; damage scoring) | **NEEDS-RE (verify)** | log-only correlation battle |
| 1.24 | Monster actions | **CONFIG-ONLY** | type set + synthetic rows |
| 1.25 | Healing runtime/forecast | **PROVEN** | DCL heal formulas = C |
