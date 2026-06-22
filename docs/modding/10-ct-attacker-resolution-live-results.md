# CT Attacker Resolution Live Results

This note records what the live CT tests proved about resolving attacker + target for the
post-damage code-mod path.

## Summary

The CT path is now proven for immediate physical actions in the live game. The runtime can:

- observe the target as the unit whose HP changed;
- resolve the attacker from CT reset/low-CT evidence;
- compute a custom formula from attacker + target stats;
- write the resulting HP back into live memory; and
- verify the write with an immediate readback.

The current demo formula was:

```text
finalDamage = max(1, attacker.pa * 10 - target.faith)
```

The game UI still shows the vanilla/neutered damage number. Our hook runs after that vanilla HP
change, so the real HP total can differ from the UI number.

## Runtime Settings That Passed

The passing CT profile uses:

```json
{
  "ResolveAttackerByCt": true,
  "ResolveAttackerByLowCtFallback": true,
  "CtDropWindowMs": 7000,
  "CtLowFallbackMaxCt": 25,
  "CtLowFallbackWindowMs": 7000,
  "RewriteConditionFormula": "event.isDamage && a.present",
  "FinalDamageFormula": "if(a.present, max(1, a.pa * 10 - t.faith), vanillaDamage)",
  "MinHpFloor": 1
}
```

The important tuning change was widening both CT windows to `7000ms`.

## What Worked

| Action | Vanilla/UI damage | Resolved source | Attacker PA | Target Faith | Custom damage | Verified HP write |
| --- | ---: | --- | ---: | ---: | ---: | --- |
| Beowulf -> Agrias | 3 | `ct-reset` | 5 | 63 | 1 | `319 -> 321`, readback `321` |
| Ninja -> Agrias, hit 1 | 12 | `ct-reset` | 15 | 63 | 87 | `309 -> 234`, readback `234` |
| Ninja -> Agrias, hit 2 | 12 | `ct-reset` | 15 | 63 | 87 | `222 -> 147`, readback `147` |
| Agrias -> Beowulf | 10 | `ct-low` | 11 | 65 | 45 | `304 -> 269`, readback `269` |
| Ramza critical -> Beowulf | 42 | `ct-low` | 20 | 65 | 135 | `227 -> 134`, readback `134` |

Notes:

- The Beowulf result writes HP upward relative to the post-vanilla value because the vanilla hit did
  3 damage and the custom formula only wanted 1 final damage.
- The Ramza critical shows the UI damage can be larger than the neutered baseline, but the custom
  formula still owns the final HP result.
- Dual wield works once the CT window is wide enough.

## What Failed Before Tuning

The earlier profile used:

```json
{
  "CtDropWindowMs": 4000,
  "CtLowFallbackWindowMs": 2500
}
```

That was too narrow for Ninja dual wield. The log showed the Ninja CT drop, but the damage events
arrived just outside the old windows:

- first hit: CT drop age about `4366ms`, seen age about `2880ms`;
- second hit: CT drop age about `4981ms`, seen age about `3495ms`.

Because of that, both hits resolved as `none` and were skipped by the guarded rewrite formula. After
raising the windows to `7000ms`, both hits resolved as `ct-reset` and rewrote correctly.

## Implementation Lessons

- `ct-reset` is the strongest CT source: a non-target unit whose CT recently dropped.
- `ct-low` is a necessary fallback when polling misses the exact useful frame but the actor is still
  near the post-action CT value.
- A CT drop into the low band must refresh the observation's `SeenTick`; otherwise poll-only CT drops
  are visible in logs but excluded from `ct-low`.
- The resolver should log negative evidence. `ctObserved=...` made it obvious why Ninja was being
  rejected.
- Watchers should count any runtime attacker source beginning with `ct-`, not only `ct-reset`.
- `[REWRITE-VERIFY]` is valuable: it proves the write landed in live memory, independent of our
  internal tracking state.

## Limits And Risks

CT is now good enough for immediate physical actions, including dual wield and actions followed by
Wait. It is not a complete final source of truth.

Known limits:

- The UI damage number is still vanilla/neutered, not the custom result.
- Charged spells and delayed actions may occur long after the caster's CT reset.
- Wait is a negative control for future pending-action tracking: Wait can change CT without creating
  a damage action.
- Reactions/counters still need their separate inversion path.
- Status damage, poison, traps, reflect, and other delayed/environmental effects need separate tests.

The safest long-term architecture remains layered:

1. real engine actor/action/target pointers, if found;
2. pending action/cast tracker for delayed actions;
3. `ct-reset` / `ct-low` for immediate actions;
4. counter inversion for reactions;
5. no rewrite when context is ambiguous.

## Next RE Target

The next major investigation should search for the engine's real action context:

- hook-register snapshots around the current HP touchpoint;
- stack/register values that equal known battle-unit pointers;
- candidate pointers for current actor, current action, target list, and pending damage;
- controlled tests for Attack, Wait, charged spell, Aim/Charge, counter, friendly fire, and AoE.

If a stable engine context pointer is found, CT should become a fallback instead of the primary
attacker source.
