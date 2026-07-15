# DCL Parry / Block formulas — offline checkpoint

## Result

The physical profile no longer supplies constant Parry and Block values.

Parry derives the defender's own right-hand Weapon Skill through the same job×family, Job Level,
character-level, grade-growth and cap pipeline used by the attacker:

```text
Parry = floor(defenderWeaponSkill / 2) + 3
```

It is eligible only when a real right-hand weapon is equipped and evaluates to zero against missile
attacks. The guard pool therefore remains available across the turn but cannot be selected or spent
for a ranged attack.

Block derives from the equipped right-shield S-Ev metadata:

```text
Block = 7 + floor(shieldPhysicalEvasion / 10)
```

It is eligible only when the slot resolves a shield. Block remains available against missiles, which
preserves its DCL identity as the strong ranged active defense.

Both conversion formulas are mechanism fixtures; the runtime routes the correct inputs and coverage,
but their constants remain calibration.

## Offline assertions

- level-50 / Job-Level-8 Ninja with an unmapped F-grade Sword derives Weapon Skill 10 and Parry 8;
- Venetian Shield S-Ev 50 derives Block 12;
- Crossbow/missile sets Parry to zero while Block stays eligible;
- the existing best-defense chooser therefore receives real Dodge/Parry/Block values before applying
  Brave and facing.

Build `DefenseFormulaAudit2`, the complete smoke suite, JSON parse and settings validation pass with
zero errors.

## Remaining gates

- Author the full job×family grade matrix; defender Parry currently inherits the sparse fixture map.
- Decide left-hand weapon Parry policy and active-hand identity for Dual Wield.
- Calibrate the Parry and shield curves against the target hit bands.
- LT15 must prove selection, depletion, missile coverage and own-turn refresh in the current build.

