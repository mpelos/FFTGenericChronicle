# DCL Magic Validation — Report

Status: **CONVERGED — autonomous run; double adversarial sign-off (one cross-model) after 3 audit rounds**.
Subject: the magic system, confronted against the
physical system. Method: Phase A inventory (3 readers → register) · Phase B map + `philosophy.md` +
cross-model pillar attack (yardstick revised: M1–M6 + P3/P4 guards) · Phase C doubts (author + 1 cross-model
+ 2 fresh critics = ~138 doubts, two fresh critics independently converged on 8 roots) · Phase D
confrontation sims (5, each magic-vs-physical across the early→late × archetype × matchup × tempo spread +
sensitivity sweep) → verdicts below · Phase E synthesis · convergence auditor sign-off.

All decisions are conflict-of-interest (AI co-authored). Code output is ground truth; critics corroborate.
Per user directive this run is **100% autonomous** — design forks are resolved into recorded
recommendations, not surfaced as questions; the deliverable is this final report.

---

## Verdict table (the 8 converged roots + contradictions)

| id | root | sim | lean | one-line |
|----|------|-----|------|----------|
| MR-1 | caster supremacy via uncosted free bolt | sim_confront_supremacy | **HOLDS (calibration-gated)** | at G_m*≈0.65 bolt < fighter on every soft target; only beats evasive (anti-evasion, not anti-armor) |
| MR-2 | Faith one-sided per role (P4) | sim_confront_faith | **HOLDS (fragile); fix #8 UNSOUND** | slider live via heal-tax, but the proposed buff-Faith-scaling *shrinks* the window (high-magic still LOW, low-magic→HIGH); reframe high-magic-LOW as matchup |
| MR-3 | magic zeroes physical defense, no analog | sim_confront_supremacy → audit_aoe | **OVERTURNED → BREAKS (M2 unmet)** | AoE erases CTX-B (k=2 crossover T≈56); CTX-A not AI-executable → 0 robust AI-executable losing contexts; needs a design fix (sim_confront_aoe) |
| MR-4 | offense unbounded / damper guards wrong term | sim_confront_oneshot | **HOLDS everyday / REVISE reserves** | bands fine; pin cap to whole product + evade cap ≤~50% |
| MR-5 | heal ≈ damage stalemate | sim_confront_heal | **HOLDS (calibration-gated)** | same-tier heal/nuke = 1:1 wash; phys chip always uncovered; no-heal-floor → out-heal is a delayed loss; corner only at fat-MP + Faith-mismatch (27/810 cells) |
| MR-6 | base(MA) linear out-slopes physical late | sim_confront_supremacy | **HOLDS floor / WATCH burst** | bolt slope ≪ physical; the burst slope out-scales (pure calibration, MP-gated) |
| MR-7 | AI can run neither side (P10) | reasoned + docs/modding | **OVERTURNED → BREAKS as written** | AI-rush is a design aspiration, not engine fact (05-reverse-engineering.md:158 "out of scope"); UNVERIFIED pending a Windows AI test; M6 unmet |
| MR-8 | balance rests on undefined knobs + mage-favorable target | sim_confront_tempo | **HOLDS @ charge≥1 / BREAK @ charge=0** | P9 undecidable while charge unset; healthy window bolt_sp 0.6–0.8 + charge≥1; Speed is a *weaker* buy for casters (fear inverted) |
| **★G_m** | inherited G_m≈3 mis-scaled to the physical baseline → catastrophic if shipped | sim_confront_supremacy | **REVISE (load-bearing)** | re-derive G_m on the physical scale (≈0.65 vs the 11.49 anchor); with no structural damper this is the master knob |
| C-1..C-5 | contradictions (Faith floor 3-ways; permanent vs build-low; one-flag-two-stats; slider-vs-spreader; cross-track import) | doc-verifiable | **CONFIRMED (all 5)** | C-1≡C-5 (Faith-floor import); C-4 → resolve to centered band; C-2 → Faith permanent (mitigant); C-3 → calibration-watch |

---

## Per-decision verdicts

### MR-4 — Bounded multiplication / the damper (sim_confront_oneshot, agent a073c21d)
**Lean: HOLDS (everyday bands) / REVISE (two under-specified reserve paths).** Honest both ways — the
doubt critics' "no damper → blocking" is an **over-call for everyday play**; the real issue is two
unspecified reserves.

- **Everyday bands HOLD.** Worst realistic corner (zealot→zealot, weak ×1.30, no Shell) = faith² 1.69 ×
  1.30 = **2.20×** over neutral, under the ~2.5× cap (cap dormant). Vs a neutral-Faith fighter, only 1.69×.
  No one-shot of an unpaid target.
- **REVISE-1 — the soft-cap watches the wrong term.** With the rare ×2-weak element (or a widened Faith
  band) the corner is 3.38×. If the cap is on `element_mult` **only** (one reading of 11.26), it sees
  `element` (2.0 < 2.5) and **never fires**, so the faith²-driven spine multiplies uncapped → 3.38×. Cap on
  the **whole product** clamps to 2.50×. Robust across cap ∈ {2.0,2.5,3.0}. *Fix: `cap = min(cap,
  faith_c·faith_t·element·…)` — pin to the whole product.*
- **REVISE-2 — binary Magic-Evade composes into de-facto immunity.** Evade scales expected throughput by
  (1−p); casts-to-kill on the 0.70× wall go 3.2→6.3 (50%)→12.7 (75%)→31.7 (90%). Within a battle's burst
  budget, an **80% evade cap is de-facto magic immunity** — breaching "never immunity." The "capped <100%"
  guardrail bounds the evade term, never `evade × resist-stack`, and binary evade has **no out** (unlike the
  multiplicative wall). *Fix: cap evade ≤~47–50% vs the stack, or make evade partial not binary.*
- **Calibration reality (OQ-23 is literal):** with `base(MA)=MA` and the register's own G_m≈3, even a
  *neutral* Firaga ~one-shots a robe mage — absolute safety lives entirely in calibration of the unset
  `G_m`/`base(MA)`. The *shape* is fine; the *level* is unpinned.
- **What would flip it:** pin the cap to the whole product + keep ×2/absorb/band-widening out of the
  everyday band → offense HOLDS; shipping cap-on-`element_mult`-only or >80% evade → both corners go live.
- **Skipped (flagged):** Rod magic-mod as a possible *third* Faith application (14.3 — would widen the
  corner); the MP/charge cast-budget as the real brake on the evade-immunity corner; faith>85; absorb sign-flip.
- Evidence: `simulations/sim_confront_oneshot.py` + `_READ.md` (raw return: critic transcript in task a073c21d).

