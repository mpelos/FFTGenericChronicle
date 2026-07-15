# DCL MP budget and trickle — offline mechanism pass

## Design-to-engine decomposition

The DCL does not require a second resource pool. Native current MP (`unit+0x34`) and MaxMP
(`unit+0x36`) already express a per-battle budget; job and equipment data author the pool size, while
native ability data continues to author spell costs. The only missing economy primitive is the
designed small trickle.

The new runtime surface is disabled by default:

- `DclMpTrickleEnabled`;
- `DclMpTrickleFormula`, evaluated in the full unit/equipment DCL formula context;
- `DclMpTrickleMaxLogs`.

Each tracked unit owns an edge state. First observation initializes without granting MP, including
when the mod attaches during an active turn. A false→true transition of `unit+0x1B8` evaluates the
formula once. The result is clamped to `0..(MaxMP-currentMP)` and written to `unit+0x34`. Pointer reuse
with a different character id resets the edge state without granting.

## Offline validation

- fresh `DclMpEconomyValidate3` build: zero warnings/errors;
- complete formula-runtime smoke suite passed;
- edge fixtures cover attach-mid-turn, held-active non-repeat, falling-edge arming, exactly one rising
  edge, and pointer reuse;
- numeric fixtures cover ordinary credit, MaxMP clamp, negative result, and full MP;
- settings validator accepts a KO-gated formula and rejects enabled-with-empty-formula.

## Confidence and remaining gate

- **Proven:** current/max MP fields and direct current-MP write surface are already used by the runtime
  harness.
- **Strong:** `+0x1B8` is the unit own-turn marker used by duration and finite-guard state.
- **Hypothesis:** one credit on its rising edge has the desired player-visible timing and survives all
  native turn-start processing.

LT19 is the bounded live gate. Pool sizes, trickle amount, spell costs, and which jobs receive the
trickle remain calibration/content authoring; this pass implements the mechanism without inventing
those values.
