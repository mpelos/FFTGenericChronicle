# DCL Flail anti-guard mechanism

## Result

The job-free physical contest now implements the Flail's structural anti-guard identity:

```text
Flail versus Parry: derived Parry - 4
Flail versus Block: derived Block - 2
Dodge: unchanged
```

Both values are named settings constants and both final defenses clamp at zero. The rule is selected
from the live active weapon (`action.weapon.category_flail`), so a mixed-hand native repeat applies
the penalty only on the Flail strike. It neither disables a defense nor changes Guard spending: the
ordinary best-defense selector and one-use finite-defense commit remain authoritative.

## Offline falsifiers

The physical mechanism smoke fixture proves that Iron Flail item 67 changes a guarded target's
Parry from 8 to 4 and Block from 12 to 10. The integrated scaffold compares the same target against
an ordinary blade and asserts exact deltas of four and two after both composition stages.

Validation passed:

- C# build: zero warnings/errors;
- compiled formula smoke executable: passed;
- physical profile JSON: valid;
- integration and unified profiles recomposed sequentially;
- unified settings SHA-256:
  `ED7E345607967FA6E5A5D0B16174CF75C56703A17F1BE921437EEBB5AD9C0122`.

## Remaining gate

One live Flail Attack should confirm forecast hit percentage and the selected defense/presentation
against both a weapon parry and a shield block. Other non-job weapon special rules remain a separate
authoring/classification gap.
