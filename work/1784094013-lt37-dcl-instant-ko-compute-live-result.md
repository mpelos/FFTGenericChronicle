# LT37 DCL instant-KO compute-point live result

## Hypothesis

A data-neutralized instant-KO ability can expose an exact probability-weighted lethal debit to AI,
sample its 3d6 contest only when the charged action resolves, cache that final outcome at the
compute point, and deliver it through native HP apply without invoking the legacy KO path.

## Controlled fixture

- Manual Save 05 supplied Josephine, Black Mage, with Death learned through the one-bit fixture.
- Death's native KO rider was replaced by the harmless formula-`0x08`, `X=Y=1`, no-status route.
- The same named mid-battle autosave snapshot was restored before both passes.
- Josephine targeted the same Red Panther, Europe, at `430/430` HP.
- Both passes used resistance `14`, 93/1000 exact success probability, and AI expected debit `40`.
- The only intended A/B setting difference was the forced 3d6 execution roll: `3` versus `18`.

## Evidence

- Resistance log: `work/1784093540-lt37a-dcl-instant-ko-compute-resist-resolved-live.log`
  (`SHA-256 9F9732EB7985252197A9F8CFA1102B2351208B660F1BF81DEC1BAEB132297431`).
- Lethal log: `work/1784093827-lt37b-dcl-instant-ko-compute-ko-resolved-live.log`
  (`SHA-256 95D29BE4633CB51E04DBE321D41ACB99EBD7E0CE140D8DB488F351AB127DBDAE`).

The resistance pass recorded `phase=execution`, `roll=3`, `outcome=resisted`, and `debit=0` exactly
once. State `0x2A` cached `hp=3/0->0/0`; pre-clamp consumed `debit=0`, `oldDebit=3`, and
`computePoint=1`. Europe remained alive.

The lethal pass recorded `phase=execution`, `roll=18`, `outcome=engine-owned-ko`, and `debit=430`
exactly once. State `0x2A` cached `hp=3/0->430/0`; pre-clamp consumed `debit=430`, `oldDebit=3`, and
`computePoint=1`. Native HP apply then recorded `430 -> 0 = 430`, and the battle UI showed the KO.

Neither log contains a `[DCL-KO]` legacy-delivery row.

## Conclusion

The hypothesis is proven for the charged Death vertical. AI expectation, one-shot execution RNG,
compute-point caching, pre-clamp reuse, native KO lifecycle, and data-side native-rider suppression
compose correctly in the same transaction.

## Cleanup

The installed DLL, runtime settings, data NXD, manual save, autosave, and Reloaded-II AppConfig were
restored from the pre-test backups. All six restored targets match their recorded SHA-256 values.
