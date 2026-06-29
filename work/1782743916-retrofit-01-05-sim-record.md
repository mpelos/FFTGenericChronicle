# Retrofit 01–05 to the new composition rules — simulation-gate record

The five already-approved jobs were retrofitted to the new composition rules (R/S/M-as-set;
innate exported via a Support; weapon-proficiency exported via a Support bringing source grade) and
re-run through the battle-simulation gate (the gate they predated). All five passed with **no design
change forced** — so per the /loop directive they were applied autonomously without interrupting
Marcelo.

Scripts: `*-sim-squire.py`, `*-sim-chemist.py` (see earlier), `*-sim-knight.py`, `*-sim-archer.py`,
`*-sim-monk.py`. GPT peer (gpt-5.5, thread 019f1030) consulted per job.

## Per-job reads

- **01 Squire** — Stalwart now free innate + exported Support (immunity as a counter-pick, slot-cost,
  doesn't nullify control doors); Field Equip / Basic Arms exports source grades. Sim clean.
- **02 Chemist** — Field Lab free innate + Reagent Kit support (2-slot + no-shield); Field Arms export
  Crossbow C + Knife D. Sim clean (alchemy 30 between Black's 45/27).
- **03 Knight** — Weapon Master free innate + exported Support (master maneuvers off-job at 2 slots);
  Heavy Armor job-gated does NOT export = moat; Arms Training exports Sword B + Flail D, Knight Sword A
  does not. Sim clean (Guard Break ~3.1× incoming, never touches Dodge; soft-tank cracked by crush/flank/magic).
- **04 Archer** — Marksmanship free innate + exported Support (range+height on basic shots; cover stays
  counterplay unless Aim); Bow Training exports Bow A (separate support), Field Arms Crossbow B + Knife D.
  Sim clean (Bow-A on a higher-PA host = ~22% raw sidegrade, no package — flagged for grade-budget recon).
- **05 Monk** — Martial Arts innate+weapon+support COLLAPSE: exports FULL (unarmed weapon + scaling) but
  scaling reads the holder's Monk-job-level (the moat) + no-shield combat-mode clause; Pummel exports
  tightly (personal burst, no overlap with Guard Break); Adrenaline Rush now portable (resets after battle).
  Sim clean: grinded off-job fist 49 < native 51 and loses its wall; low-investment splash = noodle 28;
  real donor prize = Chakra/Revive; no-DR/reach-1/Brave levers all bite (magic TTK ~5).

## Pending (non-blocking, flagged to Marcelo earlier)

- Grade-budget reconciliation under traveling aptitude (doc 15) — does the raw-damage sidegrade % balloon
  at endgame PA spread?
- Universal basic-item floor (touches registered Chemist 02).
