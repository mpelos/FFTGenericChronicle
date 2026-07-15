# DCL Dual Wield active-hand identity — offline checkpoint

## Question

Can the existing calc-entry, selector, or pre-clamp surface identify whether a Dual Wield strike uses
the right- or left-hand weapon, so mixed-family Weapon Skill can be routed without a heuristic?

Target: Steam Enhanced build `23901820`, `FFT_enhanced.exe` SHA-256
`841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`.
No process was launched and no installed runtime profile was changed.

## Static findings

The hand-slot block inside calc entry (`0x309EF7..0x309F39`) reads all four attacker equipment words:

```text
unit+0x20 right weapon   unit+0x22 right shield
unit+0x24 left weapon    unit+0x26 left shield
```

Both apparent hand branches write the same byte at RVA `0x7B077C`. The byte is cleared at
`0x309B5C`, then set to `1` only when neither the right pair nor the left pair contains an item id in
the valid `0..260` range (with `0x00FF` treated as the empty sentinel). It is an unequipped/unarmed
condition, not a hand selector. A byte-aligned RIP-relative scan found no readers and only the three
aligned writes at `0x309B5C`, `0x309F11`, and `0x309F39` in real code.

The old multihit capture made `unit+0x1BB` look promising, but the combined static/live audit refutes
it as an attacker hand field:

- resolution code writes target `+0x1BB = 1` at `0x30A528` and conditionally `= 2` at `0x30AA64`;
- the two Dual Wield hits left their target at `+0x1BB = 2`;
- the one Counter hit left its target at `+0x1BB = 1`;
- both Dual Wield pre-clamp result records were byte-identical across the captured `unit+0x1BE..+0x23D`
  window, and both selector events reported `+0x1BB = 2`.

Therefore `+0x1BB` is result/application state on the target. It does not distinguish the attacker's
right and left strike.

The calc-entry has only the known preview/AI sweep and single-action real-code callers. The visible
single-action caller has no strike loop. Selector and pre-clamp expose the target result record but no
proven hand bit. Any right-first/left-second rule or event-ordinal inference remains a hypothesis until
the VM-driven execution sequence is captured live.

## Offline implementation completed

The Weapon Skill mechanism now avoids losing information while that live gate is blocked:

- attacker slots read `RightWeapon +0x20` and `LeftWeapon +0x24` independently;
- each hand independently derives family, job-family grade, base/rate, raw skill, capped skill, and
  over-cap excess;
- Crossbow and Gun independently route capped skill as the damage input;
- Crossbow exposes over-cap raw-damage units; Gun exposes over-cap penetration units;
- conversion rates remain explicitly uncalibrated;
- legacy `dcl.weapon*` aliases deliberately point to the right-hand chain and are documented as
  unsafe for a mixed-family left-hand follow-up.

Mechanism fixtures in `work/1783980809-battle-runtime-settings.dcl-weapon-skill-mechanism.json` are
Ninja × Ninja Blade = A, Ninja × Crossbow = C, and Ninja × Gun = B. The latter two are routing
fixtures, not balance decisions.

## Validation

- `DualHandAudit` build: zero warnings, zero errors.
- Formula runtime smoke tests: passed.
- Mixed Crossbow/Gun hands resolve independent families and grades.
- Crossbow routes excess only to raw-damage units; Gun routes excess only to penetration units.
- Updated mechanism profile: JSON parse passed; settings validator reports zero errors.

## Mandatory live gate

Fold this into LT15 once the unmodified game reaches its menu:

1. equip two weapons from different families and, ideally, different native WP;
2. capture calc-entry, pre-clamp, and selector order for both strikes;
3. dump the attacker order/action record and relevant unit window at every boundary;
4. prove which strike is right and which is left by swapping the two weapons and observing the same
   field or ordering invert predictably;
5. only then bind `dcl.activeWeaponFamily/Skill/Excess` or add a strike generation to the cache.

No HP delta, time window, result-state byte, or assumed first/second ordering is accepted as the hand
identity without the swap control.
