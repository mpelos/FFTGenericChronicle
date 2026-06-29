# Squire — battle-simulation gate (retroactive; never simmed before). Frozen DCL placeholders.

Script: `tmp/job-drafts/sim_squire.py` (copy: `work/1782741842-sim-squire.py`). Tests SHAPE.

## Reads (PASS — confirms identity, no design change)
- **J1 PROOF (Stalwart uptime).** In a lockdown comp (enemy locks ~40%/turn), an immune Squire anchor +
  On Your Feet cleanse raises team action-economy **+58%** (24→38 actions/10 turns). In a no-lockdown
  comp: **+0%** — its raw output is low. Sharp, two-sided J1: huge in its niche, dead outside it.
- **SURVIVAL (endurance chassis).** Squire TTK 9.0 vs a Knight DPS (HP130 + finite Block→Dodge8) — more
  durable than the Thief (TTK 4.2, pure Dodge), far less than the Knight (TTK 33.4, DR). A distinct
  "stays up" body, not a hard tank. Focus-fire drains its Block to the Dodge floor.
- **DAMAGE wrong-pick.** Sword C 15.4 eff/turn vs Monk specialist 22.0 → loses the damage race. Axe C
  vs Heavy (crush) 23.1 = a real anti-armor niche; Throw Stone ~12 free ranged chip (uptime). Competent
  generalist, beaten by specialists; in a pure DPS race its immunity is dead weight.
- **WISH heal lane.** Flat ~40 Faith-proof HP-transfer (costs Squire HP) vs White 50 (high Faith) / 30
  (low-Faith floor). Distinct: White better on the faithful + ranged; Wish can't be blanked, works on
  the godless, costs own HP. Multi-lane healing census holds.
- **ENEMY-USE.** Enemy Squire ignores the player's Stun/Knockdown/Don't-Act/Don't-Move; counterplay is
  damage/displacement/mind+halt status/magic/focus. Not oppressive (low output).

## Verdict
Squire design holds under simulation. Retrofit to the new rules (Stalwart exported as a Support; Field
Equip/Basic Arms brings source grades Sword/Knife/Axe C + Crossbow D; R/S/M as a set; Battle-dynamics
section) is compatible — the sim confirms the J1/wrong-pick the retrofit's new J1 wording describes.
