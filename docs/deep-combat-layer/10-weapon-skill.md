# Weapon Skill

Status: Draft (structure locked; per-job/level tables open)
Date: 2026-06-25
Depends on: 04-hit-and-defense.
Review: Pending.

## Skill is per weapon family, defined by the job

Skill is the number the attacker rolls against on the 3d6 hit roll (`04`). In the DCL, **skill is
per weapon family** (swords, spears, bows, axes, staves, …) and is **defined by the job**, not by a
generic character level. A unit's skill with a given family depends on which job it is in and how
developed that job is.

### Job grade → base skill

Each job has a **grade per weapon family**, mapping to a base skill:

| Grade | Base skill |
|-------|-----------|
| A | 13 |
| B | 11 |
| C | 9 |
| D | 7 |
| F | 5 |

So a Knight might be grade A with swords (base 13) and grade D with bows (base 7); a sword in a
Knight's hand is reliable, a bow is clumsy.

### Generalists vs specialists

- **Generalists** (e.g. **Squire**) are **broad but shallow**: many families, all at lower grades.
  Flexible, never excellent.
- **Specialists** are **narrow but deep**: few families, at high grades. Excellent with their tools,
  helpless outside them.

This makes "which job holds which weapon" a real decision instead of "everyone uses the best weapon".

## Growth via Job Level (not JP)

Skill **grows with Job Level.** Each job specifies its skill progression **per job-level** for each
family — leveling a job raises that job's skills along a defined curve. This is a deliberate choice:

- **Job Level drives skill.** It homes the Job Level attribute (`01`) and ties mastery to *playing
  the job*.
- **JP does NOT drive skill.** JP stays the ability-purchase currency (learning abilities), exactly
  as in vanilla. Skill and abilities are separated: you get *better at hitting* by leveling the job,
  and you *learn new things to do* by spending JP.

## Sword Master (equippable +2)

A unit can equip **Sword Master** (a support-slot ability) for **+2 skill**, at the cost of the
support slot. This is the one general skill booster, and it is a real opportunity cost — taking it
means giving up another support ability. It lets a build push a key family over a breakpoint
(e.g. into the crit-widening 15/16 band) by sacrificing flexibility elsewhere.

## The 9–16 band and the skill cap

Skill is tuned to live in the **9–16 band**, where the 3d6 curve and the crit/fumble edges are most
expressive (`04`):

- Below ~9, fumbles dominate and hits are unreliable — the "clumsy with the wrong tool" zone.
- At 15–16, the crit window widens (5 crits at ≥15, 6 at ≥16) — mastery is felt as *more criticals*,
  not just more hits.

Skill is **capped at ~16.** Excess skill beyond the cap **does not raise hit chance further**
(3d6 ≤ 16 is already 98%); instead it **converts into damage / penetration.** So a true master keeps
gaining power past the cap — through harder, more-penetrating blows rather than an unreachable hit
rate. This keeps growth meaningful at the top end without breaking the 3d6 contest.

## Open items

The exact per-job, per-job-level skill tables, the full job×family grade matrix, the Sword Master
value (+2 provisional), and the over-cap skill→damage/penetration conversion rate are deferred —
`12-open-questions.md`.