### MR-1 / MR-3 / MR-6 — caster supremacy, bolt, armor, late-scaling (sim_confront_supremacy, agent a0cdc444)
> **⚠ MR-3 OVERTURNED by the round-1 audit (GPT-1 / CL-1): the "≥2 common losing contexts → M2 satisfied"
> conclusion below is FALSE once AoE is included.** It rested on *single-target* sims; AoE erases CTX-B (k=2
> crossover T≈56, cluster output 1.74–2.48×) and CTX-A is not AI-executable (MR-7). **MR-1 (bolt floor), MR-6
> (floor slope), and the ★G_m scale finding still stand**; the supremacy/M2 conclusion does not. Revised
> verdict in Phase F; the fix is quantified by `sim_confront_aoe`.

**Headline (as originally written — MR-1/MR-6 parts stand; the MR-3/M2 part is overturned above): the breaks
are CALIBRATION-gated, not structural. The system is sound at the correct G_m*; the inherited G_m≈3 is
mis-scaled and would be catastrophic.** This corrects the doubt critics' "structural caster-supremacy"
over-call with hard numbers.

- **★ The scale finding (load-bearing REVISE).** The grill's G_m≈3 was calibrated against a different
  (≈8× hotter) fighter baseline; it is **not transferable** to the physical (sim_core) scale. Re-derived to
  the design's own anchor (11.49: mage battle-total ≈ fighter vs soft) on the physical scale, **G_m* ≈ 0.65**;
  literal G_m∈{2,3,4} sit 3–6× above balance. Plugging the inherited 3 into the physical scale makes the bolt
  *alone* 2.37–6.51× the fighter on every target — OQ-23/M4 made concrete. **Fix: re-derive G_m on the
  physical scale; with no structural damper (MR-4) this calibration is the master knob and must be locked.**
- **MR-1 (bolt = strictly-better basic attack) → HOLDS (calibration-gated).** At G_m*, bolt/fighter per turn
  **< 1 on every soft target** (mid: leather 0.66 / robe 0.57 / plate 0.71; late 0.52–0.65). Strictly-better
  = **False**. The bolt's only win is vs the **evasive thief** (1.07–1.41×) — anti-*evasion* (auto-land beats
  the dodge the fighter eats), **not** anti-armor. Sub-revise: a right-tool **crush** fighter beats the bolt
  vs plate, so 11.47's "depleted mage ≥ fighter vs plate" is true only against a *wrong-tool* fighter.
- **MR-3 (mage dominates the armored half, no losing context) → HOLDS (≥2 common losing contexts).** At G_m*
  the mage's edge is confined to plate (marginal 1.08× TTK) + the evasive target; the **fighter out-kills the
  mage vs robe** (TTK 8.6 vs 9.9). **CTX-A** — an evasive thief rushing the lone robe mage **wins from every
  start distance** (CT-tick duel, kills the 110-HP mage at tick 30–40), **scale-robust** (survives to G_m≈3,
  flips to the mage only at G_m≈4). **CTX-B** — attrition: the fighter overtakes vs soft at **T≥12** (T16:
  157 vs 178), scale-sensitive. The dedicated magic-resist slot cuts the bolt to 25%. → **M2's ≥2 common
  losing contexts are satisfied at G_m*, and the R-A↔P1′ risk (d26) is answered by CTX-A** — the fragile robe
  is rushable, an AI-executable counter (the FFT AI targets nearest/softest — see MR-7).
- **MR-6 (late-scaling) → HOLDS for the floor / WATCH the burst.** At G_m* the bolt slope (0.52) is far below
  physical (1.31–1.50; GURPS concavity only ~13% in-band, not a cliff). But the **burst** out-slopes physical
  even at G_m* (Firaga ~1.95) because magic slope = `ma_slope·spell_power·G_m` has no structural anchor →
  **WATCH: a late burst spike that out-scales, gated only by MP** (ties to MR-8/charge).
- **Sensitivity:** every conclusion monotone in G_m; first soft-break at G_m≈0.87/ma_slope 1.2, universal by
  G_m=2. **Flip:** raise G_m toward 2–4 (or a Rod mod lifting bolt power) → all three breaks reproduce together.
- **Skipped axes (named, all bias TOWARD magic):** AoE/multi-target, off-neutral Zodiac, full CT
  guard-refresh+interrupt, status/CC, healing, reactions — so the "balanced at G_m*" reads are *generous* to
  magic and still hold.
- **Net:** NOT the structural caster-supremacy break the doubt sweep asserted — **HOLDS at a correctly-scaled
  G_m**, with the required ≥2 common losing contexts present and AI-executable. Actionable: **REVISE — pin G_m
  to the physical scale + bound the burst slope**; the no-damper fragility (MR-4) makes this calibration
  load-bearing. Evidence: `simulations/sim_confront_supremacy.py` + `_READ.md`.
### MR-2 — Faith two-sidedness / P4 (sim_confront_faith, agent a27d497d)
**Lean: HOLDS (fragile).** Refutes the doubt critics' "Faith one-sided / P4 cosmetic" (the slider IS live)
but flags a real fragility + a clean fix.
- **Premise half-wrong:** low Faith is offense-free for a non-caster (offense rides Brave — confirmed flat),
  but **not** a free defensive dump — the locked "one Faith rule for all magic" (11.29/12.5) scales **healing
  received** ×0.70 → a **×1.43 heal-budget tax**. Low Faith is a TRADE (magic-defense + status-resist vs
  sustain efficiency), not a pure gain.
- **Non-caster → REFUTED.** Best Faith moves with magic-frequency × heal-reliance: low-magic → plate wants
  **MID**, skirmisher/monk want **HIGH** (cheap healing dominates; low Faith is the *worst* pick); med →
  LOW/MID/MID; high-magic → all LOW (the one confirming regime). Mid-Faith uniquely best in **3/18** cells —
  a genuine modest seam, not vestigial.
- **Atheist-tank heal paradox → real, quantified.** A healed low-Faith tank is a net drain for any battle
  with a physical component (the ×1.43 tax hits top-off of *all* damage; the magic refund only covers the
  magic fraction). Crossover **h*≈0.36**.
- **Caster → one-sided HIGH** — but that's the *intended* glass-cannon identity (accepted downside).
- **Fragility (the finding):** non-casters collapse to LOW in two regimes — high-magic battles and heal-light
  metas (h<~0.36) — and the DCL's own pillars (heroic full-HP, DR-primary armor, MP-scarce budget) **steer
  toward heal-light**, so the fragility lands where the design points.
