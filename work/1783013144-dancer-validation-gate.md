# Dancer (#15) — design convergence & validation gate

Date: 2026-07-02. Closes the Dancer design ("The Phase Dancer", the seventh new DCL job). Registered doc:
`docs/job-balance/jobs/15-dancer.md`. Sim: `work/1783013144-sim-dancer.py` (61/61). Marcelo: **approved**.

## Process

Grounded in the vanilla Dancer (`docs/job-balance/vanilla/15-dancer.md`) + the legacy v0.2 rebase
(`docs/job-balance/legacy/jobs/18-dancer.md`, leveraged for the dance-by-dance lane notes) + the DCL weapon
framework (`docs/deep-combat-layer/14` Cloth/Bag, `06` reach-2). Never anchored on v0.2 numbers. GPT peer
divergence was **heavy and structural across five rounds** (threads `019f1a44…` for v1–v4, then a fresh
`019f1f47…` for v5+ after the first thread's session dropped). Marcelo drove the design personally through
**four full reversals** — this was a co-design, not a review.

## The design journey (four Marcelo-driven reversals)

The registered v5.2 is the fifth shape. The prior four are **dead — do not resurrect**:

1. **v1–v2 "Spotlight & Shadow"** — a single-enemy mark (Holofote) consumed by a Grand Finale + a Lure
   (pull) + a step-out survival valve. **Killed** by Marcelo: **no forced movement of any unit** exists in
   FFT (Lure + the granted step both gone; memory `no-forced-movement`); and it was too thin / off-pattern.
2. **v3** — action-gated evasion, still fragile ("survive one round then die"). Superseded by v4.
3. **v4 "The Whirl"** — durable-via-reach (medium HP + reach-2 + modest evasion), a menu of at-will
   audience-scaled AoE dances (War/Withering/charged Finale). **Killed** by Marcelo: he wanted the vanilla
   dances back **by name**, and a different survival philosophy.
4. **v5** — Marcelo's own mechanic (the phase cycle) with GPT's V4.1-style caps. **Killed** by Marcelo: the
   lane caps (9 CT, 35 % Forbidden) were "absurdly weak" — not worth the dive; the Dancer must be **heroic /
   Ninja-comparable**.
5. **v5.2 (registered)** — the phase cycle + **risk-premium** heroic numbers + the earned-premium anti-farm
   lock + Cloth/innate/RSM.

## Marcelo's load-bearing directives (the design is built on these)

- **Keep all 7 vanilla dances, same names, DCL-adapted.** Not map-wide → **radius 2**, self-centred.
- **The phase cycle (his invention):** charged dances; **while dancing** she gains audience-scaled physical
  evasion (crowd = shield, "3 = nearly impossible to hit with physical"), decaying live if they leave; **after
  resolution she is completely vulnerable** (the exposure window — a knight acting then kills her). Fragile
  normally + high mobility; not worth dancing at a single enemy.
- **Short charges** (so the exposure window pulses often) — his own fix to his own scenario (the 3-knight,
  5-turn-charge death).
- **High risk / high reward, heroic, Ninja-comparable.** Weak lane-protection numbers are the wrong tool;
  protect lanes by **mechanism + delivery + risk**, price her numbers **above** the specialists' (the risk
  premium). This is now a general roster principle (recorded in the doc).
- **Make the Cloth interesting** — honour vanilla's high W-EV (silk ~50 %); she (and IVC Bard) are the only
  equippers.

## GPT's material breaks (the divergence working) — all adopted

1. **v4.1:** an at-will audience-scaled AoE on a fast durable body = a caster in melee clothes → Grand Finale
   costed/charged, caps tightened. (Superseded when Marcelo pivoted to v5.)
2. **v5.1:** "broken as written" — (a) **projectile physical (arrows/bolts/guns) must BYPASS** the evasion
   (else a ranged attacker needs ~50 shots; guns stop being the anti-evasion answer / Orator lane); (b) the
   **Exposure Invariant** ("Curtain Call") — a minimum zero-evasion recovery slice Haste/Quick/CT can't erase
   (else align CT so enemies waste turns into 92 % and never hit the window = a tank that dances); (c)
   exposure = **zero evasion only, no +damage** (a +25 % lets one ordinary 3-hit pileup delete 60 HP);
   (d) Minuet 24 (J6 math); lane caps.
3. **v5.2 farm break:** with **Golem** (or any hard risk-deletion) covering the exposure window, she farms the
   aud-3 premium risk-free (two safe aud-3 Forbiddens → ~82 % of a pack statused). Fix: the **earned-premium
   rule** — hard-warded performances cap at the audience-2 band (collapses 82 % → ~51 %); partial mitigation
   (armour/Protect/Shell/heal) stays legal.
