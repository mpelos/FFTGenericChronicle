# Player Feedback Signals

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/02-variable-palette.md`
- `docs/formula-balance/03-family-taxonomy-and-viability.md`
- `docs/formula-balance/05-formula-proposal-protocol.md`
Review: Approved by Claude on 2026-06-20 with no required changes.

## Purpose

This document records player-facing balance signals that should inform formula design.

It is not numeric proof, and it is not a replacement for the Windows baseline/proof plan. It
exists to keep later formulas aligned with how FFT players actually experience equipment,
weapon families, magic, support abilities, and opaque mechanics.

Community rankings and forum posts are evidence of perception, not final balance truth. Treat
them as problem-shaping input that must still be tested through `04`, `05`, and `06`.

## Source Quality

Use these labels when carrying this document into later proposals:

- `guide signal`: curated guide or tier list; useful for perceived meta, not authoritative math.
- `community signal`: forum or Reddit discussion; useful for recurring complaints and play
  patterns, but noisy.
- `developer/remaster signal`: public statement or patch-note coverage; useful for intent and
  official direction.
- `design inference`: conclusion drawn by this project from the signals above.

Sources checked for this document:

- [Game8 weapon tier list](https://game8.co/games/Final-Fantasy-Tactics/archives/555215)
- [Thinking Inside the Box on FFT mages](https://wwwthinkinginsidethebox.blogspot.com/2022/02/final-fantasy-tacticss-mages.html)
- [Steam discussion of IVC balance changes](https://steamcommunity.com/app/1004640/discussions/0/595161733884032609/)
- [Nintendo Life coverage of IVC v1.5.0 patch notes](https://www.nintendolife.com/news/2026/06/final-fantasy-tactics-the-ivalice-chronicles-updated-to-version-1-5-0-adds-new-game-plus-and-much-more)
- [GamesRadar interview coverage on IVC balance philosophy](https://www.gamesradar.com/games/final-fantasy/final-fantasy-tactics-the-ivalice-chronicles-devs-havent-made-any-changes-to-characters-that-were-powerful-because-they-didnt-really-want-to-disappoint-og-fans-with-nerfs/)
- [GameFAQs overpowered/broken mechanics discussion](https://gamefaqs.gamespot.com/boards/197339-final-fantasy-tactics/68311513)
- [Reddit discussion of IVC equipment availability and weapon-class relevance](https://www.reddit.com/r/finalfantasytactics/comments/1q4tvyc/list_of_unattainable_items_in_ivalice_chronicles/)

## Signal 1 - Weapon-Family Imbalance Is Player-Visible

Guide signal:

- Knight's Swords and Guns are commonly presented as top weapon families.
- Knight's Swords are valued for high damage and strong added effects.
- Guns are valued for flat range, perfect accuracy, and weather independence.
- Swords are treated as broadly useful early/general weapons that later fall behind Knight's
  Swords.
- Rods and staves are valued more for caster support or effects than for attack damage.
- Several families are presented as weak, niche, or conditional: axe, flail, pole, crossbow,
  dagger/knife, bag, cloth, book, and instrument.

Community signal:

- Some players explicitly frame late-game equipment availability as steering play toward a small
  set of families such as knight swords, spears, rods, martial arts, or spells.
- The recurring complaint is not only that some weapons have lower numbers. It is that some
  families stop progressing, become job-locked curiosities, or have no reason to be chosen once
  stronger families appear.

Design inference:

```text
The mod should not measure success only by raising low WP values.
It must give each family a durable late-game reason to exist.
```

## Signal 2 - Four Failure Modes Need Explicit Review

These failure modes should be checked in every weapon-family formula proposal:

| Failure mode | Meaning | Typical risk |
| --- | --- | --- |
| Raw outclassing | Another family does the same job with better output or fewer costs | Crossbow, pole, knife, book, and similar families become worse swords |
| Bad volatility | Random or swingy output feels untrustworthy rather than interesting | Axe, flail, and bag become "high ceiling, bad choice" families |
| Job-lock niche | A family is tied to a narrow or weak job context without enough payoff | Cloth, instrument, katana, and ninja blade risk depending too much on one job |
| Convergence | Multiple families collapse into the same best build path | Sword/Knight Sword/support stacking swallows other physical options |

Design inference:

```text
A formula proposal should name which failure mode it solves and which new failure mode it risks.
```

## Signal 3 - Dominance Engines Matter More Than Base Attack Alone

Community signal:

- Players repeatedly identify support/equipment engines as balance-breaking, including Two
  Hands, Two Swords, Blade Grasp, Brave/Faith manipulation, Martial Arts, swordskills, and
  Calculator/Arithmetician-style action rules.
- GameFAQs discussion especially reinforces that high-Brave reaction stacking, Two Swords with
  premium weapons, Excalibur/swordskill combinations, and Math Skill can dominate beyond a
  single base-attack formula.

Design inference:

```text
Every family simulation must test the family in isolation and under plausible support engines.
If a family is fair without supports but collapses under Two Hands, Two Swords, Attack Boost,
Martial Arts, Brave stacking, or similar engines, the formula is not accepted yet.
```

This reinforces `03-family-taxonomy-and-viability.md`: transversal dominance risks are
first-order balance inputs, not polish.

## Signal 4 - Magic And Physical Damage Need Coexistence Checks

Community signal:

- Longstanding player criticism of FFT mages centers on charge time, exposure while casting,
  turn-order pressure, and the risk of spending a turn without resolving the action.
- Faith is a double-edged stat: it can increase magical output while also increasing magical
  vulnerability.
- Brave-heavy physical routes often feel safer because they improve physical tools without an
  equally visible defensive downside.
- IVC discussions and guide content indicate that some charge/cast times were changed, which
  suggests Square Enix also saw action delay as a usability target.

Design inference:

```text
Formula proposals must compare physical and magical routes in equivalent tactical windows, not
only compare damage-per-hit.
```

Minimum review checks:

- damage or effect per successful action;
- CT/action delay and vulnerability window;
- MP/resource pressure;
- Faith downside;
- silence/status exposure;
- expected value after hit rate or failure chance.

## Signal 5 - Accuracy Is A Real Identity Axis

Guide signal:

- Guns are valued partly because their accuracy is perfect and not weather-dependent.
- Bows are more conditional because line, arc, weather, and positioning matter.

Design inference:

```text
Accuracy is not a cosmetic modifier. Reliability can be a primary family identity, but perfect
accuracy plus high range plus high damage is a dominance risk.
```

Formula proposals involving ranged weapons, evasion bypass, Direct flags, or status accuracy
must report both:

- damage on hit;
- expected value after hit rate.

## Signal 6 - Legibility Remains A Hard Constraint

Community and remaster signal:

- Zodiac compatibility has historically been hard to read in play, even though its effect can be
  large.
- IVC v1.5.0 added a Zodiac Compatibility function on unit status screens, improving visibility
  compared with earlier versions.
- Even with better UI, Zodiac remains harder for a player to plan around than WP, range, CT,
  element, PA, MA, Speed, Brave, or Faith.

Design inference:

```text
Zodiac can remain part of FFT's flavor, but it should not become the visible center of a weapon
family formula unless a later proposal proves the payoff is worth the readability cost.
```

This preserves the `avoid-for-legibility` treatment in `02-variable-palette.md`, while noting
that IVC v1.5.0 improves the UI surface.

## Signal 7 - Preserve FFT Feel While Rebalancing

Developer/remaster signal:

- Public IVC interview coverage indicates Square Enix avoided major nerfs to powerful jobs and
  characters because preserving how players remember FFT mattered.
- IVC still made selected usability adjustments, including improving some hard-to-use abilities.

Design inference:

```text
Generic Chronicle can rebalance more boldly than IVC, but the player-facing goal stays the same:
it should feel like a better-balanced FFT, not like a punishment pass over beloved tools.
```

This supports the existing principle that swords can stay good. The design goal is broad
viability with distinct identities, not resentment-driven nerfs.

## Required Use In Formula Proposals

Every future family-level formula proposal should include a short `Player-signal check`:

```text
Player-signal check:
- Failure mode addressed:
- Dominance engine tested:
- Magic/physical coexistence impact:
- Accuracy/reliability impact:
- Legibility impact:
- FFT-feel risk:
```

This check is a review aid, not a separate acceptance gate. A formula can still fail on numeric
simulation, implementation proof, or Claude review even if the player-signal check looks good.

## What This Changes

This document sharpens, but does not replace, prior accepted docs:

- `01-principles.md`: adds concrete player-signal language to family viability, magic
  coexistence, role clarity, and FFT feel.
- `02-variable-palette.md`: confirms accuracy/reliability and legibility as real design axes.
- `03-family-taxonomy-and-viability.md`: turns outclassing, bad volatility, job-lock niche, and
  convergence into explicit family-review risks.
- `05-formula-proposal-protocol.md`: adds a required player-signal check to future proposal
  writeups.

## What Remains Unproven

- Community perception does not prove exact IVC formulas or values.
- Guide tier lists do not prove that a weapon family is mathematically weak in all scenarios.
- IVC v1.5.0 UI changes may reduce some legibility complaints, especially around Zodiac, but do
  not turn opaque mechanics into good primary formula identities by themselves.
- All numeric claims still require baseline data, in-game proof, and dual simulation before a
  formula can be accepted.