- **Fix — CORRECTED after round-1 audit (GPT-3 / CL-3): the buff-Faith-scaling fix is UNSOUND as a rescue.**
  Running `sim_confront_faith.py` with buff-scaling ON: high-magic stays **all-LOW** (the regime it was meant
  to rescue) AND low-magic newly collapses to **all-HIGH** → the slider-live window **shrinks from two regimes
  to one**, it does not widen. So do NOT adopt "scale buffs on Faith" as the P4 rescue. **Reframe instead:**
  high-magic-battle-all-LOW is a *defensible matchup counter-pick* (vs a caster-heavy enemy, low Faith is the
  correct buy — not a universal dominant), so MR-2 still **HOLDS (fragile)** on the heal-tax keeping the slider
  live in low/mid-magic; the residual is that *heavy-magic* battles legitimately favour low Faith for everyone
  — matchup variance, not a P4 violation. Open item: whether any *additional* non-optional low-Faith cost is
  needed at all (likely not). Evidence: sim_confront_faith.py + _READ.md + critics round-1.
### MR-5 — heal vs damage stalemate (sim_confront_heal, agent abbceacd)
**Lean: HOLDS (calibration-gated, +1 logged feels-bad).** The shared spine does NOT auto-stalemate.
- **The shared-spine worry is literally true but self-cancelling.** Per-cast `heal == nuke` exactly at equal
  tier/Faith/MA/G_m (heal/nuke = 1.00 every phase). BUT target Faith scales nuke-taken AND heal-received → the
  magic halves cancel: `incoming(both) − heal = phys_dpt > 0`. A same-tier nuker **ties** a same-tier healer;
  the fighter's chip is **uncovered damage**. Only out-tiering (Curaga 144 vs Cura 86) pulls a healer ahead.
- **Corner reproduces only narrow + removable: 27 / 810 cells.** Every one needs *all three*: mix=both, a
  **fat MP budget (≥ a full battle of casts)**, AND a **Faith mismatch** (zealot medic out-Faithing a *neutral*
  same-tier nuker by +30%). The Faith-matched control collapses every cell → matched net = bare phys chip
  **+1.5 (early)…+8.4 (late), always >0 → wash restored**.
- **MP-gate delays, never prevents.** Heal and nuke share spine *and* gate, and with **no heal floor**
  (depleted healer → damage bolt, not heal) the healer can at best **tie** during the MP window, then drops to
  **zero** while the out-of-MP nuker keeps chipping at its bolt floor. *"On one spine, healing ties during MP
  and strictly loses the endgame."* Unkillable-vs-a-lone-fighter needs ~15 Cura-casts of budget (window ≥
  battle length) — the load-bearing number is **OQ-21 (MP pool size)**.
- **The scary r = heal/turn ÷ phys_dpt ≈ 6–35 is a cross-spine magnitude** (magic G_m=3 vs physical G=1) = the
  same mis-scaling ★G_m/MR-1 flagged — calibration, not a law, and it is the gated *burst*.
- **Atheist-tank inversion confirmed (feels-bad, but anti-degenerate).** Faith scales heal-received but not
  physical damage, so a low-Faith frontline tank is healed ×0.70 with no compensating resistance → **0.54× as
  heal-efficient as a zealot (46% less)**. The unit you most want to keep up is the least sustainable by *pure
  healing* — but that pushes *away* from an unkillable-tank corner (the tank survives by DR+HP, not topping).
  Log as a watch-item (ties the MR-2 atheist-tank paradox; same h*≈0.36 crossover).
- **Determinism asymmetry:** total on the magic side (exact tie) but **not** physical (landing variance;
  P(3 lands in a row)≈0.17 can spike past a mean-tuned heal), and the duel resolves *off* the damage axis
  (MP clock / interrupt / rush the fragile robe healer) — so it doesn't hard-lock unless the healer is *also*
  unkillable, which the design prevents.
- **Banking conditions (the calibration):** (1) strong-heal MP budget **< a full battle of casts** (window <
  L); (2) heal and nuke on the **same Faith band** (no structural out-Faithing); (3) preserve **no heal
  floor**; (4) keep the healer **killable/reachable**. **Flip:** a strong-heal budget that outlasts the battle,
  OR a heal-only band/G_m that lets a medic out-Faith/out-scale a same-tier nuker → the wash tips into a
  genuine unkillable corner. Evidence: sim_confront_heal.py + _READ.md.
### MR-8 + MR-1(economy) — caster tempo, the MP gate, the charge knob (sim_confront_tempo, agent afee08862)
- **MP-gate (does the free bolt make MP irrelevant?) → REVISE.** The free bolt does **57–85%** of a mage's
  single-target sustained output; the whole MP budget multiplies sustained single-target by only
  **1.18–1.75×** (back-loaded to Firaga). MP is a **weak single-target gate** — survives only if MP's value
  reads as **burst-timing + AoE + element**, not sustained chip. Sensitivity: bolt_sp 0.5 drops the depleted
  mage to 27% of a leather-fighter (dead-weight risk — ties M3/MR-3). *Fix: make MP's value AoE/burst/element
  (accept early/mid is bolt-floored by design); lowering the bolt to make MP bite re-opens dead-weight — same knob.*
- **Speed tempo → HOLDS (refuted, inverted).** Speed 6→12 = 2.0× turns; the fighter gets the full 2.00×, but
  a caster gets **Fira 2.00× (equal) / Firaga 1.56× / Firaga-AoE 1.19×** — never strictly more, because the MP
  budget caps bursts so extra Speed-turns fall to the weak bolt floor. **Speed is a *weaker* buy for a caster
  than a fighter** — the "Speed-stacking re-imported on magic" fear (d10/S3) is **inverted**.
- **Charge knob → BREAK at charge=0; HOLDS at ≥1, comfortable at 2.** Nuke/bolt per-turn: c0 **3.75×** / c1
  1.87× / c2 1.25×. **At charge=0 the late mage hits 128% of a fighter on soft AND 306% vs plate AND safely
  AND AoE → strictly best-in-game action economy.** charge≥1 buys the telegraph turn; charge=2 collapses the
  nuke to 1.25× the bolt. **P9-magic is undecidable while charge is unset**, and charge=0 is the single most
  dangerous unset value. *Fix: lock charge ≥1 on damage nukes + verify the telegraph is AI-punishable (MR-7/P10).*
- **The needle:** these are one tension on two knobs — **healthy window = bolt_sp ≈ 0.6–0.8 AND charge ≥ 1**;
  inside it all hold (M5 satisfied), outside (fighter-tier bolt or charge=0) all fail together. Current
  placeholders (bolt 0.8, charge 1) sit inside but at the edge. Evidence: sim_confront_tempo.py + _READ.md.
