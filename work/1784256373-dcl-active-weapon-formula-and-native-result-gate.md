# DCL active-weapon formula closure and native-result gate

## Scope

This checkpoint follows the LT41J proof that the native repeat carrier reaches calc entry with the
selected weapon. It audits whether the composed DCL formulas actually consume that carrier and
separately investigates the zero-debit state-`0x2F` follow-up seen in the same live log.

## Hypothesis A — capture was proven but formula routing was still right-hand

Confirmed. The source physical profile already selected active-hand Weapon Skill aliases, but the
final damage spine still read right-hand-only values for PA/skill input, weapon power, over-cap,
typed DR, Brave neutrality, and range. The magic profile likewise classified Rod/Staff bolts and
read their element/power from the right slot. `action.weaponMatchesLeft &&
!action.weaponMatchesRight` also lost left-side identity when both item ids were equal.

The source profiles now use native `action.weaponSide == 2` for the side branch and
`action.weapon.*` for item-specific terms. Active aliases cover damage input, over-cap damage,
over-cap penetration, and skill-primary behavior. The integrated dispatch checks a Rod/Staff magic
bolt before the ordinary physical weapon branch; the bolt uses Magic Evade and is excluded from
physical defense.

The audit also found that the composed preview formula returned zero for ordinary physical Attack.
The integration manifests now route physical preview through the same `dcl.weaponModel` selected by
the active native hand; Rod/Staff bolts continue through the shared magic amount. This closes the
offline formula mismatch between preview and execution.

## Hypothesis B — `oldDebit == 0` alone is too coarse to identify a cancelled result

Confirmed. LT41J contains both a completed HP-damage result and a non-HP follow-up:

- line 166: native debit `273`, flags `0x81`, rewritten to the one-point sentinel;
- line 174: native debit `0`, flags `0x01`, also incorrectly rewritten to one point;
- later completed Dual Wield results use positive debit and flags `0x80`.

The stable semantic signal is the numeric high nibble of result flags at unit `+0x1E5`, whose
native selector contract is HP debit `0x80`, HP credit `0x40`, MP debit `0x20`, and MP credit
`0x10`. The `0x01` follow-up is therefore not a zero-magnitude HP hit; it is a non-HP result that the
sentinel profile resurrected.

`FormulaRuntimeContextBuilder` now publishes the pristine flags and four decoded booleans:

- `dcl.oldResultFlags`;
- `dcl.nativeHpDamageResult`;
- `dcl.nativeHpCreditResult`;
- `dcl.nativeMpDebitResult`;
- `dcl.nativeMpCreditResult`.

The runtime supplies the flags both at the post-calculation compute point and at the pre-clamp
fallback. Ordinary physical and magic damage replacement now requires
`dcl.nativeHpDamageResult`. Explicit DCL producers remain explicit exceptions: managed Pummel,
the authored HP/MP conversion fixture, and Undead healing inversion retain their own branches.

This is more precise than `dcl.oldDebit > 0`: a legitimate native HP-damage result with magnitude
zero and bit `0x80` remains eligible for DCL math, while cancellation or MP redirection without the
HP-debit bit stays at zero.

## Offline falsifiers

The C# smoke suite now covers:

1. mixed Crossbow/Gun right and left repeats, including skill input, over-cap route, power, DR, and
   final weapon model;
2. equal item ids with native left-side identity;
3. point-blank range from a left active Spear;
4. mixed Knife/Flame Rod dispatch, with only the active Rod becoming a Fire bolt and using Magic
   Evade;
5. right/left physical and magic-bolt preview equals the corresponding execution model in the
   JSON-composed integration scaffold rather than only duplicated fixture formulas;
6. flags `0x01`, debit zero -> final debit zero;
7. flags `0x80`, debit zero -> DCL weapon model remains eligible;
8. raw flags `0xA1` -> HP-debit and MP-debit booleans only.

Commands passed:

```text
python tools/compose_runtime_settings.py work/1784094553-dcl-runtime-composition-manifest.json --check-only
python tools/compose_runtime_settings.py work/1784168025-dcl-runtime-composition-manifest.json --check-only
dotnet run --project codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c Release
python tools/validate_dcl_runtime_data_pair.py work/1784168025-dcl-unified-sentinel-runtime-data-pair.json
```

## Remaining live gate

No new live test is needed to choose the formula policy: the carrier, result flags, and selector
semantics are already proven. The remaining active-weapon gate is behavioral integration only: run
one job-free mixed-weapon battle in which the composed formula predicts visibly different right and
left results, then verify both applied values and confirm that a non-HP zero-debit follow-up remains
zero.
