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

## Growth: Job Level (primary) + character level (secondary)

Skill **grows from two sources**, so mastery keeps scaling across the whole game — not just to the
Job-Level ceiling of 8:

- **Job Level — the primary driver.** Each job specifies its skill progression **per job-level** for
  each family. This homes the Job Level attribute (`01`) and ties mastery to *playing the job*. Job
  Level is intentionally the **bigger** lever.
- **Character level — the secondary driver, gated by grade.** Skill also rises with the unit's
  character level (→99), so a master at level 99 out-damages the same master at level 10 (the over-cap
  skill→damage path below is what makes that scale). **Crucially the character-level contribution is
  *proportional to job grade*, not a flat bonus** — a grade-A gains real skill per level, a grade-F
  almost none. So **a low-weapon-skill unit stays bad even at high character level**: leveling does
  *not* rescue the wrong tool, and the specialist↔generalist gap **widens** with level instead of
  compressing. (Explicit design target — `12`.)
- **JP does NOT drive skill.** JP stays the ability-purchase currency, as in vanilla — you get
  *better at hitting* by leveling the job, and *learn new things* by spending JP.

### The growth formula

Skill is, with the **shape locked** and the magnitudes carried as calibration (`12`):

```
skill(grade, jobLevel, charLevel)
  = base[grade]  +  rate[grade] × ( J·(jobLevel−1)  +  K·(jobLevel ÷ 8)·(charLevel−1) )
```

| Symbol | Role | Provisional |
|--------|------|-------------|
| `base[grade]` | grade base skill (table above) | A 13 · B 11 · C 9 · D 7 · F 5 |
| `rate[grade]` | how fast a grade turns investment into skill — **low grades barely move** | A 1.00 · B 0.72 · C 0.50 · D 0.32 · F 0.20 |
| `J` | job-level coefficient — the big lever | 2.5 |
| `K` | char-level coefficient, **gated by `jobLevel ÷ 8`** | 0.25 |

Two properties make it serve the intent above:

- **One job level ≈ ten character levels.** `J ÷ K = 10`, and because `rate[grade]` factors out of the
  ratio it holds for *every* grade: one job level always buys ~10 character levels of skill. Job Level
  is the fast, decisive lever; character level is the long tail that keeps a *developed* unit climbing
  from the Job-8 point out to 99.
- **Character scaling is gated by `jobLevel ÷ 8`.** A job-level-1 dabbler earns ⅛ the per-level skill of
  a job-8 master, so grinding character levels cannot rescue an undeveloped job — only *playing the job*
  unlocks the long climb. With the `rate` spread, a maxed grade-F at level 99 still sits **below the
  cap** (skill ≈ 13) while a maxed grade-A reaches ≈ 55 (≈ 39 over-cap): the wrong tool stays wrong and
  the specialist↔generalist gap **widens** with level. (Confidence: **Strong** — structure passes all
  ten design targets across a wide robust plateau in simulation; magnitudes are calibration. `12`.)

### Crossbow & Gun are skill-primary (validation A5)

Most weapons take their damage input from **PA** (`02`); **crossbow and gun instead take it from
weapon skill** — they are *marksmanship* weapons, not strength weapons. Their damage runs the normal
pipeline (`02`) with **`base(skill)` substituted for `base(PA)`**, trait-neutral (no Brave/Faith
multiplier). The **crossbow** turns skill into **raw** damage; the **gun** turns it into
**penetration** (the skill→penetration half of the over-cap below). This is what lets the two scale to
level 99 without PA, MA, or a flat number that would go obsolete in a no-new-equipment mod (`14`).

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

**Consequence — accuracy is an early/mid-game phase; late-game shifts to power.** Because skill keeps
rising with character level, by late-game an *on-grade* unit's skill clears the cap: its hit sits at
the 98% ceiling and the **excess becomes damage / penetration.** So "can I land it?" is the early-mid
question and "how hard / through how much armor?" is the late one. This only stays healthy if the
character-level term is **modest and grade-weighted** (above): an *off-grade* unit — a Knight holding
a bow — must remain in the **clumsy hit band even at 99**, or everyone hits everything late and the
wrong-tool penalty dies. The **crit window keys off the in-band (capped) skill**, not the inflated raw
skill — so leveling does **not** hand everyone the widened 15/16 crit range; crit stays a mark of
*real* (in-band) mastery, not a level perk.

## Open items

The growth-formula **shape is locked** (above); its magnitudes — `base`/`rate`, `J`, `K`, and the skill
cap — remain calibration that moves together with the `base()` table and `G` (`02`, `12`). Still fully
open: the per-job/per-job-level skill tables, the full job×family grade matrix, the Sword Master value
(+2 provisional), and the over-cap skill→damage/penetration conversion rate — `12-open-questions.md`.
