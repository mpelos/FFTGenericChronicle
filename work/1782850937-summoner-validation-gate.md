# Summoner (#14) — design convergence & validation gate

Date: 2026-06-30. Closes the Summoner design ("The Siege Summoner", the sixth new DCL job after the four
casters + the Orator). Registered doc: `docs/job-balance/jobs/14-summoner.md`. Sim:
`work/1782850937-sim-summoner.py` (39/39). Marcelo: **approved**.

## Process

Designed from the vanilla Summoner (`docs/job-balance/vanilla/14-summoner.md`) + the DCL magic equation
(`docs/deep-combat-layer/11`, G_m 0.58), the authoring laws (`15`), and the job tree (`00-job-tree.md`),
never anchored on the legacy v0.2 doc. GPT peer (thread `019f1030-9ddc-7ab2-8179-0f90cf106a30`) participated
per the divergence directive across **three rounds** and materially shaped the design; **Marcelo injected one
calibration caution mid-design** (the obsolescence dial below).

## Marcelo's calibration caution (the design is built on it)

**"Don't overdo the commitment — if each summon's commitment is too big, the Summoner becomes OBSOLETE."**
Black just does the job cheaper/faster/more flexibly and nobody pays the tax. This pulls directly against
GPT's round-1 framing that *"the commitment IS the texture, or it's just bigger Black."*

**Resolution — a TWO-TIER commitment.** A **moderate** Core workhorse (short charge, mid-high MP: the
everyday tool + the J6 floor) + **heavy** finishers only (the rare siege ceiling). The dial sweep (SIM 2)
shows a heavy *workhorse* is strictly slower than Black's multi-cast (= obsolete) **and** breaks the
surrounded-J6 floor — two independent pressures pin the everyday summon to moderate.

## GPT's material redirects (the divergence working)

1. **High Faith, not neutral** (round 1): neutral Faith is a hidden free lunch that hardens the glass cannon
   and forces over-tuned summon powers. The Summoner is a true damage caster → **high Faith**, two-sided.
2. **Distinct profiles, not a -ga ladder; trim AoE heal** (round 1): few big committed elemental areas, no
   Fire/Fira/Firaga, no cheap/small versions (that's Black's flexible lane); AoE healing → White's lane.
3. **Innate = Channeling Ward** (round 1): GPT **rejected both** my innate candidates (AoE-safety = breaks
   Black or is a crutch; MP-sustain = undermines the expense). Proposed instead a **protect-while-charging**
   ward — survives chip not focus-fire, does not cut CT/MP/FF/targeting, export-clean to any charger.
4. **The workhorse must be tuned around k≥3, area-committed not target-flexible** (round 2): a *moderate*
   workhorse risked stealing Black's anti-backline job (catching two priority casters with one drop). Fix:
   the area is big + committed + friendly-fire-prone + placement-frictioned; Black's r1 already catches a
   **pair**, so the Summoner's edge **only begins at k≥3**. My round-1 sim was biased (a single bunched
   4-pack) — GPT named the missing boards (2+2 split, moving k3, mixed-resist, scrum, Time+Summon export).
5. **Carbuncle as the second pillar** (round 3, the consensus condition): without a real off-cluster utility,
   Golem alone carries too much and the job becomes a **Golem-bot**. Elevate Carbuncle to the **team
   magic-routing field** (routing not immunity; backfire risk) — three questions per fight (shield physical /
   route magic / is the cluster worth a summon).

## Claude's lane-hygiene catch (no approved doc changed)

Plain **Reflect** is already the approved **Time Mage's** surgical single-target tool (`10`:136). Carbuncle
is therefore the **share-with-distinction** version — a **group/area magic-routing field** (committed,
expensive, backfire, one-reflection/fizzle) — the **magic mirror of Golem**. Distinct from Time by scope
(group vs single), cost (heavy vs mid), and mechanic (routing-field vs clean bounce). GPT confirmed five
boards where Time's surgical Reflect is still the right pick (spread, mixed-magic, low-MP, protect-one,
no-disruption). **No edit to the Time doc** (unlike the Orator, which had to edit Mystic).

## Locked design

- **Identity "The Siege Summoner":** the committed battlefield-scale caster. Four pillars — workhorse summons
  (k≥3 punish) + finishers (siege ceiling) + Golem (team physical wall) + Carbuncle (team magic-routing field).