### MR-7 — AI can run neither side? (P10 / M6) — **OVERTURNED by the round-1 audit → BREAKS as written**
**Revised verdict: BREAKS (as written) / UNVERIFIED pending a Windows AI-targeting test.** The round-1 auditors
(GPT-2 / CL-2) + the AI-targeting research (`critics/ai-targeting-research.md`) show the load-bearing claim
below — "the FFT AI targets nearest/softest and rushes the fragile robe" — is **a design aspiration, not an
engine fact**: `docs/modding/05-reverse-engineering.md:158` says directed-aggression targeting is "out of scope
for now," and "auto-attack nearest" is documented only as a *Berserk* fallback. My own Phase-B critic
(`pillar-philosophy-attack-RESPONSE.md:23`) already recorded the vanilla AI does NOT reliably rush a backline
mage. So **M6 is unmet**: the baseline counter is not AI-native. The mage's fragility is real and a *human* (or
a 1v1 mechanic) kills it, but the **vanilla IVC AI is not documented to exploit it** — verification needs the
Windows live build (this Linux checkout cannot run the game). Combined with the AoE break (MR-3), "bring mages"
has **zero robust AI-executable losing contexts** as specced. *Fix: build the supremacy answer on AI-ACTUAL
behaviour (close-to-melee advance + focus-fire + the mage's MP/charge/range limits + encounter geometry — see
`sim_confront_aoe`), or an explicit Tier-2 AI-targeting code mod (which violates M6's "AI-native baseline"), or
ENTD encounter authoring; settle the backline-targeting question with a live Windows AI test.*

--- *(original reasoning, now overturned — retained for the record):*
The doubt: magic balance rests on counters only a human can execute → PvE fiction. Both halves have a native path.
- **Wielding magic — [AI-core] on the *balanced* part.** The mage's balanced power (per the sims) sits in the
  always-on **bolt** (a basic ranged attack the FFT AI runs trivially) and **single-target nukes** (vanilla AI
  casts these natively, incl. crude charge-lead). The part the FFT AI plays *badly* — optimal AoE placement
  (hit the cluster, spare allies) and MP-banking-for-the-burst — is exactly the **skipped-axis upside** that
  was already *generous to magic* in MR-1/3/6. The AI runs the balanced part and under-uses the bonus → not
  PvE-fiction on offense. Tag bolt + single-target nuke `[AI-core]`; optimal-AoE + MP-husbanding `[player-depth]`.
- **Answering magic — [AI-core] baseline is AI-native.** The #1 counter (CTX-A / MR-3) is **rush the fragile
  robe caster** — precisely the FFT AI's native behaviour (target nearest/softest; the 110-HP, 0-DR robe is the
  softest unit on the board). The sim showed the evasive rush wins from every start distance, scale-robust to
  G_m≈3. Baseline answer is AI-native → **M6 satisfied.** Interrupt-the-charge and spread-vs-AoE are
  `[player-depth]`; the design does **not** rest on them.
- **The author-gated counter (acceptable).** Per R-A the magic-resist *slot* is bought, and the FFT AI does not
  shop loadouts — so on PvE that counter is an **ENTD-authoring** decision (equip resist on key enemies), not
  AI-runtime. Acceptable *because* the rush counter is the AI-runtime baseline; the slot is upside, not floor.
- **WATCH (the residue):** if calibration pushes magic's core power into the **AoE-burst** corner (the part the
  AI plays worst), a human extracts disproportionately more from magic than the AI can — a PvE/player-skill
  asymmetry, not a balance break. *Fix:* keep the balanced power budget in the single-target/bolt band the AI
  runs; AoE = situational upside, not core budget; (MR-8) lock charge≥1 so the burst is telegraphed →
  AI-punishable. Ties MR-1 (bolt floor = AI-core), MR-8 (telegraph), MR-3 (rush counter).

---

## Phase E — Synthesis

### Headline
**The magic system is structurally sound but calibration-load-bearing.** Every one of the 8 converged
"blocking roots" the Phase-C doubt sweep raised (two fresh critics independently agreed on them) resolves —
under the confrontation sims — to **HOLDS** or **HOLDS-(calibration-gated)**. **None survives as a structural
break.** The doubt sweep systematically *over-called*: it read calibration fragility as architecture failure.
But the converse is the real finding: the design has **no structural damper** (OQ-23 is literal — magic is
pure multiplication), so its safety rests **entirely** on a small set of calibration locks. Ship them wrong
and the system is catastrophically broken; lock them and it holds with the intended matchup (M1–M6 satisfied:
≥2 common AI-executable losing contexts for "bring mages", a live two-sided Faith, a bounded stack, a heroic
but non-dominant bolt, AI-runnable on both sides).

**The master knob is ★G_m.** The inherited G_m≈3 was calibrated on a different (~8× hotter) fighter baseline
and is **not transferable** to the physical (sim_core) scale; re-derived there, **G_m* ≈ 0.65**. Shipping ≈3
makes the bolt *alone* 2.4–6.5× a fighter on every target. With no damper, this single number governs whether
magic is balanced or oppressive. This is the one finding that must not be missed.

### Contradictions — all 5 CONFIRMED (doc-verifiable)
- **C-1 ≡ C-5 — the Faith floor / cross-track import.** Three floors coexist: 08.4 imports a v0.2 effective
  floor **0.60**; 11.19 sets each application's band low-end **0.70**; the product of two low applications
  reaches **0.49** (< both). 08.4 is a cross-track import (v0.2 formula-balance) the DCL never re-derived on
  its own band math. **Fix:** drop the 0.60 import; let the DCL's explicit Faith band own the floor; **clamp
  the whole faith product** (not a term) — the same clamp-the-product fix as MR-4 REVISE-1.
- **C-2 — permanent trait vs build-dial.** The docs treat Faith both as a *permanent per-unit* two-sided trait
  (P4, like vanilla Brave/Faith) and as a *per-role build dial* (the MR-2 "best Faith per role"). In FFT Faith
  is permanent and only slowly driftable. **Fix:** state Faith is a **permanent per-unit trait** (a
  recruitment/assignment decision, not a per-battle respec). This is a **mitigant**, not just a cleanup: it
  denies the free atheist-tank respec that drives the MR-2/MR-5 low-Faith-tank concern.
- **C-3 — one-flag-two-stats (inverse-Faith concentration, S6).** A single magical-status flag (13.18)
  resolves against inverse-Faith for *both* landing AND the damage analog — so low Faith resists magic damage
  *and* most magical status on one stat. Doc 13 self-flags it a "calibration watch." **Triage:** accepted-risk
  pending the status-contest calibration; the MR-2 buff-Faith-scaling fix (below) offsets the concentration.
- **C-4 — slider vs spreader.** 08.2/08.5 frame high Faith monotonically ("take *more* magic", glass-cannon);
  11.19/11.20 frame each application as a band **centered at 1.0** (mid neutral, deviating both ways). **Fix:**
  adopt the **centered-band** model as the math (it is what makes Faith two-sided per P4 and is what the MR-2
  sim validated); the monotonic prose describes the *caster's* lived experience, not the formula — reconcile
  08.2/08.5's wording to the band.

