# DCL Fear Reaction-delivery live plan

## Question

Does a unit with DCL-owned Fear retain normal offensive Reactions while the voluntary/AI action
filter rejects opposing targets?

## Fixture and bounded procedure

- Restore `work/1784418805-dcl-fear-josephine-fervor-arthur-999hp-fixture-fixture.png`.
- Josephine has learned Fervor 53 and equipped Shirahadori 451.
- Josephine casts Fervor on herself. If the DCL resistance contest resists, close, restore, and retry.
- After visible Chicken/Fear delivery, let enemy physical actions resolve before Josephine's forced
  turn. Pass Arthur/Josephine turns only when necessary.
- Close FFT after the first accepted Reaction delivery or after Josephine's Fear turn expires.

## Required evidence

- Successful `dcl-fear` packet-add transaction on Josephine.
- A Reaction candidate/commit/effect for Josephine while Fear ownership is active.
- A state-`0x2C` target-authorization row that remains `decision=allow`, including an opposing target.
- No Fear mutation of the Reaction transaction.

## Stop rule

One correlated state-`0x2C` Reaction delivery under Fear ownership is sufficient. A run with no
eligible incoming physical action is inconclusive rather than a failure.