- **Chassis:** cloth/robes, HP ~70 (most fragile), highest MA/MP, slowest, **high Faith**, Move/Jump low; rod/
  staff for the free range-3 bolt floor (the output is the summons, not the weapon).
- **Innate — Channeling Ward** (collapsed innate + Support export): a modest ward while charging (chip, not
  focus-fire); does NOT cut CT/MP/FF/targeting; export-clean to any charger.
- **Command "Summon":** two-tier damage (moderate workhorse Ifrit/Shiva/Ramuh/Titan · heavy finisher Bahamut/
  Odin) + Golem (turtle-locked) + Carbuncle (routing field) + the free bolt floor.
- **R/S/M:** Support = Channeling Ward (signature export) + generic caster economy (Half MP / Short Charge,
  not owned); Reaction + Movement open.
- **Counterplay:** most fragile/slowest (a diver ends it), narrow damage wheelhouse (Black wins the off-cluster
  boards), focus-fire through the ward, batter down Golem (it breaks), bait Carbuncle then cast.

## Sim read (`work/1782850937-sim-summoner.py`, 39/39, no forced design changes)

- **SIM1** lone Summoner clears a pack — positioned on Ward alone; surrounded only with a Golem-self panic
  button (fragility is real); safe-but-slow J6 floor; never an instant summon.
- **SIM2** Black owns efficiency/tempo (dmg/MP 8.6 vs 4.0); commitment-dial sweep: CT2 worth-it, CT4 obsolete;
  the same over-commitment also breaks surrounded-J6 → workhorse pinned to MODERATE (answers Marcelo).
- **SIM2b** break-board matrix (no-weights dominance test): Summoner wins **only** clean static k≥3; Black
  wins/ties k1, k2 (adjacent + backline), 2+2 split, moving k3, mixed-resist, scrum. Distinction is mechanical
  (Black's r1 catches a pair).
- **SIM3** Golem soaks a burst then breaks; free-refresh would turtle → refresh is costed; physical-only; no
  Protect-stack. Carbuncle = parallel magic pillar (routing not immunity; backfire; share-with-distinction vs
  Time).
- **SIM4** Channeling Ward survives chip (1–2 hits), not focus-fire (3+); doesn't cut the cost; export-clean.
- **SIM5** high Faith = real damage + two-sided fragility; neutral rejected as a free lunch.
- **SIM6** few distinct profiles, not a ladder; can't cheaply cover every element; neutral finishers don't
  undercut Black's Flare/Meteor.
- **SIM7** Summon-secondary is niche (charge+MP don't travel); Time+Summon export = a real combo, not a break
  (Short Charge floored to CT1 by the Telegraph Invariant; MP/FF intact; still loses the non-cluster boards).
- **SIM8** lane: committed barrage + Golem + Carbuncle; NOT AoE heal (White) / element ladder (Black) / clock
  (Time).

## Cross-job impact

- **No approved doc changed.** Carbuncle vs Time-Reflect is a share-with-distinction owned by the Summoner doc.
- **Lane-locks confirmed:** Summoner owns the **committed siege barrage**, the **team physical wall (Golem)**,
  and the **team magic-routing field (Carbuncle)**. AoE heal stays White; element ladder / efficient flexible
  AoE / anti-armour / burst stay Black; clock + surgical single-target Reflect stay Time.
- **Deferred to the Necromancer (#19, unbuilt):** **Lich** as a drain/undead-state tool. If kept on the
  Summoner it is **dark damage only**; the drain/undead/Doom lane belongs to the Calculator→Necromancer slot.

## Open dependencies / calibration (tagged in the doc, all → `docs/deep-combat-layer/12`)

Workhorse spell power / radius / charge / MP (calibrated so the edge begins at k≥3, charge stays moderate);
finisher magnitude / charge / MP; Golem pool size / recast / depletion; Carbuncle routing rules (one-reflection
vs fizzle, targetability, backfire scope) + MP/CT; Channeling Ward flat DR (chip-not-focus-fire); the free bolt
output; the Lich keep/cut decision (Necromancer pass); the open Reaction/Movement slots. All numbers are frozen
placeholders (G_m 0.58; workhorse CT2/MP34/sp10, finisher CT3/MP60/sp20, Golem pool 150/MP40, Ward DR8).