### Fix-directions (prioritized — this is what to feed back into the design docs)
**BLOCKING (the calibration locks the whole system rests on):**
1. **★ Re-derive G_m on the physical (sim_core) scale** — target **≈0.65**, not the inherited ≈3. Master knob;
   no structural damper makes it load-bearing. *(MR-1/3/6, MR-4, MR-5, MR-8 all trace here.)*
2. **Pin the soft-cap to the whole product:** `cap = min(cap, faith_c·faith_t·element·…)`, not `element_mult`
   alone — else the faith²-driven spine multiplies uncapped to 3.38×. *(MR-4 REVISE-1.)*
3. **Cap Magic-Evade ≤ ~50% vs the stack** (or make it partial, not binary) — binary evade composes into
   de-facto magic immunity past ~80%, breaching "never immunity." *(MR-4 REVISE-2.)*
4. **Lock charge ≥ 1 on damage nukes** + verify the telegraph is AI-punishable — charge=0 makes the late mage
   strictly best-in-game (128% of a fighter on soft, 306% vs plate, safe, AoE). *(MR-8.)*
5. **Set the bolt at spell_power ≈ 0.6–0.8** — the window where it is a real heroic floor but not a
   strictly-better basic attack, and MP still governs sustained pressure. *(MR-1 / MR-8.)*

**HIGH (bound the un-anchored slopes / windows):**
6. **Bound the burst slope** (`ma_slope·spell_power·G_m` has no structural anchor) so the late burst does not
   out-scale physical gated only by MP. *(MR-6 WATCH.)*
7. **Keep the strong-heal MP budget < a full battle of casts** (window < battle length L); heal & nuke on the
   **same Faith band**; preserve the **no-heal-floor** rule. *(MR-5 — OQ-21 is the load-bearing number.)*

