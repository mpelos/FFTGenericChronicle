# DCL physical pipeline modernization — offline checkpoint

## Result

The live-proven LT7 physical damage spine is now composed offline with the newer Weapon Skill,
per-family overcap, Brave, and Zodiac surfaces. This is a modernization of the proven LT7 route,
not a new claim that the pre-clamp delivery mechanism works: same-hit delivery was already proven
by LT7.

The executable profile is
`work/1783980809-battle-runtime-settings.dcl-weapon-skill-mechanism.json`. It now evaluates:

```text
right-hand family + job/level -> raw/capped Weapon Skill + excess
damage input = raw PA, except Crossbow/Gun use capped Weapon Skill
base(input + 4) + weapon power + Crossbow overcap damage
typed armor DR -> family DR divisor -> Gun overcap penetration
penetration floor -> wound multiplier -> Brave trait -> Zodiac -> global scale
```

The right-hand routing is deliberate. Both hands retain independent skill/excess variables, but no
attacker-side active-hand or strike-index field has been proved. Mixed-family Dual Wield therefore
remains barred from a shipping profile until the swap-controlled live gate identifies the current
strike hand.

## Mechanism fixtures

These values prove routing and integer behavior only; they are not final balance decisions:

| Fixture | Value |
| --- | ---: |
| PA/skill to ST offset | 4 |
| penetration floor | 200 permille of gross |
| Crossbow excess to raw damage | 250 permille |
| Gun excess to penetration | 250 permille |
| Crossbow DR share | 750 permille |
| Gun DR share | 500 permille |
| global scale | 1000 permille |
| Ninja × Ninja Blade | grade A |
| Ninja × Crossbow | grade C |
| Ninja × Gun | grade B |

The wound values retain the LT7 fixtures: cut `3/2`, thrust `2/1`, crush and missile `1/1`. Armor is
modernized to the DCL's current three native body classes rather than LT7's obsolete four-class
HP-threshold inference: Armor/heavy `[9,8,3,8]`, Clothing `[2,2,2,2]`, and Robe `[0,0,0,0]`.

## Zodiac encoding

The battle-unit high nibble at `unit+0x09` feeds a circular twelve-sign distance:

- distance 6: Best, `1200` damage permille and `+1` attacker hit target;
- distance 3: Bad, `900` damage permille and no hit modifier;
- distance 0 or 4: Good, `1100` damage permille and no hit modifier;
- all other ordinary distances: Neutral, `1000`;
- either sign code outside `0..11`: neutral mechanism fallback.

Worst remains designed-content policy and is not inferred from ordinary sign bytes.

## Offline assertions

All cases use an Aries attacker, a Libra Leather Armor target, level 99 / Job Level 8, and the
current integer-ordering rules unless noted:

| Weapon | Important intermediate values | Final fixture damage |
| --- | --- | ---: |
| Ninja Blade (id 11) | raw PA 12; gross 24; heavy cut DR 9; wound 22; Brave 70; Best Zodiac | 30 |
| Bowgun / Crossbow (id 77) | raw skill 30; capped 16; excess 14 -> +3 raw; effective heavy missile DR 6; Brave-neutral | 24 |
| Romandan Pistol / Gun (id 71) | raw skill 41; capped 16; excess 25 -> 6 penetration; effective DR 0; Brave-neutral | 31 |

Additional assertions prove:

- reducing Brave from 70 to 30 lowers Ninja Blade damage from 30 to 24;
- the same Brave change leaves Crossbow damage at 24;
- Best Zodiac raises the attack-skill target from 16 to 17;
- Aries against Cancer uses Bad Zodiac (`900`), leaves hit unchanged, and produces 22 blade damage;
- Crossbow excess reaches only raw damage, while Gun excess reaches only penetration;
- Leather Armor is read from the canonical body slot `unit+0x1C` and classifies as Armor/heavy. This
  explicitly rejects LT7's older `armorHpBonus` threshold that treated low-HP Armor records as leather.

## Validation

- smoke project build, configuration `PhysicalDamageAudit`: 0 warnings, 0 errors;
- formula runtime smoke suite: passed;
- profile JSON parse: passed;
- settings validator, configuration `PhysicalDamageValidate`: errors 0; expected invasive-hook warnings only.

## Remaining gates

1. Prove active strike hand with a controlled right/left weapon swap after the unmodified game can
   reach the menu again.
2. Replace the three Ninja grade fixtures with the authored full job×family matrix.
3. Calibrate Crossbow/Gun overcap conversion and DR shares; the routing is implemented, the numbers
   are not locked.
4. Decide and author the designed-content flag that can turn an opposite-sign Best into Worst.
5. Extend the same Zodiac term and the Faith/element stack through the magic/healing spine.
