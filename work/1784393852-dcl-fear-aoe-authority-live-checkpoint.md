# DCL Fear AoE authority live checkpoint

## Prepared correction

The voluntary-confirm callback now derives caster and action identity from the live turn-owner slot.
The actor-linked unit remains diagnostic because the single-target fixture reported actor slot `0`
while the turn owner and later execution caster were Josephine slot `17`.

The Release DLL was built with zero warnings/errors. Its SHA-256 is
`5D9254B8C06B036F84E6129B931CE6813AA4D8CDA9F0635FD712297EEA420298`.

## Bounded profile

`work/1784393852-battle-runtime-settings.dcl-fear-aoe-authority-live.json` keeps the already validated
Fear mechanism but explicitly sets `DclFearPlayerConfirmEnforcementEnabled=false`. Fire ability `16`
is not given a DCL status rule; it exists only to exercise native area expansion. Basic Attack `0`
remains the isolated technical Fear carrier.

## Required row

Josephine slot `17` selects Black Magicks > Fire centered on adjacent Arthur. The tester records every
unit highlighted by the forecast before confirming. The decisive pre-confirm row must report:

- `casterSource=turn-owner`;
- `casterIdx=17` and `turnOwner=17`;
- `type`/`ability=16` for Fire;
- `listAuthoritative=1`;
- `expandedTargets` equal to the complete visibly highlighted unit set;
- `decision=allow`, because enforcement is disabled and Josephine is not a DCL-Fear owner.

Any missing or extra target refutes private-list authority. The run stops without arming rejection.

## Offline gates

- complete `codemod/run-offline-checks.ps1`: PASS in 92.5 seconds;
- status/settings validator: PASS with mechanism warnings only;
- Fear pre-confirm, boundary, and forced-route analyzers: PASS;
- documentation timeless gate: PASS.