**MEDIUM (make the soft guarantees robust):**
8. **Make received buffs (Protect/Haste/Regen/Shell) scale on target-Faith like healing does** — keeps the
   Faith slider live in high-magic and heal-light metas (which the DCL's own pillars steer toward). *(MR-2 fix.)*
9. **Reconcile the Faith model** — drop the v0.2 0.60 import, centered band owns the floor, clamp the product,
   wording to the band. *(C-1/C-4/C-5.)*
10. **State Faith is a permanent per-unit trait** — denies the free atheist-tank respec. *(C-2; mitigates MR-2/MR-5.)*
11. **Keep core magic power in the single-target/bolt band the AI runs;** AoE = situational upside, not core
    budget. *(MR-7 WATCH.)*

### Doubts triage (the ~138 → disposition)
- **Resolved by sim** (the 8 roots + economy/tempo/heal/faith sub-tests): MR-1, MR-2, MR-3, MR-4 (everyday),
  MR-5, MR-6 (floor), MR-7, MR-8 (charge≥1) — all HOLD under their banking conditions above. The "structural
  caster-supremacy" cluster (d-roots) is **refuted as structural** and reclassified **calibration**.
- **Resolved by reconciliation** (contradictions): C-1/C-4/C-5 (one Faith-model + product-clamp), C-2
  (permanent trait). 
- **Accepted-risk (logged watch-items):** the atheist-tank heal inversion (MR-2/MR-5: feels-bad but
  anti-degenerate — it pushes *away* from an unkillable tank; contained by #8+#10); the inverse-Faith
  one-flag-two-stats concentration (C-3, contained by #8); the PvE/player-skill AoE asymmetry (MR-7, contained
  by #11).
- **Deferred-to-calibration (the BLOCKING fixes are *all* calibration):** items 1–7 above are the calibration
  targets; none is an architecture change. This is the whole verdict in one sentence — *the architecture
  passes; the numbers are the validation surface and they are not yet pinned.*

### Re-prioritized open questions (now the load-bearing calibration list)
1. **★ G_m on the physical scale** (OQ-17) — was "evidence toward 3"; now **known-wrong-as-imported**, target
   **≈0.65**. THE number.
2. **MP pool size / window vs battle length L** (OQ-21) — single number gating the heal-corner (MR-5) and the
   bolt-vs-burst economy (MR-8).
3. **base(MA) curve shape** (OQ-22) — co-determines the burst slope (#6).
4. **charge per nuke tier** — the ≥1 lock (MR-8).
5. **bolt spell_power** — 0.6–0.8 (MR-1).
6. **Faith band width + floor convention** (OQ-2/3/11/24; C-1) — band owns it, product clamped.
7. **soft-cap value + whole-product binding** (OQ-13; MR-4).
8. **Magic-Evade % values + the ≤50% cap** (OQ-15; MR-4).
9. **Zodiac/Shell band values** (OQ-6/12) — lower priority (modest bands).
10. **status-contest resist curves + caster-MA = base-or-total** (X4) — pending the dedicated status pass.

### Method note
All 8 roots + 5 contradictions were judged by **confrontation sims** (magic vs a physical baseline across
early→late × archetype × matchup × tempo, + G_m sensitivity sweep), each written/run by a fresh-context agent
that fixed the numbers and scenarios before the outcome was known; the report only interprets the scripts'
output. Every decision is conflict-of-interest (AI co-authored). **Convergence requires a fresh-context
adversarial auditor sign-off** (next) — until then this report is *proposed*, not final.

---

## Phase F — Convergence audit, ROUND 1: **NOT-CONVERGED** (must fix-and-re-run)

Two adversarial default-NO auditors were run on the Phase-E report: a **cross-model GPT peer** (`codex exec`,
`critics/convergence-auditor-gpt-RESPONSE.md`) and a **fresh-context Claude auditor** (agent a6e642fb,
`critics/convergence-auditor-claude-RESPONSE.md`). The GPT peer returned **NOT-CONVERGED** with 4 reproducible
breaks. Per the no-auditor-shopping rule these bind; I fix the scenarios, then re-audit. **(Claude auditor
verdict appended when it lands.)**

**GPT-1 — AoE erases the second losing context (the big one).** G_m*≈0.65 was anchored on *single-target*
soft parity (T10 mage ≈ fighter vs leather), but `philosophy.md` treats **AoE/clustering as a CORE magic
payoff**, not an optional axis. At G_m=0.652/mid/bolt_sp 0.8: single-target T16 = mage 157 vs fighter 178
(CTX-B holds). If the charged bursts hit **2 targets**, mage → 256 vs 178 (T10: 194 vs 111) → **CTX-B
erased**. Two-target parity needs G_m≈0.38, which collides with the heroic/anti-armor bolt floor (bolt → dead
weight). **My "skipped axis, generous to magic" framing was wrong**: AoE is the *primary* caster-supremacy
vector (philosophy.md names "AoE that's just more damage" a known failure mode requiring friendly-fire / enemy
spread), and the single-target G_m anchor doesn't bound it. → **Undermines MR-3/M2 + the G_m fix.** *Fix:
new `sim_confront_aoe` — give AoE a real cost (friendly-fire risk + finite size + charge + enemy-spread) and
co-calibrate G_m/AoE so ≥2 common losing contexts survive WITH AoE live.*

**GPT-2 — MR-7 is assumed, not earned.** P10/M6 rests on the **actual IVC AI** executing "rush the fragile
caster," but the bundle has no engine trace / targeting model. Repro: 110-HP/0-DR robe 5 tiles behind a
nearer plate unit + enemy thief Speed 10 — does the AI ignore the nearer target and rush the mage? If not,
CTX-A is not AI-executable and (with GPT-1) magic has **no** remaining common losing context. *Fix: substantiate
IVC AI targeting from `docs/modding/` or downgrade MR-7's CTX-A to a HYPOTHESIS pending a named Windows
AI-targeting test (this Linux checkout cannot run the game).* 

**GPT-3 — MR-2 buff-fix overclaimed.** Running `sim_confront_faith.py` with buff-scaling ON: low-magic → all
HIGH, **high-magic → all LOW** (still). The fix rescues low/mid-magic + heal-light, but **does NOT keep the
high-magic regime live** as I claimed — an internal inconsistency. *Fix: correct the MR-2 verdict — the fix is
partial; reframe high-magic-all-LOW as a defensible **matchup counter-pick** (vs caster-heavy enemies low
Faith is correct, not universally dominant) rather than claim a full rescue, and verify that reframing meets
P4.*

**GPT-4 — the "healthy window" is not a JOINT validation.** The blocking fixes are still calibration *targets*,
and the assembled window draws on sims at **different scales** (tempo used the hot G_m=3 / fighter 90·37.5;
supremacy says that scale is non-transferable). No single physical-scale run pins all locks together. → **MR-8/P9
not a completed convergence claim.** *Fix: new `sim_confront_joint` — pin G_m≈0.65 ∧ charge≥1 ∧ bolt 0.6–0.8 ∧
evade≤0.5 ∧ cap-on-product ∧ MP-window<L on the ONE physical scale, run the full matchup, and sweep to show the
joint safe region is non-empty AND has width.*

GPT explicitly **did not** reject G_m≈0.65 as circular ("it is derived from the stated soft-target parity
anchor") — it rejects it as **insufficient** because the anchor excludes AoE.

**Fresh Claude auditor (agent a6e642fb) — also NOT-CONVERGED, INDEPENDENTLY same two decisive gaps** (it wrote
its scripts *before* reading the GPT peer). It sharpened them and resolved GPT-4 in the design's favour:
- **CL-1 ≡ GPT-1 (decisive).** `simulations/audit_aoe_ctxB.py`: at G_m*=0.65/mid/bolt_sp 0.8, charged bursts
  hitting k targets → CTX-B crossover **T≈11 (k=1, holds) / T≈56 (k=2, no battle lasts that long → erased) /
  never (k=3)**; the mage's real cluster output is **1.74–2.48×** the single-target anchor it was calibrated
  "balanced" to. Flips MR-3, undermines M2 + BLOCKING fix #1.
- **CL-2 ≡ GPT-2 (decisive).** Exact doc cite: `docs/modding/05-reverse-engineering.md:158` — directed
  aggression "requires understanding/modifying the game's targeting AI **(out of scope for now)**"; "auto-attack
  nearest" is documented **only** as a *Berserk-status* fallback, never baseline targeting. So MR-7's
  AI-native rush **contradicts the repo's own engine docs.** It also checked the other leg — a screening
  frontline does NOT mechanically save the mage — so CTX-A is robust as a *1v1 mechanic* but **unproven as AI
  behaviour.** With CTX-B gone, "bring mages" has **zero robust AI-executable losing contexts.**
- **CL-3 ≡ GPT-3.** `sim_confront_faith.py` with buff-scaling ON: high-magic stays **all-LOW** *and* low-magic
  newly collapses to **all-HIGH** — fix #8 **shrinks** the slider-live window from two regimes to one (worse,
  not better). Undermines fix #8.
- **CL-4 — GPT-4 RESOLVED (design WIN).** The joint knife-edge attack **failed**: `audit_joint_locks.py`
  re-derives G_m* per config and checks all continuous locks at once → **22/24 JOINT-PASS**, the whole bolt_sp
  0.6–0.8 window passing with **+53–90% one-sided G_m drift margin.** The system is **not** a knife-edge;
  MR-4/MR-5/MR-8 genuinely hold with their fixes, jointly, on one scale. So `sim_confront_joint` is **no longer
  needed** — the auditor already proved the joint region is wide.

**Both auditors converge on the same verdict, and it is the run's real finding:** the architecture is sound and
the calibration is *wide and healthy* on every axis EXCEPT the load-bearing one — the **#1 chartered failure
(caster supremacy) is UNMET as specced**, for exactly philosophy.md's two named failure modes: (1) AoE is
"just more damage" (excluded from the G_m anchor, and the real supremacy vector), and (2) the answer leans on
an AI behaviour the engine does not have. This is NOT calibration-fixable — it needs design work.

**Round-2 plan (revised):** `sim_confront_aoe` (quantify the break + the fix levers — running, agent a32978ee)
→ rewrite the headline + MR-3/MR-7/MR-2 verdicts + fold in the joint WIN → re-run both auditors on the
corrected report. (`sim_confront_joint` dropped — subsumed by `audit_joint_locks.py`.)

---

## Phase G — CORRECTED FINAL VERDICT (post `sim_confront_aoe`, agent a32978ee)

*This section is the operative verdict; it supersedes the overturned parts of Phase E (MR-3/M2 supremacy, MR-7,
MR-2 fix #8). Phase E/F are retained as the auditable journey.*

### Headline (corrected)
The DCL magic **architecture is sound and its calibration is wide and healthy on every axis except one** — and
that one is the axis the run was chartered to hunt. The bounded-multiplication stack (MR-4), heal
non-stalemate (MR-5), charge/bolt economy window (MR-8), and Faith two-sidedness (MR-2) all **HOLD jointly** on
one physical scale with **+53–90% calibration margin** (`audit_joint_locks.py`, 22/24 joint-pass). The **single
load-bearing failure is caster supremacy via uncosted AoE**: G_m was anchored on *single-target* parity, but
Fira/Firaga are AoE (the design's core clustering payoff), so at G_m*=0.65 the mage's real cluster output is
**1.74×–2.48×** the fighter it was "balanced" against — **erasing the attrition losing context** (k=2 crossover
T≈57, k=3 never) — while the other claimed counter (the AI rushing the fragile caster) **is not a behaviour the
vanilla IVC engine has** (`docs/modding/05-reverse-engineering.md:158`, targeting AI "out of scope"). As
specced, **M2 fails — "bring mages" has zero robust AI-executable losing contexts.** This is exactly
philosophy.md's two named failure modes (AoE-that's-just-more-damage + the AI-answerability gap).

**But it is fixable — by a concrete design change, not calibration alone.** `sim_confront_aoe` found the region:

### The supremacy fix (fork resolved into a recommendation)
**Add a per-target MP cost to AoE + re-anchor G_m to realized-cluster parity + keep charge≥1:**
- **Per-target MP cost** `mp_cost = base · (1 + 0.5·(k−1))` (λ≈0.5): a 2-target Fira costs 1.5× MP, 3-target 2×.
  Bursts drain the MP budget faster → cluster output drops 1.74×/2.48× → **1.18×/1.57×**, and the MP-attrition
  crossover returns from NEVER to inside a battle. **Load-bearing and AI-independent** — it works off the
  mage's own resource clock, not the AI's smarts.
- **Re-anchor G_m ≈ 0.55** (realized-2-cluster parity), NOT single-target 0.65. Blended-G_m re-anchor *alone*
  fails (bolt floor collides into dead-weight at E[k]≥1.5) — *that's why* it must pair with the AoE cost.
- **Keep charge ≥ 1** (do NOT add +1 to AoE — over-nerfs).

### Scorecard — M2 satisfied by the fix *(SUPERSEDED by Phase H → M2 is CONDITIONAL on the Windows reachability test; MP-attrition is not stage-robust)*
Two **robust, AI-ACTUAL** losing contexts (no AI smart-targeting needed):
1. **MP-attrition** — the mage front-loads bursts then floors to the weak bolt; a fighter out-sustains a long
   fight (k=2 crossover ~T10–14, k=3 ~T18). Driven purely by the mage's own MP budget.
2. **Reachability** — once the AI's *normal* close-to-melee advance reaches the exposed 110-HP/0-DR mage
   (encounter geometry, not a smart rush), a flanker kills it in ~2.7 turns. The AI-actual replacement for CTX-A.
Plus a conditional 3rd: spread enemy team + bought magic-evade (mage 0.69×). **Identity preserved:** bolt floor
0.56 (≥0.5 → still heroic); clustering still pays 1.18×/1.57× (AoE stays the right tool vs clusters); anti-armor
kept vs wrong-tool (1.44×) and at cluster scale (1.08×). The fix bounds supremacy **without** flattening the
mage's three-part fantasy.

### Residual dependencies this Linux checkout cannot close (honest, surfaced)
1. **The reachability leg is encounter-authored, not engine-proven.** It assumes the vanilla AI's
   close-to-melee + focus-fire actually reaches/kills an exposed caster when geometry allows, rather than
   tunnelling nearer targets. **Single biggest open dependency; verifiable only on the live Windows build.**
   Repro there: 110-HP/0-DR robe 5 tiles behind a nearer plate ally, enemy flanker Speed 10 — does the AI reach
   and kill the mage inside the MP-burst window? If it reliably does NOT, escalate to ENTD encounter design
   (force caster exposure) or a Tier-2 AI-targeting code mod.
2. **G_m≈0.55 sits at exact 2-cluster parity — an ill-conditioned point** (cumulative attrition curves hug; read
   the crossover as a range ~T10–15). Needs live fine-tuning.

### Corrected fix-directions (design + calibration, re-prioritized)
**BLOCKING — DESIGN (new — the supremacy answer; NOT calibration):**
- **#0 — Cost AoE per-target (λ≈0.5) + re-anchor G_m≈0.55 + charge≥1.** Without it, M2 fails. *(MR-3/MR-7/#1 failure.)*

**BLOCKING — CALIBRATION (stand from Phase E; joint-proven wide margin):**
- **#1** soft-cap pinned to the **whole product** (MR-4). **#2** Magic-Evade ≤~50% / partial (MR-4). **#3** bolt
  spell_power 0.6–0.8 (MR-1; bolt floor 0.56 at G_m 0.55, still ≥0.5). *(G_m folds into #0 — 0.55, not 0.65.)*

**HIGH / MEDIUM:** bound the burst slope (MR-6); MP-window < battle length (MR-5 — now doubly load-bearing, it's
also the AoE-attrition brake); **DROP** the buff-Faith-scaling "fix" (old #8 — unsound) and reframe
high-magic-LOW as matchup variance (MR-2); reconcile the Faith model + permanent-trait (C-1/C-4/C-5/C-2).

**RESIDUAL (Windows live test — cannot close here):** confirm the AI reaches exposed casters (reachability leg);
fine-tune G_m around the 2-cluster parity.

### Bottom line
**The magic system is NOT shippable as specced** (caster supremacy unrefuted), **but it is one design change +
the Phase-E calibration locks away from sound:** add the per-target AoE MP cost, re-anchor G_m to cluster
parity (~0.55), apply the calibration locks (all joint-proven to have wide margin), and confirm the
reachability leg on Windows. **Every axis other than AoE/AI supremacy survived the full adversarial pass with
healthy margin.** That is the deliverable.

*Convergence status: Phase-G correction written; **re-running both adversarial auditors** on it next. Until they
sign off on the corrected verdict, this remains proposed, not final.*

> **⚠ SUPERSEDED IN PART by Phase H.** The round-2 audit (both auditors, independently) credited that the fix
> holds for the central mid/late neutral cluster but found Phase G **overclaims "M2 satisfied"** (it is
> CONDITIONAL on the Windows reachability test) and **left the early-cluster corner unsurfaced**. The operative
> scorecard + bottom line are restated honestly in **Phase H** below.

---

## Phase H — Round-2 audit response & the FINAL honest verdict

Both round-2 auditors — cross-model GPT peer (`convergence-auditor-r2-gpt-RESPONSE.md`) and fresh Claude
(`convergence-auditor-r2-claude-RESPONSE.md`, re-ran every script + wrote 4 attack scripts) — returned
**NOT-CONVERGED**, and **independently agree** on what's wrong and what holds. Captured in
`critics/round2-audit-notes.md`.

### What HELD under direct round-2 attack (credit to the design)
Both auditors *tried and failed* to break these: the **mid/late neutral cluster is genuinely bounded** by the
fix (k=2 crossover T10, k=3 T18, k≥4 T15–25, multi-mage linear — no runaway); the **heroic bolt floor survives**
(0.56 ≥ 0.5); the **joint-calibration margin is wide** (+53–90%, 22/24); the **per-target MP cost is the right,
AI-independent lever**. The fix is *directionally correct* and the central case is real.

### The two binding corrections
**H-1 (decisive, honesty): M2 is CONDITIONAL, not satisfied.** Of the fix's two scorecard legs, only
**MP-attrition is AI-independent**; **reachability is the SAME mechanic as the disqualified CTX-A** ("the
AI-actual replacement for CTX-A") carrying the *same* unproven trigger — the vanilla IVC AI is not documented
to reach/kill a back-line caster (`ai-targeting-research.md`; `05-reverse-engineering.md:158`). So the
unconditional AI-independent context count is **≤1**, and **M2 is met only IFF the Windows reachability test
passes.** If it fails, M2 falls to MP-attrition alone — insufficient — and must escalate to ENTD encounter
design (force caster exposure) or a Tier-2 AI-targeting code mod.

**H-2 (new, correctness): MP-attrition — the sole AI-independent leg — is NOT stage-robust.** The realized
2-cluster-parity G_m **drifts with stat** (early **0.41** → mid 0.55 → late 0.64), so a single flat G_m=0.55
leaves the **early** mage hot on clusters: at the shipped values (G_m=0.553, pool=80, λ=0.5) the early-vs-leather
crossover is **k=2 → T45, k=3 → NEVER** (early@T12 mage/fighter = 1.39/1.61× — mage AHEAD), versus mid k=2 T10 /
k=3 T18 and late k=2 T8 / k=3 T13. **In the common early-game-cluster regime the one AI-independent context is
absent → zero robust AI-independent contexts there.** *Remediation (auditor-verified): the MP pool must be
**stage-scaled**, not flat — a realistic smaller early pool (≈40) pulls the early k=2 crossover T45→T15.* So the
fix is **stage-aware**: small early MP budget, growing with progression (which also matches FFT's natural MP curve).

### The complete fix (stage-aware) — fork resolved
1. **Per-target MP cost on AoE** `mp_cost = base·(1 + 0.5·(k−1))` (λ≈0.5) — the load-bearing, AI-independent lever.
2. **G_m re-anchored to realized-cluster parity, STAGE-SCALED** (≈0.41 early → ≈0.55 mid → ≈0.64 late), **not** a
   flat 0.55 — equivalently, scale the **MP pool** with progression (≈40 early) so early AoE bursts deplete fast.
3. **charge ≥ 1** on damage nukes (do not add +1 to AoE — over-nerfs).

### Corners the central fix does NOT bound on its own (each enumerated with its lever — so the verdict cannot be overclaimed)
- **Early-game clustered fights** → stage-scaled MP pool / G_m (H-2; pool≈40 → crossover T15). *Calibration.*
- **Off-neutral (Zodiac/element-weak) cluster** → the neutral 1.18×/1.57× bound inflates to **1.30×/1.73×** when
  the mage hits a *shared-weakness* cluster (both auditors). *Resolution: accept as a legitimate winning matchup
  — enemy teams are rarely elementally uniform, so "nuke a same-element weak cluster" is a rare reward, not a
  universal answer (P1′ permits strong-not-universal); rely on mixed-affinity encounters; band-tighten only if
  playtest shows uniform-weak clusters are common. Ruling + calibration-watch.*
- **AI reachability** → the named **Windows AI-targeting test** (110-HP/0-DR robe 5 tiles behind a nearer plate,
  enemy flanker Speed 10: does the AI reach/kill the mage in the burst window, or tunnel the nearer unit?).
  *Cannot be closed on this Linux checkout.*
- **Multi-mage stacking / status-AoE** → named-skipped (tilt harder toward magic). *Next sim if pursued.*

### FINAL honest verdict
**The DCL magic architecture is sound, and its calibration is wide and healthy on every axis except caster
supremacy.** The bounded-stack (MR-4), heal non-stalemate (MR-5), economy window (MR-8), and Faith
two-sidedness (MR-2, with fix #8 retracted and high-magic-LOW reframed as matchup) all **HOLD jointly** with
large margin. **Caster supremacy — the #1 chartered failure — is the real finding: UNREFUTED as specced** (AoE
excluded from the G_m anchor; the AI-rush counter unbuilt). It is **answerable in principle** by the stage-aware
AoE MP-cost fix, which both auditors confirm bounds the central case **without** breaking the mage's identity —
**but M2 is CONDITIONAL**, met only if (a) the **Windows AI-reachability test** passes and (b) the **early-cluster**
and **off-neutral** corners are bounded/accepted as enumerated. If reachability fails, M2 rests on MP-attrition
alone (insufficient) and must escalate to ENTD/Tier-2 AI.

**Bottom line: NOT shippable as specced, with a clear, bounded, mostly-calibration path to sound — and exactly
two items only the live Windows build can close (AI reachability; final stage-scaled G_m/MP tuning).** Every
axis other than AoE/AI caster-supremacy survived the full adversarial pass (4 critics + 2 audit rounds, one
cross-model) with healthy margin. This is the deliverable.

---

## Phase I — CONVERGED (double adversarial sign-off)

**Round-3 confirmation: BOTH auditors returned CONVERGED** — fresh-context Claude
(`convergence-auditor-r3-claude-RESPONSE.md`, ran an independent `audit_r3_stageaware.py` + re-ran the
joint-locks) and cross-model GPT peer (`convergence-auditor-r3-gpt-RESPONSE.md`). Neither found a new decisive
break; both confirm Phase H claims only what the sims proved held and states every open corner as conditional
with a lever.

- **Two further breaks hunted in round 3, both FALSIFIED:** (a) **k≥4 cluster runaway** — bounded through
  **k=8** (crossovers T15–26); the per-target MP cost is *self-correcting* (bigger clusters drain MP faster, so
  the mage floors sooner). (b) **stage-scaled-G_m killing the early bolt floor** — early G_m=0.414 still gives
  bolt floor **0.56** (the early fighter is weak too), so the G_m-scaling↔pool-scaling "equivalence" is accurate.
- **All numbers reproduced** on the trusted physical scale: stage-drift 0.414/0.553/0.639; early k2 T45 / k3
  NEVER (T12 = 1.39/1.61×); pool=40 → early k2 T15; mid k2 T10; late k2 T8 / k3 T13; off-neutral 1.30/1.73;
  joint-locks 22/24, +53–90% margin.
- **M2-CONDITIONAL is correctly (not over-)stated.** Unconditional AI-independent context count ≤1; M2 met IFF
  the Windows reachability test passes.

**Sharpened residual risk (carry this):** the "MP-attrition fallback" is **not** a stage/element-independent
floor — it is robust **only in the neutral mid/late regime.** So the worst realistic case is
**Windows-reachability-fails ∧ (early-game OR off-neutral shared-weakness clustered fight)** → **zero** robust
AI-independent losing contexts there → caster supremacy persists in those fights until ENTD encounter authoring
or a Tier-2 AI-targeting mod closes it. Everything else holds with wide margin.

**This validation is COMPLETE.** Convergence criterion (fresh-context adversarial sign-off, cross-model for the
COI corpus) met by a **double CONVERGED** after 3 audit rounds + 4 Phase-C/D critics + 6 confrontation sims.
