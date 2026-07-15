# DCL ability-by-status policy manifest

Source: `D:/Projects/FFTGenericChronicle/work/1783987218-dcl-ability-classification.csv` (`512` abilities).
Row manifest: `work/1783987219-dcl-status-policy.csv`.

Expanded status rows: **294** across **30** native status tokens; multi-status abilities: **31**.

This report separates add, clear, revive, lifecycle, campaign, and bundle semantics. It does
not assign a resist stat where the DCL document still leaves the status nature undecided.

## Readiness

| Readiness | Ability-status rows |
| --- | ---: |
| authoring-required | 44 |
| campaign-mechanism-required | 2 |
| data-authoring-required | 9 |
| design-decision-required | 39 |
| native-lifecycle-preserved | 5 |
| surface-ready | 195 |

## Operations

| Operation | Rows |
| --- | ---: |
| add-buff-or-trait | 33 |
| add-harmful | 167 |
| remove-ko | 4 |
| remove-negative | 60 |
| remove-positive | 18 |
| special-operation-review | 12 |

## Resistance categories

| Category | Rows |
| --- | ---: |
| beneficial | 33 |
| campaign-side-effect | 2 |
| lifecycle | 14 |
| magical-inverted | 81 |
| mental-will | 24 |
| none | 78 |
| physical-body | 12 |
| special | 11 |
| unresolved-nature | 39 |

## Status-token policy

| Status | Uses | Operations | Categories | Resist stats | Readiness |
| --- | ---: | --- | --- | --- | --- |
| Berserk | 8 | add-harmful, remove-negative | mental-will, none | high-Brave, none | surface-ready |
| BloodSuck | 3 | add-harmful, remove-negative | none, unresolved-nature | design-decision-required, none | design-decision-required, surface-ready |
| Charm | 4 | add-harmful | mental-will | high-Brave | surface-ready |
| Confusion | 19 | add-harmful, remove-negative | mental-will, none | high-Brave, none | surface-ready |
| Crystal | 1 | special-operation-review | lifecycle | none | native-lifecycle-preserved |
| Darkness | 20 | add-harmful, remove-negative | none, unresolved-nature | design-decision-required, none | design-decision-required, surface-ready |
| Dead | 13 | add-harmful, remove-ko | lifecycle | explicit-rule, none | data-authoring-required, native-lifecycle-preserved |
| DeathSentence | 7 | add-harmful | magical-inverted | inverse-Faith | surface-ready |
| DontAct | 12 | add-harmful, remove-negative | magical-inverted, none, physical-body | base-HP, inverse-Faith, none | surface-ready |
| DontMove | 12 | add-harmful, remove-negative | magical-inverted, none, physical-body | base-HP, inverse-Faith, none | surface-ready |
| Faith | 6 | add-buff-or-trait, remove-negative, remove-positive, special-operation-review | beneficial, none, special | explicit-rule, none | authoring-required, surface-ready |
| Float | 5 | add-buff-or-trait, remove-negative, remove-positive, special-operation-review | beneficial, none, special | explicit-rule, none | authoring-required, surface-ready |
| Frog | 15 | add-harmful, remove-negative | magical-inverted, none | inverse-Faith, none | surface-ready |
| Haste | 9 | add-buff-or-trait, remove-negative, remove-positive, special-operation-review | beneficial, none, special | explicit-rule, none | authoring-required, surface-ready |
| Innocent | 2 | add-buff-or-trait | beneficial | none | authoring-required |
| Invite | 2 | add-harmful | campaign-side-effect | design-decision-required | campaign-mechanism-required |
| Oil | 6 | add-harmful, remove-negative, special-operation-review | none, special, unresolved-nature | design-decision-required, explicit-rule, none | authoring-required, design-decision-required, surface-ready |
| Petrify | 19 | add-harmful, remove-negative | magical-inverted, none | inverse-Faith, none | surface-ready |
| Poison | 16 | add-harmful, remove-negative | none, physical-body | base-HP, none | surface-ready |
| Protect | 11 | add-buff-or-trait, remove-negative, remove-positive, special-operation-review | beneficial, none, special | explicit-rule, none | authoring-required, surface-ready |
| Reflect | 7 | add-buff-or-trait, remove-negative, remove-positive, special-operation-review | beneficial, none, special | explicit-rule, none | authoring-required, surface-ready |
| Regen | 8 | add-buff-or-trait, remove-negative, remove-positive, special-operation-review | beneficial, none, special | explicit-rule, none | authoring-required, surface-ready |
| Reraise | 7 | add-buff-or-trait, remove-negative, remove-positive, special-operation-review | beneficial, none, special | explicit-rule, none | authoring-required, surface-ready |
| Shell | 11 | add-buff-or-trait, remove-negative, remove-positive, special-operation-review | beneficial, none, special | explicit-rule, none | authoring-required, surface-ready |
| Silence | 20 | add-harmful, remove-negative | none, unresolved-nature | design-decision-required, none | design-decision-required, surface-ready |
| Sleep | 19 | add-harmful, remove-negative | magical-inverted, none | inverse-Faith, none | surface-ready |
| Slow | 11 | add-harmful | magical-inverted | inverse-Faith | surface-ready |
| Stop | 13 | add-harmful, remove-negative | magical-inverted, none | inverse-Faith, none | surface-ready |
| Transparent | 4 | remove-negative, remove-positive, special-operation-review | none, special | explicit-rule, none | authoring-required, surface-ready |
| Undead | 4 | add-harmful | unresolved-nature | design-decision-required | design-decision-required |

## Mechanism consequences

- Multi-status abilities expand to independent per-status rules; the existing runtime rolls each authored add rule independently.
- `Dead`, `Undead`, and `Crystal` live in status byte 0. Generic `DclStatusRule` whitelists only proven-safe Undead `0x10`; offensive KO uses the dedicated lethal-debit route and Crystal remains native lifecycle state.
- Nine offensive Dead riders have runtime mechanism coverage but still require data-side native-rider suppression plus per-ability ordinary-damage/resistance authoring.
- Raise/Arise/Revive/Squeal preserve the native revive lifecycle. DCL amount authoring is limited to staged HP credit; generic status removal never clears KO.
- Bequeath Bacon preserves native formula `0x57`: bounded target level gain plus caster Crystal/campaign lifecycle. The DCL never writes or clears Crystal.
- `Invite` is a battle/campaign allegiance operation, not a normal temporary status.
- Physical Stun/Knockdown and magical Don't Act/Don't Move share bits; ability ids 213/214 are the current explicit physical candidates, while other uses remain magical by documented nature.
- Darkness, Silence, BloodSuck, Oil, Undead, Faith, and Innocent still need an explicit DCL nature/resistance decision.
