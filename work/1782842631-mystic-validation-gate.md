# Mystic (#09) — design convergence & validation gate

Date: 2026-06-30. Closes the Mystic design (fourth DCL caster, "The Spiritbreaker"). Registered doc:
`docs/job-balance/jobs/09-mystic.md`. Sim: `work/1782842631-sim-mystic.py` (22/22). Marcelo: **approved**.

## Process

Designed from the vanilla Oracle (`docs/job-balance/vanilla/09-mystic.md`) + the DCL status engine
(`docs/deep-combat-layer/13`) + magic (`11`) + the authoring laws (`15`), never anchored on a prior draft.
GPT peer (thread `019f1030-9ddc-7ab2-8179-0f90cf106a30`) participated per the divergence directive and
**materially redirected** the design several times.

## The paradox this job resolves

Vanilla Oracle is "great kit, terrible hit rates, zero damage, never fielded." The DCL fix is that **Faith
and Brave are two-sided conduits and no other job manipulates them** — so Mystic becomes the **conduit-tuner**:
*degrade a resistance (Belief↑Faith / Trepidation↓Brave), then land the affliction it opened.* Setup makes
control reliable (the whiff fix); a reliable HP-drain makes it self-sufficient (the zero-damage fix).

## GPT's material redirects (the divergence working)

1. **Status suite too broad → omnicapable.** GPT cut **Petrify** (redundant with Frog — both "unit is gone";
   keep Frog, the one moved Black→Mystic). Kept Sleep + Confuse + Frog.
2. **Charm/Berserk are Orator's**, not Mystic's (doc 15 ration "Charm = Orator") — overrides the old lane-lock
   memory that had listed Charm under Mystic.
3. **Faith = NEUTRAL** (converged): status keys off MA, so low Faith = a near-free lunch for a controller;
   neutral keeps an honest two-sided floor and stays disruptable.
4. **Innate veto #1 — "Conduit Focus" (a +status-reliability buff) was a crutch** (and, exported, an
   auto-include on Time Stop / Black Death / Orator Charm). Reliability moved into the **tuning loop** (in the
   command, travels whole); innate became **Astral Resilience** (defense vs magical+mental statuses + tuning;
   export-safe; wanted for its own sake).
5. **Belief as a rest-of-fight +30% = a permanent magic-party tax.** Fixed: tuning is a **finite,
   non-stacking, refresh-only** window → a planned burst window, not a permanent mark (SIM 3).
6. **Self-sufficiency (Marcelo's new law J6).** GPT: contested disables can't be the safety floor.
   **Invigoration** became the reliable engine (formula damage + ~100% capped self-heal + real MP cap ~7
   casts) → solo-clears a low-level pack even surrounded (SIM 6), can't drain-tank a boss.
7. **Status+damage line (Marcelo's idea).** GPT tightened: chip = a low **spell-power** (≈20 at MA16), only
   on **Blind/Silence** (non-breaking); **Frog/Sleep/Confuse/tuning pure** (Frog the capstone stays
   miss-is-the-cost; Sleep/Confuse would break on damage). Removes dead turns without a nuke/dominance (SIM 9).

## Marcelo's three rule-additions (recorded in the docs, not just here)

- **J6 — Self-sufficiency** (`docs/deep-combat-layer/15` + solo-clear gate in `job-design-process.md`): every
  job, alone vs a low-level pack, eliminates all of them — no risk of loss, no stall; the floor is a
  *reliable button*, not a contested status.
- **No crutch exports + Desirability = base stats + innate** (`15`): an innate/support must be wanted for its
  own sake; every innate is exportable; players mine innates for build variability.
- **All damage/healing is formula-derived; only items are flat** (`docs/deep-combat-layer/02` + `15`): tune
  spell-power, never a raw constant. (Mystic's "20 chip" is `MA × 2.15 × faith × G_m`, not a fixed number.)

## Locked design

- **Identity "The Spiritbreaker":** the conduit-tuner; attacks Faith/Brave then lands the opened afflictions.
- **Chassis:** Robes, high MA (status-offense stat), high MP, HP ~75, low PA, neutral Faith, low Brave,
  Speed neutral, Move 3.
- **Innate — Astral Resilience** (resists magical+mental statuses + Faith/Brave tuning; defense-only,
  export-safe).
- **Command:** Core = Belief/Disbelief/Trepidation (finite windows) · Blind/Silence (+ formula damage rider)
  · Invigoration (the self-sufficiency engine) · rod bolt. Tier-2 = Sleep · Confuse · Frog (pure capstone;
  Petrify = open flavor swap) · Empowerment (MP-drain) · Harmony (scoped cleanse).
- **R/S/M:** Hex Ward (Caution reaction) · Astral Resilience + Rod Training (supports) · Phase Step (move
  through units).

## Sim read (`work/1782842631-sim-mystic.py`, 22/22, no forced changes)

- **SIM1** reliability shape: raw 50% / one tune 74% / vs caster 74% no-setup / faithless bruiser 26% even
  with setup (hard-counter). **SIM2** one prep opens one axis only. **SIM3** Belief worth it only vs durable
  targets (D>124) and finite-window-bounded (~263 vs rejected ~1052). **SIM4** boss floor non-dominant
  (51 << 146) but meaningful. **SIM5** Astral Resilience defense-only/export-safe. **SIM6** lone Core-only
  Mystic solo-clears 4/5/tankier packs even surrounded (heal=100%, MP-capped). **SIM7** export J2-clean.
  **SIM8** affliction variance is upside, not the survival floor. **SIM9** damage rider (sp 2.15 < bolt 4 <
  Invig 5.5) removes dead turns without dominance; Frog pure.

## Cross-job impact

- Lane-lock corrected: **Charm/Berserk = Orator** (not Mystic). Time doc (10) "Distinct from" line updated
  (Mystic owns the spiritual-affliction suite — tuning/Sleep/Confuse/Frog/Blind/Silence — not Charm/Petrify).
- Carry to **#13 Orator** (next): it owns Charm + the social/recruitment conversion; confirm the Brave/Faith
  *social* lane vs Mystic's *spiritual-sabotage* tuning so the two don't collide.
- **Necromancer (#19):** Poison/Disease/Doom/drain-state/undead stays reserved.

## Open dependencies / calibration (tagged in the doc, all → `docs/deep-combat-layer/12`)

- Astral Resilience magnitude (resistance bump, not immunity); per-status durations; the tuning-window length
  + boss-reduction; Invigoration heal-% and MP cap (the no-risk floor vs boss-drain balance — GPT lock:
  heals HP only, never funds an MP loop); the chip spell-power; the 3d6 resist curves; Phase Step feasibility
  (Tier-1 vs Tier-2). All numbers are frozen DCL placeholders (G_m 0.58, MA 16, etc.).
