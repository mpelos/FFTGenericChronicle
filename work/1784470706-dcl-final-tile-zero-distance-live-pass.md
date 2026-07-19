# DCL final-tile zero-distance live pass

## Inputs

- Runtime profile: `work/1784469176-battle-runtime-settings.dcl-final-tile-convergence-observe.json`.
- Runtime-log SHA-256: `3C328EFCCA0B619A1E0EACF44C837B6272714F58C4A3343AA7AEF4F2E86A2AB0`.

## Result

The previous battle ended and generation `3` began. Enemy index `1` completed an ordinary six-step
route and published event `9`. On the controlled turn, Move was exercised against the unit's own
current tile and the zero-distance case completed. No new final-tile event was published for the
controlled ally.

No `cursor=0/length=0` snapshot appeared. The gameplay zero-distance selection does not call the
terminal movement finalizer and therefore cannot become an accepted final-tile movement event.

## Producer closure

- **Proven live positive:** one ordinary AI move and one ordinary player move each publish exactly
  one accepted post-movement event with converged coordinates.
- **Proven live negative:** preview/cancel, Wait without movement, and current-tile selection
  publish no event.
- **Proven live safety:** the convergence hook causes no crash, movement pause, per-tile stepping,
  visible slowdown, or game-state write.
- **Proven offline:** a defensive `cursor=0/length=0` snapshot is rejected as `no-movement` if an
  engine path ever exposes one.

The producer contract is closed. The next gate is an authored consumer that evaluates and resolves
only after receiving the completed-movement event.
