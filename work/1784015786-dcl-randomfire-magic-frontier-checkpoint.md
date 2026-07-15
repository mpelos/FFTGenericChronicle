# DCL RandomFire magic-multistrike frontier checkpoint

## Live environment note

The user launched FFT Enhanced through Reloaded-II and confirmed that startup completed normally
after choosing Reloaded. This confirms the installed game/profile launch path is healthy. It does
not clear the independent privileged-computer-control failure recorded in the autonomous runbook.

## Offline findings

- The DCL defines Magic Evade once per final target at spell level, including each victim of an AoE.
  Native repetition does not by itself turn that rule into one roll per projectile.
- The runtime now has a pure magic-multistrike decision resolver. It supports the canonical
  per-target policy and an explicit per-strike exception, records landed/evaded counts, and computes
  exact aggregate any-hit probability without owning native result application.
- The complete native `RandomFire` inventory is 16 enemy-usable actions: ids `169..180`, `255`, and
  `342..344`. Every record uses formula `0x1E`, `0x1F`, or `0x5E`; the common formula body produces
  one ordinary MA result and the protected outer consumer owns repetition and target selection.
- Celestial Void, Corporeal Void, and Dark Whisper combine repetition with hostile status riders.
  `managed_multistrike_status_rider` preserves both responsibilities in metadata instead of losing
  one when the other is implemented.
- The DCL says a status contest follows a successful skill landing, but it does not define the
  rider's once-per-target versus once-per-projectile cadence for repeated magic. That cadence remains
  unbound.

## Safety boundary

The pure resolver is deliberately not attached to pre-clamp damage or status delivery. If the
protected outer carrier invokes several native applies for one target, writing the full aggregate at
each callback would duplicate damage. The observe-only profile
`1784015549-battle-runtime-settings.randomfire-cardinality-observe.json` records calculation,
selector, pre-clamp, action-boundary, and target-result events without changing combat state.

## Next proof

Capture one natural `RandomFire` action and count its per-target calculation, selector, pre-clamp,
HP/status presentation, and apply events. The observed shape selects whether the runtime consumes one
aggregate per final target or one indexed decision per native projectile, and whether damage and
status rider require separate carrier ownership.
