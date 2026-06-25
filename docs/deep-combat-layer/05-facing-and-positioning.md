# Facing and Positioning

Status: Draft (structure locked; numbers open)
Date: 2026-06-25
Depends on: 04-hit-and-defense.
Review: Pending.

## Facing

The direction you attack a target from changes its ability to defend:

| Attack from | Defender's active defense |
|-------------|---------------------------|
| **Front** | Full defense roll (`04`). |
| **Side / flank** | **−2** to the defense value. |
| **Back** | **No defense roll at all** — the attack only needs to hit. |

FFT already tracks facing; the DCL gives it teeth. This is also the *anti-frustration* design
Marcelo asked for: a heavily-defended target is not an immovable wall you can't damage, it is a
target you must **out-position**. Attacking a defended foe from the front at ~31% is fine *because*
you have agency — move around it.

A worked feel-check from the session: attacking front-on at ~37% is acceptable as a baseline tension
point *because* flanking and back-strikes give the player a lever to raise it.

## The counterplay triangle

Against a defensive target, there are **three independent ways through**, each attacking a different
part of the defense stack. This is the core tactical decision of the DCL:

```
            FLANK  (beats facing)
              /\
             /  \
            /    \
  FOCUS-FIRE ---- CRUSH + PENETRATION
  (beats depletion)   (beats damage type / armor)
```

1. **Flank / back-strike — beats *facing*.** Move around the target. Side = −2, back = no defense.
   Costs movement and exposes your own flank; rewards mobility and turn economy.
2. **Focus-fire — beats *depletion*.** Pile attacks onto one target before its turn. Its depleting
   Parry/Block drain to the Dodge floor and stay there until it refreshes (`04`). Costs your whole
   team's tempo on one target; rewards coordination and punishes slow defenders.
3. **Crush + penetration — beats *damage type / armor*.** Bring the right tool: crush vs plate,
   armor-divisor missiles vs heavy DR (`03`). Costs a loadout commitment; rewards preparation.

No single answer is universal. A turtle can be cracked three ways, and a smart defender invests to
make each path costly — but cannot close all three at once. This is what keeps a defended target a
*puzzle* rather than a *stat-check*.

## Interaction with guard depletion

Facing and depletion compound. A flanked attack (−2) against a defender whose Block is already
depleted (focus-fire) lands far more often than either lever alone. The triangle's edges are
multiplicative in practice: coordinated teams that flank *and* focus-fire *and* bring the right type
collapse even a dedicated tank — as they should, at the cost of doing all three.

## Open items

Exact facing modifiers (the −2 is provisional), back-strike rules around large/multi-tile units, and
how facing interacts with area magic are calibration/detail items in `12-open-questions.md`.
