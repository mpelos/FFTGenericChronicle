# LT13 — Status-authority reclassification

## Question

Does the `target+0x1A8` / `target+0x1D0` pair carry the status that the native formula stages, and
can the DCL safely use it for status suppression/forcing?

## Evidence

The LT10 Kiyomori capture produced two phases:

```text
early: ail(+0x1A8)=0x002B mask(+0x1D0)=0x00 kind=0x00 resFlag=0x08
later: ail(+0x1A8)=0x002B mask(+0x1D0)=0x08 kind=0x08 resFlag=0x01
```

`0x2B` is item id 43, Kiyomori, in the 261-row item catalog. It is not a status id inferred from
the action.

The apply routine has the following native consumer:

```asm
30A761  mov  rax,[0x14186AF70]
30A768  test byte [rax+0x12],08h       ; target+0x1D0 bit 0x08
30A76E  mov  rcx,[0x14186AF68]
30A775  movzx edx,word [rcx+0x1A8]
30A77C  call 30CF38
```

`0x30CF38` accepts only ids `1..0x104`, exactly the item-catalog domain. It rejects unavailable
unit states, calls `0x279064(itemId)`, and either invokes a VM handler or increments
`byte[0x1411A7C00 + itemId]` when the owned count is below 99.

`0x279064` starts by reading `byte[0x1411A7C00 + itemId]`, then scans equipped item words across
battle units and roster structures to count copies. This identifies `0x1411A7C00` as the inventory
quantity array and `0x30CF38` as item-return/inventory authority.

The actual status state is a classic 40-bit layout across four five-byte arrays:

```text
+0x57..+0x5B   innate/equipment source
+0x5C..+0x60   immunity
+0x61..+0x65   effective mirror
+0x1EF..+0x1F3 durable master
```

Blind live evidence changes `effective[1]` and `master[1]` by `0x20`. The Undead diagnostic write
to master/effective byte 0 was already live-proven. Native byte-0 recompute at `0x30D42A` performs:

```text
master[0] &= 0xF2
effective[0] = master[0] | source[0]
```

The expanded offline xref scan is reproducible via
`work/1783942877-lt13-status-stage-xrefs.py`; its raw output is
`work/1783943489-lt13-status-array-xrefs.txt`.

## Result

- **Refuted:** `+0x1A8` is a staged ailment/status id.
- **Refuted:** `+0x1D0` bit `0x08` is a status-apply bit.
- **Strong:** `+0x1A8` is an item/inventory side-effect id in the observed action/apply context.
- **Strong:** `+0x1D0` bit `0x08` gates the item-return/inventory consumer.
- **Proven:** status state and immunity use the four five-byte arrays above.
- **Proven:** direct master/effective writes can apply native status behavior; immunity input can
  reject a native status.

## Safety correction

The legacy `DclStatusSuppressEnabled`, `DclStatusForceId`, and `DclStatusForceValue` settings now
fail validation. Runtime also converts every such request to an empty write plan and logs
`[DCL-STAGED-AUX-WRITE-BLOCKED]`, so skipping the standalone validator cannot reach the unsafe
write. The staged-bundle hook hard-blocks `StagedBundleForceAilment` and
`StagedBundleForceApplyMask` in its unmanaged buffer for the same reason.

The log-only legacy probe remains and reports the corrected labels through `[DCL-STAGED-AUX]`.

## Replacement hypothesis

A DCL status action can use the following data-first pipeline:

1. Remove the native status rider in the mod's action/item data whenever the DCL owns that action.
2. Resolve the action's normal connect/miss through the existing DCL hit decision.
3. Select a per-action status rule containing byte index, mask, resistance category, and duration.
4. Roll the authored 3d6 resistance contest from the full caster/target formula context.
5. On failed resistance, OR the mask into durable master and effective mirror.
6. Track authored duration in DCL state and clear master/effective after the configured target turns.
7. Respect native immunity before rolling and surface the exact authored probability in forecast.

This path preserves native status behavior/presentation while keeping category and duration on the
inflicting skill, which is required for the physical Stun/Knockdown versus magical DA/DM distinction.

## Remaining gates before live testing

- Define and validate a per-action status-rule schema.
- Prove the pre-clamp callback covers status-only actions or identify a later universal per-target
  commit hook.
- Implement offline-testable 3d6 probability and deterministic-roll helpers.
- Implement five-byte add/remove plus DCL-owned duration state and turn-boundary cleanup.
- Define native-rider suppression policy for data-authored DCL actions.
- Integrate status forecast probability without conflating it with the action hit percentage.