4. **Pieces round (Cloth/innate/RSM), "no consensus as written", 5 sub-breaks:** Cloth parry must **deplete
   harshly** (50/25/0, else a flat 50 % → 6 % kill in transit = too safe); **Stage Grace export caps at aud2
   without a performer weapon** (else a ~110-HP Monk + Dance-secondary out-dances the Dancer by body — two
   uncapped cycles ~97 dmg survives, capped ~117 dies); **Fly → Performance Step** (+1 Move/ignore-height on
   own move, no pass-through/ZoC/hover — no Thief-lane leak, no universal export); **Encore cut** (a
   post-resolution CT-1 erases the scatter/telegraph counterplay, worse on the Bard); **Stage Grace procs on
   Dance actions only** (else the Bard gets evasion by singing = lock break); **Bag +1-audience-band rejected**
   (a 33 % risk discount on the premium tier) → flat debuff riders only.

## Locked design (v5.2)

- **Identity "The Phase Dancer":** Tier A, high-risk/high-reward performer; dive → charge (evasion up) →
  resolve (audience-scaled) → Curtain Call (defenceless) → repeat. Radius 2, audience cap 3.
- **Chassis:** ♀, Cloth (light/no-DR/avoidance), HP ~60, HIGH Speed, Move 4, neutral B/F. Cloth or Bag weapon.
- **The 7 dances (heroic, risk-premium, audience ladders):** Mincing Minuet (dmg 8/16/24), Polka & Heathen
  Frolic (phys/magic offence sap 10/18/25 %), Slow Dance (−10/−20/−35 CT one-shot, no Stop), Witch Hunt (MP
  burn 12/24/35), Forbidden Dance (random soft {Blind,Silence,Slow,Poison} 35/55/75 %, no hard-control),
  Last Waltz (long-charge capstone, ~40/target; instant-KO rejected).
- **Innate — Stage Grace:** the command carries the cost (charge + Curtain Call travel), the innate carries
  the shield (physical evasion 30/70/92, Dance-action-gated, aud-2 export cap without a performer weapon,
  heavy-armour-suppressed). Projectile/magic/status bypass; live-audience re-check.
- **Cloth weapon:** the only 1H reach-2 + best 1H parry (depleting 50/25/0, suppressed in the Curtain Call, no
  double-dip with Stage Grace, guns bypass). Bag = reach-1 utility, flat debuff riders (never fakes audience).
- **R/S/M (identical to Bard, parity law):** Earplugs / Stage Grace / Performance Step. Encore cut.
- **Earned-premium rule:** hard risk-deletion (Golem/Cover/invuln) caps her at the aud-2 band.

## Sim read (`work/1783013144-sim-dancer.py`, 61/61, no forced design changes)

- **SIM 2** Marcelo's 3-knight board: long charge → 60→11 HP (dead), short charge → 60→29 HP (j0playable) —
  the short-charge fix confirmed. **SIM 3** a camping Dancer dies in ~2 cycles (phased safety, not a blender).
- **SIM 4** live-decay (walk-away → aud1 → 70 % hit); projectile + magic + status bypass. **SIM 5** the
  Exposure Invariant. **SIM 6** J6 clears 3 mooks in 2 cycles, ~38/60 HP (knife-edge by design).
- **SIM 8** risk-premium lanes: Slow −105 CT total but one-shot/no-Stop (Time intact); Forbidden ~2.25 statuses
  but random/soft (Mystic intact); Golem-farm collapses 82 %→51 % under the earned-premium cap.
- **SIM 11** Cloth depleting parry (~49 on 60 HP in transit, ~37 % kill). **SIM 12** Stage Grace export cap
  kills the Monk-host abuse. **SIM 13** R/S/M parity.

## Cross-job impact

- **No approved doc changed.** Lanes held by mechanism+risk, not by editing neighbours.
- **New roster principle recorded:** **risk-premium pricing** — lane protection is mechanism/delivery/risk,
  not weak numbers; a high-risk job's per-action numbers may exceed a safe specialist's.
- **New roster rule recorded:** the **earned-premium** anti-farm lock (hard risk-deletion caps a risk-priced
  payoff).
- **Cloth weapon defined** (`docs/deep-combat-layer/14` had it stubbed as low-parry — now the best 1H parry;
  the doc-14 Cloth row should be updated to reflect reach-2 + best-1H-parry + depleting when convenient).
- **Bard #16 (mirror, unbuilt):** same phase-cycle skeleton (charged, area-2 **ally** audience, resolution
  sampled) but **NO evasion** (his audience is safe) and songs **damage-interruptible** (he pays interruption,
  she pays exposure); lower per-body payoff; **one shared capped performance layer** (no buff+sap blender);
  no Haste/Quick/heal/ward/Brave-Faith/Charm; **R/S/M records identical** to the Dancer (parity law).

## Open dependencies / calibration (tagged in the doc → `docs/deep-combat-layer/12`)

All audience ladders; the Stage Grace evasion ladder + aud-2 export cap; the Cloth parry depletion + wmod; Bag
rider values; charge lengths + exposure-window minimum slice; radius (2) / cap (3); the earned-premium trigger
set (which hard-null/redirect layers cap to aud-2). Forbidden's table + hard-control exclusions ride
`docs/deep-combat-layer/13`. The doc-14 Cloth row update (reach-2 + best-1H-parry) is a small follow-up edit.
