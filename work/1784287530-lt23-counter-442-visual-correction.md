# LT23 Counter 442 visual-correlation correction

## Conflict

The LT23 log records a pass-2 row with `reactionId=442` immediately after Rion's HP changed
`277 -> 37`. The earlier analysis treated the following target HP writes `192 -> 3 -> 0` as a
visibly executed Dual Wield Counter.

The witnessed battle sequence contradicts that presentation claim:

1. Rion enabled Auto-battle on the Archer.
2. Rion moved to the Archer and threw a shuriken.
3. The Archer attacked Rion with a bow.
4. A Chocobo attacked Rion.
5. Rion died.

No visible Counter action occurred.

## Corrected interpretation

LT23 proves that pass 2 can contain an internally accepted record whose presentation id is `442`.
It does not, by itself, prove that the game presented or visibly executed Counter. The adjacent HP
writes are temporally correlated but cannot override the direct visual negative observation,
especially because the polling context rows resolve no attacker.

Any future proof of native Counter execution must correlate all of the following in one controlled
fixture:

- exact incoming source/action;
- pass-2 accepted record `442`;
- materialized executable Attack order and final target;
- state-`0x2C` effect row(s) and HP apply;
- visible Counter presentation/animation.

Until then, `442` is an internal accepted-order marker in LT23, not proof of a visible counterattack.
