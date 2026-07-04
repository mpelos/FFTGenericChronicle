# Time Mage (#10) — design convergence & validation gate

Date: 2026-06-30. Closes the Time Mage design (third DCL caster, "The Clockbreaker"). Registered doc:
`docs/job-balance/jobs/10-time-mage.md`. Sim harness: `work/1782837029-sim-time-mage.py`. Awaiting Marcelo
validation (new job).

## Process

Pre-seeded during the Black Mage impasse (Marcelo: "Black takes all offense — what's left for Time, which is
too support?"). Designed from the vanilla Time Mage (`docs/job-balance/vanilla/10-time-mage.md`) + the DCL
turn/CT model (`docs/deep-combat-layer/01`) + statuses (`13`) + magic (`11`), never anchored on a prior draft.
GPT peer (thread `019f1030-9ddc-7ab2-8179-0f90cf106a30`) participated per the divergence + every-decision
directives and **materially redirected** the design (below).

## The paradox this job resolves

Vanilla Time is **A/S for optimizers** (tempo is FFT's strongest lever) yet **plays** like a useless-by-itself
buff-bot — a FEEL problem, not a power problem. The DCL also makes tempo *the* job-owned axis: doc 01 decouples
Speed from Dodge and says explicitly the "agile/tempo playstyle" should come from **job skills**, not an
attribute — i.e. the tempo axis is left for a job, and Time is it. So the design keeps tempo strong, makes it
FEEL active, and guards the one way it breaks the game (instant bursts).

## GPT's material redirects (the divergence working)

1. **Load-bearing fork = Quick + Short Charge stacking, not Stop.** Stop is legible (single-target, contest,
   immunity, duration); Quick+Short-Charge can erase the charge window every charged burst relies on. GPT's
   break line: *Haste→Black, Short-Charge Firaga, Quick→Black, second Firaga before reposition.* → became the
   **Telegraph Invariant**.
2. **Compass:** my "Conductor" (sounds like ally-buff support) → GPT's **"Clockbreaker"** (sells "I attack the
   turn order" — Marcelo's active-feel need). Conceded.
3. **Faith:** I leaned LOW → GPT: **NEUTRAL** (low Faith + MA-driven control double-dips: strong control *and*
   better magic/status resistance on robes/Teleport/Reflect; and Comet/bolt need Faith to matter). Conceded.
4. **Reflect:** not a Core starter (volatile, bounce-cheese) → **mid/Tier-2-lite**. Float = Core utility.
5. **Float ownership:** Float = Time's **command utility** (status spell), Teleport = Time's **movement** →
   resolves Black's deferred movement: **Black takes no signature movement** (Black doc updated).

## Locked design

- **Identity "The Clockbreaker":** owns the turn-frequency axis; weaponizes the clock. Support/control PRIMARY,
  but self-sufficient (Gravity + Comet + bolt). Never a damage caster (Black), never the afflictions
  (Mystic/Oracle owns Sleep/Confuse/Petrify; Time owns the CLOCK — Slow/Stop/Haste/Quick).
- **Chassis:** Robes (sprite), good MA (below Black), HP ~75, **neutral Faith** (least self-vulnerable caster),
  moderate Speed, low Brave, Move 3, Rod A (bolt floor).
- **Innate — Short Charge** (free + the signature export; White-Liturgy/Black-Rod-Attunement pattern), bound by
  the Telegraph Invariant (moderate, additive-capped vs Haste).
- **Command — Time Magic.** Core: Haste, Slow (the active offensive button — denies a turn + lags guard so the
  team cracks it), Gravity (de-niched %-HP softener, can't finish), Comet (~-ra single-target minimum offense),
  Float. Tier-2: Stop (one hard-denial door, inverse-Faith, boss-immune), Quick (costed extra-turn that can't
  resolve/erase a charge), Hasteja/Slowja, Graviga, Reflect (mid), Meteor (capstone timing-puzzle).
- **R/S/M:** Mana Shield (real MP fuel) · Short Charge + Rod Training exports · Teleport (distance-risk).
- **★ THE TELEGRAPH INVARIANT (stated as a roster rule in the doc):** *Time may improve timing windows, but it
  may not erase the readable charge window of charged offense.*

## Sim read (`work/1782837029-sim-time-mage.py`, 7 SIMs, all pass; no forced changes)

- **SIM1 ★ Telegraph Invariant:** enemy reposition-windows during a -ga charge — none 2.0 / moderate SC 1.34 /
  Haste-only 1.33 / **SC×Haste multiplicative (no cap) 0.89 = BREAK** / **SC×Haste additive-cap (floor 1.0) =
  OK** / near-instant SC(.90)+Haste 0.13 = BREAK. Proves the two guardrails (moderate SC + additive cap) are
  load-bearing; Quick adds a 2nd *telegraphed* cast and can't resolve a charge.
- **SIM2 Stop/Slow:** inverse-Faith land low 16 / neutral 38 / high 62%, boss 0%; guard-crack ~+20 effective
  dmg per missed refresh on a Slowed frontliner.
- **SIM3 Gravity:** de-niched (20/37.5/75 across HP 80/150/300), 3.0 on a 12-HP target = can't finish.
- **SIM4 Comet:** 67.9 — above Black neutral Fire 62.6, BELOW Fira 104 / Firaga 146 / weak 190 / Flare 125.
  One button, below the arsenal.
- **SIM5 Mana Shield:** eff HP 75→147 at full MP (TTK 1.4→2.7), but full-shield = zero tempo output and
  focus-fire still kills. Flagged: ratio must keep the survive-vs-control choice real.
- **SIM6 Quick:** all lines bounded (Quick-Black/Summoner still telegraph; Quick-into-Quick needs 2 Times or
  the whole budget; charge-not-resolved hard exception). **SIM7:** Gravity+Comet+bolt = a solo damage line
  every turn — not the 100%-support trap; Black still vastly out-damages.

## Open dependencies / calibration (tagged in the doc)

- **★ Short Charge magnitude is NOT free tuning** (doc 12, Hypothesis): must preserve ≥1 enemy reposition
  window under the worst legal Haste stack; if it fails, reduce Short Charge before touching Black/Summoner
  damage.
- **Mana Shield MP→HP ratio** (doc 12, Hypothesis): must keep the survive-vs-control choice real; if full
  shielding still leaves the tempo budget intact, the ratio is too generous.
- **Quick** downgrades to a bounded CT-advance if any residual loop survives sim at #-calibration.
- **Teleport distance-risk** magnitude; per-status durations for Stop/Slow; the inverse-Faith→3d6 curve; the
  guard-refresh-crack magnitude — all calibration (doc 12).
- All numbers are frozen DCL placeholders (G_m 0.58, Faith band, MA 13, Comet sp 9, Gravity 25%). Real
  calibration is `docs/deep-combat-layer/12`.

## Cross-job impact

- **Black doc updated:** movement note resolved (Float = Time's; Black takes no signature movement).
- **Mystic/Oracle (#09) boundary reserved:** the mind/body affliction suite (Sleep/Confuse/Charm/Berserk/
  Petrify/Blind/Silence) is Mystic's; Time owns only the clock statuses (Slow/Stop) + Haste/Quick. Carry to #09.
- **Summoner (#14):** the heavy committed barrage stays reserved; Meteor is a *lower-reliability* capstone, not
  that barrage.
