# DCL Barrage carrier checkpoint

## Question

Determine whether Barrage's repeat count and per-strike result application are owned by formula
`0x6A`, editable animation/effect data, or an outer protected battle carrier.

## Offline evidence

- Ability `358` is the Piracy command's fourth action and is AI-usable physical damage.
- Formula `0x6A` performs exactly one dynamic dispatch to the equipped weapon's formula and then
  enters the native normal-attack postprocessor. It contains no local repeat loop.
- The action record sets `WeaponRange=1`, `NormalAttack=1`, and `CounterFlood=1`; `RandomFire` is
  clear.
- `AbilityTypeData` assigns animation `0` and `AbilityEffectNumberFilterData` assigns effect `-1`.
  There is no Barrage-specific repeat sequence in either editable TableData surface.
- Pummel is a useful contrast: it has dedicated animation `104` and effect `96`, while its native
  formula is still one aggregate result. Presentation metadata alone therefore does not establish
  result-apply cardinality.
- The ability classifier now assigns `managed_multistrike` to both physical multihit candidates,
  Pummel and Barrage, so the approval overlay cannot silently discard their carrier requirement.

## Conclusion

**Strong:** Barrage reuses ordinary weapon math and normal-attack postprocessing. Its repetition is
outside formula `0x6A` and outside the editable animation/effect tables. The remaining carrier lies
in the protected outer battle execution layer.

The DCL job design also leaves Barrage hit count, per-hit power, and commitment cost open. Runtime
code must not freeze the vanilla count as final balance policy.

## Remaining proof

Observe one natural Barrage and count calculation-entry, selector, pre-clamp, HP apply, visible hit,
and reaction events. This decides whether the managed DCL route can retain one aggregate apply or
must consume one indexed decision per native strike. No mutating runtime behavior is justified
before that observation.

## Offline validation

- The focused ability-classification smoke test passes with Pummel and Barrage retaining
  `managed_multistrike`.
- The native multistrike analyzer passes every formula and TableData carrier check, including the
  Barrage/Piracy ownership checks.
- The whole-DCL coverage validator passes with 30 mechanisms and now tracks the Barrage weapon
  carrier as a distinct `partial-live-gated` mechanism.
- The complete offline suite passes in 35.6 seconds: Python tooling, static executable scan, C#
  build and smoke tests, all 25 settings files, simulators, dry-runs, and whitespace validation.
- No runtime profile was deployed and no game state was changed.
